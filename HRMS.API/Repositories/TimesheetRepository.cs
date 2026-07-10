using System.Data;
using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class TimesheetRepository : BaseRepository, ITimesheetRepository
{
    public TimesheetRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<TimesheetConfigDto?> GetConfigAsync(Guid orgId)
    {
        var result = await QueryAsync<TimesheetConfigDto>(@"
            SELECT config_id AS configid, organization_id AS organizationid,
                   requires_manager_approval AS requiresmanagerapproval,
                   requires_hr_approval AS requireshrapproval,
                   allow_employee_corrections AS allowemployeecorrections,
                   correction_deadline_days AS correctiondeadlinedays,
                   working_days AS workingdays,
                   half_day_threshold_hours AS halfdaythresholdhours,
                   full_day_threshold_hours AS fulldaythresholdhours,
                   late_grace_minutes AS lategraceminutes,
                   auto_finalize_on_lock AS autofinalizeonlock,
                   overtime_multiplier_regular AS overtimemultiplierregular,
                   overtime_multiplier_weekend AS overtimemultiplierweekend,
                   overtime_multiplier_holiday AS overtimemultiplierholiday,
                   timezone AS timezone,
                   shift_start_hour AS shiftstarthour,
                   shift_start_minute AS shiftstartminute
            FROM attendance.timesheet_config WHERE organization_id = @orgId",
            new { orgId });
        return result.ToList().FirstOrDefault();
    }

    public async Task<TimesheetConfigDto> UpsertConfigAsync(Guid orgId, TimesheetConfigDto dto)
    {
        await ExecuteAsync(@"
            INSERT INTO attendance.timesheet_config (
                organization_id, requires_manager_approval, requires_hr_approval,
                allow_employee_corrections, correction_deadline_days, working_days,
                half_day_threshold_hours, full_day_threshold_hours, late_grace_minutes,
                auto_finalize_on_lock, overtime_multiplier_regular, overtime_multiplier_weekend,
                overtime_multiplier_holiday, timezone, shift_start_hour, shift_start_minute)
            VALUES (@orgId, @rm, @rh, @ae, @cd, @wd::text[],
                    @ht, @ft, @lg, @af, @or, @ow, @oh, @tz, @sh, @sm)
            ON CONFLICT (organization_id) DO UPDATE SET
                requires_manager_approval = EXCLUDED.requires_manager_approval,
                requires_hr_approval = EXCLUDED.requires_hr_approval,
                allow_employee_corrections = EXCLUDED.allow_employee_corrections,
                correction_deadline_days = EXCLUDED.correction_deadline_days,
                working_days = EXCLUDED.working_days,
                half_day_threshold_hours = EXCLUDED.half_day_threshold_hours,
                full_day_threshold_hours = EXCLUDED.full_day_threshold_hours,
                late_grace_minutes = EXCLUDED.late_grace_minutes,
                auto_finalize_on_lock = EXCLUDED.auto_finalize_on_lock,
                overtime_multiplier_regular = EXCLUDED.overtime_multiplier_regular,
                overtime_multiplier_weekend = EXCLUDED.overtime_multiplier_weekend,
                overtime_multiplier_holiday = EXCLUDED.overtime_multiplier_holiday,
                timezone = EXCLUDED.timezone,
                shift_start_hour = EXCLUDED.shift_start_hour,
                shift_start_minute = EXCLUDED.shift_start_minute",
            new { orgId, rm = dto.RequiresManagerApproval, rh = dto.RequiresHrApproval,
                  ae = dto.AllowEmployeeCorrections, cd = dto.CorrectionDeadlineDays,
                  wd = dto.WorkingDays ?? new string[0],
                  ht = dto.HalfDayThresholdHours, ft = dto.FullDayThresholdHours,
                  lg = dto.LateGraceMinutes, af = dto.AutoFinalizeOnLock,
                  or = dto.OvertimeMultiplierRegular, ow = dto.OvertimeMultiplierWeekend,
                  oh = dto.OvertimeMultiplierHoliday, tz = dto.Timezone,
                  sh = dto.ShiftStartHour, sm = dto.ShiftStartMinute });
        return await GetConfigAsync(orgId) ?? dto;
    }

    public async Task<IEnumerable<TimesheetPeriodDto>> GetPeriodsAsync(Guid orgId, DateTime? from, DateTime? to)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@orgId", orgId);
        parameters.Add("@from", from, DbType.DateTime);
        parameters.Add("@to", to, DbType.DateTime);
        return await QueryAsync<TimesheetPeriodDto>(@"
            SELECT mt.timesheet_id AS timesheetid, mt.timesheet_name AS timesheetname,
                   mt.period_start AS periodstart, mt.period_end AS periodend,
                   mt.status AS status,
                   CASE
                     WHEN mt.scope_type = 'all' THEN
                       COALESCE((SELECT COUNT(DISTINCT a.employeeid)
                                 FROM attendance.attendance a
                                 JOIN employee.employees e ON e.employeeid = a.employeeid
                                 WHERE e.organizationid = mt.organization_id
                                   AND a.workdate BETWEEN mt.period_start AND mt.period_end), 0)
                     ELSE
                       (SELECT COUNT(DISTINCT tpa.employee_id)
                        FROM attendance.timesheet_period_assignments tpa
                        WHERE tpa.timesheet_id = mt.timesheet_id)
                   END AS totalemployees,
                   mt.created_at AS createdat, mt.submitted_at AS submittedat,
                   mt.finalized_at AS finalizedat, mt.locked_at AS lockedat,
                   mt.scope_type AS scopetype, mt.scope_label AS scopelabel,
                   mt.rejection_reason AS rejectionreason
            FROM attendance.monthly_timesheets mt
            WHERE mt.organization_id = @orgId
              AND (@from IS NULL OR mt.period_end >= @from)
              AND (@to IS NULL OR mt.period_start <= @to)
            ORDER BY mt.period_start DESC", parameters);
    }

    public async Task<TimesheetPeriodDto?> GetPeriodAsync(Guid timesheetId)
    {
        var result = await QueryAsync<TimesheetPeriodDto>(@"
            SELECT mt.timesheet_id AS timesheetid, mt.timesheet_name AS timesheetname,
                   mt.period_start AS periodstart, mt.period_end AS periodend,
                   mt.status AS status,
                   CASE
                     WHEN mt.scope_type = 'all' THEN
                       COALESCE((SELECT COUNT(DISTINCT a.employeeid)
                                 FROM attendance.attendance a
                                 JOIN employee.employees e ON e.employeeid = a.employeeid
                                 WHERE e.organizationid = mt.organization_id
                                   AND a.workdate BETWEEN mt.period_start AND mt.period_end), 0)
                     ELSE
                       (SELECT COUNT(DISTINCT tpa.employee_id)
                        FROM attendance.timesheet_period_assignments tpa
                        WHERE tpa.timesheet_id = mt.timesheet_id)
                   END AS totalemployees,
                   mt.created_at AS createdat, mt.submitted_at AS submittedat,
                   mt.finalized_at AS finalizedat, mt.locked_at AS lockedat,
                   mt.scope_type AS scopetype, mt.scope_label AS scopelabel,
                   mt.rejection_reason AS rejectionreason
            FROM attendance.monthly_timesheets mt WHERE mt.timesheet_id = @timesheetId",
            new { timesheetId });
        return result.ToList().FirstOrDefault();
    }

    public async Task<Guid> CreatePeriodAsync(Guid orgId, CreateTimesheetPeriodDto dto, Guid createdBy)
    {
        var id = Guid.NewGuid();
        var scopeType = string.IsNullOrWhiteSpace(dto.ScopeType) ? "all" : dto.ScopeType;
        var scopeLabel = await ComputeScopeLabelAsync(orgId, dto);

        await ExecuteAsync(@"
            INSERT INTO attendance.monthly_timesheets
                (timesheet_id, timesheet_name, period_start, period_end, status,
                 organization_id, generated_by, scope_type, scope_label)
            VALUES (@id, @name, @start, @end, 'Draft', @orgId, @createdBy, @scopeType, @scopeLabel)",
            new { id, name = dto.PeriodName, start = dto.PeriodStart, end = dto.PeriodEnd,
                  orgId, createdBy, scopeType, scopeLabel });

        // Insert assignment rows for department/employees scope
        if (scopeType == "department" && dto.DepartmentIds?.Any() == true)
        {
            await ExecuteAsync(@"
                INSERT INTO attendance.timesheet_period_assignments (timesheet_id, employee_id)
                SELECT @tsId, e.employeeid
                FROM employee.employees e
                JOIN employee.employmentdetails ed ON ed.employeeid = e.employeeid AND ed.iscurrent = true
                WHERE e.organizationid = @orgId
                  AND e.status = 'active'
                  AND ed.departmentid = ANY(@deptIds)
                ON CONFLICT (timesheet_id, employee_id) DO NOTHING",
                new { tsId = id, orgId, deptIds = dto.DepartmentIds });
        }
        else if (scopeType == "employees" && dto.EmployeeIds?.Any() == true)
        {
            foreach (var empId in dto.EmployeeIds)
            {
                await ExecuteAsync(@"
                    INSERT INTO attendance.timesheet_period_assignments (timesheet_id, employee_id)
                    VALUES (@tsId, @empId)
                    ON CONFLICT (timesheet_id, employee_id) DO NOTHING",
                    new { tsId = id, empId });
            }
        }

        return id;
    }

    private async Task<string?> ComputeScopeLabelAsync(Guid orgId, CreateTimesheetPeriodDto dto)
    {
        var scopeType = string.IsNullOrWhiteSpace(dto.ScopeType) ? "all" : dto.ScopeType;
        if (scopeType == "all") return null;

        if (scopeType == "department" && dto.DepartmentIds?.Any() == true)
        {
            var names = await QueryAsync<string>(
                "SELECT departmentname FROM employee.departments WHERE departmentid = ANY(@ids)",
                new { ids = dto.DepartmentIds });
            return string.Join(", ", names);
        }

        if (scopeType == "employees" && dto.EmployeeIds?.Any() == true)
        {
            var names = await QueryAsync<string>(
                "SELECT firstname || ' ' || lastname FROM employee.employees WHERE employeeid = ANY(@ids)",
                new { ids = dto.EmployeeIds });
            return string.Join(", ", names);
        }

        return null;
    }

    public async Task<bool> TransitionStatusAsync(Guid timesheetId, string newStatus, Guid actorId, string? reason)
    {
        var result = await ExecuteScalarAsync<bool>(
            "SELECT attendance.fn_timesheet_transition(@tsId, @newStatus, @actorId, @reason)",
            new { tsId = timesheetId, newStatus, actorId, reason = (object?)reason ?? DBNull.Value });
        return result;
    }

    public async Task<int> CountPendingCorrectionsAsync(Guid timesheetId)
    {
        return await ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM attendance.attendance_update_requests WHERE timesheet_id = @tsId AND status = 'pending'",
            new { tsId = timesheetId });
    }

    public async Task<IEnumerable<EmployeeTimesheetDto>> GetEmployeeDaysAsync(Guid timesheetId, Guid orgId, Guid? employeeId)
    {
        // Fetch scope_type to decide whether to filter by assignments
        var scopeType = await QuerySingleOrDefaultAsync<string>(
            "SELECT scope_type FROM attendance.monthly_timesheets WHERE timesheet_id = @tsId",
            new { tsId = timesheetId }) ?? "all";

        // Build employee × date matrix so employees with NO attendance still appear
        var rawData = await QueryAsync<dynamic>(@"
        WITH scoped_employees AS (
            SELECT e.employeeid, e.firstname || ' ' || e.lastname AS employeename, e.employeecode
            FROM employee.employees e
            WHERE e.organizationid = @orgId
              AND e.status <> 'deleted'
              AND (
                  @scopeType = 'all'
                  OR e.employeeid IN (
                      SELECT tpa.employee_id
                      FROM attendance.timesheet_period_assignments tpa
                      WHERE tpa.timesheet_id = @timesheetId
                  )
              )
        ),
        period_dates AS (
            SELECT gs::date AS workdate
            FROM attendance.monthly_timesheets mt,
                 generate_series(mt.period_start, mt.period_end, '1 day') gs
            WHERE mt.timesheet_id = @timesheetId
        )
        SELECT
            se.employeeid,
            se.employeename,
            se.employeecode,
            a.attendanceid,
            ed.workdate AS date,
            a.checkintime,
            a.checkouttime,
            COALESCE(a.status, 'no_record') AS status,
            COALESCE(a.totalhours, 0) AS totalhours,
            COALESCE(a.overtimehours, 0) AS overtimehours,
            a.overtimetype,
            COALESCE(a.late_minutes, 0) AS lateminutes,
            EXISTS (
                SELECT 1 FROM attendance.finalized_timesheet_records f
                WHERE (f.attendance_id = a.attendanceid
                       OR (f.employee_id = se.employeeid AND f.work_date = ed.workdate AND f.attendance_id IS NULL))
                  AND f.record_status IN ('finalized','overridden')
            ) AS isfinalized,
            EXISTS (
                SELECT 1 FROM attendance.finalized_timesheet_records f_ov
                WHERE (f_ov.attendance_id = a.attendanceid
                       OR (f_ov.employee_id = se.employeeid AND f_ov.work_date = ed.workdate AND f_ov.attendance_id IS NULL))
                  AND f_ov.record_status = 'overridden'
            ) AS ismanageroverride,
            EXISTS (
                SELECT 1 FROM attendance.attendance_update_requests rp
                WHERE rp.attendance_id = a.attendanceid AND rp.status = 'pending'
            ) AS haspendingrequest,
            EXISTS (
                SELECT 1 FROM attendance.attendance_update_requests rd
                WHERE rd.attendance_id = a.attendanceid AND rd.status = 'draft'
            ) AS hasdraftrequest,
            EXISTS (
                SELECT 1 FROM attendance.attendance_update_requests ra
                WHERE ra.attendance_id = a.attendanceid AND ra.status = 'approved'
            ) AS hasapprovedrequest,
            (SELECT rl.requested_checkin FROM attendance.attendance_update_requests rl
             WHERE rl.attendance_id = a.attendanceid
               AND rl.status IN ('pending','draft','approved')
             ORDER BY rl.submitted_at DESC LIMIT 1) AS requestedcheckin,
            (SELECT rl.requested_checkout FROM attendance.attendance_update_requests rl
             WHERE rl.attendance_id = a.attendanceid
               AND rl.status IN ('pending','draft','approved')
             ORDER BY rl.submitted_at DESC LIMIT 1) AS requestedcheckout,
            (SELECT rl.requested_status FROM attendance.attendance_update_requests rl
             WHERE rl.attendance_id = a.attendanceid
               AND rl.status IN ('pending','draft','approved')
             ORDER BY rl.submitted_at DESC LIMIT 1) AS requestedstatus,
            rej.rejection_reason AS rejectionreason
        FROM scoped_employees se
        CROSS JOIN period_dates ed
        LEFT JOIN attendance.attendance a ON a.employeeid = se.employeeid AND a.workdate = ed.workdate
        LEFT JOIN LATERAL (
            SELECT r.rejection_reason
            FROM attendance.attendance_update_requests r
            WHERE r.employee_id = se.employeeid
              AND r.workdate = ed.workdate
              AND r.status = 'rejected'
              AND r.rejection_reason IS NOT NULL
            ORDER BY r.submitted_at DESC
            LIMIT 1
        ) rej ON TRUE
        WHERE (@employeeId IS NULL OR se.employeeid = @employeeId)
        ORDER BY se.employeename, ed.workdate",
            new { timesheetId, orgId, employeeId, scopeType });

        var grouped = rawData.GroupBy(r => (Guid)r.employeeid)
            .Select(g => new EmployeeTimesheetDto
            {
                EmployeeId = g.Key,
                EmployeeName = (string)g.First().employeename,
                EmployeeCode = (string)g.First().employeecode,
                TotalHoursWorked = g.Sum(x => (decimal)(x.totalhours ?? 0m)),
                OvertimeHours = g.Sum(x => (decimal)(x.overtimehours ?? 0m)),
                DailyRecords = g.Select(x => new DailyAttendanceRecord
                {
                    EmployeeId = g.Key,
                    AttendanceId = (Guid?)x.attendanceid,
                    Date = (DateTimeOffset)x.date,
                    CheckInTime = (DateTimeOffset?)x.checkintime,
                    CheckOutTime = (DateTimeOffset?)x.checkouttime,
                    Status = (string)x.status ?? "no_record",
                    TotalHours = (decimal)(x.totalhours ?? 0m),
                    OvertimeHours = (decimal)(x.overtimehours ?? 0m),
                    OvertimeType = (string)x.overtimetype,
                    LateMinutes = (int)(x.lateminutes ?? 0),
                    IsFinalized = (bool)(x.isfinalized ?? false),
                    IsManagerOverride = (bool)(x.ismanageroverride ?? false),
                    HasPendingRequest = (bool)(x.haspendingrequest ?? false),
                    HasDraftRequest = (bool)(x.hasdraftrequest ?? false),
                    HasApprovedRequest = (bool)(x.hasapprovedrequest ?? false),
                    RequestedCheckIn = (DateTime?)x.requestedcheckin,
                    RequestedCheckOut = (DateTime?)x.requestedcheckout,
                    RequestedStatus = (string)x.requestedstatus,
                    RejectionReason = (string?)x.rejectionreason
                }).ToList()
            });

        return grouped;
    }

    public async Task<IEnumerable<PendingCorrectionDto>> GetPendingCorrectionsAsync(Guid timesheetId, Guid orgId)
    {
        return await QueryAsync<PendingCorrectionDto>(@"
        SELECT r.request_id AS requestid, r.attendance_id AS attendanceid,
               r.employee_id AS employeeid, e.firstname || ' ' || e.lastname AS employeename,
               r.timesheet_id AS timesheetid, r.workdate AS workdate,
               -- Original attendance values
               a.status AS originalstatus,
               a.checkintime AS originalcheckin,
               a.checkouttime AS originalcheckout,
               a.overtimehours AS originalovertimehours,
               a.overtimetype AS originalovertimetype,
               a.late_minutes AS originallateminutes,
               lt_req.leavetypeid AS requestedleavetypeid,
               lt_req.typename AS requestedleavetypename,
               -- Requested corrections
               r.requested_status AS requestedstatus,
               r.requested_checkin AS requestedcheckin,
               r.requested_checkout AS requestedcheckout,
               r.requested_overtime_hours AS requestedovertimehours,
               r.requested_overtime_type AS requestedovertimetype,
               r.requested_late_minutes AS requestedlateminutes,
               r.requested_notes AS requestednotes,
               r.reason_for_edit AS reasonforedit,
               r.status AS status, r.submitted_at AS submittedat,
               r.rejection_reason AS rejectionreason
        FROM attendance.attendance_update_requests r
        JOIN employee.employees e ON r.employee_id = e.employeeid
        LEFT JOIN attendance.attendance a ON a.attendanceid = r.attendance_id
        LEFT JOIN attendance.leavetypes lt_req ON lt_req.leavetypeid = r.requested_leave_type_id
        WHERE r.timesheet_id = @timesheetId AND r.status = 'pending'
          AND e.organizationid = @orgId
        ORDER BY e.firstname, e.lastname, r.workdate",
            new { timesheetId, orgId });
    }

    public async Task<Guid?> CreateCorrectionAsync(Guid timesheetId, Guid employeeId, Guid orgId, TimesheetCorrectionDto dto)
    {
        // Determine the initial status: use what the caller sent, default to 'pending'.
        // The FE sends "draft" for Save Draft and "submitted" for Submit.
        // Normalize to lowercase to match DB convention (view, PL/pgSQL functions all use lowercase).
        var initialStatus = (dto.Status ?? string.Empty).ToLowerInvariant() switch
        {
            "submitted" => "pending",
            "draft" => "draft",
            "" => "pending",
            _ => (dto.Status ?? "pending").ToLowerInvariant()
        };

        // Upsert pattern: check for an existing draft/pending for the same employee+date.
        // The partial unique index uq_active_request_per_date enforces one active request per (employee, date).
        var newId = Guid.NewGuid();

        // Step 1: Try to find existing active request for this employee+date
        var existingId = await QuerySingleOrDefaultAsync<Guid?>(@"
            SELECT request_id FROM attendance.attendance_update_requests
            WHERE employee_id = @empId AND workdate = @wd
              AND status IN ('draft', 'pending')",
            new { empId = employeeId, wd = dto.WorkDate });

        if (existingId.HasValue)
        {
            // Step 2a: Update existing request
            await ExecuteAsync(@"
                UPDATE attendance.attendance_update_requests SET
                    attendance_id = COALESCE(@attId, attendance_id),
                    requested_status = @rs,
                    requested_checkin = @rci,
                    requested_checkout = @rco,
                    requested_overtime_hours = @roh,
                    requested_overtime_type = @rot,
                    requested_late_minutes = @rlm,
                    requested_leave_type_id = @rlt,
                    requested_notes = @rn,
                    reason_for_edit = @rea,
                    status = @status,
                    idempotency_key = @key,
                    submitted_at = CASE WHEN @status = 'pending' THEN NOW() ELSE submitted_at END
                WHERE request_id = @existingId",
                new { existingId = existingId.Value,
                      attId = (object?)dto.AttendanceId ?? DBNull.Value,
                      rs = (object?)dto.RequestedStatus ?? DBNull.Value,
                      rci = (object?)dto.RequestedCheckIn ?? DBNull.Value,
                      rco = (object?)dto.RequestedCheckOut ?? DBNull.Value,
                      roh = (object?)dto.RequestedOvertimeHours ?? DBNull.Value,
                      rot = (object?)dto.RequestedOvertimeType ?? DBNull.Value,
                      rlm = (object?)dto.RequestedLateMinutes ?? DBNull.Value,
                      rlt = (object?)dto.RequestedLeaveTypeId ?? DBNull.Value,
                      rn = (object?)dto.RequestedNotes ?? DBNull.Value,
                      rea = dto.ReasonForEdit, status = initialStatus,
                      key = (object?)dto.IdempotencyKey ?? DBNull.Value });
            return existingId.Value;
        }

        // Step 2b: Insert new request
        await ExecuteAsync(@"
            INSERT INTO attendance.attendance_update_requests
                (request_id, attendance_id, timesheet_id, employee_id, organization_id, workdate,
                 requested_status, requested_checkin, requested_checkout,
                 requested_overtime_hours, requested_overtime_type, requested_late_minutes,
                 requested_leave_type_id, requested_notes, reason_for_edit, status, idempotency_key)
            VALUES (@id, @attId, @tsId, @empId, @orgId, @wd,
                    @rs, @rci, @rco,
                    @roh, @rot, @rlm,
                    @rlt, @rn, @rea, @status, @key)",
            new { id = newId, attId = (object?)dto.AttendanceId ?? DBNull.Value, tsId = timesheetId,
                  empId = employeeId, orgId = orgId,
                  wd = dto.WorkDate, rs = (object?)dto.RequestedStatus ?? DBNull.Value,
                  rci = (object?)dto.RequestedCheckIn ?? DBNull.Value, rco = (object?)dto.RequestedCheckOut ?? DBNull.Value,
                  roh = (object?)dto.RequestedOvertimeHours ?? DBNull.Value,
                  rot = (object?)dto.RequestedOvertimeType ?? DBNull.Value,
                  rlm = (object?)dto.RequestedLateMinutes ?? DBNull.Value,
                  rlt = (object?)dto.RequestedLeaveTypeId ?? DBNull.Value,
                  rn = (object?)dto.RequestedNotes ?? DBNull.Value,
                  rea = dto.ReasonForEdit, status = initialStatus,
                  key = (object?)dto.IdempotencyKey ?? DBNull.Value });
        return newId;
    }

    public async Task<TimesheetCorrectionDto?> GetCorrectionAsync(Guid correctionId)
    {
        var result = await QueryAsync<TimesheetCorrectionDto>(@"
            SELECT request_id AS requestid, attendance_id AS attendanceid,
                   employee_id AS employeeid,
                   timesheet_id AS timesheetid, workdate AS workdate,
                   requested_status AS requestedstatus, requested_checkin AS requestedcheckin,
                   requested_checkout AS requestedcheckout,
                   requested_overtime_hours AS requestedovertimehours,
                   requested_overtime_type AS requestedovertimetype,
                   requested_late_minutes AS requestedlateminutes,
                   requested_leave_type_id AS requestedleavetypeid,
                   requested_notes AS requestednotes, reason_for_edit AS reasonforedit, status
            FROM attendance.attendance_update_requests WHERE request_id = @correctionId",
            new { correctionId });
        return result.ToList().FirstOrDefault();
    }

    public async Task<CorrectionActionResult> ApproveCorrectionAsync(Guid correctionId, Guid managerId)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            // Acquire row-level lock and read current state in one shot
            var row = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT status, actioned_by, actioned_at, timesheet_id
                FROM attendance.attendance_update_requests
                WHERE request_id = @id FOR UPDATE",
                new { id = correctionId }, transaction: tx);

            if (row == null)
            {
                tx.Commit();
                return new CorrectionActionResult { Success = false, Message = "Correction not found." };
            }

            var currentStatus = (string)row.status;
            var timesheetId = (Guid?)row.timesheet_id;

            // ALREADY_ACTIONED check — another actor moved this row while we were waiting
            if (currentStatus != "pending")
            {
                string actionedByIdStr = ((string?)row.actioned_by) ?? "";
                DateTime actionedTime = row.actioned_at is DateTime atDt ? atDt : default;
                string actorName = "another approver";
                if (actionedByIdStr.Length > 0 && Guid.TryParse(actionedByIdStr, out Guid parsedActor))
                    actorName = await ResolveActorNameAsync(parsedActor, conn);

                string timeStr = actionedTime != default ? actionedTime.ToLocalTime().ToString("HH:mm") : "";

                tx.Commit();
                return new CorrectionActionResult
                {
                    Success = false,
                    ErrorCode = TimesheetErrorCodes.AlreadyActioned,
                    Message = $"This correction was already {currentStatus.ToLower()} by {actorName}" +
                              (timeStr != "" ? $" at {timeStr}" : "") + ".",
                    CurrentStatus = currentStatus,
                    ActionedBy = actorName,
                    ActionedAt = actionedTime != default ? actionedTime.ToString("o") : ""
                };
            }

            // Step 1: Apply the correction to attendance.attendance via the PL/pgSQL function
            // This copies requested_checkin, requested_checkout, requested_status,
            // requested_overtime_hours, requested_overtime_type, requested_late_minutes,
            // requested_leave_type_id onto the attendance row.
            await conn.ExecuteAsync(
                "SELECT attendance.fn_finalize_attendance_record(@requestId, @actionedBy, TRUE)",
                new { requestId = correctionId, actionedBy = managerId },
                transaction: tx, commandType: System.Data.CommandType.Text);

            // Step 2: Write audit log
            if (timesheetId.HasValue)
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO attendance.timesheet_audit_log
                        (timesheet_id, entity_type, entity_id, action, actor_id, actor_role, old_values, new_values, reason)
                    VALUES (@tsId, @type, @eid, 'CorrectionApproved', @actor, 'Manager', @old::jsonb, @new::jsonb, NULL)",
                    new { tsId = timesheetId.Value, type = "correction", eid = correctionId,
                          actor = managerId, @old = "{\"status\":\"pending\"}", @new = "{\"status\":\"approved\"}" },
                    transaction: tx);
            }

            tx.Commit();
            return new CorrectionActionResult { Success = true, Message = "Correction approved." };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<CorrectionActionResult> RejectCorrectionAsync(Guid correctionId, Guid managerId, string reason)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            // Acquire row-level lock and read current state
            var row = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT status, actioned_by, actioned_at, timesheet_id
                FROM attendance.attendance_update_requests
                WHERE request_id = @id FOR UPDATE",
                new { id = correctionId }, transaction: tx);

            if (row == null)
            {
                tx.Commit();
                return new CorrectionActionResult { Success = false, Message = "Correction not found." };
            }

            var currentStatus = (string)row.status;
            var timesheetId = (Guid?)row.timesheet_id;

            // ALREADY_ACTIONED check
            if (currentStatus != "pending")
            {
                string actionedByIdStr2 = ((string?)row.actioned_by) ?? "";
                DateTime actionedTime2 = row.actioned_at is DateTime actDt2 ? actDt2 : default;
                string actorName = "another approver";
                if (actionedByIdStr2.Length > 0 && Guid.TryParse(actionedByIdStr2, out Guid parsedActor2))
                    actorName = await ResolveActorNameAsync(parsedActor2, conn);

                string timeStr = actionedTime2 != default ? actionedTime2.ToLocalTime().ToString("HH:mm") : "";

                tx.Commit();
                return new CorrectionActionResult
                {
                    Success = false,
                    ErrorCode = TimesheetErrorCodes.AlreadyActioned,
                    Message = $"This correction was already {currentStatus.ToLower()} by {actorName}" +
                              (timeStr != "" ? $" at {timeStr}" : "") + ".",
                    CurrentStatus = currentStatus,
                    ActionedBy = actorName,
                    ActionedAt = actionedTime2 != default ? actionedTime2.ToString("o") : ""
                };
            }

            // Step 1: Reject via the PL/pgSQL function — sets status='rejected',
            // actioned_by, actioned_at, and rejection_reason in one atomic call.
            await conn.ExecuteAsync(
                "SELECT attendance.fn_finalize_attendance_record(@requestId, @actionedBy, FALSE, @rejectionReason)",
                new { requestId = correctionId, actionedBy = managerId, rejectionReason = reason },
                transaction: tx, commandType: System.Data.CommandType.Text);

            // Step 2: Write audit log
            if (timesheetId.HasValue)
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO attendance.timesheet_audit_log
                        (timesheet_id, entity_type, entity_id, action, actor_id, actor_role, old_values, new_values, reason)
                    VALUES (@tsId, @type, @eid, 'CorrectionRejected', @actor, 'Manager', @old::jsonb, @new::jsonb, @reason)",
                    new { tsId = timesheetId.Value, type = "correction", eid = correctionId,
                          actor = managerId, @old = "{\"status\":\"pending\"}", @new = "{\"status\":\"rejected\"}", reason },
                    transaction: tx);
            }

            tx.Commit();
            return new CorrectionActionResult { Success = true, Message = "Correction rejected." };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<BulkApproveResult> ApproveBulkCorrectionsAsync(IEnumerable<Guid> correctionIds, Guid managerId)
    {
        var correctionList = correctionIds.ToList();
        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            // Pre-fetch correction details for error responses and audit logging
            var rawDetails = (await conn.QueryAsync<dynamic>(@"
                SELECT r.request_id, r.employee_id, r.workdate, r.timesheet_id,
                       e.firstname || ' ' || e.lastname AS employee_name
                FROM attendance.attendance_update_requests r
                JOIN employee.employees e ON e.employeeid = r.employee_id
                WHERE r.request_id = ANY(@ids)",
                new { ids = correctionList.ToArray() }, transaction: tx)).ToList();
            var details = new Dictionary<Guid, dynamic>();
            foreach (var d in rawDetails)
                details[(Guid)d.request_id] = d;

            var timesheetAuditMap = new Dictionary<Guid, Guid>();

            foreach (var cid in correctionList)
            {
                if (!details.TryGetValue(cid, out var detail))
                {
                    tx.Rollback();
                    return new BulkApproveResult
                    {
                        Success = false,
                        Message = "Bulk approval failed. No changes were applied.",
                        FailedItem = new FailedCorrectionItem
                        {
                            CorrectionId = cid,
                            EmployeeName = "Unknown",
                            WorkDate = DateOnly.MinValue,
                            Reason = "Correction record not found."
                        }
                    };
                }

                // Acquire row-level lock on this correction
                var row = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT status FROM attendance.attendance_update_requests
                    WHERE request_id = @id FOR UPDATE",
                    new { id = cid }, transaction: tx);

                if (row == null)
                {
                    tx.Rollback();
                    return new BulkApproveResult
                    {
                        Success = false,
                        Message = "Bulk approval failed. No changes were applied.",
                        FailedItem = new FailedCorrectionItem
                        {
                            CorrectionId = cid,
                            EmployeeName = (string)detail.employee_name,
                            WorkDate = DateOnly.FromDateTime((DateTime)detail.workdate),
                            Reason = "Correction record not found."
                        }
                    };
                }

                if ((string)row.status != "pending")
                {
                    tx.Rollback();
                    return new BulkApproveResult
                    {
                        Success = false,
                        Message = "Bulk approval failed. No changes were applied.",
                        FailedItem = new FailedCorrectionItem
                        {
                            CorrectionId = cid,
                            EmployeeName = (string)detail.employee_name,
                            WorkDate = DateOnly.FromDateTime((DateTime)detail.workdate),
                            Reason = $"This correction was already {((string)row.status).ToLower()} by another approver."
                        }
                    };
                }

                // Apply the correction to attendance.attendance via PL/pgSQL function
                await conn.ExecuteAsync(
                    "SELECT attendance.fn_finalize_attendance_record(@requestId, @actionedBy, TRUE)",
                    new { requestId = cid, actionedBy = managerId },
                    transaction: tx, commandType: System.Data.CommandType.Text);

                timesheetAuditMap[cid] = (Guid)detail.timesheet_id;
            }

            // Write audit entries for each approved correction
            foreach (var cid in correctionList.Where(c => timesheetAuditMap.ContainsKey(c)))
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO attendance.timesheet_audit_log
                        (timesheet_id, entity_type, entity_id, action, actor_id, actor_role, old_values, new_values, reason)
                    VALUES (@tsId, @type, @eid, 'CorrectionApproved', @actor, 'Manager', @old::jsonb, @new::jsonb, NULL)",
                    new { tsId = timesheetAuditMap[cid], type = "correction", eid = cid,
                          actor = managerId, @old = "{\"status\":\"pending\"}", @new = "{\"status\":\"approved\"}" },
                    transaction: tx);
            }

            tx.Commit();
            return new BulkApproveResult { Success = true, Message = $"{timesheetAuditMap.Count} correction(s) approved." };
        }
        catch (Exception ex)
        {
            tx.Rollback();
            // Return structured failure with context about the last item being processed
            return new BulkApproveResult
            {
                Success = false,
                Message = "Bulk approval failed. No changes were applied.",
                FailedItem = new FailedCorrectionItem
                {
                    CorrectionId = correctionList.Count > 0 ? correctionList[^1] : Guid.Empty,
                    EmployeeName = "Unknown",
                    WorkDate = DateOnly.MinValue,
                    Reason = ex.Message
                }
            };
        }
    }

    private async Task<string> ResolveActorNameAsync(Guid actorId, IDbConnection conn)
    {
        return await conn.QueryFirstOrDefaultAsync<string>(
            @"SELECT firstname || ' ' || lastname FROM employee.employees WHERE employeeid = @actorId",
            new { actorId }) ?? "another approver";
    }

    public async Task<bool> ApplyOverrideAsync(Guid timesheetId, Guid managerId, Guid orgId, TimesheetOverrideDto dto)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var id = dto.AttendanceId ?? Guid.NewGuid();

            // Calculate total hours from check-in/check-out when both are provided
            // Never default to a hardcoded value — leave at 0 if times are missing
            decimal hrs = 0M;
            if (dto.CheckInTime.HasValue && dto.CheckOutTime.HasValue)
            {
                var diff = dto.CheckOutTime.Value - dto.CheckInTime.Value;
                if (diff.TotalHours > 0)
                    hrs = Math.Round((decimal)diff.TotalHours, 2);
            }

            await conn.ExecuteAsync(@"
                INSERT INTO attendance.attendance
                    (attendanceid, employeeid, workdate, status, checkintime, checkouttime,
                     totalhours, overtimehours, overtimetype, late_minutes, leave_type_id, notes)
                VALUES (@id, @empId, @wd, @status, @ci::timestamptz, @co::timestamptz,
                        @hrs, @ot, @otType, @lm, @lt, @notes)
                ON CONFLICT (employeeid, workdate) DO UPDATE SET
                    status = EXCLUDED.status, checkintime = EXCLUDED.checkintime,
                    checkouttime = EXCLUDED.checkouttime, totalhours = EXCLUDED.totalhours,
                    overtimehours = EXCLUDED.overtimehours, overtimetype = EXCLUDED.overtimetype,
                    late_minutes = EXCLUDED.late_minutes, leave_type_id = EXCLUDED.leave_type_id,
                    notes = EXCLUDED.notes",
                new { id, empId = dto.EmployeeId, wd = dto.WorkDate, status = dto.Status,
                      ci = (object?)dto.CheckInTime ?? DBNull.Value, co = (object?)dto.CheckOutTime ?? DBNull.Value,
                      hrs, ot = dto.OvertimeHours ?? 0M,
                      otType = (object?)dto.OvertimeType ?? DBNull.Value,
                      lm = dto.LateMinutes ?? 0, lt = (object?)dto.LeaveTypeId ?? DBNull.Value,
                      notes = (object?)dto.Notes ?? DBNull.Value },
                transaction: tx);

            await conn.ExecuteAsync(
                "INSERT INTO attendance.timesheet_audit_log (timesheet_id, entity_type, entity_id, action, actor_id, actor_role, old_values, new_values, reason) VALUES (@tsId, @type, @eid, 'ManagerOverride', @actor, 'Manager', NULL, @new::jsonb, @reason)",
                new { tsId = timesheetId, type = "Attendance", eid = id, actor = managerId,
                      @new = System.Text.Json.JsonSerializer.Serialize(dto), reason = dto.Reason },
                transaction: tx);

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<int> FinalizeBatchAsync(Guid timesheetId, Guid orgId, Guid actorId, Guid? employeeId)
    {
        return await ExecuteScalarAsync<int>(
            "SELECT attendance.fn_batch_finalize_timesheet(@tsId, @actorId, @empId)",
            new { tsId = timesheetId, actorId, empId = (object?)employeeId ?? DBNull.Value });
    }

    public async Task<IEnumerable<TimesheetPeriodLinkDto>> GetFinalizedTimesheetLinksAsync(Guid orgId)
    {
        return await QueryAsync<TimesheetPeriodLinkDto>(
            "SELECT * FROM attendance.fn_get_finalized_timesheet_links(@orgId)",
            new { orgId });
    }

    public async Task<bool> LockForPayrollAsync(Guid timesheetId, Guid lockedBy, Guid? payrollPeriodId)
    {
        return await ExecuteScalarAsync<bool>(
            "SELECT attendance.fn_lock_timesheet(@tsId, @lockedBy, @ppId)",
            new { tsId = timesheetId, lockedBy, ppId = (object?)payrollPeriodId ?? DBNull.Value });
    }

    public async Task<TimesheetPayrollSummaryDto?> GetPayrollSummaryAsync(Guid timesheetId)
    {
        var headerResult = await QueryAsync<TimesheetPayrollSummaryDto>(@"
            SELECT timesheet_id AS timesheetid, timesheet_name AS timesheetname,
                   period_start AS periodstart, period_end AS periodend,
                   status AS status, finalized_at AS finalizedat, locked_at AS lockedat
            FROM attendance.monthly_timesheets WHERE timesheet_id = @id",
            new { id = timesheetId });
        var headerItem = headerResult.ToList().FirstOrDefault();
        if (headerItem == null) return null;
        headerItem.Employees = (await QueryAsync<EmployeePayrollSummaryDto>(
            "SELECT * FROM attendance.fn_get_timesheet_payroll_summary(@id)",
            new { id = timesheetId })).ToList();
        return headerItem;
    }

    public async Task<IEnumerable<TimesheetAuditEntryDto>> GetAuditLogAsync(Guid timesheetId)
    {
        return await QueryAsync<TimesheetAuditEntryDto>(@"
            SELECT audit_id AS auditid, entity_type AS entitytype,
                   action AS action, actor_role AS actorrole,
                   old_values::text AS oldvalues, new_values::text AS newvalues,
                   reason AS reason, created_at AS createdat
            FROM attendance.timesheet_audit_log
            WHERE timesheet_id = @id ORDER BY created_at DESC",
            new { id = timesheetId });
    }

    public async Task InsertAuditEntryAsync(Guid timesheetId, string entityType, Guid entityId,
        string action, Guid actorId, string? actorRole, string? oldValues, string? newValues, string? reason)
    {
        await ExecuteAsync(@"
            INSERT INTO attendance.timesheet_audit_log
                (timesheet_id, entity_type, entity_id, action, actor_id, actor_role,
                 old_values, new_values, reason)
            VALUES (@tsId, @type, @eid, @action, @actor, @role,
                    @old::jsonb, @new::jsonb, @reason)",
            new { tsId = timesheetId, type = entityType, eid = entityId, action,
                  actor = actorId, role = actorRole, old = oldValues, @new = newValues, reason });
    }

    // ── Phase 2-5: Projects ──
    public async Task<List<TimesheetProjectDto>> GetProjectsAsync(Guid organizationId)
    {
        var result = await QueryAsync<TimesheetProjectDto>(
            @"SELECT project_id AS projectid, organization_id AS organizationid,
          project_name AS projectname, project_code AS projectcode,
          client_name AS clientname, cost_center_code AS costcentercode,
          gl_account_code AS glaccountcode, is_active AS isactive,
          start_date AS startdate, end_date AS enddate
          FROM attendance.timesheet_projects WHERE organization_id = @orgId ORDER BY project_name",
            new { orgId = organizationId });
        return result.ToList();
    }

    public async Task<Guid> CreateProjectAsync(TimesheetProjectDto dto)
    {
        return await ExecuteScalarAsync<Guid>(
            @"INSERT INTO attendance.timesheet_projects
              (organization_id, project_name, project_code, client_name, cost_center_code, gl_account_code, start_date, end_date)
              VALUES (@orgId, @name, @code, @client, @cc, @gl, @start, @end) RETURNING project_id",
            new { orgId = dto.OrganizationId, name = dto.ProjectName,
                  code = (object?)dto.ProjectCode ?? DBNull.Value,
                  client = (object?)dto.ClientName ?? DBNull.Value,
                  cc = (object?)dto.CostCenterCode ?? DBNull.Value,
                  gl = (object?)dto.GlAccountCode ?? DBNull.Value,
                  start = (object?)dto.StartDate ?? DBNull.Value,
                  end = (object?)dto.EndDate ?? DBNull.Value });
    }

    public async Task<List<TimesheetTaskDto>> GetProjectTasksAsync(Guid projectId)
    {
        var result = await QueryAsync<TimesheetTaskDto>(
            @"SELECT task_id AS taskid, project_id AS projectid, task_name AS taskname,
          is_billable_by_default AS isbillabledefault, is_active AS isactive
          FROM attendance.timesheet_project_tasks WHERE project_id = @projectId AND is_active = true ORDER BY task_name",
            new { projectId });
        return result.ToList();
    }

    public async Task<List<TimeAllocationDto>> GetTimeAllocationsAsync(Guid timesheetId, Guid employeeId)
    {
        var result = await QueryAsync<TimeAllocationDto>(
            @"SELECT a.allocation_id AS allocationid, a.attendance_id AS attendanceid,
              a.timesheet_id AS timesheetid, a.employee_id AS employeeid, a.work_date AS workdate,
              a.project_id AS projectid, a.task_id AS taskid, a.hours, a.is_billable AS isbillable,
              a.description,
              p.project_name AS projectname, t.task_name AS taskname
              FROM attendance.timesheet_time_allocations a
              JOIN attendance.timesheet_projects p ON a.project_id = p.project_id
              JOIN attendance.timesheet_project_tasks t ON a.task_id = t.task_id
              WHERE a.timesheet_id = @timesheetId AND a.employee_id = @employeeId
              ORDER BY a.work_date, p.project_name",
            new { timesheetId, employeeId });
        return result.ToList();
    }

    public async Task SaveTimeAllocationsAsync(SaveTimeAllocationDto dto)
    {
        var groups = dto.Entries.GroupBy(e => new { e.EmployeeId, e.WorkDate });
        foreach (var g in groups)
        {
            await this.ExecuteAsync(
                @"DELETE FROM attendance.timesheet_time_allocations
                  WHERE timesheet_id = @tsId AND employee_id = @empId AND work_date = @wd",
                new { tsId = dto.TimesheetId, empId = g.Key.EmployeeId, wd = g.Key.WorkDate });
            foreach (var e in g)
            {
                await this.ExecuteAsync(
                    @"INSERT INTO attendance.timesheet_time_allocations
                      (attendance_id, timesheet_id, employee_id, work_date, project_id, task_id, hours, is_billable, description)
                      VALUES (@attId, @tsId, @empId, @wd, @projId, @taskId, @hrs, @bill, @desc)",
                    new { attId = (object?)e.AttendanceId ?? DBNull.Value, tsId = dto.TimesheetId,
                          empId = e.EmployeeId, wd = e.WorkDate, projId = e.ProjectId, taskId = e.TaskId,
                          hrs = e.Hours, bill = e.IsBillable,
                          desc = (object?)e.Description ?? DBNull.Value });
            }
        }
    }

    public async Task<CompTimeBalanceDto> GetCompTimeBalanceAsync(Guid employeeId, Guid organizationId, int year)
    {
        var result = await QueryAsync<CompTimeBalanceDto>(
            @"SELECT ct.comp_time_id AS comptimeid, ct.employee_id AS employeeid,
              e.firstname || ' ' || e.lastname AS employeename, ct.year,
              ct.overtime_hours_banked AS overtimehoursbanked,
              ct.overtime_hours_used AS overtimehoursused,
              (ct.overtime_hours_banked - ct.overtime_hours_used) AS overtimehoursavailable,
              ct.conversion_ratio AS conversionratio, ct.expires_at AS expiresat
              FROM attendance.timesheet_comp_time ct
              JOIN employee.employees e ON e.employeeid = ct.employee_id
              WHERE ct.employee_id = @employeeId AND ct.organization_id = @organizationId AND ct.year = @year",
            new { employeeId, organizationId, year });
        var list = result.ToList();
        return list.FirstOrDefault() ?? new CompTimeBalanceDto { EmployeeId = employeeId, Year = year };
    }

    public async Task<TimesheetDashboardDto> GetDashboardAsync(Guid organizationId, Guid? periodId)
    {
            var p = new DynamicParameters();
            p.Add("@orgId", organizationId);
            p.Add("@periodId", periodId.HasValue ? periodId.Value : null, DbType.Guid);

            var complList = await QueryAsync<MetricCardDto>(
                @"SELECT ROUND(COALESCE(AVG(CASE WHEN a.totalhours > 0 THEN 100 ELSE 0 END), 0), 1) AS value
              FROM attendance.attendance a JOIN attendance.monthly_timesheets mt ON a.workdate BETWEEN mt.period_start AND mt.period_end
              WHERE mt.organization_id = @orgId AND (@periodId IS NULL OR mt.timesheet_id = @periodId)", p);
            var avgHrsList = await QueryAsync<MetricCardDto>(
                @"SELECT ROUND(COALESCE(AVG(a.totalhours), 0), 1) AS value
              FROM attendance.attendance a JOIN attendance.monthly_timesheets mt ON a.workdate BETWEEN mt.period_start AND mt.period_end
              WHERE mt.organization_id = @orgId AND (@periodId IS NULL OR mt.timesheet_id = @periodId)", p);
            var otList = await QueryAsync<MetricCardDto>(
                @"SELECT ROUND(COALESCE(SUM(a.overtimehours), 0), 1) AS value
              FROM attendance.attendance a JOIN attendance.monthly_timesheets mt ON a.workdate BETWEEN mt.period_start AND mt.period_end
              WHERE mt.organization_id = @orgId AND (@periodId IS NULL OR mt.timesheet_id = @periodId)", p);
            var pendList = await QueryAsync<MetricCardDto>(
                @"SELECT COUNT(*) AS value
              FROM attendance.attendance_update_requests r JOIN attendance.monthly_timesheets mt ON r.timesheet_id = mt.timesheet_id
              WHERE mt.organization_id = @orgId AND r.status = 'pending' AND (@periodId IS NULL OR mt.timesheet_id = @periodId)", p);

            var deptUtilList = await QueryAsync<DepartmentUtilizationDto>(
                @"SELECT COALESCE(d.departmentname, 'Unassigned') AS department,
              ROUND(SUM(a.totalhours), 1) AS scheduled,
              ROUND(SUM(a.totalhours) + SUM(COALESCE(a.overtimehours, 0)), 1) AS actual,
              ROUND(AVG(CASE WHEN a.totalhours >= 8 THEN 100 ELSE (a.totalhours / 8 * 100) END), 1) AS utilizationpercent
              FROM attendance.attendance a
              JOIN employee.employees e ON a.employeeid = e.employeeid
              LEFT JOIN employee.employmentdetails ed ON ed.employeeid = e.employeeid AND ed.iscurrent = true
              LEFT JOIN employee.departments d ON d.departmentid = ed.departmentid
              JOIN attendance.monthly_timesheets mt ON a.workdate BETWEEN mt.period_start AND mt.period_end
              WHERE mt.organization_id = @orgId AND (@periodId IS NULL OR mt.timesheet_id = @periodId)
              GROUP BY COALESCE(d.departmentname, 'Unassigned')", p);
            var trendList = await QueryAsync<UtilizationTrendDto>(
                @"SELECT TO_CHAR(mt.period_start, 'Mon YY') AS period,
              ROUND(AVG(CASE WHEN a.totalhours >= 8 THEN 100 ELSE (a.totalhours / 8 * 100) END), 1) AS utilizationpercent,
              ROUND(COALESCE(AVG(a.overtimehours) * 100 / NULLIF(AVG(a.totalhours), 0), 0), 1) AS overtimepercent
              FROM attendance.attendance a
              JOIN attendance.monthly_timesheets mt ON a.workdate BETWEEN mt.period_start AND mt.period_end
              WHERE mt.organization_id = @orgId GROUP BY mt.period_start ORDER BY mt.period_start DESC LIMIT 6", p);
            var reasonsList = await QueryAsync<OvertimeReasonBreakdownDto>(
                @"SELECT COALESCE(r.reason_for_edit, 'Standard OT') AS reason,
              ROUND(SUM(COALESCE(a.overtimehours, 0)), 1) AS totalhours,
              COUNT(DISTINCT a.employeeid) AS employeecount
              FROM attendance.attendance a
              JOIN attendance.monthly_timesheets mt ON a.workdate BETWEEN mt.period_start AND mt.period_end
              LEFT JOIN attendance.attendance_update_requests r ON r.attendance_id = a.attendanceid AND r.status = 'approved'
              WHERE mt.organization_id = @orgId AND a.overtimehours > 0 AND (@periodId IS NULL OR mt.timesheet_id = @periodId)
              GROUP BY r.reason_for_edit", p);

        return new TimesheetDashboardDto
            {
                CompletionRate = complList.ToList().FirstOrDefault() ?? new MetricCardDto(),
                AvgHoursPerDay = avgHrsList.ToList().FirstOrDefault() ?? new MetricCardDto(),
                OvertimeTrend = otList.ToList().FirstOrDefault() ?? new MetricCardDto(),
                PendingApprovals = pendList.ToList().FirstOrDefault() ?? new MetricCardDto(),
                DepartmentUtilization = deptUtilList.ToList(),
                UtilizationTrend = trendList.ToList(),
                OvertimeReasons = reasonsList.ToList()
            };
    }

    public async Task<TimesheetDelegationDto?> GetActiveDelegationAsync(Guid approverId)
    {
        var result = await QueryAsync<TimesheetDelegationDto>(
            @"SELECT d.delegation_id AS delegationid, d.delegator_id AS delegatedfrom,
              ef.firstname || ' ' || ef.lastname AS delegatedfromname,
              d.delegate_id AS delegatedto,
              et.firstname || ' ' || et.lastname AS delegatedtoname,
              d.start_date AS startdate, d.end_date AS enddate,
              d.is_active AS isactive
              FROM attendance.timesheet_approval_delegations d
              JOIN employee.employees ef ON d.delegator_id = ef.employeeid
              JOIN employee.employees et ON d.delegate_id = et.employeeid
              WHERE d.delegate_id = @approverId AND d.is_active = true
                AND d.start_date <= CURRENT_DATE AND d.end_date >= CURRENT_DATE",
            new { approverId });
        return result.ToList().FirstOrDefault();
    }

    public async Task<Guid> CreateDelegationAsync(CreateTimesheetDelegationDto dto, Guid delegatedFrom)
    {
        return await ExecuteScalarAsync<Guid>(
            @"INSERT INTO attendance.timesheet_approval_delegations
              (delegator_id, delegate_id, start_date, end_date)
              VALUES (@from, @to, @start, @end) RETURNING delegation_id",
            new { from = delegatedFrom, to = dto.DelegatedTo, start = dto.StartDate,
                  end = dto.EndDate });
    }

    public async Task<List<EmployeeRateCardDto>> GetRateCardsAsync(Guid organizationId)
    {
        var result = await QueryAsync<EmployeeRateCardDto>(
            @"SELECT e.employeeid AS employeeid,
              e.firstname || ' ' || e.lastname AS employeename,
              e.employeecode AS employeecode,
              COALESCE(rc.standard_rate, 0) AS standardrate,
              COALESCE(rc.overtime_rate, 0) AS overtimerate,
              COALESCE(rc.weekend_rate, 0) AS weekendrate,
              COALESCE(rc.holiday_rate, 0) AS holidayrate,
              COALESCE(rc.effective_from, CURRENT_DATE) AS effectivefrom
              FROM employee.employees e
              LEFT JOIN attendance.employee_rate_cards rc
                ON e.employeeid = rc.employee_id
               AND rc.organization_id = @organizationId
               AND rc.effective_from <= CURRENT_DATE
              WHERE e.organizationid = @organizationId ORDER BY e.firstname",
            new { organizationId });
        return result.ToList();
    }

    public async Task BulkCopyFromPreviousAsync(BulkCopyRequestDto dto)
    {
        await this.ExecuteAsync(
            @"INSERT INTO attendance.attendance_update_requests
              (attendance_id, timesheet_id, employee_id, organization_id, workdate, requested_status,
               requested_checkin, requested_checkout, requested_overtime_hours,
               requested_overtime_type, requested_late_minutes, requested_leave_type_id,
               requested_notes, reason_for_edit, status)
              SELECT a.attendanceid, @targetId, a.employeeid,
                  (SELECT organizationid FROM employee.employees WHERE employeeid = a.employeeid LIMIT 1),
                  a.workdate, a.status,
                  a.checkintime::text, a.checkouttime::text, a.overtimehours,
                  a.overtimetype, a.late_minutes, a.leave_type_id,
                  'Bulk copied', 'Bulk copy from previous period', 'draft'
              FROM attendance.attendance a
              JOIN attendance.monthly_timesheets mt ON a.workdate BETWEEN mt.period_start AND mt.period_end
              WHERE mt.timesheet_id = @sourceId AND a.employeeid = ANY(@empIds)",
            new { sourceId = dto.SourceTimesheetId, targetId = dto.TargetTimesheetId,
                  empIds = dto.EmployeeIds.ToArray() });
    }

    public async Task AutoFillFromScheduleAsync(BulkAutoFillRequestDto dto)
    {
        foreach (var date in dto.Dates)
        {
            var ci = dto.CheckInTime ?? "09:00";
            var co = dto.CheckOutTime ?? "18:00";
            decimal hrs = 9m;
            if (TimeSpan.TryParse(ci, out var tsIn) && TimeSpan.TryParse(co, out var tsOut))
            {
                var diff = tsOut - tsIn;
                if (diff.TotalHours > 0) hrs = Math.Round((decimal)diff.TotalHours, 2);
            }
            await this.ExecuteAsync(
                @"INSERT INTO attendance.attendance (employeeid, workdate, status, checkintime, checkouttime, totalhours)
                  VALUES (@empId, @date, @status, @ci::timestamptz, @co::timestamptz, @hrs)
                  ON CONFLICT (employeeid, workdate) DO UPDATE
                  SET status = EXCLUDED.status, checkintime = EXCLUDED.checkintime,
                      checkouttime = EXCLUDED.checkouttime, totalhours = EXCLUDED.totalhours",
                new { empId = dto.EmployeeId, date, status = dto.Status, ci = (object?)null ?? DBNull.Value, co = (object?)null ?? DBNull.Value, hrs });
        }
    }

    // ── Guards ──────────────────────────────────────────────────────────────
    public async Task<bool> IsRecordOverriddenAsync(Guid? attendanceId, Guid employeeId, DateTime workDate)
    {
        if (attendanceId.HasValue)
        {
            return await ExecuteScalarAsync<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM attendance.finalized_timesheet_records
                    WHERE attendance_id = @attId AND record_status = 'overridden'
                )",
                new { attId = attendanceId.Value });
        }
        return await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1 FROM attendance.finalized_timesheet_records
                WHERE employee_id = @empId AND work_date = @wd AND record_status = 'overridden'
            )",
            new { empId = employeeId, wd = workDate });
    }

    public async Task<bool> IsAttendanceOwnedByEmployeeAsync(Guid attendanceId, Guid employeeId)
    {
        return await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1 FROM attendance.attendance
                WHERE attendanceid = @attId AND employeeid = @empId
            )",
            new { attId = attendanceId, empId = employeeId });
    }

    public async Task<bool> DoesEmployeeHaveAttendanceOnDateAsync(Guid employeeId, DateTime workDate)
    {
        return await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1 FROM attendance.attendance
                WHERE employeeid = @empId AND workdate = @wd
            )",
            new { empId = employeeId, wd = workDate.Date });
    }

    public async Task<IEnumerable<Guid>> GetEmployeeIdsInTimesheetAsync(Guid timesheetId)
    {
        // Get all employee IDs that have attendance records within this timesheet period
        return await QueryAsync<Guid>(@"
            SELECT DISTINCT a.employeeid
            FROM attendance.attendance a
            JOIN attendance.monthly_timesheets mt ON a.workdate BETWEEN mt.period_start AND mt.period_end
            WHERE mt.timesheet_id = @tsId",
            new { tsId = timesheetId });
    }

    public async Task<bool> IsActorTimesheetApproverAsync(Guid timesheetId, Guid actorId)
    {
        // Check if the actor is an assigned approver for any employee in this timesheet
        return await ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1 FROM attendance.timesheet_approval_progress p
                WHERE p.timesheet_id = @tsId AND p.approver_id = @actorId AND p.status = 'Pending'
            )",
            new { tsId = timesheetId, actorId });
    }
}
