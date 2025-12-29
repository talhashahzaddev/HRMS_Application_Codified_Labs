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
    private readonly IMapper _mapper;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(
        ILeaveRepository leaveRepository,
        INotificationRepository notificationRepository,
        IEmployeeRepository employeeRepository,
        IMapper mapper,
        ILogger<LeaveService> logger)
    {
        _leaveRepository = leaveRepository;
        _notificationRepository = notificationRepository;
        _employeeRepository = employeeRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<LeaveRequestDto> CreateLeaveRequestAsync(CreateLeaveRequest request, Guid employeeId)
    {
        var empId = await _leaveRepository.GetEmpId(employeeId);
        // Check for overlapping leave requests
        var hasOverlap = await _leaveRepository.HasOverlappingLeaveAsync(empId, request.StartDate, request.EndDate);
        if (hasOverlap)
        {
            throw new InvalidOperationException("Leave request overlaps with existing leave");
        }
        var leaveRequest = await _leaveRepository.CreateLeaveRequestAsync(
            empId, request.LeaveTypeId, request.StartDate, request.EndDate, request.Reason);

        //Create Notification
        var employee = await _employeeRepository.GetEmployeeByIdAsync(empId);
        //Getting reciverid
        var reportingmangerid = await _notificationRepository.GetReciveridAsync(employee.EmployeeId);

        //var reciverid = await _notificationRepository.GetEmpId(reportingmangerid);
        if (leaveRequest!=null)

        {
            var notification = new NotificationCreateDto
            {
                Receiveruserid = reportingmangerid,
                Senderuserid = employee.EmployeeId,
                Ttitle = "Leave Request",
                RequestId = leaveRequest.RequestId,
                Message = $"Employee {employee.FirstName} Department {employee.DepartmentName}  request a Leave ",
                Notificationtype = "Request",
                Createdby = employee.EmployeeId,
                moduletype="Leave",
                Redirecturl= "/leave/team",
                organizationId = employee.OrganizationId
            };
            await _notificationRepository.CreateNotificationAsync(employee.OrganizationId, notification);
        }

        return _mapper.Map<LeaveRequestDto>(leaveRequest);
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
            var response = await CreateLeaveRequestAsync(requestDto, employeeId);
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
        catch(Exception ex)
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
    public async Task<List<LeaveRequest>> GetLeaveRequestByEmployeeId( Guid employeeId)
    {
        var empId = await _leaveRepository.GetEmpId(employeeId);
        var LeaveRequest = await _leaveRepository.GetLeaveRequestsByEmployeeId(empId);       
        return LeaveRequest;
    }

    public async Task<bool> UpdateLeaveRequestStatusAsync(Guid requestId, UpdateLeaveRequestStatusRequest request, Guid approvedBy)
    {
        var empId = await _leaveRepository.GetEmpId(approvedBy);
        // First, update the leave request status using the requestId parameter directly
        var result = await _leaveRepository.UpdateLeaveRequestStatusAsync(requestId, request.Status, empId, request.RejectionReason);
        
        if (!result)
        {
            _logger.LogWarning("Failed to update leave request status for RequestId: {RequestId}", requestId);
            return false;
        }

        // Try to get the original notification (if it exists)
        var Originalnotification = await _notificationRepository.getnotificationdetailAsync(requestId);

        // Build the notification message based on status
        string message;
        if (request.Status.ToLower() == "approved")
        {
            message = "Your leave request has been approved";
        }
        else if (request.Status.ToLower() == "rejected")
        {
            message = $"Your leave request has been rejected. Reason: {request.RejectionReason}";
        }
        else
        {
            message = $"Your leave request status has been updated to {request.Status}";
        }

        // If original notification exists, use it to create response notification
        if (Originalnotification != null)
        {
            var notification = new NotificationCreateDto
            {
                Receiveruserid = Originalnotification.SenderUserId,
                Senderuserid = Originalnotification.ReceiverUserId,
                Ttitle = "Leave Request",
                RequestId = Originalnotification.requestid,
                Message = message,
                Notificationtype = "Message",
                Createdby = Originalnotification.SenderUserId,
            };
            await _notificationRepository.CreateNotificationAsync(Originalnotification.organizationId, notification);
        }
        else
        {
            // If no original notification exists (e.g., approved/rejected from pending approvals section),
            // get leave request details to find the employee and create notification
            var leaveRequest = await _leaveRepository.GetLeaveRequestByIdAsync(requestId);
            if (leaveRequest != null)
            {
                // Get employee userId and organizationId from employeeId
                var employeeInfo = await _notificationRepository.GetEmployeeUserInfoAsync(leaveRequest.EmployeeId);
                if (employeeInfo.HasValue)
                {
                    var notification = new NotificationCreateDto
                    {
                        Receiveruserid = employeeInfo.Value.userId, // Employee who requested leave (userId)
                        Senderuserid = empId, // Manager/HR who approved/rejected (userId)
                        Ttitle = "Leave Request",
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

    public async Task<PagedResult<LeaveRequestDto>> GetLeaveRequestsAsync(Guid userId, LeaveRequestSearchViewModel searchModel, Guid organizationId)
    {
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
        return await _leaveRepository.GetLeaveRequestsByOrganizationAsync(
            organizationId,
            searchModel.Status,
            searchModel.Page,
            searchModel.PageSize);
    }

    public async Task<List<LeaveTypeDto>> GetLeaveTypesAsync(Guid organizationId)
    {
        return await _leaveRepository.GetLeaveTypesAsync(organizationId);
    }
    public async Task<Guid> CreateLeaveTypeAsync(Guid organizationId, CreateLeaveTypeRequest request)
    {
        var leaveTypeId = Guid.NewGuid();

        var dto = new LeaveTypeDto
        {
            LeaveTypeId = leaveTypeId,
            TypeName = request.TypeName,
            Description = request.Description,
            MaxDaysPerYear = request.MaxDaysPerYear,
            IsPaid = request.IsPaid,
            CarryForwardAllowed = request.CarryForwardAllowed,
            Color = request.Color,
            IsActive = true
        };

        var result = await _leaveRepository.CreateLeaveTypeAsync(organizationId, dto);
        if(result)
        {
            return leaveTypeId;
        }
        else
        {
            return Guid.Empty;
        }

        
    }



    public async Task<List<LeaveEntitlementDto>> GetLeaveEntitlementsAsync(Guid employeeId, int year)
    {
        return await _leaveRepository.GetLeaveEntitlementsAsync(employeeId, year);
    }

    public async Task<List<LeaveBalanceDto>> GetLeaveBalanceAsync(Guid employeeId, int year)
    {
        var empId = await _leaveRepository.GetEmpId(employeeId);
        return await _leaveRepository.GetLeaveBalanceAsync(empId, year);
    }

    public async Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(Guid requestId)
    {
        var leaveRequest = await _leaveRepository.GetLeaveRequestByIdAsync(requestId);
        return leaveRequest != null ? _mapper.Map<LeaveRequestDto>(leaveRequest) : null;
    }

    public async Task<LeaveRequestDto?> UpdateLeaveRequestAsync(Guid requestId, UpdateLeaveRequest request, Guid employeeId)
    {
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
        return await _leaveRepository.CancelLeaveRequestAsync(requestId);
    }
    public async Task<List<LeaveCalendarDto>> GetLeaveRequestsForCalendarAsync(Guid employeeId, DateTime startDate, DateTime endDate)
    {
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
        var empId = await _leaveRepository.GetEmpId(employeeId);
        return await _leaveRepository.HasOverlappingLeaveAsync(empId, startDate, endDate, excludeRequestId);
    }

}
