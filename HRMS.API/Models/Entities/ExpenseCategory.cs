using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("expensecategories", Schema = "expense")]
public class ExpenseCategory
{
    [Column("categoryid")]
    public Guid CategoryId { get; set; }

    [Column("organizationid")]
    public Guid OrganizationId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("createdat")]
    public DateTime? CreatedAt { get; set; }
}
