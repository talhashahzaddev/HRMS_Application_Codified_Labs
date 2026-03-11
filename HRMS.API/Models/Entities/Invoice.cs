using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("invoices", Schema = "payment")]
public class Invoice
{
    [Key]
    [Column("invoice_id")]
    public Guid InvoiceId { get; set; }

    [Column("transaction_id")]
    public Guid TransactionId { get; set; }

    [Column("company_id")]
    public Guid CompanyId { get; set; }

    [Column("invoice_number")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("tax_amount")]
    public decimal TaxAmount { get; set; }

    [Column("discount_amount")]
    public decimal DiscountAmount { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "USD";

    [Column("billing_period_start")]
    public DateTime BillingPeriodStart { get; set; }

    [Column("billing_period_end")]
    public DateTime BillingPeriodEnd { get; set; }

    [Column("plan_name")]
    public string PlanName { get; set; } = string.Empty;

    [Column("billing_cycle")]
    public string BillingCycle { get; set; } = string.Empty;

    [Column("company_name")]
    public string CompanyName { get; set; } = string.Empty;

    [Column("company_email")]
    public string? CompanyEmail { get; set; }

    [Column("status")]
    public string Status { get; set; } = "draft";

    [Column("issued_at")]
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }
}
