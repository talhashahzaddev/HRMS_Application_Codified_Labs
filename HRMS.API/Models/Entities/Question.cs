using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("questions", Schema = "jobs")]
public class Question
{
    [Column("questionid")]
    public Guid QuestionId { get; set; }

    [Column("categoryid")]
    public Guid CategoryId { get; set; }

    [Column("organizationid")]
    public Guid OrganizationId { get; set; }

    [Column("questiontext")]
    public string QuestionText { get; set; } = string.Empty;

    [Column("questiontype")]
    public string QuestionType { get; set; } = "Text";

    [Column("options")]
    public string? Options { get; set; }

    [Column("isknockout")]
    public bool IsKnockout { get; set; }

    [Column("expectedanswer")]
    public string? ExpectedAnswer { get; set; }

    [Column("createdat")]
    public DateTime? CreatedAt { get; set; }

    [Column("updatedat")]
    public DateTime? UpdatedAt { get; set; }

    [Column("isdeleted")]
    public bool IsDeleted { get; set; }
}
