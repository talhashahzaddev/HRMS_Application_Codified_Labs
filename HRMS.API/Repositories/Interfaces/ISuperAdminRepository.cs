using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces;

public interface ISuperAdminRepository
{
    Task<PlatformDashboardDto> GetPlatformDashboardAsync();
    Task<PagedResult<OrganizationListItemDto>> GetOrganizationsPagedAsync(OrganizationFilterRequest filter);
    Task<OrganizationDetailDto?> GetOrganizationDetailAsync(Guid organizationId);
    Task<List<SubscriptionHistoryDto>> GetSubscriptionHistoryAsync(Guid companyId);
    Task LogSubscriptionHistoryAsync(Guid subscriptionId, Guid companyId, string action,
        Guid? oldPlanId, Guid? newPlanId, DateTime? oldEndDate, DateTime? newEndDate,
        Guid? performedBy, string? notes);
}
