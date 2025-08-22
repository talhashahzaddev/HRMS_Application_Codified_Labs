using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

public abstract class BaseEntity
{
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
