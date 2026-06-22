using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface ISuperAdminService
{
    Task<ServiceResponse<PlatformDashboardDto>> GetPlatformDashboardAsync();
    Task<ServiceResponse<PagedResult<OrganizationListItemDto>>> GetOrganizationsAsync(OrganizationFilterRequest filter);
    Task<ServiceResponse<OrganizationDetailDto>> GetOrganizationDetailAsync(Guid organizationId);
    Task<ServiceResponse<List<SubscriptionHistoryDto>>> GetSubscriptionHistoryAsync(Guid companyId);
    Task<ServiceResponse<bool>> ManageSubscriptionAsync(Guid companyId, ManageSubscriptionRequest request, Guid performedBy);
    Task<List<GlobalRoleDetailResponse>> GetGlobalRolesAsync();
    Task<GlobalRoleActionResponse> CreateGlobalRoleAsync(CreateGlobalRoleRequest request);
    Task<GlobalRoleActionResponse> UpdateGlobalRoleAsync(UpdateGlobalRoleRequest request);
    Task<bool> DeleteGlobalRoleAsync(Guid roleId);
    Task<List<MenuDto>> GetMenusAsync();
}
