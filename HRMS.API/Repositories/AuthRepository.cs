using HRMS.API.Configuration;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using System.Data;
using Dapper;

namespace HRMS.API.Repositories;

public class AuthRepository : BaseRepository, IAuthRepository
{
    public AuthRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<User?> AuthenticateAsync(string email, string passwordHash)
    {
        var sql = "SELECT * FROM auth.fn_authenticate_user(@email, @passwordHash)";
        var result = await QuerySingleOrDefaultAsync<User?>(sql, new { email, passwordHash });
        
        if (result == null) return null;

        //var data= new User
        //{
        //    UserId = result.UserId,
        //    OrganizationId = result.OrganizationId,
        //    Email = result.Email,
        //    FirstName = result.FirstName,
        //    LastName = result.LastName,
        //    IsActive = result.IsActive,
        //    Role = new Role 
        //    { 
        //        RoleName = result?.Role.RoleName,
        //        Permissions = result?.Role.Permissions
        //    },
        //    Organization = new Organization 
        //    { 
        //        Name = result?.Organization.Name 
        //    }
        //};
        return result;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        var sql = @"
            SELECT u.user_id, u.organization_id, u.email, u.first_name, u.last_name, 
                   u.is_active, u.last_login_at, u.created_at,
                   r.role_name, r.permissions,
                   o.name as organization_name
            FROM auth.users u
            INNER JOIN auth.roles r ON u.role_id = r.role_id
            INNER JOIN auth.organizations o ON u.organization_id = o.organization_id
            WHERE u.user_id = @userId";

        var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new { userId });
        if (result == null) return null;

        return MapToUser(result);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var sql = @"
            SELECT u.user_id, u.organization_id, u.email, u.first_name, u.last_name, 
                   u.is_active, u.last_login_at, u.created_at,
                   r.role_name, r.permissions,
                   o.name as organization_name
            FROM auth.users u
            INNER JOIN auth.roles r ON u.role_id = r.role_id
            INNER JOIN auth.organizations o ON u.organization_id = o.organization_id
            WHERE u.email = @email";

        var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new { email });
        if (result == null) return null;

        return MapToUser(result);
    }

    public async Task<User> CreateUserAsync(User user)
    {
        var userId = await ExecuteScalarAsync<Guid>(
            "SELECT auth.fn_create_user(@organizationId, @email, @passwordHash, @firstName, @lastName, @roleName)",
            new
            {
                organizationId = user.OrganizationId,
                email = user.Email,
                passwordHash = user.PasswordHash,
                firstName = user.FirstName,
                lastName = user.LastName,
                roleName = user.Role?.RoleName ?? "Employee"
            });

        user.UserId = userId;
        return user;
    }

    public async Task<bool> UpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiresAt)
    {
        var sql = @"
            UPDATE auth.users 
            SET refresh_token = @refreshToken, 
                refresh_token_expires_at = @expiresAt,
                updated_at = CURRENT_TIMESTAMP
            WHERE user_id = @userId";

        var affected = await ExecuteAsync(sql, new { userId, refreshToken, expiresAt });
        return affected > 0;
    }
    public async Task<string> GetHashedPassword(string email)
    {
        var sql = "SELECT password_hash FROM auth.users WHERE email = @Email";
        var result = await QuerySingleOrDefaultAsync<string>(sql, new { Email = email });
        return result ?? string.Empty;
    }
    public async Task<bool> UpdateLastLoginAsync(Guid userId)
    {
        var sql = @"
            UPDATE auth.users 
            SET last_login_at = CURRENT_TIMESTAMP,
                updated_at = CURRENT_TIMESTAMP
            WHERE user_id = @userId";

        var affected = await ExecuteAsync(sql, new { userId });
        return affected > 0;
    }

    public async Task<Organization> CreateOrganizationAsync(string name, string? domain = null, string subscriptionType = "basic")
    {
        var organizationId = await ExecuteScalarAsync<Guid>(
            "SELECT auth.fn_create_organization(@name, @domain, @subscriptionType)",
            new { name, domain, subscriptionType });

        return new Organization
        {
            OrganizationId = organizationId,
            Name = name,
            Domain = domain,
            SubscriptionType = subscriptionType
        };
    }

    public async Task<Role?> GetRoleByNameAsync(Guid organizationId, string roleName)
    {
        var sql = @"
            SELECT role_id, organization_id, role_name, description, permissions, is_system_role, created_at
            FROM auth.roles 
            WHERE organization_id = @organizationId AND role_name = @roleName";

        var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new { organizationId, roleName });
        if (result == null) return null;

        return new Role
        {
            RoleId = result.role_id,
            OrganizationId = result.organization_id,
            RoleName = result.role_name,
            Description = result.description,
            Permissions = result.permissions,
            IsSystemRole = result.is_system_role,
            CreatedAt = result.created_at
        };
    }

    public async Task<List<Role>> GetOrganizationRolesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT role_id, organization_id, role_name, description, permissions, is_system_role, created_at
            FROM auth.roles 
            WHERE organization_id = @organizationId
            ORDER BY role_name";

        var results = await QueryAsync<dynamic>(sql, new { organizationId });

        return results.Select(r => new Role
        {
            RoleId = r.role_id,
            OrganizationId = r.organization_id,
            RoleName = r.role_name,
            Description = r.description,
            Permissions = r.permissions,
            IsSystemRole = r.is_system_role,
            CreatedAt = r.created_at
        }).ToList();
    }

    private static User MapToUser(dynamic result)
    {
        return new User
        {
            UserId = result.user_id,
            OrganizationId = result.organization_id,
            Email = result.email,
            FirstName = result.first_name,
            LastName = result.last_name,
            IsActive = result.is_active,
            LastLoginAt = result.last_login_at,
            CreatedAt = result.created_at,
            Role = new Role
            {
                RoleName = result.role_name,
                Permissions = result.permissions
            },
            Organization = new Organization
            {
                Name = result.organization_name
            }
        };
    }
}
