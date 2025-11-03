namespace HRMS.API.Requests;

public class CalculatePayrollRequest
{
    public Guid? RuleId { get; set; }
    public List<Guid>? DepartmentIds { get; set; }
    public bool IncludeTax { get; set; } = false;
}


