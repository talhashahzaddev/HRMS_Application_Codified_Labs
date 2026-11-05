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
using System.Threading;

namespace HRMS.AI.Services
{
    /// <summary>
    /// Holds the Groq API keys for the whole app. Registered as a singleton, so keys are
    /// validated once (not on every chat message). If no key works, validation is retried
    /// at most once every <see cref="RetryAfterFailure"/> so a fixed key is picked up without
    /// a restart, while a bad key doesn't cost a Groq call per request.
    /// </summary>
    public class ApiKeyProvider : IApiKeyProvider
    {
        public static readonly TimeSpan RetryAfterFailure = TimeSpan.FromMinutes(5);

        private readonly ILogger<ApiKeyProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly List<string> _allKeys;
        private readonly string? _model;
        private readonly string? _url;
        private readonly Func<DateTime> _utcNow;
        private readonly object _lock = new();

        private List<string>? _activeKeys;
        private DateTime _lastFailedValidationUtc = DateTime.MinValue;
        private int _currentIndex;

        public ApiKeyProvider(IConfiguration configuration, ILogger<ApiKeyProvider> logger, IHttpClientFactory httpClientFactory)
            : this(configuration, logger, httpClientFactory, () => DateTime.UtcNow)
        {
        }

        internal ApiKeyProvider(IConfiguration configuration, ILogger<ApiKeyProvider> logger, IHttpClientFactory httpClientFactory, Func<DateTime> utcNow)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _utcNow = utcNow;
            _model = configuration["GroqAI:Model"];
            _url = configuration["GroqAI:Url"];

            _allKeys = configuration.GetSection("GroqAI:ApiKeys")
                                    .GetChildren()
                                    .Select(x => x.Value!)
                                    .Where(v => !string.IsNullOrWhiteSpace(v))
                                    .ToList();

            var singleKey = configuration["GROQ_API_KEY"] ?? configuration["GroqAI:ApiKey"];
            if (!string.IsNullOrWhiteSpace(singleKey) && !_allKeys.Contains(singleKey))
                _allKeys.Insert(0, singleKey);
        }

        public string GetCurrentKey()
        {
            var keys = EnsureValidated();
            return keys[Volatile.Read(ref _currentIndex) % keys.Count];
        }

        public void RotateKey()
        {
            var keys = EnsureValidated();
            var next = Interlocked.Increment(ref _currentIndex) % keys.Count;
            _logger.LogInformation("API key rotated to index {Index}", next);
        }

        private List<string> EnsureValidated()
        {
            var keys = _activeKeys;
            if (keys != null)
                return keys;

            lock (_lock)
            {
                if (_activeKeys != null)
                    return _activeKeys;

                if (_allKeys.Count == 0)
                    throw new InvalidOperationException("No API keys configured in GroqAI:ApiKeys.");

                if (_utcNow() - _lastFailedValidationUtc < RetryAfterFailure)
                    throw new InvalidOperationException("No valid API keys found after validation.");

                var valid = _allKeys.Where(IsKeyValid).ToList();
                if (valid.Count == 0)
                {
                    _lastFailedValidationUtc = _utcNow();
                    _logger.LogError("No valid Groq API key found. Will check again in {Minutes} minutes.", RetryAfterFailure.TotalMinutes);
                    throw new InvalidOperationException("No valid API keys found after validation.");
                }

                // Shuffle so multiple app instances don't all start on the same key.
                _activeKeys = valid.OrderBy(_ => Guid.NewGuid()).ToList();
                _logger.LogInformation("{Count} Groq API key(s) validated.", _activeKeys.Count);
                return _activeKeys;
            }
        }

        private bool IsKeyValid(string key)
        {
            try
            {
                var requestBody = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful assistant." },
                        new { role = "user", content = "Hi" }
                    },
                    max_tokens = 20,
                    temperature = 0.0
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, _url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                req.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                using var response = _httpClientFactory.CreateClient().SendAsync(req).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                    return true;

                _logger.LogWarning("API key validation failed with status code {StatusCode}.", (int)response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "API key validation threw an exception.");
                return false;
            }
        }
    }
}
