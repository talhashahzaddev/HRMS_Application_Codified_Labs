using HRMS.API.Controllers;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Services.Interfaces;

public interface IPerformanceService
{
    Task<IReadOnlyList<GoalDto>> GetEmployeeAssignedGoalsAsync(Guid organizationId, Guid currentEmployeeId);
    Task<MYPerformanceSummary> GetMyPerformanceSummaryAsync(Guid organizationId, Guid currentEmpid);
    Task<IReadOnlyList<ManagerReviewDto>> GetAllManagerReviewsAsync(Guid organizationId, Guid managerId, SearchManagersDto search);
    Task<IReadOnlyList<HrReviewDto>> GetAllAppraisaAsync(Guid organizationId, Guid employeeId, SearchAppraisalDto search);
    Task<HrReviewDto> SubmitHrReviewAsync(HrReviewRequest request, Guid organizationId, Guid currentEmployeeId);
    Task<IReadOnlyList<HrReviewDto>> GetHrReviewsAsync(Guid organizationId, Guid CurrentEmpId);
    Task<IReadOnlyList<HrReviewDto>> GetHrReviewsByEmployeeAsync(Guid organizationId, Guid employeeId);
    Task<bool> UpdateGoalStatusAsync(Guid goalId, string status);
    //Task<AppraisalCycleDto> CreateAppraisalCycleAsync(string cycleName, DateTime startDate, DateTime endDate, Guid organizationId, string? description = null);
    Task<GoalDto> CreateGoalAsync(Guid organizationId, Guid createdBy, CreateGoalRequest request);
    Task<GoalDto?> UpdateGoalAsync(Guid organizationId, UpdateGoalRequest request);
    Task DeleteGoalAsync(Guid organizationId, Guid goalId);
    Task<GoalDto?> GetGoalByIdAsync(Guid organizationId, Guid goalId);
    Task<IReadOnlyList<GoalDto>> GetAllGoalsAsync(Guid organizationId, Guid CurrentEmpid);
    Task<bool> AssignGoalAsync(Guid organizationId, Guid goalId, Guid assignedTo, Guid assignedBy);
    Task<IReadOnlyList<SelfAssessmentDto>> GetSelfAssessmentsByEmployeeAsync(Guid employeeId, Guid organizationId);
    Task<IReadOnlyList<SelfAssessmentDto>> GetSelfAssessmentsForManagerAsync(Guid managerId, Guid organizationId);

    Task<IReadOnlyList<ManagerReviewDto>> GetManagerReviewsAsync(Guid organizationId, Guid managerId);
    Task<IReadOnlyList<ManagerReviewDto>> GetEmployeeReviewsAsync(Guid organizationId, Guid employeeId);



    Task<AppraisalCycleDto> CreateAppraisalCycleAsync(CreateAppraisalCycleRequest request, Guid organizationId, Guid currentEmployeeId);

    Task<IReadOnlyList<AppraisalCycleDto>> GetAppraisalCyclesAsync(Guid organizationId);
    Task<EmployeeAppraisalDto> CreateEmployeeAppraisalAsync(CreateAppraisalRequest request, Guid reviewerId, Guid organizationId);
    Task<IReadOnlyList<EmployeeAppraisalDto>> GetEmployeeAppraisalsAsync(Guid cycleId, Guid employeeId);
    Task<SkillSetDto?> CreateSkillSetAsync(CreateSkillSetRequest request, Guid organizationId);
    Task<IReadOnlyList<SkillSetDto>> GetSkillSetsAsync(Guid organizationId);
    Task<IReadOnlyList<EmployeeSkillDto>> GetEmployeeSkillsAsync(Guid employeeId);
    Task<PerformanceSummaryDto> GetPerformanceSummaryAsync(Guid organizationId, Guid? cycleId = null);
    Task<IReadOnlyList<TopPerformerDto>> GetTopPerformersAsync(Guid organizationId, int count = 10);

    // Additional methods based on frontend consumption
    Task<PerformanceSummaryDto> GetPerformanceDashboardSummaryAsync(Guid organizationId);
    Task<PagedResult<SkillSetDto>> GetSkillSetsPagedAsync(Guid organizationId, SkillSetFilterRequest filter, int page, int limit);
    Task<SkillSetDto?> GetSkillSetByIdAsync(Guid skillSetId);
    Task<SkillSetDto> UpdateSkillSetAsync(Guid skillSetId, UpdateSkillSetRequest request);
    Task<bool> DeleteSkillSetAsync(Guid skillSetId);
    Task<PagedResult<EmployeeSkillDto>> GetEmployeeSkillsPagedAsync(Guid organizationId, EmployeeSkillFilterRequest filter, int page, int limit);
    Task<PagedResult<EmployeeSkillDto>> GetEmployeeSkillsByManagerPagedAsync(Guid managerEmployeeId, EmployeeSkillFilterRequest filter, int page, int limit);
    Task<IReadOnlyList<EmployeeSkillDto>> GetEmployeeSkillsByEmployeeAsync(Guid employeeId);
    Task<EmployeeSkillDto?> GetEmployeeSkillByIdAsync(Guid employeeSkillId);
    Task<EmployeeSkillDto> CreateEmployeeSkillAsync(CreateEmployeeSkillRequest request);
    Task<EmployeeSkillDto> UpdateEmployeeSkillAsync(Guid employeeSkillId, UpdateEmployeeSkillRequest request);
    Task<bool> DeleteEmployeeSkillAsync(Guid employeeSkillId);

    //KRA 
    Task<PagedResult<KRADto>> GetKRAsAsync(Guid organizationId, int page, int limit, string? search);
    Task<KRADto?> GetKRAByIdAsync(Guid kraId);
    Task<KRADto> CreateKRAAsync(Guid organizationId, Guid CurrentEmpId, CreateKRARequest request);
    Task<KRADto> UpdateKRAAsync(Guid kraId, UpdateKRARequest request);
    Task<bool> DeleteKRAAsync(Guid kraId);
    Task<bool> UpdateKRAStatusAsync(Guid kraId, bool isActive);
    Task<PagedResult<AppraisalCycleDto>> GetAppraisalCyclesPagedAsync(Guid organizationId, int page, int limit, string? search);
    Task<AppraisalCycleDto?> GetAppraisalCycleByIdAsync(Guid cycleId);
    Task<AppraisalCycleDto> UpdateAppraisalCycleAsync(Guid cycleId, UpdateAppraisalCycleRequest request);
    Task<bool> DeleteAppraisalCycleAsync(Guid cycleId);
    Task<PagedResult<EmployeeAppraisalDto>> GetEmployeeAppraisalsPagedAsync(Guid organizationId, AppraisalFilterRequest filter, int page, int limit);
    Task<IReadOnlyList<EmployeeAppraisalDto>> GetEmployeeAppraisalsByEmployeeAsync(Guid employeeId);
    Task<EmployeeAppraisalDto?> GetEmployeeAppraisalByIdAsync(Guid appraisalId);
    Task<bool> DeleteEmployeeAppraisalAsync(Guid appraisalId);
    Task<EmployeeAppraisalDto> SubmitAppraisalAsync(Guid appraisalId, SubmitAppraisalRequest request);
    Task<EmployeeAppraisalDto> ReviewAppraisalAsync(Guid appraisalId, ReviewAppraisalRequest request);
    Task<PerformanceReportDto> GeneratePerformanceReportAsync(Guid organizationId, PerformanceReportFilterRequest filter);
    Task<byte[]> ExportPerformanceReportAsync(Guid organizationId, PerformanceReportFilterRequest filter);

    Task<SelfAssessmentDto> CreateSelfAssessmentAsync(CreateSelfAssessmentRequest request, Guid organizationId, Guid CurrentEmployeeId);
    Task<IReadOnlyList<SelfAssessmentDto>> GetSelfAssessmentsAsync(Guid employeeId, Guid cycleId);
    Task<IReadOnlyList<SelfAssessmentDto>> GetMySelfAssessmentsAsync(Guid employeeId, Guid? cycleId = null, Guid? kraId = null, string? search = null);
    Task<string> SubmitSelfAssessmentAsync(Guid employeeId, Guid cycleId);
    Task<IReadOnlyList<SelfAssessmentDto>> GetTeamSelfAssessmentsAsync(Guid managerId, Guid cycleId);
    Task<IReadOnlyList<SelfAssessmentDto>> GetAllEmployeeSelfAssessmentsAsync(Guid organizationId, Guid? cycleId = null, Guid? employeeId = null, Guid? kraId = null, string? search = null);
    Task<IReadOnlyList<SelfAssessmentDto>> GetMyTeamSelfAssessmentsAsync(Guid managerEmployeeId, Guid? cycleId = null, Guid? employeeId = null, Guid? kraId = null, string? search = null);
    Task<ManagerReviewDto> SubmitManagerReviewAsync(ManagerReviewRequest request, Guid organizationId, Guid currentEmpId);
    Task<IReadOnlyList<EmployeeAppraisalForEmployeeDto>> GetEmployeeAppraisalsForEmployeeAsync(Guid employeeId, Guid? cycleId = null, Guid? kraId = null, string? search = null, string? status = null);

    // Appraisal Cycle Management - Enhanced (New methods only)
    Task<AppraisalCycleDto?> GetActiveAppraisalCycleAsync(Guid organizationId);

    // Appraisal Consolidation
    //Task<EmployeeAppraisalDto> ConsolidateAppraisalAsync(ConsolidateAppraisalRequest request);

    // Reports and Analytics
    Task<EmployeePerformanceHistoryDto> GetEmployeePerformanceHistoryAsync(Guid employeeId);
    Task<Guid> GetEmployeeFromTable(Guid Userid);
    Task<TeamPerformanceOverviewDto> GetTeamPerformanceOverviewAsync(Guid managerEmployeeId, Guid organizationId);
    Task<SkillWithEmployeesDto?> GetSkillWithEmployeesAsync(Guid skillId);
    Task<SkillSetDto> ToggleSkillStatusAsync(Guid skillId);
    Task<PagedResult<EmployeeSkillSummaryDto>> GetEmployeeSkillsSummaryPagedAsync(Guid organizationId, EmployeeSkillFilterRequest filter, int page, int limit);
    Task<EmployeeSkillFullDetailDto> GetEmployeeSkillFullDetailAsync(Guid employeeId);

}
