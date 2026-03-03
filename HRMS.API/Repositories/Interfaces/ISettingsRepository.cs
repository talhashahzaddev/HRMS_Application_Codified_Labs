using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces;

public interface ISettingsRepository
{
    Task<string?> GetCurrencyAsync(Guid organizationId);
    Task<bool> UpdateCurrencyAsync(Guid organizationId, string currency);
    Task<int> GetManualEntryTimeLimitAsync(Guid organizationId);
    Task<bool> UpdateCareerPageAsync(Guid organizationId, string? careerBgImageUrl, string? careerHeaderText, string? careerDescription, string? logoUrl);
    Task<CareerPageDto?> GetCareerPageAsync(Guid organizationId);
}










