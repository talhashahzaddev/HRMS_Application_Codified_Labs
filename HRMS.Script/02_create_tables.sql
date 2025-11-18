-- HRMS Database Tables Creation Script


-- Organizations (Multi-tenant support)
-- ==============================
-- AUTH SCHEMA
-- ==============================

CREATE TABLE IF NOT EXISTS auth.organizations
(
    organizationid uuid NOT NULL DEFAULT uuid_generate_v4(),
    name character varying(255) COLLATE pg_catalog."default" NOT NULL,
    subscriptiontype character varying(50) COLLATE pg_catalog."default" DEFAULT 'basic'::character varying,
    domain character varying(255) COLLATE pg_catalog."default",
    logourl character varying(500) COLLATE pg_catalog."default",
    status character varying(20) COLLATE pg_catalog."default" DEFAULT 'active'::character varying,
    settings jsonb DEFAULT '{}'::jsonb,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    currency character varying(50) COLLATE pg_catalog."default" DEFAULT NULL::character varying,
    CONSTRAINT organizations_pkey PRIMARY KEY (organizationid)
)

CREATE TABLE auth.Roles (
    RoleId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    OrganizationId UUID,
    RoleName VARCHAR(100) NOT NULL,
    Description TEXT,
    Permissions JSONB DEFAULT '{}',
    IsSystemRole BOOLEAN DEFAULT false,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_Roles UNIQUE(OrganizationId, RoleName)
);

CREATE TABLE auth.Users (
    UserId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    OrganizationId UUID,
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    RoleId UUID,
    FirstName VARCHAR(100),
    LastName VARCHAR(100),
    IsActive BOOLEAN DEFAULT true,
    EmailVerified BOOLEAN DEFAULT false,
    LastLoginAt TIMESTAMP WITH TIME ZONE,
    PasswordChangedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    RefreshToken VARCHAR(500),
    RefreshTokenExpiresAt TIMESTAMP WITH TIME ZONE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Table: auth.password_reset_tokens

-- DROP TABLE IF EXISTS auth.password_reset_tokens;

CREATE TABLE IF NOT EXISTS auth.password_reset_tokens
(
    tokenid uuid NOT NULL DEFAULT gen_random_uuid(),
    userid uuid NOT NULL,
    token text COLLATE pg_catalog."default" NOT NULL,
    expiresat timestamp with time zone NOT NULL,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    used boolean DEFAULT false,
    CONSTRAINT password_reset_tokens_pkey PRIMARY KEY (tokenid),
    CONSTRAINT password_reset_tokens_userid_fkey FOREIGN KEY (userid)
        REFERENCES auth.users (userid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)

TABLESPACE pg_default;

-- ==============================
-- EMPLOYEE SCHEMA
-- ==============================

CREATE TABLE employee.Departments (
    DepartmentId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    OrganizationId UUID,
    DepartmentName VARCHAR(100) NOT NULL,
    Description TEXT,
    ManagerId UUID,
    ParentDepartmentId UUID,
    Budget DECIMAL(15, 2),
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_Departments UNIQUE(OrganizationId, DepartmentName)
);

CREATE TABLE employee.Positions (
    positionid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid,
    positiontitle character varying(100) COLLATE pg_catalog."default" NOT NULL,
    departmentid uuid,
    description text COLLATE pg_catalog."default",
    requirements jsonb DEFAULT '{}'::jsonb,
    salaryrange jsonb DEFAULT '{}'::jsonb,
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    roleid uuid,
    CONSTRAINT positions_pkey PRIMARY KEY (positionid)
);

CREATE TABLE employee.Employees (
    EmployeeId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    OrganizationId UUID,
    UserId UUID,
    EmployeeCode VARCHAR(50) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Email VARCHAR(255) NOT NULL,
    Phone VARCHAR(20),
    Address JSONB DEFAULT '{}',
    DateOfBirth DATE,
    Gender VARCHAR(20),
    Nationality VARCHAR(50),
    MaritalStatus VARCHAR(20),
    EmergencyContact JSONB DEFAULT '{}',
    HireDate DATE NOT NULL,
    TerminationDate DATE,
    Status VARCHAR(20) DEFAULT 'active',
    ProfilePictureUrl VARCHAR(500),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_EmployeeCode UNIQUE(OrganizationId, EmployeeCode),
    CONSTRAINT UQ_EmployeeEmail UNIQUE(OrganizationId, Email)
);

CREATE TABLE employee.EmploymentDetails (
    DetailId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    EmployeeId UUID,
    DepartmentId UUID,
    PositionId UUID,
    ShiftId UUID,
    EmploymentType VARCHAR(50) DEFAULT 'full_time',
    PayType VARCHAR(20) DEFAULT 'salary',
    BasicSalary DECIMAL(15, 2),
    Currency VARCHAR(3) DEFAULT 'USD',
    WorkLocation VARCHAR(100),
    ReportingManagerId UUID,
    EffectiveDate DATE NOT NULL,
    EndDate DATE,
    IsCurrent BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE employee.Documents (
    DocumentId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    EmployeeId UUID,
    DocumentType VARCHAR(100) NOT NULL,
    DocumentName VARCHAR(255) NOT NULL,
    FileUrl VARCHAR(500) NOT NULL,
    FileSize BIGINT,
    MimeType VARCHAR(100),
    UploadedBy UUID,
    IsConfidential BOOLEAN DEFAULT false,
    ExpiryDate DATE,
    Status VARCHAR(20) DEFAULT 'active',
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ==============================
-- ATTENDANCE SCHEMA
-- ==============================

CREATE TABLE attendance.Shifts (
    ShiftId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    OrganizationId UUID,
    ShiftName VARCHAR(100) NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    BreakDuration INTEGER DEFAULT 0,
    DaysOfWeek INTEGER[] DEFAULT '{1,2,3,4,5}',
    Timezone VARCHAR(50) DEFAULT 'UTC',
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_Shifts UNIQUE(OrganizationId, ShiftName)
);

CREATE TABLE attendance.Attendance (
    AttendanceId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    EmployeeId UUID,
    WorkDate DATE NOT NULL,
    CheckInTime TIMESTAMP WITH TIME ZONE,
    CheckOutTime TIMESTAMP WITH TIME ZONE,
    BreakStartTime TIMESTAMP WITH TIME ZONE,
    BreakEndTime TIMESTAMP WITH TIME ZONE,
    TotalHours DECIMAL(5, 2) DEFAULT 0,
    OvertimeHours DECIMAL(5, 2) DEFAULT 0,
    BreakHours DECIMAL(5, 2) DEFAULT 0,
    Status VARCHAR(20) DEFAULT 'absent',
    Location JSONB DEFAULT '{}',
    Notes TEXT,
    ApprovedBy UUID,
    ApprovedAt TIMESTAMP WITH TIME ZONE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_Attendance UNIQUE(EmployeeId, WorkDate)
);

CREATE TABLE attendance.LeaveTypes (
    LeaveTypeId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    OrganizationId UUID,
    TypeName VARCHAR(100) NOT NULL,
    Description TEXT,
    MaxDaysPerYear INTEGER DEFAULT 0,
    IsPaid BOOLEAN DEFAULT true,
    CarryForwardAllowed BOOLEAN DEFAULT false,
    MaxCarryForwardDays INTEGER DEFAULT 0,
    RequiresApproval BOOLEAN DEFAULT true,
    AdvanceNoticeDays INTEGER DEFAULT 0,
    Color VARCHAR(7) DEFAULT '#2196F3',
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_LeaveTypes UNIQUE(OrganizationId, TypeName)
);

CREATE TABLE attendance.LeaveEntitlements (
    EntitlementId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    EmployeeId UUID,
    LeaveTypeId UUID,
    Year INTEGER NOT NULL,
    TotalDays DECIMAL(5, 2) DEFAULT 0,
    UsedDays DECIMAL(5, 2) DEFAULT 0,
    RemainingDays DECIMAL(5, 2) DEFAULT 0,
    CarryForwardDays DECIMAL(5, 2) DEFAULT 0,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_LeaveEntitlements UNIQUE(EmployeeId, LeaveTypeId, Year)
);

CREATE TABLE attendance.LeaveRequests (
    RequestId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    EmployeeId UUID,
    LeaveTypeId UUID,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    DaysRequested DECIMAL(5, 2) NOT NULL,
    Reason TEXT,
    AttachmentUrl VARCHAR(500),
    Status VARCHAR(20) DEFAULT 'pending',
    SubmittedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    ApprovedBy UUID,
    ApprovedAt TIMESTAMP WITH TIME ZONE,
    RejectionReason TEXT,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ==============================
-- PAYROLL SCHEMA
-- ==============================

CREATE TABLE payroll.PayrollPeriods (
    PeriodId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    OrganizationId UUID,
    PeriodName VARCHAR(100) NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    PayDate DATE,
    Status VARCHAR(20) DEFAULT 'draft',
    TotalEmployees INTEGER DEFAULT 0,
    TotalGrossAmount DECIMAL(15, 2) DEFAULT 0,
    TotalTaxAmount DECIMAL(15, 2) DEFAULT 0,
    TotalNetAmount DECIMAL(15, 2) DEFAULT 0,
    ProcessedBy UUID,
    ProcessedAt TIMESTAMP WITH TIME ZONE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_PayrollPeriods UNIQUE(OrganizationId, PeriodName)
);

CREATE TABLE payroll.PayrollEntries (
    EntryId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    PeriodId UUID,
    EmployeeId UUID,
    BasicSalary DECIMAL(15, 2) DEFAULT 0,
    Allowances JSONB DEFAULT '{}',
    Deductions JSONB DEFAULT '{}',
    OvertimeAmount DECIMAL(15, 2) DEFAULT 0,
    BonusAmount DECIMAL(15, 2) DEFAULT 0,
    GrossSalary DECIMAL(15, 2) DEFAULT 0,
    TaxAmount DECIMAL(15, 2) DEFAULT 0,
    OtherDeductions DECIMAL(15, 2) DEFAULT 0,
    NetSalary DECIMAL(15, 2) DEFAULT 0,
    Currency VARCHAR(3) DEFAULT 'USD',
    Status VARCHAR(20) DEFAULT 'draft',
    CalculatedAt TIMESTAMP WITH TIME ZONE,
    PaidAt TIMESTAMP WITH TIME ZONE,
    PaymentMethod VARCHAR(50),
    PaymentReference VARCHAR(100),
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_PayrollEntries UNIQUE(PeriodId, EmployeeId)
);
-- Table: payroll.salaryrules

-- DROP TABLE IF EXISTS payroll.salaryrules;

CREATE TABLE IF NOT EXISTS payroll.salaryrules
(
    ruleid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid NOT NULL,
    departmentid uuid,
    positionid uuid,
    componentid uuid,
    value_override numeric(10,2),
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    rulename character varying(100) COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT salaryrules_pkey PRIMARY KEY (ruleid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS payroll.salaryrules
    OWNER to postgres;
    -- Table: payroll.salarycomponents

-- DROP TABLE IF EXISTS payroll.salarycomponents;

CREATE TABLE IF NOT EXISTS payroll.salarycomponents
(
    componentid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid NOT NULL,
    name character varying(100) COLLATE pg_catalog."default" NOT NULL,
    code character varying(50) COLLATE pg_catalog."default" NOT NULL,
    type character varying(20) COLLATE pg_catalog."default" NOT NULL,
    calculationtype character varying(20) COLLATE pg_catalog."default" NOT NULL,
    value numeric(10,2) NOT NULL DEFAULT 0,
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT salarycomponents_pkey PRIMARY KEY (componentid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS payroll.salarycomponents
    OWNER to postgres;

-- ==============================
-- PERFORMANCE SCHEMA
-- ==============================

-- Table: performance.appraisalcycles

-- DROP TABLE IF EXISTS performance.appraisalcycles;

CREATE TABLE IF NOT EXISTS performance.appraisalcycles
(
    cycleid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid,
    cyclename character varying(100) COLLATE pg_catalog."default" NOT NULL,
    description text COLLATE pg_catalog."default",
    startdate date NOT NULL,
    enddate date NOT NULL,
    reviewperiodstart date,
    reviewperiodend date,
    status character varying(20) COLLATE pg_catalog."default" DEFAULT 'active'::character varying,
    ratingscale jsonb DEFAULT '{"type": "5-point", "scale": [1, 2, 3, 4, 5]}'::jsonb,
    selfreviewenabled boolean DEFAULT true,
    peerreviewenabled boolean DEFAULT false,
    managerreviewenabled boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT appraisalcycles_pkey PRIMARY KEY (cycleid),
    CONSTRAINT uq_appraisalcycles UNIQUE (organizationid, cyclename)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS performance.appraisalcycles
    OWNER to postgres;
-- Table: performance.employeeappraisals

-- DROP TABLE IF EXISTS performance.employeeappraisals;

CREATE TABLE IF NOT EXISTS performance.employeeappraisals
(
    appraisalid uuid NOT NULL DEFAULT uuid_generate_v4(),
    cycleid uuid,
    employeeid uuid,
    reviewerid uuid,
    reviewtype character varying(20) COLLATE pg_catalog."default" DEFAULT 'manager'::character varying,
    overallrating numeric(3,2),
    kraratings jsonb DEFAULT '{}'::jsonb,
    skillratings jsonb DEFAULT '{}'::jsonb,
    goalsachieved jsonb DEFAULT '{}'::jsonb,
    feedback text COLLATE pg_catalog."default",
    improvementareas text COLLATE pg_catalog."default",
    developmentplan text COLLATE pg_catalog."default",
    status character varying(20) COLLATE pg_catalog."default" DEFAULT 'draft'::character varying,
    submittedat timestamp with time zone,
    reviewedat timestamp with time zone,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT employeeappraisals_pkey PRIMARY KEY (appraisalid),
    CONSTRAINT uq_employeeappraisals UNIQUE (cycleid, employeeid, reviewerid, reviewtype)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS performance.employeeappraisals
    OWNER to postgres;
-- Table: performance.employeeskills

-- DROP TABLE IF EXISTS performance.employeeskills;

CREATE TABLE IF NOT EXISTS performance.employeeskills
(
    employeeskillid uuid NOT NULL DEFAULT uuid_generate_v4(),
    employeeid uuid,
    skillid uuid,
    proficiencylevel integer DEFAULT 1,
    assessedby uuid,
    lastassessed date,
    notes text COLLATE pg_catalog."default",
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT employeeskills_pkey PRIMARY KEY (employeeskillid),
    CONSTRAINT uq_employeeskills UNIQUE (employeeid, skillid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS performance.employeeskills
    OWNER to postgres;
-- Table: performance.kras

-- DROP TABLE IF EXISTS performance.kras;

CREATE TABLE IF NOT EXISTS performance.kras
(
    kraid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid,
    positionid uuid,
    kratitle character varying(255) COLLATE pg_catalog."default" NOT NULL,
    description text COLLATE pg_catalog."default",
    weightpercentage integer DEFAULT 0,
    measurementcriteria text COLLATE pg_catalog."default",
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT kras_pkey PRIMARY KEY (kraid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS performance.kras
    OWNER to postgres;
-- Table: performance.managerreviews

-- DROP TABLE IF EXISTS performance.managerreviews;

CREATE TABLE IF NOT EXISTS performance.managerreviews
(
    reviewid uuid NOT NULL DEFAULT uuid_generate_v4(),
    managerid uuid NOT NULL,
    employeeid uuid NOT NULL,
    cycleid uuid NOT NULL,
    overallrating numeric(3,2),
    feedback text COLLATE pg_catalog."default",
    improvementareas text COLLATE pg_catalog."default",
    developmentplan text COLLATE pg_catalog."default",
    status character varying(20) COLLATE pg_catalog."default" DEFAULT 'submitted'::character varying,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT managerreviews_pkey PRIMARY KEY (reviewid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS performance.managerreviews
    OWNER to postgres;
-- Table: performance.selfassessments

-- DROP TABLE IF EXISTS performance.selfassessments;

CREATE TABLE IF NOT EXISTS performance.selfassessments
(
    selfassessmentid uuid NOT NULL DEFAULT uuid_generate_v4(),
    employeeid uuid NOT NULL,
    cycleid uuid NOT NULL,
    kraid uuid,
    skillid uuid,
    rating numeric(3,2),
    comments text COLLATE pg_catalog."default",
    evidenceurls jsonb DEFAULT '[]'::jsonb,
    status character varying(20) COLLATE pg_catalog."default" DEFAULT 'draft'::character varying,
    submittedat timestamp with time zone,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT selfassessments_pkey PRIMARY KEY (selfassessmentid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS performance.selfassessments
    OWNER to postgres;
-- Table: performance.skillsets

-- DROP TABLE IF EXISTS performance.skillsets;

CREATE TABLE IF NOT EXISTS performance.skillsets
(
    skillid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid,
    skillname character varying(100) COLLATE pg_catalog."default" NOT NULL,
    category character varying(100) COLLATE pg_catalog."default",
    description text COLLATE pg_catalog."default",
    skilllevelscale jsonb DEFAULT '[1, 2, 3, 4, 5]'::jsonb,
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT skillsets_pkey PRIMARY KEY (skillid),
    CONSTRAINT uq_skillsets UNIQUE (organizationid, skillname)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS performance.skillsets
    OWNER to postgres;

-- ==============================
-- ADMIN SCHEMA
-- ==============================

CREATE TABLE admin.AuditLogs (
    LogId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    OrganizationId UUID,
    UserId UUID,
    TableName VARCHAR(100) NOT NULL,
    RecordId UUID NOT NULL,
    Action VARCHAR(20) NOT NULL,
    OldValues JSONB,
    NewValues JSONB,
    IpAddress INET,
    UserAgent TEXT,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE attendance.AttendanceSessions (
    SessionId     UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    AttendanceId  UUID NOT NULL REFERENCES attendance.Attendance(AttendanceId) ON DELETE CASCADE,
    CheckInTime   TIMESTAMP WITH TIME ZONE NOT NULL,
    CheckOutTime  TIMESTAMP WITH TIME ZONE,
    Location      JSONB DEFAULT '{}',
    CreatedAt     TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt     TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

--Employee Goals For performance
CREATE TABLE IF NOT EXISTS performance.employeegoals
(
    goalid uuid NOT NULL DEFAULT gen_random_uuid(),
    employeeid uuid NOT NULL,
    title character varying(255) COLLATE pg_catalog."default" NOT NULL,
    description text COLLATE pg_catalog."default",
    startdate date,
    enddate date,
    status character varying(20) COLLATE pg_catalog."default" DEFAULT 'pending'::character varying,
    totalgoals integer DEFAULT 0,
    achievedgoals integer DEFAULT 0,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT employeegoals_pkey PRIMARY KEY (goalid)
)