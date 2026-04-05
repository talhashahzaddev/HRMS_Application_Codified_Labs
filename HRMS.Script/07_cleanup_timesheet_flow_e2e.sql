-- ============================================================================
-- Cleanup Script: Timesheet Flow E2E Test Data
-- File: HRMS.Script/07_cleanup_timesheet_flow_e2e.sql
-- Removes rows seeded by 05_seed_timesheet_flow_e2e.sql
-- ============================================================================

BEGIN;

CREATE TEMP TABLE _ts_test_cfg (
    user1_id uuid NOT NULL,
    user2_id uuid NOT NULL,
    period_month int NOT NULL,
    period_year int NOT NULL,
    period_start date NOT NULL,
    period_end date NOT NULL,
    timesheet_name varchar(150) NOT NULL
);

INSERT INTO _ts_test_cfg (
    user1_id,
    user2_id,
    period_month,
    period_year,
    period_start,
    period_end,
    timesheet_name
)
VALUES (
    '59e11ead-c86d-43b1-8a7f-0c6a1377ca08'::uuid,
    '8801d65c-991e-40dc-b24a-7264ca28592e'::uuid,
    3,
    2026,
    DATE '2026-03-01',
    DATE '2026-03-31',
    'TS_E2E_FLOW_MAR_2026'
);

CREATE TEMP TABLE _ts_test_employees AS
SELECT
    e.employeeid,
    e.userid,
    e.organizationid
FROM employee.employees e
JOIN _ts_test_cfg c
  ON e.userid IN (c.user1_id, c.user2_id);

CREATE TEMP TABLE _ts_target_timesheet AS
SELECT mt.timesheet_id
FROM attendance.monthly_timesheets mt
JOIN _ts_test_cfg c
  ON mt.organization_id = (SELECT organizationid FROM _ts_test_employees LIMIT 1)
 AND mt.timesheet_name = c.timesheet_name
 AND mt.month = c.period_month
 AND mt.year = c.period_year
LIMIT 1;

-- Remove request rows seeded for E2E
DELETE FROM attendance.attendance_update_requests r
USING _ts_test_employees e, _ts_test_cfg c
WHERE r.employee_id = e.employeeid
  AND r.workdate BETWEEN c.period_start AND c.period_end
  AND (
      r.reason_for_edit LIKE 'E2E_FLOW_TEST:%'
      OR COALESCE(r.requested_notes, '') LIKE 'E2E_FLOW_TEST:%'
      OR COALESCE(r.rejection_reason, '') LIKE 'E2E_FLOW_TEST:%'
  );

-- Remove finalized rows linked to test timesheet and target employees
DELETE FROM attendance.finalized_timesheet_records f
USING _ts_test_employees e, _ts_target_timesheet t
WHERE f.employee_id = e.employeeid
  AND f.timesheet_id = t.timesheet_id;

-- Remove seeded attendance rows
DELETE FROM attendance.attendance a
USING _ts_test_employees e, _ts_test_cfg c
WHERE a.employeeid = e.employeeid
  AND a.workdate BETWEEN c.period_start AND c.period_end
  AND a.notes LIKE 'E2E_FLOW_TEST:%';

-- Optionally delete test timesheet row if now empty for this scope
DELETE FROM attendance.monthly_timesheets mt
USING _ts_target_timesheet t
WHERE mt.timesheet_id = t.timesheet_id
  AND NOT EXISTS (
      SELECT 1
      FROM attendance.finalized_timesheet_records f
      WHERE f.timesheet_id = mt.timesheet_id
  )
  AND NOT EXISTS (
      SELECT 1
      FROM attendance.attendance_update_requests r
      WHERE r.timesheet_id = mt.timesheet_id
  );

COMMIT;
