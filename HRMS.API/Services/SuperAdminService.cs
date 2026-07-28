using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using System.Security.Cryptography;

namespace HRMS.API.Services;

public class SuperAdminService : ISuperAdminService
{
    private readonly ISuperAdminRepository _superAdminRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<SuperAdminService> _logger;
    private readonly IAuthRepository _authRepository;
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IJobsRepository _jobsRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public SuperAdminService(
        ISuperAdminRepository superAdminRepository,
        IPaymentRepository paymentRepository,
        ILogger<SuperAdminService> logger,
        IAuthRepository authRepository,
        IAuthService authService,
        IEmailService emailService,
        IOrganizationRepository organizationRepository,
        IJobsRepository jobsRepository,
        IEmployeeRepository employeeRepository)
    {
        _superAdminRepository = superAdminRepository;
        _paymentRepository = paymentRepository;
        _logger = logger;
        _authRepository = authRepository;
        _authService = authService;
        _emailService = emailService;
        _organizationRepository = organizationRepository;
        _jobsRepository = jobsRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<ServiceResponse<PlatformDashboardDto>> GetPlatformDashboardAsync()
    {
        try
        {
            var dashboard = await _superAdminRepository.GetPlatformDashboardAsync();
            return new ServiceResponse<PlatformDashboardDto> { Success = true, Data = dashboard };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting platform dashboard");
            return new ServiceResponse<PlatformDashboardDto>
            {
                Success = false,
                Message = "Failed to load dashboard data"
            };
        }
    }

    public async Task<ServiceResponse<PagedResult<OrganizationListItemDto>>> GetOrganizationsAsync(OrganizationFilterRequest filter)
    {
        try
        {
            var result = await _superAdminRepository.GetOrganizationsPagedAsync(filter);
            return new ServiceResponse<PagedResult<OrganizationListItemDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organizations list");
            return new ServiceResponse<PagedResult<OrganizationListItemDto>>
            {
                Success = false,
                Message = "Failed to load organizations"
            };
        }
    }

    public async Task<ServiceResponse<OrganizationDetailDto>> GetOrganizationDetailAsync(Guid organizationId)
    {
        try
        {
            var detail = await _superAdminRepository.GetOrganizationDetailAsync(organizationId);
            if (detail == null)
            {
                return new ServiceResponse<OrganizationDetailDto>
                {
                    Success = false,
                    Message = "Organization not found"
                };
            }
            return new ServiceResponse<OrganizationDetailDto> { Success = true, Data = detail };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization detail for {OrganizationId}", organizationId);
            return new ServiceResponse<OrganizationDetailDto>
            {
                Success = false,
                Message = "Failed to load organization details"
            };
        }
    }

    public async Task<ServiceResponse<List<SubscriptionHistoryDto>>> GetSubscriptionHistoryAsync(Guid companyId)
    {
        try
        {
            var history = await _superAdminRepository.GetSubscriptionHistoryAsync(companyId);
            return new ServiceResponse<List<SubscriptionHistoryDto>> { Success = true, Data = history };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription history for {CompanyId}", companyId);
            return new ServiceResponse<List<SubscriptionHistoryDto>>
            {
                Success = false,
                Message = "Failed to load subscription history"
            };
        }
    }

    public async Task<ServiceResponse<bool>> ManageSubscriptionAsync(Guid companyId, ManageSubscriptionRequest request, Guid performedBy)
    {
        try
        {
            var validActions = new[] { "upgrade", "downgrade", "extend", "cancel" };
            if (!validActions.Contains(request.Action.ToLower()))
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = $"Invalid action. Allowed values: {string.Join(", ", validActions)}"
                };
            }

            // Get current subscription
            var currentSubs = await _paymentRepository.GetCompanySubscriptionsByCompanyIdAsync(companyId);
            var currentSub = currentSubs.OrderByDescending(s => s.EndDate).FirstOrDefault();

            if (currentSub == null && request.Action.ToLower() != "upgrade")
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "No active subscription found for this organization"
                };
            }

            Guid? oldPlanId = currentSub?.PlanId;
            DateTime? oldEndDate = currentSub?.EndDate;

            switch (request.Action.ToLower())
            {
                case "upgrade":
                case "downgrade":
                    if (!request.NewPlanId.HasValue)
                    {
                        return new ServiceResponse<bool>
                        {
                            Success = false,
                            Message = "New plan ID is required for upgrade/downgrade"
                        };
                    }

                    var plan = await _paymentRepository.GetSubscriptionPlanByIdAsync(request.NewPlanId.Value);
                    if (plan == null)
                    {
                        return new ServiceResponse<bool>
                        {
                            Success = false,
                            Message = "Selected plan not found"
                        };
                    }

                    var updateRequest = new UpdateCompanySubscriptionRequest
                    {
                        PlanId = request.NewPlanId.Value,
                        Status = "active",
                        BillingCycle = request.BillingCycle ?? currentSub?.BillingCycle ?? "monthly",
                        StartDate = DateTime.UtcNow,
                        EndDate = request.NewEndDate ?? DateTime.UtcNow.AddMonths(
                            (request.BillingCycle ?? currentSub?.BillingCycle) == "annual" ? 12 : 1)
                    };

                    if (currentSub != null)
                    {
                        await _paymentRepository.UpdateCompanySubscriptionAsync(currentSub.SubscriptionId, updateRequest);
                    }
                    else
                    {
                        // Create new subscription
                        await _paymentRepository.CreateCompanySubscriptionAsync(new CreateCompanySubscriptionRequest
                        {
                            CompanyId = companyId,
                            PlanId = request.NewPlanId.Value,
                            Status = "active",
                            BillingCycle = request.BillingCycle ?? "monthly",
                            StartDate = DateTime.UtcNow,
                            EndDate = updateRequest.EndDate
                        });
                    }
                    break;

                case "extend":
                    if (!request.NewEndDate.HasValue)
                    {
                        return new ServiceResponse<bool>
                        {
                            Success = false,
                            Message = "New end date is required for extension"
                        };
                    }

                    await _paymentRepository.UpdateCompanySubscriptionAsync(currentSub!.SubscriptionId,
                        new UpdateCompanySubscriptionRequest
                        {
                            PlanId = currentSub.PlanId,
                            Status = "active",
                            BillingCycle = currentSub.BillingCycle,
                            StartDate = currentSub.StartDate,
                            EndDate = request.NewEndDate.Value
                        });
                    break;

                case "cancel":
                    await _paymentRepository.UpdateCompanySubscriptionAsync(currentSub!.SubscriptionId,
                        new UpdateCompanySubscriptionRequest
                        {
                            PlanId = currentSub.PlanId,
                            Status = "cancelled",
                            BillingCycle = currentSub.BillingCycle,
                            StartDate = currentSub.StartDate,
                            EndDate = DateTime.UtcNow
                        });
                    break;
            }

            // Log history
            await _superAdminRepository.LogSubscriptionHistoryAsync(
                subscriptionId: currentSub?.SubscriptionId ?? Guid.Empty,
                companyId: companyId,
                action: request.Action.ToLower(),
                oldPlanId: oldPlanId,
                newPlanId: request.NewPlanId,
                oldEndDate: oldEndDate,
                newEndDate: request.NewEndDate ?? (request.Action == "cancel" ? DateTime.UtcNow : null),
                performedBy: performedBy,
                notes: request.Notes
            );

            _logger.LogInformation("Subscription {Action} for company {CompanyId} by admin {AdminId}",
                request.Action, companyId, performedBy);

            return new ServiceResponse<bool>
            {
                Success = true,
                Data = true,
                Message = $"Subscription {request.Action} completed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error managing subscription for {CompanyId}", companyId);
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "Failed to manage subscription"
            };
        }
    }

    public async Task<List<GlobalRoleDetailResponse>> GetGlobalRolesAsync()
    {
        var result = await _superAdminRepository.GetGlobalRolesAsync();

        if (result == null || !result.Any())
            return new List<GlobalRoleDetailResponse>();

        var roles = result
            .GroupBy(x => new
            {
                x.RoleId,
                x.RoleName,
                x.Description,
                x.Version,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt
            })
            .Select(g => new GlobalRoleDetailResponse
            {
                RoleId = g.Key.RoleId,
                RoleName = g.Key.RoleName,
                Description = g.Key.Description,
                Version = g.Key.Version,
                IsActive = g.Key.IsActive,
                CreatedAt = g.Key.CreatedAt,
                UpdatedAt = g.Key.UpdatedAt,

                PermissionsCount = g
                    .Where(x => x.ActionId != Guid.Empty)
                    .Select(x => x.ActionId)
                    .Distinct()
                    .Count(),

                Menus = g
                    .Where(x => x.MenuId != Guid.Empty)
                    .GroupBy(x => new
                    {
                        x.MenuId,
                        x.MenuName
                    })
                    .Select(mg => new RoleMenuPermission
                    {
                        MenuId = mg.Key.MenuId,
                        MenuName = mg.Key.MenuName,

                        SubMenus = mg
                            .Where(x => x.SubMenuId != Guid.Empty)
                            .GroupBy(x => new
                            {
                                x.SubMenuId,
                                x.SubMenuName
                            })
                            .Select(sg => new RoleSubMenuPermission
                            {
                                SubMenuId = sg.Key.SubMenuId,
                                SubMenuName = sg.Key.SubMenuName,

                                Actions = sg
                                    .Where(a => a.ActionId != Guid.Empty)
                                    .Select(a => new RoleActionPermission
                                    {
                                        ActionId = a.ActionId,
                                        ActionName = a.ActionName,
                                        ActionKey = a.ActionKey,
                                        HasPermission = a.HasPermission
                                    })
                                    .DistinctBy(a => a.ActionId)
                                    .ToList()

                            }).ToList()

                    }).ToList()
            }).ToList();

        return roles;
    }

    public async Task<GlobalRoleActionResponse> CreateGlobalRoleAsync(CreateGlobalRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoleName))
            throw new ArgumentException("Role name is required");

        var existingRole = await _superAdminRepository.GetGlobalRoleByNameAsync(request.RoleName);

        if (existingRole != null)
            throw new InvalidOperationException("Global role already exists");

        foreach (var menu in request.Menus)
        {
            var menuExists = await _authRepository.MenuExistsAsync(menu.MenuId);

            if (!menuExists)
                throw new ArgumentException($"Menu does not exist: {menu.MenuId}");
        }

        var roleId = await _superAdminRepository.CreateGlobalRoleAsync(request);

        return new GlobalRoleActionResponse
        {
            RoleId = roleId
        };
    }
    public async Task<GlobalRoleActionResponse> UpdateGlobalRoleAsync(UpdateGlobalRoleRequest request)
    {
        var existingRole = await _superAdminRepository.GetGlobalRoleByIdAsync(request.RoleId);

        if (existingRole == null)
            throw new KeyNotFoundException("Global role not found");

        var response = await _superAdminRepository.UpdateGlobalRoleAsync(request);
        if (!response)
            throw new Exception("Failed to update global role");
        return new GlobalRoleActionResponse
        {
            RoleId = request.RoleId
        };
    }
    public async Task<bool> DeleteGlobalRoleAsync(Guid roleId)
    {
        var existingRole = await _superAdminRepository.GetGlobalRoleByIdAsync(roleId);

        if (existingRole == null)
            throw new KeyNotFoundException("Global role not found");

        return await _superAdminRepository.DeleteGlobalRoleAsync(roleId);
    }

    public async Task<List<MenuDto>> GetMenusAsync()
    {
        var result = await _authRepository.GetMenusAsync();

        if (result == null || !result.Any())
            throw new InvalidOperationException("No menus found");

        var menus = result
         .Where(x => x.menuid != null)
         .GroupBy(x => new { x.menuid, x.menuname, x.menuicon })
         .Select(g => new MenuDto
         {
             MenuId = g.Key.menuid,
             MenuName = g.Key.menuname,
             Icon = g.Key.menuicon,
             SubMenus = g
                 .Where(x => x.submenuid != null)
                 .GroupBy(x => new { x.submenuid, x.submenuname, x.submenuicon })
                 .Select(sg => new SubMenuDto
                 {
                     SubMenuId = sg.Key.submenuid,
                     SubMenuName = sg.Key.submenuname,
                     Icon = sg.Key.submenuicon,
                     Actions = sg
                         .Where(x => x.actionid != null)
                         .Select(x => new ActionDto
                         {
                             ActionId = x.actionid,
                             ActionName = x.actionname,
                             ActionKey = x.actionkey
                         })
                         .DistinctBy(a => a.ActionId) // Remove duplicate actions if any
                         .ToList()
                 }).ToList()
         })
         .ToList();

        return menus;
    }

    // ========== User Invitation Methods ==========

    public async Task<ServiceResponse<bool>> InviteUserAsync(InviteUserRequest request, string portalUrl)
    {
        try
        {
            // Check if email is already taken
            var emailExists = await _authRepository.EmailExistsAsync(request.Email);
            if (emailExists)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "A user with this email already exists"
                };
            }

            // Invitation is always for a NEW organization (matches self-registration flow)
            // Create user directly without fn_create_user (which requires an existing org)
            // The org, role, and employee setup happens in SetPasswordAsync when user sets their password
            var placeholderPassword = Guid.NewGuid().ToString("N") + "!Aa1";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(placeholderPassword);

            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = passwordHash,
                FirstName = request.FirstName,
                LastName = request.LastName
            };
            var savedUser = await _authRepository.CreateUserWithoutOrganizationAsync(newUser);

            // Set user as inactive until they set their password
            await _superAdminRepository.DeactivateUserAsync(savedUser.UserId!.Value);

            // Generate cryptographically secure invitation token
            var tokenBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            var token = BitConverter.ToString(tokenBytes).Replace("-", "").ToLowerInvariant();

            // Save invitation (48-hour expiry) with optional organization name
            var expiresAt = DateTime.UtcNow.AddHours(48);
            await _superAdminRepository.SaveInvitationAsync(savedUser.UserId.Value, token, expiresAt, request.OrganizationName);

            // Organization name for email (use admin's choice or default)
            var orgName = request.OrganizationName ?? "Your Organization";

            // Send invitation email
            var setPasswordLink = $"{portalUrl.TrimEnd('/')}/set-password?token={token}";
            await _emailService.SendUserInvitationAsync(request.Email, request.FirstName, orgName, setPasswordLink);

            _logger.LogInformation("User invitation sent to {Email}", request.Email);

            return new ServiceResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Invitation sent successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inviting user {Email}", request.Email);
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "Failed to send invitation"
            };
        }
    }

    public async Task<ServiceResponse<PagedResult<UserInvitationDto>>> GetInvitationsAsync(UserInvitationFilterRequest filter)
    {
        try
        {
            var result = await _superAdminRepository.GetInvitationsPagedAsync(filter);
            return new ServiceResponse<PagedResult<UserInvitationDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invitations list");
            return new ServiceResponse<PagedResult<UserInvitationDto>>
            {
                Success = false,
                Message = "Failed to load invitations"
            };
        }
    }

    public async Task<ServiceResponse<bool>> ResendInvitationAsync(Guid invitationId, string portalUrl)
    {
        try
        {
            var invitation = await _superAdminRepository.GetInvitationByIdAsync(invitationId);
            if (invitation == null)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invitation not found"
                };
            }

            // Check if user has already completed setup
            var completed = await _superAdminRepository.UserHasCompletedSetupAsync(invitation.UserId);
            if (completed)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "User has already completed setup"
                };
            }

            // Deactivate old invitations for this user
            await _superAdminRepository.DeactivateOldInvitationsForUserAsync(invitation.UserId);

            // Generate new token
            var tokenBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            var newToken = BitConverter.ToString(tokenBytes).Replace("-", "").ToLowerInvariant();

            // Save new invitation
            var expiresAt = DateTime.UtcNow.AddHours(48);
            await _superAdminRepository.SaveInvitationAsync(invitation.UserId, newToken, expiresAt);

            // Get user details for email
            var user = await _authRepository.GetUserByIdAsync(invitation.UserId);
            if (user == null)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Get organization name
            var orgName = "Your Organization";
            var orgDetail = await _superAdminRepository.GetOrganizationDetailAsync(user.OrganizationId!.Value);
            if (orgDetail != null) orgName = orgDetail.Name;

            // Send new invitation email
            var setPasswordLink = $"{portalUrl.TrimEnd('/')}/set-password?token={newToken}";
            await _emailService.SendUserInvitationAsync(user.Email!, user.FirstName!, orgName, setPasswordLink);

            _logger.LogInformation("Invitation resent to {Email}", user.Email);

            return new ServiceResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Invitation resent successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending invitation {InvitationId}", invitationId);
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "Failed to resend invitation"
            };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteInvitationAsync(Guid invitationId)
    {
        try
        {
            var invitation = await _superAdminRepository.GetInvitationByIdAsync(invitationId);
            if (invitation == null)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invitation not found"
                };
            }

            var completed = await _superAdminRepository.UserHasCompletedSetupAsync(invitation.UserId);
            if (completed)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Cannot delete invitation for user who has completed setup"
                };
            }

            var deleted = await _superAdminRepository.DeleteInvitationAsync(invitationId);
            return new ServiceResponse<bool>
            {
                Success = deleted,
                Data = deleted,
                Message = deleted ? "Invitation deleted" : "Failed to delete invitation"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invitation {InvitationId}", invitationId);
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "Failed to delete invitation"
            };
        }
    }

    public async Task<ServiceResponse<SetPasswordValidationDto>> ValidateInvitationTokenAsync(string token)
    {
        try
        {
            var invitation = await _superAdminRepository.GetInvitationByTokenAsync(token);
            if (invitation == null)
            {
                return new ServiceResponse<SetPasswordValidationDto>
                {
                    Success = false,
                    Data = new SetPasswordValidationDto { Valid = false, Message = "Invalid token" },
                    Message = "Invalid token"
                };
            }

            if (invitation.Used)
            {
                return new ServiceResponse<SetPasswordValidationDto>
                {
                    Success = false,
                    Data = new SetPasswordValidationDto { Valid = false, Message = "Token has already been used" },
                    Message = "Token has already been used"
                };
            }

            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                return new ServiceResponse<SetPasswordValidationDto>
                {
                    Success = false,
                    Data = new SetPasswordValidationDto { Valid = false, Message = "Token has expired" },
                    Message = "Token has expired"
                };
            }

            // Get user details
            var user = await _authRepository.GetUserByIdAsync(invitation.UserId);
            if (user == null)
            {
                return new ServiceResponse<SetPasswordValidationDto>
                {
                    Success = false,
                    Data = new SetPasswordValidationDto { Valid = false, Message = "User not found" },
                    Message = "User not found"
                };
            }

            // This is always a new organization invitation — user MUST provide org name
            // Return admin's suggested name (if any) so user can see/override it
            return new ServiceResponse<SetPasswordValidationDto>
            {
                Success = true,
                Data = new SetPasswordValidationDto
                {
                    Valid = true,
                    FirstName = user.FirstName,
                    Email = user.Email,
                    OrganizationName = invitation.OrganizationName, // admin's choice (nullable)
                    RequiresOrganization = true // always — user must set/confirm org name
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invitation token");
            return new ServiceResponse<SetPasswordValidationDto>
            {
                Success = false,
                Message = "Failed to validate token"
            };
        }
    }

    public async Task<ServiceResponse<bool>> SetPasswordAsync(SetPasswordRequest request)
    {
        try
        {
            var invitation = await _superAdminRepository.GetInvitationByTokenAsync(request.Token);
            if (invitation == null)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invalid token"
                };
            }

            if (invitation.Used)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Token has already been used"
                };
            }

            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Token has expired"
                };
            }

            // Get user details
            var user = await _authRepository.GetUserByIdAsync(invitation.UserId);
            if (user == null)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Always create a new organization (this is a new-org-only invitation)
            var orgName = request.OrganizationName;

            // Check if organization name already exists
            var existingOrg = await _organizationRepository.GetOrganizationByNameAsync(orgName);
            if (existingOrg != null)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = $"An organization named '{orgName}' already exists. Please choose a different name."
                };
            }

            // Create new organization (fn_create_organization copies global roles incl. "Super Admin")
            var newOrg = new Organization
            {
                Name = orgName,
                AdminEmail = user.Email,
                Status = "active"
            };
            var createdOrg = await _organizationRepository.CreateOrganizationAsync(newOrg);

            if (!createdOrg.OrganizationId.HasValue || !user.UserId.HasValue)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Failed to create organization"
                };
            }

            var newOrgId = createdOrg.OrganizationId.Value;

            // === Mirror self-registration: full org scaffolding ===

            // 1. Create default job application stages
            var defaultStages = new[] { ("Initial", 1), ("Short List", 2), ("Interviewed", 3), ("Selected", 4), ("Rejected", 5) };
            foreach (var (stageName, stageOrder) in defaultStages)
            {
                await _jobsRepository.CreateStageMasterAsync(newOrgId, stageName, stageOrder, false);
            }
            _logger.LogInformation("Default job stages created for organization {OrgId}", newOrgId);

            // 2. Create default departments and positions
            var allRoles = await _authRepository.GetOrganizationRolesAsync(newOrgId);
            var departments = new List<(
                string DeptName,
                string Description,
                List<(string PositionName, string RoleName)> Positions)>
            {
                (
                    "Human Resources",
                    "This is default department of Human Resources",
                    new List<(string PositionName, string RoleName)>
                    {
                        ("Manager", "Manager")
                    }
                ),
                (
                    "Marketing",
                    "This is Marketing Department",
                    new List<(string PositionName, string RoleName)>
                    {
                        ("Team Lead", "Employee"),
                        ("Trainee", "Employee")
                    }
                )
            };
            foreach (var dept in departments)
            {
                var createdDepartment = await _employeeRepository.CreateDepartmentAsync(new Department
                {
                    DepartmentName = dept.DeptName,
                    Description = dept.Description,
                    OrganizationId = newOrgId
                });
                foreach (var pos in dept.Positions)
                {
                    var role = allRoles.FirstOrDefault(r => r.RoleName == pos.RoleName);
                    if (role == null)
                        continue;
                    await _authRepository.CreatePositionAsync(
                        newOrgId,
                        role.RoleId.Value,
                        createdDepartment.DepartmentId,
                        pos.PositionName
                    );
                }
            }
            _logger.LogInformation("Default departments and positions created for organization {OrgId}", newOrgId);

            // 3. Update user's organization
            await _authRepository.UpdateUserOrganizationAsync(user.UserId.Value, newOrgId);

            // 4. Find the "Super Admin" role and assign it to the user
            var superAdminRole = await _authRepository.GetRoleByNameAsync(newOrgId, "Super Admin")
                ?? await _authRepository.GetRoleByNameAsync(newOrgId, "SuperAdmin")
                ?? await _authRepository.GetRoleByNameAsync(newOrgId, "Organization Admin")
                ?? await _authRepository.GetRoleByNameAsync(newOrgId, "Admin");
            if (superAdminRole != null)
            {
                await _authRepository.UpdateUserRoleAsync(user.UserId, superAdminRole.RoleId);
                _logger.LogInformation("Assigned role '{RoleName}' to user {UserId} in organization {OrgId}",
                    superAdminRole.RoleName, user.UserId, newOrgId);
            }
            else
            {
                _logger.LogWarning("No admin role found in organization {OrgId} for user {UserId}", newOrgId, user.UserId);
            }

            // 5. Create employee record for the admin
            user.OrganizationId = newOrgId;
            user.RoleId = superAdminRole?.RoleId;
            user.employeecode = GenerateEmployeeCode();
            await _authRepository.adminIntoEmployeeAsync(user);

            // 6. Create "Super Admin" position and link to employee
            var superadminEmployeeId = await _authRepository.GetEmployeeIdByEmailAsync(user.Email);
            if (superAdminRole?.RoleId != null)
            {
                var adminPosition = await _authRepository.CreatePositionAsync(newOrgId, superAdminRole.RoleId.Value, null, "Super Admin");
                await _authRepository.AdminIntoEmployeeDetailsAsync(superadminEmployeeId, adminPosition);
            }
            else
            {
                _logger.LogWarning("Super Admin role not found in organization {OrgId} — skipping position creation for admin", newOrgId);
            }

            // 7. Run onboarding configuration
            await _authRepository.OnboardingConfigurationAsync(newOrgId);

            _logger.LogInformation("Organization {OrgId} ({OrgName}) fully scaffolded for user {UserId} — matches self-registration flow",
                newOrgId, orgName, user.UserId);

            // Hash the new password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Update user: set password
            await _authRepository.UpdateUserPasswordAsync(invitation.UserId, passwordHash);

            // Activate user and mark email as verified
            await _superAdminRepository.ActivateUserAsync(invitation.UserId);

            // Mark invitation as used
            await _superAdminRepository.MarkInvitationAsUsedAsync(request.Token);

            _logger.LogInformation("Password set for user {UserId} via invitation", invitation.UserId);

            return new ServiceResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Password set successfully. You can now log in."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting password via invitation");
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "Failed to set password"
            };
        }
    }

    private string GenerateEmployeeCode()
    {
        var random = new Random();
        int number = random.Next(1000, 9999);
        return $"EMP{number}";
    }
}
