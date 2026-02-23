using HRMS.AI.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HRMS.AI.Services
{
    public class ApiKeyProvider : IApiKeyProvider
    {
        private readonly ILogger<ApiKeyProvider> _logger;
        private readonly List<string> _allKeys;
        private readonly List<string> _activeKeys;
        private int _currentIndex = 0;

        public ApiKeyProvider(IConfiguration configuration, ILogger<ApiKeyProvider> logger, HttpClient httpClient)
        {
            _logger = logger;

            // Load all keys from configuration
            _allKeys = configuration.GetSection("GroqAI:ApiKeys")
                                    .GetChildren()
                                    .Select(x => x.Value)
                                    .Where(v => !string.IsNullOrEmpty(v))
                                    .ToList();
            if (_allKeys.Count == 0)
                throw new InvalidOperationException("No API keys configured in GroqAI:ApiKeys.");

            _activeKeys = new List<string>();

            // ✅ Proactively validate keys at startup
            if (_activeKeys.Count == 0)
            {
                foreach (var key in _allKeys)
                {
                    try
                    {
                        var requestBody = new
                        {
                            model = configuration["GroqAI:Model"],
                            messages = new[] {
                        new { role = "system", content = "" },
                        new { role = "user", content = "Hy" }
                    },
                            max_tokens = 20,
                            temperature = 0.0
                        };

                        var requestJson = JsonSerializer.Serialize(requestBody);
                        var req = new HttpRequestMessage(HttpMethod.Post, configuration["GroqAI:Url"]);
                        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                        req.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                        var response = httpClient.SendAsync(req).GetAwaiter().GetResult();
                        if (response.IsSuccessStatusCode)
                        {
                            _activeKeys.Add(key);
                            break;
                            _logger.LogInformation("API key validated successfully.");
                        }
                        else
                        {
                            _logger.LogWarning("API key validation failed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "API key validation threw an exception.");
                    }
                }
            }

            if (_activeKeys.Count == 0)
                throw new InvalidOperationException("No valid API keys found after validation.");

            // Optional: shuffle keys to avoid hitting the same key repeatedly
            _activeKeys = _activeKeys.OrderBy(_ => Guid.NewGuid()).ToList();
        }

        public string GetCurrentKey() => _activeKeys[_currentIndex];

        public void RotateKey()
        {
            _currentIndex = (_currentIndex + 1) % _activeKeys.Count;
            _logger.LogInformation("API key rotated to index {Index}", _currentIndex);
        }
    }
}