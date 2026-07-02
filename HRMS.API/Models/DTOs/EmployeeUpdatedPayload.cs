namespace HRMS.API.Models.DTOs
{
    public class EmployeeUpdatedPayload
    {
        public Guid EmployeeId { get; set; }
        public Guid OrganizationId { get; set; }
        public string Message { get; set; } = "Employee updated";
    }
}
