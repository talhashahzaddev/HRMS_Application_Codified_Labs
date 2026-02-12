using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class JobsService : IJobsService
{
    private readonly IJobsRepository _jobsRepository;
    private readonly ILogger<JobsService> _logger;

    public JobsService(IJobsRepository jobsRepository, ILogger<JobsService> logger)
    {
        _jobsRepository = jobsRepository;
        _logger = logger;
    }

    // ==================== StageMaster ====================
    public async Task<IEnumerable<StageMasterDto>> GetAllStageMastersAsync(Guid organizationId)
    {
        return await _jobsRepository.GetAllStageMastersAsync(organizationId);
    }

    public async Task<StageMasterDto?> GetStageMasterByIdAsync(Guid stageId)
    {
        return await _jobsRepository.GetStageMasterByIdAsync(stageId);
    }

    public async Task<StageMasterDto> CreateStageMasterAsync(Guid organizationId, CreateStageMasterRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.StageName))
            throw new ArgumentException("Stage name is required", nameof(request));

        var stageId = await _jobsRepository.CreateStageMasterAsync(organizationId, request.StageName.Trim(), request.StageOrder);
        var created = await _jobsRepository.GetStageMasterByIdAsync(stageId);
        if (created == null)
            throw new InvalidOperationException("Failed to retrieve created stage");
        return created;
    }

    public async Task<StageMasterDto?> UpdateStageMasterAsync(Guid stageId, UpdateStageMasterRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.StageName))
            throw new ArgumentException("Stage name is required", nameof(request));

        var updated = await _jobsRepository.UpdateStageMasterAsync(stageId, request.StageName.Trim(), request.StageOrder);
        if (!updated)
            return null;
        return await _jobsRepository.GetStageMasterByIdAsync(stageId);
    }

    public async Task<bool> DeleteStageMasterAsync(Guid stageId)
    {
        return await _jobsRepository.DeleteStageMasterAsync(stageId);
    }

    // ==================== JobOpenings ====================
    public async Task<IEnumerable<JobOpeningDto>> GetAllJobOpeningsAsync(Guid organizationId)
    {
        return await _jobsRepository.GetAllJobOpeningsAsync(organizationId);
    }

    public async Task<JobOpeningDto?> GetJobOpeningByIdAsync(Guid jobId)
    {
        return await _jobsRepository.GetJobOpeningByIdAsync(jobId);
    }

    public async Task<JobOpeningDto> CreateJobOpeningAsync(Guid organizationId, Guid? createdBy, CreateJobOpeningRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(request.JobRoleName))
            throw new ArgumentException("Job role name is required", nameof(request));
        if (string.IsNullOrWhiteSpace(request.PostedAs))
            throw new ArgumentException("Posted as is required", nameof(request));
        if (request.Vacancies < 1)
            throw new ArgumentException("Vacancies must be at least 1", nameof(request));

        var entity = new JobOpening
        {
            OrganizationId = organizationId,
            JobRoleName = request.JobRoleName.Trim(),
            DepartmentId = request.DepartmentId,
            ExperienceMin = request.ExperienceMin,
            ExperienceMax = request.ExperienceMax,
            CtcMin = request.CtcMin,
            CtcMax = request.CtcMax,
            Currency = request.Currency?.Trim(),
            Vacancies = request.Vacancies,
            Location = request.Location?.Trim(),
            WorkMode = request.WorkMode?.Trim(),
            EmploymentType = request.EmploymentType?.Trim(),
            JobIntroduction = request.JobIntroduction?.Trim(),
            Responsibilities = request.Responsibilities?.Trim(),
            Skillset = request.Skillset?.Trim(),
            LastDate = request.LastDate,
            PostedAs = request.PostedAs.Trim(),
            ExternalLink = request.ExternalLink?.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Open" : request.Status.Trim(),
            CreatedBy = createdBy
        };

        var jobId = await _jobsRepository.CreateJobOpeningAsync(entity);
        var created = await _jobsRepository.GetJobOpeningByIdAsync(jobId);
        if (created == null)
            throw new InvalidOperationException("Failed to retrieve created job opening");
        return created;
    }

    public async Task<JobOpeningDto?> UpdateJobOpeningAsync(Guid jobId, UpdateJobOpeningRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var existing = await _jobsRepository.GetJobOpeningByIdAsync(jobId);
        if (existing == null)
            return null;

        var entity = new JobOpening
        {
            JobId = jobId,
            OrganizationId = existing.OrganizationId,
            JobRoleName = request.JobRoleName ?? existing.JobRoleName,
            DepartmentId = request.DepartmentId ?? existing.DepartmentId,
            ExperienceMin = request.ExperienceMin ?? existing.ExperienceMin,
            ExperienceMax = request.ExperienceMax ?? existing.ExperienceMax,
            CtcMin = request.CtcMin ?? existing.CtcMin,
            CtcMax = request.CtcMax ?? existing.CtcMax,
            Currency = request.Currency ?? existing.Currency,
            Vacancies = request.Vacancies ?? existing.Vacancies,
            Location = request.Location ?? existing.Location,
            WorkMode = request.WorkMode ?? existing.WorkMode,
            EmploymentType = request.EmploymentType ?? existing.EmploymentType,
            JobIntroduction = request.JobIntroduction ?? existing.JobIntroduction,
            Responsibilities = request.Responsibilities ?? existing.Responsibilities,
            Skillset = request.Skillset ?? existing.Skillset,
            LastDate = request.LastDate ?? existing.LastDate,
            PostedAs = request.PostedAs ?? existing.PostedAs,
            ExternalLink = request.ExternalLink ?? existing.ExternalLink,
            Status = request.Status ?? existing.Status
        };

        var updated = await _jobsRepository.UpdateJobOpeningAsync(entity);
        if (!updated)
            return null;
        return await _jobsRepository.GetJobOpeningByIdAsync(jobId);
    }

    public async Task<bool> DeleteJobOpeningAsync(Guid jobId)
    {
        return await _jobsRepository.DeleteJobOpeningAsync(jobId);
    }

    // ==================== JobApplications ====================
    public async Task<IEnumerable<JobApplicationDto>> GetAllJobApplicationsAsync(Guid organizationId)
    {
        return await _jobsRepository.GetAllJobApplicationsAsync(organizationId);
    }

    public async Task<JobApplicationDto?> GetJobApplicationByIdAsync(Guid jobApplyId)
    {
        return await _jobsRepository.GetJobApplicationByIdAsync(jobApplyId);
    }

    public async Task<IEnumerable<JobApplicationDto>> GetJobApplicationsByEmployeeIdAsync(Guid employeeId)
    {
        return await _jobsRepository.GetJobApplicationsByEmployeeIdAsync(employeeId);
    }

    public async Task<IEnumerable<JobApplicationDto>> GetJobApplicationsByJobIdAsync(Guid jobId)
    {
        return await _jobsRepository.GetJobApplicationsByJobIdAsync(jobId);
    }

    public async Task<JobApplicationDto> CreateJobApplicationAsync(Guid organizationId, Guid employeeId, CreateJobApplicationRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        if (request.JobId == Guid.Empty)
            throw new ArgumentException("Job ID is required", nameof(request));

        var entity = new JobApplication
        {
            JobId = request.JobId,
            OrganizationId = organizationId,
            EmployeeId = employeeId,
            CandidateName = request.CandidateName?.Trim(),
            CandidateEmail = request.CandidateEmail?.Trim(),
            LinkedInUrl = request.LinkedInUrl?.Trim(),
            ResumeUrl = request.ResumeUrl?.Trim(),
            Phone = request.Phone?.Trim(),
            CoverLetter = request.CoverLetter?.Trim(),
            CurrentStageId = request.CurrentStageId,
            ApplicationSource = request.ApplicationSource?.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Applied" : request.Status.Trim()
        };

        var jobApplyId = await _jobsRepository.CreateJobApplicationAsync(entity);
        var created = await _jobsRepository.GetJobApplicationByIdAsync(jobApplyId);
        if (created == null)
            throw new InvalidOperationException("Failed to retrieve created job application");
        return created;
    }

    public async Task<JobApplicationDto?> UpdateJobApplicationAsync(Guid jobApplyId, UpdateJobApplicationRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var existing = await _jobsRepository.GetJobApplicationByIdAsync(jobApplyId);
        if (existing == null)
            return null;

        var entity = new JobApplication
        {
            JobApplyId = jobApplyId,
            CandidateName = request.CandidateName ?? existing.CandidateName,
            CandidateEmail = request.CandidateEmail ?? existing.CandidateEmail,
            LinkedInUrl = request.LinkedInUrl ?? existing.LinkedInUrl,
            ResumeUrl = request.ResumeUrl ?? existing.ResumeUrl,
            Phone = request.Phone ?? existing.Phone,
            CoverLetter = request.CoverLetter ?? existing.CoverLetter,
            CurrentStageId = request.CurrentStageId ?? existing.CurrentStageId,
            ApplicationSource = request.ApplicationSource ?? existing.ApplicationSource,
            Status = request.Status ?? existing.Status
        };

        var updated = await _jobsRepository.UpdateJobApplicationAsync(entity);
        if (!updated)
            return null;
        return await _jobsRepository.GetJobApplicationByIdAsync(jobApplyId);
    }

    public async Task<bool> DeleteJobApplicationAsync(Guid jobApplyId)
    {
        return await _jobsRepository.DeleteJobApplicationAsync(jobApplyId);
    }
}
