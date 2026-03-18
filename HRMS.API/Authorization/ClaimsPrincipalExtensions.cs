using System.Security.Claims;

namespace HRMS.API.Authorization
{
    public static class ClaimsPrincipalExtensions
    {
        // Check if user has a specific permission
        public static bool HasPermission(this ClaimsPrincipal user, string permission)
        {
            return user.HasClaim(c => c.Type == "permission" && c.Value == permission);
        }

        // Check if user has ANY of the given permissions
        public static bool HasAnyPermission(this ClaimsPrincipal user, params string[] permissions)
        {
            return permissions.Any(p => user.HasPermission(p));
        }

        // Check if user has ALL of the given permissions
        public static bool HasAllPermissions(this ClaimsPrincipal user, params string[] permissions)
        {
            return permissions.All(p => user.HasPermission(p));
        }

        // Get all permissions of the user
        public static List<string> GetPermissions(this ClaimsPrincipal user)
        {
            return user.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToList();
        }
    }
}
