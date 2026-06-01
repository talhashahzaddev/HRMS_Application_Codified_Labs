// ============================================================
// FILE: Services/Interfaces/IOnboardingService.cs
// ============================================================
using HRMS.API.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace HRMS.API.Services.Interfaces;

public interface IOnboardingService
{
    // ── Step 1 – Personal Info ────────────────────────────────────────────────
    Task<(bool success, string onboardingId, string message)> SavePersonalInfoAsync(
        Guid userId,
        Guid organizationId,
        OnboardingPersonalInfoRequest request,
        IFormFile? photo,
        IFormFile? resume);

    // ── Step 2 – Education ────────────────────────────────────────────────────
    Task<OnboardingEducationDto> AddEducationAsync(
        Guid userId,
        OnboardingEducationRequest request,
        string? fileUrl);

    Task<bool> UpdateEducationAsync(
        Guid userId,
        Guid educationId,
        OnboardingEducationRequest request,
        IFormFile? file);

    Task<bool> DeleteEducationAsync(Guid userId, Guid educationId);

    // ── Step 3 – Work Experience ──────────────────────────────────────────────
    Task<OnboardingWorkExperienceDto> AddWorkExperienceAsync(
        Guid userId,
        OnboardingWorkExperienceRequest request,
        IFormFile? file);

    Task<bool> UpdateWorkExperienceAsync(
        Guid userId,
        Guid experienceId,
        OnboardingWorkExperienceRequest request,
        IFormFile? file);

    Task<bool> DeleteWorkExperienceAsync(Guid userId, Guid experienceId);

    // ── Step 4 – Bank Details ─────────────────────────────────────────────────
    Task<OnboardingBankDetailsDto?> GetBankDetailsAsync(Guid userId);

    Task<(bool success, string message)> SaveBankDetailsAsync(
        Guid userId,
        OnboardingBankDetailsRequest request);

    Task<bool> DeleteBankDetailsAsync(Guid userId, string? bankDetailsId);

    // ── Step 5 – Policy Acceptance ────────────────────────────────────────────
    Task<bool> AcceptPoliciesAsync(
        Guid userId,
        bool accepted,
        string? documentNotes);

    // ── Step 6 – Final Submit ─────────────────────────────────────────────────
    Task<bool> FinalSubmitAsync(
        Guid userId,
        Guid organizationId,
        OnboardingSubmitRequest request,
        IFormFile? photo,
        IFormFile? resume);

    // ── Status ────────────────────────────────────────────────────────────────
    Task<OnboardingStatusResponse?> GetMyStatusAsync(Guid userId, Guid organizationId);

    // ── Step tracker ──────────────────────────────────────────────────────────
    Task<bool> UpdateCurrentStepAsync(Guid userId, int step);
}
// IPolicyService.cs
// ============================================================
// FILE: Services/Interfaces/IPolicyService.cs
// ============================================================

public interface IPolicyService
{
    Task<List<PolicyDto>> GetPoliciesAsync(Guid organizationId, bool isSuperAdmin);

    Task<PolicyDto?> GetByIdAsync(Guid policyId, Guid organizationId);

    /// <summary>
    /// Creates a new policy. File (if any) has already been uploaded separately;
    /// its public URL is provided in request.FileUrl.
    /// </summary>
    Task<PolicyDto> CreateAsync(Guid organizationId, CreatePolicyRequest request);

    /// <summary>
    /// Updates an existing policy. If request.FileUrl is non-null, the file was
    /// replaced; otherwise the existing DB URL is kept unchanged.
    /// </summary>
    Task<(bool found, bool updated)> UpdateAsync(
        Guid organizationId,
        Guid policyId,
        UpdatePolicyRequest request);

    Task<bool> DeleteAsync(Guid policyId, Guid organizationId);
}