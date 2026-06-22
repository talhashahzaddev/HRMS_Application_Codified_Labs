using HRMS.AI.Models;
using HRMS.AI.Models.Enums;
using HRMS.AI.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HRMS.AI.Services
{
    public class IntentRecognitionService : IIntentRecognitionService
    {
        private readonly ILogger<IntentRecognitionService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IApiKeyProvider _apiKeyProvider;
        private readonly string _Url;
        private readonly string _model;
        public IntentRecognitionService(IConfiguration configuration, IApiKeyProvider apiKeyProvider, ILogger<IntentRecognitionService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _apiKeyProvider = apiKeyProvider;
            _Url = _configuration["GroqAI:Url"] ?? string.Empty;
            _model = _configuration["GroqAI:Model"] ?? string.Empty;

        }
        private string CurrentKey => _apiKeyProvider.GetCurrentKey();

        public async Task<IntentDetectionResult> DetectIntentAsync(string message)
        {
          var ruleResult = DetectIntentByRules(message);
          if (ruleResult != null)
          {
            return ruleResult;
          }

            // not implemented yet
            //-GetPayslip:
            //     User wants their own salary or payroll information: basic salary, net salary,
            //     last/latest payroll, payroll history, deduction breakdown, or payslip/salary slip.
            //     NOT for admin payroll processing questions (those are Guidance / Payroll module).
            var prompt =
     $@"You are classifying HRMS user messages.
        Labels:
             - Greeting:
               Greetings, casual conversations
               User starts the conversation with greetings such as
               hi, hello, hey, good morning, good afternoon, or similar
               conversation starters without asking an HR-related question.

          - Guidance:
              Questions related to HRMS rules, permissions, processes, or steps.
              Example topics include leave application, attendance rules, Employee Management,
              Performance Management, payroll process, HR policies, Assets Management
              (creating asset types, assigning/viewing/editing assets), Account creation or organization registration,
              Global Calendar, News updates, Job postings, Expense management, or other process-related queries,
              for any question related Calendar move to global calnder.

              Module mapping hints for Guidance:
              - If user asks about holidays, holiday calendar, restricted holidays, or holiday policy, module must be HolidayManagement.
              - If user asks about dashboard/home page/default page, module must be Dashboard.
              - If user asks about profile details, module should be Profile.

             - GetPerformanceSummary:
               User wants to view performance-related information such as performance summary,
               ratings, appraisals, or last month/quarter performance review details.

             - GetHolidaySummary:
               User wants holiday information such as yearly holiday summary,
               upcoming holidays, mandatory/restricted holiday counts, or company holidays.

             - GetMyRestrictedHolidays:
               User wants to view or manage their restricted holidays,
               selected restricted holidays, or available restricted holidays.

             - GetTimesheetStatus:
               User wants to check timesheet status, whether timesheet is draft/finalized,
               pending review status, or monthly timesheet progress.

             - GetExpenseStatus:
               User wants to check expense claims, claim status,
               pending/approved/rejected expenses, or expense summary.

             - GetJobsStatus:
               User wants information about job openings, applications,
               application status, or hiring stages.

             - GetNewsUpdates:
               User wants latest company news, announcements,
               or recently published news items.

             - GetMyAssets:
               User wants assigned asset information,
               active assets, or asset assignment history.

             - GetSubscriptionStatus:
               User wants details about subscription plan, billing cycle,
               plan expiry, or whether subscription is active/expired.

             - GetAttendance:
               User wants to view attendance-related data including working days, present days,
               absents, late check-ins, check-in/check-out times, or attendance summary.

             - CheckMyShift:
               User wants to know their assigned shift details such as shift timing,
               working hours, day/night shift, or shift schedule.

             - GetLeaveBalance:
               User wants to check leave-related information including total leave balance,
               remaining leaves, used leaves, leave types, or leave balance summary.

             - ApplyForLeave:
               User wants to apply for leave, submit a leave request, select leave type,
               choose leave dates, or check leave eligibility.

             - GetPayslip:
               User wants their own salary or payroll information: basic salary, net salary,
               last/latest payroll, payroll history, deduction breakdown, or payslip/salary slip.
               NOT for admin payroll processing questions (those are Guidance).

             - LeaveApplicationDetails:
               User provides leave application details such as leave type,
               start date, end date, and reason for applying for leave.
               If user already provided leave type and both dates in the same message,
               classify as LeaveApplicationDetails instead of ApplyForLeave.

             - EmployeeInfo:
               User wants to know about their personal info only.

             - GetMySkills:
               User wants to view their skill set or skill matrix.

             - ClockIn:
               User wants to clock in for attendance.

             - ClockOut:
               User wants to clock out for attendance.

             - Unknown:
               Any request that does not clearly match HRMS guidance.
 
If the label is Guidance, Greeting, also determine the affected module, and if related department of positions then also return EmployeeManagement. If label is Greeting then module is also Greeting. Possible modules: Greeting, Dashboard, EmployeeManagement, Attendance, Leave, HolidayManagement, Payroll, Performance, SkillMatrix, ShiftManagement, Profile, OrganizationSettings, HRPolicies, AssetsManagement, AccountCreation, GlobalCalendar, NewsManagement, JobsManagement, ExpensesManagement, Subscription, Other.

        Return EXACT output in this format (no extra text):
        Label: <Greeting|Guidance|GetHolidaySummary|GetMyRestrictedHolidays|GetTimesheetStatus|GetExpenseStatus|GetJobsStatus|GetNewsUpdates|GetMyAssets|GetSubscriptionStatus|GetPerformanceSummary|GetAttendance|CheckMyShift|GetLeaveBalance|ApplyForLeave|GetPayslip|EmployeeInfo|LeaveApplicationDetails|GetMySkills|ClockIn|ClockOut|Unknown>; Module: <module or empty>

        User message:
        ""{message}"".

       Result:";

            try
            {
                var requestBody = new
                {
                    model = _model,
                    messages = new[] {
                        new { role = "system", content = "You are a concise intent classification assistant. check the behaviour of user question then decide the intent " },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 20,
                    temperature = 0.0
                };

                var requestJson = JsonSerializer.Serialize(requestBody);
                var req = new HttpRequestMessage(HttpMethod.Post, _Url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CurrentKey);
                req.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                var resp = await _httpClient.SendAsync(req);
                resp.EnsureSuccessStatusCode();

                var content = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var choices = doc.RootElement.GetProperty("choices");
                var text = string.Empty;
                if (choices.GetArrayLength() > 0)
                {
                    var messageElement = choices[0].GetProperty("message");
                    text = messageElement.GetProperty("content").GetString()?.Trim() ?? string.Empty;
                }

                _logger.LogInformation("IntentRecognition response raw: {Text}", text);

                // Expected format: "Label: Guidance; Module: Attendance"
                var label = string.Empty;
                var module = string.Empty;
                if (!string.IsNullOrEmpty(text))
                {
                    var parts = text.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in parts)
                    {
                        var idx = p.IndexOf(':');
                        if (idx <= 0) continue;
                        var key = p.Substring(0, idx).Trim();
                        var val = p.Substring(idx + 1).Trim();
                        if (key.Equals("Label", StringComparison.OrdinalIgnoreCase))
                            label = val;
                        else if (key.Equals("Module", StringComparison.OrdinalIgnoreCase))
                            module = val;
                    }
                }

                ChatIntent intent = label switch
                {
                    "Greeting" => ChatIntent.Greeting,
                    "Guidance" => ChatIntent.Guidance,
                  "GetHolidaySummary" => ChatIntent.GetHolidaySummary,
                  "GetMyRestrictedHolidays" => ChatIntent.GetMyRestrictedHolidays,
                  "GetTimesheetStatus" => ChatIntent.GetTimesheetStatus,
                    "GetExpenseStatus" => ChatIntent.GetExpenseStatus,
                    "GetJobsStatus" => ChatIntent.GetJobsStatus,
                    "GetNewsUpdates" => ChatIntent.GetNewsUpdates,
                    "GetMyAssets" => ChatIntent.GetMyAssets,
                    "GetSubscriptionStatus" => ChatIntent.GetSubscriptionStatus,
                    "GetPerformanceSummary" => ChatIntent.GetPerformanceSummary,
                    "GetAttendance" => ChatIntent.GetAttendance,
                    "CheckMyShift" => ChatIntent.CheckMyShift,
                    "GetLeaveBalance" => ChatIntent.GetLeaveBalance,
                    "ApplyForLeave" => ChatIntent.ApplyForLeave,
                    "GetPayslip" => ChatIntent.GetPayslip,
                    "EmployeeInfo" => ChatIntent.EmployeeInfo,
                    "LeaveApplicationDetails" => ChatIntent.LeaveApplicationDetails,
                    "GetMySkills" => ChatIntent.GetMySkills,
                    "ClockIn" => ChatIntent.ClockIn,
                    "ClockOut" => ChatIntent.ClockOut,
                    _ => ChatIntent.Unknown
                };

                if (intent != ChatIntent.Guidance && intent != ChatIntent.Greeting)
                {
                    // clear module if not guidance
                    module = string.Empty;
                }

                return new IntentDetectionResult { Intent = intent, Module = module };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Intent detection failed");
                return new IntentDetectionResult { Intent = ChatIntent.Unknown };
            }
        }

        private IntentDetectionResult? DetectIntentByRules(string message)
        {
          if (string.IsNullOrWhiteSpace(message))
          {
            return null;
          }

          var text = message.Trim().ToLowerInvariant();

          // Direct status/data queries should bypass Guidance.
          if (text.Contains("payslip") || text.Contains("salary slip")
              || text.Contains("basic salary") || text.Contains("my salary")
              || text.Contains("net salary") || text.Contains("last payroll")
              || text.Contains("payroll history") || text.Contains("salary deduction")
              || (text.Contains("payroll") && (text.Contains("history") || text.Contains("deduction") || text.Contains("breakdown"))))
            return new IntentDetectionResult { Intent = ChatIntent.GetPayslip };

          if (text.Contains("expense") && (text.Contains("status") || text.Contains("claim") || text.Contains("reimb")))
            return new IntentDetectionResult { Intent = ChatIntent.GetExpenseStatus };

          if ((text.Contains("job application") || text.Contains("applications")) && text.Contains("status"))
            return new IntentDetectionResult { Intent = ChatIntent.GetJobsStatus };

          if (text.Contains("latest") && text.Contains("news") || text.Contains("company news"))
            return new IntentDetectionResult { Intent = ChatIntent.GetNewsUpdates };

          if ((text.Contains("asset") || text.Contains("assets")) && (text.Contains("assigned") || text.Contains("my")))
            return new IntentDetectionResult { Intent = ChatIntent.GetMyAssets };

          if (text.Contains("subscription") && text.Contains("status"))
            return new IntentDetectionResult { Intent = ChatIntent.GetSubscriptionStatus };

          if (text.Contains("timesheet") && (text.Contains("status") || text.Contains("finalized") || text.Contains("finalised")))
            return new IntentDetectionResult { Intent = ChatIntent.GetTimesheetStatus };

          if (text.Contains("holiday summary") || text.Contains("upcoming holiday") || text.Contains("any upcoming holiday"))
            return new IntentDetectionResult { Intent = ChatIntent.GetHolidaySummary };

          if (text.Contains("my restricted holiday") || text.Contains("show my restricted holiday") || text.Contains("restricted holidays"))
            return new IntentDetectionResult { Intent = ChatIntent.GetMyRestrictedHolidays };

          if ((text.Contains("select restricted holiday") || text.Contains("how do i select restricted")) && text.Contains("holiday"))
            return new IntentDetectionResult { Intent = ChatIntent.Guidance, Module = "HolidayManagement" };

          if ((text.Contains("dashboard") || text.Contains("home page")) && (text.Contains("open") || text.Contains("go") || text.Contains("show") || text.Contains("how")))
            return new IntentDetectionResult { Intent = ChatIntent.Guidance, Module = "Dashboard" };

          // Leave application with details in one message.
          var hasTwoDates = Regex.Matches(text, "\\b(\\d{4}-\\d{2}-\\d{2}|\\d{1,2}[/-]\\d{1,2}[/-]\\d{2,4})\\b").Count >= 2;
          if (text.Contains("leave") && hasTwoDates && (text.Contains("from") || text.Contains("to")))
            return new IntentDetectionResult { Intent = ChatIntent.LeaveApplicationDetails };

          return null;
        }
    }

}
