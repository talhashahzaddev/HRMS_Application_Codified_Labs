using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("jobquestions", Schema = "jobs")]
public class JobQuestion
{
    [Column("jobquestionid")]
    public Guid JobQuestionId { get; set; }

    [Column("jobid")]
    public Guid JobId { get; set; }

    [Column("questionid")]
    public Guid QuestionId { get; set; }

    [Column("isrequired")]
    public bool IsRequired { get; set; } = true;

    [Column("createdat")]
    public DateTime? CreatedAt { get; set; }
}
