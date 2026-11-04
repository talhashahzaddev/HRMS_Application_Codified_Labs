using HRMS.API.Services;
using Xunit;

namespace HRMS.API.Tests;

public sealed class LoginLinkResolverTests
{
    private const string Live = "https://login.briskpeople.com/login";

    [Theory]
    [InlineData("http://aree.localhost:4201", "http://login.localhost:4201/login")]
    [InlineData("http://localhost:4201", "http://localhost:4201/login")]
    [InlineData("http://127.0.0.1:4200", "http://127.0.0.1:4200/login")]
    public void Local_frontend_gets_local_login_page(string origin, string expected) =>
        Assert.Equal(expected, LoginLinkResolver.Resolve(origin, Live));

    [Theory]
    [InlineData("https://aree.briskpeople.com")]
    [InlineData("https://frontend-hrms-wine.vercel.app")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not a url")]
    [InlineData("file:///etc/passwd")]
    public void Live_or_unknown_origin_gets_configured_link(string? origin) =>
        Assert.Equal(Live, LoginLinkResolver.Resolve(origin, Live));
}
