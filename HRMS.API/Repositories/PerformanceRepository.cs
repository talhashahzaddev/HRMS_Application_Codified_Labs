using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Dapper;

namespace HRMS.API.Repositories;

public class PerformanceRepository : BaseRepository, IPerformanceRepository
{
    public PerformanceRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<AppraisalCycle> CreateAppraisalCycleAsync(Guid organizationId, string cycleName, DateTime startDate, DateTime endDate, string? description = null)
    {
        var cycleId = await ExecuteScalarAsync<Guid>(
            "SELECT performance.fn_create_appraisal_cycle(@organizationId, @cycleName, @startDate, @endDate, @description)",
            new { organizationId, cycleName, startDate, endDate, description });

        return new AppraisalCycle
        {
            CycleId = cycleId,
            OrganizationId = organizationId,
            CycleName = cycleName,
            StartDate = startDate,
            EndDate = endDate,
            Description = description
        };
    }

    public async Task<List<AppraisalCycleDto>> GetAppraisalCyclesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT ac.cycleid, ac.cyclename, ac.description, ac.startdate, ac.enddate,
                   ac.status, ac.ratingscale, ac.selfreviewenabled, ac.peerreviewenabled,
                   ac.managerreviewenabled,
                   COUNT(ea.appraisalid) as total_appraisals,
                   COUNT(CASE WHEN ea.status = 'completed' THEN 1 END) as completed_appraisals
            FROM performance.appraisalcycles ac
            LEFT JOIN performance.employeeappraisals ea ON ac.cycleid = ea.cycleid
            WHERE ac.organizationid = @organizationId
            GROUP BY ac.cycleid, ac.cyclename, ac.description, ac.startdate, ac.enddate,
                     ac.status, ac.ratingscale, ac.selfreviewenabled, ac.peerreviewenabled,
                     ac.managerreviewenabled
            ORDER BY ac.startdate DESC";

        var results = await QueryAsync<AppraisalCycleDto>(sql, new { organizationId });
        return results.ToList();
    }

    public async Task<EmployeeAppraisal> CreateEmployeeAppraisalAsync(EmployeeAppraisal appraisal)
    {
        var sql = @"
            INSERT INTO performance.employee_appraisals (
                cycle_id, employee_id, reviewer_id, review_type, overall_rating,
                kra_ratings, skill_ratings, goals_achieved, feedback, improvement_areas,
                development_plan, status
            )
            VALUES (
                @cycleId, @employeeId, @reviewerId, @reviewType, @overallRating,
                @kraRatings::jsonb, @skillRatings::jsonb, @goalsAchieved::jsonb,
                @feedback, @improvementAreas, @developmentPlan, @status
            )
            RETURNING appraisal_id";

        var appraisalId = await ExecuteScalarAsync<Guid>(sql, new
        {
            cycleId = appraisal.CycleId,
            employeeId = appraisal.EmployeeId,
            reviewerId = appraisal.ReviewerId,
            reviewType = appraisal.ReviewType,
            overallRating = appraisal.OverallRating,
            kraRatings = System.Text.Json.JsonSerializer.Serialize(appraisal.KraRatings),
            skillRatings = System.Text.Json.JsonSerializer.Serialize(appraisal.SkillRatings),
            goalsAchieved = System.Text.Json.JsonSerializer.Serialize(appraisal.GoalsAchieved),
            feedback = appraisal.Feedback,
            improvementAreas = appraisal.ImprovementAreas,
            developmentPlan = appraisal.DevelopmentPlan,
            status = appraisal.Status
        });

        appraisal.AppraisalId = appraisalId;
        return appraisal;
    }

    public async Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsAsync(Guid cycleId, Guid? employeeId = null)
    {
        var sql = @"
            SELECT ea.appraisalid, ea.employeeid, ea.reviewerid, ea.reviewtype,
                   ea.overallrating, ea.kraratings, ea.skillratings, ea.feedback,
                   ea.improvementareas, ea.status, ea.submittedat, ea.reviewedat,
                   CONCAT(e.firstname, ' ', e.lastname) as employee_name,
                   CONCAT(r.firstname, ' ', r.lastname) as reviewer_name,
                   ac.cyclename
            FROM performance.employeeappraisals ea
            INNER JOIN employee.employees e ON ea.employeeid = e.employeeid
            LEFT JOIN employee.employees r ON ea.reviewerid = r.employeeid
            INNER JOIN performance.appraisalcycles ac ON ea.cycleid = ac.cycleid
            WHERE ea.cycleid = @cycleId
              AND (@employeeId IS NULL OR ea.employeeid = @employeeId)
            ORDER BY e.firstname, e.lastname";

        var results = await QueryAsync<EmployeeAppraisalDto>(sql, new { cycleId, employeeId });
        return results.ToList();
    }

    public async Task<List<SkillSetDto>> GetSkillSetsAsync(Guid organizationId)
    {
        var sql = @"
            SELECT skillid, skillname, category, description, skilllevelscale, isactive
            FROM performance.skillsets
            WHERE organizationid = @organizationId AND isactive = true
            ORDER BY category, skillname";

        var results = await QueryAsync<SkillSetDto>(sql, new { organizationId });
        return results.ToList();
    }

    public async Task<List<EmployeeSkillDto>> GetEmployeeSkillsAsync(Guid employeeId)
    {
        var sql = @"
            SELECT es.employee_skill_id, es.employee_id, es.skill_id, es.proficiency_level,
                   es.assessed_by, es.last_assessed, es.notes,
                   CONCAT(e.first_name, ' ', e.last_name) as employee_name,
                   s.skill_name, s.category as skill_category,
                   CONCAT(a.first_name, ' ', a.last_name) as assessor_name
            FROM performance.employee_skills es
            INNER JOIN employee.employees e ON es.employee_id = e.employee_id
            INNER JOIN performance.skill_sets s ON es.skill_id = s.skill_id
            LEFT JOIN employee.employees a ON es.assessed_by = a.employee_id
            WHERE es.employee_id = @employeeId
            ORDER BY s.category, s.skill_name";

        var results = await QueryAsync<EmployeeSkillDto>(sql, new { employeeId });
        return results.ToList();
    }

    public async Task<PerformanceSummaryDto> GetPerformanceSummaryAsync(Guid organizationId, Guid? cycleId = null)
    {
        var sql = @"
            SELECT 
                AVG(overallrating) as average_rating,
                COUNT(*) as total_appraisals,
                COUNT(CASE WHEN ea.status = 'pending' THEN 1 END) as pending_appraisals
            FROM performance.employeeappraisals ea
            INNER JOIN employee.employees e ON ea.employeeid = e.employeeid
            WHERE e.organizationid = @organizationId
              AND (@cycleId IS NULL OR ea.cycleid = @cycleId)
              AND ea.overallrating IS NOT NULL";

        var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new { organizationId, cycleId });

        return new PerformanceSummaryDto
        {
            AverageRating = result?.average_rating ?? 0,
            TotalAppraisals = result?.total_appraisals ?? 0,
            PendingAppraisals = result?.pending_appraisals ?? 0
        };
    }

    public async Task<bool> UpdateAppraisalStatusAsync(Guid appraisalId, string status)
    {
        var sql = @"
            UPDATE performance.employee_appraisals 
            SET status = @status, 
                submitted_at = CASE WHEN @status = 'submitted' THEN CURRENT_TIMESTAMP ELSE submitted_at END,
                reviewed_at = CASE WHEN @status = 'completed' THEN CURRENT_TIMESTAMP ELSE reviewed_at END,
                updated_at = CURRENT_TIMESTAMP
            WHERE appraisal_id = @appraisalId";

        var affected = await ExecuteAsync(sql, new { appraisalId, status });
        return affected > 0;
    }

    public async Task<List<TopPerformerDto>> GetTopPerformersAsync(Guid organizationId, int count = 10)
    {
        var sql = @"
            SELECT CONCAT(e.first_name, ' ', e.last_name) as employee_name,
                   AVG(ea.overall_rating) as rating,
                   d.department_name as department
            FROM performance.employee_appraisals ea
            INNER JOIN employee.employees e ON ea.employee_id = e.employee_id
            LEFT JOIN employee.employment_details ed ON e.employee_id = ed.employee_id AND ed.is_current = true
            LEFT JOIN employee.departments d ON ed.department_id = d.department_id
            WHERE e.organization_id = @organizationId
              AND ea.overall_rating IS NOT NULL
            GROUP BY e.employee_id, e.first_name, e.last_name, d.department_name
            ORDER BY AVG(ea.overall_rating) DESC
            LIMIT @count";

        var results = await QueryAsync<TopPerformerDto>(sql, new { organizationId, count });
        return results.ToList();
    }
}
