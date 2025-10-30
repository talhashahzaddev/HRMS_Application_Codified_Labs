using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Newtonsoft.Json;
using System.Data;

namespace HRMS.API.Repositories;

public class PayrollRepository : BaseRepository, IPayrollRepository
{
    public PayrollRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    #region Salary Component Operations

    public async Task<SalaryComponent> CreateSalaryComponentAsync(Guid organizationId, CreateSalaryComponentRequest request)
    {
        try
        {
            var sql = @"
            INSERT INTO payroll.salarycomponents 
            (organizationid, name, code, type, calculationtype, value, isactive, createdat)
            VALUES 
            (@OrganizationId, @Name, @Code, @Type, @CalculationType, @Value, @IsActive, NOW())
            RETURNING componentid, createdat";

            var parameters = new
            {
                OrganizationId = organizationId,
                request.Name,
                request.Code,
                request.Type,
                request.CalculationType,
                request.Value,
                request.IsActive
            };

            var result = await QuerySingleAsync<dynamic>(sql, parameters);

            return new SalaryComponent
            {
                ComponentId = result.componentid,
                OrganizationId = organizationId,
                Name = request.Name,
                Code = request.Code,
                Type = request.Type,
                CalculationType = request.CalculationType,
                Value = request.Value,
                IsActive = request.IsActive,
                CreatedAt = result.createdat
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<List<SalaryComponentDto>> GetSalaryComponentsAsync(Guid organizationId)
    {
        try
        {
            var sql = @"
            SELECT 
                componentid, organizationid, name, code, type, 
                calculationtype, value, isactive, createdat, updatedat
            FROM payroll.salarycomponents
            WHERE organizationid = @OrganizationId
            ORDER BY name";

            return (await QueryAsync<SalaryComponentDto>(sql, new { OrganizationId = organizationId })).ToList();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<SalaryComponentDto?> GetSalaryComponentByIdAsync(Guid componentId)
    {
        try
        {
            var sql = @"
            SELECT 
                componentid, organizationid, name, code, type, 
                calculationtype, value, isactive, createdat, updatedat
            FROM payroll.salarycomponents
            WHERE componentid = @ComponentId";

            return await QuerySingleOrDefaultAsync<SalaryComponentDto>(sql, new { ComponentId = componentId });
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<SalaryComponent> UpdateSalaryComponentAsync(Guid componentId, UpdateSalaryComponentRequest request)
    {
        try
        {
            var sql = @"
            UPDATE payroll.salarycomponents
            SET 
                name = @Name,
                code = @Code,
                type = @Type,
                calculationtype = @CalculationType,
                value = @Value,
                isactive = @IsActive,
                updatedat = NOW()
            WHERE componentid = @ComponentId
            RETURNING componentid, organizationid, name, code, type, calculationtype, value, isactive, createdat, updatedat";

            var parameters = new
            {
                ComponentId = componentId,
                request.Name,
                request.Code,
                request.Type,
                request.CalculationType,
                request.Value,
                request.IsActive
            };

            return await QuerySingleAsync<SalaryComponent>(sql, parameters);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> DeleteSalaryComponentAsync(Guid componentId)
    {
        try
        {
            var sql = @"
            DELETE FROM payroll.salarycomponents
            WHERE componentid = @ComponentId";

            var rowsAffected = await ExecuteAsync(sql, new { ComponentId = componentId });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    #endregion

    #region Salary Rule Operations

    public async Task<SalaryRule> CreateSalaryRuleAsync(Guid organizationId, CreateSalaryRuleRequest request)
    {
        try
        {
            var sql = @"
            INSERT INTO payroll.salaryrules 
            (rulename,organizationid, departmentid, positionid, componentid, value_override, createdat)
            VALUES 
            (@RuleName,@OrganizationId, @DepartmentId, @PositionId, @ComponentId, @ValueOverride, NOW())
            RETURNING ruleid, createdat";

            var parameters = new
            {
                request.RuleName,
                OrganizationId = organizationId,
                request.DepartmentId,
                request.PositionId,
                request.ComponentId,
                request.ValueOverride
            };

            var result = await QuerySingleAsync<dynamic>(sql, parameters);

            return new SalaryRule
            {
                RuleId = result.ruleid,
                RuleName= request.RuleName,
                OrganizationId = organizationId,
                DepartmentId = request.DepartmentId,
                PositionId = request.PositionId,
                ComponentId = request.ComponentId,
                ValueOverride = request.ValueOverride,
                CreatedAt = result.createdat
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<List<SalaryRuleDto>> GetSalaryRulesAsync(Guid organizationId)
    {
        try
        {
            var sql = @"
            SELECT 
                sr.rulename,sr.ruleid, sr.organizationid, sr.departmentid, d.departmentname, 
                sr.positionid, p.positiontitle, sr.componentid, sc.name as componentname, 
                sc.code as componentcode, sc.type as componenttype, 
                sc.calculationtype as calculationtype, sc.value as componentvalue,
                COALESCE(sr.value_override, sc.value) AS valueoverride,
                sr.createdat, sr.updatedat
            FROM payroll.salaryrules sr
            JOIN payroll.salarycomponents sc ON sr.componentid = sc.componentid
            LEFT JOIN employee.departments d ON sr.departmentid = d.departmentid
            LEFT JOIN employee.positions p ON sr.positionid = p.positionid
            WHERE sr.organizationid = @OrganizationId
            ORDER BY d.departmentname, p.positiontitle, sc.name";

            return (await QueryAsync<SalaryRuleDto>(sql, new { OrganizationId = organizationId })).ToList();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<SalaryRuleDto?> GetSalaryRuleByIdAsync(Guid ruleId)
    {
        try
        {
            var sql = @"
            SELECT 
                sr.rulename,sr.ruleid, sr.organizationid, sr.departmentid, d.departmentname, 
                sr.positionid, p.positiontitle, sr.componentid, sc.name as componentname, 
                sc.code as componentcode, sc.type as componenttype, 
                sc.calculationtype as calculationtype, sc.value as componentvalue,
                sr.value_override as valueoverride, sr.createdat, sr.updatedat
            FROM payroll.salaryrules sr
            JOIN payroll.salarycomponents sc ON sr.componentid = sc.componentid
            LEFT JOIN employee.departments d ON sr.departmentid = d.departmentid
            LEFT JOIN employee.positions p ON sr.positionid = p.positionid
            WHERE sr.ruleid = @RuleId";

            return await QuerySingleOrDefaultAsync<SalaryRuleDto>(sql, new { RuleId = ruleId });
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<SalaryRule> UpdateSalaryRuleAsync(Guid ruleId, UpdateSalaryRuleRequest request)
    {
        try
        {
            var sql = @"
            UPDATE payroll.salaryrules
            SET 
                rulename = @rulename,
                departmentid = @DepartmentId,
                positionid = @PositionId,
                componentid = @ComponentId,
                value_override = @ValueOverride,
                updatedat = NOW()
            WHERE ruleid = @RuleId
            RETURNING ruleid, organizationid, departmentid, positionid, componentid, value_override as valueoverride, createdat, updatedat";

            var parameters = new
            {
                request.RuleName,
                RuleId = ruleId,
                request.DepartmentId,
                request.PositionId,
                request.ComponentId,
                request.ValueOverride
            };

            return await QuerySingleAsync<SalaryRule>(sql, parameters);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> DeleteSalaryRuleAsync(Guid ruleId)
    {
        try
        {
            var sql = @"
            DELETE FROM payroll.salaryrules
            WHERE ruleid = @RuleId";

            var rowsAffected = await ExecuteAsync(sql, new { RuleId = ruleId });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    #endregion


    public async Task<PayrollPeriod> CreatePayrollPeriodAsync(Guid organizationId, string periodName, DateTime startDate, DateTime endDate, DateTime? payDate = null)
    {
        try
        {

        var periodId = await ExecuteScalarAsync<Guid>(
            "SELECT payroll.fn_create_payroll_period(@p_organization_id, @p_period_name, @p_start_date::date, @p_end_date::date, @p_pay_date::date)",
            new
            {
                p_organization_id = organizationId,
                p_period_name = periodName,
                p_start_date = startDate,
                p_end_date = endDate,
                p_pay_date = payDate
            });

            return new PayrollPeriod
        {
            PeriodId = periodId,
            OrganizationId = organizationId,
            PeriodName = periodName,
            StartDate = startDate,
            EndDate = endDate,
            PayDate = payDate
        };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<List<PayrollPeriodDto>> GetPayrollPeriodsAsync(Guid organizationId)
    {
        var sql = @"
            SELECT periodid, periodname, startdate, enddate, paydate, status,
                   totalemployees, totalgrossamount, totaltaxamount, totalnetamount,
                   processedat
            FROM payroll.payrollperiods
            WHERE organizationid = @organizationId
            ORDER BY startdate DESC";

        var results = await QueryAsync<PayrollPeriodDto>(sql, new { organizationId });
        return results.ToList();
    }

    public async Task<PayrollPeriod?> GetPayrollPeriodByIdAsync(Guid periodId)
    {
        var sql = @"
            SELECT periodid, organizationid, periodname, startdate, enddate, paydate,
                   status, totalemployees, totalgrossamount, totaltaxamount, totalnetamount,
                   processedby, processedat, createdat
            FROM payroll.payrollperiods
            WHERE periodid = @periodId";

        return await QuerySingleOrDefaultAsync<PayrollPeriod>(sql, new { periodId });
    }

    public async Task<int> CalculatePayrollAsync(Guid periodId, Guid? ruleId)
    {try
        {

        // Get the payroll period to determine organization
        var period = await GetPayrollPeriodByIdAsync(periodId);
        if (period == null)
        {
            throw new KeyNotFoundException($"Payroll period with ID {periodId} not found");
        }
        
        var organizationId = period.OrganizationId;
        
        // Get all employees eligible for this payroll period
        var employeesSql = @"
            SELECT e.employeeid, e.organizationid, ed.departmentid, ed.positionid, ed.basicsalary
            FROM employee.employees e
            JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid
            WHERE e.organizationid = @OrganizationId
            AND e.status = 'active'";
            
        var employees = await QueryAsync<dynamic>(employeesSql, new { OrganizationId = organizationId });
        
        int entriesCreated = 0;
        
        foreach (var employee in employees)
        {
            decimal basicSalary = (decimal)(employee.basicsalary ?? 0m);
            decimal totalAllowances = 0m;
            decimal totalDeductions = 0m;
            
            // Get applicable salary components. If a specific rule is selected, limit to that rule.
            string componentsSql;
            object componentsParams;

            if (ruleId.HasValue)
            {
                componentsSql = @"
                SELECT 
                    sc.componentid, sc.name, sc.type, sc.calculationtype, sc.value,
                    sr.value_override
                FROM payroll.salarycomponents sc
                INNER JOIN payroll.salaryrules sr ON sc.componentid = sr.componentid
                WHERE sc.organizationid = @OrganizationId
                  AND sc.isactive = true
                  AND sr.ruleid = @RuleId";
                componentsParams = new { OrganizationId = organizationId, RuleId = ruleId.Value };
            }
            else
            {
                componentsSql = @"
                SELECT 
                    sc.componentid, sc.name, sc.type, sc.calculationtype, sc.value,
                    sr.value_override
                FROM payroll.salarycomponents sc
                LEFT JOIN payroll.salaryrules sr ON sc.componentid = sr.componentid
                WHERE sc.organizationid = @OrganizationId
                  AND sc.isactive = true
                  AND (
                      (sr.departmentid = @DepartmentId OR sr.departmentid IS NULL)
                      AND (sr.positionid = @PositionId OR sr.positionid IS NULL)
                  )";
                componentsParams = new { OrganizationId = organizationId, DepartmentId = employee.departmentid, PositionId = employee.positionid };
            }

            var components = await QueryAsync<dynamic>(componentsSql, componentsParams);
            
            // Calculate each component
            foreach (var component in components)
            {
                decimal componentValue = 0m;

                // Use value_override if available, otherwise use the component's default value
                decimal valueToUse = component.value_override != null ? (decimal)component.value_override : (decimal)component.value;

                // Calculate based on type
                string calculationType = component.calculationtype?.ToString()?.ToLower() ?? "fixed";
                if (calculationType == "percentage")
                {
                    componentValue = (basicSalary * valueToUse) / 100m;
                }
                else // fixed
                {
                    componentValue = valueToUse;
                }

                // Apply based on component type: 'allowance' adds, 'deduction' subtracts
                string compType = component.type?.ToString()?.ToLower() ?? string.Empty;
                if (compType == "allowance")
                {
                    totalAllowances += componentValue;
                }
                else if (compType == "deduction")
                {
                    totalDeductions += componentValue;
                }
            }

            decimal grossSalary = basicSalary + totalAllowances;
            decimal netSalary = grossSalary - totalDeductions;
            
            // Insert payroll entry
            var insertSql = @"
                INSERT INTO payroll.payrollentries (
                    entryid, periodid, employeeid, basicsalary, 
                    allowances, deductions, overtimeamount, bonusamount, grosssalary, taxamount, 
                    netsalary, status, calculatedat, createdat, updatedat
                )
                VALUES (
                    uuid_generate_v4(), @PeriodId, @EmployeeId, @BasicSalary,
                    @Allowances::jsonb, @Deductions::jsonb, @OvertimeAmount, @BonusAmount, @GrossSalary, @TaxAmount,
                    @NetSalary, 'calculated', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                )";
                
            await ExecuteAsync(insertSql, new {
                PeriodId = periodId,
                EmployeeId = employee.employeeid,
                BasicSalary = basicSalary,
                // Store totals as jsonb numbers
                Allowances = JsonConvert.SerializeObject(totalAllowances),
                Deductions = JsonConvert.SerializeObject(totalDeductions),
                OvertimeAmount = 0m,
                BonusAmount = totalAllowances,
                GrossSalary = grossSalary,
                // As requested: reflect deductions in TaxAmount
                TaxAmount = totalDeductions,
                NetSalary = netSalary
            });
            
            entriesCreated++;
        }
        
        return entriesCreated;
        }
        catch (Exception ex)
        {
            throw;
        }

    }

    public async Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesAsync(Guid periodId, int page = 1, int pageSize = 50)
    {
        var sql = @"
            SELECT pe.entryid, pe.employeeid, pe.basicsalary, pe.overtimeamount,
                   pe.bonusamount, pe.grosssalary, pe.taxamount, pe.netsalary,
                   pe.currency, pe.status, pe.calculatedat, pe.paidat,
                   CONCAT(e.firstname, ' ', e.lastname) as employeename,
                   e.employee_code
            FROM payroll.payrollentries pe
            INNER JOIN employee.employees e ON pe.employeeid = e.employeeid
            WHERE pe.periodid = @periodId
            ORDER BY e.firstname, e.lastname
            LIMIT @pageSize OFFSET @offset";

        var results = await QueryAsync<PayrollEntryDto>(sql, new 
        { 
            periodId, 
            pageSize, 
            offset = (page - 1) * pageSize 
        });

        var countSql = "SELECT COUNT(*) FROM payroll.payrollentries WHERE periodid = @periodId";
        var totalCount = await ExecuteScalarAsync<int>(countSql, new { periodId });

        return new PagedResult<PayrollEntryDto>
        {
            Data = results.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PayrollEntry?> GetPayrollEntryAsync(Guid entryId)
    {
        var sql = @"
            SELECT entryid, periodid, employeeid, basicsalary, allowances, deductions,
                   overtimeamount, bonusamount, grosssalary, taxamount, otherdeductions,
                   netsalary, currency, status, calculatedat, paidat, paymentmethod,
                   paymentreference, createdat, updatedat
            FROM payroll.payrollentries
            WHERE entryid = @entryId";

        return await QuerySingleOrDefaultAsync<PayrollEntry>(sql, new { entryId });
    }

    public async Task<bool> UpdatePayrollPeriodStatusAsync(Guid periodId, string status)
    {
        var sql = @"
            UPDATE payroll.payrollperiods 
            SET status = @status, updatedat = CURRENT_TIMESTAMP
            WHERE periodid = @periodId";

        var affected = await ExecuteAsync(sql, new { periodId, status });
        return affected > 0;
    }

    public async Task<PayrollSummaryDto> GetPayrollSummaryAsync(Guid periodId)
    {
        var sql = @"
            SELECT 
                COALESCE(SUM(gross_salary), 0) as total_gross_salary,
                COALESCE(SUM(tax_amount), 0) as total_tax,
                COALESCE(SUM(net_salary), 0) as total_net_salary,
                COUNT(*) as total_employees
            FROM payroll.payroll_entries
            WHERE period_id = @periodId";

        var result = await QuerySingleAsync<dynamic>(sql, new { periodId });

        return new PayrollSummaryDto
        {
            TotalGrossSalary = result.total_gross_salary,
            TotalTax = result.total_tax,
            TotalNetSalary = result.total_net_salary,
            TotalEmployees = result.total_employees
        };
    }

    public async Task<bool> ProcessPayrollPaymentAsync(Guid entryId, string paymentMethod, string paymentReference)
    {
        var sql = @"
            UPDATE payroll.payroll_entries 
            SET paymentmethod = @paymentMethod, paymentreference = @paymentReference,
                paidat = CURRENT_TIMESTAMP, status = 'paid', updatedat = CURRENT_TIMESTAMP
            WHERE entryid = @entryId";

        var affected = await ExecuteAsync(sql, new { entryId, paymentMethod, paymentReference });
        return affected > 0;
    }

    public async Task<PayrollPeriodDto?> GetCurrentPayrollPeriodAsync(Guid organizationId)
    {
        var sql = @"
            SELECT periodid as periodid, organizationid as organizationid, periodname as periodname, 
                   startdate as startdate, enddate as enddate, paydate as paydate, status,
                   totalemployees as totalemployees, totalgrossamount as totalgrossamount, 
                   totaltaxamount as totaltaxamount, totalnetamount as totalnetamount,
                   processedat as processedat
            FROM payroll.payrollperiods
            WHERE organizationid = @organizationId 
              AND status IN ('active', 'processing')
              AND startdate <= CURRENT_DATE 
              AND enddate >= CURRENT_DATE
            ORDER BY startdate DESC
            LIMIT 1";

        return await QuerySingleOrDefaultAsync<PayrollPeriodDto>(sql, new { organizationId });
    }

    public async Task<PayrollPeriod> UpdatePayrollPeriodAsync(Guid periodId, UpdatePayrollPeriodRequest request)
    {
        var sql = @"
            UPDATE payroll.payrollperiods 
            SET periodname = @periodName, startdate = @startDate, enddate = @endDate, 
                status = @status, updatedat = CURRENT_TIMESTAMP
            WHERE periodid = @periodId
            RETURNING periodid, organizationid, periodname, startdate, enddate, paydate, status";

        var result = await QuerySingleAsync<PayrollPeriod>(sql, new 
        { 
            periodId, 
            periodName = request.PeriodName, 
            startDate = request.StartDate, 
            endDate = request.EndDate, 
            status = request.Status 
        });

        return result;
    }

    public async Task<bool> DeletePayrollPeriodAsync(Guid periodId)
    {
        // First check if there are any entries
        var entriesExist = await ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM payroll.payrollentries WHERE periodid = @periodId)",
            new { periodId });

        if (entriesExist)
        {
            throw new InvalidOperationException("Cannot delete payroll period with existing entries");
        }

        var sql = "DELETE FROM payroll.payrollperiods WHERE periodid = @periodId";
        var affected = await ExecuteAsync(sql, new { periodId });
        return affected > 0;
    }

    public async Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesFilteredAsync(Guid organizationId, PayrollFilterRequest filter, int page, int limit)
    {
        try
        {

        var whereConditions = new List<string> { "e.organizationid = @organizationId" };
        var parameters = new Dictionary<string, object> { { "organizationId", organizationId } };

        if (filter.PayrollPeriodId.HasValue)
        {
            whereConditions.Add("pe.periodid = @payrollPeriodId");
            parameters.Add("payrollPeriodId", filter.PayrollPeriodId.Value);
        }

        if (filter.EmployeeId.HasValue)
        {
            whereConditions.Add("pe.employeeid = @employeeId");
            parameters.Add("employeeId", filter.EmployeeId.Value);
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            whereConditions.Add("pe.status = @status");
            parameters.Add("status", filter.Status);
        }

        if (!string.IsNullOrEmpty(filter.Search))
        {
            whereConditions.Add("(LOWER(e.firstname || ' ' || e.lastname) LIKE LOWER(@search) OR LOWER(e.employeecode) LIKE LOWER(@search))");
            parameters.Add("search", $"%{filter.Search}%");
        }

        if (!string.IsNullOrEmpty(filter.Department))
        {
            whereConditions.Add("d.departmentname = @department");
            parameters.Add("department", filter.Department);
        }

        var whereClause = string.Join(" AND ", whereConditions);
        
        var sql = $@"
            SELECT pe.entryid as entry_id, pe.employeeid as employee_id, pe.basicsalary as basic_salary, 
                   pe.overtimeamount as overtime_amount, pe.bonusamount as bonus_amount, 
                   pe.grosssalary as gross_salary, pe.taxamount as tax_amount, pe.netsalary as net_salary,
                   pe.currency, pe.status, pe.calculatedat as calculated_at, pe.paidat as paid_at,
                   CONCAT(e.firstname, ' ', e.lastname) as employee_name, e.employeecode as employee_code,
                   d.departmentname as department_name
            FROM payroll.payrollentries pe
            INNER JOIN employee.employees e ON pe.employeeid = e.employeeid
            INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
            INNER JOIN employee.departments d ON ed.departmentid = d.departmentid
            WHERE {whereClause}
            ORDER BY e.firstname, e.lastname
            LIMIT @limit OFFSET @offset";

        parameters.Add("limit", limit);
        parameters.Add("offset", (page - 1) * limit);

        var results = await QueryAsync<PayrollEntryDto>(sql, parameters);

        var countSql = $@"
            SELECT COUNT(*)
            FROM payroll.payrollentries pe
            INNER JOIN employee.employees e ON pe.employeeid = e.employeeid
            INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
            INNER JOIN employee.departments d ON ed.departmentid = d.departmentid
            WHERE {whereClause}";

        var totalCount = await ExecuteScalarAsync<int>(countSql, parameters.Where(p => p.Key != "limit" && p.Key != "offset").ToDictionary(p => p.Key, p => p.Value));

        return new PagedResult<PayrollEntryDto>
        {
            Data = results.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = limit
        };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesByEmployeeAsync(Guid employeeId, int page, int limit)
    {
        var sql = @"
            SELECT pe.entryid as entry_id, pe.employeeid as employee_id, pe.basicsalary as basic_salary, 
                   pe.overtimeamount as overtime_amount, pe.bonusamount as bonus_amount, 
                   pe.grosssalary as gross_salary, pe.taxamount as tax_amount, pe.netsalary as net_salary,
                   pe.currency, pe.status, pe.calculatedat as calculated_at, pe.paidat as paid_at,
                   CONCAT(e.firstname, ' ', e.lastname) as employee_name, e.employeecode as employee_code,
                   d.departmentname as department_name,
                   pp.periodname as period_name, pp.startdate as period_start, pp.enddate as period_end
            FROM payroll.payrollentries pe
            INNER JOIN employee.employees e ON pe.employeeid = e.employeeid
            INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
            INNER JOIN employee.departments d ON ed.departmentid = d.departmentid
            INNER JOIN payroll.payrollperiods pp ON pe.periodid = pp.periodid
            WHERE pe.employeeid = @employeeId
            ORDER BY pp.startdate DESC
            LIMIT @limit OFFSET @offset";

        var results = await QueryAsync<PayrollEntryDto>(sql, new 
        { 
            employeeId, 
            limit, 
            offset = (page - 1) * limit 
        });

        var countSql = "SELECT COUNT(*) FROM payroll.payrollentries WHERE employeeid = @employeeId";
        var totalCount = await ExecuteScalarAsync<int>(countSql, new { employeeId });

        return new PagedResult<PayrollEntryDto>
        {
            Data = results.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = limit
        };
    }

    public async Task<PayrollEntryDto?> GetPayrollEntryByIdAsync(Guid entryId)
    {
        var sql = @"
            SELECT pe.entryid as entry_id, pe.employeeid as employee_id, pe.basicsalary as basic_salary, 
                   pe.allowances, pe.deductions, pe.overtimeamount as overtime_amount, 
                   pe.bonusamount as bonus_amount, pe.grosssalary as gross_salary, 
                   pe.taxamount as tax_amount, pe.netsalary as net_salary,
                   pe.currency, pe.status, pe.calculatedat as calculated_at, pe.paidat as paid_at,
                   CONCAT(e.firstname, ' ', e.lastname) as employee_name, e.employeecode as employee_code,
                   d.departmentname as department_name
            FROM payroll.payrollentries pe
            INNER JOIN employee.employees e ON pe.employeeid = e.employeeid
            INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
            INNER JOIN employee.departments d ON ed.departmentid = d.departmentid
            WHERE pe.entryid = @entryId";

        return await QuerySingleOrDefaultAsync<PayrollEntryDto>(sql, new { entryId });
    }

    public async Task<PayrollEntry> ApprovePayrollEntryAsync(Guid entryId)
    {
        var sql = @"
            UPDATE payroll.payrollentries 
            SET status = 'approved', updatedat = CURRENT_TIMESTAMP
            WHERE entryid = @entryId
            RETURNING *";

        var result = await QuerySingleAsync<PayrollEntry>(sql, new { entryId });
        return result;
    }

    public async Task<bool> ApproveMultiplePayrollEntriesAsync(List<Guid> entryIds)
    {
        var sql = @"
            UPDATE payroll.payrollentries 
            SET status = 'approved', updatedat = CURRENT_TIMESTAMP
            WHERE entryid = ANY(@entryIds)";

        var affected = await ExecuteAsync(sql, new { entryIds = entryIds.ToArray() });
        return affected > 0;
    }

    public async Task<PayrollEntry> RejectPayrollEntryAsync(Guid entryId, string reason)
    {
        var sql = @"
            UPDATE payroll.payrollentries 
            SET status = 'rejected', updatedat = CURRENT_TIMESTAMP
            WHERE entryid = @entryId
            RETURNING *";

        var result = await QuerySingleAsync<PayrollEntry>(sql, new { entryId, reason });
        return result;
    }

    public async Task<PayrollEntry> MarkPayrollEntryAsPaidAsync(Guid entryId)
    {
        var sql = @"
            UPDATE payroll.payrollentries 
            SET status = 'paid', paidat = CURRENT_TIMESTAMP, updatedat = CURRENT_TIMESTAMP
            WHERE entryid = @entryId
            RETURNING *";

        var result = await QuerySingleAsync<PayrollEntry>(sql, new { entryId });
        return result;
    }

    public async Task<bool> MarkMultiplePayrollEntriesAsPaidAsync(List<Guid> entryIds)
    {
        var sql = @"
            UPDATE payroll.payrollentries 
            SET status = 'paid', paidat = CURRENT_TIMESTAMP, updatedat = CURRENT_TIMESTAMP
            WHERE entryid = ANY(@entryIds)";

        var affected = await ExecuteAsync(sql, new { entryIds = entryIds.ToArray() });
        return affected > 0;
    }

    public async Task<PayrollSummaryDto> GetPayrollDashboardSummaryAsync(Guid organizationId)
    {
        try
        {

        var sql = @"
            SELECT 
                COALESCE(SUM(pe.grosssalary), 0) as total_gross_salary,
                COALESCE(SUM(pe.taxamount), 0) as total_tax,
                COALESCE(SUM(pe.netsalary), 0) as total_net_salary,
                COUNT(DISTINCT pe.employeeid) as total_employees
            FROM payroll.payrollentries pe
            INNER JOIN payroll.payrollperiods pp ON pe.periodid = pp.periodid
            INNER JOIN employee.employees e ON pe.employeeid = e.employeeid
            WHERE e.organizationid = @organizationId
              AND pp.status = 'active'
              AND EXTRACT(MONTH FROM pp.startdate) = EXTRACT(MONTH FROM CURRENT_DATE)
              AND EXTRACT(YEAR FROM pp.startdate) = EXTRACT(YEAR FROM CURRENT_DATE)";

        var result = await QuerySingleAsync<dynamic>(sql, new { organizationId });

        // Get department-wise breakdown
        var deptSql = @"
            SELECT 
                d.departmentname,
                COALESCE(SUM(pe.netsalary), 0) as totalamount
            FROM payroll.payrollentries pe
            INNER JOIN payroll.payrollperiods pp ON pe.periodid = pp.periodid
            INNER JOIN employee.employees e ON pe.employeeid = e.employeeid
            INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
            INNER JOIN employee.departments d ON ed.departmentid = d.departmentid
            WHERE e.organizationid = @organizationId
              AND pp.status = 'active'
              AND EXTRACT(MONTH FROM pp.startdate) = EXTRACT(MONTH FROM CURRENT_DATE)
              AND EXTRACT(YEAR FROM pp.startdate) = EXTRACT(YEAR FROM CURRENT_DATE)
            GROUP BY d.departmentname";

        var deptResults = await QueryAsync<dynamic>(deptSql, new { organizationId });

        return new PayrollSummaryDto
        {
            TotalGrossSalary = result.total_gross_salary,
            TotalTax = result.total_tax,
            TotalNetSalary = result.total_net_salary,
            TotalEmployees = (int)result.total_employees,
            DepartmentWisePayroll = deptResults.ToDictionary(d => (string)d.departmentname, d => (decimal)d.totalamount)
        };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PayrollSummaryDto> GetPayrollReportSummaryAsync(Guid organizationId, PayrollReportFilterRequest filter)
    {
        try
        {
            var whereConditions = new List<string> { "e.organizationid = @organizationId" };
            var parameters = new Dictionary<string, object> { { "organizationId", organizationId } };

            if (filter.PayrollPeriodId.HasValue)
            {
                whereConditions.Add("pe.periodid = @payrollPeriodId");
                parameters.Add("payrollPeriodId", filter.PayrollPeriodId.Value);
            }

            if (!string.IsNullOrEmpty(filter.Department))
            {
                whereConditions.Add("d.departmentname = @department");
                parameters.Add("department", filter.Department);
            }

            if (!string.IsNullOrEmpty(filter.Status))
            {
                whereConditions.Add("pe.status = @status");
                parameters.Add("status", filter.Status);
            }

            if (filter.StartDate.HasValue)
            {
                whereConditions.Add("pp.startdate >= @startDate");
                parameters.Add("startDate", filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                whereConditions.Add("pp.enddate <= @endDate");
                parameters.Add("endDate", filter.EndDate.Value);
            }

            var whereClause = string.Join(" AND ", whereConditions);

            var sql = $@"
                SELECT 
                    COALESCE(SUM(pe.grosssalary), 0) as total_gross_salary,
                    COALESCE(SUM(pe.taxamount), 0) as total_tax,
                    COALESCE(SUM(pe.netsalary), 0) as total_net_salary,
                    COUNT(DISTINCT pe.employeeid) as total_employees
                FROM payroll.payrollentries pe
                INNER JOIN payroll.payrollperiods pp ON pe.periodid = pp.periodid
                INNER JOIN employee.employees e ON pe.employeeid = e.employeeid
                INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
                INNER JOIN employee.departments d ON ed.departmentid = d.departmentid
                WHERE {whereClause}";

            var result = await QuerySingleAsync<dynamic>(sql, parameters);

            // Get department-wise breakdown for the same filter
            var deptSql = $@"
                SELECT 
                    d.departmentname,
                    COALESCE(SUM(pe.netsalary), 0) as total_amount
                FROM payroll.payrollentries pe
                INNER JOIN payroll.payrollperiods pp ON pe.periodid = pp.periodid
                INNER JOIN employee.employees e ON pe.employeeid = e.employeeid
                INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
                INNER JOIN employee.departments d ON ed.departmentid = d.departmentid
                WHERE {whereClause}
                GROUP BY d.departmentname";

            var deptResults = await QueryAsync<dynamic>(deptSql, parameters);

            return new PayrollSummaryDto
            {
                TotalGrossSalary = result.total_gross_salary,
                TotalTax = result.total_tax,
                TotalNetSalary = result.total_net_salary,
                TotalEmployees = (int)result.total_employees,
                DepartmentWisePayroll = deptResults.ToDictionary(d => (string)d.departmentname, d => (decimal)d.total_amount)
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PayrollStatisticsDto> GetPayrollStatisticsAsync(Guid organizationId, DateTime? startDate, DateTime? endDate)
    {
        try
        {

        var dateFilter = "";
        var parameters = new Dictionary<string, object> { { "organizationId", organizationId } };

        if (startDate.HasValue)
        {
            dateFilter += " AND pp.startdate >= @startDate";
            parameters.Add("startDate", startDate.Value);
        }

        if (endDate.HasValue)
        {
            dateFilter += " AND pp.enddate <= @endDate";
            parameters.Add("endDate", endDate.Value);
        }

        var sql = $@"
            SELECT 
                COALESCE(SUM(pe.netsalary), 0) as total_payroll,
                COALESCE(AVG(pe.netsalary), 0) as average_payroll,
                COUNT(DISTINCT pe.employeeid) as total_employees,
                COALESCE(SUM(pe.taxamount + pe.otherdeductions), 0) as total_deductions,
                COALESCE(SUM(pe.overtimeamount + pe.bonusamount), 0) as total_allowances
            FROM payroll.payrollentries pe
            INNER JOIN payroll.payrollperiods pp ON pe.periodid = pp.periodid
            INNER JOIN employee.employees e ON pe.employeeid = e.employeeid
            WHERE e.organizationid = @organizationId{dateFilter}";

        var result = await QuerySingleAsync<dynamic>(sql, parameters);

        // Get monthly trends
        var trendsSql = $@"
            SELECT 
                TO_CHAR(pp.startdate, 'YYYY-MM') as month,
                COALESCE(SUM(pe.netsalary), 0) as amount,
                COUNT(DISTINCT pe.employeeid) as employee_count
            FROM payroll.payrollentries pe
            INNER JOIN payroll.payrollperiods pp ON pe.periodid = pp.periodid
            INNER JOIN employee.employees e ON pe.employeeid = e.employeeid
            WHERE e.organizationid = @organizationId{dateFilter}
            GROUP BY TO_CHAR(pp.startdate, 'YYYY-MM')
            ORDER BY month DESC
            LIMIT 12";

        var trends = await QueryAsync<dynamic>(trendsSql, parameters);

        return new PayrollStatisticsDto
        {
            TotalPayroll = result.total_payroll,
            AveragePayroll = result.average_payroll,
            TotalDeductions = result.total_deductions,
            TotalAllowances = result.total_allowances,
            TotalEmployees = (int)result.total_employees,
            MonthlyTrends = trends.Select(t => new MonthlyPayrollDto
            {
                Month = t.month,
                Amount = (decimal)t.amount,
                EmployeeCount = (int)t.employee_count
            }).ToList()
        };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<DepartmentPayrollBreakdownDto> GetDepartmentPayrollBreakdownAsync(Guid organizationId, Guid? payrollPeriodId)
    {
        try
        {

        var periodFilter = payrollPeriodId.HasValue ? "AND pp.periodid = @payrollPeriodId" : "";
        var parameters = new Dictionary<string, object> { { "organizationId", organizationId } };
        
        if (payrollPeriodId.HasValue)
        {
            parameters.Add("payrollPeriodId", payrollPeriodId.Value);
        }

        var sql = $@"
            SELECT 
                d.departmentname as department_name,
                COALESCE(SUM(pe.netsalary), 0) as total_amount,
                COUNT(DISTINCT pe.employeeid) as employee_count,
                COALESCE(AVG(pe.netsalary), 0) as average_amount
            FROM payroll.payrollentries pe
            INNER JOIN payroll.payrollperiods pp ON pe.periodid = pp.periodid
            INNER JOIN employee.employees e ON pe.employeeid = e.employeeid
            INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
            INNER JOIN employee.departments d ON ed.departmentid = d.departmentid
            WHERE e.organizationid = @organizationId {periodFilter}
            GROUP BY d.departmentname
            ORDER BY total_amount DESC";

        var results = await QueryAsync<dynamic>(sql, parameters);
        var totalPayroll = results.Sum(r => (decimal)r.total_amount);

        return new DepartmentPayrollBreakdownDto
        {
            DepartmentName = "All Departments",
            TotalAmount = totalPayroll,
            EmployeeCount = results.Sum(r => (int)r.employee_count),
            AverageAmount = results.Any() ? results.Average(r => (decimal)r.average_amount) : 0,
            Percentage = 100
        };
        }
        catch(Exception ex)
        {
            throw;
        }
    }

    public async Task<PayrollTaxSummaryDto> CalculateTaxDeductionsAsync(Guid employeeId, decimal grossPay, Guid payrollPeriodId)
    {
        // Simple tax calculation logic - can be enhanced based on business rules
        var incomeTax = grossPay > 5000 ? grossPay * 0.15m : grossPay * 0.10m;
        var socialSecurity = grossPay * 0.062m;
        var medicare = grossPay * 0.0145m;
        var totalDeductions = incomeTax + socialSecurity + medicare;

        return new PayrollTaxSummaryDto
        {
            GrossPay = grossPay,
            IncomeTax = incomeTax,
            SocialSecurity = socialSecurity,
            Medicare = medicare,
            TotalDeductions = totalDeductions,
            NetPay = grossPay - totalDeductions
        };
    }

    public async Task<PayrollDeductionSummaryDto> GetDeductionSummaryAsync(Guid payrollPeriodId)
    {
        var sql = @"
            SELECT 
                COALESCE(SUM(pe.taxamount + pe.otherdeductions), 0) as total_deductions,
                COALESCE(SUM(pe.taxamount), 0) as tax_deductions,
                COALESCE(SUM(pe.otherdeductions), 0) as other_deductions
            FROM payroll.payrollentries pe
            WHERE pe.periodid = @payrollPeriodId";

        var result = await QuerySingleAsync<dynamic>(sql, new { payrollPeriodId });

        return new PayrollDeductionSummaryDto
        {
            TotalDeductions = result.total_deductions,
            TaxDeductions = result.tax_deductions,
            InsuranceDeductions = 0, // Can be enhanced
            LoanDeductions = 0, // Can be enhanced
            OtherDeductions = result.other_deductions,
            DeductionItems = new List<DeductionItemDto>
            {
                new() { Name = "Income Tax", Amount = result.tax_deductions, Type = "Tax" },
                new() { Name = "Other Deductions", Amount = result.other_deductions, Type = "Other" }
            }
        };
    }

    public async Task<List<PayrollProcessingHistoryDto>> GetProcessingHistoryAsync(Guid organizationId, int limit = 10)
    {
        try
        {
            var sql = @"
                SELECT 
                    pp.periodid as PeriodId,
                    pp.periodname as PeriodName,
                    pp.processedat as ProcessedAt,
                    COUNT(pe.entryid) as EntriesCreated,
                    COALESCE(SUM(pe.grosssalary), 0) as TotalGrossAmount,
                    COALESCE(SUM(pe.netsalary), 0) as TotalNetAmount,
                    pp.status as Status,
                    COALESCE(u.firstname || ' ' || u.lastname, 'System') as ProcessedBy
                FROM payroll.payrollperiods pp
                LEFT JOIN payroll.payrollentries pe ON pp.periodid = pe.periodid
                LEFT JOIN auth.users u ON pp.processedby = u.userid
                WHERE pp.organizationid = @organizationId 
                  AND pp.status IN ('calculated', 'completed', 'paid')
                GROUP BY pp.periodid, pp.periodname, pp.processedat, pp.status, u.firstname, u.lastname
                ORDER BY pp.processedat DESC
                LIMIT @limit";

            var results = await QueryAsync<PayrollProcessingHistoryDto>(sql, new { organizationId, limit });
            return results.ToList();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
