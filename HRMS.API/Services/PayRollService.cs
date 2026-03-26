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

    public async Task<ServiceResponse<PayRollRulesGroupedDto>> GetAllRulesAsync(Guid organizationId)
    {
        try
        {
            _logger.LogInformation("Getting all payroll rules for organization: {OrganizationId}", organizationId);

            if (organizationId == Guid.Empty)
            {
                return new ServiceResponse<PayRollRulesGroupedDto>
                {
                    Success = false,
                    Message = "Organization ID is required"
                };
            }

            var data = await _payRollRepository.GetAllRulesAsync(organizationId);

            return new ServiceResponse<PayRollRulesGroupedDto>
            {
                Success = true,
                Data = data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll rules for organization: {OrganizationId}", organizationId);
            return new ServiceResponse<PayRollRulesGroupedDto>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll rules"
            };
        }
    }

    public async Task<ServiceResponse<IEnumerable<OvertimeRuleDto>>> GetAllOvertimeRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAllOvertimeRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<OvertimeRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overtime rules");
            return new ServiceResponse<IEnumerable<OvertimeRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<OvertimeRuleDto>> GetOvertimeRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetOvertimeRuleByIdAsync(ruleId, organizationId);
            if (result == null)
            {
                return new ServiceResponse<OvertimeRuleDto> { Success = false, Message = "Overtime rule not found." };
            }
            return new ServiceResponse<OvertimeRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overtime rule");
            return new ServiceResponse<OvertimeRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<OvertimeRuleDto>> CreateOvertimeRuleAsync(Guid organizationId, CreateOvertimeRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateOvertimeRuleAsync(organizationId, dto);
            return new ServiceResponse<OvertimeRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating overtime rule");
            return new ServiceResponse<OvertimeRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<OvertimeRuleDto>> UpdateOvertimeRuleAsync(Guid ruleId, Guid organizationId, UpdateOvertimeRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateOvertimeRuleAsync(ruleId, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<OvertimeRuleDto> { Success = false, Message = "Overtime rule not found." };
            }
            return new ServiceResponse<OvertimeRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating overtime rule");
            return new ServiceResponse<OvertimeRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> ToggleOvertimeRuleStatusAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.ToggleOvertimeRuleStatusAsync(ruleId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Status toggled successfully." : "Rule not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling overtime rule status");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> ToggleOvertimeRuleDeletionAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.ToggleOvertimeRuleDeletionAsync(ruleId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deletion status toggled successfully." : "Rule not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling overtime rule deletion");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }
}
