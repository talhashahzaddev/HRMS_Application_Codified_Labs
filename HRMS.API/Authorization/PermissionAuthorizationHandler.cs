using Microsoft.AspNetCore.Authorization;

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

        public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
        {
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            // Get all permission claims from the user's token
            var userPermissions = context.User.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToList();

            // Check if user has ALL required permissions
            bool hasPermissions = requirement.Permissions.All(p => userPermissions.Contains(p));

            if (hasPermissions)
            {
                _logger.LogInformation("User has required permissions: {Permissions}", string.Join(", ", requirement.Permissions));
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("User missing permissions. Required: {Required}, User has: {UserHas}",
                    string.Join(", ", requirement.Permissions),
                    string.Join(", ", userPermissions));
            }

            return Task.CompletedTask;
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
