using System.Security.Claims;
using HRMS.API.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace HRMS.API.Authorization
{
    public static class ClaimsPrincipalExtensions
    {
        private static readonly TimeSpan PermissionCacheDuration = TimeSpan.FromMinutes(10);

        private static Guid? GetRoleId(this ClaimsPrincipal user)
        {
            var roleIdClaim = user.FindFirst("roleId")?.Value;
            return Guid.TryParse(roleIdClaim, out var roleId) ? roleId : null;
        }

        private static HashSet<string> GetPermissionsFromStore(this ClaimsPrincipal user, IAuthRepository authRepository, IMemoryCache memoryCache)
        {
            var roleId = user.GetRoleId();
            if (!roleId.HasValue)
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            var cacheKey = $"permissions:role:{roleId.Value}";
            return memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = PermissionCacheDuration;

                var permissions = authRepository
                    .GetRolePermissionsWithMenuDetailsAsync(roleId.Value)
                    .GetAwaiter()
                    .GetResult();

                return permissions
                    .Where(p => p.HasPermission && !string.IsNullOrWhiteSpace(p.ActionKey))
                    .Select(p => p.ActionKey)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        // Check if user has a specific permission
        public static bool HasPermission(this ClaimsPrincipal user, string permission)
        {
            return user.HasClaim(c => c.Type == "permission" && c.Value == permission);
        }

        public static bool HasPermission(this ClaimsPrincipal user, string permission, IAuthRepository authRepository, IMemoryCache memoryCache)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                return false;
            }

            var tokenClaimPermission = user.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (tokenClaimPermission.Contains(permission))
            {
                return true;
            }

            var storePermissions = user.GetPermissionsFromStore(authRepository, memoryCache);
            return storePermissions.Contains(permission);
        }

        // Check if user has ANY of the given permissions
        public static bool HasAnyPermission(this ClaimsPrincipal user, params string[] permissions)
        {
            return permissions.Any(p => user.HasPermission(p));
        }

        public static bool HasAnyPermission(this ClaimsPrincipal user, IAuthRepository authRepository, IMemoryCache memoryCache, params string[] permissions)
        {
            return permissions.Any(p => user.HasPermission(p, authRepository, memoryCache));
        }

        // Check if user has ALL of the given permissions
        public static bool HasAllPermissions(this ClaimsPrincipal user, params string[] permissions)
        {
            return permissions.All(p => user.HasPermission(p));
        }

        public static bool HasAllPermissions(this ClaimsPrincipal user, IAuthRepository authRepository, IMemoryCache memoryCache, params string[] permissions)
        {
            return permissions.All(p => user.HasPermission(p, authRepository, memoryCache));
        }

        // Get all permissions of the user
        public static List<string> GetPermissions(this ClaimsPrincipal user)
        {
            return user.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToList();
        }

        public static List<string> GetPermissions(this ClaimsPrincipal user, IAuthRepository authRepository, IMemoryCache memoryCache)
        {
            var tokenClaimPermissions = user.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var storePermissions = user.GetPermissionsFromStore(authRepository, memoryCache);
            tokenClaimPermissions.UnionWith(storePermissions);

            return tokenClaimPermissions.ToList();
        }
    }
}
