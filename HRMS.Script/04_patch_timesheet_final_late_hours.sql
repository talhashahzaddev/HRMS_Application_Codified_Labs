-- ============================================================================
-- Patch: Add finalized late hours to timesheet records
-- File: HRMS.Script/04_patch_timesheet_final_late_hours.sql
-- ============================================================================

BEGIN;

ALTER TABLE IF EXISTS attendance.finalized_timesheet_records
    ADD COLUMN IF NOT EXISTS final_late_hours numeric(5,2);

-- Backfill for rows linked to attendance records
UPDATE attendance.finalized_timesheet_records f
SET final_late_hours = CASE
    WHEN a.checkintime IS NULL THEN 0
    WHEN LOWER(COALESCE(f.final_status, a.status, 'absent')) IN ('absent', 'leave', 'holiday', 'weekend') THEN 0
    WHEN s.starttime IS NULL THEN 0
    WHEN GREATEST(
        EXTRACT(EPOCH FROM ((COALESCE(f.final_checkin, a.checkintime) AT TIME ZONE COALESCE(NULLIF(s.timezone, ''), 'UTC'))::time - s.starttime)) / 60,
        0
    ) > COALESCE(lar.graceminutes, 0)
    THEN ROUND(
        GREATEST(
            EXTRACT(EPOCH FROM ((COALESCE(f.final_checkin, a.checkintime) AT TIME ZONE COALESCE(NULLIF(s.timezone, ''), 'UTC'))::time - s.starttime)) / 3600.0,
            0
        ),
        2
    )
    ELSE 0
END
FROM attendance.attendance a
LEFT JOIN employee.employmentdetails ed
    ON ed.employeeid = a.employeeid
   AND ed.iscurrent = true
LEFT JOIN attendance.shifts s
    ON s.shiftid = ed.shiftid
LEFT JOIN employee.employees e
    ON e.employeeid = a.employeeid
LEFT JOIN payroll.lateattendancerules lar
    ON lar.organizationid = e.organizationid
   AND lar.isactive = true
   AND lar.isdeleted = false
WHERE f.attendance_id IS NOT NULL
  AND f.attendance_id = a.attendanceid
  AND (f.final_late_hours IS NULL OR f.final_late_hours < 0);

-- Backfill for absent-day rows without attendance record
UPDATE attendance.finalized_timesheet_records
SET final_late_hours = 0
WHERE attendance_id IS NULL
  AND (final_late_hours IS NULL OR final_late_hours < 0);

-- Optional guard
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'chk_ftr_final_late_hours_non_negative'
  ) THEN
    ALTER TABLE attendance.finalized_timesheet_records
      ADD CONSTRAINT chk_ftr_final_late_hours_non_negative
      CHECK (COALESCE(final_late_hours, 0) >= 0);
  END IF;
END $$;

COMMIT;
