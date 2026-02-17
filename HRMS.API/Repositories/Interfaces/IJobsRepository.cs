using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IJobsRepository
{
    // StageMaster
    Task<IEnumerable<StageMasterDto>> GetAllStageMastersAsync(Guid organizationId);
    Task<StageMasterDto?> GetStageMasterByIdAsync(Guid stageId);
    Task<Guid> CreateStageMasterAsync(Guid organizationId, string stageName, int? stageOrder);
    Task<bool> UpdateStageMasterAsync(Guid stageId, string stageName, int? stageOrder);
    Task<bool> DeleteStageMasterAsync(Guid stageId);

    // JobOpenings
    Task<IEnumerable<JobOpeningDto>> GetAllJobOpeningsAsync(Guid organizationId);
    Task<PagedResult<JobOpeningDto>> GetJobOpeningsPagedAsync(Guid organizationId, string? search, string? status, DateTime? lastDateFrom, DateTime? lastDateTo, int pageNumber, int pageSize);
    Task<JobOpeningDto?> GetJobOpeningByIdAsync(Guid jobId);
    Task<Guid> CreateJobOpeningAsync(JobOpening entity);
    Task<bool> UpdateJobOpeningAsync(JobOpening entity);
    Task<bool> DeleteJobOpeningAsync(Guid jobId);

    // JobApplications
    Task<IEnumerable<JobApplicationDto>> GetAllJobApplicationsAsync(Guid organizationId);
    Task<JobApplicationDto?> GetJobApplicationByIdAsync(Guid jobApplyId);
    Task<IEnumerable<JobApplicationDto>> GetJobApplicationsByEmployeeIdAsync(Guid employeeId);
    Task<PagedResult<JobApplicationDto>> GetJobApplicationsByEmployeeIdPagedAsync(Guid employeeId, Guid? stageId, string? status, int pageNumber, int pageSize);
    Task<IEnumerable<JobApplicationDto>> GetJobApplicationsByJobIdAsync(Guid jobId);
    Task<Guid> CreateJobApplicationAsync(JobApplication entity);
    Task<bool> UpdateJobApplicationAsync(JobApplication entity);
    Task<bool> DeleteJobApplicationAsync(Guid jobApplyId);

    // ApplicationStages
    Task<IEnumerable<ApplicationStageDto>> GetAllApplicationStagesAsync(Guid organizationId);
    Task<ApplicationStageDto?> GetApplicationStageByIdAsync(Guid applicationStageId);
    Task<IEnumerable<ApplicationStageDto>> GetApplicationStagesByJobApplyIdAsync(Guid jobApplyId);
    Task<Guid> CreateApplicationStageAsync(ApplicationStage entity);
    Task<bool> UpdateApplicationStageAsync(ApplicationStage entity);
    Task<bool> DeleteApplicationStageAsync(Guid applicationStageId);
    Task<IEnumerable<InterviewerInfoDto>> GetInterviewerDetailsAsync(IEnumerable<Guid> employeeIds);
}
