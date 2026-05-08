using DocumentFormat.OpenXml.Bibliography;
using HRMS.AI.Models;
using HRMS.AI.Models.Enums;
using HRMS.API.Models.DTOs;
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
        private readonly IHolidayService _holidayService;
        private readonly IExpenseRepository _expenseRepository;
        private readonly IJobsRepository _jobsRepository;
        private readonly INewsRepository _newsRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPayRollRepository _payrollRepository;
        private readonly IAttendanceService _attendanceService;
        private readonly ILogger<ChatBoardStatsService> _logger;

        public ChatBoardStatsService(
            ILeaveRepository leaveRepository,
            IAttendanceRepository attendanceRepository,
            IPerformanceRepository performanceRepository,
            IEmployeeRepository employeeRepository,
            IHolidayService holidayService,
            IExpenseRepository expenseRepository,
            IJobsRepository jobsRepository,
            INewsRepository newsRepository,
            IAssetRepository assetRepository,
            IPaymentRepository paymentRepository,
            IPayRollRepository payrollRepository,
            IAttendanceService attendanceService,
            ILogger<ChatBoardStatsService> logger)
        {
            _leaveRepository = leaveRepository;
            _attendanceRepository = attendanceRepository;
            _performanceRepository = performanceRepository;
            _employeeRepository = employeeRepository;
            _holidayService = holidayService;
            _expenseRepository = expenseRepository;
            _jobsRepository = jobsRepository;
            _newsRepository = newsRepository;
            _assetRepository = assetRepository;
            _paymentRepository = paymentRepository;
            _payrollRepository = payrollRepository;
            _attendanceService = attendanceService;
            _logger = logger;
        }

        public async Task<ChatResponseDto> HandleNonGuidanceIntentAsync(
            ChatIntent intent,
            string module,
            Guid employeeId,
            Guid currentUserId,
            Guid organizationId,
            string ipAddress,
            string userMessage,
            string baseUrl)
        {
            switch (intent)
            {
                case ChatIntent.GetHolidaySummary:
                    return await HandleGetHolidaySummary(organizationId, userMessage, baseUrl);
                case ChatIntent.GetMyRestrictedHolidays:
                    return await HandleGetMyRestrictedHolidays(employeeId, organizationId, baseUrl);
                case ChatIntent.GetTimesheetStatus:
                    return await HandleGetTimesheetStatus(employeeId, organizationId, userMessage, baseUrl);
                case ChatIntent.GetExpenseStatus:
                    return await HandleGetExpenseStatus(employeeId, organizationId, userMessage, baseUrl);
                case ChatIntent.GetJobsStatus:
                    return await HandleGetJobsStatus(employeeId, organizationId, baseUrl);
                case ChatIntent.GetNewsUpdates:
                    return await HandleGetNewsUpdates(employeeId, organizationId, userMessage, baseUrl);
                case ChatIntent.GetMyAssets:
                    return await HandleGetMyAssets(employeeId, organizationId, userMessage, baseUrl);
                case ChatIntent.GetSubscriptionStatus:
                    return await HandleGetSubscriptionStatus(organizationId, userMessage, baseUrl);
                case ChatIntent.GetPerformanceSummary:
                    return await HandleGetPerformanceSummary(employeeId, organizationId);
                case ChatIntent.GetAttendance:
                    return await HandleGetAttendance(employeeId, organizationId);
                case ChatIntent.CheckMyShift:
                    return await HandleCheckMyShift(employeeId, organizationId);
                case ChatIntent.GetLeaveBalance:
                    return await HandleGetLeaveBalance(employeeId, organizationId);
                case ChatIntent.ApplyForLeave:
                    return await HandleApplyForLeave(employeeId, organizationId);
                case ChatIntent.GetPayslip:
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.Unknown,
                        ResponseText = "I am an HR assistant and can only help with HR-related queries.\r\nPlease ask about attendance, leave, holidays, timesheets, payroll, expenses, jobs, assets, subscription, performance, or HR policies.",
                        Module = module
                    };
                case ChatIntent.EmployeeInfo:
                    return await HandleGetEmployeeInfo(employeeId, organizationId);
                case ChatIntent.GetMySkills:
                    return await HandleGetMySkills(employeeId, organizationId);
                case ChatIntent.ClockIn:
                    return await HandleClockIn(employeeId, organizationId,ipAddress);
                case ChatIntent.ClockOut:
                    return await HandleClockOut(employeeId, organizationId, ipAddress);
                case ChatIntent.Unknown:
                    _logger.LogWarning("Received Unknown intent for employee {EmployeeId}", employeeId);
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.Unknown,
                        ResponseText = "I am an HR assistant and can only help with HR-related queries.\r\nPlease ask about attendance, leave, holidays, timesheets, payroll, expenses, jobs, assets, subscription, performance, or HR policies.",
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

        private static bool IsYesNoQuestion(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            var m = message.Trim().ToLowerInvariant();
            return m.StartsWith("is ")
                || m.StartsWith("are ")
                || m.StartsWith("any ")
                || m.StartsWith("do i ")
                || m.StartsWith("does ")
                || m.StartsWith("can i ");
        }

        private static string BuildVerifyLink(string baseUrl, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return baseUrl;
            var normalizedBase = (baseUrl ?? string.Empty).TrimEnd('/');
            var normalizedPath = path.StartsWith("/") ? path : "/" + path;
            return string.IsNullOrWhiteSpace(normalizedBase) ? normalizedPath : normalizedBase + normalizedPath;
        }

        private async Task<ChatResponseDto> HandleGetExpenseStatus(Guid employeeId, Guid organizationId, string userMessage, string baseUrl)
        {
            try
            {
                var verifyUrl = BuildVerifyLink(baseUrl, "/expense/claims");
                var expenses = (await _expenseRepository.GetExpensesAsync(organizationId, employeeId)).ToList();
                if (!expenses.Any())
                {
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.GetExpenseStatus,
                        ResponseText = $"No, you do not have any expense claims yet. Verify here: {verifyUrl}",
                        Module = "ExpensesManagement"
                    };
                }

                var pending = expenses.Count(x => string.Equals(x.Status, "pending", StringComparison.OrdinalIgnoreCase));
                var approved = expenses.Count(x => string.Equals(x.Status, "approved", StringComparison.OrdinalIgnoreCase));
                var rejected = expenses.Count(x => string.Equals(x.Status, "rejected", StringComparison.OrdinalIgnoreCase));
                var latest = expenses
                    .OrderByDescending(x => x.CreatedAt ?? x.UpdatedAt ?? x.BillDate)
                    .Take(3)
                    .Select((x, i) => $"{i + 1}. {x.Title} - {x.Status} ({x.Amount:0.##})");

                var message = IsYesNoQuestion(userMessage)
                    ? $"Yes, you have {expenses.Count} expense claim(s): Pending {pending}, Approved {approved}, Rejected {rejected}. Verify here: {verifyUrl}"
                    : $"Your expense summary: Total {expenses.Count}, Pending {pending}, Approved {approved}, Rejected {rejected}.\nRecent claims:\n{string.Join("\n", latest)}\nVerify here: {verifyUrl}";

                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetExpenseStatus,
                    ResponseText = message,
                    Module = "ExpensesManagement"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch expense status for employee {EmployeeId}", employeeId);
                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetExpenseStatus,
                    ResponseText = "Error fetching expense status.",
                    Module = "ExpensesManagement"
                };
            }
        }

        private async Task<ChatResponseDto> HandleGetJobsStatus(Guid employeeId, Guid organizationId, string baseUrl)
        {
            try
            {
            var verifyUrl = BuildVerifyLink(baseUrl, "/jobs/applied");
                var openings = (await _jobsRepository.GetAllJobOpeningsAsync(organizationId)).ToList();
                var myApplications = (await _jobsRepository.GetJobApplicationsByEmployeeIdAsync(employeeId)).ToList();

                var openPositions = openings.Count(x => string.Equals(x.Status, "open", StringComparison.OrdinalIgnoreCase));
                var activeApplications = myApplications.Count(x =>
                    !string.Equals(x.Status, "rejected", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(x.Status, "closed", StringComparison.OrdinalIgnoreCase));

                var latestApplications = myApplications
                    .OrderByDescending(x => x.CreatedDate ?? x.UpdatedDate)
                    .Take(3)
                    .Select((x, i) => $"{i + 1}. {x.JobRoleName ?? "Job"} - {x.Status}");

                var applicationsSection = latestApplications.Any()
                    ? "\nRecent applications:\n" + string.Join("\n", latestApplications)
                    : "";

                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetJobsStatus,
                    ResponseText =
                        $"Jobs summary: {openPositions} open positions in your organization. You have {myApplications.Count} application(s), with {activeApplications} active." + applicationsSection + $"\nVerify here: {verifyUrl}",
                    Module = "JobsManagement"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch jobs status for employee {EmployeeId}", employeeId);
                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetJobsStatus,
                    ResponseText = "Error fetching jobs status.",
                    Module = "JobsManagement"
                };
            }
        }

        private async Task<ChatResponseDto> HandleGetNewsUpdates(Guid employeeId, Guid organizationId, string userMessage, string baseUrl)
        {
            try
            {
            var verifyUrl = BuildVerifyLink(baseUrl, "/news/dashboard");
                var search = new NewsDTOs.NewsSearchViewModel
                {
                    Page = 1,
                    PageSize = 5,
                    SortBy = "createdat",
                    SortOrder = "desc"
                };

                var page = await _newsRepository.GetAllEmployeeAsync(employeeId, organizationId, search);
                var news = page.Data?.Take(5).ToList() ?? new List<NewsDTOs.NewsDto>();

                if (!news.Any())
                {
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.GetNewsUpdates,
                        ResponseText = $"No, there are no recent company news updates right now. Verify here: {verifyUrl}",
                        Module = "NewsManagement"
                    };
                }

                var lines = news.Select((x, i) =>
                    $"{i + 1}. {x.Title} ({x.Status})" + (x.PublishedAt.HasValue ? $" - {x.PublishedAt.Value:dd MMM yyyy}" : string.Empty));

                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetNewsUpdates,
                    ResponseText = (IsYesNoQuestion(userMessage)
                        ? "Yes, there are recent news updates:\n"
                        : "Here are the latest news updates:\n") + string.Join("\n", lines) + $"\nVerify here: {verifyUrl}",
                    Module = "NewsManagement"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch news updates for employee {EmployeeId}", employeeId);
                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetNewsUpdates,
                    ResponseText = "Error fetching latest news.",
                    Module = "NewsManagement"
                };
            }
        }

        private async Task<ChatResponseDto> HandleGetMyAssets(Guid employeeId, Guid organizationId, string userMessage, string baseUrl)
        {
            try
            {
                var verifyUrl = BuildVerifyLink(baseUrl, "/assets/create");
                var assignments = (await _assetRepository.GetAssetAssignmentHistoryAsync(organizationId, null, employeeId)).ToList();
                if (!assignments.Any())
                {
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.GetMyAssets,
                        ResponseText = $"No, you do not have any assigned assets right now. Verify here: {verifyUrl}",
                        Module = "AssetsManagement"
                    };
                }

                var active = assignments.Count(x => string.Equals(x.Status, "Active", StringComparison.OrdinalIgnoreCase));
                var returned = assignments.Count(x => x.ReturnedAt.HasValue || string.Equals(x.Status, "Returned", StringComparison.OrdinalIgnoreCase));
                var latest = assignments
                    .OrderByDescending(x => x.AssignedAt)
                    .Take(3)
                    .Select((x, i) => $"{i + 1}. {x.AssetName} ({x.AssetTag}) - {x.Status}");

                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetMyAssets,
                    ResponseText =
                        (IsYesNoQuestion(userMessage)
                            ? $"Yes, you have asset assignments. Total {assignments.Count}, Active {active}, Returned {returned}.\n"
                            : $"Your asset summary: Total assignments {assignments.Count}, Active {active}, Returned {returned}.\n")
                        + $"Recent assets:\n{string.Join("\n", latest)}\nVerify here: {verifyUrl}",
                    Module = "AssetsManagement"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch assets for employee {EmployeeId}", employeeId);
                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetMyAssets,
                    ResponseText = "Error fetching asset assignments.",
                    Module = "AssetsManagement"
                };
            }
        }

        private async Task<ChatResponseDto> HandleGetSubscriptionStatus(Guid organizationId, string userMessage, string baseUrl)
        {
            try
            {
                var verifyUrl = BuildVerifyLink(baseUrl, "/subscription");
                var details = await _paymentRepository.GetCompanySubscriptionDetailsByCompanyIdAsync(organizationId);
                if (details == null)
                {
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.GetSubscriptionStatus,
                        ResponseText = $"No subscription details were found for your organization. Verify here: {verifyUrl}",
                        Module = "Subscription"
                    };
                }

                var endDateText = details.EndDate.HasValue
                    ? details.EndDate.Value.ToString("dd MMM yyyy")
                    : "N/A";
                var startDateText = details.StartDate.HasValue
                    ? details.StartDate.Value.ToString("dd MMM yyyy")
                    : "N/A";
                var state = details.IsExpired ? "Expired" : "Active";

                var message = IsYesNoQuestion(userMessage)
                    ? $"Yes, your subscription is {state}. Plan: {details.PlanName}, Billing cycle: {details.BillingCycle}, End date: {endDateText}. Verify here: {verifyUrl}"
                    : $"Subscription plan: {details.PlanName}. Billing cycle: {details.BillingCycle}. Status: {state}. Start date: {startDateText}. End date: {endDateText}.\nVerify here: {verifyUrl}";

                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetSubscriptionStatus,
                    ResponseText = message,
                    Module = "Subscription"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch subscription status for organization {OrganizationId}", organizationId);
                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetSubscriptionStatus,
                    ResponseText = "Error fetching subscription status.",
                    Module = "Subscription"
                };
            }
        }
        private async Task<ChatResponseDto> HandleGetHolidaySummary(Guid organizationId, string userMessage, string baseUrl)
        {
            try
            {
                var targetYear = DateTime.UtcNow.Year;
            var holidayUrl = BuildVerifyLink(baseUrl, "/holidays");
            var calendarUrl = BuildVerifyLink(baseUrl, "/calendar");
                var summaryResponse = await _holidayService.GetHolidaySummaryAsync(organizationId, targetYear);

                if (!summaryResponse.Success || summaryResponse.Data == null)
                {
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.GetHolidaySummary,
                        ResponseText = string.IsNullOrWhiteSpace(summaryResponse.Message)
                            ? "I could not retrieve your holiday summary right now."
                            : summaryResponse.Message,
                        Module = "HolidayManagement"
                    };
                }

                var summary = summaryResponse.Data;
                var upcoming = summary.UpcomingHoliday;
                var upcomingText = upcoming == null
                    ? "No upcoming holiday is currently scheduled."
                    : $"Upcoming holiday: {upcoming.HolidayName} on {upcoming.HolidayDate:dd MMM yyyy}.";

                var askUpcomingOnly = !string.IsNullOrWhiteSpace(userMessage)
                    && userMessage.Contains("upcoming", StringComparison.OrdinalIgnoreCase)
                    && userMessage.Contains("holiday", StringComparison.OrdinalIgnoreCase);

                if (askUpcomingOnly)
                {
                    var upcomingAnswer = upcoming == null
                        ? $"No, there is no upcoming holiday scheduled right now. Verify here: {holidayUrl}"
                        : $"Yes, upcoming holiday is {upcoming.HolidayName} on {upcoming.HolidayDate:dd MMM yyyy}. Verify here: {holidayUrl}";

                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.GetHolidaySummary,
                        ResponseText = upcomingAnswer,
                        Module = "HolidayManagement"
                    };
                }

                var message =
                    $"Holiday summary for {summary.Year}: Total {summary.TotalHolidays}, Mandatory {summary.MandatoryCount}, Restricted {summary.RestrictedCount}, Optional {summary.OptionalCount}.\n{upcomingText}\nVerify here: {holidayUrl}\nCalendar: {calendarUrl}";

                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetHolidaySummary,
                    ResponseText = message,
                    Module = "HolidayManagement"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch holiday summary for organization {OrganizationId}", organizationId);
                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetHolidaySummary,
                    ResponseText = "Error fetching holiday summary.",
                    Module = "HolidayManagement"
                };
            }
        }

        private async Task<ChatResponseDto> HandleGetMyRestrictedHolidays(Guid employeeId, Guid organizationId, string baseUrl)
        {
            try
            {
            var verifyUrl = BuildVerifyLink(baseUrl, "/holidays/my-holidays");
                var targetYear = DateTime.UtcNow.Year;
                var restrictedResponse = await _holidayService.GetEmployeeRestrictedHolidaysAsync(employeeId, organizationId, targetYear);

                if (!restrictedResponse.Success)
                {
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.GetMyRestrictedHolidays,
                        ResponseText = string.IsNullOrWhiteSpace(restrictedResponse.Message)
                            ? "I could not retrieve your restricted holidays right now."
                            : restrictedResponse.Message,
                        Module = "HolidayManagement"
                    };
                }

                var holidays = restrictedResponse.Data?.OrderBy(x => x.HolidayDate).ToList() ?? new List<EmployeeRestrictedHolidayDto>();
                if (!holidays.Any())
                {
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.GetMyRestrictedHolidays,
                        ResponseText = $"No, you have not selected any restricted holidays yet this year. Verify here: {verifyUrl}",
                        Module = "HolidayManagement"
                    };
                }

                var preview = holidays.Take(5)
                    .Select((x, i) => $"{i + 1}. {x.HolidayName} ({x.HolidayDate:dd MMM yyyy}) - {x.Status}");
                var suffix = holidays.Count > 5
                    ? $"\nShowing first 5 of {holidays.Count} selected restricted holidays."
                    : string.Empty;

                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetMyRestrictedHolidays,
                    ResponseText = "Your restricted holidays are:\n" + string.Join("\n", preview) + suffix + $"\nVerify here: {verifyUrl}",
                    Module = "HolidayManagement"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch restricted holidays for employee {EmployeeId}", employeeId);
                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetMyRestrictedHolidays,
                    ResponseText = "Error fetching your restricted holidays.",
                    Module = "HolidayManagement"
                };
            }
        }

        private async Task<ChatResponseDto> HandleGetTimesheetStatus(Guid employeeId, Guid organizationId, string userMessage, string baseUrl)
        {
            try
            {
                var verifyUrl = BuildVerifyLink(baseUrl, "/attendance/timesheet");
                var response = await _attendanceService.GetMonthlyTimesheetSnapshotsAsync(
                    organizationId,
                    null,
                    null,
                    employeeId);

                if (!response.Success)
                {
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.GetTimesheetStatus,
                        ResponseText = string.IsNullOrWhiteSpace(response.Message)
                            ? "I could not retrieve timesheet status right now."
                            : response.Message,
                        Module = "Attendance"
                    };
                }

                var snapshots = response.Data?
                    .OrderByDescending(x => x.Year)
                    .ThenByDescending(x => x.Month)
                    .ToList() ?? new List<MonthlyTimesheetSummary>();

                if (!snapshots.Any())
                {
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.GetTimesheetStatus,
                        ResponseText = "No timesheet snapshots were found for your profile.",
                        Module = "Attendance"
                    };
                }

                var latest = snapshots.First();
                var latestSummary =
                    $"Latest timesheet: {latest.TimesheetName} ({latest.MonthName} {latest.Year}) is {latest.Status}. Attendance: {latest.AttendancePercentage:0.##}%.";

                var askFinalized = !string.IsNullOrWhiteSpace(userMessage)
                    && userMessage.Contains("timesheet", StringComparison.OrdinalIgnoreCase)
                    && (
                        userMessage.Contains("finalized", StringComparison.OrdinalIgnoreCase)
                        || userMessage.Contains("finalised", StringComparison.OrdinalIgnoreCase)
                        || (IsYesNoQuestion(userMessage) && userMessage.Contains("status", StringComparison.OrdinalIgnoreCase))
                    );

                if (askFinalized)
                {
                    var isFinalized = string.Equals(latest.Status, "finalized", StringComparison.OrdinalIgnoreCase);
                    var direct = isFinalized
                        ? $"Yes, your latest timesheet ({latest.MonthName} {latest.Year}) is finalized. Verify here: {verifyUrl}"
                        : $"No, your latest timesheet ({latest.MonthName} {latest.Year}) is {latest.Status}. Verify here: {verifyUrl}";

                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.GetTimesheetStatus,
                        ResponseText = direct,
                        Module = "Attendance"
                    };
                }

                var additional = snapshots.Skip(1).Take(2)
                    .Select(x => $"- {x.MonthName} {x.Year}: {x.Status}");

                var message = additional.Any()
                    ? latestSummary + "\nRecent statuses:\n" + string.Join("\n", additional)
                    : latestSummary;

                message += $"\nVerify here: {verifyUrl}";

                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetTimesheetStatus,
                    ResponseText = message,
                    Module = "Attendance"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch timesheet status for employee {EmployeeId}", employeeId);
                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetTimesheetStatus,
                    ResponseText = "Error fetching timesheet status.",
                    Module = "Attendance"
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

        private async Task<ChatResponseDto> HandleCheckMyShift(Guid employeeId, Guid organizationId)
        {
            _logger.LogInformation("Fetching shift for employee {EmployeeId}", employeeId);

            try
            {
                var shift = await _attendanceRepository.GetShiftByEmployeeIdAsync(employeeId);
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
                _logger.LogError(ex, "Failed to fetch shift for employee {EmployeeId}", employeeId);
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
                Module = "Leave"
            };
        }

        //private Task<ChatResponseDto> HandleGetPayslip(Guid employeeId, Guid organizationId, string userMessage, string baseUrl)
        //{
        //    return HandleGetPayslipInternal(employeeId, userMessage, baseUrl);
        //}

        //private async Task<ChatResponseDto> HandleGetPayslipInternal(Guid employeeId, string userMessage, string baseUrl)
        //{
        //    try
        //    {
        //        var verifyUrl = BuildVerifyLink(baseUrl, "/payroll/payslip");
        //        var result = await _payrollRepository.GetPayrollEntriesByEmployeeAsync(employeeId, 1, 1);
        //        var latest = result.Data?.FirstOrDefault();
        //        if (latest == null)
        //        {
        //            return new ChatResponseDto
        //            {
        //                Intent = ChatIntent.GetPayslip,
        //                ResponseText = $"No, no payroll entry or payslip data is available for your profile yet. Verify here: {verifyUrl}",
        //                Module = "Payroll"
        //            };
        //        }

        //        var message = IsYesNoQuestion(userMessage)
        //            ? $"Yes, your latest payslip is available for {latest.PeriodName}. Status: {latest.Status}. Verify here: {verifyUrl}"
        //            : $"Latest payslip: {latest.PeriodName}. Net Salary: {latest.NetSalary:0.##} {latest.Currency}. Gross: {latest.GrossSalary:0.##}, Tax: {latest.TaxAmount:0.##}. Status: {latest.Status}.\nVerify here: {verifyUrl}";

        //        return new ChatResponseDto
        //        {
        //            Intent = ChatIntent.GetPayslip,
        //            ResponseText = message,
        //            Module = "Payroll"
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to fetch payslip for employee {EmployeeId}", employeeId);
        //        return new ChatResponseDto
        //        {
        //            Intent = ChatIntent.GetPayslip,
        //            ResponseText = "Error fetching payslip details.",
        //            Module = "Payroll"
        //        };
        //    }
        //}

        private async Task<ChatResponseDto> HandleGetMySkills(Guid employeeId, Guid organizationId)
        {
            _logger.LogInformation("Fetching employee skills for employee {EmployeeId}", employeeId);

            try
            {
                var skills = await _performanceRepository.GetEmployeeSkillsAsync(employeeId);

                if (skills != null && skills.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("Here are your registered skills:");

                    int index = 1;
                    string? currentCategory = null;

                    foreach (var item in skills)
                    {
                        // Category heading (grouped)
                        if (!string.Equals(currentCategory, item.SkillCategory, StringComparison.OrdinalIgnoreCase))
                        {
                            currentCategory = item.SkillCategory;
                            sb.AppendLine();
                            sb.AppendLine($"📌 {currentCategory}");
                        }

                        sb.Append($"{index}. {item.SkillName}");
                        sb.Append($" – Proficiency Level: {item.ProficiencyLevel}");

                        if (item.LastAssessed.HasValue)
                        {
                            sb.Append($", Last Assessed: {item.LastAssessed.Value:dd MMM yyyy}");
                        }

                        if (!string.IsNullOrWhiteSpace(item.AssessorName))
                        {
                            sb.Append($", Assessed By: {item.AssessorName}");
                        }

                        if (!string.IsNullOrWhiteSpace(item.Notes))
                        {
                            sb.Append($", Notes: {item.Notes}");
                        }

                        sb.AppendLine();
                        index++;
                    }

                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.GetMySkills,
                        ResponseText = sb.ToString().TrimEnd(),
                        Module = "Performance"
                    };
                }

                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetMySkills,
                    ResponseText = "No skill information found for your profile.",
                    Module = "Performance"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch employee skills for employee {EmployeeId}", employeeId);

                return new ChatResponseDto
                {
                    Intent = ChatIntent.GetMySkills,
                    ResponseText = "Error fetching employee skills.",
                    Module = "Performance"
                };
            }
        }

        private async Task<ChatResponseDto> HandleClockIn(Guid employeeId, Guid organizationId, string ipAddress)
        {
            _logger.LogInformation("Processing clock-in request for employee {EmployeeId}", employeeId);

            try
            {
                var request = new ClockInOutRequest
                {
                    Action = "in",
                    Location = null 
                };

                var result = await _attendanceService.ClockInAsync(employeeId, request,ipAddress);

                if (result.Success)
                {
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.ClockIn,
                        ResponseText = "✅ You have successfully clocked in. Have a productive day!",
                        Module = "Attendance"
                    };
                }

                return new ChatResponseDto
                {
                    Intent = ChatIntent.ClockIn,
                    ResponseText = "⚠️ Clock-in failed. You may already be clocked in or an error occurred.",
                    Module = "Attendance"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Clock-in failed for employee {EmployeeId}", employeeId);

                return new ChatResponseDto
                {
                    Intent = ChatIntent.ClockIn,
                    ResponseText = "❌ " + ex.Message,
                    Module = "Attendance"
                };
            }
        }

        private async Task<ChatResponseDto> HandleClockOut(Guid employeeId, Guid organizationId, string ipAddress)
        {
            _logger.LogInformation("Processing clock-out request for employee {EmployeeId}", employeeId);

            try
            {
                var request = new ClockInOutRequest
                {
                    Action = "out",
                    Location = null 
                };

                var result = await _attendanceService.ClockOutAsync(employeeId, request, ipAddress);

                if (result.Success)
                {
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.ClockOut,
                        ResponseText = "✅ You have successfully clocked out. See you next time!",
                        Module = "Attendance"
                    };
                }

                return new ChatResponseDto
                {
                    Intent = ChatIntent.ClockOut,
                    ResponseText = "⚠️ Clock-out failed. You may not be clocked in or an error occurred.",
                    Module = "Attendance"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Clock-out failed for employee {EmployeeId}", employeeId);

                return new ChatResponseDto
                {
                    Intent = ChatIntent.ClockOut,
                    ResponseText = "❌ "+ ex.Message,
                    Module = "Attendance"
                };
            }
        }
        


    }
}
