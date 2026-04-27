// ============================================================
// FILE: Controllers/OnboardingController.cs
// ============================================================
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace HRMS.API.Controllers;

/// <summary>
/// Employee Onboarding API
///
/// All endpoints derive the current user from the JWT token (ClaimTypes.NameIdentifier).
/// The userId is resolved to an employeeId via EmployeeRepository.getemployeeid().
///
/// Flow:
///   GET  /api/onboarding/my-status           → Pre-fill UI with existing data
///   PUT  /api/onboarding/personal-info        → Step 1 (upsert, no duplicate)
///   POST /api/onboarding/education            → Step 2 – add education entry
///   PUT  /api/onboarding/education/{id}       → Step 2 – update education entry
///   DEL  /api/onboarding/education/{id}       → Step 2 – delete education entry
///   POST /api/onboarding/work-experience      → Step 3 – add experience entry
///   PUT  /api/onboarding/work-experience/{id} → Step 3 – update experience entry
///   DEL  /api/onboarding/work-experience/{id} → Step 3 – delete experience entry
///   PUT  /api/onboarding/accept-policies      → Step 5 – accept policies
///   PUT  /api/onboarding/step                 → Update current step tracker
///   POST /api/onboarding                      → Final submit (Step 6)
/// </summary>
[ApiController]
[Route("api/onboarding")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _service;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(IOnboardingService service, ILogger<OnboardingController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ── GET /api/onboarding/my-status ────────────────────────────────────────

    /// <summary>
    /// Returns full onboarding state for the logged-in employee.
    /// Frontend uses this to pre-fill all form steps on load.
    /// Returns 404 when no session exists yet (fresh employee).
    /// </summary>
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
    /// Save / update personal information. Accepts multipart/form-data so
    /// photo and resume files can be uploaded in the same request.
    /// On the first call a new onboarding session is created; subsequent calls
    /// UPDATE the same session — never inserting a duplicate.
    /// </summary>
    [HttpPut("personal-info")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SavePersonalInfo(
        [FromForm] string fullName,
        [FromForm] string jobTitle,
        [FromForm] string email,
        [FromForm] string phone,
        [FromForm] string department,
        [FromForm] string gender,
        [FromForm] string nationality,
        [FromForm] string idNumber,
        IFormFile? photo,
        IFormFile? resume)
    {
        try
        {
            var userId = GetCurrentUserId();
            var orgId = GetCurrentOrganizationId();

            var request = new OnboardingPersonalInfoRequest
            {
                FullName = fullName,
                JobTitle = jobTitle,
                Email = email,
                Phone = phone,
                Department = department,
                Gender = gender,
                Nationality = nationality,
                IdNumber = idNumber
            };

            var (success, onboardingId, message) = await _service.SavePersonalInfoAsync(
                userId, orgId, request, photo, resume);

            return Ok(new
            {
                success,
                message,
                data = new { onboardingId }
            });
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
        [FromForm] string? description,
        [FromForm] string? onboardingId,    // accepted but not used — session is resolved from JWT
        IFormFile? file)
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

            var result = await _service.AddEducationAsync(userId, request, file);
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

    // ── DELETE /api/onboarding/education/{id}  (Step 2 – delete) ─────────────

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

    // ── DELETE /api/onboarding/work-experience/{id}  (Step 3 – delete) ───────

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
            var success = await _service.AcceptPoliciesAsync(userId, body.PolicyAccepted, body.DocumentNotes);
            return Ok(new { success, message = "Policy status saved." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving policy acceptance");
            return StatusCode(500, new { success = false, message = "Failed to save policy acceptance." });
        }
    }

    // ── PUT /api/onboarding/step  (Step tracker) ─────────────────────────────

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

    // ── POST /api/onboarding  (Final submit – Step 6) ────────────────────────

    /// <summary>
    /// Final onboarding submission. Accepts multipart/form-data with
    /// a JSON 'payload' field plus optional photo/resume files.
    /// </summary>
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

    // ── helpers ───────────────────────────────────────────────────────────────

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
}

// ── Inline request bodies for simple PUT endpoints ────────────────────────────

public record AcceptPoliciesRequest(bool PolicyAccepted, string? DocumentNotes);
public record UpdateStepRequest(int Step);
