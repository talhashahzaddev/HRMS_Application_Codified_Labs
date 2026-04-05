-- ============================================================================
-- Verification Script: Timesheet Flow E2E
-- File: HRMS.Script/06_verify_timesheet_flow_e2e.sql
-- Use this after each UI action to confirm behavior of submit/approve/finalize.
-- ============================================================================

-- ----------------------------------------------------------------------------
-- CONFIG (must match seed script)
-- ----------------------------------------------------------------------------
WITH cfg AS (
    SELECT
        '59e11ead-c86d-43b1-8a7f-0c6a1377ca08'::uuid AS user1_id,
        '8801d65c-991e-40dc-b24a-7264ca28592e'::uuid AS user2_id,
        3::int AS period_month,
        2026::int AS period_year,
        DATE '2026-03-01' AS period_start,
        DATE '2026-03-31' AS period_end,
        'TS_E2E_FLOW_MAR_2026'::varchar(150) AS timesheet_name
),
emp AS (
    SELECT
        e.employeeid,
        e.userid,
        e.organizationid,
        ROW_NUMBER() OVER (ORDER BY e.employeeid) AS rn
    FROM employee.employees e
    JOIN cfg c ON e.userid IN (c.user1_id, c.user2_id)
),
ts AS (
    SELECT mt.timesheet_id, mt.organization_id, mt.status, mt.timesheet_name, mt.month, mt.year
    FROM attendance.monthly_timesheets mt
    JOIN cfg c
      ON mt.timesheet_name = c.timesheet_name
     AND mt.month = c.period_month
     AND mt.year = c.period_year
     AND mt.organization_id = (SELECT organizationid FROM emp LIMIT 1)
    LIMIT 1
)
SELECT
    'employees' AS section,
    e.rn::text AS col1,
    e.userid::text AS col2,
    e.employeeid::text AS col3,
    e.organizationid::text AS col4
FROM emp e
UNION ALL
SELECT
    'timesheet' AS section,
    ts.timesheet_id::text,
    ts.status::text,
    ts.timesheet_name::text,
    (ts.month::text || '/' || ts.year::text)
FROM ts;

-- 1) Attendance rows seeded (baseline)
WITH cfg AS (
    SELECT DATE '2026-03-01' AS period_start, DATE '2026-03-31' AS period_end
),
emp AS (
    SELECT e.employeeid
    FROM employee.employees e
    WHERE e.userid IN (
        '59e11ead-c86d-43b1-8a7f-0c6a1377ca08'::uuid,
        '8801d65c-991e-40dc-b24a-7264ca28592e'::uuid
    )
)
SELECT
    a.employeeid,
    a.workdate,
    a.status,
    a.totalhours,
    a.overtimehours,
    a.islate,
    a.notes
FROM attendance.attendance a
JOIN emp e ON e.employeeid = a.employeeid
JOIN cfg c ON a.workdate BETWEEN c.period_start AND c.period_end
WHERE a.notes LIKE 'E2E_FLOW_TEST:%'
ORDER BY a.employeeid, a.workdate;

-- 2) Request state by employee/date
WITH emp AS (
    SELECT e.employeeid
    FROM employee.employees e
    WHERE e.userid IN (
        '59e11ead-c86d-43b1-8a7f-0c6a1377ca08'::uuid,
        '8801d65c-991e-40dc-b24a-7264ca28592e'::uuid
    )
)
SELECT
    r.employee_id,
    r.workdate,
    r.status,
    r.requested_status,
    r.rejection_reason,
    r.reason_for_edit,
    r.submitted_at,
    r.actioned_at
FROM attendance.attendance_update_requests r
JOIN emp e ON e.employeeid = r.employee_id
WHERE r.reason_for_edit LIKE 'E2E_FLOW_TEST:%'
ORDER BY r.employee_id, r.workdate, r.submitted_at;

-- 3) Finalized records by employee (run after each finalize action)
WITH cfg AS (
    SELECT
        3::int AS period_month,
        2026::int AS period_year,
        'TS_E2E_FLOW_MAR_2026'::varchar(150) AS timesheet_name
),
emp AS (
    SELECT e.employeeid, e.organizationid
    FROM employee.employees e
    WHERE e.userid IN (
        '59e11ead-c86d-43b1-8a7f-0c6a1377ca08'::uuid,
        '8801d65c-991e-40dc-b24a-7264ca28592e'::uuid
    )
),
ts AS (
    SELECT mt.timesheet_id, mt.status
    FROM attendance.monthly_timesheets mt
    JOIN cfg c
      ON mt.timesheet_name = c.timesheet_name
     AND mt.month = c.period_month
     AND mt.year = c.period_year
     AND mt.organization_id = (SELECT organizationid FROM emp LIMIT 1)
    LIMIT 1
)
SELECT
    ts.timesheet_id,
    ts.status AS monthly_status,
    f.employee_id,
    COUNT(*) AS finalized_rows,
    SUM(CASE WHEN f.record_status = 'overridden' THEN 1 ELSE 0 END) AS overridden_rows,
    SUM(CASE WHEN f.attendance_id IS NULL THEN 1 ELSE 0 END) AS no_record_rows
FROM attendance.finalized_timesheet_records f
JOIN ts ON ts.timesheet_id = f.timesheet_id
GROUP BY ts.timesheet_id, ts.status, f.employee_id
ORDER BY f.employee_id;

-- 4) Rejection hint source check (expected to feed BE response hints)
WITH emp AS (
    SELECT e.employeeid
    FROM employee.employees e
    WHERE e.userid IN (
        '59e11ead-c86d-43b1-8a7f-0c6a1377ca08'::uuid,
        '8801d65c-991e-40dc-b24a-7264ca28592e'::uuid
    )
)
SELECT
    x.employee_id,
    x.workdate,
    x.latest_request_status,
    x.has_rejected_history,
    x.latest_rejection_reason
FROM (
    SELECT
        e.employeeid AS employee_id,
        d.workdate,
        (
            SELECT rr.status
            FROM attendance.attendance_update_requests rr
            WHERE rr.employee_id = e.employeeid
              AND rr.workdate = d.workdate
            ORDER BY COALESCE(rr.actioned_at, rr.submitted_at) DESC, rr.submitted_at DESC, rr.request_id DESC
            LIMIT 1
        ) AS latest_request_status,
        EXISTS (
            SELECT 1
            FROM attendance.attendance_update_requests rr
            WHERE rr.employee_id = e.employeeid
              AND rr.workdate = d.workdate
              AND rr.status = 'rejected'
        ) AS has_rejected_history,
        (
            SELECT rr.rejection_reason
            FROM attendance.attendance_update_requests rr
            WHERE rr.employee_id = e.employeeid
              AND rr.workdate = d.workdate
              AND rr.status = 'rejected'
            ORDER BY COALESCE(rr.actioned_at, rr.submitted_at) DESC, rr.submitted_at DESC, rr.request_id DESC
            LIMIT 1
        ) AS latest_rejection_reason
    FROM emp e
    CROSS JOIN (VALUES
        (DATE '2026-03-04'),
        (DATE '2026-03-06'),
        (DATE '2026-03-11'),
        (DATE '2026-03-13')
    ) AS d(workdate)
) x
ORDER BY x.employee_id, x.workdate;

-- 5) Expected checks per action (read these with query outputs)
-- A. After employee "Submit Timesheet": draft -> pending for that employee requests.
-- B. After manager "Approve All" for one employee: pending should drop, approved should rise, finalized_rows should still be 0.
-- C. After employee-only finalize: only that employee finalized_rows should rise;
--    monthly_status should remain Draft/Submitted (not Finalized).
-- D. After full batch finalize: monthly_status should become Finalized for that timesheet.
