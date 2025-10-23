using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IPerformanceRepository
{
    Task<AppraisalCycle> CreateAppraisalCycleAsync(Guid organizationId, string cycleName, DateTime startDate, DateTime endDate, string? description = null);
    Task<List<AppraisalCycleDto>> GetAppraisalCyclesAsync(Guid organizationId);
    Task<EmployeeAppraisal> CreateEmployeeAppraisalAsync(EmployeeAppraisal appraisal);
    Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsAsync(Guid cycleId, Guid? employeeId = null);
    Task<SkillSetDto?> CreateSkillSetAsync(CreateSkillSetRequest request, Guid organizationId);
    Task<List<SkillSetDto>> GetSkillSetsAsync(Guid organizationId);
    Task<EmployeeSkillDto?> CreateEmployeeSkillAsync(CreateEmployeeSkillRequest request);
    Task<List<EmployeeSkillDto>> GetEmployeeSkillsAsync(Guid employeeId);
    Task<PerformanceSummaryDto> GetPerformanceSummaryAsync(Guid organizationId, Guid? cycleId = null);
    Task<bool> UpdateAppraisalStatusAsync(Guid appraisalId, string status);
    Task<List<TopPerformerDto>> GetTopPerformersAsync(Guid organizationId, int count = 10);
    //Goals
    Task<EmployeeGoalDto> CreateEmployeeGoalAsync(CreateEmployeeGoalRequest request);

    Task<List<EmployeeGoalDto>> GetEmployeeGoalsAsync(Guid employeeId);

    Task<List<EmployeeGoalDto>> GetAllEmployeeGoalsAsync();

    Task<bool> GoalAsCompletedAsync(Guid goalId);

    //Kras
    Task<KRADto> CreateKRAAsync(CreateKRARequest request, Guid organizationId);
    Task<List<KRADto>> GetKRAsAsync(Guid organizationId);

    Task<KRADto> GetKraByIdAsync(Guid KraId);

    Task<KRADto> UpdatekraAsync(Guid kraId, UpdateKRARequest request);
    Task<bool> UpdateKRAStatusAsync(Guid kraId, bool isActive);



}
