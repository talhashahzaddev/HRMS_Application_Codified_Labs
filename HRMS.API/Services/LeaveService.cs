using AutoMapper;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.ViewModels;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class LeaveService : ILeaveService
{
    private readonly ILeaveRepository _leaveRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(
        ILeaveRepository leaveRepository,
        IMapper mapper,
        ILogger<LeaveService> logger)
    {
        _leaveRepository = leaveRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<LeaveRequestDto> CreateLeaveRequestAsync(CreateLeaveRequest request, Guid employeeId)
    {
        // Check for overlapping leave requests
        var hasOverlap = await _leaveRepository.HasOverlappingLeaveAsync(employeeId, request.StartDate, request.EndDate);
        if (hasOverlap)
            throw new InvalidOperationException("Leave request overlaps with existing leave");

        var leaveRequest = await _leaveRepository.CreateLeaveRequestAsync(
            employeeId, request.LeaveTypeId, request.StartDate, request.EndDate, request.Reason);

        return _mapper.Map<LeaveRequestDto>(leaveRequest);
    }

    public async Task<bool> UpdateLeaveRequestStatusAsync(Guid requestId, UpdateLeaveRequestStatusRequest request, Guid approvedBy)
    {
        return await _leaveRepository.UpdateLeaveRequestStatusAsync(requestId, request.Status, approvedBy, request.RejectionReason);
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

    public async Task<List<LeaveEntitlementDto>> GetLeaveEntitlementsAsync(Guid employeeId, int year)
    {
        return await _leaveRepository.GetLeaveEntitlementsAsync(employeeId, year);
    }

    public async Task<List<LeaveBalanceDto>> GetLeaveBalanceAsync(Guid employeeId, int year)
    {
        return await _leaveRepository.GetLeaveBalanceAsync(employeeId, year);
    }

    public async Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(Guid requestId)
    {
        var leaveRequest = await _leaveRepository.GetLeaveRequestByIdAsync(requestId);
        return leaveRequest != null ? _mapper.Map<LeaveRequestDto>(leaveRequest) : null;
    }
}
