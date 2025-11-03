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
            var sql = @"
            INSERT INTO performance.employeeappraisals (
                cycleid, employeeid, reviewerid, reviewtype, overallrating,
                kraratings, skillratings, goalsachieved, feedback, improvementareas,
                developmentplan, status
            )
            VALUES (
                @cycleId, @employeeId, @reviewerId, @reviewType, @overallRating,
                @kraRatings::jsonb, @skillRatings::jsonb, @goalsAchieved::jsonb,
                @feedback, @improvementAreas, @developmentPlan, @status
            )
            RETURNING appraisalid";

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
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsAsync(Guid cycleId, Guid? userId = null)
    {
        var employeeId = await GetEmpId(userId);


        var sql = @"
            SELECT ea.appraisalid, ea.employeeid, ea.reviewerid, ea.reviewtype,ea.cycleid,
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
WHERE organizationid = @organizationId AND isactive = true
ORDER BY category, skillname";

            var results = await QueryAsync<SkillSetDto>(sql, new { organizationId });
            return results.ToList();
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<EmployeeSkillDto?> CreateEmployeeSkillAsync(CreateEmployeeSkillRequest request)
    {
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
            using var connection = CreateConnection();
            var result = await connection.QuerySingleOrDefaultAsync<EmployeeSkillDto>(sql, request);
            return result;
        }
        catch (PostgresException ex)
        {
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
WHERE es.employeeid = @employeeId
ORDER BY s.category, s.skillname;
";

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


    // Overall_Ratting caulation function
    public async Task<int> UpdateEmployeeStatsAsync(Guid employeeId, int achievedgoals, int totalgoals)
    {
        // Calculate overall rating
        double overallRating = totalgoals > 0
            ? Math.Round(((double)achievedgoals / totalgoals) * 5, 2)
            : 0;

        //  Update overall rating in employee appraisal table

        var achievedGoals = new
        {
            completed = achievedgoals,
            total = totalgoals
        };


        var updateRatingSql = @"
        UPDATE performance.employeeappraisals
        SET overallrating = @OverallRating,
            goalsachieved = @AchievedGoals ::jsonb,
            updatedat = CURRENT_TIMESTAMP
        WHERE employeeid = @EmployeeId;
    ";

        var result = await ExecuteAsync(updateRatingSql, new
        {
            EmployeeId = employeeId,
            OverallRating = overallRating,
            AchievedGoals = Newtonsoft.Json.JsonConvert.SerializeObject(achievedGoals) // ? this must be JSON
        });

        return result;

    }


    //GET Total Number of Goals given  an employee
    public async Task<int> GetGoalCountAsync(Guid employeeId)
    {
        var sql = @"
        SELECT COUNT(*) 
        FROM performance.employeegoals 
        WHERE employeeid = @EmployeeId;
    ";

        // use your repo s built-in query helper
        var count = await QuerySingleOrDefaultAsync<int>(sql, new { EmployeeId = employeeId });
        return count;
    }


    //Count achieve Goals
    public async Task<int> GetAchievedGoalsAsync(Guid employeeId)
    {
        var sql = @"
        SELECT COUNT(*) 
        FROM performance.employeegoals 
        WHERE employeeid = @EmployeeId AND status = 'completed';
    ";

        var count = await QuerySingleOrDefaultAsync<int>(sql, new { EmployeeId = employeeId });
        return count;
    }



    // Create Employee Goal
    public async Task<EmployeeGoalDto> CreateEmployeeGoalAsync(CreateEmployeeGoalRequest request)
    {
        var sql = @"
        INSERT INTO performance.employeegoals
        ( employeeid, title, description, startdate, enddate, status, createdat, updatedat)
        VALUES
        (@EmployeeId, @Title, @Description, @StartDate, @EndDate, @Status, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
        RETURNING goalid, employeeid,  title, description, startdate, enddate, status, createdat, updatedat;
    ";


        var result = await QuerySingleOrDefaultAsync<EmployeeGoalDto>(sql, request);

        var totalgoals = await GetGoalCountAsync(request.EmployeeId);
        var updateSql = @"UPDATE performance.employeegoals
                      SET totalgoals = @TotalGoals
                      WHERE employeeid = @EmployeeId";
        await ExecuteAsync(updateSql, new { TotalGoals = totalgoals, EmployeeId = request.EmployeeId });

        var Achievegoal = await GetAchievedGoalsAsync(request.EmployeeId);
        var updateachieveGoals = @"update performance.employeegoals
                                  SET achievedgoals= @Achievegoals
                                  WHERE employeeid=@EmployeeId";

        await ExecuteAsync(updateachieveGoals, new { Achievegoals = Achievegoal, EmployeeId = request.EmployeeId });

        //Updating OverallRating after creting a employee

        await UpdateEmployeeStatsAsync(request.EmployeeId, Achievegoal, totalgoals);


        return result!;
    }


    // Completed Goal Function
    public async Task<bool> GoalAsCompletedAsync(Guid goalId)
    {
        // Get employeeId for the given goalId
        var getEmployeeSql = @"SELECT employeeid FROM performance.employeegoals WHERE goalid = @GoalId";
        var employeeId = await QuerySingleOrDefaultAsync<Guid>(getEmployeeSql, new { GoalId = goalId });

        if (employeeId == Guid.Empty)
            return false;

        //Mark the goal as completed
        var updateGoalSql = @"
        UPDATE performance.employeegoals
        SET status = 'completed',
           createdat = CURRENT_TIMESTAMP
        WHERE goalid = @GoalId;
    ";
        await ExecuteAsync(updateGoalSql, new { GoalId = goalId });

        // Update achieved goals count for this employee
        var countAchievedSql = @"
        SELECT COUNT(*) 
        FROM performance.employeegoals
        WHERE employeeid = @EmployeeId
          AND status = 'completed';
    ";
        var achievedGoals = await QuerySingleOrDefaultAsync<int>(countAchievedSql, new { EmployeeId = employeeId });

        var updateAchievedSql = @"
        UPDATE performance.employeegoals
        SET achievedgoals = @AchievedGoals
        WHERE employeeid = @EmployeeId;
    ";
        await ExecuteAsync(updateAchievedSql, new { AchievedGoals = achievedGoals, EmployeeId = employeeId });


        //Step 1: Count total and achieved goals for the employee

        var totalgoals = await GetGoalCountAsync(employeeId);


        await UpdateEmployeeStatsAsync(employeeId, achievedGoals, totalgoals);


        return true;
    }



    // Get Employee Goal By Id
    public async Task<List<EmployeeGoalDto>> GetEmployeeGoalsAsync(Guid userId)
    {

        var employeeId = await GetEmpId(userId);


        var sql = @"
        SELECT   g.goalid, 
            g.employeeid, 
            CONCAT(e.firstname, ' ', e.lastname) as employee_name,
            g.title, 
            g.description, 
            g.startdate, 
            g.enddate, 
            g.status, 
            g.createdat, 
            g.updatedat
        FROM performance.employeegoals g
        INNER JOIN employee.employees e ON g.employeeid = e.employeeid
        WHERE g.employeeid = @EmployeeId
        ORDER BY g.createdat DESC;
    ";

        var result = await QueryAsync<EmployeeGoalDto>(sql, new { EmployeeId = employeeId });
        return result.ToList();
    }

    //Get all Employee Goals
    public async Task<List<EmployeeGoalDto>> GetAllEmployeeGoalsAsync()
    {
        var sql = @"
        SELECT  
            g.goalid, 
            g.employeeid, 
            CONCAT(e.firstname, ' ', e.lastname) as employee_name,
            g.title, 
            g.description, 
            g.startdate, 
            g.enddate, 
            g.status, 
            g.createdat, 
            g.updatedat
        FROM performance.employeegoals g
        INNER JOIN employee.employees e ON g.employeeid = e.employeeid
        
        ORDER BY createdat DESC;
    ";

        var result = await QueryAsync<EmployeeGoalDto>(sql);
        return result.ToList();
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
        var sql = @"
        INSERT INTO performance.SelfAssessments
        (employeeid, cycleid, kraid, skillid, rating, comments, evidenceurls, status, createdat, updatedat)
        VALUES
        (@EmployeeId, @CycleId, @KraId, @SkillId, @Rating, @Comments, @EvidenceUrls::jsonb, @Status, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
        RETURNING selfassessmentid, employeeid, cycleid, kraid, skillid, rating, comments, evidenceurls, status, createdat, updatedat;
    ";

        return await QuerySingleOrDefaultAsync<SelfAssessmentDto>(sql, request);
    }

    public async Task<IEnumerable<SelfAssessmentDto>> GetSelfAssessmentsAsync(Guid employeeId, Guid cycleId)
    {
        var sql = @"SELECT * FROM performance.SelfAssessments WHERE employeeid = @EmployeeId AND cycleid = @CycleId";
        return await QueryAsync<SelfAssessmentDto>(sql, new { EmployeeId = employeeId, CycleId = cycleId });
    }

    public async Task UpdateSelfAssessmentStatusAsync(Guid employeeId, Guid cycleId, string status)
    {
        var sql = @"UPDATE performance.SelfAssessments
                SET status = @Status, updatedat = CURRENT_TIMESTAMP, submittedat = CURRENT_TIMESTAMP
                WHERE employeeid = @EmployeeId AND cycleid = @CycleId";
        await ExecuteAsync(sql, new { EmployeeId = employeeId, CycleId = cycleId, Status = status });
    }

    public async Task<IEnumerable<SelfAssessmentDto>> GetTeamSelfAssessmentsAsync(Guid managerId, Guid cycleId)
    {
        var sql = @"
        SELECT sa.*
        FROM performance.SelfAssessments sa
        INNER JOIN employees e ON sa.employeeid = e.employeeid
        WHERE e.managerid = @ManagerId AND sa.cycleid = @CycleId;
    ";
        return await QueryAsync<SelfAssessmentDto>(sql, new { ManagerId = managerId, CycleId = cycleId });
    }

    public async Task<ManagerReviewDto> SubmitManagerReviewAsync(ManagerReviewRequest request)
    {
        var sql = @"
        INSERT INTO performance.ManagerReviews
        (managerid, employeeid, cycleid, overallrating, feedback, improvementareas, developmentplan, createdat, updatedat)
        VALUES
        (@ManagerId, @EmployeeId, @CycleId, @OverallRating, @Feedback, @ImprovementAreas, @DevelopmentPlan, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
        RETURNING reviewid, managerid, employeeid, cycleid, overallrating, feedback, improvementareas, developmentplan, 'submitted' as status, createdat, updatedat;
    ";

        return await QuerySingleOrDefaultAsync<ManagerReviewDto>(sql, request);
    }
}
