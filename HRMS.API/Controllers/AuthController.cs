using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using HRMS.API.Models.Entities;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ServiceResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.AuthenticateAsync(request);
            
            // Debug: Log the token to see what claims it contains
            _logger.LogInformation("Generated JWT Token: {Token}", response.Token);
            
            return Ok(new ServiceResponse<AuthResponse>
            {
                Success = true,
                Message = "Login successful",
                Data = response
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email: {Email}", request.Email);
            return StatusCode(500, new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = "An error occurred during login"
            });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ServiceResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request);
            return Ok(new ServiceResponse<AuthResponse>
            {
                Success = true,
                Message = "Token refreshed successfully",
                Data = response
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return StatusCode(500, new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = "An error occurred during token refresh"
            });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<bool>>> Logout()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _authService.LogoutAsync(userId);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Logout successful",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred during logout"
            });
        }
    }

    [HttpPost("users")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<UserDto>>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var user = await _authService.CreateUserAsync(request, organizationId);
            
            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, new ServiceResponse<UserDto>
            {
                Success = true,
                Message = "User created successfully",
                Data = user
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ServiceResponse<UserDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User creation failed");
            return StatusCode(500, new ServiceResponse<UserDto>
            {
                Success = false,
                Message = "An error occurred while creating user"
            });
        }
    }

    [HttpGet("users/{id}")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<UserDto>>> GetUser(Guid id)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new ServiceResponse<UserDto>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            return Ok(new ServiceResponse<UserDto>
            {
                Success = true,
                Data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, new ServiceResponse<UserDto>
            {
                Success = false,
                Message = "An error occurred while retrieving user"
            });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<UserDto>>> GetCurrentUser()
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _authService.GetUserByIdAsync(userId);
            
            return Ok(new ServiceResponse<UserDto>
            {
                Success = true,
                Data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user");
            return StatusCode(500, new ServiceResponse<UserDto>
            {
                Success = false,
                Message = "An error occurred while retrieving user information"
            });
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<ServiceResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(new ServiceResponse<AuthResponse>
            {
                Success = true,
                Message = "Registration successful",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for email: {Email}", request.Email);
            return StatusCode(500, new ServiceResponse<AuthResponse>
            {
                Success = false,
                Message = "An error occurred during registration"
            });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ServiceResponse<bool>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var result = await _authService.ForgotPasswordAsync(request);
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Password reset email sent successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Forgot password failed for email: {Email}", request.Email);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while processing password reset request"
            });
        }
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<UserDto>>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _authService.UpdateProfileAsync(userId, request);
            
            return Ok(new ServiceResponse<UserDto>
            {
                Success = true,
                Message = "Profile updated successfully",
                Data = user
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ServiceResponse<UserDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Profile update failed for user: {UserId}", GetCurrentUserId());
            return StatusCode(500, new ServiceResponse<UserDto>
            {
                Success = false,
                Message = "An error occurred while updating profile"
            });
        }
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _authService.ChangePasswordAsync(userId, request);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Password changed successfully",
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password change failed for user: {UserId}", GetCurrentUserId());
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while changing password"
            });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            _logger.LogError("User ID claim not found in JWT token");
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogError("Invalid user ID format in JWT token: {UserId}", userIdClaim);
            throw new UnauthorizedAccessException("Invalid user ID format");
        }
        
        return userId;
    }

    private Guid GetCurrentOrganizationId()
    {
        var orgIdClaim = User.FindFirstValue("organizationId");
        
        if (string.IsNullOrEmpty(orgIdClaim))
        {
            _logger.LogError("Organization ID claim not found in JWT token");
            throw new UnauthorizedAccessException("Organization ID not found in token");
        }
        
        if (!Guid.TryParse(orgIdClaim, out var organizationId))
        {
            _logger.LogError("Invalid organization ID format in JWT token: {OrganizationId}", orgIdClaim);
            throw new UnauthorizedAccessException("Invalid organization ID format");
        }
        
        return organizationId;
    }

    [HttpGet("debug-claims")]
    [Authorize]
    public IActionResult DebugClaims()
    {
        var claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();
        
        // Debug specific claim lookups
        var orgIdDirect = User.FindFirstValue("organizationId");
        var orgIdHttp = User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/organizationId");
        var orgIdCustom = User.FindFirstValue("http://schemas.microsoft.com/ws/2008/06/identity/claims/organizationId");
        
        return Ok(new ServiceResponse<object>
        {
            Success = true,
            Message = "JWT Claims retrieved",
            Data = new { 
                Claims = claims,
                DirectOrgId = orgIdDirect,
                HttpOrgId = orgIdHttp,
                CustomOrgId = orgIdCustom,
                TotalClaims = claims.Count
            }
        });
    }

    [HttpPost("test-auth")]
    public async Task<IActionResult> TestAuth([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("=== TESTING AUTHENTICATION FLOW ===");
            
            // Step 1: Check if user exists
            var hashedPassword = await _authService.GetHashedPasswordAsync(request.Email);
            _logger.LogInformation("Step 1 - Found hashed password: {Found}", !string.IsNullOrEmpty(hashedPassword));
            
            // Step 2: Test password verification
            var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, hashedPassword);
            _logger.LogInformation("Step 2 - Password valid: {Valid}", passwordValid);
            
            if (!passwordValid)
            {
                return BadRequest("Invalid password");
            }

            // Step 3: Get user from database
            var user = await _authService.GetUserForTestingAsync(request.Email, hashedPassword);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            _logger.LogInformation("Step 3 - User found: UserId={UserId}, OrganizationId={OrganizationId}, Email={Email}", 
                user.UserId, user.OrganizationId, user.Email);

            return Ok(new
            {
                Message = "Authentication test completed",
                User = new
                {
                    UserId = user.UserId,
                    OrganizationId = user.OrganizationId,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    RoleName = user.Role?.RoleName,
                    OrganizationName = user.Organization?.Name
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test authentication failed");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("test-hardcoded-token")]
    public async Task<IActionResult> TestHardcodedToken()
    {
        try
        {
            // Create a hardcoded user object to test token generation
            var testUser = new User
            {
                UserId = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                OrganizationId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Email = "admin@demo.company.com",
                FirstName = "System",
                LastName = "Administrator",
                IsActive = true,
                Role = new Role { RoleName = "Super Admin" },
                Organization = new Organization { Name = "Demo Company Ltd." }
            };

            _logger.LogInformation("=== TESTING HARDCODED TOKEN GENERATION ===");
            _logger.LogInformation("Test User: UserId={UserId}, OrganizationId={OrganizationId}", 
                testUser.UserId, testUser.OrganizationId);

            // Generate token directly
            var response = await _authService.CreateTestTokenAsync(testUser);
            
            return Ok(new
            {
                Message = "Hardcoded token test completed",
                Token = response.Token,
                User = new
                {
                    testUser.UserId,
                    testUser.OrganizationId,
                    testUser.Email,
                    testUser.FirstName,
                    testUser.LastName
                },
                AuthResponse = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hardcoded token test failed");
            return StatusCode(500, ex.Message);
        }
    }
}
