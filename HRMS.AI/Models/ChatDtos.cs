using HRMS.AI.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.AI.Models
{
    public class ChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponseDto
    {
        public string ResponseText { get; set; } = string.Empty;
        public ChatIntent Intent { get; set; }
        public string Module { get; set; } = string.Empty; // Added to carry affected module when Guidance
    }

    public class EmployeeStatsDto
    {
        public Guid EmployeeId { get; set; }
        public Guid OrganizationId { get; set; }
        public int LeaveTakenYearToDate { get; set; }
        public int LeaveRemaining { get; set; }
        public decimal AttendancePercentage { get; set; }
        public string PerformanceSummary { get; set; } = string.Empty;
    }

    // Result returned by intent recognition containing both intent label and optional module
    public class IntentDetectionResult
    {
        public ChatIntent Intent { get; set; }
        public string Module { get; set; } = string.Empty;
    }

}
