using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("stagemaster", Schema = "jobs")]
public class StageMaster
{
    [Column("stageid")]
    public Guid StageId { get; set; }

    [Column("organizationid")]
    public Guid OrganizationId { get; set; }

    [Column("stagename")]
    public string StageName { get; set; } = string.Empty;

    [Column("stageorder")]
    public int? StageOrder { get; set; }

    [Column("isinterviewstage")]
    public bool IsInterviewStage { get; set; } = false;

    [Column("createdat")]
    public DateTime? CreatedAt { get; set; }
}
