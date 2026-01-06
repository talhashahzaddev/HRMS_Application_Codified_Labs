using HRMS.AI.Models;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Requests;

namespace HRMS.API.Services.Interfaces;

public interface ILeaveService
{
    Task<LeaveRequestDto> CreateLeaveRequestAsync(CreateLeaveRequest request, Guid employeeId);
    
    Task<LeaveApplicationDetails> CreateLeaveRequestByChatBotAsync(LeaveApplicationDetails request, Guid employeeId, Guid organizationId);
    Task<ServiceResponse<bool>> UpdateLeaveRequestStatusAsync(Guid requestId,UpdateLeaveRequestStatusRequest request,Guid approvedBy);
    Task<ServiceResponse<PagedResult<LeaveRequestDto>>> GetLeaveRequestsAsync(Guid userId, LeaveRequestSearchViewModel searchModel, Guid organizationId);
    Task<ServiceResponse<PagedResult<LeaveRequestDto>>> GetLeaveRequestsByOrganizationAsync(LeaveRequestSearchViewModel searchModel,Guid organizationId);
    Task<ServiceResponse<List<LeaveTypeDto>>> GetLeaveTypesAsync(Guid organizationId);
    Task<ServiceResponse<Guid>> CreateLeaveTypeAsync(Guid organizationId, CreateLeaveTypeRequest request);
    Task<ServiceResponse<bool>> EditLeaveTypeAsync(Guid organizationId, Guid leaveTypeId, EditLeaveTypeRequest request);
    //Task<List<LeaveEntitlementDto>> GetLeaveEntitlementsAsync(Guid employeeId, int year);
    Task<ServiceResponse<List<LeaveEntitlementDto>>> GetLeaveEntitlementsAsync(Guid employeeId, int year);
    Task<ServiceResponse<List<LeaveBalanceDto>>> GetLeaveBalanceAsync(Guid employeeId, int year);
    Task<ServiceResponse<LeaveRequestDto?>> GetLeaveRequestByIdAsync(Guid requestId);
    Task<ServiceResponse<LeaveRequestDto?>> UpdateLeaveRequestAsync(Guid requestId, UpdateLeaveRequest request, Guid employeeId);

    Task<ServiceResponse<bool>> CancelLeaveRequestAsync(Guid requestId, Guid employeeId);
    Task<ServiceResponse<List<LeaveRequest>>>GetLeaveRequestByEmployeeId(Guid employeeId);


    Task<ServiceResponse<List<LeaveCalendarDto>>> GetLeaveRequestsForCalendarAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<ServiceResponse<PagedResult<TeamRemainingLeavesDto>>> GetTeamRemainingLeavesAsync(Guid managerUserId,Guid organizationId,TeamRemainingLeavesSearchViewModel searchModel);
    Task<ServiceResponse<bool>> HasOverlappingLeaveAsync(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeRequestId = null);

    //Task<bool> HasOverlappingLeaveAsync(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeRequestId = null);

}
