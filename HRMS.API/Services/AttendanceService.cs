using AutoMapper;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IMapper mapper,
        ILogger<AttendanceService> logger)
    {
        _attendanceRepository = attendanceRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<bool> ClockInAsync(Guid employeeId, ClockInOutRequest request)
    {
        return await _attendanceRepository.ClockInOutAsync(employeeId, "in", DateTime.UtcNow, request.Location);
    }

    public async Task<bool> ClockOutAsync(Guid employeeId, ClockInOutRequest request)
    {
        return await _attendanceRepository.ClockInOutAsync(employeeId, "out", DateTime.UtcNow, request.Location);
    }

    public async Task<AttendanceDto?> GetTodayAttendanceAsync(Guid employeeId)
    {
        var attendance = await _attendanceRepository.GetTodayAttendanceAsync(employeeId);
        return attendance != null ? _mapper.Map<AttendanceDto>(attendance) : null;
    }

    public async Task<PagedResult<AttendanceDto>> GetAttendanceAsync(AttendanceSearchViewModel searchModel, Guid organizationId)
    {
        return await _attendanceRepository.GetAttendanceAsync(
            searchModel.EmployeeId,
            organizationId,
            searchModel.StartDate,
            searchModel.EndDate,
            searchModel.Page,
            searchModel.PageSize);
    }

    public async Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(Guid employeeId, DateTime startDate, DateTime endDate)
    {
        return await _attendanceRepository.GetAttendanceSummaryAsync(employeeId, startDate, endDate);
    }

    public async Task<bool> UpdateAttendanceAsync(Guid attendanceId, CreateAttendanceRequest request)
    {
        var attendance = _mapper.Map<Attendance>(request);
        return await _attendanceRepository.UpdateAttendanceAsync(attendanceId, attendance);
    }

    public async Task<List<AttendanceDto>> GetDepartmentAttendanceAsync(Guid departmentId, DateTime date)
    {
        return await _attendanceRepository.GetDepartmentAttendanceAsync(departmentId, date);
    }
}
