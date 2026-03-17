namespace HRMS.API.Models.Entities;

public class CompanyHoliday : BaseEntity
{
    public Guid HolidayId { get; set; }
    public Guid OrganizationId { get; set; }
    public string HolidayName { get; set; } = string.Empty;
    public DateTime HolidayDate { get; set; }
    public string HolidayType { get; set; } = "mandatory"; // mandatory, restricted, optional
    public string? Description { get; set; }
    public bool IsHalfDay { get; set; } = false;
    public int Year { get; set; }
    public string ApplicableTo { get; set; } = "all"; // all, department, location
    public string? ApplicableValue { get; set; }
    public Guid? CatalogHolidayId { get; set; }
    public Guid? CreatedBy { get; set; }
    public bool IsActive { get; set; } = true;
}
