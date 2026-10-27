using System.Security.Claims;
using HRMS.API.Authorization;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace HRMS.API.Middleware;

public sealed class TenantEntitlementMiddleware
{
    private static readonly IReadOnlyDictionary<string, string> ControllerFeatures =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dashboard"] = "dashboard",
            ["Employee"] = "employee_management",
            ["Department"] = "employee_management",
            ["Position"] = "employee_management",
            ["Attendance"] = "attendance",
            ["Timesheet"] = "timesheet",
            ["Assets"] = "assets_management",
            ["Leave"] = "leave_management",
            ["Performance"] = "performance",
            ["News"] = "news",
            ["Jobs"] = "jobs",
            ["PayRoll"] = "payroll",
            ["Expense"] = "expense",
            ["HelpDesk"] = "help_desk",
            ["Chat"] = "ai_assistant",
            ["GeoFence"] = "attendance",
            ["Holiday"] = "leave_management"
        };

    private readonly RequestDelegate _next;
    private readonly ILogger<TenantEntitlementMiddleware> _logger;

    public TenantEntitlementMiddleware(
        RequestDelegate next,
        ILogger<TenantEntitlementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IEntitlementService entitlementService)
    {
        var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (action == null ||
            !ControllerFeatures.TryGetValue(action.ControllerName, out var moduleFeature) ||
            context.User.Identity?.IsAuthenticated != true ||
            context.User.HasClaim("isPlatformAdmin", "true"))
        {
            await _next(context);
            return;
        }

        var organizationIdValue = context.User.FindFirstValue(CustomClaimTypes.OrganizationId);
        if (!Guid.TryParse(organizationIdValue, out var tenantId) || tenantId == Guid.Empty)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                code = "TENANT_CONTEXT_REQUIRED",
                message = "A valid tenant context is required to access this feature."
            });
            return;
        }

        var entitlements = await entitlementService.GetTenantEntitlementsAsync(tenantId);
        var requiredFeatures = GetRequiredFeatures(action, moduleFeature, context.Request.Path);
        foreach (var feature in requiredFeatures)
        {
            if (!entitlements.Features.TryGetValue(feature, out var access) || !access.Enabled)
            {
                _logger.LogInformation(
                    "Tenant {TenantId} denied access to feature {FeatureKey}.",
                    tenantId,
                    feature);
                await WriteFeatureDeniedAsync(context, feature);
                return;
            }

            if (string.Equals(feature, "ai_assistant", StringComparison.OrdinalIgnoreCase) &&
                !await entitlementService.TryConsumeAiQueryAsync(tenantId, access.Limit))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = "FEATURE_LIMIT_REACHED",
                    feature,
                    limit = access.Limit,
                    upgradeRequired = true
                });
                return;
            }
        }

        if (entitlements.IsReadOnly && !IsReadOnlyAllowed(context.Request.Method, action.ActionName, context.Request.Path))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                code = "SUBSCRIPTION_READ_ONLY",
                status = entitlements.SubscriptionStatus,
                message = "This tenant's subscription is inactive. Viewing and exporting data are still allowed."
            });
            return;
        }

        var usersToAdd = await GetEmployeeCountIncreaseAsync(context, action, entitlements.UserCountType);
        if (usersToAdd < 0)
        {
            return;
        }

        if (usersToAdd > 0 &&
            entitlements.MaxUsers.HasValue &&
            entitlements.UserCount + usersToAdd > entitlements.MaxUsers.Value)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                code = "USER_LIMIT_REACHED",
                maxUsers = entitlements.MaxUsers,
                userCount = entitlements.UserCount,
                requestedIncrease = usersToAdd,
                upgradeRequired = true
            });
            return;
        }

        if (IsCustomRoleMutation(action) &&
            (!entitlements.Features.TryGetValue("custom_roles", out var customRoles) || !customRoles.Enabled))
        {
            await WriteFeatureDeniedAsync(context, "custom_roles");
            return;
        }

        await _next(context);
    }

    private static IEnumerable<string> GetRequiredFeatures(
        ControllerActionDescriptor action,
        string moduleFeature,
        PathString requestPath)
    {
        yield return moduleFeature;
        var path = requestPath.Value ?? string.Empty;
        var actionName = action.ActionName;

        if (string.Equals(moduleFeature, "payroll", StringComparison.OrdinalIgnoreCase))
        {
            var subFeature = path.Contains("payslip", StringComparison.OrdinalIgnoreCase) || actionName.Contains("Payslip", StringComparison.OrdinalIgnoreCase)
                ? "payslips"
                : path.Contains("sio", StringComparison.OrdinalIgnoreCase) || actionName.Contains("Sio", StringComparison.OrdinalIgnoreCase)
                    ? "sio_calculation"
                    : path.Contains("provident-fund", StringComparison.OrdinalIgnoreCase) ||
                      actionName.Contains("ProvidentFund", StringComparison.OrdinalIgnoreCase)
                        ? "sio_calculation"
                    : path.Contains("eosb", StringComparison.OrdinalIgnoreCase) || actionName.Contains("Eosb", StringComparison.OrdinalIgnoreCase) ||
                      actionName.Contains("EndOfService", StringComparison.OrdinalIgnoreCase)
                        ? "eosb"
                        : path.Contains("wps", StringComparison.OrdinalIgnoreCase) || actionName.Contains("Wps", StringComparison.OrdinalIgnoreCase)
                            ? "wps_file_export"
                            : path.Contains("loan", StringComparison.OrdinalIgnoreCase) || path.Contains("advance", StringComparison.OrdinalIgnoreCase) ||
                              actionName.Contains("Loan", StringComparison.OrdinalIgnoreCase) || actionName.Contains("Advance", StringComparison.OrdinalIgnoreCase)
                                ? "loans_advances"
                                : null;

            if (subFeature != null)
            {
                yield return subFeature;
            }
            else
            {
                yield return "payroll_processing";
            }
        }

        if (string.Equals(moduleFeature, "attendance", StringComparison.OrdinalIgnoreCase))
        {
            var subFeature = string.Equals(action.ControllerName, "GeoFence", StringComparison.OrdinalIgnoreCase)
                ? "geo_fencing"
                : path.Contains("shift", StringComparison.OrdinalIgnoreCase) ||
                             path.Contains("overtime", StringComparison.OrdinalIgnoreCase) ||
                             actionName.Contains("Shift", StringComparison.OrdinalIgnoreCase) ||
                             actionName.Contains("Overtime", StringComparison.OrdinalIgnoreCase)
                ? "shifts_overtime"
                : path.Contains("biometric", StringComparison.OrdinalIgnoreCase) || actionName.Contains("Biometric", StringComparison.OrdinalIgnoreCase)
                    ? "biometric_integration"
                        : "basic_attendance";
            yield return subFeature;

            if (path.Contains("timesheet", StringComparison.OrdinalIgnoreCase) ||
                actionName.Contains("Timesheet", StringComparison.OrdinalIgnoreCase))
            {
                yield return "timesheet";
            }
        }

        if (string.Equals(moduleFeature, "jobs", StringComparison.OrdinalIgnoreCase) &&
            (path.Contains("bahrainisation", StringComparison.OrdinalIgnoreCase) ||
             actionName.Contains("Bahrainisation", StringComparison.OrdinalIgnoreCase)))
        {
            yield return "bahrainisation_tracking";
        }
    }

    private static bool IsReadOnlyAllowed(string method, string actionName, PathString path) =>
        HttpMethods.IsGet(method) ||
        HttpMethods.IsHead(method) ||
        actionName.Contains("Export", StringComparison.OrdinalIgnoreCase) ||
        actionName.Contains("Download", StringComparison.OrdinalIgnoreCase) ||
        (path.Value?.Contains("export", StringComparison.OrdinalIgnoreCase) ?? false);

    private static bool IsEmployeeCreateOrActivate(ControllerActionDescriptor action)
    {
        if (!string.Equals(action.ControllerName, "Employee", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return action.ActionName.Equals("CreateEmployee", StringComparison.OrdinalIgnoreCase) ||
               action.ActionName.Equals("ActivateEmployee", StringComparison.OrdinalIgnoreCase) ||
               action.ActionName.Equals("BulkActivateEmployees", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<int> GetEmployeeCountIncreaseAsync(
        HttpContext context,
        ControllerActionDescriptor action,
        string userCountType)
    {
        if (!IsEmployeeCreateOrActivate(action))
        {
            return 0;
        }

        if (!action.ActionName.Equals("CreateEmployee", StringComparison.OrdinalIgnoreCase) &&
            userCountType.Equals("active_and_inactive", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (action.ActionName.Equals("BulkActivateEmployees", StringComparison.OrdinalIgnoreCase))
        {
            if (context.Request.ContentLength is 0)
            {
                return 0;
            }

            context.Request.EnableBuffering();
            try
            {
                using var document = await System.Text.Json.JsonDocument.ParseAsync(context.Request.Body);
                context.Request.Body.Position = 0;
                if (document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        if (property.Name.Equals("employeeIds", StringComparison.OrdinalIgnoreCase) &&
                            property.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            return property.Value.GetArrayLength();
                        }
                    }
                }

                return 0;
            }
            catch (System.Text.Json.JsonException)
            {
                context.Request.Body.Position = 0;
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    code = "INVALID_REQUEST_BODY",
                    message = "Employee IDs must be sent as a valid JSON array."
                });
                return -1;
            }
        }

        return 1;
    }

    private static bool IsCustomRoleMutation(ControllerActionDescriptor action) =>
        string.Equals(action.ControllerName, "Auth", StringComparison.OrdinalIgnoreCase) &&
        action.ActionName.Contains("Role", StringComparison.OrdinalIgnoreCase) &&
        !action.ActionName.Contains("Get", StringComparison.OrdinalIgnoreCase);

    private static async Task WriteFeatureDeniedAsync(HttpContext context, string feature)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            code = "FEATURE_NOT_IN_PLAN",
            feature,
            upgradeRequired = true
        });
    }
}
