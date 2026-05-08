using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IPlatformAdminRepository
{
    Task<PlatformAdmin?> GetByEmailAsync(string email);
    Task<bool> UpdateLastLoginAsync(Guid adminId);
}
