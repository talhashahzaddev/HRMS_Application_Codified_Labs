using System.IdentityModel.Tokens.Jwt;
using AutoMapper;
using HRMS.API.Authorization;
using HRMS.API.Configuration;
using HRMS.API.Hubs;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace HRMS.API.Tests;

public sealed class AuthServiceTests
{
    private const string Email = "ali@test.com";
    private const string Password = "Secret#123";
    private static readonly string PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password, workFactor: 4);

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid RoleId = Guid.NewGuid();

    private readonly Mock<IAuthRepository> _authRepo = new();

    private readonly JwtSettings _jwt = new()
    {
        SecretKey = "unit-test-secret-key-that-is-long-enough-for-hs256!!",
        Issuer = "hrms-tests",
        Audience = "hrms-tests",
        AccessTokenExpirationMinutes = 30,
        RefreshTokenExpirationDays = 7
    };

    private AuthService CreateService() => new(
        _authRepo.Object,
        Mock.Of<IOrganizationService>(),
        Options.Create(_jwt),
        Mock.Of<IMapper>(),
        NullLogger<AuthService>.Instance,
        Mock.Of<ICloudinaryService>(),
        Mock.Of<IEmployeeRepository>(),
        Mock.Of<IEmailService>(),
        Mock.Of<IJobsRepository>(),
        Mock.Of<INotificationRepository>(),
        Mock.Of<IHubContext<PermissionHub>>(),
        new ConfigurationBuilder().Build());

    private static User ValidUser() => new()
    {
        UserId = UserId,
        OrganizationId = OrgId,
        Email = Email,
        PasswordHash = PasswordHash,
        FirstName = "Ali",
        LastName = "Khan",
        Role = new Role { RoleName = "HR Manager" }
    };

    private void SetupValidLogin(User? user = null)
    {
        user ??= ValidUser();
        _authRepo.Setup(r => r.GetUserByEmailAsync(Email)).ReturnsAsync(user);
        _authRepo.Setup(r => r.GetHashedPassword(Email)).ReturnsAsync(PasswordHash);
        _authRepo.Setup(r => r.IsEmailVerifiedAsync(Email)).ReturnsAsync(true);
        _authRepo.Setup(r => r.AuthenticateAsync(Email, PasswordHash)).ReturnsAsync(user);
        _authRepo.Setup(r => r.GetRoleByNameforTokenAsync(It.IsAny<string>(), It.IsAny<Guid?>()))
            .ReturnsAsync(new Role { RoleId = RoleId });
    }

    // ---------- Login ----------

    [Theory]
    [InlineData("", Password)]
    [InlineData(Email, "")]
    [InlineData("  ", "  ")]
    public async Task Login_MissingFields_Throws(string email, string password)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService().AuthenticateAsync(new LoginRequest { Email = email, Password = password }));
    }

    [Fact]
    public async Task Login_UnknownEmail_ThrowsNotFound()
    {
        _authRepo.Setup(r => r.GetUserByEmailAsync(Email)).ReturnsAsync((User?)null);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateService().AuthenticateAsync(new LoginRequest { Email = Email, Password = Password }));
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        SetupValidLogin();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateService().AuthenticateAsync(new LoginRequest { Email = Email, Password = "wrong" }));
        _authRepo.Verify(r => r.UpdateRefreshTokenAsync(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task Login_EmailNotVerified_IsBlocked()
    {
        SetupValidLogin();
        _authRepo.Setup(r => r.IsEmailVerifiedAsync(Email)).ReturnsAsync(false);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService().AuthenticateAsync(new LoginRequest { Email = Email, Password = Password }));
        Assert.Contains("verify", ex.Message);
    }

    [Fact]
    public async Task Login_UserWithoutOrganization_IsBlocked()
    {
        var user = ValidUser();
        user.OrganizationId = null;
        SetupValidLogin(user);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService().AuthenticateAsync(new LoginRequest { Email = Email, Password = Password }));
    }

    [Fact]
    public async Task Login_Success_ReturnsSignedJwtWithExpectedClaims()
    {
        SetupValidLogin();

        var response = await CreateService().AuthenticateAsync(new LoginRequest { Email = Email, Password = Password });

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.Equal("HR Manager", response.RoleName);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);
        Assert.Equal(_jwt.Issuer, jwt.Issuer);
        Assert.Equal(UserId.ToString(), jwt.Claims.First(c => c.Type == "nameid").Value);
        Assert.Equal(OrgId.ToString(), jwt.Claims.First(c => c.Type == CustomClaimTypes.OrganizationId).Value);
        Assert.Equal(RoleId.ToString(), jwt.Claims.First(c => c.Type == CustomClaimTypes.RoleId).Value);
        Assert.InRange(jwt.ValidTo, DateTime.UtcNow.AddMinutes(25), DateTime.UtcNow.AddMinutes(31));

        _authRepo.Verify(r => r.UpdateRefreshTokenAsync(UserId, response.RefreshToken, It.IsAny<DateTime>()), Times.Once);
        _authRepo.Verify(r => r.UpdateLastLoginAsync(UserId), Times.Once);
    }

    [Fact]
    public async Task Login_TwoLogins_ProduceDifferentRefreshTokens()
    {
        SetupValidLogin();
        var svc = CreateService();
        var a = await svc.AuthenticateAsync(new LoginRequest { Email = Email, Password = Password });
        var b = await svc.AuthenticateAsync(new LoginRequest { Email = Email, Password = Password });
        Assert.NotEqual(a.RefreshToken, b.RefreshToken);
    }

    // ---------- Refresh token ----------

    [Fact]
    public async Task Refresh_WithRefreshTokenIssuedAtLogin_ReturnsNewTokens()
    {
        SetupValidLogin();
        var svc = CreateService();
        var login = await svc.AuthenticateAsync(new LoginRequest { Email = Email, Password = Password });

        // Simulate the DB now holding the refresh token that was just issued.
        var stored = ValidUser();
        stored.RefreshToken = login.RefreshToken;
        stored.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        _authRepo.Setup(r => r.GetUserByEmailAsync(Email)).ReturnsAsync(stored);
        _authRepo.Setup(r => r.GetUserByRefreshTokenAsync(login.RefreshToken)).ReturnsAsync(stored);

        var refreshed = await svc.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = login.RefreshToken });

        Assert.False(string.IsNullOrWhiteSpace(refreshed.Token));
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
    }

    [Fact]
    public async Task Refresh_UnknownToken_IsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateService().RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "not-a-real-token" }));
    }

    [Fact]
    public async Task Refresh_ExpiredToken_IsUnauthorized()
    {
        var stored = ValidUser();
        stored.RefreshToken = "abc";
        stored.RefreshTokenExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        _authRepo.Setup(r => r.GetUserByRefreshTokenAsync("abc")).ReturnsAsync(stored);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateService().RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "abc" }));
    }

    [Fact]
    public async Task Logout_ClearsRefreshToken()
    {
        _authRepo.Setup(r => r.UpdateRefreshTokenAsync(UserId, string.Empty, It.IsAny<DateTime>())).ReturnsAsync(true);
        Assert.True(await CreateService().LogoutAsync(UserId));
    }

    // ---------- Change password ----------

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Throws()
    {
        _authRepo.Setup(r => r.GetUserByIdAsync(UserId)).ReturnsAsync(ValidUser());
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().ChangePasswordAsync(UserId,
            new ChangePasswordRequest { CurrentPassword = "wrong", NewPassword = "NewPass#1" }));
    }

    [Fact]
    public async Task ChangePassword_SameAsOld_Throws()
    {
        _authRepo.Setup(r => r.GetUserByIdAsync(UserId)).ReturnsAsync(ValidUser());
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().ChangePasswordAsync(UserId,
            new ChangePasswordRequest { CurrentPassword = Password, NewPassword = Password }));
        Assert.Contains("different", ex.Message);
    }

    [Fact]
    public async Task ChangePassword_Valid_StoresNewBcryptHash()
    {
        _authRepo.Setup(r => r.GetUserByIdAsync(UserId)).ReturnsAsync(ValidUser());
        string? savedHash = null;
        _authRepo.Setup(r => r.ChanegPasswordAsync(UserId, It.IsAny<string>()))
            .Callback<Guid, string>((_, h) => savedHash = h)
            .ReturnsAsync(true);

        Assert.True(await CreateService().ChangePasswordAsync(UserId,
            new ChangePasswordRequest { CurrentPassword = Password, NewPassword = "NewPass#1" }));
        Assert.NotNull(savedHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass#1", savedHash));
    }

    // ---------- Reset password ----------

    [Fact]
    public async Task ResetPassword_UsedToken_IsRejected()
    {
        _authRepo.Setup(r => r.GetPasswordResetTokenAsync("t")).ReturnsAsync(new PasswordResetToken
        {
            used = true, expiresat = DateTime.UtcNow.AddHours(1), userid = UserId
        });
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateService().ResetPasswordAsync(new ResetPasswordRequest { token = "t", password = "x" }));
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_IsRejected()
    {
        _authRepo.Setup(r => r.GetPasswordResetTokenAsync("t")).ReturnsAsync(new PasswordResetToken
        {
            used = false, expiresat = DateTime.UtcNow.AddMinutes(-1), userid = UserId
        });
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateService().ResetPasswordAsync(new ResetPasswordRequest { token = "t", password = "x" }));
    }

    [Fact]
    public async Task ResetPassword_Valid_UpdatesPasswordAndBurnsToken()
    {
        _authRepo.Setup(r => r.GetPasswordResetTokenAsync("t")).ReturnsAsync(new PasswordResetToken
        {
            used = false, expiresat = DateTime.UtcNow.AddHours(1), userid = UserId
        });
        _authRepo.Setup(r => r.GetUserByIdAsync(UserId)).ReturnsAsync(ValidUser());

        Assert.True(await CreateService().ResetPasswordAsync(new ResetPasswordRequest { token = "t", password = "NewPass#1" }));
        _authRepo.Verify(r => r.UpdateUserPasswordAsync(UserId, It.IsAny<string>()), Times.Once);
        _authRepo.Verify(r => r.MarkResetTokenAsUsedAsync("t"), Times.Once);
    }

    [Fact]
    public void GenerateEmployeeCode_HasExpectedFormat()
    {
        var code = CreateService().GenerateEmployeeCode();
        Assert.Matches(@"^EMP\d{4}$", code);
    }
}
