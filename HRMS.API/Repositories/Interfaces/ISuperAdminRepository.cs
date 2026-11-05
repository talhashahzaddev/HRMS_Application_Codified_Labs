using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface ISuperAdminRepository
{
    Task<PlatformDashboardDto> GetPlatformDashboardAsync();
    Task<PagedResult<OrganizationListItemDto>> GetOrganizationsPagedAsync(OrganizationFilterRequest filter);
    Task<OrganizationDetailDto?> GetOrganizationDetailAsync(Guid organizationId);
    Task<IReadOnlyList<SubscriptionHistoryDto>> GetSubscriptionHistoryAsync(Guid companyId);
    Task LogSubscriptionHistoryAsync(Guid subscriptionId, Guid companyId, string action,
        Guid? oldPlanId, Guid? newPlanId, DateTime? oldEndDate, DateTime? newEndDate,
        Guid? performedBy, string? notes);
    Task<IReadOnlyList<GlobalRolePermissionFlatDto>> GetGlobalRolesAsync();
    Task<Guid> CreateGlobalRoleAsync(CreateGlobalRoleRequest request);
    Task<bool> UpdateGlobalRoleAsync(UpdateGlobalRoleRequest request);
    Task<bool> DeleteGlobalRoleAsync(Guid roleId);
    Task<GlobalRole?> GetGlobalRoleByNameAsync(string roleName);
    Task<GlobalRole?> GetGlobalRoleByIdAsync(Guid roleId);

    // User Invitation methods
    Task<Guid> SaveInvitationAsync(Guid userId, string token, DateTime expiresAt, string? organizationName = null);
    Task<UserInvitation?> GetInvitationByTokenAsync(string token);
    Task<bool> MarkInvitationAsUsedAsync(string token);
    Task<bool> DeactivateOldInvitationsForUserAsync(Guid userId);
    Task<bool> UserHasCompletedSetupAsync(Guid userId);
    Task<PagedResult<UserInvitationDto>> GetInvitationsPagedAsync(UserInvitationFilterRequest filter);
    Task<bool> DeleteInvitationAsync(Guid invitationId);
    Task<UserInvitation?> GetInvitationByIdAsync(Guid invitationId);
    Task<bool> DeactivateUserAsync(Guid userId);
    Task<bool> ActivateUserAsync(Guid userId);
}
