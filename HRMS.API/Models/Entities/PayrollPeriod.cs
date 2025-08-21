namespace HRMS.API.Models.Entities;

public class PayrollPeriod : BaseEntity
{
    public Guid PeriodId { get; set; }
    public Guid OrganizationId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? PayDate { get; set; }
    public string Status { get; set; } = "draft";
    public int TotalEmployees { get; set; } = 0;
    public decimal TotalGrossAmount { get; set; } = 0;
    public decimal TotalTaxAmount { get; set; } = 0;
    public decimal TotalNetAmount { get; set; } = 0;
    public Guid? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Employee? Processor { get; set; }
    public ICollection<PayrollEntry> PayrollEntries { get; set; } = new List<PayrollEntry>();
}
