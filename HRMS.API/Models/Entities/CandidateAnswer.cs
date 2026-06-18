using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("candidateanswers", Schema = "jobs")]
public class CandidateAnswer
{
    [Column("answerid")]
    public Guid AnswerId { get; set; }

    [Column("jobapplyid")]
    public Guid JobApplyId { get; set; }

    [Column("questionid")]
    public Guid QuestionId { get; set; }

    [Column("answertext")]
    public string? AnswerText { get; set; }

    [Column("createdat")]
    public DateTime? CreatedAt { get; set; }
}
