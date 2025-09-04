using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using System.Data;
using Dapper;

namespace HRMS.API.Repositories;

public class EmployeeRepository : BaseRepository, IEmployeeRepository
{
    public EmployeeRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<Employee> CreateEmployeeAsync(Employee employee)
    {
        try
        {
        var employeeId = await ExecuteScalarAsync<Guid>(
            @"SELECT employee.fn_create_employee(
                @organizationId, @userId, @employeeCode, @firstName, @lastName, 
                @email, @phone, @dateOfBirth, @hireDate, @departmentId, 
                @positionId, @basicSalary, @address::jsonb)",
            new
            {
                organizationId = employee.OrganizationId,
                userId = employee.UserId,
                employeeCode = employee.EmployeeCode,
                firstName = employee.FirstName,
                lastName = employee.LastName,
                email = employee.Email,
                phone = employee.Phone,
                dateOfBirth = employee.DateOfBirth,
                hireDate = employee.HireDate,
                departmentId = employee.DepartmentId,
                positionId = employee.PositionId,     
                basicSalary = employee.BasicSalary,
                address = System.Text.Json.JsonSerializer.Serialize(employee.Address)
            });

        employee.EmployeeId = employeeId;
        return employee;
        }
        catch (Exception ex)
        {
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
            SELECT departmentid, organizationid, departmentname, description, 
                   managerid, parentdepartmentid, budget, isactive, createdat, updatedat
            FROM employee.departments 
            WHERE organizationid = @organizationId AND isactive = true
            ORDER BY departmentname";

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
        var sql = @"
            SELECT positionid, organizationid, positiontitle, departmentid, 
                   description, requirements, salaryrange, isactive, createdat, updatedat
            FROM employee.positions 
            WHERE organizationid = @organizationId 
              AND (@departmentId IS NULL OR departmentid = @departmentId)
              AND isactive = true
            ORDER BY positiontitle";

        var results = await QueryAsync<Position>(sql, new { organizationId, departmentId });
        return results.ToList();
    }

    public async Task<Position> CreatePositionAsync(Position position)
    {
        var sql = @"
            INSERT INTO employee.positions (organizationid, positiontitle, departmentid, description)
            VALUES (@organizationId, @positionTitle, @departmentId, @description)
            RETURNING positionid";

        var positionId = await ExecuteScalarAsync<Guid>(sql, new
        {
            organizationId = position.OrganizationId,
            positionTitle = position.PositionTitle,
            departmentId = position.DepartmentId,
            description = position.Description
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
            SELECT positionid, organizationid, positiontitle, departmentid, 
                   description, isactive, createdat
            FROM employee.positions 
            WHERE positionid = @positionId";

        return await QuerySingleOrDefaultAsync<Position>(sql, new { positionId });
    }
}
