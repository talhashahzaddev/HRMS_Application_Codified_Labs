using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface ILeaveRepository
{
    //adding gthe method  here
    Task<bool> SubstractLeaveDayAsync(Guid leaveTypeId, Guid employeeId, int totalLeaveDays);
    Task<Guid> GetEmpId(Guid? employeeId);
    Task<LeaveRequest> CreateLeaveRequestAsync(Guid employeeId, Guid leaveTypeId, DateTime startDate, DateTime endDate, string? reason = null);
    Task<bool> UpdateLeaveRequestStatusAsync(Guid requestId, string status, Guid approvedBy, string? rejectionReason = null);
    Task<PushEmailLeaveRequest> GetEmployeeDetails(Guid RequestId);
    Task<PagedResult<LeaveRequestDto>> GetLeaveRequestsAsync(Guid? employeeId , Guid organizationId , string? status = null, int page = 1, int pageSize = 50);
    Task<PagedResult<LeaveRequestDto>> GetLeaveRequestsByOrganizationAsync(Guid organizationId, string? status = null, int page = 1, int pageSize = 50);
    Task<List<LeaveTypeDto>> GetLeaveTypesAsync(Guid organizationId);
    Task<List<LeaveTypeDto>> GetLeaveTypesforRequestAsync(Guid organizationId);
    Task<List<LeaveTypeDto>> GetLeaveTypeIdByNameAsync(string LeaveType, Guid organizationId);
    Task<string> GetLeaveTypeNameByIdAsync(Guid LeaveTypeid, Guid organizationId);
    Task<bool> CreateLeaveTypeAsync(Guid organizationId, LeaveTypeDto dto);
    Task<bool> EditLeaveTypeAsync(Guid organizationId, LeaveTypeDto dto);
    Task<List<LeaveEntitlementDto>> GetLeaveEntitlementsAsync(Guid employeeId, int year);
    Task<List<LeaveBalanceDto>> GetLeaveBalanceAsync(Guid employeeId, int year);
    Task<LeaveRequest?> GetLeaveRequestByIdAsync(Guid requestId);
    Task<bool> UpdateLeaveRequestAsync(Guid requestId, Guid leaveTypeId, DateTime startDate, DateTime endDate, decimal daysRequested, string? reason = null);
    Task<bool> CancelLeaveRequestAsync(Guid requestId);
    Task<bool> HasOverlappingLeaveAsync(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeRequestId = null);
    Task<List<LeaveRequest>> GetLeaveRequestsByEmployeeId(Guid employeeId);
    Task<List<LeaveCalendarDto>> GetLeaveRequestsAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<List<LeaveCalendarDto>> GetApprovedLeaveRequestsAsync(Guid employeeId, DateTime startDate, DateTime endDate);
    Task<PagedResult<EmployeeLeaveBalanceDto>> GetTeamRemainingLeavesAsync(
        Guid managerEmployeeId, 
        Guid organizationId, 
        int year,
        string? employeeName = null,
        Guid? leaveTypeId = null,
        int page = 1,
        int pageSize = 50,
        string? sortBy = null,
        string? sortOrder = "asc");
    Task<PagedResult<EmployeeLeaveBalanceDto>> GetTeamRemainingLeavesByOrganizationAsync(
        Guid organizationId, 
        int year,
        string? employeeName = null,
        Guid? leaveTypeId = null,
        int page = 1,
        int pageSize = 50,
        string? sortBy = null,
        string? sortOrder = "asc");
    Task<string?> GetUserRoleNameAsync(Guid userId);
}
