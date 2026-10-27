-- ============================================================
-- HRMS DATABASE — FUNCTIONS SCHEMA
-- Auto-organised from He.sql (pg_dump binary)
-- Schemas: admin | auth | attendance | employee |
--          helpdesk | news | payment | payroll | performance | public
-- ============================================================


-- ============================================================
-- SCHEMA: ADMIN
-- ============================================================

-- Function: admin.fn_get_dashboard_stats(uuid)
CREATE FUNCTION admin.fn_get_dashboard_stats(p_organization_id uuid) RETURNS TABLE(totalemployees bigint, activeemployees bigint, pendingleaverequests bigint, presenttoday bigint, totaldepartments bigint)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        -- Total employees
        (SELECT COUNT(*) 
         FROM employee.employees 
         WHERE organizationId = p_organization_id) AS totalEmployees,

        -- Active employees
        (SELECT COUNT(*) 
         FROM employee.employees 
         WHERE organizationId = p_organization_id 
           AND status = 'active') AS activeEmployees,

        -- Pending leave requests
        (SELECT COUNT(*) 
         FROM attendance.leaveRequests lr 
         LEFT JOIN employee.employees e 
                ON lr.employeeId = e.employeeId 
         WHERE e.organizationId = p_organization_id 
           AND lr.status = 'pending') AS pendingLeaveRequests,

        -- Employees present today
        (SELECT COUNT(*) 
         FROM attendance.attendance a 
         LEFT JOIN employee.employees e 
                ON a.employeeId = e.employeeId 
         WHERE e.organizationId = p_organization_id 
           AND a.workDate = CURRENT_DATE 
           AND a.status = 'present') AS presentToday,

        -- Active departments
        (SELECT COUNT(*) 
         FROM employee.departments 
         WHERE organizationId = p_organization_id 
           AND isActive = true) AS totalDepartments;
END;
$$;

-- Function: admin.fn_get_dashboard_stats_v2(uuid)
CREATE FUNCTION admin.fn_get_dashboard_stats_v2(p_organization_id uuid) RETURNS TABLE(totalemployees bigint, activeemployees bigint, pendingleaverequests bigint, presenttoday bigint, totaldepartments bigint, geofenceviolationstoday bigint, employeesinzone bigint)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT
        (SELECT COUNT(*) FROM employee.employees
         WHERE organizationid = p_organization_id)                          AS totalemployees,
        (SELECT COUNT(*) FROM employee.employees
         WHERE organizationid = p_organization_id AND status = 'active')    AS activeemployees,
        (SELECT COUNT(*) FROM attendance.leaverequests lr
         LEFT JOIN employee.employees e ON lr.employeeid = e.employeeid
         WHERE e.organizationid = p_organization_id
           AND lr.status = 'pending')                                       AS pendingleaverequests,
        (SELECT COUNT(*) FROM attendance.attendance a
         LEFT JOIN employee.employees e ON a.employeeid = e.employeeid
         WHERE e.organizationid = p_organization_id
           AND a.workdate = CURRENT_DATE
           AND a.status = 'present')                                        AS presenttoday,
        (SELECT COUNT(*) FROM employee.departments
         WHERE organizationid = p_organization_id AND isactive = TRUE)      AS totaldepartments,
        (SELECT COUNT(*) FROM attendance.geo_fence_violations gv
         LEFT JOIN employee.employees e ON gv.employeeid = e.employeeid
         WHERE e.organizationid = p_organization_id
           AND gv.violatedat::DATE = CURRENT_DATE)                          AS geofenceviolationstoday,
        (SELECT COUNT(*) FROM attendance.vw_team_live_stream
         WHERE organizationid = p_organization_id
           AND location_status = 'in_zone')                                 AS employeesinzone;
END;
$$;


-- ============================================================
-- SCHEMA: AUTH
-- ============================================================

-- Function: auth.fn_authenticate_user(character varying, character varying)
CREATE FUNCTION auth.fn_authenticate_user(p_email character varying, p_password_hash character varying) RETURNS TABLE(userid uuid, organizationid uuid, email character varying, firstname character varying, lastname character varying, rolename character varying, permissions jsonb, organizationname character varying, domain character varying, isactive boolean)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        u.userId,
        u.organizationId,
        u.email,
        u.firstName,
        u.lastName,
        r.roleName,
        r.permissions,
        o.name AS organizationName,
		o.domain,
        u.isActive
    FROM auth.users u
    INNER JOIN auth.roles r ON u.roleId = r.roleId
    INNER JOIN auth.organizations o ON u.organizationId = o.organizationId
    WHERE u.email = p_email 
      AND u.passwordHash = p_password_hash 
      AND u.isActive = true
      AND o.status = 'active';
END;
$$;

-- Function: auth.fn_create_organization(character varying, character varying, character varying, character varying)
CREATE FUNCTION auth.fn_create_organization(p_name character varying, p_domain character varying DEFAULT NULL::character varying, p_subscription_type character varying DEFAULT 'basic'::character varying, p_currency character varying DEFAULT NULL::character varying) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    newOrgId UUID;
    superAdminRoleId UUID;
    permissionsCount INTEGER;
BEGIN
    -- Insert new organization
    INSERT INTO auth.organizations (name, domain, subscriptionType, currency)
    VALUES (p_name, p_domain, p_subscription_type, p_currency)
    RETURNING organizationId INTO newOrgId;
    
    -- Create ONLY Super Admin role for the organization
    INSERT INTO auth.roles (organizationId, roleName, description, isSystemRole)
    VALUES (
        newOrgId, 
        'Super Admin', 
        'System administrator with full access to all features', 
        false
    )
    RETURNING roleId INTO superAdminRoleId;
    
    -- Bulk insert all permissions for Super Admin (NO LOOP
    INSERT INTO auth.permissions (
        roleId, 
        actionId, 
        organizationId, 
        has_permission,
        created_date,
        updated_date
    )
    SELECT 
        superAdminRoleId,
        a.actionId,
        newOrgId,
        true,  -- Super Admin gets all permissions
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP
    FROM auth.actions a
	WHERE a.actionname != 'Employee Dashboard';
	
    -- Get count of inserted permissions for logging
    GET DIAGNOSTICS permissionsCount = ROW_COUNT;
    
    -- Log the creation
    RAISE NOTICE 'Organization created with ID: %, Super Admin role created with % permissions', 
        newOrgId, permissionsCount;

	INSERT INTO payment.companysubscriptions (
		companyid,
		planid,
		status,
		billingcycle,
		startdate, --timestamp without time zone
		enddate --timestamp without time zone
	)
	VALUES (
		newOrgId,
		'620212ec-cf42-4365-a4c1-f9c92c4250b9',
		'active',
		'monthly',
		now(),
		now() + interval '1 month'
	);
	
    RETURN newOrgId;
END;
$$;

-- Function: auth.fn_create_role(uuid, text, text, boolean, jsonb)
CREATE FUNCTION auth.fn_create_role(p_organizationid uuid, p_rolename text, p_description text, p_issystemrole boolean, p_menus jsonb) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    new_role_id uuid;
    menu_record jsonb;
    submenu_record jsonb;
    action_record jsonb;
    menu_id uuid;
    submenu_id uuid;
    action_id uuid;
    v_haspermission boolean;
BEGIN
    -- Insert into auth.roles
    INSERT INTO auth.roles (
        roleid,
        organizationid,
        rolename,
        description,
        issystemrole,
        createdat
    )
    VALUES (
        uuid_generate_v4(),
        p_organizationid,
        p_rolename,
        p_description,
        p_issystemrole,
        CURRENT_TIMESTAMP
    )
    RETURNING roleid INTO new_role_id;

    -- Loop through menus
    FOR menu_record IN SELECT * FROM jsonb_array_elements(p_menus)
    LOOP
        menu_id := (menu_record->>'MenuId')::uuid;

        -- Loop through submenus in each menu
        FOR submenu_record IN SELECT * FROM jsonb_array_elements(menu_record->'SubMenus')
        LOOP
            submenu_id := (submenu_record->>'SubMenuId')::uuid;

            -- Loop through actions in each submenu
            FOR action_record IN SELECT * FROM jsonb_array_elements(submenu_record->'Actions')
            LOOP
                action_id := (action_record->>'ActionId')::uuid;
                v_haspermission := COALESCE((action_record->>'HasPermission')::boolean, false);

                -- Insert permissions into auth.permissions (UPDATED TABLE STRUCTURE)
                INSERT INTO auth.permissions (
                    permissionid,
                    roleid,
                    actionid,        -- Changed from menuid/submenuid
                    organizationid,
                    has_permission,   -- Changed from canview/canadd/canedit/candelete
                    created_date
                )
                VALUES (
                    uuid_generate_v4(),
                    new_role_id,
                    action_id,        -- Now storing actionid directly
                    p_organizationid,
                    v_haspermission,   -- Single permission flag
                    CURRENT_TIMESTAMP
                );
            END LOOP;
        END LOOP;
    END LOOP;

    RETURN new_role_id;
END;
$$;

-- Function: auth.fn_create_user(uuid, character varying, character varying, character varying, character varying, character varying)
CREATE FUNCTION auth.fn_create_user(p_organization_id uuid, p_email character varying, p_password_hash character varying, p_first_name character varying, p_last_name character varying, p_role_name character varying DEFAULT 'Employee'::character varying) RETURNS TABLE(user_id uuid, role_id uuid)
    LANGUAGE plpgsql
    AS $$
DECLARE
    newUserId UUID;
    roleIdVar UUID;
BEGIN
    -- Get role ID
    SELECT roleId INTO roleIdVar 
    FROM auth.roles 
    WHERE organizationId = p_organization_id AND roleName = p_role_name;
    
    IF roleIdVar IS NULL THEN
        RAISE EXCEPTION 'Role % not found for organization', p_role_name;
    END IF;
    
    -- Insert the user
    INSERT INTO auth.users (organizationId, email, passwordHash, firstName, lastName, roleId)
    VALUES (p_organization_id, p_email, p_password_hash, p_first_name, p_last_name, roleIdVar)
    RETURNING userId INTO newUserId;
    
    -- Return both values as a query result
    RETURN QUERY SELECT newUserId, roleIdVar;
END;
$$;

-- Function: auth.fn_create_user(uuid, character varying, character varying, character varying, character varying, character varying, boolean, character varying, timestamp with time zone)
CREATE FUNCTION auth.fn_create_user(p_organization_id uuid, p_email character varying, p_password_hash character varying, p_first_name character varying, p_last_name character varying, p_role_name character varying DEFAULT 'Employee'::character varying, p_email_verify boolean DEFAULT NULL::boolean, p_email_verify_token character varying DEFAULT NULL::character varying, p_email_verify_expiry timestamp with time zone DEFAULT NULL::timestamp with time zone) RETURNS TABLE(user_id uuid, role_id uuid)
    LANGUAGE plpgsql
    AS $$
DECLARE
    newUserId UUID;
    roleIdVar UUID;
BEGIN
    -- Get role ID
    SELECT roleId INTO roleIdVar 
    FROM auth.roles 
    WHERE organizationId = p_organization_id AND roleName = p_role_name;
    
    IF roleIdVar IS NULL THEN
        RAISE EXCEPTION 'Role % not found for organization', p_role_name;
    END IF;
    
    -- Insert the user
    INSERT INTO auth.users (organizationId, email, passwordHash, firstName, lastName, roleId, emailVerify, emailVerifyToken, emailVerifyExpiry)
    VALUES (p_organization_id, p_email, p_password_hash, p_first_name, p_last_name, roleIdVar, p_email_verify, p_email_verify_token, p_email_verify_expiry)
    RETURNING userId INTO newUserId;
    
    -- Return both values as a query result
    RETURN QUERY SELECT newUserId, roleIdVar;
END;
$$;

-- Function: auth.fn_delete_role(uuid, uuid)
CREATE FUNCTION auth.fn_delete_role(p_organizationid uuid, p_roleid uuid) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_user_count integer;
    v_role_name text;
    v_is_system_role boolean;
BEGIN
    -- Get role details
    SELECT rolename, issystemrole INTO v_role_name, v_is_system_role
    FROM auth.roles 
    WHERE roleid = p_roleid 
      AND (organizationid = p_organizationid OR (organizationid IS NULL AND p_organizationid IS NULL));

    -- Check if role exists
    IF NOT FOUND THEN
        RETURN false;
    END IF;

    -- Check if role is assigned to any users
    SELECT COUNT(*) INTO v_user_count
    FROM auth.users
    WHERE roleid = p_roleid;

    IF v_user_count > 0 THEN
        RAISE EXCEPTION 'Role is assigned to % user(s) and cannot be deleted', v_user_count;
    END IF;

    -- Delete permissions first (due to foreign key constraints)
    DELETE FROM auth.permissions
    WHERE roleid = p_roleid;

    -- Delete the role
    DELETE FROM auth.roles
    WHERE roleid = p_roleid;

    RETURN true;
END;
$$;

-- Function: auth.fn_update_role(uuid, uuid, text, text, boolean, jsonb)
CREATE FUNCTION auth.fn_update_role(p_organizationid uuid, p_roleid uuid, p_rolename text, p_description text, p_issystemrole boolean, p_menus jsonb) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    menu_record jsonb;
    submenu_record jsonb;
    action_record jsonb;
    menu_id uuid;
    submenu_id uuid;
    action_id uuid;
    v_haspermission boolean;
BEGIN
    -- Update role in auth.roles
    UPDATE auth.roles
    SET 
        rolename = p_rolename,
        description = p_description,
        issystemrole = p_issystemrole
    WHERE roleid = p_roleid 
      AND (organizationid = p_organizationid OR (organizationid IS NULL AND p_organizationid IS NULL));

    -- Check if role was updated
    IF NOT FOUND THEN
        RETURN false;
    END IF;

    -- Delete existing permissions for this role
    DELETE FROM auth.permissions
    WHERE roleid = p_roleid;

    -- Insert new permissions from the menu structure
    FOR menu_record IN SELECT * FROM jsonb_array_elements(p_menus)
    LOOP
        menu_id := (menu_record->>'MenuId')::uuid;

        FOR submenu_record IN SELECT * FROM jsonb_array_elements(menu_record->'SubMenus')
        LOOP
            submenu_id := (submenu_record->>'SubMenuId')::uuid;

            FOR action_record IN SELECT * FROM jsonb_array_elements(submenu_record->'Actions')
            LOOP
                action_id := (action_record->>'ActionId')::uuid;
                v_haspermission := COALESCE((action_record->>'HasPermission')::boolean, false);

                -- Insert permissions into auth.permissions
                INSERT INTO auth.permissions (
                    permissionid,
                    roleid,
                    actionid,
                    organizationid,
                    has_permission,
                    created_date
                )
                VALUES (
                    uuid_generate_v4(),
                    p_roleid,
                    action_id,
                    p_organizationid,
                    v_haspermission,
                    CURRENT_TIMESTAMP
                );
            END LOOP;
        END LOOP;
    END LOOP;

    RETURN true;
END;
$$;

-- Function: auth.fn_verify_email(character varying, timestamp with time zone)
CREATE FUNCTION auth.fn_verify_email(p_token character varying, p_verify_time timestamp with time zone) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_rows_affected INTEGER;
BEGIN
    UPDATE auth.users
    SET
        emailverified = TRUE,
        emailVerify = TRUE,
        emailVerifyToken = NULL,
        emailVerifyExpiry = NULL,
        updatedat = CURRENT_TIMESTAMP
    WHERE emailVerifyToken = p_token
      AND emailVerifyExpiry > p_verify_time
      AND emailVerify = FALSE;

    GET DIAGNOSTICS v_rows_affected = ROW_COUNT;

    IF v_rows_affected > 0 THEN
        RETURN TRUE;
    END IF;

    RETURN FALSE;
END;
$$;


-- ============================================================
-- SCHEMA: EMPLOYEE
-- ============================================================

-- Function: employee.fn_create_employee(uuid, uuid, character varying, character varying, character varying, character varying, character varying, timestamp with time zone, timestamp with time zone, uuid, uuid, numeric, jsonb, character varying, uuid, character varying, character varying, character varying, character varying, jsonb)

CREATE OR REPLACE FUNCTION employee.fn_create_employee(
	p_organization_id uuid,
	p_user_id uuid,
	p_employee_code character varying,
	p_first_name character varying,
	p_last_name character varying,
	p_email character varying,
	p_phone character varying,
	p_date_of_birth timestamp with time zone,
	p_hire_date timestamp with time zone,
	p_department_id uuid,
	p_position_id uuid,
	p_basic_salary numeric,
	p_address jsonb,
	p_work_location character varying DEFAULT NULL::character varying,
	p_reporting_manager_id uuid DEFAULT NULL::uuid,
	p_employment_type character varying DEFAULT NULL::character varying,
	p_nationality character varying DEFAULT NULL::character varying,
	p_gender character varying DEFAULT NULL::character varying,
	p_marital_status character varying DEFAULT NULL::character varying,
	p_emergency_contact jsonb DEFAULT '{}'::jsonb)
    RETURNS uuid
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
DECLARE
    v_employee_id UUID;
BEGIN
    -- Insert into employees table
    INSERT INTO employee.employees (
        organizationid,
        userid,
        employeecode,
        firstname,
        lastname,
        email,
        phone,
        address,
        dateofbirth,
        hiredate,
        nationality,
        gender,
        maritalstatus,
        emergencycontact
    )
    VALUES (
        p_organization_id,
        p_user_id,
        p_employee_code,
        p_first_name,
        p_last_name,
        p_email,
        p_phone,
        COALESCE(p_address, '{}'::jsonb),
        p_date_of_birth::date,
        p_hire_date::date,
        p_nationality,
        p_gender,
        p_marital_status,
        COALESCE(p_emergency_contact, '{}'::jsonb)
    )
    RETURNING employeeid INTO v_employee_id;

    -- Insert into employmentdetails table
    INSERT INTO employee.employmentdetails (
        employeeid,
        departmentid,
        positionid,
        basicsalary,
        worklocation,
        reportingmanagerid,
		employmenttype,
        effectivedate,
        iscurrent
    )
    VALUES (
        v_employee_id,
        p_department_id,
        p_position_id,
        p_basic_salary,
        p_work_location,
        p_reporting_manager_id,
	    p_employment_type,
        p_hire_date::date,
        true
    );

    RETURN v_employee_id;
END;
$BODY$;

-- Function: employee.fn_get_employees(uuid, character varying, uuid, uuid, character varying, character varying, integer, integer)

CREATE OR REPLACE FUNCTION employee.fn_get_employees(
	p_organization_id uuid DEFAULT NULL::uuid,
	p_search_term character varying DEFAULT NULL::character varying,
	p_department_id uuid DEFAULT NULL::uuid,
	p_position_id uuid DEFAULT NULL::uuid,
	p_employment_type character varying DEFAULT NULL::character varying,
	p_status character varying DEFAULT NULL::character varying,
	p_limit integer DEFAULT 50,
	p_offset integer DEFAULT 0)
    RETURNS TABLE(employeeid uuid, employeecode character varying, firstname character varying, lastname character varying, email character varying, phone character varying, hiredate date, status character varying, departmentid uuid, departmentname character varying, profilepictureurl character varying, positionid uuid, positiontitle character varying, employmenttype character varying, paytype character varying, basicsalary numeric, reportingmanagerid uuid, reportingmanagername character varying, gender character varying, nationality character varying, maritalstatus character varying, dateofbirth date, address jsonb, emergencycontact jsonb, roleid uuid, rolename character varying, totalcount bigint) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
DECLARE
    total_records BIGINT;
BEGIN

    SELECT COUNT(*) INTO total_records
    FROM employee.employees e
    LEFT JOIN employee.employmentdetails ed 
        ON e.employeeid = ed.employeeid AND ed.iscurrent = true
    LEFT JOIN employee.positions pos
        ON ed.positionid = pos.positionid
    WHERE (p_organization_id IS NULL OR e.organizationid = p_organization_id)
      AND (p_department_id IS NULL OR ed.departmentid = p_department_id)
      AND (p_position_id IS NULL OR ed.positionid = p_position_id)
      AND (p_employment_type IS NULL OR ed.employmenttype = p_employment_type)
      AND (p_search_term IS NULL OR 
           LOWER(CONCAT(e.firstname, ' ', e.lastname, ' ', e.email, ' ', e.employeecode)) 
           LIKE LOWER('%' || p_search_term || '%'))
      AND (p_status IS NULL OR e.status = p_status);

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
        r.roleid,            -- ✅ from positions
        r.rolename,          -- ✅ from auth.roles
        total_records
    FROM employee.employees e
    LEFT JOIN employee.employmentdetails ed 
        ON e.employeeid = ed.employeeid AND ed.iscurrent = true
    LEFT JOIN employee.departments d 
        ON ed.departmentid = d.departmentid
    LEFT JOIN employee.positions pos 
        ON ed.positionid = pos.positionid
    LEFT JOIN auth.roles r
        ON pos.roleid = r.roleid   -- ✅ CORRECT JOIN
    LEFT JOIN employee.employees rm
        ON ed.reportingmanagerid = rm.employeeid
    WHERE (p_organization_id IS NULL OR e.organizationid = p_organization_id)
      AND (p_department_id IS NULL OR ed.departmentid = p_department_id)
      AND (p_position_id IS NULL OR ed.positionid = p_position_id)
      AND (p_employment_type IS NULL OR ed.employmenttype = p_employment_type)
      AND (p_search_term IS NULL OR 
           LOWER(CONCAT(e.firstname, ' ', e.lastname, ' ', e.email, ' ', e.employeecode)) 
           LIKE LOWER('%' || p_search_term || '%'))
      AND (p_status IS NULL OR e.status = p_status)
    ORDER BY e.firstname, e.lastname
    LIMIT p_limit OFFSET p_offset;

END;
$BODY$;


-- ============================================================
-- SCHEMA: ATTENDANCE
-- ============================================================

-- Function: attendance.fn_approve_all_pending_requests(uuid, uuid, uuid, uuid)
CREATE FUNCTION attendance.fn_approve_all_pending_requests(p_manager_id uuid, p_org_id uuid, p_timesheet_id uuid, p_employee_id uuid DEFAULT NULL::uuid) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_count  integer := 0;
    v_month  integer;
    v_year   integer;
BEGIN
    SELECT month, year INTO v_month, v_year
    FROM attendance.monthly_timesheets
    WHERE timesheet_id = p_timesheet_id;

    -- Step 1: Mark requests as approved
    WITH approved_requests AS (
        UPDATE attendance.attendance_update_requests
        SET status       = 'approved',
            actioned_by  = p_manager_id,
            actioned_at  = CURRENT_TIMESTAMP,
            timesheet_id = p_timesheet_id
        WHERE organization_id = p_org_id
          AND status           = 'pending'
          AND (p_employee_id IS NULL OR employee_id = p_employee_id)
          AND (
              timesheet_id = p_timesheet_id
              OR (timesheet_id IS NULL
                  AND EXTRACT(MONTH FROM workdate) = v_month
                  AND EXTRACT(YEAR  FROM workdate) = v_year)
          )
        RETURNING
            request_id, employee_id, organization_id,
            attendance_id, workdate,
            requested_checkin, requested_checkout, requested_status
    ),
    -- Step 2: Apply the approved corrections to the attendance table
    --         so the view immediately reflects the corrected times/status.
    --         (finalized_timesheet_records is intentionally NOT touched here —
    --          that is the manager's explicit "Finalize" action.)
    updated_attendance AS (
        UPDATE attendance.attendance a
        SET checkintime  = ar.requested_checkin,
            checkouttime = ar.requested_checkout,
            status       = ar.requested_status,
            updatedat    = CURRENT_TIMESTAMP
        FROM approved_requests ar
        WHERE a.attendanceid = ar.attendance_id
          AND ar.attendance_id IS NOT NULL
    )
    SELECT COUNT(*) INTO v_count FROM approved_requests;

    RETURN v_count;
END;
$$;

-- Function: attendance.fn_batch_finalize_month(uuid, uuid, integer, integer)
CREATE FUNCTION attendance.fn_batch_finalize_month(p_timesheet_id uuid, p_org_id uuid, p_month integer, p_year integer) RETURNS TABLE(finalized_count integer, skipped_count integer)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_start_date  DATE := make_date(p_year, p_month, 1);
    v_end_date    DATE := (make_date(p_year, p_month, 1) + INTERVAL '1 month - 1 day')::DATE;
    v_cutoff_date DATE := LEAST(v_end_date, CURRENT_DATE - 1);
    v_fin_count   INT  := 0;
    v_skip_count  INT  := 0;
BEGIN
    -- Step 1: Finalize days that have an actual attendance record
    WITH upserted AS (
        INSERT INTO attendance.finalized_timesheet_records
            (record_id, timesheet_id, employee_id, organization_id,
             attendance_id, work_date, record_status, approved_at)
        SELECT
            gen_random_uuid(),
            p_timesheet_id,
            a.employeeid,
            p_org_id,
            a.attendanceid,
            a.workdate,
            CASE WHEN ftr_ex.record_status = 'overridden' THEN 'overridden' ELSE 'finalized' END,
            NOW()
        FROM attendance.attendance a
        JOIN employee.employees e
            ON e.employeeid = a.employeeid
           AND e.organizationid = p_org_id
        LEFT JOIN attendance.finalized_timesheet_records ftr_ex
            ON ftr_ex.attendance_id = a.attendanceid
        WHERE a.workdate >= v_start_date
          AND a.workdate <= v_end_date
        ON CONFLICT (attendance_id) WHERE attendance_id IS NOT NULL
        DO UPDATE SET
            record_status = CASE
                WHEN attendance.finalized_timesheet_records.record_status = 'overridden' THEN 'overridden'
                ELSE 'finalized'
            END,
            approved_at = NOW()
        RETURNING 1
    )
    SELECT COUNT(*) INTO v_fin_count FROM upserted;

    -- Step 2: Finalize truly absent days (no attendance row, not weekend, elapsed only)
    WITH absent_upserted AS (
        INSERT INTO attendance.finalized_timesheet_records
            (record_id, timesheet_id, employee_id, organization_id,
             attendance_id, work_date, record_status, approved_at)
        SELECT
            gen_random_uuid(),
            p_timesheet_id,
            e.employeeid,
            p_org_id,
            NULL,
            gs.work_day::date,
            'finalized',
            NOW()
        FROM employee.employees e
        CROSS JOIN generate_series(v_start_date, v_cutoff_date, '1 day'::interval) AS gs(work_day)
        WHERE e.organizationid = p_org_id
          AND e.status        != 'deleted'
          AND EXTRACT(DOW FROM gs.work_day) NOT IN (0, 6)
          AND NOT EXISTS (
              SELECT 1 FROM attendance.attendance a
              WHERE a.employeeid = e.employeeid
                AND a.workdate   = gs.work_day::date
          )
        ON CONFLICT (employee_id, work_date) WHERE attendance_id IS NULL
        DO NOTHING
        RETURNING 1
    )
    SELECT v_fin_count + COUNT(*) INTO v_fin_count FROM absent_upserted;

    RETURN QUERY SELECT v_fin_count, v_skip_count;
END;
$$;

-- Function: attendance.fn_clock_in_out(uuid, character varying, timestamp with time zone, jsonb, character varying)
CREATE FUNCTION attendance.fn_clock_in_out(p_employee_id uuid, p_action character varying, p_timestamp timestamp with time zone DEFAULT CURRENT_TIMESTAMP, p_location jsonb DEFAULT NULL::jsonb, p_notes character varying DEFAULT NULL::character varying) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    -- GLOBAL FIX: Normalize to UTC BEFORE extracting date
    todayDate DATE := (p_timestamp AT TIME ZONE 'UTC')::DATE;

    attendanceRecord RECORD;
	v_ip TEXT;

BEGIN
	    v_ip := p_location ->> 'ip';
    -- Step 1: Ensure daily attendance record exists
    SELECT * INTO attendanceRecord 
    FROM attendance.Attendance 
    WHERE employeeId = p_employee_id AND workDate = todayDate;

    IF attendanceRecord IS NULL THEN
        INSERT INTO attendance.Attendance (employeeId, workDate, location, status, notes)
        VALUES (p_employee_id, todayDate, COALESCE(p_location, '{}'), 'present', p_notes)
        RETURNING * INTO attendanceRecord;
    END IF;

    -- Step 2: Handle Check-In
    IF p_action = 'in' THEN
        UPDATE attendance.Attendance 
        SET checkInTime  = p_timestamp,
            checkOutTime = NULL,
            status       = 'present',
            location     = COALESCE(p_location, location),
            updatedAt    = CURRENT_TIMESTAMP
        WHERE attendanceId = attendanceRecord.attendanceId;

        INSERT INTO attendance.AttendanceSessions (attendanceId, checkInTime, location, checkinip)
			VALUES (
			    attendanceRecord.attendanceId, 
			    p_timestamp, 
			    COALESCE(p_location, '{}'),
			    v_ip
			);

    -- Step 3: Handle Check-Out
    ELSIF p_action = 'out' THEN
        UPDATE attendance.Attendance 
        SET checkOutTime = p_timestamp,
            location     = COALESCE(p_location, location),
            notes        = COALESCE(p_notes, notes),
            updatedAt    = CURRENT_TIMESTAMP
        WHERE attendanceId = attendanceRecord.attendanceId;

        UPDATE attendance.AttendanceSessions
			SET checkOutTime = p_timestamp,
			    location = COALESCE(p_location, location),
			    checkoutip = v_ip,
			    updatedAt = CURRENT_TIMESTAMP

        WHERE sessionId = (
            SELECT sessionId
            FROM attendance.AttendanceSessions
            WHERE attendanceId = attendanceRecord.attendanceId
              AND checkOutTime IS NULL
            ORDER BY checkInTime DESC
            LIMIT 1
        );

        UPDATE attendance.Attendance
        SET totalHours = (
            SELECT COALESCE(SUM(EXTRACT(EPOCH FROM (checkOutTime - checkInTime))) / 3600, 0)
            FROM attendance.AttendanceSessions
            WHERE attendanceId = attendanceRecord.attendanceId
              AND checkOutTime IS NOT NULL
        ),
        updatedAt = CURRENT_TIMESTAMP
        WHERE attendanceId = attendanceRecord.attendanceId;
    END IF;

    RETURN TRUE;
END;
$$;

-- Function: attendance.fn_clock_in_out_geo(uuid, character varying, timestamp with time zone, jsonb, character varying, uuid, numeric, numeric, uuid)
CREATE FUNCTION attendance.fn_clock_in_out_geo(p_employee_id uuid, p_action character varying, p_timestamp timestamp with time zone DEFAULT CURRENT_TIMESTAMP, p_location jsonb DEFAULT NULL::jsonb, p_notes character varying DEFAULT NULL::character varying, p_geofenceid uuid DEFAULT NULL::uuid, p_latitude numeric DEFAULT NULL::numeric, p_longitude numeric DEFAULT NULL::numeric, p_face_log_id uuid DEFAULT NULL::uuid) RETURNS TABLE(success boolean, attendanceid uuid, sessionid uuid, location_status character varying, distance_meters numeric, violation_id uuid, message character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_today         DATE := (p_timestamp AT TIME ZONE 'UTC')::DATE;
    v_att           attendance.attendance%ROWTYPE;
    v_dist          NUMERIC;
    v_loc_status    VARCHAR(30) := 'unknown';
    v_violation_id  UUID;
    v_new_sess_id   UUID;
    v_ip            TEXT;
    v_active_fence  UUID;
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
        UPDATE attendance.attendance AS a
        SET checkintime              = p_timestamp,
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

        INSERT INTO attendance.attendancesessions AS s
            (attendanceid, checkintime, location, checkinip,
             geofenceid, latitude, longitude, location_status, distance_from_fence_m, face_log_id)
        VALUES
            (v_att.attendanceid, p_timestamp, COALESCE(p_location, '{}'::jsonb), v_ip,
             v_active_fence, p_latitude, p_longitude, v_loc_status, v_dist, p_face_log_id)
        RETURNING s.sessionid INTO v_new_sess_id;

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
        'Success'::VARCHAR;

EXCEPTION WHEN OTHERS THEN
    RETURN QUERY
    SELECT
        FALSE, NULL::UUID, NULL::UUID, 'unknown'::VARCHAR,
        NULL::NUMERIC, NULL::UUID, SQLERRM::VARCHAR;
END;
$$;

-- Function: attendance.fn_create_leave_request(uuid, uuid, timestamp with time zone, timestamp with time zone, text)
CREATE FUNCTION attendance.fn_create_leave_request(p_employee_id uuid, p_leave_type_id uuid, p_start_date timestamp with time zone, p_end_date timestamp with time zone, p_reason text DEFAULT NULL::text) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    new_request_id UUID;
    days_requested DECIMAL(5,2);
    available_days DECIMAL(5,2);
    current_year INTEGER := EXTRACT(YEAR FROM p_start_date);
	shift_days int[];
BEGIN
  -- Calculate working days between start and end date
    SELECT s.daysofweek
    INTO shift_days
    FROM employee.employmentdetails ed
    JOIN attendance.shifts s ON s.shiftid = ed.shiftid
    WHERE ed.employeeid = p_employee_id
      AND ed.iscurrent = true
    LIMIT 1;
    IF shift_days IS NULL THEN
        RAISE EXCEPTION 'Shift not found for employee %', p_employee_id;
    END IF;
    SELECT COUNT(*)
    INTO days_requested
    FROM generate_series(p_start_date::date, p_end_date::date, '1 day') AS d
    WHERE EXTRACT(dow FROM d)::int = ANY (shift_days);

    -- Check available leave balance
    SELECT remainingDays INTO available_days
    FROM attendance.leaveentitlements 
    WHERE employeeId = p_employee_id 
      AND leaveTypeId = p_leave_type_id 
      AND year = current_year;
      
    IF available_days < days_requested THEN
        RAISE EXCEPTION 'Insufficient leave balance. Available: %, Requested: %', available_days, days_requested;
    END IF;
    
    -- Create leave request
    INSERT INTO attendance.leaverequests (
        employeeId, leaveTypeId, startDate, endDate, 
        daysRequested, reason
    )
    VALUES (
        p_employee_id, p_leave_type_id, p_start_date, p_end_date,
        days_requested, p_reason
    )
    RETURNING requestId INTO new_request_id;
    
    RETURN new_request_id;
END;
$$;

-- Function: attendance.fn_distance_to_fence_meters(numeric, numeric, uuid)
CREATE FUNCTION attendance.fn_distance_to_fence_meters(p_latitude numeric, p_longitude numeric, p_geofenceid uuid) RETURNS numeric
    LANGUAGE plpgsql STABLE
    AS $$
DECLARE
    v_fence   attendance.geo_fences%ROWTYPE;
    v_dist_m  NUMERIC;
BEGIN
    SELECT * INTO v_fence
    FROM attendance.geo_fences
    WHERE geofenceid = p_geofenceid
      AND deleted_at IS NULL;

    IF NOT FOUND OR v_fence.shape != 'circle' THEN
        RETURN NULL; -- polygon handled in app layer
    END IF;

    v_dist_m := earth_distance(
        ll_to_earth(v_fence.center_latitude::float8, v_fence.center_longitude::float8),
        ll_to_earth(p_latitude::float8, p_longitude::float8)
    );

    -- Negative = inside boundary, Positive = outside
    RETURN ROUND((v_dist_m - v_fence.radius_meters)::NUMERIC, 2);
END;
$$;

-- Function: attendance.fn_finalize_attendance_record(uuid, uuid, boolean, text)
CREATE FUNCTION attendance.fn_finalize_attendance_record(p_request_id uuid, p_actioned_by uuid, p_is_approved boolean, p_rejection_reason text DEFAULT NULL::text) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_req RECORD;
BEGIN
    SELECT * INTO v_req
    FROM attendance.attendance_update_requests
    WHERE request_id = p_request_id AND status = 'pending';

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Request not found or already processed.';
    END IF;

    IF p_is_approved THEN
        -- Apply the approved correction to the attendance table.
        -- (finalized_timesheet_records is intentionally NOT touched here —
        --  that is the manager's explicit "Finalize" action.)
        UPDATE attendance.attendance
        SET checkintime  = v_req.requested_checkin,
            checkouttime = v_req.requested_checkout,
            status       = v_req.requested_status,
            updatedat    = CURRENT_TIMESTAMP
        WHERE attendanceid = v_req.attendance_id
          AND v_req.attendance_id IS NOT NULL;

        -- Mark the request as approved
        UPDATE attendance.attendance_update_requests
        SET status      = 'approved',
            actioned_by = p_actioned_by,
            actioned_at = CURRENT_TIMESTAMP
        WHERE request_id = p_request_id;
    ELSE
        -- Reject
        UPDATE attendance.attendance_update_requests
        SET status           = 'rejected',
            actioned_by      = p_actioned_by,
            actioned_at      = CURRENT_TIMESTAMP,
            rejection_reason = p_rejection_reason
        WHERE request_id = p_request_id;
    END IF;

    RETURN TRUE;
END;
$$;

-- Function: attendance.fn_get_attendance(character varying, uuid, uuid, uuid, character varying, timestamp with time zone, timestamp with time zone, integer, integer)
CREATE FUNCTION attendance.fn_get_attendance(p_search_term character varying DEFAULT NULL::character varying, p_employee_id uuid DEFAULT NULL::uuid, p_organization_id uuid DEFAULT NULL::uuid, p_department_id uuid DEFAULT NULL::uuid, p_status character varying DEFAULT NULL::character varying, p_start_date timestamp with time zone DEFAULT NULL::timestamp with time zone, p_end_date timestamp with time zone DEFAULT NULL::timestamp with time zone, p_limit integer DEFAULT 100, p_offset integer DEFAULT 0) RETURNS TABLE(attendanceid uuid, employeeid uuid, employeename character varying, workdate date, checkintime timestamp with time zone, checkouttime timestamp with time zone, location jsonb, checkinip character varying, checkoutip character varying, totalhours numeric, overtimehours numeric, status character varying, sessionscount integer, notes character varying, totalcount bigint)
    LANGUAGE plpgsql
    AS $$

DECLARE
    totalRecords BIGINT;

BEGIN

    -- Count employees after filters
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
            OR LOWER(CONCAT(e.firstname,' ',e.lastname)) LIKE LOWER('%'||p_search_term||'%')
          );

    RETURN QUERY
    SELECT 
        a.attendanceid,
        e.employeeid,
        CONCAT(e.firstname,' ',e.lastname)::varchar AS employeename,
        gs.workdate,
        a.checkintime,
        a.checkouttime,
        a.location,
        last_session.checkinip,
        last_session.checkoutip,

        COALESCE(a.totalhours,0)::numeric AS totalhours,
        COALESCE(a.overtimehours,0)::numeric AS overtimehours,
        COALESCE(a.status,'absent')::varchar AS status,
        COALESCE(sess.sessionscount,0)::integer AS sessionscount,
        a.notes::varchar,
        totalRecords::bigint AS totalcount

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
            e.employeeid,
            e.firstname,
            e.lastname,
            e.hiredate
        FROM employee.employees e
        INNER JOIN employee.employmentdetails d
            ON e.employeeid = d.employeeid
        WHERE e.status = 'active'
          AND (p_organization_id IS NULL OR e.organizationid = p_organization_id)
          AND (p_department_id IS NULL OR d.departmentid = p_department_id)
          AND (p_employee_id IS NULL OR e.employeeid = p_employee_id)
          AND (
                p_search_term IS NULL
                OR LOWER(CONCAT(e.firstname,' ',e.lastname)) LIKE LOWER('%'||p_search_term||'%')
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

-- Function: attendance.fn_initialize_leave_entitlements(uuid, uuid)
CREATE FUNCTION attendance.fn_initialize_leave_entitlements(p_employee_id uuid, p_organization_id uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
    rec RECORD;
    current_year INT := EXTRACT(YEAR FROM NOW());
BEGIN
    FOR rec IN 
        SELECT leavetypeid, maxdaysperyear
        FROM attendance.leavetypes
        WHERE organizationid = p_organization_id
          AND isactive = true
    LOOP
        INSERT INTO attendance.leaveentitlements
            (employeeid, leavetypeid, year, totaldays, useddays, remainingdays, carryforwarddays, createdat)
        VALUES
            (p_employee_id, rec.leavetypeid, current_year, rec.maxdaysperyear, 0, rec.maxdaysperyear, 0, NOW());
    END LOOP;
END;
$$;

-- Function: attendance.fn_initialize_leave_entitlements_for_new_leavetype(uuid, uuid)
CREATE FUNCTION attendance.fn_initialize_leave_entitlements_for_new_leavetype(p_leavetype_id uuid, p_organization_id uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
    rec RECORD;
    current_year INT := EXTRACT(YEAR FROM NOW());
BEGIN
    FOR rec IN 
        SELECT employeeid 
        FROM employee.employees
        WHERE organizationid = p_organization_id
    LOOP
        -- Check if entitlement already exists for this employee and leave type
        IF NOT EXISTS (
            SELECT 1 
            FROM attendance.leaveentitlements 
            WHERE employeeid = rec.employeeid 
              AND leavetypeid = p_leavetype_id 
              AND year = current_year
        ) THEN
            INSERT INTO attendance.leaveentitlements
                (employeeid, leavetypeid, year, totaldays, useddays, remainingdays, carryforwarddays, createdat)
            SELECT 
                rec.employeeid,
                p_leavetype_id,
                current_year,
                lt.maxdaysperyear,
                0,
                lt.maxdaysperyear,
                0,
                NOW()
            FROM attendance.leavetypes lt
            WHERE lt.leavetypeid = p_leavetype_id;
        END IF;
    END LOOP;
END;
$$;

-- Function: attendance.fn_is_within_geo_fence(numeric, numeric, uuid)
CREATE FUNCTION attendance.fn_is_within_geo_fence(p_latitude numeric, p_longitude numeric, p_geofenceid uuid) RETURNS boolean
    LANGUAGE plpgsql STABLE
    AS $$
DECLARE
    v_dist NUMERIC;
BEGIN
    v_dist := attendance.fn_distance_to_fence_meters(p_latitude, p_longitude, p_geofenceid);
    IF v_dist IS NULL THEN RETURN NULL; END IF; -- polygon: app layer decides
    RETURN v_dist <= 0;
END;
$$;

-- Function: attendance.fn_submit_attendance_request(uuid, uuid, uuid, date, timestamp with time zone, timestamp with time zone, character varying, text, text, uuid)
CREATE FUNCTION attendance.fn_submit_attendance_request(p_employee_id uuid, p_organization_id uuid, p_attendance_id uuid, p_work_date date, p_checkin timestamp with time zone, p_checkout timestamp with time zone, p_status character varying, p_reason text, p_notes text, p_timesheet_id uuid DEFAULT NULL::uuid) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_request_id UUID;
BEGIN
    -- ────────────────────────────────────────────────────────────────
    -- GUARD: Block new draft creation while a pending batch exists
    -- for this employee in the same period.  An employee may not add
    -- fresh corrections once a batch has been submitted for review —
    -- they must wait for the manager to complete the review first.
    -- ────────────────────────────────────────────────────────────────
    IF EXISTS (
        SELECT 1
        FROM attendance.attendance_update_requests
        WHERE employee_id = p_employee_id
          AND status      = 'pending'
          AND (
              -- Same timesheet explicitly tagged
              (p_timesheet_id IS NOT NULL AND timesheet_id = p_timesheet_id)
              OR
              -- OR same month/year (covers NULL-timesheet rows)
              (
                  EXTRACT(MONTH FROM workdate) = EXTRACT(MONTH FROM p_work_date)
                  AND EXTRACT(YEAR  FROM workdate) = EXTRACT(YEAR  FROM p_work_date)
              )
          )
    ) THEN
        RAISE EXCEPTION 'SUBMISSION_LOCKED: Cannot create new corrections while a batch is pending manager review.'
            USING ERRCODE = 'P0001';
    END IF;
	  -- ────────────────────────────────────────────────────────────────
    -- Guard: reject correction on a manager-overridden record
    -- Add this block after SUBMISSION_LOCKED and before final INSERT
    -- ───────────────────────────────────────────────────────────────
-- Guard: reject correction on a manager-overridden record
IF p_attendance_id IS NOT NULL THEN
    IF EXISTS (
        SELECT 1
        FROM attendance.finalized_timesheet_records ftr
        WHERE ftr.attendance_id  = p_attendance_id
          AND ftr.record_status  = 'overridden'
    ) THEN
        RAISE EXCEPTION 'OVERRIDDEN_RECORD: This record has been overridden by a manager and cannot be corrected.'
            USING ERRCODE = 'P0001';
    END IF;
ELSE
    -- Absent-day request: guard by employee + work_date
    IF EXISTS (
        SELECT 1
        FROM attendance.finalized_timesheet_records ftr
        WHERE ftr.employee_id   = p_employee_id
          AND ftr.work_date     = p_work_date
          AND ftr.record_status = 'overridden'
    ) THEN
        RAISE EXCEPTION 'OVERRIDDEN_RECORD: This record has been overridden by a manager and cannot be corrected.'
            USING ERRCODE = 'P0001';
    END IF;
END IF;
    -- ────────────────────────────────────────────────────────────────
    -- Normal submit / upsert path
    -- ────────────────────────────────────────────────────────────────
    INSERT INTO attendance.attendance_update_requests (
        employee_id, organization_id, attendance_id, workdate,
        requested_checkin, requested_checkout, requested_status,
        reason_for_edit, requested_notes, status, submitted_at,
        timesheet_id
    )
    VALUES (
        p_employee_id, p_organization_id, p_attendance_id, p_work_date,
        p_checkin, p_checkout, p_status, p_reason, p_notes, 'draft', CURRENT_TIMESTAMP,
        p_timesheet_id
    )
    ON CONFLICT (employee_id, workdate) WHERE status = 'draft'
    DO UPDATE SET
        requested_checkin  = EXCLUDED.requested_checkin,
        requested_checkout = EXCLUDED.requested_checkout,
        requested_status   = EXCLUDED.requested_status,
        reason_for_edit    = EXCLUDED.reason_for_edit,
        requested_notes    = EXCLUDED.requested_notes,
        timesheet_id       = EXCLUDED.timesheet_id,
        submitted_at       = CURRENT_TIMESTAMP
    RETURNING request_id INTO v_request_id;

    RETURN v_request_id;
END;
$$;

-- Function: attendance.fn_submit_period_for_approval(uuid, uuid, uuid)
CREATE FUNCTION attendance.fn_submit_period_for_approval(p_employee_id uuid, p_organization_id uuid, p_timesheet_id uuid) RETURNS integer
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE attendance.attendance_update_requests
    SET status = 'pending',
        timesheet_id = p_timesheet_id, 
        submitted_at = CURRENT_TIMESTAMP
    WHERE employee_id = p_employee_id 
      AND organization_id = p_organization_id 
      AND status = 'draft';
      
    RETURN (SELECT COUNT(*) FROM attendance.attendance_update_requests 
            WHERE timesheet_id = p_timesheet_id AND employee_id = p_employee_id);
END;
$$;

-- Function: attendance.fn_sync_leave_entitlements_for_leavetype(uuid, uuid)
CREATE FUNCTION attendance.fn_sync_leave_entitlements_for_leavetype(p_leavetype_id uuid, p_organization_id uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
    current_year INT := EXTRACT(YEAR FROM NOW());
BEGIN
    UPDATE attendance.leaveentitlements le
    SET
        totaldays = lt.maxdaysperyear::numeric(5,2),

        carryforwarddays = CASE
            WHEN lt.carryforwardallowed = false THEN 0::numeric(5,2)
            ELSE LEAST(
                    le.carryforwarddays,
                    lt.maxcarryforwarddays::numeric(5,2)
                 )
        END,

        remainingdays = GREATEST(
            (
                lt.maxdaysperyear::numeric(5,2)
                +
                CASE
                    WHEN lt.carryforwardallowed = false THEN 0::numeric(5,2)
                    ELSE LEAST(
                            le.carryforwarddays,
                            lt.maxcarryforwarddays::numeric(5,2)
                         )
                END
            ) - le.useddays,
            0::numeric(5,2)
        ),

        updatedat = NOW()

    FROM attendance.leavetypes lt
    WHERE lt.leavetypeid = p_leavetype_id
      AND lt.organizationid = p_organization_id
      AND le.leavetypeid = p_leavetype_id
      AND le.year = current_year
      AND le.employeeid IN (
          SELECT employeeid
          FROM employee.employees
          WHERE organizationid = p_organization_id
      );
END;
$$;

-- Function: attendance.fn_update_leave_request_status(uuid, character varying, uuid, text)
CREATE FUNCTION attendance.fn_update_leave_request_status(p_request_id uuid, p_status character varying, p_approved_by uuid, p_rejection_reason text DEFAULT NULL::text) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    leave_request RECORD;
    current_year INTEGER;
BEGIN
    -- Get leave request
    SELECT *
    INTO leave_request
    FROM attendance.leaverequests
    WHERE requestId = p_request_id;

    IF leave_request IS NULL THEN
        RAISE EXCEPTION 'Leave request not found';
    END IF;

    current_year := EXTRACT(YEAR FROM leave_request.startDate);

    -- 🔹 Update leave request status only
    UPDATE attendance.leaverequests
    SET status = p_status,
        approvedBy = p_approved_by,
        approvedAt = CASE WHEN p_status = 'approved' THEN CURRENT_TIMESTAMP ELSE NULL END,
        rejectionReason = p_rejection_reason,
        updatedAt = CURRENT_TIMESTAMP
    WHERE requestId = p_request_id;

    -- 🔹 Handle reject: Add days back to leave entitlements
    IF p_status = 'rejected' THEN
        UPDATE attendance.leaveentitlements
        SET usedDays = usedDays - leave_request.daysRequested,
            remainingDays = remainingDays + leave_request.daysRequested,
            updatedAt = CURRENT_TIMESTAMP
        WHERE employeeId = leave_request.employeeId
          AND leaveTypeId = leave_request.leaveTypeId
          AND year = current_year;
    END IF;

    RETURN TRUE;
END;
$$;


-- ============================================================
-- SCHEMA: HELPDESK
-- ============================================================

-- Function: helpdesk.fn_create_ticket(uuid, uuid, uuid, character varying, character varying, uuid, character varying, text, character varying, uuid)
CREATE FUNCTION helpdesk.fn_create_ticket(p_organizationid uuid, p_employeeid uuid, p_departmentid uuid, p_title character varying, p_tickettype character varying, p_categoryid uuid, p_priority character varying, p_description text, p_attachment character varying, p_assignedgroup uuid DEFAULT NULL::uuid) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_ticketid uuid;
    v_assignmentid uuid;
BEGIN
    -- 1️⃣ Insert ticket with department and categoryid
    INSERT INTO helpdesk.tickets(
        organizationid, employeeid, departmentid, title, tickettype, categoryid, priority, description, attachment
    ) VALUES (
        p_organizationid, p_employeeid, p_departmentid, p_title, p_tickettype, p_categoryid, COALESCE(p_priority,'Medium'), p_description, p_attachment
    )
    RETURNING ticketid INTO v_ticketid;

    -- 2️⃣ If assigned group is provided, insert assignment
    IF p_assignedgroup IS NOT NULL THEN
        INSERT INTO helpdesk.ticket_assignments(
            ticketid, groupid, assignedby, createdat
        ) VALUES (
            v_ticketid, p_assignedgroup, p_employeeid, CURRENT_TIMESTAMP
        )
        RETURNING assignmentid INTO v_assignmentid;

        -- 3️⃣ Assign employees from group
        INSERT INTO helpdesk.ticket_assigned_employees(ticketid, employeeid, assignmentid, createdat)
        SELECT v_ticketid, unnest(employeeids), v_assignmentid, CURRENT_TIMESTAMP
        FROM helpdesk.ticket_group
        WHERE groupid = p_assignedgroup;
    END IF;

    RETURN v_ticketid;
END;
$$;


-- ============================================================
-- SCHEMA: NEWS
-- ============================================================

-- Function: news.fn_get_news_admin(uuid, uuid, character varying, character varying, integer, integer)
CREATE FUNCTION news.fn_get_news_admin(p_employee_id uuid, p_organization_id uuid DEFAULT NULL::uuid, p_search_term character varying DEFAULT NULL::character varying, p_status character varying DEFAULT NULL::character varying, p_limit integer DEFAULT 50, p_offset integer DEFAULT 0) RETURNS TABLE(newsid uuid, title character varying, description text, createdby uuid, authorname character varying, category character varying, imageurl character varying, publishedat timestamp with time zone, status character varying, visibleto character varying, selectedemployees uuid[], createdat date, updatedat date, organizationid uuid, isread boolean, views integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT
        n.newsid,
        n.title,
        n.description,
        n.createdby,
        (CONCAT(e.firstname, ' ', e.lastname))::varchar As authorname,  -- ✅ JOINED NAME
        n.category,
        n.imageurl,
        n.publishedat,
        n.status,
        n.visibleto,
        n.selectedemployees::uuid[],
        n.createdat::date,
        n.updatedat::date,
        n.organizationid,
        (nr.employeeid IS NOT NULL) AS isread,
        n.views
    FROM news.news n

    -- ✅ JOIN WITH EMPLOYEE TABLE
    LEFT JOIN employee.employees e
        ON e.employeeid = n.createdby

    LEFT JOIN news.news_reads_count nr
        ON n.newsid = nr.newsid
        AND nr.employeeid = p_employee_id

    WHERE
        (p_organization_id IS NULL OR n.organizationid = p_organization_id)
        AND (p_status IS NULL OR n.status = p_status)
        AND (p_search_term IS NULL 
            OR LOWER(n.title || ' ' || n.description) 
               LIKE LOWER('%' || p_search_term || '%'))

    ORDER BY n.publishedat DESC
    LIMIT p_limit OFFSET p_offset;
END;
$$;

-- Function: news.fn_get_news_employee(uuid, uuid, character varying, character varying, integer, integer)
CREATE FUNCTION news.fn_get_news_employee(p_employee_id uuid, p_organization_id uuid DEFAULT NULL::uuid, p_search_term character varying DEFAULT NULL::character varying, p_status character varying DEFAULT NULL::character varying, p_limit integer DEFAULT 50, p_offset integer DEFAULT 0) RETURNS TABLE(newsid uuid, title character varying, description text, createdby uuid, category character varying, imageurl character varying, publishedat timestamp with time zone, status character varying, visibleto character varying, selectedemployees uuid[], isread boolean, authorname character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT
        n.newsid,
        n.title,
        n.description,
        n.createdby,
        n.category,
        n.imageurl,
        n.publishedat,
        n.status,
        n.visibleto,
        n.selectedemployees,
       (nr.employeeid IS NOT NULL) AS isread,
	   (concat(e.firstname,'',e.lastname))::varchar as authorname
    FROM news.news n
	Left join employee.employees e on
		e.employeeid=n.createdby
    LEFT JOIN news.news_reads_count nr
        ON n.newsid = nr.newsid
        AND nr.employeeid = p_employee_id
    WHERE
        (p_organization_id IS NULL OR n.organizationid = p_organization_id)
        AND (p_status IS NULL OR n.status = p_status)
        AND (p_search_term IS NULL OR LOWER(n.title || ' ' || n.description) LIKE LOWER('%' || p_search_term || '%'))
        AND (
            COALESCE(n.visibleto, 'Everyone') = 'Everyone'
            OR (
                n.visibleto = 'Selected'
                AND n.selectedemployees IS NOT NULL
                AND p_employee_id = ANY(n.selectedemployees)
            )
        )
    ORDER BY n.publishedat DESC
    LIMIT p_limit OFFSET p_offset;
END;
$$;


-- ============================================================
-- SCHEMA: PAYMENT
-- ============================================================

-- Function: payment.generate_invoice_number()
CREATE FUNCTION payment.generate_invoice_number() RETURNS text
    LANGUAGE plpgsql
    AS $$
DECLARE
    next_val BIGINT;
    year_part TEXT;
BEGIN
    next_val := nextval('payment.invoice_number_seq');
    year_part := EXTRACT(YEAR FROM NOW())::TEXT;
    RETURN 'INV-' || year_part || '-' || LPAD(next_val::TEXT, 6, '0');
END;
$$;

-- Function: payment.update_updated_at_column()
CREATE FUNCTION payment.update_updated_at_column() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$;


-- ============================================================
-- SCHEMA: PAYROLL
-- ============================================================

-- Function: payroll.fn_calculate_payroll(uuid)
CREATE FUNCTION payroll.fn_calculate_payroll(p_period_id uuid) RETURNS integer
    LANGUAGE plpgsql
    AS $_$
DECLARE
    payroll_period RECORD;
    employee_record RECORD;
    entries_created INTEGER := 0;
    basic_salary DECIMAL(15,2);
    total_hours DECIMAL(8,2);
    overtime_hours DECIMAL(8,2);
    overtime_rate DECIMAL(8,4) := 1.5;
    gross_salary DECIMAL(15,2);
    tax_amount DECIMAL(15,2);
    net_salary DECIMAL(15,2);
BEGIN
    -- Get payroll period details
    SELECT * INTO payroll_period 
    FROM payroll.payrollPeriods 
    WHERE periodId = p_period_id;
    
    -- Loop through all employees (independent, no FK required)
    FOR employee_record IN 
        SELECT e.employeeId, ed.basicSalary
        FROM employee.employees e
        LEFT JOIN employee.employmentDetails ed ON e.employeeId = ed.employeeId
        WHERE e.status = 'active'
          AND ed.isCurrent = true
    LOOP
        basic_salary := COALESCE(employee_record.basicSalary, 0);
        
        -- Calculate total hours worked in the period
        SELECT 
            COALESCE(SUM(totalHours), 0),
            COALESCE(SUM(overtimeHours), 0)
        INTO total_hours, overtime_hours
        FROM attendance.attendance
        WHERE employeeId = employee_record.employeeId
          AND workDate BETWEEN payroll_period.startDate AND payroll_period.endDate;
        
        -- Calculate gross salary (basic + overtime)
        gross_salary := basic_salary + (overtime_hours * (basic_salary / 160) * overtime_rate);
        
        -- Simple tax calculation (15% for amounts > $2000)
        tax_amount := CASE 
            WHEN gross_salary > 2000 THEN gross_salary * 0.15 
            ELSE 0 
        END;
        
        net_salary := gross_salary - tax_amount;
        
        -- Insert or update payroll entry
        INSERT INTO payroll.payrollEntries (
            periodId, employeeId, basicSalary, overtimeAmount,
            grossSalary, taxAmount, netSalary, calculatedAt
        )
        VALUES (
            p_period_id, employee_record.employeeId, basic_salary,
            overtime_hours * (basic_salary / 160) * overtime_rate,
            gross_salary, tax_amount, net_salary, CURRENT_TIMESTAMP
        )
        ON CONFLICT (periodId, employeeId) 
        DO UPDATE SET 
            basicSalary = EXCLUDED.basicSalary,
            overtimeAmount = EXCLUDED.overtimeAmount,
            grossSalary = EXCLUDED.grossSalary,
            taxAmount = EXCLUDED.taxAmount,
            netSalary = EXCLUDED.netSalary,
            calculatedAt = CURRENT_TIMESTAMP;
        
        entries_created := entries_created + 1;
    END LOOP;
    
    -- Update payroll period totals
    UPDATE payroll.payrollPeriods 
    SET totalEmployees = entries_created,
        totalGrossAmount = (SELECT SUM(grossSalary) FROM payroll.payrollEntries WHERE periodId = p_period_id),
        totalTaxAmount = (SELECT SUM(taxAmount) FROM payroll.payrollEntries WHERE periodId = p_period_id),
        totalNetAmount = (SELECT SUM(netSalary) FROM payroll.payrollEntries WHERE periodId = p_period_id),
        updatedAt = CURRENT_TIMESTAMP
    WHERE periodId = p_period_id;
    
    RETURN entries_created;
END;
$_$;

-- Function: payroll.fn_create_payroll_period(uuid, character varying, date, date, date)
CREATE FUNCTION payroll.fn_create_payroll_period(p_organization_id uuid, p_period_name character varying, p_start_date date, p_end_date date, p_pay_date date DEFAULT NULL::date) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    new_period_id UUID;
    employees_count INTEGER;
    total_gross NUMERIC;
BEGIN
    -- Count active employees
    SELECT COUNT(*) INTO employees_count
    FROM employee.employees
    WHERE organizationid = p_organization_id AND status = 'active';

    -- Sum basic salaries of active employees
    SELECT COALESCE(SUM(ed.basicsalary), 0) INTO total_gross
    FROM employee.employees e
    JOIN employee.employmentdetails ed ON e.employeeid = ed.employeeid
    WHERE e.organizationid = p_organization_id AND e.status = 'active';

    -- Insert payroll period
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
-- SCHEMA: PERFORMANCE
-- ============================================================

-- Function: performance.fn_create_appraisal_cycle(uuid, character varying, timestamp with time zone, timestamp with time zone, uuid, character varying, timestamp with time zone, timestamp with time zone, character varying, boolean, boolean, boolean, text)
CREATE FUNCTION performance.fn_create_appraisal_cycle(p_organization_id uuid, p_cycle_name character varying, p_start_date timestamp with time zone, p_end_date timestamp with time zone, p_created_by uuid, p_cycle_type character varying DEFAULT NULL::character varying, p_review_start_date timestamp with time zone DEFAULT NULL::timestamp with time zone, p_review_end_date timestamp with time zone DEFAULT NULL::timestamp with time zone, p_status character varying DEFAULT 'upcoming'::character varying, p_self_assessment_enable boolean DEFAULT false, p_manager_review boolean DEFAULT false, p_appraisal_enable boolean DEFAULT false, p_description text DEFAULT NULL::text) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
DECLARE
    new_cycle_id UUID;
BEGIN
    INSERT INTO performance.appraisal_cycle (
        organizationid,
        cyclename,
        cycletype,
        startdate,
        enddate,
        reviewstartdate,
        reviewenddate,
        status,
        isselfassessmentenable,
        managerreview,
        isappraisalenable,
        createdby,
        description,
        createdat,
        updatedat
    )
    VALUES (
        p_organization_id,
        p_cycle_name,
        p_cycle_type,
        p_start_date,
        p_end_date,
        p_review_start_date,
        p_review_end_date,
        p_status,
        p_self_assessment_enable,
        p_manager_review,
        p_appraisal_enable,
        p_created_by,
        p_description,
        now(),
        now()
    )
    RETURNING cycleid INTO new_cycle_id;

    RETURN new_cycle_id;
END;
$$;