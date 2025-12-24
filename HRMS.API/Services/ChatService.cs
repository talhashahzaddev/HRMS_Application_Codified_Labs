using HRMS.API.Services.Interfaces;
using HRMS_API; // Your ML Model namespace

namespace HRMS.API.Services
{
    public class ChatService : IChatService
    {
        private readonly ILeaveService _leaveService;
        private readonly IAttendanceService _attendanceService;
        private readonly IPayrollService _payrollService;
        private readonly IEmployeeService _employeeService;

        public ChatService(
            ILeaveService leaveService,
            IAttendanceService attendanceService,
            IPayrollService payrollService,
            IEmployeeService employeeService)
        {
            _leaveService = leaveService;
            _attendanceService = attendanceService;
            _payrollService = payrollService;
            _employeeService = employeeService;
        }

        public async Task<string> ProcessUserMessageAsync(string userText, Guid userId)
        {
            // 1. PREDICT INTENT
            var input = new HrmsIntentModel.ModelInput { UserText = userText };
            var prediction = HrmsIntentModel.Predict(input);
            string intent = prediction.PredictedLabel;
            float confidence = prediction.Score.Max();

            if (confidence < 0.60)
                return "I'm not sure I understood that. Could you ask in a different way?";

            // 2. GET EMPLOYEE CONTEXT (Resolves UserId -> EmployeeId)
            // Your EmployeeService handles the conversion internally
            var employeeProfile = await _employeeService.GetEmployeeByIdAsync(userId);

            if (employeeProfile == null)
                return "I couldn't find an active employee profile linked to your account.";

            var employeeId = employeeProfile.EmployeeId; // The real Employee GUID

            // 3. ROUTE TO SERVICES
            switch (intent)
            {
                case "CheckLeaveBalance":
                    // LeaveService.GetLeaveBalanceAsync takes userId and converts it internally
                    var balances = await _leaveService.GetLeaveBalanceAsync(userId, DateTime.Now.Year);

                    if (balances == null || !balances.Any())
                        return "You have no leave entitlements assigned for this year.";

                    var balanceLines = balances.Select(b =>
                        $"{b.LeaveTypeName}: {b.RemainingDays}/{b.TotalDays} days remaining");

                    return $"Your Leave Balance ({DateTime.Now.Year}):\n" + string.Join("\n", balanceLines);

                case "CheckShift":
                    // 1. Get Assigned Shift
                    var shift = await _attendanceService.GetShiftByEmployeeIdAsync(employeeId);
                    // 2. Get Today's Punch
                    var attendance = await _attendanceService.GetTodayAttendanceAsync(employeeId);

                    string shiftTime = shift != null
                        ? $"{FormatTime(shift.StartTime)} - {FormatTime(shift.EndTime)}"
                        : "No shift assigned";

                    string status = attendance?.CheckInTime != null
                        ? $"You checked in at {attendance.CheckInTime.Value:hh:mm tt}."
                        : "You have not checked in yet.";

                    return $"Shift: {shiftTime}. {status}";

                case "CheckManager":
                    // EmployeeDto already contains the ReportingManagerName!
                    if (string.IsNullOrEmpty(employeeProfile.ReportingManagerName))
                        return "You do not have a reporting manager assigned.";

                    return $"Your Reporting Manager is {employeeProfile.ReportingManagerName}.";

                case "GetPayslip":
                    // Get latest 3 entries to find the most recent approved one
                    var payrolls = await _payrollService.GetPayrollEntriesByEmployeeAsync(employeeId, 1, 3);
                    var latestSlip = payrolls.Data.FirstOrDefault(p => p.Status == "Approved" || p.Status == "Paid");

                    if (latestSlip == null)
                        return "No generated payslips found for you yet.";

                    return $"Your latest payslip is for {latestSlip.PeriodName}. Net Salary: {latestSlip.Currency} {latestSlip.NetSalary:N2}.";

                case "ApplyLeave":
                    return "To apply for leave, please use the 'Leave Request' form in your dashboard.";

                case "Greeting":
                    return $"Hello {employeeProfile.FirstName}! I am your HR Assistant. Ask me about leaves, attendance, or payslips.";

                default:
                    return "I understood you, but I don't have a response configured for that yet.";
            }
        }

        private string FormatTime(TimeSpan? time)
        {
            if (!time.HasValue) return "--";
            return DateTime.Today.Add(time.Value).ToString("hh:mm tt");
        }
    }
}