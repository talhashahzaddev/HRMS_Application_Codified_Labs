using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class PayRollRepository : BaseRepository, IPayRollRepository
{
    public PayRollRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<IEnumerable<PayRollRuleDto>> GetAllRulesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT
                x.id AS Id,
                x.organizationid AS OrganizationId,
                x.rulename AS RuleName,
                x.description AS Description,
                x.rulescount AS RulesCount,
                x.isactive AS IsActive,
                x.isdeleted AS IsDeleted,
                x.createdat AS CreatedAt
            FROM (
                SELECT
                    o.ruleid AS id,
                    o.organizationid,
                    o.rulename,
                    o.description,
                    COUNT(*) OVER (PARTITION BY o.organizationid) AS rulescount,
                    o.isactive,
                    o.isdeleted,
                    o.createdat
                FROM payroll.overtimerules o
                WHERE o.organizationid = @organizationId

                UNION ALL

                SELECT
                    a.ruleid AS id,
                    a.organizationid,
                    a.rulename,
                    a.description,
                    COUNT(*) OVER (PARTITION BY a.organizationid) AS rulescount,
                    a.isactive,
                    a.isdeleted,
                    a.createdat
                FROM payroll.attendancedeductionrules a
                WHERE a.organizationid = @organizationId

                UNION ALL

                SELECT
                    l.ruleid AS id,
                    l.organizationid,
                    l.rulename,
                    l.description,
                    COUNT(*) OVER (PARTITION BY l.organizationid) AS rulescount,
                    l.isactive,
                    l.isdeleted,
                    l.createdat
                FROM payroll.lateattendancerules l
                WHERE l.organizationid = @organizationId

                UNION ALL

                SELECT
                    lr.ruleid AS id,
                    lr.organizationid,
                    lr.rulename,
                    lr.description,
                    COUNT(*) OVER (PARTITION BY lr.organizationid) AS rulescount,
                    lr.isactive,
                    lr.isdeleted,
                    lr.createdat
                FROM payroll.leaverules lr
                WHERE lr.organizationid = @organizationId

                UNION ALL

                SELECT
                    p.ruleid AS id,
                    p.organizationid,
                    p.rulename,
                    p.description,
                    COUNT(*) OVER (PARTITION BY p.organizationid) AS rulescount,
                    p.isactive,
                    p.isdeleted,
                    p.createdat
                FROM payroll.performancerules p
                WHERE p.organizationid = @organizationId

                UNION ALL

                SELECT
                    lo.ruleid AS id,
                    lo.organizationid,
                    lo.rulename,
                    lo.description,
                    COUNT(*) OVER (PARTITION BY lo.organizationid) AS rulescount,
                    lo.isactive,
                    lo.isdeleted,
                    lo.createdat
                FROM payroll.loanrules lo
                WHERE lo.organizationid = @organizationId

                UNION ALL

                SELECT
                    s.ruleid AS id,
                    s.organizationid,
                    s.rulename,
                    s.description,
                    COUNT(*) OVER (PARTITION BY s.organizationid) AS rulescount,
                    s.isactive,
                    s.isdeleted,
                    s.createdat
                FROM payroll.salaryadvancerules s
                WHERE s.organizationid = @organizationId

                UNION ALL

                SELECT
                    pf.ruleid AS id,
                    pf.organizationid,
                    pf.rulename,
                    pf.description,
                    COUNT(*) OVER (PARTITION BY pf.organizationid) AS rulescount,
                    pf.isactive,
                    pf.isdeleted,
                    pf.createdat
                FROM payroll.pfrules pf
                WHERE pf.organizationid = @organizationId

                UNION ALL

                SELECT
                    t.slabid AS id,
                    t.organizationid,
                    t.slabrulename AS rulename,
                    t.description,
                    COUNT(*) OVER (PARTITION BY t.organizationid) AS rulescount,
                    t.isactive,
                    t.isdeleted,
                    t.createdat
                FROM payroll.taxslabs t
                WHERE t.organizationid = @organizationId

                UNION ALL

                SELECT
                    ss.id AS id,
                    ss.organizationid,
                    ss.configrulename AS rulename,
                    ss.description,
                    COUNT(*) OVER (PARTITION BY ss.organizationid) AS rulescount,
                    ss.isactive,
                    ss.isdeleted,
                    ss.createdat
                FROM payroll.socialsecurityconfig ss
                WHERE ss.organizationid = @organizationId

                UNION ALL

                SELECT
                    g.id AS id,
                    g.organizationid,
                    g.configrulename AS rulename,
                    g.description,
                    COUNT(*) OVER (PARTITION BY g.organizationid) AS rulescount,
                    g.isactive,
                    g.isdeleted,
                    g.createdat
                FROM payroll.gratuityconfig g
                WHERE g.organizationid = @organizationId
            ) x
            ORDER BY x.createdat DESC;";

        return await QueryAsync<PayRollRuleDto>(sql, new { organizationId });
    }
}
