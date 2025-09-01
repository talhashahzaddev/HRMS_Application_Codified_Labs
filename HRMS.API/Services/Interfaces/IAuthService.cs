using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> AuthenticateAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> LogoutAsync(Guid userId);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, Guid organizationId);
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    // New missing methods
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    
    // Debug methods for testing
    Task<string> GetHashedPasswordAsync(string email);
    Task<User?> GetUserForTestingAsync(string email, string hashedPassword);
    Task<AuthResponse> CreateTestTokenAsync(User user);
}
