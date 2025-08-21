namespace HRMS.API.Models.Entities;

public class AppraisalCycle : BaseEntity
{
    public Guid CycleId { get; set; }
    public Guid OrganizationId { get; set; }
    public string CycleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? ReviewPeriodStart { get; set; }
    public DateTime? ReviewPeriodEnd { get; set; }
    public string Status { get; set; } = "active";
    public Dictionary<string, object> RatingScale { get; set; } = new() { ["type"] = "5-point", ["scale"] = new[] { 1, 2, 3, 4, 5 } };
    public bool SelfReviewEnabled { get; set; } = true;
    public bool PeerReviewEnabled { get; set; } = false;
    public bool ManagerReviewEnabled { get; set; } = true;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<EmployeeAppraisal> EmployeeAppraisals { get; set; } = new List<EmployeeAppraisal>();
}
