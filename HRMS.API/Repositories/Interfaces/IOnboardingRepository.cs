// ============================================================
// FILE: Repositories/Interfaces/IOnboardingRepository.cs
// ============================================================
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IOnboardingRepository
{
    // ── Session (master record) ──────────────────────────────
    /// <summary>Returns existing session or null. Never creates.</summary>
    Task<OnboardingSession?> GetSessionByEmployeeIdAsync(Guid employeeId);

    /// <summary>Creates a fresh session row for this employee.</summary>
    Task<OnboardingSession> CreateSessionAsync(OnboardingSession session);

    /// <summary>Updates any fields on an existing session.</summary>
    Task<bool> UpdateSessionAsync(OnboardingSession session);

    // ── Personal Info (writes back to employee.employees) ────
    Task<bool> UpsertPersonalInfoAsync(
        Guid employeeId,
        string fullName,
        string phone,
        string gender,
        string nationality,
        string idNumber,
        string? photoUrl,
        string? resumeUrl,
        string? resumeFileName,
        string? jobTitle,
        string? department);

    // ── Education ────────────────────────────────────────────
    Task<List<OnboardingEducation>> GetEducationBySessionAsync(Guid onboardingId);
    Task<OnboardingEducation> AddEducationAsync(OnboardingEducation record);
    Task<bool> UpdateEducationAsync(OnboardingEducation record);
    Task<bool> DeleteEducationAsync(Guid educationId, Guid onboardingId);

    // ── Work Experience ──────────────────────────────────────
    Task<List<OnboardingWorkExperience>> GetWorkExperienceBySessionAsync(Guid onboardingId);
    Task<OnboardingWorkExperience> AddWorkExperienceAsync(OnboardingWorkExperience record);
    Task<bool> UpdateWorkExperienceAsync(OnboardingWorkExperience record);
    Task<bool> DeleteWorkExperienceAsync(Guid experienceId, Guid onboardingId);

    // ── Bank Details ─────────────────────────────────────────
    //Task<OnboardingBankDetails?> GetBankDetailsBySessionAsync(Guid onboardingId);
    //Task<OnboardingBankDetails> AddBankDetailsAsync(OnboardingBankDetails record);
    //Task<bool> UpdateBankDetailsAsync(OnboardingBankDetails record);

    // ── Full status for GET /my-status ───────────────────────
    Task<OnboardingStatusResponse?> GetFullStatusAsync(Guid employeeId, Guid organizationId);
    // In Repositories/Interfaces/IOnboardingRepository.cs
    // Add these two methods to the existing interface:

    Task<OnboardingBankDetails?> GetBankDetailsByOnboardingIdAsync(Guid onboardingId);

    /// <summary>
    /// INSERT ... ON CONFLICT (onboardingid) DO UPDATE
    /// Guarantees one row per session — safe to call on every save.
    /// </summary>
    Task<OnboardingBankDetails> UpsertBankDetailsAsync(OnboardingBankDetails record);

    Task<bool> DeleteBankDetailsAsync(Guid bankDetailsId);
}