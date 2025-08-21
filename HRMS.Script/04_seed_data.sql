-- HRMS Database Seed Data

-- Create demo organization
INSERT INTO auth.organizations (organization_id, name, domain, subscription_type, status)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'Demo Company Ltd.',
    'demo.company.com',
    'premium',
    'active'
);

-- Create default leave types for demo organization
INSERT INTO attendance.leave_types (organization_id, type_name, description, max_days_per_year, is_paid, carry_forward_allowed, color)
VALUES 
    ('00000000-0000-0000-0000-000000000001', 'Annual Leave', 'Yearly vacation leave', 25, true, true, '#4CAF50'),
    ('00000000-0000-0000-0000-000000000001', 'Sick Leave', 'Medical leave', 10, true, false, '#FF9800'),
    ('00000000-0000-0000-0000-000000000001', 'Personal Leave', 'Personal time off', 5, false, false, '#2196F3'),
    ('00000000-0000-0000-0000-000000000001', 'Maternity Leave', 'Maternity/Paternity leave', 90, true, false, '#E91E63'),
    ('00000000-0000-0000-0000-000000000001', 'Emergency Leave', 'Emergency situations', 3, true, false, '#F44336');

-- Create default shifts
INSERT INTO attendance.shifts (organization_id, shift_name, start_time, end_time, break_duration, days_of_week)
VALUES 
    ('00000000-0000-0000-0000-000000000001', 'Day Shift', '09:00:00', '17:00:00', 60, '{1,2,3,4,5}'),
    ('00000000-0000-0000-0000-000000000001', 'Night Shift', '21:00:00', '05:00:00', 60, '{1,2,3,4,5}'),
    ('00000000-0000-0000-0000-000000000001', 'Flexible Hours', '08:00:00', '16:00:00', 60, '{1,2,3,4,5}');

-- Create departments
INSERT INTO employee.departments (department_id, organization_id, department_name, description)
VALUES 
    ('10000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'Information Technology', 'IT and Software Development'),
    ('10000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'Human Resources', 'HR and People Management'),
    ('10000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001', 'Finance & Accounting', 'Financial Management'),
    ('10000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000001', 'Marketing', 'Marketing and Sales'),
    ('10000000-0000-0000-0000-000000000005', '00000000-0000-0000-0000-000000000001', 'Operations', 'Business Operations');

-- Create positions
INSERT INTO employee.positions (position_id, organization_id, position_title, department_id, description)
VALUES 
    ('20000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'Software Developer', '10000000-0000-0000-0000-000000000001', 'Full-stack software development'),
    ('20000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'Senior Developer', '10000000-0000-0000-0000-000000000001', 'Senior software development role'),
    ('20000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001', 'HR Manager', '10000000-0000-0000-0000-000000000002', 'Human Resources Management'),
    ('20000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000001', 'HR Specialist', '10000000-0000-0000-0000-000000000002', 'HR Operations and Support'),
    ('20000000-0000-0000-0000-000000000005', '00000000-0000-0000-0000-000000000001', 'Accountant', '10000000-0000-0000-0000-000000000003', 'Financial Accounting'),
    ('20000000-0000-0000-0000-000000000006', '00000000-0000-0000-0000-000000000001', 'Marketing Manager', '10000000-0000-0000-0000-000000000004', 'Marketing Strategy and Execution'),
    ('20000000-0000-0000-0000-000000000007', '00000000-0000-0000-0000-000000000001', 'Operations Manager', '10000000-0000-0000-0000-000000000005', 'Business Operations Management');

-- Create skill sets
INSERT INTO performance.skill_sets (organization_id, skill_name, category, description)
VALUES 
    ('00000000-0000-0000-0000-000000000001', 'JavaScript', 'Technical', 'JavaScript programming language'),
    ('00000000-0000-0000-0000-000000000001', 'TypeScript', 'Technical', 'TypeScript programming language'),
    ('00000000-0000-0000-0000-000000000001', 'Angular', 'Technical', 'Angular framework'),
    ('00000000-0000-0000-0000-000000000001', '.NET Core', 'Technical', '.NET Core framework'),
    ('00000000-0000-0000-0000-000000000001', 'PostgreSQL', 'Technical', 'PostgreSQL database'),
    ('00000000-0000-0000-0000-000000000001', 'Communication', 'Soft Skills', 'Effective communication'),
    ('00000000-0000-0000-0000-000000000001', 'Leadership', 'Soft Skills', 'Team leadership abilities'),
    ('00000000-0000-0000-0000-000000000001', 'Problem Solving', 'Soft Skills', 'Analytical problem solving'),
    ('00000000-0000-0000-0000-000000000001', 'Project Management', 'Management', 'Project planning and execution'),
    ('00000000-0000-0000-0000-000000000001', 'Time Management', 'Management', 'Effective time management');

-- Create roles (done automatically by the organization creation function)
-- But let's ensure we have the correct role IDs for our demo data
INSERT INTO auth.roles (role_id, organization_id, role_name, permissions, is_system_role)
VALUES 
    ('30000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'Super Admin', '{"all": true}', true),
    ('30000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'HR Manager', '{"employees": "all", "payroll": "all", "attendance": "all", "performance": "all"}', true),
    ('30000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001', 'Manager', '{"employees": "read", "attendance": "manage_team", "performance": "manage_team"}', true),
    ('30000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000001', 'Employee', '{"employees": "self", "attendance": "self", "performance": "self"}', true)
ON CONFLICT (organization_id, role_name) DO UPDATE SET
    permissions = EXCLUDED.permissions,
    is_system_role = EXCLUDED.is_system_role;

-- Create demo users
INSERT INTO auth.users (user_id, organization_id, email, password_hash, first_name, last_name, role_id, is_active)
VALUES 
    ('40000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'admin@demo.company.com', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'System', 'Administrator', '30000000-0000-0000-0000-000000000001', true),
    ('40000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'hr@demo.company.com', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Sarah', 'Johnson', '30000000-0000-0000-0000-000000000002', true),
    ('40000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001', 'john.doe@demo.company.com', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'John', 'Doe', '30000000-0000-0000-0000-000000000003', true),
    ('40000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000001', 'jane.smith@demo.company.com', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Jane', 'Smith', '30000000-0000-0000-0000-000000000004', true),
    ('40000000-0000-0000-0000-000000000005', '00000000-0000-0000-0000-000000000001', 'mike.wilson@demo.company.com', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Mike', 'Wilson', '30000000-0000-0000-0000-000000000004', true);

-- Create demo employees
INSERT INTO employee.employees (
    employee_id, organization_id, user_id, employee_code, first_name, last_name, 
    email, phone, hire_date, status, address
)
VALUES 
    ('50000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', 'EMP001', 'System', 'Administrator', 'admin@demo.company.com', '+1-555-0001', '2023-01-01', 'active', '{"street": "123 Admin St", "city": "Tech City", "state": "CA", "zip": "90210"}'),
    ('50000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000002', 'EMP002', 'Sarah', 'Johnson', 'hr@demo.company.com', '+1-555-0002', '2023-01-15', 'active', '{"street": "456 HR Ave", "city": "Business City", "state": "CA", "zip": "90211"}'),
    ('50000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000003', 'EMP003', 'John', 'Doe', 'john.doe@demo.company.com', '+1-555-0003', '2023-02-01', 'active', '{"street": "789 Dev Blvd", "city": "Code City", "state": "CA", "zip": "90212"}'),
    ('50000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000004', 'EMP004', 'Jane', 'Smith', 'jane.smith@demo.company.com', '+1-555-0004', '2023-02-15', 'active', '{"street": "321 Designer St", "city": "Creative City", "state": "CA", "zip": "90213"}'),
    ('50000000-0000-0000-0000-000000000005', '00000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000005', 'EMP005', 'Mike', 'Wilson', 'mike.wilson@demo.company.com', '+1-555-0005', '2023-03-01', 'active', '{"street": "654 Finance Rd", "city": "Money City", "state": "CA", "zip": "90214"}');

-- Create employment details
INSERT INTO employee.employment_details (
    employee_id, department_id, position_id, basic_salary, effective_date, reporting_manager_id
)
VALUES 
    ('50000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000002', 120000.00, '2023-01-01', NULL),
    ('50000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000003', 95000.00, '2023-01-15', '50000000-0000-0000-0000-000000000001'),
    ('50000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000002', 85000.00, '2023-02-01', '50000000-0000-0000-0000-000000000001'),
    ('50000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 75000.00, '2023-02-15', '50000000-0000-0000-0000-000000000003'),
    ('50000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000005', 70000.00, '2023-03-01', '50000000-0000-0000-0000-000000000002');

-- Create leave entitlements for current year
INSERT INTO attendance.leave_entitlements (employee_id, leave_type_id, year, total_days, remaining_days)
SELECT 
    e.employee_id,
    lt.leave_type_id,
    EXTRACT(YEAR FROM CURRENT_DATE),
    lt.max_days_per_year,
    lt.max_days_per_year
FROM employee.employees e
CROSS JOIN attendance.leave_types lt
WHERE e.organization_id = '00000000-0000-0000-0000-000000000001'
  AND lt.organization_id = '00000000-0000-0000-0000-000000000001';

-- Create some sample attendance records for the current month
INSERT INTO attendance.attendance (
    employee_id, work_date, check_in_time, check_out_time, total_hours, status
)
SELECT 
    e.employee_id,
    gs.work_date,
    (gs.work_date + TIME '09:00:00' + (RANDOM() * INTERVAL '30 minutes'))::timestamptz AS check_in_time,
    (gs.work_date + TIME '17:00:00' + (RANDOM() * INTERVAL '60 minutes'))::timestamptz AS check_out_time,
    8.0 + (RANDOM() * 2) AS total_hours,
    'present'
FROM employee.employees e
CROSS JOIN LATERAL (
    SELECT generate_series(
        DATE_TRUNC('month', CURRENT_DATE),
        CURRENT_DATE - INTERVAL '1 day',
        '1 day'
    )::date AS work_date
) gs
WHERE e.organization_id = '00000000-0000-0000-0000-000000000001'
  AND EXTRACT(dow FROM gs.work_date) BETWEEN 1 AND 5; -- Mon-Fri only


-- Create sample leave requests
INSERT INTO attendance.leave_requests (employee_id, leave_type_id, start_date, end_date, days_requested, reason, status)
VALUES 
    ('50000000-0000-0000-0000-000000000003', 
     (SELECT leave_type_id FROM attendance.leave_types WHERE type_name = 'Annual Leave' AND organization_id = '00000000-0000-0000-0000-000000000001'),
     CURRENT_DATE + INTERVAL '10 days', CURRENT_DATE + INTERVAL '14 days', 5, 'Family vacation', 'pending'),
    ('50000000-0000-0000-0000-000000000004', 
     (SELECT leave_type_id FROM attendance.leave_types WHERE type_name = 'Sick Leave' AND organization_id = '00000000-0000-0000-0000-000000000001'),
     CURRENT_DATE - INTERVAL '3 days', CURRENT_DATE - INTERVAL '3 days', 1, 'Medical appointment', 'approved'),
    ('50000000-0000-0000-0000-000000000005', 
     (SELECT leave_type_id FROM attendance.leave_types WHERE type_name = 'Personal Leave' AND organization_id = '00000000-0000-0000-0000-000000000001'),
     CURRENT_DATE + INTERVAL '20 days', CURRENT_DATE + INTERVAL '21 days', 2, 'Personal matters', 'pending');

-- Update department managers
UPDATE employee.departments SET manager_id = '50000000-0000-0000-0000-000000000001' WHERE department_id = '10000000-0000-0000-0000-000000000001';
UPDATE employee.departments SET manager_id = '50000000-0000-0000-0000-000000000002' WHERE department_id = '10000000-0000-0000-0000-000000000002';

-- Create a sample payroll period
INSERT INTO payroll.payroll_periods (organization_id, period_name, start_date, end_date, status)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'January 2024 Payroll',
    '2024-01-01',
    '2024-01-31',
    'draft'
);

-- Create sample appraisal cycle
INSERT INTO performance.appraisal_cycles (organization_id, cycle_name, start_date, end_date, description)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'Q1 2024 Performance Review',
    '2024-01-01',
    '2024-03-31',
    'First quarter performance review cycle'
);

-- Grant appropriate permissions (if using row-level security)
-- This would be implemented based on specific security requirements

COMMIT;

