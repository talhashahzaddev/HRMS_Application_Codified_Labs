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
            // Determine if this is an attendance-based tax component based on the name
            bool isAbsentTax = IsAbsentTaxComponent(request.Name);
            
            var sql = @"
            INSERT INTO payroll.salarycomponents 
            (organizationid, name, code, type, calculationtype, value, isactive, isabsenttax, createdat)
            VALUES 
            (@OrganizationId, @Name, @Code, @Type, @CalculationType, @Value, @IsActive, @IsAbsentTax, NOW())
            RETURNING componentid, createdat";

            var parameters = new
            {
                OrganizationId = organizationId,
                request.Name,
                request.Code,
                request.Type,
                request.CalculationType,
                request.Value,
                request.IsActive,
                IsAbsentTax = isAbsentTax
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
            // Determine if this is an attendance-based tax component based on the name
            bool isAbsentTax = IsAbsentTaxComponent(request.Name);
            
            var sql = @"
            UPDATE payroll.salarycomponents
            SET 
                name = @Name,
                code = @Code,
                type = @Type,
                calculationtype = @CalculationType,
                value = @Value,
                isactive = @IsActive,
                isabsenttax = @IsAbsentTax,
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
                request.IsActive,
                IsAbsentTax = isAbsentTax
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

    public async Task<int> CalculatePayrollAsync(Guid periodId, Guid organizationId, Guid? ruleId, List<Guid>? departmentIds = null, bool includeTax = false)
    {
        try
        {
            // Get the payroll period to determine dates
            var period = await GetPayrollPeriodByIdAsync(periodId);
            if (period == null)
            {
                throw new KeyNotFoundException($"Payroll period with ID {periodId} not found");
            }
            
            // Check if ruleId is provided and if the component is attendance-based
            bool isAttendanceBasedTax = false;
            Guid? absentTaxComponentId = null;
            Guid? absentTaxPositionId = null;
            decimal? absentTaxValue = null;
            string? absentTaxCalculationType = null;
            
            if (ruleId.HasValue)
            {
                // Check if the rule's component is attendance-based
                var ruleCheckSql = @"
                    SELECT 
                        sc.componentid, 
                        sc.isabsenttax,
                        sc.calculationtype,
                        sc.value,
                        sr.positionid,
                        sr.value_override
                    FROM payroll.salaryrules sr
                    INNER JOIN payroll.salarycomponents sc ON sr.componentid = sc.componentid
                    WHERE sr.ruleid = @RuleId
                    AND sc.organizationid = @OrganizationId";
                
                var ruleCheck = await QuerySingleOrDefaultAsync<dynamic>(ruleCheckSql, new 
                { 
                    RuleId = ruleId.Value,
                    OrganizationId = organizationId
                });
                
                if (ruleCheck != null && ruleCheck.isabsenttax != null && Convert.ToBoolean(ruleCheck.isabsenttax))
                {
                    isAttendanceBasedTax = true;
                    absentTaxComponentId = ruleCheck.componentid;
                    absentTaxPositionId = ruleCheck.positionid;
                    absentTaxCalculationType = ruleCheck.calculationtype?.ToString()?.ToLower();
                    absentTaxValue = ruleCheck.value_override != null ? (decimal?)ruleCheck.value_override : (decimal?)ruleCheck.value;
                }
            }
            
            // Build employee query with optional department filter
            var employeesSql = @"
                SELECT e.employeeid, e.organizationid, ed.departmentid, ed.positionid, ed.basicsalary, ed.shiftid
                FROM employee.employees e
                JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid
                WHERE e.organizationid = @OrganizationId
                AND e.status = 'active'
                AND ed.iscurrent = true";
            
            object employeeParams;
            
            // Add department filter if provided
            if (departmentIds != null && departmentIds.Any())
            {
                employeesSql += " AND ed.departmentid = ANY(@DepartmentIds)";
                employeeParams = new { OrganizationId = organizationId, DepartmentIds = departmentIds.ToArray() };
            }
            else
            {
                employeeParams = new { OrganizationId = organizationId };
            }
            
            var employees = await QueryAsync<dynamic>(employeesSql, employeeParams);
            
            int entriesCreated = 0;
            
            foreach (var employee in employees)
            {
                decimal basicSalary = employee.basicsalary;
                decimal totalEarnings = basicSalary;
                decimal totalDeductions = 0;
                
                // Get applicable salary components/rules for this employee
                string componentsSql;
                object componentParams;
                
                // Filter by specific rule if provided
                if (ruleId.HasValue)
                {
                    // When a specific rule is selected, use INNER JOIN to ensure we get the rule's component
                    componentsSql = @"
                        SELECT 
                            sc.componentid, sc.name, sc.type, sc.calculationtype, sc.value,
                            sc.isabsenttax, sr.value_override, sr.ruleid
                        FROM payroll.salaryrules sr
                        INNER JOIN payroll.salarycomponents sc ON sr.componentid = sc.componentid
                        WHERE sr.ruleid = @RuleId
                        AND sc.organizationid = @OrganizationId
                        AND sc.isactive = true
                        AND (
                            (sr.departmentid = @DepartmentId OR sr.departmentid IS NULL)
                            AND (sr.positionid = @PositionId OR sr.positionid IS NULL)
                        )";
                    
                    componentParams = new 
                    { 
                        OrganizationId = organizationId,
                        DepartmentId = employee.departmentid,
                        PositionId = employee.positionid,
                        RuleId = ruleId.Value
                    };
                }
                else
                {
                    // When no specific rule, get all applicable components for the employee
                    componentsSql = @"
                        SELECT 
                            sc.componentid, sc.name, sc.type, sc.calculationtype, sc.value,
                            sc.isabsenttax, sr.value_override, sr.ruleid
                        FROM payroll.salarycomponents sc
                        LEFT JOIN payroll.salaryrules sr ON sc.componentid = sr.componentid
                        WHERE sc.organizationid = @OrganizationId
                        AND sc.isactive = true
                        AND (
                            (sr.departmentid = @DepartmentId OR sr.departmentid IS NULL)
                            AND (sr.positionid = @PositionId OR sr.positionid IS NULL)
                        )";
                    
                    componentParams = new 
                    { 
                        OrganizationId = organizationId,
                        DepartmentId = employee.departmentid,
                        PositionId = employee.positionid
                    };
                }
                
                var components = await QueryAsync<dynamic>(componentsSql, componentParams);
                
                // Track individual allowances and deductions for proper storage
                var allowancesDict = new Dictionary<string, object>();
                var deductionsDict = new Dictionary<string, object>();
                
                // Calculate each component (skip attendance-based tax as it's calculated separately)
                foreach (var component in components)
                {
                    // Skip attendance-based tax component as it's calculated separately
                    bool isAbsentTax = component.isabsenttax != null && Convert.ToBoolean(component.isabsenttax);
                    if (isAbsentTax && isAttendanceBasedTax)
                    {
                        continue; // Skip this component, it will be calculated based on attendance
                    }
                    
                    decimal componentValue = 0;
                    
                    // Use value_override if available, otherwise use the component's default value
                    decimal valueToUse = component.value_override != null ? (decimal)component.value_override : (decimal)component.value;
                    
                    // Calculate based on type
                    if (component.calculationtype.ToString().ToLower() == "percentage")
                    {
                        componentValue = (basicSalary * valueToUse) / 100;
                    }
                    else // fixed
                    {
                        componentValue = valueToUse;
                    }
                    
                    string componentName = component.name?.ToString() ?? "Unknown";
                    string componentType = component.type?.ToString()?.ToLower() ?? "";
                    
                    // Add to earnings or deductions based on component type
                    // Check for both "allowance" (from frontend) and "earning" (legacy/alternative naming)
                    if (componentType == "earning" || componentType == "allowance")
                    {
                        totalEarnings += componentValue;
                        allowancesDict[componentName] = componentValue;
                    }
                    else // deduction
                    {
                        totalDeductions += componentValue;
                        deductionsDict[componentName] = componentValue;
                    }
                }
                
                // Calculate attendance-based tax if applicable
                decimal attendanceTaxAmount = 0;
                if (isAttendanceBasedTax && absentTaxComponentId.HasValue && absentTaxValue.HasValue)
                {
                    // Check if this employee's position matches the rule's position (if specified)
                    bool applyToEmployee = true;
                    if (absentTaxPositionId.HasValue && employee.positionid != absentTaxPositionId.Value)
                    {
                        applyToEmployee = false;
                    }
                    
                    if (applyToEmployee)
                    {
                        attendanceTaxAmount = await CalculateAttendanceBasedTaxAsync(
                            (Guid)employee.employeeid,
                            organizationId,
                            period.StartDate,
                            period.EndDate,
                            absentTaxValue.Value,
                            absentTaxCalculationType ?? "percentage",
                            basicSalary
                        );
                        
                        if (attendanceTaxAmount > 0)
                        {
                            totalDeductions += attendanceTaxAmount;
                            string absentTaxComponentName = await GetComponentNameAsync(absentTaxComponentId.Value);
                            deductionsDict[absentTaxComponentName] = attendanceTaxAmount;
                        }
                    }
                }
                
                // Calculate tax if includeTax is true
                // IMPORTANT: Tax is calculated on totalEarnings (basic salary + allowances)
                decimal taxAmount = 0;
                if (includeTax)
                {
                    var taxSummary = await CalculateTaxDeductionsAsync((Guid)employee.employeeid, totalEarnings, periodId);
                    taxAmount = taxSummary.IncomeTax; // Use income tax from calculation
                    totalDeductions += taxAmount;
                }
                
                decimal grossSalary = totalEarnings;
                decimal netSalary = grossSalary - totalDeductions;
                
                // Insert payroll entry
                var insertSql = @"
                    INSERT INTO payroll.payrollentries (
                        entryid, periodid, employeeid, basicsalary, 
                        allowances, deductions, grosssalary, taxamount, 
                        netsalary, status, calculatedat, createdat, updatedat
                    )
                    VALUES (
                        uuid_generate_v4(), @PeriodId, @EmployeeId, @BasicSalary,
                        @Allowances::jsonb, @Deductions::jsonb, @GrossSalary, @TaxAmount,
                        @NetSalary, 'calculated', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                    )";
                if (deductionsDict == null || deductionsDict.Count == 0)
                {
                    if (taxAmount != 0)
                    {
                        deductionsDict["Tax"] = taxAmount;
                    }
                }
                await ExecuteAsync(insertSql, new {
                    PeriodId = periodId,
                    EmployeeId = employee.employeeid,
                    BasicSalary = basicSalary,
                    Allowances = JsonConvert.SerializeObject(allowancesDict), // Dictionary with component names and values
                    Deductions = JsonConvert.SerializeObject(deductionsDict), // Dictionary with deduction names and values (excluding tax)
                    GrossSalary = grossSalary,
                    TaxAmount = taxAmount,
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
            SELECT pe.entryid as entry_id, pe.periodid as period_id, pe.employeeid as employee_id, pe.basicsalary as basic_salary, 
                   pe.allowances::text as allowances, pe.deductions::text as deductions, 
                   pe.overtimeamount as overtime_amount, pe.bonusamount as bonus_amount, 
                   pe.grosssalary as gross_salary, pe.taxamount as tax_amount, pe.netsalary as net_salary,
                   pe.currency, pe.status, pe.calculatedat as calculated_at, pe.paidat as paid_at,
                   CONCAT(e.firstname, ' ', e.lastname) as employee_name, e.employeecode as employee_code,
                   d.departmentname as department_name, pp.periodname as period_name
            FROM payroll.payrollentries pe
            INNER JOIN employee.employees e ON pe.employeeid = e.employeeid
            INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
            INNER JOIN employee.departments d ON ed.departmentid = d.departmentid
            LEFT JOIN payroll.payrollperiods pp ON pe.periodid = pp.periodid
            WHERE {whereClause}
            ORDER BY pp.periodname, e.firstname, e.lastname
            LIMIT @limit OFFSET @offset";

        parameters.Add("limit", limit);
        parameters.Add("offset", (page - 1) * limit);

        var results = await QueryAsync<dynamic>(sql, parameters);
        
        // Manually map to ensure JSONB columns are properly deserialized
        var mappedResults = new List<PayrollEntryDto>();
        foreach (var r in results)
        {
            mappedResults.Add(MapPayrollEntryDto(r));
        }

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
            Data = mappedResults,
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
            SELECT pe.entryid as entry_id, pe.periodid as period_id, pe.employeeid as employee_id, pe.basicsalary as basic_salary, 
                   pe.allowances::text as allowances, pe.deductions::text as deductions, 
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

        var results = await QueryAsync<dynamic>(sql, new 
        { 
            employeeId, 
            limit, 
            offset = (page - 1) * limit 
        });
        
        // Manually map to ensure JSONB columns are properly deserialized
        var mappedResults = new List<PayrollEntryDto>();
        foreach (var r in results)
        {
            mappedResults.Add(MapPayrollEntryDto(r));
        }

        var countSql = "SELECT COUNT(*) FROM payroll.payrollentries WHERE employeeid = @employeeId";
        var totalCount = await ExecuteScalarAsync<int>(countSql, new { employeeId });

        return new PagedResult<PayrollEntryDto>
        {
            Data = mappedResults,
            TotalCount = totalCount,
            Page = page,
            PageSize = limit
        };
    }

    public async Task<PayrollEntryDto?> GetPayrollEntryByIdAsync(Guid entryId)
    {
        var sql = @"
            SELECT pe.entryid as entry_id, pe.periodid as period_id, pe.employeeid as employee_id, pe.basicsalary as basic_salary, 
                   pe.allowances::text as allowances, pe.deductions::text as deductions, 
                   pe.overtimeamount as overtime_amount, pe.bonusamount as bonus_amount, 
                   pe.grosssalary as gross_salary, pe.taxamount as tax_amount, pe.netsalary as net_salary,
                   pe.currency, pe.status, pe.calculatedat as calculated_at, pe.paidat as paid_at,
                   CONCAT(e.firstname, ' ', e.lastname) as employee_name, e.employeecode as employee_code,
                   d.departmentname as department_name, pp.periodname as period_name
            FROM payroll.payrollentries pe
            INNER JOIN employee.employees e ON pe.employeeid = e.employeeid
            INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
            INNER JOIN employee.departments d ON ed.departmentid = d.departmentid
            LEFT JOIN payroll.payrollperiods pp ON pe.periodid = pp.periodid
            WHERE pe.entryid = @entryId";

        var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new { entryId });
        if (result == null) return null;
        return MapPayrollEntryDto(result);
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
    COALESCE(SUM(pe.netsalary), 0) AS total_payroll,
    COALESCE(AVG(pe.netsalary), 0) AS average_payroll,
    COUNT(DISTINCT pe.employeeid) AS total_employees,
    COALESCE(SUM(pe.taxamount + pe.otherdeductions), 0) AS total_deductions,
    COALESCE(
        SUM(
            pe.overtimeamount + pe.bonusamount +
            COALESCE((pe.allowances ->> 'Allowance')::numeric, 0)
        ), 0
    ) AS total_allowances
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

    private async Task<decimal> CalculateAttendanceBasedTaxAsync(
        Guid employeeId,
        Guid organizationId,
        DateTime startDate,
        DateTime endDate,
        decimal taxValue,
        string calculationType,
        decimal basicSalary)
    {
        try
        {
            // Get all paid leave types for the organization
            var paidLeaveTypesSql = @"
                SELECT leavetypeid
                FROM attendance.leavetypes
                WHERE organizationid = @OrganizationId
                AND ispaid = true
                AND isactive = true";
            
            var paidLeaveTypeIds = (await QueryAsync<Guid>(paidLeaveTypesSql, new { OrganizationId = organizationId })).ToList();
            
            if (!paidLeaveTypeIds.Any())
            {
                // No paid leave types, so all absences are taxable
                return await CalculateAbsenceTax(employeeId, startDate, endDate, taxValue, calculationType, basicSalary, 0);
            }
            
            // Get leave entitlements for the employee for the current year
            int currentYear = startDate.Year;
            var leaveEntitlementsSql = @"
                SELECT 
                    leavetypeid,
                    totaldays,
                    useddays,
                    remainingdays
                FROM attendance.leaveentitlements
                WHERE employeeid = @EmployeeId
                AND year = @Year
                AND leavetypeid = ANY(@LeaveTypeIds)";
            
            var leaveEntitlements = await QueryAsync<dynamic>(leaveEntitlementsSql, new
            {
                EmployeeId = employeeId,
                Year = currentYear,
                LeaveTypeIds = paidLeaveTypeIds.ToArray()
            });
            
            // Calculate total remaining paid leaves
            decimal totalRemainingLeaves = 0;
            foreach (var entitlement in leaveEntitlements)
            {
                totalRemainingLeaves += (decimal)(entitlement.remainingdays ?? 0);
            }
            
            // Get employee's shift information
            var shiftSql = @"
                SELECT 
                    s.shiftid,
                    s.daysofweek,
                    s.starttime,
                    s.endtime
                FROM employee.employmentdetails ed
                LEFT JOIN attendance.shifts s ON ed.shiftid = s.shiftid
                WHERE ed.employeeid = @EmployeeId
                AND ed.iscurrent = true";
            
            var shiftInfo = await QuerySingleOrDefaultAsync<dynamic>(shiftSql, new { EmployeeId = employeeId });
            
            if (shiftInfo == null || shiftInfo.shiftid == null || Convert.IsDBNull(shiftInfo.shiftid))
            {
                return 0; // No shift information, cannot calculate
            }
            
            // Parse days of week (format: {1,2,3,4,5} where 1=Monday, 5=Friday)
            var daysOfWeekStr = shiftInfo.daysofweek?.ToString() ?? "{}";
            var daysOfWeek = ParseDaysOfWeek(daysOfWeekStr);
            
            // Get attendance records for the period
            var attendanceSql = @"
                SELECT 
                    workdate,
                    checkintime,
                    status
                FROM attendance.attendance
                WHERE employeeid = @EmployeeId
                AND workdate >= @StartDate
                AND workdate <= @EndDate
                ORDER BY workdate";
            
            var attendanceRecords = await QueryAsync<dynamic>(attendanceSql, new
            {
                EmployeeId = employeeId,
                StartDate = startDate.Date,
                EndDate = endDate.Date
            });
            
            // Calculate absences
            int absences = 0;
            var attendanceDict = attendanceRecords.ToDictionary(
                r => ((DateTime)r.workdate).Date,
                r => new { HasCheckIn = r.checkintime != null && !Convert.IsDBNull(r.checkintime), Status = r.status?.ToString() }
            );
            
            // Iterate through each day in the period
            for (DateTime date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                // Check if this day is a working day according to shift
                // PostgreSQL array uses 0-based indexing where 0=Monday, 6=Sunday
                // But the array format {1,2,3,4,5} means Monday=1, Tuesday=2, etc.
                DayOfWeek dayOfWeek = date.DayOfWeek;
                // Convert C# DayOfWeek (Sunday=0, Monday=1, ..., Saturday=6) to PostgreSQL format (Monday=1, ..., Sunday=7)
                int dayNumber = dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek;
                
                if (!daysOfWeek.Contains(dayNumber))
                {
                    continue; // Not a working day, skip
                }
                
                // Check if employee was present
                bool isPresent = false;
                if (attendanceDict.ContainsKey(date))
                {
                    var attendance = attendanceDict[date];
                    // Check if employee checked in or if status is present/approved
                    isPresent = attendance.HasCheckIn || 
                               (attendance.Status != null && 
                                (attendance.Status.ToLower() == "present" || 
                                 attendance.Status.ToLower() == "approved"));
                }
                
                if (!isPresent)
                {
                    // Employee was absent on this working day
                    absences++;
                }
            }
            
            // Calculate taxable absences (absences that exceed remaining leaves)
            int taxableAbsences = Math.Max(0, absences - (int)Math.Floor(totalRemainingLeaves));
            
            if (taxableAbsences == 0)
            {
                return 0; // No taxable absences
            }
            
            // Calculate tax based on taxable absences
            return await CalculateAbsenceTax(employeeId, startDate, endDate, taxValue, calculationType, basicSalary, taxableAbsences);
        }
        catch (Exception ex)
        {
            // Log error and return 0 to avoid breaking payroll calculation
            return 0;
        }
    }
    
    private async Task<decimal> CalculateAbsenceTax(
        Guid employeeId,
        DateTime startDate,
        DateTime endDate,
        decimal taxValue,
        string calculationType,
        decimal basicSalary,
        int taxableAbsences)
    {
        decimal taxAmount = 0;
        
        if (calculationType == "percentage")
        {
            // For percentage: tax per absence = (basicSalary * taxValue / 100)
            // Total tax = tax per absence * number of taxable absences
            decimal taxPerAbsence = (basicSalary * taxValue) / 100;
            taxAmount = taxPerAbsence * taxableAbsences;
        }
        else // fixed
        {
            // For fixed: tax per absence = taxValue
            // Total tax = taxValue * number of taxable absences
            taxAmount = taxValue * taxableAbsences;
        }
        
        return taxAmount;
    }
    
    private List<int> ParseDaysOfWeek(string daysOfWeekStr)
    {
        var days = new List<int>();
        
        // Remove braces and split by comma
        daysOfWeekStr = daysOfWeekStr.Trim('{', '}');
        if (string.IsNullOrWhiteSpace(daysOfWeekStr))
        {
            return days;
        }
        
        var parts = daysOfWeekStr.Split(',');
        foreach (var part in parts)
        {
            if (int.TryParse(part.Trim(), out int day))
            {
                days.Add(day);
            }
        }
        
        return days;
    }
    
    private async Task<string> GetComponentNameAsync(Guid componentId)
    {
        try
        {
            var sql = "SELECT name FROM payroll.salarycomponents WHERE componentid = @ComponentId";
            var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new { ComponentId = componentId });
            return result?.name?.ToString() ?? "Absent Tax";
        }
        catch
        {
            return "Absent Tax";
        }
    }

    /// <summary>
    /// Determines if a salary component name indicates it's an attendance-based tax component
    /// </summary>
    private bool IsAbsentTaxComponent(string componentName)
    {
        if (string.IsNullOrWhiteSpace(componentName))
        {
            return false;
        }
        
        // Convert to lowercase for case-insensitive comparison
        string nameLower = componentName.ToLower();
        
        // Check for keywords related to absent/attendance
        string[] absentKeywords = { "absent", "absents", "absence", "absences" };
        string[] attendanceKeywords = { "attendance", "attendence", "attend" };
        
        // Check if name contains any absent-related keywords
        foreach (var keyword in absentKeywords)
        {
            if (nameLower.Contains(keyword))
            {
                return true;
            }
        }
        
        // Check if name contains any attendance-related keywords
        foreach (var keyword in attendanceKeywords)
        {
            if (nameLower.Contains(keyword))
            {
                return true;
            }
        }
        
        return false;
    }

    private PayrollEntryDto MapPayrollEntryDto(dynamic r)
    {
        // Deserialize JSONB columns (allowances and deductions)
        Dictionary<string, object> allowances = new();
        Dictionary<string, object> deductions = new();

        try
        {
            if (r.allowances != null && !Convert.IsDBNull(r.allowances))
            {
                string raw = r.allowances.ToString();

                if (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{"))
                {
                    allowances = JsonConvert.DeserializeObject<Dictionary<string, object>>(raw) ?? new();
                }
                else
                {
                    // fallback: wrap primitive value into a dictionary
                    allowances = new Dictionary<string, object> { { "value", raw } };
                }
            }
        }
        catch
        {
            allowances = new();
        }

        try
        {
            if (r.deductions != null && !Convert.IsDBNull(r.deductions))
            {
                string raw = r.deductions.ToString();

                if (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{"))
                {
                    deductions = JsonConvert.DeserializeObject<Dictionary<string, object>>(raw) ?? new();
                }
                else
                {
                    deductions = new Dictionary<string, object> { { "value", raw } };
                }
            }
        }
        catch
        {
            deductions = new();
        }

        return new PayrollEntryDto
        {
            EntryId = r.entry_id ?? r.EntryId,
            PeriodId = r.period_id ?? r.PeriodId,
            PeriodName = r.period_name?.ToString() ?? r.PeriodName?.ToString() ?? string.Empty,
            EmployeeId = r.employee_id ?? r.EmployeeId,
            EmployeeName = r.employee_name?.ToString() ?? r.EmployeeName?.ToString() ?? string.Empty,
            EmployeeCode = r.employee_code?.ToString() ?? r.EmployeeCode?.ToString() ?? string.Empty,
            DepartmentName = r.department_name?.ToString() ?? r.DepartmentName?.ToString() ?? string.Empty,
            PositionTitle = r.position_title?.ToString() ?? r.PositionTitle?.ToString(),
            BasicSalary = r.basic_salary ?? r.BasicSalary ?? 0,
            Allowances = allowances,
            Deductions = deductions,
            OvertimeAmount = r.overtime_amount ?? r.OvertimeAmount ?? 0,
            BonusAmount = r.bonus_amount ?? r.BonusAmount ?? 0,
            GrossSalary = r.gross_salary ?? r.GrossSalary ?? 0,
            TaxAmount = r.tax_amount ?? r.TaxAmount ?? 0,
            NetSalary = r.net_salary ?? r.NetSalary ?? 0,
            Currency = r.currency?.ToString() ?? r.Currency?.ToString() ?? "USD",
            Status = r.status?.ToString() ?? r.Status?.ToString() ?? string.Empty,
            CalculatedAt = r.calculated_at ?? r.CalculatedAt,
            PaidAt = r.paid_at ?? r.PaidAt
        };
    }
}
