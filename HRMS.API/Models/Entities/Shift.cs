namespace HRMS.API.Models.Entities;

public class Shift : BaseEntity
{
    public Guid ShiftId { get; set; }
    public Guid OrganizationId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int BreakDuration { get; set; } = 0; // minutes
    public int[] DaysOfWeek { get; set; } = {1, 2, 3, 4, 5}; // Monday to Friday
    public string Timezone { get; set; } = "UTC";
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ICollection<EmploymentDetail> EmploymentDetails { get; set; } = new List<EmploymentDetail>();
}
