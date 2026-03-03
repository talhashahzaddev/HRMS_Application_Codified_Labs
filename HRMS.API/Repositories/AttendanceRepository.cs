using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Npgsql;
using System.Data;
using System.Data.Common;

namespace HRMS.API.Repositories;

public class AttendanceRepository : BaseRepository, IAttendanceRepository
{
    private readonly ILogger<AttendanceRepository> _logger;

    public AttendanceRepository(IDatabaseConnectionFactory connectionFactory, ILogger<AttendanceRepository> logger) : base(connectionFactory)
    {
        _logger = logger;
    }
 public async Task<IEnumerable<Attendance>> GetManualAttendanceRecordsAsync(Guid organizationId, Guid? managerId, Guid? employeeId, DateTime startDate, DateTime endDate)
    {
        var sql = @"
            SELECT a.*
            FROM attendance.attendance a
            JOIN employee.employees e ON a.employeeid = e.employeeid
            WHERE e.organizationid = @organizationId
            AND a.workdate BETWEEN @startDate AND @endDate
            AND (@employeeId IS NULL OR a.employeeid = @employeeId)
            AND (@managerId IS NULL OR e.employeeid = @managerId)";
            
        return await QueryAsync<Attendance>(sql, new { organizationId, managerId, employeeId, startDate, endDate });
    }

    public async Task<bool> UpdateManualAttendanceAsync(Attendance attendance, AttendanceAudit audit)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var updateSql = @"
                UPDATE attendance.attendance
                SET checkintime = @CheckInTime,
                    checkouttime = @CheckOutTime,
                    status = @Status,
                    notes = @Notes,
                    updatedat = NOW() AT TIME ZONE 'UTC'
                WHERE attendanceid = @AttendanceId";

            await connection.ExecuteAsync(updateSql, attendance, transaction);

            var auditSql = @"
                INSERT INTO attendance.attendance_audit (
                    audit_id, attendance_id, employee_id, modified_by, modified_at, 
                    change_type, old_values, new_values, ip_address
                ) VALUES (
                    @AuditId, @AttendanceId, @EmployeeId, @ModifiedBy, @ModifiedAt,
                    @ChangeType, @OldValues::jsonb, @NewValues::jsonb, @IpAddress
                )";

            await connection.ExecuteAsync(auditSql, audit, transaction);

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> HasPermissionToEditAsync(Guid editorId, Guid targetEmployeeId, Guid organizationId)
    {
        if (editorId == targetEmployeeId) return true;

        var sql = @"
            SELECT COUNT(1)
            FROM employee.employees
            WHERE employeeid = @targetEmployeeId
            AND employeeid = @editorId
            AND organizationid = @organizationId";

        var count = await ExecuteScalarAsync<int>(sql, new { editorId, targetEmployeeId, organizationId });
        return count > 0;
    }

    public async Task<bool> CreateMonthlySnapshotAsync(Guid organizationId, Guid generatedBy, MonthlyTimesheetCreateDto dto)
    {
        var sql = @"
        INSERT INTO attendance.monthly_timesheets (organization_id, timesheet_name, month, year, generated_by)
        VALUES (@organizationId, @TimesheetName, @Month, @Year, @generatedBy)
        ON CONFLICT ON CONSTRAINT uq_org_month_year_name DO NOTHING;";

        var rows = await ExecuteAsync(sql, new
        {
            organizationId,
            dto.TimesheetName,
            dto.Month,
            dto.Year,
            generatedBy
        });
        return rows > 0;
    }
    public async Task<IEnumerable<PendingAttendanceRequest>> GetAllPendingRequestsAsync(Guid organizationId, Guid? managerId = null)
    {
        try {
            var sql = @"
    SELECT 
        r.request_id        AS RequestId,
        r.attendance_id     AS AttendanceId,
        r.employee_id       AS EmployeeId,
        (e.firstname || ' ' || e.lastname) AS EmployeeName,
        e.employeecode      AS EmployeeCode,
        COALESCE(a.workdate, r.workdate) AS WorkDate,
        a.checkintime       AS OriginalCheckIn,
        a.checkouttime      AS OriginalCheckOut,
        a.status            AS OriginalStatus,
        r.requested_checkin  AS RequestedCheckIn,
        r.requested_checkout AS RequestedCheckOut,
        r.requested_status   AS RequestedStatus,
        r.reason_for_edit    AS ReasonForEdit,
        r.requested_notes    AS RequestedNotes,
        r.submitted_at       AS submittedat,
        r.status             AS Status
    FROM attendance.attendance_update_requests r
    JOIN employee.employees e ON r.employee_id = e.employeeid
    LEFT JOIN attendance.attendance a ON r.attendance_id = a.attendanceid
    LEFT JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
    WHERE e.organizationid = @organizationId 
      AND r.status = 'pending'
      AND (@managerId IS NULL OR ed.reportingmanagerid = @managerId)
    ORDER BY r.submitted_at DESC";
            return await QueryAsync<PendingAttendanceRequest>(sql, new { organizationId, managerId });
        }
        catch (Exception ex)
        { return Enumerable.Empty<PendingAttendanceRequest>(); }
    }
    public async Task<Guid> SubmitUpdateRequestAsync(Guid employeeId, Guid organizationId, AttendanceUpdateRequestDto dto)
    {
        try
        {
            var sql = @"
            SELECT attendance.fn_submit_attendance_request(
                @employeeId::uuid, 
                @organizationId::uuid, 
                @AttendanceId::uuid, 
                @WorkDate::date, 
                @RequestedCheckIn::timestamptz, 
                @RequestedCheckOut::timestamptz, 
                @RequestedStatus::varchar, 
                @ReasonForEdit::text, 
                @RequestedNotes::text,
                @TimesheetId::uuid 
            )";

            return await ExecuteScalarAsync<Guid>(sql, new
            {
                employeeId,
                organizationId,
                dto.AttendanceId,
                dto.WorkDate,
                dto.RequestedCheckIn,
                dto.RequestedCheckOut,
                dto.RequestedStatus,
                dto.ReasonForEdit,
                dto.RequestedNotes,
                dto.TimesheetId
            });
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> ProcessUpdateRequestAsync(Guid requestId, Guid actionedBy, bool isApproved, string? rejectionReason)
    {
        var sql = "SELECT attendance.fn_finalize_attendance_record(@requestId, @actionedBy, @isApproved, @rejectionReason)";
        return await ExecuteScalarAsync<bool>(sql, new { requestId, actionedBy, isApproved, rejectionReason });
    }
    public async Task<AttendanceUpdateRequestDto?> GetAttendanceUpdateRequestDetailsAsync(Guid requestId)
    {
        const string sql = @"
        SELECT 
            request_id AS RequestId,
            attendance_id AS AttendanceId,
            employee_id AS EmployeeId,
            workdate    AS WorkDate, 
            requested_checkin AS RequestedCheckIn,
            requested_checkout AS RequestedCheckOut,
            requested_status AS RequestedStatus,
            requested_notes AS RequestedNotes,
            reason_for_edit AS ReasonForEdit
        FROM attendance.attendance_update_requests
        WHERE request_id = @requestId";

        return await QuerySingleOrDefaultAsync<AttendanceUpdateRequestDto>(sql, new { requestId });
    }
    public async Task<int> BatchFinalizeMonthAsync(Guid timesheetId, Guid organizationId, Guid actionedBy)
    {
        var snapshot = await QuerySingleOrDefaultAsync<(int Month, int Year)>(
            "SELECT month, year FROM attendance.monthly_timesheets WHERE timesheet_id = @timesheetId",
            new { timesheetId });

        if (snapshot == default) return 0;

        const string sql = @"
        SELECT (fn_result).finalized_count
        FROM (
            SELECT attendance.fn_batch_finalize_month(
                @timesheetId, @organizationId, @month, @year
            ) AS fn_result
        ) t";

        return await ExecuteScalarAsync<int>(sql, new
        {
            timesheetId,
            organizationId,
            month = snapshot.Month,
            year = snapshot.Year
        });
    }
    public async Task<int> BatchFinalizeForEmployeeAsync(Guid employeeId, Guid timesheetId)
    {
        using var connection = CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var snapshot = await connection.QuerySingleOrDefaultAsync<(int Month, int Year, string OrgId)>(
                @"SELECT month, year, organization_id::text
              FROM attendance.monthly_timesheets
              WHERE timesheet_id = @timesheetId::uuid",
                new { timesheetId = timesheetId.ToString() },
                transaction);

            if (snapshot == default) return 0;

            var startDate = new DateTime(snapshot.Year, snapshot.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            var orgId = snapshot.OrgId;

            var parameters = new
            {
                timesheetId = timesheetId.ToString(),
                orgId,
                employeeId = employeeId.ToString(),
                startDate = startDate.ToString("yyyy-MM-dd"),
                endDate = endDate.ToString("yyyy-MM-dd")
            };

            // Step 1: Finalize days with an actual attendance record
            const string step1 = @"
            INSERT INTO attendance.finalized_timesheet_records
                (record_id, timesheet_id, employee_id, organization_id,
                 attendance_id, work_date, record_status, approved_at)
            SELECT DISTINCT ON (a.workdate)
                gen_random_uuid(),
                @timesheetId::uuid,
                @employeeId::uuid,
                @orgId::uuid,
                a.attendanceid,
                a.workdate,
                CASE WHEN ftr.record_status = 'overridden' THEN 'overridden' ELSE 'finalized' END,
                NOW()
            FROM attendance.attendance a
            LEFT JOIN attendance.finalized_timesheet_records ftr
                ON ftr.attendance_id = a.attendanceid
            WHERE a.employeeid = @employeeId::uuid
              AND a.workdate  >= @startDate::date
              AND a.workdate  <= @endDate::date
            ORDER BY a.workdate, a.attendanceid
            ON CONFLICT (attendance_id) WHERE attendance_id IS NOT NULL
            DO UPDATE SET
                record_status = CASE
                    WHEN attendance.finalized_timesheet_records.record_status = 'overridden' THEN 'overridden'
                    ELSE 'finalized'
                END,
                approved_at = NOW();";

            // Step 2: Finalize approved absent-day requests (attendance_id IS NULL)
            const string step2 = @"
            INSERT INTO attendance.finalized_timesheet_records
                (record_id, timesheet_id, employee_id, organization_id,
                 attendance_id, work_date, record_status, approved_at)
            SELECT DISTINCT ON (r.workdate)
                gen_random_uuid(),
                @timesheetId::uuid,
                @employeeId::uuid,
                @orgId::uuid,
                NULL::uuid,
                r.workdate,
                'finalized',
                NOW()
            FROM attendance.attendance_update_requests r
            WHERE r.employee_id  = @employeeId::uuid
              AND r.status        = 'approved'
              AND r.attendance_id IS NULL
              AND r.workdate     >= @startDate::date
              AND r.workdate     <= @endDate::date
            ORDER BY r.workdate, r.submitted_at DESC
            ON CONFLICT (employee_id, work_date) WHERE attendance_id IS NULL
            DO UPDATE SET record_status = 'finalized', approved_at = NOW();";

            // Step 3: Finalize truly absent days (no attendance row, no request at all, elapsed weekday)
            const string step3 = @"
            INSERT INTO attendance.finalized_timesheet_records
                (record_id, timesheet_id, employee_id, organization_id,
                 attendance_id, work_date, record_status, approved_at)
            SELECT
                gen_random_uuid(),
                @timesheetId::uuid,
                @employeeId::uuid,
                @orgId::uuid,
                NULL::uuid,
                gs.d::date,
                'finalized',
                NOW()
            FROM generate_series(
                @startDate::date,
                LEAST(@endDate::date, CURRENT_DATE - 1),
                '1 day'::interval
            ) AS gs(d)
            WHERE EXTRACT(DOW FROM gs.d) NOT IN (0, 6)
              AND NOT EXISTS (
                  SELECT 1 FROM attendance.attendance a
                  WHERE a.employeeid = @employeeId::uuid
                    AND a.workdate   = gs.d::date
              )
            ON CONFLICT (employee_id, work_date) WHERE attendance_id IS NULL
            DO NOTHING;";

            // Step 4: Mark timesheet as Finalized
            const string step4 = @"
            UPDATE attendance.monthly_timesheets
            SET status = 'Finalized'
            WHERE timesheet_id = @timesheetId::uuid;";

            await connection.ExecuteAsync(step1, parameters, transaction);
            await connection.ExecuteAsync(step2, parameters, transaction);
            await connection.ExecuteAsync(step3, parameters, transaction);
            await connection.ExecuteAsync(step4, new { timesheetId = timesheetId.ToString() }, transaction);

            transaction.Commit();
            return 1;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
    public async Task<IEnumerable<FinalizedTimesheetRecordDto>> GetFinalizedRecordsAsync(Guid timesheetId, Guid? employeeId = null)
    {
        const string sql = @"
        SELECT 
            f.record_id AS RecordId, f.timesheet_id AS TimesheetId, f.employee_id AS EmployeeId, 
            f.work_date AS WorkDate, f.final_checkin AS FinalCheckIn, f.final_checkout AS FinalCheckOut, 
            f.final_total_hours AS FinalTotalHours, f.final_status AS FinalStatus,
            e.firstname || ' ' || e.lastname AS EmployeeName
        FROM attendance.finalized_timesheet_records f
        JOIN employee.employees e ON f.employee_id = e.employeeid
        WHERE f.timesheet_id = @timesheetId
        AND (@employeeId IS NULL OR f.employee_id = @employeeId)
        ORDER BY f.work_date DESC, EmployeeName ASC";

        return await QueryAsync<FinalizedTimesheetRecordDto>(sql, new { timesheetId, employeeId });
    }

    public async Task<IEnumerable<MonthlyTimesheetSummary>> GetMonthlySnapshotsAsync(Guid organizationId, DateTime? startDate, DateTime? endDate)
    {
        var sql = @"
        SELECT 
            t.timesheet_id AS TimesheetId,
            t.timesheet_name AS TimesheetName,
            t.month AS Month,
            t.year AS Year,
            t.status AS Status,
            t.created_at AS CreatedAt,
            (SELECT COUNT(*) FROM employee.employees e 
             WHERE e.organizationid = @organizationId
            ) AS TotalEmployees
        FROM attendance.monthly_timesheets t
        WHERE t.organization_id = @organizationId
          AND (@startDate::timestamp IS NULL OR t.created_at >= @startDate::timestamp)
          AND (@endDate::timestamp IS NULL OR t.created_at <= @endDate::timestamp)
        ORDER BY t.year DESC, t.month DESC";

        return await QueryAsync<MonthlyTimesheetSummary>(sql, new { organizationId, startDate, endDate });
    }
    public async Task<IEnumerable<AttendanceDto>> GetConsolidatedAttendanceAsync(
    Guid organizationId, DateTime startDate, DateTime endDate,
    Guid? employeeId, Guid? managerId = null)
    {
        try
        {
            bool isGlobalView = managerId == null || managerId == Guid.Empty;

            var sql = @"
WITH date_series AS (
    SELECT generate_series(@startStr::date, @endStr::date, interval '1 day')::date AS workdate
),
filtered_employees AS (
    SELECT
        e.employeeid,
        (e.firstname || ' ' || e.lastname) AS employeename,
        e.employeecode,
        d.departmentname
    FROM employee.employees e
    LEFT JOIN employee.employmentdetails ed
           ON e.employeeid = ed.employeeid AND ed.iscurrent = true
    LEFT JOIN employee.departments d
           ON ed.departmentid = d.departmentid
    WHERE e.organizationid = @orgStr::UUID
      AND (@empStr::UUID IS NULL OR e.employeeid = @empStr::UUID)
      AND (@isGlobalView = TRUE OR ed.reportingmanagerid = @mgrStr::UUID)
),
emp_dates AS (
    SELECT
        fe.employeeid,
        fe.employeename,
        fe.employeecode,
        fe.departmentname,
        ds.workdate
    FROM filtered_employees fe
    CROSS JOIN date_series ds
)
SELECT
    ed.employeeid,
    ed.employeename,
    ed.employeecode,
    ed.departmentname,
    v.attendanceid          AS AttendanceId,
    ed.workdate,
    v.checkintime,
    v.checkouttime,
    v.status,
    COALESCE(v.totalhours, 0)           AS totalhours,
    COALESCE(v.is_finalized,   FALSE)   AS ""IsFinalized"",
    COALESCE(v.is_manager_override, FALSE) AS ""IsManagerOverride"",
    COALESCE(v.has_pending_request, FALSE) AS ""HasPendingRequest"",
    COALESCE(v.has_draft_request,   FALSE) AS ""HasDraftRequest"",
    COALESCE(v.has_approved_request,FALSE) AS ""HasApprovedRequest"",
    v.requested_checkin  AS ""RequestedCheckIn"",
    v.requested_checkout AS ""RequestedCheckOut"",
    v.requested_status   AS ""RequestedStatus""
FROM emp_dates ed
LEFT JOIN attendance.vw_employee_timesheet_details v
       ON v.employeeid = ed.employeeid
      AND v.workdate   = ed.workdate
ORDER BY ed.employeename, ed.workdate ASC";

            var result = await QueryAsync<AttendanceDto>(sql, new
            {
                startStr = startDate.ToString("yyyy-MM-dd"),
                endStr = endDate.ToString("yyyy-MM-dd"),
                orgStr = organizationId.ToString(),
                empStr = employeeId?.ToString(),
                mgrStr = managerId?.ToString(),
                isGlobalView
            });

            _logger.LogWarning("REPO DEBUG: Found {Count} rows for Org {Org} (Global: {Global})",
                result.Count(), organizationId, isGlobalView);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetConsolidatedAttendanceAsync");
            throw;
        }
    }

    public async Task<MonthlyTimesheetSummary?> GetSnapshotByIdAsync(Guid timesheetId)
    {
        var sql = @"SELECT timesheet_id as TimesheetId, month, year, timesheet_name as TimesheetName 
                FROM attendance.monthly_timesheets WHERE timesheet_id = @timesheetId";
        return await QuerySingleOrDefaultAsync<MonthlyTimesheetSummary>(sql, new { timesheetId });
    }
    public async Task<int> SubmitBatchForApprovalAsync(Guid employeeId, Guid organizationId, Guid timesheetId, DateTime startDate, DateTime endDate)
    {
        var sql = "SELECT attendance.fn_submit_period_for_approval(@employeeId, @organizationId, @timesheetId, @startDate, @endDate)";

        return await ExecuteScalarAsync<int>(sql, new
        {
            employeeId,
            organizationId,
            timesheetId,
            startDate = startDate.Date,
            endDate = endDate.Date
        });
    }
    public async Task<int> ApproveAllBatchAsync(Guid timesheetId, Guid? employeeId, Guid managerId, Guid organizationId)
    {
        var sql = "SELECT attendance.fn_approve_all_pending_requests(@managerId, @organizationId, @timesheetId, @employeeId)";

        using var connection = CreateConnection();
        try {
        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            managerId,
            organizationId,
            timesheetId,
            employeeId
        });
    }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch approving requests for Timesheet {TimesheetId}", timesheetId);
            throw;
        }
    }
    public async Task<bool> IsEmployeePackageFinalizedAsync(Guid timesheetId, Guid employeeId)
    {
        const string sql = @"
        WITH timesheet_period AS (
            SELECT month, year
            FROM attendance.monthly_timesheets
            WHERE timesheet_id = @timesheetId
            LIMIT 1
        ),
        elapsed_workdays AS (
            SELECT COUNT(gs.d)::int AS total_elapsed
            FROM timesheet_period tp
            CROSS JOIN generate_series(
                make_date(tp.year, tp.month, 1),
                LEAST(
                    (make_date(tp.year, tp.month, 1) + INTERVAL '1 month - 1 day')::date,
                    CURRENT_DATE - 1
                ),
                '1 day'::interval
            ) AS gs(d)
            WHERE EXTRACT(DOW FROM gs.d) NOT IN (0, 6)
        ),
        finalized_days AS (
            SELECT COUNT(DISTINCT work_date)::int AS finalized_count
            FROM attendance.finalized_timesheet_records
            WHERE timesheet_id = @timesheetId
              AND employee_id   = @employeeId
              AND record_status IN ('finalized', 'overridden')
        )
        SELECT
            CASE WHEN ew.total_elapsed > 0
                      AND fd.finalized_count >= ew.total_elapsed
                 THEN TRUE ELSE FALSE END
        FROM elapsed_workdays ew, finalized_days fd;";

        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(sql, new { timesheetId, employeeId });
    }
    public async Task<OrgSubmissionProgressDto> GetOrgSubmissionProgressAsync(
    Guid organizationId, int month, int year)
    {
        try
        {
            const string sql = @"
        WITH
        -- All employee-timesheet enrollments for this org + period
        enrollments AS (
    SELECT
        mt.timesheet_id AS timesheetid,
        e.employeeid
    FROM attendance.monthly_timesheets mt
    JOIN employee.employees e ON e.organizationid = mt.organization_id
    WHERE mt.organization_id = @orgId::uuid
      AND mt.month = @month
      AND mt.year  = @year
     ),
        -- Elapsed working days in the month up to yesterday (Mon–Fri only)
        elapsed_month_workdays AS (
    SELECT COUNT(gs.d)::int AS total_elapsed
    FROM generate_series(
        make_date(@year, @month, 1),
        LEAST(
            (make_date(@year, @month, 1) + INTERVAL '1 month - 1 day')::date,
            CURRENT_DATE - 1
        ),
        '1 day'::interval
        ) AS gs(d)
        WHERE EXTRACT(DOW FROM gs.d) NOT IN (0, 6)
        ),
        fully_finalized AS (
      SELECT f.employee_id AS employeeid
      FROM attendance.finalized_timesheet_records f
      INNER JOIN attendance.monthly_timesheets mt
        ON mt.timesheet_id = f.timesheet_id
    WHERE mt.organization_id = @orgId::uuid
      AND mt.month           = @month
      AND mt.year            = @year
      AND f.record_status   IN ('finalized', 'overridden')
    GROUP BY f.employee_id
    HAVING COUNT(DISTINCT f.work_date) >= (SELECT total_elapsed FROM elapsed_month_workdays)
),
        -- Submitted: has at least one PENDING request (not finalized)
        pending_employees AS (
            SELECT DISTINCT aur.employee_id AS employeeid
            FROM attendance.attendance_update_requests aur
            JOIN enrollments en
                ON en.timesheetid = aur.timesheet_id
               AND en.employeeid  = aur.employee_id
            WHERE aur.status = 'pending'
              AND aur.employee_id NOT IN (SELECT employeeid FROM fully_finalized)
        ),

        -- Draft: has drafts but no pending (not finalized)
        draft_employees AS (
            SELECT DISTINCT aur.employee_id AS employeeid
            FROM attendance.attendance_update_requests aur
            JOIN enrollments en
                ON en.timesheetid = aur.timesheet_id
               AND en.employeeid  = aur.employee_id
            WHERE aur.status = 'draft'
              AND aur.employee_id NOT IN (SELECT employeeid FROM fully_finalized)
              AND aur.employee_id NOT IN (SELECT employeeid FROM pending_employees)
        ),

        -- Rejected: has rejected requests but no pending (not finalized)
        rejected_employees AS (
            SELECT DISTINCT aur.employee_id AS employeeid
            FROM attendance.attendance_update_requests aur
            JOIN enrollments en
                ON en.timesheetid = aur.timesheet_id
               AND en.employeeid  = aur.employee_id
            WHERE aur.status = 'rejected'
              AND aur.employee_id NOT IN (SELECT employeeid FROM fully_finalized)
              AND aur.employee_id NOT IN (SELECT employeeid FROM pending_employees)
        ),
       in_progress_employees AS (
      SELECT DISTINCT f.employee_id AS employeeid
      FROM attendance.finalized_timesheet_records f
      INNER JOIN attendance.monthly_timesheets mt
        ON mt.timesheet_id = f.timesheet_id
      WHERE mt.organization_id = @orgId::uuid
      AND mt.month           = @month
      AND mt.year            = @year
      AND f.record_status   IN ('finalized', 'overridden')
      AND f.employee_id NOT IN (SELECT employeeid FROM fully_finalized)
        ),
        all_employees AS (
            SELECT DISTINCT employeeid FROM enrollments
        )
             SELECT
    COUNT(DISTINCT ae.employeeid)              AS total_employees,
    COUNT(DISTINCT ff.employeeid)              AS finalized_count,
    COUNT(DISTINCT pe.employeeid)              AS submitted_count,
    COUNT(DISTINCT de.employeeid)              AS draft_count,
    COUNT(DISTINCT re.employeeid)              AS rejected_count,
    COUNT(DISTINCT ip.employeeid)              AS in_progress_count,
    COUNT(DISTINCT ae.employeeid)
        - COUNT(DISTINCT ff.employeeid)
        - COUNT(DISTINCT pe.employeeid)
        - COUNT(DISTINCT de.employeeid)
        - COUNT(DISTINCT re.employeeid)
        - COUNT(DISTINCT ip.employeeid)        AS untouched_count,
         CASE WHEN COUNT(DISTINCT ae.employeeid) = 0 THEN 0
         ELSE ROUND(
             COUNT(DISTINCT ff.employeeid) * 100.0
             / COUNT(DISTINCT ae.employeeid)
             )
            END     AS finalization_percentage
               FROM all_employees ae
                 LEFT JOIN fully_finalized     ff ON ff.employeeid = ae.employeeid
                 LEFT JOIN pending_employees   pe ON pe.employeeid = ae.employeeid
                 LEFT JOIN draft_employees     de ON de.employeeid = ae.employeeid
                 LEFT JOIN rejected_employees  re ON re.employeeid = ae.employeeid
                 LEFT JOIN in_progress_employees ip ON ip.employeeid = ae.employeeid";

            using var conn = CreateConnection();
            var row = await conn.QuerySingleAsync(sql, new
            {
                orgId = organizationId,
                month,
                year
            });

            int total = (int)(row.total_employees ?? 0);
            int finalized = (int)(row.finalized_count ?? 0);
            int pending = (int)(row.submitted_count ?? 0);
            int draft = (int)(row.draft_count ?? 0);
            int rejected = (int)(row.rejected_count ?? 0);
            int inprogress = (int)(row.in_progress_count ?? 0);
            int untouched = (int)(row.untouched_count ?? 0);

            return new OrgSubmissionProgressDto
            {
                Month = month,
                Year = year,
                TotalEmployees = total,
                FinalizedCount = finalized,
                SubmittedCount = pending,
                PendingReviewCount = pending,
                DraftCount = draft,
                RejectedCount = rejected,
                InProgressCount = inprogress,
                UntouchedCount = untouched,
                FinalizationPercentage = (int)(row.finalization_percentage ?? 0),
                SubmissionRate = total > 0 ? Math.Round((decimal)(pending + finalized) * 100m / total, 2) : 0,
                ComplianceRate = total > 0 ? Math.Round((decimal)finalized * 100m / total, 2) : 0
            };
        }
        catch (Exception ex) {
            throw ex;
        }
    }
    public async Task<bool> AdminFinalizeRecordAsync(ManualAttendanceUpdateDto dto, Guid managerId, Guid orgId)
    {
        try
        {
            if (dto.TimesheetId == Guid.Empty)
                throw new ArgumentException("TimesheetId is required for admin override.");

            var attendanceId = dto.AttendanceId == Guid.Empty ? (Guid?)null : dto.AttendanceId;

            string sql;

            if (attendanceId.HasValue)
            {
                sql = @"
            INSERT INTO attendance.finalized_timesheet_records (
                record_id,
                timesheet_id,
                employee_id,
                organization_id,
                work_date,
                final_checkin,
                final_checkout,
                final_status,
                approved_by,
                approved_at,
                attendance_id,
                override_reason,
                record_status
            )
            VALUES (
                uuid_generate_v4(),
                @TimesheetId,
                @EmployeeId,
                @OrgId,
                @WorkDate,
                @CheckInTime,
                @CheckOutTime,
                @Status,
                @ManagerId,
                NOW(),
                @AttendanceId,
                @Reason,
                'overridden'
            )
            ON CONFLICT (attendance_id) WHERE attendance_id IS NOT NULL
            DO UPDATE SET
                final_checkin   = EXCLUDED.final_checkin,
                final_checkout  = EXCLUDED.final_checkout,
                final_status    = EXCLUDED.final_status,
                approved_by     = EXCLUDED.approved_by,
                approved_at     = NOW(),
                attendance_id   = EXCLUDED.attendance_id,
                override_reason = EXCLUDED.override_reason,
                record_status   = 'overridden';";
            }
            else
            {
                sql = @"
            INSERT INTO attendance.finalized_timesheet_records (
                record_id,
                timesheet_id,
                employee_id,
                organization_id,
                work_date,
                final_checkin,
                final_checkout,
                final_status,
                approved_by,
                approved_at,
                attendance_id,
                override_reason,
                record_status
            )
            VALUES (
                uuid_generate_v4(),
                @TimesheetId,
                @EmployeeId,
                @OrgId,
                @WorkDate,
                @CheckInTime,
                @CheckOutTime,
                @Status,
                @ManagerId,
                NOW(),
                NULL,
                @Reason,
                'overridden'
            )
            ON CONFLICT (employee_id, work_date) WHERE attendance_id IS NULL
            DO UPDATE SET
                final_checkin   = EXCLUDED.final_checkin,
                final_checkout  = EXCLUDED.final_checkout,
                final_status    = EXCLUDED.final_status,
                approved_by     = EXCLUDED.approved_by,
                approved_at     = NOW(),
                override_reason = EXCLUDED.override_reason,
                record_status   = 'overridden';";
            }

            using var connection = CreateConnection();

            return await connection.ExecuteAsync(sql, new
            {
                dto.EmployeeId,
                OrgId = orgId,
                ManagerId = managerId,
                TimesheetId = dto.TimesheetId,
                AttendanceId = attendanceId,
                WorkDate = dto.WorkDate.Date,
                dto.CheckInTime,
                dto.CheckOutTime,
                dto.Status,
                Reason = dto.Notes
            }) > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying admin override for employee {EmployeeId} on {Date}", dto.EmployeeId, dto.WorkDate);
            throw;
        }
    }
    public async Task<int> FlipDraftsToPendingAsync(Guid employeeId, int month, int year, Guid timesheetId)
    {
        try
        {
            var sql = @"
        UPDATE attendance.attendance_update_requests
        SET status       = 'pending',
            timesheet_id = @timesheetId,  
            submitted_at = NOW()
        WHERE employee_id = @employeeId 
          AND status = 'draft'
          AND EXTRACT(MONTH FROM workdate) = @month
          AND EXTRACT(YEAR FROM workdate) = @year";
            using var connection = CreateConnection();

            if (connection == null)
                throw new Exception("❌ Database connection could not be established.");

            return await connection.ExecuteAsync(sql, new { employeeId, timesheetId, month, year });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flipping drafts to pending for employee {EmpId}", employeeId);
            throw;
        }
    }
    public async Task<bool> ClockInOutAsync(Guid employeeId, string action, DateTime timestamp, Dictionary<string, object>? location = null, string? notes = null)
    {
        try
        {
            var result = await ExecuteScalarAsync<bool>(
                "SELECT attendance.fn_clock_in_out(@employeeId, @action, @timestamp, @location::jsonb, @notes)",
                new
                {
                    employeeId,
                    action,
                    timestamp,
                    location = System.Text.Json.JsonSerializer.Serialize(location ?? new()),
                    notes
                });

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception("Check in faild", ex.InnerException);
        }
    }

    public async Task<Attendance?> UpdateTodayAttendanceLateStatusAsync(Guid employeeId)
    {
        try
        {
            var sql = @"
        UPDATE attendance.attendance a
        SET 
            islate = CASE
                        WHEN a.checkintime IS NULL THEN false
                        WHEN a.checkintime::time >= s.starttime + INTERVAL '1 hour' THEN true
                        ELSE false
                     END,
            updatedat = NOW() AT TIME ZONE 'UTC'
        FROM employee.employmentdetails e
        JOIN attendance.shifts s ON e.shiftid = s.shiftid
        WHERE a.employeeid = e.employeeid
          AND a.employeeid = @employeeId
          AND e.iscurrent = true
          AND a.workdate = (NOW() AT TIME ZONE 'UTC')::date
        RETURNING a.*;
        ";

            return await QuerySingleOrDefaultAsync<Attendance>(
                sql,
                new { employeeId }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating late status for employee {EmployeeId}",
                employeeId
            );
            throw;
        }
    }

    public async Task<Attendance?> UpdateOvertimeHoursAsync(Guid employeeId)
    {
        try
        {
            var sql = @"
        UPDATE attendance.attendance
        SET 
            overtimehours = CASE
                                WHEN totalhours > 8 THEN totalhours - 8
                                ELSE 0
                            END,
            updatedat = NOW() AT TIME ZONE 'UTC'
        WHERE employeeid = @employeeId
          AND workdate = (NOW() AT TIME ZONE 'UTC')::date
        RETURNING *;
        ";

            return await QuerySingleOrDefaultAsync<Attendance>(
                sql,
                new { employeeId }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating overtime hours for employee {EmployeeId}",
                employeeId
            );
            throw;
        }
    }





    public async Task<Attendance?> UpdateTodayAttendanceFromSessionsAsync(Guid employeeId)
    {
        try
        {
            var sql = @"
        WITH todays_attendance AS (
            SELECT attendanceid
            FROM attendance.attendance
            WHERE employeeid = @employeeId
              AND workdate = (NOW() AT TIME ZONE 'UTC')::date
        ),
        first_session AS (
            SELECT MIN(checkintime) AS first_checkin
            FROM attendance.attendancesessions
            WHERE attendanceid IN (SELECT attendanceid FROM todays_attendance)
        ),
        last_session AS (
            SELECT checkouttime AS last_checkout
            FROM attendance.attendancesessions
            WHERE attendanceid IN (SELECT attendanceid FROM todays_attendance)
            ORDER BY checkintime DESC
            LIMIT 1
        )
        UPDATE attendance.attendance a
        SET 
            checkintime  = (SELECT first_checkin FROM first_session),
            checkouttime = (SELECT last_checkout FROM last_session),
            updatedat    = NOW() AT TIME ZONE 'UTC'
        WHERE a.employeeid = @employeeId
          AND a.workdate = (NOW() AT TIME ZONE 'UTC')::date
        RETURNING a.*;
        ";

            return await QuerySingleOrDefaultAsync<Attendance>(
                sql,
                new { employeeId }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating today's attendance from sessions for employee {EmployeeId}",
                employeeId
            );
            throw;
        }
    }


    public async Task<Attendance?> GetTodayAttendanceAsync(Guid employeeId)
    {
        try
        {
            var sql = @"
                SELECT attendanceid, employeeid, workdate, checkintime, checkouttime,
                       breakstarttime, breakendtime, totalhours, overtimehours, breakhours,
                       status, location, notes, approvedby, approvedat, createdat, updatedat
                FROM attendance.attendance 
                WHERE employeeid = @employeeId AND workdate = (NOW() AT TIME ZONE 'UTC')::date";

            return await QuerySingleOrDefaultAsync<Attendance>(sql, new { employeeId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting today's attendance for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    //Checkin checkout Checkckkss

    public async Task<bool> CheckAlreadyClockInAsync(Guid employeeId)
    {
        try
        {
            var sql = @"
            SELECT EXISTS (
                SELECT 1
                FROM attendance.attendance
                WHERE employeeid = @employeeId
                  AND workdate = (NOW() AT TIME ZONE 'UTC')::date
                  AND checkouttime IS NULL
            );
        ";

            return await ExecuteScalarAsync<bool>(sql, new
            {
                employeeId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking already clock-in for employee {EmployeeId}", employeeId);
            throw;
        }
    }



    public async Task<PagedResult<AttendanceDto>> GetAttendanceAsync(
        Guid? employeeId = null, Guid? organizationId = null, DateTime? startDate = null,
        DateTime? endDate = null, int page = 1, int pageSize = 10, Guid? departmentId = null, string? Status = null)
    {
        try
        {
            var results = await QueryAsync<dynamic>(
                "SELECT * FROM attendance.fn_get_attendance(@employeeId, @organizationId,@departmentId,@Status,@startDate,@endDate, @limit, @offset)",
                new
                {
                    employeeId,
                    organizationId,
                    startDate,
                    endDate,
                    limit = pageSize,
                    offset = (page - 1) * pageSize,
                    departmentId,
                    Status 
                });

            var attendanceList = results.Select(r => new AttendanceDto
            {
                AttendanceId = r.attendanceid,
                EmployeeId = r.employeeid,
                EmployeeName = r.employeename,
                WorkDate = r.workdate,
                CheckInTime = r.checkintime,
                CheckOutTime = r.checkouttime,
                TotalHours = r.totalhours,
                SessionsCount = r.sessionscount,
                OvertimeHours = r.overtimehours,
                Status = r.status,
                Location = r.location,
                checkinip = r.checkinip,
                checkoutip = r.checkoutip,
                ShiftId = r.shiftid,
                Notes = r.notes
            }).ToList();

            var totalCount = results.FirstOrDefault()?.totalcount ?? 0;

            return new PagedResult<AttendanceDto>
            {
                Data = attendanceList,
                TotalCount = (int)totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance records. EmployeeId: {EmployeeId}, OrganizationId: {OrganizationId}", employeeId, organizationId);
            throw;
        }
    }


    public async Task<Guid?> GetAttendanceIdAsync(Guid employeeId, DateTime? date = null)
    {
        try
        {
            string sql;
            object param;

            if (date == null)
            {
                sql = @"
                SELECT attendanceid
                FROM attendance.attendance
                WHERE employeeid = @employeeId
                  AND workdate = (NOW() AT TIME ZONE 'UTC')::date
                LIMIT 1;
            ";
                param = new { employeeId };
            }
            else
            {
                sql = @"
                SELECT attendanceid
                FROM attendance.attendance
                WHERE employeeid = @employeeId
                  AND workdate::date = @workDate
                LIMIT 1;
            ";
                param = new { employeeId, workDate = date.Value.Date };
            }

            return await QuerySingleOrDefaultAsync<Guid?>(sql, param);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting attendance id for employee {EmployeeId} on date {Date}", employeeId, date);
            throw;
        }
    }



    public async Task<List<AttendanceSessionDto>> GetTodaySessionAsync(Guid? attendanceId)
    {
        try
        {
            if (attendanceId == null)
                return new List<AttendanceSessionDto>(); // return empty list if no attendance

            var sql = @"
            SELECT 
                sessionid AS SessionId,
                attendanceid AS AttendanceId,
                checkintime AS CheckInTime,
                checkouttime AS CheckOutTime
            FROM attendance.attendancesessions
            WHERE attendanceid = @attendanceId
            ORDER BY checkintime ASC;
        ";

            var sessions = await QueryAsync<AttendanceSessionDto>(sql, new
            {
                attendanceId
            });

            return sessions.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting today's attendance sessions for attendance {AttendanceId}", attendanceId);
            throw;
        }
    }


    public async Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(Guid employeeId, DateTime startDate, DateTime endDate)
    {
        try
        {
            // Calculate number of working days between startDate and endDate (Monday to Friday)
            int totalWorkDays = 0;
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    totalWorkDays++;
            }

            var sql = @"
            SELECT 
                COUNT(*) FILTER (WHERE status = 'present') AS presentdays,
                COUNT(*) FILTER (WHERE islate = TRUE) AS latedays,
                COALESCE(SUM(totalhours), 0) AS totalhours,
                COALESCE(SUM(overtimehours), 0) AS overtimehours
            FROM attendance.attendance
            WHERE employeeid = @employeeId
              AND workdate BETWEEN @startDate AND @endDate";

            var result = await QuerySingleAsync<AttendanceSummaryDto>(sql, new { employeeId, startDate, endDate });

            // Calculate absent days as remaining working days
            int absentDays = totalWorkDays - (result.PresentDays ?? 0);

            return new AttendanceSummaryDto
            {
                TotalWorkDays = totalWorkDays,
                PresentDays = result.PresentDays ?? 0,
                AbsentDays = absentDays,
                LateDays = result.LateDays, // you can calculate similarly if you have a islate column
                TotalHours = result.TotalHours,
                OvertimeHours = result.OvertimeHours,
                AverageHoursPerDay = totalWorkDays > 0 ? result.TotalHours / totalWorkDays : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance summary for employee {EmployeeId} from {StartDate} to {EndDate}", employeeId, startDate, endDate);
            throw;
        }
    }



    public async Task<PagedResult<AttendanceSessionWithInfoDto>> GetEmployeeSessionsWithInfoAsync(
        Guid employeeId,
        DateTime startDate,
        DateTime endDate,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var offset = (pageNumber - 1) * pageSize;

        // ? Paginated sessions (existing query)
        var sql = @"        
      SELECT 
          s.sessionid AS SessionId,
          s.attendanceid AS AttendanceId,
          s.checkintime AS CheckInTime,
          s.checkouttime AS CheckOutTime,
          s.checkinip,
          s.checkoutip,
          s.location AS Location,
          (e.firstname || ' ' || e.lastname) AS EmployeeName,
          a.workdate AS WorkDate
      FROM attendance.attendance a
      INNER JOIN attendance.attendancesessions s
          ON a.attendanceid = s.attendanceid
      INNER JOIN employee.employees e
          ON a.employeeid = e.employeeid
      WHERE a.employeeid = @employeeId
        AND a.workdate BETWEEN @startDate AND @endDate
      ORDER BY a.workdate DESC, s.checkintime ASC
      LIMIT @pageSize OFFSET @offset;
    ";

        var data = (await QueryAsync<AttendanceSessionWithInfoDto>(sql, new
        {
            employeeId,
            startDate,
            endDate,
            pageSize,
            offset
        })).ToList();

        // ? Count total sessions, not attendances
        var countSql = @"
      SELECT COUNT(*)
      FROM attendance.attendancesessions s
      INNER JOIN attendance.attendance a
          ON s.attendanceid = a.attendanceid
      WHERE a.employeeid = @employeeId
        AND a.workdate BETWEEN @startDate AND @endDate;
    ";

        var totalCount = await QuerySingleAsync<int>(countSql, new
        {
            employeeId,
            startDate,
            endDate
        });

        // ? Return paginated result
        return new PagedResult<AttendanceSessionWithInfoDto>
        {
            Data = data,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }



    public async Task<bool> UpdateAttendanceAsync(Guid attendanceId, Attendance attendance)
    {
        try
        {
            var sql = @"
                UPDATE attendance.attendance 
                SET checkintime = @checkInTime, checkouttime = @checkOutTime,
                    totalhours = @totalHours, status = @status, notes = @notes,
                    updatedat = CURRENT_TIMESTAMP
                WHERE attendanceid = @attendanceId";

            var affected = await ExecuteAsync(sql, new
            {
                attendanceId,
                checkInTime = attendance.CheckInTime,
                checkOutTime = attendance.CheckOutTime,
                totalHours = attendance.TotalHours,
                status = attendance.Status,
                notes = attendance.Notes
            });

            return affected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attendance record {AttendanceId}", attendanceId);
            throw;
        }
    }

    public async Task<List<AttendanceDto>> GetDepartmentAttendanceAsync(Guid departmentId, DateTime date)
    {
        try
        {
            var sql = @"
                SELECT a.attendanceid, a.employeeid, a.workdate, a.checkintime, 
                       a.checkouttime, a.totalhours, a.status,
                       CONCAT(e.firstname, ' ', e.lastname) as employeename
                FROM attendance.attendance a
                INNER JOIN employee.employees e ON a.employeeid = e.employeeid
                INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid
                WHERE ed.departmentid = @departmentId AND a.workdate = @date AND ed.iscurrent = true
                ORDER BY e.firstname, e.lastname";

            var results = await QueryAsync<dynamic>(sql, new { departmentId, date });

            return results.Select(r => new AttendanceDto
            {
                AttendanceId = r.attendance_id,
                EmployeeId = r.employee_id,
                EmployeeName = r.employee_name,
                WorkDate = r.work_date,
                CheckInTime = r.check_in_time,
                CheckOutTime = r.check_out_time,
                TotalHours = r.total_hours,
                Status = r.status
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting department attendance for department {DepartmentId} on {Date}", departmentId, date);
            throw;
        }
    }

    public async Task<Attendance?> GetAttendanceByIdAsync(Guid? attendanceId)
    {
        try
        {
            var sql = @"
                SELECT attendanceid, employeeid, workdate, checkintime, checkouttime,
                       totalhours, overtimehours, status, notes, createdat
                FROM attendance.attendance 
                WHERE attendanceid = @attendanceId";

            return await QuerySingleOrDefaultAsync<Attendance>(sql, new { attendanceId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance record by ID {AttendanceId}", attendanceId);
            throw;
        }
    }

    public async Task<bool> CreateShiftAsync(Guid organizationId, CreateShiftDto request)
    {
        try
        {
            var createdAt = DateTime.UtcNow;
                
            var sql = @"
        INSERT INTO attendance.shifts
        (organizationid, shiftname, starttime, endtime, applymarginhours, marginhours, breakduration, breakstarttime, breakendtime,
         overtimestarttime, maxovertimeinminutes, daysofweek, timezone, createdat)
        VALUES
        (@OrganizationId, @ShiftName, @StartTime, @EndTime,@ApplyMarginhours,@marginHours, @BreakDuration, @BreakStartTime, @BreakEndTime,
         @OvertimeStartTime, @MaxOvertimeInMinutes, @DaysofWeek, @Timezone, @CreatedAt);
    ";

            var parameters = new
            {
                OrganizationId = organizationId,
                ShiftName = request.ShiftName,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                ApplyMarginhours = request.ApplyMarginhours,
                marginHours = request.marginHours,

                BreakDuration = request.BreakDuration ?? 0,
                BreakStartTime = request.BreakStartTime,
                BreakEndTime = request.BreakEndTime,

                OvertimeStartTime = request.OvertimeStartTime,
                MaxOvertimeInMinutes = request.MaxOvertimeInMinutes ?? 0,

                DaysofWeek = request.DaysofWeek ?? new int[] { 1, 2, 3, 4, 5 },
                Timezone = request.Timezone ?? "UTC",

                CreatedAt = createdAt
            };

            var rowsAffected = await ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shift for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<ShiftDto?> GetshiftInfoAsync(Guid shiftId)
    {
        var sql = @"
        SELECT 
            shiftid     AS ShiftId,
            shiftname   AS ShiftName,
            starttime   AS StartTime,
            endtime     AS EndTime
        FROM attendance.shifts
        WHERE shiftid = @shiftId
          AND isactive = true;
    ";

        var shift = await QuerySingleOrDefaultAsync<ShiftDto>(
            sql,
            new { shiftId }
        );

        return shift;
    }


    public async Task<bool> AssignShiftToEmployeeAsync(Guid organizationId, AssignShiftDto request)
    {
        try
        {
            var sql = @"
            UPDATE employee.employmentdetails
            SET shiftid = @ShiftId
            WHERE employeeid = @EmployeeId
          
        ";

            var parameters = new
            {
                ShiftId = request.ShiftId,
                EmployeeId = request.EmployeeId,
                //OrganizationId = organizationId
            };

            var rowsAffected = await ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning shift {ShiftId} to employee {EmployeeId} for organization {OrganizationId}",
                request.ShiftId, request.EmployeeId, organizationId);
            throw;
        }

    }

    //Get by shift

    public async Task<List<EmployeeShiftDto>> GetEmployeesByShiftAsync(Guid organizationId, Guid shiftId)
    {
        try
        {


            var sql = @"
         SELECT 
        e.employeeid,
        e.firstname,
        e.lastname,
        e.email,
        ed.shiftid,
        d.departmentname  
         FROM employee.employees e
         INNER JOIN employee.employmentdetails ed 
         ON e.employeeid = ed.employeeid
         LEFT JOIN employee.departments d
        ON ed.departmentid = d.departmentid   -- join to get department name
          WHERE e.organizationid = @OrganizationId
           AND ed.shiftid = @ShiftId;
            ";

            var parameters = new { OrganizationId = organizationId, ShiftId = shiftId };
            var result = await QueryAsync<EmployeeShiftDto>(sql, parameters);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching employees for shift {ShiftId} in organization {OrganizationId}", shiftId, organizationId);
            throw;
        }
    }

    //Getting Employee BY shift id for Manager

    public async Task<List<EmployeeShiftDto>> GetManagerEmployeesByShiftAsync(Guid organizationId, Guid shiftId,Guid currentEmp)
    {
        try
        {


            var sql = @"
                SELECT 
                e.employeeid,
                e.firstname,
                e.lastname,
                e.email,
                ed.shiftid,
                d.departmentname  
            FROM employee.employees e
            INNER JOIN employee.employmentdetails ed 
                ON e.employeeid = ed.employeeid
            LEFT JOIN employee.departments d
                ON ed.departmentid = d.departmentid
            WHERE e.organizationid = @OrganizationId
              AND ed.shiftid = @ShiftId
              AND ed.reportingmanagerid = @CurrentEmp;
            ";

            var parameters = new { OrganizationId = organizationId, ShiftId = shiftId,CurrentEmp=currentEmp };
            var result = await QueryAsync<EmployeeShiftDto>(sql, parameters);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching employees for shift {ShiftId} in organization {OrganizationId}", shiftId, organizationId);
            throw;
        }
    }



    public async Task<Guid?> GetReportingManagerIdAsync(Guid employeeId)
    {
        var sql = @"
        SELECT reportingmanagerid
        FROM employee.employmentdetails
        WHERE employeeid = @EmployeeId
        AND iscurrent = true
        LIMIT 1;
    ";

        var result = await QuerySingleOrDefaultAsync<Guid?>(sql, new
        {
            EmployeeId = employeeId
        });

         return result; // Returns null if manager not found
    }

    public async Task<ShiftSwapRequestCreatedDto> CreateShiftSwapRequestAsync(Guid organizationId, CreateShiftSwapRequestDto requestDto)
    {
        try
        {
            var employeeId = await GetEmpId(requestDto.EmployeeId);
            var reportingmanagerid = await GetReportingManagerIdAsync(employeeId);


            var sql = @"
            INSERT INTO attendance.shiftswaprequests
            (employeeid, currentshiftid, requestedshiftid, reason, organizationid, reportingmanagerid)
            VALUES
            (@EmployeeId, @CurrentShiftId, @RequestedShiftId, @Reason, @OrganizationId, @ReportingManagerId)
                RETURNING
                requestid        AS RequestId,
                employeeid       AS EmployeeId,
                reportingmanagerid AS ReportingManagerId;
        ";

            var parameters = new
            {
                EmployeeId = employeeId,
                CurrentShiftId = requestDto.CurrentShiftId,
                RequestedShiftId = requestDto.RequestedShiftId,
                Reason = requestDto.Reason,
                organizationId = organizationId,
                ReportingManagerId=reportingmanagerid
            };
           return await QuerySingleAsync<ShiftSwapRequestCreatedDto>(sql,parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shift swap request for employee {EmployeeId}", requestDto.EmployeeId);
            return null;
        }
    }


    public async Task<bool> RejectShiftSwapRequestAsync(ApproveShiftSwapRequestDto dto)
    {
        try
        {
            var status = dto.IsApproved ? "approved" : "rejected";

            var sql = @"
            UPDATE attendance.shiftswaprequests
            SET status = @Status,
                approvedby = @ApprovedBy,
                approvedat = @ApprovedAt,
                rejectionreason = @RejectionReason,
                updatedat = @UpdatedAt
            WHERE requestid = @RequestId;
        ";

            var parameters = new
            {
                Status = status,
                ApprovedBy = dto.ApprovedBy,
                ApprovedAt = DateTime.UtcNow,
                RejectionReason = dto.RejectionReason,
                UpdatedAt = DateTime.Now,
                RequestId = dto.RequestId
            };

            var rows = await ExecuteAsync(sql, parameters);
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving/rejecting shift swap request {RequestId}", dto.RequestId);
            return false;
        }
    }


    //Accept Shift Swap Request

    public async Task<bool> AcceptswapRequestAsync(ApproveShiftSwapRequestDto dto)
    {
        try
        {
            var status = dto.IsApproved ? "approved" : "rejected";

            var sql = @"
            WITH updated_swap AS (
                UPDATE attendance.shiftswaprequests
                SET status = 'approved',
                    approvedby = @ApprovedBy,
                    approvedat = @ApprovedAt,
                    updatedat = NOW()
                WHERE requestid = @RequestId
                RETURNING employeeid, requestedshiftid
            )
            UPDATE employee.employmentdetails ed
            SET shiftid = u.requestedshiftid
            FROM updated_swap u
            WHERE ed.employeeid = u.employeeid;
        ";

            var parameters = new
            {
                Status = status,
                ApprovedBy = dto.ApprovedBy,
                ApprovedAt = DateTime.UtcNow,
                RejectionReason = dto.RejectionReason,
                UpdatedAt = DateTime.Now,
                RequestId = dto.RequestId
            };

            var rows = await ExecuteAsync(sql, parameters);
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving/rejecting shift swap request {RequestId}", dto.RequestId);
            return false;
        }
    }




    public async Task<List<PendingShiftSwapDto>> GetPendingShiftSwapRequestsAsync(Guid organizationId)
    {
        try
        {
            var sql = @"
    SELECT 
        ss.requestid,
        ss.employeeid,
        e.firstname || ' ' || e.lastname AS EmployeeName,
        ss.currentshiftid,
        cs.shiftname AS CurrentShiftName,
        ss.requestedshiftid,
        rs.shiftname AS RequestedShiftName,
        ss.reason,
        ss.status
    FROM attendance.shiftswaprequests ss
    INNER JOIN employee.employees e 
        ON ss.employeeid = e.employeeid
    LEFT JOIN attendance.shifts cs
        ON ss.currentshiftid = cs.shiftid
    LEFT JOIN attendance.shifts rs
        ON ss.requestedshiftid = rs.shiftid
    WHERE ss.organizationid = @OrganizationId
      AND ss.status = 'pending';
";

            var parameters = new { OrganizationId = organizationId };
            var result = await QueryAsync<PendingShiftSwapDto>(sql, parameters);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending shift swap requests for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<List<PendingShiftSwapDto>> GetManagerPendingShiftsAsync(Guid organizationId, Guid currentEmployeeId)
    {
        try
        {
            var sql = @"
        SELECT 
            ss.requestid,
            ss.employeeid,
            e.firstname || ' ' || e.lastname AS EmployeeName,
            ss.currentshiftid,
            cs.shiftname AS CurrentShiftName,
            ss.requestedshiftid,
            rs.shiftname AS RequestedShiftName,
            ss.reason,
            ss.status
        FROM attendance.shiftswaprequests ss
        INNER JOIN employee.employees e 
            ON ss.employeeid = e.employeeid
        LEFT JOIN attendance.shifts cs
            ON ss.currentshiftid = cs.shiftid
        LEFT JOIN attendance.shifts rs
            ON ss.requestedshiftid = rs.shiftid
        WHERE ss.organizationid = @OrganizationId
          AND ss.reportingmanagerid = @ManagerId
          AND ss.status = 'pending';
        ";

            var parameters = new
            {
                OrganizationId = organizationId,
                ManagerId = currentEmployeeId
            };

            var result = await QueryAsync<PendingShiftSwapDto>(sql, parameters);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending shift swap requests for manager {ManagerId} in organization {OrganizationId}", currentEmployeeId, organizationId);
            throw;
        }
    }


    public async Task<ShiftDto?> GetShiftByEmployeeIdAsync(Guid employeeIdd)
    {
        try
        {
            var sql = @"
       SELECT 
    s.shiftid AS ""ShiftId"",
    s.organizationid AS ""OrganizationId"",
    s.shiftname AS ""ShiftName"",
    s.starttime AS ""StartTime"",
    s.endtime AS ""EndTime"",
    s.applymarginhours AS ""ApplyMarginhours"",
    s.marginhours AS ""marginHours"",
    s.breakduration AS ""BreakDuration"",
    s.daysofweek AS ""DaysOfWeek""
FROM employee.employmentdetails ed
INNER JOIN attendance.shifts s
    ON ed.shiftid = s.shiftid
WHERE ed.employeeid = @EmployeeId
  AND ed.iscurrent = TRUE;

    ";

            var parameters = new { EmployeeId = employeeIdd };
            var result = await QuerySingleOrDefaultAsync<ShiftDto>(sql, parameters);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching shift for employee {EmployeeId}", employeeIdd);
            throw;
        }
    }

   
    //Get employee all request

    public async Task<List<PendingShiftSwapDto>> GetShiftSwapRequestsByEmployeeAsync(Guid employeeIdd)
    {
        try
        {


            var employeeId = await GetEmpId(employeeIdd);

            var sql = @"
            SELECT 
                ss.requestid,
                ss.employeeid,
                e.firstname || ' ' || e.lastname AS EmployeeName,
                ss.currentshiftid,
                cs.shiftname AS CurrentShiftName,
                ss.requestedshiftid,
                rs.shiftname AS RequestedShiftName,
                ss.status
            FROM attendance.shiftswaprequests ss
            INNER JOIN employee.employees e ON ss.employeeid = e.employeeid
            LEFT JOIN attendance.shifts cs ON ss.currentshiftid = cs.shiftid
            INNER JOIN attendance.shifts rs ON ss.requestedshiftid = rs.shiftid
            WHERE ss.employeeid = @EmployeeId
            ORDER BY ss.submittedat DESC;
        ";

            var parameters = new { EmployeeId = employeeId };
            var result = await QueryAsync<PendingShiftSwapDto>(sql, parameters);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching shift swap requests for employee {EmployeeId}");
            throw;
        }
    }


    public async Task<List<ShiftDto>> GetAllShiftsAsync(Guid organizationId)
    {
        try
        {
            var sql = @"
        SELECT 
            shiftid AS ShiftId,
            organizationid AS OrganizationId,
            shiftname AS ShiftName,
            starttime AS StartTime,
            endtime AS EndTime,
            applymarginhours AS ApplyMarginhours,
            marginhours AS marginHours,
            breakstarttime AS BreakStartTime,
            breakendtime AS BreakEndTime,
            overtimestarttime AS OvertimeStartTime,
            maxovertimeinminutes AS MaxOvertimeInMinutes,
            breakduration AS BreakDuration,
            daysofweek AS DaysOfWeek,
            timezone AS Timezone,
            isactive AS IsActive,
            createdat AS CreatedAt
        FROM attendance.shifts
        WHERE organizationid = @OrganizationId
        ORDER BY createdat DESC;
        ";

            var parameters = new { OrganizationId = organizationId };
            var result = await QueryAsync<ShiftDto>(sql, parameters);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching shifts for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<bool> UpdateShiftAsync(Guid organizationId, Guid shiftId, UpdateShiftDto request)
    {
        try
        {
            var sql = @"
        UPDATE attendance.shifts
        SET
            shiftname = @ShiftName,
            starttime = @StartTime,
            endtime = @EndTime,
            marginhours = @marginHours,
            applymarginhours = @ApplyMarginhours,
            breakstarttime = @BreakStartTime,
            breakendtime = @BreakEndTime,
            overtimestarttime = @OvertimeStartTime,
            maxovertimeinminutes = @MaxOvertimeInMinutes,
            breakduration = @BreakDuration,
            daysofweek = @DaysofWeek,
            timezone = @Timezone
        WHERE
            organizationid = @OrganizationId AND shiftid = @ShiftId;
        ";

            var parameters = new
            {
                OrganizationId = organizationId,
                ShiftId = shiftId,
                ShiftName = request.ShiftName,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                ApplyMarginhours = request.ApplyMarginhours,
                MarginHours = request.marginHours,
                BreakStartTime = request.BreakStartTime,
                BreakEndTime = request.BreakEndTime,
                OvertimeStartTime = request.OvertimeStartTime,
                MaxOvertimeInMinutes = request.MaxOvertimeInMinutes,
                BreakDuration = request.BreakDuration ?? 0,
                DaysofWeek = request.DaysofWeek ?? new int[] { 1, 2, 3, 4, 5 },
                Timezone = request.Timezone ?? "UTC"
            };

            var rowsAffected = await ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shift {ShiftId} for organization {OrganizationId}", shiftId, organizationId);
            throw;
        }
    }


    // Get Shift Swap Request Details by RequestId
    public async Task<ShiftSwapRequestDto?> getshifrequestDetailsAsync(Guid requestId)
    {
        var sql = @"
        SELECT
            requestid            AS RequestId,
            organizationid       AS OrganizationId,
            employeeid           AS EmployeeId,
            currentshiftid       AS CurrentShiftId,
            requestedshiftid     AS RequestedShiftId,
            reason               AS Reason,
            status               AS Status,
            submittedat          AS SubmittedAt,
            approvedby           AS ApprovedBy,
            approvedat           AS ApprovedAt,
            rejectionreason      AS RejectionReason,
            updatedat            AS UpdatedAt,
            reportingmanagerid   AS ReportingManagerId
        FROM attendance.shiftswaprequests
        WHERE requestid = @requestId;
    ";

        return await QuerySingleOrDefaultAsync<ShiftSwapRequestDto>(
            sql,
            new { requestId }
        );
    }



    public async Task<bool> DeleteShiftAsync(Guid organizationId, Guid shiftId)
    {
        try
        {
            var sql = @"
            DELETE FROM attendance.shifts
            WHERE organizationid = @OrganizationId AND shiftid = @ShiftId;
        ";

            var parameters = new
            {
                OrganizationId = organizationId,
                ShiftId = shiftId
            };

            var rowsAffected = await ExecuteAsync(sql, parameters);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting shift {ShiftId} for organization {OrganizationId}", shiftId, organizationId);
            throw;
        }
    }





    public async Task<Attendance> CreateAttendanceAsync(Attendance attendance)
    {
        try
        {
            var sql = @"
                INSERT INTO attendance.attendance 
                (employeeid, workdate, checkintime, checkouttime, status, notes, totalhours, overtimehours, createdat)
                VALUES (@EmployeeId, @WorkDate, @CheckInTime, @CheckOutTime, @Status, @Notes, @TotalHours, @OvertimeHours, @CreatedAt)
                RETURNING attendanceid, employeeid, workdate, checkintime, checkouttime, totalhours, overtimehours, status, notes, createdat";

            var result = await QuerySingleAsync<Attendance>(sql, attendance);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating attendance record for employee {EmployeeId}", attendance.EmployeeId);
            throw;
        }
    }

    public async Task<bool> DeleteAttendanceAsync(Guid attendanceId)
    {
        try
        {
            var sql = "DELETE FROM attendance.attendance WHERE attendance_id = @attendanceId";
            var rowsAffected = await ExecuteAsync(sql, new { attendanceId });
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attendance record {AttendanceId}", attendanceId);
            throw;
        }
    }

    public async Task<List<Attendance>> GetEmployeeAttendanceAsync(Guid employeeId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var sql = @"
                SELECT attendanceid, employeeid, workdate, checkintime, checkouttime,
                       totalhours, overtimehours, status, notes, createdat
                FROM attendance.attendance 
                WHERE employeeid = @employeeId 
                AND workdate >= @startDate 
                AND workdate <= @endDate
                ORDER BY workdate DESC";

            var results = await QueryAsync<Attendance>(sql, new { employeeId, startDate, endDate });
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance records for employee {EmployeeId} from {StartDate} to {EndDate}", employeeId, startDate, endDate);
            throw;
        }
    }


    public async Task<AttendanceReportDto> GetAttendanceReportAsync(Guid organizationId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var sql = @"
        -- Generate all dates in the range
        WITH date_range AS (
            SELECT generate_series(@startDate::date, @endDate::date, '1 day') AS workdate
        ),
        -- Active employees in the organization
        employees AS (
            SELECT e.employeeid
            FROM employee.employees e
            WHERE e.organizationid = @organizationId
              AND e.status = 'active'
        ),
        -- Cross join employees � dates to create full calendar
        calendar AS (
            SELECT dr.workdate, e.employeeid
            FROM date_range dr
            CROSS JOIN employees e
        ),
        -- Join attendance table to the full calendar
        attendance_data AS (
            SELECT 
                c.workdate,
                c.employeeid,
                a.status,
                a.islate,
                COALESCE(a.totalhours,0) AS totalhours,
                COALESCE(a.overtimehours,0) AS overtimehours
            FROM calendar c
            LEFT JOIN attendance.attendance a
                ON a.employeeid = c.employeeid
               AND a.workdate = c.workdate
        ),
        -- Classify absent/present and late
        classified AS (
            SELECT 
                employeeid,
                workdate,
                COALESCE(status,'absent') AS status,
                totalhours,
                overtimehours,
                CASE WHEN islate THEN 1 ELSE 0 END AS islate
            FROM attendance_data
        )
        -- Final report
        SELECT
            @startDate AS StartDate,
            @endDate AS EndDate,
            (SELECT COUNT(*) FROM employees) AS TotalEmployees,
            (SELECT COUNT(*) FROM classified WHERE status = 'present') AS TotalPresentDays,
            (SELECT COUNT(*) FROM classified WHERE status = 'absent') AS TotalAbsentDays,
            (SELECT SUM(islate) FROM classified) AS TotalLateDays,
            (SELECT SUM(totalhours) FROM classified) AS TotalHours,
            (SELECT SUM(overtimehours) FROM classified) AS OvertimeHours,
            CASE 
                WHEN (SELECT COUNT(*) FROM classified) > 0
                THEN ROUND(
                    (SELECT COUNT(*) FROM classified WHERE status = 'present') * 100.0 /
                    NULLIF((SELECT COUNT(*) FROM classified), 0), 2
                )
                ELSE 0
            END AS AverageAttendanceRate
        ";

            using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();

            var report = await connection.QueryFirstAsync<AttendanceReportDto>(sql, new
            {
                organizationId,
                startDate,
                endDate
            });

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating attendance report from database");
            throw;
        }
    }



    public async Task<List<DepartmentAttendanceStatsDto>> GetDepartmentAttendanceStatsAsync(
    Guid organizationId, DateTime startDate, DateTime endDate, Guid? departmentId)
    {
        var sql = @"
    WITH employees AS (
        SELECT e.employeeid, ed.departmentid
        FROM employee.employees e
        INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
        WHERE e.organizationid = @organizationId
          AND e.status = 'active'
          AND (@departmentId IS NULL OR ed.departmentid = @departmentId)
    ),
    date_range AS (
        SELECT generate_series(@startDate::date, @endDate::date, interval '1 day')::date AS workdate
    ),
    calendar AS (
        SELECT d.employeeid, d.departmentid, dr.workdate
        FROM employees d
        CROSS JOIN date_range dr
    ),
    attendance_data AS (
        SELECT c.employeeid, c.departmentid, c.workdate,
               COALESCE(a.status, 'absent') AS status
        FROM calendar c
        LEFT JOIN attendance.attendance a
            ON a.employeeid = c.employeeid
           AND a.workdate = c.workdate
    )
    SELECT 
        d.departmentid AS DepartmentId,
        d.departmentname AS DepartmentName,
        COUNT(DISTINCT e.employeeid) AS TotalEmployees,
        SUM(CASE WHEN ad.status = 'present' THEN 1 ELSE 0 END) AS PresentEmployees,
        SUM(CASE WHEN ad.status = 'absent' THEN 1 ELSE 0 END) AS AbsentEmployees,
        CASE 
            WHEN COUNT(DISTINCT e.employeeid) > 0 AND COUNT(DISTINCT dr.workdate) > 0 THEN
                ROUND(
                    SUM(CASE WHEN ad.status = 'present' THEN 1 ELSE 0 END) * 100.0 /
                    NULLIF(COUNT(DISTINCT e.employeeid) * COUNT(DISTINCT dr.workdate), 0),
                    2
                )
            ELSE 0
        END AS AttendanceRate
    FROM employee.departments d
    LEFT JOIN employee.employmentdetails ed ON d.departmentid = ed.departmentid AND ed.iscurrent = true
    LEFT JOIN employee.employees e ON ed.employeeid = e.employeeid AND e.status = 'active'
    CROSS JOIN date_range dr
    LEFT JOIN attendance_data ad ON ad.employeeid = e.employeeid 
                                 AND ad.departmentid = d.departmentid
                                 AND ad.workdate = dr.workdate
    WHERE d.organizationid = @organizationId
      AND d.isactive = true
      AND (@departmentId IS NULL OR d.departmentid = @departmentId)
    GROUP BY d.departmentid, d.departmentname
    ORDER BY d.departmentname;
    ";

        using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();
        return (await connection.QueryAsync<DepartmentAttendanceStatsDto>(sql, new
        {
            organizationId,
            startDate,
            endDate,
            departmentId
        })).ToList();
    }



    public async Task<List<DailyAttendanceStatsDto>> GetDailyAttendanceStatsAsync(Guid organizationId, DateTime startDate, DateTime endDate)
    {
        var sql = @"
        SELECT 
            a.workdate as Date,
            COUNT(DISTINCT a.employeeid) as TotalEmployees,
            COUNT(CASE WHEN a.status = 'present' THEN 1 END) as PresentEmployees,
            COUNT(CASE WHEN a.status = 'absent' THEN 1 END) as AbsentEmployees,
            COUNT(CASE WHEN a.status = 'late' THEN 1 END) as LateEmployees,
            CASE 
                WHEN COUNT(DISTINCT a.employeeid) > 0 
                THEN ROUND(
                    COUNT(CASE WHEN a.status = 'present' THEN 1 END) * 100.0 / 
                    COUNT(DISTINCT a.employeeid), 
                    2
                )
                ELSE 0 
            END as AttendancePercentage
        FROM attendance.attendance a
        INNER JOIN employee.employees e ON a.employeeid = e.employeeid
        WHERE e.organizationid = @organizationId
        AND a.workdate BETWEEN @startDate AND @endDate
        GROUP BY a.workdate
        ORDER BY a.workdate";
        using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();
        return (await connection.QueryAsync<DailyAttendanceStatsDto>(sql, new
        {
            organizationId,
            startDate,
            endDate
        })).ToList();
    }

   

    public async Task<PagedResult<AttendanceDto>> GetAttendanceRecordsAsync(
    Guid organizationId,
    DateTime startDate,
    DateTime endDate,
    Guid? departmentId = null,
    string? status = null,
    Guid? employeeId = null,
    int pageNumber = 1,
    int pageSize = 10)
    {
        var offset = (pageNumber - 1) * pageSize;

        var countSql = @"
    -- Get count of records
    WITH employees AS (
        SELECT e.employeeid, e.firstname, e.lastname, ed.departmentid
        FROM employee.employees e
        INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
        WHERE e.organizationid = @organizationId
          AND e.status = 'active'
          AND (@departmentId IS NULL OR ed.departmentid = @departmentId)
          AND (@employeeId IS NULL OR e.employeeid = @employeeId)
    ),
    date_range AS (
        SELECT generate_series(@startDate::date, @endDate::date, interval '1 day')::date AS workdate
    ),
    calendar AS (
        SELECT emp.employeeid, emp.firstname, emp.lastname, emp.departmentid, dr.workdate
        FROM employees emp
        CROSS JOIN date_range dr
    ),
    attendance_data AS (
        SELECT 
            c.employeeid,
            c.firstname,
            c.lastname,
            c.workdate,
            COALESCE(a.status, 'absent') AS status
        FROM calendar c
        LEFT JOIN attendance.attendance a
            ON a.employeeid = c.employeeid
           AND a.workdate = c.workdate
    )
    SELECT COUNT(*)
    FROM attendance_data
    WHERE (@status IS NULL OR status = @status);
    ";

        var sql = @"
    -- Get all active employees with optional department and employee filter
    WITH employees AS (
        SELECT e.employeeid, e.firstname, e.lastname, ed.departmentid
        FROM employee.employees e
        INNER JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
        WHERE e.organizationid = @organizationId
          AND e.status = 'active'
          AND (@departmentId IS NULL OR ed.departmentid = @departmentId)
          AND (@employeeId IS NULL OR e.employeeid = @employeeId)  -- new filter
    ),
    -- Generate the date range
    date_range AS (
        SELECT generate_series(@startDate::date, @endDate::date, interval '1 day')::date AS workdate
    ),
    -- Cross join to get all employee � date combinations
    calendar AS (
        SELECT emp.employeeid, emp.firstname, emp.lastname, emp.departmentid, dr.workdate
        FROM employees emp
        CROSS JOIN date_range dr
    ),
    -- Left join attendance to mark present and absent
    attendance_data AS (
        SELECT 
            c.employeeid,
            c.firstname,
            c.lastname,
            c.workdate,
            COALESCE(a.attendanceid, NULL) AS attendanceid,
            COALESCE(a.checkintime, NULL) AS checkintime,
            COALESCE(a.checkouttime, NULL) AS checkouttime,
            COALESCE(a.totalhours, 0) AS totalhours,
            COALESCE(a.overtimehours, 0) AS overtimehours,
            COALESCE(a.status, 'absent') AS status,
            a.notes,
            COALESCE(a.islate, false) AS islate
        FROM calendar c
        LEFT JOIN attendance.attendance a
            ON a.employeeid = c.employeeid
           AND a.workdate = c.workdate
    )
    SELECT 
        attendanceid AS AttendanceId,
        employeeid AS EmployeeId,
        firstname || ' ' || lastname AS EmployeeName,
        workdate AS WorkDate,
        workdate AS Date,
        checkintime AS CheckInTime,
        checkouttime AS CheckOutTime,
        totalhours AS TotalHours,
        overtimehours AS OvertimeHours,
        status AS Status,
        notes AS Notes,
        islate AS IsLate
    FROM attendance_data
    WHERE (@status IS NULL OR status = @status)
    ORDER BY workdate DESC, firstname, lastname
    LIMIT @pageSize OFFSET @offset;
    ";

        using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var parameters = new
        {
            organizationId,
            startDate,
            endDate,
            departmentId,
            status,
            employeeId,
            pageSize,
            offset
        };

        var data = (await connection.QueryAsync<AttendanceDto>(sql, parameters)).ToList();
        var totalCount = await connection.QuerySingleAsync<int>(countSql, new
        {
            organizationId,
            startDate,
            endDate,
            departmentId,
            status,
            employeeId
        });

        return new PagedResult<AttendanceDto>
        {
            Data = data,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<EmployeeSimpleDto>> GetEmployeeForAdminAsync(
    Guid organizationId,
    Guid? departmentId)
    {
        try
        {
            var sql = @"
       SELECT 
    e.employeeid       AS EmployeeId,
    e.employeecode     AS EmployeeCode,
    e.firstname        AS FirstName,
    e.lastname         AS LastName,
    p.positiontitle    AS Position,         
    e.email            AS Email,
    e.status           AS Status,
    e.hiredate         AS HireDate,    
    CONCAT(m.firstname, ' ', m.lastname) AS ReportingManagerName 

FROM employee.employees e

INNER JOIN employee.employmentdetails d 
    ON e.employeeid = d.employeeid

LEFT JOIN employee.positions p 
    ON d.positionid = p.positionid

LEFT JOIN employee.employees m 
    ON d.reportingmanagerid = m.employeeid

WHERE e.organizationid = @OrganizationId
  AND e.status = 'active'
  AND d.iscurrent = true
  AND (@DepartmentId IS NULL OR d.departmentid = @DepartmentId)

ORDER BY e.firstname, e.lastname";

            var parameters = new
            {
                OrganizationId = organizationId,
                DepartmentId = departmentId
            };

            var result = await QueryAsync<EmployeeSimpleDto>(sql, parameters);

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving employee list for Admin in Organization {OrganizationId}",
                organizationId
            );

            throw;
        }
    }


    public async Task<List<EmployeeSimpleDto>> GetManagerEmployeeAsync(
        Guid managerId,
        Guid organizationId,
        Guid? departmentId)
    {
        try
        {
            var sql = @"
            SELECT 
                e.employeeid AS EmployeeId,
                e.firstname AS FirstName,
                e.lastname AS LastName,
                p.positiontitle    AS Position,
                CONCAT(m.firstname, ' ', m.lastname) AS ReportingManagerName
            FROM employee.employees e
            INNER JOIN employee.employmentdetails d
                ON e.employeeid = d.employeeid

LEFT JOIN employee.positions p 
    ON d.positionid = p.positionid


LEFT JOIN employee.employees m 
    ON d.reportingmanagerid = m.employeeid

            WHERE e.organizationid = @OrganizationId
              AND e.status = 'active'
              AND d.reportingmanagerid = @ManagerId
              AND d.iscurrent = true
              AND (@DepartmentId IS NULL OR d.departmentid = @DepartmentId)
            ORDER BY e.firstname, e.lastname;
        ";

            var parameters = new
            {
                OrganizationId = organizationId,
                ManagerId = managerId,
                DepartmentId = departmentId
            };

            // ?? This matches EXACTLY your UpdateShiftAsync repo style
            var result = await QueryAsync<EmployeeSimpleDto>(sql, parameters);

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving employees for Manager {ManagerId} in Organization {OrganizationId}",
                managerId, organizationId);

            throw;
        }
    }



    public async Task<Guid> GetEmpId(Guid? employeeId)
    {
        try
        {
            var sql = @"
            SELECT em.employeeid
            FROM employee.employees em
            WHERE ( em.userid = @employeeId)
            LIMIT 1";

            var results = await QueryAsync<Guid>(sql, new { employeeId });

            return results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            return Guid.Empty;
        }
    }
    public async Task<DailyAttendanceStatsDto> GetDailyAttendanceStatsAsync(Guid organizationId, DateTime date)
    {
        try
        {
            const string sql = @"
WITH emp AS (
    SELECT e.employeeid
    FROM employee.employees e
    WHERE e.organizationid = @organizationId
),
PresentEmployees AS (
    SELECT DISTINCT a.employeeid
    FROM attendance.attendance a
    INNER JOIN emp ON emp.employeeid = a.employeeid
    WHERE a.workdate = @date
      AND LOWER(TRIM(a.status)) = 'present'
),
OnLeaveEmployees AS (
    SELECT DISTINCT lr.employeeid
    FROM attendance.leaverequests lr
    INNER JOIN emp ON emp.employeeid = lr.employeeid
    WHERE LOWER(TRIM(lr.status)) = 'approved'
      AND @date BETWEEN lr.startdate AND lr.enddate
)
SELECT
    @date AS Date,
    COALESCE((SELECT COUNT(*) FROM emp), 0) AS TotalEmployees,
    COALESCE((SELECT COUNT(*) FROM PresentEmployees), 0) AS PresentEmployees,
    COALESCE((SELECT COUNT(*) FROM OnLeaveEmployees), 0) AS OnLeaveEmployees,
    GREATEST(
        COALESCE((SELECT COUNT(*) FROM emp), 0)
        - COALESCE((SELECT COUNT(*) FROM PresentEmployees), 0)
        - COALESCE((SELECT COUNT(*) FROM OnLeaveEmployees), 0),
        0
    ) AS AbsentEmployees,
    ROUND(
        (COALESCE((SELECT COUNT(*)::numeric FROM PresentEmployees), 0)
         / NULLIF(COALESCE((SELECT COUNT(*)::numeric FROM emp),0), 0)
        ) * 100,
    2) AS AttendancePercentage;
";

            var parameters = new DynamicParameters();
            parameters.Add("organizationId", organizationId, DbType.Guid);
            parameters.Add("date", date.Date, DbType.Date);

            return await QuerySingleOrDefaultAsync<DailyAttendanceStatsDto>(sql, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting daily attendance stats for organization {OrganizationId} on {Date}",
                organizationId, date);
            throw;
        }
    }
    public async Task<DailyAttendanceStatsDto> GetDailyAttendanceStatsForManagerAsync(Guid employeeId,Guid organizationId, DateTime date)
    {
        try
        {
            const string sql = @"
WITH emp AS (
    SELECT DISTINCT e.employeeid
    FROM employee.employees e
    INNER JOIN employee.employmentdetails ed 
        ON ed.employeeid = e.employeeid 
        AND ed.iscurrent = true
    WHERE e.organizationid = @organizationId
      AND ed.reportingmanagerid = @employeeId
      AND e.status != 'deleted'
),
PresentEmployees AS (
    SELECT DISTINCT a.employeeid
    FROM attendance.attendance a
    INNER JOIN emp ON emp.employeeid = a.employeeid
    WHERE a.workdate = @date
      AND LOWER(TRIM(a.status)) = 'present'
),
OnLeaveEmployees AS (
    SELECT DISTINCT lr.employeeid
    FROM attendance.leaverequests lr
    INNER JOIN emp ON emp.employeeid = lr.employeeid
    WHERE LOWER(TRIM(lr.status)) = 'approved'
      AND @date BETWEEN lr.startdate AND lr.enddate
)
SELECT
    @date AS ""Date"",
    COALESCE((SELECT COUNT(*) FROM emp), 0) AS ""TotalEmployees"",
    COALESCE((SELECT COUNT(*) FROM PresentEmployees), 0) AS ""PresentEmployees"",
    COALESCE((SELECT COUNT(*) FROM OnLeaveEmployees), 0) AS ""OnLeaveEmployees"",
    GREATEST(
        COALESCE((SELECT COUNT(*) FROM emp), 0)
        - COALESCE((SELECT COUNT(*) FROM PresentEmployees), 0)
        - COALESCE((SELECT COUNT(*) FROM OnLeaveEmployees), 0),
        0
    ) AS ""AbsentEmployees"",
    ROUND(
        (COALESCE((SELECT COUNT(*)::numeric FROM PresentEmployees), 0)
         / NULLIF(COALESCE((SELECT COUNT(*)::numeric FROM emp),0), 0)
        ) * 100,
    2) AS ""AttendancePercentage"";

";

            var parameters = new DynamicParameters();
            parameters.Add("organizationId", organizationId, DbType.Guid);
            parameters.Add("employeeId", employeeId, DbType.Guid);
            parameters.Add("date", date.Date, DbType.Date);

            return await QuerySingleOrDefaultAsync<DailyAttendanceStatsDto>(sql, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting daily attendance stats for manager {EmployeeId} in organization {OrganizationId} on {Date}",
                employeeId, organizationId, date);
            throw;
        }
    }
    public async Task<Guid> GetEmployeeFromTable(Guid Userid)
    {
        try
        {
            var sql = @"SELECT employeeid FROM employee.employees WHERE userid = @Userid";
            var employeeId = await QuerySingleOrDefaultAsync<Guid?>(sql, new { Userid });

            if (!employeeId.HasValue || employeeId.Value == Guid.Empty)
            {
                throw new UnauthorizedAccessException($"No employee record found for user {Userid}. Please ensure your user account is linked to an employee record.");
            }

            return employeeId.Value;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving employee ID for user {Userid}", ex);
        }
    }

    async Task<bool> IAttendanceRepository.IsHRManagerAsync(Guid employeeId, Guid organizationId)
    {
        try
        {
            var sql = @"SELECT 
    CASE 
        WHEN r.rolename = 'HR Manager' THEN TRUE
        ELSE FALSE
    END AS is_hr_manager
FROM employee.employees e
JOIN auth.users u ON u.userid = e.userid
JOIN auth.roles r ON r.roleid = u.roleid
WHERE e.employeeid = @employeeid AND u.organizationId = @organizationId;
";
            var parameters = new
            {
                employeeId,
                organizationId
            };
            var isHRManager = await QuerySingleAsync<bool>(sql, parameters);
            return isHRManager;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error checking HR manager status for employee {employeeId} in organization {organizationId}", ex);
        }
    }
    public async Task<IEnumerable<PendingAttendanceRequest>> GetAllReviewableRequestsAsync(
    Guid organizationId, Guid? managerId = null)
    {
        var sql = @"
    SELECT 
        r.request_id        AS RequestId,
        r.attendance_id     AS AttendanceId,
        r.employee_id       AS EmployeeId,
        (e.firstname || ' ' || e.lastname) AS EmployeeName,
        e.employeecode      AS EmployeeCode,
        COALESCE(a.workdate, r.workdate) AS WorkDate,
        a.checkintime       AS OriginalCheckIn,
        a.checkouttime      AS OriginalCheckOut,
        a.status            AS OriginalStatus,
        r.requested_checkin  AS RequestedCheckIn,
        r.requested_checkout AS RequestedCheckOut,
        r.requested_status   AS RequestedStatus,
        r.reason_for_edit    AS ReasonForEdit,
        r.requested_notes    AS RequestedNotes,
        r.rejection_reason   AS RejectionReason,
        r.submitted_at       AS Submittedat,
        r.status             AS Status
    FROM attendance.attendance_update_requests r
    JOIN employee.employees e ON r.employee_id = e.employeeid
    LEFT JOIN attendance.attendance a ON r.attendance_id = a.attendanceid
    LEFT JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid AND ed.iscurrent = true
    WHERE e.organizationid = @organizationId
      AND r.status IN ('pending', 'approved', 'rejected')   -- all actionable states
      AND (@managerId IS NULL OR ed.reportingmanagerid = @managerId)
    ORDER BY r.submitted_at DESC";

        return await QueryAsync<PendingAttendanceRequest>(sql, new { organizationId, managerId });
    }
    async Task<bool> IAttendanceRepository.IsAdminOrHRAsync(Guid employeeId, Guid organizationId)
    {
        try
        {
            var sql = @"SELECT 
    CASE 
        WHEN r.rolename IN ('Super Admin', 'HR Manager') THEN TRUE
        ELSE FALSE
    END AS is_admin_or_hr
FROM employee.employees e
JOIN auth.users u ON u.userid = e.userid
JOIN auth.roles r ON r.roleid = u.roleid
WHERE e.employeeid = @employeeid AND u.organizationId = @organizationId;
";
            var parameters = new
            {
                employeeId,
                organizationId
            };
            var isAdminOrHR = await QuerySingleAsync<bool>(sql, parameters);
            return isAdminOrHR;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error checking Admin/HR status for employee {employeeId} in organization {organizationId}", ex);
        }
    }

    async Task<List<Guid>> IAttendanceRepository.GetTeamEmployeeIdsAsync(Guid managerEmployeeId, Guid organizationId)
    {
        try
        {
            var sql = @"
                SELECT DISTINCT e.employeeid
                FROM employee.employees e
                INNER JOIN employee.employmentdetails ed 
                    ON ed.employeeid = e.employeeid 
                    AND ed.iscurrent = true
                WHERE e.organizationid = @organizationId
                  AND ed.reportingmanagerid = @managerEmployeeId
                  AND e.status != 'deleted';
";
            var parameters = new
            {
                managerEmployeeId,
                organizationId
            };
            var teamEmployeeIds = await QueryAsync<Guid>(sql, parameters);
            return teamEmployeeIds.ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error getting team employee IDs for manager {managerEmployeeId} in organization {organizationId}", ex);
        }
    }
}
