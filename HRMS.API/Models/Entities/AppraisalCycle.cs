namespace HRMS.API.Models.Entities;

public class AppraisalCycle : BaseEntity
{
    public Guid CycleId { get; set; }
    public Guid OrganizationId { get; set; }

    public string CycleName { get; set; } = string.Empty;
    public string? CycleType { get; set; }       // cycletype
    public string? Description { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public DateTime? ReviewPeriodStart { get; set; } // reviewstartdate
    public DateTime? ReviewPeriodEnd { get; set; }   // reviewenddate

    public string Status { get; set; } = "upcoming";  // default value

    // Optional: default 5-point rating scale
    public Dictionary<string, object> RatingScale { get; set; } = new()
    {
        ["type"] = "5-point",
        ["scale"] = new[] { 1, 2, 3, 4, 5 }
    };

    public bool SelfReviewEnabled { get; set; } = true;     // isselfassessmentenable
    public bool ManagerReviewEnabled { get; set; } = true;  // managerreview
    public bool AppraisalEnabled { get; set; } = true;      // isappraisalenable

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<EmployeeAppraisal> EmployeeAppraisals { get; set; } = new List<EmployeeAppraisal>();
}
