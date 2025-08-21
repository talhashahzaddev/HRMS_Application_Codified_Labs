-- HRMS Database Tables Creation Script


-- Organizations (Multi-tenant support)
CREATE TABLE auth.organizations (
    organization_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    subscription_type VARCHAR(50) DEFAULT 'basic',
    domain VARCHAR(255),
    logo_url VARCHAR(500),
    status VARCHAR(20) DEFAULT 'active',
    settings JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Roles & Permissions
CREATE TABLE auth.roles (
    role_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID REFERENCES auth.organizations(organization_id) ON DELETE CASCADE,
    role_name VARCHAR(100) NOT NULL,
    description TEXT,
    permissions JSONB DEFAULT '{}',
    is_system_role BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(organization_id, role_name)
);

-- Users & Authentication
CREATE TABLE auth.users (
    user_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID REFERENCES auth.organizations(organization_id) ON DELETE CASCADE,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role_id UUID REFERENCES auth.roles(role_id),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    is_active BOOLEAN DEFAULT true,
    email_verified BOOLEAN DEFAULT false,
    last_login_at TIMESTAMP WITH TIME ZONE,
    password_changed_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    refresh_token VARCHAR(500),
    refresh_token_expires_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Departments
CREATE TABLE employee.departments (
    department_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID REFERENCES auth.organizations(organization_id) ON DELETE CASCADE,
    department_name VARCHAR(100) NOT NULL,
    description TEXT,
    manager_id UUID,
    parent_department_id UUID REFERENCES employee.departments(department_id),
    budget DECIMAL(15, 2),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(organization_id, department_name)
);

-- Positions
CREATE TABLE employee.positions (
    position_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID REFERENCES auth.organizations(organization_id) ON DELETE CASCADE,
    position_title VARCHAR(100) NOT NULL,
    department_id UUID REFERENCES employee.departments(department_id),
    description TEXT,
    requirements JSONB DEFAULT '{}',
    salary_range JSONB DEFAULT '{}',
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Employee Personal Information
CREATE TABLE employee.employees (
    employee_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID REFERENCES auth.organizations(organization_id) ON DELETE CASCADE,
    user_id UUID REFERENCES auth.users(user_id) ON DELETE SET NULL,
    employee_code VARCHAR(50) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL,
    phone VARCHAR(20),
    address JSONB DEFAULT '{}',
    date_of_birth DATE,
    gender VARCHAR(20),
    nationality VARCHAR(50),
    marital_status VARCHAR(20),
    emergency_contact JSONB DEFAULT '{}',
    hire_date DATE NOT NULL,
    termination_date DATE,
    status VARCHAR(20) DEFAULT 'active',
    profile_picture_url VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(organization_id, employee_code),
    UNIQUE(organization_id, email)
);

-- Employment Details
CREATE TABLE employee.employment_details (
    detail_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    employee_id UUID REFERENCES employee.employees(employee_id) ON DELETE CASCADE,
    department_id UUID REFERENCES employee.departments(department_id),
    position_id UUID REFERENCES employee.positions(position_id),
    shift_id UUID,
    employment_type VARCHAR(50) DEFAULT 'full_time',
    pay_type VARCHAR(20) DEFAULT 'salary',
    basic_salary DECIMAL(15, 2),
    currency VARCHAR(3) DEFAULT 'USD',
    work_location VARCHAR(100),
    reporting_manager_id UUID REFERENCES employee.employees(employee_id),
    effective_date DATE NOT NULL,
    end_date DATE,
    is_current BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Shifts Management
CREATE TABLE attendance.shifts (
    shift_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID REFERENCES auth.organizations(organization_id) ON DELETE CASCADE,
    shift_name VARCHAR(100) NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    break_duration INTEGER DEFAULT 0, -- minutes
    days_of_week INTEGER[] DEFAULT '{1,2,3,4,5}', -- 0=Sunday, 6=Saturday
    timezone VARCHAR(50) DEFAULT 'UTC',
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(organization_id, shift_name)
);

-- Update employment_details to reference shifts
ALTER TABLE employee.employment_details 
ADD CONSTRAINT fk_shift_id FOREIGN KEY (shift_id) REFERENCES attendance.shifts(shift_id);

-- Attendance Records
CREATE TABLE attendance.attendance (
    attendance_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    employee_id UUID REFERENCES employee.employees(employee_id) ON DELETE CASCADE,
    work_date DATE NOT NULL,
    check_in_time TIMESTAMP WITH TIME ZONE,
    check_out_time TIMESTAMP WITH TIME ZONE,
    break_start_time TIMESTAMP WITH TIME ZONE,
    break_end_time TIMESTAMP WITH TIME ZONE,
    total_hours DECIMAL(5, 2) DEFAULT 0,
    overtime_hours DECIMAL(5, 2) DEFAULT 0,
    break_hours DECIMAL(5, 2) DEFAULT 0,
    status VARCHAR(20) DEFAULT 'absent',
    location JSONB DEFAULT '{}',
    notes TEXT,
    approved_by UUID REFERENCES employee.employees(employee_id),
    approved_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(employee_id, work_date)
);

-- Leave Types
CREATE TABLE attendance.leave_types (
    leave_type_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID REFERENCES auth.organizations(organization_id) ON DELETE CASCADE,
    type_name VARCHAR(100) NOT NULL,
    description TEXT,
    max_days_per_year INTEGER DEFAULT 0,
    is_paid BOOLEAN DEFAULT true,
    carry_forward_allowed BOOLEAN DEFAULT false,
    max_carry_forward_days INTEGER DEFAULT 0,
    requires_approval BOOLEAN DEFAULT true,
    advance_notice_days INTEGER DEFAULT 0,
    color VARCHAR(7) DEFAULT '#2196F3',
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(organization_id, type_name)
);

-- Leave Entitlements
CREATE TABLE attendance.leave_entitlements (
    entitlement_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    employee_id UUID REFERENCES employee.employees(employee_id) ON DELETE CASCADE,
    leave_type_id UUID REFERENCES attendance.leave_types(leave_type_id) ON DELETE CASCADE,
    year INTEGER NOT NULL,
    total_days DECIMAL(5, 2) DEFAULT 0,
    used_days DECIMAL(5, 2) DEFAULT 0,
    remaining_days DECIMAL(5, 2) DEFAULT 0,
    carry_forward_days DECIMAL(5, 2) DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(employee_id, leave_type_id, year)
);

-- Leave Requests
CREATE TABLE attendance.leave_requests (
    request_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    employee_id UUID REFERENCES employee.employees(employee_id) ON DELETE CASCADE,
    leave_type_id UUID REFERENCES attendance.leave_types(leave_type_id) ON DELETE CASCADE,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    days_requested DECIMAL(5, 2) NOT NULL,
    reason TEXT,
    attachment_url VARCHAR(500),
    status VARCHAR(20) DEFAULT 'pending',
    submitted_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    approved_by UUID REFERENCES employee.employees(employee_id),
    approved_at TIMESTAMP WITH TIME ZONE,
    rejection_reason TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Payroll Periods
CREATE TABLE payroll.payroll_periods (
    period_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID REFERENCES auth.organizations(organization_id) ON DELETE CASCADE,
    period_name VARCHAR(100) NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    pay_date DATE,
    status VARCHAR(20) DEFAULT 'draft',
    total_employees INTEGER DEFAULT 0,
    total_gross_amount DECIMAL(15, 2) DEFAULT 0,
    total_tax_amount DECIMAL(15, 2) DEFAULT 0,
    total_net_amount DECIMAL(15, 2) DEFAULT 0,
    processed_by UUID REFERENCES employee.employees(employee_id),
    processed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(organization_id, period_name)
);

-- Payroll Entries
CREATE TABLE payroll.payroll_entries (
    entry_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    period_id UUID REFERENCES payroll.payroll_periods(period_id) ON DELETE CASCADE,
    employee_id UUID REFERENCES employee.employees(employee_id) ON DELETE CASCADE,
    basic_salary DECIMAL(15, 2) DEFAULT 0,
    allowances JSONB DEFAULT '{}',
    deductions JSONB DEFAULT '{}',
    overtime_amount DECIMAL(15, 2) DEFAULT 0,
    bonus_amount DECIMAL(15, 2) DEFAULT 0,
    gross_salary DECIMAL(15, 2) DEFAULT 0,
    tax_amount DECIMAL(15, 2) DEFAULT 0,
    other_deductions DECIMAL(15, 2) DEFAULT 0,
    net_salary DECIMAL(15, 2) DEFAULT 0,
    currency VARCHAR(3) DEFAULT 'USD',
    status VARCHAR(20) DEFAULT 'draft',
    calculated_at TIMESTAMP WITH TIME ZONE,
    paid_at TIMESTAMP WITH TIME ZONE,
    payment_method VARCHAR(50),
    payment_reference VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(period_id, employee_id)
);

-- Skill Sets
CREATE TABLE performance.skill_sets (
    skill_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID REFERENCES auth.organizations(organization_id) ON DELETE CASCADE,
    skill_name VARCHAR(100) NOT NULL,
    category VARCHAR(100),
    description TEXT,
    skill_level_scale JSONB DEFAULT '[1,2,3,4,5]',
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(organization_id, skill_name)
);

-- Employee Skills
CREATE TABLE performance.employee_skills (
    employee_skill_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    employee_id UUID REFERENCES employee.employees(employee_id) ON DELETE CASCADE,
    skill_id UUID REFERENCES performance.skill_sets(skill_id) ON DELETE CASCADE,
    proficiency_level INTEGER DEFAULT 1,
    assessed_by UUID REFERENCES employee.employees(employee_id),
    last_assessed DATE,
    notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(employee_id, skill_id)
);

-- KRA (Key Result Areas)
CREATE TABLE performance.kras (
    kra_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID REFERENCES auth.organizations(organization_id) ON DELETE CASCADE,
    position_id UUID REFERENCES employee.positions(position_id),
    kra_title VARCHAR(255) NOT NULL,
    description TEXT,
    weight_percentage INTEGER DEFAULT 0,
    measurement_criteria TEXT,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Appraisal Cycles
CREATE TABLE performance.appraisal_cycles (
    cycle_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID REFERENCES auth.organizations(organization_id) ON DELETE CASCADE,
    cycle_name VARCHAR(100) NOT NULL,
    description TEXT,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    review_period_start DATE,
    review_period_end DATE,
    status VARCHAR(20) DEFAULT 'active',
    rating_scale JSONB DEFAULT '{"type": "5-point", "scale": [1,2,3,4,5]}',
    self_review_enabled BOOLEAN DEFAULT true,
    peer_review_enabled BOOLEAN DEFAULT false,
    manager_review_enabled BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(organization_id, cycle_name)
);

-- Employee Appraisals
CREATE TABLE performance.employee_appraisals (
    appraisal_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    cycle_id UUID REFERENCES performance.appraisal_cycles(cycle_id) ON DELETE CASCADE,
    employee_id UUID REFERENCES employee.employees(employee_id) ON DELETE CASCADE,
    reviewer_id UUID REFERENCES employee.employees(employee_id),
    review_type VARCHAR(20) DEFAULT 'manager', -- self, peer, manager
    overall_rating DECIMAL(3, 2),
    kra_ratings JSONB DEFAULT '{}',
    skill_ratings JSONB DEFAULT '{}',
    goals_achieved JSONB DEFAULT '{}',
    feedback TEXT,
    improvement_areas TEXT,
    development_plan TEXT,
    status VARCHAR(20) DEFAULT 'draft',
    submitted_at TIMESTAMP WITH TIME ZONE,
    reviewed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(cycle_id, employee_id, reviewer_id, review_type)
);

-- Document Management
CREATE TABLE employee.documents (
    document_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    employee_id UUID REFERENCES employee.employees(employee_id) ON DELETE CASCADE,
    document_type VARCHAR(100) NOT NULL,
    document_name VARCHAR(255) NOT NULL,
    file_url VARCHAR(500) NOT NULL,
    file_size BIGINT,
    mime_type VARCHAR(100),
    uploaded_by UUID REFERENCES employee.employees(employee_id),
    is_confidential BOOLEAN DEFAULT false,
    expiry_date DATE,
    status VARCHAR(20) DEFAULT 'active',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Audit Log for tracking changes
CREATE TABLE admin.audit_logs (
    log_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    organization_id UUID REFERENCES auth.organizations(organization_id),
    user_id UUID REFERENCES auth.users(user_id),
    table_name VARCHAR(100) NOT NULL,
    record_id UUID NOT NULL,
    action VARCHAR(20) NOT NULL, -- INSERT, UPDATE, DELETE
    old_values JSONB,
    new_values JSONB,
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Add department manager foreign key constraint
ALTER TABLE employee.departments 
ADD CONSTRAINT fk_department_manager FOREIGN KEY (manager_id) REFERENCES employee.employees(employee_id);

-- Create indexes for better performance
CREATE INDEX idx_users_organization ON auth.users(organization_id);
CREATE INDEX idx_users_email ON auth.users(email);
CREATE INDEX idx_employees_organization ON employee.employees(organization_id);
CREATE INDEX idx_employees_code ON employee.employees(employee_code);
CREATE INDEX idx_attendance_employee_date ON attendance.attendance(employee_id, work_date);
CREATE INDEX idx_leave_requests_employee ON attendance.leave_requests(employee_id);
CREATE INDEX idx_payroll_entries_period ON payroll.payroll_entries(period_id);
CREATE INDEX idx_audit_logs_table_record ON admin.audit_logs(table_name, record_id);
