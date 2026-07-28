using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using System;
using System.Threading.Tasks;

namespace HRMS.API.Repositories;

public class CareerRepository : BaseRepository, ICareerRepository
{
    public CareerRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<JobOpeningDto?> GetExternalJobByJobCodeAsync(string jobCode, Guid organizationId)
    {
        var sql = @"
            SELECT j.jobid AS JobId, j.jobcode AS JobCode, j.organizationid AS OrganizationId, j.jobrolename AS JobRoleName,
                   j.departmentid AS DepartmentId, d.departmentname AS DepartmentName,
                   j.experiencemin AS ExperienceMin, j.experiencemax AS ExperienceMax,
                   j.ctcmin AS CtcMin, j.ctcmax AS CtcMax, j.currency AS Currency,
                   j.vacancies AS Vacancies, j.location AS Location, j.workmode AS WorkMode,
                   j.employmenttype AS EmploymentType, j.jobintroduction AS JobIntroduction,
                   j.responsibilities AS Responsibilities, j.skillset AS Skillset,
                   j.lastdate AS LastDate, j.posteddate AS PostedDate, j.postedas AS PostedAs,
                   j.externallink AS ExternalLink, j.status AS Status, j.createdby AS CreatedBy,
                   CONCAT(emp.firstname, ' ', emp.lastname) AS CreatedByName,
                   j.createdat AS CreatedAt, j.updatedat AS UpdatedAt,
                   j.mandatoryskills AS MandatorySkills
            FROM jobs.jobopenings j
            LEFT JOIN employee.departments d ON j.departmentid = d.departmentid
            LEFT JOIN employee.employees emp ON j.createdby = emp.employeeid
            WHERE LOWER(j.jobcode) = LOWER(@jobCode) AND j.organizationid = @organizationId";
            
        return await QuerySingleOrDefaultAsync<JobOpeningDto>(sql, new { jobCode, organizationId });
    }

    public async Task<Guid> ApplyJobByExternalCandidateAsync(JobApplication entity)
    {
        var sql = @"
            INSERT INTO jobs.jobapplications (jobid, organizationid, employeeid, candidatename, candidateemail,
                linkedinurl, resumeurl, phone, coverletter, currentstageid, applicationsource, status, isself, isexternal)
            VALUES (@JobId, @OrganizationId, NULL, @CandidateName, @CandidateEmail,
                @LinkedInUrl, @ResumeUrl, @Phone, @CoverLetter, @CurrentStageId, @ApplicationSource, @Status, @IsSelf, @IsExternal)
            RETURNING jobapplyid";
        return await ExecuteScalarAsync<Guid>(sql, new
        {
            entity.JobId,
            entity.OrganizationId,
            entity.CandidateName,
            entity.CandidateEmail,
            entity.LinkedInUrl,
            entity.ResumeUrl,
            entity.Phone,
            entity.CoverLetter,
            entity.CurrentStageId,
            entity.ApplicationSource,
            entity.Status,
            entity.IsSelf,
            entity.IsExternal
        });
    }
}
