namespace HRMS.API.Models.Entities;

public class EmployeeRestrictedHoliday
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid CompanyHolidayId { get; set; }
    public string Status { get; set; } = "selected"; // selected, cancelled
    public DateTime SelectedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
