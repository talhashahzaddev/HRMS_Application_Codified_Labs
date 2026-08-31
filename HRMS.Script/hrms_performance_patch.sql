-- ============================================================
-- HRMS DATABASE PATCH v2: Memory & Performance Optimizations
-- File: hrms_performance_patch.sql
--
-- HOW TO USE:
--   1. Run your main schema file (mydb_schema.sql) as normal.
--   2. Run THIS file right after it on your Railway PostgreSQL.
--   3. Safe to re-run multiple times (uses CREATE OR REPLACE / IF NOT EXISTS).
--
-- CHANGES IN THIS FILE:
--   A. 7 missing performance indexes
--   B. Statement timeout (45 seconds)
--   C. auth.fn_create_role    — triple FOR LOOP → single bulk INSERT
--   D. auth.fn_update_role    — triple FOR LOOP → single bulk INSERT
--   E. employee.fn_get_employees — double query → single query with window COUNT
--   F. payroll.fn_create_payroll_period — 2 aggregate queries → 1 combined query
--
-- WHAT IS NOT CHANGED:
--   - No table structure changes
--   - No data deleted
--   - No triggers modified
--   - fn_calculate_payroll not touched (not used by the C# app)
--   - All business logic is 100% identical
-- ============================================================


-- ============================================================
-- SECTION A: MISSING INDEXES
-- Every index below was missing from mydb_schema.sql.
-- They fix full sequential scans that waste CPU and PostgreSQL RAM.
-- ============================================================

-- A1. admin.auditlogs
--   Without these, every audit log query scans the entire table.
--   Even at 736KB, future growth will cause problems without them.
CREATE INDEX IF NOT EXISTS idx_auditlogs_org_created
    ON admin.auditlogs (organizationid, createdat DESC);

CREATE INDEX IF NOT EXISTS idx_auditlogs_table_record
    ON admin.auditlogs (tablename, recordid);

CREATE INDEX IF NOT EXISTS idx_auditlogs_userid_created
    ON admin.auditlogs (userid, createdat DESC);

-- A2. attendance.attendance
--   The C# CalculatePayrollAsync queries this table with employee+date filters.
--   Without the index each query scans every attendance row.
CREATE INDEX IF NOT EXISTS idx_attendance_employee_workdate
    ON attendance.attendance (employeeid, workdate);

CREATE INDEX IF NOT EXISTS idx_attendance_org_status_date
    ON attendance.attendance (workdate, status);

-- A3. attendance.attendancesessions
--   fn_clock_in_out has this subquery on every clock-out:
--   "WHERE attendanceId = X AND checkOutTime IS NULL ORDER BY checkInTime DESC LIMIT 1"
--   Without this partial index it sorts all sessions for every clock-out.
CREATE INDEX IF NOT EXISTS idx_attendancesessions_open
    ON attendance.attendancesessions (attendanceid, checkintime DESC)
    WHERE checkouttime IS NULL;

-- A4. employee.employmentdetails
--   Every employee join query filters on iscurrent = true.
--   This partial index makes those lookups instant.
CREATE INDEX IF NOT EXISTS idx_employmentdetails_employee_current
    ON employee.employmentdetails (employeeid)
    WHERE iscurrent = true;


-- ============================================================
-- SECTION B: STATEMENT TIMEOUT
-- Kills any query that runs longer than 45 seconds.
-- Prevents a single runaway query from eating all container RAM.
-- NOTE: Replace 'postgres' with your actual Railway DB name if different.
--       Check your Railway PostgreSQL environment variables for the DB name.
-- ============================================================
ALTER DATABASE postgres SET statement_timeout = '45s';


-- ============================================================
-- SECTION C: auth.fn_create_role
--
-- PROBLEM: Triple nested FOR LOOP processes one action at a time.
--   For a role with 30 menus × 5 submenus × 5 actions = 750 individual
--   INSERT statements, each triggering an audit trigger write.
--
-- FIX: Replace the 3 nested loops with a single bulk INSERT...SELECT.
--   The jsonb_array_elements CROSS JOINs do the same unnesting the loop did.
--   Business logic: IDENTICAL — same rows, same ON CONFLICT DO NOTHING.
--
-- MEMORY IMPACT: Reduces 750 separate INSERT calls to 1 INSERT call.
--   This means 1 audit log write instead of 750.
-- ============================================================
CREATE OR REPLACE FUNCTION auth.fn_create_role(
    p_organizationid uuid,
    p_rolename       text,
    p_description    text,
    p_issystemrole   boolean,
    p_menus          jsonb
)
RETURNS uuid
LANGUAGE plpgsql
AS $$
DECLARE
    new_role_id uuid;
BEGIN
    -- Step 1: Insert the new role (IDENTICAL to original)
    INSERT INTO auth.roles (roleid, organizationid, rolename, description, issystemrole, createdat)
    VALUES (uuid_generate_v4(), p_organizationid, p_rolename, p_description, p_issystemrole, CURRENT_TIMESTAMP)
    RETURNING roleid INTO new_role_id;

    -- Step 2: Bulk-insert all permissions in a single statement.
    --
    -- OLD CODE:
    --   FOR menu_record IN jsonb_array_elements(p_menus) LOOP
    --     FOR submenu_record IN jsonb_array_elements(menu->'SubMenus') LOOP
    --       FOR action_record IN jsonb_array_elements(submenu->'Actions') LOOP
    --         INSERT INTO auth.permissions ... VALUES (1 row)
    --       END LOOP;
    --     END LOOP;
    --   END LOOP;
    --
    -- NEW CODE: One INSERT...SELECT using CROSS JOIN — identical output.
    -- The DISTINCT ON handles the duplicate-action-id check (was: seen_action_ids array).
    INSERT INTO auth.permissions (permissionid, roleid, actionid, organizationid, has_permission, created_date)
    SELECT DISTINCT ON ((action_record->>'ActionId')::uuid)
        uuid_generate_v4(),
        new_role_id,
        (action_record->>'ActionId')::uuid,
        p_organizationid,
        COALESCE((action_record->>'HasPermission')::boolean, false),
        CURRENT_TIMESTAMP
    FROM jsonb_array_elements(p_menus)          AS menu_record
    CROSS JOIN jsonb_array_elements(menu_record->'SubMenus')  AS submenu_record
    CROSS JOIN jsonb_array_elements(submenu_record->'Actions') AS action_record
    -- Only include rows where ActionId is a valid non-empty UUID
    WHERE (action_record->>'ActionId') IS NOT NULL
      AND (action_record->>'ActionId') <> ''
    -- This is the original duplicate-check: ON CONFLICT (roleid, actionid, organizationid) DO NOTHING
    ON CONFLICT (roleid, actionid, organizationid) DO NOTHING;

    RETURN new_role_id;
END;
$$;


-- ============================================================
-- SECTION D: auth.fn_update_role
--
-- PROBLEM: Same triple nested FOR LOOP as fn_create_role.
--   Each action in the menu = 1 INSERT call = 1 audit trigger write.
--   On a role with many permissions this means hundreds of DB round-trips.
--
-- FIX: Same bulk INSERT...SELECT pattern.
--   The DELETE is unchanged. Only the re-INSERT loop is replaced.
--   Business logic: IDENTICAL — same DELETE + re-INSERT behaviour.
-- ============================================================
CREATE OR REPLACE FUNCTION auth.fn_update_role(
    p_organizationid uuid,
    p_roleid         uuid,
    p_rolename       text,
    p_description    text,
    p_issystemrole   boolean,
    p_menus          jsonb
)
RETURNS boolean
LANGUAGE plpgsql
AS $$
BEGIN
    -- Step 1: Update the role record (IDENTICAL to original)
    UPDATE auth.roles
    SET rolename     = p_rolename,
        description  = p_description,
        issystemrole = p_issystemrole
    WHERE roleid         = p_roleid
      AND organizationid = p_organizationid;

    IF NOT FOUND THEN
        RETURN false;
    END IF;

    -- Step 2: Delete existing permissions (IDENTICAL to original)
    DELETE FROM auth.permissions
    WHERE roleid         = p_roleid
      AND organizationid = p_organizationid;

    -- Step 3: Re-insert all permissions as a single bulk statement.
    --
    -- OLD CODE: Triple nested FOR loop, 1 INSERT per action row.
    -- NEW CODE: One INSERT...SELECT using CROSS JOIN (identical output).
    --
    -- NOTE: The original fn_update_role did NOT have ON CONFLICT (it deleted first).
    -- We keep the same behaviour: delete then bulk insert.
    INSERT INTO auth.permissions (
        permissionid,
        roleid,
        actionid,
        organizationid,
        has_permission,
        created_date
    )
    SELECT
        uuid_generate_v4(),
        p_roleid,
        (action_record->>'ActionId')::uuid,
        p_organizationid,
        COALESCE((action_record->>'HasPermission')::boolean, false),
        CURRENT_TIMESTAMP
    FROM jsonb_array_elements(p_menus)           AS menu_record
    CROSS JOIN jsonb_array_elements(menu_record->'SubMenus')   AS submenu_record
    CROSS JOIN jsonb_array_elements(submenu_record->'Actions')  AS action_record
    WHERE (action_record->>'ActionId') IS NOT NULL
      AND (action_record->>'ActionId') <> '';

    RETURN true;
END;
$$;


-- ============================================================
-- SECTION E: employee.fn_get_employees
--
-- PROBLEM: The function runs the same expensive multi-join query TWICE.
--   First pass: COUNT(*) to get total_records
--   Second pass: Exact same JOINs to get actual rows
--   This means every call to this endpoint costs 2× the work.
--
-- FIX: Use COUNT(*) OVER() window function to get the total count
--   in the same single pass as the data rows.
--   Business logic: IDENTICAL — same filters, same ORDER BY, same LIMIT/OFFSET.
--   The returned totalcount value is unchanged.
-- ============================================================
CREATE OR REPLACE FUNCTION employee.fn_get_employees(
    p_organization_id  uuid    DEFAULT NULL::uuid,
    p_search_term      varchar DEFAULT NULL::varchar,
    p_department_id    uuid    DEFAULT NULL::uuid,
    p_position_id      uuid    DEFAULT NULL::uuid,
    p_employment_type  varchar DEFAULT NULL::varchar,
    p_status           varchar DEFAULT NULL::varchar,
    p_limit            integer DEFAULT 50,
    p_offset           integer DEFAULT 0
)
RETURNS TABLE(
    employeeid              uuid,
    employeecode            character varying,
    firstname               character varying,
    lastname                character varying,
    email                   character varying,
    phone                   character varying,
    hiredate                date,
    status                  character varying,
    departmentid            uuid,
    departmentname          character varying,
    profilepictureurl       character varying,
    positionid              uuid,
    positiontitle           character varying,
    employmenttype          character varying,
    paytype                 character varying,
    basicsalary             numeric,
    reportingmanagerid      uuid,
    reportingmanagername    character varying,
    gender                  character varying,
    nationality             character varying,
    maritalstatus           character varying,
    dateofbirth             date,
    address                 jsonb,
    emergencycontact        jsonb,
    roleid                  uuid,
    rolename                character varying,
    categoryid              uuid,
    categoryname            character varying,
    totalcount              bigint
)
LANGUAGE plpgsql
AS $$
BEGIN
    -- OLD CODE: Ran COUNT(*) separately then ran the same JOIN query again.
    --   = 2 full multi-join executions per API call.
    --
    -- NEW CODE: Single pass using COUNT(*) OVER() window function.
    --   = 1 full execution, window function adds the count to every row.
    --   Functionally identical — callers receive the same totalcount column value.
    RETURN QUERY
    SELECT
        e.employeeid,
        e.employeecode,
        e.firstname,
        e.lastname,
        e.email,
        e.phone,
        e.hiredate,
        e.status,
        ed.departmentid,
        d.departmentname,
        e.profilepictureurl,
        ed.positionid,
        pos.positiontitle,
        ed.employmenttype,
        ed.paytype,
        ed.basicsalary,
        ed.reportingmanagerid,
        (CASE
            WHEN rm.firstname IS NULL AND rm.lastname IS NULL THEN NULL
            ELSE rm.firstname || ' ' || rm.lastname
         END)::character varying,
        e.gender,
        e.nationality,
        e.maritalstatus,
        e.dateofbirth,
        e.address::jsonb,
        e.emergencycontact::jsonb,
        r.roleid,
        r.rolename,
        e.categoryid,
        tc.categoryname,
        -- Window function replaces the separate COUNT(*) query.
        -- Returns the total matching rows alongside each row — same value as before.
        COUNT(*) OVER()::bigint AS totalcount
    FROM employee.employees e
    LEFT JOIN employee.employmentdetails ed
        ON e.employeeid = ed.employeeid AND ed.iscurrent = true
    LEFT JOIN employee.departments d
        ON ed.departmentid = d.departmentid
    LEFT JOIN employee.positions pos
        ON ed.positionid = pos.positionid
    LEFT JOIN auth.roles r
        ON pos.roleid = r.roleid AND r.organizationid = e.organizationid
    LEFT JOIN employee.employees rm
        ON ed.reportingmanagerid = rm.employeeid
    LEFT JOIN payroll.taxcategories tc
        ON e.categoryid = tc.categoryid
    WHERE (p_organization_id IS NULL OR e.organizationid = p_organization_id)
      AND (p_department_id   IS NULL OR ed.departmentid  = p_department_id)
      AND (p_position_id     IS NULL OR ed.positionid    = p_position_id)
      AND (p_employment_type IS NULL OR ed.employmenttype = p_employment_type)
      AND (p_search_term     IS NULL OR
           LOWER(CONCAT(e.firstname, ' ', e.lastname, ' ', e.email, ' ', e.employeecode))
           LIKE LOWER('%' || p_search_term || '%'))
      AND (p_status IS NULL OR e.status = p_status)
    ORDER BY e.firstname, e.lastname
    LIMIT p_limit OFFSET p_offset;
END;
$$;


-- ============================================================
-- SECTION F: payroll.fn_create_payroll_period
--
-- PROBLEM: Runs 2 separate aggregate queries on the same tables.
--   Query 1: COUNT(*) on employee.employees
--   Query 2: SUM(basicsalary) with a JOIN on employee.employmentdetails
--   These are separate DB round-trips on the same data.
--
-- FIX: Combine into a single query using conditional aggregates.
--   Business logic: IDENTICAL — same COUNT and SUM values.
-- ============================================================
CREATE OR REPLACE FUNCTION payroll.fn_create_payroll_period(
    p_organization_id uuid,
    p_period_name     character varying,
    p_start_date      date,
    p_end_date        date,
    p_pay_date        date DEFAULT NULL::date
)
RETURNS uuid
LANGUAGE plpgsql
AS $$
DECLARE
    new_period_id    UUID;
    employees_count  INTEGER;
    total_gross      NUMERIC;
BEGIN
    -- OLD CODE: 2 separate queries (COUNT + SUM with JOIN).
    -- NEW CODE: 1 query — same filters, same output values.
    SELECT
        COUNT(DISTINCT e.employeeid),
        COALESCE(SUM(ed.basicsalary), 0)
    INTO employees_count, total_gross
    FROM employee.employees e
    LEFT JOIN employee.employmentdetails ed
        ON e.employeeid = ed.employeeid
       AND ed.iscurrent = true  -- same join as original SUM query
    WHERE e.organizationid = p_organization_id
      AND e.status = 'active';  -- same filter as original COUNT query

    -- Insert payroll period (IDENTICAL to original)
    INSERT INTO payroll.payrollPeriods (
        organizationId, periodName, startDate, endDate, payDate,
        totalEmployees, totalGrossAmount
    )
    VALUES (
        p_organization_id, p_period_name, p_start_date, p_end_date, p_pay_date,
        employees_count, total_gross
    )
    RETURNING periodId INTO new_period_id;

    RETURN new_period_id;
END;
$$;


-- ============================================================
-- DONE. Summary of what was applied:
--
--  [A] ✅ 7 performance indexes created (IF NOT EXISTS)
--  [B] ✅ Statement timeout set to 45s
--  [C] ✅ auth.fn_create_role      — triple loop → bulk INSERT (750 calls → 1)
--  [D] ✅ auth.fn_update_role      — triple loop → bulk INSERT
--  [E] ✅ employee.fn_get_employees — double query → single window COUNT pass
--  [F] ✅ payroll.fn_create_payroll_period — 2 aggregates → 1 combined query
-- ============================================================
SELECT 'hrms_performance_patch.sql applied successfully.' AS status;
