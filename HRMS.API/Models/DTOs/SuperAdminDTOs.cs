using System.ComponentModel.DataAnnotations;

namespace HRMS.API.Models.DTOs;

// ========== Platform Admin Auth DTOs ==========

public class PlatformAdminLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class PlatformAdminAuthResponse
{
    public Guid AdminId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Platform Admin";
    public string Token { get; set; } = string.Empty;
    public DateTime TokenExpiration { get; set; }
}

public class UserInvitationDto
{
    public Guid InvitationId { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? OrganizationName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

// ========== Platform Dashboard DTOs ==========

public class PlatformDashboardDto
{
    public int TotalOrganizations { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int ExpiredSubscriptions { get; set; }
    public int TrialAccounts { get; set; }
    public long TotalActiveEmployees { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public int NewOrganizationsThisMonth { get; set; }
    public int TotalDemoInquiries { get; set; }
    public int PendingInquiries { get; set; }
}

// ========== Organization List / Detail DTOs ==========

public class OrganizationListItemDto
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Country { get; set; }
    public string? SubscriptionPlanName { get; set; }
    public string? SubscriptionStatus { get; set; }
    public DateTime? SubscriptionExpiryDate { get; set; }
    public int TotalActiveEmployees { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrganizationDetailDto
{
    // Company details
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? Industry { get; set; }
    public string? Country { get; set; }
    public string? AdminEmail { get; set; }
    public string? CompanySize { get; set; }
    public string? Currency { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }

    // Subscription details
    public CompanySubscriptionDetailsDto? Subscription { get; set; }

    // Employee metrics
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int InactiveEmployees { get; set; }

    // Subscription history
    public List<SubscriptionHistoryDto> SubscriptionHistory { get; set; } = new();
}

// ========== Subscription Management DTOs ==========

public class ManageSubscriptionRequest
{
    [Required]
    public string Action { get; set; } = string.Empty; // upgrade, downgrade, extend, cancel

    public Guid? NewPlanId { get; set; }
    public DateTime? NewEndDate { get; set; }
    public string? BillingCycle { get; set; }
    public string? Notes { get; set; }
}

public class SubscriptionHistoryDto
{
    public Guid HistoryId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldPlanName { get; set; }
    public string? NewPlanName { get; set; }
    public DateTime? OldEndDate { get; set; }
    public DateTime? NewEndDate { get; set; }
    public string? PerformedByName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ========== Organization Filter DTOs ==========

public class OrganizationFilterRequest
{
    public string? Search { get; set; }
    public string? SubscriptionStatus { get; set; }
    public Guid? PlanId { get; set; }
    public string? SortBy { get; set; } = "createdat";
    public string? SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
