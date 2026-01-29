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
        DateTime? endDate = null, int page = 1, int pageSize = 10, Guid? departmentId = null, string Status = null)
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

    //Chnages

    //public async Task<Guid?> GetAttendanceIdAsync(Guid employeeId)
    //{
    //    try
    //    {
    //        var sql = @"
    //        SELECT attendanceid
    //        FROM attendance.attendance
    //        WHERE employeeid = @employeeId
    //          AND workdate =(NOW() AT TIME ZONE 'UTC')::date
    //        LIMIT 1;
    //    ";

    //        return await QuerySingleOrDefaultAsync<Guid?>(sql, new { employeeId });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex,
    //            "Error getting today's attendance id for employee {EmployeeId}", employeeId);
    //        throw;
    //    }
    //}


    //public async Task<Guid?> GetAttendanceIdAsync(Guid employeeId, DateTime? date = null)
    //{
    //    try
    //    {
    //        // Use provided date or default to today (UTC)
    //        var targetDate = date?.Date ?? DateTime.UtcNow.Date;

    //        var sql = @"
    //    SELECT attendanceid
    //    FROM attendance.attendance
    //    WHERE employeeid = @employeeId
    //      AND workdate::date = @workDate
    //    LIMIT 1;
    //    ";

    //        return await QuerySingleOrDefaultAsync<Guid?>(sql, new
    //        {
    //            employeeId,
    //            workDate = targetDate
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex,
    //            "Error getting attendance id for employee {EmployeeId} on date {Date}", employeeId, date);
    //        throw;
    //    }
    //}


    public async Task<Guid?> GetAttendanceIdAsync(Guid employeeId, DateTime? date = null)
    {
        try
        {
            string sql;
            object param;

            if (date == null)
            {
                // No date provided ? get today�s attendance
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
                // Date provided ? match workdate with that date
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

            // Fetch attendance records for employee in the month
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

    // New missing methods implementation
    public async Task<Attendance?> GetAttendanceByIdAsync(Guid attendanceId)
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


    //Chnages here

    //public async Task<bool> CreateShiftAsync(Guid organizationId, CreateShiftDto request)
    //{
    //    try
    //    {
    //        var createdAt = DateTime.Now;
    //        var sql = @"
    //        INSERT INTO attendance.shifts
    //            (organizationid, shiftname, starttime, endtime, breakduration, daysofweek, timezone,createdat)
    //        VALUES
    //            (@OrganizationId, @ShiftName, @StartTime, @EndTime, @BreakDuration, @DaysofWeek, @Timezone,@createdAt);
    //    ";

    //        var parameters = new
    //        {
    //            createdAt = createdAt,
    //            OrganizationId = organizationId,
    //            ShiftName = request.ShiftName,
    //            StartTime = request.StartTime,
    //            EndTime = request.EndTime,
    //            BreakDuration = request.BreakDuration ?? 0,
    //            DaysofWeek = request.DaysofWeek ?? new int[] { 1, 2, 3, 4, 5 },
    //            Timezone = request.Timezone ?? "UTC"
    //        };

    //        var rowsAffected = await ExecuteAsync(sql, parameters);
    //        return rowsAffected > 0;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error creating shift for organization {OrganizationId}", organizationId);
    //        throw;
    //    }
    //}


    public async Task<bool> CreateShiftAsync(Guid organizationId, CreateShiftDto request)
    {
        try
        {
            var createdAt = DateTime.UtcNow;
                
            var sql = @"
        INSERT INTO attendance.shifts
        (organizationid, shiftname, starttime, endtime,marginhours, breakduration, breakstarttime, breakendtime,
         overtimestarttime, maxovertimeinminutes, daysofweek, timezone, createdat)
        VALUES
        (@OrganizationId, @ShiftName, @StartTime, @EndTime,@marginHours, @BreakDuration, @BreakStartTime, @BreakEndTime,
         @OvertimeStartTime, @MaxOvertimeInMinutes, @DaysofWeek, @Timezone, @CreatedAt);
    ";

            var parameters = new
            {
                OrganizationId = organizationId,
                ShiftName = request.ShiftName,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
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

    // Get Shift Details (Name, StartTime, EndTime)
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









    //public async Task<List<ShiftDto>> GetAllShiftsAsync(Guid organizationId)
    //{
    //    try
    //    {
    //        var sql = @"
    //        SELECT 
    //            shiftid AS ShiftId,
    //            organizationid AS OrganizationId,
    //            shiftname AS ShiftName,
    //            starttime AS StartTime,
    //            endtime AS EndTime,
    //            breakduration AS BreakDuration,
    //            daysofweek AS DaysOfWeek,
    //            timezone AS Timezone,
    //            isactive AS IsActive,
    //            createdat AS CreatedAt
    //        FROM attendance.shifts
    //        WHERE organizationid = @OrganizationId
    //        ORDER BY createdat DESC;
    //    ";

    //        var parameters = new { OrganizationId = organizationId };
    //        var result = await QueryAsync<ShiftDto>(sql, parameters);
    //        return result.ToList();
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error fetching shifts for organization {OrganizationId}", organizationId);
    //        throw;
    //    }
    //}


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



    //public async Task<bool> UpdateShiftAsync(Guid organizationId, Guid shiftId, UpdateShiftDto request)
    //{
    //    try
    //    {

    //        var sql = @"
    //        UPDATE attendance.shifts
    //        SET
    //            shiftname = @ShiftName,
    //            starttime = @StartTime,
    //            endtime = @EndTime,
    //            breakduration = @BreakDuration,
    //            daysofweek = @DaysofWeek,
    //            timezone = @Timezone,

    //        WHERE
    //            organizationid = @OrganizationId AND shiftid = @ShiftId;
    //    ";

    //        var parameters = new
    //        {
    //            OrganizationId = organizationId,
    //            ShiftId = shiftId,
    //            ShiftName = request.ShiftName,
    //            StartTime = request.StartTime,
    //            EndTime = request.EndTime,
    //            BreakDuration = request.BreakDuration ?? 0,
    //            DaysofWeek = request.DaysofWeek ?? new int[] { 1, 2, 3, 4, 5 },
    //            Timezone = request.Timezone ?? "UTC",

    //        };

    //        var rowsAffected = await ExecuteAsync(sql, parameters);
    //        return rowsAffected > 0;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error updating shift {ShiftId} for organization {OrganizationId}", shiftId, organizationId);
    //        throw;
    //    }
    //}

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


    //public async Task<List<EmployeeSimpleDto>> GetEmployeeForAdminAsync(
    //Guid organizationId,
    //Guid? departmentId)
    //{
    //    try
    //    {
    //        var sql = @"
    //        SELECT 
    //            e.employeeid AS EmployeeId,
    //            e.firstname AS FirstName,
    //            e.lastname AS LastName
    //        FROM employee.employees e
    //        INNER JOIN employee.employmentdetails d
    //            ON e.employeeid = d.employeeid
    //        WHERE e.organizationid = @OrganizationId
    //          AND e.status = 'active'
    //          AND d.iscurrent = true
    //          AND (@DepartmentId IS NULL OR d.departmentid = @DepartmentId)
    //        ORDER BY e.firstname, e.lastname;
    //    ";

    //        var parameters = new
    //        {
    //            OrganizationId = organizationId,
    //            DepartmentId = departmentId
    //        };

    //        // ?? EXACT same repository pattern you use everywhere
    //        var result = await QueryAsync<EmployeeSimpleDto>(sql, parameters);

    //        return result.ToList();
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex,
    //            "Error retrieving employee list for Admin in Organization {OrganizationId}",
    //            organizationId);

    //        throw;
    //    }
    //}

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
//    Task<bool> IsHRManagerAsync(Guid employeeId, Guid organizationId)
//    {
//        try
//        {
//            var sql = @"SELECT 
//    CASE 
//        WHEN r.rolename = 'HR Manager' THEN TRUE
//        ELSE FALSE
//    END AS is_hr_manager
//FROM employee.employee e
//JOIN auth.users u ON u.userid = e.userid
//JOIN auth.roles r ON r.roleid = u.roleid
//WHERE e.employeeid = @employeeid and u.organizationId=@organizationId;
//";
//            var parameters = new
//            {
//                employeeId,
//                organizationId
//            };
//            var isHRManager = QuerySingleAsync<bool>(sql, parameters);
//            return isHRManager;
//        }
//        catch (Exception ex)
//        {
//            throw new Exception($"Error checking HR manager status for employee {employeeId} in organization {organizationId}", ex);
//        }
//    }

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
