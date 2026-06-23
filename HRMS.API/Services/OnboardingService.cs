using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using System.Text.Json;

namespace HRMS.API.Services;

public class OnboardingService : IOnboardingService
{
    private readonly IOnboardingRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<OnboardingService> _logger;

    public OnboardingService(
        IOnboardingRepository repo,
        IEmployeeRepository employeeRepo,
        IFileStorageService fileStorage,
        ILogger<OnboardingService> logger)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>Resolves employee ID from userId, then gets-or-creates onboarding session.</summary>
    private async Task<(Guid employeeId, OnboardingSession session)> GetOrCreateSessionAsync(
        Guid userId, Guid organizationId)
    {
        var employeeId = await _employeeRepo.GetEmployeeIdAsync(userId)
            ?? throw new KeyNotFoundException("Employee record not found for this user.");

        var session = await _repo.GetSessionByEmployeeIdAsync(employeeId);
        if (session is null)
        {
            session = await _repo.CreateSessionAsync(new OnboardingSession
            {
                EmployeeId = employeeId,
                OrganizationId = organizationId,
                CurrentStep = 1,
                Status = "in_progress"
            });
        }

        return (employeeId, session);
    }

    private static decimal CalculateCompletion(OnboardingSession s,
        bool hasEducation, bool hasExperience)
    {
        int done = 0, total = 7;
        // personal
        if (!string.IsNullOrEmpty(s.IdNumber)) done++;
        if (!string.IsNullOrEmpty(s.JobTitle)) done++;
        if (!string.IsNullOrEmpty(s.PhotoUrl)) done++;
        if (!string.IsNullOrEmpty(s.ResumeUrl)) done++;
        if (hasEducation) done++;
        if (hasExperience) done++;
        if (s.IsPolicyAccepted) done++;
        return Math.Round((decimal)done / total * 100, 0);
    }

    // ── Step 1 – personal info ────────────────────────────────────────────────

    public async Task<(bool success, string onboardingId, string message)> SavePersonalInfoAsync(
        Guid userId, Guid organizationId,
        OnboardingPersonalInfoRequest request,
        IFormFile? photo, IFormFile? resume)
    {
        try
        {
            var (employeeId, session) = await GetOrCreateSessionAsync(userId, organizationId);

            // Upload files if provided
            string? photoUrl = null;
            string? resumeUrl = null;
            string? resumeFileName = null;

            if (photo is { Length: > 0 })
                photoUrl = await _fileStorage.UploadFileAsync(photo, "Uploads/Photos");

            if (resume is { Length: > 0 })
            {
                resumeUrl = await _fileStorage.UploadFileAsync(resume, "Uploads/Resumes");
                resumeFileName = resume.FileName;
            }

            await _repo.UpsertPersonalInfoAsync(
                employeeId,
                request.FullName,
                request.Phone,
                request.Gender,
                request.Nationality,
                request.IdNumber,
                photoUrl,
                resumeUrl,
                resumeFileName,
                request.JobTitle,
                request.Department);

            // Update session step
            session.CurrentStep = Math.Max(session.CurrentStep, 2);
            session.JobTitle = request.JobTitle;
            session.Department = request.Department;
            session.IdNumber = request.IdNumber;
            if (photoUrl != null) session.PhotoUrl = photoUrl;
            if (resumeUrl != null) { session.ResumeUrl = resumeUrl; session.ResumeFileName = resumeFileName; }

            var education = await _repo.GetEducationBySessionAsync(session.OnboardingId);
            var experience = await _repo.GetWorkExperienceBySessionAsync(session.OnboardingId);
            session.CompletionRate = CalculateCompletion(session,
                education.Count > 0, experience.Count > 0);

            await _repo.UpdateSessionAsync(session);

            return (true, session.OnboardingId.ToString(), "Personal information saved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving personal info for user {UserId}", userId);
            throw;
        }
    }

    // ── Step 2 – education ────────────────────────────────────────────────────

    public async Task<OnboardingEducationDto> AddEducationAsync(
        Guid userId, OnboardingEducationRequest request, IFormFile? file)
    {
        var employeeId = await _employeeRepo.GetEmployeeIdAsync(userId)
            ?? throw new KeyNotFoundException("Employee not found.");

        var session = await _repo.GetSessionByEmployeeIdAsync(employeeId)
            ?? throw new InvalidOperationException("Onboarding session not started. Save personal info first.");

        string? fileUrl = null;
        string? fileName = request.FileName;
        if (file is { Length: > 0 })
        {
            fileUrl = await _fileStorage.UploadFileAsync(file, "Uploads/Education");
            fileName = file.FileName;
        }

        var record = await _repo.AddEducationAsync(new OnboardingEducation
        {
            OnboardingId = session.OnboardingId,
            Degree = request.Degree,
            Institution = request.Institution,
            Field = request.Field,
            Gpa = request.Gpa,
            FileName = fileName,
            FileUrl = fileUrl,
            Description = request.Description
        });

        // Update completion
        var experience = await _repo.GetWorkExperienceBySessionAsync(session.OnboardingId);
        session.CompletionRate = CalculateCompletion(session, true, experience.Count > 0);
        session.CurrentStep = Math.Max(session.CurrentStep, 2);
        await _repo.UpdateSessionAsync(session);

        return new OnboardingEducationDto
        {
            EducationId = record.EducationId,
            OnboardingId = record.OnboardingId,
            Degree = record.Degree,
            Institution = record.Institution,
            Field = record.Field,
            Gpa = record.Gpa,
            FileName = record.FileName,
            FileUrl = record.FileUrl,
            Description = record.Description
        };
    }

    public async Task<bool> UpdateEducationAsync(
        Guid userId, Guid educationId, OnboardingEducationRequest request, IFormFile? file)
    {
        var employeeId = await _employeeRepo.GetEmployeeIdAsync(userId)
            ?? throw new KeyNotFoundException("Employee not found.");

        var session = await _repo.GetSessionByEmployeeIdAsync(employeeId)
            ?? throw new InvalidOperationException("No active onboarding session.");

        string? fileUrl = null;
        string? fileName = request.FileName;
        if (file is { Length: > 0 })
        {
            fileUrl = await _fileStorage.UploadFileAsync(file, "Uploads/Education");
            fileName = file.FileName;
        }

        return await _repo.UpdateEducationAsync(new OnboardingEducation
        {
            EducationId = educationId,
            OnboardingId = session.OnboardingId,
            Degree = request.Degree,
            Institution = request.Institution,
            Field = request.Field,
            Gpa = request.Gpa,
            FileName = fileName,
            FileUrl = fileUrl,
            Description = request.Description
        });
    }

    public async Task<bool> DeleteEducationAsync(Guid userId, Guid educationId)
    {
        var employeeId = await _employeeRepo.GetEmployeeIdAsync(userId)
            ?? throw new KeyNotFoundException("Employee not found.");

        var session = await _repo.GetSessionByEmployeeIdAsync(employeeId)
            ?? throw new InvalidOperationException("No active onboarding session.");

        return await _repo.DeleteEducationAsync(educationId, session.OnboardingId);
    }

    // ── Step 3 – work experience ──────────────────────────────────────────────

    public async Task<OnboardingWorkExperienceDto> AddWorkExperienceAsync(
        Guid userId, OnboardingWorkExperienceRequest request, IFormFile? file)
    {
        var employeeId = await _employeeRepo.GetEmployeeIdAsync(userId)
            ?? throw new KeyNotFoundException("Employee not found.");

        var session = await _repo.GetSessionByEmployeeIdAsync(employeeId)
            ?? throw new InvalidOperationException("Onboarding session not started.");

        string? fileUrl = null;
        string? fileName = request.FileName;
        if (file is { Length: > 0 })
        {
            fileUrl = await _fileStorage.UploadFileAsync(file, "Uploads/WorkExperience");
            fileName = file.FileName;
        }

        var record = await _repo.AddWorkExperienceAsync(new OnboardingWorkExperience
        {
            OnboardingId = session.OnboardingId,
            JobTitle = request.JobTitle,
            Company = request.Company,
            EmploymentType = request.EmploymentType,
            Location = request.Location,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CurrentlyWorking = request.CurrentlyWorking,
            Responsibilities = request.Responsibilities,
            FileName = fileName,
            FileUrl = fileUrl
        });

        // Update completion
        var education = await _repo.GetEducationBySessionAsync(session.OnboardingId);
        session.CompletionRate = CalculateCompletion(session, education.Count > 0, true);
        session.CurrentStep = Math.Max(session.CurrentStep, 3);
        await _repo.UpdateSessionAsync(session);

        return new OnboardingWorkExperienceDto
        {
            ExperienceId = record.ExperienceId,
            OnboardingId = record.OnboardingId,
            JobTitle = record.JobTitle,
            Company = record.Company,
            EmploymentType = record.EmploymentType,
            Location = record.Location,
            StartDate = record.StartDate,
            EndDate = record.EndDate,
            CurrentlyWorking = record.CurrentlyWorking,
            Responsibilities = record.Responsibilities,
            FileName = record.FileName,
            FileUrl = record.FileUrl
        };
    }

    public async Task<bool> UpdateWorkExperienceAsync(
        Guid userId, Guid experienceId, OnboardingWorkExperienceRequest request, IFormFile? file)
    {
        var employeeId = await _employeeRepo.GetEmployeeIdAsync(userId)
            ?? throw new KeyNotFoundException("Employee not found.");

        var session = await _repo.GetSessionByEmployeeIdAsync(employeeId)
            ?? throw new InvalidOperationException("No active onboarding session.");

        string? fileUrl = null;
        string? fileName = request.FileName;
        if (file is { Length: > 0 })
        {
            fileUrl = await _fileStorage.UploadFileAsync(file, "Uploads/WorkExperience");
            fileName = file.FileName;
        }

        return await _repo.UpdateWorkExperienceAsync(new OnboardingWorkExperience
        {
            ExperienceId = experienceId,
            OnboardingId = session.OnboardingId,
            JobTitle = request.JobTitle,
            Company = request.Company,
            EmploymentType = request.EmploymentType,
            Location = request.Location,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CurrentlyWorking = request.CurrentlyWorking,
            Responsibilities = request.Responsibilities,
            FileName = fileName,
            FileUrl = fileUrl
        });
    }

    public async Task<bool> DeleteWorkExperienceAsync(Guid userId, Guid experienceId)
    {
        var employeeId = await _employeeRepo.GetEmployeeIdAsync(userId)
            ?? throw new KeyNotFoundException("Employee not found.");

        var session = await _repo.GetSessionByEmployeeIdAsync(employeeId)
            ?? throw new InvalidOperationException("No active onboarding session.");

        return await _repo.DeleteWorkExperienceAsync(experienceId, session.OnboardingId);
    }

    // ── Step 5 – policy acceptance ────────────────────────────────────────────

    public async Task<bool> AcceptPoliciesAsync(Guid userId, bool accepted, string? documentNotes)
    {
        var employeeId = await _employeeRepo.GetEmployeeIdAsync(userId)
            ?? throw new KeyNotFoundException("Employee not found.");

        var session = await _repo.GetSessionByEmployeeIdAsync(employeeId)
            ?? throw new InvalidOperationException("No active onboarding session.");

        session.IsPolicyAccepted = accepted;
        session.DocumentNotes = documentNotes;
        session.CurrentStep = Math.Max(session.CurrentStep, 5);

        var education = await _repo.GetEducationBySessionAsync(session.OnboardingId);
        var experience = await _repo.GetWorkExperienceBySessionAsync(session.OnboardingId);
        session.CompletionRate = CalculateCompletion(session, education.Count > 0, experience.Count > 0);

        return await _repo.UpdateSessionAsync(session);
    }

    // ── Step 6 – final submit ─────────────────────────────────────────────────

    public async Task<bool> FinalSubmitAsync(
        Guid userId, Guid organizationId,
        OnboardingSubmitRequest request,
        IFormFile? photo, IFormFile? resume)
    {
        var (employeeId, session) = await GetOrCreateSessionAsync(userId, organizationId);

        // Upload files
        string? photoUrl = null;
        string? resumeUrl = null;
        string? resumeFileName = null;

        if (photo is { Length: > 0 }) photoUrl = await _fileStorage.UploadFileAsync(photo, "Uploads/Photos");
        if (resume is { Length: > 0 })
        {
            resumeUrl = await _fileStorage.UploadFileAsync(resume, "Uploads/Resumes");
            resumeFileName = resume.FileName;
        }

        // Upsert personal info
        var fullName = $"{request.FirstName} {request.LastName}".Trim();
        await _repo.UpsertPersonalInfoAsync(employeeId, fullName, request.Phone,
            request.Gender, request.Nationality, request.IdNumber,
            photoUrl, resumeUrl, resumeFileName ?? request.ResumeFileName,
            request.JobTitle, request.Department);

        // Education: add any new entries that don't have an id
        foreach (var edu in request.Education.Where(e => e.EducationId is null))
            await _repo.AddEducationAsync(new OnboardingEducation
            {
                OnboardingId = session.OnboardingId,
                Degree = edu.Degree,
                Institution = edu.Institution,
                Field = edu.Field,
                Gpa = edu.Gpa,
                FileName = edu.FileName,
                Description = edu.Description
            });

        // Work experience: add new entries
        foreach (var exp in request.WorkExperience.Where(e => e.ExperienceId is null))
            await _repo.AddWorkExperienceAsync(new OnboardingWorkExperience
            {
                OnboardingId = session.OnboardingId,
                JobTitle = exp.JobTitle,
                Company = exp.Company,
                EmploymentType = exp.EmploymentType,
                Location = exp.Location,
                StartDate = exp.StartDate,
                EndDate = exp.EndDate,
                CurrentlyWorking = exp.CurrentlyWorking,
                Responsibilities = exp.Responsibilities,
                FileName = exp.FileName
            });

        // Mark as completed
        session.Status = "completed";
        session.CurrentStep = 7;
        session.IsPolicyAccepted = request.PolicyAccepted;
        session.DocumentNotes = request.DocumentNotes;
        if (photoUrl != null) session.PhotoUrl = photoUrl;
        if (resumeUrl != null) { session.ResumeUrl = resumeUrl; session.ResumeFileName = resumeFileName; }

        var eduList = await _repo.GetEducationBySessionAsync(session.OnboardingId);
        var expList = await _repo.GetWorkExperienceBySessionAsync(session.OnboardingId);
        session.CompletionRate = CalculateCompletion(session, eduList.Count > 0, expList.Count > 0);

        return await _repo.UpdateSessionAsync(session);
    }

    // ── Step tracker ──────────────────────────────────────────────────────────

    public async Task<bool> UpdateCurrentStepAsync(Guid userId, int step)
    {
        var employeeId = await _employeeRepo.GetEmployeeIdAsync(userId)
            ?? throw new KeyNotFoundException("Employee not found.");

        var session = await _repo.GetSessionByEmployeeIdAsync(employeeId)
            ?? throw new InvalidOperationException("No active onboarding session.");

        session.CurrentStep = Math.Max(session.CurrentStep, step);
        return await _repo.UpdateSessionAsync(session);
    }

    // ── GET my-status ─────────────────────────────────────────────────────────

    public async Task<OnboardingStatusResponse?> GetMyStatusAsync(Guid userId, Guid organizationId)
    {
        var employeeId = await _employeeRepo.GetEmployeeIdAsync(userId);
        if (employeeId is null) return null;

        return await _repo.GetFullStatusAsync(employeeId.Value, organizationId);
    }

// ── Step 4 – bank details ─────────────────────────────────────────────────────

public async Task<OnboardingBankDetailsDto?> GetBankDetailsAsync(Guid userId)
    {
        var employeeId = await _employeeRepo.GetEmployeeIdAsync(userId)
            ?? throw new KeyNotFoundException("Employee not found.");

        var session = await _repo.GetSessionByEmployeeIdAsync(employeeId);
        if (session is null) return null;

        var record = await _repo.GetBankDetailsByOnboardingIdAsync(session.OnboardingId);
        if (record is null) return null;

        return new OnboardingBankDetailsDto
        {
            BankDetailsId = record.BankDetailsId,
            OnboardingId = record.OnboardingId,
            NoBankAccount = record.NoBankAccount,
            AccountHolderName = record.AccountHolderName,
            AccountNumber = record.AccountNumber,
            BankName = record.BankName,
            PaymentMethod = record.PaymentMethod,
            Iban = record.Iban,
            BranchName = record.BranchName,
            BranchCode = record.BranchCode
        };
    }

    public async Task<(bool success, string message)> SaveBankDetailsAsync(
        Guid userId,
        OnboardingBankDetailsRequest request)
    {
        try
        {
            var employeeId = await _employeeRepo.GetEmployeeIdAsync(userId)
                ?? throw new KeyNotFoundException("Employee record not found for this user.");

            var session = await _repo.GetSessionByEmployeeIdAsync(employeeId)
                ?? throw new InvalidOperationException(
                    "No active onboarding session found. Please complete Step 1 first.");

            var record = new OnboardingBankDetails
            {
                OnboardingId = session.OnboardingId,
                NoBankAccount = request.NoBankAccount,
                AccountHolderName = request.NoBankAccount ? null : request.AccountHolderName?.Trim(),
                AccountNumber = request.NoBankAccount ? null : request.AccountNumber?.Trim(),
                BankName = request.NoBankAccount ? null : request.BankName?.Trim(),
                PaymentMethod = request.NoBankAccount ? null : request.PaymentMethod?.Trim(),
                Iban = request.NoBankAccount ? null : request.Iban?.Trim(),
                BranchName = request.NoBankAccount ? null : request.BranchName?.Trim(),
                BranchCode = request.NoBankAccount ? null : request.BranchCode?.Trim()
            };

            await _repo.UpsertBankDetailsAsync(record);

            // Advance step tracker and recalculate completion
            session.CurrentStep = Math.Max(session.CurrentStep, 4);
            var education = await _repo.GetEducationBySessionAsync(session.OnboardingId);
            var experience = await _repo.GetWorkExperienceBySessionAsync(session.OnboardingId);
            session.CompletionRate = CalculateCompletion(session,
                education.Count > 0, experience.Count > 0);
            await _repo.UpdateSessionAsync(session);

            return (true, "Bank details saved successfully.");
        }
        catch (KeyNotFoundException ex) { throw; }
        catch (InvalidOperationException ex) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving bank details for user {UserId}", userId);
            throw;
        }
    }

}
// PolicyService.cs


    // ============================================================
    // FILE: Services/PolicyService.cs
    // ============================================================

public class PolicyService : IPolicyService
{
    private readonly IPolicyRepository _repo;
    private readonly ILogger<PolicyService> _logger;

    public PolicyService(
        IPolicyRepository repo,
        ILogger<PolicyService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    /// <summary>Super Admin sees ALL; employees see published only.</summary>
    public async Task<List<PolicyDto>> GetPoliciesAsync(Guid organizationId, bool isSuperAdmin)
        => isSuperAdmin
            ? await _repo.GetAllAsync(organizationId)
            : await _repo.GetPublishedAsync(organizationId);

    public async Task<PolicyDto?> GetByIdAsync(Guid policyId, Guid organizationId)
        => await _repo.GetByIdAsync(policyId, organizationId);

    // ── Create ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a policy.
    ///
    /// The file is NOT uploaded here. The frontend uploads it first via
    /// POST /api/uploads/files and passes the returned public URL as
    /// request.FileUrl. We store that URL string directly in the DB.
    /// </summary>
    public async Task<PolicyDto> CreateAsync(
        Guid organizationId,
        CreatePolicyRequest request)
    {
        var toggles = DeserializeToggles(request.FieldToggles);

        ValidateByToggles(
            request.PolicyName,
            request.Category,
            request.PolicyContent,
            hasAttachment: !string.IsNullOrWhiteSpace(request.FileUrl),
            toggles: toggles,
            isPublishIntent: request.IsPublished);

        var policy = new PolicyDto
        {
            OrganizationId = organizationId,
            PolicyName = (request.PolicyName ?? string.Empty).Trim(),
            Category = (request.Category ?? string.Empty).Trim(),
            PolicyContent = (request.PolicyContent ?? string.Empty).Trim(),
            IsPublished = request.IsPublished,
            // Store the pre-uploaded public URL exactly as received.
            // No path manipulation — the frontend gives us a ready-to-use URL.
            FileUrl = string.IsNullOrWhiteSpace(request.FileUrl) ? null : request.FileUrl.Trim(),
            FileName = string.IsNullOrWhiteSpace(request.FileName) ? null : request.FileName.Trim(),
            FieldToggles = request.FieldToggles
        };

        return await _repo.CreateAsync(policy);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates a policy.
    ///
    /// If request.FileUrl is non-null → a new file was uploaded, use it.
    /// If request.FileUrl is null     → keep the existing URL in DB (COALESCE).
    /// </summary>
    public async Task<(bool found, bool updated)> UpdateAsync(
        Guid policyId,
        Guid organizationId,
        UpdatePolicyRequest request)
    {
        var existing = await _repo.GetByIdAsync(policyId, organizationId);
        if (existing is null) return (false, false);

        var toggles = DeserializeToggles(request.FieldToggles);

        // For attachment validation: a file exists if a new URL was provided
        // OR an old URL is already in DB
        var hasAttachment = !string.IsNullOrWhiteSpace(request.FileUrl)
                         || !string.IsNullOrWhiteSpace(existing.FileUrl);

        ValidateByToggles(
            request.PolicyName,
            request.Category,
            request.PolicyContent,
            hasAttachment: hasAttachment,
            toggles: toggles,
            isPublishIntent: request.IsPublished);

        existing.PolicyName = (request.PolicyName ?? string.Empty).Trim();
        existing.Category = (request.Category ?? string.Empty).Trim();
        existing.PolicyContent = (request.PolicyContent ?? string.Empty).Trim();
        existing.IsPublished = request.IsPublished;
        existing.FieldToggles = request.FieldToggles;

        // Only overwrite file fields when a new URL was actually provided
        if (!string.IsNullOrWhiteSpace(request.FileUrl))
        {
            existing.FileUrl = request.FileUrl.Trim();
            existing.FileName = string.IsNullOrWhiteSpace(request.FileName)
                ? null
                : request.FileName.Trim();
        }

        var updated = await _repo.UpdateAsync(existing);
        return (true, updated);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    public async Task<bool> DeleteAsync(Guid policyId, Guid organizationId)
    {
        var existing = await _repo.GetByIdAsync(policyId, organizationId);
        if (existing is null) return false;
        return await _repo.DeleteAsync(policyId, organizationId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PolicyFieldToggle DeserializeToggles(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new PolicyFieldToggle();
        try
        {
            return JsonSerializer.Deserialize<PolicyFieldToggle>(
                       json,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new PolicyFieldToggle();
        }
        catch
        {
            return new PolicyFieldToggle();
        }
    }

    private static void ValidateByToggles(
        string? name,
        string? category,
        string? content,
        bool hasAttachment,
        PolicyFieldToggle toggles,
        bool isPublishIntent)
    {
        var errors = new List<string>();

        if (isPublishIntent)
        {
            if (string.IsNullOrWhiteSpace(name))
                errors.Add("Policy name is required when publishing.");
            if (string.IsNullOrWhiteSpace(content))
                errors.Add("Policy content is required when publishing.");
        }
        else
        {
            if (toggles.NameRequired && string.IsNullOrWhiteSpace(name))
                errors.Add("Policy name is required (enabled by toggle).");
            if (toggles.CategoryRequired && string.IsNullOrWhiteSpace(category))
                errors.Add("Category is required (enabled by toggle).");
            if (toggles.ContentRequired && string.IsNullOrWhiteSpace(content))
                errors.Add("Policy content is required (enabled by toggle).");
            if (toggles.AttachmentRequired && !hasAttachment)
                errors.Add("An attachment is required (enabled by toggle).");
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" | ", errors));
    }
}