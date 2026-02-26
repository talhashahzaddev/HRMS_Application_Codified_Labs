using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class JobsRepository : BaseRepository, IJobsRepository
{
    public JobsRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    // ==================== StageMaster ====================
    public async Task<IEnumerable<StageMasterDto>> GetAllStageMastersAsync(Guid organizationId)
    {
        var sql = @"
            SELECT stageid AS StageId, organizationid AS OrganizationId, stagename AS StageName,
                   stageorder AS StageOrder, isinterviewstage AS IsInterviewStage, createdat AS CreatedAt
            FROM jobs.stagemaster
            WHERE organizationid = @organizationId
            ORDER BY stageorder NULLS LAST, stagename";
        return await QueryAsync<StageMasterDto>(sql, new { organizationId });
    }

    public async Task<StageMasterDto?> GetStageMasterByIdAsync(Guid stageId)
    {
        var sql = @"
            SELECT stageid AS StageId, organizationid AS OrganizationId, stagename AS StageName,
                   stageorder AS StageOrder, isinterviewstage AS IsInterviewStage, createdat AS CreatedAt
            FROM jobs.stagemaster
            WHERE stageid = @stageId";
        return await QuerySingleOrDefaultAsync<StageMasterDto>(sql, new { stageId });
    }

    public async Task<Guid> CreateStageMasterAsync(Guid organizationId, string stageName, int? stageOrder, bool isInterviewStage)
    {
        var sql = @"
            INSERT INTO jobs.stagemaster (organizationid, stagename, stageorder, isinterviewstage)
            VALUES (@organizationId, @stageName, @stageOrder, @isInterviewStage)
            RETURNING stageid";
        return await ExecuteScalarAsync<Guid>(sql, new { organizationId, stageName, stageOrder, isInterviewStage });
    }

    public async Task<bool> UpdateStageMasterAsync(Guid stageId, string stageName, int? stageOrder, bool isInterviewStage)
    {
        var sql = @"
            UPDATE jobs.stagemaster
            SET stagename = @stageName, stageorder = @stageOrder, isinterviewstage = @isInterviewStage
            WHERE stageid = @stageId";
        var rows = await ExecuteAsync(sql, new { stageId, stageName, stageOrder, isInterviewStage });
        return rows > 0;
    }

    public async Task<bool> DeleteStageMasterAsync(Guid stageId)
    {
        var sql = "DELETE FROM jobs.stagemaster WHERE stageid = @stageId";
        var rows = await ExecuteAsync(sql, new { stageId });
        return rows > 0;
    }

    // ==================== JobOpenings ====================
    public async Task<IEnumerable<JobOpeningDto>> GetAllJobOpeningsAsync(Guid organizationId)
    {
        var sql = @"
            SELECT j.jobid AS JobId, j.organizationid AS OrganizationId, j.jobrolename AS JobRoleName,
                   j.departmentid AS DepartmentId, d.departmentname AS DepartmentName,
                   j.experiencemin AS ExperienceMin, j.experiencemax AS ExperienceMax,
                   j.ctcmin AS CtcMin, j.ctcmax AS CtcMax, j.currency AS Currency,
                   j.vacancies AS Vacancies, j.location AS Location, j.workmode AS WorkMode,
                   j.employmenttype AS EmploymentType, j.jobintroduction AS JobIntroduction,
                   j.responsibilities AS Responsibilities, j.skillset AS Skillset,
                   j.lastdate AS LastDate, j.posteddate AS PostedDate, j.postedas AS PostedAs,
                   j.externallink AS ExternalLink, j.status AS Status, j.createdby AS CreatedBy,
                   CONCAT(emp.firstname, ' ', emp.lastname) AS CreatedByName,
                   j.createdat AS CreatedAt, j.updatedat AS UpdatedAt
            FROM jobs.jobopenings j
            LEFT JOIN employee.departments d ON j.departmentid = d.departmentid
            LEFT JOIN employee.employees emp ON j.createdby = emp.employeeid
            WHERE j.organizationid = @organizationId
            ORDER BY j.posteddate DESC NULLS LAST, j.createdat DESC";
        return await QueryAsync<JobOpeningDto>(sql, new { organizationId });
    }

    public async Task<PagedResult<JobOpeningDto>> GetJobOpeningsPagedAsync(Guid organizationId, string? search, string? status, DateTime? lastDateFrom, DateTime? lastDateTo, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var offset = (pageNumber - 1) * pageSize;
        var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
        // Use sentinel dates when null so Npgsql can determine parameter type (it cannot infer from null)
        var fromDate = lastDateFrom ?? new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = lastDateTo ?? new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var countSql = @"
            SELECT COUNT(*)
            FROM jobs.jobopenings j
            LEFT JOIN employee.departments d ON j.departmentid = d.departmentid
            WHERE j.organizationid = @organizationId
              AND (@searchPattern IS NULL OR (
                  j.jobrolename ILIKE @searchPattern OR
                  j.location ILIKE @searchPattern OR
                  d.departmentname ILIKE @searchPattern OR
                  j.workmode ILIKE @searchPattern OR
                  j.employmenttype ILIKE @searchPattern OR
                  j.jobintroduction ILIKE @searchPattern OR
                  j.skillset ILIKE @searchPattern))
              AND (@status IS NULL OR j.status = @status)
              AND ((CAST(@fromDate AS date) = '1900-01-01'::date) OR (j.lastdate >= @fromDate))
              AND ((CAST(@toDate AS date) = '9999-12-31'::date) OR (j.lastdate <= @toDate))";
        var totalCount = await ExecuteScalarAsync<int>(countSql, new { organizationId, searchPattern, status, fromDate, toDate });

        var dataSql = @"
            SELECT j.jobid AS JobId, j.organizationid AS OrganizationId, j.jobrolename AS JobRoleName,
                   j.departmentid AS DepartmentId, d.departmentname AS DepartmentName,
                   j.experiencemin AS ExperienceMin, j.experiencemax AS ExperienceMax,
                   j.ctcmin AS CtcMin, j.ctcmax AS CtcMax, j.currency AS Currency,
                   j.vacancies AS Vacancies, j.location AS Location, j.workmode AS WorkMode,
                   j.employmenttype AS EmploymentType, j.jobintroduction AS JobIntroduction,
                   j.responsibilities AS Responsibilities, j.skillset AS Skillset,
                   j.lastdate AS LastDate, j.posteddate AS PostedDate, j.postedas AS PostedAs,
                   j.externallink AS ExternalLink, j.status AS Status, j.createdby AS CreatedBy,
                   CONCAT(emp.firstname, ' ', emp.lastname) AS CreatedByName,
                   j.createdat AS CreatedAt, j.updatedat AS UpdatedAt
            FROM jobs.jobopenings j
            LEFT JOIN employee.departments d ON j.departmentid = d.departmentid
            LEFT JOIN employee.employees emp ON j.createdby = emp.employeeid
            WHERE j.organizationid = @organizationId
              AND (@searchPattern IS NULL OR (
                  j.jobrolename ILIKE @searchPattern OR
                  j.location ILIKE @searchPattern OR
                  d.departmentname ILIKE @searchPattern OR
                  j.workmode ILIKE @searchPattern OR
                  j.employmenttype ILIKE @searchPattern OR
                  j.jobintroduction ILIKE @searchPattern OR
                  j.skillset ILIKE @searchPattern))
              AND (@status IS NULL OR j.status = @status)
              AND ((CAST(@fromDate AS date) = '1900-01-01'::date) OR (j.lastdate >= @fromDate))
              AND ((CAST(@toDate AS date) = '9999-12-31'::date) OR (j.lastdate <= @toDate))
            ORDER BY j.posteddate DESC NULLS LAST, j.createdat DESC
            LIMIT @pageSize OFFSET @offset";
        var data = await QueryAsync<JobOpeningDto>(dataSql, new { organizationId, searchPattern, status, fromDate, toDate, pageSize, offset });
        return new PagedResult<JobOpeningDto>
        {
            Data = data?.ToList() ?? new List<JobOpeningDto>(),
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<JobOpeningDto?> GetJobOpeningByIdAsync(Guid jobId)
    {
        var sql = @"
            SELECT j.jobid AS JobId, j.organizationid AS OrganizationId, j.jobrolename AS JobRoleName,
                   j.departmentid AS DepartmentId, d.departmentname AS DepartmentName,
                   j.experiencemin AS ExperienceMin, j.experiencemax AS ExperienceMax,
                   j.ctcmin AS CtcMin, j.ctcmax AS CtcMax, j.currency AS Currency,
                   j.vacancies AS Vacancies, j.location AS Location, j.workmode AS WorkMode,
                   j.employmenttype AS EmploymentType, j.jobintroduction AS JobIntroduction,
                   j.responsibilities AS Responsibilities, j.skillset AS Skillset,
                   j.lastdate AS LastDate, j.posteddate AS PostedDate, j.postedas AS PostedAs,
                   j.externallink AS ExternalLink, j.status AS Status, j.createdby AS CreatedBy,
                   CONCAT(emp.firstname, ' ', emp.lastname) AS CreatedByName,
                   j.createdat AS CreatedAt, j.updatedat AS UpdatedAt
            FROM jobs.jobopenings j
            LEFT JOIN employee.departments d ON j.departmentid = d.departmentid
            LEFT JOIN employee.employees emp ON j.createdby = emp.employeeid
            WHERE j.jobid = @jobId";
        return await QuerySingleOrDefaultAsync<JobOpeningDto>(sql, new { jobId });
    }

    public async Task<Guid> CreateJobOpeningAsync(JobOpening entity)
    {
        var sql = @"
            INSERT INTO jobs.jobopenings (organizationid, jobrolename, departmentid, experiencemin, experiencemax,
                ctcmin, ctcmax, currency, vacancies, location, workmode, employmenttype, jobintroduction,
                responsibilities, skillset, lastdate, postedas, externallink, status, createdby)
            VALUES (@OrganizationId, @JobRoleName, @DepartmentId, @ExperienceMin, @ExperienceMax,
                @CtcMin, @CtcMax, @Currency, @Vacancies, @Location, @WorkMode, @EmploymentType, @JobIntroduction,
                @Responsibilities, @Skillset, @LastDate, @PostedAs, @ExternalLink, @Status, @CreatedBy)
            RETURNING jobid";
        return await ExecuteScalarAsync<Guid>(sql, new
        {
            entity.OrganizationId,
            entity.JobRoleName,
            entity.DepartmentId,
            entity.ExperienceMin,
            entity.ExperienceMax,
            entity.CtcMin,
            entity.CtcMax,
            entity.Currency,
            entity.Vacancies,
            entity.Location,
            entity.WorkMode,
            entity.EmploymentType,
            entity.JobIntroduction,
            entity.Responsibilities,
            entity.Skillset,
            entity.LastDate,
            entity.PostedAs,
            entity.ExternalLink,
            entity.Status,
            entity.CreatedBy
        });
    }

    public async Task<bool> UpdateJobOpeningAsync(JobOpening entity)
    {
        var sql = @"
            UPDATE jobs.jobopenings
            SET jobrolename = @JobRoleName, departmentid = @DepartmentId, experiencemin = @ExperienceMin,
                experiencemax = @ExperienceMax, ctcmin = @CtcMin, ctcmax = @CtcMax, currency = @Currency,
                vacancies = @Vacancies, location = @Location, workmode = @WorkMode, employmenttype = @EmploymentType,
                jobintroduction = @JobIntroduction, responsibilities = @Responsibilities, skillset = @Skillset,
                lastdate = @LastDate, postedas = @PostedAs, externallink = @ExternalLink, status = @Status,
                updatedat = CURRENT_TIMESTAMP
            WHERE jobid = @JobId";
        var rows = await ExecuteAsync(sql, new
        {
            entity.JobId,
            entity.JobRoleName,
            entity.DepartmentId,
            entity.ExperienceMin,
            entity.ExperienceMax,
            entity.CtcMin,
            entity.CtcMax,
            entity.Currency,
            entity.Vacancies,
            entity.Location,
            entity.WorkMode,
            entity.EmploymentType,
            entity.JobIntroduction,
            entity.Responsibilities,
            entity.Skillset,
            entity.LastDate,
            entity.PostedAs,
            entity.ExternalLink,
            entity.Status
        });
        return rows > 0;
    }

    public async Task<bool> DeleteJobOpeningAsync(Guid jobId)
    {
        var sql = "DELETE FROM jobs.jobopenings WHERE jobid = @jobId";
        var rows = await ExecuteAsync(sql, new { jobId });
        return rows > 0;
    }

    // ==================== JobApplications ====================
    private const string JobApplicationSelectSql = @"
            SELECT a.jobapplyid AS JobApplyId, a.jobid AS JobId, j.jobrolename AS JobRoleName,
                   a.organizationid AS OrganizationId, a.employeeid AS EmployeeId,
                   CONCAT(emp.firstname, ' ', emp.lastname) AS EmployeeName,
                   a.candidateid AS CandidateId, a.candidatename AS CandidateName, a.candidateemail AS CandidateEmail,
                   a.linkedinurl AS LinkedInUrl, a.resumeurl AS ResumeUrl, a.phone AS Phone,
                   a.coverletter AS CoverLetter, a.currentstageid AS CurrentStageId, s.stagename AS CurrentStageName,
                   a.applicationsource AS ApplicationSource, a.status AS Status,
                   a.createddate AS CreatedDate, a.updateddate AS UpdatedDate
            FROM jobs.jobapplications a
            LEFT JOIN jobs.jobopenings j ON a.jobid = j.jobid
            LEFT JOIN employee.employees emp ON a.employeeid = emp.employeeid
            LEFT JOIN jobs.stagemaster s ON a.currentstageid = s.stageid";

    public async Task<IEnumerable<JobApplicationDto>> GetAllJobApplicationsAsync(Guid organizationId)
    {
        var sql = JobApplicationSelectSql + @"
            WHERE a.organizationid = @organizationId
            ORDER BY a.createddate DESC NULLS LAST";
        return await QueryAsync<JobApplicationDto>(sql, new { organizationId });
    }

    public async Task<PagedResult<JobApplicationDto>> GetJobApplicationsPagedAsync(Guid organizationId, string? search, DateTime? applyDateFrom, DateTime? applyDateTo, Guid? stageId, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var offset = (pageNumber - 1) * pageSize;
        var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
        var fromDate = applyDateFrom ?? new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = applyDateTo ?? new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var countSql = @"
            SELECT COUNT(*)
            FROM jobs.jobapplications a
            LEFT JOIN jobs.jobopenings j ON a.jobid = j.jobid
            WHERE a.organizationid = @organizationId
              AND (@searchPattern IS NULL OR (
                  a.candidatename ILIKE @searchPattern OR
                  a.candidateemail ILIKE @searchPattern OR
                  j.jobrolename ILIKE @searchPattern))
              AND ((CAST(@fromDate AS date) = '1900-01-01'::date) OR (a.createddate >= @fromDate))
              AND ((CAST(@toDate AS date) = '9999-12-31'::date) OR (a.createddate <= @toDate))
              AND (@stageId IS NULL OR a.currentstageid = @stageId)";
        var totalCount = await ExecuteScalarAsync<int>(countSql, new { organizationId, searchPattern, fromDate, toDate, stageId });

        var dataSql = JobApplicationSelectSql + @"
            WHERE a.organizationid = @organizationId
              AND (@searchPattern IS NULL OR (
                  a.candidatename ILIKE @searchPattern OR
                  a.candidateemail ILIKE @searchPattern OR
                  j.jobrolename ILIKE @searchPattern))
              AND ((CAST(@fromDate AS date) = '1900-01-01'::date) OR (a.createddate >= @fromDate))
              AND ((CAST(@toDate AS date) = '9999-12-31'::date) OR (a.createddate <= @toDate))
              AND (@stageId IS NULL OR a.currentstageid = @stageId)
            ORDER BY a.createddate DESC NULLS LAST
            LIMIT @pageSize OFFSET @offset";
        var data = await QueryAsync<JobApplicationDto>(dataSql, new { organizationId, searchPattern, fromDate, toDate, stageId, pageSize, offset });
        return new PagedResult<JobApplicationDto>
        {
            Data = data?.ToList() ?? new List<JobApplicationDto>(),
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<JobApplicationDto>> GetJobApplicationsPostedByMePagedAsync(Guid employeeId, string? search, DateTime? applyDateFrom, DateTime? applyDateTo, Guid? stageId, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var offset = (pageNumber - 1) * pageSize;
        var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
        var fromDate = applyDateFrom ?? new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = applyDateTo ?? new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var countSql = @"
            SELECT COUNT(*)
            FROM jobs.jobapplications a
            LEFT JOIN jobs.jobopenings j ON a.jobid = j.jobid
            WHERE a.jobid IN (SELECT jobid FROM jobs.jobopenings WHERE createdby = @employeeId)
              AND (@searchPattern IS NULL OR (
                  a.candidatename ILIKE @searchPattern OR
                  a.candidateemail ILIKE @searchPattern OR
                  j.jobrolename ILIKE @searchPattern))
              AND ((CAST(@fromDate AS date) = '1900-01-01'::date) OR (a.createddate >= @fromDate))
              AND ((CAST(@toDate AS date) = '9999-12-31'::date) OR (a.createddate <= @toDate))
              AND (@stageId IS NULL OR a.currentstageid = @stageId)";
        var totalCount = await ExecuteScalarAsync<int>(countSql, new { employeeId, searchPattern, fromDate, toDate, stageId });

        var dataSql = JobApplicationSelectSql + @"
            WHERE a.jobid IN (SELECT jobid FROM jobs.jobopenings WHERE createdby = @employeeId)
              AND (@searchPattern IS NULL OR (
                  a.candidatename ILIKE @searchPattern OR
                  a.candidateemail ILIKE @searchPattern OR
                  j.jobrolename ILIKE @searchPattern))
              AND ((CAST(@fromDate AS date) = '1900-01-01'::date) OR (a.createddate >= @fromDate))
              AND ((CAST(@toDate AS date) = '9999-12-31'::date) OR (a.createddate <= @toDate))
              AND (@stageId IS NULL OR a.currentstageid = @stageId)
            ORDER BY a.createddate DESC NULLS LAST
            LIMIT @pageSize OFFSET @offset";
        var data = await QueryAsync<JobApplicationDto>(dataSql, new { employeeId, searchPattern, fromDate, toDate, stageId, pageSize, offset });
        return new PagedResult<JobApplicationDto>
        {
            Data = data?.ToList() ?? new List<JobApplicationDto>(),
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<JobApplicationDto?> GetJobApplicationByIdAsync(Guid jobApplyId)
    {
        var sql = JobApplicationSelectSql + @"
            WHERE a.jobapplyid = @jobApplyId";
        return await QuerySingleOrDefaultAsync<JobApplicationDto>(sql, new { jobApplyId });
    }

    public async Task<IEnumerable<JobApplicationDto>> GetJobApplicationsByEmployeeIdAsync(Guid employeeId)
    {
        var sql = JobApplicationSelectSql + @"
            WHERE a.employeeid = @employeeId
            ORDER BY a.createddate DESC NULLS LAST";
        return await QueryAsync<JobApplicationDto>(sql, new { employeeId });
    }

    public async Task<PagedResult<JobApplicationDto>> GetJobApplicationsByEmployeeIdPagedAsync(Guid employeeId, string? search, Guid? stageId, string? status, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var offset = (pageNumber - 1) * pageSize;
        var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";

        var countSql = @"
            SELECT COUNT(*)
            FROM jobs.jobapplications a
            LEFT JOIN jobs.jobopenings j ON a.jobid = j.jobid
            WHERE a.employeeid = @employeeId
              AND (a.isself = false OR a.isself IS NULL)
              AND (@searchPattern IS NULL OR j.jobrolename ILIKE @searchPattern)
              AND (@stageId IS NULL OR a.currentstageid = @stageId)
              AND (@status IS NULL OR a.status = @status)";
        var totalCount = await ExecuteScalarAsync<int>(countSql, new { employeeId, searchPattern, stageId, status });

        var dataSql = JobApplicationSelectSql + @"
            WHERE a.employeeid = @employeeId
              AND (a.isself = false OR a.isself IS NULL)
              AND (@searchPattern IS NULL OR j.jobrolename ILIKE @searchPattern)
              AND (@stageId IS NULL OR a.currentstageid = @stageId)
              AND (@status IS NULL OR a.status = @status)
            ORDER BY a.createddate DESC NULLS LAST
            LIMIT @pageSize OFFSET @offset";
        var data = await QueryAsync<JobApplicationDto>(dataSql, new { employeeId, searchPattern, stageId, status, pageSize, offset });
        return new PagedResult<JobApplicationDto>
        {
            Data = data?.ToList() ?? new List<JobApplicationDto>(),
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<JobApplicationDto>> GetMySelfJobApplicationsByEmployeeIdPagedAsync(Guid employeeId, string? search, Guid? stageId, string? status, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var offset = (pageNumber - 1) * pageSize;
        var searchPattern = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";

        var countSql = @"
            SELECT COUNT(*)
            FROM jobs.jobapplications a
            LEFT JOIN jobs.jobopenings j ON a.jobid = j.jobid
            WHERE a.employeeid = @employeeId
              AND a.isself = true
              AND (@searchPattern IS NULL OR j.jobrolename ILIKE @searchPattern)
              AND (@stageId IS NULL OR a.currentstageid = @stageId)
              AND (@status IS NULL OR a.status = @status)";
        var totalCount = await ExecuteScalarAsync<int>(countSql, new { employeeId, searchPattern, stageId, status });

        var dataSql = JobApplicationSelectSql + @"
            WHERE a.employeeid = @employeeId
              AND a.isself = true
              AND (@searchPattern IS NULL OR j.jobrolename ILIKE @searchPattern)
              AND (@stageId IS NULL OR a.currentstageid = @stageId)
              AND (@status IS NULL OR a.status = @status)
            ORDER BY a.createddate DESC NULLS LAST
            LIMIT @pageSize OFFSET @offset";
        var data = await QueryAsync<JobApplicationDto>(dataSql, new { employeeId, searchPattern, stageId, status, pageSize, offset });
        return new PagedResult<JobApplicationDto>
        {
            Data = data?.ToList() ?? new List<JobApplicationDto>(),
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<JobApplicationDto>> GetJobApplicationsByJobIdAsync(Guid jobId)
    {
        var sql = JobApplicationSelectSql + @"
            WHERE a.jobid = @jobId
            ORDER BY a.createddate DESC NULLS LAST";
        return await QueryAsync<JobApplicationDto>(sql, new { jobId });
    }

    public async Task<Guid> CreateJobApplicationAsync(JobApplication entity)
    {
        var sql = @"
            INSERT INTO jobs.jobapplications (jobid, organizationid, employeeid, candidatename, candidateemail,
                linkedinurl, resumeurl, phone, coverletter, currentstageid, applicationsource, status,isself)
            VALUES (@JobId, @OrganizationId, @EmployeeId, @CandidateName, @CandidateEmail,
                @LinkedInUrl, @ResumeUrl, @Phone, @CoverLetter, @CurrentStageId, @ApplicationSource, @Status,@IsSelf)
            RETURNING jobapplyid";
        return await ExecuteScalarAsync<Guid>(sql, new
        {
            entity.JobId,
            entity.OrganizationId,
            entity.EmployeeId,
            entity.CandidateName,
            entity.CandidateEmail,
            entity.LinkedInUrl,
            entity.ResumeUrl,
            entity.Phone,
            entity.CoverLetter,
            entity.CurrentStageId,
            entity.ApplicationSource,
            entity.Status,
            entity.IsSelf
        });
    }

    public async Task<bool> UpdateJobApplicationAsync(JobApplication entity)
    {
        var sql = @"
            UPDATE jobs.jobapplications
            SET candidatename = @CandidateName, candidateemail = @CandidateEmail, linkedinurl = @LinkedInUrl,
                resumeurl = @ResumeUrl, phone = @Phone, coverletter = @CoverLetter,
                currentstageid = @CurrentStageId, applicationsource = @ApplicationSource, status = @Status,
                updateddate = CURRENT_TIMESTAMP
            WHERE jobapplyid = @JobApplyId";
        var rows = await ExecuteAsync(sql, new
        {
            entity.JobApplyId,
            entity.CandidateName,
            entity.CandidateEmail,
            entity.LinkedInUrl,
            entity.ResumeUrl,
            entity.Phone,
            entity.CoverLetter,
            entity.CurrentStageId,
            entity.ApplicationSource,
            entity.Status
        });
        return rows > 0;
    }

    public async Task<bool> DeleteJobApplicationAsync(Guid jobApplyId)
    {
        var sql = "DELETE FROM jobs.jobapplications WHERE jobapplyid = @jobApplyId";
        var rows = await ExecuteAsync(sql, new { jobApplyId });
        return rows > 0;
    }

    // ==================== ApplicationStages ====================
    private const string ApplicationStageSelectSql = @"
            SELECT ast.applicationstageid AS ApplicationStageId, ast.jobapplyid AS JobApplyId, a.jobid AS JobId,
                   j.jobrolename AS JobRoleName, a.candidatename AS CandidateName,
                   ast.stageid AS StageId, s.stagename AS StageName, s.isinterviewstage AS IsInterviewStage,
                   ast.notes AS Notes, ast.interviewers AS InterviewersText, ast.type AS Type,
                   ast.updatedon AS UpdatedOn, ast.updatedby AS UpdatedBy,
                   CONCAT(upd.firstname, ' ', upd.lastname) AS UpdatedByName,
                   ast.interviewdate AS InterviewDate, ast.interviewplace AS InterviewPlace
            FROM jobs.applicationstages ast
            INNER JOIN jobs.jobapplications a ON ast.jobapplyid = a.jobapplyid
            LEFT JOIN jobs.jobopenings j ON a.jobid = j.jobid
            LEFT JOIN jobs.stagemaster s ON ast.stageid = s.stageid
            LEFT JOIN employee.employees upd ON ast.updatedby = upd.employeeid";

    private static List<Guid> ParseInterviewerIds(string? interviewersText)
    {
        if (string.IsNullOrWhiteSpace(interviewersText)) return new List<Guid>();
        return interviewersText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .Distinct()
            .ToList();
    }

    private static string ToInterviewersText(IEnumerable<Guid>? ids)
    {
        if (ids == null || !ids.Any()) return string.Empty;
        return string.Join(",", ids.Where(g => g != Guid.Empty).Distinct());
    }

    public async Task<IEnumerable<ApplicationStageDto>> GetAllApplicationStagesAsync(Guid organizationId)
    {
        var sql = ApplicationStageSelectSql + @"
            WHERE a.organizationid = @organizationId
            ORDER BY ast.updatedon DESC NULLS LAST";
        var rows = await QueryAsync<ApplicationStageRow>(sql, new { organizationId });
        return await BuildApplicationStageDtosAsync(rows);
    }

    public async Task<ApplicationStageDto?> GetApplicationStageByIdAsync(Guid applicationStageId)
    {
        var sql = ApplicationStageSelectSql + @"
            WHERE ast.applicationstageid = @applicationStageId";
        var row = await QuerySingleOrDefaultAsync<ApplicationStageRow>(sql, new { applicationStageId });
        if (row == null) return null;
        var list = await BuildApplicationStageDtosAsync(new[] { row });
        return list.FirstOrDefault();
    }

    public async Task<IEnumerable<ApplicationStageDto>> GetApplicationStagesByJobApplyIdAsync(Guid jobApplyId)
    {
        var sql = ApplicationStageSelectSql + @"
            WHERE ast.jobapplyid = @jobApplyId
            ORDER BY ast.updatedon DESC NULLS LAST";
        var rows = await QueryAsync<ApplicationStageRow>(sql, new { jobApplyId });
        return await BuildApplicationStageDtosAsync(rows);
    }

    private async Task<IEnumerable<ApplicationStageDto>> BuildApplicationStageDtosAsync(IEnumerable<ApplicationStageRow> rows)
    {
        var rowList = rows?.ToList() ?? new List<ApplicationStageRow>();
        if (!rowList.Any()) return rowList.Select(r => RowToDto(r, new List<InterviewerInfoDto>()));

        var allIds = rowList.SelectMany(r => ParseInterviewerIds(r.InterviewersText)).Distinct().ToList();
        var details = allIds.Any() ? (await GetInterviewerDetailsAsync(allIds)).ToList() : new List<InterviewerInfoDto>();
        var lookup = details.ToLookup(d => d.EmployeeId);

        return rowList.Select(r =>
        {
            var ids = ParseInterviewerIds(r.InterviewersText);
            var interviewers = ids.SelectMany(id => lookup[id]).ToList();
            return RowToDto(r, interviewers);
        });
    }

    private static ApplicationStageDto RowToDto(ApplicationStageRow r, List<InterviewerInfoDto> interviewers)
    {
        return new ApplicationStageDto
        {
            ApplicationStageId = r.ApplicationStageId,
            JobApplyId = r.JobApplyId,
            JobId = r.JobId,
            JobRoleName = r.JobRoleName,
            CandidateName = r.CandidateName,
            StageId = r.StageId,
            StageName = r.StageName,
            IsInterviewStage = r.IsInterviewStage,
            Notes = r.Notes,
            InterviewerIds = ParseInterviewerIds(r.InterviewersText),
            Interviewers = interviewers,
            Type = r.Type,
            UpdatedOn = r.UpdatedOn,
            UpdatedBy = r.UpdatedBy,
            UpdatedByName = r.UpdatedByName,
            InterviewDate = r.InterviewDate,
            InterviewPlace = r.InterviewPlace
        };
    }

    public async Task<IEnumerable<InterviewerInfoDto>> GetInterviewerDetailsAsync(IEnumerable<Guid> employeeIds)
    {
        var ids = employeeIds?.Where(g => g != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
        if (!ids.Any()) return new List<InterviewerInfoDto>();

        var sql = @"
            SELECT e.employeeid AS EmployeeId,
                   CONCAT(e.firstname, ' ', e.lastname) AS EmployeeName,
                   p.positiontitle AS PositionTitle
            FROM employee.employees e
            LEFT JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
            LEFT JOIN employee.positions p ON ed.positionid = p.positionid
            WHERE e.employeeid = ANY(@ids)";
        return await QueryAsync<InterviewerInfoDto>(sql, new { ids = ids.ToArray() });
    }

    public async Task<Guid> CreateApplicationStageAsync(ApplicationStage entity)
    {
        var sql = @"
            INSERT INTO jobs.applicationstages (jobapplyid, stageid, notes, interviewers, type, updatedby, interviewdate, interviewplace)
            VALUES (@JobApplyId, @StageId, @Notes, @Interviewers, @Type, @UpdatedBy, @InterviewDate, @InterviewPlace)
            RETURNING applicationstageid";
        var id = await ExecuteScalarAsync<Guid>(sql, new
        {
            entity.JobApplyId,
            entity.StageId,
            entity.Notes,
            entity.Interviewers,
            entity.Type,
            entity.UpdatedBy,
            entity.InterviewDate,
            entity.InterviewPlace
        });

        var updateJobAppSql = @"
            UPDATE jobs.jobapplications
            SET currentstageid = @stageId, status = (SELECT stagename FROM jobs.stagemaster WHERE stageid = @stageId), updateddate = CURRENT_TIMESTAMP
            WHERE jobapplyid = @jobApplyId";
        await ExecuteAsync(updateJobAppSql, new { entity.StageId, entity.JobApplyId });

        return id;
    }

    public async Task<bool> UpdateApplicationStageAsync(ApplicationStage entity)
    {
        var sql = @"
            UPDATE jobs.applicationstages
            SET notes = @Notes, interviewers = @Interviewers, type = @Type, updatedon = CURRENT_TIMESTAMP, updatedby = @UpdatedBy,
                interviewdate = @InterviewDate, interviewplace = @InterviewPlace
            WHERE applicationstageid = @ApplicationStageId";
        var rows = await ExecuteAsync(sql, new
        {
            entity.ApplicationStageId,
            entity.Notes,
            entity.Interviewers,
            entity.Type,
            entity.UpdatedBy,
            entity.InterviewDate,
            entity.InterviewPlace
        });
        return rows > 0;
    }

    public async Task<bool> DeleteApplicationStageAsync(Guid applicationStageId)
    {
        var sql = "DELETE FROM jobs.applicationstages WHERE applicationstageid = @applicationStageId";
        var rows = await ExecuteAsync(sql, new { applicationStageId });
        return rows > 0;
    }

    private sealed class ApplicationStageRow
    {
        public Guid ApplicationStageId { get; set; }
        public Guid JobApplyId { get; set; }
        public Guid? JobId { get; set; }
        public string? JobRoleName { get; set; }
        public string? CandidateName { get; set; }
        public Guid StageId { get; set; }
        public string? StageName { get; set; }
        public bool IsInterviewStage { get; set; }
        public string? Notes { get; set; }
        public string? InterviewersText { get; set; }
        public string? Type { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public Guid? UpdatedBy { get; set; }
        public string? UpdatedByName { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string? InterviewPlace { get; set; }
    }
}
