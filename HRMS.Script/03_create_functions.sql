
-- =====================================================================
-- auth FUNCTIONS
-- =====================================================================


-- ======================================================
-- Function: fn_authenticate_user
-- ======================================================
CREATE OR REPLACE FUNCTION auth.fn_authenticate_user(
    p_email character varying,
    p_password_hash character varying
)
RETURNS TABLE(
    userId uuid, 
    organizationId uuid, 
    email character varying, 
    firstName character varying, 
    lastName character varying, 
    roleName character varying, 
    permissions jsonb, 
    organizationName character varying, 
    isActive boolean
) 
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
CREATE OR REPLACE FUNCTION auth.fn_create_organization(
    p_name character varying,
    p_domain character varying DEFAULT NULL::character varying,
    p_subscription_type character varying DEFAULT 'basic'::character varying
)
RETURNS uuid
LANGUAGE 'plpgsql'
COST 100
VOLATILE PARALLEL UNSAFE
AS $BODY$
DECLARE
    newOrgId UUID;
BEGIN
    INSERT INTO auth.organizations (name, domain, subscriptionType)
    VALUES (p_name, p_domain, p_subscription_type)
    RETURNING organizationId INTO newOrgId;
    
    -- Create default roles for the organization
    INSERT INTO auth.roles (organizationId, roleName, permissions, isSystemRole)
    VALUES 
        (newOrgId, 'Super Admin', '{"all": true}', true),
        (newOrgId, 'HR Manager', '{"employees": "all", "payroll": "all", "attendance": "all", "performance": "all"}', true),
        (newOrgId, 'Manager', '{"employees": "read", "attendance": "manage_team", "performance": "manage_team"}', true),
        (newOrgId, 'Employee', '{"employees": "self", "attendance": "self", "performance": "self"}', true);
    
    RETURN newOrgId;
END;
$BODY$;

ALTER FUNCTION auth.fn_create_organization(character varying, character varying, character varying)
    OWNER TO postgres;


-- ======================================================
-- Function: fn_create_user
-- ======================================================
CREATE OR REPLACE FUNCTION auth.fn_create_user(
    p_organization_id uuid,
    p_email character varying,
    p_password_hash character varying,
    p_first_name character varying,
    p_last_name character varying,
    p_role_name character varying DEFAULT 'Employee'::character varying
)
RETURNS uuid
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
    
    INSERT INTO auth.users (organizationId, email, passwordHash, firstName, lastName, roleId)
    VALUES (p_organization_id, p_email, p_password_hash, p_first_name, p_last_name, roleIdVar)
    RETURNING userId INTO newUserId;
    
    RETURN newUserId;
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
    p_work_location character varying DEFAULT NULL,
    p_reporting_manager_id uuid DEFAULT NULL
)
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
        hiredate
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
        p_hire_date::date
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
        p_hire_date::date, -- effective from hire date
        true
    );

    RETURN v_employee_id;
END;
$BODY$;

ALTER FUNCTION employee.fn_create_employee(
    uuid, uuid, character varying, character varying, character varying, character varying, character varying,
    timestamp with time zone, timestamp with time zone, uuid, uuid, numeric, jsonb, character varying, uuid
) OWNER TO postgres;



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
CREATE OR REPLACE FUNCTION employee.fn_get_employees(
    p_organization_id uuid DEFAULT NULL::uuid,
    p_search_term character varying DEFAULT NULL::character varying,
    p_department_id uuid DEFAULT NULL::uuid,
    p_status character varying DEFAULT NULL::character varying,
    p_limit integer DEFAULT 50,
    p_offset integer DEFAULT 0
)
RETURNS TABLE(
    employeeid uuid,
    employeecode character varying,
    firstname character varying,
    lastname character varying,
    email character varying,
    phone character varying,
    hiredate date,
    status character varying,
    departmentname character varying,
    positiontitle character varying,
    totalcount bigint
) 
LANGUAGE 'plpgsql'
AS $BODY$
DECLARE
    total_records BIGINT;
BEGIN
    -- Count with flexible filtering
    SELECT COUNT(*) INTO total_records
    FROM employee.employees e
    LEFT JOIN employee.employmentdetails ed 
           ON e.employeeid = ed.employeeid AND ed.iscurrent = true
    LEFT JOIN employee.departments d 
           ON ed.departmentid = d.departmentid
    WHERE (p_organization_id IS NULL OR e.organizationid = p_organization_id)
      AND (p_department_id IS NULL OR ed.departmentid = p_department_id)
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
        d.departmentname,
        pos.positiontitle,
        total_records
    FROM employee.employees e
    LEFT JOIN employee.employmentdetails ed 
           ON e.employeeid = ed.employeeid AND ed.iscurrent = true
    LEFT JOIN employee.departments d 
           ON ed.departmentid = d.departmentid
    LEFT JOIN employee.positions pos 
           ON ed.positionid = pos.positionid
    WHERE (p_organization_id IS NULL OR e.organizationid = p_organization_id)
      AND (p_department_id IS NULL OR ed.departmentid = p_department_id)
      AND (p_search_term IS NULL OR 
           LOWER(CONCAT(e.firstname, ' ', e.lastname, ' ', e.email, ' ', e.employeecode)) 
           LIKE LOWER('%' || p_search_term || '%'))
      AND (p_status IS NULL OR e.status = p_status)
    ORDER BY e.firstname, e.lastname
    LIMIT p_limit OFFSET p_offset;
END;
$BODY$;

ALTER FUNCTION employee.fn_get_employees(
    uuid, character varying, uuid, character varying, integer, integer
) OWNER TO postgres;



-- =====================================================================
-- ATTENDANCE FUNCTIONS
-- =====================================================================

-- Function to clock in/out
CREATE OR REPLACE FUNCTION attendance.fn_clock_in_out(
	p_employee_id uuid,
	p_action character varying,
	p_timestamp timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
	p_location jsonb DEFAULT NULL::jsonb)
    RETURNS boolean
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
DECLARE
    todayDate DATE := p_timestamp::DATE;
    attendanceRecord RECORD;
BEGIN
    -- Step 1: Ensure daily attendance record exists
    SELECT * INTO attendanceRecord 
    FROM attendance.Attendance 
    WHERE employeeId = p_employee_id AND workDate = todayDate;

    IF attendanceRecord IS NULL THEN
        INSERT INTO attendance.Attendance (employeeId, workDate, location, status)
        VALUES (p_employee_id, todayDate, COALESCE(p_location, '{}'), 'present')
        RETURNING * INTO attendanceRecord;
    END IF;

    -- Step 2: Handle Check-In
    IF p_action = 'in' THEN
        -- Update main table with latest check-in (reset checkout for new session)
        UPDATE attendance.Attendance 
        SET checkInTime  = p_timestamp,
            checkOutTime = NULL,  -- ? reset checkout on new check-in
            status       = 'present',
            location     = COALESCE(p_location, location),
            updatedAt    = CURRENT_TIMESTAMP
        WHERE attendanceId = attendanceRecord.attendanceId;

        -- Insert new session row
        INSERT INTO attendance.AttendanceSessions (attendanceId, checkInTime, location)
        VALUES (attendanceRecord.attendanceId, p_timestamp, COALESCE(p_location, '{}'));

    -- Step 3: Handle Check-Out
    ELSIF p_action = 'out' THEN
        -- Update main table with latest check-out
        UPDATE attendance.Attendance 
        SET checkOutTime = p_timestamp,
            location     = COALESCE(p_location, location),
            updatedAt    = CURRENT_TIMESTAMP
        WHERE attendanceId = attendanceRecord.attendanceId;

        -- ? Directly update the latest open session
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

        -- Recalculate total hours for the day
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
	p_employee_id uuid DEFAULT NULL::uuid,
	p_organization_id uuid DEFAULT NULL::uuid,
	p_start_date date DEFAULT NULL::date,
	p_end_date date DEFAULT NULL::date,
	p_limit integer DEFAULT 100,
	p_offset integer DEFAULT 0)
    RETURNS TABLE(attendanceid uuid, employeeid uuid, employeename character varying, workdate date, checkintime timestamp with time zone, checkouttime timestamp with time zone, totalhours numeric, overtimehours numeric, status character varying, totalcount bigint) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
DECLARE
    totalRecords BIGINT;
BEGIN
    -- Get total count
    SELECT COUNT(*) INTO totalRecords
    FROM attendance.Attendance a
    INNER JOIN employee.Employees e ON a.employeeId = e.employeeId
    WHERE (p_employee_id IS NULL OR a.employeeId = p_employee_id)
      AND (p_organization_id IS NULL OR e.organizationId = p_organization_id)
      AND (p_start_date IS NULL OR a.workDate >= p_start_date)
      AND (p_end_date IS NULL OR a.workDate <= p_end_date);

    RETURN QUERY
    SELECT 
        a.attendanceId,
        a.employeeId,
        CONCAT(e.firstName, ' ', e.lastName)::varchar AS employeeName,
        a.workDate,
        a.checkInTime,
        a.checkOutTime,
        a.totalHours,
        a.overtimeHours,
        a.status,
        totalRecords
    FROM attendance.Attendance a
    INNER JOIN employee.Employees e ON a.employeeId = e.employeeId
    WHERE (p_employee_id IS NULL OR a.employeeId = p_employee_id)
      AND (p_organization_id IS NULL OR e.organizationId = p_organization_id)
      AND (p_start_date IS NULL OR a.workDate >= p_start_date)
      AND (p_end_date IS NULL OR a.workDate <= p_end_date)
    ORDER BY a.workDate DESC, e.firstName
    LIMIT p_limit OFFSET p_offset;
END;
$BODY$;

ALTER FUNCTION attendance.fn_get_attendance(uuid, uuid, date, date, integer, integer)
    OWNER TO postgres;

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
    p_request_id UUID,
    p_status VARCHAR(20),
    p_approved_by UUID,
    p_rejection_reason TEXT DEFAULT NULL
)
RETURNS BOOLEAN AS $$
DECLARE
    leave_request RECORD;
    current_year INTEGER;
BEGIN
    -- Get leave request details
    SELECT * INTO leave_request 
    FROM attendance.leaverequests 
    WHERE requestId = p_request_id;
    
    IF leave_request IS NULL THEN
        RAISE EXCEPTION 'Leave request not found';
    END IF;
    
    current_year := EXTRACT(YEAR FROM leave_request.startDate);
    
    -- Update request status
    UPDATE attendance.leaverequests 
    SET status = p_status,
        approvedBy = p_approved_by,
        approvedAt = CASE WHEN p_status = 'approved' THEN CURRENT_TIMESTAMP ELSE NULL END,
        rejectionReason = p_rejection_reason,
        updatedAt = CURRENT_TIMESTAMP
    WHERE requestId = p_request_id;
    
    -- If approved, update leave entitlements
    IF p_status = 'approved' THEN
        UPDATE attendance.leaveentitlements 
        SET usedDays = usedDays + leave_request.daysRequested,
            remainingDays = remainingDays - leave_request.daysRequested,
            updatedAt = CURRENT_TIMESTAMP
        WHERE employeeId = leave_request.employeeId 
          AND leaveTypeId = leave_request.leaveTypeId 
          AND year = current_year;
    END IF;
    
    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

-- =====================================================================
-- PAYROLL FUNCTIONS
-- =====================================================================

-- ============================
-- Function: Create Payroll Period
-- ============================
CREATE OR REPLACE FUNCTION payroll.fn_create_payroll_period(
    p_organization_id UUID,
    p_period_name VARCHAR(100),
    p_start_date DATE,
    p_end_date DATE,
    p_pay_date DATE DEFAULT NULL
)
RETURNS UUID AS $$
DECLARE
    new_period_id UUID;
BEGIN
    INSERT INTO payroll.payrollPeriods (
        organizationId, periodName, startDate, endDate, payDate
    )
    VALUES (
        p_organization_id, p_period_name, p_start_date, p_end_date, p_pay_date
    )
    RETURNING periodId INTO new_period_id;
    
    RETURN new_period_id;
END;
$$ LANGUAGE plpgsql;

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


