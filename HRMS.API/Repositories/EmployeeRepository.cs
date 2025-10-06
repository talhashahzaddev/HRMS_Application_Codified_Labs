using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using System.Data;
using Dapper;
using Npgsql;

namespace HRMS.API.Repositories;

public class EmployeeRepository : BaseRepository, IEmployeeRepository
{
    public EmployeeRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<Employee> CreateEmployeeAsync(Employee employee)
    {
        using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var employeeId = await connection.ExecuteScalarAsync<Guid>(
                @"SELECT employee.fn_create_employee(
                @organizationId, @userId, @employeeCode, @firstName, @lastName, 
                @email, @phone, @dateOfBirth, @hireDate, @departmentId, 
                @positionId, @basicSalary, @address::jsonb, @workLocation, @reportingManagerId)",
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
                    reportingManagerId = employee.ReportingManagerId
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
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Employee?> GetEmployeeByIdAsync(Guid employeeId)
    {
        var sql = @"
            SELECT employeeid, organizationid, userid, employeecode, firstname, lastname,
                   email, phone, address, dateofbirth, gender, nationality, maritalstatus,
                   emergencycontact, hiredate, terminationdate, status, profilepictureurl,
                   createdat, updatedat
            FROM employee.employees 
            WHERE employeeid = @employeeId";

        return await QuerySingleOrDefaultAsync<Employee>(sql, new { employeeId });
    }

    public async Task<Employee?> GetEmployeeByCodeAsync(Guid organizationId, string employeeCode)
    {
        var sql = @"
            SELECT employeeid, organizationid, userid, employeecode, firstname, lastname,
                   email, phone, dateofbirth, hiredate, status, createdat
            FROM employee.employees 
            WHERE organizationid = @organizationId AND employeecode = @employeeCode";

        var result = await QuerySingleOrDefaultAsync<Employee>(sql, new { organizationId, employeeCode });
        return result;
    }

    public async Task<Employee?> GetEmployeeByEmailAsync(Guid organizationId, string email)
    {
        var sql = @"
            SELECT employeeid, organizationid, userid, employeecode, firstname, lastname,
                   email, phone, dateofbirth, hiredate, status, createdat
            FROM employee.employees 
            WHERE organizationid = @organizationId AND email = @email";

        var result = await QuerySingleOrDefaultAsync<Employee>(sql, new { organizationId, email });
        return result;
    }

    public async Task<PagedResult<EmployeeListItemDto>> GetEmployeesAsync(
    Guid? organizationId = null, string? searchTerm = null, Guid? departmentId = null,
    string? status = null, int page = 1, int pageSize = 50)
{
    try
    {
        var results = await QueryAsync<dynamic>(
            @"SELECT * 
              FROM employee.fn_get_employees(
                   @organizationId, 
                   @searchTerm, 
                   @departmentId, 
                   @status, 
                   @limit, 
                   @offset)",
            new
            {
                organizationId = organizationId == Guid.Empty ? null : organizationId,
                searchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm,
                departmentId = departmentId == Guid.Empty ? null : departmentId,
                status = string.IsNullOrWhiteSpace(status) ? null : status,
                limit = pageSize,
                offset = (page - 1) * pageSize
            });

        var employees = results.Select(r => new EmployeeListItemDto
        {
            EmployeeId = r.employeeid != null ? (Guid)r.employeeid : Guid.Empty,
            EmployeeCode = r.employeecode,
            FullName = $"{(string?)r.firstname ?? ""} {(string?)r.lastname ?? ""}".Trim(),
            Email = r.email,
            Phone = r.phone,
            HireDate = r.hiredate,
            Status = r.status,
            DepartmentName = r.departmentname,
            PositionTitle = r.positiontitle
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
        // You can log ex here if needed
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

    public async Task<bool> DeleteEmployeeAsync(Guid employeeId)
    {
        var sql = @"
            UPDATE employee.employees 
            SET status = 'deleted', updatedat = CURRENT_TIMESTAMP 
            WHERE employeeid = @employeeId";

        var affected = await ExecuteAsync(sql, new { employeeId });
        return affected > 0;
    }

    public async Task<List<Department>> GetDepartmentsAsync(Guid organizationId)
    {
        try
        {
        var sql = @"
            SELECT d.departmentid, d.organizationid, d.departmentname, d.description, 
                   d.managerid, d.parentdepartmentid, d.budget, d.isactive, d.createdat, d.updatedat,
                   COALESCE(emp_count.employee_count, 0) as employeecount
            FROM employee.departments d
            LEFT JOIN (
                SELECT ed.departmentid, COUNT(e.employeeid) as employee_count
                FROM employee.employmentdetails ed
                INNER JOIN employee.employees e ON ed.employeeid = e.employeeid
                WHERE ed.iscurrent = true AND e.organizationid = @organizationId
                GROUP BY ed.departmentid
            ) emp_count ON d.departmentid = emp_count.departmentid
            WHERE d.organizationid = @organizationId AND d.isactive = true
            ORDER BY d.departmentname";

        var results = await QueryAsync<Department>(sql, new { organizationId });
        return results.ToList();

        }
        catch (Exception ex)
        {
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

    public async Task<List<Position>> GetPositionsAsync(Guid organizationId, Guid? departmentId = null)
    {
        try
        {
            var sql = @"
            SELECT ep.positionid, ep.organizationid, ep.positiontitle, 
                   ep.departmentid, ed.departmentname AS DepartmentName, 
                   ep.description, ep.requirements, ep.salaryrange, 
                   ep.roleid, ar.rolename AS RoleName,
                   ep.isactive, ep.createdat, ep.updatedat
            FROM employee.positions ep
            LEFT JOIN employee.departments ed ON ep.departmentid = ed.departmentid
            LEFT JOIN auth.roles ar ON ep.roleid = ar.roleid
            WHERE ep.organizationid = @organizationId 
              AND ep.isactive = true
            ORDER BY ep.positiontitle";

            var results = await QueryAsync<Position>(sql, new { organizationId, departmentId });
            return results.ToList();
        }
        catch (Exception)
        {
            throw;
        }
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
            INNER JOIN employee.employment_details ed ON e.employeeid = ed.employeeid
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
        var sql = @"
            SELECT COUNT(*) 
            FROM employee.employees e
            INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid
            WHERE ed.positionid = @positionId AND e.status != 'deleted'";

        return await ExecuteScalarAsync<int>(sql, new { positionId });
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

    public async Task<List<Employee>> GetManagersAsync(Guid organizationId)
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
              AND e.status = 'active'
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
              )
            ORDER BY e.firstname, e.lastname";

        var results = await QueryAsync<Employee>(sql, new { organizationId });
        return results.ToList();
    }


}
