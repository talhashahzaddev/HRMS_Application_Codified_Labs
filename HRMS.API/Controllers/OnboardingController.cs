// ============================================================
// FILE: Controllers/OnboardingController.cs
// ============================================================
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/onboarding")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _service;
    private readonly IOnboardingConfigRepository _configRepo;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(
        IOnboardingService service,
        IOnboardingConfigRepository configRepo,
        ILogger<OnboardingController> logger)
    {
        _service = service;
        _configRepo = configRepo;
        _logger = logger;
    }

    // ── GET /api/onboarding/my-status ────────────────────────────────────────

    [HttpGet("my-status")]
    public async Task<IActionResult> GetMyStatus()
    {
        try
        {
            var userId = GetCurrentUserId();
            var orgId = GetCurrentOrganizationId();
            var status = await _service.GetMyStatusAsync(userId, orgId);

            if (status is null)
                return NotFound(new { success = false, message = "No onboarding session found." });

            return Ok(new { success = true, data = status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching onboarding status");
            return StatusCode(500, new { success = false, message = "Failed to load onboarding status." });
        }
    }

    // ── PUT /api/onboarding/personal-info  (Step 1) ──────────────────────────

    /// <summary>
    /// Accepts all personal-info fields as optional strings from the form.
    /// Required-field enforcement is fully driven by the org's field config
    /// loaded from onboarding.field_configurations — no hardcoded checks.
    /// </summary>
    [HttpPut("personal-info")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SavePersonalInfo(
        [FromForm] string? fullName,
        [FromForm] string? jobTitle,
        [FromForm] string? email,
        [FromForm] string? phone,
        [FromForm] string? department,
        [FromForm] string? gender,
        [FromForm] string? nationality,
        [FromForm] string? idNumber,
        [FromForm] string? photoUrl,
        [FromForm] string? resumeUrl,
        [FromForm] string? resumeFileName,
        IFormFile? photo,
        IFormFile? resume)
    {
        try
        {
            var userId = GetCurrentUserId();
            var orgId = GetCurrentOrganizationId();

            // ── Load field config and validate dynamically ────────────────
            var fieldConfig = await BuildPersonalInfoFieldConfig(orgId);

            // A photo / resume is "provided" if a file was attached OR a pre-uploaded URL exists
            var photoIndicator = (photo is { Length: > 0 }) ? "provided" : photoUrl;
            var resumeIndicator = (resume is { Length: > 0 }) ? "provided" : (resumeUrl ?? resumeFileName);

            var errors = ValidatePersonalInfo(
                fullName, email, phone, department, gender,
                nationality, idNumber, photoIndicator, resumeIndicator,
                fieldConfig);

            if (errors.Count > 0)
                return BadRequest(new { success = false, message = string.Join(" | ", errors) });

            // ── Delegate to service ───────────────────────────────────────
            var request = new OnboardingPersonalInfoRequest
            {
                FullName = fullName ?? string.Empty,
                JobTitle = jobTitle ?? string.Empty,
                Email = email ?? string.Empty,
                Phone = phone ?? string.Empty,
                Department = department ?? string.Empty,
                Gender = gender ?? string.Empty,
                Nationality = nationality ?? string.Empty,
                IdNumber = idNumber ?? string.Empty
            };

            var (success, onboardingId, message) =
                await _service.SavePersonalInfoAsync(userId, orgId, request, photo, resume);

            return Ok(new { success, message, data = new { onboardingId } });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving personal info");
            return StatusCode(500, new { success = false, message = "Failed to save personal information." });
        }
    }

    // ── POST /api/onboarding/education  (Step 2 – add) ───────────────────────

    [HttpPost("education")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddEducation(
        [FromForm] string degree,
        [FromForm] string institution,
        [FromForm] string field,
        [FromForm] string? gpa,
        [FromForm] string? fileName,
        [FromForm] string? fileUrl,
        [FromForm] string? description,
        [FromForm] string? onboardingId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var request = new OnboardingEducationRequest
            {
                Degree = degree,
                Institution = institution,
                Field = field,
                Gpa = gpa,
                FileName = fileName,
                Description = description
            };

            var result = await _service.AddEducationAsync(userId, request, fileUrl);
            return Ok(new { success = true, message = "Education record added.", data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding education");
            return StatusCode(500, new { success = false, message = "Failed to save education." });
        }
    }

    // ── PUT /api/onboarding/education/{id}  (Step 2 – update) ────────────────

    [HttpPut("education/{id:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateEducation(
        Guid id,
        [FromForm] string degree,
        [FromForm] string institution,
        [FromForm] string field,
        [FromForm] string? gpa,
        [FromForm] string? fileName,
        [FromForm] string? description,
        IFormFile? file)
    {
        try
        {
            var userId = GetCurrentUserId();
            var request = new OnboardingEducationRequest
            {
                EducationId = id,
                Degree = degree,
                Institution = institution,
                Field = field,
                Gpa = gpa,
                FileName = fileName,
                Description = description
            };

            var updated = await _service.UpdateEducationAsync(userId, id, request, file);
            if (!updated)
                return NotFound(new { success = false, message = "Education record not found." });

            return Ok(new { success = true, message = "Education record updated." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating education {Id}", id);
            return StatusCode(500, new { success = false, message = "Failed to update education." });
        }
    }

    // ── DELETE /api/onboarding/education/{id} ────────────────────────────────

    [HttpDelete("education/{id:guid}")]
    public async Task<IActionResult> DeleteEducation(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var deleted = await _service.DeleteEducationAsync(userId, id);
            if (!deleted)
                return NotFound(new { success = false, message = "Education record not found." });

            return Ok(new { success = true, message = "Education record deleted." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting education {Id}", id);
            return StatusCode(500, new { success = false, message = "Failed to delete education." });
        }
    }

    // ── POST /api/onboarding/work-experience  (Step 3 – add) ─────────────────

    [HttpPost("work-experience")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddWorkExperience(
        [FromForm] string jobTitle,
        [FromForm] string company,
        [FromForm] string employmentType,
        [FromForm] string? location,
        [FromForm] string startDate,
        [FromForm] string? endDate,
        [FromForm] bool currentlyWorking,
        [FromForm] string? responsibilities,
        [FromForm] string? fileName,
        [FromForm] string? onboardingId,
        IFormFile? file)
    {
        try
        {
            var userId = GetCurrentUserId();
            var request = new OnboardingWorkExperienceRequest
            {
                JobTitle = jobTitle,
                Company = company,
                EmploymentType = employmentType,
                Location = location,
                StartDate = startDate,
                EndDate = endDate,
                CurrentlyWorking = currentlyWorking,
                Responsibilities = responsibilities,
                FileName = fileName
            };

            var result = await _service.AddWorkExperienceAsync(userId, request, file);
            return Ok(new { success = true, message = "Work experience added.", data = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding work experience");
            return StatusCode(500, new { success = false, message = "Failed to save work experience." });
        }
    }

    // ── PUT /api/onboarding/work-experience/{id}  (Step 3 – update) ──────────

    [HttpPut("work-experience/{id:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateWorkExperience(
        Guid id,
        [FromForm] string jobTitle,
        [FromForm] string company,
        [FromForm] string employmentType,
        [FromForm] string? location,
        [FromForm] string startDate,
        [FromForm] string? endDate,
        [FromForm] bool currentlyWorking,
        [FromForm] string? responsibilities,
        [FromForm] string? fileName,
        IFormFile? file)
    {
        try
        {
            var userId = GetCurrentUserId();
            var request = new OnboardingWorkExperienceRequest
            {
                ExperienceId = id,
                JobTitle = jobTitle,
                Company = company,
                EmploymentType = employmentType,
                Location = location,
                StartDate = startDate,
                EndDate = endDate,
                CurrentlyWorking = currentlyWorking,
                Responsibilities = responsibilities,
                FileName = fileName
            };

            var updated = await _service.UpdateWorkExperienceAsync(userId, id, request, file);
            if (!updated)
                return NotFound(new { success = false, message = "Work experience record not found." });

            return Ok(new { success = true, message = "Work experience updated." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating work experience {Id}", id);
            return StatusCode(500, new { success = false, message = "Failed to update work experience." });
        }
    }

    // ── DELETE /api/onboarding/work-experience/{id} ───────────────────────────

    [HttpDelete("work-experience/{id:guid}")]
    public async Task<IActionResult> DeleteWorkExperience(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var deleted = await _service.DeleteWorkExperienceAsync(userId, id);
            if (!deleted)
                return NotFound(new { success = false, message = "Work experience record not found." });

            return Ok(new { success = true, message = "Work experience deleted." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting work experience {Id}", id);
            return StatusCode(500, new { success = false, message = "Failed to delete work experience." });
        }
    }

    // ── PUT /api/onboarding/accept-policies  (Step 5) ────────────────────────

    [HttpPut("accept-policies")]
    public async Task<IActionResult> AcceptPolicies([FromBody] AcceptPoliciesRequest body)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _service.AcceptPoliciesAsync(
                userId, body.PolicyAccepted, body.DocumentNotes);
            return Ok(new { success, message = "Policy status saved." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving policy acceptance");
            return StatusCode(500, new { success = false, message = "Failed to save policy acceptance." });
        }
    }

    // ── PUT /api/onboarding/step ──────────────────────────────────────────────

    [HttpPut("step")]
    public async Task<IActionResult> UpdateStep([FromBody] UpdateStepRequest body)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _service.UpdateCurrentStepAsync(userId, body.Step);
            return Ok(new { success, message = "Step updated." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating step");
            return StatusCode(500, new { success = false, message = "Failed to update step." });
        }
    }

    // ── GET /api/onboarding/bank-details ─────────────────────────────────────

    [HttpGet("bank-details")]
    public async Task<IActionResult> GetBankDetails()
    {
        try
        {
            var userId = GetCurrentUserId();
            var dto = await _service.GetBankDetailsAsync(userId);

            if (dto is null)
                return NotFound(new { success = false, message = "No bank details found." });

            return Ok(new { success = true, data = dto });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bank details");
            return StatusCode(500, new { success = false, message = "Failed to retrieve bank details." });
        }
    }

    // ── POST /api/onboarding/bank-details ────────────────────────────────────

    [HttpPost("bank-details")]
    public async Task<IActionResult> SaveBankDetails(
        [FromBody] OnboardingBankDetailsRequest request)
    {
        try
        {
            if (request is null)
                return BadRequest(new { success = false, message = "Request body is required." });

            var userId = GetCurrentUserId();
            var (success, message) = await _service.SaveBankDetailsAsync(userId, request);
            return Ok(new { success, message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving bank details");
            return StatusCode(500, new { success = false, message = "Failed to save bank details." });
        }
    }

    // ── PUT /api/onboarding/bank-details ─────────────────────────────────────

    [HttpPut("bank-details")]
    public async Task<IActionResult> UpdateBankDetails(
        [FromBody] OnboardingBankDetailsRequest request)
    {
        try
        {
            if (request is null)
                return BadRequest(new { success = false, message = "Request body is required." });

            var userId = GetCurrentUserId();
            var (success, message) = await _service.SaveBankDetailsAsync(userId, request);
            return Ok(new { success, message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bank details");
            return StatusCode(500, new { success = false, message = "Failed to update bank details." });
        }
    }

    // ── DELETE /api/onboarding/bank-details/{id?} ────────────────────────────

    [HttpDelete("bank-details/{bankDetailsId?}")]
    public async Task<IActionResult> DeleteBankDetails(string? bankDetailsId = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var deleted = await _service.DeleteBankDetailsAsync(userId, bankDetailsId);
            if (!deleted)
                return NotFound(new { success = false, message = "Bank details not found." });

            return Ok(new { success = true, message = "Bank details deleted." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bank details");
            return StatusCode(500, new { success = false, message = "Failed to delete bank details." });
        }
    }

    // ── POST /api/onboarding  (Final submit – Step 6) ────────────────────────

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubmitOnboarding(
        [FromForm] string payload,
        IFormFile? photo,
        IFormFile? resume)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payload))
                return BadRequest(new { success = false, message = "Payload is required." });

            var request = JsonSerializer.Deserialize<OnboardingSubmitRequest>(
                payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request is null)
                return BadRequest(new { success = false, message = "Invalid payload format." });

            var userId = GetCurrentUserId();
            var orgId = GetCurrentOrganizationId();
            var success = await _service.FinalSubmitAsync(userId, orgId, request, photo, resume);

            return Ok(new { success, message = "Onboarding completed successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting onboarding");
            return StatusCode(500, new { success = false, message = "Failed to submit onboarding." });
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var id))
            throw new UnauthorizedAccessException("User ID not found in token.");
        return id;
    }

    private Guid GetCurrentOrganizationId()
    {
        var claim = User.FindFirstValue("organizationId");
        if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var id))
            throw new UnauthorizedAccessException("Organization ID not found in token.");
        return id;
    }

    /// <summary>
    /// Builds a <see cref="PersonalInfoFieldConfig"/> by reading the org's
    /// field_configurations rows (section = "personalInformation").
    /// Falls back to requiring all fields if no config rows exist.
    /// </summary>
    private async Task<PersonalInfoFieldConfig> BuildPersonalInfoFieldConfig(
        Guid organizationId)
    {
        var records = await _configRepo.GetOnBoadingConfigurations(organizationId);

        var lookup = records
            .Where(r => string.Equals(
                r.Section, "personalInformation",
                StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                r => r.FieldKey,
                r => r,
                StringComparer.OrdinalIgnoreCase);

        // Default = required when no config row exists (safe fallback)
        bool Req(string key) =>
            lookup.TryGetValue(key, out var row) ? row.Required : true;

        return new PersonalInfoFieldConfig
        {
            NameRequired = Req(OnboardingFieldKeys.Name),
            EmailRequired = Req(OnboardingFieldKeys.Email),
            PhoneRequired = Req(OnboardingFieldKeys.Phone),
            DepartmentRequired = Req(OnboardingFieldKeys.Department),
            GenderRequired = Req(OnboardingFieldKeys.Gender),
            NationalityRequired = Req(OnboardingFieldKeys.Nationality),
            IdNumberRequired = Req(OnboardingFieldKeys.IdNumber),
            PhotoRequired = Req(OnboardingFieldKeys.Photo),
            ResumeRequired = Req(OnboardingFieldKeys.Resume),
        };
    }

    /// <summary>
    /// Validates personal-info fields against the loaded config.
    /// Returns a list of error messages; empty list = valid.
    /// Zero hardcoded required fields — every check is config-driven.
    /// </summary>
    private static List<string> ValidatePersonalInfo(
        string? fullName,
        string? email,
        string? phone,
        string? department,
        string? gender,
        string? nationality,
        string? idNumber,
        string? photoIndicator,
        string? resumeIndicator,
        PersonalInfoFieldConfig cfg)
    {
        var errors = new List<string>();

        if (cfg.NameRequired && string.IsNullOrWhiteSpace(fullName))
            errors.Add("Full name is required.");
        if (cfg.EmailRequired && string.IsNullOrWhiteSpace(email))
            errors.Add("Email address is required.");
        if (cfg.PhoneRequired && string.IsNullOrWhiteSpace(phone))
            errors.Add("Phone number is required.");
        if (cfg.DepartmentRequired && string.IsNullOrWhiteSpace(department))
            errors.Add("Department is required.");
        if (cfg.GenderRequired && string.IsNullOrWhiteSpace(gender))
            errors.Add("Gender is required.");
        if (cfg.NationalityRequired && string.IsNullOrWhiteSpace(nationality))
            errors.Add("Nationality is required.");
        if (cfg.IdNumberRequired && string.IsNullOrWhiteSpace(idNumber))
            errors.Add("National ID / CNIC is required.");
        if (cfg.PhotoRequired && string.IsNullOrWhiteSpace(photoIndicator))
            errors.Add("Profile photo is required.");
        if (cfg.ResumeRequired && string.IsNullOrWhiteSpace(resumeIndicator))
            errors.Add("Resume / CV is required.");

        return errors;
    }
}

// ── Inline request records ─────────────────────────────────────────────────────
public record AcceptPoliciesRequest(bool PolicyAccepted, string? DocumentNotes);
public record UpdateStepRequest(int Step);