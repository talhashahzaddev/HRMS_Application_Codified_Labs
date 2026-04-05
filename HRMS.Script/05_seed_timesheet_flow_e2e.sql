-- ============================================================================
-- Seed Script: Timesheet Flow E2E Test Data
-- File: HRMS.Script/05_seed_timesheet_flow_e2e.sql
-- Purpose:
-- 1) Remove noisy absent/no-record finalized rows for two target employees.
-- 2) Seed hardcoded attendance rows for a closed month.
-- 3) Seed draft/pending/rejected correction requests.
-- 4) Ensure one dedicated timesheet snapshot exists in Draft state.
--
-- Recommended period for testing: March 2026 (closed month).
-- ============================================================================

-- ----------------------------------------------------------------------------
-- PREFLIGHT (outside explicit transaction for better error visibility)
-- ----------------------------------------------------------------------------
WITH target_users AS (
    SELECT unnest(ARRAY[
        '59e11ead-c86d-43b1-8a7f-0c6a1377ca08'::uuid,
        '8801d65c-991e-40dc-b24a-7264ca28592e'::uuid
    ]) AS userid
)
SELECT
    tu.userid,
    e.employeeid,
    e.organizationid
FROM target_users tu
LEFT JOIN employee.employees e ON e.userid = tu.userid
ORDER BY tu.userid;

DO $$
DECLARE
    v_cnt int;
    v_org_cnt int;
BEGIN
    SELECT COUNT(*)
    INTO v_cnt
    FROM employee.employees e
    WHERE e.userid IN (
        '59e11ead-c86d-43b1-8a7f-0c6a1377ca08'::uuid,
        '8801d65c-991e-40dc-b24a-7264ca28592e'::uuid
    );

    IF v_cnt <> 2 THEN
        RAISE EXCEPTION 'Preflight failed: expected 2 employee mappings for target user IDs, found %.', v_cnt;
    END IF;

    SELECT COUNT(DISTINCT e.organizationid)
    INTO v_org_cnt
    FROM employee.employees e
    WHERE e.userid IN (
        '59e11ead-c86d-43b1-8a7f-0c6a1377ca08'::uuid,
        '8801d65c-991e-40dc-b24a-7264ca28592e'::uuid
    );

    IF v_org_cnt <> 1 THEN
        RAISE EXCEPTION 'Preflight failed: target users are in different organizations.';
    END IF;
END $$;

BEGIN;

-- ----------------------------------------------------------------------------
-- CONFIG
-- ----------------------------------------------------------------------------
CREATE TEMP TABLE _ts_test_cfg (
    user1_id uuid NOT NULL,
    user2_id uuid NOT NULL,
    organization_id uuid,
    period_month int NOT NULL,
    period_year int NOT NULL,
    period_start date NOT NULL,
    period_end date NOT NULL,
    timesheet_name varchar(150) NOT NULL
);

INSERT INTO _ts_test_cfg (
    user1_id,
    user2_id,
    organization_id,
    period_month,
    period_year,
    period_start,
    period_end,
    timesheet_name
)
VALUES (
    '59e11ead-c86d-43b1-8a7f-0c6a1377ca08'::uuid,
    '8801d65c-991e-40dc-b24a-7264ca28592e'::uuid,
    NULL,
    3,
    2026,
    DATE '2026-03-01',
    DATE '2026-03-31',
    'TS_E2E_FLOW_MAR_2026'
);

-- ----------------------------------------------------------------------------
-- Resolve target employees from user IDs
-- ----------------------------------------------------------------------------
CREATE TEMP TABLE _ts_test_employees AS
SELECT
    e.employeeid,
    e.userid,
    e.organizationid,
    ROW_NUMBER() OVER (ORDER BY e.employeeid) AS rn
FROM employee.employees e
JOIN _ts_test_cfg c
  ON e.userid IN (c.user1_id, c.user2_id);

DO $$
DECLARE
    v_cnt int;
    v_org_cnt int;
BEGIN
    SELECT COUNT(*) INTO v_cnt FROM _ts_test_employees;
    IF v_cnt <> 2 THEN
        RAISE EXCEPTION 'Expected 2 employee mappings from user IDs, found %.', v_cnt;
    END IF;

    SELECT COUNT(DISTINCT organizationid) INTO v_org_cnt FROM _ts_test_employees;
    IF v_org_cnt <> 1 THEN
        RAISE EXCEPTION 'Target users belong to different organizations. Use users from same organization.';
    END IF;
END $$;

UPDATE _ts_test_cfg c
SET organization_id = e.organizationid
FROM (
    SELECT organizationid
    FROM _ts_test_employees
    LIMIT 1
) e
WHERE c.organization_id IS NULL;

DO $$
DECLARE
    v_cfg_org uuid;
    v_emp_org uuid;
BEGIN
    SELECT organization_id INTO v_cfg_org FROM _ts_test_cfg LIMIT 1;
    SELECT organizationid INTO v_emp_org FROM _ts_test_employees LIMIT 1;

    IF v_cfg_org <> v_emp_org THEN
        RAISE EXCEPTION 'Configured organization_id does not match resolved employee organization.';
    END IF;
END $$;

-- ----------------------------------------------------------------------------
-- Ensure dedicated test timesheet exists
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
    uuid_generate_v4(),
    c.organization_id,
    c.timesheet_name,
    c.period_month,
    c.period_year,
    e.employeeid,
    NOW(),
    'Draft'
FROM _ts_test_cfg c
JOIN _ts_test_employees e ON e.rn = 1
WHERE NOT EXISTS (
    SELECT 1
    FROM attendance.monthly_timesheets mt
    WHERE mt.organization_id = c.organization_id
      AND mt.month = c.period_month
      AND mt.year = c.period_year
      AND mt.timesheet_name = c.timesheet_name
);

CREATE TEMP TABLE _ts_target_timesheet AS
SELECT mt.timesheet_id
FROM attendance.monthly_timesheets mt
JOIN _ts_test_cfg c
  ON mt.organization_id = c.organization_id
 AND mt.month = c.period_month
 AND mt.year = c.period_year
 AND mt.timesheet_name = c.timesheet_name
LIMIT 1;

-- Keep test snapshot open for testing each button flow.
UPDATE attendance.monthly_timesheets mt
SET status = 'Draft'
FROM _ts_target_timesheet t
WHERE mt.timesheet_id = t.timesheet_id;

-- ----------------------------------------------------------------------------
-- Cleanup old artifacts in scope (safe, restricted)
-- ----------------------------------------------------------------------------
-- Remove finalized rows only for this test timesheet and target employees.
DELETE FROM attendance.finalized_timesheet_records f
USING _ts_target_timesheet t, _ts_test_employees e
WHERE f.timesheet_id = t.timesheet_id
  AND f.employee_id = e.employeeid;

-- Remove generated "no record" finalized absences for period for target employees.
DELETE FROM attendance.finalized_timesheet_records f
USING _ts_test_cfg c, _ts_test_employees e
WHERE f.employee_id = e.employeeid
  AND f.work_date BETWEEN c.period_start AND c.period_end
  AND f.attendance_id IS NULL
  AND LOWER(COALESCE(f.final_status, 'absent')) = 'absent';

CREATE TEMP TABLE _ts_request_dates (workdate date PRIMARY KEY);
INSERT INTO _ts_request_dates (workdate) VALUES
    (DATE '2026-03-04'),
    (DATE '2026-03-06'),
    (DATE '2026-03-11'),
    (DATE '2026-03-13');

-- Delete request rows on seeded test dates for target employees (to avoid unique conflicts).
DELETE FROM attendance.attendance_update_requests r
USING _ts_test_employees e, _ts_request_dates d
WHERE r.employee_id = e.employeeid
  AND r.workdate = d.workdate;

-- Delete noisy absent rows and prior E2E rows in test month for target employees.
DELETE FROM attendance.attendance a
USING _ts_test_cfg c, _ts_test_employees e
WHERE a.employeeid = e.employeeid
  AND a.workdate BETWEEN c.period_start AND c.period_end
  AND (
      LOWER(COALESCE(a.status, 'absent')) = 'absent'
      OR a.notes LIKE 'E2E_FLOW_TEST:%'
  );

-- ----------------------------------------------------------------------------
-- Seed hardcoded attendance rows
-- ----------------------------------------------------------------------------
WITH e AS (
    SELECT
        (SELECT employeeid FROM _ts_test_employees WHERE rn = 1 LIMIT 1) AS emp1,
        (SELECT employeeid FROM _ts_test_employees WHERE rn = 2 LIMIT 1) AS emp2
), seed_rows AS (
    -- Employee 1
    SELECT e.emp1 AS employeeid, DATE '2026-03-03' AS workdate,
           TIMESTAMPTZ '2026-03-03 09:05:00+00' AS checkin,
           TIMESTAMPTZ '2026-03-03 18:00:00+00' AS checkout,
           8.92::numeric(5,2) AS totalhours, 0.92::numeric(5,2) AS overtimehours,
           'present'::varchar(20) AS status, false AS islate,
           'E2E_FLOW_TEST: emp1 baseline present'::text AS notes
    FROM e
    UNION ALL
    SELECT e.emp1, DATE '2026-03-04',
           TIMESTAMPTZ '2026-03-04 09:55:00+00',
           TIMESTAMPTZ '2026-03-04 18:35:00+00',
           8.67::numeric(5,2), 0.67::numeric(5,2),
           'late', true,
           'E2E_FLOW_TEST: emp1 late day'
    FROM e
    UNION ALL
    SELECT e.emp1, DATE '2026-03-05',
           TIMESTAMPTZ '2026-03-05 09:02:00+00',
           TIMESTAMPTZ '2026-03-05 19:00:00+00',
           9.97::numeric(5,2), 1.97::numeric(5,2),
           'present', false,
           'E2E_FLOW_TEST: emp1 overtime day'
    FROM e
    UNION ALL
    SELECT e.emp1, DATE '2026-03-06',
           TIMESTAMPTZ '2026-03-06 09:10:00+00',
           TIMESTAMPTZ '2026-03-06 17:40:00+00',
           8.50::numeric(5,2), 0.50::numeric(5,2),
           'present', false,
           'E2E_FLOW_TEST: emp1 normal day'
    FROM e

    UNION ALL

    -- Employee 2
    SELECT e.emp2, DATE '2026-03-03',
           TIMESTAMPTZ '2026-03-03 09:00:00+00',
           TIMESTAMPTZ '2026-03-03 17:30:00+00',
           8.50::numeric(5,2), 0.50::numeric(5,2),
           'present', false,
           'E2E_FLOW_TEST: emp2 baseline present'
    FROM e
    UNION ALL
    SELECT e.emp2, DATE '2026-03-04',
           TIMESTAMPTZ '2026-03-04 09:08:00+00',
           TIMESTAMPTZ '2026-03-04 17:40:00+00',
           8.53::numeric(5,2), 0.53::numeric(5,2),
           'present', false,
           'E2E_FLOW_TEST: emp2 normal day'
    FROM e
    UNION ALL
    SELECT e.emp2, DATE '2026-03-05',
           TIMESTAMPTZ '2026-03-05 09:42:00+00',
           TIMESTAMPTZ '2026-03-05 18:40:00+00',
           8.97::numeric(5,2), 0.97::numeric(5,2),
           'late', true,
           'E2E_FLOW_TEST: emp2 late day'
    FROM e
    UNION ALL
    SELECT e.emp2, DATE '2026-03-06',
           TIMESTAMPTZ '2026-03-06 09:15:00+00',
           TIMESTAMPTZ '2026-03-06 18:05:00+00',
           8.83::numeric(5,2), 0.83::numeric(5,2),
           'present', false,
           'E2E_FLOW_TEST: emp2 request rejection test day'
    FROM e
    UNION ALL
    SELECT e.emp2, DATE '2026-03-11',
           TIMESTAMPTZ '2026-03-11 09:05:00+00',
           TIMESTAMPTZ '2026-03-11 17:45:00+00',
           8.67::numeric(5,2), 0.67::numeric(5,2),
           'present', false,
           'E2E_FLOW_TEST: emp2 pending-approval test day'
    FROM e
)
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
    uuid_generate_v4(),
    s.employeeid,
    s.workdate,
    s.checkin,
    s.checkout,
    s.totalhours,
    s.overtimehours,
    s.status,
    s.notes,
    NOW(),
    NOW(),
    s.islate
FROM seed_rows s
ON CONFLICT (employeeid, workdate)
DO UPDATE SET
    checkintime = EXCLUDED.checkintime,
    checkouttime = EXCLUDED.checkouttime,
    totalhours = EXCLUDED.totalhours,
    overtimehours = EXCLUDED.overtimehours,
    status = EXCLUDED.status,
    notes = EXCLUDED.notes,
    islate = EXCLUDED.islate,
    updatedat = NOW();

-- ----------------------------------------------------------------------------
-- Seed correction requests (draft, pending, rejected)
-- ----------------------------------------------------------------------------
WITH e AS (
    SELECT
        (SELECT employeeid FROM _ts_test_employees WHERE rn = 1 LIMIT 1) AS emp1,
        (SELECT employeeid FROM _ts_test_employees WHERE rn = 2 LIMIT 1) AS emp2
), t AS (
    SELECT timesheet_id FROM _ts_target_timesheet LIMIT 1
), a AS (
    SELECT attendanceid, employeeid, workdate
    FROM attendance.attendance
    WHERE workdate IN (DATE '2026-03-04', DATE '2026-03-06', DATE '2026-03-11')
)
INSERT INTO attendance.attendance_update_requests (
    request_id,
    attendance_id,
    employee_id,
    organization_id,
    requested_checkin,
    requested_checkout,
    requested_status,
    requested_notes,
    reason_for_edit,
    status,
    submitted_at,
    actioned_by,
    actioned_at,
    rejection_reason,
    timesheet_id,
    workdate
)
SELECT
    uuid_generate_v4(),
    (SELECT attendanceid FROM a, e WHERE a.employeeid = e.emp1 AND a.workdate = DATE '2026-03-04' LIMIT 1),
    (SELECT emp1 FROM e),
    (SELECT organization_id FROM _ts_test_cfg LIMIT 1),
    TIMESTAMPTZ '2026-03-04 09:20:00+00',
    TIMESTAMPTZ '2026-03-04 18:45:00+00',
    'present',
    'E2E_FLOW_TEST: draft request on late day',
    'E2E_FLOW_TEST: employee requests correction from late to present',
    'draft',
    NOW() - INTERVAL '3 hours',
    NULL::uuid,
    NULL::timestamptz,
    NULL::text,
    (SELECT timesheet_id FROM t),
    DATE '2026-03-04'
UNION ALL
SELECT
    uuid_generate_v4(),
    NULL::uuid,
    (SELECT emp1 FROM e),
    (SELECT organization_id FROM _ts_test_cfg LIMIT 1),
    TIMESTAMPTZ '2026-03-13 09:35:00+00',
    TIMESTAMPTZ '2026-03-13 18:10:00+00',
    'present',
    'E2E_FLOW_TEST: draft absent-day correction',
    'E2E_FLOW_TEST: employee requests absent-day conversion to present',
    'draft',
    NOW() - INTERVAL '2 hours',
    NULL::uuid,
    NULL::timestamptz,
    NULL::text,
    (SELECT timesheet_id FROM t),
    DATE '2026-03-13'
UNION ALL
SELECT
    uuid_generate_v4(),
    (SELECT attendanceid FROM a, e WHERE a.employeeid = e.emp2 AND a.workdate = DATE '2026-03-11' LIMIT 1),
    (SELECT emp2 FROM e),
    (SELECT organization_id FROM _ts_test_cfg LIMIT 1),
    TIMESTAMPTZ '2026-03-11 09:05:00+00',
    TIMESTAMPTZ '2026-03-11 16:00:00+00',
    'half_day',
    'E2E_FLOW_TEST: pending manager action request',
    'E2E_FLOW_TEST: pending request for manager approve/reject testing',
    'pending',
    NOW() - INTERVAL '2 hours',
    NULL::uuid,
    NULL::timestamptz,
    NULL::text,
    (SELECT timesheet_id FROM t),
    DATE '2026-03-11'
UNION ALL
SELECT
    uuid_generate_v4(),
    (SELECT attendanceid FROM a, e WHERE a.employeeid = e.emp2 AND a.workdate = DATE '2026-03-06' LIMIT 1),
    (SELECT emp2 FROM e),
    (SELECT organization_id FROM _ts_test_cfg LIMIT 1),
    TIMESTAMPTZ '2026-03-06 10:30:00+00',
    TIMESTAMPTZ '2026-03-06 18:05:00+00',
    'present',
    'E2E_FLOW_TEST: rejected request history',
    'E2E_FLOW_TEST: rejected request history for finalized hint testing',
    'rejected',
    NOW() - INTERVAL '2 days',
    (SELECT emp1 FROM e),
    NOW() - INTERVAL '1 day',
    'E2E_FLOW_TEST: rejected due to insufficient proof',
    (SELECT timesheet_id FROM t),
    DATE '2026-03-06';

-- ----------------------------------------------------------------------------
-- Output summary for quick validation
-- ----------------------------------------------------------------------------
SELECT
    c.organization_id,
    t.timesheet_id,
    c.timesheet_name,
    c.period_month,
    c.period_year
FROM _ts_test_cfg c
CROSS JOIN _ts_target_timesheet t;

SELECT
    e.rn,
    e.userid,
    e.employeeid,
    e.organizationid
FROM _ts_test_employees e
ORDER BY e.rn;

SELECT
    a.employeeid,
    a.workdate,
    a.status,
    a.checkintime,
    a.checkouttime,
    a.totalhours,
    a.overtimehours,
    a.islate,
    a.notes
FROM attendance.attendance a
JOIN _ts_test_employees e ON e.employeeid = a.employeeid
JOIN _ts_test_cfg c ON a.workdate BETWEEN c.period_start AND c.period_end
WHERE a.notes LIKE 'E2E_FLOW_TEST:%'
ORDER BY a.employeeid, a.workdate;

SELECT
    r.employee_id,
    r.workdate,
    r.status,
    r.requested_status,
    r.rejection_reason,
    r.reason_for_edit
FROM attendance.attendance_update_requests r
JOIN _ts_test_employees e ON e.employeeid = r.employee_id
WHERE r.reason_for_edit LIKE 'E2E_FLOW_TEST:%'
ORDER BY r.employee_id, r.workdate;

COMMIT;
