using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

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
}
