using HRMS.API.Controllers;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IPerformanceRepository
{
    Task<MYPerformanceSummary> GetMyPerformanceSummaryAsync(Guid organizationId, Guid employeeId, Guid? cycleId);
    Task<Guid?> GetCurrentCycleid(Guid organizationId);
    Task<List<ManagerReviewDto>> GetAllManagerReviewsAsync(Guid organizationId, SearchManagersDto search);
    Task<List<HrReviewDto>> GetAllAppraisalAsync(Guid organizationId, Guid? employeeId, SearchAppraisalDto search);
    Task<List<HrReviewDto>> GetHrReviewsByEmployeeAsync(Guid organizationId, Guid employeeId);
    Task<HrReviewDto?> GetEmployeeAppraisal(Guid organizationId, Guid cycleId, Guid employeeId);
    Task<List<HrReviewDto>> GetHrReviewsAsync(Guid organizationId, Guid CurrentEmpId);
    Task<HrReviewDto> SubmitHrReviewAsync(HrReviewRequest request, Guid organizationId, Guid currentEmployeeId);
    Task<List<ManagerReviewDto>> GetManagerReviewsAsync(Guid organizationId, Guid managerId);
    Task<List<ManagerReviewDto>> GetEmployeeReviewsAsync(Guid organizationId, Guid employeeId);
    Task<bool> UpdateGoalStatusAsync(Guid goalId, string status);
    Task<IEnumerable<SelfAssessmentDto>> GetSelfAssessmentsByEmployeeAsync(Guid employeeId, Guid organizationId);
    Task<IEnumerable<SelfAssessmentDto>> GetSelfAssessmentsByEmployeesAsync(List<Guid> employeeIds, Guid organizationId);
    Task<AppraisalCycle> CreateAppraisalCycleAsync(CreateAppraisalCycleRequest request, Guid organizationId, Guid currentEmployeeId);
    Task<GoalDto> CreateGoalAsync(CreateGoalRequest request, Guid organizationId, Guid createdBy);
    Task<List<GoalDto>> GetCompletedGoalsByKRAAsync(Guid kraId);
    Task<bool> UpdateKRAProgressAsync(Guid kraId, string krarate, int noOfGoals);
    Task<List<GoalDto>> GetAllGoalsAsync(Guid organizationId);
    Task<GoalDto?> GetGoalByIdAsync(Guid goalId);
    Task<GoalDto?> UpdateGoalAsync(Guid organizationId, UpdateGoalRequest request);
    Task DeleteGoalAsync(Guid organizationId, Guid goalId);
    Task<bool> UpdateGoalAssignmentAsync(Guid organizationId, Guid goalId, Guid assignedTo, Guid assignedBy);
    Task<List<GoalDto>> GetEmployeeAssignedGoalAsync(Guid organizationId, Guid employeeId);
    //Task<AppraisalCycle> CreateAppraisalCycleAsync(Guid organizationId, string cycleName, DateTime startDate, DateTime endDate, string? description = null);
    Task<List<AppraisalCycleDto>> GetAppraisalCyclesAsync(Guid organizationId);
    Task<EmployeeAppraisal> CreateEmployeeAppraisalAsync(EmployeeAppraisal appraisal);
    Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsAsync(Guid cycleId, Guid employeeId);
    Task<SkillSetDto?> CreateSkillSetAsync(CreateSkillSetRequest request, Guid organizationId);
    Task<List<SkillSetDto>> GetSkillSetsAsync(Guid organizationId);
    Task<SkillSetDto?> GetSkillSetByIdAsync(Guid skillSetId);
    Task<SkillSetDto> UpdateSkillSetAsync(Guid skillSetId, UpdateSkillSetRequest request);
    Task<bool> DeleteSkillSetAsync(Guid skillSetId);
    Task<EmployeeSkillDto?> CreateEmployeeSkillAsync(CreateEmployeeSkillRequest request);
    Task<List<EmployeeSkillDto>> GetEmployeeSkillsAsync(Guid? employeeId);
    Task<List<EmployeeSkillDto>> GetEmployeeSkillsByOrganizationAsync(Guid organizationId, EmployeeSkillFilterRequest filter);
    Task<List<EmployeeSkillDto>> GetEmployeeSkillsByManagerAsync(Guid managerEmployeeId, EmployeeSkillFilterRequest filter);
    Task<EmployeeSkillDto?> GetEmployeeSkillByIdAsync(Guid employeeSkillId);
    Task<EmployeeSkillDto> UpdateEmployeeSkillAsync(Guid employeeSkillId, UpdateEmployeeSkillRequest request);
    Task<bool> DeleteEmployeeSkillAsync(Guid employeeSkillId);
    Task<PerformanceSummaryDto> GetPerformanceSummaryAsync(Guid organizationId, Guid? cycleId = null);
    Task<bool> UpdateAppraisalStatusAsync(Guid appraisalId, string status);
    Task<List<TopPerformerDto>> GetTopPerformersAsync(Guid organizationId, int count = 10);

    //Kras
    Task<KRADto> CreateKRAAsync(CreateKRARequest request, Guid organizationId, Guid CurrentEmpId);
    Task<List<KRADto>> GetKRAsAsync(Guid organizationId);

    Task<KRADto> GetKraByIdAsync(Guid KraId);

    Task<KRADto> UpdatekraAsync(Guid kraId, UpdateKRARequest request);
    Task<bool> UpdateKRAStatusAsync(Guid kraId, bool isActive);
    Task<SelfAssessmentDto> CreateSelfAssessmentAsync(CreateSelfAssessmentRequest request, Guid OrganizationId, Guid currentEmployeeId);
    Task<IEnumerable<SelfAssessmentDto>> GetSelfAssessmentsAsync(Guid employeeId, Guid cycleId);
    Task<IEnumerable<SelfAssessmentDto>> GetMySelfAssessmentsAsync(Guid employeeId, Guid? cycleId = null, Guid? kraId = null, string? search = null);
    Task UpdateSelfAssessmentStatusAsync(Guid employeeId, Guid cycleId, string status);
    Task<IEnumerable<SelfAssessmentDto>> GetTeamSelfAssessmentsAsync(Guid managerId, Guid cycleId);
    Task<IEnumerable<SelfAssessmentDto>> GetAllEmployeeSelfAssessmentsAsync(Guid organizationId, Guid? cycleId = null, Guid? employeeId = null, Guid? kraId = null, string? search = null);
    Task<IEnumerable<SelfAssessmentDto>> GetMyTeamSelfAssessmentsAsync(Guid managerEmployeeId, Guid? cycleId = null, Guid? employeeId = null, Guid? kraId = null, string? search = null);
    Task<ManagerReviewDto> SubmitManagerReviewAsync(ManagerReviewRequest request, Guid organizationId, Guid currentEmpId);
    Task<IEnumerable<EmployeeAppraisalForEmployeeDto>> GetEmployeeAppraisalsForEmployeeAsync(Guid employeeId, Guid? cycleId = null, Guid? kraId = null, string? search = null, string? status = null);

    // Appraisal Cycle Management - Enhanced
    Task<AppraisalCycleDto?> GetAppraisalCycleByIdAsync(Guid cycleId);
    Task<AppraisalCycleDto> UpdateAppraisalCycleAsync(Guid cycleId, UpdateAppraisalCycleRequest request);
    Task<bool> DeleteAppraisalCycleAsync(Guid cycleId);
    Task<AppraisalCycleDto?> GetActiveAppraisalCycleAsync(Guid organizationId);

    // KRA Management - Enhanced
    Task<bool> DeleteKRAAsync(Guid kraId);

    // Appraisal Consolidation
    //Task<EmployeeAppraisalDto> ConsolidateAppraisalAsync(ConsolidateAppraisalRequest request);

    // Reports and Analytics
    Task<EmployeePerformanceHistoryDto> GetEmployeePerformanceHistoryAsync(Guid employeeId);
    Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsByEmployeeAsync(Guid employeeId);
    Task<EmployeeAppraisalDto?> GetEmployeeAppraisalByIdAsync(Guid appraisalId);
    Task<bool> DeleteEmployeeAppraisalAsync(Guid appraisalId);

    // Paged Appraisals
    Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsPagedAsync(Guid organizationId, AppraisalFilterRequest filter, int page, int limit);
    Task<int> GetEmployeeAppraisalsCountAsync(Guid organizationId, AppraisalFilterRequest filter);

    // Reports
    Task<List<DepartmentPerformanceDto>> GetDepartmentPerformanceBreakdownAsync(Guid organizationId, DateTime? startDate, DateTime? endDate);
    Task<string?> GetEmployeeDepartmentAsync(Guid employeeId);
    Task<Guid> GetEmployeeFromTable(Guid Userid);
    Task<TeamPerformanceOverviewDto> GetTeamPerformanceOverviewAsync(Guid managerEmployeeId, Guid organizationId);
    Task<(List<SkillSetDto> Items, int Total)> GetSkillSetsPagedAsync(Guid organizationId, SkillSetFilterRequest filter, int page, int limit);
    Task<SkillWithEmployeesDto?> GetSkillWithEmployeesAsync(Guid skillId);
    Task<SkillSetDto> ToggleSkillStatusAsync(Guid skillId);
    Task<(List<EmployeeSkillSummaryDto> Items, int Total)> GetEmployeeSkillsSummaryPagedAsync(Guid organizationId, EmployeeSkillFilterRequest filter, int page, int limit);
    Task<EmployeeSkillFullDetailDto> GetEmployeeSkillFullDetailAsync(Guid employeeId);
}
