using HRMS.API.Models.DTOs;
using System;
using System.Threading.Tasks;

namespace HRMS.API.Services.Interfaces;

public interface ICareerService
{
    Task<JobOpeningDto?> GetExternalJobByJobCodeAsync(string jobCode, Guid organizationId);
    Task<JobApplicationDto> ApplyJobByExternalCandidateAsync(Guid organizationId, CreateJobApplicationRequest request);
}
