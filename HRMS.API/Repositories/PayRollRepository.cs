using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using Dapper;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace HRMS.API.Repositories;

public class PayRollRepository : BaseRepository, IPayRollRepository
{
    public PayRollRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    private sealed class WithdrawalConfigRoot
    {
        public List<WithdrawalConfigEntry>? temporary { get; set; }
        public List<WithdrawalConfigEntry>? permanent { get; set; }
    }

    private sealed class WithdrawalConfigEntry
    {
        public string? reason { get; set; }
        public decimal? min_pct { get; set; }
        public decimal? max_pct { get; set; }
    }

    public async Task<PayRollRulesGroupedDto> GetAllRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                x.id AS Id,
                x.organizationid AS OrganizationId,
                x.rulename AS RuleName,
                x.description AS Description,
                x.isactive AS IsActive,
                x.isdeleted AS IsDeleted,
                x.createdat AS CreatedAt,
                x.policyname AS PolicyName
            FROM (
                SELECT
                    o.ruleid AS id,
                    o.organizationid,
                    o.rulename,
                    o.description,
                    o.isactive,
                    o.isdeleted,
                    o.createdat,
                    'OvertimePolicy' AS policyname
                FROM payroll.overtimerules o
                WHERE o.organizationid = @organizationId

                UNION ALL

                SELECT
                    a.ruleid AS id,
                    a.organizationid,
                    a.rulename,
                    a.description,
                    a.isactive,
                    a.isdeleted,
                    a.createdat,
                    'AttendanceDeductionPolicy' AS policyname
                FROM payroll.attendancedeductionrules a
                WHERE a.organizationid = @organizationId

                UNION ALL

                SELECT
                    l.ruleid AS id,
                    l.organizationid,
                    l.rulename,
                    l.description,
                    l.isactive,
                    l.isdeleted,
                    l.createdat,
                    'LateArrivalPolicy' AS policyname
                FROM payroll.lateattendancerules l
                WHERE l.organizationid = @organizationId

                UNION ALL

                SELECT
                    lr.ruleid AS id,
                    lr.organizationid,
                    lr.rulename,
                    lr.description,
                    lr.isactive,
                    lr.isdeleted,
                    lr.createdat,
                    'LeaveDeductionPolicy' AS policyname
                FROM payroll.leaverules lr
                WHERE lr.organizationid = @organizationId

                UNION ALL

                SELECT
                    p.ruleid AS id,
                    p.organizationid,
                    p.rulename,
                    p.description,
                    p.isactive,
                    p.isdeleted,
                    p.createdat,
                    'PerformanceBonusPolicy' AS policyname
                FROM payroll.performancerules p
                WHERE p.organizationid = @organizationId

                UNION ALL

                SELECT
                    lo.ruleid AS id,
                    lo.organizationid,
                    lo.rulename,
                    lo.description,
                    lo.isactive,
                    lo.isdeleted,
                    lo.createdat,
                    'EmployeeLoanPolicy' AS policyname
                FROM payroll.loanrules lo
                WHERE lo.organizationid = @organizationId

                UNION ALL

                SELECT
                    s.ruleid AS id,
                    s.organizationid,
                    s.rulename,
                    s.description,
                    s.isactive,
                    s.isdeleted,
                    s.createdat,
                    'SalaryAdvancePolicy' AS policyname
                FROM payroll.salaryadvancerules s
                WHERE s.organizationid = @organizationId

                UNION ALL

                SELECT
                    pf.ruleid AS id,
                    pf.organizationid,
                    pf.rulename,
                    pf.description,
                    pf.isactive,
                    pf.isdeleted,
                    pf.createdat,
                    'ProvidentFundPolicy' AS policyname
                FROM payroll.pfrules pf
                WHERE pf.organizationid = @organizationId

                UNION ALL

                SELECT
                    t.regimeid AS id,
                    t.organizationid,
                    t.regimename AS rulename,
                    t.country AS description,
                    t.isactive,
                    t.isdeleted,
                    t.createdat,
                    'IncomeTaxPolicy' AS policyname
                FROM payroll.taxregimes t
                WHERE t.organizationid = @organizationId

                UNION ALL

                SELECT
                    ss.ruleid AS id,
                    ss.organizationid,
                    ss.rulename,
                    ss.description,
                    ss.isactive,
                    ss.isdeleted,
                    ss.createdat,
                    'SocialSecurityPolicy' AS policyname
                FROM payroll.socialsecurityrules ss
                WHERE ss.organizationid = @organizationId

                UNION ALL

                SELECT
                    g.id AS id,
                    g.organizationid,
                    g.configrulename AS rulename,
                    g.description,
                    g.isactive,
                    g.isdeleted,
                    g.createdat,
                    'GratuityPolicy' AS policyname
                FROM payroll.gratuityconfig g
                WHERE g.organizationid = @organizationId

                UNION ALL

                SELECT
                    b.ruleid AS id,
                    b.organizationid,
                    b.rulename,
                    b.description,
                    b.isactive,
                    b.isdeleted,
                    b.createdat,
                    'BonusPolicy' AS policyname
                FROM payroll.bonusrules b
                WHERE b.organizationid = @organizationId
            ) x
            ORDER BY x.createdat DESC;";

        var rows = await QueryAsync<PayRollRuleQueryDto>(sql, new { organizationId });

        var grouped = new PayRollRulesGroupedDto
        {
            OvertimePolicy = BuildPolicySection(rows, "OvertimePolicy"),
            AttendanceDeductionPolicy = BuildPolicySection(rows, "AttendanceDeductionPolicy"),
            LateArrivalPolicy = BuildPolicySection(rows, "LateArrivalPolicy"),
            LeaveDeductionPolicy = BuildPolicySection(rows, "LeaveDeductionPolicy"),
            PerformanceBonusPolicy = BuildPolicySection(rows, "PerformanceBonusPolicy"),
            EmployeeLoanPolicy = BuildPolicySection(rows, "EmployeeLoanPolicy"),
            SalaryAdvancePolicy = BuildPolicySection(rows, "SalaryAdvancePolicy"),
            ProvidentFundPolicy = BuildPolicySection(rows, "ProvidentFundPolicy"),
            IncomeTaxPolicy = BuildPolicySection(rows, "IncomeTaxPolicy"),
            SocialSecurityPolicy = BuildPolicySection(rows, "SocialSecurityPolicy"),
            GratuityPolicy = BuildPolicySection(rows, "GratuityPolicy"),
            BonusPolicy = BuildPolicySection(rows, "BonusPolicy")
        };

        return grouped;
    }

    private static PayRollRuleDto MapRule(PayRollRuleQueryDto row)
    {
        return new PayRollRuleDto
        {
            Id = row.Id,
            OrganizationId = row.OrganizationId,
            RuleName = row.RuleName,
            Description = row.Description,
            IsActive = row.IsActive,
            IsDeleted = row.IsDeleted,
            CreatedAt = row.CreatedAt
        };
    }

    private static PayRollPolicySectionDto BuildPolicySection(IEnumerable<PayRollRuleQueryDto> rows, string policyName)
    {
        var policyRules = rows
            .Where(x => x.PolicyName == policyName)
            .Select(MapRule)
            .ToList();

        return new PayRollPolicySectionDto
        {
            RulesCount = policyRules.Count,
            Rules = policyRules
        };
    }

    private class PayRollRuleQueryDto : PayRollRuleDto
    {
        public string PolicyName { get; set; } = string.Empty;
    }

    private class ExistingGratuityRecordDto
    {
        public Guid Id { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }

    private static readonly HashSet<string> AllowedGratuityPaymentMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "payroll_credit",
        "bank_transfer",
        "cash"
    };

    private static string NormalizeGratuityPaymentMethod(string? paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            return "payroll_credit";
        }

        var normalized = paymentMethod.Trim().ToLowerInvariant();
        if (!AllowedGratuityPaymentMethods.Contains(normalized))
        {
            throw new InvalidOperationException("Invalid payment method. Allowed values: payroll_credit, bank_transfer, cash.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalGratuityPaymentMethod(string? paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            return null;
        }

        return NormalizeGratuityPaymentMethod(paymentMethod);
    }

    public async Task<IEnumerable<OvertimeRuleDto>> GetAllOvertimeRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                overtimetype AS OvertimeType,
                fixedamount AS FixedAmount,
                percentage AS Percentage,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.overtimerules
            WHERE organizationid = @organizationId AND isdeleted=false
            ORDER BY createdat DESC;";

        return await QueryAsync<OvertimeRuleDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<OvertimeRuleDto>> GetActiveOvertimeRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                overtimetype AS OvertimeType,
                fixedamount AS FixedAmount,
                percentage AS Percentage,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.overtimerules
            WHERE organizationid = @organizationId AND isdeleted = false AND isactive = true
            ORDER BY createdat DESC;";

        return await QueryAsync<OvertimeRuleDto>(sql, new { organizationId });
    }

    public async Task<OvertimeRuleDto?> GetOvertimeRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                overtimetype AS OvertimeType,
                fixedamount AS FixedAmount,
                percentage AS Percentage,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.overtimerules
            WHERE ruleid = @ruleId AND organizationid = @organizationId AND isdeleted=false;";

        return await QuerySingleOrDefaultAsync<OvertimeRuleDto>(sql, new { ruleId, organizationId });
    }

    public async Task<OvertimeRuleDto> CreateOvertimeRuleAsync(Guid organizationId, CreateOvertimeRuleDto dto)
    {
        var sql = @"
            INSERT INTO payroll.overtimerules (
                organizationid, rulename, description, overtimetype, 
                fixedamount, percentage, isactive, isdeleted
            )
            VALUES (
                @organizationId, @RuleName, @Description, @OvertimeType, 
                @FixedAmount, @Percentage, true, false
            )
            RETURNING 
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                overtimetype AS OvertimeType,
                fixedamount AS FixedAmount,
                percentage AS Percentage,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt;";

        var parameters = new
        {
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.OvertimeType,
            dto.FixedAmount,
            dto.Percentage
        };

        return await QuerySingleAsync<OvertimeRuleDto>(sql, parameters);
    }

    public async Task<OvertimeRuleDto?> UpdateOvertimeRuleAsync(Guid ruleId, Guid organizationId, UpdateOvertimeRuleDto dto)
    {
        var sql = @"
            UPDATE payroll.overtimerules
            SET 
                rulename = @RuleName,
                description = @Description,
                overtimetype = @OvertimeType,
                fixedamount = @FixedAmount,
                percentage = @Percentage,
                updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING 
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                overtimetype AS OvertimeType,
                fixedamount AS FixedAmount,
                percentage AS Percentage,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt;";

        var parameters = new
        {
            ruleId,
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.OvertimeType,
            dto.FixedAmount,
            dto.Percentage
        };

        return await QuerySingleOrDefaultAsync<OvertimeRuleDto>(sql, parameters);
    }

    public async Task<bool> ToggleOvertimeRuleStatusAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.overtimerules
            SET isactive = NOT isactive, updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING isactive;";

        var result = await QuerySingleOrDefaultAsync<bool?>(sql, new { ruleId, organizationId });
        return result.HasValue;
    }

    public async Task<bool> ToggleOvertimeRuleDeletionAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.overtimerules
            SET isdeleted = NOT isdeleted, updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING isdeleted;";
        var result = await QuerySingleOrDefaultAsync<bool?>(sql, new { ruleId, organizationId });
        return result.HasValue;
    }

    // Overtime Entries
    public async Task<PagedResult<OvertimeEntryDto>> GetAllOvertimeEntriesAsync(Guid organizationId, OvertimeEntryFilterDto filter)
    {
        var conditions = new List<string> { "e.organizationid = @organizationId" };
        if (filter.PeriodId.HasValue) conditions.Add("oe.periodid = @PeriodId");
        if (filter.RuleId.HasValue) conditions.Add("oe.ruleid = @RuleId");
        if (!string.IsNullOrEmpty(filter.OvertimeType)) conditions.Add("r.overtimetype = @OvertimeType");
        if (!string.IsNullOrEmpty(filter.EmployeeName)) conditions.Add("(e.firstname || ' ' || e.lastname) ILIKE '%' || @EmployeeName || '%'");

        var whereClause = string.Join(" AND ", conditions);

        var countSql = $@"
            SELECT COUNT(1)
            FROM payroll.overtimeentries oe
            JOIN employee.Employees e ON oe.employeeid = e.employeeid
            LEFT JOIN payroll.overtimerules r ON oe.ruleid = r.ruleid
            WHERE {whereClause};";

        var sql = $@"
            SELECT
                oe.overtimeid AS OvertimeId,
                oe.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                oe.periodid AS PeriodId,
                oe.ruleid AS RuleId,
                oe.hoursworked AS HoursWorked,
                oe.finalamount AS FinalAmount,
                oe.entrystatus AS EntryStatus,
                r.overtimetype AS OvertimeType,
                oe.approvedby AS ApprovedBy,
                oe.overtimedate AS OvertimeDate,
                oe.createdat AS CreatedAt,
                oe.updatedat AS UpdatedAt
            FROM payroll.overtimeentries oe
            JOIN employee.Employees e ON oe.employeeid = e.employeeid
            LEFT JOIN payroll.overtimerules r ON oe.ruleid = r.ruleid
            WHERE {whereClause}
            ORDER BY oe.overtimedate DESC
            LIMIT @PageSize OFFSET @Offset;";

        var parameters = new
        {
            organizationId,
            filter.PeriodId,
            filter.RuleId,
            filter.OvertimeType,
            filter.EmployeeName,
            PageSize = filter.PageSize,
            Offset = (filter.Page - 1) * filter.PageSize
        };

        var totalCount = await QuerySingleAsync<int>(countSql, parameters);
        var data = await QueryAsync<OvertimeEntryDto>(sql, parameters);

        return new PagedResult<OvertimeEntryDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<OvertimeEntryDto?> GetOvertimeEntryByIdAsync(Guid overtimeId, Guid organizationId)
    {
        var sql = @"
            SELECT
                oe.overtimeid AS OvertimeId,
                oe.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                oe.periodid AS PeriodId,
                oe.ruleid AS RuleId,
                oe.hoursworked AS HoursWorked,
                oe.finalamount AS FinalAmount,
                oe.entrystatus AS EntryStatus,
                r.overtimetype AS OvertimeType,
                oe.approvedby AS ApprovedBy,
                oe.overtimedate AS OvertimeDate,
                oe.createdat AS CreatedAt,
                oe.updatedat AS UpdatedAt
            FROM payroll.overtimeentries oe
            JOIN employee.Employees e ON oe.employeeid = e.employeeid
            LEFT JOIN payroll.overtimerules r ON oe.ruleid = r.ruleid
            WHERE oe.overtimeid = @overtimeId AND e.organizationid = @organizationId;";

        return await QuerySingleOrDefaultAsync<OvertimeEntryDto>(sql, new { overtimeId, organizationId });
    }

    public async Task<OvertimeEntryDto> CreateOvertimeEntryAsync(Guid organizationId, CreateOvertimeEntryDto dto)
    {
        var sql = @"
            INSERT INTO payroll.overtimeentries (
                employeeid, periodid, ruleid, hoursworked, finalamount, entrystatus, overtimedate
            )
            VALUES (
                @EmployeeId, @PeriodId, @RuleId, @HoursWorked, @FinalAmount, @EntryStatus, @OvertimeDate
            )
            RETURNING overtimeid;";

        var overtimeId = await QuerySingleAsync<Guid>(sql, dto);
        return (await GetOvertimeEntryByIdAsync(overtimeId, organizationId))!;
    }

    public async Task<OvertimeEntryDto?> UpdateOvertimeEntryAsync(Guid overtimeId, Guid organizationId, UpdateOvertimeEntryDto dto)
    {
        var sql = @"
            UPDATE payroll.overtimeentries oe
            SET 
                ruleid = @RuleId,
                hoursworked = @HoursWorked,
                finalamount = @FinalAmount,
                entrystatus = @EntryStatus,
                approvedby = @ApprovedBy,
                overtimedate = @OvertimeDate,
                updatedat = current_timestamp
            FROM employee.Employees e
            WHERE oe.employeeid = e.employeeid AND oe.overtimeid = @overtimeId AND e.organizationid = @organizationId
            RETURNING oe.overtimeid;";

        var parameters = new
        {
            overtimeId,
            organizationId,
            dto.RuleId,
            dto.HoursWorked,
            dto.FinalAmount,
            dto.EntryStatus,
            dto.ApprovedBy,
            dto.OvertimeDate
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetOvertimeEntryByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteOvertimeEntryAsync(Guid overtimeId, Guid organizationId)
    {
        var sql = @"
            DELETE FROM payroll.overtimeentries oe
            USING employee.Employees e
            WHERE oe.employeeid = e.employeeid AND oe.overtimeid = @overtimeId AND e.organizationid = @organizationId
            RETURNING oe.overtimeid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { overtimeId, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Payroll Periods
    public async Task<PayrollPeriodPagedResult> GetAllPayrollPeriodsAsync(Guid organizationId, PayrollPeriodFilterDto filter)
    {
        filter ??= new PayrollPeriodFilterDto();
        if (filter.PageSize <= 0) filter.PageSize = 10;
        if (filter.Page <= 0) filter.Page = 1;

        var whereClause = "WHERE p.organizationid = @organizationId AND p.isdeleted = false";
        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);

        if (!string.IsNullOrWhiteSpace(filter.PeriodName))
        {
            whereClause += " AND p.periodname ILIKE @PeriodName";
            parameters.Add("PeriodName", $"%{filter.PeriodName}%");
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "All")
        {
            whereClause += " AND p.status = @Status";
            parameters.Add("Status", filter.Status);
        }

        var countSql = $@"
            SELECT 
                COUNT(*) AS TotalCount,
                COUNT(CASE WHEN p.status = 'Open' THEN 1 END) AS OpenCount,
                COUNT(CASE WHEN p.status = 'Processed' THEN 1 END) AS ProcessedCount,
                COUNT(CASE WHEN p.status = 'Locked' OR p.status = 'Closed' THEN 1 END) AS LockedCount
            FROM payroll.payrollperiods p 
            {whereClause};";

        var summary = await QuerySingleAsync<dynamic>(countSql, parameters);
        var totalCount = Convert.ToInt32(summary.totalcount ?? 0);
        var openCount = Convert.ToInt32(summary.opencount ?? 0);
        var processedCount = Convert.ToInt32(summary.processedcount ?? 0);
        var lockedCount = Convert.ToInt32(summary.lockedcount ?? 0);

        var sql = $@"
            SELECT 
                p.periodid AS PeriodId,
                p.organizationid AS OrganizationId,
                p.periodname AS PeriodName,
                p.startdate AS StartDate,
                p.enddate AS EndDate,
                p.paymentdate AS PaymentDate,
                p.status AS Status,
                p.description AS Description,
                p.isactive AS IsActive,
                p.createdby AS CreatedBy,
                e.firstname || ' ' || e.lastname AS CreatedByEmployeeName,
                p.createdat AS CreatedAt,
                p.updatedat AS UpdatedAt
            FROM payroll.payrollperiods p
            LEFT JOIN employee.Employees e ON p.createdby = e.employeeid
            {whereClause}
            ORDER BY p.startdate DESC
            LIMIT @PageSize OFFSET @Offset;";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", (filter.Page - 1) * filter.PageSize);

        var data = await QueryAsync<PayrollPeriodDto>(sql, parameters);

        return new PayrollPeriodPagedResult
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            OpenCount = openCount,
            ProcessedCount = processedCount,
            LockedCount = lockedCount
        };
    }

    public async Task<PayrollPeriodDto?> GetPayrollPeriodByIdAsync(Guid periodId, Guid organizationId)
    {
        var sql = @"
            SELECT 
                p.periodid AS PeriodId,
                p.organizationid AS OrganizationId,
                p.periodname AS PeriodName,
                p.startdate AS StartDate,
                p.enddate AS EndDate,
                p.paymentdate AS PaymentDate,
                p.status AS Status,
                p.description AS Description,
                p.isactive AS IsActive,
                p.createdby AS CreatedBy,
                e.firstname || ' ' || e.lastname AS CreatedByEmployeeName,
                p.createdat AS CreatedAt,
                p.updatedat AS UpdatedAt
            FROM payroll.payrollperiods p
            LEFT JOIN employee.Employees e ON p.createdby = e.employeeid
            WHERE p.periodid = @periodId AND p.organizationid = @organizationId AND p.isdeleted = false;";

        return await QuerySingleOrDefaultAsync<PayrollPeriodDto>(sql, new { periodId, organizationId });
    }

    public async Task<PayrollPeriodDto> CreatePayrollPeriodAsync(Guid organizationId, Guid employeeId, CreatePayrollPeriodDto dto)
    {
        try
        {
            var sql = @"
            INSERT INTO payroll.payrollperiods (
                organizationid, periodname, startdate, enddate, paymentdate, status, description, createdby, isactive, isdeleted
            )
            VALUES (
                @organizationId, @PeriodName, @StartDate, @EndDate, @PaymentDate, @Status, @Description, @employeeId, true, false
            )
            RETURNING periodid;";

            var parameters = new
            {
                organizationId,
                employeeId,
                dto.PeriodName,
                dto.StartDate,
                dto.EndDate,
                dto.PaymentDate,
                dto.Status,
                dto.Description
            };

            var periodId = await QuerySingleAsync<Guid>(sql, parameters);
            return (await GetPayrollPeriodByIdAsync(periodId, organizationId))!;

        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }

    }

    public async Task<PayrollPeriodDto?> UpdatePayrollPeriodAsync(Guid periodId, Guid organizationId, UpdatePayrollPeriodDto dto)
    {
        var sql = @"
            UPDATE payroll.payrollperiods
            SET 
                periodname = @PeriodName,
                startdate = @StartDate,
                enddate = @EndDate,
                paymentdate = @PaymentDate,
                status = @Status,
                description = @Description,
                isactive = @IsActive,
                updatedat = current_timestamp
            WHERE periodid = @periodId AND organizationid = @organizationId
            RETURNING periodid;";

        var parameters = new
        {
            periodId,
            organizationId,
            dto.PeriodName,
            dto.StartDate,
            dto.EndDate,
            dto.PaymentDate,
            dto.Status,
            dto.Description,
            dto.IsActive
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetPayrollPeriodByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeletePayrollPeriodAsync(Guid periodId, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.payrollperiods
            SET isdeleted = true, updatedat = current_timestamp
            WHERE periodid = @periodId AND organizationid = @organizationId
            RETURNING periodid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { periodId, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Attendance Deduction Rules
    public async Task<IEnumerable<AttendanceDeductionRuleDto>> GetAllAttendanceDeductionRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                fixeddeduction AS FixedDeduction,
                percentagededuction AS PercentageDeduction,
                halfdaymultiplier AS HalfDayMultiplier,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.attendancedeductionrules
            WHERE organizationid = @organizationId AND isdeleted = false
            ORDER BY createdat DESC;";

        return await QueryAsync<AttendanceDeductionRuleDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<AttendanceDeductionRuleDto>> GetActiveAttendanceDeductionRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                fixeddeduction AS FixedDeduction,
                percentagededuction AS PercentageDeduction,
                halfdaymultiplier AS HalfDayMultiplier,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.attendancedeductionrules
            WHERE organizationid = @organizationId AND isdeleted = false AND isactive = true
            ORDER BY createdat DESC;";

        return await QueryAsync<AttendanceDeductionRuleDto>(sql, new { organizationId });
    }

    public async Task<AttendanceDeductionRuleDto?> GetAttendanceDeductionRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                fixeddeduction AS FixedDeduction,
                percentagededuction AS PercentageDeduction,
                halfdaymultiplier AS HalfDayMultiplier,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.attendancedeductionrules
            WHERE ruleid = @ruleId AND organizationid = @organizationId AND isdeleted = false;";

        return await QuerySingleOrDefaultAsync<AttendanceDeductionRuleDto>(sql, new { ruleId, organizationId });
    }

    public async Task<AttendanceDeductionRuleDto> CreateAttendanceDeductionRuleAsync(Guid organizationId, CreateAttendanceDeductionRuleDto dto)
    {
        var sql = @"
            INSERT INTO payroll.attendancedeductionrules (
                organizationid, rulename, description, fixeddeduction, 
                percentagededuction, halfdaymultiplier, isactive, isdeleted
            )
            VALUES (
                @organizationId, @RuleName, @Description, @FixedDeduction, 
                @PercentageDeduction, @HalfDayMultiplier, @IsActive, false
            )
            RETURNING ruleid;";

        var parameters = new
        {
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.FixedDeduction,
            dto.PercentageDeduction,
            dto.HalfDayMultiplier,
            dto.IsActive
        };

        var ruleId = await QuerySingleAsync<Guid>(sql, parameters);
        return (await GetAttendanceDeductionRuleByIdAsync(ruleId, organizationId))!;
    }

    public async Task<AttendanceDeductionRuleDto?> UpdateAttendanceDeductionRuleAsync(Guid ruleId, Guid organizationId, UpdateAttendanceDeductionRuleDto dto)
    {
        var sql = @"
            UPDATE payroll.attendancedeductionrules 
            SET 
                rulename = @RuleName,
                description = @Description,
                fixeddeduction = @FixedDeduction,
                percentagededuction = @PercentageDeduction,
                halfdaymultiplier = @HalfDayMultiplier,
                isactive = @IsActive,
                updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING ruleid;";

        var parameters = new
        {
            ruleId,
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.FixedDeduction,
            dto.PercentageDeduction,
            dto.HalfDayMultiplier,
            dto.IsActive
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetAttendanceDeductionRuleByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteAttendanceDeductionRuleAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.attendancedeductionrules
            SET isdeleted = true, updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING ruleid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { ruleId, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Late Arrival Rules
    public async Task<IEnumerable<LateArrivalRuleDto>> GetAllLateArrivalRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                fixeddeduction AS FixedDeduction,
                percentagededuction AS PercentageDeduction,
                graceminutes AS GraceMinutes,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.lateattendancerules
            WHERE organizationid = @organizationId AND isdeleted = false
            ORDER BY createdat DESC;";

        return await QueryAsync<LateArrivalRuleDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<LateArrivalRuleDto>> GetActiveLateArrivalRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                fixeddeduction AS FixedDeduction,
                percentagededuction AS PercentageDeduction,
                graceminutes AS GraceMinutes,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.lateattendancerules
            WHERE organizationid = @organizationId AND isdeleted = false AND isactive = true
            ORDER BY createdat DESC;";

        return await QueryAsync<LateArrivalRuleDto>(sql, new { organizationId });
    }

    public async Task<LateArrivalRuleDto?> GetLateArrivalRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                fixeddeduction AS FixedDeduction,
                percentagededuction AS PercentageDeduction,
                graceminutes AS GraceMinutes,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.lateattendancerules
            WHERE ruleid = @ruleId AND organizationid = @organizationId AND isdeleted = false;";

        return await QuerySingleOrDefaultAsync<LateArrivalRuleDto>(sql, new { ruleId, organizationId });
    }

    public async Task<LateArrivalRuleDto> CreateLateArrivalRuleAsync(Guid organizationId, CreateLateArrivalRuleDto dto)
    {
        var sql = @"
            INSERT INTO payroll.lateattendancerules (
                organizationid, rulename, description, fixeddeduction, 
                percentagededuction, graceminutes, isactive, isdeleted
            )
            VALUES (
                @organizationId, @RuleName, @Description, @FixedDeduction, 
                @PercentageDeduction, @GraceMinutes, @IsActive, false
            )
            RETURNING ruleid;";

        var parameters = new
        {
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.FixedDeduction,
            dto.PercentageDeduction,
            dto.GraceMinutes,
            dto.IsActive
        };

        var ruleId = await QuerySingleAsync<Guid>(sql, parameters);
        return (await GetLateArrivalRuleByIdAsync(ruleId, organizationId))!;
    }

    public async Task<LateArrivalRuleDto?> UpdateLateArrivalRuleAsync(Guid ruleId, Guid organizationId, UpdateLateArrivalRuleDto dto)
    {
        var sql = @"
            UPDATE payroll.lateattendancerules 
            SET 
                rulename = @RuleName,
                description = @Description,
                fixeddeduction = @FixedDeduction,
                percentagededuction = @PercentageDeduction,
                graceminutes = @GraceMinutes,
                isactive = @IsActive,
                updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING ruleid;";

        var parameters = new
        {
            ruleId,
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.FixedDeduction,
            dto.PercentageDeduction,
            dto.GraceMinutes,
            dto.IsActive
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetLateArrivalRuleByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteLateArrivalRuleAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.lateattendancerules
            SET isdeleted = true, updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING ruleid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { ruleId, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Leave Rules
    public async Task<IEnumerable<LeaveRuleDto>> GetAllLeaveRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                unpaidmultiplier AS UnpaidMultiplier,
                halfpaidmultiplier AS HalfPaidMultiplier,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.leaverules
            WHERE organizationid = @organizationId AND isdeleted = false
            ORDER BY createdat DESC;";

        return await QueryAsync<LeaveRuleDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<LeaveRuleDto>> GetActiveLeaveRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                unpaidmultiplier AS UnpaidMultiplier,
                halfpaidmultiplier AS HalfPaidMultiplier,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.leaverules
            WHERE organizationid = @organizationId AND isdeleted = false AND isactive = true
            ORDER BY createdat DESC;";

        return await QueryAsync<LeaveRuleDto>(sql, new { organizationId });
    }

    public async Task<LeaveRuleDto?> GetLeaveRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                unpaidmultiplier AS UnpaidMultiplier,
                halfpaidmultiplier AS HalfPaidMultiplier,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.leaverules
            WHERE ruleid = @ruleId AND organizationid = @organizationId AND isdeleted = false;";

        return await QuerySingleOrDefaultAsync<LeaveRuleDto>(sql, new { ruleId, organizationId });
    }

    public async Task<LeaveRuleDto> CreateLeaveRuleAsync(Guid organizationId, CreateLeaveRuleDto dto)
    {
        var sql = @"
            INSERT INTO payroll.leaverules (
                organizationid, rulename, description, unpaidmultiplier, 
                halfpaidmultiplier, isactive, isdeleted
            )
            VALUES (
                @organizationId, @RuleName, @Description, @UnpaidMultiplier, 
                @HalfPaidMultiplier, @IsActive, false
            )
            RETURNING 
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                unpaidmultiplier AS UnpaidMultiplier,
                halfpaidmultiplier AS HalfPaidMultiplier,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt;";

        var parameters = new
        {
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.UnpaidMultiplier,
            dto.HalfPaidMultiplier,
            dto.IsActive
        };

        return await QuerySingleAsync<LeaveRuleDto>(sql, parameters);
    }

    public async Task<LeaveRuleDto?> UpdateLeaveRuleAsync(Guid ruleId, Guid organizationId, UpdateLeaveRuleDto dto)
    {
        var sql = @"
            UPDATE payroll.leaverules
            SET 
                rulename = @RuleName,
                description = @Description,
                unpaidmultiplier = @UnpaidMultiplier,
                halfpaidmultiplier = @HalfPaidMultiplier,
                isactive = @IsActive,
                updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING 
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                unpaidmultiplier AS UnpaidMultiplier,
                halfpaidmultiplier AS HalfPaidMultiplier,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt;";

        var parameters = new
        {
            ruleId,
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.UnpaidMultiplier,
            dto.HalfPaidMultiplier,
            dto.IsActive
        };

        return await QuerySingleOrDefaultAsync<LeaveRuleDto>(sql, parameters);
    }

    public async Task<bool> DeleteLeaveRuleAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.leaverules
            SET isdeleted = true, updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING ruleid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { ruleId, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Attendance Summary
    public async Task<PagedResult<PayrollAttendanceSummaryDto>> GetAllAttendanceSummariesAsync(Guid organizationId, PayrollAttendanceSummaryFilterDto filter)
    {
        var conditions = new List<string> { "e.organizationid = @organizationId" };
        if (filter.PeriodId.HasValue) conditions.Add("ats.periodid = @PeriodId");
        if (filter.RuleId.HasValue) conditions.Add("ats.ruleid = @RuleId");
        if (!string.IsNullOrEmpty(filter.EmployeeName)) conditions.Add("(e.firstname || ' ' || e.lastname) ILIKE @EmployeeName");

        var whereClause = string.Join(" AND ", conditions);

        var countSql = $@"
            SELECT COUNT(1)
            FROM payroll.attendancesummary ats
            JOIN employee.Employees e ON ats.employeeid = e.employeeid
            WHERE {whereClause};";

        var sql = $@"
            SELECT
                ats.id AS Id,
                ats.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                ats.periodid AS PeriodId,
                ats.ruleid AS RuleId,
                adr.rulename AS RuleName,
                ats.shiftid AS ShiftId,
                s.shiftname AS ShiftName,
                ats.scheduleddays AS ScheduledDays,
                ats.totaldays AS TotalDays,
                ats.presentdays AS PresentDays,
                ats.absentdays AS AbsentDays,
                ats.halfdays AS HalfDays,
                ats.effectiveabsents AS EffectiveAbsents,
                ats.absentdeduction AS AbsentDeduction,
                ats.halfdaydeduction AS HalfDayDeduction,
                ats.totaldeduction AS TotalDeduction,
                ats.createdat AS CreatedAt,
                ats.updatedat AS UpdatedAt
            FROM payroll.attendancesummary ats
            JOIN employee.Employees e ON ats.employeeid = e.employeeid
            LEFT JOIN payroll.attendancedeductionrules adr ON ats.ruleid = adr.ruleid
            LEFT JOIN attendance.shifts s ON ats.shiftid = s.shiftid
            WHERE {whereClause}
            ORDER BY ats.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);
        parameters.Add("PeriodId", filter.PeriodId);
        parameters.Add("RuleId", filter.RuleId);
        parameters.Add("EmployeeName", $"%{filter.EmployeeName}%");
        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", (filter.Page - 1) * filter.PageSize);

        var totalCount = await QuerySingleAsync<int>(countSql, parameters);
        var data = await QueryAsync<PayrollAttendanceSummaryDto>(sql, parameters);

        return new PagedResult<PayrollAttendanceSummaryDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PayrollAttendanceSummaryDto?> GetAttendanceSummaryByIdAsync(Guid id, Guid organizationId)
    {
        try
        {
            var sql = @"
            SELECT
                ats.id AS Id,
                ats.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                ats.periodid AS PeriodId,
                ats.ruleid AS RuleId,
                adr.rulename AS RuleName,
                ats.shiftid AS ShiftId,
                s.shiftname AS ShiftName,
                ats.scheduleddays AS ScheduledDays,
                ats.totaldays AS TotalDays,
                ats.presentdays AS PresentDays,
                ats.absentdays AS AbsentDays,
                ats.halfdays AS HalfDays,
                ats.effectiveabsents AS EffectiveAbsents,
                ats.absentdeduction AS AbsentDeduction,
                ats.halfdaydeduction AS HalfDayDeduction,
                ats.totaldeduction AS TotalDeduction,
                ats.createdat AS CreatedAt,
                ats.updatedat AS UpdatedAt
            FROM payroll.attendancesummary ats
            JOIN employee.Employees e ON ats.employeeid = e.employeeid
            LEFT JOIN payroll.attendancedeductionrules adr ON ats.ruleid = adr.ruleid
            LEFT JOIN attendance.shifts s ON ats.shiftid = s.shiftid
            WHERE ats.id = @id AND e.organizationid = @organizationId;";

            return await QuerySingleOrDefaultAsync<PayrollAttendanceSummaryDto>(sql, new { id, organizationId });

        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<PayrollAttendanceSummaryDto> CreateAttendanceSummaryAsync(
        Guid organizationId,
        CreatePayrollAttendanceSummaryDto dto,
        Guid? shiftId,
        int scheduledDays,
        int totalDays)
    {
        try
        {

            var sql = @"
            INSERT INTO payroll.attendancesummary (
                employeeid, periodid, ruleid, shiftid, scheduleddays, 
                totaldays, presentdays, absentdays, halfdays, effectiveabsents, 
                absentdeduction, halfdaydeduction, totaldeduction
            )
            VALUES (
                @EmployeeId, @PeriodId, @RuleId, @ShiftId, @ScheduledDays, 
                @TotalDays, @PresentDays, @AbsentDays, @HalfDays, @EffectiveAbsents, 
                @AbsentDeduction, @HalfDayDeduction, @TotalDeduction
            )
            RETURNING id;";

            var parameters = new
            {
                dto.EmployeeId,
                dto.PeriodId,
                dto.RuleId,
                ShiftId = shiftId,
                ScheduledDays = scheduledDays,
                TotalDays = totalDays,
                dto.PresentDays,
                dto.AbsentDays,
                dto.HalfDays,
                dto.EffectiveAbsents,
                dto.AbsentDeduction,
                dto.HalfDayDeduction,
                dto.TotalDeduction
            };

            var id = await QuerySingleAsync<Guid>(sql, parameters);
            return (await GetAttendanceSummaryByIdAsync(id, organizationId))!;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<PayrollAttendanceSummaryDto?> UpdateAttendanceSummaryAsync(Guid id, Guid organizationId, UpdatePayrollAttendanceSummaryDto dto)
    {
        // Note: shiftid, scheduleddays, totaldays are intentionally NOT updated here;
        // they are auto-computed at creation time from the employee's shift assignment and the period dates.
        var sql = @"
            UPDATE payroll.attendancesummary ats
            SET 
                ruleid = @RuleId,
                presentdays = @PresentDays,
                absentdays = @AbsentDays,
                halfdays = @HalfDays,
                effectiveabsents = @EffectiveAbsents,
                absentdeduction = @AbsentDeduction,
                halfdaydeduction = @HalfDayDeduction,
                totaldeduction = @TotalDeduction,
                updatedat = current_timestamp
            FROM employee.Employees e
            WHERE ats.employeeid = e.employeeid AND ats.id = @id AND e.organizationid = @organizationId
            RETURNING ats.id;";

        var parameters = new
        {
            id,
            organizationId,
            dto.RuleId,
            dto.PresentDays,
            dto.AbsentDays,
            dto.HalfDays,
            dto.EffectiveAbsents,
            dto.AbsentDeduction,
            dto.HalfDayDeduction,
            dto.TotalDeduction
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetAttendanceSummaryByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteAttendanceSummaryAsync(Guid id, Guid organizationId)
    {
        var sql = @"
            DELETE FROM payroll.attendancesummary ats
            USING employee.Employees e
            WHERE ats.employeeid = e.employeeid AND ats.id = @id AND e.organizationid = @organizationId
            RETURNING ats.id;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { id, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Late Attendance
    public async Task<PagedResult<LateAttendanceDto>> GetAllLateAttendanceAsync(Guid organizationId, LateAttendanceFilterDto filter)
    {
        var conditions = new List<string> { "e.organizationid = @organizationId" };
        if (filter.PeriodId.HasValue) conditions.Add("la.periodid = @PeriodId");
        if (filter.RuleId.HasValue) conditions.Add("la.ruleid = @RuleId");
        if (!string.IsNullOrEmpty(filter.EmployeeName)) conditions.Add("(e.firstname || ' ' || e.lastname) ILIKE @EmployeeName");

        var whereClause = string.Join(" AND ", conditions);

        var countSql = $@"
            SELECT COUNT(1)
            FROM payroll.lateattendance la
            JOIN employee.Employees e ON la.employeeid = e.employeeid
            WHERE {whereClause};";

        var sql = $@"
            SELECT
                la.id AS Id,
                la.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                la.periodid AS PeriodId,
                la.ruleid AS RuleId,
                lar.rulename AS RuleName,
                la.lateminutes AS LateMinutes,
                la.deduction AS Deduction,
                la.isgrace AS IsGrace,
                la.attendancedate AS AttendanceDate,
                la.createdat AS CreatedAt,
                la.updatedat AS UpdatedAt
            FROM payroll.lateattendance la
            JOIN employee.Employees e ON la.employeeid = e.employeeid
            LEFT JOIN payroll.lateattendancerules lar ON la.ruleid = lar.ruleid
            WHERE {whereClause}
            ORDER BY la.attendancedate DESC, la.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);
        parameters.Add("PeriodId", filter.PeriodId);
        parameters.Add("RuleId", filter.RuleId);
        parameters.Add("EmployeeName", $"%{filter.EmployeeName}%");
        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", (filter.Page - 1) * filter.PageSize);

        var totalCount = await QuerySingleAsync<int>(countSql, parameters);
        var data = await QueryAsync<LateAttendanceDto>(sql, parameters);

        return new PagedResult<LateAttendanceDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<LateAttendanceDto?> GetLateAttendanceByIdAsync(Guid id, Guid organizationId)
    {
        var sql = @"
            SELECT
                la.id AS Id,
                la.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                la.periodid AS PeriodId,
                la.ruleid AS RuleId,
                lar.rulename AS RuleName,
                la.lateminutes AS LateMinutes,
                la.deduction AS Deduction,
                la.isgrace AS IsGrace,
                la.attendancedate AS AttendanceDate,
                la.createdat AS CreatedAt,
                la.updatedat AS UpdatedAt
            FROM payroll.lateattendance la
            JOIN employee.Employees e ON la.employeeid = e.employeeid
            LEFT JOIN payroll.lateattendancerules lar ON la.ruleid = lar.ruleid
            WHERE la.id = @id AND e.organizationid = @organizationId;";

        return await QuerySingleOrDefaultAsync<LateAttendanceDto>(sql, new { id, organizationId });
    }

    public async Task<LateAttendanceDto> CreateLateAttendanceAsync(Guid organizationId, CreateLateAttendanceDto dto)
    {
        var sql = @"
            INSERT INTO payroll.lateattendance (
                employeeid, periodid, ruleid, lateminutes, 
                deduction, isgrace, attendancedate
            )
            VALUES (
                @EmployeeId, @PeriodId, @RuleId, @LateMinutes, 
                @Deduction, @IsGrace, @AttendanceDate
            )
            RETURNING id;";

        var id = await QuerySingleAsync<Guid>(sql, dto);
        return (await GetLateAttendanceByIdAsync(id, organizationId))!;
    }

    public async Task<LateAttendanceDto?> UpdateLateAttendanceAsync(Guid id, Guid organizationId, UpdateLateAttendanceDto dto)
    {
        var sql = @"
            UPDATE payroll.lateattendance la
            SET 
                ruleid = @RuleId,
                lateminutes = @LateMinutes,
                deduction = @Deduction,
                isgrace = @IsGrace,
                attendancedate = @AttendanceDate,
                updatedat = current_timestamp
            FROM employee.Employees e
            WHERE la.employeeid = e.employeeid AND la.id = @id AND e.organizationid = @organizationId
            RETURNING la.id;";

        var parameters = new
        {
            id,
            organizationId,
            dto.RuleId,
            dto.LateMinutes,
            dto.Deduction,
            dto.IsGrace,
            dto.AttendanceDate
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null) return null;

        return await GetLateAttendanceByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteLateAttendanceAsync(Guid id, Guid organizationId)
    {
        var sql = @"
            DELETE FROM payroll.lateattendance la
            USING employee.Employees e
            WHERE la.employeeid = e.employeeid AND la.id = @id AND e.organizationid = @organizationId
            RETURNING la.id;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { id, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Leave Summary
    public async Task<PagedResult<LeaveSummaryDto>> GetLeaveSummariesAsync(Guid organizationId, LeaveSummaryFilterDto filter)
    {
        var conditions = new List<string> { "e.organizationid = @organizationId" };
        if (filter.PeriodId.HasValue) conditions.Add("ls.periodid = @PeriodId");
        if (filter.RuleId.HasValue) conditions.Add("ls.ruleid = @RuleId");
        if (!string.IsNullOrEmpty(filter.EmployeeName)) conditions.Add("(e.firstname || ' ' || e.lastname) ILIKE @EmployeeName");

        var whereClause = string.Join(" AND ", conditions);

        var countSql = $@"
            SELECT COUNT(1)
            FROM payroll.payrollleavesummary ls
            JOIN employee.Employees e ON ls.employeeid = e.employeeid
            WHERE {whereClause};";

        var sql = $@"
            SELECT
                ls.id AS Id,
                ls.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                e.employeecode AS EmployeeCode,
                ls.periodid AS PeriodId,
                ls.ruleid AS RuleId,
                lr.rulename AS RuleName,
                ls.paiddays AS PaidDays,
                ls.unpaiddays AS UnpaidDays,
                ls.halfpaiddays AS HalfPaidDays,
                ls.unpaiddeduction AS UnpaidDeduction,
                ls.halfpaiddeduction AS HalfPaidDeduction,
                ls.totalleavedays AS TotalLeaveDays,
                ls.totaldeduction AS TotalDeduction,
                ls.createdat AS CreatedAt,
                ls.updatedat AS UpdatedAt
            FROM payroll.payrollleavesummary ls
            JOIN employee.Employees e ON ls.employeeid = e.employeeid
            LEFT JOIN payroll.leaverules lr ON ls.ruleid = lr.ruleid
            WHERE {whereClause}
            ORDER BY ls.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);
        parameters.Add("PeriodId", filter.PeriodId);
        parameters.Add("RuleId", filter.RuleId);
        parameters.Add("EmployeeName", $"%{filter.EmployeeName}%");
        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", (filter.Page - 1) * filter.PageSize);

        var totalCount = await QuerySingleAsync<int>(countSql, parameters);
        var data = await QueryAsync<LeaveSummaryDto>(sql, parameters);

        return new PagedResult<LeaveSummaryDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<LeaveSummaryDto?> GetLeaveSummaryByIdAsync(Guid id, Guid organizationId)
    {
        var sql = @"
            SELECT
                ls.id AS Id,
                ls.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                e.employeecode AS EmployeeCode,
                ls.periodid AS PeriodId,
                ls.ruleid AS RuleId,
                lr.rulename AS RuleName,
                ls.paiddays AS PaidDays,
                ls.unpaiddays AS UnpaidDays,
                ls.halfpaiddays AS HalfPaidDays,
                ls.unpaiddeduction AS UnpaidDeduction,
                ls.halfpaiddeduction AS HalfPaidDeduction,
                ls.totalleavedays AS TotalLeaveDays,
                ls.totaldeduction AS TotalDeduction,
                ls.createdat AS CreatedAt,
                ls.updatedat AS UpdatedAt
            FROM payroll.payrollleavesummary ls
            JOIN employee.Employees e ON ls.employeeid = e.employeeid
            LEFT JOIN payroll.leaverules lr ON ls.ruleid = lr.ruleid
            WHERE ls.id = @id AND e.organizationid = @organizationId;";

        return await QuerySingleOrDefaultAsync<LeaveSummaryDto>(sql, new { id, organizationId });
    }

    public async Task<LeaveSummaryDto> CreateLeaveSummaryAsync(Guid organizationId, CreateLeaveSummaryDto dto)
    {
        var sql = @"
            INSERT INTO payroll.payrollleavesummary (
                employeeid, periodid, ruleid, paiddays, 
                unpaiddays, halfpaiddays, unpaiddeduction, 
                halfpaiddeduction, totaldeduction
            )
            VALUES (
                @EmployeeId, @PeriodId, @RuleId, @PaidDays, 
                @UnpaidDays, @HalfPaidDays, @UnpaidDeduction, 
                @HalfPaidDeduction, @TotalDeduction
            )
            RETURNING id;";

        var id = await QuerySingleAsync<Guid>(sql, dto);
        return (await GetLeaveSummaryByIdAsync(id, organizationId))!;
    }

    public async Task<LeaveSummaryDto?> UpdateLeaveSummaryAsync(Guid id, Guid organizationId, UpdateLeaveSummaryDto dto)
    {
        var sql = @"
            UPDATE payroll.payrollleavesummary ls
            SET 
                ruleid = @RuleId,
                paiddays = @PaidDays,
                unpaiddays = @UnpaidDays,
                halfpaiddays = @HalfPaidDays,
                unpaiddeduction = @UnpaidDeduction,
                halfpaiddeduction = @HalfPaidDeduction,
                totaldeduction = @TotalDeduction,
                updatedat = current_timestamp
            FROM employee.Employees e
            WHERE ls.employeeid = e.employeeid AND ls.id = @id AND e.organizationid = @organizationId
            RETURNING ls.id;";

        var parameters = new
        {
            id,
            organizationId,
            dto.RuleId,
            dto.PaidDays,
            dto.UnpaidDays,
            dto.HalfPaidDays,
            dto.UnpaidDeduction,
            dto.HalfPaidDeduction,
            dto.TotalDeduction
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null) return null;

        return await GetLeaveSummaryByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteLeaveSummaryAsync(Guid id, Guid organizationId)
    {
        var sql = @"
            DELETE FROM payroll.payrollleavesummary ls
            USING employee.Employees e
            WHERE ls.employeeid = e.employeeid AND ls.id = @id AND e.organizationid = @organizationId
            RETURNING ls.id;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { id, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }


    // Performance Rules
    public async Task<IEnumerable<PerformanceRuleDto>> GetAllPerformanceRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                minscore AS MinScore,
                maxscore AS MaxScore,
                fixedamount AS FixedAmount,
                percentage AS Percentage,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.performancerules
            WHERE organizationid = @organizationId AND isdeleted = false
            ORDER BY createdat DESC;";

        return await QueryAsync<PerformanceRuleDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<PerformanceRuleDto>> GetActivePerformanceRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                minscore AS MinScore,
                maxscore AS MaxScore,
                fixedamount AS FixedAmount,
                percentage AS Percentage,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.performancerules
            WHERE organizationid = @organizationId AND isdeleted = false AND isactive = true
            ORDER BY createdat DESC;";

        return await QueryAsync<PerformanceRuleDto>(sql, new { organizationId });
    }

    public async Task<PerformanceRuleDto?> GetPerformanceRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                minscore AS MinScore,
                maxscore AS MaxScore,
                fixedamount AS FixedAmount,
                percentage AS Percentage,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.performancerules
            WHERE ruleid = @ruleId AND organizationid = @organizationId AND isdeleted = false;";

        return await QuerySingleOrDefaultAsync<PerformanceRuleDto>(sql, new { ruleId, organizationId });
    }

    public async Task<PerformanceRuleDto> CreatePerformanceRuleAsync(Guid organizationId, CreatePerformanceRuleDto dto)
    {
        var sql = @"
            INSERT INTO payroll.performancerules (
                organizationid, rulename, description, minscore, maxscore, 
                fixedamount, percentage, isactive, isdeleted
            )
            VALUES (
                @organizationId, @RuleName, @Description, @MinScore, @MaxScore, 
                @FixedAmount, @Percentage, @IsActive, false
            )
            RETURNING ruleid;";

        var parameters = new
        {
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.MinScore,
            dto.MaxScore,
            dto.FixedAmount,
            dto.Percentage,
            dto.IsActive
        };

        var ruleId = await QuerySingleAsync<Guid>(sql, parameters);
        return (await GetPerformanceRuleByIdAsync(ruleId, organizationId))!;
    }

    public async Task<PerformanceRuleDto?> UpdatePerformanceRuleAsync(Guid ruleId, Guid organizationId, UpdatePerformanceRuleDto dto)
    {
        var sql = @"
            UPDATE payroll.performancerules
            SET 
                rulename = @RuleName,
                description = @Description,
                minscore = @MinScore,
                maxscore = @MaxScore,
                fixedamount = @FixedAmount,
                percentage = @Percentage,
                isactive = @IsActive,
                updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING ruleid;";

        var parameters = new
        {
            ruleId,
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.MinScore,
            dto.MaxScore,
            dto.FixedAmount,
            dto.Percentage,
            dto.IsActive
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetPerformanceRuleByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeletePerformanceRuleAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.performancerules
            SET isdeleted = true, updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING ruleid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { ruleId, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Bonus Rules
    public async Task<IEnumerable<BonusRuleDto>> GetAllBonusRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                bonustype AS BonusType,
                bonusamount AS BonusAmount,
                bonuspercentage AS BonusPercentage,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.bonusrules
            WHERE organizationid = @organizationId AND isdeleted = false
            ORDER BY createdat DESC;";

        return await QueryAsync<BonusRuleDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<BonusRuleDto>> GetActiveBonusRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                bonustype AS BonusType,
                bonusamount AS BonusAmount,
                bonuspercentage AS BonusPercentage,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.bonusrules
            WHERE organizationid = @organizationId AND isdeleted = false AND isactive = true
            ORDER BY createdat DESC;";

        return await QueryAsync<BonusRuleDto>(sql, new { organizationId });
    }

    public async Task<BonusRuleDto?> GetBonusRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                bonustype AS BonusType,
                bonusamount AS BonusAmount,
                bonuspercentage AS BonusPercentage,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.bonusrules
            WHERE ruleid = @ruleId AND organizationid = @organizationId AND isdeleted = false;";

        return await QuerySingleOrDefaultAsync<BonusRuleDto>(sql, new { ruleId, organizationId });
    }

    public async Task<BonusRuleDto> CreateBonusRuleAsync(Guid organizationId, CreateBonusRuleDto dto)
    {
        var sql = @"
            INSERT INTO payroll.bonusrules (
                organizationid, rulename, description, bonustype, 
                bonusamount, bonuspercentage, isactive, isdeleted
            )
            VALUES (
                @organizationId, @RuleName, @Description, @BonusType, 
                @BonusAmount, @BonusPercentage, @IsActive, false
            )
            RETURNING ruleid;";

        var parameters = new
        {
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.BonusType,
            dto.BonusAmount,
            dto.BonusPercentage,
            dto.IsActive
        };

        var ruleId = await QuerySingleAsync<Guid>(sql, parameters);
        return (await GetBonusRuleByIdAsync(ruleId, organizationId))!;
    }

    public async Task<BonusRuleDto?> UpdateBonusRuleAsync(Guid ruleId, Guid organizationId, UpdateBonusRuleDto dto)
    {
        var sql = @"
            UPDATE payroll.bonusrules
            SET 
                rulename = @RuleName,
                description = @Description,
                bonustype = @BonusType,
                bonusamount = @BonusAmount,
                bonuspercentage = @BonusPercentage,
                isactive = @IsActive,
                updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING ruleid;";

        var parameters = new
        {
            ruleId,
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.BonusType,
            dto.BonusAmount,
            dto.BonusPercentage,
            dto.IsActive
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetBonusRuleByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteBonusRuleAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.bonusrules
            SET isdeleted = true, updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING ruleid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { ruleId, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Performance Pay
    public async Task<PagedResult<PerformancePayDto>> GetAllPerformancePayAsync(Guid organizationId, PerformancePayFilterDto filter)
    {
        filter ??= new PerformancePayFilterDto();
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.PageSize <= 0) filter.PageSize = 10;

        var conditions = new List<string>
        {
            "e.organizationid = @organizationId",
            "per.organizationid = @organizationId"
        };
        if (filter.PeriodId.HasValue) conditions.Add("pp.periodid = @PeriodId");
        if (filter.RuleId.HasValue) conditions.Add("pp.ruleid = @RuleId");
        if (!string.IsNullOrWhiteSpace(filter.EmployeeName))
            conditions.Add("(e.firstname || ' ' || e.lastname) ILIKE '%' || @EmployeeName || '%'");

        var whereClause = string.Join(" AND ", conditions);

        var countSql = $@"
            SELECT COUNT(1)
            FROM payroll.performancepay pp
            JOIN employee.Employees e ON pp.employeeid = e.employeeid
            JOIN payroll.payrollperiods per ON pp.periodid = per.periodid
            LEFT JOIN payroll.performancerules pr ON pp.ruleid = pr.ruleid
            WHERE {whereClause};";

        var sql = $@"
            SELECT
                pp.performancepayid AS PerformancePayId,
                pp.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                pp.periodid AS PeriodId,
                per.periodname AS PeriodName,
                pp.ruleid AS RuleId,
                pr.rulename AS RuleName,
                pp.score AS Score,
                pp.finalamount AS FinalAmount,
                pp.calculatedat AS CalculatedAt,
                pp.createdat AS CreatedAt
            FROM payroll.performancepay pp
            JOIN employee.Employees e ON pp.employeeid = e.employeeid
            JOIN payroll.payrollperiods per ON pp.periodid = per.periodid
            LEFT JOIN payroll.performancerules pr ON pp.ruleid = pr.ruleid
            WHERE {whereClause}
            ORDER BY pp.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        var parameters = new
        {
            organizationId,
            filter.PeriodId,
            filter.RuleId,
            filter.EmployeeName,
            PageSize = filter.PageSize,
            Offset = (filter.Page - 1) * filter.PageSize
        };

        var totalCount = await QuerySingleAsync<int>(countSql, parameters);
        var data = await QueryAsync<PerformancePayDto>(sql, parameters);

        return new PagedResult<PerformancePayDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PerformancePayDto?> GetPerformancePayByIdAsync(Guid performancePayId, Guid organizationId)
    {
        var sql = @"
            SELECT
                pp.performancepayid AS PerformancePayId,
                pp.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                pp.periodid AS PeriodId,
                per.periodname AS PeriodName,
                pp.ruleid AS RuleId,
                pr.rulename AS RuleName,
                pp.score AS Score,
                pp.finalamount AS FinalAmount,
                pp.calculatedat AS CalculatedAt,
                pp.createdat AS CreatedAt
            FROM payroll.performancepay pp
            JOIN employee.Employees e ON pp.employeeid = e.employeeid
            JOIN payroll.payrollperiods per ON pp.periodid = per.periodid
            LEFT JOIN payroll.performancerules pr ON pp.ruleid = pr.ruleid
            WHERE pp.performancepayid = @performancePayId
              AND e.organizationid = @organizationId
              AND per.organizationid = @organizationId;";

        return await QuerySingleOrDefaultAsync<PerformancePayDto>(sql, new { performancePayId, organizationId });
    }

    public async Task<PerformancePayDto> CreatePerformancePayAsync(Guid organizationId, CreatePerformancePayDto dto)
    {
        var sql = @"
            INSERT INTO payroll.performancepay (
                employeeid, periodid, ruleid, score, finalamount
            )
            VALUES (
                @EmployeeId, @PeriodId, @RuleId, @Score, @FinalAmount
            )
            RETURNING performancepayid;";

        var parameters = new
        {
            dto.EmployeeId,
            dto.PeriodId,
            dto.RuleId,
            dto.Score,
            dto.FinalAmount
        };

        var id = await QuerySingleAsync<Guid>(sql, parameters);
        return (await GetPerformancePayByIdAsync(id, organizationId))!;
    }

    public async Task<PerformancePayDto?> UpdatePerformancePayAsync(Guid performancePayId, Guid organizationId, UpdatePerformancePayDto dto)
    {
        var sql = @"
            UPDATE payroll.performancepay pp
            SET
                ruleid = @RuleId,
                score = @Score,
                finalamount = @FinalAmount,
                calculatedat = current_timestamp
            FROM employee.Employees e
            WHERE pp.employeeid = e.employeeid
              AND pp.performancepayid = @performancePayId
              AND e.organizationid = @organizationId
            RETURNING pp.performancepayid;";

        var parameters = new
        {
            performancePayId,
            organizationId,
            dto.RuleId,
            dto.Score,
            dto.FinalAmount
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetPerformancePayByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeletePerformancePayAsync(Guid performancePayId, Guid organizationId)
    {
        var sql = @"
            DELETE FROM payroll.performancepay pp
            USING employee.Employees e
            WHERE pp.employeeid = e.employeeid
              AND pp.performancepayid = @performancePayId
              AND e.organizationid = @organizationId
            RETURNING pp.performancepayid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { performancePayId, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Bonus Entries
    public async Task<PagedResult<BonusEntryDto>> GetAllBonusEntriesAsync(Guid organizationId, BonusEntryFilterDto filter)
    {
        filter ??= new BonusEntryFilterDto();
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.PageSize <= 0) filter.PageSize = 10;

        var conditions = new List<string>
        {
            "be.organizationid = @organizationId",
            "e.organizationid = @organizationId",
            "per.organizationid = @organizationId"
        };
        if (filter.PeriodId.HasValue) conditions.Add("be.periodid = @PeriodId");
        if (filter.RuleId.HasValue) conditions.Add("be.ruleid = @RuleId");
        if (!string.IsNullOrWhiteSpace(filter.EmployeeName))
            conditions.Add("(e.firstname || ' ' || e.lastname) ILIKE '%' || @EmployeeName || '%'");

        var whereClause = string.Join(" AND ", conditions);

        var countSql = $@"
            SELECT COUNT(1)
            FROM payroll.bonusentries be
            JOIN employee.Employees e ON be.employeeid = e.employeeid
            JOIN payroll.payrollperiods per ON be.periodid = per.periodid
            LEFT JOIN payroll.bonusrules br ON be.ruleid = br.ruleid
            WHERE {whereClause};";

        var sql = $@"
            SELECT
                be.bonusid AS BonusId,
                be.organizationid AS OrganizationId,
                be.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                be.periodid AS PeriodId,
                per.periodname AS PeriodName,
                be.ruleid AS RuleId,
                br.rulename AS RuleName,
                br.description AS RuleDescription,
                be.bonousamount AS BonusAmount,
                be.createdat AS CreatedAt,
                be.updatedat AS UpdatedAt
            FROM payroll.bonusentries be
            JOIN employee.Employees e ON be.employeeid = e.employeeid
            JOIN payroll.payrollperiods per ON be.periodid = per.periodid
            LEFT JOIN payroll.bonusrules br ON be.ruleid = br.ruleid
            WHERE {whereClause}
            ORDER BY be.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        var parameters = new
        {
            organizationId,
            filter.PeriodId,
            filter.RuleId,
            filter.EmployeeName,
            PageSize = filter.PageSize,
            Offset = (filter.Page - 1) * filter.PageSize
        };

        var totalCount = await QuerySingleAsync<int>(countSql, parameters);
        var data = await QueryAsync<BonusEntryDto>(sql, parameters);

        return new PagedResult<BonusEntryDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<BonusEntryDto?> GetBonusEntryByIdAsync(Guid bonusId, Guid organizationId)
    {
        var sql = @"
            SELECT
                be.bonusid AS BonusId,
                be.organizationid AS OrganizationId,
                be.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                be.periodid AS PeriodId,
                per.periodname AS PeriodName,
                be.ruleid AS RuleId,
                br.rulename AS RuleName,
                br.description AS RuleDescription,
                be.bonousamount AS BonusAmount,
                be.createdat AS CreatedAt,
                be.updatedat AS UpdatedAt
            FROM payroll.bonusentries be
            JOIN employee.Employees e ON be.employeeid = e.employeeid
            JOIN payroll.payrollperiods per ON be.periodid = per.periodid
            LEFT JOIN payroll.bonusrules br ON be.ruleid = br.ruleid
            WHERE be.bonusid = @bonusId
              AND be.organizationid = @organizationId
              AND e.organizationid = @organizationId
              AND per.organizationid = @organizationId;";

        return await QuerySingleOrDefaultAsync<BonusEntryDto>(sql, new { bonusId, organizationId });
    }

    public async Task<BonusEntryDto> CreateBonusEntryAsync(Guid organizationId, CreateBonusEntryDto dto)
    {
        var sql = @"
            INSERT INTO payroll.bonusentries (
                organizationid, employeeid, periodid, ruleid, bonousamount
            )
            VALUES (
                @organizationId, @EmployeeId, @PeriodId, @RuleId, @BonusAmount
            )
            RETURNING bonusid;";

        var parameters = new
        {
            organizationId,
            dto.EmployeeId,
            dto.PeriodId,
            dto.RuleId,
            dto.BonusAmount
        };

        var id = await QuerySingleAsync<Guid>(sql, parameters);
        return (await GetBonusEntryByIdAsync(id, organizationId))!;
    }

    public async Task<BonusEntryDto?> UpdateBonusEntryAsync(Guid bonusId, Guid organizationId, UpdateBonusEntryDto dto)
    {
        var sql = @"
            UPDATE payroll.bonusentries
            SET
                ruleid = @RuleId,
                bonousamount = @BonusAmount,
                updatedat = current_timestamp
            WHERE bonusid = @bonusId AND organizationid = @organizationId
            RETURNING bonusid;";

        var parameters = new
        {
            bonusId,
            organizationId,
            dto.RuleId,
            dto.BonusAmount
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetBonusEntryByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteBonusEntryAsync(Guid bonusId, Guid organizationId)
    {
        var sql = @"
            DELETE FROM payroll.bonusentries
            WHERE bonusid = @bonusId AND organizationid = @organizationId
            RETURNING bonusid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { bonusId, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Loan Rules
    public async Task<IEnumerable<LoanRuleDto>> GetAllLoanRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                maxloanamount AS MaxLoanAmount,
                maxinstallments AS MaxInstallments,
                interestrate AS InterestRate,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.loanrules
            WHERE organizationid = @organizationId AND isdeleted = false
            ORDER BY createdat DESC;";

        return await QueryAsync<LoanRuleDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<LoanRuleDto>> GetActiveLoanRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                maxloanamount AS MaxLoanAmount,
                maxinstallments AS MaxInstallments,
                interestrate AS InterestRate,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.loanrules
            WHERE organizationid = @organizationId AND isdeleted = false AND isactive = true
            ORDER BY createdat DESC;";

        return await QueryAsync<LoanRuleDto>(sql, new { organizationId });
    }

    public async Task<LoanRuleDto?> GetLoanRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                maxloanamount AS MaxLoanAmount,
                maxinstallments AS MaxInstallments,
                interestrate AS InterestRate,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.loanrules
            WHERE ruleid = @ruleId AND organizationid = @organizationId AND isdeleted = false;";

        return await QuerySingleOrDefaultAsync<LoanRuleDto>(sql, new { ruleId, organizationId });
    }

    public async Task<LoanRuleDto> CreateLoanRuleAsync(Guid organizationId, CreateLoanRuleDto dto)
    {
        var sql = @"
            INSERT INTO payroll.loanrules (
                organizationid, rulename, description, maxloanamount, 
                maxinstallments, interestrate, isactive, isdeleted
            )
            VALUES (
                @organizationId, @RuleName, @Description, @MaxLoanAmount, 
                @MaxInstallments, @InterestRate, @IsActive, false
            )
            RETURNING ruleid;";

        var parameters = new
        {
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.MaxLoanAmount,
            dto.MaxInstallments,
            dto.InterestRate,
            dto.IsActive
        };

        var ruleId = await QuerySingleAsync<Guid>(sql, parameters);
        return (await GetLoanRuleByIdAsync(ruleId, organizationId))!;
    }

    public async Task<LoanRuleDto?> UpdateLoanRuleAsync(Guid ruleId, Guid organizationId, UpdateLoanRuleDto dto)
    {
        var sql = @"
            UPDATE payroll.loanrules
            SET 
                rulename = @RuleName,
                description = @Description,
                maxloanamount = @MaxLoanAmount,
                maxinstallments = @MaxInstallments,
                interestrate = @InterestRate,
                isactive = @IsActive,
                updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING ruleid;";

        var parameters = new
        {
            ruleId,
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.MaxLoanAmount,
            dto.MaxInstallments,
            dto.InterestRate,
            dto.IsActive
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetLoanRuleByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteLoanRuleAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.loanrules
            SET isdeleted = true, updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING ruleid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { ruleId, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Salary Advance Rules
    public async Task<IEnumerable<SalaryAdvanceRuleDto>> GetAllSalaryAdvanceRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                maxpercentage AS MaxPercentage,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.salaryadvancerules
            WHERE organizationid = @organizationId AND isdeleted = false
            ORDER BY createdat DESC;";

        return await QueryAsync<SalaryAdvanceRuleDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<SalaryAdvanceRuleDto>> GetActiveSalaryAdvanceRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                maxpercentage AS MaxPercentage,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.salaryadvancerules
            WHERE organizationid = @organizationId AND isdeleted = false AND isactive = true
            ORDER BY createdat DESC;";

        return await QueryAsync<SalaryAdvanceRuleDto>(sql, new { organizationId });
    }

    public async Task<SalaryAdvanceRuleDto?> GetSalaryAdvanceRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                maxpercentage AS MaxPercentage,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.salaryadvancerules
            WHERE ruleid = @ruleId AND organizationid = @organizationId AND isdeleted = false;";

        return await QuerySingleOrDefaultAsync<SalaryAdvanceRuleDto>(sql, new { ruleId, organizationId });
    }

    public async Task<SalaryAdvanceRuleDto> CreateSalaryAdvanceRuleAsync(Guid organizationId, CreateSalaryAdvanceRuleDto dto)
    {
        var sql = @"
            INSERT INTO payroll.salaryadvancerules (
                organizationid, rulename, description, maxpercentage, isactive, isdeleted
            )
            VALUES (
                @organizationId, @RuleName, @Description, @MaxPercentage, @IsActive, false
            )
            RETURNING ruleid;";

        var parameters = new
        {
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.MaxPercentage,
            dto.IsActive
        };

        var ruleId = await QuerySingleAsync<Guid>(sql, parameters);
        return (await GetSalaryAdvanceRuleByIdAsync(ruleId, organizationId))!;
    }

    public async Task<SalaryAdvanceRuleDto?> UpdateSalaryAdvanceRuleAsync(Guid ruleId, Guid organizationId, UpdateSalaryAdvanceRuleDto dto)
    {
        var sql = @"
            UPDATE payroll.salaryadvancerules
            SET
                rulename = @RuleName,
                description = @Description,
                maxpercentage = @MaxPercentage,
                isactive = @IsActive,
                updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING ruleid;";

        var parameters = new
        {
            ruleId,
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.MaxPercentage,
            dto.IsActive
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetSalaryAdvanceRuleByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteSalaryAdvanceRuleAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.salaryadvancerules
            SET isdeleted = true, updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING ruleid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { ruleId, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Provident Fund Rules
    public async Task<IEnumerable<ProvidentFundRuleDto>> GetAllProvidentFundRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                defaultemployeepct AS DefaultEmployeePct,
                defaultemployerpct AS DefaultEmployerPct,
                contributionbasis AS ContributionBasis,
                withdrawal_config::text AS WithdrawalConfig,
                allowpartialwithdraw AS AllowPartialWithdraw,
                vestingmonths AS VestingMonths,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.pfrules
            WHERE organizationid = @organizationId AND isdeleted = false
            ORDER BY createdat DESC;";

        return await QueryAsync<ProvidentFundRuleDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<ProvidentFundRuleDto>> GetActiveProvidentFundRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                defaultemployeepct AS DefaultEmployeePct,
                defaultemployerpct AS DefaultEmployerPct,
                contributionbasis AS ContributionBasis,
                withdrawal_config::text AS WithdrawalConfig,
                allowpartialwithdraw AS AllowPartialWithdraw,
                vestingmonths AS VestingMonths,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.pfrules
            WHERE organizationid = @organizationId AND isdeleted = false AND isactive = true
            ORDER BY createdat DESC;";

        return await QueryAsync<ProvidentFundRuleDto>(sql, new { organizationId });
    }

    public async Task<ProvidentFundRuleDto?> GetProvidentFundRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                rulename AS RuleName,
                description AS Description,
                defaultemployeepct AS DefaultEmployeePct,
                defaultemployerpct AS DefaultEmployerPct,
                contributionbasis AS ContributionBasis,
                withdrawal_config::text AS WithdrawalConfig,
                allowpartialwithdraw AS AllowPartialWithdraw,
                vestingmonths AS VestingMonths,
                isactive AS IsActive,
                isdeleted AS IsDeleted,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt
            FROM payroll.pfrules
            WHERE ruleid = @ruleId AND organizationid = @organizationId AND isdeleted = false;";

        return await QuerySingleOrDefaultAsync<ProvidentFundRuleDto>(sql, new { ruleId, organizationId });
    }

    public async Task<ProvidentFundRuleDto> CreateProvidentFundRuleAsync(Guid organizationId, CreateProvidentFundRuleDto dto)
    {
        var sql = @"
            INSERT INTO payroll.pfrules (
                organizationid, rulename, description, defaultemployeepct, defaultemployerpct,
                contributionbasis, withdrawal_config, allowpartialwithdraw, vestingmonths, isactive, isdeleted
            )
            VALUES (
                @organizationId, @RuleName, @Description, @DefaultEmployeePct, @DefaultEmployerPct,
                @ContributionBasis, @WithdrawalConfig::jsonb, @AllowPartialWithdraw, @VestingMonths, @IsActive, false
            )
            RETURNING ruleid;";

        var parameters = new
        {
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.DefaultEmployeePct,
            dto.DefaultEmployerPct,
            dto.ContributionBasis,
            dto.WithdrawalConfig,
            dto.AllowPartialWithdraw,
            dto.VestingMonths,
            dto.IsActive
        };

        var ruleId = await QuerySingleAsync<Guid>(sql, parameters);
        return (await GetProvidentFundRuleByIdAsync(ruleId, organizationId))!;
    }

    public async Task<ProvidentFundRuleDto?> UpdateProvidentFundRuleAsync(Guid ruleId, Guid organizationId, UpdateProvidentFundRuleDto dto)
    {
        var sql = @"
            UPDATE payroll.pfrules
            SET
                rulename = @RuleName,
                description = @Description,
                defaultemployeepct = @DefaultEmployeePct,
                defaultemployerpct = @DefaultEmployerPct,
                contributionbasis = @ContributionBasis,
                withdrawal_config = @WithdrawalConfig::jsonb,
                allowpartialwithdraw = @AllowPartialWithdraw,
                vestingmonths = @VestingMonths,
                isactive = @IsActive,
                updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING ruleid;";

        var parameters = new
        {
            ruleId,
            organizationId,
            dto.RuleName,
            dto.Description,
            dto.DefaultEmployeePct,
            dto.DefaultEmployerPct,
            dto.ContributionBasis,
            dto.WithdrawalConfig,
            dto.AllowPartialWithdraw,
            dto.VestingMonths,
            dto.IsActive
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetProvidentFundRuleByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteProvidentFundRuleAsync(Guid ruleId, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.pfrules
            SET isdeleted = true, updatedat = current_timestamp
            WHERE ruleid = @ruleId AND organizationid = @organizationId
            RETURNING ruleid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { ruleId, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Tax Regimes
    public async Task<IEnumerable<TaxRegimeDto>> GetAllTaxRegimesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                regimeid AS RegimeId,
                organizationid AS OrganizationId,
                country AS Country,
                regimename AS RegimeName,
                startdate AS StartDate,
                enddate AS EndDate,
                isactive AS IsActive,
                createdat AS CreatedAt
            FROM payroll.taxregimes
            WHERE organizationid = @organizationId AND isdeleted = false
            ORDER BY createdat DESC;";

        return await QueryAsync<TaxRegimeDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<TaxRegimeDto>> GetActiveTaxRegimesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                regimeid AS RegimeId,
                organizationid AS OrganizationId,
                country AS Country,
                regimename AS RegimeName,
                startdate AS StartDate,
                enddate AS EndDate,
                isactive AS IsActive,
                createdat AS CreatedAt
            FROM payroll.taxregimes
            WHERE organizationid = @organizationId AND isactive = true AND isdeleted = false
            ORDER BY createdat DESC;";

        return await QueryAsync<TaxRegimeDto>(sql, new { organizationId });
    }

    public async Task<TaxRegimeDto?> GetTaxRegimeByIdAsync(Guid regimeId, Guid organizationId)
    {
        var sql = @"
            SELECT
                regimeid AS RegimeId,
                organizationid AS OrganizationId,
                country AS Country,
                regimename AS RegimeName,
                startdate AS StartDate,
                enddate AS EndDate,
                isactive AS IsActive,
                createdat AS CreatedAt
            FROM payroll.taxregimes
            WHERE regimeid = @regimeId AND organizationid = @organizationId AND isdeleted = false;";

        return await QuerySingleOrDefaultAsync<TaxRegimeDto>(sql, new { regimeId, organizationId });
    }

    public async Task<TaxRegimeDto> CreateTaxRegimeAsync(Guid organizationId, CreateTaxRegimeDto dto)
    {
        var sql = @"
            INSERT INTO payroll.taxregimes (
                organizationid, country, regimename, startdate, enddate, isactive
            )
            VALUES (
                @organizationId, @Country, @RegimeName, @StartDate, @EndDate, @IsActive
            )
            RETURNING regimeid;";

        var parameters = new
        {
            organizationId,
            dto.Country,
            dto.RegimeName,
            dto.StartDate,
            dto.EndDate,
            dto.IsActive
        };

        var regimeId = await QuerySingleAsync<Guid>(sql, parameters);
        return (await GetTaxRegimeByIdAsync(regimeId, organizationId))!;
    }

    public async Task<TaxRegimeDto?> UpdateTaxRegimeAsync(Guid regimeId, Guid organizationId, UpdateTaxRegimeDto dto)
    {
        var sql = @"
            UPDATE payroll.taxregimes
            SET
                country = @Country,
                regimename = @RegimeName,
                startdate = @StartDate,
                enddate = @EndDate,
                isactive = @IsActive
            WHERE regimeid = @regimeId AND organizationid = @organizationId AND isdeleted = false
            RETURNING regimeid;";

        var parameters = new
        {
            regimeId,
            organizationId,
            dto.Country,
            dto.RegimeName,
            dto.StartDate,
            dto.EndDate,
            dto.IsActive
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetTaxRegimeByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteTaxRegimeAsync(Guid regimeId, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.taxregimes
            SET isdeleted = true
            WHERE regimeid = @regimeId AND organizationid = @organizationId AND isdeleted = false
            RETURNING regimeid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { regimeId, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Tax Categories
    public async Task<IEnumerable<TaxCategoryDto>> GetAllTaxCategoriesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                tc.categoryid AS CategoryId,
                tc.regimeid AS RegimeId,
                tr.regimename AS RegimeName,
                tc.categoryname AS CategoryName,
                tc.isactive AS IsActive,
                tc.isdeleted AS IsDeleted,
                tc.createdat AS CreatedAt
            FROM payroll.taxcategories tc
            INNER JOIN payroll.taxregimes tr ON tr.regimeid = tc.regimeid
            WHERE tr.organizationid = @organizationId
              AND tr.isdeleted = false
              AND tc.isdeleted = false
            ORDER BY tc.createdat DESC;";

        return await QueryAsync<TaxCategoryDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<TaxCategoryDto>> GetActiveTaxCategoriesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                tc.categoryid AS CategoryId,
                tc.regimeid AS RegimeId,
                tr.regimename AS RegimeName,
                tc.categoryname AS CategoryName,
                tc.isactive AS IsActive,
                tc.isdeleted AS IsDeleted,
                tc.createdat AS CreatedAt
            FROM payroll.taxcategories tc
            INNER JOIN payroll.taxregimes tr ON tr.regimeid = tc.regimeid
            WHERE tr.organizationid = @organizationId
              AND tr.isdeleted = false
              AND tc.isactive = true
              AND tc.isdeleted = false
            ORDER BY tc.createdat DESC;";

        return await QueryAsync<TaxCategoryDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<TaxCategoryDto>> GetTaxCategoriesByRegimeIdAsync(Guid regimeId, Guid organizationId)
    {
        var sql = @"
            SELECT
                tc.categoryid AS CategoryId,
                tc.regimeid AS RegimeId,
                tr.regimename AS RegimeName,
                tc.categoryname AS CategoryName,
                tc.isactive AS IsActive,
                tc.isdeleted AS IsDeleted,
                tc.createdat AS CreatedAt
            FROM payroll.taxcategories tc
            INNER JOIN payroll.taxregimes tr ON tr.regimeid = tc.regimeid
            WHERE tc.regimeid = @regimeId
              AND tr.organizationid = @organizationId
              AND tr.isdeleted = false
              AND tc.isdeleted = false
            ORDER BY tc.createdat DESC;";

        return await QueryAsync<TaxCategoryDto>(sql, new { regimeId, organizationId });
    }

    public async Task<TaxCategoryDto?> GetTaxCategoryByIdAsync(Guid categoryId, Guid organizationId)
    {
        var sql = @"
            SELECT
                tc.categoryid AS CategoryId,
                tc.regimeid AS RegimeId,
                tr.regimename AS RegimeName,
                tc.categoryname AS CategoryName,
                tc.isactive AS IsActive,
                tc.isdeleted AS IsDeleted,
                tc.createdat AS CreatedAt
            FROM payroll.taxcategories tc
            INNER JOIN payroll.taxregimes tr ON tr.regimeid = tc.regimeid
            WHERE tc.categoryid = @categoryId
              AND tr.organizationid = @organizationId
              AND tr.isdeleted = false
              AND tc.isdeleted = false;";

        return await QuerySingleOrDefaultAsync<TaxCategoryDto>(sql, new { categoryId, organizationId });
    }

    public async Task<TaxCategoryDto> CreateTaxCategoryAsync(Guid organizationId, CreateTaxCategoryDto dto)
    {
        var sql = @"
            INSERT INTO payroll.taxcategories (
                regimeid, categoryname, isactive, isdeleted
            )
            SELECT
                @RegimeId, @CategoryName, @IsActive, false
            FROM payroll.taxregimes tr
            WHERE tr.regimeid = @RegimeId
              AND tr.organizationid = @organizationId
              AND tr.isdeleted = false
            RETURNING categoryid;";

        var parameters = new
        {
            organizationId,
            dto.RegimeId,
            dto.CategoryName,
            dto.IsActive
        };

        var categoryId = await QuerySingleAsync<Guid>(sql, parameters);
        return (await GetTaxCategoryByIdAsync(categoryId, organizationId))!;
    }

    public async Task<TaxCategoryDto?> UpdateTaxCategoryAsync(Guid categoryId, Guid organizationId, UpdateTaxCategoryDto dto)
    {
        var sql = @"
            UPDATE payroll.taxcategories tc
            SET
                regimeid = @RegimeId,
                categoryname = @CategoryName,
                isactive = @IsActive
            WHERE tc.categoryid = @categoryId
              AND tc.isdeleted = false
              AND EXISTS (
                  SELECT 1
                  FROM payroll.taxregimes tr
                  WHERE tr.regimeid = tc.regimeid
                    AND tr.organizationid = @organizationId
                    AND tr.isdeleted = false
              )
              AND EXISTS (
                  SELECT 1
                  FROM payroll.taxregimes tr2
                  WHERE tr2.regimeid = @RegimeId
                    AND tr2.organizationid = @organizationId
                    AND tr2.isdeleted = false
              )
            RETURNING categoryid;";

        var parameters = new
        {
            categoryId,
            organizationId,
            dto.RegimeId,
            dto.CategoryName,
            dto.IsActive
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetTaxCategoryByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteTaxCategoryAsync(Guid categoryId, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.taxcategories tc
            SET isdeleted = true
            WHERE tc.categoryid = @categoryId
              AND tc.isdeleted = false
              AND EXISTS (
                  SELECT 1
                  FROM payroll.taxregimes tr
                  WHERE tr.regimeid = tc.regimeid
                    AND tr.organizationid = @organizationId
                    AND tr.isdeleted = false
              )
            RETURNING tc.categoryid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { categoryId, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Tax Slabs
    public async Task<IEnumerable<TaxSlabDto>> GetAllTaxSlabsAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ts.slabid AS SlabId,
                ts.categoryid AS CategoryId,
                tc.categoryname AS CategoryName,
                ts.slaborder AS SlabOrder,
                ts.minincome AS MinIncome,
                ts.maxincome AS MaxIncome,
                ts.fixedamount AS FixedAmount,
                ts.percentage AS Percentage,
                ts.isdeleted AS IsDeleted,
                ts.createdat AS CreatedAt
            FROM payroll.taxslabs ts
            INNER JOIN payroll.taxcategories tc ON tc.categoryid = ts.categoryid
            INNER JOIN payroll.taxregimes tr ON tr.regimeid = tc.regimeid
            WHERE tr.organizationid = @organizationId
              AND tr.isdeleted = false
              AND tc.isdeleted = false
              AND ts.isdeleted = false
            ORDER BY ts.createdat DESC, ts.slaborder ASC;";

        return await QueryAsync<TaxSlabDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<TaxSlabDto>> GetActiveTaxSlabsAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                ts.slabid AS SlabId,
                ts.categoryid AS CategoryId,
                tc.categoryname AS CategoryName,
                ts.slaborder AS SlabOrder,
                ts.minincome AS MinIncome,
                ts.maxincome AS MaxIncome,
                ts.fixedamount AS FixedAmount,
                ts.percentage AS Percentage,
                ts.isdeleted AS IsDeleted,
                ts.createdat AS CreatedAt
            FROM payroll.taxslabs ts
            INNER JOIN payroll.taxcategories tc ON tc.categoryid = ts.categoryid
            INNER JOIN payroll.taxregimes tr ON tr.regimeid = tc.regimeid
            WHERE tr.organizationid = @organizationId
              AND tr.isdeleted = false
              AND tr.isactive = true
              AND tc.isdeleted = false
              AND tc.isactive = true
              AND ts.isdeleted = false
            ORDER BY ts.createdat DESC, ts.slaborder ASC;";

        return await QueryAsync<TaxSlabDto>(sql, new { organizationId });
    }

    public async Task<TaxSlabDto?> GetTaxSlabByIdAsync(Guid slabId, Guid organizationId)
    {
        var sql = @"
            SELECT
                ts.slabid AS SlabId,
                ts.categoryid AS CategoryId,
                tc.categoryname AS CategoryName,
                ts.slaborder AS SlabOrder,
                ts.minincome AS MinIncome,
                ts.maxincome AS MaxIncome,
                ts.fixedamount AS FixedAmount,
                ts.percentage AS Percentage,
                ts.isdeleted AS IsDeleted,
                ts.createdat AS CreatedAt
            FROM payroll.taxslabs ts
            INNER JOIN payroll.taxcategories tc ON tc.categoryid = ts.categoryid
            INNER JOIN payroll.taxregimes tr ON tr.regimeid = tc.regimeid
            WHERE ts.slabid = @slabId
              AND tr.organizationid = @organizationId
              AND tr.isdeleted = false
              AND tc.isdeleted = false
              AND ts.isdeleted = false;";

        return await QuerySingleOrDefaultAsync<TaxSlabDto>(sql, new { slabId, organizationId });
    }

    public async Task<TaxSlabDto> CreateTaxSlabAsync(Guid organizationId, CreateTaxSlabDto dto)
    {
        var sql = @"
            INSERT INTO payroll.taxslabs (
                categoryid, slaborder, minincome, maxincome, fixedamount, percentage, isdeleted
            )
            SELECT
                @CategoryId, @SlabOrder, @MinIncome, @MaxIncome, @FixedAmount, @Percentage, false
            FROM payroll.taxcategories tc
            INNER JOIN payroll.taxregimes tr ON tr.regimeid = tc.regimeid
            WHERE tc.categoryid = @CategoryId
              AND tc.isdeleted = false
              AND tr.organizationid = @organizationId
              AND tr.isdeleted = false
            RETURNING slabid;";

        var parameters = new
        {
            organizationId,
            dto.CategoryId,
            dto.SlabOrder,
            dto.MinIncome,
            dto.MaxIncome,
            dto.FixedAmount,
            dto.Percentage
        };

        var slabId = await QuerySingleAsync<Guid>(sql, parameters);
        return (await GetTaxSlabByIdAsync(slabId, organizationId))!;
    }

    public async Task<TaxSlabDto?> UpdateTaxSlabAsync(Guid slabId, Guid organizationId, UpdateTaxSlabDto dto)
    {
        var sql = @"
            UPDATE payroll.taxslabs ts
            SET
                categoryid = @CategoryId,
                slaborder = @SlabOrder,
                minincome = @MinIncome,
                maxincome = @MaxIncome,
                fixedamount = @FixedAmount,
                percentage = @Percentage
            WHERE ts.slabid = @slabId
              AND ts.isdeleted = false
              AND EXISTS (
                  SELECT 1
                  FROM payroll.taxcategories tc
                  INNER JOIN payroll.taxregimes tr ON tr.regimeid = tc.regimeid
                  WHERE tc.categoryid = ts.categoryid
                    AND tc.isdeleted = false
                    AND tr.organizationid = @organizationId
                    AND tr.isdeleted = false
              )
              AND EXISTS (
                  SELECT 1
                  FROM payroll.taxcategories tc2
                  INNER JOIN payroll.taxregimes tr2 ON tr2.regimeid = tc2.regimeid
                  WHERE tc2.categoryid = @CategoryId
                    AND tc2.isdeleted = false
                    AND tr2.organizationid = @organizationId
                    AND tr2.isdeleted = false
              )
            RETURNING slabid;";

        var parameters = new
        {
            slabId,
            organizationId,
            dto.CategoryId,
            dto.SlabOrder,
            dto.MinIncome,
            dto.MaxIncome,
            dto.FixedAmount,
            dto.Percentage
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetTaxSlabByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteTaxSlabAsync(Guid slabId, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.taxslabs ts
            SET isdeleted = true
            WHERE ts.slabid = @slabId
              AND ts.isdeleted = false
              AND EXISTS (
                  SELECT 1
                  FROM payroll.taxcategories tc
                  INNER JOIN payroll.taxregimes tr ON tr.regimeid = tc.regimeid
                  WHERE tc.categoryid = ts.categoryid
                    AND tc.isdeleted = false
                    AND tr.organizationid = @organizationId
                    AND tr.isdeleted = false
              )
            RETURNING ts.slabid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { slabId, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Tax Component Rules
    public async Task<IEnumerable<TaxComponentRuleDto>> GetAllTaxComponentRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                tcr.componentruleid AS ComponentRuleId,
                tcr.regimeid AS RegimeId,
                tr.regimename AS RegimeName,
                tcr.organizationid AS OrganizationId,
                tcr.componentname AS ComponentName,
                tcr.taxability AS Taxability,
                tcr.limitamount AS LimitAmount,
                tcr.limitpercentage AS LimitPercentage,
                tcr.applystage AS ApplyStage,
                tcr.isdeleted AS IsDeleted,
                tcr.createdat AS CreatedAt
            FROM payroll.taxcomponentrules tcr
            INNER JOIN payroll.taxregimes tr ON tr.regimeid = tcr.regimeid
            WHERE tcr.organizationid = @organizationId
              AND tr.organizationid = @organizationId
              AND tr.isdeleted = false
              AND tcr.isdeleted = false
            ORDER BY tcr.createdat DESC;";

        return await QueryAsync<TaxComponentRuleDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<TaxComponentRuleDto>> GetActiveTaxComponentRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                tcr.componentruleid AS ComponentRuleId,
                tcr.regimeid AS RegimeId,
                tr.regimename AS RegimeName,
                tcr.organizationid AS OrganizationId,
                tcr.componentname AS ComponentName,
                tcr.taxability AS Taxability,
                tcr.limitamount AS LimitAmount,
                tcr.limitpercentage AS LimitPercentage,
                tcr.applystage AS ApplyStage,
                tcr.isdeleted AS IsDeleted,
                tcr.createdat AS CreatedAt
            FROM payroll.taxcomponentrules tcr
            INNER JOIN payroll.taxregimes tr ON tr.regimeid = tcr.regimeid
            WHERE tcr.organizationid = @organizationId
              AND tr.organizationid = @organizationId
              AND tr.isdeleted = false
              AND tr.isactive = true
              AND tcr.isdeleted = false
            ORDER BY tcr.createdat DESC;";

        return await QueryAsync<TaxComponentRuleDto>(sql, new { organizationId });
    }

    public async Task<TaxComponentRuleDto?> GetTaxComponentRuleByIdAsync(Guid componentRuleId, Guid organizationId)
    {
        var sql = @"
            SELECT
                tcr.componentruleid AS ComponentRuleId,
                tcr.regimeid AS RegimeId,
                tr.regimename AS RegimeName,
                tcr.organizationid AS OrganizationId,
                tcr.componentname AS ComponentName,
                tcr.taxability AS Taxability,
                tcr.limitamount AS LimitAmount,
                tcr.limitpercentage AS LimitPercentage,
                tcr.applystage AS ApplyStage,
                tcr.isdeleted AS IsDeleted,
                tcr.createdat AS CreatedAt
            FROM payroll.taxcomponentrules tcr
            INNER JOIN payroll.taxregimes tr ON tr.regimeid = tcr.regimeid
            WHERE tcr.componentruleid = @componentRuleId
              AND tcr.organizationid = @organizationId
              AND tr.organizationid = @organizationId
              AND tr.isdeleted = false
              AND tcr.isdeleted = false;";

        return await QuerySingleOrDefaultAsync<TaxComponentRuleDto>(sql, new { componentRuleId, organizationId });
    }

    public async Task<TaxComponentRuleDto> CreateTaxComponentRuleAsync(Guid organizationId, CreateTaxComponentRuleDto dto)
    {
        var sql = @"
            INSERT INTO payroll.taxcomponentrules (
                regimeid, organizationid, componentname, taxability, limitamount, limitpercentage, applystage, isdeleted
            )
            SELECT
                @RegimeId, @organizationId, @ComponentName, @Taxability, @LimitAmount, @LimitPercentage, @ApplyStage, false
            FROM payroll.taxregimes tr
            WHERE tr.regimeid = @RegimeId
              AND tr.organizationid = @organizationId
              AND tr.isdeleted = false
            RETURNING componentruleid;";

        var parameters = new
        {
            organizationId,
            dto.RegimeId,
            dto.ComponentName,
            dto.Taxability,
            dto.LimitAmount,
            dto.LimitPercentage,
            dto.ApplyStage
        };

        var componentRuleId = await QuerySingleAsync<Guid>(sql, parameters);
        return (await GetTaxComponentRuleByIdAsync(componentRuleId, organizationId))!;
    }

    public async Task<TaxComponentRuleDto?> UpdateTaxComponentRuleAsync(Guid componentRuleId, Guid organizationId, UpdateTaxComponentRuleDto dto)
    {
        var sql = @"
            UPDATE payroll.taxcomponentrules tcr
            SET
                regimeid = @RegimeId,
                componentname = @ComponentName,
                taxability = @Taxability,
                limitamount = @LimitAmount,
                limitpercentage = @LimitPercentage,
                applystage = @ApplyStage
            WHERE tcr.componentruleid = @componentRuleId
              AND tcr.organizationid = @organizationId
              AND tcr.isdeleted = false
              AND EXISTS (
                  SELECT 1
                  FROM payroll.taxregimes tr
                  WHERE tr.regimeid = @RegimeId
                    AND tr.organizationid = @organizationId
                    AND tr.isdeleted = false
              )
            RETURNING componentruleid;";

        var parameters = new
        {
            componentRuleId,
            organizationId,
            dto.RegimeId,
            dto.ComponentName,
            dto.Taxability,
            dto.LimitAmount,
            dto.LimitPercentage,
            dto.ApplyStage
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetTaxComponentRuleByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteTaxComponentRuleAsync(Guid componentRuleId, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.taxcomponentrules
            SET isdeleted = true
            WHERE componentruleid = @componentRuleId
              AND organizationid = @organizationId
              AND isdeleted = false
            RETURNING componentruleid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { componentRuleId, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // Tax Transactions
    public async Task<IEnumerable<TaxTransactionDto>> GetAllTaxTransactionsAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                tt.id AS Id,
                tt.organizationid AS OrganizationId,
                tt.employeeid AS EmployeeId,
                CONCAT(COALESCE(e.firstname, ''), ' ', COALESCE(e.lastname, '')) AS EmployeeName,
                tt.periodid AS PeriodId,
                pp.periodname AS PeriodName,
                tt.categoryid AS CategoryId,
                tc.categoryname AS CategoryName,
                tt.grossincome AS GrossIncome,
                tt.taxableincome AS TaxableIncome,
                tt.taxamount AS TaxAmount,
                tt.monthlytax AS MonthlyTax,
                tt.createdat AS CreatedAt
            FROM payroll.taxtransactions tt
            INNER JOIN employee.employees e ON e.employeeid = tt.employeeid AND e.organizationid = tt.organizationid
            INNER JOIN payroll.payrollperiods pp ON pp.periodid = tt.periodid AND pp.organizationid = tt.organizationid
            LEFT JOIN payroll.taxcategories tc ON tc.categoryid = tt.categoryid
            WHERE tt.organizationid = @organizationId
            ORDER BY tt.createdat DESC;";

        return await QueryAsync<TaxTransactionDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<TaxTransactionDto>> GetActiveTaxTransactionsAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                tt.id AS Id,
                tt.organizationid AS OrganizationId,
                tt.employeeid AS EmployeeId,
                CONCAT(COALESCE(e.firstname, ''), ' ', COALESCE(e.lastname, '')) AS EmployeeName,
                tt.periodid AS PeriodId,
                pp.periodname AS PeriodName,
                tt.categoryid AS CategoryId,
                tc.categoryname AS CategoryName,
                tt.grossincome AS GrossIncome,
                tt.taxableincome AS TaxableIncome,
                tt.taxamount AS TaxAmount,
                tt.monthlytax AS MonthlyTax,
                tt.createdat AS CreatedAt
            FROM payroll.taxtransactions tt
            INNER JOIN employee.employees e ON e.employeeid = tt.employeeid AND e.organizationid = tt.organizationid
            INNER JOIN payroll.payrollperiods pp ON pp.periodid = tt.periodid AND pp.organizationid = tt.organizationid
            LEFT JOIN payroll.taxcategories tc ON tc.categoryid = tt.categoryid
            WHERE tt.organizationid = @organizationId
              AND (LOWER(COALESCE(e.status, 'active')) = 'active')
              AND pp.isactive = true
              AND pp.isdeleted = false
              AND (tt.categoryid IS NULL OR (tc.isactive = true AND tc.isdeleted = false))
            ORDER BY tt.createdat DESC;";

        return await QueryAsync<TaxTransactionDto>(sql, new { organizationId });
    }

    public async Task<TaxTransactionDto?> GetTaxTransactionByIdAsync(Guid id, Guid organizationId)
    {
        var sql = @"
            SELECT
                tt.id AS Id,
                tt.organizationid AS OrganizationId,
                tt.employeeid AS EmployeeId,
                CONCAT(COALESCE(e.firstname, ''), ' ', COALESCE(e.lastname, '')) AS EmployeeName,
                tt.periodid AS PeriodId,
                pp.periodname AS PeriodName,
                tt.categoryid AS CategoryId,
                tc.categoryname AS CategoryName,
                tt.grossincome AS GrossIncome,
                tt.taxableincome AS TaxableIncome,
                tt.taxamount AS TaxAmount,
                tt.monthlytax AS MonthlyTax,
                tt.createdat AS CreatedAt
            FROM payroll.taxtransactions tt
            INNER JOIN employee.employees e ON e.employeeid = tt.employeeid AND e.organizationid = tt.organizationid
            INNER JOIN payroll.payrollperiods pp ON pp.periodid = tt.periodid AND pp.organizationid = tt.organizationid
            LEFT JOIN payroll.taxcategories tc ON tc.categoryid = tt.categoryid
            WHERE tt.id = @id
              AND tt.organizationid = @organizationId;";

        return await QuerySingleOrDefaultAsync<TaxTransactionDto>(sql, new { id, organizationId });
    }

    public async Task<TaxTransactionDto> CreateTaxTransactionAsync(Guid organizationId, CreateTaxTransactionDto dto)
    {
        var sql = @"
            INSERT INTO payroll.taxtransactions (
                organizationid, employeeid, periodid, categoryid, grossincome, taxableincome, taxamount, monthlytax
            )
            SELECT
                @organizationId, @EmployeeId, @PeriodId, @CategoryId, @GrossIncome, @TaxableIncome, @TaxAmount, @MonthlyTax
            WHERE EXISTS (
                SELECT 1
                FROM employee.employees e
                WHERE e.employeeid = @EmployeeId
                  AND e.organizationid = @organizationId
            )
            AND EXISTS (
                SELECT 1
                FROM payroll.payrollperiods pp
                WHERE pp.periodid = @PeriodId
                  AND pp.organizationid = @organizationId
            )
            AND (
                @CategoryId IS NULL
                OR EXISTS (
                    SELECT 1
                    FROM payroll.taxcategories tc
                    INNER JOIN payroll.taxregimes tr ON tr.regimeid = tc.regimeid
                    WHERE tc.categoryid = @CategoryId
                      AND tr.organizationid = @organizationId
                      AND tc.isdeleted = false
                      AND tr.isdeleted = false
                )
            )
            RETURNING id;";

        var parameters = new
        {
            organizationId,
            dto.EmployeeId,
            dto.PeriodId,
            dto.CategoryId,
            dto.GrossIncome,
            dto.TaxableIncome,
            dto.TaxAmount,
            dto.MonthlyTax
        };

        var id = await QuerySingleAsync<Guid>(sql, parameters);
        return (await GetTaxTransactionByIdAsync(id, organizationId))!;
    }

    public async Task<TaxTransactionDto?> UpdateTaxTransactionAsync(Guid id, Guid organizationId, UpdateTaxTransactionDto dto)
    {
        var sql = @"
            UPDATE payroll.taxtransactions tt
            SET
                employeeid = @EmployeeId,
                periodid = @PeriodId,
                categoryid = @CategoryId,
                grossincome = @GrossIncome,
                taxableincome = @TaxableIncome,
                taxamount = @TaxAmount,
                monthlytax = @MonthlyTax
            WHERE tt.id = @id
              AND tt.organizationid = @organizationId
              AND EXISTS (
                  SELECT 1
                  FROM employee.employees e
                  WHERE e.employeeid = @EmployeeId
                    AND e.organizationid = @organizationId
              )
              AND EXISTS (
                  SELECT 1
                  FROM payroll.payrollperiods pp
                  WHERE pp.periodid = @PeriodId
                    AND pp.organizationid = @organizationId
              )
              AND (
                  @CategoryId IS NULL
                  OR EXISTS (
                      SELECT 1
                      FROM payroll.taxcategories tc
                      INNER JOIN payroll.taxregimes tr ON tr.regimeid = tc.regimeid
                      WHERE tc.categoryid = @CategoryId
                        AND tr.organizationid = @organizationId
                        AND tc.isdeleted = false
                        AND tr.isdeleted = false
                  )
              )
            RETURNING tt.id;";

        var parameters = new
        {
            id,
            organizationId,
            dto.EmployeeId,
            dto.PeriodId,
            dto.CategoryId,
            dto.GrossIncome,
            dto.TaxableIncome,
            dto.TaxAmount,
            dto.MonthlyTax
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty)
            return null;

        return await GetTaxTransactionByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteTaxTransactionAsync(Guid id, Guid organizationId)
    {
        var sql = @"
            DELETE FROM payroll.taxtransactions
            WHERE id = @id AND organizationid = @organizationId
            RETURNING id;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { id, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    public async Task<ProvidentFundRequestDto> CreateProvidentFundEnrollmentAsync(Guid organizationId, Guid employeeId, CreateProvidentFundEnrollmentDto dto, decimal employerPct)
    {
        var sql = @"
            INSERT INTO payroll.pfrequests (
                organizationid, employeeid, ruleid, pfid,
                requesttype, status,
                requestedemployeepct, requestedemployerpct
            )
            VALUES (
                @organizationId, @employeeId, @RuleId, NULL,
                'enrollment', 'pending',
                @EmployeePct, @EmployerPct
            )
            RETURNING
                requestid AS RequestId,
                organizationid AS OrganizationId,
                employeeid AS EmployeeId,
                ruleid AS RuleId,
                pfid AS PfId,
                requesttype AS RequestType,
                status AS Status,
                requestedemployeepct AS RequestedEmployeePct,
                requestedemployerpct AS RequestedEmployerPct,
                withdrawaltype AS WithdrawalType,
                reason AS Reason,
                requestedamount AS RequestedAmount,
                remarks AS Remarks,
                rejectionreason AS RejectionReason,
                approvedby AS ApprovedBy,
                approvedat AS ApprovedAt,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt;";

        return await QuerySingleAsync<ProvidentFundRequestDto>(sql, new
        {
            organizationId,
            employeeId,
            dto.RuleId,
            dto.EmployeePct,
            EmployerPct = employerPct
        });
    }

    public async Task<ProvidentFundRequestDto> UpdateProvidentFundEnrollmentRequestAsync(Guid requestId, Guid organizationId, Guid employeeId, CreateProvidentFundEnrollmentDto dto, decimal employerPct)
    {
        var sql = @"
            UPDATE payroll.pfrequests
            SET
                ruleid = @RuleId,
                requestedemployeepct = @EmployeePct,
                requestedemployerpct = @EmployerPct,
                updatedat = current_timestamp
            WHERE requestid = @requestId
              AND organizationid = @organizationId
              AND employeeid = @employeeId
              AND status = 'pending'
              AND requesttype = 'enrollment'
            RETURNING
                requestid AS RequestId,
                organizationid AS OrganizationId,
                employeeid AS EmployeeId,
                ruleid AS RuleId,
                pfid AS PfId,
                requesttype AS RequestType,
                status AS Status,
                requestedemployeepct AS RequestedEmployeePct,
                requestedemployerpct AS RequestedEmployerPct,
                withdrawaltype AS WithdrawalType,
                reason AS Reason,
                requestedamount AS RequestedAmount,
                remarks AS Remarks,
                rejectionreason AS RejectionReason,
                approvedby AS ApprovedBy,
                approvedat AS ApprovedAt,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt;";

        return await QuerySingleAsync<ProvidentFundRequestDto>(sql, new
        {
            requestId,
            organizationId,
            employeeId,
            dto.RuleId,
            dto.EmployeePct,
            EmployerPct = employerPct
        });
    }

    public async Task<ProvidentFundRequestDto> UpdateProvidentFundPercentageAsync(Guid organizationId, Guid employeeId, UpdateProvidentFundPercentageDto dto)
    {
        var sql = @"
            INSERT INTO payroll.pfrequests (
                organizationid, employeeid, ruleid, pfid,
                requesttype, status,
                requestedemployeepct, requestedemployerpct
            )
            SELECT
                pf.organizationid,
                pf.employeeid,
                @RuleId,
                pf.pfid,
                'update',
                'pending',
                @EmployeePct,
                pr.defaultemployerpct
            FROM payroll.providentfund pf
            JOIN payroll.pfrules pr
              ON pr.organizationid = pf.organizationid
             AND pr.ruleid = @RuleId
             AND COALESCE(pr.isdeleted, false) = false
             AND COALESCE(pr.isactive, true) = true
            WHERE pf.organizationid = @organizationId
              AND pf.employeeid = @employeeId
              AND pf.isactive = true
            LIMIT 1
            RETURNING
                requestid AS RequestId,
                organizationid AS OrganizationId,
                employeeid AS EmployeeId,
                ruleid AS RuleId,
                pfid AS PfId,
                requesttype AS RequestType,
                status AS Status,
                requestedemployeepct AS RequestedEmployeePct,
                requestedemployerpct AS RequestedEmployerPct,
                withdrawaltype AS WithdrawalType,
                reason AS Reason,
                requestedamount AS RequestedAmount,
                remarks AS Remarks,
                rejectionreason AS RejectionReason,
                approvedby AS ApprovedBy,
                approvedat AS ApprovedAt,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt;";

        return await QuerySingleAsync<ProvidentFundRequestDto>(sql, new
        {
            organizationId,
            employeeId,
            dto.RuleId,
            dto.EmployeePct
        });
    }

    public async Task<ProvidentFundRequestDto> CreateProvidentFundWithdrawRequestAsync(Guid organizationId, Guid employeeId, CreateProvidentFundWithdrawRequestDto dto)
    {
        var sql = @"
            INSERT INTO payroll.pfrequests (
                organizationid, employeeid, ruleid, pfid,
                requesttype, status,
                withdrawaltype, reason, requestedamount
            )
            SELECT
                pf.organizationid,
                pf.employeeid,
                pf.ruleid,
                pf.pfid,
                'withdrawal',
                'pending',
                @WithdrawalType,
                @Reason,
                @Amount
            FROM payroll.providentfund pf
            WHERE pf.organizationid = @organizationId
              AND pf.employeeid = @employeeId
              AND pf.isactive = true
            LIMIT 1
            RETURNING
                requestid AS RequestId,
                organizationid AS OrganizationId,
                employeeid AS EmployeeId,
                ruleid AS RuleId,
                pfid AS PfId,
                requesttype AS RequestType,
                status AS Status,
                requestedemployeepct AS RequestedEmployeePct,
                requestedemployerpct AS RequestedEmployerPct,
                withdrawaltype AS WithdrawalType,
                reason AS Reason,
                requestedamount AS RequestedAmount,
                remarks AS Remarks,
                rejectionreason AS RejectionReason,
                approvedby AS ApprovedBy,
                approvedat AS ApprovedAt,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt;";

        return await QuerySingleAsync<ProvidentFundRequestDto>(sql, new
        {
            organizationId,
            employeeId,
            WithdrawalType = dto.WithdrawalType,
            dto.Reason,
            dto.Amount
        });
    }

    public async Task<bool> DeleteProvidentFundRequestAsync(Guid requestId, Guid organizationId, Guid employeeId)
    {
        var sql = @"
            DELETE FROM payroll.pfrequests
            WHERE requestid = @requestId
              AND organizationid = @organizationId
              AND employeeid = @employeeId
              AND status <> 'approved'
            RETURNING requestid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new
        {
            requestId,
            organizationId,
            employeeId
        });

        return deletedId != null && deletedId != Guid.Empty;
    }

    public async Task<PagedResult<ProvidentFundRequestDto>> GetMyProvidentFundRequestsAsync(Guid organizationId, Guid employeeId, ProvidentFundRequestFilterDto filter)
    {
        filter ??= new ProvidentFundRequestFilterDto();
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.PageSize <= 0) filter.PageSize = 10;

        var conditions = new List<string>
        {
            "r.organizationid = @organizationId",
            "r.employeeid = @employeeId"
        };

        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);
        parameters.Add("employeeId", employeeId);

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            conditions.Add("LOWER(r.status) = LOWER(@Status)");
            parameters.Add("Status", filter.Status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.RequestType))
        {
            conditions.Add("LOWER(r.requesttype) = LOWER(@RequestType)");
            parameters.Add("RequestType", filter.RequestType.Trim());
        }

        var whereClause = $"WHERE {string.Join(" AND ", conditions)}";
        var offset = (filter.Page - 1) * filter.PageSize;
        parameters.Add("Limit", filter.PageSize);
        parameters.Add("Offset", offset);

        var dataSql = $@"
            SELECT
                r.requestid AS RequestId,
                r.organizationid AS OrganizationId,
                r.employeeid AS EmployeeId,
                r.ruleid AS RuleId,
                pr.rulename AS RuleName,
                r.pfid AS PfId,
                r.requesttype AS RequestType,
                r.status AS Status,
                r.requestedemployeepct AS RequestedEmployeePct,
                r.requestedemployerpct AS RequestedEmployerPct,
                r.withdrawaltype AS WithdrawalType,
                r.reason AS Reason,
                r.requestedamount AS RequestedAmount,
                r.remarks AS Remarks,
                r.rejectionreason AS RejectionReason,
                r.approvedby AS ApprovedBy,
                r.approvedat AS ApprovedAt,
                r.createdat AS CreatedAt,
                r.updatedat AS UpdatedAt,
                COUNT(*) OVER() AS TotalCount
            FROM payroll.pfrequests r
            LEFT JOIN payroll.pfrules pr
              ON pr.ruleid = r.ruleid
             AND pr.organizationid = r.organizationid
            {whereClause}
            ORDER BY r.createdat DESC
            LIMIT @Limit OFFSET @Offset;";

        var items = (await QueryAsync<ProvidentFundRequestDto>(dataSql, parameters)).ToList();
        var totalCount = items.FirstOrDefault()?.TotalCount ?? 0;

        return new PagedResult<ProvidentFundRequestDto>
        {
            Data = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<ProvidentFundAccountDto?> GetMyActiveProvidentFundAsync(Guid organizationId, Guid employeeId)
    {
        var sql = @"
            SELECT
                pf.pfid AS PfId,
                pf.organizationid AS OrganizationId,
                pf.employeeid AS EmployeeId,
                pf.ruleid AS RuleId,
                pr.rulename AS RuleName,
                pr.contributionbasis AS ContributionBasis,
                pr.allowpartialwithdraw AS AllowPartialWithdraw,
                pr.withdrawal_config::text AS WithdrawalConfig,
                pf.employeepct AS EmployeePct,
                pf.employerpct AS EmployerPct,
                pf.pfstatus AS PfStatus,
                pf.effectivefrom AS EffectiveFrom,
                pf.isactive AS IsActive,
                pf.createdat AS CreatedAt,
                pf.updatedat AS UpdatedAt
            FROM payroll.providentfund pf
            JOIN payroll.pfrules pr
              ON pr.ruleid = pf.ruleid
             AND pr.organizationid = pf.organizationid
            WHERE pf.organizationid = @organizationId
              AND pf.employeeid = @employeeId
              AND pf.isactive = true
            ORDER BY pf.updatedat DESC
            LIMIT 1;";

        return await QuerySingleOrDefaultAsync<ProvidentFundAccountDto>(sql, new { organizationId, employeeId });
    }

    public async Task<PagedResult<ProvidentFundTransactionDto>> GetMyProvidentFundTransactionsAsync(Guid organizationId, Guid employeeId, ProvidentFundTransactionFilterDto filter)
    {
        filter ??= new ProvidentFundTransactionFilterDto();
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.PageSize <= 0) filter.PageSize = 10;

        var conditions = new List<string>
        {
            "pt.organizationid = @organizationId",
            "pt.employeeid = @employeeId"
        };

        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);
        parameters.Add("employeeId", employeeId);

        if (filter.Year.HasValue)
        {
            conditions.Add("EXTRACT(YEAR FROM pt.createdat) = @Year");
            parameters.Add("Year", filter.Year.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.TransactionType))
        {
            conditions.Add("LOWER(pt.transactiontype) = LOWER(@TransactionType)");
            parameters.Add("TransactionType", filter.TransactionType.Trim());
        }

        var whereClause = string.Join(" AND ", conditions);
        var offset = (filter.Page - 1) * filter.PageSize;

        var sql = $@"
            SELECT
                pt.transactionid AS TransactionId,
                pt.organizationid AS OrganizationId,
                pt.employeeid AS EmployeeId,
                pt.periodid AS PeriodId,
                pp.periodname AS PeriodName,
                pt.pfid AS PfId,
                pt.pfrequestid AS PfRequestId,
                pt.salarybasisamount AS SalaryBasisAmount,
                pt.employeepct AS EmployeePct,
                pt.employerpct AS EmployerPct,
                pt.employeeamount AS EmployeeAmount,
                pt.employeramount AS EmployerAmount,
                pt.totalamount AS TotalAmount,
                pt.runningbalance AS RunningBalance,
                pt.transactiontype AS TransactionType,
                pt.createdat AS CreatedAt,
                pt.updatedat AS UpdatedAt,
                COUNT(*) OVER() as TotalCount
            FROM payroll.pftransactions pt
            INNER JOIN payroll.payrollperiods pp
                ON pp.periodid = pt.periodid
                AND pp.organizationid = pt.organizationid
            WHERE {whereClause}
            ORDER BY pt.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", offset);

        var data = await QueryAsync<ProvidentFundTransactionDto>(sql, parameters);
        var totalCount = data.FirstOrDefault()?.TotalCount ?? 0;

        return new PagedResult<ProvidentFundTransactionDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PagedResult<AdminProvidentFundRequestDto>> GetAllProvidentFundRequestsAsync(Guid organizationId, AdminProvidentFundRequestFilterDto filter)
    {
        filter ??= new AdminProvidentFundRequestFilterDto();
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.PageSize <= 0) filter.PageSize = 10;

        var whereClause = "WHERE r.organizationid = @organizationId";

        if (!string.IsNullOrWhiteSpace(filter.RequestType))
            whereClause += " AND LOWER(r.requesttype) = LOWER(@RequestType)";

        if (!string.IsNullOrWhiteSpace(filter.Status))
            whereClause += " AND LOWER(r.status) = LOWER(@Status)";

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            whereClause += " AND (e.firstname || ' ' || e.lastname) ILIKE @SearchTerm";

        var countSql = $@"
            SELECT COUNT(1)
            FROM payroll.pfrequests r
            JOIN employee.Employees e ON e.employeeid = r.employeeid
            {whereClause};";

        var sql = $@"
            SELECT
                r.requestid AS RequestId,
                r.pfid AS PfId,
                r.organizationid AS OrganizationId,
                r.employeeid AS EmployeeId,
                (e.firstname || ' ' || e.lastname) AS EmployeeName,
                ed.basicsalary AS BasicSalary,
                r.ruleid AS RuleId,
                pr.rulename AS RuleName,
                r.requestedemployeepct AS RequestedEmployeePct,
                r.requestedemployerpct AS RequestedEmployerPct,
                r.requesttype AS RequestType,
                r.status AS Status,
                r.withdrawaltype AS WithdrawalType,
                r.reason AS Reason,
                r.requestedamount AS RequestedAmount,
                r.approvedby AS ApprovedBy,
                r.approvedat AS ApprovedAt,
                r.remarks AS Remarks,
                r.rejectionreason AS RejectionReason,
                r.createdat AS CreatedAt,
                r.updatedat AS UpdatedAt
            FROM payroll.pfrequests r
            JOIN employee.Employees e ON e.employeeid = r.employeeid
            LEFT JOIN employee.EmploymentDetails ed ON ed.employeeid = e.employeeid AND ed.iscurrent = true
            LEFT JOIN payroll.pfrules pr ON pr.ruleid = r.ruleid AND pr.organizationid = r.organizationid
            {whereClause}
            ORDER BY r.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        var parameters = new
        {
            organizationId,
            filter.RequestType,
            filter.Status,
            SearchTerm = !string.IsNullOrWhiteSpace(filter.SearchTerm) ? $"%{filter.SearchTerm.Trim()}%" : null,
            PageSize = filter.PageSize,
            Offset = (filter.Page - 1) * filter.PageSize
        };

        var totalCount = await QuerySingleAsync<int>(countSql, parameters);
        var data = await QueryAsync<AdminProvidentFundRequestDto>(sql, parameters);

        return new PagedResult<AdminProvidentFundRequestDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PagedResult<AdminProvidentFundAccountDto>> GetAllProvidentFundAccountsAsync(Guid organizationId, AdminProvidentFundAccountFilterDto filter)
    {
        filter ??= new AdminProvidentFundAccountFilterDto();
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.PageSize <= 0) filter.PageSize = 10;

        var conditions = new List<string>
        {
            "pf.organizationid = @organizationId"
        };

        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);

        if (!string.IsNullOrWhiteSpace(filter.PfStatus))
        {
            conditions.Add("LOWER(pf.pfstatus) = LOWER(@PfStatus)");
            parameters.Add("PfStatus", filter.PfStatus.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            conditions.Add("(e.firstname || ' ' || e.lastname) ILIKE @SearchTerm");
            parameters.Add("SearchTerm", $"%{filter.SearchTerm.Trim()}%");
        }

        var whereClause = string.Join(" AND ", conditions);
        var offset = (filter.Page - 1) * filter.PageSize;

        var sql = $@"
            SELECT
                pf.pfid AS PfId,
                pf.organizationid AS OrganizationId,
                pf.employeeid AS EmployeeId,
                (e.firstname || ' ' || e.lastname) AS EmployeeName,
                ed.basicsalary AS BasicSalary,
                pf.ruleid AS RuleId,
                pr.rulename AS RuleName,
                pf.employeepct AS EmployeePct,
                pf.employerpct AS EmployerPct,
                pf.pfstatus AS PfStatus,
                pf.effectivefrom AS EffectiveFrom,
                pf.isactive AS IsActive,
                pf.createdat AS CreatedAt,
                pf.updatedat AS UpdatedAt,
                COUNT(*) OVER() AS TotalCount
            FROM payroll.providentfund pf
            INNER JOIN employee.employees e
                ON e.employeeid = pf.employeeid
               AND e.organizationid = pf.organizationid
            LEFT JOIN employee.employmentdetails ed
                ON ed.employeeid = e.employeeid
               AND ed.iscurrent = true
            LEFT JOIN payroll.pfrules pr
                ON pr.ruleid = pf.ruleid
               AND pr.organizationid = pf.organizationid
            WHERE {whereClause}
            ORDER BY pf.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", offset);

        var data = await QueryAsync<AdminProvidentFundAccountDto>(sql, parameters);
        var totalCount = data.FirstOrDefault()?.TotalCount ?? 0;

        return new PagedResult<AdminProvidentFundAccountDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PagedResult<AdminProvidentFundRepaymentDto>> GetAllProvidentFundRepaymentsAsync(Guid organizationId, AdminProvidentFundRepaymentFilterDto filter)
    {
        filter ??= new AdminProvidentFundRepaymentFilterDto();
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.PageSize <= 0) filter.PageSize = 10;

        var conditions = new List<string>
        {
            "pt.organizationid = @organizationId"
        };

        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            conditions.Add("(e.firstname || ' ' || e.lastname) ILIKE @SearchTerm");
            parameters.Add("SearchTerm", $"%{filter.SearchTerm.Trim()}%");
        }

        if (filter.PeriodId.HasValue && filter.PeriodId.Value != Guid.Empty)
        {
            conditions.Add("pt.periodid = @PeriodId");
            parameters.Add("PeriodId", filter.PeriodId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.TransactionType))
        {
            conditions.Add("LOWER(pt.transactiontype) = LOWER(@TransactionType)");
            parameters.Add("TransactionType", filter.TransactionType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.PfStatus))
        {
            conditions.Add("LOWER(pf.pfstatus) = LOWER(@PfStatus)");
            parameters.Add("PfStatus", filter.PfStatus.Trim());
        }

        var whereClause = string.Join(" AND ", conditions);
        var offset = (filter.Page - 1) * filter.PageSize;

        var sql = $@"
            SELECT
                pt.transactionid AS TransactionId,
                pt.organizationid AS OrganizationId,
                pt.employeeid AS EmployeeId,
                (e.firstname || ' ' || e.lastname) AS EmployeeName,
                pt.pfid AS PfId,
                COALESCE(pr.rulename, '') AS RuleName,
                pt.periodid AS PeriodId,
                pp.periodname AS PeriodName,
                pt.employeepct AS EmployeePct,
                pt.employeeamount AS EmployeeAmount,
                pt.employerpct AS EmployerPct,
                pt.employeramount AS EmployerAmount,
                pt.runningbalance AS RunningBalance,
                pt.totalamount AS TotalAmount,
                pt.transactiontype AS TransactionType,
                pf.pfstatus AS PfStatus,
                pt.createdat AS CreatedAt,
                COUNT(*) OVER() AS TotalCount
            FROM payroll.pftransactions pt
            INNER JOIN payroll.payrollperiods pp
                ON pp.periodid = pt.periodid
               AND pp.organizationid = pt.organizationid
            INNER JOIN payroll.providentfund pf
                ON pf.pfid = pt.pfid
               AND pf.organizationid = pt.organizationid
            LEFT JOIN payroll.pfrules pr
                ON pr.ruleid = pf.ruleid
               AND pr.organizationid = pf.organizationid
            INNER JOIN employee.employees e
                ON e.employeeid = pt.employeeid
               AND e.organizationid = pt.organizationid
            WHERE {whereClause}
            ORDER BY pt.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", offset);

        var data = await QueryAsync<AdminProvidentFundRepaymentDto>(sql, parameters);
        var totalCount = data.FirstOrDefault()?.TotalCount ?? 0;

        return new PagedResult<AdminProvidentFundRepaymentDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<bool> ApproveProvidentFundRequestAsync(Guid organizationId, Guid approvedBy, ApproveProvidentFundRequestDto dto)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var reqSql = @"
                SELECT
                    requestid,
                    organizationid,
                    employeeid,
                    ruleid,
                    pfid,
                    requesttype,
                    status,
                    requestedemployeepct,
                    requestedemployerpct,
                    withdrawaltype,
                    reason,
                    requestedamount
                FROM payroll.pfrequests
                WHERE requestid = @requestId
                  AND organizationid = @organizationId
                FOR UPDATE;";

            var req = await connection.QuerySingleOrDefaultAsync<dynamic>(reqSql, new { requestId = dto.RequestId, organizationId }, transaction);
            if (req == null)
            {
                transaction.Rollback();
                return false;
            }

            var requestType = ((string?)req.requesttype)?.Trim().ToLowerInvariant();
            var currentStatus = ((string?)req.status)?.Trim().ToLowerInvariant();
            if (currentStatus != "pending")
            {
                transaction.Rollback();
                return false;
            }

            // 1) Mark request approved (common)
            var approveReqSql = @"
                UPDATE payroll.pfrequests
                SET status = 'approved',
                    approvedby = @approvedBy,
                    approvedat = current_timestamp,
                    updatedat = current_timestamp
                WHERE requestid = @requestId
                  AND organizationid = @organizationId
                  AND status = 'pending'
                RETURNING requestid;";

            var approvedId = await connection.QuerySingleOrDefaultAsync<Guid?>(approveReqSql, new
            {
                requestId = (Guid)req.requestid,
                organizationId,
                approvedBy
            }, transaction);

            if (approvedId == null || approvedId == Guid.Empty)
            {
                transaction.Rollback();
                return false;
            }

            if (requestType == "enrollment")
            {
                var upsertPfSql = @"
                    INSERT INTO payroll.providentfund (
                        organizationid, employeeid, ruleid,
                        employeepct, employerpct,
                        pfstatus, effectivefrom,
                        isactive
                    )
                    VALUES (
                        @organizationId, @employeeId, @ruleId,
                        @employeePct, @employerPct,
                        'active', current_date,
                        true
                    )
                    ON CONFLICT (organizationid, employeeid)
                    DO UPDATE SET
                        ruleid = EXCLUDED.ruleid,
                        employeepct = EXCLUDED.employeepct,
                        employerpct = EXCLUDED.employerpct,
                        pfstatus = 'active',
                        effectivefrom = EXCLUDED.effectivefrom,
                        isactive = true,
                        updatedat = current_timestamp
                    RETURNING pfid;";

                var pfId = await connection.QuerySingleAsync<Guid>(upsertPfSql, new
                {
                    organizationId,
                    employeeId = (Guid)req.employeeid,
                    ruleId = (Guid)req.ruleid,
                    employeePct = (decimal?)req.requestedemployeepct ?? 0m,
                    employerPct = (decimal?)req.requestedemployerpct ?? 0m
                }, transaction);

                // Back-fill request.pfid (optional but useful)
                await connection.ExecuteAsync(@"
                    UPDATE payroll.pfrequests
                    SET pfid = @pfId, updatedat = current_timestamp
                    WHERE requestid = @requestId AND organizationid = @organizationId;",
                    new { pfId, requestId = (Guid)req.requestid, organizationId }, transaction);
            }
            else if (requestType == "update")
            {
                var updatePfSql = @"
                    UPDATE payroll.providentfund
                    SET
                        ruleid = @ruleId,
                        employeepct = @employeePct,
                        employerpct = @employerPct,
                        pfstatus = 'active',
                        isactive = true,
                        updatedat = current_timestamp
                    WHERE organizationid = @organizationId
                      AND employeeid = @employeeId
                      AND (@pfId IS NULL OR pfid = @pfId)
                    RETURNING pfid;";

                var updatedPfId = await connection.QuerySingleOrDefaultAsync<Guid?>(updatePfSql, new
                {
                    organizationId,
                    employeeId = (Guid)req.employeeid,
                    pfId = (Guid?)req.pfid,
                    ruleId = (Guid)req.ruleid,
                    employeePct = (decimal?)req.requestedemployeepct ?? 0m,
                    employerPct = (decimal?)req.requestedemployerpct ?? 0m
                }, transaction);

                if (updatedPfId == null || updatedPfId == Guid.Empty)
                {
                    transaction.Rollback();
                    return false;
                }
            }
            else if (requestType == "withdrawal")
            {
                var pfSql = @"
                    SELECT
                        pfid,
                        employeepct,
                        employerpct
                    FROM payroll.providentfund
                    WHERE organizationid = @organizationId
                      AND employeeid = @employeeId
                      AND isactive = true
                    ORDER BY updatedat DESC
                    LIMIT 1
                    FOR UPDATE;";

                var pf = await connection.QuerySingleOrDefaultAsync<dynamic>(pfSql, new { organizationId, employeeId = (Guid)req.employeeid }, transaction);
                if (pf == null)
                {
                    transaction.Rollback();
                    return false;
                }

                Guid pfId = pf.pfid;
                decimal pfEmployeePct = pf.employeepct;
                decimal pfEmployerPct = pf.employerpct;

                // Determine period id (reuse latest transaction period; fallback to latest payroll period)
                var periodIdSql = @"
                    SELECT periodid
                    FROM payroll.pftransactions
                    WHERE organizationid = @organizationId
                      AND employeeid = @employeeId
                      AND pfid = @pfId
                    ORDER BY createdat DESC
                    LIMIT 1;";
                var periodId = await connection.QuerySingleOrDefaultAsync<Guid?>(periodIdSql, new { organizationId, employeeId = (Guid)req.employeeid, pfId }, transaction);
                if (periodId == null || periodId == Guid.Empty)
                {
                    periodId = await connection.QuerySingleOrDefaultAsync<Guid?>(@"
                        SELECT periodid
                        FROM payroll.payrollperiods
                        WHERE organizationid = @organizationId
                        ORDER BY createdat DESC
                        LIMIT 1;", new { organizationId }, transaction);
                }

                if (periodId == null || periodId == Guid.Empty)
                {
                    transaction.Rollback();
                    return false;
                }

                // Base balance: only monthly/profit/etc (exclude prior withdrawals/adjustments/settlements) and only base (pfrequestid is null)
                var baseTotalsSql = @"
                    SELECT
                        COALESCE(SUM(employeeamount), 0) AS employeeamount,
                        COALESCE(SUM(employeramount), 0) AS employeramount
                    FROM payroll.pftransactions
                    WHERE organizationid = @organizationId
                      AND employeeid = @employeeId
                      AND pfid = @pfId
                      AND pfrequestid IS NULL
                      AND LOWER(transactiontype) NOT IN ('withdrawal','adjustment','settlement');";

                var baseTotals = await connection.QuerySingleAsync<dynamic>(baseTotalsSql, new { organizationId, employeeId = (Guid)req.employeeid, pfId }, transaction);
                decimal baseEmployee = baseTotals.employeeamount;
                decimal baseEmployer = baseTotals.employeramount;
                decimal baseBalance = baseEmployee + baseEmployer;

                decimal requestedAmount = (decimal?)req.requestedamount ?? 0m;
                var withdrawalType = ((string?)req.withdrawaltype)?.Trim().ToLowerInvariant();
                var requestReason = ((string?)req.reason)?.Trim();

                if (requestedAmount <= 0)
                {
                    transaction.Rollback();
                    return false;
                }

                // Validate withdrawal reason against pfrules.withdrawal_config (reason match + min/max % checks)
                if (string.IsNullOrWhiteSpace(withdrawalType) || (withdrawalType != "temporary" && withdrawalType != "permanent"))
                {
                    transaction.Rollback();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(requestReason))
                {
                    transaction.Rollback();
                    return false;
                }

                var configSql = @"
                    SELECT
                        pr.allowpartialwithdraw AS AllowPartialWithdraw,
                        pr.withdrawal_config::text AS WithdrawalConfig
                    FROM payroll.providentfund pf
                    JOIN payroll.pfrules pr
                      ON pr.organizationid = pf.organizationid
                     AND pr.ruleid = pf.ruleid
                    WHERE pf.organizationid = @organizationId
                      AND pf.employeeid = @employeeId
                      AND pf.pfid = @pfId
                      AND pf.isactive = true
                    LIMIT 1;";

                var configRow = await connection.QuerySingleOrDefaultAsync<dynamic>(
                    configSql,
                    new { organizationId, employeeId = (Guid)req.employeeid, pfId },
                    transaction);

                if (configRow == null)
                {
                    transaction.Rollback();
                    return false;
                }

                bool allowPartialWithdraw = (bool)configRow.allowpartialwithdraw;
                string? withdrawalConfigJson = configRow.withdrawalconfig;

                if (!allowPartialWithdraw && withdrawalType == "temporary")
                {
                    transaction.Rollback();
                    return false;
                }

                WithdrawalConfigRoot? config;
                try
                {
                    config = string.IsNullOrWhiteSpace(withdrawalConfigJson)
                        ? null
                        : JsonSerializer.Deserialize<WithdrawalConfigRoot>(withdrawalConfigJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }

                var bucket = withdrawalType == "temporary" ? config?.temporary : config?.permanent;
                if (bucket == null || bucket.Count == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                static string EscapeRegex(string value) => Regex.Escape(value);

                // Match strategy:
                // 1) exact match (case-insensitive)
                // 2) otherwise, whole-word match against free text reason (case-insensitive)
                var exact = bucket.FirstOrDefault(e =>
                    !string.IsNullOrWhiteSpace(e.reason) &&
                    string.Equals(e.reason.Trim(), requestReason, StringComparison.OrdinalIgnoreCase));

                List<WithdrawalConfigEntry> wordMatches = new();
                if (exact == null)
                {
                    foreach (var entry in bucket)
                    {
                        var configuredReason = entry.reason?.Trim();
                        if (string.IsNullOrWhiteSpace(configuredReason))
                            continue;

                        var pattern = $@"\b{EscapeRegex(configuredReason)}\b";
                        if (Regex.IsMatch(requestReason, pattern, RegexOptions.IgnoreCase))
                        {
                            wordMatches.Add(entry);
                        }
                    }
                }

                var matchedEntry = exact ?? (wordMatches.Count == 1 ? wordMatches[0] : null);
                if (matchedEntry == null)
                {
                    // no match OR ambiguous match (multiple reasons matched)
                    transaction.Rollback();
                    return false;
                }

                var minPct = matchedEntry.min_pct ?? 0m;
                var maxPct = matchedEntry.max_pct ?? 100m;
                if (minPct < 0m || maxPct < 0m || minPct > maxPct || maxPct > 100m)
                {
                    transaction.Rollback();
                    return false;
                }

                if (baseBalance <= 0m)
                {
                    transaction.Rollback();
                    return false;
                }

                var minAllowedAmount = Math.Round((baseBalance * minPct) / 100m, 2);
                var maxAllowedAmount = Math.Round((baseBalance * maxPct) / 100m, 2);

                if (requestedAmount < minAllowedAmount || requestedAmount > maxAllowedAmount)
                {
                    transaction.Rollback();
                    return false;
                }

                if (withdrawalType == "temporary")
                {
                    var remaining = baseBalance - requestedAmount;
                    var half = requestedAmount / 2m;

                    var insertSql = @"
                        INSERT INTO payroll.pftransactions (
                            organizationid, employeeid, periodid, pfid,
                            pfrequestid,
                            salarybasisamount, employeepct, employerpct,
                            employeeamount, employeramount, runningbalance, transactiontype
                        )
                        VALUES (
                            @organizationId, @employeeId, @periodId, @pfId,
                            @pfrequestid,
                            0, @employeePct, @employerPct,
                            @employeeAmount, @employerAmount, @runningBalance, 'withdrawal'
                        );";

                    await connection.ExecuteAsync(insertSql, new
                    {
                        organizationId,
                        employeeId = (Guid)req.employeeid,
                        periodId,
                        pfId,
                        pfrequestid = (Guid)req.requestid,
                        employeePct = pfEmployeePct,
                        employerPct = pfEmployerPct,
                        employeeAmount = -half,
                        employerAmount = -half,
                        runningBalance = remaining
                    }, transaction);
                }
                else if (withdrawalType == "permanent")
                {
                    // Settlement/close: zero out the base balance
                    var insertSql = @"
                        INSERT INTO payroll.pftransactions (
                            organizationid, employeeid, periodid, pfid,
                            pfrequestid,
                            salarybasisamount, employeepct, employerpct,
                            employeeamount, employeramount, runningbalance, transactiontype
                        )
                        VALUES (
                            @organizationId, @employeeId, @periodId, @pfId,
                            @pfrequestid,
                            0, @employeePct, @employerPct,
                            @employeeAmount, @employerAmount, 0, 'settlement'
                        );";

                    await connection.ExecuteAsync(insertSql, new
                    {
                        organizationId,
                        employeeId = (Guid)req.employeeid,
                        periodId,
                        pfId,
                        pfrequestid = (Guid)req.requestid,
                        employeePct = pfEmployeePct,
                        employerPct = pfEmployerPct,
                        employeeAmount = -baseEmployee,
                        employerAmount = -baseEmployer
                    }, transaction);

                    var closedRows = await connection.ExecuteAsync(@"
                        UPDATE payroll.providentfund
                        SET pfstatus = 'closed',
                            isactive = false,
                            updatedat = current_timestamp
                        WHERE organizationid = @organizationId
                          AND employeeid = @employeeId
                          AND pfid = @pfId;",
                        new { organizationId, employeeId = (Guid)req.employeeid, pfId }, transaction);

                    if (closedRows <= 0)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
                else
                {
                    transaction.Rollback();
                    return false;
                }
            }
            else
            {
                transaction.Rollback();
                return false;
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> RejectProvidentFundRequestAsync(Guid requestId, Guid organizationId, RejectProvidentFundRequestDto dto)
    {
        var sql = @"
            UPDATE payroll.pfrequests
            SET
                status = 'rejected',
                rejectionreason = @Remarks,
                updatedat = current_timestamp
            WHERE requestid = @RequestId
              AND organizationid = @organizationId
              AND status = 'pending'
            RETURNING requestid;";

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new
        {
            RequestId = requestId,
            organizationId,
            dto.Remarks
        });

        return updatedId != null && updatedId != Guid.Empty;
    }

    public async Task<bool> AddProvidentFundMonthlyTransactionAsync(Guid organizationId, CreateProvidentFundMonthlyTransactionDto dto)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var activePfSql = @"
                SELECT
                    pfid,
                    employeepct,
                    employerpct
                FROM payroll.providentfund
                WHERE organizationid = @organizationId
                  AND employeeid = @EmployeeId
                  AND pfstatus = 'active'
                  AND isactive = true
                ORDER BY updatedat DESC
                LIMIT 1
                FOR UPDATE;";

            var activePf = await connection.QuerySingleOrDefaultAsync<dynamic>(
                activePfSql,
                new { organizationId, dto.EmployeeId },
                transaction);

            if (activePf == null)
            {
                transaction.Rollback();
                return false;
            }

            var basicSalarySql = @"
                SELECT ed.basicsalary
                FROM employee.EmploymentDetails ed
                JOIN employee.Employees e ON e.employeeid = ed.employeeid
                WHERE ed.employeeid = @EmployeeId
                  AND e.organizationid = @organizationId
                  AND ed.iscurrent = true
                ORDER BY ed.effectivedate DESC
                LIMIT 1;";

            var basicSalary = await connection.QuerySingleOrDefaultAsync<decimal?>(
                basicSalarySql,
                new { organizationId, dto.EmployeeId },
                transaction);

            if (!basicSalary.HasValue || basicSalary.Value <= 0)
            {
                transaction.Rollback();
                return false;
            }

            Guid pfId = (Guid)activePf.pfid;
            decimal employeePct = (decimal)activePf.employeepct;
            decimal employerPct = (decimal)activePf.employerpct;
            decimal salaryBasisAmount = basicSalary.Value;

            decimal employeeAmount = Math.Round((salaryBasisAmount * employeePct) / 100m, 2);
            decimal employerAmount = Math.Round((salaryBasisAmount * employerPct) / 100m, 2);

            var existingSumsSql = @"
                SELECT
                    COALESCE(SUM(employeeamount), 0) AS totalemployeeamount,
                    COALESCE(SUM(employeramount), 0) AS totalemployeramount
                FROM payroll.pftransactions
                WHERE organizationid = @organizationId
                  AND employeeid = @EmployeeId
                  AND pfid = @pfId;";

            var sums = await connection.QuerySingleAsync<dynamic>(
                existingSumsSql,
                new { organizationId, dto.EmployeeId, pfId },
                transaction);

            decimal runningBalance = (decimal)sums.totalemployeeamount + (decimal)sums.totalemployeramount + employeeAmount + employerAmount;

            var insertSql = @"
                INSERT INTO payroll.pftransactions (
                    organizationid, employeeid, periodid, pfid,
                    salarybasisamount, employeepct, employerpct,
                    employeeamount, employeramount, runningbalance, transactiontype
                )
                VALUES (
                    @organizationId, @EmployeeId, @PeriodId, @pfId,
                    @SalaryBasisAmount, @EmployeePct, @EmployerPct,
                    @EmployeeAmount, @EmployerAmount, @RunningBalance, 'monthly'
                )
                RETURNING transactionid;";

            var insertedId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                insertSql,
                new
                {
                    organizationId,
                    dto.EmployeeId,
                    dto.PeriodId,
                    pfId,
                    SalaryBasisAmount = salaryBasisAmount,
                    EmployeePct = employeePct,
                    EmployerPct = employerPct,
                    EmployeeAmount = employeeAmount,
                    EmployerAmount = employerAmount,
                    RunningBalance = runningBalance
                },
                transaction);

            if (insertedId == null || insertedId == Guid.Empty)
            {
                transaction.Rollback();
                return false;
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<ProvidentFundEnrollmentDto> ManualProvidentFundEnrollmentAsync(Guid organizationId, ManualProvidentFundEnrollmentDto dto)
    {
        var sql = @"
            INSERT INTO payroll.providentfund (
                organizationid, employeeid, ruleid, employeepct, employerpct,
                pfstatus, effectivefrom, isactive
            )
            VALUES (
                @organizationId, @EmployeeId, @RuleId, @EmployeePct, @EmployerPct,
                'active', @EffectiveFrom, true
            )
            ON CONFLICT (organizationid, employeeid)
            DO UPDATE SET
                ruleid = EXCLUDED.ruleid,
                employeepct = EXCLUDED.employeepct,
                employerpct = EXCLUDED.employerpct,
                pfstatus = 'active',
                effectivefrom = EXCLUDED.effectivefrom,
                isactive = true,
                updatedat = current_timestamp
            RETURNING
                pfid AS PfId,
                organizationid AS OrganizationId,
                employeeid AS EmployeeId,
                ruleid AS RuleId,
                employeepct AS EmployeePct,
                employerpct AS EmployerPct,
                'enrollment' AS RequestType,
                pfstatus AS PfStatus,
                effectivefrom AS EffectiveFrom,
                NULL::uuid AS ApprovedBy,
                NULL::timestamptz AS ApprovedAt,
                NULL::text AS Remarks,
                NULL::text AS RejectionReason,
                isactive AS IsActive,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt;";

        return await QuerySingleAsync<ProvidentFundEnrollmentDto>(sql, new
        {
            organizationId,
            dto.EmployeeId,
            dto.RuleId,
            dto.EmployeePct,
            dto.EmployerPct,
            dto.EffectiveFrom
        });
    }

    // Salary Advance
    public async Task<SalaryAdvanceDto> CreateSalaryAdvanceAsync(Guid organizationId, Guid employeeId, CreateSalaryAdvanceDto dto)
    {
        var sql = @"
            INSERT INTO payroll.salaryadvance (
                organizationid, employeeid, ruleid, amount, reason, createdby
            )
            VALUES (
                @organizationId, @employeeId, @RuleId, @Amount, @Reason, @employeeId
            )
            RETURNING
                advanceid AS AdvanceId,
                organizationid AS OrganizationId,
                employeeid AS EmployeeId,
                ruleid AS RuleId,
                amount AS Amount,
                reason AS Reason,
                advancestatus AS AdvanceStatus,
                createdby AS CreatedBy,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt;";

        var parameters = new
        {
            organizationId,
            employeeId,
            dto.RuleId,
            dto.Amount,
            dto.Reason
        };

        return await QuerySingleAsync<SalaryAdvanceDto>(sql, parameters);
    }

    public async Task<SalaryAdvanceDto?> UpdateMySalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid employeeId, CreateSalaryAdvanceDto dto)
    {
        var sql = @"
            UPDATE payroll.salaryadvance
            SET
                ruleid = @RuleId,
                amount = @Amount,
                reason = @Reason,
                updatedat = current_timestamp
            WHERE advanceid = @advanceId
              AND organizationid = @organizationId
              AND employeeid = @employeeId
              AND advancestatus = 'pending'
            RETURNING
                advanceid AS AdvanceId,
                organizationid AS OrganizationId,
                employeeid AS EmployeeId,
                ruleid AS RuleId,
                amount AS Amount,
                reason AS Reason,
                advancestatus AS AdvanceStatus,
                createdby AS CreatedBy,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt;";

        var parameters = new
        {
            advanceId,
            organizationId,
            employeeId,
            dto.RuleId,
            dto.Amount,
            dto.Reason
        };

        return await QuerySingleOrDefaultAsync<SalaryAdvanceDto>(sql, parameters);
    }

    public async Task<bool> CancelMySalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid employeeId)
    {
        var sql = @"
            UPDATE payroll.salaryadvance
            SET
                advancestatus = 'cancelled',
                updatedat = current_timestamp
            WHERE advanceid = @advanceId
              AND organizationid = @organizationId
              AND employeeid = @employeeId
              AND advancestatus = 'pending'
            RETURNING advanceid;";

        var cancelledId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { advanceId, organizationId, employeeId });
        return cancelledId != null && cancelledId != Guid.Empty;
    }

    public async Task<PagedResult<MySalaryAdvanceDto>> GetMySalaryAdvancesAsync(Guid organizationId, Guid employeeId, SalaryAdvanceFilterDto filter)
    {
        filter ??= new SalaryAdvanceFilterDto();
        if (filter.PageSize <= 0) filter.PageSize = 10;
        if (filter.Page <= 0) filter.Page = 1;

        var whereClause = "WHERE organizationid = @organizationId AND employeeid = @employeeId";
        if (!string.IsNullOrWhiteSpace(filter.AdvanceStatus))
            whereClause += " AND advancestatus = @AdvanceStatus";

        var countSql = $@"
            SELECT COUNT(*)
            FROM payroll.salaryadvance
            {whereClause};";

        var sql = $@"
            SELECT
                advanceid AS AdvanceId,
                amount AS Amount,
                reason AS Reason,
                advancestatus AS AdvanceStatus,
                createdat AS CreatedAt,
                approvedat AS ApprovedAt,
                deductedperiodid AS DeductedPeriodId,
                deductedat AS DeductedAt
            FROM payroll.salaryadvance
            {whereClause}
            ORDER BY createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        var parameters = new
        {
            organizationId,
            employeeId,
            filter.AdvanceStatus,
            PageSize = filter.PageSize,
            Offset = (filter.Page - 1) * filter.PageSize
        };

        var totalCount = await QuerySingleAsync<int>(countSql, parameters);
        var data = await QueryAsync<MySalaryAdvanceDto>(sql, parameters);

        return new PagedResult<MySalaryAdvanceDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<MySalaryAdvanceDto?> GetMySalaryAdvanceByIdAsync(Guid advanceId, Guid organizationId, Guid employeeId)
    {
        var sql = @"
            SELECT
                advanceid AS AdvanceId,
                amount AS Amount,
                reason AS Reason,
                advancestatus AS AdvanceStatus,
                createdat AS CreatedAt,
                approvedat AS ApprovedAt,
                deductedperiodid AS DeductedPeriodId,
                deductedat AS DeductedAt
            FROM payroll.salaryadvance
            WHERE advanceid = @advanceId
              AND organizationid = @organizationId
              AND employeeid = @employeeId;";

        return await QuerySingleOrDefaultAsync<MySalaryAdvanceDto>(sql, new { advanceId, organizationId, employeeId });
    }

    public async Task<PagedResult<AdminSalaryAdvanceDto>> GetAllSalaryAdvanceRequestsAsync(Guid organizationId, AdminSalaryAdvanceFilterDto filter)
    {
        filter ??= new AdminSalaryAdvanceFilterDto();
        if (filter.PageSize <= 0) filter.PageSize = 10;
        if (filter.Page <= 0) filter.Page = 1;

        var whereClause = "WHERE sa.organizationid = @organizationId";
        if (!string.IsNullOrWhiteSpace(filter.AdvanceStatus))
            whereClause += " AND sa.advancestatus = @AdvanceStatus";

        var countSql = $@"
            SELECT COUNT(1)
            FROM payroll.salaryadvance sa
            {whereClause};";

        var sql = $@"
            SELECT
                sa.advanceid AS AdvanceId,
                sa.employeeid AS EmployeeId,
                (e.firstname || ' ' || e.lastname) AS EmployeeName,
                d.departmentname AS Department,
                sa.amount AS Amount,
                sa.reason AS Reason,
                sa.advancestatus AS AdvanceStatus,
                sa.createdat AS CreatedAt,
                sa.approvedby AS ApprovedBy,
                sa.approvedat AS ApprovedAt,
                sa.rejectedby AS RejectedBy,
                sa.rejectedat AS RejectedAt,
                sa.rejectionreason AS RejectionReason,
                sa.deductedperiodid AS DeductedPeriodId,
                sa.deductedat AS DeductedAt
            FROM payroll.salaryadvance sa
            JOIN employee.Employees e ON sa.employeeid = e.employeeid
            LEFT JOIN employee.EmploymentDetails ed ON ed.employeeid = e.employeeid AND ed.iscurrent = true
            LEFT JOIN employee.Departments d ON d.departmentid = ed.departmentid
            {whereClause}
            ORDER BY sa.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        var parameters = new
        {
            organizationId,
            filter.AdvanceStatus,
            PageSize = filter.PageSize,
            Offset = (filter.Page - 1) * filter.PageSize
        };

        var totalCount = await QuerySingleAsync<int>(countSql, parameters);
        var data = await QueryAsync<AdminSalaryAdvanceDto>(sql, parameters);

        return new PagedResult<AdminSalaryAdvanceDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<AdminSalaryAdvanceDto?> GetSalaryAdvanceRequestByIdAsync(Guid advanceId, Guid organizationId)
    {
        var sql = @"
            SELECT
                sa.advanceid AS AdvanceId,
                sa.employeeid AS EmployeeId,
                (e.firstname || ' ' || e.lastname) AS EmployeeName,
                d.departmentname AS Department,
                sa.amount AS Amount,
                sa.reason AS Reason,
                sa.advancestatus AS AdvanceStatus,
                sa.createdat AS CreatedAt,
                sa.approvedby AS ApprovedBy,
                sa.approvedat AS ApprovedAt,
                sa.rejectedby AS RejectedBy,
                sa.rejectedat AS RejectedAt,
                sa.rejectionreason AS RejectionReason,
                sa.deductedperiodid AS DeductedPeriodId,
                sa.deductedat AS DeductedAt
            FROM payroll.salaryadvance sa
            JOIN employee.Employees e ON sa.employeeid = e.employeeid
            LEFT JOIN employee.EmploymentDetails ed ON ed.employeeid = e.employeeid AND ed.iscurrent = true
            LEFT JOIN employee.Departments d ON d.departmentid = ed.departmentid
            WHERE sa.advanceid = @advanceId
              AND sa.organizationid = @organizationId;";

        return await QuerySingleOrDefaultAsync<AdminSalaryAdvanceDto>(sql, new { advanceId, organizationId });
    }

    public async Task<bool> ApproveSalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid approvedBy)
    {
        var sql = @"
            UPDATE payroll.salaryadvance
            SET
                advancestatus = 'approved',
                approvedby = @approvedBy,
                approvedat = current_timestamp,
                updatedat = current_timestamp
            WHERE advanceid = @advanceId
              AND organizationid = @organizationId
              AND advancestatus = 'pending'
            RETURNING advanceid;";

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { advanceId, organizationId, approvedBy });
        return updatedId != null && updatedId != Guid.Empty;
    }

    public async Task<bool> RejectSalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid rejectedBy, RejectSalaryAdvanceDto dto)
    {
        var sql = @"
            UPDATE payroll.salaryadvance
            SET
                advancestatus = 'rejected',
                rejectedby = @rejectedBy,
                rejectedat = current_timestamp,
                rejectionreason = @RejectionReason,
                updatedat = current_timestamp
            WHERE advanceid = @advanceId
              AND organizationid = @organizationId
              AND advancestatus = 'pending'
            RETURNING advanceid;";

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new
        {
            advanceId,
            organizationId,
            rejectedBy,
            dto.RejectionReason
        });
        return updatedId != null && updatedId != Guid.Empty;
    }

    public async Task<bool> DisburseSalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid disbursedBy, DisburseSalaryAdvanceDto dto)
    {
        var sql = @"
            UPDATE payroll.salaryadvance
            SET
                advancestatus = 'disbursed',
                disbursedby = @disbursedBy,
                disbursedat = current_timestamp,
                disbursementnote = @DisbursementNote,
                updatedat = current_timestamp
            WHERE advanceid = @advanceId
              AND organizationid = @organizationId
              AND advancestatus = 'approved'
            RETURNING advanceid;";

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new
        {
            advanceId,
            organizationId,
            disbursedBy,
            dto.DisbursementNote
        });
        return updatedId != null && updatedId != Guid.Empty;
    }

    public async Task<LoanDto?> GetLoanByIdAsync(Guid loanid, Guid organizationId)
    {
        var sql = @"
            SELECT 
                l.loanid AS LoanId,
                l.organizationid AS OrganizationId,
                l.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                l.ruleid AS RuleId,
                lr.rulename AS RuleName,
                COALESCE(NULLIF(TRIM(l.referenceid), ''), ('LN-' || upper(substring(replace(l.loanid::text, '-'::text, ''::text), 1, 10)))) AS ReferenceId,
                l.principalamount AS PrincipalAmount,
                l.totalinterest AS TotalInterest,
                l.totalamount AS TotalAmount,
                l.monthlyinstallment AS MonthlyInstallment,
                l.remainingamount AS RemainingAmount,
                l.paidamount AS PaidAmount,
                l.totalinstallments AS TotalInstallments,
                l.paidinstallments AS PaidInstallments,
                l.repaymenttype AS RepaymentType,
                l.requeststatus AS RequestStatus,
                l.loanstatus AS LoanStatus,
                l.disbursementstatus AS DisbursementStatus,
                l.disbursementdate AS DisbursementDate,
                l.description AS Description,
                l.startdate AS StartDate,
                l.enddate AS EndDate,
                l.enddate AS ReturnDate,
                l.createdat AS CreatedAt
            FROM payroll.loans l
            JOIN employee.Employees e ON l.employeeid = e.employeeid
            LEFT JOIN payroll.loanrules lr ON l.ruleid = lr.ruleid
            WHERE l.loanid = @loanid AND l.organizationid = @organizationId;";

        return await QuerySingleOrDefaultAsync<LoanDto>(sql, new { loanid, organizationId });
    }

    public async Task<LoanDto> CreateLoanRequestAsync(Guid organizationId, Guid employeeId, CreateLoanRequestDto dto)
    {

        var sql = @"
            INSERT INTO payroll.loans (
                organizationid, employeeid, ruleid, principalamount, totalinterest, 
                remainingamount, totalinstallments, monthlyinstallment, 
                description, repaymenttype, requeststatus, loanstatus, enddate
            )
            VALUES (
                @organizationId, @employeeId, @RuleId, @PrincipalAmount, @TotalInterest, 
                0, @TotalInstallments, @MonthlyInstallment, 
                @Description, @RepaymentType, 'pending', 'inactive', @ReturnDate
            )
            RETURNING loanid;";

        var parameters = new
        {
            organizationId,
            employeeId,
            dto.RuleId,
            dto.PrincipalAmount,
            dto.TotalInterest,
            dto.TotalInstallments,
            dto.MonthlyInstallment,
            dto.Description,
            dto.RepaymentType,
            dto.ReturnDate
        };

        var loanId = await QuerySingleAsync<Guid>(sql, parameters);
        return (await GetLoanByIdAsync(loanId, organizationId))!;
    }

    public async Task<PagedResult<LoanDto>> GetMyLoansAsync(Guid organizationId, Guid employeeId, LoanFilterDto filter)
    {
        var conditions = new List<string> { "l.organizationid = @organizationId", "l.employeeid = @employeeId" };
        if (!string.IsNullOrEmpty(filter.RequestStatus)) conditions.Add("l.requeststatus = @RequestStatus");

        var whereClause = string.Join(" AND ", conditions);

        var countSql = $@"
            SELECT COUNT(1)
            FROM payroll.loans l
            WHERE {whereClause};";

        var sql = $@"
            SELECT 
                l.loanid AS LoanId,
                l.organizationid AS OrganizationId,
                l.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                l.ruleid AS RuleId,
                lr.rulename AS RuleName,
                l.referenceid AS ReferenceId,
                l.principalamount AS PrincipalAmount,
                l.totalinterest AS TotalInterest,
                l.totalamount AS TotalAmount,
                l.monthlyinstallment AS MonthlyInstallment,
                l.remainingamount AS RemainingAmount,
                l.paidamount AS PaidAmount,
                l.totalinstallments AS TotalInstallments,
                l.paidinstallments AS PaidInstallments,
                l.repaymenttype AS RepaymentType,
                l.requeststatus AS RequestStatus,
                l.loanstatus AS LoanStatus,
                l.disbursementstatus AS DisbursementStatus,
                l.disbursementdate AS DisbursementDate,
                l.description AS Description,
                l.startdate AS StartDate,
                l.enddate AS EndDate,
                l.enddate AS ReturnDate,
                l.createdat AS CreatedAt
            FROM payroll.loans l
            JOIN employee.Employees e ON l.employeeid = e.employeeid
            LEFT JOIN payroll.loanrules lr ON l.ruleid = lr.ruleid
            WHERE {whereClause}
            ORDER BY l.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        var parameters = new
        {
            organizationId,
            employeeId,
            filter.RequestStatus,
            PageSize = filter.PageSize,
            Offset = (filter.Page - 1) * filter.PageSize
        };

        var totalCount = await QuerySingleAsync<int>(countSql, parameters);
        var data = await QueryAsync<LoanDto>(sql, parameters);

        return new PagedResult<LoanDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<LoanDto?> UpdateLoanStatusAsync(Guid organizationId, Guid approvedBy, UpdateLoanStatusDto dto)
    {
        var sql = @"
            UPDATE payroll.loans
            SET 
                requeststatus = @RequestStatus,
                approvedby = @approvedBy,
                approvedat = CURRENT_TIMESTAMP,
                rejectionreason = @RejectionReason,
                updatedat = CURRENT_TIMESTAMP
            WHERE loanid = @LoanId AND organizationid = @organizationId
            RETURNING loanid;";

        var parameters = new
        {
            dto.LoanId,
            organizationId,
            approvedBy,
            dto.RequestStatus,
            dto.RejectionReason
        };

        var loanId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (loanId == null) return null;

        return await GetLoanByIdAsync(loanId.Value, organizationId);
    }

    public async Task<LoanDto?> DisburseLoanAsync(Guid organizationId, Guid createdBy, DisburseLoanDto dto)
    {
        var sql = @"
            UPDATE payroll.loans
            SET 
                disbursementstatus = 'paid',
                loanstatus = 'active',
                disbursementdate = CURRENT_DATE,
                startdate = @StartDate,
                enddate = @EndDate,
                createdby = @createdBy,
                remainingamount = principalamount + totalinterest,
                updatedat = CURRENT_TIMESTAMP
            WHERE loanid = @LoanId AND organizationid = @organizationId AND requeststatus = 'accepted'
            RETURNING loanid;";

        var parameters = new
        {
            dto.LoanId,
            organizationId,
            createdBy,
            dto.StartDate,
            dto.EndDate
        };

        var loanId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (loanId == null) return null;

        return await GetLoanByIdAsync(loanId.Value, organizationId);
    }

    public async Task<IEnumerable<MyActiveLoanDto>> GetMyActiveLoansAsync(Guid organizationId, Guid employeeId)
    {
        var sql = @"
            SELECT 
                l.loanid AS LoanId,
                l.organizationid AS OrganizationId,
                l.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                l.ruleid AS RuleId,
                lr.rulename AS RuleName,
                l.referenceid AS ReferenceId,
                l.principalamount AS PrincipalAmount,
                l.totalinterest AS TotalInterest,
                l.totalamount AS TotalAmount,
                l.monthlyinstallment AS MonthlyInstallment,
                COALESCE(lp.remainingamount, l.remainingamount) AS RemainingAmount,
                (l.totalamount - COALESCE(lp.remainingamount, l.remainingamount)) AS PaidAmount,
                l.totalinstallments AS TotalInstallments,
                COALESCE(lp.installmentnumber, 0) AS PaidInstallments,
                l.repaymenttype AS RepaymentType,
                l.requeststatus AS RequestStatus,
                l.loanstatus AS LoanStatus,
                l.description AS Description,
                l.startdate AS StartDate,
                l.enddate AS EndDate,
                l.enddate AS ReturnDate,
                l.createdat AS CreatedAt,
                GREATEST(0, (l.totalinstallments - COALESCE(lp.installmentnumber, 0))) AS RemainingInstallments,
                CASE 
                    WHEN l.totalamount = 0 THEN 0 
                    ELSE ROUND(((l.totalamount - COALESCE(lp.remainingamount, l.remainingamount)) / l.totalamount) * 100, 2)
                END AS CompletedPercentage
            FROM payroll.loans l
            JOIN employee.Employees e ON l.employeeid = e.employeeid
            LEFT JOIN payroll.loanrules lr ON l.ruleid = lr.ruleid
            LEFT JOIN (
                SELECT DISTINCT ON (loanid) loanid, installmentnumber, remainingamount
                FROM payroll.loanpayments
                ORDER BY loanid, installmentnumber DESC
            ) lp ON l.loanid = lp.loanid
            WHERE l.organizationid = @organizationId 
              AND l.employeeid = @employeeId 
              AND l.loanstatus = 'active' 
            ORDER BY l.createdat DESC;";

        return await QueryAsync<MyActiveLoanDto>(sql, new { organizationId, employeeId });
    }

    public async Task<IEnumerable<MyActiveLoanDto>> GetMyPendingLoansAsync(Guid organizationId, Guid employeeId)
    {
        var sql = @"
            SELECT 
    l.loanid AS LoanId,
    l.organizationid AS OrganizationId,
    l.employeeid AS EmployeeId,
    e.firstname || ' ' || e.lastname AS EmployeeName,
    l.ruleid AS RuleId,
    lr.rulename AS RuleName,
    l.referenceid AS ReferenceId,
    l.principalamount AS PrincipalAmount,
    l.totalinterest AS TotalInterest,
    l.totalamount AS TotalAmount,
    l.monthlyinstallment AS MonthlyInstallment,
    COALESCE(lp.remainingamount, l.remainingamount) AS RemainingAmount,
    (l.totalamount - COALESCE(lp.remainingamount, l.remainingamount)) AS PaidAmount,
    l.totalinstallments AS TotalInstallments,
    COALESCE(lp.installmentnumber, 0) AS PaidInstallments,
    l.repaymenttype AS RepaymentType,
    l.requeststatus AS RequestStatus,
    l.loanstatus AS LoanStatus,
    l.description AS Description,
    l.startdate AS StartDate,
    l.enddate AS EndDate,
    l.enddate AS ReturnDate,
    l.createdat AS CreatedAt,
    GREATEST(0, (l.totalinstallments - COALESCE(lp.installmentnumber, 0))) AS RemainingInstallments,
    CASE 
        WHEN l.totalamount = 0 THEN 0 
        ELSE ROUND(((l.totalamount - COALESCE(lp.remainingamount, l.remainingamount)) / l.totalamount) * 100, 2)
    END AS CompletedPercentage
FROM payroll.loans l
JOIN employee.Employees e ON l.employeeid = e.employeeid
LEFT JOIN payroll.loanrules lr ON l.ruleid = lr.ruleid
LEFT JOIN (
    SELECT DISTINCT ON (loanid) loanid, installmentnumber, remainingamount
    FROM payroll.loanpayments
    ORDER BY loanid, installmentnumber DESC
) lp ON l.loanid = lp.loanid
WHERE l.organizationid = @organizationId 
  AND l.employeeid = @employeeId 
  AND LOWER(TRIM(l.requeststatus)) IN ('pending', 'accepted', 'approved') 
  AND LOWER(TRIM(l.disbursementstatus)) = 'pending'
ORDER BY l.createdat DESC;";

        return await QueryAsync<MyActiveLoanDto>(sql, new { organizationId, employeeId });
    }

    public async Task<PagedResult<MyActiveLoanDto>> GetDisbursedLoansAsync(Guid organizationId, DisbursedLoanFilterDto filter)
    {
        var conditions = new List<string> { "l.organizationid = @organizationId", "l.disbursementstatus != 'pending'" };
        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            conditions.Add("(e.firstname || ' ' || e.lastname) ILIKE @SearchTerm");
            parameters.Add("SearchTerm", $"%{filter.SearchTerm}%");
        }

        if (!string.IsNullOrEmpty(filter.LoanStatus))
        {
            conditions.Add("l.loanstatus = @LoanStatus");
            parameters.Add("LoanStatus", filter.LoanStatus);
        }

        if (filter.MinTotalAmount.HasValue)
        {
            conditions.Add("l.totalamount >= @MinTotalAmount");
            parameters.Add("MinTotalAmount", filter.MinTotalAmount);
        }

        if (filter.MaxTotalAmount.HasValue)
        {
            conditions.Add("l.totalamount <= @MaxTotalAmount");
            parameters.Add("MaxTotalAmount", filter.MaxTotalAmount);
        }

        var whereClause = string.Join(" AND ", conditions);
        var offset = (filter.Page - 1) * filter.PageSize;

        var sql = $@"
            SELECT 
                l.loanid AS LoanId,
                l.organizationid AS OrganizationId,
                l.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                l.ruleid AS RuleId,
                lr.rulename AS RuleName,
                l.referenceid AS ReferenceId,
                l.principalamount AS PrincipalAmount,
                l.totalinterest AS TotalInterest,
                l.totalamount AS TotalAmount,
                l.monthlyinstallment AS MonthlyInstallment,
                COALESCE(lp.remainingamount, l.remainingamount) AS RemainingAmount,
                (l.totalamount - COALESCE(lp.remainingamount, l.remainingamount)) AS PaidAmount,
                l.totalinstallments AS TotalInstallments,
                COALESCE(lp.installmentnumber, 0) AS PaidInstallments,
                l.repaymenttype AS RepaymentType,
                l.requeststatus AS RequestStatus,
                l.loanstatus AS LoanStatus,
                l.description AS Description,
                l.startdate AS StartDate,
                l.enddate AS EndDate,
                l.enddate AS ReturnDate,
                l.createdat AS CreatedAt,
                GREATEST(0, (l.totalinstallments - COALESCE(lp.installmentnumber, 0))) AS RemainingInstallments,
                CASE 
                    WHEN l.totalamount = 0 THEN 0 
                    ELSE ROUND(((l.totalamount - COALESCE(lp.remainingamount, l.remainingamount)) / l.totalamount) * 100, 2)
                END AS CompletedPercentage,
                COUNT(*) OVER() as TotalCount
            FROM payroll.loans l
            JOIN employee.Employees e ON l.employeeid = e.employeeid
            LEFT JOIN payroll.loanrules lr ON l.ruleid = lr.ruleid
            LEFT JOIN (
                SELECT DISTINCT ON (loanid) loanid, installmentnumber, remainingamount
                FROM payroll.loanpayments
                ORDER BY loanid, installmentnumber DESC
            ) lp ON l.loanid = lp.loanid
            WHERE {whereClause}
            ORDER BY l.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", offset);

        var data = await QueryAsync<MyActiveLoanDto>(sql, parameters);
        var totalCount = data.FirstOrDefault()?.TotalCount ?? 0;

        return new PagedResult<MyActiveLoanDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PagedResult<MyActiveLoanDto>> GetDisbursedActiveLoansAsync(Guid organizationId, DisbursedLoanFilterDto filter)
    {
        var conditions = new List<string>
        {
            "l.organizationid = @organizationId",
            "l.disbursementstatus = 'paid'",
            "LOWER(TRIM(l.loanstatus)) = 'active'"
        };
        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            conditions.Add("(e.firstname || ' ' || e.lastname) ILIKE @SearchTerm");
            parameters.Add("SearchTerm", $"%{filter.SearchTerm}%");
        }

        if (!string.IsNullOrEmpty(filter.LoanStatus))
        {
            conditions.Add("l.loanstatus = @LoanStatus");
            parameters.Add("LoanStatus", filter.LoanStatus);
        }

        if (filter.MinTotalAmount.HasValue)
        {
            conditions.Add("l.totalamount >= @MinTotalAmount");
            parameters.Add("MinTotalAmount", filter.MinTotalAmount);
        }

        if (filter.MaxTotalAmount.HasValue)
        {
            conditions.Add("l.totalamount <= @MaxTotalAmount");
            parameters.Add("MaxTotalAmount", filter.MaxTotalAmount);
        }

        var whereClause = string.Join(" AND ", conditions);
        var offset = (filter.Page - 1) * filter.PageSize;

        var sql = $@"
            SELECT 
                l.loanid AS LoanId,
                l.organizationid AS OrganizationId,
                l.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                l.ruleid AS RuleId,
                lr.rulename AS RuleName,
                l.referenceid AS ReferenceId,
                l.principalamount AS PrincipalAmount,
                l.totalinterest AS TotalInterest,
                l.totalamount AS TotalAmount,
                l.monthlyinstallment AS MonthlyInstallment,
                COALESCE(lp.remainingamount, l.remainingamount) AS RemainingAmount,
                (l.totalamount - COALESCE(lp.remainingamount, l.remainingamount)) AS PaidAmount,
                l.totalinstallments AS TotalInstallments,
                COALESCE(lp.installmentnumber, 0) AS PaidInstallments,
                l.repaymenttype AS RepaymentType,
                l.requeststatus AS RequestStatus,
                l.loanstatus AS LoanStatus,
                l.description AS Description,
                l.startdate AS StartDate,
                l.enddate AS EndDate,
                l.enddate AS ReturnDate,
                l.createdat AS CreatedAt,
                GREATEST(0, (l.totalinstallments - COALESCE(lp.installmentnumber, 0))) AS RemainingInstallments,
                CASE 
                    WHEN l.totalamount = 0 THEN 0 
                    ELSE ROUND(((l.totalamount - COALESCE(lp.remainingamount, l.remainingamount)) / l.totalamount) * 100, 2)
                END AS CompletedPercentage,
                COUNT(*) OVER() as TotalCount
            FROM payroll.loans l
            JOIN employee.Employees e ON l.employeeid = e.employeeid
            LEFT JOIN payroll.loanrules lr ON l.ruleid = lr.ruleid
            LEFT JOIN (
                SELECT DISTINCT ON (loanid) loanid, installmentnumber, remainingamount
                FROM payroll.loanpayments
                ORDER BY loanid, installmentnumber DESC
            ) lp ON l.loanid = lp.loanid
            WHERE {whereClause}
            ORDER BY l.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", offset);

        var data = await QueryAsync<MyActiveLoanDto>(sql, parameters);
        var totalCount = data.FirstOrDefault()?.TotalCount ?? 0;

        return new PagedResult<MyActiveLoanDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PagedResult<RepaymentHistoryDto>> GetAllRepaymentsAsync(Guid organizationId, RepaymentFilterDto filter)
    {
        var conditions = new List<string> { "lp.organizationid = @organizationId" };
        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            conditions.Add("(e.firstname || ' ' || e.lastname) ILIKE @SearchTerm");
            parameters.Add("SearchTerm", $"%{filter.SearchTerm}%");
        }

        if (!string.IsNullOrEmpty(filter.RepaymentType))
        {
            conditions.Add("lp.repaymenttype = @RepaymentType");
            parameters.Add("RepaymentType", filter.RepaymentType);
        }

        if (!string.IsNullOrEmpty(filter.PaymentMethod))
        {
            conditions.Add("lp.paymentmethod = @PaymentMethod");
            parameters.Add("PaymentMethod", filter.PaymentMethod);
        }

        if (!string.IsNullOrEmpty(filter.LoanStatus))
        {
            conditions.Add("l.loanstatus = @LoanStatus");
            parameters.Add("LoanStatus", filter.LoanStatus);
        }

        var whereClause = string.Join(" AND ", conditions);
        var offset = (filter.Page - 1) * filter.PageSize;

        var sql = $@"
            SELECT 
                lp.id AS Id,
                lp.loanid AS LoanId,
                l.referenceid AS ReferenceId,
                lp.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                l.totalamount AS LoanTotalAmount,
                l.loanstatus AS LoanStatus,
                lp.periodid AS PeriodId,
                lp.installmentnumber AS InstallmentNumber,
                lp.installmentamount AS InstallmentAmount,
                lp.remainingamount AS RemainingAmount,
                lp.repaymenttype AS RepaymentType,
                lp.paymentdate AS PaymentDate,
                lp.paymentmethod AS PaymentMethod,
                lp.paymentstatus AS PaymentStatus,
                lp.createdat AS CreatedAt,
                COUNT(*) OVER() as TotalCount
            FROM payroll.loanpayments lp
            JOIN employee.Employees e ON lp.employeeid = e.employeeid
            JOIN payroll.loans l ON lp.loanid = l.loanid
            WHERE {whereClause}
            ORDER BY lp.paymentdate DESC, lp.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", offset);

        var data = await QueryAsync<RepaymentHistoryDto>(sql, parameters);
        var totalCount = data.FirstOrDefault()?.TotalCount ?? 0;

        return new PagedResult<RepaymentHistoryDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<bool> AddLoanPaymentAsync(Guid organizationId, AddLoanPaymentDto dto)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Update the main loan table (only if not already completed and has balance)
            var updateSql = @"
                UPDATE payroll.loans
                SET 
                    remainingamount = GREATEST(0, remainingamount - @InstallmentAmount),
                    paidinstallments = @InstallmentNumber,
                    loanstatus = CASE 
                        WHEN (remainingamount - @InstallmentAmount) <= 0 THEN 'completed' 
                        ELSE loanstatus 
                    END,
                    disbursementstatus = CASE
                        WHEN (remainingamount - @InstallmentAmount) <= 0 THEN 'recovered'
                        ELSE disbursementstatus
                    END,
                    updatedat = CURRENT_TIMESTAMP
                WHERE referenceid = @ReferenceId 
                  AND organizationid = @organizationId 
                  AND employeeid = @EmployeeId
                  AND loanstatus != 'completed'
                  AND remainingamount > 0
                RETURNING remainingamount, loanid;";

            var result = await connection.QuerySingleOrDefaultAsync<dynamic>(updateSql, new
            {
                dto.ReferenceId,
                organizationId,
                dto.EmployeeId,
                dto.InstallmentAmount,
                dto.InstallmentNumber
            }, transaction);

            if (result == null)
            {
                transaction.Rollback();
                return false; // Result will be handled as "Loan already completed or not found" in Service
            }

            decimal newRemainingAmount = result.remainingamount;
            Guid loanId = result.loanid;

            // 2. Insert into loanpayments table
            var insertSql = @"
                INSERT INTO payroll.loanpayments (
                    organizationid, loanid, employeeid, periodid, 
                    installmentnumber, installmentamount, remainingamount, 
                    repaymenttype, paymentdate, paymentmethod, paymentstatus
                ) VALUES (
                    @organizationId, @loanId, @EmployeeId, @PeriodId, 
                    @InstallmentNumber, @InstallmentAmount, @RemainingAmount, 
                    @RepaymentType, @PaymentDate, @PaymentMethod, 'settled'
                );";

            await connection.ExecuteAsync(insertSql, new
            {
                organizationId,
                loanId,
                dto.EmployeeId,
                dto.PeriodId,
                dto.InstallmentNumber,
                dto.InstallmentAmount,
                RemainingAmount = newRemainingAmount,
                dto.RepaymentType,
                dto.PaymentDate,
                dto.PaymentMethod
            }, transaction);

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<PayrollCalculationResultDto?> CalculatePayrollAsync(Guid organizationId, CalculatePayrollDto dto)
    #region
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var periodSql = @"
                SELECT periodid, startdate, enddate
                FROM payroll.payrollperiods
                WHERE periodid = @PeriodId
                  AND organizationid = @organizationId
                  AND isdeleted = false
                LIMIT 1;";

            var period = await connection.QuerySingleOrDefaultAsync<dynamic>(periodSql, new { dto.PeriodId, organizationId }, transaction);
            if (period == null) { transaction.Rollback(); return null; }

            // 1. Fetch Active Employees
            var activeEmployeesSql = @"
                SELECT
                    e.employeeid AS EmployeeId,
                    COALESCE(ed.basicsalary, 0) AS BasicSalary,
                    ed.categoryid AS CategoryId
                FROM employee.employees e
                LEFT JOIN LATERAL (
                    SELECT ed2.basicsalary, ed2.categoryid
                    FROM employee.employmentdetails ed2
                    WHERE ed2.employeeid = e.employeeid
                    ORDER BY ed2.iscurrent DESC, ed2.effectivedate DESC, ed2.createdat DESC
                    LIMIT 1
                ) ed ON true
                WHERE e.organizationid = @organizationId
                  AND LOWER(COALESCE(e.status, '')) = 'active';";

            var activeEmployeesList = (await connection.QueryAsync<dynamic>(activeEmployeesSql, new { organizationId }, transaction)).ToList();

            // Setup Result Object
            var result = new PayrollCalculationResultDto
            {
                PeriodId = dto.PeriodId,
                OrganizationId = organizationId,
                PeriodStartDate = (DateTime)period.startdate,
                PeriodEndDate = (DateTime)period.enddate,
                EmployeesConsidered = activeEmployeesList.Count
            };

            // Prefetch components
            dynamic? attendanceRule = null;
            if (dto.AttendanceRuleId.HasValue && dto.AttendanceRuleId.Value != Guid.Empty)
            {
                var attRuleSql = @"SELECT ruleid, fixeddeduction, percentagededuction, halfdaymultiplier FROM payroll.attendancedeductionrules WHERE ruleid = @RuleId AND organizationid = @organizationId AND isactive = true AND isdeleted = false LIMIT 1;";
                attendanceRule = await connection.QuerySingleOrDefaultAsync<dynamic?>(attRuleSql, new { organizationId, RuleId = dto.AttendanceRuleId.Value }, transaction);
            }

            var timesheetsSql = @"
                SELECT employee_id,
                       MAX(shiftid) as shiftid,
                       SUM(scheduled_working_days) as scheduled_working_days,
                       SUM(total_calendar_days) as total_calendar_days,
                       SUM(present_days) as present_days,
                       SUM(absent_days) as absent_days,
                       SUM(half_days) as half_days,
                       SUM(late_minutes) as late_minutes,
                       SUM(leave_days_total) as leave_days_total,
                       SUM(paid_leave_days) as paid_leave_days,
                       SUM(unpaid_leave_days) as unpaid_leave_days,
                       SUM(half_paid_leave_days) as half_paid_leave_days,
                       SUM(regular_overtime_hours) as regular_overtime_hours,
                       SUM(weekend_overtime_hours) as weekend_overtime_hours,
                       SUM(holiday_overtime_hours) as holiday_overtime_hours
                FROM attendance.timesheet_payroll_summary 
                WHERE organization_id = @organizationId 
                  AND period_end <= @PeriodEndDate 
                  AND is_consumed_by_payroll = FALSE
                GROUP BY employee_id;";

            var timesheets = (await connection.QueryAsync<dynamic>(timesheetsSql, new { organizationId, PeriodEndDate = (DateTime)period.enddate }, transaction)).ToList();

            // Prefetch late attendance rule
            dynamic? lateAttendanceRule = null;
            if (dto.LateAttendanceRuleId.HasValue && dto.LateAttendanceRuleId.Value != Guid.Empty)
            {
                var lateRuleSql = @"SELECT ruleid, fixeddeduction, percentagededuction, graceminutes FROM payroll.lateattendancerules WHERE ruleid = @RuleId AND organizationid = @organizationId AND isactive = true AND isdeleted = false LIMIT 1;";
                lateAttendanceRule = await connection.QuerySingleOrDefaultAsync<dynamic?>(lateRuleSql, new { organizationId, RuleId = dto.LateAttendanceRuleId.Value }, transaction);
            }

            // Prefetch distinct shifts used in timesheets
            var distinctShiftIds = timesheets
                .Where(t => t.shiftid != null)
                .Select(t => (Guid)t.shiftid)
                .Distinct()
                .ToList();

            var shifts = new List<dynamic>();
            if (distinctShiftIds.Any())
            {
                var shiftsSql = @"SELECT shiftid, starttime, endtime FROM attendance.shifts WHERE shiftid = ANY(@ShiftIds) AND organizationid = @organizationId AND isactive = true;";
                shifts = (await connection.QueryAsync<dynamic>(shiftsSql, new { ShiftIds = distinctShiftIds.ToArray(), organizationId }, transaction)).ToList();
            }

            // Prefetch leave rule
            dynamic? leaveRule = null;
            if (dto.LeaveRuleId.HasValue && dto.LeaveRuleId.Value != Guid.Empty)
            {
                var leaveRuleSql = @"SELECT ruleid, unpaidmultiplier, halfpaidmultiplier FROM payroll.leaverules WHERE ruleid = @RuleId AND organizationid = @organizationId AND isactive = true AND isdeleted = false LIMIT 1;";
                leaveRule = await connection.QuerySingleOrDefaultAsync<dynamic?>(leaveRuleSql, new { organizationId, RuleId = dto.LeaveRuleId.Value }, transaction);
            }

            // Prefetch overtime rules (up to 3 — one per type: Regular, Weekend, Holiday)
            var overtimeRulesSql = @"SELECT ruleid, overtimetype, fixedamount, percentage FROM payroll.overtimerules WHERE organizationid = @organizationId AND isactive = true AND isdeleted = false;";
            var overtimeRules = (await connection.QueryAsync<dynamic>(overtimeRulesSql, new { organizationId }, transaction)).ToList();

            var performanceRules = new List<dynamic>();
            var appraisals = new List<dynamic>();
            if (dto.CycleId != Guid.Empty)
            {
                var perfSql = @"SELECT ruleid, minscore, maxscore, fixedamount, percentage FROM payroll.performancerules WHERE organizationid = @organizationId AND isactive = true AND isdeleted = false;";
                performanceRules = (await connection.QueryAsync<dynamic>(perfSql, new { organizationId }, transaction)).ToList();
                var appSql = @"SELECT employeeid, finalrating FROM performance.hrappraisals WHERE organizationid = @organizationId AND cycleid = @CycleId AND LOWER(status) = 'submitted';";
                appraisals = (await connection.QueryAsync<dynamic>(appSql, new { organizationId, CycleId = dto.CycleId }, transaction)).ToList();
            }

            dynamic? bonusRule = null;
            var bonusEntries = new List<dynamic>();
            if (dto.BonusRuleId.HasValue && dto.BonusRuleId.Value != Guid.Empty)
            {
                var ruleSql = @"SELECT ruleid, bonustype, bonusamount, bonuspercentage FROM payroll.bonusrules WHERE organizationid = @organizationId AND ruleid = @RuleId AND isactive = true AND isdeleted = false LIMIT 1;";
                bonusRule = await connection.QuerySingleOrDefaultAsync<dynamic?>(ruleSql, new { organizationId, RuleId = dto.BonusRuleId.Value }, transaction);
            }
            else
            {
                var entriesSql = @"SELECT bonusid, employeeid, ruleid, bonousamount FROM payroll.bonusentries WHERE organizationid = @organizationId AND periodid = @PeriodId;";
                bonusEntries = (await connection.QueryAsync<dynamic>(entriesSql, new { organizationId, PeriodId = dto.PeriodId }, transaction)).ToList();
            }

            var gratuitiesSql = @"SELECT id, employeeid, totalamount FROM payroll.gratuitytransactions WHERE organizationid = @organizationId AND periodid = @PeriodId AND isdeleted = false AND paymentstatus != 'cancelled';";
            var gratuities = (await connection.QueryAsync<dynamic>(gratuitiesSql, new { organizationId, PeriodId = dto.PeriodId }, transaction)).ToList();

            var pfSql = @"
                SELECT pf.pfid AS PfId, pf.employeeid AS EmployeeId, COALESCE(pf.employeepct, 0) AS EmployeePct, COALESCE(pf.employerpct, 0) AS EmployerPct
                FROM payroll.providentfund pf
                INNER JOIN employee.employees e ON e.employeeid = pf.employeeid AND e.organizationid = pf.organizationid
                WHERE pf.organizationid = @organizationId AND LOWER(COALESCE(e.status, '')) = 'active' AND LOWER(COALESCE(pf.pfstatus, '')) = 'active' AND pf.isactive = true;";
            var activeProvidentFunds = (await connection.QueryAsync<dynamic>(pfSql, new { organizationId }, transaction)).ToList();

            var taxRuleSql = @"SELECT componentruleid, taxability, limitamount, limitpercentage FROM payroll.taxcomponentrules WHERE organizationid = @organizationId AND LOWER(applystage) = 'providentfunds' AND isdeleted = false LIMIT 1;";
            var pfTaxRule = await connection.QueryFirstOrDefaultAsync<dynamic>(taxRuleSql, new { organizationId }, transaction);

            var taxSlabsSql = @"SELECT categoryid, minincome, maxincome, fixedamount, percentage FROM payroll.taxslabs WHERE isdeleted = false;";
            var taxSlabs = (await connection.QueryAsync<dynamic>(taxSlabsSql, null, transaction)).ToList();

            var loansSql = @"
                SELECT l.loanid AS LoanId, l.employeeid AS EmployeeId, COALESCE(l.monthlyinstallment, 0) AS MonthlyInstallment, COALESCE(l.remainingamount, 0) AS RemainingAmount
                FROM payroll.loans l
                WHERE l.organizationid = @organizationId AND LOWER(COALESCE(l.loanstatus, '')) = 'active' AND LOWER(COALESCE(l.repaymenttype, '')) = 'installment' AND COALESCE(l.remainingamount, 0) > 0 AND COALESCE(l.monthlyinstallment, 0) > 0;";
            var loans = (await connection.QueryAsync<dynamic>(loansSql, new { organizationId }, transaction)).ToList();

            var advanceSql = @"
                SELECT sa.advanceid AS AdvanceId, sa.employeeid AS EmployeeId, COALESCE(sa.amount, 0) AS Amount
                FROM payroll.salaryadvance sa
                WHERE sa.organizationid = @organizationId AND LOWER(COALESCE(sa.advancestatus, '')) = 'disbursed' AND sa.deductedperiodid IS NULL ORDER BY sa.createdat ASC;";
            var advances = (await connection.QueryAsync<dynamic>(advanceSql, new { organizationId }, transaction)).ToList();

            // Process per Employee
            foreach (var employee in activeEmployeesList)
            {
                var employeeId = (Guid)employee.employeeid;
                var basicSalary = Convert.ToDecimal(employee.basicsalary);
                var categoryId = employee.categoryid != null ? (Guid?)employee.categoryid : null;

                decimal remainingSalary = basicSalary;
                decimal totalBonuses = 0;

                // 0. Attendance Deduction
                if (attendanceRule != null)
                {
                    var ts = timesheets.FirstOrDefault(t => t.employee_id == employeeId);
                    if (ts != null)
                    {
                        int absentDays = Convert.ToInt32(ts.absent_days);
                        int halfDays = Convert.ToInt32(ts.half_days);
                        int scheduledDays = Convert.ToInt32(ts.scheduled_working_days);
                        int totalDays = Convert.ToInt32(ts.total_calendar_days);
                        int presentDays = Convert.ToInt32(ts.present_days);

                        decimal fixedDeduction = attendanceRule.fixeddeduction != null ? Convert.ToDecimal(attendanceRule.fixeddeduction) : 0m;
                        decimal percentageDeduction = attendanceRule.percentagededuction != null ? Convert.ToDecimal(attendanceRule.percentagededuction) : 0m;
                        decimal halfDayMultiplier = attendanceRule.halfdaymultiplier != null ? Convert.ToDecimal(attendanceRule.halfdaymultiplier) : 0.50m;

                        decimal effectiveAbsents = absentDays + (halfDays * halfDayMultiplier);

                        if (effectiveAbsents > 0)
                        {
                            decimal deductionPerAbsent = 0;
                            if (fixedDeduction > 0)
                            {
                                deductionPerAbsent = fixedDeduction;
                            }
                            else if (percentageDeduction > 0)
                            {
                                decimal perDaySalary = basicSalary / 30m;
                                deductionPerAbsent = Math.Round(perDaySalary * (percentageDeduction / 100m), 2);
                            }

                            decimal absentDeductionAmt = absentDays * deductionPerAbsent;
                            decimal halfDayDeductionAmt = (halfDays * halfDayMultiplier) * deductionPerAbsent;
                            decimal totalAttDeduction = absentDeductionAmt + halfDayDeductionAmt;

                            if (totalAttDeduction > 0)
                            {
                                var existingAttSumSql = @"SELECT id FROM payroll.attendancesummary WHERE employeeid = @EmployeeId AND periodid = @PeriodId LIMIT 1;";
                                var existingAttSum = await connection.QuerySingleOrDefaultAsync<Guid?>(existingAttSumSql, new { EmployeeId = employeeId, PeriodId = dto.PeriodId }, transaction);

                                if (existingAttSum == null)
                                {
                                    var insertAttSumSql = @"
                                        INSERT INTO payroll.attendancesummary (
                                            employeeid, periodid, ruleid, scheduleddays, totaldays, presentdays, absentdays, halfdays, effectiveabsents, absentdeduction, halfdaydeduction, totaldeduction
                                        ) VALUES (
                                            @EmployeeId, @PeriodId, @RuleId, @ScheduledDays, @TotalDays, @PresentDays, @AbsentDays, @HalfDays, @EffectiveAbsents, @AbsentDeduction, @HalfDayDeduction, @TotalDeduction
                                        ) RETURNING id;";
                                    await connection.QuerySingleOrDefaultAsync<Guid?>(insertAttSumSql, new
                                    {
                                        EmployeeId = employeeId,
                                        PeriodId = dto.PeriodId,
                                        RuleId = dto.AttendanceRuleId!.Value,
                                        ScheduledDays = scheduledDays,
                                        TotalDays = totalDays,
                                        PresentDays = presentDays,
                                        AbsentDays = absentDays,
                                        HalfDays = halfDays,
                                        EffectiveAbsents = effectiveAbsents,
                                        AbsentDeduction = absentDeductionAmt,
                                        HalfDayDeduction = halfDayDeductionAmt,
                                        TotalDeduction = totalAttDeduction
                                    }, transaction);

                                    var updateTsSql = @"
                                        UPDATE attendance.timesheet_payroll_summary 
                                        SET is_consumed_by_payroll = TRUE, consumed_at = CURRENT_TIMESTAMP, payroll_period_id = @PeriodId
                                        WHERE employee_id = @EmployeeId AND organization_id = @organizationId AND period_end <= @PeriodEndDate AND is_consumed_by_payroll = FALSE;";
                                    await connection.ExecuteAsync(updateTsSql, new { EmployeeId = employeeId, organizationId, PeriodEndDate = (DateTime)period.enddate, PeriodId = dto.PeriodId }, transaction);

                                    remainingSalary -= totalAttDeduction;

                                    result.AttendanceDeductionsApplied += 1;
                                    result.TotalAttendanceDeductionAmount += totalAttDeduction;
                                    result.AttendanceDeductionDetails.Add(new PayrollAttendanceDeductionDetailDto
                                    {
                                        EmployeeId = employeeId,
                                        ScheduledDays = scheduledDays,
                                        TotalDays = totalDays,
                                        PresentDays = presentDays,
                                        AbsentDays = absentDays,
                                        HalfDays = halfDays,
                                        EffectiveAbsents = effectiveAbsents,
                                        AbsentDeduction = absentDeductionAmt,
                                        HalfDayDeduction = halfDayDeductionAmt,
                                        TotalDeduction = totalAttDeduction,
                                        NetSalaryAfterAttendanceDeduction = remainingSalary
                                    });
                                }
                            }
                        }
                    }
                }

                // 0b. Late Attendance Deduction
                if (lateAttendanceRule != null)
                {
                    var ts = timesheets.FirstOrDefault(t => t.employee_id == employeeId);
                    if (ts != null)
                    {
                        int totalLateMinutes = Convert.ToInt32(ts.late_minutes);
                        Guid? tsShiftId = ts.shiftid != null ? (Guid?)ts.shiftid : null;

                        int graceMinutes = lateAttendanceRule!.graceminutes != null ? Convert.ToInt32(lateAttendanceRule!.graceminutes) : 0;
                        int billableMinutes = Math.Max(0, totalLateMinutes - graceMinutes);
                        bool isGrace = totalLateMinutes > 0 && billableMinutes == 0;

                        if (billableMinutes > 0 && tsShiftId.HasValue)
                        {
                            var shift = shifts.FirstOrDefault(s => s.shiftid == tsShiftId.Value);
                            if (shift != null)
                            {
                                TimeSpan startTime = (TimeSpan)shift.starttime;
                                TimeSpan endTime = (TimeSpan)shift.endtime;
                                decimal shiftHours = endTime > startTime
                                    ? (decimal)(endTime - startTime).TotalHours
                                    : (decimal)(endTime.Add(TimeSpan.FromHours(24)) - startTime).TotalHours;

                                decimal lateFixedDeduction = lateAttendanceRule!.fixeddeduction != null ? Convert.ToDecimal(lateAttendanceRule!.fixeddeduction) : 0m;
                                decimal latePercentageDeduction = lateAttendanceRule!.percentagededuction != null ? Convert.ToDecimal(lateAttendanceRule!.percentagededuction) : 0m;

                                decimal deductionPerMinute = 0m;
                                if (lateFixedDeduction > 0)
                                {
                                    deductionPerMinute = lateFixedDeduction;
                                }
                                else if (latePercentageDeduction > 0 && shiftHours > 0)
                                {
                                    // (BasicSalary / 30 days) / shiftHours / 60 minutes = cost per minute (100%)
                                    decimal perMinuteSalary = (basicSalary / 30m) / shiftHours / 60m;
                                    deductionPerMinute = Math.Round(perMinuteSalary * (latePercentageDeduction / 100m), 6);
                                }

                                decimal totalLateDeduction = Math.Round(deductionPerMinute * billableMinutes, 2);

                                if (totalLateDeduction > 0)
                                {
                                    var existingLateSql = @"SELECT id FROM payroll.lateattendance WHERE employeeid = @EmployeeId AND periodid = @PeriodId LIMIT 1;";
                                    var existingLate = await connection.QuerySingleOrDefaultAsync<Guid?>(existingLateSql, new { EmployeeId = employeeId, PeriodId = dto.PeriodId }, transaction);

                                    if (existingLate == null)
                                    {
                                        var insertLateSql = @"
                                            INSERT INTO payroll.lateattendance (
                                                employeeid, periodid, ruleid, lateminutes, deduction, isgrace, attendancedate
                                            ) VALUES (
                                                @EmployeeId, @PeriodId, @RuleId, @LateMinutes, @Deduction, FALSE, CURRENT_DATE
                                            ) RETURNING id;";
                                        await connection.QuerySingleOrDefaultAsync<Guid?>(insertLateSql, new
                                        {
                                            EmployeeId = employeeId,
                                            PeriodId = dto.PeriodId,
                                            RuleId = dto.LateAttendanceRuleId!.Value,
                                            LateMinutes = billableMinutes,
                                            Deduction = totalLateDeduction
                                        }, transaction);

                                        remainingSalary -= totalLateDeduction;
                                        result.LateAttendanceDeductionsApplied += 1;
                                        result.TotalLateAttendanceDeductionAmount += totalLateDeduction;
                                        result.LateAttendanceDeductionDetails.Add(new PayrollLateAttendanceDeductionDetailDto
                                        {
                                            EmployeeId = employeeId,
                                            TotalLateMinutes = totalLateMinutes,
                                            GraceMinutes = graceMinutes,
                                            BillableMinutes = billableMinutes,
                                            ShiftHours = shiftHours,
                                            DeductionPerMinute = deductionPerMinute,
                                            TotalDeduction = totalLateDeduction,
                                            IsGrace = false,
                                            NetSalaryAfterLateDeduction = remainingSalary
                                        });
                                    }
                                }
                            }
                        }
                        else if (isGrace)
                        {
                            // Employee was late but within grace period — log with zero deduction
                            var existingLateSql = @"SELECT id FROM payroll.lateattendance WHERE employeeid = @EmployeeId AND periodid = @PeriodId LIMIT 1;";
                            var existingLate = await connection.QuerySingleOrDefaultAsync<Guid?>(existingLateSql, new { EmployeeId = employeeId, PeriodId = dto.PeriodId }, transaction);
                            if (existingLate == null)
                            {
                                var insertGraceSql = @"
                                    INSERT INTO payroll.lateattendance (
                                        employeeid, periodid, ruleid, lateminutes, deduction, isgrace, attendancedate
                                    ) VALUES (
                                        @EmployeeId, @PeriodId, @RuleId, @LateMinutes, 0, TRUE, CURRENT_DATE
                                    ) RETURNING id;";
                                await connection.QuerySingleOrDefaultAsync<Guid?>(insertGraceSql, new
                                {
                                    EmployeeId = employeeId,
                                    PeriodId = dto.PeriodId,
                                    RuleId = dto.LateAttendanceRuleId!.Value,
                                    LateMinutes = totalLateMinutes
                                }, transaction);
                            }
                        }
                    }
                }

                // 0c. Leave Deduction
                if (leaveRule != null)
                {
                    var ts = timesheets.FirstOrDefault(t => t.employee_id == employeeId);
                    if (ts != null)
                    {
                        decimal paidDays = ts.paid_leave_days != null ? Convert.ToDecimal(ts.paid_leave_days) : 0m;
                        decimal unpaidDays = ts.unpaid_leave_days != null ? Convert.ToDecimal(ts.unpaid_leave_days) : 0m;
                        decimal halfPaidDays = ts.half_paid_leave_days != null ? Convert.ToDecimal(ts.half_paid_leave_days) : 0m;

                        decimal unpaidMultiplier = leaveRule!.unpaidmultiplier != null ? Convert.ToDecimal(leaveRule!.unpaidmultiplier) : 1.00m;
                        decimal halfPaidMultiplier = leaveRule!.halfpaidmultiplier != null ? Convert.ToDecimal(leaveRule!.halfpaidmultiplier) : 0.50m;

                        if (unpaidDays > 0 || halfPaidDays > 0)
                        {
                            decimal perDaySalary = basicSalary / 30m;
                            decimal unpaidDeduction = Math.Round(unpaidDays * unpaidMultiplier * perDaySalary, 2);
                            decimal halfPaidDeduction = Math.Round(halfPaidDays * halfPaidMultiplier * perDaySalary, 2);
                            decimal totalLeaveDeduction = unpaidDeduction + halfPaidDeduction;

                            if (totalLeaveDeduction > 0)
                            {
                                var existingLeaveSumSql = @"SELECT id FROM payroll.payrollleavesummary WHERE employeeid = @EmployeeId AND periodid = @PeriodId LIMIT 1;";
                                var existingLeaveSum = await connection.QuerySingleOrDefaultAsync<Guid?>(existingLeaveSumSql, new { EmployeeId = employeeId, PeriodId = dto.PeriodId }, transaction);

                                if (existingLeaveSum == null)
                                {
                                    var insertLeaveSql = @"
                                        INSERT INTO payroll.payrollleavesummary (
                                            employeeid, periodid, ruleid, paiddays, unpaiddays, halfpaiddays,
                                            unpaiddeduction, halfpaiddeduction, totaldeduction
                                        ) VALUES (
                                            @EmployeeId, @PeriodId, @RuleId, @PaidDays, @UnpaidDays, @HalfPaidDays,
                                            @UnpaidDeduction, @HalfPaidDeduction, @TotalDeduction
                                        ) RETURNING id;";
                                    await connection.QuerySingleOrDefaultAsync<Guid?>(insertLeaveSql, new
                                    {
                                        EmployeeId = employeeId,
                                        PeriodId = dto.PeriodId,
                                        RuleId = dto.LeaveRuleId!.Value,
                                        PaidDays = paidDays,
                                        UnpaidDays = unpaidDays,
                                        HalfPaidDays = halfPaidDays,
                                        UnpaidDeduction = unpaidDeduction,
                                        HalfPaidDeduction = halfPaidDeduction,
                                        TotalDeduction = totalLeaveDeduction
                                    }, transaction);

                                    remainingSalary -= totalLeaveDeduction;
                                    result.LeaveDeductionsApplied += 1;
                                    result.TotalLeaveDeductionAmount += totalLeaveDeduction;
                                    result.LeaveDeductionDetails.Add(new PayrollLeaveDeductionDetailDto
                                    {
                                        EmployeeId = employeeId,
                                        PaidDays = paidDays,
                                        UnpaidDays = unpaidDays,
                                        HalfPaidDays = halfPaidDays,
                                        PerDaySalary = Math.Round(perDaySalary, 2),
                                        UnpaidMultiplier = unpaidMultiplier,
                                        HalfPaidMultiplier = halfPaidMultiplier,
                                        UnpaidDeduction = unpaidDeduction,
                                        HalfPaidDeduction = halfPaidDeduction,
                                        TotalDeduction = totalLeaveDeduction,
                                        NetSalaryAfterLeaveDeduction = remainingSalary
                                    });
                                }
                            }
                        }
                    }
                }

                // 0d. Overtime Bonus
                if (overtimeRules.Any())
                {
                    var ts = timesheets.FirstOrDefault(t => t.employee_id == employeeId);
                    if (ts != null)
                    {
                        decimal regularOtHours = ts.regular_overtime_hours != null ? Convert.ToDecimal(ts.regular_overtime_hours) : 0m;
                        decimal weekendOtHours = ts.weekend_overtime_hours != null ? Convert.ToDecimal(ts.weekend_overtime_hours) : 0m;
                        decimal holidayOtHours = ts.holiday_overtime_hours != null ? Convert.ToDecimal(ts.holiday_overtime_hours) : 0m;

                        // Resolve shift hours for per-hour salary calculation
                        Guid? tsShiftId = ts.shiftid != null ? (Guid?)ts.shiftid : null;
                        decimal shiftHoursForOt = 8m; // fallback default
                        if (tsShiftId.HasValue)
                        {
                            var shift = shifts.FirstOrDefault(s => s.shiftid == tsShiftId.Value);
                            if (shift != null)
                            {
                                TimeSpan st = (TimeSpan)shift.starttime;
                                TimeSpan et = (TimeSpan)shift.endtime;
                                shiftHoursForOt = et > st
                                    ? (decimal)(et - st).TotalHours
                                    : (decimal)(et.Add(TimeSpan.FromHours(24)) - st).TotalHours;
                            }
                        }

                        // per-hour salary = (basicSalary / 30) / shiftHours
                        decimal perHourSalary = shiftHoursForOt > 0 ? Math.Round((basicSalary / 30m) / shiftHoursForOt, 6) : 0m;

                        decimal regularOtAmount = 0m;
                        decimal weekendOtAmount = 0m;
                        decimal holidayOtAmount = 0m;

                        var regularRule = overtimeRules.FirstOrDefault(r => ((string)r.overtimetype).Equals("Regular", StringComparison.OrdinalIgnoreCase));
                        var weekendRule = overtimeRules.FirstOrDefault(r => ((string)r.overtimetype).Equals("Weekend", StringComparison.OrdinalIgnoreCase));
                        var holidayRule = overtimeRules.FirstOrDefault(r => ((string)r.overtimetype).Equals("Holiday", StringComparison.OrdinalIgnoreCase));

                        // Calculate Regular overtime
                        if (regularRule != null && regularOtHours > 0)
                        {
                            decimal fixedAmt = regularRule!.fixedamount != null ? Convert.ToDecimal(regularRule!.fixedamount) : 0m;
                            decimal pct = regularRule!.percentage != null ? Convert.ToDecimal(regularRule!.percentage) : 0m;
                            if (fixedAmt > 0)
                                regularOtAmount = Math.Round(fixedAmt * regularOtHours, 2);
                            else if (pct > 0)
                                regularOtAmount = Math.Round(perHourSalary * (pct / 100m) * regularOtHours, 2);
                        }

                        // Calculate Weekend overtime
                        if (weekendRule != null && weekendOtHours > 0)
                        {
                            decimal fixedAmt = weekendRule!.fixedamount != null ? Convert.ToDecimal(weekendRule!.fixedamount) : 0m;
                            decimal pct = weekendRule!.percentage != null ? Convert.ToDecimal(weekendRule!.percentage) : 0m;
                            if (fixedAmt > 0)
                                weekendOtAmount = Math.Round(fixedAmt * weekendOtHours, 2);
                            else if (pct > 0)
                                weekendOtAmount = Math.Round(perHourSalary * (pct / 100m) * weekendOtHours, 2);
                        }

                        // Calculate Holiday overtime
                        if (holidayRule != null && holidayOtHours > 0)
                        {
                            decimal fixedAmt = holidayRule!.fixedamount != null ? Convert.ToDecimal(holidayRule!.fixedamount) : 0m;
                            decimal pct = holidayRule!.percentage != null ? Convert.ToDecimal(holidayRule!.percentage) : 0m;
                            if (fixedAmt > 0)
                                holidayOtAmount = Math.Round(fixedAmt * holidayOtHours, 2);
                            else if (pct > 0)
                                holidayOtAmount = Math.Round(perHourSalary * (pct / 100m) * holidayOtHours, 2);
                        }

                        decimal totalOvertimeBonus = regularOtAmount + weekendOtAmount + holidayOtAmount;

                        if (totalOvertimeBonus > 0)
                        {
                            // Insert one entry per OT type that has hours
                            var typesToInsert = new[]
                            {
                                (Rule: regularRule, Hours: regularOtHours, Amount: regularOtAmount),
                                (Rule: weekendRule, Hours: weekendOtHours, Amount: weekendOtAmount),
                                (Rule: holidayRule, Hours: holidayOtHours, Amount: holidayOtAmount)
                            };

                            foreach (var (otRule, otHours, otAmount) in typesToInsert)
                            {
                                if (otRule != null && otHours > 0 && otAmount > 0)
                                {
                                    var otRuleId = (Guid)otRule!.ruleid;
                                    var existingOtSql = @"SELECT overtimeid FROM payroll.overtimeentries WHERE employeeid = @EmployeeId AND periodid = @PeriodId AND ruleid = @RuleId LIMIT 1;";
                                    var existingOt = await connection.QuerySingleOrDefaultAsync<Guid?>(existingOtSql, new { EmployeeId = employeeId, PeriodId = dto.PeriodId, RuleId = otRuleId }, transaction);
                                    if (existingOt == null)
                                    {
                                        var insertOtSql = @"
                                            INSERT INTO payroll.overtimeentries (
                                                employeeid, periodid, ruleid, hoursworked, finalamount, entrystatus, overtimedate
                                            ) VALUES (
                                                @EmployeeId, @PeriodId, @RuleId, @HoursWorked, @FinalAmount, 'approved', CURRENT_DATE
                                            ) RETURNING overtimeid;";
                                        await connection.QuerySingleOrDefaultAsync<Guid?>(insertOtSql, new
                                        {
                                            EmployeeId = employeeId,
                                            PeriodId = dto.PeriodId,
                                            RuleId = otRuleId,
                                            HoursWorked = otHours,
                                            FinalAmount = otAmount
                                        }, transaction);
                                    }
                                }
                            }

                            remainingSalary += totalOvertimeBonus;
                            totalBonuses += totalOvertimeBonus;
                            result.OvertimeBonusApplied += 1;
                            result.TotalOvertimeBonusAmount += totalOvertimeBonus;
                            result.TotalRegularOvertimeAmount += regularOtAmount;
                            result.TotalWeekendOvertimeAmount += weekendOtAmount;
                            result.TotalHolidayOvertimeAmount += holidayOtAmount;
                            result.OvertimeDetails.Add(new PayrollOvertimeDetailDto
                            {
                                EmployeeId = employeeId,
                                RegularOvertimeHours = regularOtHours,
                                WeekendOvertimeHours = weekendOtHours,
                                HolidayOvertimeHours = holidayOtHours,
                                ShiftHours = shiftHoursForOt,
                                PerHourSalary = Math.Round(perHourSalary, 2),
                                RegularOvertimeAmount = regularOtAmount,
                                WeekendOvertimeAmount = weekendOtAmount,
                                HolidayOvertimeAmount = holidayOtAmount,
                                TotalOvertimeAmount = totalOvertimeBonus,
                                NetSalaryAfterOvertime = remainingSalary
                            });
                        }
                    }
                }

                // 1. Performance Bonus
                if (dto.CycleId != Guid.Empty && performanceRules.Any() && appraisals.Any(a => a.employeeid == employeeId))
                {
                    var appraisal = appraisals.First(a => a.employeeid == employeeId);
                    var finalRating = appraisal.finalrating != null ? Convert.ToDecimal(appraisal.finalrating) : 0m;
                    var matchedRule = performanceRules.FirstOrDefault(r => finalRating >= Convert.ToDecimal(r.minscore) && finalRating <= Convert.ToDecimal(r.maxscore));
                    if (matchedRule != null)
                    {
                        var ruleId = (Guid)matchedRule.ruleid;
                        var fixedAmount = matchedRule.fixedamount != null ? Convert.ToDecimal(matchedRule.fixedamount) : 0m;
                        var percentage = matchedRule.percentage != null ? Convert.ToDecimal(matchedRule.percentage) : 0m;
                        decimal bonusAmount = fixedAmount > 0 ? fixedAmount : (percentage > 0 ? Math.Round((basicSalary * percentage) / 100m, 2) : 0);

                        if (bonusAmount > 0)
                        {
                            var existingBonusSql = @"SELECT performancepayid FROM payroll.performancepay WHERE employeeid = @EmployeeId AND periodid = @PeriodId LIMIT 1;";
                            var existingBonus = await connection.QuerySingleOrDefaultAsync<Guid?>(existingBonusSql, new { EmployeeId = employeeId, PeriodId = dto.PeriodId }, transaction);
                            if (existingBonus == null)
                            {
                                var insertBonusSql = @"INSERT INTO payroll.performancepay (employeeid, periodid, ruleid, score, finalamount) VALUES (@EmployeeId, @PeriodId, @RuleId, @Score, @FinalAmount) RETURNING performancepayid;";
                                var bonusId = await connection.QuerySingleOrDefaultAsync<Guid?>(insertBonusSql, new { EmployeeId = employeeId, PeriodId = dto.PeriodId, RuleId = ruleId, Score = finalRating, FinalAmount = bonusAmount }, transaction);
                                if (bonusId != null && bonusId != Guid.Empty)
                                {
                                    remainingSalary += bonusAmount;
                                    totalBonuses += bonusAmount;
                                    result.PerformanceBonusApplied += 1;
                                    result.TotalPerformanceBonusAmount += bonusAmount;
                                    result.PerformanceBonusDetails.Add(new PayrollPerformanceBonusDetailDto { EmployeeId = employeeId, RuleId = ruleId, Score = finalRating, BonusAmount = bonusAmount, NetSalaryAfterBonus = remainingSalary });
                                }
                            }
                        }
                    }
                }

                // 2. General Bonus
                decimal generalBonusAmount = 0;
                Guid? appliedGeneralBonusRule = null;
                if (bonusRule != null)
                {
                    var isFixed = ((string)bonusRule.bonustype).ToLower() == "fixed";
                    var fixedAmount = bonusRule.bonusamount != null ? Convert.ToDecimal(bonusRule.bonusamount) : 0m;
                    var percentage = bonusRule.bonuspercentage != null ? Convert.ToDecimal(bonusRule.bonuspercentage) : 0m;
                    generalBonusAmount = isFixed ? fixedAmount : Math.Round((basicSalary * percentage) / 100m, 2);

                    if (generalBonusAmount > 0)
                    {
                        var existingBonusEntrySql = @"SELECT bonusid FROM payroll.bonusentries WHERE employeeid = @EmployeeId AND periodid = @PeriodId AND ruleid = @RuleId LIMIT 1;";
                        var existingEntry = await connection.QuerySingleOrDefaultAsync<Guid?>(existingBonusEntrySql, new { EmployeeId = employeeId, PeriodId = dto.PeriodId, RuleId = dto.BonusRuleId!.Value }, transaction);
                        if (existingEntry == null)
                        {
                            var insertBonusEntrySql = @"INSERT INTO payroll.bonusentries (organizationid, employeeid, periodid, ruleid, bonousamount) VALUES (@organizationId, @EmployeeId, @PeriodId, @RuleId, @BonousAmount) RETURNING bonusid;";
                            await connection.QuerySingleOrDefaultAsync<Guid?>(insertBonusEntrySql, new { organizationId, EmployeeId = employeeId, PeriodId = dto.PeriodId, RuleId = dto.BonusRuleId!.Value, BonousAmount = generalBonusAmount }, transaction);
                        }
                        appliedGeneralBonusRule = dto.BonusRuleId!.Value;
                    }
                }
                else
                {
                    var entry = bonusEntries.FirstOrDefault(e => e.employeeid == employeeId);
                    if (entry != null)
                    {
                        generalBonusAmount = Convert.ToDecimal(entry.bonousamount);
                        appliedGeneralBonusRule = entry.ruleid != null ? (Guid?)entry.ruleid : null;
                    }
                }

                if (generalBonusAmount > 0)
                {
                    remainingSalary += generalBonusAmount;
                    totalBonuses += generalBonusAmount;
                    result.GeneralBonusApplied += 1;
                    result.TotalGeneralBonusAmount += generalBonusAmount;
                    result.GeneralBonusDetails.Add(new PayrollGeneralBonusDetailDto { EmployeeId = employeeId, RuleId = appliedGeneralBonusRule, BonusAmount = generalBonusAmount, NetSalaryAfterBonus = remainingSalary });
                }

                // 3. Gratuity
                var grat = gratuities.FirstOrDefault(g => g.employeeid == employeeId);
                if (grat != null)
                {
                    var gratAmount = Convert.ToDecimal(grat.totalamount);
                    var gratTxId = (Guid)grat.id;
                    if (gratAmount > 0)
                    {
                        remainingSalary += gratAmount;
                        totalBonuses += gratAmount;
                        result.GratuityApplied += 1;
                        result.TotalGratuityAmount += gratAmount;
                        result.GratuityDetails.Add(new PayrollGratuityDetailDto { EmployeeId = employeeId, TransactionId = gratTxId, GratuityAmount = gratAmount, NetSalaryAfterGratuity = remainingSalary });
                    }
                }

                // 4. Provident Fund
                decimal employeePfAmount = 0;
                var pf = activeProvidentFunds.FirstOrDefault(p => p.employeeid == employeeId);
                if (pf != null && remainingSalary > 0)
                {
                    var pfId = (Guid)pf!.pfid;
                    var employeePct = Convert.ToDecimal(pf!.employeepct);
                    var employerPct = Convert.ToDecimal(pf!.employerpct);
                    var employeeAmt = Math.Round((remainingSalary * employeePct) / 100m, 2);
                    var employerAmt = Math.Round((remainingSalary * employerPct) / 100m, 2);

                    if (employeeAmt > 0 || employerAmt > 0)
                    {
                        var existingMonthlyTxSql = @"SELECT transactionid FROM payroll.pftransactions WHERE organizationid = @organizationId AND employeeid = @EmployeeId AND pfid = @PfId AND periodid = @PeriodId AND LOWER(COALESCE(transactiontype, '')) = 'monthly' LIMIT 1;";
                        var existingTxId = await connection.QuerySingleOrDefaultAsync<Guid?>(existingMonthlyTxSql, new { organizationId, EmployeeId = employeeId, PfId = pfId, PeriodId = dto.PeriodId }, transaction);
                        if (existingTxId == null || existingTxId == Guid.Empty)
                        {
                            var sumsSql = @"SELECT COALESCE(SUM(employeeamount), 0) AS totalemployeeamount, COALESCE(SUM(employeramount), 0) AS totalemployeramount FROM payroll.pftransactions WHERE organizationid = @organizationId AND employeeid = @EmployeeId AND pfid = @PfId;";
                            var sums = await connection.QuerySingleAsync<dynamic>(sumsSql, new { organizationId, EmployeeId = employeeId, PfId = pfId }, transaction);
                            decimal runningBalance = (decimal)sums.totalemployeeamount + (decimal)sums.totalemployeramount + employeeAmt + employerAmt;

                            var insertPfTxSql = @"INSERT INTO payroll.pftransactions (organizationid, employeeid, periodid, pfid, salarybasisamount, employeepct, employerpct, employeeamount, employeramount, runningbalance, transactiontype) VALUES (@organizationId, @EmployeeId, @PeriodId, @PfId, @SalaryBasisAmount, @EmployeePct, @EmployerPct, @EmployeeAmount, @EmployerAmount, @RunningBalance, 'monthly') RETURNING transactionid;";
                            var insertedTxId = await connection.QuerySingleOrDefaultAsync<Guid?>(insertPfTxSql, new { organizationId, EmployeeId = employeeId, PeriodId = dto.PeriodId, PfId = pfId, SalaryBasisAmount = remainingSalary, EmployeePct = employeePct, EmployerPct = employerPct, EmployeeAmount = employeeAmt, EmployerAmount = employerAmt, RunningBalance = runningBalance }, transaction);

                            if (insertedTxId != null && insertedTxId != Guid.Empty)
                            {
                                employeePfAmount = employeeAmt;
                                remainingSalary -= employeePfAmount;
                                result.ProvidentFundDeductionsApplied += 1;
                                result.TotalProvidentFundDeductionAmount += employeePfAmount;
                                result.ProvidentFundDeductionDetails.Add(new PayrollProvidentFundDeductionDetailDto { EmployeeId = employeeId, PfId = pfId, SalaryBasisAmount = remainingSalary + employeePfAmount, EmployeeAmount = employeePfAmount, EmployerAmount = employerAmt, NetSalaryAfterProvidentFundDeduction = remainingSalary });
                            }
                        }
                    }
                }

                // 5. TAX Calculation
                decimal monthlyGross = basicSalary + totalBonuses;
                decimal pfExemptAmount = 0;

                if (pfTaxRule != null && employeePfAmount > 0)
                {
                    string taxability = pfTaxRule!.taxability?.ToString() ?? "FullyTaxable";
                    if (taxability.Equals("Exempt", StringComparison.OrdinalIgnoreCase))
                    {
                        pfExemptAmount = employeePfAmount;
                    }
                    else if (taxability.Equals("PartiallyExempt", StringComparison.OrdinalIgnoreCase))
                    {
                        decimal limitAmount = pfTaxRule!.limitamount != null ? Convert.ToDecimal(pfTaxRule!.limitamount) : 0m;
                        decimal limitPercentage = pfTaxRule!.limitpercentage != null ? Convert.ToDecimal(pfTaxRule!.limitpercentage) : 0m;

                        if (limitAmount > 0)
                        {
                            pfExemptAmount = Math.Min(employeePfAmount, limitAmount);
                        }
                        else if (limitPercentage > 0)
                        {
                            pfExemptAmount = Math.Min(employeePfAmount, Math.Round((basicSalary * limitPercentage) / 100m, 2));
                        }
                    }
                }

                decimal monthlyTaxableIncome = monthlyGross - pfExemptAmount;
                decimal annualTaxableIncome = (basicSalary * 12) + totalBonuses - (pfExemptAmount * 12);
                decimal taxAmountAnnual = 0;
                decimal monthlyTax = 0;

                if (categoryId.HasValue && annualTaxableIncome > 0)
                {
                    var empSlabs = taxSlabs.Where(s => (Guid)s.categoryid == categoryId.Value).OrderBy(s => (int)s.slaborder).ToList();
                    var applicableSlab = empSlabs.FirstOrDefault(s => annualTaxableIncome >= Convert.ToDecimal(s.minincome) && (s.maxincome == null || annualTaxableIncome <= Convert.ToDecimal(s.maxincome)));
                    if (applicableSlab != null)
                    {
                        decimal minIncome = Convert.ToDecimal(applicableSlab.minincome);
                        decimal fixedAmt = Convert.ToDecimal(applicableSlab.fixedamount);
                        decimal pct = Convert.ToDecimal(applicableSlab.percentage);

                        taxAmountAnnual = fixedAmt + ((annualTaxableIncome - minIncome) * (pct / 100m));
                        monthlyTax = Math.Round(taxAmountAnnual / 12m, 2);
                    }
                }

                if (monthlyTax > 0)
                {
                    var existingTaxSql = @"SELECT id FROM payroll.taxtransactions WHERE organizationid = @organizationId AND employeeid = @EmployeeId AND periodid = @PeriodId LIMIT 1;";
                    var existingTax = await connection.QuerySingleOrDefaultAsync<Guid?>(existingTaxSql, new { organizationId, EmployeeId = employeeId, PeriodId = dto.PeriodId }, transaction);
                    if (existingTax == null)
                    {
                        var insertTaxSql = @"INSERT INTO payroll.taxtransactions (organizationid, employeeid, periodid, categoryid, grossincome, taxableincome, taxamount, monthlytax) VALUES (@organizationId, @EmployeeId, @PeriodId, @CategoryId, @GrossIncome, @TaxableIncome, @TaxAmount, @MonthlyTax) RETURNING id;";
                        await connection.QuerySingleOrDefaultAsync<Guid?>(insertTaxSql, new { organizationId, EmployeeId = employeeId, PeriodId = dto.PeriodId, CategoryId = categoryId, GrossIncome = monthlyGross, TaxableIncome = monthlyTaxableIncome, TaxAmount = Math.Round(taxAmountAnnual, 2), MonthlyTax = monthlyTax }, transaction);

                        remainingSalary -= monthlyTax;
                        result.TaxDeductionsApplied += 1;
                        result.TotalTaxDeductionAmount += monthlyTax;
                        result.TaxDeductionDetails.Add(new PayrollTaxDeductionDetailDto { EmployeeId = employeeId, MonthlyGross = monthlyGross, PFExemptAmount = pfExemptAmount, MonthlyTaxableIncome = monthlyTaxableIncome, AnnualTaxableIncome = annualTaxableIncome, TaxAmount = Math.Round(taxAmountAnnual, 2), MonthlyTax = monthlyTax, NetSalaryAfterTaxDeduction = remainingSalary });
                    }
                }

                // 6. Loans
                var empLoans = loans.Where(l => l.employeeid == employeeId).ToList();
                foreach (var empLoan in empLoans)
                {
                    var loanId = (Guid)empLoan.loanid;
                    var monthlyInstallment = Convert.ToDecimal(empLoan.monthlyinstallment);
                    var remainingBefore = Convert.ToDecimal(empLoan.remainingamount);
                    var installmentAmount = Math.Min(monthlyInstallment, remainingBefore);

                    if (installmentAmount > 0)
                    {
                        var installmentNoSql = @"SELECT COALESCE(MAX(installmentnumber), 0) FROM payroll.loanpayments WHERE organizationid = @organizationId AND loanid = @loanId AND employeeid = @employeeId;";
                        var previousInstallmentNumber = await connection.QuerySingleAsync<int>(installmentNoSql, new { organizationId, loanId, employeeId }, transaction);
                        var nextInstallmentNumber = previousInstallmentNumber + 1;

                        var updateLoanSql = @"UPDATE payroll.loans SET remainingamount = GREATEST(0, remainingamount - @InstallmentAmount), paidinstallments = @InstallmentNumber, loanstatus = CASE WHEN (remainingamount - @InstallmentAmount) <= 0 THEN 'completed' ELSE loanstatus END, disbursementstatus = CASE WHEN (remainingamount - @InstallmentAmount) <= 0 THEN 'recovered' ELSE disbursementstatus END, updatedat = CURRENT_TIMESTAMP WHERE loanid = @loanId AND organizationid = @organizationId AND employeeid = @employeeId AND LOWER(COALESCE(loanstatus, '')) = 'active' AND COALESCE(remainingamount, 0) > 0 RETURNING remainingamount;";
                        var remainingAfter = await connection.QuerySingleOrDefaultAsync<decimal?>(updateLoanSql, new { loanId, organizationId, employeeId, InstallmentAmount = installmentAmount, InstallmentNumber = nextInstallmentNumber }, transaction);

                        if (remainingAfter != null)
                        {
                            var insertPaymentSql = @"INSERT INTO payroll.loanpayments (organizationid, loanid, employeeid, periodid, installmentnumber, installmentamount, remainingamount, repaymenttype, paymentdate, paymentmethod, paymentstatus) VALUES (@organizationId, @loanId, @employeeId, @PeriodId, @InstallmentNumber, @InstallmentAmount, @RemainingAmount, 'installment', CURRENT_DATE, 'payroll_deduction', 'deducted');";
                            await connection.ExecuteAsync(insertPaymentSql, new { organizationId, loanId, employeeId, PeriodId = dto.PeriodId, InstallmentNumber = nextInstallmentNumber, InstallmentAmount = installmentAmount, RemainingAmount = remainingAfter.Value }, transaction);

                            remainingSalary -= installmentAmount;
                            result.LoanDeductionsApplied += 1;
                            result.TotalLoanDeductionAmount += installmentAmount;
                            result.LoanDeductionDetails.Add(new PayrollLoanDeductionDetailDto { EmployeeId = employeeId, LoanId = loanId, BasicSalary = basicSalary, InstallmentAmount = installmentAmount, NetSalaryAfterLoanDeduction = remainingSalary, RemainingLoanAmountBefore = remainingBefore, RemainingLoanAmountAfter = remainingAfter.Value, InstallmentNumber = nextInstallmentNumber });
                        }
                    }
                }

                // 7. Salary Advance
                var empAdvances = advances.Where(a => a.employeeid == employeeId).ToList();
                foreach (var empAdvance in empAdvances)
                {
                    var advanceId = (Guid)empAdvance.advanceid;
                    var advanceAmount = Convert.ToDecimal(empAdvance.amount);

                    if (advanceAmount > 0)
                    {
                        var updateAdvanceSql = @"UPDATE payroll.salaryadvance SET advancestatus = 'deducted', deductedperiodid = @PeriodId, deductedat = CURRENT_TIMESTAMP, updatedat = CURRENT_TIMESTAMP WHERE advanceid = @AdvanceId AND organizationid = @organizationId AND employeeid = @EmployeeId AND LOWER(COALESCE(advancestatus, '')) = 'disbursed' AND deductedperiodid IS NULL RETURNING advanceid;";
                        var updatedAdvanceId = await connection.QuerySingleOrDefaultAsync<Guid?>(updateAdvanceSql, new { PeriodId = dto.PeriodId, AdvanceId = advanceId, organizationId, EmployeeId = employeeId }, transaction);

                        if (updatedAdvanceId != null)
                        {
                            remainingSalary -= advanceAmount;
                            result.SalaryAdvanceDeductionsApplied += 1;
                            result.TotalSalaryAdvanceDeductionAmount += advanceAmount;
                            result.SalaryAdvanceDeductionDetails.Add(new PayrollSalaryAdvanceDeductionDetailDto { EmployeeId = employeeId, AdvanceId = advanceId, DeductedAmount = advanceAmount, NetSalaryAfterSalaryAdvanceDeduction = remainingSalary });
                        }
                    }
                }

                // --- INSERT OR UPDATE PAYROLL RESULTS ---
                decimal empTotalDeductions = basicSalary + totalBonuses - remainingSalary;

                var empLoanDeductions = JsonSerializer.Serialize(result.LoanDeductionDetails.Where(x => x.EmployeeId == employeeId).ToList());
                var empSalaryAdvances = JsonSerializer.Serialize(result.SalaryAdvanceDeductionDetails.Where(x => x.EmployeeId == employeeId).ToList());
                var empPfDeductions = JsonSerializer.Serialize(result.ProvidentFundDeductionDetails.Where(x => x.EmployeeId == employeeId).ToList());
                var empTaxDeductions = JsonSerializer.Serialize(result.TaxDeductionDetails.Where(x => x.EmployeeId == employeeId).ToList());
                var empAttendanceDeductions = JsonSerializer.Serialize(result.AttendanceDeductionDetails.Where(x => x.EmployeeId == employeeId).ToList());
                var empLateAttendanceDeductions = JsonSerializer.Serialize(result.LateAttendanceDeductionDetails.Where(x => x.EmployeeId == employeeId).ToList());
                var empLeaveDeductions = JsonSerializer.Serialize(result.LeaveDeductionDetails.Where(x => x.EmployeeId == employeeId).ToList());
                var empPerformanceBonuses = JsonSerializer.Serialize(result.PerformanceBonusDetails.Where(x => x.EmployeeId == employeeId).ToList());
                var empGeneralBonuses = JsonSerializer.Serialize(result.GeneralBonusDetails.Where(x => x.EmployeeId == employeeId).ToList());
                var empGratuity = JsonSerializer.Serialize(result.GratuityDetails.Where(x => x.EmployeeId == employeeId).ToList());
                var empOvertime = JsonSerializer.Serialize(result.OvertimeDetails.Where(x => x.EmployeeId == employeeId).ToList());

                var upsertResultSql = @"
                    INSERT INTO payroll.payrollresults (
                        organizationid, employeeid, periodid, basicsalary, totaldeductions, totalbonuses, netsalary,
                        loandeductions, salaryadvancedeductions, pfdeductions, taxdeductions,
                        attendancedeductions, lateattendancedeductions, leavedeductions,
                        performancebonuses, generalbonuses, gratuity, overtimedetails, calculatedat
                    ) VALUES (
                        @OrganizationId, @EmployeeId, @PeriodId, @BasicSalary, @TotalDeductions, @TotalBonuses, @NetSalary,
                        @LoanDeductions::jsonb, @SalaryAdvanceDeductions::jsonb, @PfDeductions::jsonb, @TaxDeductions::jsonb,
                        @AttendanceDeductions::jsonb, @LateAttendanceDeductions::jsonb, @LeaveDeductions::jsonb,
                        @PerformanceBonuses::jsonb, @GeneralBonuses::jsonb, @Gratuity::jsonb, @OvertimeDetails::jsonb, CURRENT_TIMESTAMP
                    )
                    ON CONFLICT (organizationid, employeeid, periodid) 
                    DO UPDATE SET 
                        basicsalary = EXCLUDED.basicsalary,
                        totaldeductions = EXCLUDED.totaldeductions,
                        totalbonuses = EXCLUDED.totalbonuses,
                        netsalary = EXCLUDED.netsalary,
                        loandeductions = EXCLUDED.loandeductions,
                        salaryadvancedeductions = EXCLUDED.salaryadvancedeductions,
                        pfdeductions = EXCLUDED.pfdeductions,
                        taxdeductions = EXCLUDED.taxdeductions,
                        attendancedeductions = EXCLUDED.attendancedeductions,
                        lateattendancedeductions = EXCLUDED.lateattendancedeductions,
                        leavedeductions = EXCLUDED.leavedeductions,
                        performancebonuses = EXCLUDED.performancebonuses,
                        generalbonuses = EXCLUDED.generalbonuses,
                        gratuity = EXCLUDED.gratuity,
                        overtimedetails = EXCLUDED.overtimedetails,
                        calculatedat = CURRENT_TIMESTAMP;";

                await connection.ExecuteAsync(upsertResultSql, new
                {
                    OrganizationId = organizationId,
                    EmployeeId = employeeId,
                    PeriodId = dto.PeriodId,
                    BasicSalary = basicSalary,
                    TotalDeductions = empTotalDeductions,
                    TotalBonuses = totalBonuses,
                    NetSalary = remainingSalary,
                    LoanDeductions = empLoanDeductions,
                    SalaryAdvanceDeductions = empSalaryAdvances,
                    PfDeductions = empPfDeductions,
                    TaxDeductions = empTaxDeductions,
                    AttendanceDeductions = empAttendanceDeductions,
                    LateAttendanceDeductions = empLateAttendanceDeductions,
                    LeaveDeductions = empLeaveDeductions,
                    PerformanceBonuses = empPerformanceBonuses,
                    GeneralBonuses = empGeneralBonuses,
                    Gratuity = empGratuity,
                    OvertimeDetails = empOvertime
                }, transaction);
            }

            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    #endregion
    }

    public async Task<LoanPaymentDto?> GetLoanPaymentByIdAsync(Guid paymentId, Guid organizationId)
    {
        var sql = @"
            SELECT 
                id AS Id,
                loanid AS LoanId,
                employeeid AS EmployeeId,
                periodid AS PeriodId,
                installmentnumber AS InstallmentNumber,
                installmentamount AS InstallmentAmount,
                remainingamount AS RemainingAmount,
                repaymenttype AS RepaymentType,
                paymentdate AS PaymentDate,
                paymentmethod AS PaymentMethod,
                paymentstatus AS PaymentStatus,
                createdat AS CreatedAt
            FROM payroll.loanpayments
            WHERE id = @paymentId AND organizationid = @organizationId;";

        return await QuerySingleOrDefaultAsync<LoanPaymentDto>(sql, new { paymentId, organizationId });
    }

    public async Task<bool> UpdateLoanPaymentAsync(Guid organizationId, UpdateLoanPaymentDto dto)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Get the old record to calculate the difference
            var oldRecordSql = @"
                SELECT installmentamount, loanid 
                FROM payroll.loanpayments 
                WHERE id = @Id AND organizationid = @organizationId FOR UPDATE;";

            var oldRecord = await connection.QuerySingleOrDefaultAsync(oldRecordSql, new { dto.Id, organizationId }, transaction);
            if (oldRecord == null)
            {
                transaction.Rollback();
                return false;
            }

            decimal oldAmount = oldRecord.installmentamount;
            decimal diff = oldAmount - dto.InstallmentAmount; // If new is larger, diff is negative

            // 2. Update the main loan table
            var updateLoanSql = @"
                UPDATE payroll.loans
                SET 
                    remainingamount = GREATEST(0, remainingamount + @diff),
                    paidinstallments = @InstallmentNumber,
                    loanstatus = CASE 
                        WHEN (remainingamount + @diff) <= 0 THEN 'completed' 
                        ELSE 'active' 
                    END,
                    disbursementstatus = CASE
                        WHEN (remainingamount + @diff) <= 0 THEN 'recovered'
                        ELSE disbursementstatus
                    END,
                    updatedat = CURRENT_TIMESTAMP
                WHERE loanid = @LoanId AND organizationid = @organizationId
                RETURNING remainingamount;";

            var newLoanRemaining = await connection.QuerySingleOrDefaultAsync<decimal?>(updateLoanSql, new
            {
                diff,
                dto.InstallmentNumber,
                dto.LoanId,
                organizationId
            }, transaction);

            // 3. Update the payment record
            var updatePaymentSql = @"
                UPDATE payroll.loanpayments
                SET 
                    periodid = @PeriodId,
                    installmentnumber = @InstallmentNumber,
                    installmentamount = @InstallmentAmount,
                    remainingamount = @RemainingAmount,
                    repaymenttype = @RepaymentType,
                    paymentdate = @PaymentDate,
                    paymentmethod = @PaymentMethod,
                    paymentstatus = @PaymentStatus,
                    updatedat = CURRENT_TIMESTAMP
                WHERE id = @Id AND organizationid = @organizationId;";

            await connection.ExecuteAsync(updatePaymentSql, new
            {
                dto.PeriodId,
                dto.InstallmentNumber,
                dto.InstallmentAmount,
                RemainingAmount = newLoanRemaining ?? 0,
                dto.RepaymentType,
                dto.PaymentDate,
                dto.PaymentMethod,
                dto.PaymentStatus,
                dto.Id,
                organizationId
            }, transaction);

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<PagedResult<LoanDto>> GetAllLoansAsync(Guid organizationId, LoanFilterDto filter)
    {
        var conditions = new List<string> { "l.organizationid = @organizationId" };
        if (!string.IsNullOrEmpty(filter.SearchTerm))
            conditions.Add("(e.firstname ILIKE @SearchTerm OR e.lastname ILIKE @SearchTerm OR (e.firstname || ' ' || e.lastname) ILIKE @SearchTerm)");
        if (!string.IsNullOrEmpty(filter.RequestStatus))
        {
            if (filter.RequestStatus.ToLower() == "pending")
            {
                conditions.Add("LOWER(l.requeststatus) IN ('pending', 'accepted', 'approved')");
            }
            else
            {
                conditions.Add("LOWER(l.requeststatus) = LOWER(@RequestStatus)");
            }
        }
        conditions.Add("LOWER(l.disbursementstatus) = 'pending'");

        var whereClause = string.Join(" AND ", conditions);

        var countSql = $@"
            SELECT COUNT(1)
            FROM payroll.loans l
            JOIN employee.Employees e ON l.employeeid = e.employeeid
            WHERE {whereClause};";

        var sql = $@"
            SELECT 
                l.loanid AS LoanId,
                l.organizationid AS OrganizationId,
                l.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                l.ruleid AS RuleId,
                lr.rulename AS RuleName,
                l.referenceid AS ReferenceId,
                l.principalamount AS PrincipalAmount,
                l.totalinterest AS TotalInterest,
                l.totalamount AS TotalAmount,
                l.monthlyinstallment AS MonthlyInstallment,
                l.remainingamount AS RemainingAmount,
                l.paidamount AS PaidAmount,
                l.totalinstallments AS TotalInstallments,
                l.paidinstallments AS PaidInstallments,
                l.repaymenttype AS RepaymentType,
                l.requeststatus AS RequestStatus,
                l.loanstatus AS LoanStatus,
                l.description AS Description,
                l.startdate AS StartDate,
                l.enddate AS EndDate,
                l.createdat AS CreatedAt
            FROM payroll.loans l
            JOIN employee.Employees e ON l.employeeid = e.employeeid
            LEFT JOIN payroll.loanrules lr ON l.ruleid = lr.ruleid
            WHERE {whereClause}
            ORDER BY l.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        var parameters = new
        {
            organizationId,
            SearchTerm = !string.IsNullOrEmpty(filter.SearchTerm) ? $"%{filter.SearchTerm.Trim()}%" : null,
            filter.RequestStatus,
            PageSize = filter.PageSize,
            Offset = (filter.Page - 1) * filter.PageSize
        };

        var totalCount = await QuerySingleAsync<int>(countSql, parameters);
        var data = await QueryAsync<LoanDto>(sql, parameters);

        return new PagedResult<LoanDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<LoanDto?> GetLoanByIdAndEmployeeAsync(Guid loanId, Guid organizationId, Guid employeeId)
    {
        var sql = @"
            SELECT 
                l.loanid AS LoanId,
                l.organizationid AS OrganizationId,
                l.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                l.ruleid AS RuleId,
                lr.rulename AS RuleName,
                l.referenceid AS ReferenceId,
                l.principalamount AS PrincipalAmount,
                l.totalinterest AS TotalInterest,
                l.totalamount AS TotalAmount,
                l.monthlyinstallment AS MonthlyInstallment,
                l.remainingamount AS RemainingAmount,
                l.paidamount AS PaidAmount,
                l.totalinstallments AS TotalInstallments,
                l.paidinstallments AS PaidInstallments,
                l.repaymenttype AS RepaymentType,
                l.requeststatus AS RequestStatus,
                l.loanstatus AS LoanStatus,
                l.description AS Description,
                l.startdate AS StartDate,
                l.enddate AS EndDate,
                l.createdat AS CreatedAt
            FROM payroll.loans l
            JOIN employee.Employees e ON l.employeeid = e.employeeid
            LEFT JOIN payroll.loanrules lr ON l.ruleid = lr.ruleid
            WHERE l.loanid = @loanId AND l.organizationid = @organizationId AND l.employeeid = @employeeId;";

        return await QuerySingleOrDefaultAsync<LoanDto>(sql, new { loanId, organizationId, employeeId });
    }

    public async Task<LoanDto?> UpdateLoanRequestAsync(Guid loanId, Guid organizationId, Guid employeeId, CreateLoanRequestDto dto)
    {
        var sql = @"
            UPDATE payroll.loans
            SET 
                ruleid = @RuleId,
                principalamount = @PrincipalAmount,
                totalinterest = @TotalInterest,
                totalinstallments = @TotalInstallments,
                monthlyinstallment = @MonthlyInstallment,
                description = @Description,
                repaymenttype = @RepaymentType,
                enddate = @ReturnDate,
                updatedat = CURRENT_TIMESTAMP
            WHERE loanid = @loanId AND organizationid = @organizationId AND employeeid = @employeeId AND requeststatus IN ('pending', 'approved')
            RETURNING loanid;";

        var parameters = new
        {
            loanId,
            organizationId,
            employeeId,
            dto.RuleId,
            dto.PrincipalAmount,
            dto.TotalInterest,
            dto.TotalInstallments,
            dto.MonthlyInstallment,
            dto.Description,
            dto.RepaymentType,
            dto.ReturnDate
        };

        var resultId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (resultId == null) return null;

        return await GetLoanByIdAsync(resultId.Value, organizationId);
    }

    public async Task<bool> DeleteLoanRequestAsync(Guid loanId, Guid organizationId, Guid employeeId)
    {
        var sql = @"
            DELETE FROM payroll.loans
            WHERE loanid = @loanId AND organizationid = @organizationId AND employeeid = @employeeId AND requeststatus IN ('pending', 'approved')
            RETURNING loanid;";

        var resultId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { loanId, organizationId, employeeId });
        return resultId != null;
    }

    public async Task<PagedResult<MyLoanPaymentDto>> GetLoanHistoryAsync(Guid organizationId, Guid employeeId, MyLoanHistoryFilterDto filter)
    {
        var conditions = new List<string> { "organizationid = @organizationId", "employeeid = @employeeId" };
        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);
        parameters.Add("employeeId", employeeId);

        if (filter.LoanId.HasValue)
        {
            conditions.Add("loanid = @LoanId");
            parameters.Add("LoanId", filter.LoanId.Value);
        }

        if (filter.PeriodId.HasValue)
        {
            conditions.Add("periodid = @PeriodId");
            parameters.Add("PeriodId", filter.PeriodId.Value);
        }

        var whereClause = string.Join(" AND ", conditions);
        var offset = (filter.Page - 1) * filter.PageSize;

        var sql = $@"
            SELECT 
                id AS Id,
                loanid AS LoanId,
                employeeid AS EmployeeId,
                periodid AS PeriodId,
                installmentnumber AS InstallmentNumber,
                installmentamount AS InstallmentAmount,
                remainingamount AS RemainingAmount,
                repaymenttype AS RepaymentType,
                paymentdate AS PaymentDate,
                paymentmethod AS PaymentMethod,
                paymentstatus AS PaymentStatus,
                createdat AS CreatedAt,
                COUNT(*) OVER() as TotalCount
            FROM payroll.loanpayments
            WHERE {whereClause}
            ORDER BY createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", offset);

        var data = await QueryAsync<MyLoanPaymentDto>(sql, parameters);
        var totalCount = data.FirstOrDefault()?.TotalCount ?? 0;

        return new PagedResult<MyLoanPaymentDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IEnumerable<LoanReferenceDto>> GetMyLoanReferencesAsync(Guid organizationId, Guid employeeId)
    {
        var sql = @"
            SELECT 
                loanid AS LoanId, 
                referenceid AS ReferenceId 
            FROM payroll.loans 
            WHERE employeeid = @EmployeeId 
              AND organizationid = @OrganizationId 
              AND requeststatus != 'pending' 
              AND disbursementstatus != 'pending'
            ORDER BY createdat DESC;";

        return await QueryAsync<LoanReferenceDto>(sql, new { EmployeeId = employeeId, OrganizationId = organizationId });
    }

    // ─── Social Security Config ──────────────────────────────────────────────

    public async Task<IEnumerable<SocialSecurityJurisdictionDto>> GetSocialSecurityJurisdictionsAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                jurisdictionid AS JurisdictionId,
                organizationid AS OrganizationId,
                jurisdictioncode AS JurisdictionCode,
                jurisdictionname AS JurisdictionName,
                countrycode AS CountryCode,
                currency AS Currency,
                timezone AS Timezone,
                COALESCE(isdefault, false) AS IsDefault,
                COALESCE(isactive, true) AS IsActive,
                metadata::text AS Metadata
            FROM payroll.socialsecurityjurisdictions
            WHERE organizationid = @organizationId
              AND isdeleted = false
            ORDER BY isdefault DESC, jurisdictionname ASC;";

        return await QueryAsync<SocialSecurityJurisdictionDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<SocialSecurityAuthorityDto>> GetSocialSecurityAuthoritiesAsync(Guid organizationId, Guid? jurisdictionId)
    {
        var sql = @"
            SELECT
                authorityid AS AuthorityId,
                organizationid AS OrganizationId,
                jurisdictionid AS JurisdictionId,
                authoritycode AS AuthorityCode,
                authorityname AS AuthorityName,
                portalurl AS PortalUrl,
                remittancefrequency AS RemittanceFrequency,
                filingmode AS FilingMode,
                paymentmode AS PaymentMode,
                dueoffsetdays AS DueOffsetDays,
                COALESCE(isactive, true) AS IsActive,
                metadata::text AS Metadata
            FROM payroll.socialsecurityauthorities
            WHERE organizationid = @organizationId
              AND isdeleted = false
              AND (@jurisdictionId IS NULL OR jurisdictionid = @jurisdictionId)
            ORDER BY authorityname ASC;";

        return await QueryAsync<SocialSecurityAuthorityDto>(sql, new { organizationId, jurisdictionId });
    }

    public async Task<IEnumerable<SocialSecuritySchemeDto>> GetSocialSecuritySchemesAsync(Guid organizationId, Guid? jurisdictionId, Guid? authorityId)
    {
        var sql = @"
            SELECT
                schemeid AS SchemeId,
                organizationid AS OrganizationId,
                jurisdictionid AS JurisdictionId,
                authorityid AS AuthorityId,
                schemecode AS SchemeCode,
                schemename AS SchemeName,
                schemetype AS SchemeType,
                mandatorymode AS MandatoryMode,
                COALESCE(employeemandatory, true) AS EmployeeMandatory,
                COALESCE(employermandatory, true) AS EmployerMandatory,
                COALESCE(allowvoluntarytopup, false) AS AllowVoluntaryTopup,
                COALESCE(allowcrossborder, false) AS AllowCrossBorder,
                versionstartdate AS VersionStartDate,
                versionenddate AS VersionEndDate,
                COALESCE(isactive, true) AS IsActive,
                metadata::text AS Metadata
            FROM payroll.socialsecurityschemes
            WHERE organizationid = @organizationId
              AND isdeleted = false
              AND (@jurisdictionId IS NULL OR jurisdictionid = @jurisdictionId)
              AND (@authorityId IS NULL OR authorityid = @authorityId)
            ORDER BY schemename ASC, versionstartdate DESC;";

        return await QueryAsync<SocialSecuritySchemeDto>(sql, new { organizationId, jurisdictionId, authorityId });
    }

    public async Task<IEnumerable<SocialSecurityRuleDto>> GetSocialSecurityRulesAsync(Guid organizationId, Guid? schemeId)
    {
        var sql = @"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                schemeid AS SchemeId,
                rulename AS RuleName,
                description AS Description,
                COALESCE(contributionbasis, 'gross') AS ContributionBasis,
                COALESCE(employeedefaultpct, 0) AS EmployeeDefaultPct,
                COALESCE(employerdefaultpct, 0) AS EmployerDefaultPct,
                employeefixedamount AS EmployeeFixedAmount,
                employerfixedamount AS EmployerFixedAmount,
                minsalarylimit AS MinSalaryLimit,
                maxsalarylimit AS MaxSalaryLimit,
                annualsalarycap AS AnnualSalaryCap,
                effectivefrom AS EffectiveFrom,
                effectiveto AS EffectiveTo,
                COALESCE(isactive, true) AS IsActive
            FROM payroll.socialsecurityrules
            WHERE organizationid = @organizationId
              AND isdeleted = false
              AND (@schemeId IS NULL OR schemeid = @schemeId)
            ORDER BY rulename ASC, effectivefrom DESC;";

        return await QueryAsync<SocialSecurityRuleDto>(sql, new { organizationId, schemeId });
    }

    public async Task<SocialSecurityJurisdictionDto> CreateSocialSecurityJurisdictionAsync(Guid organizationId, CreateSocialSecurityJurisdictionDto dto)
    {
        var existing = await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1
                FROM payroll.socialsecurityjurisdictions
                WHERE organizationid = @organizationId
                  AND lower(jurisdictioncode) = lower(@JurisdictionCode)
                  AND isdeleted = false
            );", new { organizationId, dto.JurisdictionCode });

        if (existing)
        {
            throw new InvalidOperationException("A jurisdiction with the same code already exists.");
        }

        var id = await QuerySingleAsync<Guid>(@"
            INSERT INTO payroll.socialsecurityjurisdictions
                (organizationid, jurisdictioncode, jurisdictionname, countrycode, currency, timezone, isdefault, isactive, metadata, isdeleted)
            VALUES
                (@organizationId, @JurisdictionCode, @JurisdictionName, @CountryCode, @Currency, @Timezone, @IsDefault, @IsActive,
                 CASE WHEN @Metadata IS NULL OR btrim(@Metadata) = '' THEN NULL ELSE CAST(@Metadata AS jsonb) END,
                 false)
            RETURNING jurisdictionid;", new
        {
            organizationId,
            JurisdictionCode = dto.JurisdictionCode.Trim(),
            JurisdictionName = dto.JurisdictionName.Trim(),
            CountryCode = string.IsNullOrWhiteSpace(dto.CountryCode) ? null : dto.CountryCode.Trim(),
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? null : dto.Currency.Trim(),
            Timezone = string.IsNullOrWhiteSpace(dto.Timezone) ? null : dto.Timezone.Trim(),
            dto.IsDefault,
            dto.IsActive,
            dto.Metadata
        });

        return (await GetSocialSecurityJurisdictionByIdAsync(id, organizationId))!;
    }

    public async Task<SocialSecurityJurisdictionDto?> UpdateSocialSecurityJurisdictionAsync(Guid id, Guid organizationId, UpdateSocialSecurityJurisdictionDto dto)
    {
        var duplicate = await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1
                FROM payroll.socialsecurityjurisdictions
                WHERE organizationid = @organizationId
                  AND lower(jurisdictioncode) = lower(@JurisdictionCode)
                  AND jurisdictionid <> @id
                  AND isdeleted = false
            );", new { organizationId, id, dto.JurisdictionCode });

        if (duplicate)
        {
            throw new InvalidOperationException("A jurisdiction with the same code already exists.");
        }

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(@"
            UPDATE payroll.socialsecurityjurisdictions
            SET
                jurisdictioncode = @JurisdictionCode,
                jurisdictionname = @JurisdictionName,
                countrycode = @CountryCode,
                currency = @Currency,
                timezone = @Timezone,
                isdefault = @IsDefault,
                isactive = @IsActive,
                metadata = CASE WHEN @Metadata IS NULL OR btrim(@Metadata) = '' THEN NULL ELSE CAST(@Metadata AS jsonb) END,
                updatedat = current_timestamp
            WHERE jurisdictionid = @id
              AND organizationid = @organizationId
              AND isdeleted = false
            RETURNING jurisdictionid;", new
        {
            id,
            organizationId,
            JurisdictionCode = dto.JurisdictionCode.Trim(),
            JurisdictionName = dto.JurisdictionName.Trim(),
            CountryCode = string.IsNullOrWhiteSpace(dto.CountryCode) ? null : dto.CountryCode.Trim(),
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? null : dto.Currency.Trim(),
            Timezone = string.IsNullOrWhiteSpace(dto.Timezone) ? null : dto.Timezone.Trim(),
            dto.IsDefault,
            dto.IsActive,
            dto.Metadata
        });

        if (updatedId == null || updatedId == Guid.Empty)
        {
            return null;
        }

        return await GetSocialSecurityJurisdictionByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteSocialSecurityJurisdictionAsync(Guid id, Guid organizationId)
    {
        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(@"
            UPDATE payroll.socialsecurityjurisdictions
            SET isdeleted = true, isactive = false, updatedat = current_timestamp
            WHERE jurisdictionid = @id
              AND organizationid = @organizationId
              AND isdeleted = false
            RETURNING jurisdictionid;", new { id, organizationId });

        return deletedId != null && deletedId != Guid.Empty;
    }

    public async Task<SocialSecurityAuthorityDto> CreateSocialSecurityAuthorityAsync(Guid organizationId, CreateSocialSecurityAuthorityDto dto)
    {
        var jurisdictionExists = await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1 FROM payroll.socialsecurityjurisdictions
                WHERE jurisdictionid = @JurisdictionId
                  AND organizationid = @organizationId
                  AND isdeleted = false
            );", new { organizationId, dto.JurisdictionId });

        if (!jurisdictionExists)
        {
            throw new InvalidOperationException("Selected jurisdiction is not available for this organization.");
        }

        var existing = await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1
                FROM payroll.socialsecurityauthorities
                WHERE organizationid = @organizationId
                  AND lower(authoritycode) = lower(@AuthorityCode)
                  AND isdeleted = false
            );", new { organizationId, dto.AuthorityCode });

        if (existing)
        {
            throw new InvalidOperationException("An authority with the same code already exists.");
        }

        var id = await QuerySingleAsync<Guid>(@"
            INSERT INTO payroll.socialsecurityauthorities
                (organizationid, jurisdictionid, authoritycode, authorityname, portalurl, remittancefrequency, filingmode, paymentmode, dueoffsetdays, isactive, metadata, isdeleted)
            VALUES
                (@organizationId, @JurisdictionId, @AuthorityCode, @AuthorityName, @PortalUrl, @RemittanceFrequency, @FilingMode, @PaymentMode, @DueOffsetDays, @IsActive,
                 CASE WHEN @Metadata IS NULL OR btrim(@Metadata) = '' THEN NULL ELSE CAST(@Metadata AS jsonb) END,
                 false)
            RETURNING authorityid;", new
        {
            organizationId,
            dto.JurisdictionId,
            AuthorityCode = dto.AuthorityCode.Trim(),
            AuthorityName = dto.AuthorityName.Trim(),
            PortalUrl = string.IsNullOrWhiteSpace(dto.PortalUrl) ? null : dto.PortalUrl.Trim(),
            RemittanceFrequency = string.IsNullOrWhiteSpace(dto.RemittanceFrequency) ? null : dto.RemittanceFrequency.Trim(),
            FilingMode = string.IsNullOrWhiteSpace(dto.FilingMode) ? null : dto.FilingMode.Trim(),
            PaymentMode = string.IsNullOrWhiteSpace(dto.PaymentMode) ? null : dto.PaymentMode.Trim(),
            dto.DueOffsetDays,
            dto.IsActive,
            dto.Metadata
        });

        return (await GetSocialSecurityAuthorityByIdAsync(id, organizationId))!;
    }

    public async Task<SocialSecurityAuthorityDto?> UpdateSocialSecurityAuthorityAsync(Guid id, Guid organizationId, UpdateSocialSecurityAuthorityDto dto)
    {
        var jurisdictionExists = await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1 FROM payroll.socialsecurityjurisdictions
                WHERE jurisdictionid = @JurisdictionId
                  AND organizationid = @organizationId
                  AND isdeleted = false
            );", new { organizationId, dto.JurisdictionId });

        if (!jurisdictionExists)
        {
            throw new InvalidOperationException("Selected jurisdiction is not available for this organization.");
        }

        var duplicate = await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1
                FROM payroll.socialsecurityauthorities
                WHERE organizationid = @organizationId
                  AND lower(authoritycode) = lower(@AuthorityCode)
                  AND authorityid <> @id
                  AND isdeleted = false
            );", new { organizationId, id, dto.AuthorityCode });

        if (duplicate)
        {
            throw new InvalidOperationException("An authority with the same code already exists.");
        }

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(@"
            UPDATE payroll.socialsecurityauthorities
            SET
                jurisdictionid = @JurisdictionId,
                authoritycode = @AuthorityCode,
                authorityname = @AuthorityName,
                portalurl = @PortalUrl,
                remittancefrequency = @RemittanceFrequency,
                filingmode = @FilingMode,
                paymentmode = @PaymentMode,
                dueoffsetdays = @DueOffsetDays,
                isactive = @IsActive,
                metadata = CASE WHEN @Metadata IS NULL OR btrim(@Metadata) = '' THEN NULL ELSE CAST(@Metadata AS jsonb) END,
                updatedat = current_timestamp
            WHERE authorityid = @id
              AND organizationid = @organizationId
              AND isdeleted = false
            RETURNING authorityid;", new
        {
            id,
            organizationId,
            dto.JurisdictionId,
            AuthorityCode = dto.AuthorityCode.Trim(),
            AuthorityName = dto.AuthorityName.Trim(),
            PortalUrl = string.IsNullOrWhiteSpace(dto.PortalUrl) ? null : dto.PortalUrl.Trim(),
            RemittanceFrequency = string.IsNullOrWhiteSpace(dto.RemittanceFrequency) ? null : dto.RemittanceFrequency.Trim(),
            FilingMode = string.IsNullOrWhiteSpace(dto.FilingMode) ? null : dto.FilingMode.Trim(),
            PaymentMode = string.IsNullOrWhiteSpace(dto.PaymentMode) ? null : dto.PaymentMode.Trim(),
            dto.DueOffsetDays,
            dto.IsActive,
            dto.Metadata
        });

        if (updatedId == null || updatedId == Guid.Empty)
        {
            return null;
        }

        return await GetSocialSecurityAuthorityByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteSocialSecurityAuthorityAsync(Guid id, Guid organizationId)
    {
        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(@"
            UPDATE payroll.socialsecurityauthorities
            SET isdeleted = true, isactive = false, updatedat = current_timestamp
            WHERE authorityid = @id
              AND organizationid = @organizationId
              AND isdeleted = false
            RETURNING authorityid;", new { id, organizationId });

        return deletedId != null && deletedId != Guid.Empty;
    }

    public async Task<SocialSecuritySchemeDto> CreateSocialSecuritySchemeAsync(Guid organizationId, CreateSocialSecuritySchemeDto dto)
    {
        var jurisdictionExists = await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1 FROM payroll.socialsecurityjurisdictions
                WHERE jurisdictionid = @JurisdictionId
                  AND organizationid = @organizationId
                  AND isdeleted = false
            );", new { organizationId, dto.JurisdictionId });

        if (!jurisdictionExists)
        {
            throw new InvalidOperationException("Selected jurisdiction is not available for this organization.");
        }

        if (dto.AuthorityId.HasValue)
        {
            var authorityExists = await ExecuteScalarAsync<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM payroll.socialsecurityauthorities
                    WHERE authorityid = @AuthorityId
                      AND organizationid = @organizationId
                      AND isdeleted = false
                );", new { organizationId, dto.AuthorityId });

            if (!authorityExists)
            {
                throw new InvalidOperationException("Selected authority is not available for this organization.");
            }
        }

        var existing = await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1
                FROM payroll.socialsecurityschemes
                WHERE organizationid = @organizationId
                  AND lower(schemecode) = lower(@SchemeCode)
                  AND isdeleted = false
            );", new { organizationId, dto.SchemeCode });

        if (existing)
        {
            throw new InvalidOperationException("A scheme with the same code already exists.");
        }

        var id = await QuerySingleAsync<Guid>(@"
            INSERT INTO payroll.socialsecurityschemes
                (organizationid, jurisdictionid, authorityid, schemecode, schemename, schemetype, mandatorymode,
                 employeemandatory, employermandatory, allowvoluntarytopup, allowcrossborder, versionstartdate, versionenddate,
                 isactive, metadata, isdeleted)
            VALUES
                (@organizationId, @JurisdictionId, @AuthorityId, @SchemeCode, @SchemeName, @SchemeType, @MandatoryMode,
                 @EmployeeMandatory, @EmployerMandatory, @AllowVoluntaryTopup, @AllowCrossBorder, @VersionStartDate, @VersionEndDate,
                 @IsActive,
                 CASE WHEN @Metadata IS NULL OR btrim(@Metadata) = '' THEN NULL ELSE CAST(@Metadata AS jsonb) END,
                 false)
            RETURNING schemeid;", new
        {
            organizationId,
            dto.JurisdictionId,
            dto.AuthorityId,
            SchemeCode = dto.SchemeCode.Trim(),
            SchemeName = dto.SchemeName.Trim(),
            SchemeType = string.IsNullOrWhiteSpace(dto.SchemeType) ? null : dto.SchemeType.Trim(),
            MandatoryMode = string.IsNullOrWhiteSpace(dto.MandatoryMode) ? null : dto.MandatoryMode.Trim(),
            dto.EmployeeMandatory,
            dto.EmployerMandatory,
            dto.AllowVoluntaryTopup,
            dto.AllowCrossBorder,
            VersionStartDate = dto.VersionStartDate ?? DateTime.UtcNow.Date,
            dto.VersionEndDate,
            dto.IsActive,
            dto.Metadata
        });

        return (await GetSocialSecuritySchemeByIdAsync(id, organizationId))!;
    }

    public async Task<SocialSecuritySchemeDto?> UpdateSocialSecuritySchemeAsync(Guid id, Guid organizationId, UpdateSocialSecuritySchemeDto dto)
    {
        var jurisdictionExists = await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1 FROM payroll.socialsecurityjurisdictions
                WHERE jurisdictionid = @JurisdictionId
                  AND organizationid = @organizationId
                  AND isdeleted = false
            );", new { organizationId, dto.JurisdictionId });

        if (!jurisdictionExists)
        {
            throw new InvalidOperationException("Selected jurisdiction is not available for this organization.");
        }

        if (dto.AuthorityId.HasValue)
        {
            var authorityExists = await ExecuteScalarAsync<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM payroll.socialsecurityauthorities
                    WHERE authorityid = @AuthorityId
                      AND organizationid = @organizationId
                      AND isdeleted = false
                );", new { organizationId, dto.AuthorityId });

            if (!authorityExists)
            {
                throw new InvalidOperationException("Selected authority is not available for this organization.");
            }
        }

        var duplicate = await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1
                FROM payroll.socialsecurityschemes
                WHERE organizationid = @organizationId
                  AND lower(schemecode) = lower(@SchemeCode)
                  AND schemeid <> @id
                  AND isdeleted = false
            );", new { organizationId, id, dto.SchemeCode });

        if (duplicate)
        {
            throw new InvalidOperationException("A scheme with the same code already exists.");
        }

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(@"
            UPDATE payroll.socialsecurityschemes
            SET
                jurisdictionid = @JurisdictionId,
                authorityid = @AuthorityId,
                schemecode = @SchemeCode,
                schemename = @SchemeName,
                schemetype = @SchemeType,
                mandatorymode = @MandatoryMode,
                employeemandatory = @EmployeeMandatory,
                employermandatory = @EmployerMandatory,
                allowvoluntarytopup = @AllowVoluntaryTopup,
                allowcrossborder = @AllowCrossBorder,
                versionstartdate = @VersionStartDate,
                versionenddate = @VersionEndDate,
                isactive = @IsActive,
                metadata = CASE WHEN @Metadata IS NULL OR btrim(@Metadata) = '' THEN NULL ELSE CAST(@Metadata AS jsonb) END,
                updatedat = current_timestamp
            WHERE schemeid = @id
              AND organizationid = @organizationId
              AND isdeleted = false
            RETURNING schemeid;", new
        {
            id,
            organizationId,
            dto.JurisdictionId,
            dto.AuthorityId,
            SchemeCode = dto.SchemeCode.Trim(),
            SchemeName = dto.SchemeName.Trim(),
            SchemeType = string.IsNullOrWhiteSpace(dto.SchemeType) ? null : dto.SchemeType.Trim(),
            MandatoryMode = string.IsNullOrWhiteSpace(dto.MandatoryMode) ? null : dto.MandatoryMode.Trim(),
            dto.EmployeeMandatory,
            dto.EmployerMandatory,
            dto.AllowVoluntaryTopup,
            dto.AllowCrossBorder,
            VersionStartDate = dto.VersionStartDate ?? DateTime.UtcNow.Date,
            dto.VersionEndDate,
            dto.IsActive,
            dto.Metadata
        });

        if (updatedId == null || updatedId == Guid.Empty)
        {
            return null;
        }

        return await GetSocialSecuritySchemeByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteSocialSecuritySchemeAsync(Guid id, Guid organizationId)
    {
        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(@"
            UPDATE payroll.socialsecurityschemes
            SET isdeleted = true, isactive = false, updatedat = current_timestamp
            WHERE schemeid = @id
              AND organizationid = @organizationId
              AND isdeleted = false
            RETURNING schemeid;", new { id, organizationId });

        return deletedId != null && deletedId != Guid.Empty;
    }

    public async Task<SocialSecurityRuleDto> CreateSocialSecurityRuleAsync(Guid organizationId, CreateSocialSecurityRuleDto dto)
    {
        var schemeExists = await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1 FROM payroll.socialsecurityschemes
                WHERE schemeid = @SchemeId
                  AND organizationid = @organizationId
                  AND isdeleted = false
            );", new { organizationId, dto.SchemeId });

        if (!schemeExists)
        {
            throw new InvalidOperationException("Selected scheme is not available for this organization.");
        }

        var id = await QuerySingleAsync<Guid>(@"
            INSERT INTO payroll.socialsecurityrules
                (organizationid, schemeid, rulename, description, contributionbasis, employeedefaultpct, employerdefaultpct,
                 employeefixedamount, employerfixedamount, minsalarylimit, maxsalarylimit, annualsalarycap,
                 effectivefrom, effectiveto, isactive, isdeleted)
            VALUES
                (@organizationId, @SchemeId, @RuleName, @Description, @ContributionBasis, @EmployeeDefaultPct, @EmployerDefaultPct,
                 @EmployeeFixedAmount, @EmployerFixedAmount, @MinSalaryLimit, @MaxSalaryLimit, @AnnualSalaryCap,
                 @EffectiveFrom, @EffectiveTo, @IsActive, false)
            RETURNING ruleid;", new
        {
            organizationId,
            dto.SchemeId,
            RuleName = dto.RuleName.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            ContributionBasis = string.IsNullOrWhiteSpace(dto.ContributionBasis) ? "gross" : dto.ContributionBasis.Trim().ToLowerInvariant(),
            dto.EmployeeDefaultPct,
            dto.EmployerDefaultPct,
            dto.EmployeeFixedAmount,
            dto.EmployerFixedAmount,
            dto.MinSalaryLimit,
            dto.MaxSalaryLimit,
            dto.AnnualSalaryCap,
            EffectiveFrom = dto.EffectiveFrom ?? DateTime.UtcNow.Date,
            dto.EffectiveTo,
            dto.IsActive
        });

        return (await GetSocialSecurityRuleByIdAsync(id, organizationId))!;
    }

    public async Task<SocialSecurityRuleDto?> UpdateSocialSecurityRuleAsync(Guid id, Guid organizationId, UpdateSocialSecurityRuleDto dto)
    {
        var schemeExists = await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1 FROM payroll.socialsecurityschemes
                WHERE schemeid = @SchemeId
                  AND organizationid = @organizationId
                  AND isdeleted = false
            );", new { organizationId, dto.SchemeId });

        if (!schemeExists)
        {
            throw new InvalidOperationException("Selected scheme is not available for this organization.");
        }

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(@"
            UPDATE payroll.socialsecurityrules
            SET
                schemeid = @SchemeId,
                rulename = @RuleName,
                description = @Description,
                contributionbasis = @ContributionBasis,
                employeedefaultpct = @EmployeeDefaultPct,
                employerdefaultpct = @EmployerDefaultPct,
                employeefixedamount = @EmployeeFixedAmount,
                employerfixedamount = @EmployerFixedAmount,
                minsalarylimit = @MinSalaryLimit,
                maxsalarylimit = @MaxSalaryLimit,
                annualsalarycap = @AnnualSalaryCap,
                effectivefrom = @EffectiveFrom,
                effectiveto = @EffectiveTo,
                isactive = @IsActive,
                updatedat = current_timestamp
            WHERE ruleid = @id
              AND organizationid = @organizationId
              AND isdeleted = false
            RETURNING ruleid;", new
        {
            id,
            organizationId,
            dto.SchemeId,
            RuleName = dto.RuleName.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            ContributionBasis = string.IsNullOrWhiteSpace(dto.ContributionBasis) ? "gross" : dto.ContributionBasis.Trim().ToLowerInvariant(),
            dto.EmployeeDefaultPct,
            dto.EmployerDefaultPct,
            dto.EmployeeFixedAmount,
            dto.EmployerFixedAmount,
            dto.MinSalaryLimit,
            dto.MaxSalaryLimit,
            dto.AnnualSalaryCap,
            EffectiveFrom = dto.EffectiveFrom ?? DateTime.UtcNow.Date,
            dto.EffectiveTo,
            dto.IsActive
        });

        if (updatedId == null || updatedId == Guid.Empty)
        {
            return null;
        }

        return await GetSocialSecurityRuleByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteSocialSecurityRuleAsync(Guid id, Guid organizationId)
    {
        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(@"
            UPDATE payroll.socialsecurityrules
            SET isdeleted = true, isactive = false, updatedat = current_timestamp
            WHERE ruleid = @id
              AND organizationid = @organizationId
              AND isdeleted = false
            RETURNING ruleid;", new { id, organizationId });

        return deletedId != null && deletedId != Guid.Empty;
    }

    private async Task<SocialSecurityJurisdictionDto?> GetSocialSecurityJurisdictionByIdAsync(Guid id, Guid organizationId)
    {
        return await QuerySingleOrDefaultAsync<SocialSecurityJurisdictionDto>(@"
            SELECT
                jurisdictionid AS JurisdictionId,
                organizationid AS OrganizationId,
                jurisdictioncode AS JurisdictionCode,
                jurisdictionname AS JurisdictionName,
                countrycode AS CountryCode,
                currency AS Currency,
                timezone AS Timezone,
                COALESCE(isdefault, false) AS IsDefault,
                COALESCE(isactive, true) AS IsActive,
                metadata::text AS Metadata
            FROM payroll.socialsecurityjurisdictions
            WHERE jurisdictionid = @id
              AND organizationid = @organizationId
              AND isdeleted = false;", new { id, organizationId });
    }

    private async Task<SocialSecurityAuthorityDto?> GetSocialSecurityAuthorityByIdAsync(Guid id, Guid organizationId)
    {
        return await QuerySingleOrDefaultAsync<SocialSecurityAuthorityDto>(@"
            SELECT
                authorityid AS AuthorityId,
                organizationid AS OrganizationId,
                jurisdictionid AS JurisdictionId,
                authoritycode AS AuthorityCode,
                authorityname AS AuthorityName,
                portalurl AS PortalUrl,
                remittancefrequency AS RemittanceFrequency,
                filingmode AS FilingMode,
                paymentmode AS PaymentMode,
                dueoffsetdays AS DueOffsetDays,
                COALESCE(isactive, true) AS IsActive,
                metadata::text AS Metadata
            FROM payroll.socialsecurityauthorities
            WHERE authorityid = @id
              AND organizationid = @organizationId
              AND isdeleted = false;", new { id, organizationId });
    }

    private async Task<SocialSecuritySchemeDto?> GetSocialSecuritySchemeByIdAsync(Guid id, Guid organizationId)
    {
        return await QuerySingleOrDefaultAsync<SocialSecuritySchemeDto>(@"
            SELECT
                schemeid AS SchemeId,
                organizationid AS OrganizationId,
                jurisdictionid AS JurisdictionId,
                authorityid AS AuthorityId,
                schemecode AS SchemeCode,
                schemename AS SchemeName,
                schemetype AS SchemeType,
                mandatorymode AS MandatoryMode,
                COALESCE(employeemandatory, true) AS EmployeeMandatory,
                COALESCE(employermandatory, true) AS EmployerMandatory,
                COALESCE(allowvoluntarytopup, false) AS AllowVoluntaryTopup,
                COALESCE(allowcrossborder, false) AS AllowCrossBorder,
                versionstartdate AS VersionStartDate,
                versionenddate AS VersionEndDate,
                COALESCE(isactive, true) AS IsActive,
                metadata::text AS Metadata
            FROM payroll.socialsecurityschemes
            WHERE schemeid = @id
              AND organizationid = @organizationId
              AND isdeleted = false;", new { id, organizationId });
    }

    private async Task<SocialSecurityRuleDto?> GetSocialSecurityRuleByIdAsync(Guid id, Guid organizationId)
    {
        return await QuerySingleOrDefaultAsync<SocialSecurityRuleDto>(@"
            SELECT
                ruleid AS RuleId,
                organizationid AS OrganizationId,
                schemeid AS SchemeId,
                rulename AS RuleName,
                description AS Description,
                COALESCE(contributionbasis, 'gross') AS ContributionBasis,
                COALESCE(employeedefaultpct, 0) AS EmployeeDefaultPct,
                COALESCE(employerdefaultpct, 0) AS EmployerDefaultPct,
                employeefixedamount AS EmployeeFixedAmount,
                employerfixedamount AS EmployerFixedAmount,
                minsalarylimit AS MinSalaryLimit,
                maxsalarylimit AS MaxSalaryLimit,
                annualsalarycap AS AnnualSalaryCap,
                effectivefrom AS EffectiveFrom,
                effectiveto AS EffectiveTo,
                COALESCE(isactive, true) AS IsActive
            FROM payroll.socialsecurityrules
            WHERE ruleid = @id
              AND organizationid = @organizationId
              AND isdeleted = false;", new { id, organizationId });
    }

    public async Task<PagedResult<SocialSecurityConfigDto>> GetAllSocialSecurityConfigsAsync(Guid organizationId, SocialSecurityConfigFilterDto filter)
    {
        filter ??= new SocialSecurityConfigFilterDto();
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.PageSize <= 0) filter.PageSize = 10;

        var conditions = new List<string>
        {
            "sc.organizationid = @organizationId",
            "sc.isdeleted = false"
        };

        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);

        if (filter.IsActive.HasValue)
        {
            conditions.Add("sc.isactive = @IsActive");
            parameters.Add("IsActive", filter.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            conditions.Add("(sc.configrulename ILIKE @SearchTerm OR COALESCE(sc.description, '') ILIKE @SearchTerm OR COALESCE(sr.rulename, '') ILIKE @SearchTerm OR COALESCE(ss.schemename, '') ILIKE @SearchTerm)");
            parameters.Add("SearchTerm", $"%{filter.SearchTerm.Trim()}%");
        }

        if (filter.JurisdictionId.HasValue)
        {
            conditions.Add("sc.jurisdictionid = @JurisdictionId");
            parameters.Add("JurisdictionId", filter.JurisdictionId.Value);
        }

        if (filter.SchemeId.HasValue)
        {
            conditions.Add("sc.schemeid = @SchemeId");
            parameters.Add("SchemeId", filter.SchemeId.Value);
        }

        var whereClause = string.Join(" AND ", conditions);
        var countSql = $@"
            SELECT COUNT(1)
            FROM payroll.socialsecurityconfig sc
            LEFT JOIN payroll.socialsecurityrules sr ON sr.ruleid = sc.ruleid
            LEFT JOIN payroll.socialsecurityschemes ss ON ss.schemeid = sc.schemeid
            WHERE {whereClause};";

        var sql = $@"
            SELECT
                sc.id AS Id,
                sc.organizationid AS OrganizationId,
                sc.configrulename AS ConfigName,
                sc.description AS Description,
                sc.jurisdictionid AS JurisdictionId,
                j.jurisdictioncode AS JurisdictionCode,
                j.jurisdictionname AS JurisdictionName,
                sc.authorityid AS AuthorityId,
                a.authoritycode AS AuthorityCode,
                a.authorityname AS AuthorityName,
                a.portalurl AS PortalUrl,
                sc.schemeid AS SchemeId,
                ss.schemecode AS SchemeCode,
                ss.schemename AS SchemeName,
                sc.ruleid AS RuleId,
                sr.rulename AS RuleName,
                COALESCE(sc.contributionbasis, 'gross') AS ContributionBasis,
                COALESCE(sc.employeepercentage, 0) AS EmployeeContributionPct,
                COALESCE(sc.employerpercentage, 0) AS EmployerContributionPct,
                sc.employeefixedamount AS EmployeeFixedAmount,
                sc.employerfixedamount AS EmployerFixedAmount,
                sc.minsalarylimit AS MinSalaryLimit,
                sc.maxsalarylimit AS MaxSalaryCap,
                sc.currency AS Currency,
                COALESCE(sc.isstatutory, true) AS IsStatutory,
                COALESCE(sc.allowvoluntary, false) AS AllowVoluntary,
                COALESCE(sc.allowwithdrawal, true) AS AllowWithdrawal,
                sc.metadata::text AS Metadata,
                sc.effectivefrom AS EffectiveFrom,
                sc.effectiveto AS EffectiveTo,
                sc.isactive AS IsActive,
                sc.isdeleted AS IsDeleted,
                sc.createdat AS CreatedAt,
                COALESCE(sc.updatedat, sc.createdat) AS UpdatedAt
            FROM payroll.socialsecurityconfig sc
            LEFT JOIN payroll.socialsecurityjurisdictions j ON j.jurisdictionid = sc.jurisdictionid
            LEFT JOIN payroll.socialsecurityauthorities a ON a.authorityid = sc.authorityid
            LEFT JOIN payroll.socialsecurityschemes ss ON ss.schemeid = sc.schemeid
            LEFT JOIN payroll.socialsecurityrules sr ON sr.ruleid = sc.ruleid
            WHERE {whereClause}
            ORDER BY sc.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", (filter.Page - 1) * filter.PageSize);

        var totalCount = await QuerySingleAsync<int>(countSql, parameters);
        var data = await QueryAsync<SocialSecurityConfigDto>(sql, parameters);

        return new PagedResult<SocialSecurityConfigDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IEnumerable<SocialSecurityConfigDto>> GetActiveSocialSecurityConfigsAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                                sc.id AS Id,
                                sc.organizationid AS OrganizationId,
                                sc.configrulename AS ConfigName,
                                sc.description AS Description,
                                sc.jurisdictionid AS JurisdictionId,
                                j.jurisdictioncode AS JurisdictionCode,
                                j.jurisdictionname AS JurisdictionName,
                                sc.authorityid AS AuthorityId,
                                a.authoritycode AS AuthorityCode,
                                a.authorityname AS AuthorityName,
                                a.portalurl AS PortalUrl,
                                sc.schemeid AS SchemeId,
                                ss.schemecode AS SchemeCode,
                                ss.schemename AS SchemeName,
                                sc.ruleid AS RuleId,
                                sr.rulename AS RuleName,
                                COALESCE(sc.contributionbasis, 'gross') AS ContributionBasis,
                                COALESCE(sc.employeepercentage, 0) AS EmployeeContributionPct,
                                COALESCE(sc.employerpercentage, 0) AS EmployerContributionPct,
                                sc.employeefixedamount AS EmployeeFixedAmount,
                                sc.employerfixedamount AS EmployerFixedAmount,
                                sc.minsalarylimit AS MinSalaryLimit,
                                sc.maxsalarylimit AS MaxSalaryCap,
                                sc.currency AS Currency,
                                COALESCE(sc.isstatutory, true) AS IsStatutory,
                                COALESCE(sc.allowvoluntary, false) AS AllowVoluntary,
                                COALESCE(sc.allowwithdrawal, true) AS AllowWithdrawal,
                                sc.metadata::text AS Metadata,
                                sc.effectivefrom AS EffectiveFrom,
                                sc.effectiveto AS EffectiveTo,
                                sc.isactive AS IsActive,
                                sc.isdeleted AS IsDeleted,
                                sc.createdat AS CreatedAt,
                                COALESCE(sc.updatedat, sc.createdat) AS UpdatedAt
                        FROM payroll.socialsecurityconfig sc
                        LEFT JOIN payroll.socialsecurityjurisdictions j ON j.jurisdictionid = sc.jurisdictionid
                        LEFT JOIN payroll.socialsecurityauthorities a ON a.authorityid = sc.authorityid
                        LEFT JOIN payroll.socialsecurityschemes ss ON ss.schemeid = sc.schemeid
                        LEFT JOIN payroll.socialsecurityrules sr ON sr.ruleid = sc.ruleid
                        WHERE sc.organizationid = @organizationId
                            AND sc.isactive = true
                            AND sc.isdeleted = false
                        ORDER BY sc.createdat DESC;";

        return await QueryAsync<SocialSecurityConfigDto>(sql, new { organizationId });
    }

    public async Task<SocialSecurityConfigDto?> GetSocialSecurityConfigByIdAsync(Guid id, Guid organizationId)
    {
        var sql = @"
            SELECT
                sc.id AS Id,
                sc.organizationid AS OrganizationId,
                sc.configrulename AS ConfigName,
                sc.description AS Description,
                sc.jurisdictionid AS JurisdictionId,
                j.jurisdictioncode AS JurisdictionCode,
                j.jurisdictionname AS JurisdictionName,
                sc.authorityid AS AuthorityId,
                a.authoritycode AS AuthorityCode,
                a.authorityname AS AuthorityName,
                a.portalurl AS PortalUrl,
                sc.schemeid AS SchemeId,
                ss.schemecode AS SchemeCode,
                ss.schemename AS SchemeName,
                sc.ruleid AS RuleId,
                sr.rulename AS RuleName,
                COALESCE(sc.contributionbasis, 'gross') AS ContributionBasis,
                COALESCE(sc.employeepercentage, 0) AS EmployeeContributionPct,
                COALESCE(sc.employerpercentage, 0) AS EmployerContributionPct,
                sc.employeefixedamount AS EmployeeFixedAmount,
                sc.employerfixedamount AS EmployerFixedAmount,
                sc.minsalarylimit AS MinSalaryLimit,
                sc.maxsalarylimit AS MaxSalaryCap,
                sc.currency AS Currency,
                COALESCE(sc.isstatutory, true) AS IsStatutory,
                COALESCE(sc.allowvoluntary, false) AS AllowVoluntary,
                COALESCE(sc.allowwithdrawal, true) AS AllowWithdrawal,
                sc.metadata::text AS Metadata,
                sc.effectivefrom AS EffectiveFrom,
                sc.effectiveto AS EffectiveTo,
                sc.isactive AS IsActive,
                sc.isdeleted AS IsDeleted,
                sc.createdat AS CreatedAt,
                COALESCE(sc.updatedat, sc.createdat) AS UpdatedAt
            FROM payroll.socialsecurityconfig sc
            LEFT JOIN payroll.socialsecurityjurisdictions j ON j.jurisdictionid = sc.jurisdictionid
            LEFT JOIN payroll.socialsecurityauthorities a ON a.authorityid = sc.authorityid
            LEFT JOIN payroll.socialsecurityschemes ss ON ss.schemeid = sc.schemeid
            LEFT JOIN payroll.socialsecurityrules sr ON sr.ruleid = sc.ruleid
            WHERE sc.id = @id
              AND sc.organizationid = @organizationId
              AND sc.isdeleted = false;";

        return await QuerySingleOrDefaultAsync<SocialSecurityConfigDto>(sql, new { id, organizationId });
    }

    public async Task<SocialSecurityConfigDto> CreateSocialSecurityConfigAsync(Guid organizationId, CreateSocialSecurityConfigDto dto)
    {
        var sql = @"
            INSERT INTO payroll.socialsecurityconfig
                (
                    organizationid,
                    configrulename,
                    description,
                    jurisdictionid,
                    authorityid,
                    schemeid,
                    ruleid,
                    contributionbasis,
                    employeepercentage,
                    employerpercentage,
                    employeefixedamount,
                    employerfixedamount,
                    minsalarylimit,
                    maxsalarylimit,
                    currency,
                    isstatutory,
                    allowvoluntary,
                    allowwithdrawal,
                    metadata,
                    effectivefrom,
                    effectiveto,
                    isactive,
                    isdeleted
                )
            VALUES
                (
                    @organizationId,
                    @ConfigName,
                    @Description,
                    @JurisdictionId,
                    @AuthorityId,
                    @SchemeId,
                    @RuleId,
                    COALESCE(NULLIF(@ContributionBasis, ''), 'gross'),
                    @EmployeeContributionPct,
                    @EmployerContributionPct,
                    @EmployeeFixedAmount,
                    @EmployerFixedAmount,
                    @MinSalaryLimit,
                    @MaxSalaryCap,
                    @Currency,
                    @IsStatutory,
                    @AllowVoluntary,
                    @AllowWithdrawal,
                    CASE WHEN @Metadata IS NULL OR btrim(@Metadata) = '' THEN '{}'::jsonb ELSE @Metadata::jsonb END,
                    @EffectiveFrom,
                    @EffectiveTo,
                    @IsActive,
                    false
                )
            RETURNING id;";

        var id = await QuerySingleAsync<Guid>(sql, new
        {
            organizationId,
            dto.ConfigName,
            dto.Description,
            dto.JurisdictionId,
            dto.AuthorityId,
            dto.SchemeId,
            dto.RuleId,
            dto.ContributionBasis,
            dto.EmployeeContributionPct,
            dto.EmployerContributionPct,
            dto.EmployeeFixedAmount,
            dto.EmployerFixedAmount,
            dto.MinSalaryLimit,
            dto.MaxSalaryCap,
            dto.Currency,
            dto.IsStatutory,
            dto.AllowVoluntary,
            dto.AllowWithdrawal,
            dto.Metadata,
            dto.EffectiveFrom,
            dto.EffectiveTo,
            dto.IsActive
        });

        return (await GetSocialSecurityConfigByIdAsync(id, organizationId))!;
    }

    public async Task<SocialSecurityConfigDto?> UpdateSocialSecurityConfigAsync(Guid id, Guid organizationId, UpdateSocialSecurityConfigDto dto)
    {
        var sql = @"
            UPDATE payroll.socialsecurityconfig
            SET
                configrulename = @ConfigName,
                description = @Description,
                jurisdictionid = @JurisdictionId,
                authorityid = @AuthorityId,
                schemeid = @SchemeId,
                ruleid = @RuleId,
                contributionbasis = COALESCE(NULLIF(@ContributionBasis, ''), contributionbasis, 'gross'),
                employeepercentage = @EmployeeContributionPct,
                employerpercentage = @EmployerContributionPct,
                employeefixedamount = @EmployeeFixedAmount,
                employerfixedamount = @EmployerFixedAmount,
                minsalarylimit = @MinSalaryLimit,
                maxsalarylimit = @MaxSalaryCap,
                currency = @Currency,
                isstatutory = @IsStatutory,
                allowvoluntary = @AllowVoluntary,
                allowwithdrawal = @AllowWithdrawal,
                metadata = CASE WHEN @Metadata IS NULL OR btrim(@Metadata) = '' THEN '{}'::jsonb ELSE @Metadata::jsonb END,
                effectivefrom = @EffectiveFrom,
                effectiveto = @EffectiveTo,
                isactive = @IsActive,
                updatedat = current_timestamp
            WHERE id = @id
              AND organizationid = @organizationId
              AND isdeleted = false
            RETURNING id;";

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new
        {
            id,
            organizationId,
            dto.ConfigName,
            dto.Description,
            dto.JurisdictionId,
            dto.AuthorityId,
            dto.SchemeId,
            dto.RuleId,
            dto.ContributionBasis,
            dto.EmployeeContributionPct,
            dto.EmployerContributionPct,
            dto.EmployeeFixedAmount,
            dto.EmployerFixedAmount,
            dto.MinSalaryLimit,
            dto.MaxSalaryCap,
            dto.Currency,
            dto.IsStatutory,
            dto.AllowVoluntary,
            dto.AllowWithdrawal,
            dto.Metadata,
            dto.EffectiveFrom,
            dto.EffectiveTo,
            dto.IsActive
        });

        if (updatedId == null || updatedId == Guid.Empty)
        {
            return null;
        }

        return await GetSocialSecurityConfigByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteSocialSecurityConfigAsync(Guid id, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.socialsecurityconfig
            SET isdeleted = true, updatedat = current_timestamp
            WHERE id = @id
              AND organizationid = @organizationId
              AND isdeleted = false
            RETURNING id;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { id, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // ─── Social Security Transactions ────────────────────────────────────────

    public async Task<PagedResult<SocialSecurityTransactionDto>> GetAllSocialSecurityTransactionsAsync(Guid organizationId, SocialSecurityTransactionFilterDto filter)
    {
        filter ??= new SocialSecurityTransactionFilterDto();
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.PageSize <= 0) filter.PageSize = 10;

        var conditions = new List<string>
        {
            "st.organizationid = @organizationId",
            "e.organizationid = @organizationId",
            "per.organizationid = @organizationId"
        };

        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            conditions.Add("(e.firstname || ' ' || e.lastname) ILIKE @SearchTerm");
            parameters.Add("SearchTerm", $"%{filter.SearchTerm.Trim()}%");
        }

        if (filter.PeriodId.HasValue)
        {
            conditions.Add("st.periodid = @PeriodId");
            parameters.Add("PeriodId", filter.PeriodId.Value);
        }

        if (filter.ConfigId.HasValue)
        {
            conditions.Add("st.configid = @ConfigId");
            parameters.Add("ConfigId", filter.ConfigId.Value);
        }

        if (filter.SchemeId.HasValue)
        {
            conditions.Add("st.schemeid = @SchemeId");
            parameters.Add("SchemeId", filter.SchemeId.Value);
        }

        if (filter.RuleId.HasValue)
        {
            conditions.Add("st.ruleid = @RuleId");
            parameters.Add("RuleId", filter.RuleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.RequestStatus))
        {
            conditions.Add("LOWER(COALESCE(st.requeststatus, 'pending')) = LOWER(@RequestStatus)");
            parameters.Add("RequestStatus", filter.RequestStatus.Trim());
        }

        var whereClause = string.Join(" AND ", conditions);
        var countSql = $@"
            SELECT COUNT(1)
            FROM payroll.socialsecuritytransactions st
            JOIN employee.employees e ON e.employeeid = st.employeeid
            JOIN payroll.payrollperiods per ON per.periodid = st.periodid
            LEFT JOIN payroll.socialsecurityconfig sc ON sc.id = st.configid
            LEFT JOIN payroll.socialsecurityrules sr ON sr.ruleid = st.ruleid
            LEFT JOIN payroll.socialsecurityschemes ss ON ss.schemeid = st.schemeid
            LEFT JOIN payroll.socialsecurityjurisdictions sj ON sj.jurisdictionid = st.jurisdictionid
            LEFT JOIN payroll.socialsecurityauthorities sa ON sa.authorityid = st.authorityid
            WHERE {whereClause};";

        var sql = $@"
            SELECT
                st.id AS Id,
                st.organizationid AS OrganizationId,
                st.employeeid AS EmployeeId,
                COALESCE(e.firstname || ' ' || e.lastname, '') AS EmployeeName,
                st.periodid AS PeriodId,
                COALESCE(per.periodname, '') AS PeriodName,
                st.configid AS ConfigId,
                sc.configrulename AS ConfigName,
                st.ruleid AS RuleId,
                sr.rulename AS RuleName,
                st.schemeid AS SchemeId,
                ss.schemename AS SchemeName,
                st.jurisdictionid AS JurisdictionId,
                sj.jurisdictionname AS JurisdictionName,
                st.authorityid AS AuthorityId,
                sa.authorityname AS AuthorityName,
                st.actualsalary AS ActualSalary,
                st.salarycapped AS SalaryCapped,
                COALESCE(st.contributionbase, st.salarycapped, st.actualsalary, 0) AS ContributionBase,
                COALESCE(st.contributionbasis, 'gross') AS ContributionBasis,
                COALESCE(st.employeerate, 0) AS EmployeeRate,
                COALESCE(st.employerrate, 0) AS EmployerRate,
                COALESCE(st.employeeamount, 0) AS EmployeeAmount,
                COALESCE(st.employeramount, 0) AS EmployerAmount,
                COALESCE(st.totalamount, 0) AS TotalAmount,
                st.referencecode AS ReferenceCode,
                COALESCE(st.transactiontype, 'monthly') AS TransactionType,
                COALESCE(st.requeststatus, 'pending') AS RequestStatus,
                COALESCE(st.isenrolled, false) AS IsEnrolled,
                COALESCE(st.withdrawalrequest, false) AS WithdrawalRequest,
                st.withdrawalstatus AS WithdrawalStatus,
                st.createdat AS CreatedAt,
                COALESCE(st.updatedat, st.createdat) AS UpdatedAt,
                0 AS TotalCount
            FROM payroll.socialsecuritytransactions st
            JOIN employee.employees e ON e.employeeid = st.employeeid
            JOIN payroll.payrollperiods per ON per.periodid = st.periodid
            LEFT JOIN payroll.socialsecurityconfig sc ON sc.id = st.configid
            LEFT JOIN payroll.socialsecurityrules sr ON sr.ruleid = st.ruleid
            LEFT JOIN payroll.socialsecurityschemes ss ON ss.schemeid = st.schemeid
            LEFT JOIN payroll.socialsecurityjurisdictions sj ON sj.jurisdictionid = st.jurisdictionid
            LEFT JOIN payroll.socialsecurityauthorities sa ON sa.authorityid = st.authorityid
            WHERE {whereClause}
            ORDER BY st.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", (filter.Page - 1) * filter.PageSize);

        var totalCount = await QuerySingleAsync<int>(countSql, parameters);
        var data = await QueryAsync<SocialSecurityTransactionDto>(sql, parameters);

        return new PagedResult<SocialSecurityTransactionDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<SocialSecurityTransactionDto?> GetSocialSecurityTransactionByIdAsync(Guid id, Guid organizationId)
    {
        var sql = @"
            SELECT
                st.id AS Id,
                st.organizationid AS OrganizationId,
                st.employeeid AS EmployeeId,
                COALESCE(e.firstname || ' ' || e.lastname, '') AS EmployeeName,
                st.periodid AS PeriodId,
                COALESCE(per.periodname, '') AS PeriodName,
                st.configid AS ConfigId,
                sc.configrulename AS ConfigName,
                st.ruleid AS RuleId,
                sr.rulename AS RuleName,
                st.schemeid AS SchemeId,
                ss.schemename AS SchemeName,
                st.jurisdictionid AS JurisdictionId,
                sj.jurisdictionname AS JurisdictionName,
                st.authorityid AS AuthorityId,
                sa.authorityname AS AuthorityName,
                st.actualsalary AS ActualSalary,
                st.salarycapped AS SalaryCapped,
                COALESCE(st.contributionbase, st.salarycapped, st.actualsalary, 0) AS ContributionBase,
                COALESCE(st.contributionbasis, 'gross') AS ContributionBasis,
                COALESCE(st.employeerate, 0) AS EmployeeRate,
                COALESCE(st.employerrate, 0) AS EmployerRate,
                COALESCE(st.employeeamount, 0) AS EmployeeAmount,
                COALESCE(st.employeramount, 0) AS EmployerAmount,
                COALESCE(st.totalamount, 0) AS TotalAmount,
                st.referencecode AS ReferenceCode,
                COALESCE(st.transactiontype, 'monthly') AS TransactionType,
                COALESCE(st.requeststatus, 'pending') AS RequestStatus,
                COALESCE(st.isenrolled, false) AS IsEnrolled,
                COALESCE(st.withdrawalrequest, false) AS WithdrawalRequest,
                st.withdrawalstatus AS WithdrawalStatus,
                st.createdat AS CreatedAt,
                COALESCE(st.updatedat, st.createdat) AS UpdatedAt,
                0 AS TotalCount
            FROM payroll.socialsecuritytransactions st
            JOIN employee.employees e ON e.employeeid = st.employeeid
            JOIN payroll.payrollperiods per ON per.periodid = st.periodid
            LEFT JOIN payroll.socialsecurityconfig sc ON sc.id = st.configid
            LEFT JOIN payroll.socialsecurityrules sr ON sr.ruleid = st.ruleid
            LEFT JOIN payroll.socialsecurityschemes ss ON ss.schemeid = st.schemeid
            LEFT JOIN payroll.socialsecurityjurisdictions sj ON sj.jurisdictionid = st.jurisdictionid
            LEFT JOIN payroll.socialsecurityauthorities sa ON sa.authorityid = st.authorityid
            WHERE st.id = @id
              AND st.organizationid = @organizationId
              AND e.organizationid = @organizationId
              AND per.organizationid = @organizationId;";

        return await QuerySingleOrDefaultAsync<SocialSecurityTransactionDto>(sql, new { id, organizationId });
    }

    public async Task<SocialSecurityTransactionDto> CreateSocialSecurityTransactionAsync(Guid organizationId, CreateSocialSecurityTransactionDto dto)
    {
        var employeeExists = await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1
                FROM employee.employees
                WHERE employeeid = @EmployeeId AND organizationid = @organizationId
            );", new { organizationId, dto.EmployeeId });

        if (!employeeExists)
        {
            throw new InvalidOperationException("Selected employee does not belong to this organization.");
        }

        var periodExists = await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1
                FROM payroll.payrollperiods
                WHERE periodid = @PeriodId AND organizationid = @organizationId
            );", new { organizationId, dto.PeriodId });

        if (!periodExists)
        {
            throw new InvalidOperationException("Selected payroll period is not available for this organization.");
        }

        var alreadyExists = await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1
                FROM payroll.socialsecuritytransactions
                WHERE organizationid = @organizationId
                  AND employeeid = @EmployeeId
                  AND periodid = @PeriodId
            );", new { organizationId, dto.EmployeeId, dto.PeriodId });

        if (alreadyExists)
        {
            throw new InvalidOperationException("A social security transaction already exists for this employee and payroll period.");
        }

        SocialSecurityConfigCalculationDto? config = null;
        if (dto.ConfigId.HasValue && dto.ConfigId.Value != Guid.Empty)
        {
            config = await QuerySingleOrDefaultAsync<SocialSecurityConfigCalculationDto>(@"
                SELECT
                    id AS Id,
                    ruleid AS RuleId,
                    schemeid AS SchemeId,
                    jurisdictionid AS JurisdictionId,
                    authorityid AS AuthorityId,
                    COALESCE(employeepercentage, 0) AS EmployeeContributionPct,
                    COALESCE(employerpercentage, 0) AS EmployerContributionPct,
                    employeefixedamount AS EmployeeFixedAmount,
                    employerfixedamount AS EmployerFixedAmount,
                    minsalarylimit AS MinSalaryLimit,
                    maxsalarylimit AS MaxSalaryCap,
                    COALESCE(contributionbasis, 'gross') AS ContributionBasis
                FROM payroll.socialsecurityconfig
                WHERE id = @ConfigId
                  AND organizationid = @organizationId
                  AND isdeleted = false;", new { organizationId, ConfigId = dto.ConfigId.Value });

            if (config == null)
            {
                throw new InvalidOperationException("Selected social security config is not available for this organization.");
            }
        }

        var resolvedRuleId = dto.RuleId ?? config?.RuleId;
        SocialSecurityRuleCalculationDto? rule = null;
        if (resolvedRuleId.HasValue && resolvedRuleId.Value != Guid.Empty)
        {
            rule = await QuerySingleOrDefaultAsync<SocialSecurityRuleCalculationDto>(@"
                SELECT
                    ruleid AS RuleId,
                    schemeid AS SchemeId,
                    COALESCE(employeedefaultpct, 0) AS EmployeeDefaultPct,
                    COALESCE(employerdefaultpct, 0) AS EmployerDefaultPct,
                    employeefixedamount AS EmployeeFixedAmount,
                    employerfixedamount AS EmployerFixedAmount,
                    minsalarylimit AS MinSalaryLimit,
                    maxsalarylimit AS MaxSalaryLimit,
                    COALESCE(contributionbasis, 'gross') AS ContributionBasis,
                    effectivefrom AS EffectiveFrom
                FROM payroll.socialsecurityrules
                WHERE organizationid = @organizationId
                  AND ruleid = @RuleId
                  AND isdeleted = false;", new { organizationId, RuleId = resolvedRuleId.Value });

            if (rule == null)
            {
                throw new InvalidOperationException("Selected social security rule is not available for this organization.");
            }
        }

        if (config == null && rule == null)
        {
            throw new InvalidOperationException("Either a social security config or rule is required.");
        }

        var contributionBasis = (config?.ContributionBasis ?? rule?.ContributionBasis ?? "gross").Trim().ToLowerInvariant();
        var minSalary = config?.MinSalaryLimit ?? rule?.MinSalaryLimit;
        var maxSalary = config?.MaxSalaryCap ?? rule?.MaxSalaryLimit;
        var contributionBase = dto.ActualSalary;

        if (minSalary.HasValue && contributionBase < minSalary.Value)
        {
            contributionBase = 0;
        }

        if (maxSalary.HasValue && maxSalary.Value > 0)
        {
            contributionBase = Math.Min(contributionBase, maxSalary.Value);
        }

        var salaryCapped = contributionBase;
        var employeeRate = config != null ? config.EmployeeContributionPct : (rule?.EmployeeDefaultPct ?? 0);
        var employerRate = config != null ? config.EmployerContributionPct : (rule?.EmployerDefaultPct ?? 0);
        var employeeFixedAmount = config?.EmployeeFixedAmount ?? rule?.EmployeeFixedAmount;
        var employerFixedAmount = config?.EmployerFixedAmount ?? rule?.EmployerFixedAmount;

        var employeeAmount = contributionBasis == "fixed" && employeeFixedAmount.HasValue
            ? Math.Round(employeeFixedAmount.Value, 2, MidpointRounding.AwayFromZero)
            : Math.Round((contributionBase * employeeRate) / 100m, 2, MidpointRounding.AwayFromZero);

        var employerAmount = contributionBasis == "fixed" && employerFixedAmount.HasValue
            ? Math.Round(employerFixedAmount.Value, 2, MidpointRounding.AwayFromZero)
            : Math.Round((contributionBase * employerRate) / 100m, 2, MidpointRounding.AwayFromZero);

        var requestStatus = string.IsNullOrWhiteSpace(dto.RequestStatus)
            ? "pending"
            : dto.RequestStatus.Trim().ToLowerInvariant();

        var sql = @"
            INSERT INTO payroll.socialsecuritytransactions
                (
                    organizationid,
                    employeeid,
                    periodid,
                    configid,
                    ruleid,
                    schemeid,
                    jurisdictionid,
                    authorityid,
                    actualsalary,
                    salarycapped,
                    contributionbase,
                    contributionbasis,
                    employeerate,
                    employerrate,
                    employeefixedamount,
                    employerfixedamount,
                    employeeamount,
                    employeramount,
                    requeststatus,
                    isenrolled,
                    withdrawalrequest,
                    withdrawalstatus,
                    transactiontype,
                    sourcetype,
                    referencecode
                )
            VALUES
                (
                    @organizationId,
                    @EmployeeId,
                    @PeriodId,
                    @ConfigId,
                    @RuleId,
                    @SchemeId,
                    @JurisdictionId,
                    @AuthorityId,
                    @ActualSalary,
                    @SalaryCapped,
                    @ContributionBase,
                    @ContributionBasis,
                    @EmployeeRate,
                    @EmployerRate,
                    @EmployeeFixedAmount,
                    @EmployerFixedAmount,
                    @EmployeeAmount,
                    @EmployerAmount,
                    @RequestStatus,
                    @IsEnrolled,
                    false,
                    NULL,
                    'monthly',
                    'system',
                    ('SS-' || upper(substring(replace(uuid_generate_v4()::text, '-', ''), 1, 10)))
                )
            RETURNING id;";

        var id = await QuerySingleAsync<Guid>(sql, new
        {
            organizationId,
            dto.EmployeeId,
            dto.PeriodId,
            ConfigId = config?.Id,
            RuleId = resolvedRuleId,
            SchemeId = config?.SchemeId ?? rule?.SchemeId,
            JurisdictionId = config?.JurisdictionId,
            AuthorityId = config?.AuthorityId,
            dto.ActualSalary,
            SalaryCapped = salaryCapped,
            ContributionBase = contributionBase,
            ContributionBasis = contributionBasis,
            EmployeeRate = employeeRate,
            EmployerRate = employerRate,
            EmployeeFixedAmount = employeeFixedAmount,
            EmployerFixedAmount = employerFixedAmount,
            EmployeeAmount = employeeAmount,
            EmployerAmount = employerAmount,
            RequestStatus = requestStatus,
            dto.IsEnrolled
        });

        return (await GetSocialSecurityTransactionByIdAsync(id, organizationId))!;
    }

    public async Task<SocialSecurityTransactionDto?> UpdateSocialSecurityTransactionAsync(Guid id, Guid organizationId, UpdateSocialSecurityTransactionDto dto)
    {
        var existing = await QuerySingleOrDefaultAsync<ExistingSocialSecurityTransactionDto>(@"
            SELECT id AS Id, configid AS ConfigId, ruleid AS RuleId
            FROM payroll.socialsecuritytransactions
            WHERE id = @id AND organizationid = @organizationId;", new { id, organizationId });

        if (existing == null)
        {
            return null;
        }

        var configId = dto.ConfigId ?? existing.ConfigId;
        SocialSecurityConfigCalculationDto? config = null;
        if (configId.HasValue && configId.Value != Guid.Empty)
        {
            config = await QuerySingleOrDefaultAsync<SocialSecurityConfigCalculationDto>(@"
                SELECT
                    id AS Id,
                    ruleid AS RuleId,
                    schemeid AS SchemeId,
                    jurisdictionid AS JurisdictionId,
                    authorityid AS AuthorityId,
                    COALESCE(employeepercentage, 0) AS EmployeeContributionPct,
                    COALESCE(employerpercentage, 0) AS EmployerContributionPct,
                    employeefixedamount AS EmployeeFixedAmount,
                    employerfixedamount AS EmployerFixedAmount,
                    minsalarylimit AS MinSalaryLimit,
                    maxsalarylimit AS MaxSalaryCap,
                    COALESCE(contributionbasis, 'gross') AS ContributionBasis
                FROM payroll.socialsecurityconfig
                WHERE id = @ConfigId
                  AND organizationid = @organizationId
                  AND isdeleted = false;", new { organizationId, ConfigId = configId.Value });

            if (config == null)
            {
                throw new InvalidOperationException("Selected social security config is not available for this organization.");
            }
        }

        var resolvedRuleId = dto.RuleId ?? config?.RuleId ?? existing.RuleId;
        SocialSecurityRuleCalculationDto? rule = null;
        if (resolvedRuleId.HasValue && resolvedRuleId.Value != Guid.Empty)
        {
            rule = await QuerySingleOrDefaultAsync<SocialSecurityRuleCalculationDto>(@"
                SELECT
                    ruleid AS RuleId,
                    schemeid AS SchemeId,
                    COALESCE(employeedefaultpct, 0) AS EmployeeDefaultPct,
                    COALESCE(employerdefaultpct, 0) AS EmployerDefaultPct,
                    employeefixedamount AS EmployeeFixedAmount,
                    employerfixedamount AS EmployerFixedAmount,
                    minsalarylimit AS MinSalaryLimit,
                    maxsalarylimit AS MaxSalaryLimit,
                    COALESCE(contributionbasis, 'gross') AS ContributionBasis,
                    effectivefrom AS EffectiveFrom
                FROM payroll.socialsecurityrules
                WHERE organizationid = @organizationId
                  AND ruleid = @RuleId
                  AND isdeleted = false;", new { organizationId, RuleId = resolvedRuleId.Value });

            if (rule == null)
            {
                throw new InvalidOperationException("Selected social security rule is not available for this organization.");
            }
        }

        if (config == null && rule == null)
        {
            throw new InvalidOperationException("A social security config or rule is required to update this transaction.");
        }

        var contributionBasis = (config?.ContributionBasis ?? rule?.ContributionBasis ?? "gross").Trim().ToLowerInvariant();
        var minSalary = config?.MinSalaryLimit ?? rule?.MinSalaryLimit;
        var maxSalary = config?.MaxSalaryCap ?? rule?.MaxSalaryLimit;
        var contributionBase = dto.ActualSalary;

        if (minSalary.HasValue && contributionBase < minSalary.Value)
        {
            contributionBase = 0;
        }

        if (maxSalary.HasValue && maxSalary.Value > 0)
        {
            contributionBase = Math.Min(contributionBase, maxSalary.Value);
        }

        var salaryCapped = contributionBase;
        var employeeRate = config != null ? config.EmployeeContributionPct : (rule?.EmployeeDefaultPct ?? 0);
        var employerRate = config != null ? config.EmployerContributionPct : (rule?.EmployerDefaultPct ?? 0);
        var employeeFixedAmount = config?.EmployeeFixedAmount ?? rule?.EmployeeFixedAmount;
        var employerFixedAmount = config?.EmployerFixedAmount ?? rule?.EmployerFixedAmount;

        var employeeAmount = contributionBasis == "fixed" && employeeFixedAmount.HasValue
            ? Math.Round(employeeFixedAmount.Value, 2, MidpointRounding.AwayFromZero)
            : Math.Round((contributionBase * employeeRate) / 100m, 2, MidpointRounding.AwayFromZero);

        var employerAmount = contributionBasis == "fixed" && employerFixedAmount.HasValue
            ? Math.Round(employerFixedAmount.Value, 2, MidpointRounding.AwayFromZero)
            : Math.Round((contributionBase * employerRate) / 100m, 2, MidpointRounding.AwayFromZero);

        var requestStatus = string.IsNullOrWhiteSpace(dto.RequestStatus)
            ? "pending"
            : dto.RequestStatus.Trim().ToLowerInvariant();

        var sql = @"
            UPDATE payroll.socialsecuritytransactions
            SET
                configid = @ConfigId,
                ruleid = @RuleId,
                schemeid = @SchemeId,
                jurisdictionid = @JurisdictionId,
                authorityid = @AuthorityId,
                actualsalary = @ActualSalary,
                salarycapped = @SalaryCapped,
                contributionbase = @ContributionBase,
                contributionbasis = @ContributionBasis,
                employeerate = @EmployeeRate,
                employerrate = @EmployerRate,
                employeefixedamount = @EmployeeFixedAmount,
                employerfixedamount = @EmployerFixedAmount,
                employeeamount = @EmployeeAmount,
                employeramount = @EmployerAmount,
                requeststatus = @RequestStatus,
                isenrolled = @IsEnrolled,
                updatedat = current_timestamp
            WHERE id = @id
              AND organizationid = @organizationId
            RETURNING id;";

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new
        {
            id,
            organizationId,
            ConfigId = config?.Id,
            RuleId = resolvedRuleId,
            SchemeId = config?.SchemeId ?? rule?.SchemeId,
            JurisdictionId = config?.JurisdictionId,
            AuthorityId = config?.AuthorityId,
            dto.ActualSalary,
            SalaryCapped = salaryCapped,
            ContributionBase = contributionBase,
            ContributionBasis = contributionBasis,
            EmployeeRate = employeeRate,
            EmployerRate = employerRate,
            EmployeeFixedAmount = employeeFixedAmount,
            EmployerFixedAmount = employerFixedAmount,
            EmployeeAmount = employeeAmount,
            EmployerAmount = employerAmount,
            RequestStatus = requestStatus,
            dto.IsEnrolled
        });

        if (updatedId == null || updatedId == Guid.Empty)
        {
            return null;
        }

        return await GetSocialSecurityTransactionByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteSocialSecurityTransactionAsync(Guid id, Guid organizationId)
    {
        var sql = @"
            DELETE FROM payroll.socialsecuritytransactions
            WHERE id = @id AND organizationid = @organizationId
            RETURNING id;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { id, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    public async Task<PagedResult<SocialSecurityTransactionDto>> GetMySocialSecurityTransactionsAsync(Guid organizationId, Guid employeeId, MySocialSecurityTransactionFilterDto filter)
    {
        filter ??= new MySocialSecurityTransactionFilterDto();
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.PageSize <= 0) filter.PageSize = 10;

        var conditions = new List<string>
        {
            "st.organizationid = @organizationId",
            "st.employeeid = @employeeId",
            "e.organizationid = @organizationId",
            "per.organizationid = @organizationId"
        };

        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);
        parameters.Add("employeeId", employeeId);

        if (filter.PeriodId.HasValue)
        {
            conditions.Add("st.periodid = @PeriodId");
            parameters.Add("PeriodId", filter.PeriodId.Value);
        }

        var whereClause = string.Join(" AND ", conditions);
        var countSql = $@"
            SELECT COUNT(1)
            FROM payroll.socialsecuritytransactions st
            JOIN employee.employees e ON e.employeeid = st.employeeid
            JOIN payroll.payrollperiods per ON per.periodid = st.periodid
            LEFT JOIN payroll.socialsecurityconfig sc ON sc.id = st.configid
            LEFT JOIN payroll.socialsecurityrules sr ON sr.ruleid = st.ruleid
            LEFT JOIN payroll.socialsecurityschemes ss ON ss.schemeid = st.schemeid
            LEFT JOIN payroll.socialsecurityjurisdictions sj ON sj.jurisdictionid = st.jurisdictionid
            LEFT JOIN payroll.socialsecurityauthorities sa ON sa.authorityid = st.authorityid
            WHERE {whereClause};";

        var sql = $@"
            SELECT
                st.id AS Id,
                st.organizationid AS OrganizationId,
                st.employeeid AS EmployeeId,
                COALESCE(e.firstname || ' ' || e.lastname, '') AS EmployeeName,
                st.periodid AS PeriodId,
                COALESCE(per.periodname, '') AS PeriodName,
                st.configid AS ConfigId,
                sc.configrulename AS ConfigName,
                st.ruleid AS RuleId,
                sr.rulename AS RuleName,
                st.schemeid AS SchemeId,
                ss.schemename AS SchemeName,
                st.jurisdictionid AS JurisdictionId,
                sj.jurisdictionname AS JurisdictionName,
                st.authorityid AS AuthorityId,
                sa.authorityname AS AuthorityName,
                st.actualsalary AS ActualSalary,
                st.salarycapped AS SalaryCapped,
                COALESCE(st.contributionbase, st.salarycapped, st.actualsalary, 0) AS ContributionBase,
                COALESCE(st.contributionbasis, 'gross') AS ContributionBasis,
                COALESCE(st.employeerate, 0) AS EmployeeRate,
                COALESCE(st.employerrate, 0) AS EmployerRate,
                COALESCE(st.employeeamount, 0) AS EmployeeAmount,
                COALESCE(st.employeramount, 0) AS EmployerAmount,
                COALESCE(st.totalamount, 0) AS TotalAmount,
                st.referencecode AS ReferenceCode,
                COALESCE(st.transactiontype, 'monthly') AS TransactionType,
                COALESCE(st.requeststatus, 'pending') AS RequestStatus,
                COALESCE(st.isenrolled, false) AS IsEnrolled,
                COALESCE(st.withdrawalrequest, false) AS WithdrawalRequest,
                st.withdrawalstatus AS WithdrawalStatus,
                st.createdat AS CreatedAt,
                COALESCE(st.updatedat, st.createdat) AS UpdatedAt,
                0 AS TotalCount
            FROM payroll.socialsecuritytransactions st
            JOIN employee.employees e ON e.employeeid = st.employeeid
            JOIN payroll.payrollperiods per ON per.periodid = st.periodid
            LEFT JOIN payroll.socialsecurityconfig sc ON sc.id = st.configid
            LEFT JOIN payroll.socialsecurityrules sr ON sr.ruleid = st.ruleid
            LEFT JOIN payroll.socialsecurityschemes ss ON ss.schemeid = st.schemeid
            LEFT JOIN payroll.socialsecurityjurisdictions sj ON sj.jurisdictionid = st.jurisdictionid
            LEFT JOIN payroll.socialsecurityauthorities sa ON sa.authorityid = st.authorityid
            WHERE {whereClause}
            ORDER BY st.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", (filter.Page - 1) * filter.PageSize);

        var totalCount = await QuerySingleAsync<int>(countSql, parameters);
        var data = await QueryAsync<SocialSecurityTransactionDto>(sql, parameters);

        return new PagedResult<SocialSecurityTransactionDto>
        {
            Data = data.ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PayrollOverviewDto> GetPayrollOverviewAsync(Guid organizationId)
    {
        var sql = @"
            SELECT 'Loan' as ModuleName 
            WHERE EXISTS (SELECT 1 FROM payroll.loanrules WHERE organizationid = @organizationId AND isactive = true AND isdeleted = false)
            
            UNION ALL
            
            SELECT 'Advance Salary' as ModuleName 
            WHERE EXISTS (SELECT 1 FROM payroll.salaryadvancerules WHERE organizationid = @organizationId AND isactive = true AND isdeleted = false)
            
            UNION ALL
            
            SELECT 'Provident Fund' as ModuleName 
            WHERE EXISTS (SELECT 1 FROM payroll.pfrules WHERE organizationid = @organizationId AND isactive = true AND isdeleted = false)
            
            UNION ALL
            
            SELECT 'Social Security' as ModuleName
            WHERE EXISTS (SELECT 1 FROM payroll.socialsecurityrules WHERE organizationid = @organizationId AND isactive = true AND isdeleted = false)
               OR EXISTS (SELECT 1 FROM payroll.socialsecurityconfig WHERE organizationid = @organizationId AND isactive = true AND isdeleted = false)
            
            UNION ALL
            
            SELECT 'Gratuity' as ModuleName 
            WHERE EXISTS (SELECT 1 FROM payroll.gratuityconfig WHERE organizationid = @organizationId AND isactive = true AND isdeleted = false);";

        var activeModules = await QueryAsync<string>(sql, new { organizationId });

        return new PayrollOverviewDto
        {
            ActiveModules = activeModules.ToList()
        };
    }

    // ─── Gratuity Config ─────────────────────────────────────────────────────

    public async Task<IEnumerable<GratuityConfigDto>> GetAllGratuityConfigsAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                id              AS Id,
                organizationid  AS OrganizationId,
                configrulename  AS ConfigRuleName,
                description     AS Description,
                yearsrequired   AS YearsRequired,
                calculationtype AS CalculationType,
                calculationvalue AS CalculationValue,
                isactive        AS IsActive,
                isdeleted       AS IsDeleted,
                createdat       AS CreatedAt,
                updatedat       AS UpdatedAt
            FROM payroll.gratuityconfig
            WHERE organizationid = @organizationId AND isdeleted = false
            ORDER BY createdat DESC;";

        return await QueryAsync<GratuityConfigDto>(sql, new { organizationId });
    }

    public async Task<IEnumerable<GratuityConfigDto>> GetActiveGratuityConfigsAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                id              AS Id,
                organizationid  AS OrganizationId,
                configrulename  AS ConfigRuleName,
                description     AS Description,
                yearsrequired   AS YearsRequired,
                calculationtype AS CalculationType,
                calculationvalue AS CalculationValue,
                isactive        AS IsActive,
                isdeleted       AS IsDeleted,
                createdat       AS CreatedAt,
                updatedat       AS UpdatedAt
            FROM payroll.gratuityconfig
            WHERE organizationid = @organizationId AND isdeleted = false AND isactive = true
            ORDER BY createdat DESC;";

        return await QueryAsync<GratuityConfigDto>(sql, new { organizationId });
    }

    public async Task<GratuityConfigDto?> GetGratuityConfigByIdAsync(Guid id, Guid organizationId)
    {
        var sql = @"
            SELECT
                id              AS Id,
                organizationid  AS OrganizationId,
                configrulename  AS ConfigRuleName,
                description     AS Description,
                yearsrequired   AS YearsRequired,
                calculationtype AS CalculationType,
                calculationvalue AS CalculationValue,
                isactive        AS IsActive,
                isdeleted       AS IsDeleted,
                createdat       AS CreatedAt,
                updatedat       AS UpdatedAt
            FROM payroll.gratuityconfig
            WHERE id = @id AND organizationid = @organizationId AND isdeleted = false;";

        return await QuerySingleOrDefaultAsync<GratuityConfigDto>(sql, new { id, organizationId });
    }

    public async Task<GratuityConfigDto> CreateGratuityConfigAsync(Guid organizationId, CreateGratuityConfigDto dto)
    {
        var sql = @"
            INSERT INTO payroll.gratuityconfig
                (organizationid, configrulename, description, yearsrequired, calculationtype, calculationvalue, isactive, isdeleted)
            VALUES
                (@organizationId, @ConfigRuleName, @Description, @YearsRequired, @CalculationType, @CalculationValue, @IsActive, false)
            RETURNING id;";

        var parameters = new
        {
            organizationId,
            dto.ConfigRuleName,
            dto.Description,
            dto.YearsRequired,
            dto.CalculationType,
            dto.CalculationValue,
            dto.IsActive
        };

        var newId = await QuerySingleAsync<Guid>(sql, parameters);
        return (await GetGratuityConfigByIdAsync(newId, organizationId))!;
    }

    public async Task<GratuityConfigDto?> UpdateGratuityConfigAsync(Guid id, Guid organizationId, UpdateGratuityConfigDto dto)
    {
        var sql = @"
            UPDATE payroll.gratuityconfig
            SET
                configrulename  = @ConfigRuleName,
                description     = @Description,
                yearsrequired   = @YearsRequired,
                calculationtype = @CalculationType,
                calculationvalue = @CalculationValue,
                isactive        = @IsActive,
                updatedat       = current_timestamp
            WHERE id = @id AND organizationid = @organizationId AND isdeleted = false
            RETURNING id;";

        var parameters = new
        {
            id,
            organizationId,
            dto.ConfigRuleName,
            dto.Description,
            dto.YearsRequired,
            dto.CalculationType,
            dto.CalculationValue,
            dto.IsActive
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty) return null;

        return await GetGratuityConfigByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteGratuityConfigAsync(Guid id, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.gratuityconfig
            SET isdeleted = true, updatedat = current_timestamp
            WHERE id = @id AND organizationid = @organizationId AND isdeleted = false
            RETURNING id;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { id, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    // ─── Gratuity Transactions ────────────────────────────────────────────────

    public async Task<PagedResult<GratuityTransactionDto>> GetAllGratuityTransactionsAsync(Guid organizationId, GratuityTransactionFilterDto filter)
    {
        try
        {
            var conditions = new List<string> { "gt.organizationid = @organizationId", "gt.isdeleted = false" };
            var parameters = new DynamicParameters();
            parameters.Add("organizationId", organizationId);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                conditions.Add("(e.firstname || ' ' || e.lastname) ILIKE @SearchTerm");
                parameters.Add("SearchTerm", $"%{filter.SearchTerm.Trim()}%");
            }

            if (!string.IsNullOrWhiteSpace(filter.PaymentStatus))
            {
                conditions.Add("gt.paymentstatus = @PaymentStatus");
                parameters.Add("PaymentStatus", filter.PaymentStatus);
            }

            if (filter.PeriodId.HasValue)
            {
                conditions.Add("gt.periodid = @PeriodId");
                parameters.Add("PeriodId", filter.PeriodId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.PaymentMethod))
            {
                conditions.Add("LOWER(gt.paymentmethod) = LOWER(@PaymentMethod)");
                parameters.Add("PaymentMethod", filter.PaymentMethod);
            }

            var whereClause = string.Join(" AND ", conditions);
            var offset = (filter.Page - 1) * filter.PageSize;

            var sql = $@"
            SELECT
                gt.id                   AS Id,
                gt.organizationid       AS OrganizationId,
                gt.employeeid           AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                d.departmentname        AS Department,
                gt.periodid             AS PeriodId,
                gt.configid             AS ConfigId,
                gc.configrulename       AS ConfigRuleName,
                gc.calculationtype      AS CalculationType,
                gc.calculationvalue     AS CalculationValue,
                gc.yearsrequired        AS YearsRequired,
                gt.yearsworked          AS YearsWorked,
                gt.lastsalary           AS LastSalary,
                gt.totalamount          AS TotalAmount,
                gt.paymentmethod        AS PaymentMethod,
                gt.paymentstatus        AS PaymentStatus,
                gt.approvedby           AS ApprovedBy,
                gt.approvedat           AS ApprovedAt,
                gt.paidat               AS PaidAt,
                gt.paidby               AS PaidBy,
                gt.cancellationreason   AS CancellationReason,
                gt.notes                AS Notes,
                gt.createdat            AS CreatedAt,
                gt.updatedat            AS UpdatedAt,
                COUNT(*) OVER()         AS TotalCount
            FROM payroll.gratuitytransactions gt
            INNER JOIN employee.employees e ON e.employeeid = gt.employeeid
            LEFT JOIN LATERAL (
                SELECT ed.departmentid
                FROM employee.employmentdetails ed
                WHERE ed.employeeid = e.employeeid
                ORDER BY COALESCE(ed.iscurrent, false) DESC, ed.effectivedate DESC, ed.createdat DESC
                LIMIT 1
            ) ed ON true
            LEFT JOIN employee.departments d ON d.departmentid = ed.departmentid
            LEFT JOIN payroll.gratuityconfig gc ON gc.id = gt.configid
            WHERE {whereClause}
            ORDER BY gt.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

            parameters.Add("PageSize", filter.PageSize);
            parameters.Add("Offset", offset);

            var data = await QueryAsync<GratuityTransactionDto>(sql, parameters);
            var totalCount = data.FirstOrDefault()?.TotalCount ?? 0;

            return new PagedResult<GratuityTransactionDto>
            {
                Data = data.ToList(),
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<GratuityTransactionDto?> GetGratuityTransactionByIdAsync(Guid id, Guid organizationId)
    {
        var sql = @"
            SELECT
                gt.id                   AS Id,
                gt.organizationid       AS OrganizationId,
                gt.employeeid           AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                d.departmentname        AS Department,
                gt.periodid             AS PeriodId,
                gt.configid             AS ConfigId,
                gc.configrulename       AS ConfigRuleName,
                gc.calculationtype      AS CalculationType,
                gc.calculationvalue     AS CalculationValue,
                gc.yearsrequired        AS YearsRequired,
                gt.yearsworked          AS YearsWorked,
                gt.lastsalary           AS LastSalary,
                gt.totalamount          AS TotalAmount,
                gt.paymentmethod        AS PaymentMethod,
                gt.paymentstatus        AS PaymentStatus,
                gt.approvedby           AS ApprovedBy,
                gt.approvedat           AS ApprovedAt,
                gt.paidat               AS PaidAt,
                gt.paidby               AS PaidBy,
                gt.cancellationreason   AS CancellationReason,
                gt.notes                AS Notes,
                gt.createdat            AS CreatedAt,
                gt.updatedat            AS UpdatedAt,
                0                       AS TotalCount
            FROM payroll.gratuitytransactions gt
            INNER JOIN employee.employees e ON e.employeeid = gt.employeeid
            LEFT JOIN LATERAL (
                SELECT ed.departmentid
                FROM employee.employmentdetails ed
                WHERE ed.employeeid = e.employeeid
                ORDER BY COALESCE(ed.iscurrent, false) DESC, ed.effectivedate DESC, ed.createdat DESC
                LIMIT 1
            ) ed ON true
            LEFT JOIN employee.departments d ON d.departmentid = ed.departmentid
            LEFT JOIN payroll.gratuityconfig gc ON gc.id = gt.configid
            WHERE gt.id = @id AND gt.organizationid = @organizationId AND gt.isdeleted = false;";

        return await QuerySingleOrDefaultAsync<GratuityTransactionDto>(sql, new { id, organizationId });
    }

    public async Task<GratuityTransactionDto> CreateGratuityTransactionAsync(Guid organizationId, Guid createdBy, CreateGratuityTransactionDto dto)
    {
        try
        {
            var paymentMethod = NormalizeGratuityPaymentMethod(dto.PaymentMethod);

            var configExistsSql = @"
            SELECT EXISTS (
                SELECT 1
                FROM payroll.gratuityconfig
                WHERE id = @ConfigId
                  AND organizationid = @organizationId
                  AND isdeleted = false
            );";

            var configExists = await ExecuteScalarAsync<bool>(configExistsSql, new { organizationId, dto.ConfigId });
            if (!configExists)
            {
                throw new InvalidOperationException("Selected gratuity rule is not available for this organization.");
            }

            var existingRecordSql = @"
            SELECT
                id              AS Id,
                paymentstatus   AS PaymentStatus
            FROM payroll.gratuitytransactions
            WHERE organizationid = @organizationId
              AND employeeid = @EmployeeId
              AND COALESCE(isdeleted, false) = false
            ORDER BY createdat DESC
            LIMIT 1;";

            var existingRecord = await QuerySingleOrDefaultAsync<ExistingGratuityRecordDto>(existingRecordSql, new { organizationId, dto.EmployeeId });
            if (existingRecord != null)
            {
                var status = (existingRecord.PaymentStatus ?? string.Empty).Trim().ToLowerInvariant();
                if (status is "paid" or "cancelled")
                {
                    await ExecuteAsync(@"
                    UPDATE payroll.gratuitytransactions
                    SET isdeleted = true,
                        updatedat = current_timestamp
                    WHERE id = @Id
                      AND organizationid = @organizationId
                      AND COALESCE(isdeleted, false) = false;",
                        new { existingRecord.Id, organizationId });
                }
                else
                {
                    throw new InvalidOperationException("An active gratuity record already exists for this employee. Approve, pay, cancel, or delete the existing record first.");
                }
            }

            var sql = @"
            INSERT INTO payroll.gratuitytransactions
                (organizationid, employeeid, periodid, configid, yearsworked, lastsalary, totalamount, paymentmethod, paymentstatus, notes, createdby, isdeleted)
            VALUES
                (@organizationId, @EmployeeId, @PeriodId, @ConfigId, @YearsWorked, @LastSalary, @TotalAmount, @PaymentMethod, 'calculated', @Notes, @createdBy, false)
            RETURNING id;";

            var parameters = new
            {
                organizationId,
                dto.EmployeeId,
                dto.PeriodId,
                dto.ConfigId,
                dto.YearsWorked,
                dto.LastSalary,
                dto.TotalAmount,
                PaymentMethod = paymentMethod,
                dto.Notes,
                createdBy
            };

            var newId = await QuerySingleAsync<Guid>(sql, parameters);
            return (await GetGratuityTransactionByIdAsync(newId, organizationId))!;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<GratuityTransactionDto?> UpdateGratuityTransactionAsync(Guid id, Guid organizationId, UpdateGratuityTransactionDto dto)
    {
        var paymentMethod = NormalizeOptionalGratuityPaymentMethod(dto.PaymentMethod);

        var sql = @"
            UPDATE payroll.gratuitytransactions
            SET
                configid                = @ConfigId,
                periodid                = @PeriodId,
                yearsworked             = @YearsWorked,
                lastsalary              = @LastSalary,
                totalamount             = @TotalAmount,
                paymentmethod           = COALESCE(@PaymentMethod, paymentmethod),
                paymentstatus           = @PaymentStatus,
                cancellationreason      = COALESCE(@CancellationReason, cancellationreason),
                notes                   = @Notes,
                updatedat               = current_timestamp
            WHERE id = @id AND organizationid = @organizationId AND isdeleted = false
            RETURNING id;";

        var parameters = new
        {
            id,
            organizationId,
            dto.ConfigId,
            dto.PeriodId,
            dto.YearsWorked,
            dto.LastSalary,
            dto.TotalAmount,
            PaymentMethod = paymentMethod,
            dto.PaymentStatus,
            dto.CancellationReason,
            dto.Notes
        };

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, parameters);
        if (updatedId == null || updatedId == Guid.Empty) return null;

        return await GetGratuityTransactionByIdAsync(updatedId.Value, organizationId);
    }

    public async Task<bool> DeleteGratuityTransactionAsync(Guid id, Guid organizationId)
    {
        var sql = @"
            UPDATE payroll.gratuitytransactions
            SET isdeleted = true, updatedat = current_timestamp
            WHERE id = @id AND organizationid = @organizationId AND isdeleted = false
            RETURNING id;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { id, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }

    public async Task<bool> ApproveGratuityTransactionAsync(Guid id, Guid organizationId, Guid approvedBy, ApproveGratuityDto dto)
    {
        var sql = @"
            UPDATE payroll.gratuitytransactions
            SET
                paymentstatus   = 'approved',
                approvedby      = @approvedBy,
                approvedat      = current_timestamp,
                notes           = COALESCE(@Notes, notes),
                updatedat       = current_timestamp
            WHERE id = @id AND organizationid = @organizationId AND paymentstatus = 'calculated' AND isdeleted = false
            RETURNING id;";

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { id, organizationId, approvedBy, dto.Notes });
        return updatedId != null && updatedId != Guid.Empty;
    }

    public async Task<bool> MarkGratuityPaidAsync(Guid id, Guid organizationId, Guid paidBy, MarkGratuityPaidDto dto)
    {
        var paymentMethod = NormalizeOptionalGratuityPaymentMethod(dto.PaymentMethod);

        var sql = @"
            UPDATE payroll.gratuitytransactions
            SET
                paymentstatus   = 'paid',
                paidat          = COALESCE(@PaidAt, current_timestamp),
                paidby          = @paidBy,
                periodid        = COALESCE(@PeriodId, periodid),
                paymentmethod   = COALESCE(@PaymentMethod, paymentmethod),
                notes           = COALESCE(@Notes, notes),
                updatedat       = current_timestamp
            WHERE id = @id AND organizationid = @organizationId AND paymentstatus = 'approved' AND isdeleted = false
            RETURNING id;";

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { id, organizationId, paidBy, dto.PaidAt, dto.PeriodId, PaymentMethod = paymentMethod, dto.Notes });
        return updatedId != null && updatedId != Guid.Empty;
    }

    public async Task<GratuityTransactionDto?> GetMyGratuityStatusAsync(Guid employeeId, Guid organizationId)
    {
        var sql = @"
            SELECT
                gt.id                   AS Id,
                gt.organizationid       AS OrganizationId,
                gt.employeeid           AS EmployeeId,
                gt.periodid             AS PeriodId,
                gt.configid             AS ConfigId,
                gc.configrulename       AS ConfigRuleName,
                gc.calculationtype      AS CalculationType,
                gc.calculationvalue     AS CalculationValue,
                gc.yearsrequired        AS YearsRequired,
                gt.yearsworked          AS YearsWorked,
                gt.lastsalary           AS LastSalary,
                gt.totalamount          AS TotalAmount,
                gt.paymentmethod        AS PaymentMethod,
                gt.paymentstatus        AS PaymentStatus,
                gt.approvedat           AS ApprovedAt,
                gt.paidat               AS PaidAt,
                gt.paidby               AS PaidBy,
                gt.cancellationreason   AS CancellationReason,
                gt.notes                AS Notes,
                gt.createdat            AS CreatedAt,
                gt.updatedat            AS UpdatedAt,
                0                       AS TotalCount
            FROM payroll.gratuitytransactions gt
            LEFT JOIN payroll.gratuityconfig gc ON gc.id = gt.configid
            WHERE gt.employeeid = @employeeId
              AND gt.organizationid = @organizationId
              AND gt.isdeleted = false
            ORDER BY gt.createdat DESC
            LIMIT 1;";

        return await QuerySingleOrDefaultAsync<GratuityTransactionDto>(sql, new { employeeId, organizationId });
    }

    // ============================================================================
    //  SOCIAL SECURITY — Enrollments & Requests (employee + admin lifecycle)
    // ============================================================================

    public async Task<SocialSecurityEnrollmentDto?> GetMySocialSecurityEnrollmentAsync(Guid organizationId, Guid employeeId)
    {
        var sql = @"
            SELECT
                e.enrollmentid          AS EnrollmentId,
                e.organizationid        AS OrganizationId,
                e.employeeid            AS EmployeeId,
                e.ruleid                AS RuleId,
                r.rulename              AS RuleName,
                r.schemeid              AS SchemeId,
                s.schemename            AS SchemeName,
                e.configid              AS ConfigId,
                c.configrulename        AS ConfigName,
                e.externalmemberid      AS ExternalMemberId,
                e.enrollmenttype        AS EnrollmentType,
                e.enrollmentstatus      AS EnrollmentStatus,
                e.effectivedate         AS EffectiveDate,
                e.enddate               AS EndDate,
                e.employeecustompct     AS EmployeeCustomPct,
                e.employercustompct     AS EmployerCustomPct,
                e.salarycapoverride     AS SalaryCapOverride,
                r.employeedefaultpct    AS RuleEmployeeDefaultPct,
                r.employerdefaultpct    AS RuleEmployerDefaultPct,
                r.contributionbasis     AS ContributionBasis,
                e.iscurrent             AS IsCurrent,
                e.createdat             AS CreatedAt,
                e.updatedat             AS UpdatedAt
            FROM payroll.socialsecurityemployeeenrollments e
            LEFT JOIN payroll.socialsecurityrules r   ON r.ruleid   = e.ruleid
            LEFT JOIN payroll.socialsecurityschemes s ON s.schemeid = r.schemeid
            LEFT JOIN payroll.socialsecurityconfig c  ON c.id       = e.configid
            WHERE e.organizationid = @organizationId
              AND e.employeeid     = @employeeId
              AND e.iscurrent      = TRUE
              AND e.isdeleted      = FALSE
            LIMIT 1;";

        return await QuerySingleOrDefaultAsync<SocialSecurityEnrollmentDto>(sql, new { organizationId, employeeId });
    }

    public async Task<PagedResult<SocialSecurityEnrollmentRequestDto>> GetMySocialSecurityRequestsAsync(Guid organizationId, Guid employeeId, MySocialSecurityRequestFilterDto filter)
    {
        var conditions = new List<string>
        {
            "r.organizationid = @organizationId",
            "r.employeeid     = @employeeId"
        };

        if (!string.IsNullOrWhiteSpace(filter.RequestType))
            conditions.Add("LOWER(r.requesttype) = LOWER(@RequestType)");

        if (!string.IsNullOrWhiteSpace(filter.RequestStatus))
            conditions.Add("LOWER(r.requeststatus) = LOWER(@RequestStatus)");

        var where = string.Join(" AND ", conditions);

        var sql = $@"
            SELECT
                r.requestid               AS RequestId,
                r.organizationid          AS OrganizationId,
                r.employeeid              AS EmployeeId,
                COALESCE(NULLIF(TRIM(emp.firstname || ' ' || emp.lastname), ''), 'Employee') AS EmployeeName,
                r.enrollmentid            AS EnrollmentId,
                r.ruleid                  AS RuleId,
                rl.rulename               AS RuleName,
                r.configid                AS ConfigId,
                cf.configrulename         AS ConfigName,
                r.requesttype             AS RequestType,
                r.requeststatus           AS RequestStatus,
                r.requestedat             AS RequestedAt,
                r.reason                  AS Reason,
                r.remarks                 AS Remarks,
                r.rejectionreason         AS RejectionReason,
                NULLIF((r.requestedvalues->>'employeePct')::text,'')::numeric  AS RequestedEmployeePct,
                NULLIF((r.requestedvalues->>'employerPct')::text,'')::numeric  AS RequestedEmployerPct,
                NULLIF((r.requestedvalues->>'salaryCap')::text,'')::numeric    AS RequestedSalaryCap,
                NULLIF(r.requestedvalues->>'effectiveDate','')::date           AS RequestedEffectiveDate,
                r.approvedby              AS ApprovedBy,
                r.approvedat              AS ApprovedAt,
                r.processedat             AS ProcessedAt,
                COALESCE((SELECT COUNT(1) FROM payroll.socialsecurityrequestdocuments d WHERE d.requestid = r.requestid), 0) AS DocumentCount,
                COUNT(*) OVER ()          AS TotalCount
            FROM payroll.socialsecurityrequests r
            LEFT JOIN employee.employees       emp ON emp.employeeid = r.employeeid
            LEFT JOIN payroll.socialsecurityrules rl ON rl.ruleid    = r.ruleid
            LEFT JOIN payroll.socialsecurityconfig cf ON cf.id       = r.configid
            WHERE {where}
            ORDER BY r.requestedat DESC
            LIMIT @PageSize OFFSET @Offset;";

        var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var rows = (await QueryAsync<SocialSecurityEnrollmentRequestDto>(sql, new
        {
            organizationId,
            employeeId,
            filter.RequestType,
            filter.RequestStatus,
            PageSize = pageSize,
            Offset = (page - 1) * pageSize
        })).ToList();

        return new PagedResult<SocialSecurityEnrollmentRequestDto>
        {
            Data = rows,
            TotalCount = rows.Count > 0 ? rows[0].TotalCount : 0,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<SocialSecurityEnrollmentRequestDto>> GetSocialSecurityEnrollmentRequestsAsync(Guid organizationId, SocialSecurityEnrollmentRequestFilterDto filter)
    {
        var conditions = new List<string> { "r.organizationid = @organizationId" };

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            conditions.Add("(emp.firstname || ' ' || emp.lastname) ILIKE '%' || @SearchTerm || '%'");

        if (!string.IsNullOrWhiteSpace(filter.RequestType))
            conditions.Add("LOWER(r.requesttype) = LOWER(@RequestType)");

        if (!string.IsNullOrWhiteSpace(filter.RequestStatus))
            conditions.Add("LOWER(r.requeststatus) = LOWER(@RequestStatus)");

        if (filter.RuleId.HasValue)
            conditions.Add("r.ruleid = @RuleId");

        var where = string.Join(" AND ", conditions);

        var sql = $@"
            SELECT
                r.requestid               AS RequestId,
                r.organizationid          AS OrganizationId,
                r.employeeid              AS EmployeeId,
                COALESCE(NULLIF(TRIM(emp.firstname || ' ' || emp.lastname), ''), 'Employee') AS EmployeeName,
                r.enrollmentid            AS EnrollmentId,
                r.ruleid                  AS RuleId,
                rl.rulename               AS RuleName,
                r.configid                AS ConfigId,
                cf.configrulename         AS ConfigName,
                r.requesttype             AS RequestType,
                r.requeststatus           AS RequestStatus,
                r.requestedat             AS RequestedAt,
                r.reason                  AS Reason,
                r.remarks                 AS Remarks,
                r.rejectionreason         AS RejectionReason,
                NULLIF((r.requestedvalues->>'employeePct')::text,'')::numeric  AS RequestedEmployeePct,
                NULLIF((r.requestedvalues->>'employerPct')::text,'')::numeric  AS RequestedEmployerPct,
                NULLIF((r.requestedvalues->>'salaryCap')::text,'')::numeric    AS RequestedSalaryCap,
                NULLIF(r.requestedvalues->>'effectiveDate','')::date           AS RequestedEffectiveDate,
                r.approvedby              AS ApprovedBy,
                r.approvedat              AS ApprovedAt,
                r.processedat             AS ProcessedAt,
                COALESCE((SELECT COUNT(1) FROM payroll.socialsecurityrequestdocuments d WHERE d.requestid = r.requestid), 0) AS DocumentCount,
                COUNT(*) OVER ()          AS TotalCount
            FROM payroll.socialsecurityrequests r
            LEFT JOIN employee.employees       emp ON emp.employeeid = r.employeeid
            LEFT JOIN payroll.socialsecurityrules rl ON rl.ruleid    = r.ruleid
            LEFT JOIN payroll.socialsecurityconfig cf ON cf.id       = r.configid
            WHERE {where}
            ORDER BY r.requestedat DESC
            LIMIT @PageSize OFFSET @Offset;";

        var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var rows = (await QueryAsync<SocialSecurityEnrollmentRequestDto>(sql, new
        {
            organizationId,
            filter.SearchTerm,
            filter.RequestType,
            filter.RequestStatus,
            filter.RuleId,
            PageSize = pageSize,
            Offset = (page - 1) * pageSize
        })).ToList();

        return new PagedResult<SocialSecurityEnrollmentRequestDto>
        {
            Data = rows,
            TotalCount = rows.Count > 0 ? rows[0].TotalCount : 0,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Guid?> CreateSocialSecurityEnrollmentRequestAsync(Guid organizationId, Guid employeeId, Guid requestedByUserId, CreateSocialSecurityEnrollmentRequestDto dto)
    {
        var requestType = string.IsNullOrWhiteSpace(dto.RequestType)
            ? "enrollment"
            : dto.RequestType.Trim().ToLowerInvariant();

        var allowedTypes = new[] { "enrollment", "update", "withdrawal", "suspension", "reactivation", "exemption", "correction" };
        if (!allowedTypes.Contains(requestType))
            throw new InvalidOperationException("Invalid request type.");

        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Block duplicate pending request of same type
            var dupSql = @"
                SELECT EXISTS (
                    SELECT 1 FROM payroll.socialsecurityrequests
                    WHERE organizationid = @organizationId
                      AND employeeid     = @employeeId
                      AND LOWER(requesttype) = @requestType
                      AND LOWER(requeststatus) = 'pending'
                );";
            var alreadyPending = await connection.ExecuteScalarAsync<bool>(dupSql, new { organizationId, employeeId, requestType }, transaction);
            if (alreadyPending)
            {
                transaction.Rollback();
                throw new InvalidOperationException("A pending request of this type already exists.");
            }

            // Find current enrollment (for non-enrollment request types)
            var currentEnrollmentSql = @"
                SELECT enrollmentid, ruleid
                FROM payroll.socialsecurityemployeeenrollments
                WHERE organizationid = @organizationId
                  AND employeeid     = @employeeId
                  AND iscurrent      = TRUE
                  AND isdeleted      = FALSE
                LIMIT 1;";
            var currentEnrollment = await connection.QuerySingleOrDefaultAsync<dynamic>(currentEnrollmentSql, new { organizationId, employeeId }, transaction);

            Guid? enrollmentId = currentEnrollment != null ? (Guid?)currentEnrollment.enrollmentid : null;
            Guid? resolvedRuleId = dto.RuleId ?? (currentEnrollment != null ? (Guid?)currentEnrollment.ruleid : null);

            // For enrollment requests, ruleId is required (or pick the org's first active rule)
            if (requestType == "enrollment" && !resolvedRuleId.HasValue)
            {
                var fallbackRule = await connection.ExecuteScalarAsync<Guid?>(@"
                    SELECT ruleid FROM payroll.socialsecurityrules
                    WHERE organizationid = @organizationId
                      AND isactive = TRUE AND isdeleted = FALSE
                    ORDER BY createdat DESC
                    LIMIT 1;", new { organizationId }, transaction);
                resolvedRuleId = fallbackRule;
            }

            var requestedValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                employeePct = dto.RequestedEmployeePct,
                employerPct = dto.RequestedEmployerPct,
                salaryCap = dto.RequestedSalaryCap,
                effectiveDate = dto.RequestedEffectiveDate?.ToString("yyyy-MM-dd")
            });

            var insertSql = @"
                INSERT INTO payroll.socialsecurityrequests
                    (organizationid, employeeid, enrollmentid, ruleid, configid,
                     requesttype, requeststatus, requestedat, requestedby,
                     requestedvalues, reason, createdat, updatedat)
                VALUES
                    (@organizationId, @employeeId, @enrollmentId, @ruleId, @configId,
                     @requestType, 'pending', current_timestamp, @requestedBy,
                     @requestedValues::jsonb, @reason, current_timestamp, current_timestamp)
                RETURNING requestid;";

            var requestId = await connection.QuerySingleAsync<Guid>(insertSql, new
            {
                organizationId,
                employeeId,
                enrollmentId,
                ruleId = resolvedRuleId,
                configId = dto.ConfigId,
                requestType,
                requestedBy = requestedByUserId,
                requestedValues,
                reason = dto.Reason
            }, transaction);

            // Insert documents (if any) tied to the request
            if (dto.Documents != null && dto.Documents.Count > 0)
            {
                var docSql = @"
                    INSERT INTO payroll.socialsecurityrequestdocuments
                        (requestid, organizationid, employeeid,
                         documenttype, documentname, fileurl, mimetype, filesize,
                         issuer, issueddate, expirydate, verifiedstatus, createdat)
                    VALUES
                        (@requestId, @organizationId, @employeeId,
                         @documentType, @documentName, @fileUrl, @mimeType, @fileSize,
                         @issuer, @issuedDate, @expiryDate, 'pending', current_timestamp);";

                foreach (var doc in dto.Documents.Where(d => !string.IsNullOrWhiteSpace(d.FileUrl)))
                {
                    await connection.ExecuteAsync(docSql, new
                    {
                        requestId,
                        organizationId,
                        employeeId,
                        documentType = string.IsNullOrWhiteSpace(doc.DocumentType) ? "supporting" : doc.DocumentType.Trim(),
                        documentName = string.IsNullOrWhiteSpace(doc.DocumentName) ? "Attachment" : doc.DocumentName.Trim(),
                        fileUrl = doc.FileUrl.Trim(),
                        mimeType = doc.MimeType,
                        fileSize = doc.FileSize,
                        issuer = doc.Issuer,
                        issuedDate = doc.IssuedDate,
                        expiryDate = doc.ExpiryDate
                    }, transaction);
                }
            }

            transaction.Commit();
            return requestId;
        }
        catch
        {
            try { transaction.Rollback(); } catch { /* ignore */ }
            throw;
        }
    }

    public async Task<bool> ApproveSocialSecurityEnrollmentRequestAsync(Guid organizationId, Guid requestId, Guid approvedByUserId, ApproveSocialSecurityEnrollmentRequestDto dto)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var reqRow = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
                SELECT requestid, employeeid, enrollmentid, ruleid, configid,
                       requesttype, requeststatus, requestedvalues
                FROM payroll.socialsecurityrequests
                WHERE requestid = @requestId AND organizationid = @organizationId
                FOR UPDATE;", new { requestId, organizationId }, transaction);

            if (reqRow == null)
            {
                transaction.Rollback();
                return false;
            }

            var status = ((string?)reqRow.requeststatus)?.Trim().ToLowerInvariant();
            if (status != "pending")
            {
                transaction.Rollback();
                return false;
            }

            var requestType = ((string?)reqRow.requesttype)?.Trim().ToLowerInvariant() ?? "enrollment";
            var employeeId = (Guid)reqRow.employeeid;
            Guid? ruleId = reqRow.ruleid != null ? (Guid?)reqRow.ruleid : null;
            Guid? configId = reqRow.configid != null ? (Guid?)reqRow.configid : null;
            Guid? existingEnrollmentId = reqRow.enrollmentid != null ? (Guid?)reqRow.enrollmentid : null;

            // Pull requested values from JSON, then apply admin overrides where supplied
            decimal? employeePct = dto.OverrideEmployeePct;
            decimal? employerPct = dto.OverrideEmployerPct;
            decimal? salaryCap = dto.OverrideSalaryCap;
            DateTime? effectiveDate = dto.EffectiveDate;

            if (employeePct == null || employerPct == null || salaryCap == null || effectiveDate == null)
            {
                var requested = await connection.QuerySingleOrDefaultAsync<dynamic>(@"
                    SELECT
                        NULLIF((requestedvalues->>'employeePct')::text,'')::numeric AS employeepct,
                        NULLIF((requestedvalues->>'employerPct')::text,'')::numeric AS employerpct,
                        NULLIF((requestedvalues->>'salaryCap')::text,'')::numeric AS salarycap,
                        NULLIF(requestedvalues->>'effectiveDate','')::date AS effdate
                    FROM payroll.socialsecurityrequests WHERE requestid = @requestId;",
                    new { requestId }, transaction);

                if (requested != null)
                {
                    employeePct ??= (decimal?)requested.employeepct;
                    employerPct ??= (decimal?)requested.employerpct;
                    salaryCap ??= (decimal?)requested.salarycap;
                    effectiveDate ??= (DateTime?)requested.effdate;
                }
            }

            effectiveDate ??= DateTime.UtcNow.Date;

            if (requestType == "enrollment" || requestType == "update" || requestType == "reactivation")
            {
                if (!ruleId.HasValue)
                {
                    transaction.Rollback();
                    throw new InvalidOperationException("Rule is required to approve this request.");
                }

                // Close current enrollment(s) for this rule
                await connection.ExecuteAsync(@"
                    UPDATE payroll.socialsecurityemployeeenrollments
                    SET iscurrent = FALSE,
                        enddate = CASE WHEN enddate IS NULL THEN @effectiveDate - INTERVAL '1 day' ELSE enddate END,
                        updatedat = current_timestamp
                    WHERE organizationid = @organizationId
                      AND employeeid     = @employeeId
                      AND ruleid         = @ruleId
                      AND iscurrent      = TRUE
                      AND isdeleted      = FALSE;",
                    new { organizationId, employeeId, ruleId, effectiveDate }, transaction);

                // Insert new current enrollment
                var newEnrollmentId = await connection.QuerySingleAsync<Guid>(@"
                    INSERT INTO payroll.socialsecurityemployeeenrollments
                        (organizationid, employeeid, ruleid, configid,
                         enrollmenttype, enrollmentstatus, effectivedate,
                         employeecustompct, employercustompct, salarycapoverride,
                         iscurrent, approvedby, approvedat, createdby, createdat, updatedat)
                    VALUES
                        (@organizationId, @employeeId, @ruleId, @configId,
                         CASE WHEN @requestType = 'enrollment' THEN 'voluntary' ELSE 'opt_in' END,
                         'active', @effectiveDate,
                         @employeePct, @employerPct, @salaryCap,
                         TRUE, @approvedBy, current_timestamp, @approvedBy, current_timestamp, current_timestamp)
                    RETURNING enrollmentid;",
                    new { organizationId, employeeId, ruleId, configId, requestType, effectiveDate, employeePct, employerPct, salaryCap, approvedBy = approvedByUserId },
                    transaction);

                existingEnrollmentId = newEnrollmentId;
            }
            else if (requestType == "withdrawal" || requestType == "suspension" || requestType == "exemption")
            {
                var newStatus = requestType == "withdrawal" ? "withdrawn"
                              : requestType == "suspension" ? "suspended"
                              : "closed";

                await connection.ExecuteAsync(@"
                    UPDATE payroll.socialsecurityemployeeenrollments
                    SET enrollmentstatus = @newStatus,
                        enddate = COALESCE(enddate, @effectiveDate),
                        iscurrent = CASE WHEN @newStatus = 'suspended' THEN iscurrent ELSE FALSE END,
                        updatedat = current_timestamp
                    WHERE organizationid = @organizationId
                      AND employeeid     = @employeeId
                      AND iscurrent      = TRUE
                      AND isdeleted      = FALSE;",
                    new { organizationId, employeeId, newStatus, effectiveDate }, transaction);
            }

            // Mark request approved + processed
            await connection.ExecuteAsync(@"
                UPDATE payroll.socialsecurityrequests
                SET requeststatus = 'approved',
                    enrollmentid  = COALESCE(enrollmentid, @enrollmentId),
                    approvedby    = @approvedBy,
                    approvedat    = current_timestamp,
                    processedat   = current_timestamp,
                    processedby   = @approvedBy,
                    remarks       = COALESCE(@remarks, remarks),
                    updatedat     = current_timestamp
                WHERE requestid = @requestId;",
                new { requestId, enrollmentId = existingEnrollmentId, approvedBy = approvedByUserId, remarks = dto.Remarks },
                transaction);

            transaction.Commit();
            return true;
        }
        catch
        {
            try { transaction.Rollback(); } catch { /* ignore */ }
            throw;
        }
    }

    public async Task<bool> RejectSocialSecurityEnrollmentRequestAsync(Guid organizationId, Guid requestId, Guid rejectedByUserId, RejectSocialSecurityEnrollmentRequestDto dto)
    {
        var rows = await ExecuteAsync(@"
            UPDATE payroll.socialsecurityrequests
            SET requeststatus   = 'rejected',
                rejectionreason = @rejectionReason,
                approvedby      = @rejectedBy,
                approvedat      = current_timestamp,
                processedat     = current_timestamp,
                processedby     = @rejectedBy,
                updatedat       = current_timestamp
            WHERE requestid       = @requestId
              AND organizationid  = @organizationId
              AND LOWER(requeststatus) = 'pending';",
            new { requestId, organizationId, rejectedBy = rejectedByUserId, rejectionReason = dto.RejectionReason });

        return rows > 0;
    }

    // ============================================================================
    //  SOCIAL SECURITY — Benefit Claims
    // ============================================================================

    public async Task<PagedResult<SocialSecurityClaimDto>> GetMySocialSecurityClaimsAsync(Guid organizationId, Guid employeeId, MySocialSecurityClaimFilterDto filter)
    {
        var conditions = new List<string>
        {
            "c.organizationid = @organizationId",
            "c.employeeid     = @employeeId"
        };

        if (!string.IsNullOrWhiteSpace(filter.ClaimType))
            conditions.Add("LOWER(c.claimtype) = LOWER(@ClaimType)");
        if (!string.IsNullOrWhiteSpace(filter.ClaimStatus))
            conditions.Add("LOWER(c.claimstatus) = LOWER(@ClaimStatus)");

        var where = string.Join(" AND ", conditions);

        var sql = $@"
            SELECT
                c.claimid           AS ClaimId,
                c.organizationid    AS OrganizationId,
                c.employeeid        AS EmployeeId,
                COALESCE(NULLIF(TRIM(emp.firstname || ' ' || emp.lastname), ''), 'Employee') AS EmployeeName,
                c.enrollmentid      AS EnrollmentId,
                c.requestid         AS RequestId,
                c.claimtype         AS ClaimType,
                c.claimstatus       AS ClaimStatus,
                c.claimdate         AS ClaimDate,
                c.incidentdate      AS IncidentDate,
                c.periodfrom        AS PeriodFrom,
                c.periodto          AS PeriodTo,
                c.currency          AS Currency,
                c.claimedamount     AS ClaimedAmount,
                c.approvedamount    AS ApprovedAmount,
                c.paidamount        AS PaidAmount,
                c.paymentreference  AS PaymentReference,
                c.paymentdate       AS PaymentDate,
                c.authorityreference AS AuthorityReference,
                c.decisionnotes     AS DecisionNotes,
                c.rejectionreason   AS RejectionReason,
                c.createdat         AS CreatedAt,
                c.updatedat         AS UpdatedAt,
                COALESCE((SELECT COUNT(1) FROM payroll.socialsecurityrequestdocuments d WHERE d.requestid = c.requestid), 0) AS DocumentCount,
                COUNT(*) OVER ()    AS TotalCount
            FROM payroll.socialsecurityclaims c
            LEFT JOIN employee.employees emp ON emp.employeeid = c.employeeid
            WHERE {where}
            ORDER BY c.claimdate DESC, c.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var rows = (await QueryAsync<SocialSecurityClaimDto>(sql, new
        {
            organizationId, employeeId,
            filter.ClaimType, filter.ClaimStatus,
            PageSize = pageSize,
            Offset = (page - 1) * pageSize
        })).ToList();

        return new PagedResult<SocialSecurityClaimDto>
        {
            Data = rows,
            TotalCount = rows.Count > 0 ? rows[0].TotalCount : 0,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<SocialSecurityClaimDto>> GetSocialSecurityClaimsAsync(Guid organizationId, SocialSecurityClaimFilterDto filter)
    {
        var conditions = new List<string> { "c.organizationid = @organizationId" };

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            conditions.Add("(emp.firstname || ' ' || emp.lastname) ILIKE '%' || @SearchTerm || '%'");
        if (!string.IsNullOrWhiteSpace(filter.ClaimType))
            conditions.Add("LOWER(c.claimtype) = LOWER(@ClaimType)");
        if (!string.IsNullOrWhiteSpace(filter.ClaimStatus))
            conditions.Add("LOWER(c.claimstatus) = LOWER(@ClaimStatus)");

        var where = string.Join(" AND ", conditions);

        var sql = $@"
            SELECT
                c.claimid           AS ClaimId,
                c.organizationid    AS OrganizationId,
                c.employeeid        AS EmployeeId,
                COALESCE(NULLIF(TRIM(emp.firstname || ' ' || emp.lastname), ''), 'Employee') AS EmployeeName,
                c.enrollmentid      AS EnrollmentId,
                c.requestid         AS RequestId,
                c.claimtype         AS ClaimType,
                c.claimstatus       AS ClaimStatus,
                c.claimdate         AS ClaimDate,
                c.incidentdate      AS IncidentDate,
                c.periodfrom        AS PeriodFrom,
                c.periodto          AS PeriodTo,
                c.currency          AS Currency,
                c.claimedamount     AS ClaimedAmount,
                c.approvedamount    AS ApprovedAmount,
                c.paidamount        AS PaidAmount,
                c.paymentreference  AS PaymentReference,
                c.paymentdate       AS PaymentDate,
                c.authorityreference AS AuthorityReference,
                c.decisionnotes     AS DecisionNotes,
                c.rejectionreason   AS RejectionReason,
                c.createdat         AS CreatedAt,
                c.updatedat         AS UpdatedAt,
                COALESCE((SELECT COUNT(1) FROM payroll.socialsecurityrequestdocuments d WHERE d.requestid = c.requestid), 0) AS DocumentCount,
                COUNT(*) OVER ()    AS TotalCount
            FROM payroll.socialsecurityclaims c
            LEFT JOIN employee.employees emp ON emp.employeeid = c.employeeid
            WHERE {where}
            ORDER BY c.claimdate DESC, c.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var rows = (await QueryAsync<SocialSecurityClaimDto>(sql, new
        {
            organizationId,
            filter.SearchTerm, filter.ClaimType, filter.ClaimStatus,
            PageSize = pageSize,
            Offset = (page - 1) * pageSize
        })).ToList();

        return new PagedResult<SocialSecurityClaimDto>
        {
            Data = rows,
            TotalCount = rows.Count > 0 ? rows[0].TotalCount : 0,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Guid?> CreateMySocialSecurityClaimAsync(Guid organizationId, Guid employeeId, Guid createdByUserId, CreateSocialSecurityClaimDto dto)
    {
        var allowed = new[] { "retirement", "disability", "survivor", "medical", "unemployment", "maternity", "injury", "other" };
        var claimType = string.IsNullOrWhiteSpace(dto.ClaimType) ? "" : dto.ClaimType.Trim().ToLowerInvariant();
        if (!allowed.Contains(claimType))
            throw new InvalidOperationException("Invalid claim type.");

        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var enrollmentId = await connection.ExecuteScalarAsync<Guid?>(@"
                SELECT enrollmentid FROM payroll.socialsecurityemployeeenrollments
                WHERE organizationid = @organizationId AND employeeid = @employeeId
                  AND iscurrent = TRUE AND isdeleted = FALSE
                LIMIT 1;",
                new { organizationId, employeeId }, transaction);

            // Backing request row to anchor documents (per schema design)
            var backingRequestId = await connection.QuerySingleAsync<Guid>(@"
                INSERT INTO payroll.socialsecurityrequests
                    (organizationid, employeeid, enrollmentid,
                     requesttype, requeststatus, requestedat, requestedby,
                     reason, createdat, updatedat)
                VALUES
                    (@organizationId, @employeeId, @enrollmentId,
                     'benefit_claim', 'submitted', current_timestamp, @createdBy,
                     @reason, current_timestamp, current_timestamp)
                RETURNING requestid;",
                new
                {
                    organizationId, employeeId, enrollmentId,
                    createdBy = createdByUserId,
                    reason = $"Benefit claim ({claimType})"
                }, transaction);

            var claimId = await connection.QuerySingleAsync<Guid>(@"
                INSERT INTO payroll.socialsecurityclaims
                    (organizationid, employeeid, enrollmentid, requestid,
                     claimtype, claimstatus, claimdate, incidentdate,
                     periodfrom, periodto, currency, claimedamount,
                     decisionnotes, createdby, createdat, updatedat)
                VALUES
                    (@organizationId, @employeeId, @enrollmentId, @requestId,
                     @claimType, 'submitted', current_date, @incidentDate,
                     @periodFrom, @periodTo, COALESCE(@currency, 'USD'), @claimedAmount,
                     @decisionNotes, @createdBy, current_timestamp, current_timestamp)
                RETURNING claimid;",
                new
                {
                    organizationId, employeeId, enrollmentId, requestId = backingRequestId,
                    claimType,
                    incidentDate = dto.IncidentDate,
                    periodFrom = dto.PeriodFrom,
                    periodTo = dto.PeriodTo,
                    currency = dto.Currency,
                    claimedAmount = dto.ClaimedAmount,
                    decisionNotes = dto.DecisionNotes,
                    createdBy = createdByUserId
                }, transaction);

            if (dto.Documents != null && dto.Documents.Count > 0)
            {
                var docSql = @"
                    INSERT INTO payroll.socialsecurityrequestdocuments
                        (requestid, organizationid, employeeid,
                         documenttype, documentname, fileurl, mimetype, filesize,
                         issuer, issueddate, expirydate, verifiedstatus, createdat)
                    VALUES
                        (@requestId, @organizationId, @employeeId,
                         @documentType, @documentName, @fileUrl, @mimeType, @fileSize,
                         @issuer, @issuedDate, @expiryDate, 'pending', current_timestamp);";

                foreach (var doc in dto.Documents.Where(d => !string.IsNullOrWhiteSpace(d.FileUrl)))
                {
                    await connection.ExecuteAsync(docSql, new
                    {
                        requestId = backingRequestId,
                        organizationId,
                        employeeId,
                        documentType = string.IsNullOrWhiteSpace(doc.DocumentType) ? "supporting" : doc.DocumentType.Trim(),
                        documentName = string.IsNullOrWhiteSpace(doc.DocumentName) ? "Attachment" : doc.DocumentName.Trim(),
                        fileUrl = doc.FileUrl.Trim(),
                        mimeType = doc.MimeType,
                        fileSize = doc.FileSize,
                        issuer = doc.Issuer,
                        issuedDate = doc.IssuedDate,
                        expiryDate = doc.ExpiryDate
                    }, transaction);
                }
            }

            transaction.Commit();
            return claimId;
        }
        catch
        {
            try { transaction.Rollback(); } catch { /* ignore */ }
            throw;
        }
    }

    public async Task<bool> ApproveSocialSecurityClaimAsync(Guid organizationId, Guid claimId, Guid approvedByUserId, ApproveSocialSecurityClaimDto dto)
    {
        var rows = await ExecuteAsync(@"
            UPDATE payroll.socialsecurityclaims
            SET claimstatus    = 'approved',
                approvedamount = @approvedAmount,
                decisionnotes  = COALESCE(@decisionNotes, decisionnotes),
                updatedat      = current_timestamp
            WHERE claimid       = @claimId
              AND organizationid = @organizationId
              AND LOWER(claimstatus) IN ('submitted','under_review','draft');",
            new { claimId, organizationId, approvedAmount = dto.ApprovedAmount, decisionNotes = dto.DecisionNotes });

        return rows > 0;
    }

    public async Task<bool> RejectSocialSecurityClaimAsync(Guid organizationId, Guid claimId, Guid rejectedByUserId, RejectSocialSecurityClaimDto dto)
    {
        var rows = await ExecuteAsync(@"
            UPDATE payroll.socialsecurityclaims
            SET claimstatus     = 'rejected',
                rejectionreason = @rejectionReason,
                updatedat       = current_timestamp
            WHERE claimid        = @claimId
              AND organizationid = @organizationId
              AND LOWER(claimstatus) IN ('submitted','under_review','approved');",
            new { claimId, organizationId, rejectionReason = dto.RejectionReason });

        return rows > 0;
    }

    public async Task<bool> MarkSocialSecurityClaimPaidAsync(Guid organizationId, Guid claimId, MarkSocialSecurityClaimPaidDto dto)
    {
        var rows = await ExecuteAsync(@"
            UPDATE payroll.socialsecurityclaims
            SET claimstatus        = 'paid',
                paidamount         = @paidAmount,
                paymentdate        = @paymentDate,
                paymentreference   = COALESCE(@paymentReference, paymentreference),
                authorityreference = COALESCE(@authorityReference, authorityreference),
                updatedat          = current_timestamp
            WHERE claimid        = @claimId
              AND organizationid = @organizationId
              AND LOWER(claimstatus) = 'approved';",
            new { claimId, organizationId, paidAmount = dto.PaidAmount, paymentDate = dto.PaymentDate, paymentReference = dto.PaymentReference, authorityReference = dto.AuthorityReference });

        return rows > 0;
    }

    // ============================================================================
    //  SOCIAL SECURITY — Documents
    // ============================================================================

    public async Task<IEnumerable<SocialSecurityRequestDocumentDto>> GetSocialSecurityRequestDocumentsAsync(Guid organizationId, Guid requestId)
    {
        var sql = @"
            SELECT
                d.documentid       AS DocumentId,
                d.requestid        AS RequestId,
                NULL::uuid         AS ClaimId,
                d.organizationid   AS OrganizationId,
                d.employeeid       AS EmployeeId,
                d.documenttype     AS DocumentType,
                d.documentname     AS DocumentName,
                d.fileurl          AS FileUrl,
                d.mimetype         AS MimeType,
                d.filesize         AS FileSize,
                d.issuer           AS Issuer,
                d.issueddate       AS IssuedDate,
                d.expirydate       AS ExpiryDate,
                d.verifiedstatus   AS VerifiedStatus,
                d.verifiedby       AS VerifiedBy,
                d.verifiedat       AS VerifiedAt,
                d.createdat        AS CreatedAt
            FROM payroll.socialsecurityrequestdocuments d
            WHERE d.requestid       = @requestId
              AND d.organizationid  = @organizationId
            ORDER BY d.createdat DESC;";

        return await QueryAsync<SocialSecurityRequestDocumentDto>(sql, new { requestId, organizationId });
    }

    public async Task<bool> VerifySocialSecurityRequestDocumentAsync(Guid organizationId, Guid documentId, Guid verifiedByUserId, VerifySocialSecurityRequestDocumentDto dto)
    {
        var status = string.IsNullOrWhiteSpace(dto.VerifiedStatus) ? "verified" : dto.VerifiedStatus.Trim().ToLowerInvariant();
        var allowed = new[] { "verified", "rejected", "expired" };
        if (!allowed.Contains(status))
            throw new InvalidOperationException("Invalid verification status.");

        var rows = await ExecuteAsync(@"
            UPDATE payroll.socialsecurityrequestdocuments
            SET verifiedstatus = @status,
                verifiedby     = @verifiedBy,
                verifiedat     = current_timestamp
            WHERE documentid     = @documentId
              AND organizationid = @organizationId;",
            new { documentId, organizationId, status, verifiedBy = verifiedByUserId });

        return rows > 0;
    }

    public async Task<bool> DeleteSocialSecurityRequestDocumentAsync(Guid organizationId, Guid documentId, Guid employeeId)
    {
        var rows = await ExecuteAsync(@"
            DELETE FROM payroll.socialsecurityrequestdocuments
            WHERE documentid     = @documentId
              AND organizationid = @organizationId
              AND employeeid     = @employeeId;",
            new { documentId, organizationId, employeeId });

        return rows > 0;
    }
    public async Task<PagedResult<PayrollResultItemDto>> GetPayrollResultsAsync(Guid organizationId, PayrollResultsFilterDto filter)
    {
        var sql = @"
            SELECT 
                pr.resultid, pr.organizationid, pr.employeeid, pr.periodid, pr.basicsalary, pr.totaldeductions, 
                pr.totalbonuses, pr.netsalary, pr.loandeductions, pr.salaryadvancedeductions, pr.pfdeductions, 
                pr.taxdeductions, pr.attendancedeductions, pr.lateattendancedeductions, pr.leavedeductions, 
                pr.performancebonuses, pr.generalbonuses, pr.gratuity, pr.overtimedetails, pr.calculatedat,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                e.employeecode AS EmployeeCode,
                d.departmentname AS DepartmentName,
                d.departmentid AS DepartmentId,
                COUNT(*) OVER() AS TotalCount
            FROM payroll.payrollresults pr
            INNER JOIN employee.employees e ON e.employeeid = pr.employeeid
            LEFT JOIN employee.employmentdetails ed ON ed.employeeid = e.employeeid AND ed.iscurrent = true
            LEFT JOIN employee.departments d ON d.departmentid = ed.departmentid
            WHERE pr.organizationid = @organizationId";

        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);

        if (filter.PeriodId.HasValue && filter.PeriodId != Guid.Empty)
        {
            sql += " AND pr.periodid = @PeriodId";
            parameters.Add("PeriodId", filter.PeriodId.Value);
        }

        if (filter.DepartmentId.HasValue && filter.DepartmentId != Guid.Empty)
        {
            sql += " AND ed.departmentid = @DepartmentId";
            parameters.Add("DepartmentId", filter.DepartmentId.Value);
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            sql += " AND (e.firstname || ' ' || e.lastname ILIKE @Search OR e.employeecode ILIKE @Search)";
            parameters.Add("Search", $"%{filter.SearchTerm}%");
        }

        sql += " ORDER BY pr.calculatedat DESC";
        sql += " OFFSET @Offset LIMIT @Limit;";

        parameters.Add("Offset", (filter.Page - 1) * filter.PageSize);
        parameters.Add("Limit", filter.PageSize);

        var items = (await QueryAsync<PayrollResultItemDto>(sql, parameters)).ToList();
        var totalCount = items.FirstOrDefault()?.TotalCount ?? 0;

        return new PagedResult<PayrollResultItemDto>
        {
            Data = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }
}
