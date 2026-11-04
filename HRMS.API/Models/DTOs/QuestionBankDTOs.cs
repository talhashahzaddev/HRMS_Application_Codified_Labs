using System.ComponentModel.DataAnnotations;

namespace HRMS.API.Models.DTOs;

// ==================== Question Categories ====================
public class QuestionCategoryDto
{
    public Guid CategoryId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int QuestionCount { get; set; }
}

public class CreateQuestionCategoryRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}

public class UpdateQuestionCategoryRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}

// ==================== Questions ====================
public class QuestionDto
{
    public Guid QuestionId { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public Guid OrganizationId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "Text";
    public string? Options { get; set; }
    public bool IsKnockout { get; set; }
    public string? ExpectedAnswer { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateQuestionRequest
{
    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    public string QuestionText { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string QuestionType { get; set; } = "Text";

    public string? Options { get; set; }
    public bool IsKnockout { get; set; }
    public string? ExpectedAnswer { get; set; }
}

public class UpdateQuestionRequest
{
    public string? QuestionText { get; set; }

    [MaxLength(50)]
    public string? QuestionType { get; set; }

    public string? Options { get; set; }
    public bool? IsKnockout { get; set; }
    public string? ExpectedAnswer { get; set; }
    public Guid? CategoryId { get; set; }
}

// ==================== Job Questions (Mapping) ====================
public class JobQuestionDto
{
    public Guid JobQuestionId { get; set; }
    public Guid JobId { get; set; }
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "Text";
    public string? Options { get; set; }
    public bool IsKnockout { get; set; }
    public string? ExpectedAnswer { get; set; }
    public bool IsRequired { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
}

public class AttachQuestionsToJobRequest
{
    [Required]
    public Guid JobId { get; set; }

    /// <summary>List of question IDs to attach to the job.</summary>
    [Required]
    public List<Guid> QuestionIds { get; set; } = new();
}

public class AttachCategoryToJobRequest
{
    [Required]
    public Guid JobId { get; set; }

    /// <summary>Category ID — all questions in this category will be attached to the job.</summary>
    [Required]
    public Guid CategoryId { get; set; }
}

// ==================== Candidate Answers ====================
public class CandidateAnswerDto
{
    public Guid AnswerId { get; set; }
    public Guid JobApplyId { get; set; }
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "Text";
    public bool IsKnockout { get; set; }
    public string? ExpectedAnswer { get; set; }
    public string? AnswerText { get; set; }
    public bool? Passed { get; set; }
}

public class SubmitCandidateAnswerRequest
{
    [Required]
    public Guid QuestionId { get; set; }

    public string? AnswerText { get; set; }
}

// ==================== Parsed Resume ====================
public class ParsedResumeDto
{
    public Guid ParsedResumeId { get; set; }
    public Guid JobApplyId { get; set; }
    public string? ContactDetails { get; set; }
    public string? Skills { get; set; }
    public string? WorkExperience { get; set; }
    public string? Education { get; set; }
    public string ParseStatus { get; set; } = "Pending";
    public string? ParseError { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// ==================== Apply With Resume (Combined Payload) ====================
public class ApplyWithResumeRequest
{
    [Required]
    public Guid JobId { get; set; }

    [MaxLength(200)]
    public string? CandidateName { get; set; }

    [MaxLength(200)]
    public string? CandidateEmail { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    public string? LinkedInUrl { get; set; }
    public string? CoverLetter { get; set; }

    [MaxLength(100)]
    public string? ApplicationSource { get; set; }

    /// <summary>Candidate's answers to the job's screening questions.</summary>
    public List<SubmitCandidateAnswerRequest>? Answers { get; set; }
}
