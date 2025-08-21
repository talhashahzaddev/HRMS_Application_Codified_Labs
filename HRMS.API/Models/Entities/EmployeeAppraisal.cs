namespace HRMS.API.Models.Entities;

public class EmployeeAppraisal : BaseEntity
{
    public Guid AppraisalId { get; set; }
    public Guid CycleId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? ReviewerId { get; set; }
    public string ReviewType { get; set; } = "manager"; // self, peer, manager
    public decimal? OverallRating { get; set; }
    public Dictionary<string, object> KraRatings { get; set; } = new();
    public Dictionary<string, object> SkillRatings { get; set; } = new();
    public Dictionary<string, object> GoalsAchieved { get; set; } = new();
    public string? Feedback { get; set; }
    public string? ImprovementAreas { get; set; }
    public string? DevelopmentPlan { get; set; }
    public string Status { get; set; } = "draft";
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    // Navigation properties
    public AppraisalCycle AppraisalCycle { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public Employee? Reviewer { get; set; }
}
