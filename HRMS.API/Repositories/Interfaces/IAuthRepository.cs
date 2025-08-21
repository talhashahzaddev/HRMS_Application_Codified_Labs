using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IAuthRepository
{
    Task<User?> AuthenticateAsync(string email, string passwordHash);
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User> CreateUserAsync(User user);
    Task<bool> UpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiresAt);
    Task<bool> UpdateLastLoginAsync(Guid userId);
    Task<string> GetHashedPassword(string email);
    Task<Organization> CreateOrganizationAsync(string name, string? domain = null, string subscriptionType = "basic");
    Task<Role?> GetRoleByNameAsync(Guid organizationId, string roleName);
    Task<List<Role>> GetOrganizationRolesAsync(Guid organizationId);
}
