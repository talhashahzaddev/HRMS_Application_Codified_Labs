
CREATE OR REPLACE FUNCTION employee.fn_get_employee_by_id(p_employee_id UUID)
RETURNS TABLE (
    employee_id UUID,
    organization_id UUID,
    user_id UUID,
    employee_code VARCHAR(50),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    email VARCHAR(255),
    phone VARCHAR(20),
    address JSONB,
    date_of_birth DATE,
    hire_date DATE,
    status VARCHAR(20),
    department_name VARCHAR(100),
    position_title VARCHAR(100),
    basic_salary DECIMAL(15,2),
    manager_name VARCHAR(201),
    created_at TIMESTAMP WITH TIME ZONE
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        e.employee_id,
        e.organization_id,
        e.user_id,
        e.employee_code,
        e.first_name,
        e.last_name,
        e.email,
        e.phone,
        e.address,
        e.date_of_birth,
        e.hire_date,
        e.status,
        d.department_name,
        p.position_title,
        ed.basic_salary,
        CONCAT(m.first_name, ' ', m.last_name) AS manager_name,
        e.created_at
    FROM employee.employees e
    LEFT JOIN employee.employment_details ed ON e.employee_id = ed.employee_id AND ed.is_current = true
    LEFT JOIN employee.departments d ON ed.department_id = d.department_id
    LEFT JOIN employee.positions p ON ed.position_id = p.position_id
    LEFT JOIN employee.employees m ON ed.reporting_manager_id = m.employee_id
    WHERE e.employee_id = p_employee_id;
END;
$$ LANGUAGE plpgsql;

-- Function to get employees with pagination and filters
CREATE OR REPLACE FUNCTION employee.fn_get_employees(
    p_organization_id UUID,
    p_search_term VARCHAR(255) DEFAULT NULL,
    p_department_id UUID DEFAULT NULL,
    p_status VARCHAR(20) DEFAULT NULL,
    p_limit INTEGER DEFAULT 50,
    p_offset INTEGER DEFAULT 0
)
RETURNS TABLE (
    employee_id UUID,
    employee_code VARCHAR(50),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    email VARCHAR(255),
    phone VARCHAR(20),
    hire_date DATE,
    status VARCHAR(20),
    department_name VARCHAR(100),
    position_title VARCHAR(100),
    total_count BIGINT
) AS $$
DECLARE
    total_records BIGINT;
BEGIN
    -- Get total count
    SELECT COUNT(*) INTO total_records
    FROM employee.employees e
    LEFT JOIN employee.employment_details ed ON e.employee_id = ed.employee_id AND ed.is_current = true
    LEFT JOIN employee.departments d ON ed.department_id = d.department_id
    WHERE e.organization_id = p_organization_id
      AND (p_search_term IS NULL OR 
           LOWER(CONCAT(e.first_name, ' ', e.last_name, ' ', e.email, ' ', e.employee_code)) 
           LIKE LOWER('%' || p_search_term || '%'))
      AND (p_department_id IS NULL OR ed.department_id = p_department_id)
      AND (p_status IS NULL OR e.status = p_status);

    -- Return paginated results
    RETURN QUERY
    SELECT 
        e.employee_id,
        e.employee_code,
        e.first_name,
        e.last_name,
        e.email,
        e.phone,
        e.hire_date,
        e.status,
        d.department_name,
        p.position_title,
        total_records
    FROM employee.employees e
    LEFT JOIN employee.employment_details ed ON e.employee_id = ed.employee_id AND ed.is_current = true
    LEFT JOIN employee.departments d ON ed.department_id = d.department_id
    LEFT JOIN employee.positions pos ON ed.position_id = pos.position_id
    WHERE e.organization_id = p_organization_id
      AND (p_search_term IS NULL OR 
           LOWER(CONCAT(e.first_name, ' ', e.last_name, ' ', e.email, ' ', e.employee_code)) 
           LIKE LOWER('%' || p_search_term || '%'))
      AND (p_department_id IS NULL OR ed.department_id = p_department_id)
      AND (p_status IS NULL OR e.status = p_status)
    ORDER BY e.first_name, e.last_name
    LIMIT p_limit OFFSET p_offset;
END;
$$ LANGUAGE plpgsql;

-- =====================================================================
-- ATTENDANCE FUNCTIONS
-- =====================================================================

-- Function to check in/out
CREATE OR REPLACE FUNCTION attendance.fn_clock_in_out(
    p_employee_id UUID,
    p_action VARCHAR(10), -- 'in' or 'out'
    p_timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    p_location JSONB DEFAULT NULL
)
RETURNS BOOLEAN AS $$
DECLARE
    today_date DATE := p_timestamp::DATE;
    attendance_record RECORD;
BEGIN
    -- Get or create today's attendance record
    SELECT * INTO attendance_record 
    FROM attendance.attendance 
    WHERE employee_id = p_employee_id AND work_date = today_date;
    
    IF attendance_record IS NULL THEN
        -- Create new attendance record
        INSERT INTO attendance.attendance (employee_id, work_date, location)
        VALUES (p_employee_id, today_date, COALESCE(p_location, '{}'));
    END IF;
    
    IF p_action = 'in' THEN
        UPDATE attendance.attendance 
        SET check_in_time = p_timestamp,
            status = 'present',
            location = COALESCE(p_location, location),
            updated_at = CURRENT_TIMESTAMP
        WHERE employee_id = p_employee_id AND work_date = today_date;
    ELSIF p_action = 'out' THEN
        UPDATE attendance.attendance 
        SET check_out_time = p_timestamp,
            total_hours = EXTRACT(EPOCH FROM (p_timestamp - check_in_time)) / 3600,
            location = COALESCE(p_location, location),
            updated_at = CURRENT_TIMESTAMP
        WHERE employee_id = p_employee_id AND work_date = today_date;
    END IF;
    
    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

-- Function to get attendance records
CREATE OR REPLACE FUNCTION attendance.fn_get_attendance(
    p_employee_id UUID DEFAULT NULL,
    p_organization_id UUID DEFAULT NULL,
    p_start_date DATE DEFAULT NULL,
    p_end_date DATE DEFAULT NULL,
    p_limit INTEGER DEFAULT 100,
    p_offset INTEGER DEFAULT 0
)
RETURNS TABLE (
    attendance_id UUID,
    employee_id UUID,
    employee_name VARCHAR(201),
    work_date DATE,
    check_in_time TIMESTAMP WITH TIME ZONE,
    check_out_time TIMESTAMP WITH TIME ZONE,
    total_hours DECIMAL(5,2),
    overtime_hours DECIMAL(5,2),
    status VARCHAR(20),
    total_count BIGINT
) AS $$
DECLARE
    total_records BIGINT;
BEGIN
    -- Get total count
    SELECT COUNT(*) INTO total_records
    FROM attendance.attendance a
    INNER JOIN employee.employees e ON a.employee_id = e.employee_id
    WHERE (p_employee_id IS NULL OR a.employee_id = p_employee_id)
      AND (p_organization_id IS NULL OR e.organization_id = p_organization_id)
      AND (p_start_date IS NULL OR a.work_date >= p_start_date)
      AND (p_end_date IS NULL OR a.work_date <= p_end_date);

    RETURN QUERY
    SELECT 
        a.attendance_id,
        a.employee_id,
        CONCAT(e.first_name, ' ', e.last_name) AS employee_name,
        a.work_date,
        a.check_in_time,
        a.check_out_time,
        a.total_hours,
        a.overtime_hours,
        a.status,
        total_records
    FROM attendance.attendance a
    INNER JOIN employee.employees e ON a.employee_id = e.employee_id
    WHERE (p_employee_id IS NULL OR a.employee_id = p_employee_id)
      AND (p_organization_id IS NULL OR e.organization_id = p_organization_id)
      AND (p_start_date IS NULL OR a.work_date >= p_start_date)
      AND (p_end_date IS NULL OR a.work_date <= p_end_date)
    ORDER BY a.work_date DESC, e.first_name
    LIMIT p_limit OFFSET p_offset;
END;
$$ LANGUAGE plpgsql;

-- =====================================================================
-- LEAVE MANAGEMENT FUNCTIONS
-- =====================================================================

-- Function to create leave request
CREATE OR REPLACE FUNCTION attendance.fn_create_leave_request(
    p_employee_id UUID,
    p_leave_type_id UUID,
    p_start_date DATE,
    p_end_date DATE,
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
        (p_end_date - p_start_date + 1) - 
        (EXTRACT(week FROM p_end_date) - EXTRACT(week FROM p_start_date)) * 2 - 
        CASE WHEN EXTRACT(dow FROM p_start_date) = 0 THEN 1 ELSE 0 END -
        CASE WHEN EXTRACT(dow FROM p_end_date) = 6 THEN 1 ELSE 0 END
    INTO days_requested;
    
    -- Check available leave balance
    SELECT remaining_days INTO available_days
    FROM attendance.leave_entitlements 
    WHERE employee_id = p_employee_id 
      AND leave_type_id = p_leave_type_id 
      AND year = current_year;
      
    IF available_days < days_requested THEN
        RAISE EXCEPTION 'Insufficient leave balance. Available: %, Requested: %', available_days, days_requested;
    END IF;
    
    -- Create leave request
    INSERT INTO attendance.leave_requests (
        employee_id, leave_type_id, start_date, end_date, 
        days_requested, reason
    )
    VALUES (
        p_employee_id, p_leave_type_id, p_start_date, p_end_date,
        days_requested, p_reason
    )
    RETURNING request_id INTO new_request_id;
    
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
    FROM attendance.leave_requests 
    WHERE request_id = p_request_id;
    
    IF leave_request IS NULL THEN
        RAISE EXCEPTION 'Leave request not found';
    END IF;
    
    current_year := EXTRACT(YEAR FROM leave_request.start_date);
    
    -- Update request status
    UPDATE attendance.leave_requests 
    SET status = p_status,
        approved_by = p_approved_by,
        approved_at = CASE WHEN p_status = 'approved' THEN CURRENT_TIMESTAMP ELSE NULL END,
        rejection_reason = p_rejection_reason,
        updated_at = CURRENT_TIMESTAMP
    WHERE request_id = p_request_id;
    
    -- If approved, update leave entitlements
    IF p_status = 'approved' THEN
        UPDATE attendance.leave_entitlements 
        SET used_days = used_days + leave_request.days_requested,
            remaining_days = remaining_days - leave_request.days_requested,
            updated_at = CURRENT_TIMESTAMP
        WHERE employee_id = leave_request.employee_id 
          AND leave_type_id = leave_request.leave_type_id 
          AND year = current_year;
    END IF;
    
    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

-- =====================================================================
-- PAYROLL FUNCTIONS
-- =====================================================================

-- Function to create payroll period
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
    INSERT INTO payroll.payroll_periods (
        organization_id, period_name, start_date, end_date, pay_date
    )
    VALUES (
        p_organization_id, p_period_name, p_start_date, p_end_date, p_pay_date
    )
    RETURNING period_id INTO new_period_id;
    
    RETURN new_period_id;
END;
$$ LANGUAGE plpgsql;

-- Function to calculate payroll for a period
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
    FROM payroll.payroll_periods 
    WHERE period_id = p_period_id;
    
    -- Loop through all active employees for the organization
    FOR employee_record IN 
        SELECT e.employee_id, ed.basic_salary
        FROM employee.employees e
        INNER JOIN employee.employment_details ed ON e.employee_id = ed.employee_id
        WHERE e.organization_id = payroll_period.organization_id
          AND e.status = 'active'
          AND ed.is_current = true
    LOOP
        basic_salary := COALESCE(employee_record.basic_salary, 0);
        
        -- Calculate total hours worked in the period
        SELECT 
            COALESCE(SUM(total_hours), 0),
            COALESCE(SUM(overtime_hours), 0)
        INTO total_hours, overtime_hours
        FROM attendance.attendance
        WHERE employee_id = employee_record.employee_id
          AND work_date BETWEEN payroll_period.start_date AND payroll_period.end_date;
        
        -- Calculate gross salary (basic + overtime)
        gross_salary := basic_salary + (overtime_hours * (basic_salary / 160) * overtime_rate);
        
        -- Simple tax calculation (15% for amounts > $2000)
        tax_amount := CASE 
            WHEN gross_salary > 2000 THEN gross_salary * 0.15 
            ELSE 0 
        END;
        
        net_salary := gross_salary - tax_amount;
        
        -- Insert or update payroll entry
        INSERT INTO payroll.payroll_entries (
            period_id, employee_id, basic_salary, overtime_amount,
            gross_salary, tax_amount, net_salary, calculated_at
        )
        VALUES (
            p_period_id, employee_record.employee_id, basic_salary,
            overtime_hours * (basic_salary / 160) * overtime_rate,
            gross_salary, tax_amount, net_salary, CURRENT_TIMESTAMP
        )
        ON CONFLICT (period_id, employee_id) 
        DO UPDATE SET 
            basic_salary = EXCLUDED.basic_salary,
            overtime_amount = EXCLUDED.overtime_amount,
            gross_salary = EXCLUDED.gross_salary,
            tax_amount = EXCLUDED.tax_amount,
            net_salary = EXCLUDED.net_salary,
            calculated_at = CURRENT_TIMESTAMP;
        
        entries_created := entries_created + 1;
    END LOOP;
    
    -- Update payroll period totals
    UPDATE payroll.payroll_periods 
    SET total_employees = entries_created,
        total_gross_amount = (SELECT SUM(gross_salary) FROM payroll.payroll_entries WHERE period_id = p_period_id),
        total_tax_amount = (SELECT SUM(tax_amount) FROM payroll.payroll_entries WHERE period_id = p_period_id),
        total_net_amount = (SELECT SUM(net_salary) FROM payroll.payroll_entries WHERE period_id = p_period_id),
        updated_at = CURRENT_TIMESTAMP
    WHERE period_id = p_period_id;
    
    RETURN entries_created;
END;
$$ LANGUAGE plpgsql;

-- =====================================================================
-- PERFORMANCE MANAGEMENT FUNCTIONS
-- =====================================================================

-- Function to create appraisal cycle
CREATE OR REPLACE FUNCTION performance.fn_create_appraisal_cycle(
    p_organization_id UUID,
    p_cycle_name VARCHAR(100),
    p_start_date DATE,
    p_end_date DATE,
    p_description TEXT DEFAULT NULL
)
RETURNS UUID AS $$
DECLARE
    new_cycle_id UUID;
BEGIN
    INSERT INTO performance.appraisal_cycles (
        organization_id, cycle_name, start_date, end_date, description
    )
    VALUES (
        p_organization_id, p_cycle_name, p_start_date, p_end_date, p_description
    )
    RETURNING cycle_id INTO new_cycle_id;
    
    RETURN new_cycle_id;
END;
$$ LANGUAGE plpgsql;

-- Function to get dashboard statistics
CREATE OR REPLACE FUNCTION admin.fn_get_dashboard_stats(p_organization_id UUID)
RETURNS TABLE (
    total_employees BIGINT,
    active_employees BIGINT,
    pending_leave_requests BIGINT,
    present_today BIGINT,
    total_departments BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        (SELECT COUNT(*) FROM employee.employees WHERE organization_id = p_organization_id) AS total_employees,
        (SELECT COUNT(*) FROM employee.employees WHERE organization_id = p_organization_id AND status = 'active') AS active_employees,
        (SELECT COUNT(*) FROM attendance.leave_requests lr 
         INNER JOIN employee.employees e ON lr.employee_id = e.employee_id 
         WHERE e.organization_id = p_organization_id AND lr.status = 'pending') AS pending_leave_requests,
        (SELECT COUNT(*) FROM attendance.attendance a 
         INNER JOIN employee.employees e ON a.employee_id = e.employee_id 
         WHERE e.organization_id = p_organization_id 
           AND a.work_date = CURRENT_DATE 
           AND a.status = 'present') AS present_today,
        (SELECT COUNT(*) FROM employee.departments WHERE organization_id = p_organization_id AND is_active = true) AS total_departments;
END;
$$ LANGUAGE plpgsql;
