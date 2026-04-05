-- ============================================================================
-- TEST SCRIPT: Attendance -> Timesheet Finalization -> Payroll Overtime Amount
-- File: HRMS.Script/test_overtime_e2e.sql
--
-- Purpose:
-- 1) Create/update one attendance day with overtime for a test employee.
-- 2) Ensure one monthly timesheet exists and finalize that day into
--    attendance.finalized_timesheet_records.
-- 3) Ensure an Open payroll period and active regular overtime rule exist.
-- 4) Generate/Upsert payroll.overtimeentries using finalized overtime hours.
-- 5) Print verification result sets.
--
-- How to run (psql):
--   psql -h <host> -U <user> -d <db> -f HRMS.Script/test_overtime_e2e.sql
--
-- IMPORTANT:
-- - Edit configuration values in the CONFIG block below before running.
-- - This script COMMITs so you can verify in FE screens.
-- ============================================================================

BEGIN;

-- ----------------------------------------------------------------------------
-- CONFIG: EDIT THESE VALUES
-- ----------------------------------------------------------------------------
CREATE TEMP TABLE _ot_cfg (
    org_id uuid NOT NULL,
    employee_id uuid NOT NULL,
    manager_id uuid,
    test_work_date date NOT NULL,
    payroll_period_name varchar(100) NOT NULL,
    period_start date NOT NULL,
    period_end date NOT NULL,
    payment_date date,
    overtime_rule_name varchar(100) NOT NULL,
    overtime_fixed_amount numeric(15,2),
    overtime_percentage numeric(5,2),
    base_hourly_rate numeric(15,2) NOT NULL,
    timesheet_name varchar(150) NOT NULL
);

INSERT INTO _ot_cfg (
    org_id,
    employee_id,
    manager_id,
    test_work_date,
    payroll_period_name,
    period_start,
    period_end,
    payment_date,
    overtime_rule_name,
    overtime_fixed_amount,
    overtime_percentage,
    base_hourly_rate,
    timesheet_name
)
VALUES (
    '00000000-0000-0000-0000-000000000000',  -- TODO org_id
    '00000000-0000-0000-0000-000000000000',  -- TODO employee_id
    NULL,                                     -- optional manager employee id
    CURRENT_DATE - 1,
    'E2E OT Test Period',
    date_trunc('month', CURRENT_DATE)::date,
    (date_trunc('month', CURRENT_DATE) + INTERVAL '1 month - 1 day')::date,
    (date_trunc('month', CURRENT_DATE) + INTERVAL '1 month + 4 day')::date,
    'E2E Regular OT Rule',
    500.00,                                   -- fixed amount per OT hour
    NULL,                                     -- OR set percentage and fixed NULL
    1000.00,                                  -- used only when percentage is set
    'E2E Timesheet'
);

-- ----------------------------------------------------------------------------
-- Guard checks
-- ----------------------------------------------------------------------------
DO $$
DECLARE
    v_org uuid;
    v_emp uuid;
BEGIN
    SELECT org_id, employee_id INTO v_org, v_emp FROM _ot_cfg LIMIT 1;

    IF v_org = '00000000-0000-0000-0000-000000000000'::uuid
       OR v_emp = '00000000-0000-0000-0000-000000000000'::uuid THEN
        RAISE EXCEPTION 'Please set real org_id and employee_id in _ot_cfg before running.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM auth.organizations o WHERE o.organizationid = v_org) THEN
        RAISE EXCEPTION 'Configured org_id % not found in auth.organizations.', v_org;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM employee.employees e
        WHERE e.employeeid = v_emp AND e.organizationid = v_org
    ) THEN
        RAISE EXCEPTION 'Configured employee_id % not found in employee.employees for this org.', v_emp;
    END IF;
END $$;

-- ----------------------------------------------------------------------------
-- 1) Ensure an Open payroll period exists for the configured range
-- ----------------------------------------------------------------------------
INSERT INTO payroll.payrollperiods (
    periodid,
    organizationid,
    periodname,
    startdate,
    enddate,
    paymentdate,
    status,
    description,
    isactive,
    isdeleted,
    createdby,
    createdat,
    updatedat
)
SELECT
    gen_random_uuid(),
    c.org_id,
    c.payroll_period_name,
    c.period_start,
    c.period_end,
    c.payment_date,
    'Open',
    'E2E overtime flow test period',
    true,
    false,
    c.manager_id,
    NOW(),
    NOW()
FROM _ot_cfg c
WHERE NOT EXISTS (
    SELECT 1
    FROM payroll.payrollperiods p
    WHERE p.organizationid = c.org_id
      AND p.startdate = c.period_start
      AND p.enddate = c.period_end
      AND p.isdeleted = false
);

-- ----------------------------------------------------------------------------
-- 2) Ensure active regular overtime rule exists for org
-- ----------------------------------------------------------------------------
INSERT INTO payroll.overtimerules (
    ruleid,
    organizationid,
    rulename,
    description,
    overtimetype,
    fixedamount,
    percentage,
    isactive,
    isdeleted,
    createdat,
    updatedat
)
SELECT
    gen_random_uuid(),
    c.org_id,
    c.overtime_rule_name,
    'E2E regular overtime rule',
    'regular',
    c.overtime_fixed_amount,
    c.overtime_percentage,
    true,
    false,
    NOW(),
    NOW()
FROM _ot_cfg c
WHERE NOT EXISTS (
    SELECT 1
    FROM payroll.overtimerules r
    WHERE r.organizationid = c.org_id
      AND r.overtimetype = 'regular'
      AND r.isdeleted = false
);

-- ----------------------------------------------------------------------------
-- 3) Create/Update one attendance day with overtime hours
-- ----------------------------------------------------------------------------
INSERT INTO attendance.attendance (
    attendanceid,
    employeeid,
    workdate,
    checkintime,
    checkouttime,
    totalhours,
    overtimehours,
    status,
    notes,
    createdat,
    updatedat,
    islate
)
SELECT
    gen_random_uuid(),
    c.employee_id,
    c.test_work_date,
    (c.test_work_date + TIME '09:00:00')::timestamptz,
    (c.test_work_date + TIME '19:00:00')::timestamptz,
    10.00,
    2.00,
    'present',
    'E2E OT attendance seed',
    NOW(),
    NOW(),
    false
FROM _ot_cfg c
ON CONFLICT (employeeid, workdate)
DO UPDATE SET
    checkintime = EXCLUDED.checkintime,
    checkouttime = EXCLUDED.checkouttime,
    totalhours = EXCLUDED.totalhours,
    overtimehours = EXCLUDED.overtimehours,
    status = EXCLUDED.status,
    notes = EXCLUDED.notes,
    updatedat = NOW();

-- ----------------------------------------------------------------------------
-- 4) Ensure monthly timesheet exists for test month
-- ----------------------------------------------------------------------------
INSERT INTO attendance.monthly_timesheets (
    timesheet_id,
    organization_id,
    timesheet_name,
    month,
    year,
    generated_by,
    created_at,
    status
)
SELECT
    gen_random_uuid(),
    c.org_id,
    c.timesheet_name,
    EXTRACT(MONTH FROM c.test_work_date)::int,
    EXTRACT(YEAR FROM c.test_work_date)::int,
    c.employee_id,
    NOW(),
    'Draft'
FROM _ot_cfg c
WHERE NOT EXISTS (
    SELECT 1
    FROM attendance.monthly_timesheets t
    WHERE t.organization_id = c.org_id
      AND t.month = EXTRACT(MONTH FROM c.test_work_date)::int
      AND t.year = EXTRACT(YEAR FROM c.test_work_date)::int
      AND t.timesheet_name = c.timesheet_name
);

-- ----------------------------------------------------------------------------
-- 5) Finalize that attendance day into finalized_timesheet_records
-- ----------------------------------------------------------------------------
WITH picked AS (
    SELECT
        c.org_id,
        c.employee_id,
        c.test_work_date,
        a.attendanceid,
        a.checkintime,
        a.checkouttime,
        a.totalhours,
        a.overtimehours,
        a.status,
        t.timesheet_id
    FROM _ot_cfg c
    JOIN attendance.attendance a
      ON a.employeeid = c.employee_id
     AND a.workdate = c.test_work_date
    JOIN attendance.monthly_timesheets t
      ON t.organization_id = c.org_id
     AND t.month = EXTRACT(MONTH FROM c.test_work_date)::int
     AND t.year = EXTRACT(YEAR FROM c.test_work_date)::int
     AND t.timesheet_name = c.timesheet_name
)
INSERT INTO attendance.finalized_timesheet_records (
    record_id,
    timesheet_id,
    employee_id,
    organization_id,
    work_date,
    final_checkin,
    final_checkout,
    final_total_hours,
    final_overtime_hours,
    final_late_hours,
    final_status,
    approved_by,
    approved_at,
    attendance_id,
    override_reason,
    record_status
)
SELECT
    gen_random_uuid(),
    p.timesheet_id,
    p.employee_id,
    p.org_id,
    p.test_work_date,
    p.checkintime,
    p.checkouttime,
    COALESCE(p.totalhours, 0),
    COALESCE(p.overtimehours, 0),
    COALESCE((
        SELECT CASE
            WHEN p.checkintime IS NULL THEN 0
            WHEN LOWER(COALESCE(p.status, 'absent')) IN ('absent', 'leave', 'holiday', 'weekend') THEN 0
            WHEN s.starttime IS NULL THEN 0
            WHEN GREATEST(
                EXTRACT(EPOCH FROM ((p.checkintime AT TIME ZONE COALESCE(NULLIF(s.timezone, ''), 'UTC'))::time - s.starttime)) / 60,
                0
            ) > COALESCE(lar.graceminutes, 0)
            THEN ROUND(
                GREATEST(
                    EXTRACT(EPOCH FROM ((p.checkintime AT TIME ZONE COALESCE(NULLIF(s.timezone, ''), 'UTC'))::time - s.starttime)) / 3600.0,
                    0
                ),
                2
            )
            ELSE 0
        END
        FROM employee.employmentdetails ed
        LEFT JOIN attendance.shifts s ON s.shiftid = ed.shiftid
        LEFT JOIN payroll.lateattendancerules lar
          ON lar.organizationid = p.org_id
         AND lar.isactive = true
         AND lar.isdeleted = false
        WHERE ed.employeeid = p.employee_id
          AND ed.iscurrent = true
        LIMIT 1
    ), 0),
    COALESCE(p.status, 'absent'),
    (SELECT manager_id FROM _ot_cfg LIMIT 1),
    NOW(),
    p.attendanceid,
    'E2E auto finalize test',
    'finalized'
FROM picked p
ON CONFLICT (attendance_id) WHERE attendance_id IS NOT NULL
DO UPDATE SET
    timesheet_id = EXCLUDED.timesheet_id,
    final_checkin = EXCLUDED.final_checkin,
    final_checkout = EXCLUDED.final_checkout,
    final_total_hours = EXCLUDED.final_total_hours,
    final_overtime_hours = EXCLUDED.final_overtime_hours,
    final_late_hours = EXCLUDED.final_late_hours,
    final_status = EXCLUDED.final_status,
    approved_by = EXCLUDED.approved_by,
    approved_at = NOW(),
    record_status = CASE
        WHEN attendance.finalized_timesheet_records.record_status = 'overridden' THEN 'overridden'
        ELSE 'finalized'
    END;

-- Mark timesheet finalized (for payroll-facing finalized-only consumers)
UPDATE attendance.monthly_timesheets t
SET status = 'Finalized'
FROM _ot_cfg c
WHERE t.organization_id = c.org_id
  AND t.month = EXTRACT(MONTH FROM c.test_work_date)::int
  AND t.year = EXTRACT(YEAR FROM c.test_work_date)::int
  AND t.timesheet_name = c.timesheet_name;

-- ----------------------------------------------------------------------------
-- 6) Generate payroll overtime entry from finalized_timesheet_records
-- ----------------------------------------------------------------------------
WITH cfg AS (
    SELECT * FROM _ot_cfg LIMIT 1
),
period_pick AS (
    SELECT p.periodid
    FROM payroll.payrollperiods p, cfg c
    WHERE p.organizationid = c.org_id
      AND p.startdate <= c.test_work_date
      AND p.enddate >= c.test_work_date
      AND p.status = 'Open'
      AND p.isdeleted = false
    ORDER BY p.createdat DESC
    LIMIT 1
),
rule_pick AS (
    SELECT r.ruleid, r.fixedamount, r.percentage
    FROM payroll.overtimerules r, cfg c
    WHERE r.organizationid = c.org_id
      AND r.overtimetype = 'regular'
      AND r.isactive = true
      AND r.isdeleted = false
    ORDER BY r.updatedat DESC, r.createdat DESC
    LIMIT 1
),
src AS (
    SELECT
        f.employee_id,
        f.work_date,
        COALESCE(f.final_overtime_hours, 0)::numeric(8,2) AS overtime_hours
    FROM attendance.finalized_timesheet_records f, cfg c
    WHERE f.organization_id = c.org_id
      AND f.employee_id = c.employee_id
      AND f.work_date = c.test_work_date
      AND f.record_status IN ('finalized', 'overridden')
)
INSERT INTO payroll.overtimeentries (
    overtimeid,
    employeeid,
    periodid,
    ruleid,
    hoursworked,
    finalamount,
    entrystatus,
    approvedby,
    overtimedate,
    createdat,
    updatedat
)
SELECT
    gen_random_uuid(),
    s.employee_id,
    p.periodid,
    r.ruleid,
    s.overtime_hours,
    CASE
        WHEN r.fixedamount IS NOT NULL THEN s.overtime_hours * r.fixedamount
        WHEN r.percentage IS NOT NULL THEN s.overtime_hours * c.base_hourly_rate * (r.percentage / 100.0)
        ELSE 0
    END,
    'pending',
    NULL,
    s.work_date,
    NOW(),
    NOW()
FROM src s
CROSS JOIN period_pick p
CROSS JOIN rule_pick r
CROSS JOIN cfg c
WHERE s.overtime_hours > 0
ON CONFLICT (employeeid, periodid, overtimedate)
DO UPDATE SET
    ruleid = EXCLUDED.ruleid,
    hoursworked = EXCLUDED.hoursworked,
    finalamount = EXCLUDED.finalamount,
    updatedat = NOW();

-- ----------------------------------------------------------------------------
-- 7) Verification queries
-- ----------------------------------------------------------------------------
-- Attendance source row
SELECT
    a.employeeid,
    a.workdate,
    a.totalhours,
    a.overtimehours,
    a.status,
    a.checkintime,
    a.checkouttime
FROM attendance.attendance a
JOIN _ot_cfg c ON c.employee_id = a.employeeid AND c.test_work_date = a.workdate;

-- Finalized timesheet row
SELECT
    f.timesheet_id,
    f.employee_id,
    f.work_date,
    f.final_total_hours,
    f.final_overtime_hours,
    f.final_late_hours,
    f.final_status,
    f.record_status
FROM attendance.finalized_timesheet_records f
JOIN _ot_cfg c ON c.employee_id = f.employee_id AND c.test_work_date = f.work_date
ORDER BY f.approved_at DESC
LIMIT 1;

-- Payroll overtime entry row
SELECT
    oe.overtimeid,
    oe.employeeid,
    oe.periodid,
    oe.ruleid,
    oe.hoursworked,
    oe.finalamount,
    oe.entrystatus,
    oe.overtimedate
FROM payroll.overtimeentries oe
JOIN _ot_cfg c ON c.employee_id = oe.employeeid AND c.test_work_date = oe.overtimedate
ORDER BY oe.updatedat DESC
LIMIT 1;

COMMIT;

-- ============================================================================
-- Optional cleanup (manual):
--   DELETE FROM payroll.overtimeentries WHERE overtimedate = <test_date> AND employeeid = <employee_id>;
--   DELETE FROM attendance.finalized_timesheet_records WHERE work_date = <test_date> AND employee_id = <employee_id>;
--   DELETE FROM attendance.attendance WHERE workdate = <test_date> AND employeeid = <employee_id>;
-- ============================================================================
