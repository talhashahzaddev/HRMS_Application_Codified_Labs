using AutoMapper;
using DocumentFormat.OpenXml.Office2021.Excel.RichDataWebImage;
using DocumentFormat.OpenXml.Wordprocessing;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Repositories;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using NPOI.SS.Formula.Functions;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Drawing;
using System.Text;
using static HRMS.API.Models.DTOs.NotificationDto;

namespace HRMS.API.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IMapper mapper,
        INotificationRepository notificationRepository,
        IEmployeeRepository employeeRepository,
        ILogger<AttendanceService> logger)
    {
        _attendanceRepository = attendanceRepository;
        _notificationRepository = notificationRepository;
        _employeeRepository = employeeRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<bool> ClockInAsync(Guid employeeId, ClockInOutRequest request)
    {
        //var EMP_Id = await _attendanceRepository.GetEmpId(employeeId);
        var result = await _attendanceRepository.ClockInOutAsync(employeeId, "in", DateTime.UtcNow, request.Location);
        //Update Attendace table on first checkin checkout From sessions

        var TodayAttendace = await _attendanceRepository.UpdateTodayAttendanceFromSessionsAsync(employeeId);
        //check if employee is  late or not 
        var latecheckin = await _attendanceRepository.UpdateTodayAttendanceLateStatusAsync(employeeId);



        return result;
    }

    public async Task<bool> ClockOutAsync(Guid employeeId, ClockInOutRequest request)
    {
        //var EMP_Id = await _attendanceRepository.GetEmpId(employeeId);
        var result =await  _attendanceRepository.ClockInOutAsync(employeeId, "out", DateTime.UtcNow, request.Location);

        //Update Attendace table on first checkin checkout From sessions

        var toTodayAttendaceda = await _attendanceRepository.UpdateTodayAttendanceFromSessionsAsync(employeeId);

        //Calculating OverTime when clockout

        var overtime = await _attendanceRepository.UpdateOvertimeHoursAsync(employeeId);


        return result;
    }

    public async Task<AttendanceDto?> GetTodayAttendanceAsync(Guid employeeId)
    {
        //var EMP_Id = await _attendanceRepository.GetEmpId(employeeId);
        var attendance = await _attendanceRepository.GetTodayAttendanceAsync(employeeId);
        return attendance != null ? _mapper.Map<AttendanceDto>(attendance) : null;
    }


    //public async Task<List<AttendanceSessionDto?>> EmployeeTodaySessions(Guid employeeId)
    //{
    //    //get attendce id of curernt date using employee id 
    //    var attendaceid = await _attendanceRepository.GetAttendanceIdAsync(employeeId);
    //    //passing attendace id to get today sessions 
    //    return await _attendanceRepository.GetTodaySessionAsync(attendaceid);
    //}

    public async Task<List<AttendanceSessionDto?>> EmployeeTodaySessions(Guid employeeId, DateTime? date = null)
    {
        // Use the provided date, or default to today
      

        // Get attendance ID for the employee on that date
        var attendanceId = await _attendanceRepository.GetAttendanceIdAsync(employeeId, date);

        // Fetch sessions for that attendance ID
        return await _attendanceRepository.GetTodaySessionAsync(attendanceId);
    }



    public async Task<PagedResult<AttendanceDto>> GetAttendanceAsync(AttendanceSearchViewModel searchModel, Guid organizationId, Guid currentEmployeeId)
    {
        // Check if user is Super Admin or HR Manager
        var isAdminOrHR = await _attendanceRepository.IsAdminOrHRAsync(currentEmployeeId, organizationId);
        
        if (isAdminOrHR)
        {
            // Super Admin or HR Manager: Get all employees' attendance
            _logger.LogInformation("User is Super Admin or HR Manager - returning all employees attendance");
            return await _attendanceRepository.GetAttendanceAsync(
                searchModel.EmployeeId, // Allow filtering by specific employee if provided
                organizationId,
                searchModel.StartDate,
                searchModel.EndDate,
                searchModel.Page,
                searchModel.PageSize, searchModel?.departmentid, searchModel?.Status);

        }
        else
        {
            // Manager: Get only their team's attendance (employees whose reporting manager is this manager)
            _logger.LogInformation("User is Manager - returning team employees attendance for manager: {EmployeeId}", currentEmployeeId);
            
            // Get team employee IDs
            var teamEmployeeIds = await _attendanceRepository.GetTeamEmployeeIdsAsync(currentEmployeeId, organizationId);
            
            // If user specified a specific employee ID, check if it's in their team
            if (searchModel.EmployeeId.HasValue)
            {
                if (!teamEmployeeIds.Contains(searchModel.EmployeeId.Value))
                {
                    // Employee not in manager's team, return empty result
                    return new PagedResult<AttendanceDto>
                    {
                        Data = new List<AttendanceDto>(),
                        TotalCount = 0,
                        Page = searchModel.Page,
                        PageSize = searchModel.PageSize
                    };
                }
                // Employee is in team, proceed with the specific employee filter
                return await _attendanceRepository.GetAttendanceAsync(
                    searchModel.EmployeeId,
                    organizationId,
                    searchModel.StartDate,
                    searchModel.EndDate,
                    searchModel.Page,
                    searchModel.PageSize);
            }
            
            // No specific employee filter, get attendance for all team members
            // Since the repository uses a database function that doesn't support filtering by multiple employee IDs,
            // we need to get all attendance and filter by team IDs in memory
            // Note: For better performance with large datasets, consider creating a repository method
            // that accepts a list of employee IDs or modifies the database function
            
            if (teamEmployeeIds.Count == 0)
            {
                // Manager has no team members, return empty result
                return new PagedResult<AttendanceDto>
                {
                    Data = new List<AttendanceDto>(),
                    TotalCount = 0,
                    Page = searchModel.Page,
                    PageSize = searchModel.PageSize
                };
            }
            
            // Get all attendance for the organization (with a large page size to get all records)
            // Then filter by team IDs and apply pagination
            var allAttendance = await _attendanceRepository.GetAttendanceAsync(
                null, // Get all
                organizationId,
                searchModel.StartDate,
                searchModel.EndDate,
                1, // Start from page 1
                100000,searchModel?.departmentid, searchModel?.Status); // Large page size to get all records (adjust based on expected data size)
            
            // Filter by team employee IDs
            var filteredAttendance = allAttendance.Data
                .Where(a => teamEmployeeIds.Contains(a.EmployeeId))
                .ToList();
            
            // Apply pagination to filtered results
            var totalCount = filteredAttendance.Count;
            var page = searchModel.Page;
            var pageSize = searchModel.PageSize;
            var skip = (page - 1) * pageSize;
            var pagedData = filteredAttendance
                .Skip(skip)
                .Take(pageSize)
                .ToList();
            
            return new PagedResult<AttendanceDto>
            {
                Data = pagedData,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }

    public async Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(Guid employeeId, DateTime startDate, DateTime endDate)
    {
        //var EMP_Id = await _attendanceRepository.GetEmpId(employeeId);

        return await _attendanceRepository.GetAttendanceSummaryAsync(employeeId, startDate, endDate);
    }

    public async Task<bool> UpdateAttendanceAsync(Guid attendanceId, CreateAttendanceRequest request)
    {
        var attendance = _mapper.Map<Attendance>(request);
        return await _attendanceRepository.UpdateAttendanceAsync(attendanceId, attendance);
    }


    public async Task<PagedResult<AttendanceSessionWithInfoDto>> EmployeeAllAttendanceAsync(
    Guid employeeId,
    DateTime startDate,
    DateTime endDate,
    int pageNumber,
    int pageSize)
    {
        return await _attendanceRepository.GetEmployeeSessionsWithInfoAsync(
            employeeId,
            startDate,
            endDate,
            pageNumber,
            pageSize
        );
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
                IsActive = true,
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
        //var EMP_Id = await _attendanceRepository.GetEmpId(employeeId);

        var attendance = await _attendanceRepository.GetEmployeeAttendanceAsync(employeeId, startDate, endDate);
        return _mapper.Map<List<AttendanceDto>>(attendance);
    }

   
    public async Task<List<AttendanceCalendarDto>> GetAttendanceCalendarAsync(Guid employeeId, int year, int month)
    {
        _logger.LogInformation("Getting attendance calendar for employee: {EmployeeId} for {Year}-{Month}",
            employeeId, year, month);
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var today = DateTime.Today;
        var attendance = await GetEmployeeAttendanceAsync(employeeId, startDate, endDate);

        var calendar = new List<AttendanceCalendarDto>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dayAttendance = attendance.FirstOrDefault(a => a.WorkDate.Date == date.Date);
            bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

            string status;
            if (date > today)
                status = "No Record"; // ✅ Future days
            else if (dayAttendance != null)
                status = dayAttendance.Status;
            else if (isWeekend)
                status = "Weekend";
            else
                status = "Absent";

            calendar.Add(new AttendanceCalendarDto
            {
                Date = date,
                Status = status,
                CheckInTime = dayAttendance?.CheckInTime,
                CheckOutTime = dayAttendance?.CheckOutTime,
                TotalHours = dayAttendance?.TotalHours ?? 0,
                IsWorkingDay = !isWeekend,
                IsHoliday = isWeekend
            });
        }

        return calendar;
    }

    public async Task<DailyAttendanceStatsDto> GetDailyAttendanceStatsAsync(Guid employeeId, Guid organizationId, DateTime date)
    {
        _logger.LogInformation("Getting daily attendance stats for organization: {OrganizationId} on {Date}",
            organizationId, date);
        
        // Check if user is Super Admin or HR Manager
        var isAdminOrHR = await _attendanceRepository.IsAdminOrHRAsync(employeeId, organizationId);
        
        if (isAdminOrHR)
        {
            // Super Admin or HR Manager: Get all employees' attendance stats
            _logger.LogInformation("User is Super Admin or HR Manager - returning all employees stats");
            var result = await _attendanceRepository.GetDailyAttendanceStatsAsync(organizationId, date);
            return result;
        }
        else
        {
            // Manager: Get only their team's attendance stats (employees whose reporting manager is this manager)
            _logger.LogInformation("User is Manager - returning team employees stats for manager: {EmployeeId}", employeeId);
            var result = await _attendanceRepository.GetDailyAttendanceStatsForManagerAsync(employeeId, organizationId, date);
            return result;
        }
    }


    //Adding here

    public async Task<bool> CreateShiftAsync(Guid organizationId, CreateShiftDto request)

    {
        var result = await _attendanceRepository.CreateShiftAsync(organizationId, request);

        return true;
    }

    public async Task<bool> AssignShiftToEmployeeAsync(Guid organizationId, AssignShiftDto request)
    {
        var result = await _attendanceRepository.AssignShiftToEmployeeAsync(organizationId, request);

        return true;


    }


    public async Task<List<EmployeeShiftDto>> GetEmployeesByShiftAsync(Guid organizationId, Guid shiftId)
    {
        var result=await _attendanceRepository.GetEmployeesByShiftAsync(organizationId, shiftId);
      
        return result;

    }

    //Shiftswap 



   public async Task<bool> CreateShiftSwapRequestAsync(Guid organizationId, CreateShiftSwapRequestDto requestDto)
    {
  
        var result= await _attendanceRepository.CreateShiftSwapRequestAsync(organizationId, requestDto);
        //Step 2: Create notification

        var employee = await _employeeRepository.GetEmployeeByIdAsync(requestDto.EmployeeId);
        //Getting reciverid
        var reportingmangerid = await _notificationRepository.GetReciveridAsync(employee.EmployeeId);
        //Getting manager employeeid
        var reciverid = await _notificationRepository.GetEmpId(reportingmangerid);
        if (result)

        {
            var notification = new NotificationCreateDto
            {
                Receiveruserid= reciverid,
                Senderuserid = employee.EmployeeId,
                Ttitle = "Swap Request",
                Message = $"Employee {employee.FirstName } Department {employee.DepartmentName}  requested shift swap.",
                Notificationtype = "Request",
                Createdby = requestDto.EmployeeId,
                moduletype="attendance",
                Redirecturl="/leave/team",
                organizationId = organizationId
            };
            await _notificationRepository.CreateNotificationAsync(organizationId, notification);
        }
 
        return result;

    }

    public async Task<bool> ApproveShiftSwapRequestAsync(ApproveShiftSwapRequestDto dto)
    {
        // Optional: Add business validation
        // Example: check if the request is still pending

        if (dto.IsApproved == false)
        {
            var success = await _attendanceRepository.RejectShiftSwapRequestAsync(dto);
        }
        if (dto.IsApproved == true)
        {
            var success = await _attendanceRepository.AcceptswapRequestAsync(dto);

        }
        return true;


    }


    public async Task<List<PendingShiftSwapDto>> GetPendingShiftSwapRequestsAsync(Guid organizationId,Guid currentEmployeeId)
    {
        var isAdminOrHR = await _attendanceRepository.IsAdminOrHRAsync(currentEmployeeId, organizationId);
        if (isAdminOrHR)
        {
            return await _attendanceRepository.GetPendingShiftSwapRequestsAsync(organizationId);
        }
        else
        {
            return await _attendanceRepository.GetManagerPendingShiftsAsync(organizationId,currentEmployeeId);
        }
       
    }

    public async Task<ShiftDto?> GetShiftByEmployeeIdAsync(Guid employeeId)
    {
        return await _attendanceRepository.GetShiftByEmployeeIdAsync(employeeId);
    }

    public async Task<List<PendingShiftSwapDto>> GetShiftSwapRequestsByEmployeeAsync(Guid employeeId)
    {
        return await _attendanceRepository.GetShiftSwapRequestsByEmployeeAsync(employeeId);
    }





    public async Task<List<ShiftDto>> GetAllShiftsAsync(Guid organizationId)
    {
        try
        {
            return await _attendanceRepository.GetAllShiftsAsync(organizationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shifts for organization {OrganizationId}", organizationId);
            throw;
        }
    }


    public async Task<bool> UpdateShiftAsync(Guid organizationId, Guid shiftId, UpdateShiftDto request)
    {
        return await _attendanceRepository.UpdateShiftAsync(organizationId, shiftId, request);
    }

    public async Task<bool> DeleteShiftAsync(Guid organizationId, Guid shiftId)
    {
        return await _attendanceRepository.DeleteShiftAsync(organizationId, shiftId);
    }






    public async Task<AttendanceReportDto> GetAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate, Guid currentEmployeeId, Guid? departmentId, string? status, Guid? employeeId)
    {
        try
        {
            // Check if user is Super Admin or HR Manager
            var isAdminOrHR = await _attendanceRepository.IsAdminOrHRAsync(currentEmployeeId, organizationId);

            AttendanceReportDto completeReport;

            if (isAdminOrHR)
            {
                // Super Admin or HR Manager: Get all employees' attendance report
                _logger.LogInformation("User is Super Admin or HR Manager - returning all employees attendance report");
                completeReport = await GetFullAttendanceReportAsync(organizationId, startDate, endDate, departmentId, status, employeeId);
            }
            else
            {
                // Manager: Get only their team's attendance report
                _logger.LogInformation("User is Manager - returning team employees attendance report for manager: {EmployeeId}", currentEmployeeId);
                completeReport = await GetManagerAttendanceReportAsync(currentEmployeeId, organizationId, startDate, endDate, departmentId, status, employeeId);
            }

            return completeReport;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating complete attendance report");
            throw;
        }
    }

 
    public async Task<List<EmployeeSimpleDto>> GetdepartmentEmployeesAsync(
    Guid currentEmployeeId,
    Guid? departmentId,
    Guid organizationId)
    {
        List<EmployeeDto> result;

        // Check if user is Super Admin or HR Manager
        var isAdminOrHR = await _attendanceRepository.IsAdminOrHRAsync(currentEmployeeId, organizationId);

        if (isAdminOrHR)
        {
            // Super Admin or HR Manager: Get all employees or filtered by department
            _logger.LogInformation("User is Super Admin or HR Manager - returning all employees");

            return  await _attendanceRepository.GetEmployeeForAdminAsync(organizationId,departmentId);
        }
        else
        {
            // Manager: Get only their team's employees, optionally filtered by department
            _logger.LogInformation("User is Manager - returning team employees for manager: {EmployeeId}", currentEmployeeId);

            return await _attendanceRepository.GetManagerEmployeeAsync(currentEmployeeId, organizationId, departmentId);
        }

        
    }




    private async Task<AttendanceReportDto> GetFullAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate, Guid? departmentId, string? status,Guid? employeeId)
    {
        var mainReport = await _attendanceRepository.GetAttendanceReportAsync(organizationId, startDate, endDate);
        var departmentStats = await _attendanceRepository.GetDepartmentAttendanceStatsAsync(organizationId, startDate, endDate,departmentId);
        var dailyStats = await _attendanceRepository.GetDailyAttendanceStatsAsync(organizationId, startDate, endDate);
        var attendanceRecords = await _attendanceRepository.GetAttendanceRecordsAsync(organizationId, startDate, endDate,departmentId,status,employeeId);

        var totalWorkDays = Enumerable.Range(0, (endDate - startDate).Days + 1)
            .Select(offset => startDate.AddDays(offset))
            .Count(date => date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday);

        var totalPossibleDays = mainReport.TotalEmployees * totalWorkDays;
        var calculatedAbsentDays = totalPossibleDays - mainReport.TotalPresentDays;

        return new AttendanceReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalWorkDays = totalPossibleDays,
            TotalEmployees = mainReport.TotalEmployees,
            TotalPresentDays = mainReport.TotalPresentDays,
            TotalAbsentDays = calculatedAbsentDays,
            AverageAttendanceRate = (mainReport.TotalEmployees > 0 && totalWorkDays > 0)
                ? Math.Round((mainReport.TotalPresentDays * 100.0m) / (mainReport.TotalEmployees * totalWorkDays), 2)
                : 0,
            Records = attendanceRecords,
            DailyStats = dailyStats,
            DepartmentStats = departmentStats,
            Summary = new AttendanceSummaryDto
            {
                TotalWorkDays = totalWorkDays,
                PresentDays = mainReport.TotalPresentDays,
                AbsentDays = mainReport.TotalAbsentDays,
                LateDays = mainReport.TotalLateDays,
                TotalHours = mainReport.TotalHours,
                OvertimeHours = mainReport.OvertimeHours,
                AverageHoursPerDay = totalWorkDays > 0 ?
                    Math.Round(mainReport.TotalHours / totalWorkDays, 2) : 0
            }
        };
    }

    private async Task<AttendanceReportDto> GetManagerAttendanceReportAsync(Guid managerEmployeeId, Guid organizationId, DateTime startDate, DateTime endDate,Guid? departmentId,string? status,Guid? employeeId)
    {
        // Get team employee IDs
        var teamEmployeeIds = await _attendanceRepository.GetTeamEmployeeIdsAsync(managerEmployeeId, organizationId);
        
        if (teamEmployeeIds.Count == 0)
        {
            // Manager has no team members, return empty report
            var workDaysCount = Enumerable.Range(0, (endDate - startDate).Days + 1)
                .Select(offset => startDate.AddDays(offset))
                .Count(date => date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday);

            return new AttendanceReportDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalWorkDays = 0,
                TotalEmployees = 0,
                TotalPresentDays = 0,
                TotalAbsentDays = 0,
                AverageAttendanceRate = 0,
                Records = new List<AttendanceDto>(),
                DailyStats = new List<DailyAttendanceStatsDto>(),
                DepartmentStats = new List<DepartmentAttendanceStatsDto>(),
                Summary = new AttendanceSummaryDto
                {
                    TotalWorkDays = workDaysCount,
                    PresentDays = 0,
                    AbsentDays = 0,
                    LateDays = 0,
                    TotalHours = 0,
                    OvertimeHours = 0,
                    AverageHoursPerDay = 0
                }
            };
        }

        // Get all attendance records and filter by team
        var allAttendanceRecords = await _attendanceRepository.GetAttendanceRecordsAsync(organizationId, startDate, endDate,departmentId,status,employeeId);
        var teamAttendanceRecords = allAttendanceRecords
            .Where(r => teamEmployeeIds.Contains(r.EmployeeId))
            .ToList();

        // Get all daily stats and filter by team
        var allDailyStats = await _attendanceRepository.GetDailyAttendanceStatsAsync(organizationId, startDate, endDate);
        // Note: Daily stats are aggregated, so we'll need to recalculate based on team records
        // For now, we'll use the filtered records to calculate stats

        // Get all department stats - filter to only include departments with team members
        var allDepartmentStats = await _attendanceRepository.GetDepartmentAttendanceStatsAsync(organizationId, startDate, endDate,departmentId);
        // Filter department stats based on team members (this is approximate, as department stats are aggregated)
        var teamDepartmentStats = allDepartmentStats
            .Where(d => teamAttendanceRecords.Any(r => 
                // We can't directly match department, so we'll include all for now
                // In a production system, you'd want to join with employee data to filter properly
                true))
            .ToList();

        // Calculate summary statistics from team records
        var totalWorkDays = Enumerable.Range(0, (endDate - startDate).Days + 1)
            .Select(offset => startDate.AddDays(offset))
            .Count(date => date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday);

        var totalEmployees = teamEmployeeIds.Count;
        var totalPresentDays = teamAttendanceRecords.Count(r => r.Status?.ToLower() == "present");
        var totalAbsentDays = teamAttendanceRecords.Count(r => r.Status?.ToLower() == "absent");
        var totalLateDays = teamAttendanceRecords.Count(r => r.Status?.ToLower() == "late");
        var totalHours = teamAttendanceRecords.Sum(r => r.TotalHours);
        var overtimeHours = teamAttendanceRecords.Sum(r => r.OvertimeHours);
        var totalPossibleDays = totalEmployees * totalWorkDays;
        var calculatedAbsentDays = totalPossibleDays - totalPresentDays;

        // Calculate daily stats for team
        var teamDailyStats = new List<DailyAttendanceStatsDto>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                continue;

            var dayRecords = teamAttendanceRecords.Where(r => r.WorkDate.Date == date.Date).ToList();
            var presentCount = dayRecords.Count(r => r.Status?.ToLower() == "present");
            var absentCount = totalEmployees - presentCount;

            teamDailyStats.Add(new DailyAttendanceStatsDto
            {
                Date = date,
                TotalEmployees = totalEmployees,
                PresentEmployees = presentCount,
                AbsentEmployees = absentCount,
                OnLeaveEmployees = dayRecords.Count(r => r.Status?.ToLower() == "on_leave"),
                AttendancePercentage = totalEmployees > 0 ? Math.Round((presentCount * 100.0m) / totalEmployees, 2) : 0
            });
        }

        return new AttendanceReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalWorkDays = totalPossibleDays,
            TotalEmployees = totalEmployees,
            TotalPresentDays = totalPresentDays,
            TotalAbsentDays = calculatedAbsentDays,
            AverageAttendanceRate = (totalEmployees > 0 && totalWorkDays > 0)
                ? Math.Round((totalPresentDays * 100.0m) / totalPossibleDays, 2)
                : 0,
            Records = teamAttendanceRecords,
            DailyStats = teamDailyStats,
            DepartmentStats = teamDepartmentStats,
            Summary = new AttendanceSummaryDto
            {
                TotalWorkDays = totalWorkDays,
                PresentDays = totalPresentDays,
                AbsentDays = totalAbsentDays,
                LateDays = totalLateDays,
                TotalHours = totalHours,
                OvertimeHours = overtimeHours,
                AverageHoursPerDay = totalWorkDays > 0 ?
                    Math.Round(totalHours / totalWorkDays, 2) : 0
            }
        };
    }

    public async Task<byte[]> ExportAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate, string format, Guid? employeeId, Guid currentEmployeeId,Guid? departmentId,string? status)
    {
        try
        {
            _logger.LogInformation("Exporting attendance report for organization: {OrganizationId} in format: {Format}",
                organizationId, format);

            var attendanceRecords = await GetAttendanceReportAsync(
                organizationId, startDate, endDate, currentEmployeeId,departmentId,status,employeeId);

            if (format.ToLower() == "csv")
            {
                return GenerateCsvExport(attendanceRecords.Records, startDate, endDate);
            }
            else
            {
                return GenerateExcelExport(attendanceRecords.Records, startDate, endDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting attendance report");
            throw;
        }
    }

    private byte[] GenerateCsvExport(List<AttendanceDto> records, DateTime startDate, DateTime endDate)
    {
        var csvBuilder = new StringBuilder();

        csvBuilder.AppendLine("Date,Employee Name,Check In,Check Out,Total Hours,Overtime Hours,Status,Notes");

        foreach (var record in records)
        {
            var checkInTime = record.CheckInTime?.ToString("HH:mm") ?? "N/A";
            var checkOutTime = record.CheckOutTime?.ToString("HH:mm") ?? "N/A";
            var totalHours = record.TotalHours.ToString("0.00");
            var overtimeHours = record.OvertimeHours.ToString("0.00");
            var notes = string.IsNullOrEmpty(record.Notes) ? "" : record.Notes.Replace(",", ";");

            csvBuilder.AppendLine($"\"{record.WorkDate:yyyy-MM-dd}\",\"{record.EmployeeName}\",\"{checkInTime}\",\"{checkOutTime}\",\"{totalHours}\",\"{overtimeHours}\",\"{record.Status}\",\"{notes}\"");
        }

        return Encoding.UTF8.GetBytes(csvBuilder.ToString());
    }
    private byte[] GenerateExcelExport(List<AttendanceDto> records, DateTime startDate, DateTime endDate)
    {
        var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
        var sheet = workbook.CreateSheet("Attendance Report");

        var headerRow = sheet.CreateRow(0);
        string[] headers = { "Date", "Employee Name", "Check In", "Check Out", "Total Hours", "Overtime Hours", "Status", "Notes" };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = headerRow.CreateCell(i);
            cell.SetCellValue(headers[i]);

            var headerStyle = workbook.CreateCellStyle();
            var headerFont = workbook.CreateFont();
            headerFont.IsBold = true;
            headerStyle.SetFont(headerFont);
            headerStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;
            headerStyle.FillPattern = NPOI.SS.UserModel.FillPattern.SolidForeground;
            cell.CellStyle = headerStyle;
        }

        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            var row = sheet.CreateRow(i + 1);

            row.CreateCell(0).SetCellValue(record.WorkDate.ToString("yyyy-MM-dd"));
            row.CreateCell(1).SetCellValue(record.EmployeeName);
            row.CreateCell(2).SetCellValue(record.CheckInTime?.ToString("HH:mm") ?? "N/A");
            row.CreateCell(3).SetCellValue(record.CheckOutTime?.ToString("HH:mm") ?? "N/A");
            row.CreateCell(4).SetCellValue((double)record.TotalHours);
            row.CreateCell(5).SetCellValue((double)record.OvertimeHours);
            row.CreateCell(6).SetCellValue(record.Status);
            row.CreateCell(7).SetCellValue(record.Notes ?? "");
        }

        for (int i = 0; i < headers.Length; i++)
        {
            sheet.AutoSizeColumn(i);
        }

        using var stream = new MemoryStream();
        workbook.Write(stream);
        return stream.ToArray();
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
    public async Task<Guid> GetEmployeeFromTable(Guid Userid)
    {
        _logger.LogInformation("Getting Employee Id against User id: {EmployeeId}", Userid);
        return await _attendanceRepository.GetEmployeeFromTable(Userid);
    }
}
