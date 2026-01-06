using System.ComponentModel.DataAnnotations;

namespace HRMS.API.Models.DTOs;

public class SubscriptionPlanDto
{
    public Guid PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? MonthlyPrice { get; set; }
    public decimal? AnnualPrice { get; set; }
    public int? MaxUsers { get; set; }
    public Dictionary<string, object>? Features { get; set; }
    public bool IsCustom { get; set; }
    public bool IsActive { get; set; }
}

public class CreateSubscriptionPlanRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public decimal? MonthlyPrice { get; set; }
    
    public decimal? AnnualPrice { get; set; }
    
    public int? MaxUsers { get; set; }
    
    public Dictionary<string, object>? Features { get; set; }
    
    public bool IsCustom { get; set; } = false;
    
    public bool IsActive { get; set; } = true;
}

public class UpdateSubscriptionPlanRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public decimal? MonthlyPrice { get; set; }
    
    public decimal? AnnualPrice { get; set; }
    
    public int? MaxUsers { get; set; }
    
    public Dictionary<string, object>? Features { get; set; }
    
    public bool IsCustom { get; set; }
    
    public bool IsActive { get; set; }
}

