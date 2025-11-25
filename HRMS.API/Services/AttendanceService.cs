using AutoMapper;
using DocumentFormat.OpenXml.Office2021.Excel.RichDataWebImage;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
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
        var EMP_Id = await _attendanceRepository.GetEmpId(employeeId);
        var result = await _attendanceRepository.ClockInOutAsync(EMP_Id, "in", DateTime.UtcNow, request.Location);
        return result;
    }

    public async Task<bool> ClockOutAsync(Guid employeeId, ClockInOutRequest request)
    {
        var EMP_Id = await _attendanceRepository.GetEmpId(employeeId);
        return await _attendanceRepository.ClockInOutAsync(EMP_Id, "out", DateTime.UtcNow, request.Location);
    }

    public async Task<AttendanceDto?> GetTodayAttendanceAsync(Guid employeeId)
    {
        var EMP_Id = await _attendanceRepository.GetEmpId(employeeId);
        var attendance = await _attendanceRepository.GetTodayAttendanceAsync(EMP_Id);
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
        var EMP_Id = await _attendanceRepository.GetEmpId(employeeId);

        return await _attendanceRepository.GetAttendanceSummaryAsync(EMP_Id, startDate, endDate);
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
        var EMP_Id = await _attendanceRepository.GetEmpId(employeeId);

        var attendance = await _attendanceRepository.GetEmployeeAttendanceAsync(EMP_Id, startDate, endDate);
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

    public async Task<DailyAttendanceStatsDto> GetDailyAttendanceStatsAsync(Guid organizationId, DateTime date)
    {
        _logger.LogInformation("Getting daily attendance stats for organization: {OrganizationId} on {Date}",
            organizationId, date);

        var result = await _attendanceRepository.GetDailyAttendanceStatsAsync(organizationId, date);


        return result;


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
        var reciverid = await _notificationRepository.GetReciveridAsync(employee.EmployeeId);

        if (result)

        {
            var notification = new NotificationCreateDto
            {
                Receiveruserid=reciverid,
                Senderuserid = employee.EmployeeId,
                Ttitle = "Swap Request",
                Message = $"Employee {employee.FirstName } Department {employee.DepartmentName}  requested shift swap.",
                Notificationtype = "Request ShiftSwap",
                Createdby = requestDto.EmployeeId,
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


    public async Task<List<PendingShiftSwapDto>> GetPendingShiftSwapRequestsAsync(Guid organizationId)
    {
        return await _attendanceRepository.GetPendingShiftSwapRequestsAsync(organizationId);
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






    public async Task<AttendanceReportDto> GetAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var mainReport = await _attendanceRepository.GetAttendanceReportAsync(organizationId, startDate, endDate);

            var departmentStats = await _attendanceRepository.GetDepartmentAttendanceStatsAsync(organizationId, startDate, endDate);
            var dailyStats = await _attendanceRepository.GetDailyAttendanceStatsAsync(organizationId, startDate, endDate);
            var attendanceRecords = await _attendanceRepository.GetAttendanceRecordsAsync(organizationId, startDate, endDate);

            //var totalWorkDays = (endDate - startDate).Days + 1;
            var totalWorkDays = Enumerable.Range(0, (endDate - startDate).Days + 1)
    .Select(offset => startDate.AddDays(offset))
    .Count(date => date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday);

            var totalPossibleDays = mainReport.TotalEmployees * totalWorkDays;
            var calculatedAbsentDays = totalPossibleDays - mainReport.TotalPresentDays;

            var completeReport = new AttendanceReportDto
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

            return completeReport;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating complete attendance report");
            throw;
        }
    }

    public async Task<byte[]> ExportAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate, string format, Guid? employeeId)
    {
        try
        {
            _logger.LogInformation("Exporting attendance report for organization: {OrganizationId} in format: {Format}",
                organizationId, format);

            var attendanceRecords = await GetAttendanceReportAsync(
                organizationId, startDate, endDate);

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
}
