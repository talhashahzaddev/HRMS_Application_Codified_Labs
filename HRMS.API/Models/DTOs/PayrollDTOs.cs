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
    public Guid PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? PositionTitle { get; set; }
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

// Additional DTOs for frontend consumption
public class PayrollFilterRequest
{
    public Guid? PayrollPeriodId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? Department { get; set; }
    public string? Status { get; set; }
    public string? Search { get; set; }
}

public class PayrollReportFilterRequest
{
    public Guid? PayrollPeriodId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Department { get; set; }
    public string? Status { get; set; }
}

public class PayrollReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<PayrollEntryDto> Entries { get; set; } = new();
    public PayrollSummaryDto Summary { get; set; } = new();
    public List<DepartmentPayrollBreakdownDto> DepartmentBreakdown { get; set; } = new();
}

public class PayrollStatisticsDto
{
    public decimal TotalPayroll { get; set; }
    public decimal AveragePayroll { get; set; }
    public int TotalEmployees { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalAllowances { get; set; }
    public List<MonthlyPayrollDto> MonthlyTrends { get; set; } = new();
}

public class MonthlyPayrollDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int EmployeeCount { get; set; }
}

public class DepartmentPayrollBreakdownDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int EmployeeCount { get; set; }
    public decimal AverageAmount { get; set; }
    public decimal Percentage { get; set; }
}

public class PayrollTaxSummaryDto
{
    public decimal GrossPay { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal SocialSecurity { get; set; }
    public decimal Medicare { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
}

public class PayrollDeductionSummaryDto
{
    public decimal TotalDeductions { get; set; }
    public decimal TaxDeductions { get; set; }
    public decimal InsuranceDeductions { get; set; }
    public decimal LoanDeductions { get; set; }
    public decimal OtherDeductions { get; set; }
    public List<DeductionItemDto> DeductionItems { get; set; } = new();
}

public class DeductionItemDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class UpdatePayrollPeriodRequest
{
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class BulkPayrollRequest
{
    public List<Guid> EntryIds { get; set; } = new();
}

public class RejectPayrollRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class GeneratePayslipsRequest
{
    public Guid PayrollPeriodId { get; set; }
    public List<Guid>? EmployeeIds { get; set; }
}

public class CalculateTaxRequest
{
    public Guid EmployeeId { get; set; }
    public decimal GrossPay { get; set; }
    public Guid PayrollPeriodId { get; set; }
}

public class PayrollCalculationResult
{
    public int EntriesCreated { get; set; }
    public Guid PeriodId { get; set; }
    public int TotalEmployees { get; set; }
    public decimal TotalGrossAmount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal TotalNetAmount { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public class PayrollProcessingHistoryDto
{
    public Guid PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public int EntriesCreated { get; set; }
    public decimal TotalGrossAmount { get; set; }
    public decimal TotalNetAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ProcessedBy { get; set; } = string.Empty;
}