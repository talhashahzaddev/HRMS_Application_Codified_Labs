using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using System;
using System.Threading.Tasks;

namespace HRMS.API.Repositories.Interfaces;

public interface ICareerRepository
{
    Task<JobOpeningDto?> GetExternalJobByJobCodeAsync(string jobCode, Guid organizationId);
    Task<Guid> ApplyJobByExternalCandidateAsync(JobApplication entity);
}
