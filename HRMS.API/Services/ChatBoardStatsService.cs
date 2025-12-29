using DocumentFormat.OpenXml.Bibliography;
using HRMS.AI.Models;
using HRMS.AI.Models.Enums;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Microsoft.Extensions.Logging;
using NPOI.HSSF.Record.Aggregates;
using System;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Threading.Tasks;

namespace HRMS.API.Services
{
    public class ChatBoardStatsService : IChatBoardStatsService
    {
        private readonly ILeaveRepository _leaveRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IPerformanceRepository _performanceRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<ChatBoardStatsService> _logger;

        public ChatBoardStatsService(ILeaveRepository leaveRepository, IAttendanceRepository attendanceRepository, IPerformanceRepository performanceRepository, IEmployeeRepository employeeRepository, ILogger<ChatBoardStatsService> logger)
        {
            _leaveRepository = leaveRepository;
            _attendanceRepository = attendanceRepository;
            _performanceRepository = performanceRepository;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        public async Task<ChatResponseDto> HandleNonGuidanceIntentAsync(ChatIntent intent, string module, Guid employeeId, Guid currentUserId, Guid organizationId)
        {
            switch (intent)
            {
                case ChatIntent.GetPerformanceSummary:
                    return await HandleGetPerformanceSummary(employeeId, organizationId);
                case ChatIntent.GetAttendance:
                    return await HandleGetAttendance(employeeId, organizationId);
                case ChatIntent.CheckMyShift:
                    return await HandleCheckMyShift(currentUserId, organizationId);
                case ChatIntent.GetLeaveBalance:
                    return await HandleGetLeaveBalance(employeeId, organizationId);
                case ChatIntent.ApplyForLeave:
                    return await HandleApplyForLeave(employeeId, organizationId);
                case ChatIntent.GetPayslip:
                    return await HandleGetPayslip(employeeId, organizationId);
                case ChatIntent.EmployeeInfo:
                    return await HandleGetEmployeeInfo(employeeId, organizationId);
                case ChatIntent.Unknown:
                    _logger.LogWarning("Received Unknown intent for employee {EmployeeId}", employeeId);
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.Unknown,
                        ResponseText = "I am an HR assistant and can only help with HR-related queries.\r\nPlease ask questions related to attendance, leave management,payroll, performance, or HR policies.",
                        Module = module
                    };
                default:
                    // Return the intent name for any other/unhandled intents
                    return new ChatResponseDto
                    {
                        Intent = intent,
                        ResponseText = intent.ToString(),
                        Module = module
                    };
            }
        }
        private async Task<ChatResponseDto> HandleGetEmployeeInfo(Guid? employeeId, Guid organizationId)
        {
            _logger.LogInformation("Fetching reporting manager for employee {EmployeeId}", employeeId);

            try
            {
                if (employeeId == null)
                {
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.EmployeeInfo,
                        ResponseText = "Employee information is not available.",
                        Module = "EmployeeManagement"
                    };
                }

                var employees = await _employeeRepository.GetEmployeesAsync(organizationId);
                var currentEmployee = employees?.Data?
                    .FirstOrDefault(x => x.EmployeeId == employeeId);

                if (currentEmployee == null)
                {
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.EmployeeInfo,
                        ResponseText = "Employee record not found.",
                        Module = "EmployeeManagement"
                    };
                }
                string currentEmployeeJson = JsonSerializer.Serialize(
                    currentEmployee,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }
                );
                return new ChatResponseDto
                {
                    Intent = ChatIntent.EmployeeInfo,
                    ResponseText = currentEmployeeJson,
                    Module = "EmployeeManagement"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch reporting manager for employee {EmployeeId}", employeeId);
                return new ChatResponseDto
                {
                    Intent = ChatIntent.EmployeeInfo,
                    ResponseText = "Error fetching reporting manager information.",
                    Module = "EmployeeManagement"
                };
            }
        }

        private async Task<ChatResponseDto> HandleGetPerformanceSummary(Guid employeeId, Guid organizationId)
        {
            _logger.LogInformation("Fetching performance summary for employee {EmployeeId}", employeeId);

            try
            {
                var cycle = await _performanceRepository.GetActiveAppraisalCycleAsync(organizationId);
                if (cycle == null)
                {
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.GetPerformanceSummary,
                        ResponseText = "There is no active performance cycle at the moment.",
                        Module = "PerformanceManagement"
                    };
                }

                var performanceSummary =
                    await _performanceRepository.GetEmployeeAppraisalsAsync(cycle.CycleId, employeeId);

                if (performanceSummary == null || !performanceSummary.Any())
                {
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.GetPerformanceSummary,
                        ResponseText = $"No performance record found for the {cycle.CycleName} cycle.",
                        Module = "PerformanceManagement"
                    };
                }

                var appraisal = performanceSummary.First();

                // Overall rating
                string overallRating = appraisal.OverallRating.HasValue && appraisal.OverallRating.Value > 0
          ? appraisal.OverallRating.Value.ToString("0.0")
          : "N/A";

                // KRA rating (average or single value)
                string kraRating = "N/A";
                if (appraisal.KraRatings != null && appraisal.KraRatings.Count > 0)
                {
                    var kraValues = appraisal.KraRatings
                        .Select(k => Convert.ToDecimal(k.Value.ToString()))
                        .ToList();

                    kraRating = kraValues.Average().ToString("0.0");
                }

                string reviewer = !string.IsNullOrWhiteSpace(appraisal.ReviewerName)
                    ? appraisal.ReviewerName
                    : "your manager";

                string reviewType = appraisal.ReviewType?.ToLower() == "manager"
                    ? "manager"
                    : "reviewer";

                string message =
                    $"Your performance summary for the {appraisal.CycleName} cycle is now available. " +
                    $"You received an overall rating of {overallRating} and a KRA rating of {kraRating}. " +
                    $"This evaluation was submitted by your {reviewType}, {reviewer}.";

                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetPerformanceSummary,
                    ResponseText = message,
                    Module = "PerformanceManagement"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch performance summary for employee {EmployeeId}", employeeId);
                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetPerformanceSummary,
                    ResponseText = "Error fetching performance summary.",
                    Module = "PerformanceManagement"
                };
            }
        }

        private async Task<ChatResponseDto> HandleGetAttendance(Guid employeeId, Guid organizationId)
        {
            _logger.LogInformation("Fetching attendance for employee {EmployeeId}", employeeId);
            try
            {
                DateTime startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                DateTime endDate = startDate.AddMonths(1).AddDays(-1);
                var attendanceList = await _attendanceRepository.GetEmployeeAttendanceAsync(employeeId, startDate, endDate);

                // Calculate total working days in the current month (exclude Saturday and Sunday)
                int totalWorkingDays = 0;
                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                        totalWorkingDays++;
                }

                // If no attendance records found
                if (attendanceList == null || attendanceList.Count == 0)
                {
                    var noPresentMsg = $"For {startDate:MMMM yyyy}, there are {totalWorkingDays} scheduled working days. You have no recorded present days for this month; all {totalWorkingDays} day(s) are marked absent. Attendance rate: 0%.";
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.GetAttendance,
                        ResponseText = noPresentMsg,
                        Module = "Attendance"
                    };
                }

                // Count distinct present dates (some employees may have multiple sessions per day)
                var presentDays = attendanceList
                    .Where(a => a.CheckInTime.HasValue)
                    .Select(a => a.CheckInTime.Value.Date)
                    .Distinct()
                    .Count();

                // Ensure presentDays does not exceed totalWorkingDays
                if (presentDays > totalWorkingDays)
                    presentDays = totalWorkingDays;

                int absentDays = totalWorkingDays - presentDays;
                if (absentDays < 0) absentDays = 0;

                double attendanceRate = totalWorkingDays > 0 ? (double)presentDays * 100.0 / totalWorkingDays : 0.0;

                var message = $"For {startDate:MMMM yyyy} there are {totalWorkingDays} scheduled working days. You were present on {presentDays} day(s) and absent on {absentDays} day(s). Attendance rate: {attendanceRate:0.##}%.";

                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetAttendance,
                    ResponseText = message,
                    Module = "Attendance"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch attendance for employee {EmployeeId}", employeeId);
                return new ChatResponseDto { Intent = ChatIntent.GetAttendance, ResponseText = "Error fetching attendance information.", Module = "Attendance" };
            }
        }

        private async Task<ChatResponseDto> HandleCheckMyShift(Guid currentUserId, Guid organizationId)
        {
            _logger.LogInformation("Fetching shift for employee {EmployeeId}", currentUserId);

            try
            {
                var shift = await _attendanceRepository.GetShiftByEmployeeIdAsync(currentUserId);
                if (shift == null)
                {
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.CheckMyShift,
                        ResponseText = "No shift assigned.",
                        Module = "ShiftManagement"
                    };
                }

                // Format start and end times
                string start = shift.StartTime.ToString(@"hh\:mm");
                string end = shift.EndTime.ToString(@"hh\:mm");

                // Map day numbers to names. Assume 1 = Monday, 7 = Sunday.
                string[] dayNames = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
                string daysText = "";
                if (shift.DaysOfWeek != null && shift.DaysOfWeek.Length > 0)
                {
                    var names = shift.DaysOfWeek
                        .Where(i => i >= 1 && i <= 7)
                        .Select(i => dayNames[i - 1])
                        .ToArray();

                    daysText = names.Length > 0 ? string.Join(", ", names) : string.Empty;
                }

                // Build a professional message for the assigned shift
                string message;

                // Prepare compact representation for days: use range if consecutive (e.g., Monday to Friday)
                string daysRepresentation = daysText;
                if (!string.IsNullOrWhiteSpace(daysText) && shift.DaysOfWeek != null && shift.DaysOfWeek.Length > 0)
                {
                    var idx = shift.DaysOfWeek.Where(i => i >= 1 && i <= 7).OrderBy(i => i).ToArray();
                    if (idx.Length > 1)
                    {
                        var first = idx.First();
                        var last = idx.Last();
                        if (last - first == idx.Length - 1)
                        {
                            // consecutive range
                            daysRepresentation = $"{dayNames[first - 1]} to {dayNames[last - 1]}";
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(daysRepresentation))
                {
                    message = $"You are not assigned to any scheduled working days for this shift (Shift: {shift.ShiftName}).";
                }
                else
                {
                    message = $"You are assigned to the '{shift.ShiftName}' shift. Working hours are {start} – {end}. Scheduled working days: {daysRepresentation}.";
                }

                return new ChatResponseDto
                {
                    Intent = ChatIntent.CheckMyShift,
                    ResponseText = message,
                    Module = "ShiftManagement"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch shift for employee {EmployeeId}", currentUserId);
                return new ChatResponseDto { Intent = ChatIntent.CheckMyShift, ResponseText = "Error fetching shift information.", Module = "ShiftManagement" };
            }
        }

        private async Task<ChatResponseDto> HandleGetLeaveBalance(Guid employeeId, Guid organizationId)
        {
            _logger.LogInformation("Fetching leave balance for employee {EmployeeId}", employeeId);
            try
            {
                var balanceYear =  DateTime.Now.Year;
                var LeaveBallance = await _leaveRepository.GetLeaveBalanceAsync(employeeId, balanceYear);

                // If there are any leave balance entries, build a message that includes all of them
                if (LeaveBallance != null && LeaveBallance.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("Here are your leave balances:");
                    int index = 1;

                    foreach (var item in LeaveBallance)
                    {
                        // Format decimals with up to 2 decimal places and remove trailing zeros
                        string fmtTotal = item.TotalDays % 1 == 0 ? ((int)item.TotalDays).ToString() : item.TotalDays.ToString("0.##");
                        string fmtUsed = item.UsedDays % 1 == 0 ? ((int)item.UsedDays).ToString() : item.UsedDays.ToString("0.##");
                        string fmtRemaining = item.RemainingDays % 1 == 0 ? ((int)item.RemainingDays).ToString() : item.RemainingDays.ToString("0.##");

                        sb.Append($"{index}. {item.LeaveTypeName}: Total {fmtTotal} day");
                        if (decimal.Parse(fmtTotal) != 1) sb.Append("s");
                        sb.Append($", Used {fmtUsed} day");
                        if (decimal.Parse(fmtUsed) != 1) sb.Append("s");
                        sb.Append($", Remaining {fmtRemaining} day");
                        if (decimal.Parse(fmtRemaining) != 1) sb.Append("s");

                        sb.AppendLine();
                        index++;
                    }

                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.GetLeaveBalance,
                        ResponseText = sb.ToString().TrimEnd(),
                        Module = "Leave"
                    };
                }

                return new ChatResponseDto { Intent = ChatIntent.GetLeaveBalance, ResponseText = "No leave balance information found.", Module = "Leave" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch leave balance for employee {EmployeeId}", employeeId);
                return new ChatResponseDto { Intent = ChatIntent.GetLeaveBalance, ResponseText = "Error fetching leave balance.", Module = "Leave" };
            }
        }

        private  async Task<ChatResponseDto> HandleApplyForLeave(Guid employeeId, Guid organizationId)
        {
            _logger.LogInformation(
                "Processing apply for leave request for employee {EmployeeId}",
                employeeId
            );
            
            var LeaveBallance = await HandleGetLeaveBalance (employeeId, organizationId);

            var message = LeaveBallance.ResponseText + "\n\nTo proceed with your leave application, please provide the following details: \n1. Leave Type \n2. Leave Start Date \n3. Leave End Date \n4. Reason for Leave";

            return new ChatResponseDto
            {
                Intent = ChatIntent.ApplyForLeave,
                ResponseText = message,
                Module = "ApplyForLeave"
            };
        }

        private Task<ChatResponseDto> HandleGetPayslip(Guid employeeId, Guid organizationId)
        {
            var message = "Employee Payslip process is in progress.Currently only super admin has access payroll related things";

            return Task.FromResult(new ChatResponseDto
            {
                Intent = ChatIntent.GetAttendance,
                ResponseText = message,
                Module = "Attendance"
            });
        }
    }
}
