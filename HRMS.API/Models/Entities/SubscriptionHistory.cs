using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("subscription_history", Schema = "payment")]
public class SubscriptionHistory : BaseEntity
{
    [Key]
    [Column("history_id")]
    public Guid HistoryId { get; set; }

    [Column("subscription_id")]
    public Guid SubscriptionId { get; set; }

    [Column("company_id")]
    public Guid CompanyId { get; set; }

    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [Column("old_plan_id")]
    public Guid? OldPlanId { get; set; }

    [Column("new_plan_id")]
    public Guid? NewPlanId { get; set; }

    [Column("old_end_date")]
    public DateTime? OldEndDate { get; set; }

    [Column("new_end_date")]
    public DateTime? NewEndDate { get; set; }

    [Column("performed_by")]
    public Guid? PerformedBy { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }
}
