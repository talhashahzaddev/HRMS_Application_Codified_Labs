using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories.Interfaces;

public interface ITimesheetConfigRepository
{
    Task<TimesheetConfigDto?> GetByOrgIdAsync(Guid orgId);
    Task<TimesheetConfigDto> UpsertConfigAsync(Guid orgId, TimesheetConfigDto dto);
}
