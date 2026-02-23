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
                    Name = result.organizationname,
                    Domain=result.domain
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
    INNER JOIN employee.employees e ON u.userid = e.userid
    WHERE u.email = @email
    AND e.status = 'active'";

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
        try
        {
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
        catch (Exception ex)
        {
            throw new Exception("An error occured", ex.InnerException);
        }
    }

    public async Task<Guid> CreatePositionAsync(Guid organizationId, Guid roleId, string title = "Super Admin")
    {
        // Note: We include PositionTitle because it is defined as NOT NULL in your schema.
        // We include OrganizationId so the position is linked to the correct tenant.
        const string sql = @"
        INSERT INTO employee.positions (
            organizationid, 
            positiontitle, 
            roleid,
            isactive
        )
        VALUES (
            @organizationId, 
            @positionTitle, 
            @roleId,
            true
        )
        RETURNING positionid;";

        var positionId = await ExecuteScalarAsync<Guid>(sql, new
        {
            organizationId = organizationId,
            positionTitle = title,
            roleId = roleId
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
