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
                   stageorder AS StageOrder, createdat AS CreatedAt
            FROM jobs.stagemaster
            WHERE organizationid = @organizationId
            ORDER BY stageorder NULLS LAST, stagename";
        return await QueryAsync<StageMasterDto>(sql, new { organizationId });
    }

    public async Task<StageMasterDto?> GetStageMasterByIdAsync(Guid stageId)
    {
        var sql = @"
            SELECT stageid AS StageId, organizationid AS OrganizationId, stagename AS StageName,
                   stageorder AS StageOrder, createdat AS CreatedAt
            FROM jobs.stagemaster
            WHERE stageid = @stageId";
        return await QuerySingleOrDefaultAsync<StageMasterDto>(sql, new { stageId });
    }

    public async Task<Guid> CreateStageMasterAsync(Guid organizationId, string stageName, int? stageOrder)
    {
        var sql = @"
            INSERT INTO jobs.stagemaster (organizationid, stagename, stageorder)
            VALUES (@organizationId, @stageName, @stageOrder)
            RETURNING stageid";
        return await ExecuteScalarAsync<Guid>(sql, new { organizationId, stageName, stageOrder });
    }

    public async Task<bool> UpdateStageMasterAsync(Guid stageId, string stageName, int? stageOrder)
    {
        var sql = @"
            UPDATE jobs.stagemaster
            SET stagename = @stageName, stageorder = @stageOrder
            WHERE stageid = @stageId";
        var rows = await ExecuteAsync(sql, new { stageId, stageName, stageOrder });
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
                linkedinurl, resumeurl, phone, coverletter, currentstageid, applicationsource, status)
            VALUES (@JobId, @OrganizationId, @EmployeeId, @CandidateName, @CandidateEmail,
                @LinkedInUrl, @ResumeUrl, @Phone, @CoverLetter, @CurrentStageId, @ApplicationSource, @Status)
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
            entity.Status
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
}
