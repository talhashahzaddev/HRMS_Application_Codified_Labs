using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace HRMS.API.Authorization
{
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        // The prefix that identifies permission policies
        private const string POLICY_PREFIX = "Permissions:";

        // Fallback policy provider for default policies
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        // Gets the default policy (used when no policy is specified)
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            return _fallbackPolicyProvider.GetDefaultPolicyAsync();
        }

        // Gets the fallback policy (used when no policy applies)
        public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
        {
            return _fallbackPolicyProvider.GetFallbackPolicyAsync();
        }

        // This is the key method - it gets called for every policy
        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            // Check if this is a permission policy (starts with "Permissions:")
            if (policyName.StartsWith(POLICY_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                // Extract the permissions from the policy name
                // Example: "Permissions:add_employee,edit_employees" 
                // becomes ["add_employee", "edit_employees"]
                var permissions = policyName.Substring(POLICY_PREFIX.Length).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                // Create a new policy with our PermissionRequirement
                var policy = new AuthorizationPolicyBuilder();
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new PermissionRequirement(permissions));

                return Task.FromResult(policy.Build());
            }

            // If not a permission policy, use the default provider
            return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }
    }
}