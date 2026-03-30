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

    public async Task<ServiceResponse<IEnumerable<OvertimeRuleDto>>> GetActiveOvertimeRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActiveOvertimeRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<OvertimeRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active overtime rules");
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

    public async Task<ServiceResponse<PagedResult<OvertimeEntryDto>>> GetAllOvertimeEntriesAsync(Guid organizationId, OvertimeEntryFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllOvertimeEntriesAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<OvertimeEntryDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overtime entries");
            return new ServiceResponse<PagedResult<OvertimeEntryDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<OvertimeEntryDto>> GetOvertimeEntryByIdAsync(Guid overtimeId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetOvertimeEntryByIdAsync(overtimeId, organizationId);
            if (result == null)
            {
                return new ServiceResponse<OvertimeEntryDto> { Success = false, Message = "Overtime entry not found." };
            }
            return new ServiceResponse<OvertimeEntryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overtime entry");
            return new ServiceResponse<OvertimeEntryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<OvertimeEntryDto>> CreateOvertimeEntryAsync(Guid organizationId, CreateOvertimeEntryDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateOvertimeEntryAsync(organizationId, dto);
            return new ServiceResponse<OvertimeEntryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating overtime entry");
            return new ServiceResponse<OvertimeEntryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<OvertimeEntryDto>> UpdateOvertimeEntryAsync(Guid overtimeId, Guid organizationId, UpdateOvertimeEntryDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateOvertimeEntryAsync(overtimeId, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<OvertimeEntryDto> { Success = false, Message = "Overtime entry not found." };
            }
            return new ServiceResponse<OvertimeEntryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating overtime entry");
            return new ServiceResponse<OvertimeEntryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteOvertimeEntryAsync(Guid overtimeId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteOvertimeEntryAsync(overtimeId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Entry not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting overtime entry");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Payroll Periods
    public async Task<ServiceResponse<PayrollPeriodPagedResult>> GetAllPayrollPeriodsAsync(Guid organizationId, PayrollPeriodFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllPayrollPeriodsAsync(organizationId, filter);
            return new ServiceResponse<PayrollPeriodPagedResult> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll periods");
            return new ServiceResponse<PayrollPeriodPagedResult> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PayrollPeriodDto>> GetPayrollPeriodByIdAsync(Guid periodId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetPayrollPeriodByIdAsync(periodId, organizationId);
            if (result == null)
            {
                return new ServiceResponse<PayrollPeriodDto> { Success = false, Message = "Payroll period not found." };
            }
            return new ServiceResponse<PayrollPeriodDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll period by ID");
            return new ServiceResponse<PayrollPeriodDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PayrollPeriodDto>> CreatePayrollPeriodAsync(Guid organizationId, Guid employeeId, CreatePayrollPeriodDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreatePayrollPeriodAsync(organizationId, employeeId, dto);
            return new ServiceResponse<PayrollPeriodDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payroll period");
            return new ServiceResponse<PayrollPeriodDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PayrollPeriodDto>> UpdatePayrollPeriodAsync(Guid periodId, Guid organizationId, UpdatePayrollPeriodDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdatePayrollPeriodAsync(periodId, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<PayrollPeriodDto> { Success = false, Message = "Payroll period not found." };
            }
            return new ServiceResponse<PayrollPeriodDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payroll period");
            return new ServiceResponse<PayrollPeriodDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeletePayrollPeriodAsync(Guid periodId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeletePayrollPeriodAsync(periodId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Period not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payroll period");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }
}
