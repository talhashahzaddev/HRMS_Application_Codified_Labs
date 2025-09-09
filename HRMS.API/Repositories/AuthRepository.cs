using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Newtonsoft.Json;
using System.Data;
using System.Text.Json;

namespace HRMS.API.Repositories;

public class AuthRepository : BaseRepository, IAuthRepository
{
    public AuthRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<User?> AuthenticateAsync(string email, string passwordHash)
    {
        var sql = "SELECT * FROM auth.fn_authenticate_user(@email, @passwordHash)";
        var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new { email, passwordHash });

        if (result == null) return null;
        try
        {
            return new User
            {
                UserId = result.userid,
                OrganizationId = result.organizationid,
                Email = result.email,
                FirstName = result.firstname,
                LastName = result.lastname,
                IsActive = result.isactive,
                Role = new Role
                {
                    RoleName = result.rolename,
                    Permissions = string.IsNullOrEmpty(result.permissions)
                        ? new Dictionary<string, object>()
                        : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(result.permissions)
                },
                Organization = new Organization
                {
                    Name = result.organizationname
                }
            };
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error mapping user data: {ex.Message}");
            return null;
        }
    }


    public async Task<User?> GetUserByIdAsync(Guid? userId)
    {
        var sql = @"
                    SELECT u.userid,
       u.organizationid,
       u.email AS Email, 
       u.firstname AS FirstName, 
       u.lastname AS LastName, 
       u.isactive AS IsActive, 
       u.lastloginat AS LastLoginAt, 
       u.createdat AS CreatedAt,
       r.rolename AS RoleName, 
       r.permissions AS Permissions,
       o.name AS OrganizationName
FROM auth.users u
INNER JOIN auth.roles r ON u.roleid = r.roleid
INNER JOIN auth.organizations o ON u.organizationid = o.organizationid
        WHERE u.userid = @userId";
        try
        {
        var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new { userId });
        if (result == null) return null;

        return MapToUser(result);

        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var sql = @"
    SELECT u.userid,
           u.organizationid, 
           u.email AS Email, 
           u.FirstName, 
           u.LastName , 
           u.IsActive , 
           u.LastLoginAt, 
           u.CreatedAt,
           r.RoleName, 
           r.permissions AS Permissions,
           o.name AS OrganizationName
    FROM auth.users u
    INNER JOIN auth.roles r ON u.roleid = r.roleid
    INNER JOIN auth.organizations o ON u.organizationid = o.organizationid
    WHERE u.email = @email";

        try
        {
        var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new { email });
        if (result == null) return null;

        return MapToUser(result);
        }
        catch(Exception ex)
        {
            throw new Exception("An error occured", ex.InnerException);
        }
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

    public async Task<bool> UpdateRefreshTokenAsync(Guid? userId, string refreshToken, DateTime expiresAt)
    {
        var sql = @"
            UPDATE auth.users 
            SET refreshtoken = @refreshToken, 
                refreshtokenexpiresat = @expiresAt,
                updatedat = CURRENT_TIMESTAMP
            WHERE userid = @userId";

        var affected = await ExecuteAsync(sql, new { userId, refreshToken, expiresAt });
        return affected > 0;
    }
    public async Task<string> GetHashedPassword(string email)
    {
        var sql = "SELECT passwordhash FROM auth.users WHERE email = @Email";
        try
        {
        var result = await QuerySingleOrDefaultAsync<string>(sql, new { Email = email });
        return result ?? string.Empty;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    public async Task<bool> UpdateLastLoginAsync(Guid? userId)
    {
        var sql = @"
            UPDATE auth.users 
            SET lastloginat = CURRENT_TIMESTAMP,
                updatedat = CURRENT_TIMESTAMP
            WHERE userid = @userId";

        var affected = await ExecuteAsync(sql, new { userId });
        return affected > 0;
    }



    public async Task<Role?> GetRoleByNameAsync(Guid? organizationId, string roleName)
    {
        var sql = @"
            SELECT roleid, organizationid, rolename, description, permissions, issystemrole, createdat
            FROM auth.roles 
            WHERE organizationid = @organizationId AND rolename = @roleName";

        var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new { organizationId, roleName });
        if (result == null) return null;

        return new Role
        {
            RoleId = result.roleid,
            OrganizationId = result.organizationid,
            RoleName = result.rolename,
            Description = result.description,
            Permissions = result.permissions,
            IsSystemRole = result.issystemrole,
            CreatedAt = result.createdat
        };
    }

    public async Task<List<Role>> GetOrganizationRolesAsync(Guid? organizationId)
    {
        var sql = @"
            SELECT roleid, organizationid, rolename, description, permissions, issystemrole, createdat
            FROM auth.roles 
            WHERE organizationid = @organizationId
            ORDER BY rolename";
        try
        {
            var results = await QueryAsync<dynamic>(sql, new { organizationId });

            return results.Select(r => new Role
            {
                RoleId = r.roleid,
                OrganizationId = r.organizationid,
                RoleName = r.rolename,
                Description = r.description,
                Permissions = string.IsNullOrWhiteSpace((string)r.permissions)
        ? null
        : JsonConvert.DeserializeObject<Dictionary<string, object>>((string)r.permissions),
                IsSystemRole = r.issystemrole,
                CreatedAt = r.createdat
            }).ToList();

        }
        catch (Exception ex)
        {
            throw new Exception("An error occured", ex.InnerException);
        }

    }
    private Dictionary<string, object> NormalizePermissions(Dictionary<string, object>? permissions)
    {
        if (permissions == null)
            return new Dictionary<string, object>();

        var normalized = new Dictionary<string, object>();

        foreach (var kvp in permissions)
        {
            if (kvp.Value is JsonElement jsonElement)
            {
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.String:
                        normalized[kvp.Key] = jsonElement.GetString();
                        break;
                    case JsonValueKind.Number:
                        if (jsonElement.TryGetInt32(out int intValue))
                            normalized[kvp.Key] = intValue;
                        else if (jsonElement.TryGetDecimal(out decimal decValue))
                            normalized[kvp.Key] = decValue;
                        break;
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        normalized[kvp.Key] = jsonElement.GetBoolean();
                        break;
                    case JsonValueKind.Object:
                    case JsonValueKind.Array:
                        normalized[kvp.Key] = jsonElement.ToString(); // or deserialize deeper if needed
                        break;
                    default:
                        normalized[kvp.Key] = null!;
                        break;
                }
            }
            else
            {
                normalized[kvp.Key] = kvp.Value!;
            }
        }

        return normalized;
    }

    public async Task<Role> CreateRoleAsync(Role role)
    {
        var sql = @"
            INSERT INTO auth.roles (organizationid, rolename, description, permissions, issystemrole, createdat)
            VALUES (@OrganizationId, @RoleName, @Description, @Permissions::jsonb, @IsSystemRole, CURRENT_TIMESTAMP)
            RETURNING roleid, organizationid, rolename, description, permissions, issystemrole, createdat";
        try
        {
            var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new
            {
                role.OrganizationId,
                role.RoleName,
                role.Description,
                Permissions = role.Permissions != null
            ? JsonConvert.SerializeObject(NormalizePermissions(role.Permissions))
            : null,
                role.IsSystemRole
            });


            if (result == null)
            throw new Exception("Failed to create role");

            return new Role
            {
                RoleId = result.roleid,
                OrganizationId = result.organizationid,
                RoleName = result.rolename,
                Description = result.description,
                Permissions = NormalizePermissions(
             string.IsNullOrEmpty(result.permissions)
                 ? new Dictionary<string, object>()
                 : JsonConvert.DeserializeObject<Dictionary<string, object>>(result.permissions)
         ),
                IsSystemRole = result.issystemrole,
                CreatedAt = result.createdat
            };


        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private static User MapToUser(dynamic result)
    {
        try
        {
            return new User

            {
                UserId = result.userid,
                OrganizationId = result.organizationid,
                Email = result.email,
                FirstName = result.firstname,
                LastName = result.lastname,
                IsActive = result.isactive,
                LastLoginAt = result.lastloginat,
                CreatedAt = result.createdat,
                Role = new Role
                {
                    RoleName = result.rolename,
                    Permissions = string.IsNullOrEmpty(result.permissions)
        ? new Dictionary<string, object>()
        : JsonConvert.DeserializeObject<Dictionary<string, object>>(result.permissions)
                },
                Organization = new Organization
                {
                    Name = result.rganizationame
                }
            };
        }
        catch (Exception ex)
        {
            throw new Exception("Cannot map to the result", ex.InnerException);
        }
    }

}
