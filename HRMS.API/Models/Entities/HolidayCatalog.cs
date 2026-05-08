namespace HRMS.API.Models.Entities;

public class HolidayCatalog
{
    public Guid CatalogId { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string HolidayName { get; set; } = string.Empty;
    public DateTime HolidayDate { get; set; }
    public bool IsRecurring { get; set; } = true;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
