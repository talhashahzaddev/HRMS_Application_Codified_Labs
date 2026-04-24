using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Authorization;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.Formula.Functions;
using System.Security.Claims;

using System.Text.Json;



namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ServiceResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var response = await _authService.AuthenticateAsync(request);
            
            _logger.LogInformation("Generated JWT Token: {Token}", response.Token);
            
            return Ok(new ServiceResponse<AuthResponse>
            {
                Success = true,
                Message = "Login successful",
                Data = response
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Email not found during login");
            return NotFound(new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for login");
            return BadRequest(new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for login");
            return BadRequest(new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error during login");
            return BadRequest(new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email: {Email}", request.Email);
            return StatusCode(500, new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = "An error occurred during login"

            });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ServiceResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var response = await _authService.RefreshTokenAsync(request);
            return Ok(new ServiceResponse<AuthResponse>
            {
                Success = true,
                Message = "Token refreshed successfully",
                Data = response
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for token refresh");
            return BadRequest(new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for token refresh");
            return BadRequest(new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return StatusCode(500, new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = "An error occurred during token refresh"
            });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<bool>>> Logout()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _authService.LogoutAsync(userId);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Logout successful",
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument during logout");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred during logout"
            });
        }
    }

    // Removed users endpoint - registration now handles both organization and user creation

    [HttpGet("users/{id}")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<UserDto>>> GetUser(Guid id)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new ServiceResponse<UserDto>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            return Ok(new ServiceResponse<UserDto>
            {
                Success = true,
                Data = user
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for retrieving user {UserId}", id);
            return BadRequest(new ServiceResponse<UserDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, new ServiceResponse<UserDto>
            {
                Success = false,
                Message = "An error occurred while retrieving user"
            });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<UserDto>>> GetCurrentUser()
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _authService.GetUserByIdAsync(userId);
            
            return Ok(new ServiceResponse<UserDto>
            {
                Success = true,
                Data = user
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ServiceResponse<UserDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user");
            return StatusCode(500, new ServiceResponse<UserDto>
            {
                Success = false,
                Message = "An error occurred while retrieving user information"
            });
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<ServiceResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var response = await _authService.RegisterAsync(request);
            return Ok(new ServiceResponse<AuthResponse>
            {
                Success = true,
                Message = "Registration successful",
                Data = response
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for registration");
            return BadRequest(new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for registration");
            return BadRequest(new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for email: {Email}", request.Email);
            return StatusCode(500, new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = "An error occurred during registration"
            });
        }
    }


    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody]  ResetPasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var result = await _authService.ResetPasswordAsync(request);

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Password reset  successfully",
                Data = result
            });



        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for reset password");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reset password failed for email: {Email}");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred during password reset"
            });

        }

    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ServiceResponse<bool>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var result = await _authService.ForgotPasswordAsync(request);
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Password reset email sent successfully",
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for forgot password");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Forgot password failed for email: {Email}", request.Email);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while processing password reset request"
            });
        }
    } 

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<bool>>> UpdateProfile([FromForm] UpdateProfileRequest request)
    {
        try
        {

            // Manual deserialization of JSON strings into Dictionary
            if (Request.Form.ContainsKey("Address"))
                request.Address = JsonSerializer.Deserialize<Dictionary<string, object>>(Request.Form["Address"]);

            if (Request.Form.ContainsKey("EmergencyContact"))
                request.EmergencyContact = JsonSerializer.Deserialize<Dictionary<string, object>>(Request.Form["EmergencyContact"]);



            var userId = GetCurrentUserId();
            var user = await _authService.UpdateProfileAsync(userId, request);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Profile updated successfully",
                //Data = user
            });
        }

        catch (InvalidOperationException ex) //  USER ERROR
        {
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }

        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for updating profile");
            return BadRequest(new ServiceResponse<UserDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Profile update failed for user: {UserId}", GetCurrentUserId());
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while updating profile"
            });
        }
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var userId = GetCurrentUserId();
            var result = await _authService.ChangePasswordAsync(userId, request);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Password changed successfully",
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for changing password");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error during password change for user: {UserId}", GetCurrentUserId());
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password change failed for user: {UserId}", GetCurrentUserId());
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpGet("verify-email")]
    public async Task<ActionResult<ServiceResponse<bool>>> VerifyEmail([FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("Invalid verification token");

            var result = await _authService.VerifyEmailAsync(token);

            if (!result)
            {
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Verification link expired or invalid",
                    Data = result
                });
            }

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Email verified successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification failed for token: {Token}", token);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            _logger.LogError("User ID claim not found in JWT token");
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogError("Invalid user ID format in JWT token: {UserId}", userIdClaim);
            throw new UnauthorizedAccessException("Invalid user ID format");
        }
        
        return userId;
    }

    private Guid GetCurrentOrganizationId()
    {
        var orgIdClaim = User.FindFirstValue("organizationId");
        
        if (string.IsNullOrEmpty(orgIdClaim))
        {
            _logger.LogError("Organization ID claim not found in JWT token");
            throw new UnauthorizedAccessException("Organization ID not found in token");
        }
        
        if (!Guid.TryParse(orgIdClaim, out var organizationId))
        {
            _logger.LogError("Invalid organization ID format in JWT token: {OrganizationId}", orgIdClaim);
            throw new UnauthorizedAccessException("Invalid organization ID format");
        }
        
        return organizationId;
    }

    [HttpGet("debug-claims")]
    [Authorize]
    public IActionResult DebugClaims()
    {
        var claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();
        
        // Debug specific claim lookups
        var orgIdDirect = User.FindFirstValue("organizationId");
        var orgIdHttp = User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/organizationId");
        var orgIdCustom = User.FindFirstValue("http://schemas.microsoft.com/ws/2008/06/identity/claims/organizationId");
        
        return Ok(new ServiceResponse<object>
        {
            Success = true,
            Message = "JWT Claims retrieved",
            Data = new { 
                Claims = claims,
                DirectOrgId = orgIdDirect,
                HttpOrgId = orgIdHttp,
                CustomOrgId = orgIdCustom,
                TotalClaims = claims.Count
            }
        });
    }

    [HttpPost("test-auth")]
    public async Task<IActionResult> TestAuth([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("=== TESTING AUTHENTICATION FLOW ===");
            
            // Step 1: Check if user exists
            var hashedPassword = await _authService.GetHashedPasswordAsync(request.Email);
            _logger.LogInformation("Step 1 - Found hashed password: {Found}", !string.IsNullOrEmpty(hashedPassword));
            
            // Step 2: Test password verification
            var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, hashedPassword);
            _logger.LogInformation("Step 2 - Password valid: {Valid}", passwordValid);
            
            if (!passwordValid)
            {
                return BadRequest("Invalid password");
            }

            // Step 3: Get user from database
            var user = await _authService.GetUserForTestingAsync(request.Email, hashedPassword);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            _logger.LogInformation("Step 3 - User found: UserId={UserId}, OrganizationId={OrganizationId}, Email={Email}", 
                user.UserId, user.OrganizationId, user.Email);

            return Ok(new
            {
                Message = "Authentication test completed",
                User = new
                {
                    UserId = user.UserId,
                    OrganizationId = user.OrganizationId,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    RoleName = user.Role?.RoleName,
                    OrganizationName = user.Organization?.Name
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test authentication failed");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("test-hardcoded-token")]
    public async Task<IActionResult> TestHardcodedToken()
    {
        try
        {
            // Create a hardcoded user object to test token generation
            var testUser = new User
            {
                UserId = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                OrganizationId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Email = "admin@demo.company.com",
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true,
                Role = new Role { RoleName = "Super Admin" },
                Organization = new Organization { Name = "Demo Company Ltd." }
            };

            _logger.LogInformation("=== TESTING HARDCODED TOKEN GENERATION ===");
            _logger.LogInformation("Test User: UserId={UserId}, OrganizationId={OrganizationId}", 
                testUser.UserId, testUser.OrganizationId);

            // Generate token directly
            var response = await _authService.CreateTestTokenAsync(testUser);
            
            return Ok(new
            {
                Message = "Hardcoded token test completed",
                Token = response.Token,
                User = new
                {
                    testUser.UserId,
                    testUser.OrganizationId,
                    testUser.Email,
                    testUser.FirstName,
                    testUser.LastName
                },
                AuthResponse = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hardcoded token test failed");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("roles")]
    [Authorize]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            var organizationId = GetOrganizationIdFromClaims();
            if (organizationId == null)
                return BadRequest(new ServiceResponse<RoleDto>
                {
                    Success = false,
                    Message = "Organization ID not found in claims"
                });

            var role = await _authService.CreateRoleAsync(request, organizationId.Value);
            return Ok(new ServiceResponse<RoleDto>
            {
                Success = true,
                Data = role,
                Message = "Role created successfully"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating role");
            return BadRequest(new ServiceResponse<RoleDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ServiceResponse<RoleDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ServiceResponse<RoleDto>
            {
                Success = false,
                Message = "An error occurred while creating the role"
            });
        }
    }

    [HttpGet("roles")]
    [Authorize]
    public async Task<IActionResult> GetOrganizationRoles()
    {
        try
        {
            var organizationId = GetOrganizationIdFromClaims();
            if (organizationId == null)
                return BadRequest(new ServiceResponse<List<RoleDto>>
                {
                    Success = false,
                    Message = "Organization ID not found in claims"
                });

            var roles = await _authService.GetOrganizationRolesAsync(organizationId.Value);
            return Ok(new ServiceResponse<List<RoleDto>>
            {
                Success = true,
                Data = roles,
                Message = "Roles retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ServiceResponse<List<RoleDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving roles"
            });
        }
    }
    [HttpGet("menus")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<List<MenuDto>>>> GetMenus()
    {
        try
        {
            var menus = await _authService.GetMenusAsync();

            return Ok(new ServiceResponse<List<MenuDto>>
            {
                Success = true,
                Data = menus,
                Message = "Menus retrieved successfully"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while retrieving menus");

            return BadRequest(new ServiceResponse<List<MenuDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business logic error while retrieving menus");

            return BadRequest(new ServiceResponse<List<MenuDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving menus");

            return StatusCode(500, new ServiceResponse<List<MenuDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving menus"
            });
        }
    }
    [HttpPost("create-role")]
    public async Task<ActionResult<ServiceResponse<CreateRoleResponse>>> CreateRoleAsync([FromBody] Create_RoleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<CreateRoleResponse>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var organizationid = GetOrganizationIdFromClaims();

            var result = await _authService.Create_RoleAsync(request, organizationid);

            return Ok(new ServiceResponse<CreateRoleResponse>
            {
                Success = true,
                Message = "Role created successfully",
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for role creation");
            return BadRequest(new ServiceResponse<CreateRoleResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Role already exists");
            return Conflict(new ServiceResponse<CreateRoleResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Role creation failed for role: {RoleName}", request.RoleName);
            return StatusCode(500, new ServiceResponse<CreateRoleResponse>
            {
                Success = false,
                Message = "An error occurred while creating the role"
            });
        }
    }
    [HttpPut("update-role")]
    public async Task<ActionResult<ServiceResponse<UpdateRoleResponse>>> UpdateRoleAsync([FromBody] UpdateRoleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<UpdateRoleResponse>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var organizationid = GetOrganizationIdFromClaims();

            var result = await _authService.UpdateRoleAsync(request, organizationid);

            return Ok(new ServiceResponse<UpdateRoleResponse>
            {
                Success = true,
                Message = "Role updated successfully",
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for role update");
            return BadRequest(new ServiceResponse<UpdateRoleResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Role not found for update");
            return NotFound(new ServiceResponse<UpdateRoleResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Role name conflict during update");
            return Conflict(new ServiceResponse<UpdateRoleResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Role update failed for role: {RoleId}", request.RoleId);
            return StatusCode(500, new ServiceResponse<UpdateRoleResponse>
            {
                Success = false,
                Message = "An error occurred while updating the role"
            });
        }
    }
    [HttpDelete("delete-role/{roleId}")]
    public async Task<ActionResult<ServiceResponse<DeleteRoleResponse>>> DeleteRoleAsync(Guid roleId)
    {
        try
        {
            if (roleId == Guid.Empty)
            {
                return BadRequest(new ServiceResponse<DeleteRoleResponse>
                {
                    Success = false,
                    Message = "Role ID is required"
                });
            }

            var organizationid = GetOrganizationIdFromClaims();

            var result = await _authService.DeleteRoleAsync(roleId, organizationid);

            return Ok(new ServiceResponse<DeleteRoleResponse>
            {
                Success = true,
                Message = "Role deleted successfully",
                Data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Role not found for deletion: {RoleId}", roleId);
            return NotFound(new ServiceResponse<DeleteRoleResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Role cannot be deleted as it is assigned to users: {RoleId}", roleId);
            return Conflict(new ServiceResponse<DeleteRoleResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Role deletion failed for role: {RoleId}", roleId);
            return StatusCode(500, new ServiceResponse<DeleteRoleResponse>
            {
                Success = false,
                Message = "An error occurred while deleting the role"
            });
        }
    }
    [HttpGet("get-roles-by-organization")]
    public async Task<ActionResult<ServiceResponse<List<RoleDetailResponse>>>> GetRolesByOrganization()
    {
        try
        {
            var claimsOrganizationId = GetOrganizationIdFromClaims();
            if (claimsOrganizationId == Guid.Empty)
            {
                return BadRequest(new ServiceResponse<List<RoleDetailResponse>>
                {
                    Success = false,
                    Message = "Invalid organization ID",
                    Data = null,
                    Errors = new List<string> { "Organization ID is required" }
                });
            }
           

            var response = await _authService.GetRolesByOrganizationAsync(claimsOrganizationId);

            return Ok(new ServiceResponse<List<RoleDetailResponse>>
            {
                Success = true,
                Message = "Roles retrieved successfully",
                Data = response,
                Errors = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles for organization");
            return StatusCode(500, new ServiceResponse<List<RoleDetailResponse>>
            {
                Success = false,
                Message = "An error occurred while retrieving roles",
                Data = null,
                Errors = new List<string> { ex.Message }
            });
        }
    }
    [HttpGet("get-user-permissions/{UserId}")]
    public async Task<ActionResult<ServiceResponse<UserPermissionResponse>>> GetUserPermissions(Guid UserId)
    {
        try
        {
            //var userid = GetCurrentUserId();
            var requestingUserOrganizationId = GetOrganizationIdFromClaims();
            if (requestingUserOrganizationId == Guid.Empty)
            {
                return BadRequest(new ServiceResponse<UserPermissionResponse>
                {
                    Success = false,
                    Message = "Invalid organization ID",
                    Data = null,
                    Errors = new List<string> { "Organization ID is required" }
                });
            }

            var response = await _authService.GetUserPermissionsAsync(UserId);

            if (response == null)
            {
                return NotFound(new ServiceResponse<UserPermissionResponse>
                {
                    Success = false,
                    Message = "User not found",
                    Data = null,
                    Errors = new List<string> { $"User with ID {UserId} not found" }
                });
            }

            return Ok(new ServiceResponse<UserPermissionResponse>
            {
                Success = true,
                Message = "User permissions retrieved successfully",
                Data = response,
                Errors = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for user");
            return StatusCode(500, new ServiceResponse<UserPermissionResponse>
            {
                Success = false,
                Message = "An error occurred while retrieving user permissions",
                Data = null,
                Errors = new List<string> { ex.Message }
            });
        }
    }

    private Guid? GetOrganizationIdFromClaims()
    {
        var organizationIdClaim =
            User.FindFirst(CustomClaimTypes.OrganizationId)?.Value
            ?? User.FindFirst("OrganizationId")?.Value;

        if (!string.IsNullOrWhiteSpace(organizationIdClaim) && Guid.TryParse(organizationIdClaim, out var organizationId))
        {
            return organizationId;
        }
        return null;
    }
}
