namespace HRMS.API.Models.DTOs;

public class SkillSetDto
{
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public int[] SkillLevelScale { get; set; } = Array.Empty<int>();
    public bool IsActive { get; set; }
}

public class EmployeeSkillDto
{
    public Guid EmployeeSkillId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string? SkillCategory { get; set; }
    public int ProficiencyLevel { get; set; }
    public string? AssessorName { get; set; }
    public DateTime? LastAssessed { get; set; }
    public string? Notes { get; set; }
}

public class AppraisalCycleDto
{
    public Guid CycleId { get; set; }
    public string CycleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object> RatingScale { get; set; } = new();
    public bool SelfReviewEnabled { get; set; }
    public bool PeerReviewEnabled { get; set; }
    public bool ManagerReviewEnabled { get; set; }
    public int TotalAppraisals { get; set; }
    public int CompletedAppraisals { get; set; }
}

public class EmployeeAppraisalDto
{
    public Guid AppraisalId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string CycleName { get; set; } = string.Empty;
    public string? ReviewerName { get; set; }
    public string ReviewType { get; set; } = string.Empty;
    public decimal? OverallRating { get; set; }
    public Dictionary<string, object> KraRatings { get; set; } = new();
    public Dictionary<string, object> SkillRatings { get; set; } = new();
    public string? Feedback { get; set; }
    public string? ImprovementAreas { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class CreateAppraisalRequest
{
    public Guid CycleId { get; set; }
    public Guid EmployeeId { get; set; }
    public string ReviewType { get; set; } = "manager";
    public decimal? OverallRating { get; set; }
    public Dictionary<string, object> KraRatings { get; set; } = new();
    public Dictionary<string, object> SkillRatings { get; set; } = new();
    public string? Feedback { get; set; }
    public string? ImprovementAreas { get; set; }
    public string? DevelopmentPlan { get; set; }
}

public class PerformanceSummaryDto
{
    public decimal AverageRating { get; set; }
    public int TotalAppraisals { get; set; }
    public int PendingAppraisals { get; set; }
    public Dictionary<string, int> RatingDistribution { get; set; } = new();
    public List<TopPerformerDto> TopPerformers { get; set; } = new();
}

public class TopPerformerDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string Department { get; set; } = string.Empty;
}
