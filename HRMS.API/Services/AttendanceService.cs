using AutoMapper;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2021.Excel.RichDataWebImage;
using DocumentFormat.OpenXml.Wordprocessing;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Repositories;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using iTextSharp.text;
using Mailjet.Client.Resources;
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
    private readonly ILeaveRepository _leaveRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AttendanceService> _logger;
    private readonly IIpAddressRepository _ipAddressRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IEmailService _emailService;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IMapper mapper,
        INotificationRepository notificationRepository,
        IEmployeeRepository employeeRepository,
        ILeaveRepository leaveRepository,
        IIpAddressRepository ipAddressRepository,
        IEmailService emailService,
        ISettingsRepository settingsRepository,
        ILogger<AttendanceService> logger)
    {
        _attendanceRepository = attendanceRepository;
        _notificationRepository = notificationRepository;
        _employeeRepository = employeeRepository;
        _leaveRepository = leaveRepository;
        _ipAddressRepository = ipAddressRepository;
        _settingsRepository = settingsRepository;
        _emailService = emailService;
        _mapper = mapper;
        _logger = logger;
    }
 
    public async Task<ClockInOutResponse> ClockInAsync(
    Guid employeeId,
    ClockInOutRequest request,
    string ipAddress)
    {
        if (employeeId == Guid.Empty)
            throw new ArgumentException("Employee ID cannot be empty");

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var allowedIp = await _ipAddressRepository.GetByIpAsync(ipAddress);
        string locationType = allowedIp != null ? "Office" : "Outside";

        var shift = await _attendanceRepository.GetShiftByEmployeeIdAsync(employeeId);
        if (shift == null)
            throw new InvalidOperationException("No shift assigned");

        var now = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time"));
        var shiftStartDateTime = now.Date + shift.StartTime;
        var shiftStart = shiftStartDateTime.AddHours(+shift.marginHours);
        var allowedFrom = shiftStartDateTime.AddHours(-shift.marginHours);
        var today = (int)now.DayOfWeek;
        // Check if today is included in shift days
        if (shift.DaysOfWeek == null || !shift.DaysOfWeek.Contains(today))
        {
            throw new InvalidOperationException("You have no shift assigned for today, so you cannot clock in.");
        }

        if (shift.ApplyMarginhours)
        {
            if (now < allowedFrom)
                throw new InvalidOperationException("Clock-in outside allowed margin hours");
        }
        if (await _attendanceRepository.CheckAlreadyClockInAsync(employeeId))
            throw new InvalidOperationException("Already clocked in");
        var todayAttendance = await _attendanceRepository.GetTodayAttendanceAsync(employeeId);
       

        string message = "Clocked in successfully";

        if (todayAttendance == null && now > shiftStart)
        {
            var lateBy = now - shiftStartDateTime;
            message =
                $"Clocked in successfully, but you are late by {lateBy.Hours}h {lateBy.Minutes}m";
        }
        var location = new Dictionary<string, object?>
    {
        { "ip", ipAddress },
        { "locationType", locationType }
    };

        await _attendanceRepository.ClockInOutAsync(
            employeeId,
            "in",
            DateTime.UtcNow,
            location,
            request.Notes);

        await _attendanceRepository.UpdateTodayAttendanceFromSessionsAsync(employeeId);
        await _attendanceRepository.UpdateTodayAttendanceLateStatusAsync(employeeId);

        _logger.LogInformation(
            "Employee {EmployeeId} clocked in from {IP} as {LocationType}",
            employeeId,
            ipAddress,
            locationType);

        return new ClockInOutResponse
        {
            Success = true,
            Message = message
        };
    }

    public async Task<ClockInOutResponse> ClockOutAsync(
    Guid employeeId,
    ClockInOutRequest request,
    string ipAddress)
    {
        _logger.LogInformation("Clocking out employee: {EmployeeId}", employeeId);
        if (employeeId == Guid.Empty)
            throw new ArgumentException("Employee ID cannot be empty");

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // ✅ 1️⃣ Check allowed IP
        var allowedIp = await _ipAddressRepository.GetByIpAsync(ipAddress);
        string locationType = allowedIp != null ? "Office" : "Outside";

        // ✅ 2️⃣ Get shift
        var shift = await _attendanceRepository.GetShiftByEmployeeIdAsync(employeeId);
        if (shift == null)
            throw new InvalidOperationException("No shift assigned");

        var isClockedIn =
            await _attendanceRepository.CheckAlreadyClockInAsync(employeeId);

        if (!isClockedIn)
            throw new InvalidOperationException("Not clocked in");

        var margin = TimeSpan.FromHours(shift.marginHours);

        var nowDateTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time"));

        var now = nowDateTime.TimeOfDay;

        var allowedFrom = shift.EndTime - margin;
        var allowedTo = shift.EndTime + margin;
        string message;
        if (now < allowedFrom)
        {
            var earlyBy = shift.EndTime - now;
            message =
                $"Clocked out successfully, but you left early by {earlyBy.Hours}h {earlyBy.Minutes}m";
        }
        else if (now > allowedTo)
        {
            var lateBy = now - shift.EndTime;
            message =
                $"Clocked out successfully, but you left late by {lateBy.Hours}h {lateBy.Minutes}m";
        }
        else
        {
            message = "Clocked out successfully";
        }
        // ✅ Store IP + LocationType
        var location = new Dictionary<string, object?>
    {
        { "ip", ipAddress },
        { "locationType", locationType }
    };

        await _attendanceRepository.ClockInOutAsync(
            employeeId,
            "out",
            DateTime.UtcNow,
            location,
            request.Notes);
        await _attendanceRepository.UpdateTodayAttendanceFromSessionsAsync(employeeId);
        await _attendanceRepository.UpdateOvertimeHoursAsync(employeeId);

        _logger.LogInformation(
            "Employee {EmployeeId} clocked out from {IP} as {LocationType}",
            employeeId,
            ipAddress,
            locationType);

        return new ClockInOutResponse
        {
            Success = true,
            Message = message
        };
    }


    public async Task<AttendanceDto?> GetTodayAttendanceAsync(Guid employeeId)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException("Employee ID cannot be empty", nameof(employeeId));
        }
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
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException("Employee ID cannot be empty", nameof(employeeId));
        }
        var attendanceId = await _attendanceRepository.GetAttendanceIdAsync(employeeId, date);
        return await _attendanceRepository.GetTodaySessionAsync(attendanceId);
    }


    public async Task<PagedResult<AttendanceDto>> GetAttendanceAsync(
        AttendanceSearchViewModel searchModel,
        Guid organizationId,
        Guid currentEmployeeId)
    {
        if (searchModel == null)
            throw new ArgumentNullException(nameof(searchModel));

        if (searchModel.Page <= 0)
            throw new ArgumentException("Page must be greater than zero");

        if (searchModel.PageSize <= 0)
            throw new ArgumentException("Page size must be greater than zero");

        if (searchModel.EndDate < searchModel.StartDate)
            throw new ArgumentException("End date cannot be earlier than start date");

        var isAdminOrHR =
            await _attendanceRepository.IsAdminOrHRAsync(currentEmployeeId, organizationId);

        PagedResult<AttendanceDto> result;

        if (isAdminOrHR)
        {
            // ✅ Admin / HR
            result = await _attendanceRepository.GetAttendanceAsync(
                searchModel.SearchTerm,
                searchModel.EmployeeId,
                organizationId,
                searchModel.StartDate,
                searchModel.EndDate,
                searchModel.Page,
                searchModel.PageSize,
                searchModel?.departmentid,
                searchModel?.Status);
        }
        else
        {
            // ✅ Manager
            var teamEmployeeIds =
                await _attendanceRepository.GetTeamEmployeeIdsAsync(currentEmployeeId, organizationId);

            if (searchModel.EmployeeId.HasValue &&
                !teamEmployeeIds.Contains(searchModel.EmployeeId.Value))
            {
                return new PagedResult<AttendanceDto>
                {
                    Data = new List<AttendanceDto>(),
                    TotalCount = 0,
                    Page = searchModel.Page,
                    PageSize = searchModel.PageSize
                };
            }
            if (searchModel.EmployeeId.HasValue)
            {
                result = await _attendanceRepository.GetAttendanceAsync(
                    searchModel.SearchTerm,
                    searchModel.EmployeeId,
                    organizationId,
                    searchModel.StartDate,
                    searchModel.EndDate,
                    searchModel.Page,
                    searchModel.PageSize);
            }
            else
            {
                if (teamEmployeeIds.Count == 0)
                {
                    return new PagedResult<AttendanceDto>
                    {
                        Data = new List<AttendanceDto>(),
                        TotalCount = 0,
                        Page = searchModel.Page,
                        PageSize = searchModel.PageSize
                    };
                }

                var allAttendance =
                    await _attendanceRepository.GetAttendanceAsync(
                        searchModel.SearchTerm,
                        null,
                        organizationId,
                        searchModel.StartDate,
                        searchModel.EndDate,
                        1,
                        100000,
                        searchModel?.departmentid,
                        searchModel?.Status);

                var filteredAttendance = allAttendance.Data
                    .Where(a => teamEmployeeIds.Contains(a.EmployeeId))
                    .ToList();

                var totalCount = filteredAttendance.Count;
                var skip = (searchModel.Page - 1) * searchModel.PageSize;

                var pagedData = filteredAttendance
                    .Skip(skip)
                    .Take(searchModel.PageSize)
                    .ToList();

                result = new PagedResult<AttendanceDto>
                {
                    Data = pagedData,
                    TotalCount = totalCount,
                    Page = searchModel.Page,
                    PageSize = searchModel.PageSize
                };
            }
        }
        var allowedIps = await _ipAddressRepository.GetAllAsync();
        var allowedIpStrings = allowedIps
            .Select(ip => ip.IpAddressValue)
            .ToHashSet();

        foreach (var attendance in result.Data)
        {
            attendance.checkinLocationType =
                attendance.checkinip != null &&
                allowedIpStrings.Contains(attendance.checkinip)
                    ? "Office"
                    : "Outside";

            attendance.checkoutLocationType =
                attendance.checkoutip != null &&
                allowedIpStrings.Contains(attendance.checkoutip)
                    ? "Office"
                    : "Outside";
        }

        return result;
    }

    public async Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(Guid employeeId, DateTime startDate, DateTime endDate)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException("Employee ID cannot be empty", nameof(employeeId));
        }
        if (endDate < startDate)
        {
            throw new ArgumentException("End date cannot be earlier than start date", nameof(endDate));
        }
        return await _attendanceRepository.GetAttendanceSummaryAsync(employeeId, startDate, endDate);
    }

    public async Task<bool> UpdateAttendanceAsync(Guid attendanceId, CreateAttendanceRequest request)
    {
        if (attendanceId == Guid.Empty)
        {
            throw new ArgumentException("Attendance ID cannot be empty", nameof(attendanceId));
        }
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Request cannot be null");
        }
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
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException("Employee ID cannot be empty", nameof(employeeId));
        }
        if (endDate < startDate)
        {
            throw new ArgumentException("End date cannot be earlier than start date", nameof(endDate));
        }
        if (pageNumber <= 0)
        {
            throw new ArgumentException("Page number must be greater than zero", nameof(pageNumber));
        }
        if (pageSize <= 0)
        {
            throw new ArgumentException("Page size must be greater than zero", nameof(pageSize));
        }
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
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("Department ID cannot be empty", nameof(departmentId));
        }
        return await _attendanceRepository.GetDepartmentAttendanceAsync(departmentId, date);
    }

    // New missing methods implementation
    public async Task<TimeTrackingSessionDto?> GetCurrentSessionAsync(Guid employeeId)
    {
        _logger.LogInformation("Getting current session for employee: {EmployeeId}", employeeId);
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException("Employee ID cannot be empty", nameof(employeeId));
        }

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
        if (attendanceId == Guid.Empty)
        {
            throw new ArgumentException("Attendance ID cannot be empty", nameof(attendanceId));
        }
        
        var attendance = await _attendanceRepository.GetAttendanceByIdAsync(attendanceId);
        return attendance != null ? _mapper.Map<AttendanceDto>(attendance) : null;
    }

    public async Task<AttendanceDto> CreateManualAttendanceAsync(ManualAttendanceRequest request)
    {
        _logger.LogInformation("Creating manual attendance for employee: {EmployeeId}", request.EmployeeId);
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Request cannot be null");
        }
        if (request.EmployeeId == Guid.Empty)
        {
            throw new ArgumentException("Employee ID is required", nameof(request));
        }
        if (request.CheckOutTime.HasValue && request.CheckOutTime.Value < request.CheckInTime)
        {
            throw new ArgumentException("Check-out time cannot be earlier than check-in time", nameof(request));
        }
        
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
        if (attendanceId == Guid.Empty)
        {
            throw new ArgumentException("Attendance ID cannot be empty", nameof(attendanceId));
        }
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Request cannot be null");
        }
        if (request.CheckOutTime.HasValue && request.CheckOutTime.Value < request.CheckInTime)
        {
            throw new ArgumentException("Check-out time cannot be earlier than check-in time", nameof(request));
        }
        
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
        if (attendanceId == Guid.Empty)
        {
            throw new ArgumentException("Attendance ID cannot be empty", nameof(attendanceId));
        }
        
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
        
        // Get approved leave requests for the date range
        // Expand the date range slightly to catch leaves that overlap with the month
        var expandedStartDate = startDate.AddDays(-1);
        var expandedEndDate = endDate.AddDays(1);
        var approvedLeaves = await _leaveRepository.GetApprovedLeaveRequestsAsync(employeeId, expandedStartDate, expandedEndDate);
        
        var leaveDates = new HashSet<DateTime>();
        foreach (var leave in approvedLeaves)
        {
            for (var date = leave.StartDate.Date; date <= leave.EndDate.Date; date = date.AddDays(1))
            {
                // Only add dates that are within the actual month range
                if (date >= startDate && date <= endDate)
                {
                    leaveDates.Add(date.Date); // Ensure we're using Date only (normalized)
                }
            }
        }
        
        _logger.LogInformation("Found {Count} approved leave dates for employee {EmployeeId} in {Year}-{Month}. Leaves: {Leaves}", 
            leaveDates.Count, employeeId, year, month, string.Join(", ", leaveDates));

        var calendar = new List<AttendanceCalendarDto>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var normalizedDate = date.Date; // Normalize to date only (remove time component)
            var dayAttendance = attendance.FirstOrDefault(a => a.WorkDate.Date == normalizedDate);
            bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

            string status;
            
            // ✅ Check for leave FIRST - even for future dates and weekends
            if (leaveDates.Contains(normalizedDate))
            {
                status = "Leave";
                _logger.LogDebug("Date {Date} marked as Leave for employee {EmployeeId}", normalizedDate, employeeId);
            }
            else if (normalizedDate > today)
            {
                status = "No Record"; // ✅ Future days (only if not on leave)
            }
            else if (dayAttendance != null)
            {
                status = dayAttendance.Status;
            }
            else if (isWeekend)
            {
                status = "Weekend";
            }
            else
            {
                status = "Absent";
            }

            calendar.Add(new AttendanceCalendarDto
            {
                Date = date,
                Status = status,
                CheckInTime = dayAttendance?.CheckInTime,
                CheckOutTime = dayAttendance?.CheckOutTime,
                TotalHours = dayAttendance?.TotalHours ?? 0,
                IsWorkingDay = !isWeekend,
                IsHoliday = false // ✅ Don't mark weekends as holidays - use Status instead
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

    public async Task<bool> AssignShiftToEmployeeAsync(Guid organizationId, AssignShiftDto request, Guid currentEmpid)
    {
        if (request.SelectedEmployees == null || !request.SelectedEmployees.Any())
            return false;

        // 1️⃣ Update all shifts in one DB call
        var assignResult = await _attendanceRepository.AssignShiftToEmployeeAsync(organizationId, request);
        if (!assignResult)
            return false;

        var currentEmp = await _employeeRepository.GetEmployeeByIdAsync(currentEmpid);
        var shiftDetails = await _attendanceRepository.GetshiftInfoAsync(request.ShiftId);

        // 2️⃣ Prepare receiver list (Selected Employees)
        var receiverIds = request.SelectedEmployees.ToList();
       
        if (receiverIds.Any())
        {
            foreach (var receiverId in receiverIds)
            {
                var message = receiverId == currentEmpid ? $"You assigned shift of {shiftDetails.ShiftName} successfully" :                                                     $"{currentEmp.FirstName} {currentEmp.LastName} assigned you shift of {shiftDetails.ShiftName}";
                var notification = new NotificationCreateDto
                {
                    Receiveruserid = new List<Guid> { receiverId },  // All selected employees at once
                    Senderuserid = currentEmpid,
                    Title = "Shift Assign",
                    Message = message,
                    Notificationtype = "Message",
                    Createdby = currentEmpid,
                    moduletype = "Shift",
                    Redirecturl = "/attendance/shift",
                    organizationId = organizationId
                };

                await _notificationRepository.CreateNotificationAsync(organizationId, notification);


            }


            //Implemennting Email in news creation
            var ReciverEmails = new List<string>();
            var Emailmessage = $"{currentEmp.FirstName} {currentEmp.LastName} assigned you shift of {shiftDetails.ShiftName}";
            var selectedEmployees = await _employeeRepository.GeSelectedtEmployeesByIdsAsync(receiverIds);
            ReciverEmails = selectedEmployees.Where(e => e.Email != null).Select(e => e.Email).ToList();

            await _emailService.SendAssignShiftEmailAsync(ReciverEmails, Emailmessage, organizationId);
        }

        return true;
    }



    public async Task<List<EmployeeShiftDto>> GetEmployeesByShiftAsync(Guid organizationId, Guid shiftId,Guid currentEmployeeId)
    {

        try
        {

            // Check if user is Super Admin or HR Manager
            var isAdminOrHR = await _attendanceRepository.IsAdminOrHRAsync(currentEmployeeId, organizationId);

            AttendanceReportDto completeReport;

            if (isAdminOrHR)
            {
                // Super Admin 

                var result = await _attendanceRepository.GetEmployeesByShiftAsync(organizationId, shiftId);
                return result;
            }
            else
            {
                //For Manager 
                var result = await _attendanceRepository.GetManagerEmployeesByShiftAsync(organizationId, shiftId,currentEmployeeId);

                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Employee By shift");
            throw;
        }



    }

    //Shiftswap 

   public async Task<bool> CreateShiftSwapRequestAsync(Guid organizationId, CreateShiftSwapRequestDto requestDto)
    {   
        //Step 2: Create notification
        var EMPID = await _attendanceRepository.GetEmpId(requestDto.EmployeeId);

        var employee = await _employeeRepository.GetEmployeeByIdAsync(EMPID);
        //Getting reciverid
        var reportingmangerid = await _notificationRepository.GetReciveridAsync(employee.EmployeeId);
        //Getting manager employeeid
        var reportingManagerDetail = await _employeeRepository.GetEmployeeByIdAsync(reportingmangerid);
       if (reportingManagerDetail!=null)
        {
            var result = await _attendanceRepository.CreateShiftSwapRequestAsync(organizationId, requestDto);
            var shiftDetails = await _attendanceRepository.GetshiftInfoAsync(requestDto.RequestedShiftId);


            if (result != null)

            {
                var notification = new NotificationCreateDto
                {
                    Receiveruserid = new List<Guid> { reportingmangerid },
                    Senderuserid = employee.EmployeeId,
                    Title = "Swap Request",
                    Message = $"Employee {employee.FirstName} Department {employee.DepartmentName}  requested shift of {shiftDetails.ShiftName}",
                    Notificationtype = "Request",
                    Createdby = requestDto.EmployeeId,
                    moduletype = "attendance",
                    Redirecturl = "/attendance/shift",
                    RequestId = result.RequestId,
                    organizationId = organizationId
                };
                await _notificationRepository.CreateNotificationAsync(organizationId, notification);

            }
            return true;
        }
        else
        {
            throw new InvalidOperationException("No Reporting manager is assigned");

        }



    }

    public async Task<bool> ApproveShiftSwapRequestAsync(ApproveShiftSwapRequestDto dto, Guid organizationId)
    {
        // Optional: Add business validation
        // Example: check if the request is still pending
        var shiftrequestDetails = await _attendanceRepository.getshifrequestDetailsAsync(dto.RequestId);


        if (dto.IsApproved == false)
        {
            var success = await _attendanceRepository.RejectShiftSwapRequestAsync(dto);

            //Create Notification 
            if (success != null)
            {
                var notification = new NotificationCreateDto
                {
                    Receiveruserid = new List<Guid> { shiftrequestDetails.EmployeeId },
                    Senderuserid = dto.ApprovedBy,
                    Title = "Shift Swap Request",
                    Message = $"Your shift swap request is Rejected {dto.RejectionReason} ",
                    Notificationtype = "Message",
                    Createdby = dto.ApprovedBy,
                    moduletype = "attendance",
                    Redirecturl = "/attendance/shift",
                    organizationId = organizationId
                };
                await _notificationRepository.CreateNotificationAsync(organizationId, notification);

            }
        }
        if (dto.IsApproved == true)
        {
            var success = await _attendanceRepository.AcceptswapRequestAsync(dto);
            //Create Notification 
            if (success != null)
            {
                var notification = new NotificationCreateDto
                {
                    Receiveruserid = new List<Guid> { shiftrequestDetails.EmployeeId },
                    Senderuserid = dto.ApprovedBy,
                    Title = "Shift Swap Request",
                    Message = $"Your shift swap request is Accepted ",
                    Notificationtype = "Message",
                    Createdby = dto.ApprovedBy,
                    moduletype = "attendance",
                    Redirecturl = "/attendance/shift",
                    organizationId = organizationId
                };
                await _notificationRepository.CreateNotificationAsync(organizationId, notification);

            }
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
        var empId = await _attendanceRepository.GetEmpId(employeeId);
        return await _attendanceRepository.GetShiftByEmployeeIdAsync(empId);
    }

    public async Task<List<PendingShiftSwapDto>> GetShiftSwapRequestsByEmployeeAsync(Guid employeeId)
    {
        return await _attendanceRepository.GetShiftSwapRequestsByEmployeeAsync(employeeId);
    }
    //Getting Shift Summaary
    public async Task<ShiftSummary> GetShiftSummaryAsync(Guid organizationId, Guid currentEmployeeId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Invalid organization id");

        if (currentEmployeeId == Guid.Empty)
            throw new ArgumentException("Invalid employee id");

        var summary = await _attendanceRepository.GetShiftSummaryAsync(organizationId, currentEmployeeId);

        if (summary == null)
        {
            return new ShiftSummary
            {
                TotalShifts = 0,
                ShiftAssignedEmployee = 0,
                UnassignedShiftsEmp = 0,
                shiftSwapRequests = 0
            };
        }

        return summary;
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


    public async Task<AttendanceReportDto> GetAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate, Guid currentEmployeeId, Guid? departmentId, string? status, Guid? employeeId, int pageNumber = 1, int pageSize = 10)
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
                completeReport = await GetFullAttendanceReportAsync(organizationId, startDate, endDate, departmentId, status, employeeId, pageNumber, pageSize);
            }
            else
            {
                // Manager: Get only their team's attendance report
                _logger.LogInformation("User is Manager - returning team employees attendance report for manager: {EmployeeId}", currentEmployeeId);
                completeReport = await GetManagerAttendanceReportAsync(currentEmployeeId, organizationId, startDate, endDate, departmentId, status, employeeId, pageNumber, pageSize);
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
        if (currentEmployeeId == Guid.Empty)
        {
            throw new ArgumentException("Current employee ID cannot be empty", nameof(currentEmployeeId));
        }
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        }
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

    private async Task<AttendanceReportDto> GetFullAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate, Guid? departmentId, string? status, Guid? employeeId, int pageNumber = 1, int pageSize = 10)
    {
        var mainReport = await _attendanceRepository.GetAttendanceReportAsync(organizationId, startDate, endDate);
        var departmentStats = await _attendanceRepository.GetDepartmentAttendanceStatsAsync(organizationId, startDate, endDate,departmentId);
        var dailyStats = await _attendanceRepository.GetDailyAttendanceStatsAsync(organizationId, startDate, endDate);
        var attendanceRecords = await _attendanceRepository.GetAttendanceRecordsAsync(organizationId, startDate, endDate,departmentId,status,employeeId, pageNumber, pageSize);

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

    private async Task<AttendanceReportDto> GetManagerAttendanceReportAsync(Guid managerEmployeeId, Guid organizationId, DateTime startDate, DateTime endDate, Guid? departmentId, string? status, Guid? employeeId, int pageNumber = 1, int pageSize = 10)
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
                Records = new PagedResult<AttendanceDto>
                {
                    Data = new List<AttendanceDto>(),
                    TotalCount = 0,
                    Page = pageNumber,
                    PageSize = pageSize
                },
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

        // Get paginated attendance records (note: we get all pages to calculate stats correctly)
        // For accurate stats, we need all records, but for display we paginate
        // Get a large page size to get all team records for stats calculation
        var allAttendanceRecordsResult = await _attendanceRepository.GetAttendanceRecordsAsync(organizationId, startDate, endDate, departmentId, status, employeeId, 1, 10000);
        var teamAttendanceRecords = allAttendanceRecordsResult.Data
            .Where(r => teamEmployeeIds.Contains(r.EmployeeId))
            .ToList();

        // Get paginated records for display
        var paginatedTeamRecords = teamAttendanceRecords
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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
            Records = new PagedResult<AttendanceDto>
            {
                Data = paginatedTeamRecords,
                TotalCount = teamAttendanceRecords.Count,
                Page = pageNumber,
                PageSize = pageSize
            },
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

            // For export, get all records by using a large page size
            var attendanceRecords = await GetAttendanceReportAsync(
                organizationId, startDate, endDate, currentEmployeeId, departmentId, status, employeeId, 1, 10000);

            if (format.ToLower() == "csv")
            {
                return GenerateCsvExport(attendanceRecords.Records.Data, startDate, endDate);
            }
            else
            {
                return GenerateExcelExport(attendanceRecords.Records.Data, startDate, endDate);
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
            //headerStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;
            //headerStyle.FillPattern = NPOI.SS.UserModel.FillPattern.SolidForeground;
            headerStyle.FillPattern = NPOI.SS.UserModel.FillPattern.SolidForeground;
            headerStyle.FillForegroundColor = NPOI.SS.UserModel.IndexedColors.Grey25Percent.Index;
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
    public async Task<ServiceResponse<List<EmployeeTimesheetDto>>> GetTimesheetDetailsByIdAsync(
    Guid timesheetId, Guid organizationId, Guid currentEmployeeId)
    {
        var snapshot = await _attendanceRepository.GetSnapshotByIdAsync(timesheetId);
        if (snapshot == null)
            return new ServiceResponse<List<EmployeeTimesheetDto>> { Success = false, Message = "Snapshot not found" };

        var startDate = new DateTime(snapshot.Year, snapshot.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var today = DateTime.Today;

        var isAdminOrHR = await _attendanceRepository.IsAdminOrHRAsync(currentEmployeeId, organizationId);

        var records = await _attendanceRepository.GetConsolidatedAttendanceAsync(
            organizationId,
            startDate,
            endDate,
            isAdminOrHR ? null : currentEmployeeId);

        static string EffectiveStatus(AttendanceDto r, DateTime todayDate)
        {
            if (!string.IsNullOrWhiteSpace(r.Status))
                return r.Status.Trim().ToLowerInvariant();

            var dow = r.WorkDate.DayOfWeek;
            if (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday)
                return "weekend";

            if (r.WorkDate.Date > todayDate)
                return "no_record";

            return "absent";   
        }

        var groupedTimesheet = records
            .GroupBy(r => new {
                r.EmployeeId,
                r.EmployeeName,
                r.EmployeeCode,
                r.DepartmentName
            })
            .Select(g =>
            {
                var dailyRecords = g.Select(r => new DailyAttendanceRecord
                {
                    AttendanceId = r.AttendanceId,
                    Date = r.WorkDate,
                    CheckInTime = r.CheckInTime,
                    CheckOutTime = r.CheckOutTime,
                    Status = EffectiveStatus(r, today),
                    TotalHours = r.TotalHours,
                    IsFinalized = r.IsFinalized,
                    HasPendingRequest = r.HasPendingRequest,
                    HasDraftRequest = r.HasDraftRequest,
                    RequestedCheckIn = r.RequestedCheckIn,
                    RequestedCheckOut = r.RequestedCheckOut,
                    RequestedStatus = r.RequestedStatus,
                    IsManagerOverride = r.IsManagerOverride
                }).OrderBy(d => d.Date).ToList();

                int presentDays = g.Count(r => {
                    var s = EffectiveStatus(r, today);
                    return s == "present" || s == "late" || s == "half_day";
                });

                int absentDays = g.Count(r => EffectiveStatus(r, today) == "absent");

                int leaveDays = g.Count(r => {
                    var s = EffectiveStatus(r, today);
                    return s == "leave" || s == "on_leave";
                });

                int halfDays = g.Count(r => EffectiveStatus(r, today) == "half_day");
                int lateDays = g.Count(r => EffectiveStatus(r, today) == "late");
                
                int workedOrAbsentDays = g.Count(r => {
                    var s = EffectiveStatus(r, today);
                    return r.WorkDate.Date <= today
                        && s != "weekend"
                        && s != "no_record";
                });

                decimal attendancePct = workedOrAbsentDays > 0
                    ? Math.Round((decimal)presentDays / workedOrAbsentDays * 100m, 1)
                    : 0m;

                return new EmployeeTimesheetDto
                {
                    EmployeeId = g.Key.EmployeeId,
                    EmployeeName = g.Key.EmployeeName,
                    EmployeeCode = g.Key.EmployeeCode,
                    Department = g.Key.DepartmentName,
                    DailyRecords = dailyRecords,
                    IsFinalized = g.Any(r => r.IsFinalized)
                    && g.Where(r => {
                    var s = EffectiveStatus(r, today);
                    return !string.Equals(s, "weekend", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(s, "no_record", StringComparison.OrdinalIgnoreCase)
                    && r.WorkDate.Date <= today;
                     }).All(r => r.IsFinalized),
                    TotalHoursWorked = g.Sum(r => r.TotalHours),
                    PresentDays = presentDays,
                    AbsentDays = absentDays,
                    LeaveDays = leaveDays,
                    HalfDays = halfDays,
                    LateDays = lateDays,
                    AttendancePercentage = attendancePct
                };
            }).ToList();

        return new ServiceResponse<List<EmployeeTimesheetDto>> { Data = groupedTimesheet, Success = true };
    }
    public async Task<ServiceResponse<bool>> ProcessAttendanceUpdateRequestAsync(ProcessAttendanceRequestDto dto, Guid actionedBy, Guid organizationId)
    {
        try
        {
            var requestDetails = await _attendanceRepository.GetAttendanceUpdateRequestDetailsAsync(dto.RequestId);

            if (requestDetails == null)
            {
                return new ServiceResponse<bool> { Success = false, Message = "Update request not found." };
            }

            var result = await _attendanceRepository.ProcessUpdateRequestAsync(dto.RequestId, actionedBy, dto.IsApproved, dto.RejectionReason);

            if (result && requestDetails.EmployeeId.HasValue)
            {
                string statusText = dto.IsApproved ? "Approved" : "Rejected";

                await _notificationRepository.CreateNotificationAsync(organizationId, new NotificationCreateDto
                {
                    Receiveruserid = new List<Guid> { requestDetails.EmployeeId.Value },
                    Senderuserid = actionedBy,
                    Title = "Attendance Correction " + statusText,
                    Message = $"Your request for {requestDetails.RequestedStatus} on {requestDetails.WorkDate:yyyy-MM-dd} has been {statusText.ToLower()}.",                    Notificationtype = "Message",
                    moduletype = "attendance",
                    Redirecturl = "/attendance/dashboard",
                    RequestId = dto.RequestId,
                    organizationId = organizationId
                });
            }

            return new ServiceResponse<bool>
            {
                Success = result,
                Message = result ? "Request processed successfully." : "Failed to process request.",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing attendance update request {RequestId}", dto.RequestId);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred while processing the request." };
        }
    }
    public async Task<ServiceResponse<int>> FinalizeTimesheetBatchAsync(Guid timesheetId, Guid organizationId, Guid managerEmployeeId, Guid? employeeId = null)
    {
        try
        {
            int count;

            if (employeeId.HasValue && employeeId.Value != Guid.Empty)
            {
                var snapshot = await _attendanceRepository.GetSnapshotByIdAsync(timesheetId);
                if (snapshot == null)
                    return new ServiceResponse<int> { Success = false, Message = "Timesheet snapshot not found." };

                count = await _attendanceRepository.BatchFinalizeForEmployeeAsync(
                     employeeId.Value, timesheetId);

                return new ServiceResponse<int>
                {
                    Success = true,
                    Message = $"Successfully finalized {count} attendance record(s) for the employee.",
                    Data = count
                };
            }
            else
            {
                count = await _attendanceRepository.BatchFinalizeMonthAsync(timesheetId, organizationId, managerEmployeeId);

                return new ServiceResponse<int>
                {
                    Success = true,
                    Message = $"Successfully finalized {count} attendance records for this period.",
                    Data = count
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch finalization failed for Timesheet {TimesheetId} Employee {EmployeeId}", timesheetId, employeeId);
            return new ServiceResponse<int> { Success = false, Message = "An error occurred during batch finalization." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<FinalizedTimesheetRecordDto>>> GetApprovedTimesheetAsync(Guid timesheetId, Guid currentUserEmployeeId, string role)
    {
        Guid? filterEmployeeId = (role == "Employee") ? currentUserEmployeeId : null;

        var data = await _attendanceRepository.GetFinalizedRecordsAsync(timesheetId, filterEmployeeId);

        return new ServiceResponse<IEnumerable<FinalizedTimesheetRecordDto>> { Success = true, Data = data };
    }

    public async Task<ServiceResponse<bool>> CreateTimesheetSnapshotAsync(Guid organizationId, Guid currentUserEmployeeId, MonthlyTimesheetCreateDto dto)
    {
        // Snapshot logic
        var success = await _attendanceRepository.CreateMonthlySnapshotAsync(organizationId, currentUserEmployeeId, dto);
        return new ServiceResponse<bool>
        {
            Success = success,
            Message = success ? "Timesheet period created." : "Period already exists."
        };
    }
    public async Task<ServiceResponse<List<PendingAttendanceRequest>>> GetPendingRequestsAsync(Guid organizationId, Guid currentEmployeeId)
    {
        var isAdminOrHR = await _attendanceRepository.IsAdminOrHRAsync(currentEmployeeId, organizationId);

        var requests = await _attendanceRepository.GetAllPendingRequestsAsync(
            organizationId,
            isAdminOrHR ? null : currentEmployeeId // Managers only see their team
        );

        return new ServiceResponse<List<PendingAttendanceRequest>>
        {
            Data = requests.ToList(),
            Success = true
        };
    }
    public async Task<ServiceResponse<Guid>> RequestAttendanceUpdateAsync(Guid employeeId, Guid organizationId, AttendanceUpdateRequestDto dto)
    {
        try
        {
            var requestId = await _attendanceRepository.SubmitUpdateRequestAsync(employeeId, organizationId, dto);

            var managerId = await _notificationRepository.GetReciveridAsync(employeeId);
            await _notificationRepository.CreateNotificationAsync(organizationId, new NotificationCreateDto
            {
                Receiveruserid = new List<Guid> { managerId },
                Senderuserid = employeeId,
                Title = "Attendance Edit Request",
                Message = $"An employee has requested a change for {dto.RequestedStatus}",
                Notificationtype = "Request",
                moduletype = "attendance",
                Redirecturl = "/attendance/manual"
            });

            return new ServiceResponse<Guid> { Success = true, Data = requestId };
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505") 
        {
            return new ServiceResponse<Guid> { Success = false, Message = "A pending request already exists for this date." };
        }
        catch (Npgsql.PostgresException ex) when (ex.Message.Contains("SUBMISSION_LOCKED"))
        {
            return new ServiceResponse<Guid>
            {
                Success = false,
                Message = "Cannot add new corrections while your previous submission is awaiting manager review."
            };
        }
        catch (Npgsql.PostgresException ex) when (ex.Message.Contains("OVERRIDDEN_RECORD"))
        {
            return new ServiceResponse<Guid>
            {
                Success = false,
                Message = "This record has been overridden by a manager and cannot be corrected."
            };
        }
    }
    public async Task<ServiceResponse<List<MonthlyTimesheetSummary>>> GetMonthlyTimesheetSnapshotsAsync(
    Guid organizationId, DateTime? startDate, DateTime? endDate, Guid currentEmployeeId)
    {
        var isAdminOrHR = await _attendanceRepository.IsAdminOrHRAsync(currentEmployeeId, organizationId);
        var snapshots = (await _attendanceRepository.GetMonthlySnapshotsAsync(organizationId, startDate, endDate)).ToList();
        var today = DateTime.Today;

        foreach (var snapshot in snapshots)
        {
            var start = new DateTime(snapshot.Year, snapshot.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);

            var allRosterData = await _attendanceRepository.GetConsolidatedAttendanceAsync(
                organizationId, start, end,
                isAdminOrHR ? null : currentEmployeeId);

            if (!allRosterData.Any()) continue;

            static string EffectiveStatus(AttendanceDto r, DateTime todayDate)
            {
                if (!string.IsNullOrWhiteSpace(r.Status))
                    return r.Status.Trim().ToLowerInvariant();

                var dow = r.WorkDate.DayOfWeek;
                if (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday) return "weekend";
                if (r.WorkDate.Date > todayDate) return "no_record";
                return "absent";
            }

            snapshot.TotalPresentDays = allRosterData.Count(r => {
                var s = EffectiveStatus(r, today);
                return s == "present" || s == "late" || s == "half_day";
            });

            snapshot.TotalAbsentDays = allRosterData.Count(r =>
                EffectiveStatus(r, today) == "absent");

            snapshot.TotalLateDays = allRosterData.Count(r =>
                EffectiveStatus(r, today) == "late");

            snapshot.TotalHoursWorked = allRosterData.Sum(r => r.TotalHours);

            var elapsedWorkingDays = Enumerable
                .Range(0, (end - start).Days + 1)
                .Count(offset => {
                    var d = start.AddDays(offset);
                    return d.Date <= today
                        && d.DayOfWeek != DayOfWeek.Saturday
                        && d.DayOfWeek != DayOfWeek.Sunday;
                });

            int distinctEmployees = allRosterData
                .Select(r => r.EmployeeId)
                .Distinct()
                .Count();

            decimal totalExpectedManDays = distinctEmployees * elapsedWorkingDays;

            snapshot.AttendancePercentage = totalExpectedManDays > 0
                ? Math.Round((snapshot.TotalPresentDays / totalExpectedManDays) * 100m, 1)
                : 0m;

            if (!isAdminOrHR)
                snapshot.TotalEmployees = 1;
        }

        return new ServiceResponse<List<MonthlyTimesheetSummary>> { Data = snapshots, Success = true };
    }
    public async Task<ServiceResponse<IEnumerable<Attendance>>> GetManualAttendanceAsync(ManualAttendanceSearchDto searchDto, Guid currentUserEmployeeId, string currentUserRole, Guid organizationId)
    {
        try
        {
            Guid? managerId = null;
            Guid? employeeId = searchDto.EmployeeId;

            if (currentUserRole is "Manager" or "Super Admin" or "HR Manager")
            {
                managerId = currentUserEmployeeId;
            }
            else if (currentUserRole == "Employee")
            {
                // Check if employee has permission (from settings or manager)
                // For now, assuming if they reached here, they might have view access but only for themselves
                employeeId = currentUserEmployeeId;
            }

            var records = await _attendanceRepository.GetManualAttendanceRecordsAsync(organizationId, managerId, employeeId, searchDto.StartDate, searchDto.EndDate);
            
            return new ServiceResponse<IEnumerable<Attendance>>
            {
                Success = true,
                Data = records
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching manual attendance records");
            return new ServiceResponse<IEnumerable<Attendance>> { Success = false, Message = "Failed to fetch records" };
        }
    }

    public async Task<ServiceResponse<bool>> UpdateManualAttendanceAsync(ManualAttendanceUpdateDto updateDto, Guid currentUserEmployeeId, string currentUserRole, Guid organizationId, string ipAddress)
    {
        try
        {
            var hasPermission = await _attendanceRepository.HasPermissionToEditAsync(currentUserEmployeeId, updateDto.EmployeeId, organizationId);
            if (!hasPermission && !new[] { "Super Admin", "HR Manager" }.Contains(currentUserRole))
            {
                return new ServiceResponse<bool> { Success = false, Message = "Permission denied" };
            }
            var timeLimitDays = await _settingsRepository.GetManualEntryTimeLimitAsync(organizationId);
            if (!updateDto.AttendanceId.HasValue)
            {
                return new ServiceResponse<bool> { Success = false, Message = "AttendanceId is required" };
            }

            var attendanceRecord = await _attendanceRepository.GetAttendanceByIdAsync(updateDto.AttendanceId.Value);
            if (attendanceRecord == null)
            {
                 return new ServiceResponse<bool> { Success = false, Message = "Attendance record not found" };
            }

            if ((DateTime.UtcNow.Date - attendanceRecord.WorkDate.Date).TotalDays > timeLimitDays)
            {
                return new ServiceResponse<bool> { Success = false, Message = $"Edit period has expired ({timeLimitDays} days)" };
            }
            if (updateDto.CheckInTime.HasValue && updateDto.CheckOutTime.HasValue && updateDto.CheckOutTime <= updateDto.CheckInTime)
            {
                return new ServiceResponse<bool> { Success = false, Message = "Check-out time must be after check-in time" };
            }
            var oldValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                attendanceRecord.CheckInTime,
                attendanceRecord.CheckOutTime,
                attendanceRecord.Status,
                attendanceRecord.Notes
            });
            var newValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                updateDto.CheckInTime,
                updateDto.CheckOutTime,
                updateDto.Status,
                updateDto.Notes
            });
            var audit = new AttendanceAudit
            {
                AuditId = Guid.NewGuid(),
                AttendanceId = updateDto.AttendanceId,
                EmployeeId = updateDto.EmployeeId,
                ModifiedBy = currentUserEmployeeId,
                ModifiedAt = DateTime.UtcNow,
                ChangeType = "UPDATE",
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = ipAddress
            };
            attendanceRecord.CheckInTime = updateDto.CheckInTime;
            attendanceRecord.CheckOutTime = updateDto.CheckOutTime;
            attendanceRecord.Status = updateDto.Status;
            attendanceRecord.Notes = updateDto.Notes;

            var result = await _attendanceRepository.UpdateManualAttendanceAsync(attendanceRecord, audit);

            return new ServiceResponse<bool>
            {
                Success = result,
                Message = result ? "Attendance updated successfully" : "Failed to update attendance"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating manual attendance");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred during update" };
        }
    }
    public async Task<ServiceResponse<int>> SubmitTimesheetBatchAsync(Guid timesheetId, Guid employeeId, Guid organizationId)
    {
        var snapshot = await _attendanceRepository.GetSnapshotByIdAsync(timesheetId);
        if (snapshot == null) return new ServiceResponse<int> { Success = false, Message = "Timesheet not found" };

        // 1. Flip all 'draft' requests for this employee and period to 'pending'
        int updatedCount = await _attendanceRepository.FlipDraftsToPendingAsync(employeeId, snapshot.Month, snapshot.Year,timesheetId);

        if (updatedCount > 0)
        {
            // 2. Notify the manager ONLY NOW that a batch is ready for review
            var managerId = await _notificationRepository.GetReciveridAsync(employeeId);
            await _notificationRepository.CreateNotificationAsync(organizationId, new NotificationCreateDto
            {
                Receiveruserid = new List<Guid> { managerId },
                Senderuserid = employeeId,
                Title = "Timesheet Submitted",
                Message = $"Employee has submitted {updatedCount} changes for {snapshot.TimesheetName}.",
                Notificationtype = "Request",
                moduletype = "attendance",
                organizationId = organizationId
            });
        }

        return new ServiceResponse<int>
        {
            Success = true,
            Data = updatedCount,
            Message = $"Successfully submitted {updatedCount} changes for approval."
        };
    }
    public async Task<ServiceResponse<int>> ApproveAllPendingAsync(Guid timesheetId, Guid? targetEmployeeId, Guid managerId, Guid orgId)
    {
        int count = await _attendanceRepository.ApproveAllBatchAsync(timesheetId, targetEmployeeId, managerId, orgId);

        if (count == 0)
        {
            return new ServiceResponse<int>
            {
                Success = true,
                Data = 0,
                Message = "No pending requests were found to approve for this employee."
            };
        }

        return new ServiceResponse<int>
        {
            Success = true,
            Data = count,
            Message = $"Successfully approved {count} requests."
        };
    }
    public async Task<ServiceResponse<List<EmployeeSubmissionPackage>>> GetGroupedPendingRequestsAsync(Guid organizationId, Guid currentEmployeeId)
    {
        var isAdminOrHR = await _attendanceRepository.IsAdminOrHRAsync(currentEmployeeId, organizationId);
        var rawRequests = await _attendanceRepository.GetAllPendingRequestsAsync(
            organizationId,
            isAdminOrHR ? null : currentEmployeeId
        ) ?? Enumerable.Empty<PendingAttendanceRequest>(); 
        var grouped = rawRequests
            .GroupBy(r => new { r.EmployeeId, r.EmployeeName, r.EmployeeCode })
            .Select(g => new EmployeeSubmissionPackage
            {
                EmployeeId = g.Key.EmployeeId,
                EmployeeName = g.Key.EmployeeName,
                EmployeeCode = g.Key.EmployeeCode,
                SubmittedAt = g.Max(r => r.Submittedat),
                Corrections = g.OrderBy(r => r.WorkDate).ToList()
            }).ToList();

        return new ServiceResponse<List<EmployeeSubmissionPackage>>
        {
            Data = grouped,
            Success = true
        };
    }
    public async Task<ServiceResponse<List<EmployeeReviewPackage>>> GetManagerReviewDashboardAsync(
    Guid timesheetId,
    Guid organizationId,
    Guid currentUserId,
    Guid? targetEmployeeId = null)
    {

        bool isGlobalViewer = await _attendanceRepository.IsAdminOrHRAsync(currentUserId, organizationId);
        var today = DateTime.Today;

        var snapshot = await _attendanceRepository.GetSnapshotByIdAsync(timesheetId);
        if (snapshot == null) return new ServiceResponse<List<EmployeeReviewPackage>> { Success = false, Message = "Timesheet period not found" };

        var start = new DateTime(snapshot.Year, snapshot.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        Guid? filterManagerId = isGlobalViewer ? null : currentUserId;
        var rawLogs = await _attendanceRepository.GetConsolidatedAttendanceAsync(organizationId, start, end, targetEmployeeId, filterManagerId);
        var pendingReqs = await _attendanceRepository.GetAllReviewableRequestsAsync(organizationId, filterManagerId);
        var grouped = rawLogs.GroupBy(r => new { r.EmployeeId, r.EmployeeName, r.EmployeeCode, r.DepartmentName })
            .Select(g => new EmployeeReviewPackage
            {
                EmployeeId = g.Key.EmployeeId,
                EmployeeName = g.Key.EmployeeName,
                EmployeeCode = g.Key.EmployeeCode,
                Month = snapshot.Month,
                Year = snapshot.Year,
                TimesheetId = timesheetId,
                Department = g.Key.DepartmentName ?? "Unassigned",
                FullMonthRecords = Enumerable.Range(0, (end - start).Days + 1)
    .Select(offset => {
        var currentDay = start.AddDays(offset);
        var log = g.FirstOrDefault(x => x.WorkDate.Date == currentDay.Date);
        var req = pendingReqs?.FirstOrDefault(r => r.WorkDate.Date == currentDay.Date &&
                        r.EmployeeId == g.Key.EmployeeId);
        string calculatedStatus;
        if (log != null && !string.IsNullOrWhiteSpace(log.Status))
        {
            calculatedStatus = log.Status; 
        }
        else if (currentDay.Date > today)
        {
            calculatedStatus = "no_record"; 
        }
        else if (currentDay.DayOfWeek == DayOfWeek.Saturday || currentDay.DayOfWeek == DayOfWeek.Sunday)
        {
            calculatedStatus = "weekend"; 
        }
        else
        {
            calculatedStatus = "absent"; 
        }
        return new ReviewAttendanceRecord
        {
            AttendanceId = log?.AttendanceId,
            RequestId = req?.RequestId,
            Date = currentDay,
            CheckInTime = log?.CheckInTime,
            CheckOutTime = log?.CheckOutTime,
            Status = calculatedStatus,
            TotalHours = log?.TotalHours ?? 0,
            RequestStatus = req?.Status ?? "none",
            RequestedCheckIn = req?.RequestedCheckIn,
            RequestedCheckOut = req?.RequestedCheckOut,
            RequestedStatus = req?.RequestedStatus,
            ReasonForEdit = req?.ReasonForEdit,
            IsFinalized = log?.IsFinalized ?? false,
            HasPendingRequest = (req != null && req.Status == "pending")
                    || (log?.HasPendingRequest ?? false),
            HasDraftRequest = (req != null && req.Status == "Draft")
                    || (log?.HasDraftRequest ?? false),
            HasApprovedRequest = (req != null && req.Status == "approved") 
        || (log?.HasApprovedRequest ?? false),
            IsManagerOverride = log?.IsManagerOverride ?? false
        };
    }).ToList()
            }).ToList();
        foreach (var pkg in grouped)
        {
            var records = pkg.FullMonthRecords;

            var workingDayRecords = records.Where(r => {
                var s = (r.Status ?? "").ToLowerInvariant().Replace("_", "");
                return s != "weekend" && s != "norecord";
            }).ToList();

            var elapsedWorkingRecords = workingDayRecords
                .Where(r => r.Date.Date <= DateTime.Today)
                .ToList();

            pkg.TotalRecords = elapsedWorkingRecords.Count;

            var finalizableRecords = elapsedWorkingRecords;

            pkg.FinalizedCount = finalizableRecords.Count(r => r.IsFinalized);

            pkg.PendingRequestCount = records.Count(r => r.HasPendingRequest);

            pkg.ApprovedCount = records.Count(r =>
                string.Equals(r.RequestStatus, "approved", StringComparison.OrdinalIgnoreCase)
                || (r.HasApprovedRequest
                    && !string.Equals(r.RequestStatus, "pending", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(r.RequestStatus, "rejected", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(r.RequestStatus, "none", StringComparison.OrdinalIgnoreCase)));

            pkg.RejectedCount = records.Count(r =>
                string.Equals(r.RequestStatus, "rejected", StringComparison.OrdinalIgnoreCase));

            pkg.IsFinalized = finalizableRecords.Count > 0
                           && finalizableRecords.All(r => r.IsFinalized);
        }

        return new ServiceResponse<List<EmployeeReviewPackage>> { Data = grouped, Success = true };
    }
    public async Task<OrgSubmissionProgressDto> GetOrgSubmissionProgressAsync(
    Guid organizationId, int month, int year)
    {
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be 1–12.");
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Year is out of range.");

        return await _attendanceRepository.GetOrgSubmissionProgressAsync(
            organizationId, month, year);
    }
    public async Task<bool> AdminOverrideAttendanceAsync(ManualAttendanceUpdateDto dto, Guid managerId, Guid orgId)
    {
        _logger.LogInformation("Admin override initiated by {ManagerId} for Employee {EmployeeId} on {Date}",
            managerId, dto.EmployeeId, dto.WorkDate);
        if (dto.TimesheetId != Guid.Empty)
        {
            var isPackageFinalized = await _attendanceRepository.IsEmployeePackageFinalizedAsync(
                dto.TimesheetId, dto.EmployeeId);
            if (isPackageFinalized)
                throw new InvalidOperationException(
                    "Cannot override: this employee's timesheet has been fully finalized for payroll.");
        }

        var result = await _attendanceRepository.AdminFinalizeRecordAsync(dto, managerId, orgId);

        if (result)
        {
            await _notificationRepository.CreateNotificationAsync(orgId, new NotificationCreateDto
            {
                Receiveruserid = new List<Guid> { dto.EmployeeId },
                Senderuserid = managerId,
                Title = "Attendance Override",
                Message = $"A manager has overridden your attendance record for {dto.WorkDate:yyyy-MM-dd}. The record has been updated but is not yet finalized for payroll.",
                Notificationtype = "Message",
                moduletype = "attendance",
                Redirecturl = "/attendance/dashboard",
                organizationId = orgId
            });
        }

        return result;
    }
}
