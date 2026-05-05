namespace HRMS.API.Requests
{
    public class CreateLeaveTypeRequest
    {
        public string TypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxDaysPerYear { get; set; }
        public bool IsPaid { get; set; }
        public bool IsHalfPaid { get; set; }
        public bool CarryForwardAllowed { get; set; }
        public int MaxCarryForwardDays { get; set; }
        public string Color { get; set; } = string.Empty;
    }
    public class EditLeaveTypeRequest
    {
        public string TypeName { get; set; }
        public string Description { get; set; }
        public int MaxDaysPerYear { get; set; }
        public bool IsPaid { get; set; }
        public bool CarryForwardAllowed { get; set; }
        public int MaxCarryForwardDays { get; set; }
        public string Color { get; set; }
        public bool IsActive { get; set; }  
    }


}
