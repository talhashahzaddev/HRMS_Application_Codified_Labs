
-- =====================================================================
-- auth FUNCTIONS
-- =====================================================================

-- ======================================================
-- Function: fn_verify_email
-- ======================================================

CREATE OR REPLACE FUNCTION auth.fn_verify_email(
	p_token character varying,
	p_verify_time timestamp with time zone)
    RETURNS boolean
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
DECLARE
    v_rows_affected INTEGER;
BEGIN
    UPDATE auth.users
    SET
        emailVerify = TRUE,
        emailVerifyToken = NULL,
        emailVerifyExpiry = NULL
    WHERE emailVerifyToken = p_token
      AND emailVerifyExpiry > p_verify_time
      AND emailVerify = FALSE;

    GET DIAGNOSTICS v_rows_affected = ROW_COUNT;

    IF v_rows_affected > 0 THEN
        RETURN TRUE;
    END IF;

    RETURN FALSE;
END;
$BODY$;

ALTER FUNCTION auth.fn_verify_email(character varying, timestamp with time zone)
    OWNER TO postgres;

-- ======================================================
-- Function: fn_authenticate_user
-- ======================================================


CREATE OR REPLACE FUNCTION auth.fn_authenticate_user(
	p_email character varying,
	p_password_hash character varying)
    RETURNS TABLE(userid uuid, organizationid uuid, email character varying, firstname character varying, lastname character varying, rolename character varying, permissions jsonb, organizationname character varying,domain character varying,  isactive boolean) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
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
$BODY$;

ALTER FUNCTION auth.fn_authenticate_user(character varying, character varying)
    OWNER TO postgres;



-- ======================================================
-- Function: fn_create_organization
-- ======================================================
fn_create_organization


-- ======================================================
-- Function: fn_create_user
-- ======================================================
CREATE OR REPLACE FUNCTION auth.fn_create_user(
	p_organization_id uuid,
	p_email character varying,
	p_password_hash character varying,
	p_first_name character varying,
	p_last_name character varying,
	p_role_name character varying DEFAULT 'Employee'::character varying,
	p_email_verify boolean DEFAULT NULL,
	p_email_verify_token character varying DEFAULT NULL,
	p_email_verify_expiry timestamp with time zone DEFAULT NULL)
    RETURNS TABLE(user_id uuid, role_id uuid) -- <--- CHANGED: Returns a table structure
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
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
$BODY$;

ALTER FUNCTION auth.fn_create_user(uuid, character varying, character varying, character varying, character varying, character varying)
    OWNER TO postgres;


-- =====================================================================
-- employee FUNCTIONS
-- =====================================================================

-- FUNCTION: employee.fn_create_employee
-- Updates: Insert into employees + employmentdetails


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

ALTER FUNCTION employee.fn_create_employee(uuid, uuid, character varying, character varying, character varying, character varying, character varying, timestamp with time zone, timestamp with time zone, uuid, uuid, numeric, jsonb, character varying, uuid, character varying, character varying, character varying, character varying, jsonb)
    OWNER TO postgres;



-- Function: Get Employee By Id
CREATE OR REPLACE FUNCTION employee.fn_get_employee_by_id(p_employee_id UUID)
RETURNS TABLE (
    employeeId UUID,
    organizationId UUID,
    userId UUID,
    employeeCode VARCHAR(50),
    firstName VARCHAR(100),
    lastName VARCHAR(100),
    email VARCHAR(255),
    phone VARCHAR(20),
    address JSONB,
    dateOfBirth DATE,
    hireDate DATE,
    status VARCHAR(20),
    departmentName VARCHAR(100),
    positionTitle VARCHAR(100),
    basicSalary DECIMAL(15,2),
    managerName VARCHAR(201),
    createdAt TIMESTAMP WITH TIME ZONE
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        e.employee_id AS "employeeId",
        e.organization_id AS "organizationId",
        e.user_id AS "userId",
        e.employee_code AS "employeeCode",
        e.first_name AS "firstName",
        e.last_name AS "lastName",
        e.email,
        e.phone,
        e.address,
        e.date_of_birth AS "dateOfBirth",
        e.hire_date AS "hireDate",
        e.status,
        d.department_name AS "departmentName",
        p.position_title AS "positionTitle",
        ed.basic_salary AS "basicSalary",
        CONCAT(m.first_name, ' ', m.last_name) AS "managerName",
        e.created_at AS "createdAt"
    FROM employee.employees e
    LEFT JOIN employee.employment_details ed 
           ON e.employee_id = ed.employee_id AND ed.is_current = true
    LEFT JOIN employee.departments d 
           ON ed.department_id = d.department_id
    LEFT JOIN employee.positions p 
           ON ed.position_id = p.position_id
    LEFT JOIN employee.employees m 
           ON ed.reporting_manager_id = m.employee_id
    WHERE e.employee_id = p_employee_id;
END;
$$ LANGUAGE plpgsql;


-- Function: Get Employees (with pagination & filters)


-- DROP FUNCTION IF EXISTS employee.fn_get_employees(uuid, character varying, uuid, uuid, character varying, character varying, integer, integer);


CREATE OR REPLACE FUNCTION employee.fn_get_employees(
	p_organization_id uuid DEFAULT NULL::uuid,
	p_search_term character varying DEFAULT NULL::character varying,
	p_department_id uuid DEFAULT NULL::uuid,
	p_position_id uuid DEFAULT NULL::uuid,
	p_employment_type character varying DEFAULT NULL::character varying,
	p_status character varying DEFAULT NULL::character varying,
	p_limit integer DEFAULT 50,
	p_offset integer DEFAULT 0)
    RETURNS TABLE(employeeid uuid, employeecode character varying, firstname character varying, lastname character varying, email character varying, phone character varying, hiredate date, status character varying, departmentid uuid, departmentname character varying, profilepictureurl character varying, positionid uuid, positiontitle character varying, employmenttype character varying, paytype character varying, basicsalary numeric, reportingmanagerid uuid, reportingmanagername character varying, gender character varying, nationality character varying, maritalstatus character varying, dateofbirth date, address jsonb, emergencycontact jsonb, totalcount bigint) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
DECLARE
    total_records BIGINT;
BEGIN
    -- Count total matching records
    SELECT COUNT(*) INTO total_records
    FROM employee.employees e
    LEFT JOIN employee.employmentdetails ed 
           ON e.employeeid = ed.employeeid AND ed.iscurrent = true
    LEFT JOIN employee.departments d 
           ON ed.departmentid = d.departmentid
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

    -- Return paginated results
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
        ed.departmentid,             -- added
        d.departmentname,
        e.profilepictureurl,
        ed.positionid,               -- added
        pos.positiontitle,
        ed.employmenttype,
        ed.paytype,
        ed.basicsalary,
        ed.reportingmanagerid,
        (CASE 
            WHEN rm.firstname IS NULL AND rm.lastname IS NULL THEN NULL
            ELSE rm.firstname || ' ' || rm.lastname
         END)::character varying AS reportingmanagername,
        e.gender,
        e.nationality,
        e.maritalstatus,
        e.dateofbirth,
        e.address::jsonb,
        e.emergencycontact::jsonb,
        total_records::bigint
    FROM employee.employees e
    LEFT JOIN employee.employmentdetails ed 
           ON e.employeeid = ed.employeeid AND ed.iscurrent = true
    LEFT JOIN employee.departments d 
           ON ed.departmentid = d.departmentid
    LEFT JOIN employee.positions pos 
           ON ed.positionid = pos.positionid
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


-- =====================================================================
-- ATTENDANCE FUNCTIONS
-- =====================================================================

-- Function to clock in/out


CREATE OR REPLACE FUNCTION attendance.fn_clock_in_out(
    p_employee_id uuid,
    p_action character varying,
    p_timestamp timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    p_location jsonb DEFAULT NULL::jsonb,
    p_notes character varying DEFAULT NULL)
    RETURNS boolean
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
DECLARE
    -- GLOBAL FIX: Normalize to UTC BEFORE extracting date
    todayDate DATE := (p_timestamp AT TIME ZONE 'UTC')::DATE;

    attendanceRecord RECORD;
BEGIN
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

        INSERT INTO attendance.AttendanceSessions (attendanceId, checkInTime, location)
        VALUES (attendanceRecord.attendanceId, p_timestamp, COALESCE(p_location, '{}'));

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
            updatedAt    = CURRENT_TIMESTAMP
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
$BODY$;

-- FUNCTION: attendance.fn_get_attendance(uuid, uuid, date, date, integer, integer)

-- DROP FUNCTION IF EXISTS attendance.fn_get_attendance(uuid, uuid, date, date, integer, integer);

CREATE OR REPLACE FUNCTION attendance.fn_get_attendance(
	p_search_term character varying DEFAULT NULL::character varying,
	p_employee_id uuid DEFAULT NULL::uuid,
	p_organization_id uuid DEFAULT NULL::uuid,
	p_department_id uuid DEFAULT NULL::uuid,
	p_status character varying DEFAULT NULL::character varying,
	p_start_date timestamp with time zone DEFAULT NULL::timestamp with time zone,
	p_end_date timestamp with time zone DEFAULT NULL::timestamp with time zone,
	p_limit integer DEFAULT 100,
	p_offset integer DEFAULT 0)
    RETURNS TABLE(attendanceid uuid, employeeid uuid, employeename character varying, workdate date, checkintime timestamp with time zone, checkouttime timestamp with time zone, location jsonb, checkinip character varying, checkoutip character varying, totalhours numeric, overtimehours numeric, status character varying, sessionscount integer, notes character varying, totalcount bigint) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$

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
$BODY$;

-- =====================================================================
-- LEAVE MANAGEMENT FUNCTIONS
-- =====================================================================

-- Function to create leave request
CREATE OR REPLACE FUNCTION attendance.fn_create_leave_request(
    p_employee_id UUID,
    p_leave_type_id UUID,
    p_start_date timestamp with time zone,
    p_end_date timestamp with time zone,
    p_reason TEXT DEFAULT NULL
)
RETURNS UUID AS $$
DECLARE
    new_request_id UUID;
    days_requested DECIMAL(5,2);
    available_days DECIMAL(5,2);
    current_year INTEGER := EXTRACT(YEAR FROM p_start_date);
BEGIN
  -- Calculate working days between start and end date
SELECT 
    (p_end_date::date - p_start_date::date + 1) 
    - (EXTRACT(week FROM p_end_date) - EXTRACT(week FROM p_start_date)) * 2
    - CASE WHEN EXTRACT(dow FROM p_start_date) = 0 THEN 1 ELSE 0 END
    - CASE WHEN EXTRACT(dow FROM p_end_date) = 6 THEN 1 ELSE 0 END
INTO days_requested;

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
$$ LANGUAGE plpgsql;


-- Function to approve/reject leave request

CREATE OR REPLACE FUNCTION attendance.fn_update_leave_request_status(
	p_request_id uuid,
	p_status character varying,
	p_approved_by uuid,
	p_rejection_reason text DEFAULT NULL::text)
    RETURNS boolean
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
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

    -- ?? Update leave request status only
    UPDATE attendance.leaverequests
    SET status = p_status,
        approvedBy = p_approved_by,
        approvedAt = CASE WHEN p_status = 'approved' THEN CURRENT_TIMESTAMP ELSE NULL END,
        rejectionReason = p_rejection_reason,
        updatedAt = CURRENT_TIMESTAMP
    WHERE requestId = p_request_id;

    -- ?? Handle reject: Add days back to leave entitlements
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
$BODY$;

ALTER FUNCTION attendance.fn_update_leave_request_status(uuid, character varying, uuid, text)
    OWNER TO postgres;

-- =====================================================================
-- PAYROLL FUNCTIONS
-- =====================================================================

-- ============================
-- Function: Create Payroll Period
-- ============================
CREATE OR REPLACE FUNCTION payroll.fn_create_payroll_period(
    p_organization_id uuid,
    p_period_name character varying,
    p_start_date date,
    p_end_date date,
    p_pay_date date DEFAULT NULL::date
)
RETURNS uuid
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

-- =====================================================================
-- COMMON FUNCTIONS
-- =====================================================================

CREATE OR REPLACE FUNCTION public.fn_set_updated_at()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$;

-- =====================================================================
-- ASSET TRIGGERS
-- =====================================================================

DROP TRIGGER IF EXISTS trg_asset_types_updated_at ON asset.asset_types;
CREATE TRIGGER trg_asset_types_updated_at
BEFORE UPDATE ON asset.asset_types
FOR EACH ROW
EXECUTE FUNCTION public.fn_set_updated_at();

DROP TRIGGER IF EXISTS trg_assets_updated_at ON asset.assets;
CREATE TRIGGER trg_assets_updated_at
BEFORE UPDATE ON asset.assets
FOR EACH ROW
EXECUTE FUNCTION public.fn_set_updated_at();

DROP TRIGGER IF EXISTS trg_asset_assignments_updated_at ON asset.asset_assignments;
CREATE TRIGGER trg_asset_assignments_updated_at
BEFORE UPDATE ON asset.asset_assignments
FOR EACH ROW
EXECUTE FUNCTION public.fn_set_updated_at();

ALTER FUNCTION payroll.fn_create_payroll_period(uuid, character varying, date, date, date)
    OWNER TO postgres;

-- ============================
-- Function: Calculate Payroll
-- ============================
CREATE OR REPLACE FUNCTION payroll.fn_calculate_payroll(
    p_period_id UUID
)
RETURNS INTEGER AS $$
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
$$ LANGUAGE plpgsql;

-- =====================================================================
-- PERFORMANCE MANAGEMENT FUNCTIONS
-- =====================================================================

-- ============================
-- Function: Create Appraisal Cycle
-- ============================
CREATE OR REPLACE FUNCTION performance.fn_create_appraisal_cycle(
    p_organization_id UUID,
    p_cycle_name VARCHAR(100),
	p_start_date timestamp with time zone,
	p_end_date timestamp with time zone,
    p_description TEXT DEFAULT NULL
)
RETURNS UUID AS $$
DECLARE
    new_cycle_id UUID;
BEGIN
    INSERT INTO performance.appraisalCycles (
        organizationId, cycleName, startDate, endDate, description
    )
    VALUES (
        p_organization_id, p_cycle_name, p_start_date, p_end_date, p_description
    )
    RETURNING cycleId INTO new_cycle_id;
    
    RETURN new_cycle_id;
END;
$$ LANGUAGE plpgsql;

-- ============================
-- Function: Get Dashboard Stats
-- ============================
CREATE OR REPLACE FUNCTION admin.fn_get_dashboard_stats(p_organization_id UUID)
RETURNS TABLE (
    totalEmployees BIGINT,
    activeEmployees BIGINT,
    pendingLeaveRequests BIGINT,
    presentToday BIGINT,
    totalDepartments BIGINT
) AS $$
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
$$ LANGUAGE plpgsql;


--inserting data for leave balance 

CREATE OR REPLACE FUNCTION attendance.fn_initialize_leave_entitlements(
    p_employee_id UUID,
    p_organization_id UUID
)
RETURNS VOID
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







--Leave Balance for new leave types

CREATE OR REPLACE FUNCTION attendance.fn_initialize_leave_entitlements_for_new_leavetype(
    p_leavetype_id UUID,
    p_organization_id UUID
)
RETURNS VOID
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


CREATE OR REPLACE FUNCTION attendance.fn_sync_leave_entitlements_for_leavetype(
    p_leavetype_id UUID,
    p_organization_id UUID
)
RETURNS VOID
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





-- DROP FUNCTION IF EXISTS news.fn_get_news_employee(uuid, uuid, character varying, character varying, integer, integer);

CREATE OR REPLACE FUNCTION news.fn_get_news_employee(
	p_employee_id uuid,
	p_organization_id uuid DEFAULT NULL::uuid,
	p_search_term character varying DEFAULT NULL::character varying,
	p_status character varying DEFAULT NULL::character varying,
	p_limit integer DEFAULT 50,
	p_offset integer DEFAULT 0)
    RETURNS TABLE(newsid uuid, title character varying, description text, createdby uuid, category character varying, imageurl character varying, publishedat timestamp with time zone, status character varying, visibleto character varying, selectedemployees uuid[], isread boolean, authorname character varying) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
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
$BODY$;




-- DROP FUNCTION IF EXISTS news.fn_get_news_admin(uuid, uuid, character varying, character varying, integer, integer);

CREATE OR REPLACE FUNCTION news.fn_get_news_admin(
	p_employee_id uuid,
	p_organization_id uuid DEFAULT NULL::uuid,
	p_search_term character varying DEFAULT NULL::character varying,
	p_status character varying DEFAULT NULL::character varying,
	p_limit integer DEFAULT 50,
	p_offset integer DEFAULT 0)
    RETURNS TABLE(newsid uuid, title character varying, description text, createdby uuid, authorname character varying, category character varying, imageurl character varying, publishedat timestamp with time zone, status character varying, visibleto character varying, selectedemployees uuid[], createdat date, updatedat date, organizationid uuid, isread boolean, views integer) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
BEGIN
    RETURN QUERY
    SELECT
        n.newsid,
        n.title,
        n.description,
        n.createdby,
        (CONCAT(e.firstname, ' ', e.lastname))::varchar As authorname,  -- ? JOINED NAME
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

    -- ? JOIN WITH EMPLOYEE TABLE
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
$BODY$;

--Customise Role Functions
CREATE OR REPLACE FUNCTION auth.fn_create_role(
    p_organizationid uuid,
    p_rolename text,
    p_description text,
    p_issystemrole boolean,
    p_menus jsonb
)
RETURNS uuid
LANGUAGE 'plpgsql'
COST 100
VOLATILE 
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