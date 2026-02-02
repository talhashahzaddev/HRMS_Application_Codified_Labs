using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("expenses", Schema = "expense")]
public class Expense
{
    [Column("expenseid")]
    public Guid ExpenseId { get; set; }

    [Column("organizationid")]
    public Guid OrganizationId { get; set; }

    [Column("employeeid")]
    public Guid EmployeeId { get; set; }

    [Column("categoryid")]
    public Guid? CategoryId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("billdate")]
    public DateTime BillDate { get; set; }

    [Column("paymentmethod")]
    public string? PaymentMethod { get; set; }

    [Column("merchant")]
    public string? Merchant { get; set; }

    [Column("receipturl")]
    public string? ReceiptUrl { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("approvedby")]
    public Guid? ApprovedBy { get; set; }

    [Column("createdat")]
    public DateTime? CreatedAt { get; set; }

    [Column("updatedat")]
    public DateTime? UpdatedAt { get; set; }
}
