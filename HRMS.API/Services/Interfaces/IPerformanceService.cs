using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Services.Interfaces;

public interface IPerformanceService
{
    Task<AppraisalCycleDto> CreateAppraisalCycleAsync(string cycleName, DateTime startDate, DateTime endDate, Guid organizationId, string? description = null);
    Task<List<AppraisalCycleDto>> GetAppraisalCyclesAsync(Guid organizationId);
    Task<EmployeeAppraisalDto> CreateEmployeeAppraisalAsync(CreateAppraisalRequest request, Guid reviewerId, Guid organizationId);
    Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsAsync(Guid cycleId, Guid employeeId);
    Task<SkillSetDto?> CreateSkillSetAsync(CreateSkillSetRequest request, Guid organizationId);
    Task<List<SkillSetDto>> GetSkillSetsAsync(Guid organizationId);
    Task<List<EmployeeSkillDto>> GetEmployeeSkillsAsync(Guid employeeId);
    Task<PerformanceSummaryDto> GetPerformanceSummaryAsync(Guid organizationId, Guid? cycleId = null);
    Task<List<TopPerformerDto>> GetTopPerformersAsync(Guid organizationId, int count = 10);

    // Additional methods based on frontend consumption
    Task<PerformanceSummaryDto> GetPerformanceDashboardSummaryAsync(Guid organizationId);
    Task<PagedResult<SkillSetDto>> GetSkillSetsPagedAsync(Guid organizationId, SkillSetFilterRequest filter, int page, int limit);
    Task<SkillSetDto?> GetSkillSetByIdAsync(Guid skillSetId);
    Task<SkillSetDto> UpdateSkillSetAsync(Guid skillSetId, UpdateSkillSetRequest request);
    Task<bool> DeleteSkillSetAsync(Guid skillSetId);
    Task<PagedResult<EmployeeSkillDto>> GetEmployeeSkillsPagedAsync(Guid organizationId, EmployeeSkillFilterRequest filter, int page, int limit);
    Task<PagedResult<EmployeeSkillDto>> GetEmployeeSkillsByManagerPagedAsync(Guid managerEmployeeId, EmployeeSkillFilterRequest filter, int page, int limit);
    Task<List<EmployeeSkillDto>> GetEmployeeSkillsByEmployeeAsync(Guid employeeId);
    Task<EmployeeSkillDto?> GetEmployeeSkillByIdAsync(Guid employeeSkillId);
    Task<EmployeeSkillDto> CreateEmployeeSkillAsync(CreateEmployeeSkillRequest request);
    Task<EmployeeSkillDto> UpdateEmployeeSkillAsync(Guid employeeSkillId, UpdateEmployeeSkillRequest request);
    Task<bool> DeleteEmployeeSkillAsync(Guid employeeSkillId);

    //KRA 
    Task<PagedResult<KRADto>> GetKRAsAsync(Guid organizationId, int page, int limit, string? search);
    Task<KRADto?> GetKRAByIdAsync(Guid kraId);
    Task<KRADto> CreateKRAAsync(Guid organizationId, CreateKRARequest request);
    Task<KRADto> UpdateKRAAsync(Guid kraId, UpdateKRARequest request);
    Task<bool> DeleteKRAAsync(Guid kraId);
    Task<bool> UpdateKRAStatusAsync(Guid kraId, bool isActive);
    Task<PagedResult<AppraisalCycleDto>> GetAppraisalCyclesPagedAsync(Guid organizationId, int page, int limit, string? search);
    Task<AppraisalCycleDto?> GetAppraisalCycleByIdAsync(Guid cycleId);
    Task<AppraisalCycleDto> UpdateAppraisalCycleAsync(Guid cycleId, UpdateAppraisalCycleRequest request);
    Task<bool> DeleteAppraisalCycleAsync(Guid cycleId);
    Task<PagedResult<EmployeeAppraisalDto>> GetEmployeeAppraisalsPagedAsync(Guid organizationId, AppraisalFilterRequest filter, int page, int limit);
    Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsByEmployeeAsync(Guid employeeId);
    Task<EmployeeAppraisalDto?> GetEmployeeAppraisalByIdAsync(Guid appraisalId);
    Task<bool> DeleteEmployeeAppraisalAsync(Guid appraisalId);
    Task<EmployeeAppraisalDto> SubmitAppraisalAsync(Guid appraisalId, SubmitAppraisalRequest request);
    Task<EmployeeAppraisalDto> ReviewAppraisalAsync(Guid appraisalId, ReviewAppraisalRequest request);
    Task<PerformanceReportDto> GeneratePerformanceReportAsync(Guid organizationId, PerformanceReportFilterRequest filter);
    Task<byte[]> ExportPerformanceReportAsync(Guid organizationId, PerformanceReportFilterRequest filter);

    Task<SelfAssessmentDto> CreateSelfAssessmentAsync(CreateSelfAssessmentRequest request, Guid organizationId);
    Task<IEnumerable<SelfAssessmentDto>> GetSelfAssessmentsAsync(Guid employeeId, Guid cycleId);
    Task<IEnumerable<SelfAssessmentDto>> GetMySelfAssessmentsAsync(Guid employeeId, Guid? cycleId = null, Guid? kraId = null, string? search = null);
    Task<string> SubmitSelfAssessmentAsync(Guid employeeId, Guid cycleId);
    Task<IEnumerable<SelfAssessmentDto>> GetTeamSelfAssessmentsAsync(Guid managerId, Guid cycleId);
    Task<IEnumerable<SelfAssessmentDto>> GetAllEmployeeSelfAssessmentsAsync(Guid organizationId, Guid? cycleId = null, Guid? employeeId = null, Guid? kraId = null, string? search = null);
    Task<IEnumerable<SelfAssessmentDto>> GetMyTeamSelfAssessmentsAsync(Guid managerEmployeeId, Guid? cycleId = null, Guid? employeeId = null, Guid? kraId = null, string? search = null);
    Task<ManagerReviewDto> SubmitManagerReviewAsync(ManagerReviewRequest request);
    Task<IEnumerable<EmployeeAppraisalForEmployeeDto>> GetEmployeeAppraisalsForEmployeeAsync(Guid employeeId, Guid? cycleId = null, Guid? kraId = null, string? search = null, string? status = null);

    // Appraisal Cycle Management - Enhanced (New methods only)
    Task<AppraisalCycleDto?> GetActiveAppraisalCycleAsync(Guid organizationId);

    // Appraisal Consolidation
    Task<EmployeeAppraisalDto> ConsolidateAppraisalAsync(ConsolidateAppraisalRequest request);

    // Reports and Analytics
    Task<EmployeePerformanceHistoryDto> GetEmployeePerformanceHistoryAsync(Guid employeeId);
    Task<Guid> GetEmployeeFromTable(Guid Userid);
    Task<TeamPerformanceOverviewDto> GetTeamPerformanceOverviewAsync(Guid managerEmployeeId, Guid organizationId);

}
