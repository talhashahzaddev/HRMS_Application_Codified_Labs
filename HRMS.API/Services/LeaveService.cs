using AutoMapper;
using HRMS.AI.Models;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Repositories;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Requests;
using HRMS.API.Services.Interfaces;
using static HRMS.API.Models.DTOs.NotificationDto;

namespace HRMS.API.Services;

public class LeaveService : ILeaveService
{
    private readonly ILeaveRepository _leaveRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(
        ILeaveRepository leaveRepository,
        INotificationRepository notificationRepository,
        IEmployeeRepository employeeRepository,
        IAttendanceRepository attendanceRepository,
        IEmailService emailService,
        IMapper mapper,
        ILogger<LeaveService> logger)
    {
        _leaveRepository = leaveRepository;
        _notificationRepository = notificationRepository;
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;
        _emailService = emailService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<LeaveRequestDto> CreateLeaveRequestAsync(CreateLeaveRequest request, Guid employeeId, Guid organizationid)
    {
        _logger.LogInformation("Creating leave request for employee: {EmployeeId}", employeeId);

        if (employeeId == Guid.Empty)
            throw new ArgumentException("Employee ID cannot be empty", nameof(employeeId));

        if (request == null)
            throw new ArgumentNullException(nameof(request), "Request cannot be null");

        if (request.LeaveTypeId == Guid.Empty)
            throw new ArgumentException("Leave type ID is required", nameof(request));

        if (request.StartDate.Date > request.EndDate.Date)
            throw new ArgumentException("Start date must be before or equal to end date", nameof(request));

        try
        {
            var empId = await _leaveRepository.GetEmpId(employeeId);
            var hasOverlap = await _leaveRepository.HasOverlappingLeaveAsync(empId, request.StartDate, request.EndDate);
            if (hasOverlap)
                throw new InvalidOperationException("Leave request overlaps with existing leave");

            //Subtracting the days From leave Elementals of Employee
            //var totalleaveDays = (request.EndDate.Date - request.StartDate.Date).Days + 1;
            var currentshiftdetails = await _attendanceRepository.GetShiftByEmployeeIdAsync(empId);
            int totalleaveDays = 0;

            if (currentshiftdetails != null)
            {
               
                var workingDays = new HashSet<int>(currentshiftdetails.DaysOfWeek);

                for (var date = request.StartDate.Date; date <= request.EndDate.Date; date = date.AddDays(1))
                {
                    if (workingDays.Contains((int)date.DayOfWeek))
                    {
                        totalleaveDays++;
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("No Shift assigned");

            }
            var leaveRequest = await _leaveRepository.CreateLeaveRequestAsync(
          empId, request.LeaveTypeId, request.StartDate, request.EndDate, request.Reason);

            var typename = await _leaveRepository.GetLeaveTypeNameByIdAsync(request.LeaveTypeId,organizationid);

            ////Method for Subctracting the days

            if (leaveRequest != null && leaveRequest.RequestId != Guid.Empty)
            {
                var SubctractingDays = await _leaveRepository.SubstractLeaveDayAsync(request.LeaveTypeId, empId, totalleaveDays);

            }

            var employee = await _employeeRepository.GetEmployeeByIdAsync(empId);
            var reportingmangerdetails = await _notificationRepository.GetReciveridANdEmailAsync(employee.EmployeeId);

            //if no manager assigne then push notification to superadmin

            if (reportingmangerdetails == null)
            {
                var superadmin = await _notificationRepository.GetNoManagerReciveridAsync(employee.EmployeeId);
                if (superadmin != null)
                {
                    // Create a new instance and assign values
                    reportingmangerdetails = new leaveNotification
                    {
                        reportingmanagerid = superadmin.reportingmanagerid,
                        email = superadmin.email,
                        receivername = superadmin.receivername
                    };
                }
            }

            

            if (leaveRequest != null)
            {
                var notification = new NotificationCreateDto
                {
                    Receiveruserid = new List<Guid> { reportingmangerdetails.reportingmanagerid },
                    Senderuserid = employee.EmployeeId,
                    Title = "Leave Request",
                    RequestId = leaveRequest.RequestId,
                    Message = $"Employee {employee.FirstName} Department {employee.DepartmentName}  request a Leave ",
                    Notificationtype = "Request",
                    Createdby = employee.EmployeeId,
                    moduletype = "Leave",
                    Redirecturl = "/leave/team",
                    organizationId = employee.OrganizationId
                };
                await _notificationRepository.CreateNotificationAsync(employee.OrganizationId, notification);
                var EmployeeFullName = employee.FirstName +" "+ employee.LastName;
                await _emailService.SendLeaveRequestToManagerAsync(reportingmangerdetails.email,reportingmangerdetails.receivername ,EmployeeFullName, typename,request.StartDate,request.EndDate,request.Reason,organizationid);
            }

            return _mapper.Map<LeaveRequestDto>(leaveRequest);
        }
        catch (ArgumentNullException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating leave request for employee: {EmployeeId}", employeeId);
            throw new InvalidOperationException("An error occurred while creating the leave request", ex);
        }
    }
    public async Task<LeaveApplicationDetails> CreateLeaveRequestByChatBotAsync(LeaveApplicationDetails request, Guid employeeId, Guid organizationId)
    {
        try
        {
            var leavetypeId = await _leaveRepository.GetLeaveTypeIdByNameAsync(request.leaveType, organizationId);
            var typeId = leavetypeId.FirstOrDefault().LeaveTypeId;
            var requestDto = new CreateLeaveRequest
            {
                LeaveTypeId = typeId,
                StartDate = request.startDate,
                EndDate = request.endDate,
                Reason = request.reason,
                AttachmentUrl = null
            };
            var startDate = request.startDate.Date;
            var endDate = request.endDate.Date;
            var today = DateTime.Today;
            var response = await CreateLeaveRequestAsync(requestDto, employeeId,organizationId);
            var daysrequested = CalculateWorkingDays(request.startDate, request.endDate);

            if (daysrequested > 0)
            {
                response.DaysRequested = daysrequested;
            }

            if (startDate <= today)
            {
                throw new InvalidOperationException("Leave start date must be a future date.");
            }

            if (response != null)
            {
                return new LeaveApplicationDetails
                {
                    leavetypeid = response.LeaveTypeId,
                    leaveType = response.LeaveTypeName,
                    startDate = response.StartDate,
                    endDate = response.EndDate,
                    reason = response.Reason,
                    status = response.Status,
                    statustext = $"✅ Your leave request from {response.StartDate:dd MMM yyyy} to {response.EndDate:dd MMM yyyy} " +
                          $"for {response.DaysRequested} day(s) has been submitted with {response.Status} status \nfor more details check your calander https://frontend-hrms-phi.vercel.app/leave/calendar"
                };
            }
            else
            {
                return new LeaveApplicationDetails
                {
                    leavetypeid = Guid.Empty,
                    leaveType = request.leaveType, // use original request
                    startDate = request.startDate,
                    endDate = request.endDate,
                    reason = request.reason,
                    status = "Failed",
                    statustext = "❌ Unable to submit leave request. Please try again or check details."
                };
            }
        }
        catch (Exception ex)
        {
            return new LeaveApplicationDetails
            {
                leavetypeid = Guid.Empty,
                leaveType = request.leaveType, // use original request
                startDate = request.startDate,
                endDate = request.endDate,
                reason = request.reason,
                status = "Failed",
                statustext = ex.Message
            };
        }
    }
    public async Task<List<LeaveRequest>> GetLeaveRequestByEmployeeId( Guid employeeId)
    {
        if (employeeId == Guid.Empty)
            throw new ArgumentException("Employee ID cannot be empty", nameof(employeeId));
        var empId = await _leaveRepository.GetEmpId(employeeId);
        var LeaveRequest = await _leaveRepository.GetLeaveRequestsByEmployeeId(empId);
        return LeaveRequest;
    }

    public async Task<bool> UpdateLeaveRequestStatusAsync(Guid requestId, UpdateLeaveRequestStatusRequest request, Guid approvedBy,Guid organizationid)
    {
        if (requestId == Guid.Empty)
            throw new ArgumentException("Request ID cannot be empty", nameof(requestId));
        if (approvedBy == Guid.Empty)
            throw new ArgumentException("Approver user ID cannot be empty", nameof(approvedBy));
        if (request == null)
            throw new ArgumentNullException(nameof(request), "Request cannot be null");
        if (string.IsNullOrWhiteSpace(request.Status))
            throw new ArgumentException("Status is required", nameof(request));

        var normalizedStatus = request.Status.Trim().ToLowerInvariant();
        if (normalizedStatus != "approved" && normalizedStatus != "rejected")
            throw new ArgumentException("Invalid status. Allowed: approved or rejected", nameof(request));

        try
        {
            var empId = await _leaveRepository.GetEmpId(approvedBy);
            var result = await _leaveRepository.UpdateLeaveRequestStatusAsync(requestId, request.Status, empId, request.RejectionReason);



            if (!result)
            {
                _logger.LogWarning("Failed to update leave request status for RequestId: {RequestId}", requestId);
                return false;
            }

            var Originalnotification = await _notificationRepository.getnotificationdetailAsync(requestId);

            string message = normalizedStatus == "approved"
                ? "Your leave request has been approved"
                : $"Your leave request has been rejected. Reason: {request.RejectionReason}";

            var employeedetails = await _leaveRepository.GetEmployeeDetails(requestId);

            bool isApproval = request.Status == "approved" ? true : false;


            await _emailService.SendLeaveApprovalRejectionEmailAsync(employeedetails.email,employeedetails.EmployeeName,employeedetails.typename,employeedetails.startdate,employeedetails.enddate, isApproval, organizationid);

            if (Originalnotification != null)
            {
                var notification = new NotificationCreateDto
                {
                    Receiveruserid = new List<Guid> { Originalnotification.SenderUserId.Value },
                    Senderuserid = Originalnotification.ReceiverUserIds.FirstOrDefault(),
                    Title = "Leave Request",
                    RequestId = Originalnotification.requestid,
                    Message = message,
                    Notificationtype = "Message",
                    Createdby = Originalnotification.SenderUserId,
                };
                await _notificationRepository.CreateNotificationAsync(Originalnotification.organizationId, notification);
            }
            else
            {
                var leaveRequest = await _leaveRepository.GetLeaveRequestByIdAsync(requestId);
                if (leaveRequest != null)
                {
                    var employeeInfo = await _notificationRepository.GetEmployeeUserInfoAsync(leaveRequest.EmployeeId);
                    if (employeeInfo.HasValue)
                    {
                        var notification = new NotificationCreateDto
                        {
                            Receiveruserid = new List<Guid> { employeeInfo.Value.userId },
                            Senderuserid = empId,
                            Title = "Leave Request",
                            RequestId = requestId,
                            Message = message,
                            Notificationtype = "Message",
                            Createdby = empId,
                            moduletype = "Leave",
                            Redirecturl = "/leave/my-leaves",
                            organizationId = employeeInfo.Value.organizationId
                        };
                        await _notificationRepository.CreateNotificationAsync(employeeInfo.Value.organizationId, notification);
                    }
                }
            }

            return true;
        }
        catch (ArgumentNullException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating leave request status for RequestId: {RequestId}", requestId);
            throw new InvalidOperationException("An error occurred while updating leave request status", ex);
        }
    }

    public async Task<PagedResult<LeaveRequestDto>> GetLeaveRequestsAsync(Guid userId, LeaveRequestSearchViewModel searchModel, Guid organizationId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        if (searchModel == null)
            throw new ArgumentNullException(nameof(searchModel), "Search model cannot be null");
        if (searchModel.Page <= 0)
            throw new ArgumentException("Page must be greater than 0", nameof(searchModel));
        if (searchModel.PageSize <= 0)
            throw new ArgumentException("Page size must be greater than 0", nameof(searchModel));

        Guid ? empid= await _leaveRepository.GetEmpId(userId);
        searchModel.EmployeeId = empid;
        return await _leaveRepository.GetLeaveRequestsAsync(
            searchModel.EmployeeId,
            organizationId,
            searchModel.Status,
            searchModel.Page,
            searchModel.PageSize);
    }

    public async Task<PagedResult<LeaveRequestDto>> GetLeaveRequestsByOrganizationAsync(LeaveRequestSearchViewModel searchModel, Guid organizationId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (searchModel == null)
            throw new ArgumentNullException(nameof(searchModel), "Search model cannot be null");
        if (searchModel.Page <= 0)
            throw new ArgumentException("Page must be greater than 0", nameof(searchModel));
        if (searchModel.PageSize <= 0)
            throw new ArgumentException("Page size must be greater than 0", nameof(searchModel));

        return await _leaveRepository.GetLeaveRequestsByOrganizationAsync(
            organizationId,
            searchModel.Status,
            searchModel.Page,
            searchModel.PageSize);
    }

    public async Task<List<LeaveTypeDto>> GetLeaveTypesAsync(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        return await _leaveRepository.GetLeaveTypesAsync(organizationId);
    }
    public async Task<List<LeaveTypeDto>> GetLeaveTypesforrequestAsync(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        return await _leaveRepository.GetLeaveTypesforRequestAsync(organizationId);
    }
    public async Task<Guid> CreateLeaveTypeAsync(Guid organizationId, CreateLeaveTypeRequest request)
    {
        _logger.LogInformation("Creating leave type for organization: {OrganizationId}", organizationId);

        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (request == null)
            throw new ArgumentNullException(nameof(request), "Request cannot be null");
        if (string.IsNullOrWhiteSpace(request.TypeName))
            throw new ArgumentException("Leave type name is required", nameof(request));
        if (request.MaxDaysPerYear <= 0)
            throw new ArgumentException("Max days per year must be greater than 0", nameof(request));

        if (!request.CarryForwardAllowed)
            request.MaxCarryForwardDays = 0;
        if (request.MaxCarryForwardDays < 0)
            request.MaxCarryForwardDays = 0;

        var existing = await _leaveRepository.GetLeaveTypesAsync(organizationId);
        if (existing.Any(t => string.Equals(t.TypeName?.Trim(), request.TypeName.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Leave type with name '{request.TypeName}' already exists");

        var leaveTypeId = Guid.NewGuid();
        var dto = new LeaveTypeDto
        {
            LeaveTypeId = leaveTypeId,
            TypeName = request.TypeName,
            Description = request.Description,
            MaxDaysPerYear = request.MaxDaysPerYear,
            IsPaid = request.IsPaid,
            CarryForwardAllowed = request.CarryForwardAllowed,
            MaxCarryForwardDays = request.MaxCarryForwardDays,
            Color = request.Color,
            IsActive = true
        };

        try
        {
            var result = await _leaveRepository.CreateLeaveTypeAsync(organizationId, dto);
            if (!result)
                throw new InvalidOperationException("Failed to create leave type");

            return leaveTypeId;
        }
        catch (ArgumentNullException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating leave type for organization: {OrganizationId}", organizationId);
            throw new InvalidOperationException("An error occurred while creating the leave type", ex);
        }
    }

    public async Task<bool> EditLeaveTypeAsync(
      Guid organizationId,
      Guid leaveTypeId,
      EditLeaveTypeRequest request)
    {
        _logger.LogInformation("Editing leave type: {LeaveTypeId}", leaveTypeId);

        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (leaveTypeId == Guid.Empty)
            throw new ArgumentException("Leave type ID cannot be empty", nameof(leaveTypeId));
        if (request == null)
            throw new ArgumentNullException(nameof(request), "Request cannot be null");
        if (string.IsNullOrWhiteSpace(request.TypeName))
            throw new ArgumentException("Leave type name is required", nameof(request));
        if (request.MaxDaysPerYear <= 0)
            throw new ArgumentException("Max days per year must be greater than 0", nameof(request));

        if (!request.CarryForwardAllowed)
            request.MaxCarryForwardDays = 0;
        if (request.MaxCarryForwardDays < 0)
            request.MaxCarryForwardDays = 0;

        var dto = new LeaveTypeDto
        {
            LeaveTypeId = leaveTypeId,
            TypeName = request.TypeName,
            Description = request.Description,
            MaxDaysPerYear = request.MaxDaysPerYear,
            IsPaid = request.IsPaid,
            CarryForwardAllowed = request.CarryForwardAllowed,
            MaxCarryForwardDays = request.MaxCarryForwardDays,
            Color = request.Color,
            IsActive = request.IsActive
        };

        try
        {
            var updated = await _leaveRepository.EditLeaveTypeAsync(organizationId, dto);
            if (!updated)
                throw new KeyNotFoundException($"Leave type with ID {leaveTypeId} not found");

            return true;
        }
        catch (ArgumentNullException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error editing leave type: {LeaveTypeId}", leaveTypeId);
            throw new InvalidOperationException("An error occurred while editing the leave type", ex);
        }
    }


    public async Task<List<LeaveEntitlementDto>> GetLeaveEntitlementsAsync(Guid employeeId, int year)
    {
        if (employeeId == Guid.Empty)
            throw new ArgumentException("Employee ID cannot be empty", nameof(employeeId));
        if (year <= 0)
            throw new ArgumentException("Year must be a positive number", nameof(year));
        return await _leaveRepository.GetLeaveEntitlementsAsync(employeeId, year);
    }

    public async Task<List<LeaveBalanceDto>> GetLeaveBalanceAsync(Guid employeeId, int year)
    {
        if (employeeId == Guid.Empty)
            throw new ArgumentException("Employee ID cannot be empty", nameof(employeeId));
        if (year <= 0)
            throw new ArgumentException("Year must be a positive number", nameof(year));
        var empId = await _leaveRepository.GetEmpId(employeeId);
        return await _leaveRepository.GetLeaveBalanceAsync(empId, year);
    }

    public async Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(Guid requestId)
    {
        if (requestId == Guid.Empty)
            throw new ArgumentException("Request ID cannot be empty", nameof(requestId));
        var leaveRequest = await _leaveRepository.GetLeaveRequestByIdAsync(requestId);
        return leaveRequest != null ? _mapper.Map<LeaveRequestDto>(leaveRequest) : null;
    }

    public async Task<LeaveRequestDto?> UpdateLeaveRequestAsync(Guid requestId, UpdateLeaveRequest request, Guid employeeId)
    {
        if (requestId == Guid.Empty)
            throw new ArgumentException("Request ID cannot be empty", nameof(requestId));
        if (employeeId == Guid.Empty)
            throw new ArgumentException("Employee ID cannot be empty", nameof(employeeId));
        if (request == null)
            throw new ArgumentNullException(nameof(request), "Request cannot be null");
        if (request.StartDate.Date > request.EndDate.Date)
            throw new ArgumentException("Start date must be before or equal to end date", nameof(request));
        // First check if the request exists and is pending
        var existingRequest = await _leaveRepository.GetLeaveRequestByIdAsync(requestId);
        if (existingRequest == null)
        {
            throw new InvalidOperationException("Leave request not found");
        }

        if (existingRequest.Status.ToLower() != "pending")
        {
            throw new InvalidOperationException("Only pending leave requests can be updated");
        }

        // Verify the request belongs to the employee
        var empId = await _leaveRepository.GetEmpId(employeeId);
        if (existingRequest.EmployeeId != empId)
        {
            throw new UnauthorizedAccessException("You can only update your own leave requests");
        }

        // Check for overlapping leave requests (excluding current request)
        var hasOverlap = await _leaveRepository.HasOverlappingLeaveAsync(empId, request.StartDate, request.EndDate, requestId);
        if (hasOverlap)
        {
            throw new InvalidOperationException("Leave request overlaps with existing leave");
        }

        // Calculate days requested
        var daysRequested = (decimal)(request.EndDate - request.StartDate).TotalDays + 1;

        // Update the request
        var updated = await _leaveRepository.UpdateLeaveRequestAsync(
            requestId,
            request.LeaveTypeId,
            request.StartDate,
            request.EndDate,
            daysRequested,
            request.Reason
        );

        if (!updated)
        {
            throw new InvalidOperationException("Failed to update leave request");
        }

        // Return updated request
        var updatedRequest = await _leaveRepository.GetLeaveRequestByIdAsync(requestId);
        return updatedRequest != null ? _mapper.Map<LeaveRequestDto>(updatedRequest) : null;
    }

    public async Task<bool> CancelLeaveRequestAsync(Guid requestId, Guid employeeId)
    {
        if (requestId == Guid.Empty)
            throw new ArgumentException("Request ID cannot be empty", nameof(requestId));
        if (employeeId == Guid.Empty)
            throw new ArgumentException("Employee ID cannot be empty", nameof(employeeId));
        // First check if the request exists and is pending
        var existingRequest = await _leaveRepository.GetLeaveRequestByIdAsync(requestId);
        if (existingRequest == null)
        {
            throw new InvalidOperationException("Leave request not found");
        }

        if (existingRequest.Status.ToLower() != "pending")
        {
            throw new InvalidOperationException("Only pending leave requests can be cancelled");
        }

        // Verify the request belongs to the employee
        var empId = await _leaveRepository.GetEmpId(employeeId);
        if (existingRequest.EmployeeId != empId)
        {
            throw new UnauthorizedAccessException("You can only cancel your own leave requests");
        }

        // Cancel the request
        var result= await _leaveRepository.CancelLeaveRequestAsync(requestId);
        //Cancel notification too
        return await _notificationRepository.DeleteNotificationAsync(requestId);

    }
    public async Task<List<LeaveCalendarDto>> GetLeaveRequestsForCalendarAsync(Guid employeeId, DateTime startDate, DateTime endDate)
    {
        if (employeeId == Guid.Empty)
            throw new ArgumentException("Employee ID cannot be empty", nameof(employeeId));
        if (startDate.Date > endDate.Date)
            throw new ArgumentException("Start date must be before or equal to end date");
        var EMPId = await _leaveRepository.GetEmpId(employeeId);
        var requests = await _leaveRepository.GetLeaveRequestsAsync(EMPId, startDate, endDate);

        return requests.Select(r => new LeaveCalendarDto
        {
            RequestId = r.RequestId,
            EmployeeId = r.EmployeeId,
            LeaveTypeId = r.LeaveTypeId,
            LeaveTypeName = r.LeaveTypeName,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            DaysRequested = r.DaysRequested,
            Status = r.Status,
            Color = GetStatusColor(r.Status),
            Reason = r.Reason,
            SubmittedAt = r.SubmittedAt
        }).ToList();
    }

    private string GetStatusColor(string status)
    {
        return status switch
        {
            "approved" => "#28a745", // Green
            "rejected" => "#dc3545", // Red
            "pending" => "#ffc107", // Yellow
            _ => "#6c757d"           // Gray (default)
        };
    }

    public async Task<PagedResult<TeamRemainingLeavesDto>> GetTeamRemainingLeavesAsync(
        Guid managerUserId,
        Guid organizationId,
        TeamRemainingLeavesSearchViewModel searchModel)
    {
        if (managerUserId == Guid.Empty)
            throw new ArgumentException("Manager user ID cannot be empty", nameof(managerUserId));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (searchModel == null)
            throw new ArgumentNullException(nameof(searchModel), "Search model cannot be null");
        if (searchModel.Page <= 0)
            throw new ArgumentException("Page must be greater than 0", nameof(searchModel));
        if (searchModel.PageSize <= 0)
            throw new ArgumentException("Page size must be greater than 0", nameof(searchModel));
        // Get user role name
        var roleName = await _leaveRepository.GetUserRoleNameAsync(managerUserId);

        var year = searchModel.Year ?? DateTime.Now.Year;
        PagedResult<EmployeeLeaveBalanceDto> rawData;

        // Check if user is HR Manager
        if (roleName != null && roleName.Equals("HR Manager", StringComparison.OrdinalIgnoreCase) || roleName.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
        {
            // HR Manager: Get all employees in the organization
            rawData = await _leaveRepository.GetTeamRemainingLeavesByOrganizationAsync(
                organizationId,
                year,
                searchModel.EmployeeName,
                searchModel.LeaveTypeId,
                searchModel.Page,
                searchModel.PageSize,
                searchModel.SortBy,
                searchModel.SortOrder);
        }
        else
        {
            // Regular Manager: Get employees reporting to this manager
            var managerEmployeeId = await _leaveRepository.GetEmpId(managerUserId);
            if (managerEmployeeId == Guid.Empty)
            {
                throw new InvalidOperationException("Manager employee ID not found");
            }

            rawData = await _leaveRepository.GetTeamRemainingLeavesAsync(
                managerEmployeeId,
                organizationId,
                year,
                searchModel.EmployeeName,
                searchModel.LeaveTypeId,
                searchModel.Page,
                searchModel.PageSize,
                searchModel.SortBy,
                searchModel.SortOrder);
        }

        // Group by employee and transform to TeamRemainingLeavesDto
        var groupedData = rawData.Data
            .GroupBy(x => new { x.EmployeeId, x.EmployeeName })
            .Select(g => new TeamRemainingLeavesDto
            {
                EmployeeId = g.Key.EmployeeId,
                EmployeeName = g.Key.EmployeeName,
                LeaveBalances = g.Select(x => new LeaveTypeBalanceDto
                {
                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveTypeName,
                    TotalDays = x.TotalDays,
                    UsedDays = x.UsedDays,
                    RemainingDays = x.RemainingDays
                }).ToList()
            })
            .ToList();

        return new PagedResult<TeamRemainingLeavesDto>
        {
            Data = groupedData,
            TotalCount = rawData.TotalCount,
            Page = rawData.Page,
            PageSize = rawData.PageSize
        };
    }

    public async Task<bool> HasOverlappingLeaveAsync(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeRequestId = null)
    {
        if (employeeId == Guid.Empty)
            throw new ArgumentException("Employee ID cannot be empty", nameof(employeeId));
        if (startDate.Date > endDate.Date)
            throw new ArgumentException("Start date must be before or equal to end date");
        var empId = await _leaveRepository.GetEmpId(employeeId);
        return await _leaveRepository.HasOverlappingLeaveAsync(empId, startDate, endDate, excludeRequestId);
    }
    private static int CalculateWorkingDays(DateTime startDate, DateTime endDate)
    {
        int workingDays = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday &&
                date.DayOfWeek != DayOfWeek.Sunday)
            {
                workingDays++;
            }
        }

        return workingDays;
    }

}
