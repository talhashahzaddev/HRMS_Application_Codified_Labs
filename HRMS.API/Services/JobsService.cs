using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Hangfire;

namespace HRMS.API.Services;

public class JobsService : IJobsService
{
    private readonly IJobsRepository _jobsRepository;
    private readonly IEmployeeService _employeeService;
    private readonly IQuestionBankRepository _questionBankRepository;
    private readonly IQuestionBankService _questionBankService;
    private readonly Hangfire.IBackgroundJobClient _backgroundJobClient;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
    private readonly ILogger<JobsService> _logger;

    public JobsService(
        IJobsRepository jobsRepository, 
        IEmployeeService employeeService, 
        IQuestionBankRepository questionBankRepository, 
        IQuestionBankService questionBankService,
        Hangfire.IBackgroundJobClient backgroundJobClient,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        ILogger<JobsService> logger)
    {
        _jobsRepository = jobsRepository;
        _employeeService = employeeService;
        _questionBankRepository = questionBankRepository;
        _questionBankService = questionBankService;
        _backgroundJobClient = backgroundJobClient;
        _configuration = configuration;
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

        var stageId = await _jobsRepository.CreateStageMasterAsync(organizationId, request.StageName.Trim(), request.StageOrder, request.IsInterviewStage);
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

        var updated = await _jobsRepository.UpdateStageMasterAsync(stageId, request.StageName.Trim(), request.StageOrder, request.IsInterviewStage);
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

    public async Task<JobOpeningStatsDto> GetOpeningOverallDetailsAsync(Guid organizationId)
    {
        return await _jobsRepository.GetJobOpeningStatsAsync(organizationId);
    }

    public async Task<PagedResult<JobOpeningDto>> GetJobOpeningsPagedAsync(Guid organizationId, string? search, string? status, DateTime? lastDateFrom, DateTime? lastDateTo, int pageNumber, int pageSize)
    {
        return await _jobsRepository.GetJobOpeningsPagedAsync(organizationId, search, status, lastDateFrom, lastDateTo, pageNumber, pageSize);
    }

    public async Task<PagedResult<JobOpeningDto>> GetExternalJobOpeningsPagedAsync(Guid organizationId, string? search, string? employmentType, DateTime? lastDateFrom, DateTime? lastDateTo, int pageNumber, int pageSize)
    {
        return await _jobsRepository.GetExternalJobOpeningsPagedAsync(organizationId, search, employmentType, lastDateFrom, lastDateTo, pageNumber, pageSize);
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

        if (!string.IsNullOrWhiteSpace(request.JobCode))
        {
            var exists = await _jobsRepository.IsJobCodeExistsAsync(request.JobCode.Trim(), organizationId);
            if (exists)
                throw new ArgumentException($"A job opening with the Job Code '{request.JobCode}' already exists.", nameof(request.JobCode));
        }

        var entity = new JobOpening
        {
            OrganizationId = organizationId,
            JobCode = request.JobCode?.Trim(),
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
            CreatedBy = createdBy,
            MandatorySkills = request.MandatorySkills
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

        if (!string.IsNullOrWhiteSpace(request.JobCode) && !string.Equals(request.JobCode.Trim(), existing.JobCode, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _jobsRepository.IsJobCodeExistsAsync(request.JobCode.Trim(), existing.OrganizationId, jobId);
            if (exists)
                throw new ArgumentException($"A job opening with the Job Code '{request.JobCode}' already exists.", nameof(request.JobCode));
        }

        var entity = new JobOpening
        {
            JobId = jobId,
            OrganizationId = existing.OrganizationId,
            JobCode = request.JobCode ?? existing.JobCode,
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
            Status = request.Status ?? existing.Status,
            MandatorySkills = request.MandatorySkills ?? existing.MandatorySkills
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
        var apps = await _jobsRepository.GetAllJobApplicationsAsync(organizationId);
        await CalculateMatchScoresAsync(apps);
        return apps;
    }

    public async Task<PagedResult<JobApplicationDto>> GetJobApplicationsPagedAsync(Guid organizationId, string? search, DateTime? applyDateFrom, DateTime? applyDateTo, Guid? stageId, List<Guid>? jobIds, int pageNumber, int pageSize)
    {
        var result = await _jobsRepository.GetJobApplicationsPagedAsync(organizationId, search, applyDateFrom, applyDateTo, stageId, jobIds, pageNumber, pageSize);
        await CalculateMatchScoresAsync(result.Data);
        return result;
    }

    public async Task<PagedResult<JobApplicationDto>> GetJobApplicationsPostedByMePagedAsync(Guid employeeId, string? search, DateTime? applyDateFrom, DateTime? applyDateTo, Guid? stageId, List<Guid>? jobIds, int pageNumber, int pageSize)
    {
        var result = await _jobsRepository.GetJobApplicationsPostedByMePagedAsync(employeeId, search, applyDateFrom, applyDateTo, stageId, jobIds, pageNumber, pageSize);
        await CalculateMatchScoresAsync(result.Data);
        return result;
    }

    public async Task<JobApplicationDto?> GetJobApplicationByIdAsync(Guid jobApplyId)
    {
        var app = await _jobsRepository.GetJobApplicationByIdAsync(jobApplyId);
        if (app != null) await CalculateMatchScoresAsync(new[] { app });
        return app;
    }

    public async Task<IEnumerable<JobApplicationDto>> GetJobApplicationsByEmployeeIdAsync(Guid employeeId)
    {
        var apps = await _jobsRepository.GetJobApplicationsByEmployeeIdAsync(employeeId);
        await CalculateMatchScoresAsync(apps);
        return apps;
    }

    public async Task<PagedResult<JobApplicationDto>> GetMyJobApplicationsPagedAsync(Guid employeeId, string? search, Guid? stageId, string? status, int pageNumber, int pageSize)
    {
        var result = await _jobsRepository.GetJobApplicationsByEmployeeIdPagedAsync(employeeId, search, stageId, status, pageNumber, pageSize);
        await CalculateMatchScoresAsync(result.Data);
        return result;
    }

    public async Task<PagedResult<JobApplicationDto>> GetMySelfJobApplicationsPagedAsync(Guid employeeId, string? search, Guid? stageId, string? status, int pageNumber, int pageSize)
    {
        var result = await _jobsRepository.GetMySelfJobApplicationsByEmployeeIdPagedAsync(employeeId, search, stageId, status, pageNumber, pageSize);
        await CalculateMatchScoresAsync(result.Data);
        return result;
    }

    public async Task<IEnumerable<JobApplicationDto>> GetJobApplicationsByJobIdAsync(Guid jobId)
    {
        var apps = await _jobsRepository.GetJobApplicationsByJobIdAsync(jobId);
        await CalculateMatchScoresAsync(apps);
        return apps;
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
            ApplicationSource = "Internal",
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Applied" : request.Status.Trim(),
            IsSelf=request.IsSelf
        };

        var jobApplyId = await _jobsRepository.CreateJobApplicationAsync(entity);
        
        // Save answers and evaluate knockouts
        bool passedKnockout = true;
        if (request.Answers != null && request.Answers.Any())
        {
            await _questionBankService.SaveCandidateAnswersAsync(jobApplyId, request.Answers);
            passedKnockout = await _questionBankService.EvaluateKnockoutAsync(jobApplyId);
        }
        else
        {
            // Auto pass if no knockouts
            await _jobsRepository.UpdatePassedKnockoutAsync(jobApplyId, true);
        }

        // Queue CV parsing background job ONLY if candidate passed knockouts
        if (passedKnockout && !string.IsNullOrWhiteSpace(request.ResumeUrl))
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

    public async Task<JobApplicationDto> ApplyForMySelfAsync(Guid organizationId, Guid employeeId, ApplyForMySelfRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        if (request.JobId == Guid.Empty)
            throw new ArgumentException("Job ID is required", nameof(request));

        var employee = await _employeeService.GetEmployeeByEmployeeIdAsync(employeeId);
        if (employee == null)
            throw new UnauthorizedAccessException("Current employee record not found. You must be an employee to apply for yourself.");

        var candidateName = $"{employee.FirstName?.Trim() ?? ""} {employee.LastName?.Trim() ?? ""}".Trim();
        if (string.IsNullOrWhiteSpace(candidateName))
            candidateName = employee.FullName?.Trim();

        var createRequest = new CreateJobApplicationRequest
        {
            JobId = request.JobId,
            CandidateName = candidateName,
            CandidateEmail = employee.Email?.Trim(),
            Phone = employee.Phone?.Trim(),
            LinkedInUrl = request.LinkedInUrl?.Trim(),
            ResumeUrl = request.ResumeUrl?.Trim(),
            CoverLetter = request.CoverLetter?.Trim(),
            ApplicationSource = request.ApplicationSource?.Trim() ?? "Internal",
            Status = "Applied",
            IsSelf =true,
            Answers = request.Answers
        };

        return await CreateJobApplicationAsync(organizationId, employeeId, createRequest);
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
            
        // Process Answers and Knockout
        bool previousKnockout = existing.PassedKnockout ?? false;
        bool newKnockout = previousKnockout;
        
        if (request.Answers != null && request.Answers.Any())
        {
            await _questionBankService.SaveCandidateAnswersAsync(jobApplyId, request.Answers);
            newKnockout = await _questionBankService.EvaluateKnockoutAsync(jobApplyId);
        }

        // Determine if CV should be parsed
        bool shouldParse = false;
        if (newKnockout)
        {
            // If they just passed it but they had failed it before (so it was never parsed)
            if (!previousKnockout) shouldParse = true;
            
            // Or if the resume changed explicitly
            if (!string.IsNullOrWhiteSpace(request.ResumeUrl) && request.ResumeUrl != existing.ResumeUrl) shouldParse = true;
        }

        var finalResumeUrl = request.ResumeUrl ?? existing.ResumeUrl;
        if (shouldParse && !string.IsNullOrWhiteSpace(finalResumeUrl))
        {
            var apiKey = _configuration["GoogleGemini:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                _backgroundJobClient.Enqueue<IResumeParserService>(x => x.ParseResumeAsync(jobApplyId, finalResumeUrl, apiKey));
            }
        }

        return await _jobsRepository.GetJobApplicationByIdAsync(jobApplyId);
    }

    public async Task<bool> DeleteJobApplicationAsync(Guid jobApplyId)
    {
        return await _jobsRepository.DeleteJobApplicationAsync(jobApplyId);
    }

    // ==================== ApplicationStages ====================
    public async Task<IEnumerable<ApplicationStageDto>> GetAllApplicationStagesAsync(Guid organizationId)
    {
        return await _jobsRepository.GetAllApplicationStagesAsync(organizationId);
    }

    public async Task<ApplicationStageDto?> GetApplicationStageByIdAsync(Guid applicationStageId)
    {
        return await _jobsRepository.GetApplicationStageByIdAsync(applicationStageId);
    }

    public async Task<IEnumerable<ApplicationStageDto>> GetApplicationStagesByJobApplyIdAsync(Guid jobApplyId)
    {
        return await _jobsRepository.GetApplicationStagesByJobApplyIdAsync(jobApplyId);
    }

    public async Task<ApplicationStageDto> CreateApplicationStageAsync(Guid updatedByEmployeeId, CreateApplicationStageRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (request.JobApplyId == Guid.Empty)
            throw new ArgumentException("Job apply ID is required", nameof(request));
        if (request.StageId == Guid.Empty)
            throw new ArgumentException("Stage ID is required", nameof(request));

        // Duplicate check: same application cannot be added to the same stage twice
        var alreadyExists = await _jobsRepository.ApplicationStageExistsAsync(request.JobApplyId, request.StageId);
        if (alreadyExists)
            throw new InvalidOperationException("This application has already been added to the same stage. Cannot add it again.");

        var interviewerIds = request.InterviewerIds ?? new List<Guid>();
        var interviewersText = interviewerIds.Any() ? string.Join(",", interviewerIds.Where(g => g != Guid.Empty).Distinct()) : string.Empty;

        var entity = new ApplicationStage
        {
            JobApplyId = request.JobApplyId,
            StageId = request.StageId,
            Notes = request.Notes?.Trim(),
            Interviewers = interviewersText,
            Type = request.Type?.Trim(),
            UpdatedBy = updatedByEmployeeId,
            InterviewDate = request.InterviewDate,
            InterviewPlace = request.InterviewPlace?.Trim()
        };

        var applicationStageId = await _jobsRepository.CreateApplicationStageAsync(entity);
        var created = await _jobsRepository.GetApplicationStageByIdAsync(applicationStageId);
        if (created == null)
            throw new InvalidOperationException("Failed to retrieve created application stage");
        return created;
    }

    public async Task<ApplicationStageDto?> UpdateApplicationStageAsync(Guid applicationStageId, Guid updatedByEmployeeId, UpdateApplicationStageRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var existing = await _jobsRepository.GetApplicationStageByIdAsync(applicationStageId);
        if (existing == null)
            return null;

        var interviewerIds = request.InterviewerIds ?? existing.InterviewerIds;
        var interviewersText = interviewerIds.Any() ? string.Join(",", interviewerIds.Where(g => g != Guid.Empty).Distinct()) : string.Empty;

        var entity = new ApplicationStage
        {
            ApplicationStageId = applicationStageId,
            Notes = request.Notes ?? existing.Notes,
            Interviewers = interviewersText,
            Type = request.Type ?? existing.Type,
            UpdatedBy = updatedByEmployeeId,
            InterviewDate = request.InterviewDate ?? existing.InterviewDate,
            InterviewPlace = request.InterviewPlace ?? existing.InterviewPlace
        };

        var updated = await _jobsRepository.UpdateApplicationStageAsync(entity);
        if (!updated)
            return null;
        return await _jobsRepository.GetApplicationStageByIdAsync(applicationStageId);
    }

    public async Task<bool> DeleteApplicationStageAsync(Guid applicationStageId)
    {
        return await _jobsRepository.DeleteApplicationStageAsync(applicationStageId);
    }

    private async Task CalculateMatchScoresAsync(IEnumerable<JobApplicationDto> apps)
    {
        if (apps == null || !apps.Any()) return;

        var jobApplyIds = apps.Select(a => a.JobApplyId).ToList();
        var parsedResumes = await _questionBankRepository.GetParsedResumesAsync(jobApplyIds);
        var parsedDict = parsedResumes.ToDictionary(pr => pr.JobApplyId);

        // Fetch unique jobs to get MandatorySkills and Skillset
        var uniqueJobIds = apps.Select(a => a.JobId).Distinct().ToList();
        var jobs = new Dictionary<Guid, JobOpeningDto>();
        foreach (var jid in uniqueJobIds)
        {
            var job = await _jobsRepository.GetJobOpeningByIdAsync(jid);
            if (job != null) jobs[jid] = job;
        }

        foreach (var app in apps)
        {
            if (app.PassedKnockout == false)
            {
                app.MatchScore = 0;
                continue;
            }

            if (!parsedDict.TryGetValue(app.JobApplyId, out var pr) || !jobs.TryGetValue(app.JobId, out var job))
            {
                app.MatchScore = 0;
                continue;
            }

            var parsedSkills = pr.Skills ?? "[]";
            var mandatorySkills = job.MandatorySkills ?? "[]";
            var jobSkills = job.Skillset ?? "[]";

            // Helper to extract skills cleanly (tries JSON first, falls back to comma separation)
            List<string> ExtractTokens(string input)
            {
                try 
                {
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(input);
                    if (list != null && list.Any()) 
                        return list.Select(s => s.Trim().ToLower()).Distinct().ToList();
                } 
                catch { }

                return input.ToLower().Split(new[] { ',', '\"', '[', ']', '{', '}' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Distinct()
                            .ToList();
            }

            var prTokens = ExtractTokens(parsedSkills);
            var mandatoryTokens = ExtractTokens(mandatorySkills);
            var optionalTokens = ExtractTokens(jobSkills);

            if (!prTokens.Any() && (mandatoryTokens.Any() || optionalTokens.Any()))
            {
                app.MatchScore = 0;
                continue;
            }

            decimal score = 0;
            int matchedMandatory = 0;

            if (mandatoryTokens.Any())
            {
                // Substring match: if the required skill is contained within the parsed skill (or vice versa)
                matchedMandatory = mandatoryTokens.Count(mt => prTokens.Any(pt => pt.Contains(mt) || mt.Contains(pt)));
                score += ((decimal)matchedMandatory / mandatoryTokens.Count) * 70m; 
            }
            else
            {
                score += 70m; 
            }

            if (optionalTokens.Any())
            {
                // Check how many of the parsed skills appear in the job's optional skillset text
                // Since optional skillset might be a huge HTML paragraph instead of a comma list
                var rawJobSkills = System.Text.RegularExpressions.Regex.Replace(jobSkills, "<.*?>", String.Empty).Replace("&nbsp;", " ").ToLower();
                int matchedOptional = prTokens.Count(pt => rawJobSkills.Contains(pt));
                
                // If the user pasted a paragraph, optionalTokens.Count is meaningless.
                // We'll calculate optional score based on finding at least SOME relevant skills.
                // Cap it at 100% of the optional weight (30m)
                decimal optionalScore = 0;
                if (matchedOptional > 0)
                {
                    // If they matched at least 3 skills from the description, give them full 30 points.
                    // Otherwise give proportional points.
                    optionalScore = Math.Min(30m, (matchedOptional / 3m) * 30m);
                }
                score += optionalScore;
            }
            else
            {
                score += 30m;
            }

            app.MatchScore = Math.Round(score, 2);
        }
    }
}
