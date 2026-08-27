-- ============================================================================
-- CONSOLIDATED PRD DEPLOYMENT SCRIPT  (HRMS attendance hotfixes)
-- ============================================================================
-- Applies ALL database changes required to match the backend code fixes.
-- Safe for live: every statement is CREATE OR REPLACE (idempotent — re-runnable),
-- there are NO DROP / DELETE / ALTER TABLE / TRUNCATE statements, and NO table
-- schema changes. Each function is independent, so a partial run is harmless
-- and re-running completes it.
--
-- WHAT THIS SCRIPT DEPLOYS
--   1) attendance.fn_get_attendance
--        Fixes PostgreSQL 42702 "column reference attendanceid is ambiguous"
--        on GET /api/Attendance. Qualifies every selected column so the
--        RETURNS TABLE OUT-variables no longer collide with table columns.
--   2) attendance.fn_calculate_early_departure_minutes  (NEW function)
--        Required by the clock-out "left early" message (C# calls it via
--        GetEarlyDepartureMinutesAsync). Grace resolution matches payroll's
--        late logic (shift.grace_minutes -> payroll.lateattendancerules -> 15 min).
--   3) attendance.fn_clock_in_out_geo
--        (a) Idempotent clock-in: reuses an existing OPEN session instead of
--            inserting a duplicate (fixes the uq_one_open_session 500 on
--            POST /api/GeoFence/clock).
--        (b) Timezone fix: "today" now uses Asia/Karachi (PKT) to match
--            GET /current-session, so the Clock-In / Clock-Out button state is
--            driven correctly.
--
-- CODE FIXES THAT NEED NO DB CHANGE (listed for completeness)
--   - Overtime 500 fix: pure C# manager-fallback in AttendanceService.cs
--     (no schema/FK change; a Super Admin manager is substituted when none set).
--   - Clock-in "late" message: pure C#; calls the ALREADY-EXISTING
--     fn_calculate_late_minutes (no new function needed).
--   - NOTE: early-departure is currently message-only; payroll does not yet
--     record it (finalized_timesheet_records.final_early_departure_minutes is
--     hardcoded 0). No payroll change is included here.
--
-- RUN ORDER: 1, 2, 3 (order is not strictly required; all independent).
-- ============================================================================


-- ============================================================================
-- 1) attendance.fn_get_attendance  (fix 42702 ambiguous column)
-- ============================================================================
CREATE OR REPLACE FUNCTION attendance.fn_get_attendance(
    p_search_term VARCHAR DEFAULT NULL,
    p_employee_id UUID DEFAULT NULL,
    p_organization_id UUID DEFAULT NULL,
    p_department_id UUID DEFAULT NULL,
    p_status VARCHAR DEFAULT NULL,
    p_start_date TIMESTAMPTZ DEFAULT NULL,
    p_end_date TIMESTAMPTZ DEFAULT NULL,
    p_limit INT DEFAULT 100,
    p_offset INT DEFAULT 0
) RETURNS TABLE(
    attendanceid UUID,
    employeeid UUID,
    employeename VARCHAR,
    workdate DATE,
    checkintime TIMESTAMPTZ,
    checkouttime TIMESTAMPTZ,
    location JSONB,
    checkinip VARCHAR,
    checkoutip VARCHAR,
    totalhours NUMERIC,
    overtimehours NUMERIC,
    status VARCHAR,
    sessionscount INT,
    notes VARCHAR,
    totalcount BIGINT,
    shiftid UUID
)
LANGUAGE plpgsql
AS $$
DECLARE
    totalRecords BIGINT;
BEGIN
    -- Count active employees after filters
    SELECT COUNT(*)
    INTO totalRecords
    FROM employee.employees e
    INNER JOIN employee.employmentdetails d
        ON e.employeeid = d.employeeid
    WHERE e.status = 'active'
      AND (p_organization_id IS NULL OR e.organizationid = p_organization_id)
      AND (p_department_id IS NULL OR d.departmentid = p_department_id)
      AND (p_employee_id IS NULL OR e.employeeid = p_employee_id)
      AND (
            p_search_term IS NULL
            OR LOWER(CONCAT(e.firstname,' ',e.lastname)) LIKE LOWER('%'||REPLACE(REPLACE(p_search_term,'_','__'),'%','%%')||'%')
          );

    RETURN QUERY
    SELECT
        a.attendanceid,
        e.employeeid,
        CONCAT(e.firstname,' ',e.lastname)::VARCHAR AS employeename,
        gs.workdate,
        a.checkintime,
        a.checkouttime,
        a.location,
        last_session.checkinip,
        last_session.checkoutip,
        COALESCE(a.totalhours,0)::NUMERIC AS totalhours,
        COALESCE(a.overtimehours,0)::NUMERIC AS overtimehours,
        COALESCE(a.status,'absent')::VARCHAR AS status,
        COALESCE(sess.sessionscount,0)::INT AS sessionscount,
        a.notes::VARCHAR,
        totalRecords::BIGINT AS totalcount,
        e.shiftid
    FROM
    (
        SELECT generate_series(
            COALESCE(p_start_date::date, CURRENT_DATE),
            COALESCE(p_end_date::date, CURRENT_DATE),
            interval '1 day'
        )::date AS workdate
    ) gs
    CROSS JOIN
    (
        SELECT
            emp.employeeid,
            emp.firstname,
            emp.lastname,
            emp.hiredate,
            ed.shiftid
        FROM employee.employees emp
        INNER JOIN employee.employmentdetails ed
            ON emp.employeeid = ed.employeeid
        WHERE emp.status = 'active'
          AND (p_organization_id IS NULL OR emp.organizationid = p_organization_id)
          AND (p_department_id IS NULL OR ed.departmentid = p_department_id)
          AND (p_employee_id IS NULL OR emp.employeeid = p_employee_id)
          AND (
                p_search_term IS NULL
                OR LOWER(CONCAT(emp.firstname,' ',emp.lastname)) LIKE LOWER('%'||REPLACE(REPLACE(p_search_term,'_','__'),'%','%%')||'%')
              )
    ) e
    LEFT JOIN attendance.attendance a
        ON a.employeeid = e.employeeid
       AND a.workdate = gs.workdate
    LEFT JOIN LATERAL
    (
        SELECT
            s.checkinip,
            s.checkoutip
        FROM attendance.attendancesessions s
        WHERE s.attendanceid = a.attendanceid
        ORDER BY s.checkintime DESC
        LIMIT 1
    ) last_session ON TRUE
    LEFT JOIN LATERAL
    (
        SELECT COUNT(*) AS sessionscount
        FROM attendance.attendancesessions s
        WHERE s.attendanceid = a.attendanceid
    ) sess ON TRUE
    WHERE gs.workdate >= e.hiredate
      AND (
        p_status IS NULL
        OR a.status = p_status
        OR (a.attendanceid IS NULL AND p_status = 'absent')
      )
    ORDER BY gs.workdate DESC, e.firstname
    LIMIT p_limit OFFSET p_offset;
END;
$$;


-- ============================================================================
-- 2) attendance.fn_calculate_early_departure_minutes  (NEW)
-- ============================================================================
CREATE OR REPLACE FUNCTION attendance.fn_calculate_early_departure_minutes(
    p_employee_id uuid,
    p_workdate date,
    p_checkout timestamp with time zone
) RETURNS integer
LANGUAGE plpgsql STABLE
AS $$
DECLARE
    v_shift_end   TIME;
    v_shift_grace INTEGER;
    v_org_id      UUID;
    v_grace       INTEGER;
    v_early_min   INTEGER;
    v_default_grace CONSTANT INTEGER := 15;
BEGIN
    IF p_checkout IS NULL THEN RETURN 0; END IF;

    SELECT s.endtime, NULLIF(COALESCE(s.grace_minutes, 0), 0), e.organizationid
      INTO v_shift_end, v_shift_grace, v_org_id
      FROM employee.employees e
      JOIN employee.employmentdetails ed ON ed.employeeid = e.employeeid AND ed.iscurrent = TRUE
      JOIN attendance.shifts s            ON s.shiftid     = ed.shiftid
     WHERE e.employeeid = p_employee_id
     LIMIT 1;

    IF v_shift_end IS NULL THEN RETURN 0; END IF;

    v_grace := v_shift_grace;
    IF v_grace IS NULL THEN
        SELECT graceminutes INTO v_grace
          FROM payroll.lateattendancerules
         WHERE organizationid = v_org_id
           AND COALESCE(isactive, TRUE)  = TRUE
           AND COALESCE(isdeleted, FALSE) = FALSE
         ORDER BY updatedat DESC NULLS LAST LIMIT 1;
    END IF;
    IF v_grace IS NULL THEN v_grace := v_default_grace; END IF;

    -- Early departure = minutes left before (shift_end - grace). Only positive
    -- when the employee left earlier than the tolerated early bound.
    v_early_min := GREATEST(
        FLOOR(EXTRACT(EPOCH FROM ((v_shift_end - (v_grace || ' minutes')::INTERVAL) - p_checkout::time)) / 60),
        0
    )::INTEGER;

    RETURN v_early_min;
END;
$$;


-- ============================================================================
-- 3) attendance.fn_clock_in_out_geo  (idempotent clock-in + PKT timezone)
-- ============================================================================
CREATE OR REPLACE FUNCTION attendance.fn_clock_in_out_geo(
    p_employee_id uuid,
    p_action character varying,
    p_timestamp timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    p_location jsonb DEFAULT NULL::jsonb,
    p_notes character varying DEFAULT NULL::character varying,
    p_geofenceid uuid DEFAULT NULL::uuid,
    p_latitude numeric DEFAULT NULL::numeric,
    p_longitude numeric DEFAULT NULL::numeric,
    p_face_log_id uuid DEFAULT NULL::uuid
) RETURNS TABLE(
    success boolean, attendanceid uuid, sessionid uuid,
    location_status character varying, distance_meters numeric,
    violation_id uuid, message character varying
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_today         DATE := (p_timestamp AT TIME ZONE 'Asia/Karachi')::DATE;
    v_att           attendance.attendance%ROWTYPE;
    v_dist          NUMERIC;
    v_loc_status    VARCHAR(30) := 'unknown';
    v_violation_id  UUID;
    v_new_sess_id   UUID;
    v_ip            TEXT;
    v_active_fence  UUID;
    v_message       VARCHAR(60) := 'Success';
BEGIN
    v_ip := p_location ->> 'ip';

    -- Resolve fence: explicit parameter takes priority, then shift-linked fence
    v_active_fence := p_geofenceid;
    IF v_active_fence IS NULL THEN
        SELECT sgf.geofenceid
        INTO v_active_fence
        FROM attendance.shift_geo_fences AS sgf
        JOIN employee.employmentdetails AS ed ON ed.shiftid = sgf.shiftid
        WHERE ed.employeeid = p_employee_id
          AND ed.iscurrent = TRUE
          AND sgf.isactive = TRUE
        LIMIT 1;
    END IF;

    -- Calculate distance and derive location status
    IF v_active_fence IS NOT NULL AND p_latitude IS NOT NULL AND p_longitude IS NOT NULL THEN
        v_dist := attendance.fn_distance_to_fence_meters(p_latitude, p_longitude, v_active_fence);
        IF v_dist IS NOT NULL THEN
            v_loc_status := CASE WHEN v_dist <= 0 THEN 'in_zone' ELSE 'out_of_bounds' END;
        END IF;
    END IF;

    -- Ensure today's attendance row exists
    SELECT a.*
    INTO v_att
    FROM attendance.attendance AS a
    WHERE a.employeeid = p_employee_id
      AND a.workdate = v_today
    LIMIT 1;

    IF v_att IS NULL THEN
        INSERT INTO attendance.attendance AS a
            (employeeid, workdate, location, status, notes,
             geofenceid, clock_in_latitude, clock_in_longitude,
             clock_in_location_status, clock_in_distance_m, clock_in_face_log_id)
        VALUES
            (p_employee_id, v_today, COALESCE(p_location, '{}'::jsonb), 'present', p_notes,
             v_active_fence, p_latitude, p_longitude,
             v_loc_status, v_dist, p_face_log_id)
        RETURNING a.* INTO v_att;
    END IF;

    IF LOWER(p_action) = 'in' THEN
        -- Idempotent clock-in: reuse an existing OPEN session instead of
        -- inserting a second one (which violates uq_one_open_session).
        SELECT s.sessionid
        INTO v_new_sess_id
        FROM attendance.attendancesessions AS s
        WHERE s.attendanceid = v_att.attendanceid
          AND s.checkouttime IS NULL
        ORDER BY s.checkintime DESC
        LIMIT 1;

        IF v_new_sess_id IS NULL THEN
            INSERT INTO attendance.attendancesessions AS s
                (attendanceid, checkintime, location, checkinip,
                 geofenceid, latitude, longitude, location_status, distance_from_fence_m, face_log_id)
            VALUES
                (v_att.attendanceid, p_timestamp, COALESCE(p_location, '{}'::jsonb), v_ip,
                 v_active_fence, p_latitude, p_longitude, v_loc_status, v_dist, p_face_log_id)
            RETURNING s.sessionid INTO v_new_sess_id;
            v_message := 'Clock-in successful';
        ELSE
            v_message := 'Already clocked in';
        END IF;

        UPDATE attendance.attendance AS a
        SET checkintime              = COALESCE(a.checkintime, p_timestamp),
            checkouttime             = NULL,
            status                   = 'present',
            location                 = COALESCE(p_location, a.location),
            geofenceid               = COALESCE(v_active_fence, a.geofenceid),
            clock_in_latitude        = COALESCE(p_latitude, a.clock_in_latitude),
            clock_in_longitude       = COALESCE(p_longitude, a.clock_in_longitude),
            clock_in_location_status = v_loc_status,
            clock_in_distance_m      = v_dist,
            clock_in_face_log_id     = COALESCE(p_face_log_id, a.clock_in_face_log_id),
            updatedat                = CURRENT_TIMESTAMP
        WHERE a.attendanceid = v_att.attendanceid;

        -- Log a violation if outside the fence boundary
        IF v_loc_status = 'out_of_bounds' AND v_dist IS NOT NULL THEN
            INSERT INTO attendance.geo_fence_violations AS gv
                (employeeid, geofenceid, attendanceid, face_log_id, organizationid,
                 actual_latitude, actual_longitude, distance_over_meters, action_taken)
            SELECT
                p_employee_id, v_active_fence, v_att.attendanceid, p_face_log_id,
                e.organizationid,
                p_latitude, p_longitude, v_dist, 'blocked'
            FROM employee.employees AS e
            WHERE e.employeeid = p_employee_id
            RETURNING gv.violationid INTO v_violation_id;
        END IF;

    ELSIF LOWER(p_action) = 'out' THEN
        UPDATE attendance.attendance AS a
        SET checkouttime              = p_timestamp,
            location                  = COALESCE(p_location, a.location),
            clock_out_latitude        = p_latitude,
            clock_out_longitude       = p_longitude,
            clock_out_location_status = v_loc_status,
            clock_out_distance_m      = v_dist,
            clock_out_face_log_id     = COALESCE(p_face_log_id, a.clock_out_face_log_id),
            updatedat                 = CURRENT_TIMESTAMP
        WHERE a.attendanceid = v_att.attendanceid;

        UPDATE attendance.attendancesessions AS s
        SET checkouttime = p_timestamp,
            location     = COALESCE(p_location, s.location),
            checkoutip   = v_ip,
            latitude     = COALESCE(p_latitude, s.latitude),
            longitude    = COALESCE(p_longitude, s.longitude),
            face_log_id  = COALESCE(p_face_log_id, s.face_log_id),
            updatedat    = CURRENT_TIMESTAMP
        WHERE s.sessionid = (
            SELECT s2.sessionid
            FROM attendance.attendancesessions AS s2
            WHERE s2.attendanceid = v_att.attendanceid
              AND s2.checkouttime IS NULL
            ORDER BY s2.checkintime DESC
            LIMIT 1
        )
        RETURNING s.sessionid INTO v_new_sess_id;

        -- Recalculate total hours from all sessions
        UPDATE attendance.attendance AS a
        SET totalhours = (
            SELECT COALESCE(
                SUM(EXTRACT(EPOCH FROM (s3.checkouttime - s3.checkintime))) / 3600, 0
            )
            FROM attendance.attendancesessions AS s3
            WHERE s3.attendanceid = v_att.attendanceid
              AND s3.checkouttime IS NOT NULL
        )
        WHERE a.attendanceid = v_att.attendanceid;

    ELSE
        RETURN QUERY SELECT
            FALSE, NULL::UUID, NULL::UUID, 'unknown'::VARCHAR,
            NULL::NUMERIC, NULL::UUID, 'Invalid action. Use in/out.'::VARCHAR;
        RETURN;
    END IF;

    RETURN QUERY
    SELECT
        TRUE,
        v_att.attendanceid,
        v_new_sess_id,
        v_loc_status,
        v_dist,
        v_violation_id,
        v_message::VARCHAR;

EXCEPTION
    WHEN foreign_key_violation THEN
        RAISE NOTICE 'fn_clock_in_out_geo FK violation: %', SQLERRM;
        RETURN QUERY
        SELECT
            FALSE, NULL::UUID, NULL::UUID, 'unknown'::VARCHAR,
            NULL::NUMERIC, NULL::UUID, 'Invalid employee or geofence reference.'::VARCHAR;
    WHEN unique_violation THEN
        RAISE NOTICE 'fn_clock_in_out_geo unique violation: %', SQLERRM;
        RETURN QUERY
        SELECT
            FALSE, NULL::UUID, NULL::UUID, 'unknown'::VARCHAR,
            NULL::NUMERIC, NULL::UUID, 'Duplicate clock-in detected. Please try again.'::VARCHAR;
    WHEN OTHERS THEN
        RAISE EXCEPTION 'fn_clock_in_out_geo unexpected error: %', SQLERRM;
END;
$$;


-- ============================================================================
-- VERIFICATION (read-only — confirms all three functions exist in the DB)
-- ============================================================================
SELECT
    n.nspname  AS schema,
    p.proname  AS function_name,
    pg_get_function_identity_arguments(p.oid) AS arguments
FROM pg_proc p
JOIN pg_namespace n ON n.oid = p.pronamespace
WHERE n.nspname = 'attendance'
  AND p.proname IN (
        'fn_get_attendance',
        'fn_calculate_early_departure_minutes',
        'fn_clock_in_out_geo'
      )
ORDER BY p.proname;
