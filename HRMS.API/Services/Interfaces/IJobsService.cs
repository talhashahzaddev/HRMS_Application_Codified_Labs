using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IJobsService
{
    // StageMaster
    Task<IEnumerable<StageMasterDto>> GetAllStageMastersAsync(Guid organizationId);
    Task<StageMasterDto?> GetStageMasterByIdAsync(Guid stageId);
    Task<StageMasterDto> CreateStageMasterAsync(Guid organizationId, CreateStageMasterRequest request);
    Task<StageMasterDto?> UpdateStageMasterAsync(Guid stageId, UpdateStageMasterRequest request);
    Task<bool> DeleteStageMasterAsync(Guid stageId);

    // JobOpenings
    Task<IEnumerable<JobOpeningDto>> GetAllJobOpeningsAsync(Guid organizationId);
    Task<PagedResult<JobOpeningDto>> GetJobOpeningsPagedAsync(Guid organizationId, string? search, string? status, DateTime? lastDateFrom, DateTime? lastDateTo, int pageNumber, int pageSize);
    Task<JobOpeningDto?> GetJobOpeningByIdAsync(Guid jobId);
    Task<JobOpeningDto> CreateJobOpeningAsync(Guid organizationId, Guid? createdBy, CreateJobOpeningRequest request);
    Task<JobOpeningDto?> UpdateJobOpeningAsync(Guid jobId, UpdateJobOpeningRequest request);
    Task<bool> DeleteJobOpeningAsync(Guid jobId);

    // JobApplications
    Task<IEnumerable<JobApplicationDto>> GetAllJobApplicationsAsync(Guid organizationId);
    Task<PagedResult<JobApplicationDto>> GetJobApplicationsPagedAsync(Guid organizationId, string? search, DateTime? applyDateFrom, DateTime? applyDateTo, Guid? stageId, int pageNumber, int pageSize);
    Task<PagedResult<JobApplicationDto>> GetJobApplicationsPostedByMePagedAsync(Guid employeeId, string? search, DateTime? applyDateFrom, DateTime? applyDateTo, Guid? stageId, int pageNumber, int pageSize);
    Task<JobApplicationDto?> GetJobApplicationByIdAsync(Guid jobApplyId);
    Task<IEnumerable<JobApplicationDto>> GetJobApplicationsByEmployeeIdAsync(Guid employeeId);
    Task<PagedResult<JobApplicationDto>> GetMyJobApplicationsPagedAsync(Guid employeeId, string? search, Guid? stageId, string? status, int pageNumber, int pageSize);
    Task<PagedResult<JobApplicationDto>> GetMySelfJobApplicationsPagedAsync(Guid employeeId, string? search, Guid? stageId, string? status, int pageNumber, int pageSize);
    Task<IEnumerable<JobApplicationDto>> GetJobApplicationsByJobIdAsync(Guid jobId);
    Task<JobApplicationDto> CreateJobApplicationAsync(Guid organizationId, Guid employeeId, CreateJobApplicationRequest request);
    Task<JobApplicationDto> ApplyForMySelfAsync(Guid organizationId, Guid employeeId, ApplyForMySelfRequest request);
    Task<JobApplicationDto?> UpdateJobApplicationAsync(Guid jobApplyId, UpdateJobApplicationRequest request);
    Task<bool> DeleteJobApplicationAsync(Guid jobApplyId);

    // ApplicationStages
    Task<IEnumerable<ApplicationStageDto>> GetAllApplicationStagesAsync(Guid organizationId);
    Task<ApplicationStageDto?> GetApplicationStageByIdAsync(Guid applicationStageId);
    Task<IEnumerable<ApplicationStageDto>> GetApplicationStagesByJobApplyIdAsync(Guid jobApplyId);
    Task<ApplicationStageDto> CreateApplicationStageAsync(Guid updatedByEmployeeId, CreateApplicationStageRequest request);
    Task<ApplicationStageDto?> UpdateApplicationStageAsync(Guid applicationStageId, Guid updatedByEmployeeId, UpdateApplicationStageRequest request);
    Task<bool> DeleteApplicationStageAsync(Guid applicationStageId);
}
