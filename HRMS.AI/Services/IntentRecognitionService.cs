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
        private readonly string _ApiKey;
        private readonly string _Url;
        private readonly string _model;
        public IntentRecognitionService(IConfiguration configuration, ILogger<IntentRecognitionService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _ApiKey = _configuration["GroqAI:ApiKey"] ?? string.Empty;
            _Url = _configuration["GroqAI:Url"] ?? string.Empty;
            _model = _configuration["GroqAI:Model"] ?? string.Empty;

            if (string.IsNullOrEmpty(_ApiKey))
                _logger.LogWarning("OpenAI API key is not configured. Intent recognition will fail in production.");
        }

        public async Task<IntentDetectionResult> DetectIntentAsync(string message)
        {
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
                          Example topics include how to apply for leave, attendance rules, payroll process,
                          HR policies, or where to check specific information.

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

                        - GetPayslip:
                          User wants to view or download payslip details including monthly salary,
                          deductions, allowances, payroll summary, or salary slip.

                        - GetManager:
                          User wants to know who their manager is, manager's contact details,
                          or reporting hierarchy information or wnat to know our assigned manager.

                        - Unknown:
                          Any request that does not clearly
                          match HRMS guidance or user-specific HRMS data.


                   If the label is Guidance, also determine the affected module. Possible modules: Greeting, EmployeeManagement, Attendance, Leave, Payroll, Performance, SkillMatrix, ShiftManagement, Profile, OrganizationSettings,HRPolices, Other.

                   Return EXACT output in this format (no extra text):
                   Label: <Greeting|Guidance|GetPerformanceSummary|GetAttendance|CheckMyShift|GetLeaveBalance|ApplyForLeave|GetPayslip|GetManager|Unknown>; Module: <module or empty>

                   User message:
                   ""{message}"".

                  Result:";

            try
            {
                var requestBody = new
                {
                    model = _model,
                    messages = new[] {
                        new { role = "system", content = "You are a concise intent classification assistant." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 20,
                    temperature = 0.0
                };

                var requestJson = JsonSerializer.Serialize(requestBody);
                var req = new HttpRequestMessage(HttpMethod.Post, _Url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _ApiKey);
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
                    "GetManager" => ChatIntent.GetManager,
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
