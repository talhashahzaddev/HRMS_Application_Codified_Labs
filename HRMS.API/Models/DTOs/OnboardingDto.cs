// ============================================================
// FILE: Models/DTOs/OnboardingDTOs.cs
// ============================================================
using Microsoft.AspNetCore.Http;

namespace HRMS.API.Models.DTOs;

// ─────────────────────────────────────────────────────────────
// REQUEST DTOs
// ─────────────────────────────────────────────────────────────

/// <summary>Step 1 – Personal info (upsert, no create-duplicate).</summary>
public class OnboardingPersonalInfoRequest
{
    public string FullName { get; set; } = string.Empty;  // split → FirstName + LastName
    public string JobTitle { get; set; } = string.Empty;  // stored in onboarding table
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;  // CNIC
}

/// <summary>Step 2 – Education entry (add/update).</summary>
public class OnboardingEducationRequest
{
    public Guid? EducationId { get; set; }   // null = new record
    public string Degree { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string? Gpa { get; set; }
    public string? FileName { get; set; }
    public string? Description { get; set; }
}

/// <summary>Step 3 – Work experience entry (add/update).</summary>
public class OnboardingWorkExperienceRequest
{
    public Guid? ExperienceId { get; set; }  // null = new record
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string? EndDate { get; set; }
    public bool CurrentlyWorking { get; set; }
    public string? Responsibilities { get; set; }
    public string? FileName { get; set; }
}

/// <summary>Full onboarding submit payload (Step 6 final).</summary>
public class OnboardingSubmitRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string? ResumeFileName { get; set; }
    public bool PolicyAccepted { get; set; }
    public string? DocumentNotes { get; set; }
    public List<string> SelectedModuleIds { get; set; } = new();
    public List<OnboardingEducationRequest> Education { get; set; } = new();
    public List<OnboardingWorkExperienceRequest> WorkExperience { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────
// RESPONSE DTOs
// ─────────────────────────────────────────────────────────────

public class OnboardingStatusResponse
{
    public Guid OnboardingId { get; set; }
    public Guid EmployeeId { get; set; }
    public int CurrentStep { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsPolicyAccepted { get; set; }
    public decimal CompletionRate { get; set; }
    public string? DocumentNotes { get; set; }

    public OnboardingPersonalInfoDto? PersonalInfo { get; set; }
    public List<OnboardingEducationDto> EducationList { get; set; } = new();
    public List<OnboardingWorkExperienceDto> WorkExperienceList { get; set; } = new();
}

public class OnboardingPersonalInfoDto
{
    public Guid OnboardingId { get; set; }
    public Guid EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? ResumeUrl { get; set; }
    public string? ResumeFileName { get; set; }
    public int CurrentStep { get; set; }
}

public class OnboardingEducationDto
{
    public Guid EducationId { get; set; }
    public Guid OnboardingId { get; set; }
    public string Degree { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string? Gpa { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public string? Description { get; set; }
}

public class OnboardingWorkExperienceDto
{
    public Guid ExperienceId { get; set; }
    public Guid OnboardingId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string? EndDate { get; set; }
    public bool CurrentlyWorking { get; set; }
    public string? Responsibilities { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
}
public class OnboardingSession
{
    public Guid OnboardingId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid OrganizationId { get; set; }
    public int CurrentStep { get; set; } = 1;
    public string Status { get; set; } = "in_progress"; // in_progress | completed
    public bool IsPolicyAccepted { get; set; }
    public string? DocumentNotes { get; set; }
    public string? PhotoUrl { get; set; }
    public string? ResumeUrl { get; set; }
    public string? ResumeFileName { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? IdNumber { get; set; }   // CNIC stored here for onboarding
    public decimal CompletionRate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class OnboardingEducation
{
    public Guid EducationId { get; set; }
    public Guid OnboardingId { get; set; }
    public string Degree { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string? Gpa { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class OnboardingWorkExperience
{
    public Guid ExperienceId { get; set; }
    public Guid OnboardingId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string? EndDate { get; set; }
    public bool CurrentlyWorking { get; set; }
    public string? Responsibilities { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ── Onboarding Configuration DTOs ──────────────────────────────────────────

public class FieldConfigDto
{
    public bool Enabled { get; set; }
    public bool Required { get; set; }
}

public class OnboardingFieldConfigRequest
{
    public Dictionary<string, Dictionary<string, FieldConfigDto>> Sections { get; set; } = new();
    // Shape: { "personalInformation": { "fullName": { enabled: true, required: true }, ... }, ... }
}

public class OnboardingFieldConfigResponse
{
    public Guid OrganizationId { get; set; }
    public Dictionary<string, Dictionary<string, FieldConfigDto>> Sections { get; set; } = new();
}
public class FieldConfigRecord
{
    public Guid OrganizationId { get; set; }
    public string Section { get; set; } = string.Empty;
    public string FieldKey { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool Required { get; set; }
}
// ── Policy DTOs ──────────────────────────────────────────────────────────────
// ============================================================
// FILE: Models/DTOs/PolicyDTOs.cs  (replaces the policy section in OnboardingDTOs.cs)
// ============================================================
// Add these to your existing OnboardingDTOs.cs (replace the old Policy DTOs section)

// ============================================================
// FILE: Models/DTOs/PolicyDTOs.cs
// ============================================================


// ── Policy DTOs ──────────────────────────────────────────────────────────────

/// <summary>
/// Field-level toggle sent by the frontend so the Super Admin can mark
/// individual policy-form fields as required or optional at will.
/// </summary>
public class PolicyFieldToggle
{
    public bool NameRequired { get; set; } = false;
    public bool CategoryRequired { get; set; } = false;
    public bool ContentRequired { get; set; } = false;
    public bool AttachmentRequired { get; set; } = false;
}

/// <summary>
/// Payload for POST /api/policies/Create/Policy.
///
/// File upload flow:
///   Frontend calls POST /api/uploads/files first → gets back a public URL.
///   That URL is passed here as FileUrl (plain string, not IFormFile).
///   This keeps file upload and policy save as two clean, separate steps.
/// </summary>
public class CreatePolicyRequest
{
    public string PolicyName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PolicyContent { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = false;

    /// <summary>Serialised JSON of PolicyFieldToggle.</summary>
    public string? FieldToggles { get; set; }

    /// <summary>
    /// Full public URL returned by POST /api/uploads/files.
    /// Stored directly in the DB — no further processing needed.
    /// </summary>
    public string? FileUrl { get; set; }

    /// <summary>Original filename for display (e.g. "employee-handbook.pdf").</summary>
    public string? FileName { get; set; }
}

/// <summary>
/// Payload for PUT /api/policies/{id}.
/// Same file-URL pattern as CreatePolicyRequest.
/// </summary>
public class UpdatePolicyRequest
{
    public string PolicyName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PolicyContent { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = false;
    public string? FieldToggles { get; set; }

    /// <summary>
    /// New public URL if the file was replaced; null to keep the existing one.
    /// </summary>
    public string? FileUrl { get; set; }

    /// <summary>New filename if the file was replaced; null to keep existing.</summary>
    public string? FileName { get; set; }
}

/// <summary>
/// Full policy record returned to all callers (Super Admin and Employee).
/// Both FileUrl and FileName are always included so the employee-side UI
/// can display a View/Download button without any extra API calls.
/// </summary>
public class PolicyDto
{
    public Guid PolicyId { get; set; }
    public Guid OrganizationId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PolicyContent { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public string? FileUrl { get; set; }   // Full public URL — ready to open in browser
    public string? FileName { get; set; }   // Original filename for display
    public string? FieldToggles { get; set; }   // Serialised PolicyFieldToggle JSON
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}