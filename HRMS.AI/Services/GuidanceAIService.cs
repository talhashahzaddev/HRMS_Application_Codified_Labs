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
    public class GuidanceAIService : IGuidanceAIService
    {
        private const string Content = "content";
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GuidanceAIService> _logger;
        private readonly string _ApiKey;
        private readonly string _Url;
        private readonly string _model;

        public GuidanceAIService(IConfiguration configuration, ILogger<GuidanceAIService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _ApiKey = _configuration["GroqAI:ApiKey"] ?? string.Empty;
            _Url = _configuration["GroqAI:Url"] ?? string.Empty;
            _model = _configuration["GroqAI:Model"] ?? string.Empty;

            if (string.IsNullOrEmpty(_ApiKey))
                _logger.LogWarning("OpenAI API key is not configured. Guidance AI will not work in production.");
        }

        public async Task<string> GenerateGuidanceAsync(string message, string module)
        {
            // get module-specific HRMS context
            var context = HrmsContext.GetContext(module);

            var prompt =
                        $@"HRMS CONTEXT:
                        {context}

                        USER QUESTION:
                        {message}

                        INSTRUCTIONS:
                        - Answer strictly using the provided HRMS context only.
                        - Do not assume or infer any information that is not explicitly present in the context.
                        - Do not return any links unless they are clearly provided in the context.
                        - When explaining a process:
                          - First explain briefly how the user can check or access the process.
                          - Then list the steps clearly in a numbered format (1, 2, 3).
                          _ When providing steps, list them in numbered form, with each step on its own line. 
                        - If links are required:
                          - Add only one link per statement.
                          - Always place the link at the end of the statement.
                        - If the user asks multiple questions in one message:
                          - Attach only the relevant link to each respective statement.
                        - Ensure the link exactly matches the user question.
                          - Example: If the user asks about applying for leave, return the Apply Leave link, not the dashboard link.
                        - Clearly state permissions, specifying who can and cannot access the feature.
                        - If the user is an employee:
                          - Do not explain Admin or HR-only actions unless needed to explain access restrictions.
                        - Keep responses concise, structured, professional, and limited to a maximum of 3–4 lines.
                        ;

                        FINAL ANSWER:";

            try
            {
                var requestBody = new
                {
                    model = _model,
                    messages = new[] {
                       new
                            {
                                role = "system",
                                content =
                                "You are an HRMS Guidance Assistant for an enterprise HR system. " +
                                "You must answer strictly based on the provided HRMS context. " +
                                "Do not assume features, roles, or permissions that are not mentioned in the context. " +
                                "If a user asks about something they do not have access to, clearly state the access restriction. " +
                                "When the user asks HOW to perform an action, provide clear step-by-step instructions. " +
                                "When relevant, mention applicable rules before steps. " +
                                "Keep answers concise, professional, and easy to understand."
                            },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 300,
                    temperature = 0.2
                };

                var requestJson = JsonSerializer.Serialize(requestBody);
                var req = new HttpRequestMessage(HttpMethod.Post, _Url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _ApiKey);
                req.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                var resp = await _httpClient.SendAsync(req);
                resp.EnsureSuccessStatusCode();

                var content = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                // chat/completions returns choices[].message.content
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var messageElement = choices[0].GetProperty("message");
                    var reply = messageElement.GetProperty("content").GetString()?.Trim() ?? string.Empty;
                    return reply;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Guidance AI call failed");
                return "Sorry, I'm unable to provide guidance at the moment.";
            }
        }
        public async Task<ChatResponseDto> GenrateEmployeeInfoGuidance(string message,string employeeinfo)
        {
            var prompt = $@"
                            ROLE:
                            You are an Employee Self-Service Guidance Assistant for an enterprise HRMS system.

                            EMPLOYEE INFORMATION (JSON):
                            {employeeinfo}

                            USER QUESTION:
                            {message}

                            INSTRUCTIONS:
                            - Answer strictly based on the employee information provided above.
                            - Do NOT assume any data that is not present in the JSON.
                            - If the requested information is not available, clearly say so.
                            - If the question is about employee's own details, respond confidently.
                            - If the question relates to permissions, infer ONLY from available fields (e.g. role, department, position).
                            - Do NOT explain HR or Admin actions unless the data explicitly indicates access.
                            - Keep the answer short (3–4 lines max), clear, structured, and professional.
                            - If steps are required, list them briefly.

                            FINAL ANSWER:
                            ";
            try
            {
                var requestBody = new
                {
                    model = _model,
                    messages = new[] {
                       new
                            {
                                role = "system",
                                 content =
                                            "You are an enterprise HRMS Employee Guidance Assistant. " +
                                            "Your job is to answer employee questions ONLY using the provided employee data. " +
                                            "if the data is null related any thing like manager name is null or other data then responed you are not assigned manager yet responce with profeshinal way"+
                                            "if user ask related any question like my position my manager or other question then retrun only name if not null nnot return id related things"+
                                            "if user ask about how many days i worked in this company then check hiring date and calculate with current date "+
                                            "Never guess missing information. " +
                                            "If data is unavailable, state it clearly. " +
                                            "Keep responses concise, professional, and accurate."
                            },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 300,
                    temperature = 0.2
                };

                var requestJson = JsonSerializer.Serialize(requestBody);
                var req = new HttpRequestMessage(HttpMethod.Post, _Url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _ApiKey);
                req.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                var resp = await _httpClient.SendAsync(req);
                resp.EnsureSuccessStatusCode();

                var content = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                // chat/completions returns choices[].message.content
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var messageElement = choices[0].GetProperty("message");
                    var reply = messageElement.GetProperty("content").GetString()?.Trim() ?? string.Empty;
                     return new ChatResponseDto
                    {
                        Intent = ChatIntent.EmployeeInfo,
                        ResponseText = reply,
                        Module = "EmployeeInfo"
                    };
                }

                return new ChatResponseDto
                {
                    Intent = ChatIntent.EmployeeInfo,
                    ResponseText = "Employee information is not available.",
                    Module = "EmployeeManagement"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Guidance AI call failed");
                return new ChatResponseDto
                {
                    Intent = ChatIntent.EmployeeInfo,
                    ResponseText = "Sorry, I'm unable to provide guidance at the moment.",
                    Module = "Employeeinfo"
                };
            }

        }
    }

}
