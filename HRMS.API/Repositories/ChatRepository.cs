using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.Chatbot;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories
{
    public class ChatRepository : BaseRepository, IChatRepository
    {
        public ChatRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }

        // 1. LEAVE BALANCE
        // Logic: Matches the snippet you shared. Links Entitlements to LeaveTypes.
        // We filter by Organization and Active status.
        public async Task<List<LeaveBalanceDTO>> GetLeaveBalanceAsync(Guid employeeId)
        {
            var year = DateTime.Now.Year;
            var sql = @"
                SELECT 
                    lt.typename AS LeaveTypeName, 
                    lt.color,
                    COALESCE(le.totaldays, 0) AS TotalDays,
                    COALESCE(le.useddays, 0) AS UsedDays,
                    COALESCE(le.remainingdays, 0) AS RemainingDays
                FROM attendance.leavetypes lt
                INNER JOIN employee.employees e ON e.employeeid = @employeeId
                -- Left join ensures we see leave types even if no entitlement row exists yet
                LEFT JOIN attendance.leaveentitlements le 
                    ON lt.leavetypeid = le.leavetypeid 
                    AND le.employeeid = @employeeId 
                    AND le.year = @year
                WHERE lt.organizationid = e.organizationid 
                  AND lt.isactive = true
                ORDER BY lt.typename";

            var result = await QueryAsync<LeaveBalanceDTO>(sql, new { employeeId, year });
            return result.ToList();
        }

        // 2. SHIFT & ATTENDANCE STATUS
        // Logic: Joins EmploymentDetails (for Shift) and Attendance (for today's punch).
        // Uses 'iscurrent = true' as seen in AttendanceRepository.cs.
        public async Task<ShiftStatusDto?> GetAttendanceStatusAsync(Guid employeeId)
        {
            var sql = @"
                SELECT 
                    -- Shift Details
                    s.shiftname AS ShiftName,
                    s.starttime AS StartTime,
                    s.endtime AS EndTime,
                    
                    -- Today's Attendance
                    a.checkintime AS CheckInTime,
                    a.checkouttime AS CheckOutTime,
                    COALESCE(a.status, 'Absent') AS Status
                FROM employee.employees e
                -- Get Assigned Shift
                LEFT JOIN employee.employmentdetails ed 
                    ON e.employeeid = ed.employeeid AND ed.iscurrent = true
                LEFT JOIN attendance.shifts s 
                    ON ed.shiftid = s.shiftid
                -- Get Today's Attendance Record
                LEFT JOIN attendance.attendance a 
                    ON e.employeeid = a.employeeid 
                    AND a.workdate = CURRENT_DATE
                WHERE e.employeeid = @employeeId";

            return await QuerySingleOrDefaultAsync<ShiftStatusDto>(sql, new { employeeId });
        }

        // 3. MANAGER INFO
        // Logic: Uses EmploymentDetails to find ReportingManagerId, then joins Employees table.
        // Uses 'iscurrent = true' to ensure we get the active manager.
        //public async Task<ManagerDto?> GetManagerInfoAsync(Guid employeeId)
        //{
        //    var sql = @"
        //        SELECT 
        //            m.firstname, 
        //            m.lastname, 
        //            m.email,
        //            p.positiontitle
        //        FROM employee.employmentdetails ed
        //        -- Join Manager's Employee Record
        //        INNER JOIN employee.employees m ON ed.reportingmanagerid = m.employeeid
        //        -- Optional: Join Manager's Position Title
        //        LEFT JOIN employee.employmentdetails med 
        //            ON m.employeeid = med.employeeid AND med.iscurrent = true
        //        LEFT JOIN employee.positions p 
        //            ON med.positionid = p.positionid
        //        WHERE ed.employeeid = @employeeId 
        //          AND ed.iscurrent = true";

        //    return await QuerySingleOrDefaultAsync<ManagerDto>(sql, new { employeeId });
        //}

        // 4. PAYSLIP
        // Logic: Matches PayrollRepository logic mapping 'net_salary' and 'periodname'.
        // Gets the single latest processed payslip.
        public async Task<PayslipDto?> GetLatestPayslipAsync(Guid employeeId)
        {
            var sql = @"
                SELECT 
                    pp.periodname AS PeriodName, 
                    pe.netsalary AS NetSalary, 
                    pe.currency AS Currency,
                    pp.paydate AS PayDate
                FROM payroll.payrollentries pe
                INNER JOIN payroll.payrollperiods pp ON pe.periodid = pp.periodid
                WHERE pe.employeeid = @employeeId
                  AND pe.status = 'approved' -- Ensure we only show final payslips
                ORDER BY pp.enddate DESC
                LIMIT 1";

            return await QuerySingleOrDefaultAsync<PayslipDto>(sql, new { employeeId });
        }
    }
}