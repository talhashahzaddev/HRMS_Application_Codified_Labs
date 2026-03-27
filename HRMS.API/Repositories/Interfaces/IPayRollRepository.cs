using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces;

public interface IPayRollRepository
{
    Task<PayRollRulesGroupedDto> GetAllRulesAsync(Guid organizationId);
    
    // Overtime Rules
    Task<IEnumerable<OvertimeRuleDto>> GetAllOvertimeRulesAsync(Guid organizationId);
    Task<IEnumerable<OvertimeRuleDto>> GetActiveOvertimeRulesAsync(Guid organizationId);
    Task<OvertimeRuleDto?> GetOvertimeRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<OvertimeRuleDto> CreateOvertimeRuleAsync(Guid organizationId, CreateOvertimeRuleDto dto);
    Task<OvertimeRuleDto?> UpdateOvertimeRuleAsync(Guid ruleId, Guid organizationId, UpdateOvertimeRuleDto dto);
    Task<bool> ToggleOvertimeRuleStatusAsync(Guid ruleId, Guid organizationId); // Active/Inactive
    Task<bool> ToggleOvertimeRuleDeletionAsync(Guid ruleId, Guid organizationId); // Delete/Restore

    // Overtime Entries
    Task<PagedResult<OvertimeEntryDto>> GetAllOvertimeEntriesAsync(Guid organizationId, OvertimeEntryFilterDto filter);
    Task<OvertimeEntryDto?> GetOvertimeEntryByIdAsync(Guid overtimeId, Guid organizationId);
    Task<OvertimeEntryDto> CreateOvertimeEntryAsync(Guid organizationId, CreateOvertimeEntryDto dto);
    Task<OvertimeEntryDto?> UpdateOvertimeEntryAsync(Guid overtimeId, Guid organizationId, UpdateOvertimeEntryDto dto);
    Task<bool> DeleteOvertimeEntryAsync(Guid overtimeId, Guid organizationId);
}
