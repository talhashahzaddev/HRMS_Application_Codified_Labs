using System.ComponentModel.DataAnnotations;

namespace HRMS.API.Models.DTOs;

public class ExpenseCategoryDto
{
    public Guid CategoryId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}

public class CreateExpenseCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

public class UpdateExpenseCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

public class ExpenseDto
{
    public Guid ExpenseId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime BillDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Merchant { get; set; }
    public string? ReceiptUrl { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid? ApprovedBy { get; set; }
    public string? ApprovedName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateExpenseRequest
{
    [Required]
    public Guid? CategoryId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime BillDate { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    [MaxLength(150)]
    public string? Merchant { get; set; }

    public string? ReceiptUrl { get; set; }
}

public class UpdateExpenseRequest
{
    public Guid? CategoryId { get; set; }

    [MaxLength(150)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Amount { get; set; }

    public DateTime? BillDate { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    [MaxLength(150)]
    public string? Merchant { get; set; }

    public string? ReceiptUrl { get; set; }

    [MaxLength(30)]
    public string? Status { get; set; }
}

public class ExpenseRequestActionRequest
{
    [Required]
    [RegularExpression("^(approve|reject)$", ErrorMessage = "Action must be 'approve' or 'reject'")]
    public string Action { get; set; } = string.Empty;
}

public class RecurringExpenseDto
{
    public Guid RecurringExpenseId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string Rotation { get; set; } = string.Empty;
    public string? DayOfMonth { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Merchant { get; set; }
    public string? ReceiptUrl { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateRecurringExpenseRequest
{
    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(20)]
    public string Rotation { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? DayOfMonth { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    [MaxLength(150)]
    public string? Merchant { get; set; }

    public string? ReceiptUrl { get; set; }
}

public class UpdateRecurringExpenseRequest
{
    public Guid? CategoryId { get; set; }

    [MaxLength(150)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Amount { get; set; }

    [MaxLength(20)]
    public string? Rotation { get; set; }

    [MaxLength(20)]
    public string? DayOfMonth { get; set; }

    [MaxLength(30)]
    public string? Status { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    [MaxLength(150)]
    public string? Merchant { get; set; }

    public string? ReceiptUrl { get; set; }
}

/// <summary>
/// Item for expense pie report: cost per category (categoryid, categoryname, total_cost).
/// </summary>
public class ExpensePieReportItemDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
}
