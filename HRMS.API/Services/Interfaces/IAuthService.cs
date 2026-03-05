using DocumentFormat.OpenXml.Spreadsheet;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using Microsoft.AspNetCore.Identity.Data;

namespace HRMS.API.Services.Interfaces;

public interface IAuthService
{
    Task<bool> ResetPasswordAsync(HRMS.API.Models.DTOs.ResetPasswordRequest requst);

    Task<AuthResponse> AuthenticateAsync(HRMS.API.Models.DTOs.LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> LogoutAsync(Guid userId);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, Guid organizationId);
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    // New missing methods
    Task<AuthResponse> RegisterAsync(HRMS.API.Models.DTOs.RegisterRequest request);
    Task<bool> ForgotPasswordAsync(HRMS.API.Models.DTOs.ForgotPasswordRequest request);
    Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);// bool changoe in response
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, Guid organizationId);
    Task<List<RoleDto>> GetOrganizationRolesAsync(Guid organizationId);
    Task<List<MenuDto>> GetMenusAsync();
    Task<CreateRoleResponse> Create_RoleAsync(Create_RoleRequest request, Guid? organizationId);
    Task<List<RoleDetailResponse>> GetRolesByOrganizationAsync(Guid? organizationId);
    Task<bool> UpdateUserRoleAsync(Guid userId, Guid roleId);
    
    // Debug methods for testing
    Task<string> GetHashedPasswordAsync(string email);
    Task<User?> GetUserForTestingAsync(string email, string hashedPassword);
    Task<AuthResponse> CreateTestTokenAsync(User user);
    Task<bool> VerifyEmailAsync(string token);
}
