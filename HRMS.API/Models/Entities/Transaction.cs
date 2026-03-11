using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("transactions", Schema = "payment")]
public class Transaction : BaseEntity
{
    [Key]
    [Column("transaction_id")]
    public Guid TransactionId { get; set; }

    [Column("company_id")]
    public Guid CompanyId { get; set; }

    [Column("subscription_id")]
    public Guid? SubscriptionId { get; set; }

    [Column("plan_id")]
    public Guid PlanId { get; set; }

    [Column("gateway")]
    public string Gateway { get; set; } = "stripe";

    [Column("gateway_transaction_id")]
    public string? GatewayTransactionId { get; set; }

    [Column("gateway_order_id")]
    public string? GatewayOrderId { get; set; }

    [Column("gateway_customer_id")]
    public string? GatewayCustomerId { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "USD";

    [Column("billing_cycle")]
    public string BillingCycle { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("payment_method")]
    public string? PaymentMethod { get; set; }

    [Column("card_last4")]
    public string? CardLast4 { get; set; }

    [Column("card_brand")]
    public string? CardBrand { get; set; }

    [Column("failure_reason")]
    public string? FailureReason { get; set; }

    [Column("metadata")]
    public string? Metadata { get; set; }
}
