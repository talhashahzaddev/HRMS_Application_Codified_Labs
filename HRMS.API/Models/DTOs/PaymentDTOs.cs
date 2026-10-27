using System.ComponentModel.DataAnnotations;

namespace HRMS.API.Models.DTOs;

// ========== Existing DTOs (unchanged) ==========

public class SubscriptionPlanDto
{
    public Guid PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? MonthlyPrice { get; set; }
    public decimal? AnnualPrice { get; set; }
    public int? MaxUsers { get; set; }
    public string Currency { get; set; } = "BHD";
    public Dictionary<string, object>? Features { get; set; }
    public bool IsCustom { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSubscriptionPlanRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public decimal? MonthlyPrice { get; set; }

    public decimal? AnnualPrice { get; set; }

    public int? MaxUsers { get; set; }

    public Dictionary<string, object>? Features { get; set; }

    public bool IsCustom { get; set; } = false;

    public bool IsActive { get; set; } = true;
}

public class UpdateSubscriptionPlanRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public decimal? MonthlyPrice { get; set; }

    public decimal? AnnualPrice { get; set; }

    public int? MaxUsers { get; set; }

    public Dictionary<string, object>? Features { get; set; }

    public bool IsCustom { get; set; }

    public bool IsActive { get; set; }
}

public class CompanySubscriptionDto
{
    public Guid SubscriptionId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PlanId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class CompanySubscriptionDetailsDto
{
    public Guid SubscriptionId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsExpired { get; set; }
}

public class CreateCompanySubscriptionRequest
{
    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    public Guid PlanId { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;

    [Required]
    public string BillingCycle { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

public class UpdateCompanySubscriptionRequest
{
    [Required]
    public Guid PlanId { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;

    [Required]
    public string BillingCycle { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

// ============================================================================
// PAYMENT MANAGEMENT DTOs (Transactions, Invoices, Refunds)
// ============================================================================

// ========== Checkout / Gateway DTOs ==========

public class CreateCheckoutSessionRequest
{
    [Required]
    public Guid PlanId { get; set; }

    [Required]
    public string BillingCycle { get; set; } = string.Empty; // monthly | annual

    public string? CouponCode { get; set; }
}

public class CheckoutSessionResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public Guid TransactionId { get; set; }
}

public class VerifyPaymentRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    public Guid TransactionId { get; set; }
}

public class PaymentConfirmationDto
{
    public Guid TransactionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = "USD";
    public string BillingCycle { get; set; } = string.Empty;
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public string? InvoiceNumber { get; set; }
    public Guid? InvoiceId { get; set; }
}

// ========== Transaction DTOs ==========

public class TransactionDto
{
    public Guid TransactionId { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid? SubscriptionId { get; set; }
    public Guid PlanId { get; set; }
    public string? PlanName { get; set; }
    public string Gateway { get; set; } = string.Empty;
    public string? GatewayTransactionId { get; set; }
    public string? GatewayOrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string BillingCycle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? CardLast4 { get; set; }
    public string? CardBrand { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TransactionDetailDto : TransactionDto
{
    public string? GatewayCustomerId { get; set; }
    public string? Metadata { get; set; }
    public InvoiceDto? Invoice { get; set; }
    public List<RefundDto> Refunds { get; set; } = new();
}

public class TransactionFilterRequest
{
    public string? Status { get; set; }
    public string? Gateway { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? PlanId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? SortBy { get; set; } = "created_at";
    public string? SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ========== Invoice DTOs ==========

public class InvoiceDto
{
    public Guid InvoiceId { get; set; }
    public Guid TransactionId { get; set; }
    public Guid CompanyId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime BillingPeriodStart { get; set; }
    public DateTime BillingPeriodEnd { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyEmail { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
}

public class InvoiceFilterRequest
{
    public string? Status { get; set; }
    public Guid? CompanyId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? SortBy { get; set; } = "issued_at";
    public string? SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ========== Refund DTOs ==========

public class RefundDto
{
    public Guid RefundId { get; set; }
    public Guid TransactionId { get; set; }
    public string? GatewayRefundId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string InitiatedBy { get; set; } = string.Empty;
    public Guid? AdminId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class CreateRefundRequest
{
    [Required]
    public Guid TransactionId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Refund amount must be greater than zero")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

// ========== Admin Payment Dashboard DTOs ==========

public class PaymentDashboardDto
{
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public decimal AnnualRecurringRevenue { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal RevenueLastMonth { get; set; }
    public decimal RevenueGrowthPercent { get; set; }
    public int TotalTransactions { get; set; }
    public int SuccessfulTransactions { get; set; }
    public int FailedTransactions { get; set; }
    public int PendingTransactions { get; set; }
    public int TotalRefunds { get; set; }
    public decimal TotalRefundedAmount { get; set; }
    public List<TransactionDto> RecentTransactions { get; set; } = new();
}

public class RevenueAnalyticsDto
{
    public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
    public List<PlanRevenueDto> RevenueByPlan { get; set; } = new();
    public List<CycleRevenueDto> RevenueByCycle { get; set; } = new();
}

public class MonthlyRevenueDto
{
    public string Month { get; set; } = string.Empty;      // e.g. "2026-01"
    public decimal Revenue { get; set; }
    public int TransactionCount { get; set; }
}

public class PlanRevenueDto
{
    public string PlanName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int SubscriptionCount { get; set; }
}

public class CycleRevenueDto
{
    public string BillingCycle { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Count { get; set; }
}
