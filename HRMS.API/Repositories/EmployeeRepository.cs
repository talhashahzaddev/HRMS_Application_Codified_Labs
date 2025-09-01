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
                departmentId = (Guid?)null,
                positionId = (Guid?)null,
                basicSalary = (decimal?)null,
                address = System.Text.Json.JsonSerializer.Serialize(employee.Address)
            });

        employee.EmployeeId = employeeId;
        return employee;
    }

    public async Task<Employee?> GetEmployeeByIdAsync(Guid employeeId)
    {
        var sql = @"
            SELECT employee_id, organization_id, user_id, employee_code, first_name, last_name,
                   email, phone, address, date_of_birth, gender, nationality, marital_status,
                   emergency_contact, hire_date, termination_date, status, profile_picture_url,
                   created_at, updated_at
            FROM employee.employees 
            WHERE employee_id = @employeeId";

        return await QuerySingleOrDefaultAsync<Employee>(sql, new { employeeId });
    }

    public async Task<Employee?> GetEmployeeByCodeAsync(Guid organizationId, string employeeCode)
    {
        var sql = @"
            SELECT employee_id, organization_id, user_id, employee_code, first_name, last_name,
                   email, phone, date_of_birth, hire_date, status, created_at
            FROM employee.employees 
            WHERE organization_id = @organizationId AND employee_code = @employeeCode";

        var result = await QuerySingleOrDefaultAsync<Employee>(sql, new { organizationId, employeeCode });
        return result;
    }

    public async Task<Employee?> GetEmployeeByEmailAsync(Guid organizationId, string email)
    {
        var sql = @"
            SELECT employee_id, organization_id, user_id, employee_code, first_name, last_name,
                   email, phone, date_of_birth, hire_date, status, created_at
            FROM employee.employees 
            WHERE organization_id = @organizationId AND email = @email";

        var result = await QuerySingleOrDefaultAsync<Employee>(sql, new { organizationId, email });
        return result;
    }

    public async Task<PagedResult<EmployeeListItemDto>> GetEmployeesAsync(
        Guid organizationId, string? searchTerm = null, Guid? departmentId = null, 
        string? status = null, int page = 1, int pageSize = 50)
    {
        var results = await QueryAsync<dynamic>(
            "SELECT * FROM employee.fn_get_employees(@organizationId, @searchTerm, @departmentId, @status, @limit, @offset)",
            new 
            { 
                organizationId, 
                searchTerm, 
                departmentId, 
                status, 
                limit = pageSize, 
                offset = (page - 1) * pageSize 
            });

        var employees = results.Select(r => new EmployeeListItemDto
        {
            EmployeeId = r.employee_id,
            EmployeeCode = r.employee_code,
            FullName = $"{r.first_name} {r.last_name}",
            Email = r.email,
            Phone = r.phone,
            HireDate = r.hire_date,
            Status = r.status,
            DepartmentName = r.department_name,
            PositionTitle = r.position_title
        }).ToList();

        var totalCount = results.FirstOrDefault()?.total_count ?? 0;

        return new PagedResult<EmployeeListItemDto>
        {
            Data = employees,
            TotalCount = (int)totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> UpdateEmployeeAsync(Employee employee)
    {
        var sql = @"
            UPDATE employee.employees 
            SET first_name = @firstName, last_name = @lastName, phone = @phone,
                date_of_birth = @dateOfBirth, status = @status, updated_at = CURRENT_TIMESTAMP
            WHERE employee_id = @employeeId";

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
            SET status = 'deleted', updated_at = CURRENT_TIMESTAMP 
            WHERE employee_id = @employeeId";

        var affected = await ExecuteAsync(sql, new { employeeId });
        return affected > 0;
    }

    public async Task<List<Department>> GetDepartmentsAsync(Guid organizationId)
    {
        var sql = @"
            SELECT department_id, organization_id, department_name, description, 
                   manager_id, parent_department_id, budget, is_active, created_at, updated_at
            FROM employee.departments 
            WHERE organization_id = @organizationId AND is_active = true
            ORDER BY department_name";

        var results = await QueryAsync<Department>(sql, new { organizationId });
        return results.ToList();
    }

    public async Task<Department> CreateDepartmentAsync(Department department)
    {
        var sql = @"
            INSERT INTO employee.departments (organization_id, department_name, description, manager_id)
            VALUES (@organizationId, @departmentName, @description, @managerId)
            RETURNING department_id";

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

    public async Task<List<Position>> GetPositionsAsync(Guid organizationId, Guid? departmentId = null)
    {
        var sql = @"
            SELECT position_id, organization_id, position_title, department_id, 
                   description, requirements, salary_range, is_active, created_at, updated_at
            FROM employee.positions 
            WHERE organization_id = @organizationId 
              AND (@departmentId IS NULL OR department_id = @departmentId)
              AND is_active = true
            ORDER BY position_title";

        var results = await QueryAsync<Position>(sql, new { organizationId, departmentId });
        return results.ToList();
    }

    public async Task<Position> CreatePositionAsync(Position position)
    {
        var sql = @"
            INSERT INTO employee.positions (organization_id, position_title, department_id, description)
            VALUES (@organizationId, @positionTitle, @departmentId, @description)
            RETURNING position_id";

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
            SELECT department_id, organization_id, department_name, description, 
                   manager_id, is_active, created_at
            FROM employee.departments 
            WHERE department_id = @departmentId";

        return await QuerySingleOrDefaultAsync<Department>(sql, new { departmentId });
    }

    public async Task<Position?> GetPositionByIdAsync(Guid positionId)
    {
        var sql = @"
            SELECT position_id, organization_id, position_title, department_id, 
                   description, is_active, created_at
            FROM employee.positions 
            WHERE position_id = @positionId";

        return await QuerySingleOrDefaultAsync<Position>(sql, new { positionId });
    }
}
