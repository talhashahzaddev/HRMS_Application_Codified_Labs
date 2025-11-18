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
    Task<SkillSetDto?> GetSkillSetByIdAsync(Guid skillSetId);
    Task<SkillSetDto> UpdateSkillSetAsync(Guid skillSetId, UpdateSkillSetRequest request);
    Task<bool> DeleteSkillSetAsync(Guid skillSetId);
    Task<EmployeeSkillDto?> CreateEmployeeSkillAsync(CreateEmployeeSkillRequest request);
    Task<List<EmployeeSkillDto>> GetEmployeeSkillsAsync(Guid employeeId);
    Task<List<EmployeeSkillDto>> GetEmployeeSkillsByOrganizationAsync(Guid organizationId, EmployeeSkillFilterRequest filter);
    Task<EmployeeSkillDto?> GetEmployeeSkillByIdAsync(Guid employeeSkillId);
    Task<EmployeeSkillDto> UpdateEmployeeSkillAsync(Guid employeeSkillId, UpdateEmployeeSkillRequest request);
    Task<bool> DeleteEmployeeSkillAsync(Guid employeeSkillId);
    Task<PerformanceSummaryDto> GetPerformanceSummaryAsync(Guid organizationId, Guid? cycleId = null);
    Task<bool> UpdateAppraisalStatusAsync(Guid appraisalId, string status);
    Task<List<TopPerformerDto>> GetTopPerformersAsync(Guid organizationId, int count = 10);

    //Kras
    Task<KRADto> CreateKRAAsync(CreateKRARequest request, Guid organizationId);
    Task<List<KRADto>> GetKRAsAsync(Guid organizationId);

    Task<KRADto> GetKraByIdAsync(Guid KraId);

    Task<KRADto> UpdatekraAsync(Guid kraId, UpdateKRARequest request);
    Task<bool> UpdateKRAStatusAsync(Guid kraId, bool isActive);
    Task<SelfAssessmentDto> CreateSelfAssessmentAsync(CreateSelfAssessmentRequest request);
    Task<IEnumerable<SelfAssessmentDto>> GetSelfAssessmentsAsync(Guid employeeId, Guid cycleId);
    Task<IEnumerable<SelfAssessmentDto>> GetMySelfAssessmentsAsync(Guid employeeId, Guid? cycleId = null, Guid? kraId = null, string? search = null);
    Task UpdateSelfAssessmentStatusAsync(Guid employeeId, Guid cycleId, string status);
    Task<IEnumerable<SelfAssessmentDto>> GetTeamSelfAssessmentsAsync(Guid managerId, Guid cycleId);
    Task<ManagerReviewDto> SubmitManagerReviewAsync(ManagerReviewRequest request);
    Task<IEnumerable<EmployeeAppraisalForEmployeeDto>> GetEmployeeAppraisalsForEmployeeAsync(Guid employeeId, Guid? cycleId = null, Guid? kraId = null, string? search = null);

    // Appraisal Cycle Management - Enhanced
    Task<AppraisalCycleDto?> GetAppraisalCycleByIdAsync(Guid cycleId);
    Task<AppraisalCycleDto> UpdateAppraisalCycleAsync(Guid cycleId, UpdateAppraisalCycleRequest request);
    Task<bool> DeleteAppraisalCycleAsync(Guid cycleId);
    Task<AppraisalCycleDto?> GetActiveAppraisalCycleAsync(Guid organizationId);

    // KRA Management - Enhanced
    Task<bool> DeleteKRAAsync(Guid kraId);

    // Appraisal Consolidation
    Task<EmployeeAppraisalDto> ConsolidateAppraisalAsync(ConsolidateAppraisalRequest request);

    // Reports and Analytics
    Task<EmployeePerformanceHistoryDto> GetEmployeePerformanceHistoryAsync(Guid employeeId);
    Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsByEmployeeAsync(Guid employeeId);
    Task<EmployeeAppraisalDto?> GetEmployeeAppraisalByIdAsync(Guid appraisalId);
    
    // Paged Appraisals
    Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsPagedAsync(Guid organizationId, AppraisalFilterRequest filter, int page, int limit);
    Task<int> GetEmployeeAppraisalsCountAsync(Guid organizationId, AppraisalFilterRequest filter);
    
    // Reports
    Task<List<DepartmentPerformanceDto>> GetDepartmentPerformanceBreakdownAsync(Guid organizationId, DateTime? startDate, DateTime? endDate);
    Task<string?> GetEmployeeDepartmentAsync(Guid employeeId);
    Task<Guid> GetEmployeeFromTable(Guid Userid);

}
