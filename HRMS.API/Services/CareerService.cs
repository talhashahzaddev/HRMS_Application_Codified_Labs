using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;

namespace HRMS.API.Services;

public class CareerService : ICareerService
{
    private readonly ICareerRepository _careerRepository;
    private readonly IJobsRepository _jobsRepository;
    private readonly IQuestionBankService _questionBankService;
    private readonly Hangfire.IBackgroundJobClient _backgroundJobClient;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public CareerService(
        ICareerRepository careerRepository, 
        IJobsRepository jobsRepository,
        IQuestionBankService questionBankService,
        Hangfire.IBackgroundJobClient backgroundJobClient,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _careerRepository = careerRepository;
        _jobsRepository = jobsRepository;
        _questionBankService = questionBankService;
        _backgroundJobClient = backgroundJobClient;
        _configuration = configuration;
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
        
        // Save answers and evaluate knockouts
        if (request.Answers != null && request.Answers.Any())
        {
            await _questionBankService.SaveCandidateAnswersAsync(jobApplyId, request.Answers);
            await _questionBankService.EvaluateKnockoutAsync(jobApplyId);
        }
        else
        {
            // Auto pass if no knockouts
            await _jobsRepository.UpdatePassedKnockoutAsync(jobApplyId, true);
        }

        // Queue CV parsing background job if resume URL exists
        if (!string.IsNullOrWhiteSpace(request.ResumeUrl))
        {
            var apiKey = _configuration["GoogleGemini:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                _backgroundJobClient.Enqueue<IResumeParserService>(x => x.ParseResumeAsync(jobApplyId, request.ResumeUrl, apiKey));
            }
        }

        var created = await _jobsRepository.GetJobApplicationByIdAsync(jobApplyId);
        if (created == null)
            throw new InvalidOperationException("Failed to retrieve created job application");
        return created;
    }
}
