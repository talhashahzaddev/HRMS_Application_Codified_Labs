using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("applicationstages", Schema = "jobs")]
public class ApplicationStage
{
    [Column("applicationstageid")]
    public Guid ApplicationStageId { get; set; }

    [Column("jobapplyid")]
    public Guid JobApplyId { get; set; }

    [Column("stageid")]
    public Guid StageId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("interviewers")]
    public string? Interviewers { get; set; }

    [Column("type")]
    public string? Type { get; set; }

    [Column("updatedon")]
    public DateTime? UpdatedOn { get; set; }

    [Column("updatedby")]
    public Guid? UpdatedBy { get; set; }
}
