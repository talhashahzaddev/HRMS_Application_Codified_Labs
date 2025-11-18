namespace HRMS.API.Repositories.Interfaces;

public interface ISettingsRepository
{
    Task<string?> GetCurrencyAsync(Guid organizationId);
    Task<bool> UpdateCurrencyAsync(Guid organizationId, string currency);
}





