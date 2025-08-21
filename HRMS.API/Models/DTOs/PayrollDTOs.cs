namespace HRMS.API.Models.DTOs;

public class PayrollPeriodDto
{
    public Guid PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? PayDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public decimal TotalGrossAmount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal TotalNetAmount { get; set; }
    public string? ProcessorName { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class CreatePayrollPeriodRequest
{
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? PayDate { get; set; }
}

public class PayrollEntryDto
{
    public Guid EntryId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public decimal BasicSalary { get; set; }
    public Dictionary<string, object> Allowances { get; set; } = new();
    public Dictionary<string, object> Deductions { get; set; } = new();
    public decimal OvertimeAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetSalary { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CalculatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class PayrollCalculationRequest
{
    public Guid PeriodId { get; set; }
}

public class PayrollSummaryDto
{
    public decimal TotalGrossSalary { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalNetSalary { get; set; }
    public int TotalEmployees { get; set; }
    public Dictionary<string, decimal> DepartmentWisePayroll { get; set; } = new();
}
