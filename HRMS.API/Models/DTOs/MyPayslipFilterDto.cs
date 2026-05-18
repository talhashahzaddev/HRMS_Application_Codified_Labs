namespace HRMS.API.Models.DTOs;

public class MyPayslipFilterDto
{
    public Guid? PeriodId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
