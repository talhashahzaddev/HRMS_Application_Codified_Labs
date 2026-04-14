using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using HRMS.API.Repositories.Interfaces;
using System.Security.Claims;

namespace HRMS.API.Authorization
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string[] Permissions { get; }

        public PermissionRequirement(string[] permissions)
        {
            Permissions = permissions;
        }
    }
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ILogger<PermissionAuthorizationHandler> _logger;
        private readonly IAuthRepository _authRepository;
        private readonly IMemoryCache _memoryCache;

        public PermissionAuthorizationHandler(
            ILogger<PermissionAuthorizationHandler> logger,
            IAuthRepository authRepository,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _authRepository = authRepository;
            _memoryCache = memoryCache;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var roleIdClaim = context.User.FindFirst("roleId")?.Value;
            if (!Guid.TryParse(roleIdClaim, out var roleId))
            {
                _logger.LogWarning("Authorization failed: roleId claim missing or invalid");
                return;
            }

            var cacheKey = $"permissions:role:{roleId}";
            var userPermissions = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                var permissions = await _authRepository.GetRolePermissionsWithMenuDetailsAsync(roleId);
                return permissions
                    .Where(p => p.HasPermission && !string.IsNullOrWhiteSpace(p.ActionKey))
                    .Select(p => p.ActionKey)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            });

            // Check if user has ALL required permissions
            bool hasPermissions = requirement.Permissions.All(p => userPermissions != null && userPermissions.Contains(p));

            if (hasPermissions)
            {
                _logger.LogInformation("User has required permissions: {Permissions}", string.Join(", ", requirement.Permissions));
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("User missing permissions. Required: {Required}, User has: {UserHas}",
                    string.Join(", ", requirement.Permissions),
                    userPermissions == null ? string.Empty : string.Join(", ", userPermissions));
            }
        }
    }

    // This is the attribute we'll use on our API endpoints
    public class PermissionAuthorizeAttribute : AuthorizeAttribute
    {
        public PermissionAuthorizeAttribute(params string[] permissions)
        {
            // Convert permissions to a policy name
            Policy = $"Permissions:{string.Join(",", permissions)}";
        }
    }
}
