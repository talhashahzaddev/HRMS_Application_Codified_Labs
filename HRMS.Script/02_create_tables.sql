-- HRMS Database Tables Creation Script


-- Organizations (Multi-tenant support)
-- ==============================
-- AUTH SCHEMA
-- ==============================

CREATE TABLE auth.Organizations (
    OrganizationId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    Name VARCHAR(255) NOT NULL,
    SubscriptionType VARCHAR(50) DEFAULT 'basic',
    Domain VARCHAR(255),
    LogoUrl VARCHAR(500),
    Status VARCHAR(20) DEFAULT 'active',
    Settings JSONB DEFAULT '{}',
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

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

CREATE TABLE performance.SkillSets (
    SkillId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    OrganizationId UUID,
    SkillName VARCHAR(100) NOT NULL,
    Category VARCHAR(100),
    Description TEXT,
    SkillLevelScale JSONB DEFAULT '[1,2,3,4,5]',
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_SkillSets UNIQUE(OrganizationId, SkillName)
);

CREATE TABLE performance.EmployeeSkills (
    EmployeeSkillId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    EmployeeId UUID,
    SkillId UUID,
    ProficiencyLevel INTEGER DEFAULT 1,
    AssessedBy UUID,
    LastAssessed DATE,
    Notes TEXT,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_EmployeeSkills UNIQUE(EmployeeId, SkillId)
);

CREATE TABLE performance.KRAs (
    KraId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    OrganizationId UUID,
    PositionId UUID,
    KraTitle VARCHAR(255) NOT NULL,
    Description TEXT,
    WeightPercentage INTEGER DEFAULT 0,
    MeasurementCriteria TEXT,
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE performance.AppraisalCycles (
    CycleId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    OrganizationId UUID,
    CycleName VARCHAR(100) NOT NULL,
    Description TEXT,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    ReviewPeriodStart DATE,
    ReviewPeriodEnd DATE,
    Status VARCHAR(20) DEFAULT 'active',
    RatingScale JSONB DEFAULT '{"type": "5-point", "scale": [1,2,3,4,5]}',
    SelfReviewEnabled BOOLEAN DEFAULT true,
    PeerReviewEnabled BOOLEAN DEFAULT false,
    ManagerReviewEnabled BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_AppraisalCycles UNIQUE(OrganizationId, CycleName)
);

CREATE TABLE performance.EmployeeAppraisals (
    AppraisalId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    CycleId UUID,
    EmployeeId UUID,
    ReviewerId UUID,
    ReviewType VARCHAR(20) DEFAULT 'manager',
    OverallRating DECIMAL(3, 2),
    KraRatings JSONB DEFAULT '{}',
    SkillRatings JSONB DEFAULT '{}',
    GoalsAchieved JSONB DEFAULT '{}',
    Feedback TEXT,
    ImprovementAreas TEXT,
    DevelopmentPlan TEXT,
    Status VARCHAR(20) DEFAULT 'draft',
    SubmittedAt TIMESTAMP WITH TIME ZONE,
    ReviewedAt TIMESTAMP WITH TIME ZONE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UQ_EmployeeAppraisals UNIQUE(CycleId, EmployeeId, ReviewerId, ReviewType)
);

CREATE TABLE performance.SelfAssessments (
    SelfAssessmentId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    EmployeeId UUID NOT NULL,
    CycleId UUID NOT NULL,
    KraId UUID NULL,
    SkillId UUID NULL,
    Rating DECIMAL(3,2),
    Comments TEXT,
    EvidenceUrls JSONB DEFAULT '[]',
    Status VARCHAR(20) DEFAULT 'draft',
    SubmittedAt TIMESTAMP WITH TIME ZONE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE performance.ManagerReviews (
    ReviewId UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ManagerId UUID NOT NULL,
    EmployeeId UUID NOT NULL,
    CycleId UUID NOT NULL,
    OverallRating DECIMAL(3,2),
    Feedback TEXT,
    ImprovementAreas TEXT,
    DevelopmentPlan TEXT,
    Status VARCHAR(20) DEFAULT 'submitted',
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

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