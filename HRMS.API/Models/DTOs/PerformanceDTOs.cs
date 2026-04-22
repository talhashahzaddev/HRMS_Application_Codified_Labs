using System.Text.Json.Serialization;

namespace HRMS.API.Models.DTOs;


public class CreateSkillSetDto
{
    public string SkillName { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public List<int> SkillLevelScale { get; set; } = new() { 1, 2, 3, 4, 5 };
}
public class SkillSetDto
{
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int EmployeeCount { get; set; }
}

public class EmployeeSkillDto
{
    public Guid EmployeeSkillId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? EmployeeEmail { get; set; }
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string? SkillCategory { get; set; }
    public int ProficiencyLevel { get; set; }
    public Guid? AssessedBy { get; set; }
    public DateTime? LastAssessed { get; set; }
    public string? Notes { get; set; }
    public string? AssessorName { get; set; }
}

public class AppraisalCycleDto
{
    public Guid CycleId { get; set; }
    public string CycleName { get; set; } = string.Empty;
    public string? CycleType { get; set; } // cycletype
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? ReviewStartDate { get; set; } // reviewstartdate
    public DateTime? ReviewEndDate { get; set; }   // reviewenddate
    public string Status { get; set; } = string.Empty;

    public bool SelfReviewEnabled { get; set; }   // isselfassessmentenable
    public bool ManagerReviewEnabled { get; set; } // managerreview
    public bool AppraisalEnabled { get; set; }     // isappraisalenable

    public Guid OrganizationId { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public int TotalAppraisals { get; set; }
    public int CompletedAppraisals { get; set; }
}


public class EmployeeAppraisalDto
{
    public Guid AppraisalId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid CycleId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string CycleName { get; set; } = string.Empty;
    public string? ReviewerName { get; set; }
    public string ReviewType { get; set; } = string.Empty;
    public decimal? OverallRating { get; set; }
    public decimal? CurrentRating { get; set; }
    public Dictionary<string, object> KraRatings { get; set; } = new();
    public Dictionary<string, object> SkillRatings { get; set; } = new();
    public string? Feedback { get; set; }
    public string? ImprovementAreas { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class EmployeeAppraisalForEmployeeDto
{
    public Guid AppraisalId { get; set; }
    public Guid CycleId { get; set; }
    public string CycleName { get; set; } = string.Empty;
    public string? ReviewerName { get; set; }
    public string ReviewType { get; set; } = string.Empty;
    public decimal? OverallRating { get; set; }
    public Dictionary<string, object> KraRatings { get; set; } = new();
    public Dictionary<string, object> SkillRatings { get; set; } = new();
    public string? Feedback { get; set; }
    public string? ImprovementAreas { get; set; }
    public string? DevelopmentPlan { get; set; }
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
    public bool IsSubmit { get; set; }

}

public class PerformanceSummaryDto
{
    public decimal AverageRating { get; set; }
    public long TotalAppraisals { get; set; }
    public long PendingAppraisals { get; set; }
    public Dictionary<string, long> RatingDistribution { get; set; } = new();
    public List<TopPerformerDto> TopPerformers { get; set; } = new();
}

public class MYPerformanceSummary
{
    public decimal CurrentCycleRating { get; set; }
    public decimal OverallRating { get; set; }
    public string skillsetRating { get; set; }
}

public class TopPerformerDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string Department { get; set; } = string.Empty;
}

// Additional DTOs for frontend consumption
public class SkillSetFilterRequest
{
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
    public string? Search { get; set; }
}

public class EmployeeSkillFilterRequest
{
    public Guid? EmployeeId { get; set; }
    public Guid? SkillSetId { get; set; }
    public string? Department { get; set; }
    public int? ProficiencyLevel { get; set; }
    public string? Search { get; set; }
}

public class AppraisalFilterRequest
{
    public Guid? AppraisalCycleId { get; set; }
    public string? Status { get; set; }
    public string? Department { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? ManagerId { get; set; }
    public string? Search { get; set; }
}

public class PerformanceReportFilterRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Department { get; set; }
    public Guid? AppraisalCycleId { get; set; }
    public decimal? Rating { get; set; }
}

public class CreateSkillSetRequest
{
    public string SkillName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateSkillSetRequest
{
    public string SkillName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class CreateEmployeeSkillRequest
{
    [JsonIgnore]
    public Guid? EmployeeId { get; set; }
    public Guid SkillId { get; set; }
    public int ProficiencyLevel { get; set; }
    public Guid? AssessedBy { get; set; }
    public DateTime? LastAssessed { get; set; }
    public string? Notes { get; set; }
}

public class UpdateEmployeeSkillRequest
{
    public int ProficiencyLevel { get; set; }
    public Guid? AssessedBy { get; set; }
    public DateTime? LastAssessed { get; set; }
    public string? Notes { get; set; }
}

public class KRADto
{
    public Guid KraId { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid CycleId { get; set; }
    public string CycleName { get; set; }

    public string Title { get; set; } = string.Empty;

    public string KraDescription { get; set; } = string.Empty;

    public string KraRate { get; set; } = string.Empty; // varchar in Di
    public int noOfGoals { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
}


public class CreateKRARequest
{
    public string Title { get; set; } = string.Empty;

    public string KraDescription { get; set; } = string.Empty;
    public Guid CycleId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateKRARequest
{
    public string Title { get; set; } = string.Empty;
    public string KraDescription { get; set; } = string.Empty;
    public Guid CycleId { get; set; }

    public bool IsActive { get; set; }
}

//Goals
public class CreateGoalRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid KRAId { get; set; }
    public string Progress { get; set; } = "tostart"; // default
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class GoalDto
{
    public Guid GoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid organizationId { get; set; }
    public string Progress { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? KRAId { get; set; }
    public string? KraName { get; set; }
    public Guid? AssignedTo { get; set; }
    public Guid? createdBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? AssignedtoName { get; set; }
    public bool IsActive { get; set; }
    public bool? IsSelfAssessmentEnable { get; set; }
    public bool? ManagerReview { get; set; }
    public bool? IsAppraisalEnable { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class UpdateAppraisalCycleRequest
{
    public string CycleName { get; set; } = string.Empty;
    public string? CycleType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? ReviewStartDate { get; set; }
    public DateTime? ReviewEndDate { get; set; }
    public string? Status { get; set; }
    public bool IsSelfAssessmentEnable { get; set; }
    public bool ManagerReview { get; set; }
    public bool IsAppraisalEnable { get; set; }
    public string? Description { get; set; }
}

public class UpdateGoalRequest
{
    public Guid GoalId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Progress { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid KRAId { get; set; }
}


public class SubmitAppraisalRequest
{
    public decimal? OverallRating { get; set; }
    public Dictionary<string, object> KraRatings { get; set; } = new();
    public Dictionary<string, object> SkillRatings { get; set; } = new();
    public string? Feedback { get; set; }
    public string? ImprovementAreas { get; set; }
    public string? DevelopmentPlan { get; set; }
}

public class ReviewAppraisalRequest
{
    public string ReviewType { get; set; } = string.Empty;
    public decimal? OverallRating { get; set; }
    public Dictionary<string, object> KraRatings { get; set; } = new();
    public Dictionary<string, object> SkillRatings { get; set; } = new();
    public string? ManagerFeedback { get; set; }
    public string? ReviewComments { get; set; }
    public string Status { get; set; } = string.Empty;
}
public class PerformanceReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<EmployeeAppraisalDto> Appraisals { get; set; } = new();
    public PerformanceSummaryDto Summary { get; set; } = new();
    public List<DepartmentPerformanceDto> DepartmentBreakdown { get; set; } = new();
    public List<TopPerformerDto> TopPerformers { get; set; } = new();
}

public class DepartmentPerformanceDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int EmployeeCount { get; set; }
    public int CompletedAppraisals { get; set; }
    public int PendingAppraisals { get; set; }
}



public class CreateSelfAssessmentRequest
{
    public Guid GoalId { get; set; }
    public Guid KraId { get; set; }
    public decimal SelfRating { get; set; }  // 1 to 5
    public string SelfComment { get; set; } = string.Empty;
}


public class SelfAssessmentDto
{
    public Guid SelfAssessmentId { get; set; }
    public Guid GoalId { get; set; }
    public Guid KraId { get; set; }
    public Guid OrganizationId { get; set; }

    public decimal SelfRating { get; set; }               // Matches selfrating in DB
    public string? SelfComment { get; set; }          // Matches selfcomment in DB

    public string Status { get; set; } = "SUBMITTED"; // Default status
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? GoalName { get; set; }
    public string? KraName { get; set; }
    public string? EmployeeName { get; set; }
    public Guid? CycleId { get; set; }
    public string? CycleName { get; set; }
    public bool? IsSelfAssessmentEnable { get; set; }
    public bool? ManagerReview { get; set; }
    public bool? IsAppraisalEnable { get; set; }
}

public class ManagerReviewRequest
{

    public Guid EmployeeId { get; set; }

    public Guid CycleId { get; set; }

    public Guid KraId { get; set; }

    public Guid GoalId { get; set; }

    public decimal Rating { get; set; }

    public string? Feedback { get; set; }

    public string? ImprovementArea { get; set; }

    public string Status { get; set; } = "SUBMITTED";
}

public class SearchManagersDto
{
    public Guid? kraid { get; set; }
    public Guid? CycleId { get; set; }
    public string? searchQuery { get; set; }
}

public class ManagerReviewDto
{
    public Guid ManagerReviewId { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid ManagerId { get; set; }
    public string ManagerName { get; set; } = string.Empty;

    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;

    public Guid CycleId { get; set; }
    public string CycleName { get; set; } = string.Empty;

    public Guid KraId { get; set; }
    public string KraName { get; set; } = string.Empty;

    public Guid GoalId { get; set; }
    public string GoalName { get; set; } = string.Empty;

    public decimal Rating { get; set; }

    public string? Feedback { get; set; }

    public string? ImprovementArea { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

// HR Review Request DTO


public class HrReviewRequest
{
    public Guid EmployeeId { get; set; }
    public Guid CycleId { get; set; }

    public decimal FinalRating { get; set; }
    public string? HrComments { get; set; }
    public string? ImprovementArea { get; set; }
    public string? Feedback { get; set; }

    public string Status { get; set; } = "SUBMITTED";
}

// HR Review Response DTO

public class HrReviewDto
{
    public Guid HrReviewId { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid CycleId { get; set; }
    public string CycleName { get; set; } = string.Empty;

    public Guid HrId { get; set; }
    public string HrName { get; set; } = string.Empty;

    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;

    public decimal FinalRating { get; set; }

    public string? HrComments { get; set; }
    public string? ImprovementArea { get; set; }
    public string? Feedback { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
public class SearchAppraisalDto
{
    public Guid? CycleId { get; set; }
    public decimal? Rating { get; set; }
    public string? SearchQuery { get; set; }
}

public class ConsolidateAppraisalRequest
{
    public Guid EmployeeId { get; set; }
    public Guid CycleId { get; set; }
    public Guid ReviewerId { get; set; }
}

public class EmployeePerformanceHistoryDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public List<EmployeeAppraisalDto> Appraisals { get; set; } = new();
    public decimal? AverageRating { get; set; }
    public int TotalAppraisals { get; set; }
    public int CompletedAppraisals { get; set; }
    public List<SkillGapAnalysisDto> SkillGaps { get; set; } = new();
}

public class SkillGapAnalysisDto
{
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int RequiredLevel { get; set; }
    public int CurrentLevel { get; set; }
    public int Gap { get; set; }
}

// Team Performance Overview DTOs
public class TeamPerformanceOverviewDto
{
    public List<TeamPerformanceEmployeeDto> Employees { get; set; } = new();
}

public class TeamPerformanceEmployeeDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public List<CycleRatingDto> CycleRatings { get; set; } = new();
    public List<string> AssessedSkills { get; set; } = new();
}

public class CycleRatingDto
{
    public Guid CycleId { get; set; }
    public string CycleName { get; set; } = string.Empty;
    public decimal? Rating { get; set; }
}
public class EmployeeSkillSummaryDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public List<string> SkillNames { get; set; } = new();
    public int SkillCount { get; set; }
    public double? AverageProficiency { get; set; }
}
public class EmployeeSkillFullDetailDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public List<EmployeeSkillDto> Skills { get; set; } = new();
}
public class SkillWithEmployeesDto
{
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int EmployeeCount { get; set; }
    public List<EmployeeSkillDetailDto> Employees { get; set; } = new();
}
public class EmployeeSkillDetailDto
{
    public Guid EmployeeSkillId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? EmployeeEmail { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public int ProficiencyLevel { get; set; }
    public string? Notes { get; set; }
    public string? AssessorName { get; set; }
    public DateTime? LastAssessed { get; set; }
}