using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HRMS.API.Services;

public class PlatformAdminAuthService : IPlatformAdminAuthService
{
    private readonly IPlatformAdminRepository _adminRepository;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<PlatformAdminAuthService> _logger;

    public PlatformAdminAuthService(
        IPlatformAdminRepository adminRepository,
        IOptions<JwtSettings> jwtSettings,
        ILogger<PlatformAdminAuthService> logger)
    {
        _adminRepository = adminRepository;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<ServiceResponse<PlatformAdminAuthResponse>> AuthenticateAsync(PlatformAdminLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new ServiceResponse<PlatformAdminAuthResponse>
            {
                Success = false,
                Message = "Email and password are required"
            };
        }

        var admin = await _adminRepository.GetByEmailAsync(request.Email);
        if (admin == null)
        {
            _logger.LogWarning("Platform admin login failed: email not found - {Email}", request.Email);
            return new ServiceResponse<PlatformAdminAuthResponse>
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
        {
            _logger.LogWarning("Platform admin login failed: invalid password - {Email}", request.Email);
            return new ServiceResponse<PlatformAdminAuthResponse>
            {
                Success = false,
                Message = "Invalid email or password"
            };
        }

        // Generate JWT token
        var token = GenerateToken(admin);
        var expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        // Update last login
        await _adminRepository.UpdateLastLoginAsync(admin.AdminId);

        _logger.LogInformation("Platform admin authenticated: {Email}", admin.Email);

        return new ServiceResponse<PlatformAdminAuthResponse>
        {
            Success = true,
            Message = "Login successful",
            Data = new PlatformAdminAuthResponse
            {
                AdminId = admin.AdminId,
                Email = admin.Email,
                FullName = admin.FullName,
                Role = "Platform Admin",
                Token = token,
                TokenExpiration = expiration
            }
        };
    }

    private string GenerateToken(Models.Entities.PlatformAdmin admin)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, admin.AdminId.ToString()),
            new Claim(ClaimTypes.Email, admin.Email),
            new Claim(ClaimTypes.Name, admin.FullName),
            new Claim(ClaimTypes.Role, "Platform Admin"),
            new Claim("adminId", admin.AdminId.ToString()),
            new Claim("isPlatformAdmin", "true")
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
