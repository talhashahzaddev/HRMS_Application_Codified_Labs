using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class PayRollService : IPayRollService
{
    private readonly IPayRollRepository _payRollRepository;
    private readonly ILogger<PayRollService> _logger;

    public PayRollService(IPayRollRepository payRollRepository, ILogger<PayRollService> logger)
    {
        _payRollRepository = payRollRepository;
        _logger = logger;
    }

    public async Task<ServiceResponse<IEnumerable<PayRollRuleDto>>> GetAllRulesAsync(Guid organizationId)
    {
        try
        {
            _logger.LogInformation("Getting all payroll rules for organization: {OrganizationId}", organizationId);

            if (organizationId == Guid.Empty)
            {
                return new ServiceResponse<IEnumerable<PayRollRuleDto>>
                {
                    Success = false,
                    Message = "Organization ID is required"
                };
            }

            var data = await _payRollRepository.GetAllRulesAsync(organizationId);

            return new ServiceResponse<IEnumerable<PayRollRuleDto>>
            {
                Success = true,
                Data = data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll rules for organization: {OrganizationId}", organizationId);
            return new ServiceResponse<IEnumerable<PayRollRuleDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll rules"
            };
        }
    }
}
