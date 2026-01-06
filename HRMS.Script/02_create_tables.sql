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
-- Table: attendance.attendance

CREATE TABLE IF NOT EXISTS attendance.shifts
(
    shiftid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid,
    shiftname character varying(100) COLLATE pg_catalog."default" NOT NULL,
    starttime time without time zone NOT NULL,
    endtime time without time zone NOT NULL,
    breakduration integer DEFAULT 0,
    breakstarttime time without time zone,
    breakendtime time without time zone,
    overtimestarttime time without time zone,
    maxovertimeinminutes integer DEFAULT 0,
    daysofweek integer[] DEFAULT '{1,2,3,4,5}'::integer[],
    timezone character varying(50) COLLATE pg_catalog."default" DEFAULT 'UTC'::character varying,
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT shifts_pkey PRIMARY KEY (shiftid),
    CONSTRAINT uq_shifts UNIQUE (organizationid, shiftname)
)

TABLESPACE pg_default;


CREATE TABLE IF NOT EXISTS attendance.attendance
(
    attendanceid uuid NOT NULL DEFAULT uuid_generate_v4(),
    employeeid uuid,
    workdate date NOT NULL,
    checkintime timestamp with time zone,
    checkouttime timestamp with time zone,
    breakstarttime timestamp with time zone,
    breakendtime timestamp with time zone,
    totalhours numeric(5,2) DEFAULT 0,
    overtimehours numeric(5,2) DEFAULT 0,
    breakhours numeric(5,2) DEFAULT 0,
    status character varying(20) COLLATE pg_catalog."default" DEFAULT 'absent'::character varying,
    location jsonb DEFAULT '{}'::jsonb,
    notes text COLLATE pg_catalog."default",
    approvedby uuid,
    approvedat timestamp with time zone,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    islate boolean DEFAULT false,
    CONSTRAINT attendance_pkey PRIMARY KEY (attendanceid),
    CONSTRAINT uq_attendance UNIQUE (employeeid, workdate)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS attendance.attendance
    OWNER to postgres;

-- Table: attendance.shifts

-- DROP TABLE IF EXISTS attendance.shifts;

CREATE TABLE IF NOT EXISTS attendance.shifts
(
    shiftid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid,
    shiftname character varying(100) COLLATE pg_catalog."default" NOT NULL,
    starttime time without time zone NOT NULL,
    endtime time without time zone NOT NULL,
    breakduration integer DEFAULT 0,
    daysofweek integer[] DEFAULT '{1,2,3,4,5}'::integer[],
    timezone character varying(50) COLLATE pg_catalog."default" DEFAULT 'UTC'::character varying,
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT shifts_pkey PRIMARY KEY (shiftid),
    CONSTRAINT uq_shifts UNIQUE (organizationid, shiftname)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS attendance.shifts
    OWNER to postgres;



-- Table: attendance.attendancesessions

-- DROP TABLE IF EXISTS attendance.attendancesessions;

CREATE TABLE IF NOT EXISTS attendance.attendancesessions
(
    sessionid uuid NOT NULL DEFAULT uuid_generate_v4(),
    attendanceid uuid NOT NULL,
    checkintime timestamp with time zone NOT NULL,
    checkouttime timestamp with time zone,
    location jsonb DEFAULT '{}'::jsonb,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT attendancesessions_pkey PRIMARY KEY (sessionid),
    CONSTRAINT attendancesessions_attendanceid_fkey FOREIGN KEY (attendanceid)
        REFERENCES attendance.attendance (attendanceid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS attendance.attendancesessions
    OWNER to postgres;


-- Table: attendance.leavetypes

-- DROP TABLE IF EXISTS attendance.leavetypes;

CREATE TABLE IF NOT EXISTS attendance.leavetypes
(
    leavetypeid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid,
    typename character varying(100) COLLATE pg_catalog."default" NOT NULL,
    description text COLLATE pg_catalog."default",
    maxdaysperyear integer DEFAULT 0,
    ispaid boolean DEFAULT true,
    carryforwardallowed boolean DEFAULT false,
    maxcarryforwarddays integer DEFAULT 0,
    requiresapproval boolean DEFAULT true,
    advancenoticedays integer DEFAULT 0,
    color character varying(7) COLLATE pg_catalog."default" DEFAULT '#2196F3'::character varying,
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT leavetypes_pkey PRIMARY KEY (leavetypeid),
    CONSTRAINT uq_leavetypes UNIQUE (organizationid, typename)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS attendance.leavetypes
    OWNER to postgres;

-- Table: attendance.leaveentitlements

-- DROP TABLE IF EXISTS attendance.leaveentitlements;

CREATE TABLE IF NOT EXISTS attendance.leaveentitlements
(
    entitlementid uuid NOT NULL DEFAULT uuid_generate_v4(),
    employeeid uuid,
    leavetypeid uuid,
    year integer NOT NULL,
    totaldays numeric(5,2) DEFAULT 0,
    useddays numeric(5,2) DEFAULT 0,
    remainingdays numeric(5,2) DEFAULT 0,
    carryforwarddays numeric(5,2) DEFAULT 0,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT leaveentitlements_pkey PRIMARY KEY (entitlementid),
    CONSTRAINT uq_leaveentitlements UNIQUE (employeeid, leavetypeid, year)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS attendance.leaveentitlements
    OWNER to postgres;

-- Table: attendance.leaverequests

-- DROP TABLE IF EXISTS attendance.leaverequests;

CREATE TABLE IF NOT EXISTS attendance.leaverequests
(
    requestid uuid NOT NULL DEFAULT uuid_generate_v4(),
    employeeid uuid,
    leavetypeid uuid,
    startdate date NOT NULL,
    enddate date NOT NULL,
    daysrequested numeric(5,2) NOT NULL,
    reason text COLLATE pg_catalog."default",
    attachmenturl character varying(500) COLLATE pg_catalog."default",
    status character varying(20) COLLATE pg_catalog."default" DEFAULT 'pending'::character varying,
    submittedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    approvedby uuid,
    approvedat timestamp with time zone,
    rejectionreason text COLLATE pg_catalog."default",
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT leaverequests_pkey PRIMARY KEY (requestid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS attendance.leaverequests
    OWNER to postgres;

-- Table: attendance.shiftswaprequests

-- DROP TABLE IF EXISTS attendance.shiftswaprequests;

CREATE TABLE IF NOT EXISTS attendance.shiftswaprequests
(
    requestid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    currentshiftid uuid,
    requestedshiftid uuid NOT NULL,
    reason text COLLATE pg_catalog."default",
    status character varying(20) COLLATE pg_catalog."default" DEFAULT 'pending'::character varying,
    submittedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    approvedby uuid,
    approvedat timestamp with time zone,
    rejectionreason text COLLATE pg_catalog."default",
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    reportingmanagerid uuid,
    CONSTRAINT shiftswaprequests_pkey PRIMARY KEY (requestid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS attendance.shiftswaprequests
    OWNER to postgres;
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
    isabsenttax boolean DEFAULT false,
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

--NOtifications Table
CREATE TABLE IF NOT EXISTS notification.notifications
(
    notificationid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid NOT NULL,
    receiveruserid uuid NOT NULL,
    senderuserid uuid,
    title character varying(50) COLLATE pg_catalog."default",
    notificationtype character varying(50) COLLATE pg_catalog."default",
    message character varying(500) COLLATE pg_catalog."default" NOT NULL,
    redirecturl character varying(200) COLLATE pg_catalog."default",
    isread boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    createdby uuid,
    requestid uuid,
    moduletype character varying(50) COLLATE pg_catalog."default",
    CONSTRAINT notifications_pkey PRIMARY KEY (notificationid)
)

CREATE TABLE chatboard.chat_board_logs
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employeeid UUID NOT NULL,
    organizationid UUID NOT NULL,
    rolename VARCHAR(100) ,
    usermessage TEXT ,
    intent VARCHAR(100) ,
    airesponse TEXT ,
    createdat TIMESTAMPTZ  DEFAULT NOW(),
	modulename  VARCHAR(100)
);

CREATE TABLE admin.system_jobs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    jobkey VARCHAR(100) NOT NULL,
    year INT NOT NULL,
    runat TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

TABLESPACE pg_default;

-- ==============================
-- PAYMENT SCHEMA
-- ==============================
-- Table: payment.subscriptionplans

-- DROP TABLE IF EXISTS payment.subscriptionplans;

CREATE TABLE IF NOT EXISTS payment.subscriptionplans
(
    planid uuid NOT NULL DEFAULT uuid_generate_v4(),
    name text COLLATE pg_catalog."default" NOT NULL,
    monthlyprice numeric,
    annualprice numeric,
    maxusers integer,
    features jsonb,
    iscustom boolean DEFAULT false,
    isactive boolean DEFAULT true,
    CONSTRAINT subscriptionplans_pkey PRIMARY KEY (planid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS payment.subscriptionplans
    OWNER to postgres;

    -- Table: payment.companysubscriptions

-- DROP TABLE IF EXISTS payment.companysubscriptions;

CREATE TABLE IF NOT EXISTS payment.companysubscriptions
(
    subscriptionid uuid NOT NULL DEFAULT uuid_generate_v4(),
    companyid uuid,
    planid uuid,
    status text COLLATE pg_catalog."default",
    billingcycle text COLLATE pg_catalog."default",
    startdate timestamp without time zone,
    enddate timestamp without time zone,
    CONSTRAINT companysubscriptions_pkey PRIMARY KEY (subscriptionid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS payment.companysubscriptions
    OWNER to postgres;