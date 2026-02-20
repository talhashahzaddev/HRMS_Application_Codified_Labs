using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("attendance_audit", Schema = "attendance")]
public class AttendanceAudit
{
    [Key]
    [Column("audit_id")]
    public Guid AuditId { get; set; }

    [Column("attendance_id")]
    public Guid? AttendanceId { get; set; }

    [Column("employee_id")]
    public Guid EmployeeId { get; set; }

    [Column("modified_by")]
    public Guid ModifiedBy { get; set; }

    [Column("modified_at")]
    public DateTime ModifiedAt { get; set; }

    [Column("change_type")]
    public string ChangeType { get; set; } = "UPDATE"; // UPDATE, INSERT

    [Column("old_values")]
    public string OldValues { get; set; } // JSON

    [Column("new_values")]
    public string NewValues { get; set; } // JSON

    [Column("ip_address")]
    public string? IpAddress { get; set; }
}
