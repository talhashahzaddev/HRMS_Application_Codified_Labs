using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace HRMS.API.Repositories;

public class DashboardRepository : BaseRepository, IDashboardRepository
{
    private readonly ILogger<DashboardRepository> _logger;

    public DashboardRepository(IDatabaseConnectionFactory connectionFactory, ILogger<DashboardRepository> logger)
        : base(connectionFactory)
    {
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LEAVE BALANCES
    // Replicates the same join used in LeaveRepository.GetLeaveBalanceAsync
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<List<LeaveTypeBalanceSummaryDto>> GetLeaveBalancesAsync(Guid employeeId, Guid organizationId, int year)
    {
        try
        {
            const string sql = @"
                SELECT
                    lt.typename          AS LeaveTypeName,
                    lt.color             AS Color,
                    COALESCE(le.totaldays, 0)               AS TotalDays,
                    COALESCE(le.useddays, 0)                AS UsedDays,
                    COALESCE(le.remainingdays, 0)           AS RemainingDays,
                    COALESCE(le.carryforwarddays, 0)        AS CarryForwardDays
                FROM attendance.leavetypes lt
                LEFT JOIN attendance.leaveentitlements le
                    ON le.leavetypeid = lt.leavetypeid
                   AND le.employeeid  = @EmployeeId
                   AND le.year        = @Year
                WHERE lt.organizationid = @OrganizationId AND lt.isactive = true
                ORDER BY lt.typename;";

            var result = await QueryAsync<LeaveTypeBalanceSummaryDto>(sql, new { EmployeeId = employeeId, OrganizationId = organizationId, Year = year });
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching leave balances for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FINANCIAL SNAPSHOT – latest payslip with fallback to 2nd last
    // Logic:
    //   1. Find the latest payroll period (by createdat desc) that has a
    //      payrollresult for this employee.
    //   2. From that resultid, get the payslip row.
    //   3. If payslipuploadurl IS NULL or empty, go one period back.
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<FinancialSnapshotDto?> GetLatestPayslipAsync(Guid employeeId, Guid organizationId)
    {
        try
        {
            // Pull the two most recent payroll results for this employee
            // (so we can fall back to the 2nd latest when url is missing)
            const string sql = @"
                SELECT
                    pr.resultid,
                    pr.netsalary        AS LastNetSalary,
                    pp.periodname       AS LastPayPeriodName,
                    ps.payslipuploadurl AS PayslipUploadUrl
                FROM payroll.payrollresults pr
                INNER JOIN payroll.payrollperiods pp
                    ON pp.periodid = pr.periodid
                LEFT JOIN payroll.payslips ps
                    ON ps.resultid = pr.resultid
                WHERE pr.employeeid     = @EmployeeId
                  AND pr.organizationid = @OrganizationId
                  AND pp.isdeleted      = false
                ORDER BY pp.createdat DESC
                LIMIT 2;";

            var rows = (await QueryAsync<dynamic>(sql, new { EmployeeId = employeeId, OrganizationId = organizationId }))
                       .ToList();

            if (rows.Count == 0)
                return new FinancialSnapshotDto { IsPayslipAvailable = false };

            // Prefer first row if it has a valid url; otherwise fall back to second
            dynamic chosen = rows[0];
            if (string.IsNullOrWhiteSpace((string?)chosen.payslipuploadurl) && rows.Count > 1)
                chosen = rows[1];

            string? url = (string?)chosen.payslipuploadurl;

            return new FinancialSnapshotDto
            {
                LastNetSalary      = (decimal?)chosen.lastnetsalary,
                LastPayPeriodName  = (string?)chosen.lastpayperiodname,
                PayslipUploadUrl   = string.IsNullOrWhiteSpace(url) ? null : url,
                IsPayslipAvailable = !string.IsNullOrWhiteSpace(url)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching latest payslip for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ACTIVE LOANS
    // Disbursed loans where loan is still active (loanstatus = 'active')
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<List<ActiveLoanSummaryDto>> GetActiveLoansAsync(Guid employeeId, Guid organizationId)
    {
        try
        {
            const string sql = @"
                SELECT
                    l.loanid                                AS LoanId,
                    l.referenceid                           AS ReferenceId,
                    l.remainingamount                       AS RemainingAmount,
                    l.totalamount                           AS TotalAmount,
                    (l.totalinstallments - l.paidinstallments) AS RemainingInstallments,
                    CASE
                        WHEN l.totalinstallments = 0 THEN 0
                        ELSE ROUND(
                            (l.paidinstallments::numeric / l.totalinstallments::numeric) * 100, 2
                        )
                    END                                     AS CompletedPercentage
                FROM payroll.loans l
                WHERE l.employeeid     = @EmployeeId
                  AND l.organizationid = @OrganizationId
                  AND l.loanstatus     = 'active'
                ORDER BY l.createdat DESC;";

            var result = await QueryAsync<ActiveLoanSummaryDto>(sql, new { EmployeeId = employeeId, OrganizationId = organizationId });
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching active loans for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DISBURSED SALARY ADVANCES (still outstanding – not yet fully deducted)
    // advancestatus = 'disbursed' means money was given but not yet deducted
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<List<ActiveAdvanceSummaryDto>> GetDisbursedAdvancesAsync(Guid employeeId, Guid organizationId)
    {
        try
        {
            const string sql = @"
                SELECT
                    sa.advanceid      AS AdvanceId,
                    sa.amount         AS Amount,
                    sa.advancestatus  AS AdvanceStatus,
                    sa.approvedat     AS ApprovedAt
                FROM payroll.salaryadvance sa
                WHERE sa.employeeid     = @EmployeeId
                  AND sa.organizationid = @OrganizationId
                  AND sa.advancestatus  = 'disbursed'
                ORDER BY sa.createdat DESC;";

            var result = await QueryAsync<ActiveAdvanceSummaryDto>(sql, new { EmployeeId = employeeId, OrganizationId = organizationId });
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching disbursed advances for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PENDING LEAVE REQUESTS
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<List<PendingRequestSummaryDto>> GetPendingLeaveRequestsAsync(Guid employeeId)
    {
        try
        {
            const string sql = @"
                SELECT
                    lr.requestid                                     AS RequestId,
                    'Leave'                                          AS RequestType,
                    COALESCE(lt.typename, 'Leave') || ' request - '
                        || TO_CHAR(lr.startdate, 'DD Mon') || ' to '
                        || TO_CHAR(lr.enddate,   'DD Mon')          AS Description,
                    lr.createdat                                     AS SubmittedAt
                FROM attendance.leaverequests lr
                LEFT JOIN attendance.leavetypes lt ON lt.leavetypeid = lr.leavetypeid
                WHERE lr.employeeid = @EmployeeId
                  AND lr.status     = 'pending'
                ORDER BY lr.createdat DESC;";

            var result = await QueryAsync<PendingRequestSummaryDto>(sql, new { EmployeeId = employeeId });
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching pending leave requests for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PENDING SHIFT SWAP REQUESTS
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<List<PendingRequestSummaryDto>> GetPendingShiftSwapRequestsAsync(Guid employeeId)
    {
        try
        {
            const string sql = @"
                SELECT
                    ss.requestid                                              AS RequestId,
                    'ShiftSwap'                                               AS RequestType,
                    'Shift swap: '
                        || COALESCE(cs.shiftname, 'Current')
                        || ' -> '
                        || COALESCE(rs.shiftname, 'Requested')               AS Description,
                    ss.submittedat                                            AS SubmittedAt
                FROM attendance.shiftswaprequests ss
                LEFT JOIN attendance.shifts cs ON cs.shiftid = ss.currentshiftid
                LEFT JOIN attendance.shifts rs ON rs.shiftid = ss.requestedshiftid
                WHERE ss.employeeid = @EmployeeId
                  AND ss.status     = 'pending'
                ORDER BY ss.submittedat DESC;";

            var result = await QueryAsync<PendingRequestSummaryDto>(sql, new { EmployeeId = employeeId });
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching pending shift swap requests for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PENDING OVERTIME REQUESTS
    // requesttype = 'employee' isolates employee-initiated requests
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<List<PendingRequestSummaryDto>> GetPendingOvertimeRequestsAsync(Guid employeeId, Guid organizationId)
    {
        try
        {
            const string sql = @"
                SELECT
                    o.requestid                                          AS RequestId,
                    'Overtime'                                           AS RequestType,
                    'Overtime request for '
                        || TO_CHAR(o.overtimedate, 'DD Mon YYYY')
                        || ' ('
                        || o.requestedhours::text
                        || ' hrs)'                                       AS Description,
                    o.createdat                                          AS SubmittedAt
                FROM attendance.employee_overtime_requests o
                WHERE o.employeeid     = @EmployeeId
                  AND o.organizationid = @OrganizationId
                  AND o.requesttype    = 'employee'
                  AND o.status         = 'pending'
                ORDER BY o.createdat DESC;";

            var result = await QueryAsync<PendingRequestSummaryDto>(sql, new { EmployeeId = employeeId, OrganizationId = organizationId });
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching pending overtime requests for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PENDING SALARY ADVANCE REQUESTS
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<List<PendingRequestSummaryDto>> GetPendingSalaryAdvanceRequestsAsync(Guid employeeId, Guid organizationId)
    {
        try
        {
            const string sql = @"
                SELECT
                    sa.advanceid                              AS RequestId,
                    'SalaryAdvance'                           AS RequestType,
                    'Salary advance request - '
                        || sa.amount::text                    AS Description,
                    sa.createdat                              AS SubmittedAt
                FROM payroll.salaryadvance sa
                WHERE sa.employeeid     = @EmployeeId
                  AND sa.organizationid = @OrganizationId
                  AND sa.advancestatus  = 'pending'
                ORDER BY sa.createdat DESC;";

            var result = await QueryAsync<PendingRequestSummaryDto>(sql, new { EmployeeId = employeeId, OrganizationId = organizationId });
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching pending salary advance requests for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PENDING LOAN REQUESTS
    // requeststatus = 'pending' means HR has not yet approved/rejected
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<List<PendingRequestSummaryDto>> GetPendingLoanRequestsAsync(Guid employeeId, Guid organizationId)
    {
        try
        {
            const string sql = @"
                SELECT
                    l.loanid                                  AS RequestId,
                    'Loan'                                    AS RequestType,
                    'Loan request - '
                        || l.principalamount::text            AS Description,
                    l.createdat                               AS SubmittedAt
                FROM payroll.loans l
                WHERE l.employeeid     = @EmployeeId
                  AND l.organizationid = @OrganizationId
                  AND l.requeststatus  = 'pending'
                ORDER BY l.createdat DESC;";

            var result = await QueryAsync<PendingRequestSummaryDto>(sql, new { EmployeeId = employeeId, OrganizationId = organizationId });
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching pending loan requests for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PENDING PROVIDENT FUND REQUESTS
    // status = 'pending' across all request types (enrollment, update, withdrawal)
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<List<PendingRequestSummaryDto>> GetPendingPfRequestsAsync(Guid employeeId, Guid organizationId)
    {
        try
        {
            const string sql = @"
                SELECT
                    pf.requestid                              AS RequestId,
                    'PF'                                      AS RequestType,
                    'PF '
                        || INITCAP(pf.requesttype)
                        || ' request'                         AS Description,
                    pf.createdat                              AS SubmittedAt
                FROM payroll.pfrequests pf
                WHERE pf.employeeid     = @EmployeeId
                  AND pf.organizationid = @OrganizationId
                  AND pf.status         = 'pending'
                ORDER BY pf.createdat DESC;";

            var result = await QueryAsync<PendingRequestSummaryDto>(sql, new { EmployeeId = employeeId, OrganizationId = organizationId });
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching pending PF requests for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MANAGER OVERVIEW 
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<TeamAttendanceSnapshotDto> GetTeamAttendanceSnapshotAsync(Guid managerId, Guid organizationId, DateTime date)
    {
        try
        {
            var sql = @"
                WITH team AS (
                    SELECT ed.employeeid 
                    FROM employee.employmentdetails ed
                    INNER JOIN employee.employees e ON e.employeeid = ed.employeeid
                    WHERE ed.reportingmanagerid = @ManagerId 
                      AND e.organizationid = @OrganizationId
                      AND ed.iscurrent = true
                ),
                PresentEmployees AS (
                    SELECT a.employeeid, a.late_minutes
                    FROM attendance.attendance a
                    INNER JOIN team t ON t.employeeid = a.employeeid
                    WHERE a.workdate = @Date
                      AND LOWER(TRIM(a.status)) = 'present'
                ),
                OnLeaveEmployees AS (
                    SELECT DISTINCT lr.employeeid
                    FROM attendance.leaverequests lr
                    INNER JOIN team t ON t.employeeid = lr.employeeid
                    WHERE LOWER(TRIM(lr.status)) = 'approved'
                      AND @Date BETWEEN lr.startdate AND lr.enddate
                )
                SELECT 
                    COALESCE((SELECT COUNT(*) FROM PresentEmployees), 0) AS PresentCount,
                    COALESCE((SELECT COUNT(*) FROM PresentEmployees WHERE late_minutes > 0), 0) AS LateCount,
                    COALESCE((SELECT COUNT(*) FROM OnLeaveEmployees), 0) AS OnLeaveCount,
                    GREATEST(
                        COALESCE((SELECT COUNT(*) FROM team), 0) 
                        - COALESCE((SELECT COUNT(*) FROM PresentEmployees), 0)
                        - COALESCE((SELECT COUNT(*) FROM OnLeaveEmployees), 0),
                        0
                    ) AS AbsentCount;";

            var parameters = new { ManagerId = managerId, OrganizationId = organizationId, Date = date.Date };
            return await QuerySingleOrDefaultAsync<TeamAttendanceSnapshotDto>(sql, parameters) ?? new TeamAttendanceSnapshotDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching team attendance snapshot for manager {ManagerId}", managerId);
            throw;
        }
    }

    public async Task<int> GetTeamOvertimeCountTodayAsync(Guid managerId, Guid organizationId, DateTime date)
    {
        try
        {
            var sql = @"
                SELECT COUNT(DISTINCT o.employeeid)
                FROM attendance.overtime_requests o
                INNER JOIN employee.employmentdetails ed ON ed.employeeid = o.employeeid
                INNER JOIN employee.employees e ON e.employeeid = o.employeeid
                WHERE ed.reportingmanagerid = @ManagerId
                  AND e.organizationid = @OrganizationId
                  AND ed.iscurrent = true
                  AND o.overtimedate::date = @Date::date
                  AND o.status IN ('pending', 'approved');";

            var parameters = new { ManagerId = managerId, OrganizationId = organizationId, Date = date.Date };
            var result = await QueryAsync<int>(sql, parameters);
            return result.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching team overtime count for manager {ManagerId}", managerId);
            throw;
        }
    }

    public async Task<PendingManagerActionsDto> GetPendingManagerActionsAsync(Guid managerId, Guid organizationId)
    {
        try
        {
            var sql = @"
                WITH team AS (
                    SELECT ed.employeeid 
                    FROM employee.employmentdetails ed
                    INNER JOIN employee.employees e ON e.employeeid = ed.employeeid
                    WHERE ed.reportingmanagerid = @ManagerId 
                      AND e.organizationid = @OrganizationId
                      AND ed.iscurrent = true
                )
                SELECT
                    (SELECT COUNT(*) 
                     FROM attendance.leaverequests lr
                     INNER JOIN team t ON lr.employeeid = t.employeeid
                     WHERE lr.status = 'pending') AS PendingLeaveRequests,
                    
                    (SELECT COUNT(DISTINCT p.timesheet_id) 
                     FROM attendance.timesheet_approval_progress p
                     WHERE p.approver_id = @ManagerId 
                       AND LOWER(p.status) = 'pending') AS PendingTimesheetCorrections,
                       
                    (SELECT COUNT(*) 
                     FROM attendance.shiftswaprequests ss
                     WHERE ss.reportingmanagerid = @ManagerId 
                       AND ss.status = 'pending') AS PendingShiftSwaps;";

            var parameters = new { ManagerId = managerId, OrganizationId = organizationId };
            return await QuerySingleOrDefaultAsync<PendingManagerActionsDto>(sql, parameters) ?? new PendingManagerActionsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching pending actions for manager {ManagerId}", managerId);
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HR/FINANCE OVERVIEW
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<PayrollRunStatusDto> GetLatestPayrollRunStatusAsync(Guid organizationId)
    {
        try
        {
            var sql = @"
                WITH LatestPeriod AS (
                    SELECT periodid, periodname
                    FROM payroll.payrollperiods
                    WHERE organizationid = @OrganizationId
                    ORDER BY startdate DESC
                    LIMIT 1
                )
                SELECT 
                    lp.periodname AS PeriodName,
                    (SELECT COUNT(*) FROM payroll.payrollresults r WHERE r.periodid = lp.periodid) AS TotalEmployeesProcessed,
                    (SELECT COUNT(*) FROM payroll.payslips p INNER JOIN payroll.payrollresults r ON p.resultid = r.resultid WHERE r.periodid = lp.periodid AND p.payslipurl IS NOT NULL) AS PayslipsGenerated,
                    (SELECT COUNT(*) FROM payroll.payslips p INNER JOIN payroll.payrollresults r ON p.resultid = r.resultid WHERE r.periodid = lp.periodid AND p.payslipuploadurl IS NOT NULL) AS PayslipsUploaded,
                    (SELECT COUNT(*) FROM payroll.payslips p INNER JOIN payroll.payrollresults r ON p.resultid = r.resultid WHERE r.periodid = lp.periodid AND p.mailsentstatus = 'sent') AS EmailsSent
                FROM LatestPeriod lp;";

            var parameters = new { OrganizationId = organizationId };
            return await QuerySingleOrDefaultAsync<PayrollRunStatusDto>(sql, parameters) ?? new PayrollRunStatusDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching latest payroll run status for org {OrgId}", organizationId);
            throw;
        }
    }

    public async Task<decimal> GetLatestPayrollOutflowAsync(Guid organizationId)
    {
        try
        {
            var sql = @"
                WITH LatestPeriod AS (
                    SELECT periodid
                    FROM payroll.payrollperiods
                    WHERE organizationid = @OrganizationId
                    ORDER BY startdate DESC
                    LIMIT 1
                )
                SELECT COALESCE(SUM(r.netsalary), 0)
                FROM payroll.payrollresults r
                INNER JOIN LatestPeriod lp ON r.periodid = lp.periodid
                WHERE r.organizationid = @OrganizationId;";

            var parameters = new { OrganizationId = organizationId };
            return await QuerySingleOrDefaultAsync<decimal>(sql, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching latest payroll outflow for org {OrgId}", organizationId);
            throw;
        }
    }

    public async Task<WorkforceMetricsDto> GetWorkforceMetricsAsync(Guid organizationId)
    {
        try
        {
            var sql = @"
                SELECT
                    (SELECT COUNT(*) FROM employee.employees WHERE organizationid = @OrganizationId AND LOWER(status) = 'active') AS TotalActiveEmployees,
                    (SELECT COUNT(*) FROM jobs.jobopenings WHERE organizationid = @OrganizationId AND LOWER(status) = 'open') AS TotalOpenPositions;";

            var parameters = new { OrganizationId = organizationId };
            return await QuerySingleOrDefaultAsync<WorkforceMetricsDto>(sql, parameters) ?? new WorkforceMetricsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching workforce metrics for org {OrgId}", organizationId);
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HR STATS OVERVIEW
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<WorkforceDemographicsDto> GetWorkforceDemographicsAsync(Guid organizationId)
    {
        try
        {
            var sql = @"
                WITH ActiveEmployees AS (
                    SELECT e.employeeid, e.gender, e.dateofbirth, ed.departmentid
                    FROM employee.employees e
                    LEFT JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
                    WHERE e.organizationid = @OrganizationId AND LOWER(e.status) = 'active'
                ),
                TotalCount AS (
                    SELECT COUNT(*) as Total FROM ActiveEmployees
                ),
                GenderStats AS (
                    SELECT 
                        SUM(CASE WHEN LOWER(gender) = 'male' THEN 1 ELSE 0 END) AS MaleCount,
                        SUM(CASE WHEN LOWER(gender) = 'female' THEN 1 ELSE 0 END) AS FemaleCount
                    FROM ActiveEmployees
                ),
                AgeStats AS (
                    SELECT 
                        SUM(CASE WHEN EXTRACT(YEAR FROM age(CURRENT_DATE, dateofbirth)) < 25 THEN 1 ELSE 0 END) AS Under25,
                        SUM(CASE WHEN EXTRACT(YEAR FROM age(CURRENT_DATE, dateofbirth)) BETWEEN 25 AND 34 THEN 1 ELSE 0 END) AS Age25_34,
                        SUM(CASE WHEN EXTRACT(YEAR FROM age(CURRENT_DATE, dateofbirth)) BETWEEN 35 AND 44 THEN 1 ELSE 0 END) AS Age35_44,
                        SUM(CASE WHEN EXTRACT(YEAR FROM age(CURRENT_DATE, dateofbirth)) BETWEEN 45 AND 54 THEN 1 ELSE 0 END) AS Age45_54,
                        SUM(CASE WHEN EXTRACT(YEAR FROM age(CURRENT_DATE, dateofbirth)) >= 55 THEN 1 ELSE 0 END) AS Over55
                    FROM ActiveEmployees
                ),
                DeptStats AS (
                    SELECT d.departmentname, COUNT(ae.employeeid) AS DeptCount
                    FROM ActiveEmployees ae
                    INNER JOIN employee.departments d ON ae.departmentid = d.departmentid
                    GROUP BY d.departmentname
                    ORDER BY DeptCount DESC
                    LIMIT 1
                )
                SELECT 
                    (SELECT Total FROM TotalCount) AS TotalEmployees,
                    (SELECT MaleCount FROM GenderStats) AS MaleCount,
                    (SELECT FemaleCount FROM GenderStats) AS FemaleCount,
                    (SELECT Under25 FROM AgeStats) AS Under25,
                    (SELECT Age25_34 FROM AgeStats) AS Age25_34,
                    (SELECT Age35_44 FROM AgeStats) AS Age35_44,
                    (SELECT Age45_54 FROM AgeStats) AS Age45_54,
                    (SELECT Over55 FROM AgeStats) AS Over55,
                    (SELECT departmentname FROM DeptStats) AS LargestDepartmentName,
                    (SELECT DeptCount FROM DeptStats) AS LargestDepartmentCount;";

            var parameters = new { OrganizationId = organizationId };
            var result = await QuerySingleOrDefaultAsync<dynamic>(sql, parameters);

            if (result == null || result.totalemployees == 0)
                return new WorkforceDemographicsDto();

            decimal total = (decimal)result.totalemployees;

            var demographics = new WorkforceDemographicsDto
            {
                MalePercentage = Math.Round((decimal)result.malecount / total * 100, 2),
                FemalePercentage = Math.Round((decimal)result.femalecount / total * 100, 2),
                LargestDepartmentName = result.largestdepartmentname ?? "N/A",
                LargestDepartmentPercentage = result.largestdepartmentcount != null ? Math.Round((decimal)result.largestdepartmentcount / total * 100, 2) : 0
            };

            demographics.AgeGroupPercentages.Add("Under 25", Math.Round((decimal)result.under25 / total * 100, 2));
            demographics.AgeGroupPercentages.Add("25-34", Math.Round((decimal)result.age25_34 / total * 100, 2));
            demographics.AgeGroupPercentages.Add("35-44", Math.Round((decimal)result.age35_44 / total * 100, 2));
            demographics.AgeGroupPercentages.Add("45-54", Math.Round((decimal)result.age45_54 / total * 100, 2));
            demographics.AgeGroupPercentages.Add("55+", Math.Round((decimal)result.over55 / total * 100, 2));

            return demographics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching workforce demographics for org {OrgId}", organizationId);
            throw;
        }
    }

    public async Task<List<PayrollOutflowDto>> GetLast3PayrollOutflowAsync(Guid organizationId)
    {
        try
        {
            var sql = @"
                WITH Last3Periods AS (
                    SELECT periodid, periodname, startdate
                    FROM payroll.payrollperiods
                    WHERE organizationid = @OrganizationId AND LOWER(status) != 'cancelled'
                    ORDER BY startdate DESC
                    LIMIT 3
                )
                SELECT 
                    lp.periodname AS PeriodName,
                    COALESCE(SUM(r.basicsalary), 0) AS BasicSalarySum,
                    COALESCE(SUM(r.totaldeductions), 0) AS TotalDeductionsSum,
                    COALESCE(SUM(r.totalbonuses), 0) AS TotalBonusesSum
                FROM Last3Periods lp
                LEFT JOIN payroll.payrollresults r ON lp.periodid = r.periodid AND r.organizationid = @OrganizationId
                GROUP BY lp.periodname, lp.startdate
                ORDER BY lp.startdate ASC;";

            var parameters = new { OrganizationId = organizationId };
            var result = await QueryAsync<PayrollOutflowDto>(sql, parameters);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching payroll outflow for org {OrgId}", organizationId);
            throw;
        }
    }

    public async Task<List<MonthlyExpenseDto>> GetLast3MonthsExpensesAsync(Guid organizationId)
    {
        try
        {
            var sql = @"
                WITH Last3Months AS (
                    SELECT date_trunc('month', CURRENT_DATE - INTERVAL '2 months') AS start_date,
                           date_trunc('month', CURRENT_DATE) + INTERVAL '1 month - 1 day' AS end_date
                )
                SELECT 
                    to_char(e.billdate, 'YYYY-MM') AS Month,
                    COALESCE(SUM(e.amount), 0) AS TotalExpense
                FROM expense.expenses e
                CROSS JOIN Last3Months lm
                WHERE e.organizationid = @OrganizationId 
                  AND LOWER(e.status) = 'approved'
                  AND e.billdate >= lm.start_date
                  AND e.billdate <= lm.end_date
                GROUP BY to_char(e.billdate, 'YYYY-MM')
                ORDER BY to_char(e.billdate, 'YYYY-MM') ASC;";

            var parameters = new { OrganizationId = organizationId };
            var result = await QueryAsync<MonthlyExpenseDto>(sql, parameters);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DashboardRepo: Error fetching monthly expenses for org {OrgId}", organizationId);
            throw;
        }
    }
}
