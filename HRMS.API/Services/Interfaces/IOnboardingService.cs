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
}
