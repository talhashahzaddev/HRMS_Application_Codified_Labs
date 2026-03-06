using System.ComponentModel.DataAnnotations;

namespace HRMS.API.Models.DTOs;

// ========== Demo Inquiry DTOs ==========

public class DemoInquiryDto
{
    public Guid InquiryId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanySize { get; set; }
    public string? Industry { get; set; }
    public string? Country { get; set; }
    public string? Message { get; set; }
    public string Status { get; set; } = "new";
    public string? Notes { get; set; }
    public string Source { get; set; } = "website";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateDemoInquiryRequest
{
    public string FullName { get; set; }
    public string EmailAddress { get; set; }
    public string? CompanySize { get; set; }
    public string? Country { get; set; }
    public string? PhoneNumber { get; set; }
    public string? CompanyName { get; set; }
    public string? Industry { get; set; }
    public string? Message { get; set; }
}

public class UpdateDemoInquiryStatusRequest
{
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

public class DemoInquiryFilterRequest
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? SortBy { get; set; } = "createdat";
    public string? SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
