using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.AI.Models.Enums
{
    public enum ChatIntent
    {
        Greeting,
        Guidance,
        GetHolidaySummary,
        GetMyRestrictedHolidays,
        GetTimesheetStatus,
        GetExpenseStatus,
        GetJobsStatus,
        GetNewsUpdates,
        GetMyAssets,
        GetSubscriptionStatus,
        GetPerformanceSummary,
        GetAttendance,
        CheckMyShift,
        GetLeaveBalance,
        ApplyForLeave,
        GetPayslip,
        EmployeeInfo,
        LeaveApplicationDetails,
        GetMySkills,
        ClockIn,
        ClockOut,
        Unknown
    }

}
