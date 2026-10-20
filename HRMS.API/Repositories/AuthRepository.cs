using Dapper;
using DocumentFormat.OpenXml.Wordprocessing;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Data;
using System.Text.Json;

namespace HRMS.API.Repositories;

public class AuthRepository : BaseRepository, IAuthRepository
{
    public AuthRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<User?> AuthenticateAsync(string email, string passwordHash)
    {
        var sql = @"
            SELECT
                userid AS ""UserId"",
                organizationid AS ""OrganizationId"",
                email AS ""Email"",
                firstname AS ""FirstName"",
                lastname AS ""LastName"",
                rolename AS ""RoleName"",
                permissions::text AS ""Permissions"",
                organizationname AS ""OrganizationName"",
                domain AS ""Domain"",
                isactive AS ""IsActive""
            FROM auth.fn_authenticate_user(@email, @passwordHash)";
        var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new { email, passwordHash });

        if (result == null) return null;

        string? permissionsJson = result.Permissions;
        return new User
        {
            UserId = result.UserId,
            OrganizationId = result.OrganizationId,
            Email = result.Email,
            FirstName = result.FirstName,
            LastName = result.LastName,
            IsActive = result.IsActive,
            Role = new Role
            {
                RoleName = result.RoleName,
                Permissions = string.IsNullOrWhiteSpace(permissionsJson)
                    ? new Dictionary<string, object>()
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(permissionsJson)
            },
            Organization = new Organization
            {
                Name = result.OrganizationName,
                Domain = result.Domain
            }
        };
    }
    public async Task<string> GetOrganizationDomainAsync(Guid? organizationId)
    {
        var sql = "SELECT domain FROM auth.organizations WHERE organizationid = @organizationId";
        var result = await QuerySingleOrDefaultAsync<string>(sql, new { organizationId });
        return result ?? string.Empty;
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
       u.passwordhash,
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
    WHERE u.email = @email
      AND EXISTS (
          SELECT 1
          FROM employee.employees e
          WHERE e.userid = u.userid
            AND e.status = 'active'
      )
    ORDER BY u.createdat DESC
    LIMIT 1";

        try
        {
            var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new { email });
            if (result == null) return null;

            return MapToUser(result);
        }
        catch (Exception ex)
        {
            throw new Exception("An error occured", ex.InnerException);
        }
    }

    //Changes Here
    public async Task<bool> SavePasswordResetTokenAsync(Guid userId, string token, DateTime expiresAt)
    {
        try
        {

        
        var createdAt = DateTime.Now;
        var sql = @"
  INSERT INTO auth.password_reset_tokens (userid, token, expiresat,createdat)
  VALUES (@userId, @token, @expiresAt,@createdAt)";

        var affected = await ExecuteAsync(sql, new { userId, token, expiresAt, createdAt });
        return affected > 0;
    }
        catch (Exception ex)
        {
            throw;
        }
    }


    public async Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token)
    {
        const string sql = @"
SELECT 
    tokenid,
    userid,
    token,
    expiresat,
    used,
    createdat
FROM auth.password_reset_tokens
WHERE token = @token;
";

        try
        {
            var tokenRecord = await QuerySingleOrDefaultAsync<PasswordResetToken>(sql, new { token });
            return tokenRecord;
        }
        catch (Exception ex)
        {
            throw new Exception("Error fetching password reset token", ex);
        }
    }


    public async Task<bool> UpdateUserPasswordAsync(Guid userId, string newPasswordHash)
    {
        const string sql = @"
        UPDATE auth.users
        SET 
            passwordhash = @newPasswordHash,
            passwordchangedat = NOW(),
            updatedat = NOW()
        WHERE userid = @userId;
    ";

        try
        {
            var rowsAffected = await ExecuteAsync(sql, new { userId, newPasswordHash });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            throw new Exception("Error updating user password", ex);
        }
    }


    public async Task<bool> MarkResetTokenAsUsedAsync(string token)
    {
        const string sql = @"
        UPDATE auth.password_reset_tokens
        SET used = true
        WHERE token = @token;
    ";

        try
        {
            var rowsAffected = await ExecuteAsync(sql, new { token });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            throw new Exception("Error marking reset token as used", ex);
        }
    }

    //Change Password

    public async Task<bool> ChanegPasswordAsync(Guid userId, string newPasswordHash)
    {
        var sql = @"
        UPDATE auth.users
        SET passwordhash = @PasswordHash,
            passwordchangedat = NOW(),
            updatedat = NOW()
        WHERE userid = @UserId";

        var rowsAffected = await ExecuteAsync(sql, new { PasswordHash = newPasswordHash, UserId = userId });

        return rowsAffected > 0; // Returns true if password was updated successfully
    }




    //Chnages For Employee now

    public async Task<Employee?> GetEmployeeByIdAsync(Guid userId)
    {
        var sql = @"
            SELECT employeeid
            FROM employee.employees 
            WHERE userid = @userId";

        return await QuerySingleOrDefaultAsync<Employee>(sql, new { userId });
    }




    //Adding update method for Updating Employee all information
    public async Task<bool> UpdateProfile_EmployeeAsync(Guid employeeId, UpdateProfileRequest request)
    {

        var sql = @"UPDATE employee.employees SET 
        employeecode=@EmployeeNumber, firstname=@FirstName, lastname=@LastName, email=@Email,
        phone=@Phone, address=@Address::jsonb, dateofbirth=@DateOfBirth, gender=@Gender,
        nationality=@Nationality, maritalstatus=@MaritalStatus, emergencycontact=@EmergencyContact::jsonb,
        profilepictureurl=COALESCE(@ProfileUrl, profilepictureurl),
        hiredate=@HireDate, updatedat=CURRENT_TIMESTAMP
        WHERE employeeid=@EmployeeId";

        var affected = await ExecuteAsync(sql, new
        {
            EmployeeNumber = request.EmployeeNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Address = System.Text.Json.JsonSerializer.Serialize(request.Address),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Nationality = request.Nationality,
            MaritalStatus = request.MaritalStatus,
            EmergencyContact = System.Text.Json.JsonSerializer.Serialize(request.EmergencyContact),
            HireDate = request.HireDate,
            EmployeeId = employeeId,
            profileUrl=request.profileurl
        });

        return affected > 0;


    }

    //Update Employee Details table also 

    public async Task<bool> UpdateProfile_EmployeeDetailsAsync(Guid employeeId, UpdateProfileRequest request)
    {
        var sql = @"UPDATE employee.employmentdetails SET 
        departmentid=@DepartmentId, positionid=@PositionId,
        employmenttype=@EmploymentType, paytype=@PayType, basicsalary=@BasicSalary,
        currency=@Currency, worklocation=@WorkLocation, reportingmanagerid=@ReportingManagerId,
        effectivedate=@EffectiveDate, enddate=@EndDate, iscurrent=@IsCurrent
        WHERE employeeid=@EmployeeId";

        var affected = await ExecuteAsync(sql, new
        {
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            //ShiftId = request?.ShiftId,
            EmploymentType = "full_time",
            PayType = "salary",
            BasicSalary = request.BasicSalary,
            Currency = "USD",
            WorkLocation = request.WorkLocation,
            ReportingManagerId = request.ReportingManagerId,
            EffectiveDate = DateTime.UtcNow.Date,
            EndDate = (DateTime?)null,
            IsCurrent = true,
            EmployeeId = employeeId
        });

        return affected > 0;
    }

    //Updating User table also

    public async Task<bool> UpdateProfile_UserAsync(Guid userId, UpdateProfileRequest request)
    {
        var sql = @"UPDATE auth.users SET 
        firstname=@FirstName, lastname=@LastName, email=@Email, 
       emailverified=@EmailVerified,
         updatedat=CURRENT_TIMESTAMP 
        WHERE userid=@UserId";

        var affected = await ExecuteAsync(sql, new
        {
            FirstName = request?.FirstName,
            LastName = request?.LastName,
            Email = request?.Email,
            EmailVerified = false,
            UserId = userId
        });

        return affected > 0;
    }


    public async Task<bool> adminIntoEmployeeAsync(User createUser)
    {
       
           
            var sql = @"insert into employee.employees
            (userid,organizationid,firstname,lastname,email,createdat,updatedat,employeecode,hiredate)
            values (@userId,@Organizationid,@firstName,@lastName,@email,@createdAt,@updatedAt,@employeeCode,@hireDate) ";

            var parameters = new
            {
                employeeCode=createUser.employeecode,
                createdat = createUser.CreatedAt,
                Organizationid = createUser.OrganizationId,
                userId = createUser.UserId,
                firstName = createUser.FirstName,
                lastName = createUser.LastName,
                status = createUser.IsActive,
                email = createUser.Email,
                updatedAt = createUser.UpdatedAt,
                hireDate=createUser.CreatedAt
            };

            var rowsAffected = await ExecuteAsync(sql, parameters);
            return rowsAffected > 0;


    }
    //Getting positionid  by email

    public async Task<Guid> GetPositionByEmailAsync(string email)
    {
        const string sql = @"
        SELECT 
            p.positionid
        FROM employee.positions p
        INNER JOIN auth.users u ON u.roleid = p.roleid
        WHERE u.email = @Email
        LIMIT 1;";

        var position = await QuerySingleOrDefaultAsync<Guid>(sql, new { Email = email });

        return position;
    }
    public async Task<Guid> GetEmployeeIdByEmailAsync(string email)
    {
        const string sql = @"
        SELECT e.employeeid
        FROM employee.employees e
        WHERE e.email = @Email
          AND e.status = 'active'
        LIMIT 1;";

        var employeeId = await QuerySingleOrDefaultAsync<Guid>(sql, new { Email = email });

        return employeeId;
    }


    public async Task<Guid> AdminIntoEmployeeDetailsAsync(Guid employeeId, Guid positionId)
    {
        const string sql = @"
        INSERT INTO employee.employmentdetails
            (employeeid, positionid, effectivedate, iscurrent)
        VALUES
            (@EmployeeId, @PositionId, CURRENT_DATE, true)
        RETURNING detailid;";

        // ExecuteScalarAsync returns the generated detailid
        var detailId = await ExecuteScalarAsync<Guid>(sql, new
        {
            EmployeeId = employeeId,
            PositionId = positionId
        });

        return detailId;
        }


    public async Task<bool> VerifyEmail (string token)
    {
        var result = await QuerySingleAsync<bool>("SELECT * FROM auth.fn_verify_email(@p_token, @p_verify_time)",
            new
            {
                p_token = token,
                p_verify_time = DateTime.UtcNow
            });
        return result;
    }

    public async Task<User> CreateUserAsync(User user)
    {
        // Use QuerySingleAsync to fetch the single row containing multiple columns
        // NOTE: We used "SELECT * FROM" to explode the Table result into separate columns
        var result = await QuerySingleAsync<dynamic>(
            "SELECT * FROM auth.fn_create_user(@organizationId, @email, @passwordHash, @firstName, @lastName, @roleName, @emailVerify, @emailVerifyToken, @emailVerifyExpiry)",
            new
            {
                organizationId = user.OrganizationId,
                email = user.Email,
                passwordHash = user.PasswordHash,
                firstName = user.FirstName,
                lastName = user.LastName,
                roleName = user.Role?.RoleName ?? "Employee",
                emailVerify = user.IsEmailVerified,
                emailVerifyToken = user.EmailVerificationToken,
                emailVerifyExpiry = user.EmailVerificationExpiry
            });

        // Map the snake_case results from PostgreSQL to your C# model
        user.UserId = result.user_id;
        user.RoleId = result.role_id;

        return user;
    }
    public async Task<Guid> CreatePositionAsync(Guid organizationId, Guid roleId, Guid? departmentId, string title)
    {
        // Note: We include PositionTitle because it is defined as NOT NULL in your schema.
        // We include OrganizationId so the position is linked to the correct tenant.
        const string sql = @"
        INSERT INTO employee.positions (
            organizationid, 
            positiontitle, 
            roleid,
            departmentid,
            isactive
        )
        VALUES (
            @organizationId, 
            @positionTitle, 
            @roleId,
            @departmentId,
            true
        )
        RETURNING positionid;";

        var positionId = await ExecuteScalarAsync<Guid>(sql, new
        {
            organizationId = organizationId,
            positionTitle = title,
            roleId = roleId,
            departmentId=departmentId
        });

        return positionId;
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
    //Get Domain
    public async Task<string?> GetDomainByOrganizationIdAsync(Guid organizationId)
    {
        
            var sql = @"select  
                        domain
                    from auth.organizations
                    where organizationid = @organizationId";

            var result = await QuerySingleOrDefaultAsync<string>(sql, new { organizationId });
        return result;
    }

    public async Task<string> GetHashedPassword(string email)
    {
        var sql = @"
            SELECT passwordhash
            FROM auth.users
            WHERE email = @Email
            ORDER BY isactive DESC, createdat DESC
            LIMIT 1";

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
        try
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
        catch(Exception ex)
        {
            return null;
        }
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
    public async Task<IEnumerable<dynamic>> GetMenusAsync()
    {
        var sql = @"
        SELECT 
            m.menuid,
            m.menuname,
            m.icon AS menuicon,
            s.submenuid,
            s.submenuname,
            s.icon AS submenuicon,
            a.actionid,         
            a.actionname,         
            a.actionkey          
        FROM auth.menus m
        LEFT JOIN auth.submenus s ON s.menuid = m.menuid
        LEFT JOIN auth.actions a ON a.submenuid = s.submenuid";
        //ORDER BY m.menuname, s.submenuname";

        try
        {
            var result = await QueryAsync<dynamic>(sql);
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception("Database error while retrieving menus", ex);
        }
    }

    public async Task<Guid> Create_RoleAsync(Create_RoleRequest request, Guid? organizationId)
    {
        const string sql = "SELECT * FROM auth.fn_create_role(@p_organizationid, @p_rolename, @p_description, @p_issystemrole, @p_menus::jsonb)";

        var parameters = new DynamicParameters();
        parameters.Add("p_organizationid", organizationId, DbType.Guid);
        parameters.Add("p_rolename", request.RoleName, DbType.String);
        parameters.Add("p_description", request.Description, DbType.String);
        parameters.Add("p_issystemrole", request.IsSystemRole, DbType.Boolean);

        string jsonMenus = JsonConvert.SerializeObject(request.Menus);
        parameters.Add("p_menus", jsonMenus, DbType.String);

        try
        {
            var result = await ExecuteScalarAsync<Guid?>(sql, parameters);

            if (result == null || result == Guid.Empty)
            {
                throw new InvalidOperationException("Failed to create role");
            }

            return result.Value;
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Database error while creating role: {RoleName}", request.RoleName);
            throw;
        }
    }
    public async Task<bool> UpdateRoleAsync(UpdateRoleRequest request, Guid? organizationId)
    {
        const string sql = "SELECT * FROM auth.fn_update_role(@p_organizationid, @p_roleid, @p_rolename, @p_description, @p_issystemrole, @p_menus::jsonb)";

        var parameters = new DynamicParameters();
        parameters.Add("p_organizationid", organizationId, DbType.Guid);
        parameters.Add("p_roleid", request.RoleId, DbType.Guid);
        parameters.Add("p_rolename", request.RoleName, DbType.String);
        parameters.Add("p_description", request.Description, DbType.String);
        parameters.Add("p_issystemrole", request.IsSystemRole, DbType.Boolean);

        string jsonMenus = JsonConvert.SerializeObject(request.Menus);
        parameters.Add("p_menus", jsonMenus, DbType.String);

        try
        {
            var result = await ExecuteScalarAsync<bool?>(sql, parameters);

            if (result == null || !result.Value)
            {
                throw new InvalidOperationException("Failed to update role");
            }

            return result.Value;
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Database error while updating role: {RoleId}", request.RoleId);
            throw;
        }
    }
    // Get user count by role ID
    public async Task<int> GetUserCountByRoleIdAsync(Guid roleId, Guid? organizationId)
    {
        const string sql = @"
        SELECT COUNT(*) 
        FROM auth.users 
        WHERE roleid = @RoleId 
          AND (organizationid = @OrganizationId OR @OrganizationId IS NULL)";

        var parameters = new DynamicParameters();
        parameters.Add("RoleId", roleId, DbType.Guid);
        parameters.Add("OrganizationId", organizationId, DbType.Guid);

        return await ExecuteScalarAsync<int>(sql, parameters);
    }

    // Delete role
    public async Task<bool> DeleteRoleAsync(Guid roleId, Guid? organizationId)
    {
        const string sql = "SELECT * FROM auth.fn_delete_role(@p_organizationid, @p_roleid)";

        var parameters = new DynamicParameters();
        parameters.Add("p_organizationid", organizationId, DbType.Guid);
        parameters.Add("p_roleid", roleId, DbType.Guid);

        try
        {
            var result = await ExecuteScalarAsync<bool?>(sql, parameters);

            if (result == null || !result.Value)
            {
                return false;
            }

            return result.Value;
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Database error while deleting role: {RoleId}", roleId);
            throw;
        }
    }

    public async Task<Role> GetRoleByNameforTokenAsync(string roleName, Guid? organizationId)
    {
        try
        {
            const string sql = @"
        SELECT roleid, rolename, description, organizationid, issystemrole, createdat
        FROM auth.roles 
        WHERE rolename = @rolename AND organizationid = @organizationid";

            return await QuerySingleOrDefaultAsync<Role>(sql, new { rolename = roleName, organizationid = organizationId });
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<bool> MenuExistsAsync(Guid menuId)
    {
        const string sql = "SELECT COUNT(1) FROM auth.menus WHERE menuid = @menuid";
        var count = await ExecuteScalarAsync<int>(sql, new { menuid = menuId });
        return count > 0;
    }

    public async Task<bool> SubMenuExistsAsync(Guid subMenuId, Guid menuId)
    {
        const string sql = "SELECT COUNT(1) FROM auth.submenus WHERE submenuid = @submenuid AND menuid = @menuid";
        var count = await ExecuteScalarAsync<int>(sql, new { submenuid = subMenuId, menuid = menuId });
        return count > 0;
    }
    public async Task<bool> ActionExistsAsync(Guid actionId, Guid subMenuId)
    {
        const string sql = "SELECT COUNT(1) FROM auth.actions WHERE actionid = @actionid AND submenuid = @submenuid";
        var count = await ExecuteScalarAsync<int>(sql, new { actionid = actionId, submenuid = subMenuId });
        return count > 0;
    }

    public async Task<List<Role>> GetRolesByOrganizationIdAsync(Guid? organizationId)
    {
        const string sql = @"
        SELECT roleid, rolename, description, organizationid, issystemrole, createdat
        FROM auth.roles 
        WHERE organizationid = @organizationid
        ORDER BY createdat DESC";

        return (await QueryAsync<Role>(sql, new { organizationid = organizationId })).ToList();
    }

    public async Task<List<PermissionDetail>> GetRolePermissionsWithMenuDetailsAsync(Guid? roleId)
    {
        const string sql = @"
    SELECT 
        m.menuid,
        m.menuname as MenuName,
        sm.submenuid,
        sm.submenuname as SubMenuName,
        a.actionid,
        a.actionname as ActionName,
        a.actionkey as ActionKey,
        COALESCE(p.has_permission, false) as HasPermission
    FROM auth.menus m
    INNER JOIN auth.submenus sm ON sm.menuid = m.menuid
    INNER JOIN auth.actions a ON a.submenuid = sm.submenuid
    LEFT JOIN auth.permissions p ON p.actionid = a.actionid AND p.roleid = @roleid
    ORDER BY m.menuname, sm.submenuname, a.actionname";

        return (await QueryAsync<PermissionDetail>(sql, new { roleid = roleId })).ToList();
    }
    public async Task<Role> GetRoleByIdAsync(Guid roleId)
    {
        const string sql = @"
    SELECT roleid, rolename, description, organizationid, issystemrole, createdat
    FROM auth.roles 
    WHERE roleid = @roleid";

        return await QuerySingleOrDefaultAsync<Role>(sql, new { roleid = roleId });
    }
    // Add to your AuthRepository
    public async Task<(User user, Guid? roleId)> GetUserWithRoleAsync(Guid userId)
    {
        try
        {
            const string sql = @"
    SELECT 
        u.userid, u.email, u.roleid,
        r.rolename, r.description, r.issystemrole
    FROM auth.users u
    LEFT JOIN auth.roles r ON r.roleid = u.roleid
    WHERE u.userid = @userid";

            var result = await QuerySingleOrDefaultAsync<UserWithRoleDto>(sql, new { userid = userId });

            if (result == null)
                return (null, null);

            var user = new User
            {
                UserId = result.userid,
                //UserName = result.username,
                Email = result.email,
                RoleId = result.roleid
            };

            return (user, result.roleid);
        }
        catch(Exception ex)
        {
            return (null,null);
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

    public async Task<bool> UpdateUserRoleAsync(Guid? userId, Guid? roleId)
    {
        var sql = @"
            UPDATE auth.users 
            SET roleid = @roleId,
                updatedat = CURRENT_TIMESTAMP
            WHERE userid = @userId";

        var affected = await ExecuteAsync(sql, new { userId, roleId });
        return affected > 0;
    }

    private static User MapToUser(dynamic result)
    {
        try
        {
            return new User

            {
                UserId = result.userid,
                OrganizationId = result.organizationid,
                PasswordHash = result.passwordhash,
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
