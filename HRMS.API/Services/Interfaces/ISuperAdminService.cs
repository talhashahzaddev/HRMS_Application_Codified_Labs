using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface ISuperAdminService
{
    Task<ServiceResponse<PlatformDashboardDto>> GetPlatformDashboardAsync();
    Task<ServiceResponse<PagedResult<OrganizationListItemDto>>> GetOrganizationsAsync(OrganizationFilterRequest filter);
    Task<ServiceResponse<OrganizationDetailDto>> GetOrganizationDetailAsync(Guid organizationId);
    Task<ServiceResponse<IReadOnlyList<SubscriptionHistoryDto>>> GetSubscriptionHistoryAsync(Guid companyId);
    Task<ServiceResponse<bool>> ManageSubscriptionAsync(Guid companyId, ManageSubscriptionRequest request, Guid performedBy);
    Task<IReadOnlyList<GlobalRoleDetailResponse>> GetGlobalRolesAsync();
    Task<GlobalRoleActionResponse> CreateGlobalRoleAsync(CreateGlobalRoleRequest request);
    Task<GlobalRoleActionResponse> UpdateGlobalRoleAsync(UpdateGlobalRoleRequest request);
    Task<bool> DeleteGlobalRoleAsync(Guid roleId);
    Task<IReadOnlyList<MenuDto>> GetMenusAsync();

    // User Invitation methods
    Task<ServiceResponse<bool>> InviteUserAsync(InviteUserRequest request, string portalUrl);
    Task<ServiceResponse<PagedResult<UserInvitationDto>>> GetInvitationsAsync(UserInvitationFilterRequest filter);
    Task<ServiceResponse<bool>> ResendInvitationAsync(Guid invitationId, string portalUrl);
    Task<ServiceResponse<bool>> DeleteInvitationAsync(Guid invitationId);
    Task<ServiceResponse<SetPasswordValidationDto>> ValidateInvitationTokenAsync(string token);
    Task<ServiceResponse<bool>> SetPasswordAsync(SetPasswordRequest request);
}
