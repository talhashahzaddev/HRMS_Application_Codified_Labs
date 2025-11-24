using AutoMapper;
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
        // Check for overlapping leave requests
        var hasOverlap = await _leaveRepository.HasOverlappingLeaveAsync(employeeId, request.StartDate, request.EndDate);
        if (hasOverlap)
        {
            throw new InvalidOperationException("Leave request overlaps with existing leave");
        }
        var empId = await _leaveRepository.GetEmpId(employeeId);
        var leaveRequest = await _leaveRepository.CreateLeaveRequestAsync(
            empId, request.LeaveTypeId, request.StartDate, request.EndDate, request.Reason);

        //Create Notification
        var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
        //Getting reciverid
        var reportingmangerid = await _notificationRepository.GetReciveridAsync(employee.EmployeeId);

        var reciverid = await _notificationRepository.GetEmpId(reportingmangerid);
        if (leaveRequest!=null)

        {
            var notification = new NotificationCreateDto
            {
                Receiveruserid = reciverid,
                Senderuserid = employee.EmployeeId,
                Ttitle = "Leave Request",
                RequestId = leaveRequest.RequestId,
                Message = $"Employee {employee.FirstName} Department {employee.DepartmentName}  request a Leave ",
                Notificationtype = "Request",
                Createdby = employee.EmployeeId,

                organizationId = employee.OrganizationId
            };
            await _notificationRepository.CreateNotificationAsync(employee.OrganizationId, notification);
        }

        return _mapper.Map<LeaveRequestDto>(leaveRequest);
    }
    public async Task<List<LeaveRequest>> GetLeaveRequestByEmployeeId( Guid employeeId)
    {
        var empId = await _leaveRepository.GetEmpId(employeeId);
        var LeaveRequest = await _leaveRepository.GetLeaveRequestsByEmployeeId(empId);       
        return LeaveRequest;
    }

    public async Task<bool> UpdateLeaveRequestStatusAsync(Guid requestId, UpdateLeaveRequestStatusRequest request, Guid approvedBy)
    {
        var result = await _leaveRepository.UpdateLeaveRequestStatusAsync(requestId, request.Status, approvedBy, request.RejectionReason);

        //Create Notification
        var Originalnotification = await _notificationRepository.getnotificationdetailAsync(requestId);
       
        if (Originalnotification == null)
            return false;

        // 3. Build the notification message based on status
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
            return await _notificationRepository.CreateNotificationAsync(Originalnotification.organizationId, notification);
        }

        return true;

    }

    public async Task<PagedResult<LeaveRequestDto>> GetLeaveRequestsAsync(LeaveRequestSearchViewModel searchModel, Guid organizationId)
    {
        return await _leaveRepository.GetLeaveRequestsAsync(
            searchModel.EmployeeId,
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

}
