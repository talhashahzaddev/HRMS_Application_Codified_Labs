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

    // New missing methods implementation
    public async Task<TimeTrackingSessionDto?> GetCurrentSessionAsync(Guid employeeId)
    {
        _logger.LogInformation("Getting current session for employee: {EmployeeId}", employeeId);
        
        // Mock implementation - return current active session if exists
        var todayAttendance = await GetTodayAttendanceAsync(employeeId);
        
        if (todayAttendance?.CheckInTime != null && todayAttendance.CheckOutTime == null)
        {
            return new TimeTrackingSessionDto
            {
                SessionId = Guid.NewGuid(),
                EmployeeId = employeeId,
                CheckInTime = todayAttendance.CheckInTime,
                Status = "Active",
                ElapsedTime = DateTime.UtcNow - todayAttendance.CheckInTime.Value,
                CurrentLocation = todayAttendance.CheckInLocation
            };
        }
        
        return null;
    }

    public async Task<AttendanceDto?> GetAttendanceByIdAsync(Guid attendanceId)
    {
        _logger.LogInformation("Getting attendance by ID: {AttendanceId}", attendanceId);
        
        var attendance = await _attendanceRepository.GetAttendanceByIdAsync(attendanceId);
        return attendance != null ? _mapper.Map<AttendanceDto>(attendance) : null;
    }

    public async Task<AttendanceDto> CreateManualAttendanceAsync(ManualAttendanceRequest request)
    {
        _logger.LogInformation("Creating manual attendance for employee: {EmployeeId}", request.EmployeeId);
        
        var attendance = new Attendance
        {
            EmployeeId = request.EmployeeId,
            Date = request.Date,
            CheckInTime = request.CheckInTime,
            CheckOutTime = request.CheckOutTime,
            CheckInLocation = request.CheckInLocation,
            CheckOutLocation = request.CheckOutLocation,
            Status = request.Status ?? "Present",
            Notes = request.Notes,
            IsManualEntry = true,
            CreatedAt = DateTime.UtcNow
        };

        var createdAttendance = await _attendanceRepository.CreateAttendanceAsync(attendance);
        return _mapper.Map<AttendanceDto>(createdAttendance);
    }

    public async Task<AttendanceDto> UpdateAttendanceAsync(Guid attendanceId, ManualAttendanceRequest request)
    {
        _logger.LogInformation("Updating attendance: {AttendanceId}", attendanceId);
        
        var existingAttendance = await _attendanceRepository.GetAttendanceByIdAsync(attendanceId);
        if (existingAttendance == null)
            throw new KeyNotFoundException($"Attendance with ID {attendanceId} not found");

        // Update properties
        existingAttendance.Date = request.Date;
        existingAttendance.CheckInTime = request.CheckInTime;
        existingAttendance.CheckOutTime = request.CheckOutTime;
        existingAttendance.CheckInLocation = request.CheckInLocation;
        existingAttendance.CheckOutLocation = request.CheckOutLocation;
        existingAttendance.Status = request.Status ?? existingAttendance.Status;
        existingAttendance.Notes = request.Notes;

        await _attendanceRepository.UpdateAttendanceAsync(attendanceId, existingAttendance);
        return _mapper.Map<AttendanceDto>(existingAttendance);
    }

    public async Task<bool> DeleteAttendanceAsync(Guid attendanceId)
    {
        _logger.LogInformation("Deleting attendance: {AttendanceId}", attendanceId);
        
        var attendance = await _attendanceRepository.GetAttendanceByIdAsync(attendanceId);
        if (attendance == null)
            throw new KeyNotFoundException($"Attendance with ID {attendanceId} not found");

        return await _attendanceRepository.DeleteAttendanceAsync(attendanceId);
    }

    public async Task<List<AttendanceDto>> GetEmployeeAttendanceAsync(Guid employeeId, DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Getting employee attendance for employee: {EmployeeId} from {StartDate} to {EndDate}", 
            employeeId, startDate, endDate);
        
        var attendance = await _attendanceRepository.GetEmployeeAttendanceAsync(employeeId, startDate, endDate);
        return _mapper.Map<List<AttendanceDto>>(attendance);
    }

    public async Task<List<AttendanceCalendarDto>> GetAttendanceCalendarAsync(Guid employeeId, int year, int month)
    {
        _logger.LogInformation("Getting attendance calendar for employee: {EmployeeId} for {Year}-{Month}", 
            employeeId, year, month);
        
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        
        var attendance = await GetEmployeeAttendanceAsync(employeeId, startDate, endDate);
        
        var calendar = new List<AttendanceCalendarDto>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dayAttendance = attendance.FirstOrDefault(a => a.Date.Date == date.Date);
            calendar.Add(new AttendanceCalendarDto
            {
                Date = date,
                Status = dayAttendance?.Status ?? "Absent",
                CheckInTime = dayAttendance?.CheckInTime,
                CheckOutTime = dayAttendance?.CheckOutTime,
                TotalHours = dayAttendance?.TotalHours ?? 0,
                IsHoliday = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday
            });
        }
        
        return calendar;
    }

    public async Task<DailyAttendanceStatsDto> GetDailyAttendanceStatsAsync(Guid organizationId, DateTime date)
    {
        _logger.LogInformation("Getting daily attendance stats for organization: {OrganizationId} on {Date}", 
            organizationId, date);
        
        // Mock implementation
        return new DailyAttendanceStatsDto
        {
            Date = date,
            TotalEmployees = 150,
            PresentEmployees = 142,
            AbsentEmployees = 8,
            LateEmployees = 12,
            EarlyLeavers = 5,
            OnLeaveEmployees = 3,
            AttendancePercentage = 94.7m,
            AverageCheckInTime = TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(15)),
            AverageCheckOutTime = TimeSpan.FromHours(17).Add(TimeSpan.FromMinutes(30))
        };
    }

    public async Task<AttendanceReportDto> GetAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate, Guid? employeeId, Guid? departmentId)
    {
        _logger.LogInformation("Generating attendance report for organization: {OrganizationId} from {StartDate} to {EndDate}", 
            organizationId, startDate, endDate);
        
        // Mock implementation
        return new AttendanceReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalWorkDays = (endDate - startDate).Days + 1,
            TotalEmployees = 150,
            TotalPresentDays = 2100,
            TotalAbsentDays = 150,
            AverageAttendanceRate = 93.3m,
            DepartmentStats = new List<DepartmentAttendanceStatsDto>
            {
                new() { DepartmentName = "Engineering", AttendanceRate = 95.2m, TotalEmployees = 60 },
                new() { DepartmentName = "Sales", AttendanceRate = 92.1m, TotalEmployees = 30 },
                new() { DepartmentName = "Marketing", AttendanceRate = 94.8m, TotalEmployees = 25 }
            }
        };
    }

    public async Task<byte[]> ExportAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate, string format, Guid? employeeId)
    {
        _logger.LogInformation("Exporting attendance report for organization: {OrganizationId} in format: {Format}", 
            organizationId, format);
        
        var report = await GetAttendanceReportAsync(organizationId, startDate, endDate, employeeId, null);
        
        // Mock CSV export
        var csvData = "Date,EmployeeCode,EmployeeName,CheckIn,CheckOut,TotalHours,Status\n";
        csvData += $"{startDate:yyyy-MM-dd},EMP001,John Doe,09:00,17:00,8.0,Present\n";
        csvData += $"{startDate:yyyy-MM-dd},EMP002,Jane Smith,09:15,17:30,8.25,Present\n";
        csvData += $"{startDate:yyyy-MM-dd},EMP003,Bob Wilson,,,0,Absent\n";
        
        return System.Text.Encoding.UTF8.GetBytes(csvData);
    }

    public async Task<bool> BulkApproveAttendanceAsync(List<Guid> attendanceIds)
    {
        _logger.LogInformation("Bulk approving {Count} attendance records", attendanceIds.Count);
        
        var successCount = 0;
        foreach (var attendanceId in attendanceIds)
        {
            try
            {
                var attendance = await _attendanceRepository.GetAttendanceByIdAsync(attendanceId);
                if (attendance != null)
                {
                    attendance.Status = "Approved";
                    await _attendanceRepository.UpdateAttendanceAsync(attendanceId, attendance);
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to approve attendance {AttendanceId}", attendanceId);
            }
        }
        
        _logger.LogInformation("Successfully approved {SuccessCount} out of {TotalCount} attendance records", 
            successCount, attendanceIds.Count);
        return successCount == attendanceIds.Count;
    }

    public async Task<bool> BulkRejectAttendanceAsync(List<Guid> attendanceIds, string? reason)
    {
        _logger.LogInformation("Bulk rejecting {Count} attendance records with reason: {Reason}", 
            attendanceIds.Count, reason);
        
        var successCount = 0;
        foreach (var attendanceId in attendanceIds)
        {
            try
            {
                var attendance = await _attendanceRepository.GetAttendanceByIdAsync(attendanceId);
                if (attendance != null)
                {
                    attendance.Status = "Rejected";
                    attendance.Notes = reason;
                    await _attendanceRepository.UpdateAttendanceAsync(attendanceId, attendance);
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reject attendance {AttendanceId}", attendanceId);
            }
        }
        
        _logger.LogInformation("Successfully rejected {SuccessCount} out of {TotalCount} attendance records", 
            successCount, attendanceIds.Count);
        return successCount == attendanceIds.Count;
    }
}
