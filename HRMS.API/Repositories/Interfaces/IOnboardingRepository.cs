// ============================================================
// FILE: Repositories/Interfaces/IOnboardingRepository.cs
// ============================================================
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IOnboardingRepository
{
    // ── Session ───────────────────────────────────────────────────────────────
    Task<OnboardingSession?> GetSessionByEmployeeIdAsync(Guid employeeId);
    Task<OnboardingSession> CreateSessionAsync(OnboardingSession session);
    Task<bool> UpdateSessionAsync(OnboardingSession session);

    // ── Personal Info ─────────────────────────────────────────────────────────
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

    // ── Education ─────────────────────────────────────────────────────────────
    Task<List<OnboardingEducation>> GetEducationBySessionAsync(Guid onboardingId);
    Task<OnboardingEducation> AddEducationAsync(OnboardingEducation record);
    Task<bool> UpdateEducationAsync(OnboardingEducation record);
    Task<bool> DeleteEducationAsync(Guid educationId, Guid onboardingId);

    // ── Work Experience ───────────────────────────────────────────────────────
    Task<List<OnboardingWorkExperience>> GetWorkExperienceBySessionAsync(Guid onboardingId);
    Task<OnboardingWorkExperience> AddWorkExperienceAsync(OnboardingWorkExperience record);
    Task<bool> UpdateWorkExperienceAsync(OnboardingWorkExperience record);
    Task<bool> DeleteWorkExperienceAsync(Guid experienceId, Guid onboardingId);

    // ── Bank Details ──────────────────────────────────────────────────────────
    Task<OnboardingBankDetails?> GetBankDetailsByOnboardingIdAsync(Guid onboardingId);
    Task<OnboardingBankDetails> UpsertBankDetailsAsync(OnboardingBankDetails record);
    Task<bool> DeleteBankDetailsAsync(Guid onboardingId);

    // ── Full Status ───────────────────────────────────────────────────────────
    Task<OnboardingStatusResponse?> GetFullStatusAsync(Guid employeeId, Guid organizationId);
}