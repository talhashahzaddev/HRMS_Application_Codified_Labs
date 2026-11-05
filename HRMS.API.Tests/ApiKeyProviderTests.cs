using System.Net;
using HRMS.AI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HRMS.API.Tests;

public sealed class ApiKeyProviderTests
{
    private sealed class FakeGroq : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; }
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent("{}") });
        }
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private DateTime _now = new(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);

    private static IConfiguration Config(params string[] extraKeys)
    {
        var values = new Dictionary<string, string?>
        {
            ["GroqAI:ApiKey"] = "gsk_main",
            ["GroqAI:Model"] = "openai/gpt-oss-20b",
            ["GroqAI:Url"] = "https://api.groq.com/openai/v1/chat/completions"
        };
        for (var i = 0; i < extraKeys.Length; i++)
            values[$"GroqAI:ApiKeys:{i}"] = extraKeys[i];
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private ApiKeyProvider Create(FakeGroq groq, IConfiguration? config = null) =>
        new(config ?? Config(), NullLogger<ApiKeyProvider>.Instance, new Factory(groq), () => _now);

    [Fact]
    public void Constructor_DoesNotCallGroqOrThrow()
    {
        var groq = new FakeGroq { Status = HttpStatusCode.Unauthorized };
        _ = Create(groq);
        Assert.Equal(0, groq.Calls);
    }

    [Fact]
    public void ValidKey_IsCheckedOnlyOnce_NoMatterHowManyMessages()
    {
        var groq = new FakeGroq { Status = HttpStatusCode.OK };
        var provider = Create(groq);

        for (var i = 0; i < 50; i++)
            Assert.Equal("gsk_main", provider.GetCurrentKey());

        Assert.Equal(1, groq.Calls);
    }

    [Fact]
    public void InvalidKey_Throws_ButDoesNotCallGroqOnEveryRequest()
    {
        var groq = new FakeGroq { Status = HttpStatusCode.Unauthorized };
        var provider = Create(groq);

        for (var i = 0; i < 20; i++)
        {
            var ex = Assert.Throws<InvalidOperationException>(() => provider.GetCurrentKey());
            Assert.Equal("No valid API keys found after validation.", ex.Message);
        }

        Assert.Equal(1, groq.Calls);
    }

    [Fact]
    public void InvalidKey_IsRecheckedAfterRetryWindow_SoAFixedKeyStartsWorking()
    {
        var groq = new FakeGroq { Status = HttpStatusCode.Unauthorized };
        var provider = Create(groq);
        Assert.Throws<InvalidOperationException>(() => provider.GetCurrentKey());

        groq.Status = HttpStatusCode.OK;          // e.g. Groq outage is over
        _now += TimeSpan.FromMinutes(4);
        Assert.Throws<InvalidOperationException>(() => provider.GetCurrentKey());
        Assert.Equal(1, groq.Calls);

        _now += TimeSpan.FromMinutes(2);          // past the 5 minute window
        Assert.Equal("gsk_main", provider.GetCurrentKey());
        Assert.Equal(2, groq.Calls);
    }

    [Fact]
    public void NoKeysConfigured_ThrowsClearError_WithoutCallingGroq()
    {
        var groq = new FakeGroq { Status = HttpStatusCode.OK };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var provider = Create(groq, config);

        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetCurrentKey());
        Assert.Contains("No API keys configured", ex.Message);
        Assert.Equal(0, groq.Calls);
    }

    [Fact]
    public void RotateKey_CyclesThroughValidKeys()
    {
        var groq = new FakeGroq { Status = HttpStatusCode.OK };
        var provider = Create(groq, Config("gsk_a", "gsk_b"));

        var seen = new HashSet<string>();
        for (var i = 0; i < 3; i++)
        {
            seen.Add(provider.GetCurrentKey());
            provider.RotateKey();
        }

        Assert.Equal(new HashSet<string> { "gsk_main", "gsk_a", "gsk_b" }, seen);
        Assert.Equal(3, groq.Calls); // each key validated once
    }

    [Fact]
    public async Task ConcurrentRequests_ValidateOnlyOnce()
    {
        var groq = new FakeGroq { Status = HttpStatusCode.OK };
        var provider = Create(groq);

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => provider.GetCurrentKey())));

        Assert.Equal(1, groq.Calls);
    }
}
