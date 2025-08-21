using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> AuthenticateAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> LogoutAsync(Guid userId);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, Guid organizationId);
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
}
