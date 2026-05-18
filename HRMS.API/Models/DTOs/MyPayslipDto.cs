namespace HRMS.API.Models.DTOs;

public class MyPayslipDto
{
    public Guid PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public string? PayslipUploadUrl { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal NetSalary { get; set; }
    public DateTime CalculatedAt { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid OrganizationId { get; set; }
}
