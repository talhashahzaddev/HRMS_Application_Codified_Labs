namespace HRMS.API.Models.Chatbot
{
    public class LeaveBalanceDTO
    {
        public string LeaveTypeName { get; set; }
        public string Color { get; set; }
        public decimal TotalDays { get; set; }
        public decimal UsedDays { get; set; }
        public decimal RemainingDays { get; set; }
    }

    public class ShiftStatusDto
    {
        // Shift Info
        public string ShiftName { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        // Attendance Info
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string Status { get; set; }
    }

    //public class ManagerDto
    //{
      //  public string FirstName { get; set; }
      //  public string LastName { get; set; }
      //  public string Email { get; set; }
      //  public string PositionTitle { get; set; }
   // }

    public class PayslipDto
    {
        public string PeriodName { get; set; }
        public decimal NetSalary { get; set; }
        public string Currency { get; set; }
        public DateTime? PayDate { get; set; }
    }
}