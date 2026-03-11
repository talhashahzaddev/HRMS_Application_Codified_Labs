using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("refunds", Schema = "payment")]
public class Refund
{
    [Key]
    [Column("refund_id")]
    public Guid RefundId { get; set; }

    [Column("transaction_id")]
    public Guid TransactionId { get; set; }

    [Column("gateway_refund_id")]
    public string? GatewayRefundId { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "USD";

    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("initiated_by")]
    public string InitiatedBy { get; set; } = "admin";

    [Column("admin_id")]
    public Guid? AdminId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }
}
