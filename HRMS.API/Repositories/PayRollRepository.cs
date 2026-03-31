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
            GratuityPolicy = BuildPolicySection(rows, "GratuityPolicy")
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

    // Attendance Summary
    public async Task<PagedResult<PayrollAttendanceSummaryDto>> GetAllAttendanceSummariesAsync(Guid organizationId, PayrollAttendanceSummaryFilterDto filter)
    {
        var conditions = new List<string> { "e.organizationid = @organizationId" };
        if (filter.PeriodId.HasValue) conditions.Add("as.periodid = @PeriodId");
        if (!string.IsNullOrEmpty(filter.EmployeeName)) conditions.Add("(e.firstname || ' ' || e.lastname) ILIKE @EmployeeName");

        var whereClause = string.Join(" AND ", conditions);

        var countSql = $@"
            SELECT COUNT(1)
            FROM payroll.attendancesummary AS as
            JOIN employee.Employees e ON as.employeeid = e.employeeid
            WHERE {whereClause};";

        var sql = $@"
            SELECT
                as.id AS Id,
                as.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                as.periodid AS PeriodId,
                as.ruleid AS RuleId,
                adr.rulename AS RuleName,
                as.shiftid AS ShiftId,
                s.shiftname AS ShiftName,
                as.scheduleddays AS ScheduledDays,
                as.totaldays AS TotalDays,
                as.presentdays AS PresentDays,
                as.absentdays AS AbsentDays,
                as.halfdays AS HalfDays,
                as.effectiveabsents AS EffectiveAbsents,
                as.absentdeduction AS AbsentDeduction,
                as.halfdaydeduction AS HalfDayDeduction,
                as.totaldeduction AS TotalDeduction,
                as.createdat AS CreatedAt,
                as.updatedat AS UpdatedAt
            FROM payroll.attendancesummary AS as
            JOIN employee.Employees e ON as.employeeid = e.employeeid
            LEFT JOIN payroll.attendancedeductionrules adr ON as.ruleid = adr.ruleid
            LEFT JOIN attendance.shifts s ON as.shiftid = s.shiftid
            WHERE {whereClause}
            ORDER BY as.createdat DESC
            LIMIT @PageSize OFFSET @Offset;";

        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);
        parameters.Add("PeriodId", filter.PeriodId);
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
        var sql = @"
            SELECT
                as.id AS Id,
                as.employeeid AS EmployeeId,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                as.periodid AS PeriodId,
                as.ruleid AS RuleId,
                adr.rulename AS RuleName,
                as.shiftid AS ShiftId,
                s.shiftname AS ShiftName,
                as.scheduleddays AS ScheduledDays,
                as.totaldays AS TotalDays,
                as.presentdays AS PresentDays,
                as.absentdays AS AbsentDays,
                as.halfdays AS HalfDays,
                as.effectiveabsents AS EffectiveAbsents,
                as.absentdeduction AS AbsentDeduction,
                as.halfdaydeduction AS HalfDayDeduction,
                as.totaldeduction AS TotalDeduction,
                as.createdat AS CreatedAt,
                as.updatedat AS UpdatedAt
            FROM payroll.attendancesummary AS as
            JOIN employee.Employees e ON as.employeeid = e.employeeid
            LEFT JOIN payroll.attendancedeductionrules adr ON as.ruleid = adr.ruleid
            LEFT JOIN attendance.shifts s ON as.shiftid = s.shiftid
            WHERE as.id = @id AND e.organizationid = @organizationId;";

        return await QuerySingleOrDefaultAsync<PayrollAttendanceSummaryDto>(sql, new { id, organizationId });
    }

    public async Task<PayrollAttendanceSummaryDto> CreateAttendanceSummaryAsync(
        Guid organizationId,
        CreatePayrollAttendanceSummaryDto dto,
        Guid? shiftId,
        int scheduledDays,
        int totalDays)
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

    public async Task<PayrollAttendanceSummaryDto?> UpdateAttendanceSummaryAsync(Guid id, Guid organizationId, UpdatePayrollAttendanceSummaryDto dto)
    {
        // Note: shiftid, scheduleddays, totaldays are intentionally NOT updated here;
        // they are auto-computed at creation time from the employee's shift assignment and the period dates.
        var sql = @"
            UPDATE payroll.attendancesummary AS as
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
            WHERE as.employeeid = e.employeeid AND as.id = @id AND e.organizationid = @organizationId
            RETURNING as.id;";

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
            DELETE FROM payroll.attendancesummary AS as
            USING employee.Employees e
            WHERE as.employeeid = e.employeeid AND as.id = @id AND e.organizationid = @organizationId
            RETURNING as.id;";

        var deletedId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { id, organizationId });
        return deletedId != null && deletedId != Guid.Empty;
    }
}
