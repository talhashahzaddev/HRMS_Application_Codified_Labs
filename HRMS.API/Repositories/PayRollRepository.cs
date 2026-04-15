using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using Dapper;

namespace HRMS.API.Repositories;

public class PayRollRepository : BaseRepository, IPayRollRepository
{
    public PayRollRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

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
                    t.slabid AS id,
                    t.organizationid,
                    t.slabrulename AS rulename,
                    t.description,
                    t.isactive,
                    t.isdeleted,
                    t.createdat,
                    'IncomeTaxPolicy' AS policyname
                FROM payroll.taxslabs t
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
        catch(Exception ex){
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
            ShiftId       = shiftId,
            ScheduledDays = scheduledDays,
            TotalDays     = totalDays,
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
            throw ;
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
                mincontribution AS MinContribution,
                maxcontribution AS MaxContribution,
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
                mincontribution AS MinContribution,
                maxcontribution AS MaxContribution,
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
                mincontribution AS MinContribution,
                maxcontribution AS MaxContribution,
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
                contributionbasis, mincontribution, maxcontribution, allowpartialwithdraw, vestingmonths, isactive, isdeleted
            )
            VALUES (
                @organizationId, @RuleName, @Description, @DefaultEmployeePct, @DefaultEmployerPct,
                @ContributionBasis, @MinContribution, @MaxContribution, @AllowPartialWithdraw, @VestingMonths, @IsActive, false
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
            dto.MinContribution,
            dto.MaxContribution,
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
                mincontribution = @MinContribution,
                maxcontribution = @MaxContribution,
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
            dto.MinContribution,
            dto.MaxContribution,
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

    public async Task<ProvidentFundEnrollmentDto> CreateProvidentFundEnrollmentAsync(Guid organizationId, Guid employeeId, CreateProvidentFundEnrollmentDto dto, decimal employerPct)
    {
        var sql = @"
            INSERT INTO payroll.providentfund (
                organizationid, employeeid, ruleid, employeepct, employerpct,
                requesttype, pfstatus, effectivefrom, isactive
            )
            VALUES (
                @organizationId, @employeeId, @RuleId, @EmployeePct, @EmployerPct,
                'enrollment', 'pending', @EffectiveFrom, false
            )
            ON CONFLICT (organizationid, employeeid)
            DO UPDATE SET
                ruleid = EXCLUDED.ruleid,
                employeepct = EXCLUDED.employeepct,
                employerpct = EXCLUDED.employerpct,
                requesttype = 'update',
                pfstatus = 'pending',
                effectivefrom = EXCLUDED.effectivefrom,
                approvedby = null,
                approvedat = null,
                remarks = null,
                rejectionreason = null,
                isactive = true,
                updatedat = current_timestamp
            RETURNING
                pfid AS PfId,
                organizationid AS OrganizationId,
                employeeid AS EmployeeId,
                ruleid AS RuleId,
                employeepct AS EmployeePct,
                employerpct AS EmployerPct,
                requesttype AS RequestType,
                pfstatus AS PfStatus,
                effectivefrom AS EffectiveFrom,
                approvedby AS ApprovedBy,
                approvedat AS ApprovedAt,
                remarks AS Remarks,
                rejectionreason AS RejectionReason,
                isactive AS IsActive,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt;";

        var parameters = new
        {
            organizationId,
            employeeId,
            dto.RuleId,
            dto.EmployeePct,
            EmployerPct = employerPct,
            dto.EffectiveFrom
        };

        return await QuerySingleAsync<ProvidentFundEnrollmentDto>(sql, parameters);
    }

    public async Task<ProvidentFundEnrollmentDto?> UpdateProvidentFundEnrollmentRequestAsync(Guid pfId, Guid organizationId, Guid employeeId, CreateProvidentFundEnrollmentDto dto, decimal employerPct)
    {
        var sql = @"
            UPDATE payroll.providentfund pf
            SET
                ruleid = @RuleId,
                employeepct = @EmployeePct,
                employerpct = @EmployerPct,
                effectivefrom = @EffectiveFrom,
                pfstatus = 'pending',
                updatedat = current_timestamp
            WHERE pfid = @pfId
              AND organizationid = @organizationId
              AND employeeid = @employeeId
              AND pf.pfstatus = 'pending'
            RETURNING
                pfid AS PfId,
                organizationid AS OrganizationId,
                employeeid AS EmployeeId,
                ruleid AS RuleId,
                employeepct AS EmployeePct,
                employerpct AS EmployerPct,
                requesttype AS RequestType,
                pfstatus AS PfStatus,
                effectivefrom AS EffectiveFrom,
                approvedby AS ApprovedBy,
                approvedat AS ApprovedAt,
                remarks AS Remarks,
                rejectionreason AS RejectionReason,
                isactive AS IsActive,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt;";

        return await QuerySingleOrDefaultAsync<ProvidentFundEnrollmentDto>(sql, new
        {
            pfId,
            organizationId,
            employeeId,
            dto.RuleId,
            dto.EmployeePct,
            EmployerPct = employerPct,
            dto.EffectiveFrom
        });
    }

    public async Task<ProvidentFundEnrollmentDto?> UpdateProvidentFundPercentageAsync(Guid organizationId, Guid employeeId, UpdateProvidentFundPercentageDto dto)
    {
        var sql = @"
            UPDATE payroll.providentfund pf
            SET
                ruleid = @RuleId,
                employeepct = @EmployeePct,
                employerpct = ruledata.defaultemployerpct,
                pfstatus = 'pending',
                requesttype = 'update',
                approvedby = null,
                approvedat = null,
                remarks = null,
                rejectionreason = null,
                updatedat = current_timestamp
            FROM (
                SELECT
                    pr.ruleid,
                    pr.defaultemployerpct
                FROM payroll.pfrules pr
                WHERE pr.organizationid = @organizationId
                  AND pr.ruleid = @RuleId
                  AND COALESCE(pr.isdeleted, false) = false
                  AND COALESCE(pr.isactive, true) = true
                LIMIT 1
            ) AS ruledata
            WHERE pf.organizationid = @organizationId
              AND pf.employeeid = @employeeId
              AND ruledata.ruleid = @RuleId
              AND pf.ruleid <> @RuleId
              AND pf.isactive = true
            RETURNING
                pf.pfid AS PfId,
                pf.organizationid AS OrganizationId,
                pf.employeeid AS EmployeeId,
                pf.ruleid AS RuleId,
                pf.employeepct AS EmployeePct,
                pf.employerpct AS EmployerPct,
                pf.requesttype AS RequestType,
                pf.pfstatus AS PfStatus,
                pf.effectivefrom AS EffectiveFrom,
                pf.approvedby AS ApprovedBy,
                pf.approvedat AS ApprovedAt,
                pf.remarks AS Remarks,
                pf.rejectionreason AS RejectionReason,
                pf.isactive AS IsActive,
                pf.createdat AS CreatedAt,
                pf.updatedat AS UpdatedAt;";

        return await QuerySingleOrDefaultAsync<ProvidentFundEnrollmentDto>(sql, new
        {
            organizationId,
            employeeId,
            dto.RuleId,
            dto.EmployeePct
        });
    }

    public async Task<ProvidentFundEnrollmentDto?> CreateProvidentFundWithdrawRequestAsync(Guid organizationId, Guid employeeId, CreateProvidentFundWithdrawRequestDto dto)
    {
        var sql = @"
            UPDATE payroll.providentfund
            SET
                requesttype = 'withdrawal',
                pfstatus = 'pending',
                remarks = @Reason,
                approvedby = null,
                approvedat = null,
                rejectionreason = null,
                updatedat = current_timestamp
            WHERE organizationid = @organizationId
              AND employeeid = @employeeId
              AND isactive = true
            RETURNING
                pfid AS PfId,
                organizationid AS OrganizationId,
                employeeid AS EmployeeId,
                ruleid AS RuleId,
                employeepct AS EmployeePct,
                employerpct AS EmployerPct,
                requesttype AS RequestType,
                pfstatus AS PfStatus,
                effectivefrom AS EffectiveFrom,
                approvedby AS ApprovedBy,
                approvedat AS ApprovedAt,
                remarks AS Remarks,
                rejectionreason AS RejectionReason,
                isactive AS IsActive,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt;";

        return await QuerySingleOrDefaultAsync<ProvidentFundEnrollmentDto>(sql, new
        {
            organizationId,
            employeeId,
            dto.Reason
        });
    }

    public async Task<ProvidentFundEnrollmentDto?> CreateProvidentFundSettlementRequestAsync(Guid organizationId, Guid employeeId, CreateProvidentFundSettlementRequestDto dto)
    {
        var sql = @"
            UPDATE payroll.providentfund
            SET
                requesttype = 'settlement',
                pfstatus = 'pending',
                effectivefrom = @EffectiveFrom,
                remarks = @Remarks,
                approvedby = null,
                approvedat = null,
                rejectionreason = null,
                updatedat = current_timestamp
            WHERE organizationid = @organizationId
              AND employeeid = @employeeId
              AND isactive = true
            RETURNING
                pfid AS PfId,
                organizationid AS OrganizationId,
                employeeid AS EmployeeId,
                ruleid AS RuleId,
                employeepct AS EmployeePct,
                employerpct AS EmployerPct,
                requesttype AS RequestType,
                pfstatus AS PfStatus,
                effectivefrom AS EffectiveFrom,
                approvedby AS ApprovedBy,
                approvedat AS ApprovedAt,
                remarks AS Remarks,
                rejectionreason AS RejectionReason,
                isactive AS IsActive,
                createdat AS CreatedAt,
                updatedat AS UpdatedAt;";

        return await QuerySingleOrDefaultAsync<ProvidentFundEnrollmentDto>(sql, new
        {
            organizationId,
            employeeId,
            dto.EffectiveFrom,
            dto.Remarks
        });
    }

    public async Task<bool> DeleteProvidentFundRequestAsync(Guid pfId, Guid organizationId, Guid employeeId)
    {
        var sql = @"
            DELETE FROM payroll.providentfund
            WHERE pfid = @pfId
              AND organizationid = @organizationId
              AND employeeid = @employeeId
              AND pfstatus = 'pending'
            RETURNING pfid;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new
        {
            pfId,
            organizationId,
            employeeId
        });

        return deletedId != null && deletedId != Guid.Empty;
    }

    public async Task<ProvidentFundEnrollmentDto?> GetMyPendingProvidentFundRequestAsync(Guid organizationId, Guid employeeId)
    {
        var sql = @"
            SELECT
                pfid AS PfId,
                pf.organizationid AS OrganizationId,
                employeeid AS EmployeeId,
                pf.ruleid AS RuleId,
                pr.rulename AS RuleName,
                pf.employeepct AS EmployeePct,
                pf.employerpct AS EmployerPct,
                requesttype AS RequestType,
                pfstatus AS PfStatus,
                effectivefrom AS EffectiveFrom,
                approvedby AS ApprovedBy,
                approvedat AS ApprovedAt,
                remarks AS Remarks,
                rejectionreason AS RejectionReason,
                pf.isactive AS IsActive,
                pf.createdat AS CreatedAt,
                pf.updatedat AS UpdatedAt
            FROM payroll.providentfund pf
            LEFT JOIN payroll.pfrules pr
              ON pr.ruleid = pf.ruleid
             AND pr.organizationid = pf.organizationid
            WHERE pf.organizationid = @organizationId
              AND pf.employeeid = @employeeId
            ORDER BY pf.updatedat DESC
            LIMIT 1;";

        return await QuerySingleOrDefaultAsync<ProvidentFundEnrollmentDto>(sql, new
        {
            organizationId,
            employeeId
        });
    }

    public async Task<ProvidentFundEnrollmentDto?> GetMyActiveProvidentFundRequestAsync(Guid organizationId, Guid employeeId)
    {
        var sql = @"
            SELECT
                pfid AS PfId,
                pf.organizationid AS OrganizationId,
                employeeid AS EmployeeId,
                pf.ruleid AS RuleId,
                pr.rulename AS RuleName,
                pf.employeepct AS EmployeePct,
                pf.employerpct AS EmployerPct,
                requesttype AS RequestType,
                pfstatus AS PfStatus,
                effectivefrom AS EffectiveFrom,
                approvedby AS ApprovedBy,
                approvedat AS ApprovedAt,
                remarks AS Remarks,
                rejectionreason AS RejectionReason,
                pf.isactive AS IsActive,
                pf.createdat AS CreatedAt,
                pf.updatedat AS UpdatedAt
            FROM payroll.providentfund pf
            LEFT JOIN payroll.pfrules pr
              ON pr.ruleid = pf.ruleid
             AND pr.organizationid = pf.organizationid
            WHERE pf.organizationid = @organizationId
              AND pf.employeeid = @employeeId
            ORDER BY pf.updatedat DESC
            LIMIT 1;";

        return await QuerySingleOrDefaultAsync<ProvidentFundEnrollmentDto>(sql, new
        {
            organizationId,
            employeeId
        });
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

        var whereClause = "WHERE pf.organizationid = @organizationId";

        if (!string.IsNullOrWhiteSpace(filter.RequestType))
            whereClause += " AND LOWER(pf.requesttype) = LOWER(@RequestType)";

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            whereClause += " AND (e.firstname || ' ' || e.lastname) ILIKE @SearchTerm";

        var countSql = $@"
            SELECT COUNT(1)
            FROM payroll.providentfund pf
            JOIN employee.Employees e ON e.employeeid = pf.employeeid
            {whereClause};";

        var sql = $@"
            SELECT
                pf.pfid AS PfId,
                pf.organizationid AS OrganizationId,
                pf.employeeid AS EmployeeId,
                (e.firstname || ' ' || e.lastname) AS EmployeeName,
                ed.basicsalary AS BasicSalary,
                pf.ruleid AS RuleId,
                pf.employeepct AS EmployeePct,
                pf.employerpct AS EmployerPct,
                pf.requesttype AS RequestType,
                pf.pfstatus AS PfStatus,
                pf.effectivefrom AS EffectiveFrom,
                pf.approvedby AS ApprovedBy,
                pf.approvedat AS ApprovedAt,
                pf.remarks AS Remarks,
                pf.rejectionreason AS RejectionReason,
                pf.isactive AS IsActive,
                pf.createdat AS CreatedAt,
                pf.updatedat AS UpdatedAt
            FROM payroll.providentfund pf
            JOIN employee.Employees e ON e.employeeid = pf.employeeid
            LEFT JOIN employee.EmploymentDetails ed ON ed.employeeid = e.employeeid AND ed.iscurrent = true
            {whereClause}
            ORDER BY pf.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        var parameters = new
        {
            organizationId,
            filter.RequestType,
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
        var normalizedRequestType = dto.RequestType?.Trim().ToLowerInvariant();
        var allowedTypes = new[] { "enrollment", "update", "withdrawal", "settlement" };
        if (string.IsNullOrWhiteSpace(normalizedRequestType) || !allowedTypes.Contains(normalizedRequestType))
            return false;

        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var pendingRequestSql = @"
                SELECT pfid
                FROM payroll.providentfund
                WHERE organizationid = @organizationId
                  AND employeeid = @EmployeeId
                  AND LOWER(requesttype) = @RequestType
                  AND pfstatus = 'pending'
                ORDER BY updatedat DESC
                LIMIT 1
                FOR UPDATE;";

            var pfId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                pendingRequestSql,
                new
                {
                    organizationId,
                    dto.EmployeeId,
                    RequestType = normalizedRequestType
                },
                transaction);

            if (pfId == null || pfId == Guid.Empty)
            {
                transaction.Rollback();
                return false;
            }

            string updateSql = normalizedRequestType switch
            {
                "enrollment" => @"
                    UPDATE payroll.providentfund
                    SET
                        pfstatus = 'active',
                        isactive = true,
                        approvedby = @approvedBy,
                        approvedat = current_timestamp,
                        updatedat = current_timestamp
                    WHERE pfid = @pfId
                      AND organizationid = @organizationId
                      AND employeeid = @EmployeeId
                      AND pfstatus = 'pending';",
                "update" => @"
                    UPDATE payroll.providentfund
                    SET
                        pfstatus = 'active',
                        approvedby = @approvedBy,
                        approvedat = current_timestamp,
                        updatedat = current_timestamp
                    WHERE pfid = @pfId
                      AND organizationid = @organizationId
                      AND employeeid = @EmployeeId
                      AND pfstatus = 'pending';",
                "withdrawal" => @"
                    UPDATE payroll.providentfund
                    SET
                        pfstatus = 'approved',
                        approvedby = @approvedBy,
                        approvedat = current_timestamp,
                        updatedat = current_timestamp
                    WHERE pfid = @pfId
                      AND organizationid = @organizationId
                      AND employeeid = @EmployeeId
                      AND pfstatus = 'pending';",
                "settlement" => @"
                    UPDATE payroll.providentfund
                    SET
                        pfstatus = 'closed',
                        isactive = false,
                        approvedby = @approvedBy,
                        approvedat = current_timestamp,
                        updatedat = current_timestamp
                    WHERE pfid = @pfId
                      AND organizationid = @organizationId
                      AND employeeid = @EmployeeId
                      AND pfstatus = 'pending';",
                _ => string.Empty
            };

            var updatedRows = await connection.ExecuteAsync(updateSql, new
            {
                pfId,
                organizationId,
                dto.EmployeeId,
                approvedBy
            }, transaction);

            if (updatedRows == 0)
            {
                transaction.Rollback();
                return false;
            }

            if (normalizedRequestType is "withdrawal" or "settlement")
            {
                var latestTransactionSql = @"
                    SELECT
                        periodid,
                        salarybasisamount,
                        employeepct,
                        employerpct
                    FROM payroll.pftransactions
                    WHERE organizationid = @organizationId
                      AND employeeid = @EmployeeId
                      AND pfid = @pfId
                    ORDER BY createdat DESC
                    LIMIT 1;";

                var latestTransaction = await connection.QuerySingleOrDefaultAsync<dynamic>(
                    latestTransactionSql,
                    new { organizationId, dto.EmployeeId, pfId },
                    transaction);

                if (latestTransaction == null)
                {
                    transaction.Rollback();
                    return false;
                }

                var totalsSql = @"
                    SELECT
                        COALESCE(SUM(employeeamount), 0) AS employeeamount,
                        COALESCE(SUM(employeramount), 0) AS employeramount
                    FROM payroll.pftransactions
                    WHERE organizationid = @organizationId
                      AND employeeid = @EmployeeId
                      AND pfid = @pfId;";

                var totals = await connection.QuerySingleAsync<dynamic>(
                    totalsSql,
                    new { organizationId, dto.EmployeeId, pfId },
                    transaction);

                decimal employeeAmount = totals.employeeamount;
                decimal employerAmount = totals.employeramount;
                decimal runningBalance = employeeAmount + employerAmount;

                var insertTransactionSql = @"
                    INSERT INTO payroll.pftransactions (
                        organizationid, employeeid, periodid, pfid,
                        salarybasisamount, employeepct, employerpct,
                        employeeamount, employeramount, runningbalance, transactiontype
                    )
                    VALUES (
                        @organizationId, @EmployeeId, @PeriodId, @pfId,
                        @SalaryBasisAmount, @EmployeePct, @EmployerPct,
                        @EmployeeAmount, @EmployerAmount, @RunningBalance, @TransactionType
                    );";

                await connection.ExecuteAsync(insertTransactionSql, new
                {
                    organizationId,
                    dto.EmployeeId,
                    pfId,
                    PeriodId = (Guid)latestTransaction.periodid,
                    SalaryBasisAmount = (decimal)latestTransaction.salarybasisamount,
                    EmployeePct = (decimal)latestTransaction.employeepct,
                    EmployerPct = (decimal)latestTransaction.employerpct,
                    EmployeeAmount = employeeAmount,
                    EmployerAmount = employerAmount,
                    RunningBalance = runningBalance,
                    TransactionType = normalizedRequestType
                }, transaction);
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

    public async Task<bool> RejectProvidentFundRequestAsync(Guid pfId, Guid organizationId, RejectProvidentFundRequestDto dto)
    {
        var sql = @"
            UPDATE payroll.providentfund
            SET
                pfstatus = 'rejected',
                remarks = @Remarks,
                updatedat = current_timestamp
            WHERE pfid = @pfId
              AND organizationid = @organizationId
              AND pfstatus = 'pending'
            RETURNING pfid;";

        var updatedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new
        {
            pfId,
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
                requesttype, pfstatus, effectivefrom, isactive
            )
            VALUES (
                @organizationId, @EmployeeId, @RuleId, @EmployeePct, @EmployerPct,
                'enrollment', 'active', @EffectiveFrom, true
            )
            ON CONFLICT (organizationid, employeeid)
            DO UPDATE SET
                ruleid = EXCLUDED.ruleid,
                employeepct = EXCLUDED.employeepct,
                employerpct = EXCLUDED.employerpct,
                requesttype = 'enrollment',
                pfstatus = 'active',
                effectivefrom = EXCLUDED.effectivefrom,
                approvedby = null,
                approvedat = null,
                remarks = null,
                rejectionreason = null,
                isactive = true,
                updatedat = current_timestamp
            RETURNING
                pfid AS PfId,
                organizationid AS OrganizationId,
                employeeid AS EmployeeId,
                ruleid AS RuleId,
                employeepct AS EmployeePct,
                employerpct AS EmployerPct,
                requesttype AS RequestType,
                pfstatus AS PfStatus,
                effectivefrom AS EffectiveFrom,
                approvedby AS ApprovedBy,
                approvedat AS ApprovedAt,
                remarks AS Remarks,
                rejectionreason AS RejectionReason,
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
}
