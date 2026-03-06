using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace HRMS.API.Services;

public class CareerService : ICareerService
{
    private readonly ICareerRepository _careerRepository;
    private readonly IJobsRepository _jobsRepository;

    public CareerService(ICareerRepository careerRepository, IJobsRepository jobsRepository)
    {
        _careerRepository = careerRepository;
        _jobsRepository = jobsRepository;
    }

    public async Task<JobOpeningDto?> GetExternalJobByJobCodeAsync(string jobCode, Guid organizationId)
    {
        return await _careerRepository.GetExternalJobByJobCodeAsync(jobCode, organizationId);
    }

    public async Task<JobApplicationDto> ApplyJobByExternalCandidateAsync(Guid organizationId, CreateJobApplicationRequest request)
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
            EmployeeId = null,
            CandidateName = request.CandidateName?.Trim(),
            CandidateEmail = request.CandidateEmail?.Trim(),
            LinkedInUrl = request.LinkedInUrl?.Trim(),
            ResumeUrl = request.ResumeUrl?.Trim(),
            Phone = request.Phone?.Trim(),
            CoverLetter = request.CoverLetter?.Trim(),
            CurrentStageId = request.CurrentStageId,
            ApplicationSource = "External",
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Applied" : request.Status.Trim(),
            IsSelf = false,
            IsExternal = true
        };

        var jobApplyId = await _careerRepository.ApplyJobByExternalCandidateAsync(entity);
        var created = await _jobsRepository.GetJobApplicationByIdAsync(jobApplyId);
        if (created == null)
            throw new InvalidOperationException("Failed to retrieve created job application");
        return created;
    }
}
