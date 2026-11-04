using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class TimesheetConfigRepository : BaseRepository, ITimesheetConfigRepository
{
    public TimesheetConfigRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<TimesheetConfigDto?> GetByOrgIdAsync(Guid orgId)
    {
        return await QuerySingleOrDefaultAsync<TimesheetConfigDto>(@"
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
                   COALESCE(shift_start_hour, 9) AS shiftstarthour,
                   COALESCE(shift_start_minute, 0) AS shiftstartminute
            FROM attendance.timesheet_config WHERE organization_id = @orgId", new { orgId });
    }

    public async Task<TimesheetConfigDto> UpsertConfigAsync(Guid orgId, TimesheetConfigDto dto)
    {
        await ExecuteAsync(@"
            INSERT INTO attendance.timesheet_config (organization_id, requires_manager_approval,
                requires_hr_approval, allow_employee_corrections, correction_deadline_days,
                working_days, half_day_threshold_hours, full_day_threshold_hours, late_grace_minutes,
                auto_finalize_on_lock, overtime_multiplier_regular, overtime_multiplier_weekend,
                overtime_multiplier_holiday, timezone, shift_start_hour, shift_start_minute, updated_at)
            VALUES (@orgId, @requiresManagerApproval, @requiresHrApproval, @allowEmployeeCorrections,
                @correctionDeadlineDays, @workingDays, @halfDayThresholdHours, @fullDayThresholdHours,
                @lateGraceMinutes, @autoFinalizeOnLock, @overtimeMultiplierRegular,
                @overtimeMultiplierWeekend, @overtimeMultiplierHoliday, @timezone,
                @shiftStartHour, @shiftStartMinute, now())
            ON CONFLICT (organization_id) DO UPDATE SET
                requires_manager_approval=EXCLUDED.requires_manager_approval,
                requires_hr_approval=EXCLUDED.requires_hr_approval,
                allow_employee_corrections=EXCLUDED.allow_employee_corrections,
                correction_deadline_days=EXCLUDED.correction_deadline_days,
                working_days=EXCLUDED.working_days,
                half_day_threshold_hours=EXCLUDED.half_day_threshold_hours,
                full_day_threshold_hours=EXCLUDED.full_day_threshold_hours,
                late_grace_minutes=EXCLUDED.late_grace_minutes,
                auto_finalize_on_lock=EXCLUDED.auto_finalize_on_lock,
                overtime_multiplier_regular=EXCLUDED.overtime_multiplier_regular,
                overtime_multiplier_weekend=EXCLUDED.overtime_multiplier_weekend,
                overtime_multiplier_holiday=EXCLUDED.overtime_multiplier_holiday,
                timezone=EXCLUDED.timezone,
                shift_start_hour=EXCLUDED.shift_start_hour,
                shift_start_minute=EXCLUDED.shift_start_minute,
                updated_at=now()",
            new
            {
                orgId,
                dto.RequiresManagerApproval,
                dto.RequiresHrApproval,
                dto.AllowEmployeeCorrections,
                dto.CorrectionDeadlineDays,
                dto.WorkingDays,
                dto.HalfDayThresholdHours,
                dto.FullDayThresholdHours,
                dto.LateGraceMinutes,
                dto.AutoFinalizeOnLock,
                dto.OvertimeMultiplierRegular,
                dto.OvertimeMultiplierWeekend,
                dto.OvertimeMultiplierHoliday,
                dto.Timezone,
                dto.ShiftStartHour,
                dto.ShiftStartMinute
            });
        return (await GetByOrgIdAsync(orgId))!;
    }
}
