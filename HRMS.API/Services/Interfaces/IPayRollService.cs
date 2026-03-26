using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IPayRollService
{
    Task<ServiceResponse<IEnumerable<PayRollRuleDto>>> GetAllRulesAsync(Guid organizationId);
}
