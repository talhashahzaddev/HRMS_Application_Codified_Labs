// ============================================================
// FILE: Services/Interfaces/IOnboardingService.cs
// ============================================================
using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IOnboardingService
{
    // Step 1 – personal info (PUT /onboarding/personal-info)
    Task<(bool success, string onboardingId, string message)> SavePersonalInfoAsync(
        Guid userId, Guid organizationId,
        OnboardingPersonalInfoRequest request,
        IFormFile? photo, IFormFile? resume);

    // Step 2 – education
    Task<OnboardingEducationDto> AddEducationAsync(Guid userId, OnboardingEducationRequest request, IFormFile? file);
    Task<bool> UpdateEducationAsync(Guid userId, Guid educationId, OnboardingEducationRequest request, IFormFile? file);
    Task<bool> DeleteEducationAsync(Guid userId, Guid educationId);

    // Step 3 – work experience
    Task<OnboardingWorkExperienceDto> AddWorkExperienceAsync(Guid userId, OnboardingWorkExperienceRequest request, IFormFile? file);
    Task<bool> UpdateWorkExperienceAsync(Guid userId, Guid experienceId, OnboardingWorkExperienceRequest request, IFormFile? file);
    Task<bool> DeleteWorkExperienceAsync(Guid userId, Guid experienceId);

    // Step 4/5/6 – verification + policies + final submit
    Task<bool> AcceptPoliciesAsync(Guid userId, bool accepted, string? documentNotes);
    Task<bool> FinalSubmitAsync(Guid userId, Guid organizationId, OnboardingSubmitRequest request, IFormFile? photo, IFormFile? resume);
    Task<bool> UpdateCurrentStepAsync(Guid userId, int step);

    // GET /onboarding/my-status
    Task<OnboardingStatusResponse?> GetMyStatusAsync(Guid userId, Guid organizationId);
    // In Services/Interfaces/IOnboardingService.cs
    // Add these two methods to the existing interface:

    Task<OnboardingBankDetailsDto?> GetBankDetailsAsync(Guid userId);

    Task<(bool success, string message)> SaveBankDetailsAsync(
        Guid userId,
        OnboardingBankDetailsRequest request);
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