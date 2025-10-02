using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface ILeaveRepository
{
    Task<Guid> GetEmpId(Guid? employeeId);
    Task<LeaveRequest> CreateLeaveRequestAsync(Guid employeeId, Guid leaveTypeId, DateTime startDate, DateTime endDate, string? reason = null);
    Task<bool> UpdateLeaveRequestStatusAsync(Guid requestId, string status, Guid approvedBy, string? rejectionReason = null);
    Task<PagedResult<LeaveRequestDto>> GetLeaveRequestsAsync(Guid? employeeId = null, Guid? organizationId = null, string? status = null, int page = 1, int pageSize = 50);
    Task<List<LeaveTypeDto>> GetLeaveTypesAsync(Guid organizationId);
    Task<List<LeaveEntitlementDto>> GetLeaveEntitlementsAsync(Guid employeeId, int year);
    Task<List<LeaveBalanceDto>> GetLeaveBalanceAsync(Guid employeeId, int year);
    Task<LeaveRequest?> GetLeaveRequestByIdAsync(Guid requestId);
    Task<bool> HasOverlappingLeaveAsync(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeRequestId = null);
    Task<List<LeaveRequest>> GetLeaveRequestsByEmployeeId(Guid employeeId);
}
