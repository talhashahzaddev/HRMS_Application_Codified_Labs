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
                    ss.id AS id,
                    ss.organizationid,
                    ss.configrulename AS rulename,
                    ss.description,
                    ss.isactive,
                    ss.isdeleted,
                    ss.createdat,
                    'SocialSecurityPolicy' AS policyname
                FROM payroll.socialsecurityconfig ss
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
        catch (Exception ex) {
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
                       SUM(late_minutes) as late_minutes
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
            WHERE EXISTS (SELECT 1 FROM payroll.socialsecurityconfig WHERE organizationid = @organizationId AND isactive = true AND isdeleted = false)
            
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
}
