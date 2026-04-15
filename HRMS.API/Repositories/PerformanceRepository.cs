using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Controllers;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using System.Diagnostics.Metrics;
using System.Text.Json;

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

    public async Task<AppraisalCycle> CreateAppraisalCycleAsync(
    CreateAppraisalCycleRequest request,
    Guid organizationId,
    Guid currentEmployeeId)
    {
        var cycleId = await ExecuteScalarAsync<Guid>(
            @"SELECT performance.fn_create_appraisal_cycle(
            p_organization_id => @organizationId,
            p_cycle_name => @cycleName,
            p_start_date => @startDate,
            p_end_date => @endDate,
            p_created_by => @createdBy,
            p_cycle_type => @cycleType,
            p_review_start_date => @reviewStartDate,
            p_review_end_date => @reviewEndDate,
            p_status => @status,
            p_self_assessment_enable => @selfAssessmentEnable,
            p_manager_review => @managerReview,
            p_appraisal_enable => @appraisalEnable,
            p_description => @description
        )",
            new
            {
                organizationId,
                cycleName = request.CycleName,
                startDate = request.StartDate,
                endDate = request.EndDate,
                createdBy = currentEmployeeId,
                cycleType = request.CycleType,
                reviewStartDate = request.ReviewStartDate,
                reviewEndDate = request.ReviewEndDate,
                status = string.IsNullOrWhiteSpace(request.Status) ? "upcoming" : request.Status.ToLower(),
                selfAssessmentEnable = request.IsSelfAssessmentEnable,
                managerReview = request.ManagerReview,
                appraisalEnable = request.IsAppraisalEnable,
                description = request.Description
            });

        return new AppraisalCycle
        {
            CycleId = cycleId,
            OrganizationId = organizationId,
            CycleName = request.CycleName,
            CycleType = request.CycleType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ReviewPeriodStart = request.ReviewStartDate,
            ReviewPeriodEnd = request.ReviewEndDate,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "upcoming" : request.Status.ToLower(),
            Description = request.Description,
            SelfReviewEnabled = request.IsSelfAssessmentEnable,
            ManagerReviewEnabled = request.ManagerReview,
            AppraisalEnabled = request.IsAppraisalEnable,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    //public async Task<List<AppraisalCycleDto>> GetAppraisalCyclesAsync(Guid organizationId)
    //{
    //    var sql = @"
    //        SELECT ac.cycleid, ac.cyclename, ac.description, ac.startdate, ac.enddate,
    //               ac.status, ac.ratingscale, ac.selfreviewenabled, ac.peerreviewenabled,
    //               ac.managerreviewenabled,
    //               COUNT(ea.appraisalid) as total_appraisals,
    //               COUNT(CASE WHEN ea.status = 'completed' THEN 1 END) as completed_appraisals
    //        FROM performance.appraisalcycles ac
    //        LEFT JOIN performance.employeeappraisals ea ON ac.cycleid = ea.cycleid
    //        WHERE ac.organizationid = @organizationId
    //        GROUP BY ac.cycleid, ac.cyclename, ac.description, ac.startdate, ac.enddate,
    //                 ac.status, ac.ratingscale, ac.selfreviewenabled, ac.peerreviewenabled,
    //                 ac.managerreviewenabled
    //        ORDER BY ac.startdate DESC";

    //    var results = await QueryAsync<AppraisalCycleDto>(sql, new { organizationId });
    //    return results.ToList();
    //}

    public async Task<List<AppraisalCycleDto>> GetAppraisalCyclesAsync(Guid organizationId)
    {
        var sql = @"
       SELECT 
            ac.cycleid AS CycleId,
            ac.cyclename AS CycleName,
            ac.cycletype AS CycleType,
            ac.startdate AS StartDate,
            ac.enddate AS EndDate,
            ac.reviewstartdate AS ReviewStartDate,
            ac.reviewenddate AS ReviewEndDate,
            ac.status AS Status,
            ac.organizationid AS OrganizationId,

            ac.isselfassessmentenable AS SelfReviewEnabled,
            ac.managerreview AS ManagerReviewEnabled,
            ac.isappraisalenable AS AppraisalEnabled,

            ac.createdby AS CreatedBy,
            ac.createdat AS CreatedAt,
            ac.updatedat AS UpdatedAt,

            COUNT(ea.appraisalid) AS TotalAppraisals,
            COUNT(CASE WHEN ea.status = 'completed' THEN 1 END) AS CompletedAppraisals

        FROM performance.appraisal_cycle ac
        LEFT JOIN performance.employeeappraisals ea 
            ON ac.cycleid = ea.cycleid
        WHERE ac.organizationid = @organizationId
        GROUP BY 
            ac.cycleid,
            ac.cyclename,
            ac.cycletype,
            ac.startdate,
            ac.enddate,
            ac.reviewstartdate,
            ac.reviewenddate,
            ac.status,
            ac.organizationid,
            ac.isselfassessmentenable,
            ac.managerreview,
            ac.isappraisalenable,
            ac.createdby,
            ac.createdat,
            ac.updatedat
        ORDER BY ac.startdate DESC;
    ";

        var results = await QueryAsync<AppraisalCycleDto>(sql, new { organizationId });
        return results.ToList();
    }

   

    public async Task<EmployeeAppraisal> CreateEmployeeAppraisalAsync(EmployeeAppraisal appraisal)
    {
        try
        {
            // ---------------------------------------------------------------
            // STEP 0: Extract incoming KRA IDs
            // ---------------------------------------------------------------
            var incomingKraIds = appraisal.KraRatings?.Keys?.ToArray();

            if (incomingKraIds == null || incomingKraIds.Length == 0)
                throw new Exception("No KRA provided for appraisal.");

            // ---------------------------------------------------------------
            // STEP 1: Check if ANY KRA already submitted
            // ---------------------------------------------------------------
            var kraCheckSql = @"
            SELECT DISTINCT key AS kraid
            FROM performance.employeeappraisals ea,
                 jsonb_object_keys(ea.kraratings) AS key
            WHERE ea.cycleid = @cycleId
              AND ea.employeeid = @employeeId
              AND ea.reviewtype = @reviewType
              AND ea.status = 'Submitted'
              AND key = ANY(@kraIds);
        ";

            var submittedKras = (await QueryAsync<string>(kraCheckSql, new
            {
                cycleId = appraisal.CycleId,
                employeeId = appraisal.EmployeeId,
                reviewType = appraisal.ReviewType,
                kraIds = incomingKraIds
            })).ToList();

            // ❌ BLOCK if any KRA already submitted
            if (submittedKras.Any())
            {
                throw new InvalidOperationException(
                    $"Appraisal already submitted for KRA(s): {string.Join(", ", submittedKras)}"
                );
            }

            // ---------------------------------------------------------------
            // STEP 2: Existing appraisal check (Draft / Under Review only)
            // ---------------------------------------------------------------
            var checkSql = @"
            SELECT appraisalid
            FROM performance.employeeappraisals
            WHERE cycleid = @cycleId
              AND employeeid = @employeeId
              AND reviewerid = @reviewerId
              AND reviewtype = @reviewType
              AND status <> 'Submitted';
        ";

            var existingId = await ExecuteScalarAsync<Guid?>(checkSql, new
            {
                cycleId = appraisal.CycleId,
                employeeId = appraisal.EmployeeId,
                reviewerId = appraisal.ReviewerId,
                reviewType = appraisal.ReviewType
            });

            // ---------------------------------------------------------------
            // CASE 1: UPDATE (only non-submitted)
            // ---------------------------------------------------------------
            if (existingId != null)
            {
                                var updateSql = @"
                UPDATE performance.employeeappraisals
                SET
                    kraratings = COALESCE(kraratings, '{}'::jsonb)
                                 || @kraRatings::jsonb,
                    feedback = @feedback,
                    improvementareas = @improvementAreas,
                    developmentplan = @developmentPlan,
                    status = @status, 
                    updatedat = NOW()
                WHERE appraisalid = @id
                RETURNING appraisalid;
                ";

                                appraisal.AppraisalId = await ExecuteScalarAsync<Guid>(updateSql, new
                                {
                                    id = existingId,
                                    kraRatings = JsonSerializer.Serialize(appraisal.KraRatings),
                                    feedback = appraisal.Feedback,
                                    improvementAreas = appraisal.ImprovementAreas,
                                    developmentPlan = appraisal.DevelopmentPlan,
                                    status = appraisal.Status
                                });

                return appraisal;
            }

            // ---------------------------------------------------------------
            // CASE 2: INSERT
            // ---------------------------------------------------------------
            var insertSql = @"
            INSERT INTO performance.employeeappraisals (
                cycleid, employeeid, reviewerid, reviewtype,
                overallrating, kraratings, skillratings,
                goalsachieved, feedback, improvementareas,
                developmentplan, status
            )
            VALUES (
                @cycleId, @employeeId, @reviewerId, @reviewType,
                @overallRating, @kraRatings::jsonb,
                @skillRatings::jsonb, @goalsAchieved::jsonb,
                @feedback, @improvementAreas, @developmentPlan, @status
            )
            RETURNING appraisalid;
        ";

            appraisal.AppraisalId = await ExecuteScalarAsync<Guid>(insertSql, new
            {
                cycleId = appraisal.CycleId,
                employeeId = appraisal.EmployeeId,
                reviewerId = appraisal.ReviewerId,
                reviewType = appraisal.ReviewType,
                overallRating = appraisal.OverallRating,
                kraRatings = JsonSerializer.Serialize(appraisal.KraRatings),
                skillRatings = JsonSerializer.Serialize(appraisal.SkillRatings),
                goalsAchieved = JsonSerializer.Serialize(appraisal.GoalsAchieved),
                feedback = appraisal.Feedback,
                improvementAreas = appraisal.ImprovementAreas,
                developmentPlan = appraisal.DevelopmentPlan,
                status = appraisal.Status
            });
            if (appraisal.AppraisalId != null)
            {
                // 2️⃣ Update selfassessments
                var updateSelfSql = @"
                                UPDATE performance.selfassessments 
                                SET status = @status
                                WHERE cycleid = @cycleId AND employeeid = @employeeId;
                                ";

                await ExecuteAsync(updateSelfSql, new
                {
                    cycleId = appraisal.CycleId,
                    employeeId = appraisal.EmployeeId,
                    status = appraisal.Status
                });
            }
            return appraisal;
        }
        catch (InvalidOperationException ex)
        {
            // 👇 This will be sent as WARNING to frontend
            throw;
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

    //public async Task<PerformanceSummaryDto> GetPerformanceSummaryAsync(Guid organizationId, Guid? cycleId = null)
    //{
    //    // Get summary statistics
    //    var summarySql = @"
    //        SELECT 
    //            COALESCE(AVG(overallrating), 0) as average_rating,
    //            COUNT(*) as total_appraisals,
    //            COUNT(CASE WHEN ea.status IN ('draft', 'pending', 'under_review') THEN 1 END) as pending_appraisals
    //        FROM performance.employeeappraisals ea
    //        INNER JOIN employee.employees e ON ea.employeeid = e.employeeid
    //        WHERE e.organizationid = @organizationId
    //          AND (@cycleId IS NULL OR ea.cycleid = @cycleId)";

    //    var summaryResult = await QuerySingleOrDefaultAsync<dynamic>(summarySql, new { organizationId, cycleId });

    //    // Get rating distribution
    //    var distributionSql = @"
    //        SELECT 
    //            FLOOR(overallrating)::int as rating,
    //            COUNT(*) as count
    //        FROM performance.employeeappraisals ea
    //        INNER JOIN employee.employees e ON ea.employeeid = e.employeeid
    //        WHERE e.organizationid = @organizationId
    //          AND (@cycleId IS NULL OR ea.cycleid = @cycleId)
    //          AND ea.overallrating IS NOT NULL
    //        GROUP BY FLOOR(overallrating)::int
    //        ORDER BY rating";

    //    var distributionResults = await QueryAsync<dynamic>(distributionSql, new { organizationId, cycleId });

    //    var ratingDistribution = new Dictionary<string, long>();
    //    foreach (var item in distributionResults)
    //    {
    //        ratingDistribution[item.rating.ToString()] = (long)item.count;
    //    }

    //    // Get top performers
    //    var topPerformers = await GetTopPerformersAsync(organizationId, 10);

    //    return new PerformanceSummaryDto
    //    {
    //        AverageRating = summaryResult?.average_rating ?? 0,
    //        TotalAppraisals = summaryResult?.total_appraisals ?? 0,
    //        PendingAppraisals = summaryResult?.pending_appraisals ?? 0,
    //        RatingDistribution = ratingDistribution,
    //        TopPerformers = topPerformers
    //    };
    //}



    public async Task<PerformanceSummaryDto> GetPerformanceSummaryAsync(
    Guid organizationId,
    Guid? cycleId = null)
    {
        // 🔹 Summary Stats
        var summarySql = @"
        SELECT 
            COALESCE(AVG(finalrating), 0) AS average_rating,
            COUNT(*) AS total_appraisals,

            COUNT(CASE 
                WHEN status IN ('PENDING', 'DRAFT') 
                THEN 1 
            END) AS pending_appraisals

        FROM performance.hrappraisals
        WHERE organizationid = @OrganizationId
          AND (@CycleId IS NULL OR cycleid = @CycleId);
    ";

        var summaryResult = await QuerySingleOrDefaultAsync<dynamic>(summarySql, new
        {
            OrganizationId = organizationId,
            CycleId = cycleId
        });

        // 🔹 Rating Distribution (EXACT POINTS like 3.4, 4.2)
        var distributionSql = @"
    SELECT 
        FLOOR(finalrating)::int AS rating,
        COUNT(*) AS count
    FROM performance.hrappraisals
    WHERE organizationid = @OrganizationId
      AND (@CycleId IS NULL OR cycleid = @CycleId)
      AND finalrating IS NOT NULL
    GROUP BY FLOOR(finalrating)::int
    ORDER BY rating;
";

        var distributionResults = await QueryAsync<dynamic>(distributionSql, new
        {
            OrganizationId = organizationId,
            CycleId = cycleId
        });

        var ratingDistribution = new Dictionary<string, long>();

        foreach (var item in distributionResults)
        {
            string key = item.rating.ToString();   // already grouped in SQL

            ratingDistribution[key] = (long)item.count;
        }

        var topPerformers = await GetTopPerformersAsync(organizationId, 10);
        // 🔹 Final Response
        return new PerformanceSummaryDto
        {
            AverageRating = summaryResult?.average_rating != null
                ? Math.Round((decimal)summaryResult.average_rating, 1)
                : 0,

            TotalAppraisals = summaryResult?.total_appraisals ?? 0,
            PendingAppraisals = summaryResult?.pending_appraisals ?? 0,

            RatingDistribution = ratingDistribution,
            TopPerformers = topPerformers.ToList()
        };
    }


    public async Task<Guid?> GetCurrentCycleid(Guid organizationId)
    {
        try
        {
            var sql = @"
        SELECT cycleid
        FROM performance.appraisal_cycle
        WHERE organizationid = @OrganizationId
          AND (
                (NOW() BETWEEN startdate AND enddate)   -- active cycle
              )
        ORDER BY createdat DESC
        LIMIT 1;
        ";

            var cycleId = await QuerySingleOrDefaultAsync<Guid?>(sql, new
            {
                OrganizationId = organizationId
            });

            // 🔹 If no active cycle found → get latest one
            if (cycleId == null)
            {
                var fallbackSql = @"
            SELECT cycleid
            FROM performance.appraisal_cycle
            WHERE organizationid = @OrganizationId
            ORDER BY createdat DESC
            LIMIT 1;
            ";

                cycleId = await QuerySingleOrDefaultAsync<Guid?>(fallbackSql, new
                {
                    OrganizationId = organizationId
                });
            }

            return cycleId;
        }
        catch (Exception)
        {
            throw;
        }
    }
    //mm

    public async Task<MYPerformanceSummary> GetMyPerformanceSummaryAsync(Guid organizationId,Guid employeeId,Guid? cycleId)
    {
        try
        {
            var sql = @"
        SELECT 
            -- 🔹 Current Cycle Rating (NO AVG)
            COALESCE((
                SELECT finalrating
                FROM performance.hrappraisals
                WHERE organizationid = @OrganizationId
                  AND employeeid = @EmployeeId
                  AND cycleid = @CycleId
                  AND finalrating IS NOT NULL
                ORDER BY createdat DESC
                LIMIT 1
            ), 0) AS CurrentCycleRating,

            -- 🔹 Overall Rating (AVG of all cycles)
            COALESCE((
                SELECT AVG(finalrating)
                FROM performance.hrappraisals
                WHERE organizationid = @OrganizationId
                  AND employeeid = @EmployeeId
                  AND finalrating IS NOT NULL
            ), 0) AS OverallRating;
        ";

            var result = await QuerySingleOrDefaultAsync<MYPerformanceSummary>(sql, new
            {
                OrganizationId = organizationId,
                EmployeeId = employeeId,
                CycleId = cycleId
            });

            // 🔹 Clean rounding
            if (result != null)
            {
                result.CurrentCycleRating = Math.Round(result.CurrentCycleRating, 1);
                result.OverallRating = Math.Round(result.OverallRating, 1);
            }

            return result ?? new MYPerformanceSummary();
        }
        catch (Exception)
        {
            throw;
        }
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

    //public async Task<List<TopPerformerDto>> GetTopPerformersAsync(Guid organizationId, int count = 10)
    //{
    //    var sql = @"
    //        SELECT CONCAT(e.firstname, ' ', e.lastname) as employee_name,
    //               AVG(ea.overallrating) as rating,
    //               d.departmentname as department
    //        FROM performance.employeeappraisals ea
    //        INNER JOIN employee.employees e ON ea.employeeid = e.employeeid
    //        LEFT JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
    //        LEFT JOIN employee.departments d ON ed.departmentid = d.departmentid
    //        WHERE e.organizationid = @organizationId
    //          AND ea.overallrating IS NOT NULL
    //        GROUP BY e.employeeid, e.firstname, e.lastname, d.departmentname
    //        ORDER BY AVG(ea.overallrating) DESC
    //        LIMIT @count";

    //    var results = await QueryAsync<TopPerformerDto>(sql, new { organizationId, count });
    //    return results.ToList();
    //}

    public async Task<List<TopPerformerDto>> GetTopPerformersAsync(
    Guid organizationId,
    int count = 10)
    {
        var sql = @"
        SELECT 
            CONCAT(e.firstname, ' ', e.lastname) AS EmployeeName,
            AVG(hr.finalrating) AS Rating,
            d.departmentname AS Department
        FROM performance.hrappraisals hr
        INNER JOIN employee.employees e 
            ON hr.employeeid = e.employeeid
        LEFT JOIN employee.employmentdetails ed 
            ON e.employeeid = ed.employeeid AND ed.iscurrent = true
        LEFT JOIN employee.departments d 
            ON ed.departmentid = d.departmentid
        WHERE hr.organizationid = @OrganizationId
          AND hr.finalrating IS NOT NULL
        GROUP BY e.employeeid, e.firstname, e.lastname, d.departmentname
        ORDER BY AVG(hr.finalrating) DESC
        LIMIT @Count;
    ";

        var results = await QueryAsync<TopPerformerDto>(sql, new
        {
            OrganizationId = organizationId,
            Count = count
        });

        return results.ToList();
    }
    public async Task<KRADto> CreateKRAAsync(CreateKRARequest request, Guid organizationId, Guid currentEmpId)
    {
        var sql = @"
    INSERT INTO performance.kra (
        kraname,
        kradescription,
        isactive,
        createdby,
        createdat,
        cycleid,
        organizationid
    )
    VALUES (
        @KraName,
        @KraDescription,
        @IsActive,
        @CreatedBy,
        NOW(),
        @CycleId,
        @OrganizationId
    )
    RETURNING 
        kraid AS KraId,
        kraname AS Title,
        kradescription AS KraDescription,
        isactive AS IsActive,
        cycleid AS CycleId,
        organizationid AS OrganizationId,
        createdat AS CreatedAt;
    ";

        var parameters = new
        {
            KraName = request.Title,
            KraDescription = request.KraDescription,
            CycleId = request.CycleId,
            OrganizationId = organizationId,
            IsActive = request.IsActive,
            CreatedBy = currentEmpId
        };

        var result = await QuerySingleOrDefaultAsync<KRADto>(sql, parameters);

        return result!;
    }

    public async Task<List<KRADto>> GetKRAsAsync(Guid organizationId)
    {
        const string sql = @"
SELECT
    k.kraid AS KraId,
    k.kraname AS Title,
    k.kradescription AS KraDescription,
    COALESCE(k.krarate, '0') AS KraRate,
    k.isactive AS IsActive,
    k.createdat AS CreatedAt,
    k.cycleid AS CycleId,
    k.organizationid AS OrganizationId,
    k.noofgoals AS noOfGoals,

    -- ✅ Cycle Name
    COALESCE(ac.cyclename, '') AS CycleName,

    -- ✅ Employee Name
    COALESCE(e.firstname || ' ' || e.lastname, '') AS CreatedByName

FROM performance.kra k

-- ✅ Join with Appraisal Cycle
LEFT JOIN performance.appraisal_cycle ac
    ON ac.cycleid = k.cycleid

-- ✅ Join with Employee
LEFT JOIN employee.employees e 
    ON e.employeeid = k.createdby

WHERE k.organizationid = @OrganizationId

ORDER BY k.createdat DESC;
";

        var parameters = new { OrganizationId = organizationId };

        var result = await QueryAsync<KRADto>(sql, parameters);

        return result.ToList();
    }




    public async Task<KRADto> GetKraByIdAsync(Guid kraId)
    {
        const string sql = @"
    SELECT
        k.kraid AS KraId,
        k.kraname AS Title,
        k.kradescription AS KraDescription,
        COALESCE(k.krarate, '0') AS KraRate,
        k.isactive AS IsActive,
        k.createdat AS CreatedAt,
        k.cycleid AS CycleId,
        k.organizationid AS OrganizationId,

        -- ✅ Created By Name
        COALESCE(e.firstname || ' ' || e.lastname, '') AS CreatedByName

    FROM performance.kra k
    LEFT JOIN employee.employees e 
        ON e.employeeid = k.createdby

    WHERE k.kraid = @KraId;
    ";

        var parameters = new { KraId = kraId };

        var result = await QuerySingleOrDefaultAsync<KRADto>(sql, parameters);

        return result!;
    }

    public async Task<KRADto> UpdatekraAsync(Guid kraId, UpdateKRARequest request)
    {
        var sql = @"
    UPDATE performance.kra
    SET 
        kraname = @Title,
        kradescription = @KraDescription,
         cycleid=@CycleId,
        isactive = @IsActive,
        updatedat = NOW()
    WHERE kraid = @KraId;

    SELECT
        k.kraid AS KraId,
        k.kraname AS Title,
        k.kradescription AS KraDescription,
        COALESCE(k.krarate, '0') AS KraRate,
        k.isactive AS IsActive,
        k.createdat AS CreatedAt,
        k.cycleid AS CycleId,
        k.organizationid AS OrganizationId,
        COALESCE(e.firstname || ' ' || e.lastname, '') AS CreatedByName

    FROM performance.kra k
    LEFT JOIN employee.employees e 
        ON e.employeeid = k.createdby

    WHERE k.kraid = @KraId;
    ";

        var parameters = new
        {
            KraId = kraId,
            Title = request.Title,
            KraDescription = request.KraDescription,
            CycleId=request.CycleId,
            IsActive = request.IsActive
        };

        var result = await QuerySingleOrDefaultAsync<KRADto>(sql, parameters);

        return result!;
    }

    public async Task<bool> UpdateKRAStatusAsync(Guid kraId, bool isActive)
    {
        var sql = @"
        UPDATE performance.kra
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

    public async Task<GoalDto> CreateGoalAsync(CreateGoalRequest request, Guid organizationId, Guid createdBy)
    {
        var sql = @"
INSERT INTO performance.goals (
    title,
    description,
    progress,
    startdate,
    enddate,
    kraid,
    organizationid,
    createdby,
    createdat,
    isactive
)
VALUES (
    @Title,
    @Description,
    @Progress,
    @StartDate,
    @EndDate,
    @KRAId,
    @OrganizationId,
    @CreatedBy,
    NOW(),
    TRUE
)
RETURNING 
    goalid AS GoalId,
    title AS Title,
    description AS Description,
    progress AS Progress,
    startdate AS StartDate,
    enddate AS EndDate,
    kraid AS KRAId,
    assignedto AS AssignedTo,
    organizationid AS OrganizationId,
    createdby AS CreatedBy,
    createdat AS CreatedAt,
    isactive AS IsActive;
";

        var parameters = new
        {
            Title = request.Title,
            Description = request.Description,
            Progress = request.Progress,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            KRAId = request.KRAId,
            OrganizationId = organizationId,
            CreatedBy = createdBy
        };

        var result = await QuerySingleOrDefaultAsync<GoalDto>(sql, parameters);

        return result!;
    }

    public async Task<List<GoalDto>> GetCompletedGoalsByKRAAsync(Guid kraId)
    {
        var sql = @"
SELECT 
    goalid AS GoalId,
    title AS Title,
    description AS Description,
    progress AS Progress,
    startdate AS StartDate,
    enddate AS EndDate,
    kraid AS KRAId,
    assignedto AS AssignedTo,
    organizationid AS OrganizationId,
    createdby AS CreatedBy,
    createdat AS CreatedAt,
    isactive AS IsActive
FROM performance.goals
WHERE kraid = @KRAId;
";

        var parameters = new { KRAId = kraId };

        var result = await QueryAsync<GoalDto>(sql, parameters);

        return result.ToList();
    }

    public async Task<bool> UpdateKRAProgressAsync(Guid kraId, string krarate, int noOfGoals)
    {
        var sql = @"
UPDATE performance.kra
SET krarate = @KRARate,
    noofgoals = @NoOfGoals,
    updatedat = NOW()
WHERE kraid = @KRAId;
";

        var parameters = new
        {
            KRAId = kraId,
            KRARate = krarate,
            NoOfGoals = noOfGoals
        };

        var rowsAffected = await ExecuteAsync(sql, parameters);
        return rowsAffected > 0;
    }
    public async Task<List<GoalDto>> GetAllGoalsAsync(Guid organizationId)
    {
        var sql = @"
SELECT
    g.goalid         AS GoalId,
    g.title          AS Title,
    g.description    AS Description,
    g.progress       AS Progress,
    g.startdate      AS StartDate,
    g.enddate        AS EndDate,
    g.kraid          AS KRAId,
    k.kraname        AS KraName,
    g.assignedto     AS AssignedTo,
    CONCAT(ea.firstname, ' ', ea.lastname) AS AssignedToName,
    g.createdby      AS CreatedBy,
    CONCAT(ec.firstname, ' ', ec.lastname) AS CreatedByName,
    g.organizationid AS OrganizationId,
    g.isactive       AS IsActive,
    g.createdat      AS CreatedAt
FROM performance.goals g
LEFT JOIN performance.kra k  ON k.kraid        = g.kraid
LEFT JOIN employee.employees ea ON ea.employeeid = g.assignedto
LEFT JOIN employee.employees ec ON ec.employeeid = g.createdby
WHERE g.organizationid = @OrganizationId
  AND g.isactive = TRUE
ORDER BY g.createdat DESC;";

        var result = await QueryAsync<GoalDto>(sql, new { OrganizationId = organizationId });
        return result.ToList();
    }

    public async Task<List<GoalDto>> GetEmployeeAssignedGoalAsync(Guid organizationId, Guid employeeId)
    {
        var sql = @"
SELECT
    g.goalid         AS GoalId,
    g.title          AS Title,
    g.description    AS Description,
    g.progress       AS Progress,
    g.startdate      AS StartDate,
    g.enddate        AS EndDate,
    g.kraid          AS KRAId,
    k.kraname        AS KraName,
    g.assignedto     AS AssignedTo,
    CONCAT(ea.firstname, ' ', ea.lastname) AS AssignedToName,
    g.createdby      AS CreatedBy,
    CONCAT(ec.firstname, ' ', ec.lastname) AS CreatedByName,
    g.organizationid AS OrganizationId,
    g.isactive       AS IsActive,
    g.createdat      AS CreatedAt
FROM performance.goals g
LEFT JOIN performance.kra k  ON k.kraid        = g.kraid
LEFT JOIN employee.employees ea ON ea.employeeid = g.assignedto
LEFT JOIN employee.employees ec ON ec.employeeid = g.createdby
WHERE g.organizationid = @OrganizationId
  AND g.assignedto = @EmployeeId
  AND g.isactive = TRUE
ORDER BY g.createdat DESC;";

        var result = await QueryAsync<GoalDto>(sql, new
        {
            OrganizationId = organizationId,
            EmployeeId = employeeId
        });

        return result.ToList();
    }

    public async Task<GoalDto?> GetGoalByIdAsync(Guid goalId)
    {
        var sql = @"
SELECT
    g.goalid         AS GoalId,
    g.title          AS Title,
    g.description    AS Description,
    g.progress       AS Progress,
    g.startdate      AS StartDate,
    g.enddate        AS EndDate,
    g.kraid          AS KRAId,
    k.kraname        AS KraName,
    g.assignedto     AS AssignedTo,
    CONCAT(ea.firstname, ' ', ea.lastname) AS AssignedToName,
    g.createdby      AS CreatedBy,
    CONCAT(ec.firstname, ' ', ec.lastname) AS CreatedByName,
    g.organizationid AS OrganizationId,
    g.isactive       AS IsActive,
    g.createdat      AS CreatedAt
FROM performance.goals g
LEFT JOIN performance.kra k     ON k.kraid        = g.kraid
LEFT JOIN employee.employees ea ON ea.employeeid  = g.assignedto
LEFT JOIN employee.employees ec ON ec.employeeid  = g.createdby
WHERE g.goalid = @GoalId
  AND g.isactive = TRUE;";

        return await QuerySingleOrDefaultAsync<GoalDto>(sql, new { GoalId = goalId });
    }

    public async Task<GoalDto?> UpdateGoalAsync(Guid organizationId, UpdateGoalRequest request)
    {
        var sql = @"
WITH updated AS (
    UPDATE performance.goals SET
        title       = COALESCE(@Title, title),
        description = COALESCE(@Description, description),
        progress    = COALESCE(@Progress, progress),
        startdate   = COALESCE(@StartDate, startdate),
        enddate     = COALESCE(@EndDate, enddate),
        kraid       = COALESCE(@KRAId, kraid)
    WHERE goalid = @GoalId
      AND organizationid = @OrganizationId
      AND isactive = TRUE
    RETURNING *
)
SELECT
    u.goalid         AS GoalId,
    u.title          AS Title,
    u.description    AS Description,
    u.progress       AS Progress,
    u.startdate      AS StartDate,
    u.enddate        AS EndDate,
    u.kraid          AS KRAId,
    k.kraname        AS KraName,
    u.assignedto     AS AssignedTo,
    CONCAT(ea.firstname, ' ', ea.lastname) AS AssignedToName,
    u.createdby      AS CreatedBy,
    CONCAT(ec.firstname, ' ', ec.lastname) AS CreatedByName,
    u.organizationid AS OrganizationId,
    u.isactive       AS IsActive,
    u.createdat      AS CreatedAt
FROM updated u
LEFT JOIN performance.kra k     ON k.kraid        = u.kraid
LEFT JOIN employee.employees ea ON ea.employeeid  = u.assignedto
LEFT JOIN employee.employees ec ON ec.employeeid  = u.createdby;";

        return await QuerySingleOrDefaultAsync<GoalDto>(sql, new
        {
            GoalId = request.GoalId,
            OrganizationId = organizationId,
            Title = request.Title,
            Description = request.Description,
            Progress = request.Progress,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            KRAId = request.KRAId
        });
    }

    public async Task DeleteGoalAsync(Guid organizationId, Guid goalId)
    {
        var sql = @"
UPDATE performance.goals
SET isactive = FALSE
WHERE goalid = @GoalId
  AND organizationid = @OrganizationId;";

        await ExecuteAsync(sql, new { GoalId = goalId, OrganizationId = organizationId });
    }
    public async Task<bool> UpdateGoalAssignmentAsync(
      Guid organizationId,
      Guid goalId,
      Guid assignedTo ,
      Guid assignedBy )
    {
        var sql = @"
UPDATE performance.goals
SET
    assignedto = COALESCE(@AssignedTo, assignedto),
    assignedby = COALESCE(@AssignedBy, assignedby),
    updatedat = NOW()
WHERE goalid = @GoalId
  AND isactive = TRUE;
";

        var affectedRows = await ExecuteAsync(sql, new
        {
            GoalId = goalId,
            AssignedTo = assignedTo,
            AssignedBy = assignedBy
        });

        return affectedRows > 0;
    }



    //    public async Task<SelfAssessmentDto> CreateSelfAssessmentAsync(CreateSelfAssessmentRequest request,Guid organizationId,Guid currentEmployeeid)
    //    {
    //        // ---------------------------------------------
    //        // 1) Check if the same KRA already exists for the employee & cycle
    //        // ---------------------------------------------
    //        var checkSql = @"
    //        SELECT COUNT(*) 
    //        FROM performance.selfassessments
    //        WHERE employeeid = @EmployeeId 
    //          AND kraid = @KraId;
    //    ";

    //        var existingCount = await QuerySingleOrDefaultAsync<int>(checkSql, request);

    //        if (existingCount > 0)
    //        {
    //            throw new Exception("A self-assessment for this KRA already exists for the selected cycle and employee.");
    //        }

    //        // ---------------------------------------------
    //        // 2) Insert new self-assessment
    //        // ---------------------------------------------
    //        var insertSql = @"
    //INSERT INTO performance.selfassessments
    //    (goalid, kraid, organizationid, selfrating, selfcomment, status, createdat, updatedat)
    //VALUES
    //    (@GoalId, @KraId, @OrganizationId, @SelfRating, @SelfComment, 'SUBMITTED', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
    //RETURNING selfassessmentid, goalid, kraid, organizationid, selfrating, selfcomment, status, createdat, updatedat;
    //";

    //        return await QuerySingleOrDefaultAsync<SelfAssessmentDto>(insertSql, request);
    //    }

    public async Task<SelfAssessmentDto> CreateSelfAssessmentAsync(CreateSelfAssessmentRequest request, Guid organizationId, Guid currentEmployeeId)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // ---------------------------------------------
        // 1) Check if the same KRA already exists for the employee
        // ---------------------------------------------
        var checkSql = @"
        SELECT COUNT(*) 
        FROM performance.selfassessments
        WHERE employeeid = @EmployeeId 
          AND kraid = @KraId;
    ";

        var existingCount = await QuerySingleOrDefaultAsync<int>(checkSql, new
        {
            EmployeeId = currentEmployeeId,
            KraId = request.KraId
        });

        if (existingCount > 0)
        {
            throw new Exception("A self-assessment for this KRA already exists for the selected employee.");
        }

        // ---------------------------------------------
        // 2) Insert new self-assessment
        // ---------------------------------------------
        var insertSql = @"
INSERT INTO performance.selfassessments
    (employeeid, goalid, kraid, organizationid, selfrating, selfcomment, status, createdat, updatedat)
VALUES
    (@EmployeeId, @GoalId, @KraId, @OrganizationId, @SelfRating, @SelfComment, 'SUBMITTED', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
RETURNING selfassessmentid, employeeid, goalid, kraid, organizationid, selfrating, selfcomment, status, createdat, updatedat;
";

        return await QuerySingleOrDefaultAsync<SelfAssessmentDto>(insertSql, new
        {
            EmployeeId = currentEmployeeId,
            GoalId = request.GoalId,
            KraId = request.KraId,
            OrganizationId = organizationId,
            SelfRating = request.SelfRating,
            SelfComment = request.SelfComment
        });
    }



    public async Task<IEnumerable<SelfAssessmentDto>> GetSelfAssessmentsByEmployeeAsync(Guid employeeId, Guid organizationId)
    {
        var sql = @"
    SELECT 
        sa.selfassessmentid   AS SelfAssessmentId,
        sa.goalid             AS GoalId,
        sa.kraid              AS KraId,
        sa.organizationid     AS OrganizationId,
        sa.employeeid         AS EmployeeId,
        sa.selfrating         AS SelfRating,
        sa.selfcomment        AS SelfComment,
        sa.status             AS Status,
        sa.createdat          AS CreatedAt,
        sa.updatedat          AS UpdatedAt,

        g.title               AS GoalName,
        k.kraname             AS KraName,
        e.firstname || ' ' || e.lastname AS EmployeeName,

        ac.cycleid            AS CycleId,
        ac.cyclename          AS CycleName

    FROM performance.selfassessments sa

    LEFT JOIN performance.goals g  
        ON g.goalid = sa.goalid

    LEFT JOIN performance.kra k  
        ON k.kraid = sa.kraid

    LEFT JOIN performance.appraisal_cycle ac  
        ON ac.cycleid = k.cycleid   -- 🔥 important

    LEFT JOIN employee.employees e  
        ON e.employeeid = sa.employeeid

    WHERE sa.employeeid = @EmployeeId
      AND sa.organizationid = @OrganizationId

    ORDER BY sa.createdat DESC;
    ";

        return await QueryAsync<SelfAssessmentDto>(
            sql,
            new { EmployeeId = employeeId, OrganizationId = organizationId }
        );
    }

    //mm

   
    public async Task<IEnumerable<SelfAssessmentDto>> GetSelfAssessmentsByEmployeesAsync(List<Guid> employeeIds, Guid organizationId)
    {
        if (employeeIds == null || !employeeIds.Any())
            return Enumerable.Empty<SelfAssessmentDto>();

        var sql = @"
    SELECT 
        sa.selfassessmentid   AS SelfAssessmentId,
        sa.goalid             AS GoalId,
        sa.kraid              AS KraId,
        sa.organizationid     AS OrganizationId,
        sa.employeeid         AS EmployeeId,
        sa.selfrating         AS SelfRating,
        sa.selfcomment        AS SelfComment,
        sa.status             AS Status,
        sa.createdat          AS CreatedAt,
        sa.updatedat          AS UpdatedAt,

        g.title               AS GoalName,
        k.kraname             AS KraName,
        e.firstname || ' ' || e.lastname AS EmployeeName,

        ac.cycleid            AS CycleId,
        ac.cyclename          AS CycleName

    FROM performance.selfassessments sa

    LEFT JOIN performance.goals  g  
        ON g.goalid  = sa.goalid

    LEFT JOIN performance.kra    k  
        ON k.kraid   = sa.kraid

    LEFT JOIN performance.appraisal_cycle ac  
        ON ac.cycleid = k.cycleid   -- 🔥 IMPORTANT JOIN

    LEFT JOIN employee.employees e  
        ON e.employeeid = sa.employeeid

    WHERE sa.employeeid = ANY(@EmployeeIds::uuid[])
      AND sa.organizationid = @OrganizationId

    ORDER BY sa.createdat DESC;
    ";

        return await QueryAsync<SelfAssessmentDto>(
            sql,
            new { EmployeeIds = employeeIds.ToArray(), OrganizationId = organizationId }
        );
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

    public async Task<bool> UpdateGoalStatusAsync(Guid goalId, string status)
    {
        var sql = @"
        UPDATE performance.goals
        SET progress = @Status,
            updatedat = CURRENT_TIMESTAMP
        WHERE goalid = @GoalId";

        var rowsAffected = await ExecuteAsync(sql, new
        {
            GoalId = goalId,
            Status = status
        });

        return rowsAffected > 0;
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

    //public async Task<ManagerReviewDto> SubmitManagerReviewAsync(ManagerReviewRequest request)
    //{
    //    var sql = @"
    //    INSERT INTO performance.managerreviews
    //    (managerid, employeeid, cycleid, overallrating, feedback, improvementareas, developmentplan, createdat, updatedat)
    //    VALUES
    //    (@ManagerId, @EmployeeId, @CycleId, @OverallRating, @Feedback, @ImprovementAreas, @DevelopmentPlan, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
    //    RETURNING reviewid, managerid, employeeid, cycleid, overallrating, feedback, improvementareas, developmentplan, 'submitted' as status, createdat, updatedat;
    //";

    //    return await QuerySingleOrDefaultAsync<ManagerReviewDto>(sql, request);
    //}

    public async Task<ManagerReviewDto> SubmitManagerReviewAsync(
    ManagerReviewRequest request,
    Guid organizationId,
    Guid currentEmployeeId // managerId
)
    {
        var sql = @"
    INSERT INTO performance.managerreviews
    (
        organizationid,
        cycleid,
        kraid,
        goalid,
        managerid,
        employeeid,
        rating,
        feedback,
        improvementarea,
        status,
        createdat,
        updatedat
    )
    VALUES
    (
        @OrganizationId,
        @CycleId,
        @KraId,
        @GoalId,
        @ManagerId,
        @EmployeeId,
        @Rating,
        @Feedback,
        @ImprovementArea,
        @Status,
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP
    )
    RETURNING 
        managerreviewid      AS ManagerReviewId,
        organizationid       AS OrganizationId,
        managerid            AS ManagerId,
        employeeid           AS EmployeeId,
        cycleid              AS CycleId,
        kraid                AS KraId,
        goalid               AS GoalId,
        rating               AS Rating,
        feedback             AS Feedback,
        improvementarea      AS ImprovementArea,
        status               AS Status,
        createdat            AS CreatedAt;
    ";

        return await QuerySingleOrDefaultAsync<ManagerReviewDto>(sql, new
        {
            OrganizationId = organizationId,
            ManagerId = currentEmployeeId,
            request.EmployeeId,
            request.CycleId,
            request.KraId,
            request.GoalId,
            request.Rating,
            request.Feedback,
            request.ImprovementArea,
            request.Status
        });
    }

    //

    // Get reviews created by manager for their employees
    public async Task<List<ManagerReviewDto>> GetManagerReviewsAsync(
        Guid organizationId,
        Guid managerId)
    {
        var sql = @"
        SELECT 
            mr.managerreviewid      AS ManagerReviewId,
            mr.organizationid       AS OrganizationId,
            mr.managerid            AS ManagerId,
            CONCAT(mgr.firstname, ' ', mgr.lastname) AS ManagerName,
            mr.employeeid           AS EmployeeId,
            CONCAT(emp.firstname, ' ', emp.lastname) AS EmployeeName,
            mr.cycleid              AS CycleId,
            ac.cyclename            AS CycleName,
            mr.kraid                AS KraId,
            k.kraname               AS KraName,
            mr.goalid               AS GoalId,
            g.title                 AS GoalName,
            mr.rating               AS Rating,
            mr.feedback             AS Feedback,
            mr.improvementarea      AS ImprovementArea,
            mr.status               AS Status,
            mr.createdat            AS CreatedAt
        FROM performance.managerreviews mr
        INNER JOIN employee.employees emp ON mr.employeeid = emp.employeeid
        INNER JOIN employee.employees mgr ON mr.managerid = mgr.employeeid
        INNER JOIN performance.appraisal_cycle ac ON mr.cycleid = ac.cycleid
        INNER JOIN performance.kra k ON mr.kraid = k.kraid
        INNER JOIN performance.goals g ON mr.goalid = g.goalid
        WHERE mr.organizationid = @OrganizationId
            AND mr.managerid = @ManagerId
        ORDER BY mr.createdat DESC;
    ";

        var result = await QueryAsync<ManagerReviewDto>(sql, new
        {
            OrganizationId = organizationId,
            ManagerId = managerId
        });

        return result.ToList();
    }

    //Getting all manager review from manager table
    public async Task<List<ManagerReviewDto>> GetAllManagerReviewsAsync(
     Guid organizationId,
     SearchManagersDto search)
    {
        var sql = @"
    SELECT 
        mr.managerreviewid      AS ManagerReviewId,
        mr.managerid            AS ManagerId,
        CONCAT(mgr.firstname, ' ', mgr.lastname) AS ManagerName,
        mr.employeeid           AS EmployeeId,
        CONCAT(emp.firstname, ' ', emp.lastname) AS EmployeeName,
        mr.cycleid              AS CycleId,
        ac.cyclename            AS CycleName,
        mr.kraid                AS KraId,
        k.kraname               AS KraName,
        mr.goalid               AS GoalId,
        g.title                 AS GoalName,
        mr.rating               AS Rating,
        mr.feedback             AS Feedback,
        mr.improvementarea      AS ImprovementArea,
        mr.status               AS Status,
        mr.createdat            AS CreatedAt
    FROM performance.managerreviews mr
    INNER JOIN employee.employees emp ON mr.employeeid = emp.employeeid
    INNER JOIN employee.employees mgr ON mr.managerid = mgr.employeeid
    INNER JOIN performance.appraisal_cycle ac ON mr.cycleid = ac.cycleid
    INNER JOIN performance.kra k ON mr.kraid = k.kraid
    INNER JOIN performance.goals g ON mr.goalid = g.goalid
    WHERE mr.organizationid = @OrganizationId

    -- 🔹 Cycle Filter
    AND (@CycleId IS NULL OR mr.cycleid = @CycleId)

    -- 🔹 Kra Filter
    AND (@KraId IS NULL OR mr.kraid = @KraId)

    -- 🔹 Search Query
    AND (
        @SearchQuery IS NULL OR
        LOWER(CONCAT(emp.firstname, ' ', emp.lastname)) LIKE LOWER(@SearchQuery) OR
        LOWER(CONCAT(mgr.firstname, ' ', mgr.lastname)) LIKE LOWER(@SearchQuery) OR
        LOWER(mr.feedback) LIKE LOWER(@SearchQuery) OR
        LOWER(mr.improvementarea) LIKE LOWER(@SearchQuery) OR
        LOWER(k.kraname) LIKE LOWER(@SearchQuery) OR
        LOWER(g.title) LIKE LOWER(@SearchQuery)
    )

    ORDER BY mr.createdat DESC;
    ";

        var result = await QueryAsync<ManagerReviewDto>(sql, new
        {
            OrganizationId = organizationId,
            CycleId = search.CycleId,
            KraId = search.kraid,
            SearchQuery = string.IsNullOrEmpty(search.searchQuery)
                ? null
                : $"%{search.searchQuery}%"
        });

        return result.ToList();
    }

    // Get reviews received by employee from their manager
    public async Task<List<ManagerReviewDto>> GetEmployeeReviewsAsync(
        Guid organizationId,
        Guid employeeId)
    {
        var sql = @"
        SELECT 
            mr.managerreviewid      AS ManagerReviewId,
            mr.organizationid       AS OrganizationId,
            mr.managerid            AS ManagerId,
            CONCAT(mgr.firstname, ' ', mgr.lastname) AS ManagerName,
            mr.employeeid           AS EmployeeId,
            CONCAT(emp.firstname, ' ', emp.lastname) AS EmployeeName,
            mr.cycleid              AS CycleId,
            ac.cyclename            AS CycleName,
            mr.kraid                AS KraId,
            k.kraname               AS KraName,
            mr.goalid               AS GoalId,
            g.title                 AS GoalName,
            mr.rating               AS Rating,
            mr.feedback             AS Feedback,
            mr.improvementarea      AS ImprovementArea,
            mr.status               AS Status,
            mr.createdat            AS CreatedAt
        FROM performance.managerreviews mr
        INNER JOIN employee.employees emp ON mr.employeeid = emp.employeeid
        INNER JOIN employee.employees mgr ON mr.managerid = mgr.employeeid
        INNER JOIN performance.appraisal_cycle ac ON mr.cycleid = ac.cycleid
        INNER JOIN performance.kra k ON mr.kraid = k.kraid
        INNER JOIN performance.goals g ON mr.goalid = g.goalid
        WHERE mr.organizationid = @OrganizationId
            AND mr.employeeid = @EmployeeId
        ORDER BY mr.createdat DESC;
    ";

        var result = await QueryAsync<ManagerReviewDto>(sql, new
        {
            OrganizationId = organizationId,
            EmployeeId = employeeId
        });

        return result.ToList();
    }


    // Submit HR Review
     public async Task<HrReviewDto> SubmitHrReviewAsync(
    HrReviewRequest request,
    Guid organizationId,
    Guid currentEmployeeId)
    {
        var sql = @"
    INSERT INTO performance.hrappraisals
    (
        organizationid,
        cycleid,
        hrid,
        employeeid,
        finalrating,
        hrcomments,
        improvementarea,
        feedback,
        status,
        createdat,
        updatedat
    )
    VALUES
    (
        @OrganizationId,
        @CycleId,
        @HrId,
        @EmployeeId,
        @FinalRating,
        @HrComments,
        @ImprovementArea,
        @Feedback,
        @Status,
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP
    )
    RETURNING 
        hrreviewid,
        organizationid,
        cycleid,
        hrid,
        employeeid,
        finalrating,
        hrcomments,
        improvementarea,
        feedback,
        status,
        createdat,
        updatedat;
    ";

        var result = await QuerySingleOrDefaultAsync<HrReviewDto>(sql, new
        {
            OrganizationId = organizationId,
            HrId = currentEmployeeId,
            request.CycleId,
            request.EmployeeId,
            request.FinalRating,
            request.HrComments,
            request.ImprovementArea,
            request.Feedback,
            request.Status
        });

        return result;
    }



    public async Task<HrReviewDto?> GetEmployeeAppraisal(
    Guid organizationId,
    Guid cycleId,
    Guid employeeId)
    {
        var sql = @"
    SELECT 
        hr.hrreviewid           AS HrReviewId,
        hr.organizationid       AS OrganizationId,
        hr.cycleid              AS CycleId,
        ac.cyclename            AS CycleName,
        hr.hrid                 AS HrId,
        CONCAT(hremp.firstname, ' ', hremp.lastname) AS HrName,
        hr.employeeid           AS EmployeeId,
        CONCAT(emp.firstname, ' ', emp.lastname) AS EmployeeName,
        hr.finalrating          AS FinalRating,
        hr.hrcomments           AS HrComments,
        hr.improvementarea      AS ImprovementArea,
        hr.feedback             AS Feedback,
        hr.status               AS Status,
        hr.createdat            AS CreatedAt,
        hr.updatedat            AS UpdatedAt
    FROM performance.hrappraisals hr
    INNER JOIN employee.employees emp ON hr.employeeid = emp.employeeid
    INNER JOIN employee.employees hremp ON hr.hrid = hremp.employeeid
    INNER JOIN performance.appraisal_cycle ac ON hr.cycleid = ac.cycleid
    WHERE hr.organizationid = @OrganizationId
      AND hr.cycleid = @CycleId
      AND hr.employeeid = @EmployeeId
    LIMIT 1;
    ";

        var result = await QuerySingleOrDefaultAsync<HrReviewDto>(sql, new
        {
            OrganizationId = organizationId,
            CycleId = cycleId,
            EmployeeId = employeeId
        });

        return result; // ✅ null if not found
    }










    // Get HR Review by ID (helper method)
    private async Task<HrReviewDto> GetHrReviewByIdAsync(Guid hrReviewId, Guid organizationId)
    {
        var sql = @"
        SELECT 
            hr.hrreviewid           AS HrReviewId,
            hr.organizationid       AS OrganizationId,
            hr.cycleid              AS CycleId,
            ac.cyclename            AS CycleName,
            hr.kraid                AS KraId,
            k.kraname               AS KraName,
            hr.goalid               AS GoalId,
            g.title                 AS GoalName,
            hr.managerreviewid      AS ManagerReviewId,
            hr.managerid            AS ManagerId,
            CONCAT(mgr.firstname, ' ', mgr.lastname) AS ManagerName,
            mr.rating               AS ManagerRating,
            mr.feedback             AS ManagerFeedback,
            hr.employeeid           AS EmployeeId,
            CONCAT(emp.firstname, ' ', emp.lastname) AS EmployeeName,
            hr.hrrating             AS HrRating,
            hr.finalrating          AS FinalRating,
            hr.hrcomments           AS HrComments,
            hr.improvementarea      AS ImprovementArea,
            hr.feedback             AS Feedback,
            hr.status               AS Status,
            hr.createdat            AS CreatedAt,
            hr.updatedat            AS UpdatedAt
        FROM performance.hrappraisals hr
        INNER JOIN employee.employees emp ON hr.employeeid = emp.employeeid
        INNER JOIN employee.employees mgr ON hr.managerid = mgr.employeeid
        INNER JOIN performance.appraisal_cycle ac ON hr.cycleid = ac.cycleid
        INNER JOIN performance.kra k ON hr.kraid = k.kraid
        INNER JOIN performance.goals g ON hr.goalid = g.goalid
        INNER JOIN performance.managerreviews mr ON hr.managerreviewid = mr.managerreviewid
        WHERE hr.hrreviewid = @HrReviewId
            AND hr.organizationid = @OrganizationId;
    ";

        return await QuerySingleOrDefaultAsync<HrReviewDto>(sql, new
        {
            HrReviewId = hrReviewId,
            OrganizationId = organizationId
        });
    }

    // Get all HR Reviews given to other employees
    // Get all HR Reviews created by the current HR employee
    public async Task<List<HrReviewDto>> GetHrReviewsAsync(Guid organizationId, Guid CurrentEmpid)
    {
        var sql = @"
    SELECT 
        hr.hrreviewid           AS HrReviewId,
        hr.organizationid       AS OrganizationId,
        hr.cycleid              AS CycleId,
        ac.cyclename            AS CycleName,
        hr.hrid                 AS HrId,
        CONCAT(hremp.firstname, ' ', hremp.lastname) AS HrName,
        hr.employeeid           AS EmployeeId,
        CONCAT(emp.firstname, ' ', emp.lastname) AS EmployeeName,
        hr.finalrating          AS FinalRating,
        hr.hrcomments           AS HrComments,
        hr.improvementarea      AS ImprovementArea,
        hr.feedback             AS Feedback,
        hr.status               AS Status,
        hr.createdat            AS CreatedAt,
        hr.updatedat            AS UpdatedAt
    FROM performance.hrappraisals hr
    INNER JOIN employee.employees emp ON hr.employeeid = emp.employeeid
    INNER JOIN employee.employees hremp ON hr.hrid = hremp.employeeid
    INNER JOIN performance.appraisal_cycle ac ON hr.cycleid = ac.cycleid
    WHERE hr.organizationid = @OrganizationId
      AND hr.hrid = @CurrentEmpid
    ORDER BY hr.createdat DESC;
    ";

        var result = await QueryAsync<HrReviewDto>(sql, new
        {
            OrganizationId = organizationId,
            CurrentEmpid = CurrentEmpid
        });

        return result.ToList();
    }







    // Get HR own given Reviews by there hr
    // Get all HR Reviews received by the current employee
   
    public async Task<List<HrReviewDto>> GetHrReviewsByEmployeeAsync(Guid organizationId, Guid employeeId)
    {
        var sql = @"
    SELECT 
        hr.hrreviewid           AS HrReviewId,
        hr.organizationid       AS OrganizationId,
        hr.cycleid              AS CycleId,
        ac.cyclename            AS CycleName,
        hr.hrid                 AS HrId,
        CONCAT(hremp.firstname, ' ', hremp.lastname) AS HrName,
        hr.employeeid           AS EmployeeId,
        CONCAT(emp.firstname, ' ', emp.lastname) AS EmployeeName,
        hr.finalrating          AS FinalRating,     
        hr.hrcomments           AS HrComments,
        hr.improvementarea      AS ImprovementArea,
        hr.feedback             AS Feedback,
        hr.status               AS Status,
        hr.createdat            AS CreatedAt,
        hr.updatedat            AS UpdatedAt
    FROM performance.hrappraisals hr  
    INNER JOIN employee.employees emp ON hr.employeeid = emp.employeeid
    INNER JOIN employee.employees hremp ON hr.hrid = hremp.employeeid
    INNER JOIN performance.appraisal_cycle ac ON hr.cycleid = ac.cycleid
    WHERE hr.organizationid = @OrganizationId
        AND hr.employeeid = @EmployeeId
    ORDER BY hr.createdat DESC;
    ";

        var result = await QueryAsync<HrReviewDto>(sql, new
        {
            OrganizationId = organizationId,
            EmployeeId = employeeId
        });

        return result.ToList();
    }

    //Getting all hr appraisal from table
    public async Task<List<HrReviewDto>> GetAllAppraisalAsync(
    Guid organizationId,
    Guid? employeeId,
    SearchAppraisalDto search)
    {
        var sql = @"
    SELECT 
        hr.hrreviewid           AS HrReviewId,
        hr.organizationid       AS OrganizationId,
        hr.cycleid              AS CycleId,
        ac.cyclename            AS CycleName,
        hr.hrid                 AS HrId,
        CONCAT(hremp.firstname, ' ', hremp.lastname) AS HrName,
        hr.employeeid           AS EmployeeId,
        CONCAT(emp.firstname, ' ', emp.lastname) AS EmployeeName,
        hr.finalrating          AS FinalRating,     
        hr.hrcomments           AS HrComments,
        hr.improvementarea      AS ImprovementArea,
        hr.feedback             AS Feedback,
        hr.status               AS Status,
        hr.createdat            AS CreatedAt,
        hr.updatedat            AS UpdatedAt
    FROM performance.hrappraisals hr  
    INNER JOIN employee.employees emp ON hr.employeeid = emp.employeeid
    INNER JOIN employee.employees hremp ON hr.hrid = hremp.employeeid
    INNER JOIN performance.appraisal_cycle ac ON hr.cycleid = ac.cycleid
    WHERE hr.organizationid = @OrganizationId

    -- 🔹 Cycle filter
    AND (@CycleId IS NULL OR hr.cycleid = @CycleId)

    -- 🔹 Rating filter
    AND (@Rating IS NULL OR hr.finalrating = @Rating)

    -- 🔹 Search filter
    AND (
        @SearchQuery IS NULL OR
        LOWER(CONCAT(emp.firstname, ' ', emp.lastname)) LIKE LOWER(@SearchQuery) OR
        LOWER(CONCAT(hremp.firstname, ' ', hremp.lastname)) LIKE LOWER(@SearchQuery) OR
        LOWER(hr.feedback) LIKE LOWER(@SearchQuery) OR
        LOWER(hr.hrcomments) LIKE LOWER(@SearchQuery)
    )

    ORDER BY hr.createdat DESC;
    ";

        var result = await QueryAsync<HrReviewDto>(sql, new
        {
            OrganizationId = organizationId,
            CycleId = search.CycleId,
            Rating = search.Rating,
            SearchQuery = string.IsNullOrEmpty(search.SearchQuery)
                ? null
                : $"%{search.SearchQuery}%"
        });

        return result.ToList();
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
    SELECT 
        cycleid,
        cyclename,
        cycletype,
        description,
        startdate,
        enddate,
        reviewstartdate,
        reviewenddate,
        status,
        isselfassessmentenable,
        managerreview,          -- ✅ FIXED
        isappraisalenable,
        createdby,
        createdat,
        updatedat,
        organizationid
    FROM performance.appraisal_cycle
    WHERE cycleid = @cycleId";

        var result = await QuerySingleOrDefaultAsync<AppraisalCycleDto>(
            sql,
            new { cycleId }
        );

        return result;
    }

    //old



    public async Task<AppraisalCycleDto> UpdateAppraisalCycleAsync(Guid cycleid, UpdateAppraisalCycleRequest request)
    {
        var sql = @"
    UPDATE performance.appraisal_cycle   -- ✅ FIXED HERE
    SET 
        cyclename = @CycleName,
        cycletype = @CycleType,
        description = @Description,
        startdate = @StartDate,
        enddate = @EndDate,
        reviewstartdate = @ReviewStartDate,
        reviewenddate = @ReviewEndDate,
        status = @Status,
        isselfassessmentenable = @IsSelfAssessmentEnable,
        managerreview = @ManagerReview,
        isappraisalenable = @IsAppraisalEnable,
        updatedat = NOW()
    WHERE cycleid = @CycleId
    RETURNING 
        cycleid,
        cyclename,
        cycletype,
        description,
        startdate,
        enddate,
        reviewstartdate,
        reviewenddate,
        status,
        isselfassessmentenable,
        managerreview,
        isappraisalenable,
        (SELECT COUNT(*) FROM performance.employeeappraisals WHERE cycleid = @CycleId) AS total_appraisals,
        (SELECT COUNT(*) FROM performance.employeeappraisals WHERE cycleid = @CycleId AND status = 'completed') AS completed_appraisals;
    ";

        var parameters = new
        {
            CycleId = cycleid,
            CycleName = request.CycleName,
            CycleType = request.CycleType,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ReviewStartDate = request.ReviewStartDate,
            ReviewEndDate = request.ReviewEndDate,
            Status = request.Status,
            IsSelfAssessmentEnable = request.IsSelfAssessmentEnable,
            ManagerReview = request.ManagerReview,
            IsAppraisalEnable = request.IsAppraisalEnable
        };

        var result = await QuerySingleOrDefaultAsync<AppraisalCycleDto>(sql, parameters);

        if (result == null)
            throw new KeyNotFoundException($"Appraisal cycle with ID {cycleid} not found.");

        return result;
    }


    public async Task<bool> DeleteAppraisalCycleAsync(Guid cycleId)
    {
        // Check if cycle has any appraisals
        var hasAppraisals = await QuerySingleOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM performance.kra WHERE cycleid = @CycleId",
            new { CycleId = cycleId });

        if (hasAppraisals > 0)
        {
            throw new InvalidOperationException("Cannot delete appraisal cycle with existing appraisals. Please archive it instead.");
        }

        var sql = @"DELETE FROM performance.appraisal_cycle WHERE cycleid = @CycleId";
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
        var sql = @"DELETE FROM performance.kra WHERE kraid = @KraId";
        var affected = await ExecuteAsync(sql, new { KraId = kraId });
        return affected > 0;
    }

    // Appraisal Consolidation
    //public async Task<EmployeeAppraisalDto> ConsolidateAppraisalAsync(ConsolidateAppraisalRequest request)
    //{
    //    // Get self-assessment data
    //    var selfAssessments = await GetSelfAssessmentsAsync(request.EmployeeId, request.CycleId);
    //    var selfAssessmentList = selfAssessments.ToList();

    //    // Get manager review data
    //    var managerReviewSql = @"
    //        SELECT reviewid, managerid, employeeid, cycleid, overallrating, feedback, 
    //               improvementareas, developmentplan, status, createdat, updatedat
    //        FROM performance.managerreviews
    //        WHERE employeeid = @EmployeeId AND cycleid = @CycleId
    //        ORDER BY createdat DESC
    //        LIMIT 1";

    //    var managerReview = await QuerySingleOrDefaultAsync<ManagerReviewDto>(managerReviewSql, new
    //    {
    //        EmployeeId = request.EmployeeId,
    //        CycleId = request.CycleId
    //    });

    //    if (managerReview == null)
    //    {
    //        throw new InvalidOperationException("Manager review not found. Cannot consolidate appraisal.");
    //    }

    //    // Calculate KRA ratings from self-assessment
    //    var kraRatings = new Dictionary<string, object>();
    //    var kraRatingsList = selfAssessmentList.Where(s => s.KraId.HasValue).ToList();
    //    foreach (var sa in kraRatingsList)
    //    {
    //        kraRatings[sa.KraId!.Value.ToString()] = sa.Rating;
    //    }

    //    // Calculate Skill ratings from self-assessment
    //    var skillRatings = new Dictionary<string, object>();
    //    var skillRatingsList = selfAssessmentList.Where(s => s.SkillId.HasValue).ToList();
    //    foreach (var sa in skillRatingsList)
    //    {
    //        skillRatings[sa.SkillId!.Value.ToString()] = sa.Rating;
    //    }

    //    // Calculate overall rating with weightage: KRAs 60%, Skills 30%, Manager feedback 10%
    //    decimal overallRating = 0;
    //    decimal kraAverage = 0;
    //    decimal skillAverage = 0;

    //    if (kraRatingsList.Any())
    //    {
    //        kraAverage = (decimal)kraRatingsList.Average(s => (double)s.Rating);
    //    }

    //    if (skillRatingsList.Any())
    //    {
    //        skillAverage = (decimal)skillRatingsList.Average(s => (double)s.Rating);
    //    }

    //    overallRating = (kraAverage * 0.6m) + (skillAverage * 0.3m) + (managerReview.OverallRating * 0.1m);

    //    // Create consolidated appraisal
    //    var appraisal = new EmployeeAppraisal
    //    {
    //        AppraisalId = Guid.NewGuid(),
    //        CycleId = request.CycleId,
    //        EmployeeId = request.EmployeeId,
    //        ReviewerId = request.ReviewerId,
    //        ReviewType = "consolidated",
    //        OverallRating = overallRating,
    //        KraRatings = kraRatings,
    //        SkillRatings = skillRatings,
    //        Feedback = managerReview.Feedback,
    //        ImprovementAreas = managerReview.ImprovementAreas,
    //        DevelopmentPlan = managerReview.DevelopmentPlan,
    //        Status = "completed",
    //        SubmittedAt = DateTime.UtcNow,
    //        ReviewedAt = DateTime.UtcNow
    //    };

    //    var createdAppraisal = await CreateEmployeeAppraisalAsync(appraisal);

    //    // Map to DTO
    //    var employeeSql = @"
    //        SELECT CONCAT(firstname, ' ', lastname) as employee_name
    //        FROM employee.employees
    //        WHERE employeeid = @EmployeeId";

    //    var employeeName = await QuerySingleOrDefaultAsync<string>(employeeSql, new { EmployeeId = request.EmployeeId });

    //    var cycleSql = @"
    //        SELECT cyclename
    //        FROM performance.appraisalcycles
    //        WHERE cycleid = @CycleId";

    //    var cycleName = await QuerySingleOrDefaultAsync<string>(cycleSql, new { CycleId = request.CycleId });

    //    return new EmployeeAppraisalDto
    //    {
    //        AppraisalId = createdAppraisal.AppraisalId,
    //        EmployeeId = createdAppraisal.EmployeeId,
    //        CycleId = createdAppraisal.CycleId,
    //        EmployeeName = employeeName ?? string.Empty,
    //        CycleName = cycleName ?? string.Empty,
    //        ReviewerName = null,
    //        ReviewType = createdAppraisal.ReviewType,
    //        OverallRating = createdAppraisal.OverallRating,
    //        KraRatings = createdAppraisal.KraRatings,
    //        SkillRatings = createdAppraisal.SkillRatings,
    //        Feedback = createdAppraisal.Feedback,
    //        ImprovementAreas = createdAppraisal.ImprovementAreas,
    //        Status = createdAppraisal.Status,
    //        SubmittedAt = createdAppraisal.SubmittedAt,
    //        ReviewedAt = createdAppraisal.ReviewedAt
    //    };
    //}

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
