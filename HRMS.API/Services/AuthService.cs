using AutoMapper;
using BCrypt.Net;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HRMS.API.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IOrganizationService _organizationService;
    private readonly JwtSettings _jwtSettings;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService _emailService;
    private readonly ICloudinaryService _cloudinaryService;
    public AuthService(
        IAuthRepository authRepository,
        IOrganizationService organizationService,
        IOptions<JwtSettings> jwtSettings,
        IMapper mapper,
        ILogger<AuthService> logger,
        ICloudinaryService cloudinaryService,
         IEmailService emailService)
    {
        _authRepository = authRepository;
        _organizationService = organizationService;
        _jwtSettings = jwtSettings.Value;
        _mapper = mapper;
        _logger = logger;
        _emailService = emailService;
        _cloudinaryService = cloudinaryService;

    }

    public async Task<AuthResponse> AuthenticateAsync(HRMS.API.Models.DTOs.LoginRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Email)) throw new ArgumentException("Email is required", nameof(request.Email));
        if (string.IsNullOrWhiteSpace(request.Password)) throw new ArgumentException("Password is required", nameof(request.Password));
        _logger.LogInformation("Authentication attempt for email: {Email}", request.Email);

        var userEmail = await _authRepository.GetUserByEmailAsync(request.Email);

        if (userEmail == null)
        {
            _logger.LogWarning("Email not found: {Email}", request.Email);
            throw new KeyNotFoundException("Email not found");
        }

        string hashedPassword = await _authRepository.GetHashedPassword(request.Email);
        var passwordHash = BCrypt.Net.BCrypt.Verify(request.Password, hashedPassword);
        if (!passwordHash)
        {
            _logger.LogWarning("Invalid password for email: {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid password");
        }
        User? user = await _authRepository.AuthenticateAsync(request.Email, hashedPassword);

        if (user == null)
        {
            _logger.LogWarning("Login failed after credential verification for email: {Email}", request.Email);
            throw new InvalidOperationException("Login failed");
        }

        // Debug: Log user details
        _logger.LogInformation("Authenticated user: UserId={UserId}, OrganizationId={OrganizationId}, Email={Email}",
            user.UserId, user.OrganizationId, user.Email);

        var (accessToken, refreshToken) = await GenerateTokensAsync(user);

        // Update refresh token in database
        await _authRepository.UpdateRefreshTokenAsync(user.UserId, refreshToken, DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays));

        // Update last login
        await _authRepository.UpdateLastLoginAsync(user.UserId);

        _logger.LogInformation("User {UserId} authenticated successfully", user.UserId);

        return new AuthResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            RoleName = user.Role?.RoleName ?? "Employee",
            OrganizationName = user.Organization?.Name ?? "Unknown",
            Domain = user.Organization?.Domain,
            Token = accessToken,
            RefreshToken = refreshToken,
            TokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.RefreshToken)) throw new ArgumentException("Refresh token is required", nameof(request.RefreshToken));
        ClaimsPrincipal? principal;
        try
        {
            principal = GetPrincipalFromExpiredToken(request.RefreshToken);
        }
        catch (SecurityTokenException)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }
        if (principal?.Identity?.Name == null)
            throw new UnauthorizedAccessException("Invalid refresh token");

        var userEmail = principal.Identity.Name;
        var user = await _authRepository.GetUserByEmailAsync(userEmail);

        if (user?.RefreshToken != request.RefreshToken || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        var (newAccessToken, newRefreshToken) = await GenerateTokensAsync(user);

        await _authRepository.UpdateRefreshTokenAsync(user.UserId, newRefreshToken, DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays));

        return new AuthResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            RoleName = user.Role?.RoleName ?? "Employee",
            OrganizationName = user.Organization?.Name ?? "Unknown",
            Token = newAccessToken,
            RefreshToken = newRefreshToken,
            TokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
        };
    }

    public async Task<bool> LogoutAsync(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required", nameof(userId));
        return await _authRepository.UpdateRefreshTokenAsync(userId, string.Empty, DateTime.UtcNow);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, Guid organizationId)
    {
        // Check if user already exists
        var existingUser = await _authRepository.GetUserByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            OrganizationId = organizationId,
            Email = request.Email,
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = new Role { RoleName = request.RoleName }
        };

        var createdUser = await _authRepository.CreateUserAsync(user);
        return _mapper.Map<UserDto>(createdUser);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _authRepository.GetUserByIdAsync(userId);
        return user != null ? _mapper.Map<UserDto>(user) : null;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _authRepository.GetUserByIdAsync(userId);
        if (user == null)
            return false;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return false;

        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        // Update password logic would go here - for now, return true

        return true;
    }

    // New registration method that creates organization and SuperAdmin user
    public async Task<AuthResponse> RegisterAsync(HRMS.API.Models.DTOs.RegisterRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Email)) throw new ArgumentException("Email is required", nameof(request.Email));
        if (string.IsNullOrWhiteSpace(request.Password)) throw new ArgumentException("Password is required", nameof(request.Password));
        if (string.IsNullOrWhiteSpace(request.FirstName)) throw new ArgumentException("First name is required", nameof(request.FirstName));
        if (string.IsNullOrWhiteSpace(request.LastName)) throw new ArgumentException("Last name is required", nameof(request.LastName));
        if (string.IsNullOrWhiteSpace(request.CompanyName)) throw new ArgumentException("Company name is required", nameof(request.CompanyName));
        _logger.LogInformation("Registration attempt for email: {Email} and organization: {CompanyName}", request.Email, request.CompanyName);

        // Check if user already exists
        var existingUser = await _authRepository.GetUserByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        // Step 1: Create organization
        var createOrgDto = new CreateOrganizationDTO
        {
            Name = request.CompanyName,
            Domain = !string.IsNullOrEmpty(request.Website) ? ExtractDomainFromUrl(request.Website) : null,
            SubscriptionType = GetSubscriptionTypeFromCompanySize(request.CompanySize),
            Currency = !string.IsNullOrWhiteSpace(request.Currency) ? request.Currency.ToUpperInvariant() : "USD"
        };

        var orgResponse = await _organizationService.CreateOrganizationAsync(createOrgDto);
        if (!orgResponse.Success || orgResponse.Data == null)
        {
            throw new InvalidOperationException("Failed to create organization: " + orgResponse.Message);
        }

        var organizationId = orgResponse.Data.OrganizationId;
        _logger.LogInformation("Organization {OrganizationId} created successfully", organizationId);

        // Step 2: Create SuperAdmin user for the organization
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var emailVerificationToken = Guid.NewGuid().ToString("N");
        var user = new User
        {
            OrganizationId = organizationId,
            Email = request.Email,
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = new Role { RoleName = "Super Admin" }, // Always SuperAdmin for registration
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            IsEmailVerified = false,
            EmailVerificationToken = emailVerificationToken,
            EmailVerificationExpiry = DateTime.UtcNow.AddHours(24)
        };

        var createdUser = await _authRepository.CreateUserAsync(user);

        _logger.LogInformation("SuperAdmin user {UserId} created successfully for organization {OrganizationId}", createdUser.UserId, organizationId);
        
        createdUser.employeecode = GenerateEmployeeCode();
        
        await _authRepository.adminIntoEmployeeAsync(createdUser);

        var superadminEmployeeid = await _authRepository.GetEmployeeIdByEmailAsync(createdUser.Email);

        var position = await _authRepository.CreatePositionAsync(createdUser.OrganizationId.Value, createdUser.RoleId.Value, "Super Admin");

        await _authRepository.AdminIntoEmployeeDetailsAsync(superadminEmployeeid, position);

        await _emailService.SendVerificationEmailAsync(request.Email, request.FirstName, emailVerificationToken);
        
        return new AuthResponse
        {
            Token = null,
            RefreshToken = null,
        };
    }
    private string? ExtractDomainFromUrl(string url)
    {
        try
        {
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }
            var uri = new Uri(url);
            return uri.Host.Replace("www.", "");
        }
        catch
        {
            return null;
        }
    }

    private string GetSubscriptionTypeFromCompanySize(string companySize)
    {
        return companySize switch
        {
            "1-10" => "basic",
            "11-50" => "standard",
            "51-200" => "professional",
            "201-500" => "enterprise",
            "501-1000" => "enterprise",
            "1000+" => "enterprise",
            _ => "basic"
        };
    }


    public async Task<bool> ForgotPasswordAsync(HRMS.API.Models.DTOs.ForgotPasswordRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Email)) throw new ArgumentException("Email is required", nameof(request.Email));
        _logger.LogInformation("Forgot password request for email: {Email}", request.Email);

        var user = await _authRepository.GetUserByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Forgot password attempted for non-existent email: {Email}", request.Email);
            return true; // Don't reveal that user doesn't exist
        }

        // 1️⃣ Generate unique reset token
        var resetToken = Guid.NewGuid().ToString();

        // 2️⃣ Set expiration (15 minutes from now — UTC)
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        // 3️⃣ Save to DB
        await _authRepository.SavePasswordResetTokenAsync(user.UserId.Value, resetToken, expiresAt);

        // 4️⃣ Generate link for frontend reset page
        //var resetLink = $"http://localhost:4200/reset-password?token={resetToken}";
        var resetLink = $"https://login.briskpeople.com/reset-password?token={resetToken}";

        // 5️⃣ Send email
        await _emailService.SendPasswordResetEmailAsync(user.Email, user.FirstName, resetLink);

        _logger.LogInformation("Password reset token generated and email sent for {Email}", request.Email);

        return true;
    }

    //Reset Password


    public async Task<bool> ResetPasswordAsync(HRMS.API.Models.DTOs.ResetPasswordRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        _logger.LogInformation("Reset password attempt using token: {Token}", request.token);

        if (string.IsNullOrWhiteSpace(request.token) || string.IsNullOrWhiteSpace(request.password))
        {
            _logger.LogWarning("Reset password failed - token or password missing");
            throw new ArgumentException("Token or password cannot be empty");
        }

        // 1) Fetch token record
        var tokenRecord = await _authRepository.GetPasswordResetTokenAsync(request.token);
        if (tokenRecord == null)
        {
            _logger.LogWarning("Reset password failed - token not found: {Token}", request.token);
            throw new UnauthorizedAccessException("Invalid or expired token");
        }

        // 2) Validate token (expiry & used)
        if (tokenRecord.used)
        {
            _logger.LogWarning("Reset password failed - token already used: {Token}", request.token);
            throw new UnauthorizedAccessException("Token has already been used");
        }

        if (tokenRecord.expiresat <= DateTime.UtcNow)
        {
            _logger.LogWarning("Reset password failed - token expired: {Token}", request.token);
            throw new UnauthorizedAccessException("Token has expired");
        }


        // 3) Load user
        var user = await _authRepository.GetUserByIdAsync(tokenRecord.userid);
        if (user == null)
        {
            _logger.LogError("Reset password failed - user not found for token: {Token}", request.token);
            throw new KeyNotFoundException("User not found for this token");
        }

        // 4) Hash new password and update user
        var newHashedPassword = BCrypt.Net.BCrypt.HashPassword(request.password);
        await _authRepository.UpdateUserPasswordAsync(tokenRecord.userid, newHashedPassword);

        // 5) Mark token as used
        await _authRepository.MarkResetTokenAsUsedAsync(request.token);



        _logger.LogInformation("Password reset successful for user: {UserId}", user.UserId);
        return true;
    }

    public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required", nameof(userId));
        if (request == null) throw new ArgumentNullException(nameof(request));

        //Update user Table

        var UpdateUserTable = await _authRepository.UpdateProfile_UserAsync(userId, request);


        var employee = await _authRepository.GetEmployeeByIdAsync(userId);
        if (employee == null)
            throw new KeyNotFoundException($"Employee with ID {userId} not found");

        // Phone number validation
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            // Must start with + and contain only digits after that
            if (!request.Phone.StartsWith("+"))
            {
                throw new InvalidOperationException(
                    "Phone number must include country code, e.g. +923001234567"
                );
            }

            // Remove '+' and check digits only
            var digits = request.Phone.Substring(1);

            if (!digits.All(char.IsDigit))
            {
                throw new InvalidOperationException(
                    "Phone number must contain digits only after country code"
                );
            }

            // E.164 max length = 15 digits
            if (digits.Length > 15)
            {
                throw new InvalidOperationException(
                    "Phone number cannot exceed 15 digits"
                );
            }

            // Reasonable minimum length
            if (digits.Length < 8)
            {
                throw new InvalidOperationException(
                    "Phone number is too short"
                );
            }
        }
        //_mapper.Map(request, employee);
        //Update Employee table
        var updateEmployeetable = await _authRepository.UpdateProfile_EmployeeAsync(employee.EmployeeId, request);

        //Update Employee Details table

        var updateEmployeeDetails = await _authRepository.UpdateProfile_EmployeeDetailsAsync(employee.EmployeeId, request);

        return updateEmployeetable && updateEmployeeDetails;

    }



    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required", nameof(userId));
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.CurrentPassword)) throw new ArgumentException("Current password is required", nameof(request.CurrentPassword));
        if (string.IsNullOrWhiteSpace(request.NewPassword)) throw new ArgumentException("New password is required", nameof(request.NewPassword));
        _logger.LogInformation("Change password attempt for user: {UserId}", userId);

        var user = await _authRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Invalid current password for user {UserId}", userId);
            throw new InvalidOperationException("Current password is incorrect");
        }

        // Check if new password is same as old password
        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
        {
            _logger.LogWarning("New password must be different from old password for user {UserId}", userId);
            throw new InvalidOperationException("New password must be different from the old password.");
        }


        // Hash new password
        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        // Update password in database
        await _authRepository.ChanegPasswordAsync(userId, newPasswordHash);

        _logger.LogInformation("Password changed successfully for user {UserId}", userId);
        return true;
    }
    public async Task<bool> VerifyEmailAsync(string token)
    {
        var result = await _authRepository.VerifyEmail(token);
        return result;
    }

    private async Task<(string accessToken, string refreshToken)> GenerateTokensAsync(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

        // Debug logging
        _logger.LogInformation("Generating token for user: {UserId}, OrganizationId: {OrganizationId}",
            user.UserId, user.OrganizationId);

        // Check if OrganizationId is valid
        if (user.OrganizationId == Guid.Empty)
        {
            _logger.LogError("User {UserId} has empty OrganizationId", user.UserId);
            throw new InvalidOperationException("User does not have a valid organization ID");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new("organizationId", user.OrganizationId.ToString()),
            new(ClaimTypes.Role, user.Role?.RoleName ?? "Employee"),
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };

        // Debug: Log all claims
        _logger.LogInformation("JWT Claims: {Claims}",
            string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}")));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(token);

        var refreshToken = GenerateRefreshToken();

        return (accessToken, refreshToken);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.SecretKey)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);

        if (validatedToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }

    // Debug methods for testing
    public async Task<string> GetHashedPasswordAsync(string email)
    {
        return await _authRepository.GetHashedPassword(email);
    }

    public async Task<User?> GetUserForTestingAsync(string email, string hashedPassword)
    {
        return await _authRepository.AuthenticateAsync(email, hashedPassword);
    }

    public async Task<AuthResponse> CreateTestTokenAsync(User user)
    {
        var (accessToken, refreshToken) = await GenerateTokensAsync(user);

        return new AuthResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            RoleName = user.Role?.RoleName ?? "Employee",
            OrganizationName = user.Organization?.Name ?? "Unknown",
            Token = accessToken,
            RefreshToken = refreshToken,
            TokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
        };
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, Guid organizationId)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization ID is required", nameof(organizationId));
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.RoleName)) throw new ArgumentException("Role name is required", nameof(request.RoleName));
        // Validate role name uniqueness within organization
        var existingRole = await _authRepository.GetRoleByNameAsync(organizationId, request.RoleName);
        if (existingRole != null)
        {
            throw new InvalidOperationException($"Role '{request.RoleName}' already exists in this organization.");
        }

        var role = new Role
        {
            RoleId = Guid.NewGuid(),
            OrganizationId = organizationId,
            RoleName = request.RoleName,
            Description = request.Description,
            Permissions = request.Permissions ?? new Dictionary<string, object>(),
            IsSystemRole = request.IsSystemRole,
            CreatedAt = DateTime.UtcNow
        };

        var createdRole = await _authRepository.CreateRoleAsync(role);
        return MapToRoleDto(createdRole);
    }

    public async Task<List<RoleDto>> GetOrganizationRolesAsync(Guid organizationId)
    {
        if (organizationId == Guid.Empty) throw new ArgumentException("Organization ID is required", nameof(organizationId));
        var roles = await _authRepository.GetOrganizationRolesAsync(organizationId);
        return roles.Select(MapToRoleDto).ToList();
    }

    public async Task<bool> UpdateUserRoleAsync(Guid userId, Guid roleId)
    {
        return await _authRepository.UpdateUserRoleAsync(userId, roleId);
    }

    public string GenerateEmployeeCode()
    {
        var random = new Random();
        int number = random.Next(1000, 9999);
        return $"EMP{number}";
    }


    private static RoleDto MapToRoleDto(Role role)
    {
        return new RoleDto
        {
            RoleId = role.RoleId ?? Guid.Empty,
            OrganizationId = role.OrganizationId ?? Guid.Empty,
            RoleName = role.RoleName ?? string.Empty,
            Description = role.Description ?? string.Empty,
            Permissions = role.Permissions ?? new Dictionary<string, object>(),
            IsSystemRole = role.IsSystemRole ?? false,
            CreatedAt = role.CreatedAt ?? DateTime.UtcNow
        };
    }
}
