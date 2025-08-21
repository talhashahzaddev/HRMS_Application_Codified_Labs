namespace HRMS.API.Models.Entities;

public class PayrollEntry : BaseEntity
{
    public Guid EntryId { get; set; }
    public Guid PeriodId { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal BasicSalary { get; set; } = 0;
    public Dictionary<string, object> Allowances { get; set; } = new();
    public Dictionary<string, object> Deductions { get; set; } = new();
    public decimal OvertimeAmount { get; set; } = 0;
    public decimal BonusAmount { get; set; } = 0;
    public decimal GrossSalary { get; set; } = 0;
    public decimal TaxAmount { get; set; } = 0;
    public decimal OtherDeductions { get; set; } = 0;
    public decimal NetSalary { get; set; } = 0;
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "draft";
    public DateTime? CalculatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }

    // Navigation properties
    public PayrollPeriod PayrollPeriod { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
