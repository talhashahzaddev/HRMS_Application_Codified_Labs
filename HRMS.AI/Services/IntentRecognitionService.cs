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
            // not implemented yet
            //-GetPayslip:
            //     User wants to view or download payslip details including monthly salary,
            //     deductions, allowances, payroll summary, or salary slip.
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

             - GetPerformanceSummary:
               User wants to view performance-related information such as performance summary,
               ratings, appraisals, or last month/quarter performance review details.

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

             - LeaveApplicationDetails:
               User provides leave application details such as leave type,
               start date, end date, and reason for applying for leave.

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
 
If the label is Guidance, Greeting, also determine the affected module, and if related department of positions then also return EmployeeManagement. If label is Greeting then module is also Greeting. Possible modules: Greeting, EmployeeManagement, Attendance, Leave, Payroll, Performance, SkillMatrix, ShiftManagement, Profile, OrganizationSettings, HRPolicies, AssetsManagement, AccountCreation, GlobalCalendar, NewsManagement, JobsManagement, ExpensesManagement, Other.

        Return EXACT output in this format (no extra text):
        Label: <Greeting|Guidance|GetPerformanceSummary|GetAttendance|CheckMyShift|GetLeaveBalance|ApplyForLeave|GetPayslip|EmployeeInfo|LeaveApplicationDetails|GetMySkills|ClockIn|ClockOut|Unknown>; Module: <module or empty>

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
    }

}
