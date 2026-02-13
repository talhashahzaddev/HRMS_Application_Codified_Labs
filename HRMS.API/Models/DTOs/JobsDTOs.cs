using System.ComponentModel.DataAnnotations;

namespace HRMS.API.Models.DTOs;

// ==================== Stage Master ====================
public class StageMasterDto
{
    public Guid StageId { get; set; }
    public Guid OrganizationId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int? StageOrder { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CreateStageMasterRequest
{
    [Required]
    [MaxLength(100)]
    public string StageName { get; set; } = string.Empty;

    public int? StageOrder { get; set; }
}

public class UpdateStageMasterRequest
{
    [Required]
    [MaxLength(100)]
    public string StageName { get; set; } = string.Empty;

    public int? StageOrder { get; set; }
}

// ==================== Job Openings ====================
public class JobOpeningDto
{
    public Guid JobId { get; set; }
    public Guid OrganizationId { get; set; }
    public string JobRoleName { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? ExperienceMin { get; set; }
    public int? ExperienceMax { get; set; }
    public decimal? CtcMin { get; set; }
    public decimal? CtcMax { get; set; }
    public string? Currency { get; set; }
    public int Vacancies { get; set; } = 1;
    public string? Location { get; set; }
    public string? WorkMode { get; set; }
    public string? EmploymentType { get; set; }
    public string? JobIntroduction { get; set; }
    public string? Responsibilities { get; set; }
    public string? Skillset { get; set; }
    public DateTime? LastDate { get; set; }
    public DateTime? PostedDate { get; set; }
    public string PostedAs { get; set; } = string.Empty;
    public string? ExternalLink { get; set; }
    public string Status { get; set; } = "Open";
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateJobOpeningRequest
{
    [Required]
    [MaxLength(200)]
    public string JobRoleName { get; set; } = string.Empty;

    public Guid? DepartmentId { get; set; }
    public int? ExperienceMin { get; set; }
    public int? ExperienceMax { get; set; }
    public decimal? CtcMin { get; set; }
    public decimal? CtcMax { get; set; }

    [MaxLength(10)]
    public string? Currency { get; set; }

    public int Vacancies { get; set; } = 1;

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(50)]
    public string? WorkMode { get; set; }

    [MaxLength(50)]
    public string? EmploymentType { get; set; }

    public string? JobIntroduction { get; set; }
    public string? Responsibilities { get; set; }
    public string? Skillset { get; set; }
    public DateTime? LastDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string PostedAs { get; set; } = string.Empty;

    public string? ExternalLink { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }
}

public class UpdateJobOpeningRequest
{
    [MaxLength(200)]
    public string? JobRoleName { get; set; }

    public Guid? DepartmentId { get; set; }
    public int? ExperienceMin { get; set; }
    public int? ExperienceMax { get; set; }
    public decimal? CtcMin { get; set; }
    public decimal? CtcMax { get; set; }

    [MaxLength(10)]
    public string? Currency { get; set; }

    public int? Vacancies { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(50)]
    public string? WorkMode { get; set; }

    [MaxLength(50)]
    public string? EmploymentType { get; set; }

    public string? JobIntroduction { get; set; }
    public string? Responsibilities { get; set; }
    public string? Skillset { get; set; }
    public DateTime? LastDate { get; set; }

    [MaxLength(20)]
    public string? PostedAs { get; set; }

    public string? ExternalLink { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }
}

// ==================== Job Applications ====================
public class JobApplicationDto
{
    public Guid JobApplyId { get; set; }
    public Guid JobId { get; set; }
    public string? JobRoleName { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public Guid? CandidateId { get; set; }
    public string? CandidateName { get; set; }
    public string? CandidateEmail { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? ResumeUrl { get; set; }
    public string? Phone { get; set; }
    public string? CoverLetter { get; set; }
    public Guid? CurrentStageId { get; set; }
    public string? CurrentStageName { get; set; }
    public string? ApplicationSource { get; set; }
    public string Status { get; set; } = "Applied";
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class CreateJobApplicationRequest
{
    [Required]
    public Guid JobId { get; set; }

    [MaxLength(200)]
    public string? CandidateName { get; set; }

    [MaxLength(200)]
    public string? CandidateEmail { get; set; }

    public string? LinkedInUrl { get; set; }
    public string? ResumeUrl { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    public string? CoverLetter { get; set; }
    public Guid? CurrentStageId { get; set; }

    [MaxLength(100)]
    public string? ApplicationSource { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }
}

// ==================== Application Stages (interviewer info for GET) ====================
public class InterviewerInfoDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? PositionTitle { get; set; }
}

public class ApplicationStageDto
{
    public Guid ApplicationStageId { get; set; }
    public Guid JobApplyId { get; set; }
    public Guid? JobId { get; set; }
    public string? JobRoleName { get; set; }
    public string? CandidateName { get; set; }
    public Guid StageId { get; set; }
    public string? StageName { get; set; }
    public string? Notes { get; set; }
    public List<Guid> InterviewerIds { get; set; } = new();
    public List<InterviewerInfoDto> Interviewers { get; set; } = new();
    public string? Type { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public Guid? UpdatedBy { get; set; }
    public string? UpdatedByName { get; set; }
}

public class CreateApplicationStageRequest
{
    [Required]
    public Guid JobApplyId { get; set; }

    [Required]
    public Guid StageId { get; set; }

    public string? Notes { get; set; }

    /// <summary>One or more employee IDs (interviewers).</summary>
    public List<Guid>? InterviewerIds { get; set; }

    [MaxLength(50)]
    public string? Type { get; set; }
}

public class UpdateApplicationStageRequest
{
    public string? Notes { get; set; }

    /// <summary>One or more employee IDs (interviewers).</summary>
    public List<Guid>? InterviewerIds { get; set; }

    [MaxLength(50)]
    public string? Type { get; set; }
}

public class UpdateJobApplicationRequest
{
    [MaxLength(200)]
    public string? CandidateName { get; set; }

    [MaxLength(200)]
    public string? CandidateEmail { get; set; }

    public string? LinkedInUrl { get; set; }
    public string? ResumeUrl { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    public string? CoverLetter { get; set; }
    public Guid? CurrentStageId { get; set; }

    [MaxLength(100)]
    public string? ApplicationSource { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }
}
