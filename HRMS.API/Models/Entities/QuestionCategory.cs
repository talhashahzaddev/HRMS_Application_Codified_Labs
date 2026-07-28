using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("questioncategories", Schema = "jobs")]
public class QuestionCategory
{
    [Column("categoryid")]
    public Guid CategoryId { get; set; }

    [Column("organizationid")]
    public Guid OrganizationId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("createdat")]
    public DateTime? CreatedAt { get; set; }

    [Column("updatedat")]
    public DateTime? UpdatedAt { get; set; }

    [Column("isdeleted")]
    public bool IsDeleted { get; set; }
}
