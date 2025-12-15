using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using System.Diagnostics.Metrics;

namespace HRMS.API.Repositories;

public class PerformanceRepository : BaseRepository, IPerformanceRepository
{
    public PerformanceRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<Guid> GetEmpId(Guid? employeeId)
    {
        try
        {
            var sql = @"
            SELECT em.employeeid
            FROM employee.employees em
            WHERE ( em.userid = @employeeId)
            LIMIT 1";

            var results = await QueryAsync<Guid>(sql, new { employeeId });

            return results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            return Guid.Empty;
        }
    }



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
        try
        {
            // Step 1: Check if record already exists 
            var checkSql = @"
        SELECT appraisalid
        FROM performance.employeeappraisals
        WHERE cycleid = @cycleId 
          AND employeeid = @employeeId
          AND reviewerid = @reviewerId
          AND reviewtype = @reviewType
    ";

            var existingId = await ExecuteScalarAsync<Guid?>(checkSql, new
            {
                cycleId = appraisal.CycleId,
                employeeId = appraisal.EmployeeId,
                reviewerId = appraisal.ReviewerId,
                reviewType = appraisal.ReviewType
            });

            // ---------------------------------------------------------------
            // CASE 1: RECORD EXISTS → UPDATE
            // ---------------------------------------------------------------
            if (existingId != null)
            {
                var updateSql = @"
    UPDATE performance.employeeappraisals
    SET
        kraratings = COALESCE(kraratings, '{}'::jsonb) 
                     || COALESCE(@kraRatings::jsonb, '{}'::jsonb),

        feedback = CASE 
                      WHEN @feedback IS NOT NULL AND @feedback <> '' 
                      THEN COALESCE(feedback, '') || E'\n' || @feedback
                      ELSE feedback
                   END,

        improvementareas = CASE 
                              WHEN @improvementAreas IS NOT NULL AND @improvementAreas <> '' 
                              THEN COALESCE(improvementareas, '') || E'\n' || @improvementAreas
                              ELSE improvementareas
                           END,

        developmentplan = CASE 
                              WHEN @developmentPlan IS NOT NULL AND @developmentPlan <> '' 
                              THEN COALESCE(developmentplan, '') || E'\n' || @developmentPlan
                              ELSE developmentplan
                          END,

        updatedat = NOW()
    WHERE appraisalid = @id
    RETURNING appraisalid;
";

                var updatedId = await ExecuteScalarAsync<Guid>(updateSql, new
                {
                    id = existingId,
                    kraRatings = System.Text.Json.JsonSerializer.Serialize(appraisal.KraRatings),
                    feedback = appraisal.Feedback,
                    improvementAreas = appraisal.ImprovementAreas,
                    developmentPlan = appraisal.DevelopmentPlan
                });


                appraisal.AppraisalId = updatedId;
                return appraisal;
            }

            // ---------------------------------------------------------------
            // CASE 2: RECORD DOES NOT EXIST → INSERT
            // ---------------------------------------------------------------
            var insertSql = @"
        INSERT INTO performance.employeeappraisals (
            cycleid, employeeid, reviewerid, reviewtype, overallrating,
            kraratings, skillratings, goalsachieved, feedback, 
            improvementareas, developmentplan, status
        )
        VALUES (
            @cycleId, @employeeId, @reviewerId, @reviewType, @overallRating,
            @kraRatings::jsonb, @skillRatings::jsonb, @goalsAchieved::jsonb,
            @feedback, @improvementAreas, @developmentPlan, @status
        )
        RETURNING appraisalid;
    ";

            var newId = await ExecuteScalarAsync<Guid>(insertSql, new
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

            appraisal.AppraisalId = newId;
            return appraisal;
        }
        catch (Exception)
        {
            return null;
        }

    }

    public async Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsAsync(Guid cycleId, Guid employeeId)
    {
        var sql = @"
    SELECT 
        ea.appraisalid, 
        ea.employeeid, 
        ea.reviewerid, 
        ea.reviewtype,
        ea.cycleid,

        /* ===========================================
           CURRENT RATING (KRA-based for current cycle)
           Based on KRA and weights only (100% KRA)
        =========================================== */
        CASE
            /* KRA EXISTS → Calculate based on KRA weights (100%) */
            WHEN (ea.kraratings IS NOT NULL AND ea.kraratings != '{}'::jsonb)
            THEN COALESCE((
                SELECT 
                    SUM(
                        (k.weightpercentage::numeric / 100.0) * kr.rating::numeric
                    )
                FROM jsonb_each_text(ea.kraratings) kr(kraid, rating)
                JOIN performance.kras k ON k.kraid = kr.kraid::uuid
            ), LEAST(GREATEST(COALESCE(ea.overallrating, 0), 0), 5))
            /* NO KRA → RETURN DIRECT OVERALL RATING */
            ELSE LEAST(GREATEST(COALESCE(ea.overallrating, 0), 0), 5)
        END AS currentrating,

        /* ===========================================
           OVERALL RATING (Average across all cycles)
           Average of all CurrentRatings for this employee
        =========================================== */
        (
            SELECT COALESCE(AVG(
                CASE
                    WHEN (ea_all.kraratings IS NOT NULL AND ea_all.kraratings != '{}'::jsonb)
                    THEN COALESCE((
                        SELECT 
                            SUM(
                                (k.weightpercentage::numeric / 100.0) * kr.rating::numeric
                            )
                        FROM jsonb_each_text(ea_all.kraratings) kr(kraid, rating)
                        JOIN performance.kras k ON k.kraid = kr.kraid::uuid
                    ), LEAST(GREATEST(COALESCE(ea_all.overallrating, 0), 0), 5))
                    ELSE LEAST(GREATEST(COALESCE(ea_all.overallrating, 0), 0), 5)
                END
            ), 0)
            FROM performance.employeeappraisals ea_all
            WHERE ea_all.employeeid = ea.employeeid
        ) AS overallrating,

        ea.kraratings,
        ea.skillratings,
        ea.feedback,
        ea.improvementareas,
        ea.status,
        ea.submittedat,
        ea.reviewedat,

        CONCAT(e.firstname, ' ', e.lastname) AS employee_name,
        CONCAT(r.firstname, ' ', r.lastname) AS reviewer_name,
        ac.cyclename

    FROM performance.employeeappraisals ea
    INNER JOIN employee.employees e ON ea.employeeid = e.employeeid
    LEFT JOIN employee.employees r ON ea.reviewerid = r.employeeid
    INNER JOIN performance.appraisalcycles ac ON ea.cycleid = ac.cycleid

    WHERE ea.cycleid = @cycleId
      AND (@employeeId IS NULL OR ea.employeeid = @employeeId)

    ORDER BY e.firstname, e.lastname;
";


        var results = await QueryAsync<EmployeeAppraisalDto>(sql, new { cycleId, employeeId });
        return results.ToList();
    }

    public async Task<SkillSetDto?> CreateSkillSetAsync(CreateSkillSetRequest request, Guid organizationId)
    {
        var sql = @"
INSERT INTO performance.skillsets
    (organizationid, skillname, category, description, skilllevelscale, isactive)
VALUES
    (@organizationId, @skillName, @category, @description, to_jsonb(@skillLevelScale), @isActive)
RETURNING 
    skillid, 
    skillname, 
    category, 
    description,
    ARRAY(SELECT jsonb_array_elements_text(skilllevelscale)::int) AS skilllevelscale,
    isactive, 
    createdat;";

        try
        {
            // Default scale if none provided
            var skillScale = request.SkillLevelScale?.Length > 0
                ? request.SkillLevelScale
                : new int[] { 1, 2, 3, 4, 5 };

            // Execute query and map to DTO
            var result = await QuerySingleOrDefaultAsync<SkillSetDto>(sql, new
            {
                organizationId,
                skillName = request.SkillName,
                category = request.Category ?? "General",
                description = request.Description,
                skillLevelScale = skillScale,
                isActive = request.IsActive
            });

            return result;
        }
        catch (PostgresException ex)
        {
            if (ex.SqlState == "23505") // unique_violation
            {
                // Friendly error for duplicate skill names
                throw new InvalidOperationException("A skill with this name already exists in the organization.");
            }

            // Log & rethrow for any other DB error
            throw new Exception("An unexpected database error occurred while creating the skill.", ex);
        }
        catch (Exception ex)
        {
            // General fallback error
            throw new Exception("An unexpected error occurred while creating the skill.", ex);
        }
    }
    //    public async Task<SkillSetDto?> CreateSkillSetAsync(CreateSkillSetRequest request, Guid organizationId)
    //    {
    //        var sql = @"
    //INSERT INTO performance.skillsets
    //    (organizationid, skillname, category, description, skilllevelscale, isactive)
    //VALUES
    //    (@organizationId, @skillName, @category, @description, to_jsonb(@skillLevelScale), @isActive)
    //RETURNING 
    //    skillid, 
    //    skillname, 
    //    category, 
    //    description,
    //    ARRAY(SELECT jsonb_array_elements_text(skilllevelscale)::int) AS skilllevelscale,
    //    isactive, 
    //    createdat;";

    //        try
    //        {
    //            // Default scale if none provided
    //            var skillScale = request.SkillLevelScale?.Length > 0
    //                ? request.SkillLevelScale
    //                : new int[] { 1, 2, 3, 4, 5 };

    //            // Execute query and map to DTO
    //            var result = await QuerySingleOrDefaultAsync<SkillSetDto>(sql, new
    //            {
    //                organizationId,
    //                skillName = request.SkillName,
    //                category = request.Category ?? "General",
    //                description = request.Description,
    //                skillLevelScale = skillScale,
    //                isActive = request.IsActive
    //            });

    //            return result;
    //        }
    //        catch (PostgresException ex)
    //        {
    //            if (ex.SqlState == "23505") // unique_violation
    //            {
    //                // Friendly error for duplicate skill names
    //                throw new InvalidOperationException("A skill with this name already exists in the organization.");
    //            }

    //            // Log & rethrow for any other DB error
    //            throw new Exception("An unexpected database error occurred while creating the skill.", ex);
    //        }
    //        catch (Exception ex)
    //        {
    //            // General fallback error
    //            throw new Exception("An unexpected error occurred while creating the skill.", ex);
    //        }
    //    }

    public async Task<List<SkillSetDto>> GetSkillSetsAsync(Guid organizationId)
    {
        try
        {
            var sql = @"
           SELECT 
           skillid,
    skillname,
    category,
    description,
    CASE 
     WHEN jsonb_typeof(skilllevelscale) = 'array' THEN 
         (SELECT array_agg(value::int) 
          FROM jsonb_array_elements_text(skilllevelscale))
     ELSE 
         ARRAY[]::int[]
 END AS skilllevelscale,
    isactive,
    createdat
FROM performance.skillsets
WHERE organizationid = @organizationId
ORDER BY category, skillname";

            var results = await QueryAsync<SkillSetDto>(sql, new { organizationId });
            return results.ToList();
        }
        catch (Exception ex)
        {
            return new List<SkillSetDto>();
        }
    }

    public async Task<SkillSetDto?> GetSkillSetByIdAsync(Guid skillSetId)
    {
        var sql = @"
           SELECT 
           skillid,
    skillname,
    category,
    description,
    CASE 
     WHEN jsonb_typeof(skilllevelscale) = 'array' THEN 
         (SELECT array_agg(value::int) 
          FROM jsonb_array_elements_text(skilllevelscale))
     ELSE 
         ARRAY[]::int[]
 END AS skilllevelscale,
    isactive,
    createdat
FROM performance.skillsets
WHERE skillid = @skillSetId";

        var result = await QuerySingleOrDefaultAsync<SkillSetDto>(sql, new { skillSetId });
        return result;
    }

    public async Task<SkillSetDto> UpdateSkillSetAsync(Guid skillSetId, UpdateSkillSetRequest request)
    {
        var sql = @"
            UPDATE performance.skillsets
            SET skillname = @SkillName,
                category = COALESCE(@Category, category),
                description = @Description,
                skilllevelscale = to_jsonb(@SkillLevelScale),
                isactive = @IsActive,
                updatedat = CURRENT_TIMESTAMP
            WHERE skillid = @SkillSetId
            RETURNING 
                skillid,
                skillname,
                category,
                description,
                CASE 
                    WHEN jsonb_typeof(skilllevelscale) = 'array' THEN 
                        (SELECT array_agg(value::int) 
                         FROM jsonb_array_elements_text(skilllevelscale))
                    ELSE 
                        ARRAY[]::int[]
                END AS skilllevelscale,
                isactive,
                createdat";

        var skillLevelScale = request.SkillLevelScale?.Length > 0 
            ? request.SkillLevelScale 
            : new int[] { 1, 2, 3, 4, 5 };

        var result = await QuerySingleOrDefaultAsync<SkillSetDto>(sql, new
        {
            SkillSetId = skillSetId,
            SkillName = request.SkillName,
            Category = request.Category,
            Description = request.Description,
            SkillLevelScale = skillLevelScale,
            IsActive = request.IsActive
        });

        return result!;
    }

    public async Task<bool> DeleteSkillSetAsync(Guid skillSetId)
    {
        // Check if skill is used in employee skills
        var hasEmployeeSkills = await ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM performance.employeeskills WHERE skillid = @SkillSetId",
            new { SkillSetId = skillSetId });

        if (hasEmployeeSkills > 0)
        {
            // Soft delete - set isactive to false
            var sql = @"
                UPDATE performance.skillsets
                SET isactive = false, updatedat = CURRENT_TIMESTAMP
                WHERE skillid = @SkillSetId";
            var affected = await ExecuteAsync(sql, new { SkillSetId = skillSetId });
            return affected > 0;
        }
        else
        {
            // Hard delete if not used
            var sql = @"DELETE FROM performance.skillsets WHERE skillid = @SkillSetId";
            var affected = await ExecuteAsync(sql, new { SkillSetId = skillSetId });
            return affected > 0;
        }
    }

    public async Task<EmployeeSkillDto?> CreateEmployeeSkillAsync(CreateEmployeeSkillRequest request)
    {
        // Check if skill already exists for this employee
        var checkSql = @"
SELECT COUNT(*) 
FROM performance.employeeskills 
WHERE employeeid = @EmployeeId AND skillid = @SkillId;";

        using var connection = CreateConnection();
        var existingCount = await connection.ExecuteScalarAsync<int>(checkSql, new { request.EmployeeId, request.SkillId });
        
        if (existingCount > 0)
        {
            throw new InvalidOperationException("This skill has already been added to the employee. Cannot add duplicate skills.");
        }

        var sql = @"
INSERT INTO performance.employeeskills
    (employeeid, skillid, proficiencylevel, assessedby, lastassessed, notes)
VALUES
    (@EmployeeId, @SkillId, @ProficiencyLevel, @AssessedBy, @LastAssessed, @Notes)
RETURNING 
    employeeskillid AS EmployeeSkillId,
    employeeid AS EmployeeId,
    skillid AS SkillId,
    proficiencylevel AS ProficiencyLevel,
    assessedby AS AssessedBy,
    lastassessed AS LastAssessed,
    notes AS Notes;";

        try
        {
            var result = await connection.QuerySingleOrDefaultAsync<EmployeeSkillDto>(sql, request);
            return result;
        }
        catch (PostgresException ex)
        {
            // Handle unique constraint violation (backup check)
            if (ex.SqlState == "23505") // unique_violation
            {
                throw new InvalidOperationException("This skill has already been added to the employee. Cannot add duplicate skills.");
            }
            throw new Exception($"Database error: {ex.MessageText}");
        }
        catch (Exception ex)
        {
            throw;
        }
    }


    //    public async Task<EmployeeSkillDto?> CreateEmployeeSkillAsync(CreateEmployeeSkillRequest request)
    //    {
    //        var sql = @"
    //INSERT INTO performance.employeeskills
    //    (employeeid, skillid, proficiencylevel, assessedby, lastassessed, notes)
    //VALUES
    //    (@EmployeeId, @SkillId, @ProficiencyLevel, @Assessedby, @LastAssessed, @Notes)
    //RETURNING 
    //    employeeskillid,
    //    employeeid,
    //    skillid,
    //    proficiencylevel,
    //    assessedby AS assessedby,
    //    lastassessed,
    //    notes;";

    //        try
    //        {
    //            using var connection = CreateConnection();
    //            var result = await connection.QuerySingleOrDefaultAsync<EmployeeSkillDto>(sql, request);
    //            return result;
    //        }
    //        catch (PostgresException ex)
    //        {
    //            //_logger.LogError(ex, "Database error while inserting employee skill");
    //            throw new Exception("An error occurred while adding the employee skill.");
    //        }
    //        catch (Exception ex)
    //        {
    //            // _logger.LogError(ex, "Unexpected error while inserting employee skill");
    //            throw;
    //        }
    //    }
    public async Task<List<EmployeeSkillDto>> GetEmployeeSkillsAsync(Guid employeeId)
    {
        try
        {

        var sql = @"
            SELECT 
    es.employeeskillid,
    es.employeeid,
    es.skillid,
    es.proficiencylevel,
    es.assessedby,
    es.lastassessed,
    es.notes,

    /* Employee being assessed */
    CONCAT(e.firstname, ' ', e.lastname) AS employee_name,

    /* Skill info */
    s.skillname,
    s.category AS skill_category,

    /* Assessed by (manager or person who evaluated) */
    CONCAT(a.firstname, ' ', a.lastname) AS AssessorName,
    a.email AS assessor_email

FROM performance.employeeskills es

INNER JOIN employee.employees e 
    ON es.employeeid = e.employeeid

INNER JOIN performance.skillsets s 
    ON es.skillid = s.skillid

LEFT JOIN employee.employees a 
    ON es.assessedby = a.employeeid

WHERE es.employeeid = @employeeId
ORDER BY s.category, s.skillname;
";

        var results = await QueryAsync<EmployeeSkillDto>(sql, new { employeeId });
        return results.ToList();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<List<EmployeeSkillDto>> GetEmployeeSkillsByOrganizationAsync(Guid organizationId, EmployeeSkillFilterRequest filter)
    {
        var sql = @"
            SELECT 
    es.employeeskillid,
    es.employeeid,
    es.skillid,
    es.proficiencylevel,
    es.assessedby,
    es.lastassessed,
    es.notes,
    CONCAT(e.firstname, ' ', e.lastname) AS employee_name,
    s.skillname,
    s.category AS skill_category,
    CONCAT(a.firstname, ' ', a.lastname) AS assessor_name
FROM performance.employeeskills es
INNER JOIN employee.employees e 
    ON es.employeeid = e.employeeid
INNER JOIN performance.skillsets s 
    ON es.skillid = s.skillid
LEFT JOIN employee.employees a 
    ON es.assessedby = a.employeeid
WHERE e.organizationid = @organizationId
  AND (@employeeId IS NULL OR es.employeeid = @employeeId)
ORDER BY s.category, s.skillname;
";

        var results = await QueryAsync<EmployeeSkillDto>(sql, new { organizationId, employeeId = filter.EmployeeId });
        return results.ToList();
    }

    public async Task<List<EmployeeSkillDto>> GetEmployeeSkillsByManagerAsync(Guid managerEmployeeId, EmployeeSkillFilterRequest filter)
    {
        var whereConditions = new List<string> { "ed.reportingmanagerid = @ManagerEmployeeId", "ed.iscurrent = true" };
        var parameters = new Dictionary<string, object> { { "ManagerEmployeeId", managerEmployeeId } };

        if (filter.EmployeeId.HasValue)
        {
            whereConditions.Add("es.employeeid = @EmployeeId");
            parameters.Add("EmployeeId", filter.EmployeeId.Value);
        }

        if (filter.SkillSetId.HasValue)
        {
            whereConditions.Add("es.skillid = @SkillSetId");
            parameters.Add("SkillSetId", filter.SkillSetId.Value);
        }

        if (filter.ProficiencyLevel.HasValue)
        {
            whereConditions.Add("es.proficiencylevel = @ProficiencyLevel");
            parameters.Add("ProficiencyLevel", filter.ProficiencyLevel.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            whereConditions.Add("(LOWER(CONCAT(e.firstname, ' ', e.lastname)) LIKE LOWER(@Search) OR LOWER(s.skillname) LIKE LOWER(@Search) OR LOWER(s.category) LIKE LOWER(@Search))");
            parameters.Add("Search", $"%{filter.Search.Trim()}%");
        }

        var whereClause = string.Join(" AND ", whereConditions);

        var sql = $@"
            SELECT 
                es.employeeskillid,
                es.employeeid,
                es.skillid,
                es.proficiencylevel,
                es.assessedby,
                es.lastassessed,
                es.notes,
                CONCAT(e.firstname, ' ', e.lastname) AS employee_name,
                s.skillname,
                s.category AS skill_category,
                CONCAT(a.firstname, ' ', a.lastname) AS assessor_name
            FROM performance.employeeskills es
            INNER JOIN employee.employees e 
                ON es.employeeid = e.employeeid
            INNER JOIN employee.employmentdetails ed 
                ON e.employeeid = ed.employeeid
            INNER JOIN performance.skillsets s 
                ON es.skillid = s.skillid
            LEFT JOIN employee.employees a 
                ON es.assessedby = a.employeeid
            WHERE {whereClause}
            ORDER BY s.category, s.skillname";

        var results = await QueryAsync<EmployeeSkillDto>(sql, parameters);
        return results.ToList();
    }

    public async Task<EmployeeSkillDto?> GetEmployeeSkillByIdAsync(Guid employeeSkillId)
    {
        var sql = @"
            SELECT 
    es.employeeskillid,
    es.employeeid,
    es.skillid,
    es.proficiencylevel,
    es.assessedby,
    es.lastassessed,
    es.notes,
    CONCAT(e.firstname, ' ', e.lastname) AS employee_name,
    s.skillname,
    s.category AS skill_category,
    CONCAT(a.firstname, ' ', a.lastname) AS assessor_name
FROM performance.employeeskills es
INNER JOIN employee.employees e 
    ON es.employeeid = e.employeeid
INNER JOIN performance.skillsets s 
    ON es.skillid = s.skillid
LEFT JOIN employee.employees a 
    ON es.assessedby = a.employeeid
WHERE es.employeeskillid = @employeeSkillId";

        var result = await QuerySingleOrDefaultAsync<EmployeeSkillDto>(sql, new { employeeSkillId });
        return result;
    }

    public async Task<EmployeeSkillDto> UpdateEmployeeSkillAsync(Guid employeeSkillId, UpdateEmployeeSkillRequest request)
    {
        var sql = @"
            UPDATE performance.employeeskills
            SET proficiencylevel = @ProficiencyLevel,
                assessedby = @AssessedBy,
                lastassessed = COALESCE(@LastAssessed, lastassessed),
                notes = @Notes,
                updatedat = CURRENT_TIMESTAMP
            WHERE employeeskillid = @EmployeeSkillId
            RETURNING 
                es.employeeskillid,
                es.employeeid,
                es.skillid,
                es.proficiencylevel,
                es.assessedby,
                es.lastassessed,
                es.notes,
                CONCAT(e.firstname, ' ', e.lastname) AS employee_name,
                s.skillname,
                s.category AS skill_category,
                CONCAT(a.firstname, ' ', a.lastname) AS assessor_name
            FROM performance.employeeskills es
            INNER JOIN employee.employees e ON es.employeeid = e.employeeid
            INNER JOIN performance.skillsets s ON es.skillid = s.skillid
            LEFT JOIN employee.employees a ON es.assessedby = a.employeeid
            WHERE es.employeeskillid = @EmployeeSkillId";

        // PostgreSQL doesn't support RETURNING with JOINs, so we need to do it in two steps
        var updateSql = @"
            UPDATE performance.employeeskills
            SET proficiencylevel = @ProficiencyLevel,
                assessedby = @AssessedBy,
                lastassessed = COALESCE(@LastAssessed, lastassessed),
                notes = @Notes,
                updatedat = CURRENT_TIMESTAMP
            WHERE employeeskillid = @EmployeeSkillId";

        await ExecuteAsync(updateSql, new
        {
            EmployeeSkillId = employeeSkillId,
            ProficiencyLevel = request.ProficiencyLevel,
            AssessedBy = request.AssessedBy,
            LastAssessed = request.LastAssessed,
            Notes = request.Notes
        });

        return await GetEmployeeSkillByIdAsync(employeeSkillId) ?? throw new Exception("Failed to update employee skill");
    }

    public async Task<bool> DeleteEmployeeSkillAsync(Guid employeeSkillId)
    {
        var sql = @"DELETE FROM performance.employeeskills WHERE employeeskillid = @EmployeeSkillId";
        var affected = await ExecuteAsync(sql, new { EmployeeSkillId = employeeSkillId });
        return affected > 0;
    }

    public async Task<PerformanceSummaryDto> GetPerformanceSummaryAsync(Guid organizationId, Guid? cycleId = null)
    {
        // Get summary statistics
        var summarySql = @"
            SELECT 
                COALESCE(AVG(overallrating), 0) as average_rating,
                COUNT(*) as total_appraisals,
                COUNT(CASE WHEN ea.status IN ('draft', 'pending', 'under_review') THEN 1 END) as pending_appraisals
            FROM performance.employeeappraisals ea
            INNER JOIN employee.employees e ON ea.employeeid = e.employeeid
            WHERE e.organizationid = @organizationId
              AND (@cycleId IS NULL OR ea.cycleid = @cycleId)";

        var summaryResult = await QuerySingleOrDefaultAsync<dynamic>(summarySql, new { organizationId, cycleId });

        // Get rating distribution
        var distributionSql = @"
            SELECT 
                FLOOR(overallrating)::int as rating,
                COUNT(*) as count
            FROM performance.employeeappraisals ea
            INNER JOIN employee.employees e ON ea.employeeid = e.employeeid
            WHERE e.organizationid = @organizationId
              AND (@cycleId IS NULL OR ea.cycleid = @cycleId)
              AND ea.overallrating IS NOT NULL
            GROUP BY FLOOR(overallrating)::int
            ORDER BY rating";

        var distributionResults = await QueryAsync<dynamic>(distributionSql, new { organizationId, cycleId });
        
        var ratingDistribution = new Dictionary<string, long>();
        foreach (var item in distributionResults)
        {
            ratingDistribution[item.rating.ToString()] = (long)item.count;
        }

        // Get top performers
        var topPerformers = await GetTopPerformersAsync(organizationId, 10);

        return new PerformanceSummaryDto
        {
            AverageRating = summaryResult?.average_rating ?? 0,
            TotalAppraisals = summaryResult?.total_appraisals ?? 0,
            PendingAppraisals = summaryResult?.pending_appraisals ?? 0,
            RatingDistribution = ratingDistribution,
            TopPerformers = topPerformers
        };
    }

    public async Task<bool> UpdateAppraisalStatusAsync(Guid appraisalId, string status)
    {
        var sql = @"
            UPDATE performance.employeeappraisals 
            SET status = @status, 
                submittedat = CASE WHEN @status = 'submitted' THEN CURRENT_TIMESTAMP ELSE submittedat END,
                reviewedat = CASE WHEN @status = 'completed' THEN CURRENT_TIMESTAMP ELSE reviewedat END,
                updatedat = CURRENT_TIMESTAMP
            WHERE appraisalid = @appraisalId";

        var affected = await ExecuteAsync(sql, new { appraisalId, status });
        return affected > 0;
    }

    public async Task<List<TopPerformerDto>> GetTopPerformersAsync(Guid organizationId, int count = 10)
    {
        var sql = @"
            SELECT CONCAT(e.firstname, ' ', e.lastname) as employee_name,
                   AVG(ea.overallrating) as rating,
                   d.departmentname as department
            FROM performance.employeeappraisals ea
            INNER JOIN employee.employees e ON ea.employeeid = e.employeeid
            LEFT JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
            LEFT JOIN employee.departments d ON ed.departmentid = d.departmentid
            WHERE e.organizationid = @organizationId
              AND ea.overallrating IS NOT NULL
            GROUP BY e.employeeid, e.firstname, e.lastname, d.departmentname
            ORDER BY AVG(ea.overallrating) DESC
            LIMIT @count";

        var results = await QueryAsync<TopPerformerDto>(sql, new { organizationId, count });
        return results.ToList();
    }






    public async Task<KRADto> CreateKRAAsync(CreateKRARequest request, Guid organizationId)
    {
        var sql = @"INSERT INTO performance.kras 
    (organizationid, positionid, kratitle, description, weightpercentage, measurementcriteria, isactive)
    VALUES (@OrganizationId, @PositionId, @Title, @Description, @Weight, @MeasurementCriteria, @IsActive)
    RETURNING kraid AS KRAId, kratitle AS Title, description AS Description, 
              weightpercentage AS Weight, measurementcriteria AS MeasurementCriteria, 
              isactive AS IsActive, createdat AS CreatedAt;";
        var parameters = new
        {
            OrganizationId = organizationId,
            PositionId = request.PositionId, // make sure this exists in your CreateKRARequest
            Title = request.Title,
            Description = request.Description,
            Weight = request.Weight,
            MeasurementCriteria = request.MeasurementCriteria,
            IsActive = request.IsActive
        };

        var result = await QuerySingleOrDefaultAsync<KRADto>(sql, parameters);
        return result!;
    }

    public async Task<List<KRADto>> GetKRAsAsync(Guid organizationId)
    {

        const string sql = @"
        SELECT 
            k.kraid AS KRAId,
            k.kratitle AS Title,
            k.description AS Description,
            k.weightpercentage AS Weight,
            k.measurementcriteria AS MeasurementCriteria,
            k.isactive AS IsActive,
            k.createdat AS CreatedAt,
            k.positionid AS PositionId
        FROM performance.kras k
       
        WHERE k.organizationid = @organizationId
        ORDER BY k.createdat DESC;
    ";

        var parameters = new { OrganizationId = organizationId };
        var result = await QueryAsync<KRADto>(sql, parameters);
        return result.ToList();


    }

    public async Task<KRADto> GetKraByIdAsync(Guid KraId)
    {
        const string sql = @"
        SELECT 
            k.kraid AS KRAId,
            k.kratitle AS Title,
            k.description AS Description,
            k.weightpercentage AS Weight,
            k.measurementcriteria AS MeasurementCriteria,
            k.isactive AS IsActive,
            k.createdat AS CreatedAt,
            k.positionid AS PositionId,
            k.organizationid AS OrganizationId
        FROM performance.kras k
        WHERE k.kraid = @KRAId;
    ";

        var parameters = new { KRAId = KraId };
        var result = await QuerySingleOrDefaultAsync<KRADto>(sql, parameters);
        return result;
    }


    public async Task<KRADto> UpdatekraAsync(Guid kraId, UpdateKRARequest request)
    {

        var sql = @" UPDATE performance.kras
        SET 
            kratitle = @Title,
            description = @Description,
            weightpercentage = @Weight,
            measurementcriteria = @MeasurementCriteria,
            isactive = @IsActive,
            createdat = CURRENT_TIMESTAMP
        WHERE kraid = @KraId;

        SELECT kraid AS KraId,
               kratitle AS Title,
               description AS Description,
               weightpercentage AS Weight,
               measurementcriteria AS MeasurementCriteria,
               isactive AS IsActive
        FROM performance.kras
        WHERE kraid = @KraId;";

        var parameters = new
        {
            KraId = kraId,
            Title = request.Title,
            Description = request.Description,
            Weight = request.Weight,
            MeasurementCriteria = request.MeasurementCriteria,
            IsActive = request.IsActive


        };

        var result = await QuerySingleOrDefaultAsync<KRADto>(sql, parameters);

        return result;
    }

    public async Task<bool> UpdateKRAStatusAsync(Guid kraId, bool isActive)
    {
        var sql = @"
        UPDATE performance.kras
        SET 
            isactive = @IsActive,
            createdat = CURRENT_TIMESTAMP
        WHERE kraid = @KraId;
    ";

        var parameters = new
        {
            KraId = kraId,
            IsActive = isActive
        };

        var rowsAffected = await ExecuteAsync(sql, parameters);
        return rowsAffected > 0;
    }

    public async Task<SelfAssessmentDto> CreateSelfAssessmentAsync(CreateSelfAssessmentRequest request)
    {
        // ---------------------------------------------
        // 1) Check if the same KRA already exists for the employee & cycle
        // ---------------------------------------------
        var checkSql = @"
        SELECT COUNT(*) 
        FROM performance.selfassessments
        WHERE employeeid = @EmployeeId 
          AND cycleid = @CycleId
          AND kraid = @KraId;
    ";

        var existingCount = await QuerySingleOrDefaultAsync<int>(checkSql, request);

        if (existingCount > 0)
        {
            throw new Exception("A self-assessment for this KRA already exists for the selected cycle and employee.");
        }

        // ---------------------------------------------
        // 2) Insert new self-assessment
        // ---------------------------------------------
        var insertSql = @"
        INSERT INTO performance.selfassessments
        (employeeid, cycleid, kraid, skillid, rating, comments, evidenceurls, status, createdat, updatedat)
        VALUES
        (@EmployeeId, @CycleId, @KraId, @SkillId, @Rating, @Comments, @EvidenceUrls::jsonb, 'Submitted', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
        RETURNING selfassessmentid, employeeid, cycleid, kraid, skillid, rating, comments, evidenceurls, status, createdat, updatedat;
    ";

        return await QuerySingleOrDefaultAsync<SelfAssessmentDto>(insertSql, request);
    }


    public async Task<IEnumerable<SelfAssessmentDto>> GetSelfAssessmentsAsync(Guid employeeId, Guid cycleId)
    {
        var sql = @"SELECT * FROM performance.selfassessments WHERE employeeid = @EmployeeId AND cycleid = @CycleId";
        return await QueryAsync<SelfAssessmentDto>(sql, new { EmployeeId = employeeId, CycleId = cycleId });
    }

    public async Task<IEnumerable<SelfAssessmentDto>> GetMySelfAssessmentsAsync(Guid employeeId, Guid? cycleId = null, Guid? kraId = null, string? search = null)
    {
        var whereConditions = new List<string> { "sa.employeeid = @EmployeeId" };
        var parameters = new Dictionary<string, object> { { "EmployeeId", employeeId } };

        if (cycleId.HasValue)
        {
            whereConditions.Add("sa.cycleid = @CycleId");
            parameters.Add("CycleId", cycleId.Value);
        }

        if (kraId.HasValue)
        {
            whereConditions.Add("sa.kraid = @KraId");
            parameters.Add("KraId", kraId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereConditions.Add("(LOWER(ac.cyclename) LIKE LOWER(@Search) OR LOWER(k.kratitle) LIKE LOWER(@Search) OR LOWER(sa.comments) LIKE LOWER(@Search))");
            parameters.Add("Search", $"%{search.Trim()}%");
        }

        var whereClause = string.Join(" AND ", whereConditions);

        var sql = $@"
            SELECT 
                sa.selfassessmentid as SelfAssessmentId,
                sa.employeeid as EmployeeId,
                sa.cycleid as CycleId,
                sa.kraid as KraId,
                sa.skillid as SkillId,
                sa.rating as Rating,
                sa.comments as Comments,
                sa.evidenceurls as EvidenceUrls,
                sa.status as Status,
                sa.submittedat as SubmittedAt,
                sa.createdat as CreatedAt,
                sa.updatedat as UpdatedAt,
                ac.cyclename as CycleName,
                k.kratitle as KraName
            FROM performance.selfassessments sa
            LEFT JOIN performance.appraisalcycles ac ON sa.cycleid = ac.cycleid
            LEFT JOIN performance.kras k ON sa.kraid = k.kraid
            WHERE {whereClause}
            ORDER BY sa.createdat DESC";
        
        var results = await QueryAsync<SelfAssessmentDto>(sql, parameters);
        return results;
    }

    public async Task UpdateSelfAssessmentStatusAsync(Guid employeeId, Guid cycleId, string status)
    {
        var sql = @"UPDATE performance.selfassessments
                SET status = @Status, updatedat = CURRENT_TIMESTAMP, submittedat = CURRENT_TIMESTAMP
                WHERE employeeid = @EmployeeId AND cycleid = @CycleId";
        await ExecuteAsync(sql, new { EmployeeId = employeeId, CycleId = cycleId, Status = status });
    }

    public async Task<IEnumerable<SelfAssessmentDto>> GetTeamSelfAssessmentsAsync(Guid managerId, Guid cycleId)
    {
        var sql = @"
        SELECT sa.*
        FROM performance.selfassessments sa
        INNER JOIN employee.employees e ON sa.employeeid = e.employeeid
        INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
        WHERE ed.managerid = @ManagerId AND sa.cycleid = @CycleId;
    ";
        return await QueryAsync<SelfAssessmentDto>(sql, new { ManagerId = managerId, CycleId = cycleId });
    }

    public async Task<IEnumerable<SelfAssessmentDto>> GetAllEmployeeSelfAssessmentsAsync(Guid organizationId, Guid? cycleId = null, Guid? employeeId = null, Guid? kraId = null, string? search = null)
    {
        var whereConditions = new List<string> { "e.organizationid = @OrganizationId" };
        var parameters = new Dictionary<string, object> { { "OrganizationId", organizationId } };

        if (cycleId.HasValue)
        {
            whereConditions.Add("sa.cycleid = @CycleId");
            parameters.Add("CycleId", cycleId.Value);
        }

        if (employeeId.HasValue)
        {
            whereConditions.Add("sa.employeeid = @EmployeeId");
            parameters.Add("EmployeeId", employeeId.Value);
        }

        if (kraId.HasValue)
        {
            whereConditions.Add("sa.kraid = @KraId");
            parameters.Add("KraId", kraId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereConditions.Add("(LOWER(ac.cyclename) LIKE LOWER(@Search) OR LOWER(k.kratitle) LIKE LOWER(@Search) OR LOWER(sa.comments) LIKE LOWER(@Search) OR LOWER(CONCAT(e.firstname, ' ', e.lastname)) LIKE LOWER(@Search))");
            parameters.Add("Search", $"%{search.Trim()}%");
        }

        var whereClause = string.Join(" AND ", whereConditions);

        var sql = $@"
            SELECT 
                sa.selfassessmentid as SelfAssessmentId,
                sa.employeeid as EmployeeId,
                sa.cycleid as CycleId,
                sa.kraid as KraId,
                sa.skillid as SkillId,
                sa.rating as Rating,
                sa.comments as Comments,
                sa.evidenceurls as EvidenceUrls,
                sa.status as Status,
                sa.submittedat as SubmittedAt,
                sa.createdat as CreatedAt,
                sa.updatedat as UpdatedAt,
                ac.cyclename as CycleName,
                k.kratitle as KraName,
                CONCAT(e.firstname, ' ', e.lastname) as EmployeeName
            FROM performance.selfassessments sa
            INNER JOIN employee.employees e ON sa.employeeid = e.employeeid
            LEFT JOIN performance.appraisalcycles ac ON sa.cycleid = ac.cycleid
            LEFT JOIN performance.kras k ON sa.kraid = k.kraid
            WHERE {whereClause}
            ORDER BY sa.createdat DESC";
        
        var results = await QueryAsync<SelfAssessmentDto>(sql, parameters);
        return results;
    }

    public async Task<IEnumerable<SelfAssessmentDto>> GetMyTeamSelfAssessmentsAsync(Guid managerEmployeeId, Guid? cycleId = null, Guid? employeeId = null, Guid? kraId = null, string? search = null)
    {
        var whereConditions = new List<string> { "ed.reportingmanagerid = @ManagerEmployeeId", "ed.iscurrent = true" };
        var parameters = new Dictionary<string, object> { { "ManagerEmployeeId", managerEmployeeId } };

        if (cycleId.HasValue)
        {
            whereConditions.Add("sa.cycleid = @CycleId");
            parameters.Add("CycleId", cycleId.Value);
        }

        if (employeeId.HasValue)
        {
            whereConditions.Add("sa.employeeid = @EmployeeId");
            parameters.Add("EmployeeId", employeeId.Value);
        }

        if (kraId.HasValue)
        {
            whereConditions.Add("sa.kraid = @KraId");
            parameters.Add("KraId", kraId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereConditions.Add("(LOWER(ac.cyclename) LIKE LOWER(@Search) OR LOWER(k.kratitle) LIKE LOWER(@Search) OR LOWER(sa.comments) LIKE LOWER(@Search) OR LOWER(CONCAT(e.firstname, ' ', e.lastname)) LIKE LOWER(@Search))");
            parameters.Add("Search", $"%{search.Trim()}%");
        }

        var whereClause = string.Join(" AND ", whereConditions);

        var sql = $@"
            SELECT 
                sa.selfassessmentid as SelfAssessmentId,
                sa.employeeid as EmployeeId,
                sa.cycleid as CycleId,
                sa.kraid as KraId,
                sa.skillid as SkillId,
                sa.rating as Rating,
                sa.comments as Comments,
                sa.evidenceurls as EvidenceUrls,
                sa.status as Status,
                sa.submittedat as SubmittedAt,
                sa.createdat as CreatedAt,
                sa.updatedat as UpdatedAt,
                ac.cyclename as CycleName,
                k.kratitle as KraName,
                CONCAT(e.firstname, ' ', e.lastname) as EmployeeName
            FROM performance.selfassessments sa
            INNER JOIN employee.employees e ON sa.employeeid = e.employeeid
            INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid
            LEFT JOIN performance.appraisalcycles ac ON sa.cycleid = ac.cycleid
            LEFT JOIN performance.kras k ON sa.kraid = k.kraid
            WHERE {whereClause}
            ORDER BY sa.createdat DESC";
        
        var results = await QueryAsync<SelfAssessmentDto>(sql, parameters);
        return results;
    }

    public async Task<ManagerReviewDto> SubmitManagerReviewAsync(ManagerReviewRequest request)
    {
        var sql = @"
        INSERT INTO performance.managerreviews
        (managerid, employeeid, cycleid, overallrating, feedback, improvementareas, developmentplan, createdat, updatedat)
        VALUES
        (@ManagerId, @EmployeeId, @CycleId, @OverallRating, @Feedback, @ImprovementAreas, @DevelopmentPlan, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
        RETURNING reviewid, managerid, employeeid, cycleid, overallrating, feedback, improvementareas, developmentplan, 'submitted' as status, createdat, updatedat;
    ";

        return await QuerySingleOrDefaultAsync<ManagerReviewDto>(sql, request);
    }

    public async Task<IEnumerable<EmployeeAppraisalForEmployeeDto>> GetEmployeeAppraisalsForEmployeeAsync(Guid employeeId, Guid? cycleId = null, Guid? kraId = null, string? search = null, string? status = null)
    {
        var whereConditions = new List<string> { "ea.employeeid = @EmployeeId" };
        var parameters = new Dictionary<string, object> { { "EmployeeId", employeeId } };

        if (cycleId.HasValue)
        {
            whereConditions.Add("ea.cycleid = @CycleId");
            parameters.Add("CycleId", cycleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            whereConditions.Add("LOWER(ea.status) = LOWER(@Status)");
            parameters.Add("Status", status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereConditions.Add("(LOWER(ac.cyclename) LIKE LOWER(@Search) OR LOWER(CONCAT(r.firstname, ' ', r.lastname)) LIKE LOWER(@Search) OR LOWER(ea.feedback) LIKE LOWER(@Search) OR LOWER(ea.improvementareas) LIKE LOWER(@Search))");
            parameters.Add("Search", $"%{search.Trim()}%");
        }

        var whereClause = string.Join(" AND ", whereConditions);

        var sql = $@"
            SELECT 
                ea.appraisalid as AppraisalId,
                ea.cycleid as CycleId,
                ac.cyclename as CycleName,
                CONCAT(r.firstname, ' ', r.lastname) as ReviewerName,
                ea.reviewtype as ReviewType,
                /* ===========================================
                     FINAL OVERALL RATING (0–5 NORMALIZED)
                     Based on KRA and weights only (100% KRA)
                   =========================================== */
                CASE
                    /* --------------------------------------
                       KRA EXISTS → Calculate based on KRA weights (100%)
                       Formula: SUM(rating * (weightpercentage / 100))
                       -------------------------------------- */
                    WHEN (ea.kraratings != '{{}}'::jsonb)
                    THEN (
                        SELECT 
                            SUM(
                                (k.weightpercentage::numeric / 100.0) * kr.rating::numeric
                            )
                        FROM jsonb_each_text(ea.kraratings) kr(kraid, rating)
                        JOIN performance.kras k ON k.kraid = kr.kraid::uuid
                    )
                    /* --------------------------------------
                       NO KRA → RETURN DIRECT OVERALL RATING
                       -------------------------------------- */
                    ELSE LEAST(GREATEST(ea.overallrating, 0), 5)
                END as OverallRating,
                ea.kraratings as KraRatings,
                ea.skillratings as SkillRatings,
                ea.feedback as Feedback,
                ea.improvementareas as ImprovementAreas,
                COALESCE(ea.developmentplan, '') as DevelopmentPlan,
                ea.status as Status,
                ea.submittedat as SubmittedAt,
                ea.reviewedat as ReviewedAt
            FROM performance.employeeappraisals ea
            INNER JOIN performance.appraisalcycles ac ON ea.cycleid = ac.cycleid
            LEFT JOIN employee.employees r ON ea.reviewerid = r.employeeid
            WHERE {whereClause}
            ORDER BY ea.submittedat DESC";

        var results = await QueryAsync<EmployeeAppraisalForEmployeeDto>(sql, parameters);
        return results;
    }

    // Appraisal Cycle Management - Enhanced
    public async Task<AppraisalCycleDto?> GetAppraisalCycleByIdAsync(Guid cycleId)
    {
        var sql = @"
            SELECT ac.cycleid, ac.cyclename, ac.description, ac.startdate, ac.enddate,
                   ac.status, ac.ratingscale, ac.selfreviewenabled, ac.peerreviewenabled,
                   ac.managerreviewenabled,
                   COUNT(ea.appraisalid) as total_appraisals,
                   COUNT(CASE WHEN ea.status = 'completed' THEN 1 END) as completed_appraisals
            FROM performance.appraisalcycles ac
            LEFT JOIN performance.employeeappraisals ea ON ac.cycleid = ea.cycleid
            WHERE ac.cycleid = @cycleId
            GROUP BY ac.cycleid, ac.cyclename, ac.description, ac.startdate, ac.enddate,
                     ac.status, ac.ratingscale, ac.selfreviewenabled, ac.peerreviewenabled,
                     ac.managerreviewenabled";

        var result = await QuerySingleOrDefaultAsync<AppraisalCycleDto>(sql, new { cycleId });
        return result;
    }

    public async Task<AppraisalCycleDto> UpdateAppraisalCycleAsync(Guid cycleId, UpdateAppraisalCycleRequest request)
    {
        var sql = @"
            UPDATE performance.appraisalcycles
            SET cyclename = @CycleName,
                description = @Description,
                startdate = @StartDate,
                enddate = @EndDate,
                status = @Status,
                ratingscale = @RatingScale::jsonb,
                selfreviewenabled = @SelfReviewEnabled,
                peerreviewenabled = @PeerReviewEnabled,
                managerreviewenabled = @ManagerReviewEnabled
            WHERE cycleid = @CycleId
            RETURNING cycleid, cyclename, description, startdate, enddate, status, 
                     ratingscale, selfreviewenabled, peerreviewenabled, managerreviewenabled,
                     (SELECT COUNT(*) FROM performance.employeeappraisals WHERE cycleid = @CycleId) as total_appraisals,
                     (SELECT COUNT(*) FROM performance.employeeappraisals WHERE cycleid = @CycleId AND status = 'completed') as completed_appraisals";

        var ratingScaleJson = System.Text.Json.JsonSerializer.Serialize(request.RatingScale);
        var result = await QuerySingleOrDefaultAsync<AppraisalCycleDto>(sql, new
        {
            CycleId = cycleId,
            CycleName = request.CycleName,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            RatingScale = ratingScaleJson,
            SelfReviewEnabled = request.SelfReviewEnabled,
            PeerReviewEnabled = request.PeerReviewEnabled,
            ManagerReviewEnabled = request.ManagerReviewEnabled
        });

        return result!;
    }

    public async Task<bool> DeleteAppraisalCycleAsync(Guid cycleId)
    {
        // Check if cycle has any appraisals
        var hasAppraisals = await QuerySingleOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM performance.employeeappraisals WHERE cycleid = @CycleId",
            new { CycleId = cycleId });

        if (hasAppraisals > 0)
        {
            throw new InvalidOperationException("Cannot delete appraisal cycle with existing appraisals. Please archive it instead.");
        }

        var sql = @"DELETE FROM performance.appraisalcycles WHERE cycleid = @CycleId";
        var affected = await ExecuteAsync(sql, new { CycleId = cycleId });
        return affected > 0;
    }

    public async Task<AppraisalCycleDto?> GetActiveAppraisalCycleAsync(Guid organizationId)
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
              AND ac.status = 'active'
              AND CURRENT_DATE BETWEEN ac.startdate AND ac.enddate
            GROUP BY ac.cycleid, ac.cyclename, ac.description, ac.startdate, ac.enddate,
                     ac.status, ac.ratingscale, ac.selfreviewenabled, ac.peerreviewenabled,
                     ac.managerreviewenabled
            ORDER BY ac.startdate DESC
            LIMIT 1";

        var result = await QuerySingleOrDefaultAsync<AppraisalCycleDto>(sql, new { organizationId });
        return result;
    }

    // KRA Management - Enhanced
    public async Task<bool> DeleteKRAAsync(Guid kraId)
    {
        var sql = @"DELETE FROM performance.kras WHERE kraid = @KraId";
        var affected = await ExecuteAsync(sql, new { KraId = kraId });
        return affected > 0;
    }

    // Appraisal Consolidation
    public async Task<EmployeeAppraisalDto> ConsolidateAppraisalAsync(ConsolidateAppraisalRequest request)
    {
        // Get self-assessment data
        var selfAssessments = await GetSelfAssessmentsAsync(request.EmployeeId, request.CycleId);
        var selfAssessmentList = selfAssessments.ToList();

        // Get manager review data
        var managerReviewSql = @"
            SELECT reviewid, managerid, employeeid, cycleid, overallrating, feedback, 
                   improvementareas, developmentplan, status, createdat, updatedat
            FROM performance.managerreviews
            WHERE employeeid = @EmployeeId AND cycleid = @CycleId
            ORDER BY createdat DESC
            LIMIT 1";

        var managerReview = await QuerySingleOrDefaultAsync<ManagerReviewDto>(managerReviewSql, new
        {
            EmployeeId = request.EmployeeId,
            CycleId = request.CycleId
        });

        if (managerReview == null)
        {
            throw new InvalidOperationException("Manager review not found. Cannot consolidate appraisal.");
        }

        // Calculate KRA ratings from self-assessment
        var kraRatings = new Dictionary<string, object>();
        var kraRatingsList = selfAssessmentList.Where(s => s.KraId.HasValue).ToList();
        foreach (var sa in kraRatingsList)
        {
            kraRatings[sa.KraId!.Value.ToString()] = sa.Rating;
        }

        // Calculate Skill ratings from self-assessment
        var skillRatings = new Dictionary<string, object>();
        var skillRatingsList = selfAssessmentList.Where(s => s.SkillId.HasValue).ToList();
        foreach (var sa in skillRatingsList)
        {
            skillRatings[sa.SkillId!.Value.ToString()] = sa.Rating;
        }

        // Calculate overall rating with weightage: KRAs 60%, Skills 30%, Manager feedback 10%
        decimal overallRating = 0;
        decimal kraAverage = 0;
        decimal skillAverage = 0;

        if (kraRatingsList.Any())
        {
            kraAverage = (decimal)kraRatingsList.Average(s => (double)s.Rating);
        }

        if (skillRatingsList.Any())
        {
            skillAverage = (decimal)skillRatingsList.Average(s => (double)s.Rating);
        }

        overallRating = (kraAverage * 0.6m) + (skillAverage * 0.3m) + (managerReview.OverallRating * 0.1m);

        // Create consolidated appraisal
        var appraisal = new EmployeeAppraisal
        {
            AppraisalId = Guid.NewGuid(),
            CycleId = request.CycleId,
            EmployeeId = request.EmployeeId,
            ReviewerId = request.ReviewerId,
            ReviewType = "consolidated",
            OverallRating = overallRating,
            KraRatings = kraRatings,
            SkillRatings = skillRatings,
            Feedback = managerReview.Feedback,
            ImprovementAreas = managerReview.ImprovementAreas,
            DevelopmentPlan = managerReview.DevelopmentPlan,
            Status = "completed",
            SubmittedAt = DateTime.UtcNow,
            ReviewedAt = DateTime.UtcNow
        };

        var createdAppraisal = await CreateEmployeeAppraisalAsync(appraisal);

        // Map to DTO
        var employeeSql = @"
            SELECT CONCAT(firstname, ' ', lastname) as employee_name
            FROM employee.employees
            WHERE employeeid = @EmployeeId";

        var employeeName = await QuerySingleOrDefaultAsync<string>(employeeSql, new { EmployeeId = request.EmployeeId });

        var cycleSql = @"
            SELECT cyclename
            FROM performance.appraisalcycles
            WHERE cycleid = @CycleId";

        var cycleName = await QuerySingleOrDefaultAsync<string>(cycleSql, new { CycleId = request.CycleId });

        return new EmployeeAppraisalDto
        {
            AppraisalId = createdAppraisal.AppraisalId,
            EmployeeId = createdAppraisal.EmployeeId,
            CycleId = createdAppraisal.CycleId,
            EmployeeName = employeeName ?? string.Empty,
            CycleName = cycleName ?? string.Empty,
            ReviewerName = null,
            ReviewType = createdAppraisal.ReviewType,
            OverallRating = createdAppraisal.OverallRating,
            KraRatings = createdAppraisal.KraRatings,
            SkillRatings = createdAppraisal.SkillRatings,
            Feedback = createdAppraisal.Feedback,
            ImprovementAreas = createdAppraisal.ImprovementAreas,
            Status = createdAppraisal.Status,
            SubmittedAt = createdAppraisal.SubmittedAt,
            ReviewedAt = createdAppraisal.ReviewedAt
        };
    }

    // Reports and Analytics
    public async Task<EmployeePerformanceHistoryDto> GetEmployeePerformanceHistoryAsync(Guid employeeId)
    {
        var employeeSql = @"
            SELECT CONCAT(firstname, ' ', lastname) as employee_name
            FROM employee.employees
            WHERE employeeid = @EmployeeId";

        var employeeName = await QuerySingleOrDefaultAsync<string>(employeeSql, new { EmployeeId = employeeId });

        if (string.IsNullOrEmpty(employeeName))
        {
            throw new KeyNotFoundException($"Employee with ID {employeeId} not found");
        }

        var appraisals = await GetEmployeeAppraisalsByEmployeeAsync(employeeId);

        var averageRating = appraisals.Any() && appraisals.Any(a => a.OverallRating.HasValue)
            ? (decimal?)appraisals.Where(a => a.OverallRating.HasValue).Average(a => a.OverallRating!.Value)
            : null;

        // Get skill gaps
        var skillGapsSql = @"
            SELECT 
                s.skillid,
                s.skillname,
                4 as required_level, -- Default required level, can be customized
                COALESCE(MAX(es.proficiencylevel), 0) as current_level,
                (4 - COALESCE(MAX(es.proficiencylevel), 0)) as gap
            FROM performance.skillsets s
            LEFT JOIN performance.employeeskills es ON s.skillid = es.skillid AND es.employeeid = @EmployeeId
            WHERE s.isactive = true
            GROUP BY s.skillid, s.skillname
            HAVING (4 - COALESCE(MAX(es.proficiencylevel), 0)) > 0
            ORDER BY gap DESC";

        var skillGaps = await QueryAsync<SkillGapAnalysisDto>(skillGapsSql, new { EmployeeId = employeeId });

        return new EmployeePerformanceHistoryDto
        {
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            Appraisals = appraisals,
            AverageRating = averageRating,
            TotalAppraisals = appraisals.Count,
            CompletedAppraisals = appraisals.Count(a => a.Status == "completed"),
            SkillGaps = skillGaps.ToList()
        };
    }

    public async Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsByEmployeeAsync(Guid employeeId)
    {
        var sql = @"
            SELECT ea.appraisalid, ea.employeeid, ea.reviewerid, ea.reviewtype, ea.cycleid,
                   ea.overallrating, ea.kraratings, ea.skillratings, ea.feedback,
                   ea.improvementareas, ea.status, ea.submittedat, ea.reviewedat,
                   CONCAT(e.firstname, ' ', e.lastname) as employee_name,
                   CONCAT(r.firstname, ' ', r.lastname) as reviewer_name,
                   ac.cyclename
            FROM performance.employeeappraisals ea
            INNER JOIN employee.employees e ON ea.employeeid = e.employeeid
            LEFT JOIN employee.employees r ON ea.reviewerid = r.employeeid
            INNER JOIN performance.appraisalcycles ac ON ea.cycleid = ac.cycleid
            WHERE ea.employeeid = @employeeId
            ORDER BY ea.createdat DESC";

        var results = await QueryAsync<EmployeeAppraisalDto>(sql, new { employeeId });
        return results.ToList();
    }

    public async Task<EmployeeAppraisalDto?> GetEmployeeAppraisalByIdAsync(Guid appraisalId)
    {
        var sql = @"
            SELECT ea.appraisalid, ea.employeeid, ea.reviewerid, ea.reviewtype, ea.cycleid,
                   ea.overallrating, ea.kraratings, ea.skillratings, ea.feedback,
                   ea.improvementareas, ea.status, ea.submittedat, ea.reviewedat,
                   CONCAT(e.firstname, ' ', e.lastname) as employee_name,
                   CONCAT(r.firstname, ' ', r.lastname) as reviewer_name,
                   ac.cyclename
            FROM performance.employeeappraisals ea
            INNER JOIN employee.employees e ON ea.employeeid = e.employeeid
            LEFT JOIN employee.employees r ON ea.reviewerid = r.employeeid
            INNER JOIN performance.appraisalcycles ac ON ea.cycleid = ac.cycleid
            WHERE ea.appraisalid = @appraisalId";

        var result = await QuerySingleOrDefaultAsync<EmployeeAppraisalDto>(sql, new { appraisalId });
        return result;
    }

    public async Task<bool> DeleteEmployeeAppraisalAsync(Guid appraisalId)
    {
        var sql = @"DELETE FROM performance.employeeappraisals WHERE appraisalid = @AppraisalId";
        var affected = await ExecuteAsync(sql, new { AppraisalId = appraisalId });
        return affected > 0;
    }

    public async Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsPagedAsync(Guid organizationId, AppraisalFilterRequest filter, int page, int limit)
    {
        try
        {

            var sql = @"
            SELECT 
    ea.appraisalid, 
    ea.employeeid, 
    ea.reviewerid, 
    ea.reviewtype, 
    ea.cycleid,

    /* ===========================================
         FINAL OVERALL RATING (0–5 NORMALIZED)
         Based on KRA and weights only (100% KRA)
       =========================================== */
    CASE
        /* --------------------------------------
           KRA EXISTS → Calculate based on KRA weights (100%)
           Formula: SUM(rating * (weightpercentage / 100))
           -------------------------------------- */
        WHEN (ea.kraratings != '{}'::jsonb)
        THEN (
            SELECT 
                SUM(
                    (k.weightpercentage::numeric / 100.0) * kr.rating::numeric
                )
            FROM jsonb_each_text(ea.kraratings) kr(kraid, rating)
            JOIN performance.kras k ON k.kraid = kr.kraid::uuid
        )
        /* --------------------------------------
           NO KRA → RETURN DIRECT OVERALL RATING
           -------------------------------------- */
        ELSE LEAST(GREATEST(ea.overallrating, 0), 5)
    END AS overallrating,
    /* Existing Fields */
    ea.kraratings, 
    ea.skillratings, 
    ea.feedback,
    ea.improvementareas, 
    ea.developmentplan,
    ea.status, 
    ea.submittedat, 
    ea.reviewedat,
    ea.createdat,

    CONCAT(e.firstname, ' ', e.lastname) AS employee_name,
    CONCAT(r.firstname, ' ', r.lastname) AS reviewer_name,
    ac.cyclename

FROM performance.employeeappraisals ea
INNER JOIN employee.employees e 
    ON ea.employeeid = e.employeeid

LEFT JOIN employee.employees r 
    ON ea.reviewerid = r.employeeid

INNER JOIN performance.appraisalcycles ac 
    ON ea.cycleid = ac.cycleid

WHERE e.organizationid = @organizationId
  AND (@appraisalCycleId IS NULL OR ea.cycleid = @appraisalCycleId)
  AND (@status IS NULL OR ea.status = @status)
  AND (@employeeId IS NULL OR ea.employeeid = @employeeId)
  AND (@managerId IS NULL OR ea.reviewerid = @managerId)
  AND (
        @search IS NULL 
        OR LOWER(CONCAT(e.firstname, ' ', e.lastname)) LIKE LOWER('%' || @search || '%') 
        OR LOWER(ac.cyclename) LIKE LOWER('%' || @search || '%')
      )
ORDER BY ea.createdat DESC
LIMIT @limit OFFSET @offset;
";

            var offset = (page - 1) * limit;
            var results = await QueryAsync<EmployeeAppraisalDto>(sql, new
            {
                organizationId,
                appraisalCycleId = filter.AppraisalCycleId,
                status = filter.Status,
                employeeId = filter.EmployeeId,
                managerId = filter.ManagerId,
                search = filter.Search,
                limit,
                offset
            });

            return results.ToList();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
   

    public async Task<int> GetEmployeeAppraisalsCountAsync(Guid organizationId, AppraisalFilterRequest filter)
    {
        var sql = @"
            SELECT COUNT(*)
            FROM performance.employeeappraisals ea
            INNER JOIN employee.employees e ON ea.employeeid = e.employeeid
            INNER JOIN performance.appraisalcycles ac ON ea.cycleid = ac.cycleid
            WHERE e.organizationid = @organizationId
              AND (@appraisalCycleId IS NULL OR ea.cycleid = @appraisalCycleId)
              AND (@status IS NULL OR ea.status = @status)
              AND (@employeeId IS NULL OR ea.employeeid = @employeeId)
              AND (@managerId IS NULL OR ea.reviewerid = @managerId)
              AND (@search IS NULL OR 
                   LOWER(CONCAT(e.firstname, ' ', e.lastname)) LIKE LOWER('%' || @search || '%') OR
                   LOWER(ac.cyclename) LIKE LOWER('%' || @search || '%'))";

        var count = await ExecuteScalarAsync<int>(sql, new 
        { 
            organizationId,
            appraisalCycleId = filter.AppraisalCycleId,
            status = filter.Status,
            employeeId = filter.EmployeeId,
            managerId = filter.ManagerId,
            search = filter.Search
        });
        
        return count;
    }

    public async Task<List<DepartmentPerformanceDto>> GetDepartmentPerformanceBreakdownAsync(Guid organizationId, DateTime? startDate, DateTime? endDate)
    {
        var sql = @"
            SELECT 
                d.departmentname AS DepartmentName,
                COUNT(DISTINCT e.employeeid) AS EmployeeCount,
                COUNT(ea.appraisalid) AS TotalAppraisals,
                COUNT(CASE WHEN ea.status = 'completed' THEN 1 END) AS CompletedAppraisals,
                COUNT(CASE WHEN ea.status IN ('draft', 'pending', 'under_review') THEN 1 END) AS PendingAppraisals,
                COALESCE(AVG(ea.overallrating), 0) AS AverageRating
            FROM employee.departments d
            INNER JOIN employee.employmentdetails ed ON d.departmentid = ed.departmentid AND ed.iscurrent = true
            INNER JOIN employee.employees e ON ed.employeeid = e.employeeid
            LEFT JOIN performance.employeeappraisals ea ON e.employeeid = ea.employeeid
                AND (@startDate IS NULL OR COALESCE(ea.submittedat, ea.reviewedat, ea.createdat) >= @startDate)
                AND (@endDate IS NULL OR COALESCE(ea.submittedat, ea.reviewedat, ea.createdat) <= @endDate)
            WHERE e.organizationid = @organizationId
            GROUP BY d.departmentid, d.departmentname
            ORDER BY d.departmentname";

        var results = await QueryAsync<DepartmentPerformanceDto>(sql, new 
        { 
            organizationId,
            startDate,
            endDate
        });
        
        return results.ToList();
    }

    public async Task<string?> GetEmployeeDepartmentAsync(Guid employeeId)
    {
        var sql = @"
            SELECT d.departmentname
            FROM employee.employmentdetails ed
            INNER JOIN employee.departments d ON ed.departmentid = d.departmentid
            WHERE ed.employeeid = @employeeId AND ed.iscurrent = true
            LIMIT 1";
        
        return await QuerySingleOrDefaultAsync<string>(sql, new { employeeId });
    }
    public async Task<Guid> GetEmployeeFromTable(Guid Userid)
    {
        try
        {
            var sql = @"SELECT employeeid FROM employee.employees WHERE userid = @Userid";
            var employeeId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { Userid });
            
            if (!employeeId.HasValue || employeeId.Value == Guid.Empty)
            {
                throw new UnauthorizedAccessException($"No employee record found for user {Userid}. Please ensure your user account is linked to an employee record.");
            }
            
            return employeeId.Value;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving employee ID for user {Userid}", ex);
        }
    }

    public async Task<TeamPerformanceOverviewDto> GetTeamPerformanceOverviewAsync(Guid managerEmployeeId, Guid organizationId)
    {
        try
        {
            // Step 1: Get all employees reporting to this manager
            var employeesSql = @"
                SELECT DISTINCT
                    e.employeeid,
                    CONCAT(e.firstname, ' ', e.lastname) AS employeename
                FROM employee.employees e
                INNER JOIN employee.employmentdetails ed 
                    ON e.employeeid = ed.employeeid 
                    AND ed.iscurrent = true
                    AND ed.reportingmanagerid = @ManagerEmployeeId
                WHERE e.organizationid = @OrganizationId
                    AND e.status != 'deleted'
                ORDER BY employeename";

            var employees = await QueryAsync<dynamic>(employeesSql, new { ManagerEmployeeId = managerEmployeeId, OrganizationId = organizationId });
            
            var result = new TeamPerformanceOverviewDto();

            foreach (var emp in employees)
            {
                var employeeId = (Guid)emp.employeeid;
                var employeeName = (string)emp.employeename;

                // Step 2: Get all appraisals with calculated ratings per cycle for this employee
                var appraisalsSql = @"
                    SELECT DISTINCT
                        ac.cycleid,
                        ac.cyclename,
                        ac.startdate,
                        CASE
                            /* No KRA & No Skill */
                            WHEN (ea.kraratings = '{}'::jsonb AND ea.skillratings = '{}'::jsonb)
                            THEN COALESCE(ea.overallrating, 0)

                            /* Only KRA Exists → 100% KRA */
                            WHEN (ea.kraratings != '{}'::jsonb AND ea.skillratings = '{}'::jsonb)
                            THEN (
                                SELECT 
                                    SUM(
                                        (
                                            k.weightpercentage::numeric
                                            / NULLIF(
                                                (SELECT SUM(weightpercentage)
                                                 FROM performance.kras
                                                 WHERE kraid IN (
                                                     SELECT kraid::uuid
                                                     FROM jsonb_each_text(ea.kraratings)
                                                 )
                                                ), 0
                                            )
                                        ) * kr.rating::numeric
                                    )
                                FROM jsonb_each_text(ea.kraratings) kr(kraid, rating)
                                JOIN performance.kras k ON k.kraid = kr.kraid::uuid
                            )

                            /* Only Skill Exists → 100% Skill */
                            WHEN (ea.kraratings = '{}'::jsonb AND ea.skillratings != '{}'::jsonb)
                            THEN (
                                SELECT AVG(s.rating::numeric)
                                FROM jsonb_each_text(ea.skillratings) s(skill_id, rating)
                            )

                            /* Both Exist → Weighted */
                            ELSE (
                                WITH 
                                kra_total AS (
                                    SELECT COALESCE(SUM(weightpercentage), 0) AS wt
                                    FROM performance.kras
                                    WHERE kraid IN (
                                        SELECT kraid::uuid
                                        FROM jsonb_each_text(ea.kraratings)
                                    )
                                ),
                                kra_score AS (
                                    SELECT SUM(
                                        (
                                            k.weightpercentage::numeric 
                                            / NULLIF((SELECT wt FROM kra_total), 0)
                                        ) * kr.rating::numeric
                                    ) AS score
                                    FROM jsonb_each_text(ea.kraratings) kr(kraid, rating)
                                    JOIN performance.kras k ON k.kraid = kr.kraid::uuid
                                ),
                                skill_score AS (
                                    SELECT AVG(s.rating::numeric) AS score
                                    FROM jsonb_each_text(ea.skillratings) s(skill_id, rating)
                                )
                                SELECT 
                                    (SELECT score FROM kra_score) *
                                    ((SELECT wt FROM kra_total) / 100.0)
                                    +
                                    (SELECT score FROM skill_score) *
                                    ((100 - (SELECT wt FROM kra_total)) / 100.0)
                            )
                        END AS rating
                    FROM performance.employeeappraisals ea
                    INNER JOIN performance.appraisalcycles ac ON ea.cycleid = ac.cycleid
                    WHERE ea.employeeid = @EmployeeId
                    ORDER BY ac.startdate DESC";

                var cycleRatings = await QueryAsync<CycleRatingDto>(appraisalsSql, new { EmployeeId = employeeId });

                // Step 3: Get all assessed skills for this employee
                var skillsSql = @"
                    SELECT DISTINCT
                        ss.skillname
                    FROM performance.employeeskills es
                    INNER JOIN performance.skillsets ss ON es.skillid = ss.skillid
                    WHERE es.employeeid = @EmployeeId
                        AND es.assessedby IS NOT NULL
                        AND ss.isactive = true
                    ORDER BY ss.skillname";

                var skills = await QueryAsync<string>(skillsSql, new { EmployeeId = employeeId });

                result.Employees.Add(new TeamPerformanceEmployeeDto
                {
                    EmployeeId = employeeId,
                    EmployeeName = employeeName,
                    CycleRatings = cycleRatings.ToList(),
                    AssessedSkills = skills.ToList()
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving team performance overview for manager {managerEmployeeId}", ex);
        }
    }
}
