using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("demo_inquiries", Schema = "admin")]
public class DemoInquiry : BaseEntity
{
    [Key]
    [Column("inquiry_id")]
    public Guid InquiryId { get; set; }

    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("company_name")]
    public string? CompanyName { get; set; }

    [Column("company_size")]
    public string? CompanySize { get; set; }

    [Column("industry")]
    public string? Industry { get; set; }

    [Column("country")]
    public string? Country { get; set; }

    [Column("message")]
    public string? Message { get; set; }

    [Column("status")]
    public string Status { get; set; } = "new";

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("source")]
    public string Source { get; set; } = "website";
}
