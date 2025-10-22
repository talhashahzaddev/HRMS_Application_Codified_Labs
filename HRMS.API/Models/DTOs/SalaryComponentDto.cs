using System;

namespace HRMS.API.Models.DTOs;

public class SalaryComponentDto
{
    public Guid ComponentId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // 'earning' or 'deduction'
    public string CalculationType { get; set; } = string.Empty; // 'fixed' or 'percentage'
    public decimal Value { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateSalaryComponentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // 'earning' or 'deduction'
    public string CalculationType { get; set; } = string.Empty; // 'fixed' or 'percentage'
    public decimal Value { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateSalaryComponentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string CalculationType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public bool IsActive { get; set; }
}