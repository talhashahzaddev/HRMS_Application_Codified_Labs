using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Requests;

namespace HRMS.API.Services.Interfaces;

public interface ILeaveService
{
    Task<LeaveRequestDto> CreateLeaveRequestAsync(CreateLeaveRequest request, Guid employeeId);
    Task<bool> UpdateLeaveRequestStatusAsync(Guid requestId, UpdateLeaveRequestStatusRequest request, Guid approvedBy);
    Task<PagedResult<LeaveRequestDto>> GetLeaveRequestsAsync(LeaveRequestSearchViewModel searchModel, Guid organizationId);
    Task<List<LeaveTypeDto>> GetLeaveTypesAsync(Guid organizationId);
    Task<Guid> CreateLeaveTypeAsync(Guid organizationId, CreateLeaveTypeRequest request);
    Task<List<LeaveEntitlementDto>> GetLeaveEntitlementsAsync(Guid employeeId, int year);
    Task<List<LeaveBalanceDto>> GetLeaveBalanceAsync(Guid employeeId, int year);
    Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(Guid requestId);
    Task<List<LeaveRequest>> GetLeaveRequestByEmployeeId(Guid employeeId);

    Task<List<LeaveCalendarDto>> GetLeaveRequestsForCalendarAsync(Guid employeeId, DateTime startDate, DateTime endDate);
}
