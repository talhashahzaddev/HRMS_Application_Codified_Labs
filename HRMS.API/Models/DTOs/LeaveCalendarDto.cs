namespace HRMS.API.Models.DTOs
{
    public class LeaveCalendarDto
    {
        public Guid RequestId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid LeaveTypeId { get; set; }
        public string? LeaveTypeName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysRequested { get; set; }
        public string? Status { get; set; }
        public string? Color { get; set; }
        public string? Reason { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

}
