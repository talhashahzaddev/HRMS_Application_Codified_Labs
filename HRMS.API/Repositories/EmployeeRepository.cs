using Dapper;
using DocumentFormat.OpenXml.InkML;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Npgsql;
using NPOI.HPSF;
using NPOI.SS.Formula.Functions;
using System.Data;
using System.Text.Json;

namespace HRMS.API.Repositories;

public class EmployeeRepository : BaseRepository, IEmployeeRepository
{
    public EmployeeRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<Employee> CreateEmployeeAsync(Employee employee)
    {
        using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var employeeId = await connection.ExecuteScalarAsync<Guid>(
                @"SELECT employee.fn_create_employee(
            @organizationId, @userId, @employeeCode, @firstName, @lastName, @email, @phone, 
            @dateOfBirth, @hireDate, @departmentId, @positionId, @basicSalary, @address::jsonb, 
            @workLocation,@taxCategoryid, @reportingManagerId,@employmentType, @nationality, @gender, @maritalStatus, @emergencyContact::jsonb)",
                new
                {
                    organizationId = employee.OrganizationId,
                    userId = employee.UserId,
                    employeeCode = employee.EmployeeNumber,
                    firstName = employee.FirstName,
                    lastName = employee.LastName,
                    email = employee.Email,
                    phone = employee.Phone,
                    dateOfBirth = employee.DateOfBirth,
                    hireDate = employee.HireDate,
                    departmentId = employee.DepartmentId,
                    positionId = employee.PositionId,
                    basicSalary = employee.BasicSalary,
                    address = System.Text.Json.JsonSerializer.Serialize(employee.Address),
                    workLocation = employee.WorkLocation,
                    reportingManagerId = employee.ReportingManagerId,
                    employmentType=employee.employmenttype,
                    nationality = employee.Nationality,
                    gender = employee.Gender,
                    maritalStatus = employee.MaritalStatus,
                    taxCategoryid=employee.taxCategoryId,
                    emergencyContact = System.Text.Json.JsonSerializer.Serialize(employee.EmergencyContact)
                },
                transaction
            );

            await connection.ExecuteAsync(
                "SELECT attendance.fn_initialize_leave_entitlements(@employeeId, @organizationId)",
                new { employeeId, employee.OrganizationId },
                transaction
            );

            await transaction.CommitAsync();
            employee.EmployeeId = employeeId;
            return employee;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception("Error while creating employee record", ex);
        }
    }




    public async Task<bool> UpdateEmploymentDetailCurrencyAsync(Guid employeeId, string currency)
    {
      try
        {
            var sql = @"UPDATE employee.employmentdetails 
                    SET currency = @currency 
                    WHERE employeeid = @employeeId AND iscurrent = true";
            var affectedRows = await ExecuteAsync(sql, new { employeeId, currency });
            return affectedRows > 0;
        }
        catch(Exception ex)
        {
            return false;
        }
    }
    public async Task<EmployeeDto> GetEmployeedetailsAsync(Guid employeeId)
    {
        var sql = @"
    SELECT 
        e.employeeid            AS EmployeeId,
        e.organizationid        AS OrganizationId,
        e.userid                AS UserId,
        e.employeecode          AS EmployeeCode,
        e.firstname             AS FirstName,
        e.lastname              AS LastName,
        e.email                 AS Email,
        e.phone                 AS Phone,
        e.address               AS Address,
        e.dateofbirth           AS DateOfBirth,
        e.gender                AS Gender,
        e.nationality           AS Nationality,
        e.maritalstatus         AS MaritalStatus,
        e.hiredate              AS HireDate,
        e.status                AS Status,

        d.departmentname        AS DepartmentName,
        p.positiontitle         AS PositionTitle,

        ed.basicsalary          AS BasicSalary,
        ed.currency             AS Currency,
        ed.effectivedate        AS SalaryEffectiveDate,
        ed.reportingmanagerid   AS ReportingManagerId

    FROM employee.employees e
    LEFT JOIN employee.employmentdetails ed
        ON e.employeeid = ed.employeeid
        AND ed.iscurrent = TRUE
    LEFT JOIN employee.departments d
        ON ed.departmentid = d.departmentid
    LEFT JOIN employee.positions p
        ON ed.positionid = p.positionid
    WHERE e.employeeid = @employeeId";

        return await QuerySingleOrDefaultAsync<EmployeeDto>(sql, new { employeeId });
    }



    //Getting employees under reporting manager
    //    public async Task<IEnumerable<EmployeeDto>> GetEmployeesReportingToManagerAsync(Guid managerId)
    //    {
    //        var sql = @"
    //SELECT e.employeeid,
    //       e.organizationid,
    //       e.userid,
    //       e.employeecode,
    //       e.firstname,
    //       e.lastname,
    //       e.email,
    //       e.phone,
    //       e.address,
    //       e.dateofbirth,
    //       e.gender,
    //       e.nationality,
    //       e.maritalstatus,
    //       e.hiredate,
    //       e.status,
    //       d.departmentname,
    //       p.positiontitle,
    //       ed.basicsalary
    //FROM employee.employees e
    //LEFT JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
    //LEFT JOIN employee.departments d ON ed.departmentid = d.departmentid
    //LEFT JOIN employee.positions p ON ed.positionid = p.positionid
    //WHERE e.managerid = @ManagerId
    //ORDER BY e.firstname, e.lastname;
    //";

    //        return await QueryAsync<EmployeeDto>(sql, new { ManagerId = managerId });
    //    }

    public async Task<IEnumerable<EmployeeDto>> GetEmployeesReportingToManagerAsync(Guid managerId)
    {
        var sql = @"
SELECT 
    e.employeeid,
    e.organizationid,
    e.userid,
    e.employeecode,
    e.firstname,
    e.lastname,
    e.email,
    e.phone,
    e.address,
    e.dateofbirth,
    e.gender,
    e.nationality,
    e.maritalstatus,
    e.hiredate,
    e.status,
    d.departmentname,
    p.positiontitle,
    ed.basicsalary
FROM employee.employees e
INNER JOIN employee.employmentdetails ed 
    ON e.employeeid = ed.employeeid 
    AND ed.iscurrent = true
LEFT JOIN employee.departments d 
    ON ed.departmentid = d.departmentid
LEFT JOIN employee.positions p 
    ON ed.positionid = p.positionid
WHERE ed.reportingmanagerid = @ManagerId
ORDER BY e.firstname, e.lastname;
";

        return await QueryAsync<EmployeeDto>(sql, new { ManagerId = managerId });
    }



    //Get Employee ID
    public async Task<Guid?> GetEmployeeIdAsync(Guid userId)
    {
        var sql = @"select employeeid from  employee.employees where
             userid=@userId";


        var empid = await QuerySingleOrDefaultAsync<Guid?>(sql, new { userId = userId });

        return empid;

    }

 


    //Adding function here


    public async Task<Guid?> GetEmpUserIdAsync(Guid employeeId)
    {
        var query = "SELECT userid FROM employee.employees WHERE employeeid = @EmployeeId";
        // QueryFirstOrDefaultAsync returns a single value or default (null)
        var userId = await QuerySingleOrDefaultAsync<Guid?>(query, new { EmployeeId = employeeId });
        return userId;
    }




    public async Task<bool> EditEmployeeDetailsAsync(Guid employeeId, UpdateEmployeeRequestDTo request)
    {
        var sql = @"UPDATE employee.employmentdetails SET 
                        departmentid=@DepartmentId, 
                        positionid=@PositionId,
                        employmenttype=@EmploymentType, 
                        paytype=@PayType, 
                        basicsalary=@BasicSalary,
                        currency=@Currency, 
                        worklocation=@WorkLocation, 
                        reportingmanagerid=@ReportingManagerId,
                        effectivedate=@EffectiveDate, 
                        enddate=@EndDate, 
                        iscurrent=@IsCurrent
                    WHERE employeeid=@EmployeeId";

        var affected = await ExecuteAsync(sql, new
        {
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            EmploymentType = request.EmploymentType, // Default value
            PayType = request.PayType,           // Default value
            BasicSalary = request.BasicSalary,
            Currency = "USD",             // Default value
            WorkLocation = request.WorkLocation,
            ReportingManagerId = request.ReportingManagerId,
            EffectiveDate = DateTime.UtcNow.Date,
            EndDate = (DateTime?)null,
            IsCurrent = true,
            EmployeeId = employeeId
        });

        return affected > 0;
    }


    public async Task<bool> EditEmployeeTableAsync(Guid employeeId, UpdateEmployeeRequestDTo request, string? profileUrl)
    {
        var sql = @"UPDATE employee.employees SET 
                        employeecode=@EmployeeNumber, 
                        firstname=@FirstName, 
                        lastname=@LastName, 
                        email=@Email,
                        phone=@Phone, 
                        address=@Address::jsonb, 
                        dateofbirth=@DateOfBirth, 
                        gender=@Gender,
                        nationality=@Nationality, 
                        maritalstatus=@MaritalStatus, 
                        emergencycontact=@EmergencyContact::jsonb,
                        profilepictureurl=COALESCE(@ProfileUrl, profilepictureurl),
                        hiredate=@HireDate, 
                        updatedat=CURRENT_TIMESTAMP
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
            Nationality = request.Gender,
            MaritalStatus = request.MaritalStatus,
            EmergencyContact = System.Text.Json.JsonSerializer.Serialize(request.EmergencyContact),
            HireDate = request.HireDate,
            EmployeeId = employeeId,
            ProfileUrl = profileUrl
        });

        return affected > 0;
    }


    public async Task<Guid?> GetRoleIdAsync(Guid? userId)
    {
        var sql = @"
        SELECT roleid 
        FROM employee.positions 
        WHERE positionid = @UserId
    ";

        var roleId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { UserId = userId });
        return roleId;
    }



    public async Task<bool> editEmployeeUserAsync(Guid? userId, UpdateEmployeeRequestDTo request)
    {
        var RoleId = await GetRoleIdAsync(request.PositionId);

        var sql = @"UPDATE auth.users SET 
                 firstname=@FirstName, 
                 lastname=@LastName, 
                 email=@Email, 
                 emailverified=@EmailVerified, 
                 roleid=COALESCE(@RoleId, roleid),
                 updatedat=CURRENT_TIMESTAMP 
             WHERE userid=@UserId";

        var affected = await ExecuteAsync(sql, new
        {
            FirstName = request?.FirstName,
            LastName = request?.LastName,
            Email = request?.Email,
            EmailVerified = false, // Default false
            RoleId = RoleId,
            UserId = userId
        });

        return affected > 0;
    }
    public async Task<Employee?> GetEmployeeByIdAsync(Guid? userId)
    {
        try
        {
            var sql = @"
        SELECT 
            e.employeeid, 
            e.organizationid, 
            e.userid, 
            e.employeecode AS EmployeeNumber,
            e.firstname, 
            e.lastname, 
            e.email, 
            e.phone, 
            e.address,
            e.profilepictureurl AS ProfilePictureUrl,
            e.dateofbirth, 
            e.gender, 
            e.nationality, 
            e.maritalstatus,
            e.emergencycontact AS EmergencyContact, 
            e.hiredate, 
            e.terminationdate, 
            e.status,
            e.createdat, 
            e.updatedat,

            e.categoryid AS TaxCategoryId,
            
            ed.worklocation, 
            ed.basicsalary AS BasicSalary, 
            ed.departmentid, 
            ed.positionid,
            ed.reportingmanagerid AS reporting_manager_id,

            CONCAT(mgr.firstname, ' ', mgr.lastname) AS ReportingManagerName,
            p.positiontitle, 
            p.roleid AS RoleId,
            d.departmentname,

            tc.categoryname AS TaxCategoryName,

            ar.rolename AS RoleName

        FROM employee.employees AS e

        LEFT JOIN employee.employmentdetails AS ed 
            ON e.employeeid = ed.employeeid

        LEFT JOIN employee.positions AS p 
            ON ed.positionid = p.positionid

        LEFT JOIN employee.departments AS d 
            ON ed.departmentid = d.departmentid

        LEFT JOIN employee.employees AS mgr 
            ON ed.reportingmanagerid = mgr.employeeid

        LEFT JOIN payroll.taxcategories AS tc
            ON e.categoryid = tc.categoryid
            
        LEFT JOIN auth.users AS u
            ON e.userid = u.userid
            
        LEFT JOIN auth.roles AS ar
            ON u.roleid = ar.roleid

        WHERE e.userid = @userId;
        ";

            return await QuerySingleOrDefaultAsync<Employee>(
                sql,
                new { userId }
            );
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<Employee?> GetEmployeeByEmployeeIdAsync(Guid? employeeId)
    {
        try
        {
            var sql = @"
        SELECT
            e.employeeid,
            e.organizationid,
            e.userid,
            e.employeecode AS EmployeeNumber,
            e.firstname,
            e.lastname,
            e.email,
            e.phone,
            e.address,
            e.profilepictureurl AS ProfilePictureUrl,
            e.dateofbirth,
            e.gender,
            e.nationality,
            e.maritalstatus,
            e.emergencycontact AS EmergencyContact,
            e.hiredate,
            e.terminationdate,
            e.status,
            e.createdat,
            e.updatedat,
            e.categoryid AS TaxCategoryId,
            ed.worklocation,
            ed.basicsalary AS BasicSalary,
            ed.departmentid,
            ed.positionid,
            ed.reportingmanagerid AS reporting_manager_id,
            CONCAT(mgr.firstname, ' ', mgr.lastname) AS ReportingManagerName,
            p.positiontitle,
            p.roleid AS RoleId,
            d.departmentname,
            tc.categoryname AS TaxCategoryName,
            ar.rolename AS RoleName
        FROM employee.employees AS e
        LEFT JOIN employee.employmentdetails AS ed ON e.employeeid = ed.employeeid
        LEFT JOIN employee.positions AS p ON ed.positionid = p.positionid
        LEFT JOIN employee.departments AS d ON ed.departmentid = d.departmentid
        LEFT JOIN employee.employees AS mgr ON ed.reportingmanagerid = mgr.employeeid
        LEFT JOIN payroll.taxcategories AS tc ON e.categoryid = tc.categoryid
        LEFT JOIN auth.users AS u ON e.userid = u.userid
        LEFT JOIN auth.roles AS ar ON u.roleid = ar.roleid
        WHERE e.employeeid = @employeeId;";

            return await QuerySingleOrDefaultAsync<Employee>(sql, new { employeeId });
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<string> GetUserRoleAsync(Guid userid)
    {
        try
        {
            var sql = @"
        SELECT r.rolename 
        FROM auth.users u
        JOIN auth.roles r ON u.roleid = r.roleid
        WHERE u.userid = @userid AND u.isactive = true";
            var roleName = await QuerySingleOrDefaultAsync<string>(sql, new { userid });
            return roleName;
        }
        catch (Exception ex) {
            return null;
        }
    }
    public async Task<List<EmployeeNameList>> GetEmployeeByOrganizationAsync(Guid? organizationid, Guid? reportingmanagerid)
    {
        try
        {
            var sql = @"
        SELECT 
            CONCAT(e.firstname, ' ', e.lastname) AS EmployeeName,
            e.employeecode AS EmployeeCode,
            e.email AS Email 
        FROM employee.employees AS e
        LEFT JOIN employee.employmentdetails AS ed 
            ON e.employeeid = ed.employeeid
        WHERE 
            e.organizationid = @organizationid
            AND e.status = 'active'
            AND (
                @reportingmanagerid IS NULL 
                OR ed.reportingmanagerid = @reportingmanagerid
            );
        ";

            var result =  await QueryAsync<EmployeeNameList>(
                sql,
                new { organizationid, reportingmanagerid }
            );
            return result.ToList();
        }
        catch (Exception)
        {
            return null;
        }
    }
    public async Task<List<Employee>> GeSelectedtEmployeesByIdsAsync(List<Guid> employeeIds)
    {
        try
        {
            var sql = @"
        SELECT 
            e.employeeid, e.organizationid, e.userid, e.employeecode AS EmployeeNumber,
            e.firstname, e.lastname, e.email, e.phone, e.address, 
            e.profilepictureurl AS ProfilePictureUrl,
            e.dateofbirth, e.gender, e.nationality, e.maritalstatus,
            e.emergencycontact AS EmergencyContact, 
            e.hiredate, e.terminationdate, e.status,
            e.createdat, e.updatedat,
            ed.worklocation, ed.basicsalary AS BasicSalary, 
            ed.departmentid, ed.positionid,
            ed.reportingmanagerid AS reporting_manager_id,
            CONCAT(mgr.firstname, ' ', mgr.lastname) AS ReportingManagerName,
            p.positiontitle, d.departmentname
        FROM employee.employees AS e
        LEFT JOIN employee.employmentdetails AS ed 
            ON e.employeeid = ed.employeeid
        LEFT JOIN employee.positions AS p 
            ON ed.positionid = p.positionid
        LEFT JOIN employee.departments AS d 
            ON ed.departmentid = d.departmentid
        LEFT JOIN employee.employees AS mgr 
            ON ed.reportingmanagerid = mgr.employeeid
        WHERE e.employeeid = ANY(@employeeIds);
        ";

            return (await QueryAsync<Employee>(
                sql,
                new { employeeIds }
            )).ToList();
        }
        catch (Exception)
        {
            return new List<Employee>();
        }
    }
    public async Task<Employee?> GetEmployeeByCodeAsync(Guid organizationId, string employeeCode)
    {
        var sql = @"
        SELECT employeeid, organizationid, userid, employeecode, firstname, lastname,
               email, phone, dateofbirth, hiredate, status, createdat
        FROM employee.employees 
        WHERE organizationid = @organizationId 
        AND UPPER(employeecode) = UPPER(@employeeCode)";

        var result = await QuerySingleOrDefaultAsync<Employee>(
            sql,
            new { organizationId, employeeCode });

        return result;
    }
    public async Task<Employee?> GetEmployeeByEmailAsync(Guid organizationId, string email)
    {
        var sql = @"
            SELECT employeeid, organizationid, userid, employeecode, firstname, lastname,
                   email, phone, dateofbirth, hiredate, status, createdat
            FROM employee.employees 
            WHERE email = @email";

        var result = await QuerySingleOrDefaultAsync<Employee>(sql, new { organizationId, email });
        return result;
    }


    public async Task<PagedResult<EmployeeListItemDto>> GetEmployeesAsync(
    Guid? organizationId = null,
    string? searchTerm = null,
    Guid? departmentId = null,
    Guid? positionId = null,
    string? employmentType = null,
    string? status = null,
    int page = 1,
    int pageSize = 50)
    {
        try
        {
            var results = await QueryAsync<dynamic>(
                @"SELECT * 
              FROM employee.fn_get_employees(
                  @organizationId, 
                  @searchTerm, 
                  @departmentId, 
                  @positionId,
                  @employmentType, 
                  @status, 
                  @limit, 
                  @offset)",
                new
                {
                    organizationId = organizationId == Guid.Empty ? null : organizationId,
                    searchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm,
                    departmentId = departmentId == Guid.Empty ? null : departmentId,
                    positionId = positionId == Guid.Empty ? null : positionId,
                    employmentType = string.IsNullOrWhiteSpace(employmentType) ? null : employmentType,
                    status = string.IsNullOrWhiteSpace(status) ? null : status,
                    limit = pageSize,
                    offset = (page - 1) * pageSize
                });

            var employees = results.Select(r => new EmployeeListItemDto
            {
                EmployeeId = r.employeeid != null ? (Guid)r.employeeid : Guid.Empty,
                EmployeeCode = r.employeecode,
                FullName = $"{(string?)r.firstname ?? ""} {(string?)r.lastname ?? ""}".Trim(),
                FirstName = r.firstname,
                LastName = r.lastname,
                Email = r.email,
                Phone = r.phone,
                HireDate = r.hiredate,
                Status = r.status,

                // Existing fields
                IsActive = r.isactive, // agar backend me column ya mapping hai
                employmentType = r.employmenttype ?? "full_time",

                // ✅ New ID fields
                DepartmentId = r.departmentid,
                DepartmentName = r.departmentname,
                PositionId = r.positionid,
                PositionTitle = r.positiontitle,

                ProfilePictureUrl = r.profilepictureurl ?? string.Empty,
                PayType = r.paytype,
                CategoryId = r.categoryid,
                CategoryName = r.categoryname,
                BasicSalary = r.basicsalary,
                ReportingManagerId = r.reportingmanagerid,
                ReportingManagerName = r.reportingmanagername,
                Gender = r.gender,
                Nationality = r.nationality,
                MaritalStatus = r.maritalstatus,
                RoleId = r.roleid,
                RoleName = r.rolename,
                DateOfBirth = r.dateofbirth,
                // JSON fields
                Address = r.address != null
              ? JsonSerializer.Deserialize<Dictionary<string, string>>(r.address.ToString())
              : null,
                EmergencyContact = r.emergencycontact != null
              ? JsonSerializer.Deserialize<Dictionary<string, string>>(r.emergencycontact.ToString())
              : null
            }).ToList();


            var totalCount = results.FirstOrDefault()?.totalcount ?? 0;

            return new PagedResult<EmployeeListItemDto>
            {
                Data = employees,
                TotalCount = (int)totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            // Optional: log the exception
            throw;
        }
    }


    public async Task<List<EmployeeListItemDto>> GetEmployeesByManagerAsync(Guid managerEmployeeId, Guid organizationId)
    {
        try
        {
            var sql = @"
                SELECT 
                    e.employeeid,
                    e.employeecode,
                    e.firstname,
                    e.lastname,
                    e.email,
                    e.phone,
                    e.hiredate,
                    e.status,
                    e.profilepictureurl,
                    d.departmentname,
                    p.positiontitle
                FROM employee.employees e
                INNER JOIN employee.employmentdetails ed 
                    ON e.employeeid = ed.employeeid 
                    AND ed.iscurrent = true
                    AND ed.reportingmanagerid = @ManagerEmployeeId
                LEFT JOIN employee.departments d 
                    ON ed.departmentid = d.departmentid
                LEFT JOIN employee.positions p 
                    ON ed.positionid = p.positionid
                WHERE e.organizationid = @OrganizationId
                    AND e.status != 'deleted'
                ORDER BY e.firstname, e.lastname";

            var results = await QueryAsync<dynamic>(sql, new { ManagerEmployeeId = managerEmployeeId, OrganizationId = organizationId });

            var employees = results.Select(r => new EmployeeListItemDto
            {
                EmployeeId = r.employeeid != null ? (Guid)r.employeeid : Guid.Empty,
                EmployeeCode = r.employeecode,
                FullName = $"{(string?)r.firstname ?? ""} {(string?)r.lastname ?? ""}".Trim(),
                Email = r.email,
                Phone = r.phone,
                HireDate = r.hiredate,
                Status = r.status,
                ProfilePictureUrl = r.profilepictureurl ?? string.Empty,
                DepartmentName = r.departmentname,
                PositionTitle = r.positiontitle,
                IsActive = r.status?.ToString().ToLower() == "active"
            }).ToList();

            return employees;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> CheckEmployeeLimitAsync(Guid organizationId)
    {
        try
        {
        var sql = @"
            SELECT 
                CASE 
                    WHEN sp.maxusers IS NULL THEN true
                    WHEN (SELECT COUNT(*) FROM auth.Users WHERE OrganizationId = @organizationId) < sp.maxusers THEN true
                    ELSE false
                END
            FROM payment.companysubscriptions cs
            INNER JOIN payment.subscriptionplans sp ON cs.planid = sp.planid
            WHERE cs.companyid = @organizationId AND cs.status = 'active'
            ORDER BY cs.startdate DESC
            LIMIT 1";

        return await QuerySingleOrDefaultAsync<bool>(sql, new { organizationId });
        }
        catch (Exception ex)
        {
            throw;
        }
    }


    public async Task<bool> UpdateEmployeeAsync(Employee employee)
    {
        var sql = @"
            UPDATE employee.employees 
            SET firstname = @firstName, lastname = @lastName, phone = @phone,
                dateofbirth = @dateOfBirth, status = @status, updatedat = CURRENT_TIMESTAMP
            WHERE employeeid = @employeeId";

        var affected = await ExecuteAsync(sql, new
        {
            firstName = employee.FirstName,
            lastName = employee.LastName,
            phone = employee.Phone,
            dateOfBirth = employee.DateOfBirth,
            status = employee.Status,
            employeeId = employee.EmployeeId
        });

        return affected > 0;
    }
    public async Task<bool> DeleteEmployeeAsync(Guid employeeId, string status)
    {
        var sql = @"
        UPDATE employee.employees
        SET status = @status, updatedat = CURRENT_TIMESTAMP
        WHERE employeeid = @employeeId";

        var affected = await ExecuteAsync(sql, new { employeeId, status });
        return affected > 0;
    }


    public async Task<List<Department>> GetDepartmentsAsync(
        Guid organizationId,
        string? searchQuery = null,
        bool? status = null)
    {
        try
        {
            var sql = @"
        SELECT 
            d.departmentid, 
            d.organizationid, 
            d.departmentname, 
            d.description, 
            d.managerid, 
            d.parentdepartmentid, 
            d.budget, 
            d.isactive, 
            d.createdat, 
            d.updatedat,
            COALESCE(emp_count.employee_count, 0) as employeecount,
            m.firstname AS ManagerFirstName,
            m.lastname AS ManagerLastName
        FROM employee.departments d
        LEFT JOIN (
            SELECT ed.departmentid, COUNT(e.employeeid) as employee_count
            FROM employee.employmentdetails ed
            INNER JOIN employee.employees e ON ed.employeeid = e.employeeid
            WHERE ed.iscurrent = true AND e.organizationid = @organizationId
            GROUP BY ed.departmentid
        ) emp_count ON d.departmentid = emp_count.departmentid
        LEFT JOIN employee.employees m ON d.managerid = m.employeeid
        WHERE d.organizationid = @organizationId
        AND (@status IS NULL OR d.isactive = @status)
        AND (@searchQuery IS NULL OR d.departmentname ILIKE @searchQuery)
        ORDER BY d.departmentname;";

            var parameters = new DynamicParameters();
            parameters.Add("organizationId", organizationId, System.Data.DbType.Guid);
            parameters.Add("searchQuery", string.IsNullOrEmpty(searchQuery) ? null : $"%{searchQuery}%", System.Data.DbType.String);
            parameters.Add("status", status, System.Data.DbType.Boolean);

            var results = await QueryAsync<Department>(sql, parameters);

            var departments = results.Select(d => new Department
            {
                DepartmentId = d.DepartmentId,
                OrganizationId = d.OrganizationId,
                DepartmentName = d.DepartmentName,
                Description = d.Description,
                ManagerId = d.ManagerId,
                ParentDepartmentId = d.ParentDepartmentId,
                Budget = d.Budget,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                EmployeeCount = d.EmployeeCount,
                Manager = d.ManagerId.HasValue ? new Employee
                {
                    EmployeeId = d.ManagerId.Value,
                    FirstName = d.ManagerFirstName,
                    LastName = d.ManagerLastName
                } : null
            }).ToList();

            return departments;
        }
        catch (Exception ex)
        {
            // Optional: log the exception
            throw;
        }
    }




    public async Task<Department> CreateDepartmentAsync(Department department)
    {
        var sql = @"
            INSERT INTO employee.departments (organizationid, departmentname, description, managerid)
            VALUES (@organizationId, @departmentName, @description, @managerId)
            RETURNING departmentid";
        try
        {
            var departmentId = await ExecuteScalarAsync<Guid>(sql, new
            {
                organizationId = department.OrganizationId,
                departmentName = department.DepartmentName,
                description = department.Description,
                managerId = department.ManagerId
            });

            department.DepartmentId = departmentId;
            return department;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

  


    public async Task<List<Position>> GetPositionsAsync(
    Guid organizationId,
    Guid? departmentId = null,
    string? search = null,
    string? status = null) // status can be "active"/"inactive" or your column value
    {
        try
        {
            var sql = @"
        SELECT 
            ep.positionid, 
            ep.organizationid, 
            ep.positiontitle, 
            ep.departmentid, 
            ed.departmentname AS DepartmentName, 
            ep.description, 
            ep.requirements, 
            ep.salaryrange, 
            ep.roleid, 
            ar.rolename AS RoleName,
            ep.isactive, 
            ep.createdat, 
            ep.updatedat,
            -- Employee Details
            ee.employeeid AS EmployeeId,
            ee.employeecode AS EmployeeCode,
            ee.firstname AS EmployeeFirstName,
            ee.lastname AS EmployeeLastName,
            ee.email AS EmployeeEmail,
            ee.phone AS EmployeePhone,
            ee.status AS EmployeeStatus,
            ee.hiredate AS HireDate,
            -- User Details
            au.firstname AS UserFirstName,
            au.lastname AS UserLastName,
            au.email AS UserEmail,
            -- Employment Details
            edet.employmenttype AS EmploymentType,
            edet.paytype AS PayType,
            edet.basicsalary AS BasicSalary,
            edet.worklocation AS WorkLocation,
            edet.effectivedate AS EffectiveDate,
            edet.iscurrent AS IsCurrentEmployment
        FROM employee.positions ep
        LEFT JOIN employee.departments ed ON ep.departmentid = ed.departmentid
        LEFT JOIN auth.roles ar ON ep.roleid = ar.roleid
        LEFT JOIN employee.employmentdetails edet ON ep.positionid = edet.positionid AND edet.iscurrent = true
        LEFT JOIN employee.employees ee ON edet.employeeid = ee.employeeid
        LEFT JOIN auth.users au ON ee.userid = au.userid AND au.roleid = ep.roleid
        WHERE ep.organizationid = @organizationId
          AND (@departmentId IS NULL OR ep.departmentid = @departmentId)
          AND (@search IS NULL OR ep.positiontitle ILIKE '%' || @search || '%')
          AND (@status IS NULL OR ee.status = @status)
        ORDER BY ep.positiontitle, ee.firstname, ee.lastname";

            var results = await QueryAsync<PositionWithEmployees>(sql, new { organizationId, departmentId, search, status }, commandTimeout: 120);

            var positions = results
                .GroupBy(r => r.PositionId)
                .Select(g => new Position
                {
                    PositionId = g.Key,
                    OrganizationId = g.First().OrganizationId,
                    PositionTitle = g.First().PositionTitle,
                    DepartmentId = g.First().DepartmentId,
                    DepartmentName = g.First().DepartmentName,
                    Description = g.First().Description,
                    Requirements = g.First().Requirements,
                    SalaryRange = g.First().SalaryRange,
                    RoleId = g.First().RoleId,
                    RoleName = g.First().RoleName,
                    IsActive = g.First().IsActive,
                    CreatedAt = g.First().CreatedAt,
                    UpdatedAt = g.First().UpdatedAt,
                    EmploymentDetails = g.Where(x => x.EmployeeId != null)
                        .Select(e => new EmploymentDetail
                        {
                            EmployeeId = e.EmployeeId.Value,
                            PositionId = g.Key,
                            EmploymentType = e.EmploymentType,
                            PayType = e.PayType,
                            BasicSalary = e.BasicSalary,
                            WorkLocation = e.WorkLocation,
                            EffectiveDate = e.EffectiveDate ?? DateTime.UtcNow,
                            IsCurrent = e.IsCurrentEmployment ?? true,
                            Employee = new Employee
                            {
                                EmployeeId = e.EmployeeId.Value,
                                FirstName = e.EmployeeFirstName,
                                LastName = e.EmployeeLastName,
                                Email = e.EmployeeEmail,
                                Phone = e.EmployeePhone,
                                HireDate = e.HireDate ?? DateTime.UtcNow,
                                Status = e.EmployeeStatus
                            }
                        }).ToList()
                }).ToList();

            return positions;
        }
        catch (Exception)
        {
            throw;
        }
    }


    //Emloyee gettig of postion
    public async Task<List<PositionEmployeeDto>> GetEmployeesForPositionAsync(Guid organizationId, Guid positionId)
    {
        var sql = @"
        SELECT 
            ee.employeeid AS EmployeeId,
            ee.employeecode AS EmployeeCode,
            ee.firstname AS FirstName,
            ee.lastname AS LastName,
            ee.email AS Email,
            ee.status AS Status,
            ee.hiredate AS HireDate,
            ep.positionid AS PositionId,
            ep.positiontitle AS PositionTitle
        FROM employee.employmentdetails edet
        INNER JOIN employee.employees ee 
            ON edet.employeeid = ee.employeeid
        INNER JOIN employee.positions ep
            ON edet.positionid = ep.positionid
        WHERE edet.positionid = @PositionId
          AND edet.iscurrent = true
          AND ep.organizationid = @OrganizationId
        ORDER BY ee.firstname, ee.lastname;
    ";

        // Use helper QueryAsync method to keep consistent with ExecuteScalarAsync pattern
        var employees = await QueryAsync<PositionEmployeeDto>(sql, new
        {
            OrganizationId = organizationId,
            PositionId = positionId
        });

        return employees.ToList();
    }




    public async Task<Position> CreatePositionAsync(Position position)
    {
        var sql = @"
            INSERT INTO employee.positions (organizationid, positiontitle, departmentid, description, roleid)
            VALUES (@organizationId, @positionTitle, @departmentId, @description, @roleId)
            RETURNING positionid";

        var positionId = await ExecuteScalarAsync<Guid>(sql, new
        {
            organizationId = position.OrganizationId,
            positionTitle = position.PositionTitle,
            departmentId = position.DepartmentId,
            description = position.Description,
            roleId = position.RoleId
        });

        position.PositionId = positionId;
        return position;
    }

    public async Task<bool> UpdatePositionStatusAsync(
    Guid organizationId,
    Guid positionId,
    bool isActive)
    {
        var sql = @"
        UPDATE employee.positions
        SET 
            isactive = @isActive,
            updatedat = CURRENT_TIMESTAMP
        WHERE positionid = @positionId
          AND organizationid = @organizationId";

        var rowsAffected = await ExecuteAsync(sql, new
        {
            positionId = positionId,
            organizationId = organizationId,
            isActive = isActive
        });

        return rowsAffected > 0;
    }

    public async Task<bool> UpdateDepartmentStatusAsync(
    Guid organizationId,
    Guid departmentId,
    bool isActive)
    {
        var sql = @"
    UPDATE employee.departments
    SET 
        isactive = @isActive,
        updatedat = CURRENT_TIMESTAMP
    WHERE departmentid = @departmentId
      AND organizationid = @organizationId";

        var rowsAffected = await ExecuteAsync(sql, new
        {
            departmentId = departmentId,
            organizationId = organizationId,
            isActive = isActive
        });

        return rowsAffected > 0;
    }



    public async Task<Department?> GetDepartmentByIdAsync(Guid departmentId)
    {
        var sql = @"
            SELECT departmentid, organizationid, departmentname, description, 
                   managerid, isactive, createdat
            FROM employee.departments 
            WHERE departmentid = @departmentId";

        return await QuerySingleOrDefaultAsync<Department>(sql, new { departmentId });
    }

    public async Task<Position?> GetPositionByIdAsync(Guid positionId)
    {
        var sql = @"
            SELECT ep.positionid, ep.organizationid, ep.positiontitle, ep.departmentid, 
                   ep.description, ep.roleid, ar.rolename AS RoleName,
                   ep.isactive, ep.createdat
            FROM employee.positions ep
            LEFT JOIN auth.roles ar ON ep.roleid = ar.roleid
            WHERE ep.positionid = @positionId";

        return await QuerySingleOrDefaultAsync<Position>(sql, new { positionId });
    }

    public async Task<Department> UpdateDepartmentAsync(Department department)
    {
        var sql = @"
            UPDATE employee.departments 
            SET departmentname = @departmentName, 
                description = @description, 
                managerid = @managerId,
                updatedat = CURRENT_TIMESTAMP
            WHERE departmentid = @departmentId
            RETURNING departmentid, organizationid, departmentname, description, 
                     managerid, isactive, createdat, updatedat";

        var updatedDepartment = await QuerySingleAsync<Department>(sql, new
        {
            departmentName = department.DepartmentName,
            description = department.Description,
            managerId = department.ManagerId,
            departmentId = department.DepartmentId
        });

        return updatedDepartment;
    }

    public async Task<bool> DeleteDepartmentAsync(Guid departmentId)
    {
        var sql = @"
            UPDATE employee.departments 
            SET isactive = false, updatedat = CURRENT_TIMESTAMP 
            WHERE departmentid = @departmentId";

        var affected = await ExecuteAsync(sql, new { departmentId });
        return affected > 0;
    }

    public async Task<int> GetEmployeeCountByDepartmentAsync(Guid departmentId)
    {
        var sql = @"
            SELECT COUNT(*) 
            FROM employee.employees e
            INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid
            WHERE ed.departmentid = @departmentId AND e.status != 'deleted'";

        return await ExecuteScalarAsync<int>(sql, new { departmentId });
    }

    public async Task<Position> UpdatePositionAsync(Position position)
    {
        var sql = @"
            UPDATE employee.positions 
            SET positiontitle = @positionTitle, 
                departmentid = @departmentId,
                description = @description,
                roleid = @roleId,
                updatedat = CURRENT_TIMESTAMP
            WHERE positionid = @positionId
            RETURNING positionid, organizationid, positiontitle, departmentid, 
                     description, roleid, isactive, createdat, updatedat";

        var updatedPosition = await QuerySingleAsync<Position>(sql, new
        {
            positionTitle = position.PositionTitle,
            departmentId = position.DepartmentId,
            description = position.Description,
            roleId = position.RoleId,
            positionId = position.PositionId
        });

        return updatedPosition;
    }

    public async Task<bool> DeletePositionAsync(Guid positionId)
    {
        var sql = @"
            UPDATE employee.positions 
            SET isactive = false, updatedat = CURRENT_TIMESTAMP 
            WHERE positionid = @positionId";

        var affected = await ExecuteAsync(sql, new { positionId });
        return affected > 0;
    }

    public async Task<int> GetEmployeeCountByPositionAsync(Guid positionId)
    {
        try
        {
            var sql = @"
            SELECT COUNT(*) 
            FROM employee.employees e
            INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid
            WHERE ed.positionid = @positionId AND e.status != 'deleted'";

            return await ExecuteScalarAsync<int>(sql, new { positionId });
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<List<Employee>> GetEmployeesByPositionAsync(Guid positionId)
    {
        var sql = @"
            SELECT e.employeeid, e.organizationid, e.userid, e.employeecode, e.firstname, e.lastname,
                   e.email, e.phone, e.address, e.dateofbirth, e.gender, e.nationality, e.maritalstatus,
                   e.emergencycontact, e.hiredate, e.terminationdate, e.status, e.profilepictureurl,
                   e.createdat, e.updatedat, ed.positionid, ed.departmentid, ed.basicsalary
            FROM employee.employees e
            INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid
            WHERE ed.positionid = @positionId AND ed.iscurrent = true AND e.status != 'deleted'";

        var results = await QueryAsync<Employee>(sql, new { positionId });
        return results.ToList();
    }

    public async Task<List<Employee>> GetManagersAsync(Guid organizationId, Guid? departmentId = null)
    {
        var sql = @"
            SELECT DISTINCT 
                e.employeeid, e.organizationid, e.userid, e.employeecode,
                e.firstname, e.lastname, e.email, e.phone, e.address,
                e.dateofbirth, e.hiredate, e.status, e.createdat
            FROM employee.employees e
            LEFT JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
            LEFT JOIN employee.positions p ON ed.positionid = p.positionid
            LEFT JOIN auth.users u ON e.userid = u.userid
            LEFT JOIN auth.roles r ON u.roleid = r.roleid
            WHERE e.organizationid = @organizationId 
              AND e.status = 'active'";

        if (departmentId.HasValue && departmentId.Value != Guid.Empty)
        {
            sql += @"
              AND (
                  ed.departmentid = @departmentId
                  OR r.rolename ILIKE '%super admin%'
              )";
        }
        else
        {
            sql += @"
              AND (
                  -- Department managers
                  e.employeeid IN (SELECT managerid FROM employee.departments WHERE managerid IS NOT NULL AND isactive = true)
                  -- Position-based managers
                  OR p.positiontitle ILIKE '%manager%' 
                  OR p.positiontitle ILIKE '%lead%'
                  OR p.positiontitle ILIKE '%supervisor%'
                  OR p.positiontitle ILIKE '%director%'
                  -- Role-based managers
                  OR r.rolename ILIKE '%manager%'
                  OR r.rolename = 'Manager'
                  -- Reporting managers
                  OR e.employeeid IN (
                      SELECT DISTINCT reportingmanagerid 
                      FROM employee.employmentdetails 
                      WHERE reportingmanagerid IS NOT NULL AND iscurrent = true
                  )
              )";
        }

        sql += " ORDER BY e.firstname, e.lastname";

        var results = await QueryAsync<Employee>(sql, new { organizationId, departmentId });
        return results.ToList();
    }

    public async Task<Guid?> GetRoleIdByPositionIdAsync(Guid positionId)
    {
        const string query = @"
            SELECT roleid
            FROM employee.positions
            WHERE positionid = @PositionId;
        ";

        return await QuerySingleOrDefaultAsync<Guid?>(query, new { PositionId = positionId });
    }


    public async Task<List<Employee>> GetDepartmentEmployeesAndAdminsAsync(Guid organizationId, Guid departmentId)
    {
        var sql = @"
            SELECT DISTINCT
                e.employeeid, e.organizationid, e.userid, e.employeecode,
                e.firstname, e.lastname, e.email, e.phone, e.address,
                e.dateofbirth, e.hiredate, e.status, e.createdat
            FROM employee.employees e
            LEFT JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
            LEFT JOIN auth.users u ON e.userid = u.userid
            LEFT JOIN auth.roles r ON u.roleid = r.roleid
            WHERE e.organizationid = @organizationId
              AND e.status = 'active'
              AND (
                  -- All employees in the selected department
                  ed.departmentid = @departmentId
                  -- OR anyone with the Super Admin role (case-insensitive)
                  OR LOWER(r.rolename) = 'super admin'
              )
            ORDER BY e.firstname, e.lastname";

        var results = await QueryAsync<Employee>(sql, new { organizationId, departmentId });
        return results.ToList();
    }


}
