using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("parsedresumes", Schema = "jobs")]
public class ParsedResume
{
    [Column("parsedresumeid")]
    public Guid ParsedResumeId { get; set; }

    [Column("jobapplyid")]
    public Guid JobApplyId { get; set; }

    [Column("contactdetails")]
    public string? ContactDetails { get; set; }

    [Column("skills")]
    public string? Skills { get; set; }

    [Column("workexperience")]
    public string? WorkExperience { get; set; }

    [Column("education")]
    public string? Education { get; set; }

    [Column("rawtext")]
    public string? RawText { get; set; }

    [Column("parsestatus")]
    public string ParseStatus { get; set; } = "Pending";

    [Column("parseerror")]
    public string? ParseError { get; set; }

    [Column("createdat")]
    public DateTime? CreatedAt { get; set; }

    [Column("updatedat")]
    public DateTime? UpdatedAt { get; set; }
}
