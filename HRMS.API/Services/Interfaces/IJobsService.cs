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
    Task<JobOpeningDto?> GetJobOpeningByIdAsync(Guid jobId);
    Task<JobOpeningDto> CreateJobOpeningAsync(Guid organizationId, Guid? createdBy, CreateJobOpeningRequest request);
    Task<JobOpeningDto?> UpdateJobOpeningAsync(Guid jobId, UpdateJobOpeningRequest request);
    Task<bool> DeleteJobOpeningAsync(Guid jobId);

    // JobApplications
    Task<IEnumerable<JobApplicationDto>> GetAllJobApplicationsAsync(Guid organizationId);
    Task<JobApplicationDto?> GetJobApplicationByIdAsync(Guid jobApplyId);
    Task<IEnumerable<JobApplicationDto>> GetJobApplicationsByEmployeeIdAsync(Guid employeeId);
    Task<IEnumerable<JobApplicationDto>> GetJobApplicationsByJobIdAsync(Guid jobId);
    Task<JobApplicationDto> CreateJobApplicationAsync(Guid organizationId, Guid employeeId, CreateJobApplicationRequest request);
    Task<JobApplicationDto?> UpdateJobApplicationAsync(Guid jobApplyId, UpdateJobApplicationRequest request);
    Task<bool> DeleteJobApplicationAsync(Guid jobApplyId);
}
