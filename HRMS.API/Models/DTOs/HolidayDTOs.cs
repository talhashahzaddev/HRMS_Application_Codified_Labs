using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HRMS.API.Models.DTOs;

// ========================
// Holiday Catalog DTOs
// ========================

public class HolidayCatalogDto
{
    [JsonPropertyName("catalogId")]
    public Guid CatalogId { get; set; }

    [JsonPropertyName("countryCode")]
    public string CountryCode { get; set; } = string.Empty;

    [JsonPropertyName("countryName")]
    public string CountryName { get; set; } = string.Empty;

    [JsonPropertyName("holidayName")]
    public string HolidayName { get; set; } = string.Empty;

    [JsonPropertyName("holidayDate")]
    public DateTime HolidayDate { get; set; }

    [JsonPropertyName("isRecurring")]
    public bool IsRecurring { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class CatalogCountryDto
{
    [JsonPropertyName("countryCode")]
    public string CountryCode { get; set; } = string.Empty;

    [JsonPropertyName("countryName")]
    public string CountryName { get; set; } = string.Empty;

    [JsonPropertyName("holidayCount")]
    public int HolidayCount { get; set; }
}

// ========================
// Company Holiday DTOs
// ========================

public class CompanyHolidayDto
{
    [JsonPropertyName("holidayId")]
    public Guid HolidayId { get; set; }

    [JsonPropertyName("organizationId")]
    public Guid OrganizationId { get; set; }

    [JsonPropertyName("holidayName")]
    public string HolidayName { get; set; } = string.Empty;

    [JsonPropertyName("holidayDate")]
    public DateTime HolidayDate { get; set; }

    [JsonPropertyName("holidayType")]
    public string HolidayType { get; set; } = "mandatory";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isHalfDay")]
    public bool IsHalfDay { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("applicableTo")]
    public string ApplicableTo { get; set; } = "all";

    [JsonPropertyName("applicableValue")]
    public string? ApplicableValue { get; set; }

    [JsonPropertyName("catalogHolidayId")]
    public Guid? CatalogHolidayId { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("createdBy")]
    public Guid? CreatedBy { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("employeeIds")]
    public List<Guid>? EmployeeIds { get; set; }
}

public class CreateCompanyHolidayDto
{
    [Required]
    [JsonPropertyName("holidayName")]
    public string HolidayName { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("holidayDate")]
    public DateTime HolidayDate { get; set; }

    [JsonPropertyName("holidayType")]
    public string HolidayType { get; set; } = "mandatory";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isHalfDay")]
    public bool IsHalfDay { get; set; } = false;

    [JsonPropertyName("applicableTo")]
    public string ApplicableTo { get; set; } = "all";

    [JsonPropertyName("applicableValue")]
    public string? ApplicableValue { get; set; }

    [JsonPropertyName("catalogHolidayId")]
    public Guid? CatalogHolidayId { get; set; }

    [JsonPropertyName("employeeIds")]
    public List<Guid>? EmployeeIds { get; set; }
}

public class UpdateCompanyHolidayDto
{
    [Required]
    [JsonPropertyName("holidayName")]
    public string HolidayName { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("holidayDate")]
    public DateTime HolidayDate { get; set; }

    [JsonPropertyName("holidayType")]
    public string HolidayType { get; set; } = "mandatory";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isHalfDay")]
    public bool IsHalfDay { get; set; } = false;

    [JsonPropertyName("applicableTo")]
    public string ApplicableTo { get; set; } = "all";

    [JsonPropertyName("applicableValue")]
    public string? ApplicableValue { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("employeeIds")]
    public List<Guid>? EmployeeIds { get; set; }
}

public class ImportFromCatalogDto
{
    [Required]
    [JsonPropertyName("catalogHolidayIds")]
    public List<Guid> CatalogHolidayIds { get; set; } = new();

    [JsonPropertyName("holidayType")]
    public string HolidayType { get; set; } = "mandatory";

    [JsonPropertyName("year")]
    public int? Year { get; set; }
}

// ========================
// Employee Restricted Holiday DTOs
// ========================

public class EmployeeRestrictedHolidayDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("employeeId")]
    public Guid EmployeeId { get; set; }

    [JsonPropertyName("companyHolidayId")]
    public Guid CompanyHolidayId { get; set; }

    [JsonPropertyName("holidayName")]
    public string HolidayName { get; set; } = string.Empty;

    [JsonPropertyName("holidayDate")]
    public DateTime HolidayDate { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "selected";

    [JsonPropertyName("selectedAt")]
    public DateTime SelectedAt { get; set; }
}

public class SelectRestrictedHolidayDto
{
    [Required]
    [JsonPropertyName("companyHolidayId")]
    public Guid CompanyHolidayId { get; set; }
}

// ========================
// Holiday Range Query DTOs
// ========================

public class HolidayRangeDto
{
    [JsonPropertyName("holidayId")]
    public Guid HolidayId { get; set; }

    [JsonPropertyName("holidayName")]
    public string HolidayName { get; set; } = string.Empty;

    [JsonPropertyName("holidayDate")]
    public DateTime HolidayDate { get; set; }

    [JsonPropertyName("holidayType")]
    public string HolidayType { get; set; } = string.Empty;

    [JsonPropertyName("isHalfDay")]
    public bool IsHalfDay { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("applicableTo")]
    public string? ApplicableTo { get; set; }

    [JsonPropertyName("applicableValue")]
    public string? ApplicableValue { get; set; }
}

public class HolidaySummaryDto
{
    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("totalHolidays")]
    public int TotalHolidays { get; set; }

    [JsonPropertyName("mandatoryCount")]
    public int MandatoryCount { get; set; }

    [JsonPropertyName("restrictedCount")]
    public int RestrictedCount { get; set; }

    [JsonPropertyName("optionalCount")]
    public int OptionalCount { get; set; }

    [JsonPropertyName("upcomingHoliday")]
    public CompanyHolidayDto? UpcomingHoliday { get; set; }
}
