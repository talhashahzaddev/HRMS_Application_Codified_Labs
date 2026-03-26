using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces;

public interface IPayRollRepository
{
    Task<IEnumerable<PayRollRuleDto>> GetAllRulesAsync(Guid organizationId);
}
