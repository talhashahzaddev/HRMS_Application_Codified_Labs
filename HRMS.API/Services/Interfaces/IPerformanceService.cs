using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IPerformanceService
{
    Task<AppraisalCycleDto> CreateAppraisalCycleAsync(string cycleName, DateTime startDate, DateTime endDate, Guid organizationId, string? description = null);
    Task<List<AppraisalCycleDto>> GetAppraisalCyclesAsync(Guid organizationId);
    Task<EmployeeAppraisalDto> CreateEmployeeAppraisalAsync(CreateAppraisalRequest request, Guid reviewerId);
    Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsAsync(Guid cycleId, Guid? employeeId = null);
    Task<List<SkillSetDto>> GetSkillSetsAsync(Guid organizationId);
    Task<List<EmployeeSkillDto>> GetEmployeeSkillsAsync(Guid employeeId);
    Task<PerformanceSummaryDto> GetPerformanceSummaryAsync(Guid organizationId, Guid? cycleId = null);
    Task<List<TopPerformerDto>> GetTopPerformersAsync(Guid organizationId, int count = 10);
}
