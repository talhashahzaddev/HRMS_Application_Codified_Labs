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
                        - Answer only using the HRMS context above.
                        - If the question is about a process, explain it in clear steps.
                        - If the question is about permissions, clearly state who can and cannot access it.
                        - If the user is an employee, do not explain admin or HR-only actions unless required to explain restrictions.
                        - Keep the answer short, structured, and professional.

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
    }

}
