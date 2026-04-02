-- HRMS Database Tables Creation Script


-- Organizations (Multi-tenant support)
-- ==============================
-- AUTH SCHEMA
-- ==============================

-- Table: auth.organizations

-- DROP TABLE IF EXISTS auth.organizations;

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
    careerbgimageurl character varying(500) COLLATE pg_catalog."default",
    careerheadertext character varying(255) COLLATE pg_catalog."default",
    careerdescription text COLLATE pg_catalog."default",
    CONSTRAINT organizations_pkey PRIMARY KEY (organizationid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS auth.organizations
    OWNER to postgres;

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

ALTER TABLE auth.users
ADD COLUMN emailVerify BOOLEAN DEFAULT NULL,
ADD COLUMN emailVerifyToken CHARACTER VARYING,
ADD COLUMN emailVerifyExpiry TIMESTAMP WITH TIME ZONE;

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
-- ASSET SCHEMA
-- ==============================
CREATE TABLE IF NOT EXISTS assets.asset_types
(
    asset_type_id uuid NOT NULL DEFAULT uuid_generate_v4(),
    organization_id uuid NOT NULL,
    name character varying(120) NOT NULL,
    description character varying(500),
    is_active boolean DEFAULT true,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    created_by uuid,
    is_deleted boolean DEFAULT false,
    CONSTRAINT asset_types_pkey PRIMARY KEY (asset_type_id),
    CONSTRAINT uq_asset_types_name UNIQUE (organization_id, name)
);

DROP TABLE IF EXISTS asset.assets CASCADE;

CREATE TABLE asset.assets
(
    asset_id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),

    organization_id uuid NOT NULL,
    asset_type_id uuid NOT NULL,

    asset_tag varchar(50) NOT NULL CHECK (trim(asset_tag) <> ''),
    name varchar(200) NOT NULL CHECK (trim(name) <> ''),

    description text,
    serial_number varchar(120),

    -- CHANGED: DATE ? TIMESTAMPTZ (matches C# DateTime)
    purchase_date timestamp with time zone,

    purchase_cost numeric(12,2),

    status varchar(30) NOT NULL DEFAULT 'Available',

    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,

    created_by uuid,
    is_deleted boolean NOT NULL DEFAULT false,

    CONSTRAINT fk_assets_asset_type
        FOREIGN KEY (asset_type_id)
        REFERENCES asset.asset_types(asset_type_id)
        ON DELETE RESTRICT,

    CONSTRAINT uq_assets_tag
        UNIQUE (organization_id, asset_tag)
);


CREATE TABLE IF NOT EXISTS asset.asset_assignments
(
    assignment_id uuid NOT NULL DEFAULT uuid_generate_v4(),
    organization_id uuid NOT NULL,
    asset_id uuid NOT NULL,
    employee_id uuid NOT NULL,
    assigned_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    due_date timestamp with time zone,
    returned_at timestamp with time zone,
    status character varying(30) DEFAULT 'Active',
    notes character varying(500),
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    created_by uuid,
    is_deleted boolean DEFAULT false,
    CONSTRAINT asset_assignments_pkey PRIMARY KEY (assignment_id),
    CONSTRAINT fk_asset_assignments_asset FOREIGN KEY (asset_id)
        REFERENCES asset.assets (asset_id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE RESTRICT,
    CONSTRAINT fk_asset_assignments_employee FOREIGN KEY (employee_id)
        REFERENCES employee.employees (employeeid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS idx_asset_types_org ON asset.asset_types (organization_id);
CREATE INDEX IF NOT EXISTS idx_assets_org ON asset.assets (organization_id);
CREATE INDEX IF NOT EXISTS idx_assets_type ON asset.assets (asset_type_id);
CREATE INDEX IF NOT EXISTS idx_assets_status ON asset.assets (status);
CREATE INDEX IF NOT EXISTS idx_asset_assignments_org ON asset.asset_assignments (organization_id);
CREATE INDEX IF NOT EXISTS idx_asset_assignments_asset ON asset.asset_assignments (asset_id);
CREATE INDEX IF NOT EXISTS idx_asset_assignments_employee ON asset.asset_assignments (employee_id);
CREATE INDEX IF NOT EXISTS idx_asset_assignments_status ON asset.asset_assignments (status);
CREATE UNIQUE INDEX IF NOT EXISTS uq_asset_assignments_active
    ON asset.asset_assignments (asset_id)
    WHERE status = 'Active' AND is_deleted = false;

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
-- HOLIDAY MANAGEMENT TABLES
-- ==============================

-- Table: attendance.holiday_catalogs
-- Master catalog of country-based holidays (pre-seeded)

CREATE TABLE IF NOT EXISTS attendance.holiday_catalogs
(
    catalogid uuid NOT NULL DEFAULT uuid_generate_v4(),
    countrycode character varying(5) NOT NULL,
    countryname character varying(100) NOT NULL,
    holidayname character varying(200) NOT NULL,
    holidaydate date NOT NULL,
    isrecurring boolean DEFAULT true,
    description text,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT holiday_catalogs_pkey PRIMARY KEY (catalogid),
    CONSTRAINT uq_holiday_catalog UNIQUE (countrycode, holidayname, holidaydate)
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS attendance.holiday_catalogs
    OWNER to postgres;

CREATE INDEX idx_holiday_catalogs_country ON attendance.holiday_catalogs(countrycode);

-- Table: attendance.company_holidays
-- Holidays adopted by a company for a particular year

CREATE TABLE IF NOT EXISTS attendance.company_holidays
(
    holidayid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid NOT NULL,
    holidayname character varying(200) NOT NULL,
    holidaydate date NOT NULL,
    holidaytype character varying(20) NOT NULL DEFAULT 'mandatory',
    description text,
    ishalfday boolean DEFAULT false,
    year integer NOT NULL,
    applicableto character varying(20) NOT NULL DEFAULT 'all',
    applicablevalue text,
    catalogholidayid uuid,
    createdby uuid,
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT company_holidays_pkey PRIMARY KEY (holidayid),
    CONSTRAINT fk_company_holidays_org FOREIGN KEY (organizationid)
        REFERENCES auth.organizations (organizationid) ON DELETE CASCADE,
    CONSTRAINT fk_company_holidays_catalog FOREIGN KEY (catalogholidayid)
        REFERENCES attendance.holiday_catalogs (catalogid) ON DELETE SET NULL,
    CONSTRAINT chk_holiday_type CHECK (holidaytype IN ('mandatory', 'restricted', 'optional')),
    CONSTRAINT chk_applicable_to CHECK (applicableto IN ('all', 'department', 'location')),
    CONSTRAINT uq_company_holiday UNIQUE (organizationid, holidayname, holidaydate)
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS attendance.company_holidays
    OWNER to postgres;

CREATE INDEX idx_company_holidays_org_year ON attendance.company_holidays(organizationid, year);
CREATE INDEX idx_company_holidays_date ON attendance.company_holidays(holidaydate);

-- Table: attendance.employee_restricted_holidays
-- Employee selections from the restricted holiday pool

CREATE TABLE IF NOT EXISTS attendance.employee_restricted_holidays
(
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    companyholidayid uuid NOT NULL,
    status character varying(20) NOT NULL DEFAULT 'selected',
    selectedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT employee_restricted_holidays_pkey PRIMARY KEY (id),
    CONSTRAINT fk_erh_org FOREIGN KEY (organizationid)
        REFERENCES auth.organizations (organizationid) ON DELETE CASCADE,
    CONSTRAINT fk_erh_employee FOREIGN KEY (employeeid)
        REFERENCES employee.employees (employeeid) ON DELETE CASCADE,
    CONSTRAINT fk_erh_holiday FOREIGN KEY (companyholidayid)
        REFERENCES attendance.company_holidays (holidayid) ON DELETE CASCADE,
    CONSTRAINT chk_erh_status CHECK (status IN ('selected', 'cancelled')),
    CONSTRAINT uq_employee_restricted UNIQUE (employeeid, companyholidayid)
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS attendance.employee_restricted_holidays
    OWNER to postgres;

CREATE INDEX idx_erh_employee ON attendance.employee_restricted_holidays(employeeid);
CREATE INDEX idx_erh_org_year ON attendance.employee_restricted_holidays(organizationid);


-- ==============================
-- SEED DATA: Country Holiday Catalogs
-- ==============================

-- India Public Holidays 2025
INSERT INTO attendance.holiday_catalogs (countrycode, countryname, holidayname, holidaydate, isrecurring, description) VALUES
('IN', 'India', 'Republic Day', '2025-01-26', true, 'Celebrates the adoption of the Constitution of India'),
('IN', 'India', 'Maha Shivaratri', '2025-02-26', true, 'Hindu festival dedicated to Lord Shiva'),
('IN', 'India', 'Holi', '2025-03-14', true, 'Festival of colors'),
('IN', 'India', 'Good Friday', '2025-04-18', true, 'Christian observance of the crucifixion of Jesus'),
('IN', 'India', 'Eid ul-Fitr', '2025-03-31', false, 'Marks the end of Ramadan'),
('IN', 'India', 'Dr. Ambedkar Jayanti', '2025-04-14', true, 'Birth anniversary of Dr. B.R. Ambedkar'),
('IN', 'India', 'Ram Navami', '2025-04-06', true, 'Celebrates the birth of Lord Rama'),
('IN', 'India', 'Mahavir Jayanti', '2025-04-10', true, 'Birth anniversary of Lord Mahavir'),
('IN', 'India', 'May Day', '2025-05-01', true, 'International Workers Day'),
('IN', 'India', 'Buddha Purnima', '2025-05-12', true, 'Celebrates the birth of Gautama Buddha'),
('IN', 'India', 'Eid ul-Adha', '2025-06-07', false, 'Festival of Sacrifice'),
('IN', 'India', 'Muharram', '2025-07-06', false, 'Islamic New Year'),
('IN', 'India', 'Independence Day', '2025-08-15', true, 'Celebrates India''s independence from British rule'),
('IN', 'India', 'Janmashtami', '2025-08-16', true, 'Celebrates the birth of Lord Krishna'),
('IN', 'India', 'Milad un-Nabi', '2025-09-05', false, 'Birthday of Prophet Muhammad'),
('IN', 'India', 'Mahatma Gandhi Jayanti', '2025-10-02', true, 'Birth anniversary of Mahatma Gandhi'),
('IN', 'India', 'Dussehra', '2025-10-02', true, 'Victory of good over evil'),
('IN', 'India', 'Diwali', '2025-10-20', true, 'Festival of lights'),
('IN', 'India', 'Guru Nanak Jayanti', '2025-11-05', true, 'Birth anniversary of Guru Nanak'),
('IN', 'India', 'Christmas Day', '2025-12-25', true, 'Celebrates the birth of Jesus Christ')
ON CONFLICT ON CONSTRAINT uq_holiday_catalog DO NOTHING;

-- United States Federal Holidays 2025
INSERT INTO attendance.holiday_catalogs (countrycode, countryname, holidayname, holidaydate, isrecurring, description) VALUES
('US', 'United States', 'New Year''s Day', '2025-01-01', true, 'Celebrates the start of the new year'),
('US', 'United States', 'Martin Luther King Jr. Day', '2025-01-20', true, 'Honors civil rights leader Martin Luther King Jr.'),
('US', 'United States', 'Presidents'' Day', '2025-02-17', true, 'Honors all U.S. presidents'),
('US', 'United States', 'Memorial Day', '2025-05-26', true, 'Honors military personnel who have died in service'),
('US', 'United States', 'Juneteenth', '2025-06-19', true, 'Commemorates the emancipation of enslaved people'),
('US', 'United States', 'Independence Day', '2025-07-04', true, 'Celebrates the Declaration of Independence'),
('US', 'United States', 'Labor Day', '2025-09-01', true, 'Honors the American labor movement'),
('US', 'United States', 'Columbus Day', '2025-10-13', true, 'Commemorates the arrival of Columbus in the Americas'),
('US', 'United States', 'Veterans Day', '2025-11-11', true, 'Honors military veterans'),
('US', 'United States', 'Thanksgiving Day', '2025-11-27', true, 'A day of giving thanks'),
('US', 'United States', 'Christmas Day', '2025-12-25', true, 'Celebrates the birth of Jesus Christ')
ON CONFLICT ON CONSTRAINT uq_holiday_catalog DO NOTHING;

-- United Kingdom Public Holidays 2025
INSERT INTO attendance.holiday_catalogs (countrycode, countryname, holidayname, holidaydate, isrecurring, description) VALUES
('GB', 'United Kingdom', 'New Year''s Day', '2025-01-01', true, 'Celebrates the start of the new year'),
('GB', 'United Kingdom', 'Good Friday', '2025-04-18', true, 'Christian observance'),
('GB', 'United Kingdom', 'Easter Monday', '2025-04-21', true, 'Day after Easter Sunday'),
('GB', 'United Kingdom', 'Early May Bank Holiday', '2025-05-05', true, 'First Monday of May'),
('GB', 'United Kingdom', 'Spring Bank Holiday', '2025-05-26', true, 'Last Monday of May'),
('GB', 'United Kingdom', 'Summer Bank Holiday', '2025-08-25', true, 'Last Monday of August'),
('GB', 'United Kingdom', 'Christmas Day', '2025-12-25', true, 'Celebrates the birth of Jesus Christ'),
('GB', 'United Kingdom', 'Boxing Day', '2025-12-26', true, 'Day after Christmas')
ON CONFLICT ON CONSTRAINT uq_holiday_catalog DO NOTHING;

-- Canada Public Holidays 2025
INSERT INTO attendance.holiday_catalogs (countrycode, countryname, holidayname, holidaydate, isrecurring, description) VALUES
('CA', 'Canada', 'New Year''s Day', '2025-01-01', true, 'Celebrates the start of the new year'),
('CA', 'Canada', 'Family Day', '2025-02-17', true, 'Provincial family holiday'),
('CA', 'Canada', 'Good Friday', '2025-04-18', true, 'Christian observance'),
('CA', 'Canada', 'Victoria Day', '2025-05-19', true, 'Honours Queen Victoria'),
('CA', 'Canada', 'Canada Day', '2025-07-01', true, 'Celebrates the anniversary of Canadian Confederation'),
('CA', 'Canada', 'Labour Day', '2025-09-01', true, 'Celebrates the labour movement'),
('CA', 'Canada', 'Thanksgiving', '2025-10-13', true, 'A day of giving thanks'),
('CA', 'Canada', 'Remembrance Day', '2025-11-11', true, 'Honours military veterans'),
('CA', 'Canada', 'Christmas Day', '2025-12-25', true, 'Celebrates the birth of Jesus Christ'),
('CA', 'Canada', 'Boxing Day', '2025-12-26', true, 'Day after Christmas')
ON CONFLICT ON CONSTRAINT uq_holiday_catalog DO NOTHING;

-- Australia Public Holidays 2025
INSERT INTO attendance.holiday_catalogs (countrycode, countryname, holidayname, holidaydate, isrecurring, description) VALUES
('AU', 'Australia', 'New Year''s Day', '2025-01-01', true, 'Celebrates the start of the new year'),
('AU', 'Australia', 'Australia Day', '2025-01-27', true, 'Commemorates the foundation of Australia'),
('AU', 'Australia', 'Good Friday', '2025-04-18', true, 'Christian observance'),
('AU', 'Australia', 'Easter Saturday', '2025-04-19', true, 'Day before Easter Sunday'),
('AU', 'Australia', 'Easter Monday', '2025-04-21', true, 'Day after Easter Sunday'),
('AU', 'Australia', 'Anzac Day', '2025-04-25', true, 'Commemorates Australian and New Zealand war veterans'),
('AU', 'Australia', 'Queen''s Birthday', '2025-06-09', true, 'Honours the reigning British Monarch'),
('AU', 'Australia', 'Christmas Day', '2025-12-25', true, 'Celebrates the birth of Jesus Christ'),
('AU', 'Australia', 'Boxing Day', '2025-12-26', true, 'Day after Christmas')
ON CONFLICT ON CONSTRAINT uq_holiday_catalog DO NOTHING;

-- UAE Public Holidays 2025
INSERT INTO attendance.holiday_catalogs (countrycode, countryname, holidayname, holidaydate, isrecurring, description) VALUES
('AE', 'United Arab Emirates', 'New Year''s Day', '2025-01-01', true, 'Celebrates the start of the new year'),
('AE', 'United Arab Emirates', 'Eid al-Fitr', '2025-03-30', false, 'Marks the end of Ramadan'),
('AE', 'United Arab Emirates', 'Eid al-Fitr Holiday', '2025-03-31', false, 'Extended Eid al-Fitr holiday'),
('AE', 'United Arab Emirates', 'Arafat Day', '2025-06-06', false, 'Day before Eid al-Adha'),
('AE', 'United Arab Emirates', 'Eid al-Adha', '2025-06-07', false, 'Festival of Sacrifice'),
('AE', 'United Arab Emirates', 'Islamic New Year', '2025-06-27', false, 'Start of Islamic calendar year'),
('AE', 'United Arab Emirates', 'Prophet Muhammad''s Birthday', '2025-09-05', false, 'Birthday of Prophet Muhammad'),
('AE', 'United Arab Emirates', 'Commemoration Day', '2025-11-30', true, 'Honours UAE martyrs'),
('AE', 'United Arab Emirates', 'UAE National Day', '2025-12-02', true, 'Celebrates the formation of the UAE'),
('AE', 'United Arab Emirates', 'UAE National Day Holiday', '2025-12-03', true, 'Extended National Day holiday')
ON CONFLICT ON CONSTRAINT uq_holiday_catalog DO NOTHING;

-- Singapore Public Holidays 2025
INSERT INTO attendance.holiday_catalogs (countrycode, countryname, holidayname, holidaydate, isrecurring, description) VALUES
('SG', 'Singapore', 'New Year''s Day', '2025-01-01', true, 'Celebrates the start of the new year'),
('SG', 'Singapore', 'Chinese New Year', '2025-01-29', true, 'Lunar New Year celebration'),
('SG', 'Singapore', 'Chinese New Year Day 2', '2025-01-30', true, 'Second day of Lunar New Year'),
('SG', 'Singapore', 'Good Friday', '2025-04-18', true, 'Christian observance'),
('SG', 'Singapore', 'Hari Raya Puasa', '2025-03-31', false, 'Marks the end of Ramadan'),
('SG', 'Singapore', 'Labour Day', '2025-05-01', true, 'International Workers Day'),
('SG', 'Singapore', 'Vesak Day', '2025-05-12', true, 'Celebrates the birth of Buddha'),
('SG', 'Singapore', 'Hari Raya Haji', '2025-06-07', false, 'Festival of Sacrifice'),
('SG', 'Singapore', 'National Day', '2025-08-09', true, 'Celebrates Singapore''s independence'),
('SG', 'Singapore', 'Deepavali', '2025-10-20', true, 'Festival of lights'),
('SG', 'Singapore', 'Christmas Day', '2025-12-25', true, 'Celebrates the birth of Jesus Christ')
ON CONFLICT ON CONSTRAINT uq_holiday_catalog DO NOTHING;
-- =============================================
-- Holiday Employee Assignment Table + Schema Updates
-- Adds ability to assign holidays to specific employees
-- Also updates applicableTo constraint to include 'employee'
-- =============================================

-- 1. Alter the CHECK constraint on applicableto to include 'employee'
ALTER TABLE attendance.company_holidays DROP CONSTRAINT IF EXISTS chk_applicable_to;
ALTER TABLE attendance.company_holidays ADD CONSTRAINT chk_applicable_to 
    CHECK (applicableto IN ('all', 'department', 'location', 'employee'));

-- 2. Create holiday_employee_assignments table
-- Used when applicableTo = 'employee' to assign holidays to specific employees
CREATE TABLE IF NOT EXISTS attendance.holiday_employee_assignments (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    holiday_id      uuid NOT NULL REFERENCES attendance.company_holidays(holidayid) ON DELETE CASCADE,
    employee_id     uuid NOT NULL REFERENCES employee.employees(employeeid) ON DELETE CASCADE,
    assigned_by     uuid,
    assigned_at     timestamptz NOT NULL DEFAULT NOW(),
    
    -- Prevent duplicate assignments
    CONSTRAINT uq_holiday_employee UNIQUE (holiday_id, employee_id)
);

-- Indexes for fast lookups
CREATE INDEX IF NOT EXISTS idx_holiday_emp_assign_holiday ON attendance.holiday_employee_assignments(holiday_id);
CREATE INDEX IF NOT EXISTS idx_holiday_emp_assign_employee ON attendance.holiday_employee_assignments(employee_id);

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

-- ============================
-- Expense Categories
-- ============================
CREATE TABLE IF NOT EXISTS expense.expensecategories
(
    categoryid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid NOT NULL,
    name character varying(100) COLLATE pg_catalog."default" NOT NULL,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT expensecategories_pkey PRIMARY KEY (categoryid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS expense.expensecategories
    OWNER to postgres;

    -- ============================================================================
-- Migration: Payment Management Tables
-- Date: 2026-03-11
-- Description: Creates transactions, invoices, and refunds tables in the
--              payment schema to support full payment gateway integration
--              and admin payment management.
-- ============================================================================

-- ============================================================================
-- 1. INVOICE NUMBER SEQUENCE
-- ============================================================================
CREATE SEQUENCE IF NOT EXISTS payment.invoice_number_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- ============================================================================
-- 2. TRANSACTIONS TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS payment.transactions (
    transaction_id          UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id              UUID            NOT NULL,
    subscription_id         UUID            NULL,
    plan_id                 UUID            NOT NULL,

    -- Gateway details
    gateway                 VARCHAR(50)     NOT NULL DEFAULT 'stripe',       -- 'stripe' | 'razorpay' | 'manual'
    gateway_transaction_id  VARCHAR(255)    NULL,                            -- e.g. Stripe PaymentIntent ID
    gateway_order_id        VARCHAR(255)    NULL,                            -- e.g. Stripe Checkout Session ID
    gateway_customer_id     VARCHAR(255)    NULL,                            -- e.g. Stripe Customer ID

    -- Payment details
    amount                  DECIMAL(12,2)   NOT NULL,
    currency                VARCHAR(10)     NOT NULL DEFAULT 'USD',
    billing_cycle           VARCHAR(20)     NOT NULL,                        -- 'monthly' | 'annual'
    status                  VARCHAR(30)     NOT NULL DEFAULT 'pending',      -- 'pending' | 'completed' | 'failed' | 'refunded' | 'cancelled'

    -- Payment method info (captured from gateway)
    payment_method          VARCHAR(50)     NULL,                            -- 'card' | 'upi' | 'netbanking' | 'wallet' | 'bank_transfer'
    card_last4              VARCHAR(4)      NULL,
    card_brand              VARCHAR(30)     NULL,                            -- 'visa' | 'mastercard' | 'amex' etc.

    -- Failure / extra info
    failure_reason          TEXT            NULL,
    metadata                JSONB           NULL,                            -- Gateway-specific payload / extra data

    -- Timestamps
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    -- Foreign keys
    CONSTRAINT fk_transactions_company
        FOREIGN KEY (company_id) REFERENCES auth.organizations(organizationid) ON DELETE RESTRICT,
    CONSTRAINT fk_transactions_subscription
        FOREIGN KEY (subscription_id) REFERENCES payment.companysubscriptions(subscriptionid) ON DELETE SET NULL,
    CONSTRAINT fk_transactions_plan
        FOREIGN KEY (plan_id) REFERENCES payment.subscriptionplans(planid) ON DELETE RESTRICT
);

-- Indexes for transactions
CREATE INDEX IF NOT EXISTS idx_transactions_company_id      ON payment.transactions(company_id);
CREATE INDEX IF NOT EXISTS idx_transactions_status          ON payment.transactions(status);
CREATE INDEX IF NOT EXISTS idx_transactions_gateway         ON payment.transactions(gateway);
CREATE INDEX IF NOT EXISTS idx_transactions_created_at      ON payment.transactions(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_transactions_gateway_txn_id  ON payment.transactions(gateway_transaction_id) WHERE gateway_transaction_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_transactions_gateway_order   ON payment.transactions(gateway_order_id) WHERE gateway_order_id IS NOT NULL;

-- ============================================================================
-- 3. INVOICES TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS payment.invoices (
    invoice_id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id          UUID            NOT NULL,
    company_id              UUID            NOT NULL,

    -- Invoice number (auto-generated, human-readable)
    invoice_number          VARCHAR(50)     NOT NULL UNIQUE,

    -- Amounts
    amount                  DECIMAL(12,2)   NOT NULL,                        -- Subtotal (plan price)
    tax_amount              DECIMAL(12,2)   NOT NULL DEFAULT 0.00,
    discount_amount         DECIMAL(12,2)   NOT NULL DEFAULT 0.00,
    total_amount            DECIMAL(12,2)   NOT NULL,                        -- amount + tax - discount
    currency                VARCHAR(10)     NOT NULL DEFAULT 'USD',

    -- Billing period
    billing_period_start    TIMESTAMPTZ     NOT NULL,
    billing_period_end      TIMESTAMPTZ     NOT NULL,

    -- Snapshot fields (captured at time of invoice for audit trail)
    plan_name               VARCHAR(100)    NOT NULL,
    billing_cycle           VARCHAR(20)     NOT NULL,
    company_name            VARCHAR(255)    NOT NULL,
    company_email           VARCHAR(255)    NULL,

    -- Status
    status                  VARCHAR(20)     NOT NULL DEFAULT 'draft',        -- 'draft' | 'paid' | 'void' | 'refunded'

    -- Dates
    issued_at               TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    paid_at                 TIMESTAMPTZ     NULL,
    due_date                TIMESTAMPTZ     NULL,

    -- Notes
    notes                   TEXT            NULL,

    -- Foreign keys
    CONSTRAINT fk_invoices_transaction
        FOREIGN KEY (transaction_id) REFERENCES payment.transactions(transaction_id) ON DELETE RESTRICT,
    CONSTRAINT fk_invoices_company
        FOREIGN KEY (company_id) REFERENCES auth.organizations(organizationid) ON DELETE RESTRICT
);

-- Indexes for invoices
CREATE INDEX IF NOT EXISTS idx_invoices_company_id      ON payment.invoices(company_id);
CREATE INDEX IF NOT EXISTS idx_invoices_transaction_id  ON payment.invoices(transaction_id);
CREATE INDEX IF NOT EXISTS idx_invoices_status          ON payment.invoices(status);
CREATE INDEX IF NOT EXISTS idx_invoices_issued_at       ON payment.invoices(issued_at DESC);
CREATE INDEX IF NOT EXISTS idx_invoices_invoice_number  ON payment.invoices(invoice_number);

-- ============================================================================
-- 4. REFUNDS TABLE
-- ============================================================================
CREATE TABLE IF NOT EXISTS payment.refunds (
    refund_id               UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id          UUID            NOT NULL,

    -- Gateway refund reference
    gateway_refund_id       VARCHAR(255)    NULL,

    -- Refund details
    amount                  DECIMAL(12,2)   NOT NULL,
    currency                VARCHAR(10)     NOT NULL DEFAULT 'USD',
    reason                  TEXT            NOT NULL,
    status                  VARCHAR(30)     NOT NULL DEFAULT 'pending',      -- 'pending' | 'completed' | 'failed'

    -- Who initiated
    initiated_by            VARCHAR(20)     NOT NULL DEFAULT 'admin',        -- 'admin' | 'system'
    admin_id                UUID            NULL,                            -- References admin.platform_admins if initiated by admin

    -- Timestamps
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    processed_at            TIMESTAMPTZ     NULL,

    -- Foreign key
    CONSTRAINT fk_refunds_transaction
        FOREIGN KEY (transaction_id) REFERENCES payment.transactions(transaction_id) ON DELETE RESTRICT
);

-- Indexes for refunds
CREATE INDEX IF NOT EXISTS idx_refunds_transaction_id   ON payment.refunds(transaction_id);
CREATE INDEX IF NOT EXISTS idx_refunds_status           ON payment.refunds(status);
CREATE INDEX IF NOT EXISTS idx_refunds_created_at       ON payment.refunds(created_at DESC);

-- ============================================================================
-- 5. HELPER FUNCTION: Auto-generate invoice numbers (INV-YYYY-NNNNNN)
-- ============================================================================
CREATE OR REPLACE FUNCTION payment.generate_invoice_number()
RETURNS TEXT AS $$
DECLARE
    next_val BIGINT;
    year_part TEXT;
BEGIN
    next_val := nextval('payment.invoice_number_seq');
    year_part := EXTRACT(YEAR FROM NOW())::TEXT;
    RETURN 'INV-' || year_part || '-' || LPAD(next_val::TEXT, 6, '0');
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- 6. TRIGGER: Auto-update updated_at on transactions
-- ============================================================================
CREATE OR REPLACE FUNCTION payment.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_transactions_updated_at ON payment.transactions;
CREATE TRIGGER trg_transactions_updated_at
    BEFORE UPDATE ON payment.transactions
    FOR EACH ROW
    EXECUTE FUNCTION payment.update_updated_at_column();

-- ============================================================================
-- 7. ADD INDEX ON EXISTING companysubscriptions FOR FASTER JOINS
-- ============================================================================
CREATE INDEX IF NOT EXISTS idx_companysubscriptions_company_id
    ON payment.companysubscriptions(companyid);
CREATE INDEX IF NOT EXISTS idx_companysubscriptions_status
    ON payment.companysubscriptions(status);

-- ============================================================================
-- MIGRATION COMPLETE
-- ============================================================================
-- Tables created:
--   payment.transactions    — Records every payment attempt/completion
--   payment.invoices        — Auto-generated invoices for completed payments
--   payment.refunds         — Tracks refund requests and processing
--
-- Functions created:
--   payment.generate_invoice_number()  — Returns 'INV-YYYY-NNNNNN'
--   payment.update_updated_at_column() — Auto-updates updated_at timestamp
--
-- To verify, run:
--   SELECT table_name FROM information_schema.tables WHERE table_schema = 'payment';
-- ============================================================================


-- ============================
-- Expenses
-- ============================
-- Table: expense.expenses

-- DROP TABLE IF EXISTS expense.expenses;

CREATE TABLE IF NOT EXISTS expense.expenses
(
    expenseid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    categoryid uuid,
    title character varying(150) COLLATE pg_catalog."default" NOT NULL,
    description text COLLATE pg_catalog."default",
    amount numeric(12,2) NOT NULL,
    billdate date NOT NULL,
    paymentmethod character varying(50) COLLATE pg_catalog."default",
    merchant character varying(150) COLLATE pg_catalog."default",
    receipturl text COLLATE pg_catalog."default",
    status character varying(30) COLLATE pg_catalog."default" NOT NULL DEFAULT 'Pending'::character varying,
    approvedby uuid,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT expenses_pkey PRIMARY KEY (expenseid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS expense.expenses
    OWNER to postgres;

-- ============================
-- Recurring Expenses
-- ============================
-- Table: expense.recurringexpenses

-- DROP TABLE IF EXISTS expense.recurringexpenses;

CREATE TABLE IF NOT EXISTS expense.recurringexpenses
(
    recurringexpenseid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    categoryid uuid NOT NULL,
    title character varying(150) COLLATE pg_catalog."default" NOT NULL,
    description text COLLATE pg_catalog."default",
    amount numeric(12,2) NOT NULL,
    rotation character varying(20) COLLATE pg_catalog."default" NOT NULL,
    dayofmonth character varying(20) COLLATE pg_catalog."default",
    status character varying(30) COLLATE pg_catalog."default" NOT NULL DEFAULT 'Pending'::character varying,
    approvedby uuid,
    paymentmethod character varying(50) COLLATE pg_catalog."default",
    merchant character varying(150) COLLATE pg_catalog."default",
    receipturl text COLLATE pg_catalog."default",
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT recurringexpenses_pkey PRIMARY KEY (recurringexpenseid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS expense.recurringexpenses
    OWNER to postgres;

-- ============================
-- News
-- ============================
-- Table: News

 -- DROP TABLE IF EXISTS news.news;

CREATE TABLE IF NOT EXISTS news.news
(
    newsid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid NOT NULL,
    title character varying(255) COLLATE pg_catalog."default" NOT NULL,
    description text COLLATE pg_catalog."default" NOT NULL,
    createdby uuid NOT NULL,
    category character varying(50) COLLATE pg_catalog."default" DEFAULT 'Other'::character varying,
    imageurl character varying(255) COLLATE pg_catalog."default",
    publishedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    status character varying(20) COLLATE pg_catalog."default" DEFAULT 'Draft'::character varying,
    views integer DEFAULT 0,
    visibleto character varying(20) COLLATE pg_catalog."default" DEFAULT 'Everyone'::character varying,
    selectedemployees uuid[] DEFAULT '{}'::uuid[],
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT news_pkey PRIMARY KEY (newsid)

-- ==============================
-- JOBS SCHEMA
-- ==============================
-- Table: jobs.stagemaster

-- DROP TABLE IF EXISTS jobs.stagemaster;

CREATE TABLE IF NOT EXISTS jobs.stagemaster
(
    stageid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid NOT NULL,
    stagename character varying(100) COLLATE pg_catalog."default" NOT NULL,
    stageorder integer,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    isinterviewstage boolean DEFAULT false,
    CONSTRAINT stagemaster_pkey PRIMARY KEY (stageid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS jobs.stagemaster
    OWNER to postgres;

-- DROP TABLE IF EXISTS news.news_reads_count;

CREATE TABLE IF NOT EXISTS news.news_reads_count
(
    newsid uuid NOT NULL,
    employeeid uuid NOT NULL,
    readat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_news_reads PRIMARY KEY (newsid, employeeid)
)

TABLESPACE pg_default;
ALTER TABLE IF EXISTS jobs.stagemaster
    OWNER to postgres;

    -- Table: jobs.jobopenings

-- DROP TABLE IF EXISTS jobs.jobopenings;

CREATE TABLE IF NOT EXISTS jobs.jobopenings
(
    jobid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid NOT NULL,
    jobrolename character varying(200) COLLATE pg_catalog."default" NOT NULL,
    departmentid uuid,
    experiencemin integer,
    experiencemax integer,
    ctcmin numeric(12,2),
    ctcmax numeric(12,2),
    currency character varying(10) COLLATE pg_catalog."default",
    vacancies integer NOT NULL DEFAULT 1,
    location character varying(200) COLLATE pg_catalog."default",
    workmode character varying(50) COLLATE pg_catalog."default",
    employmenttype character varying(50) COLLATE pg_catalog."default" DEFAULT 'Full-time'::character varying,
    jobintroduction text COLLATE pg_catalog."default",
    responsibilities text COLLATE pg_catalog."default",
    skillset text COLLATE pg_catalog."default",
    lastdate date,
    posteddate timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    postedas character varying(20) COLLATE pg_catalog."default" NOT NULL,
    externallink text COLLATE pg_catalog."default",
    status character varying(50) COLLATE pg_catalog."default" DEFAULT 'Open'::character varying,
    createdby uuid,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone,
    jobcode character varying(100) COLLATE pg_catalog."default",
    CONSTRAINT jobopenings_pkey PRIMARY KEY (jobid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS jobs.jobopenings
    OWNER to postgres;

    -- Table: jobs.jobapplications

-- DROP TABLE IF EXISTS jobs.jobapplications;

CREATE TABLE IF NOT EXISTS jobs.jobapplications
(
    jobapplyid uuid NOT NULL DEFAULT uuid_generate_v4(),
    jobid uuid NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid,
    candidateid uuid DEFAULT uuid_generate_v4(),
    candidatename character varying(200) COLLATE pg_catalog."default",
    candidateemail character varying(200) COLLATE pg_catalog."default",
    linkedinurl text COLLATE pg_catalog."default",
    resumeurl text COLLATE pg_catalog."default",
    phone character varying(50) COLLATE pg_catalog."default",
    coverletter text COLLATE pg_catalog."default",
    currentstageid uuid,
    applicationsource character varying(100) COLLATE pg_catalog."default",
    status character varying(50) COLLATE pg_catalog."default" DEFAULT 'Applied'::character varying,
    createddate timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updateddate timestamp with time zone,
    isself boolean DEFAULT false,
    isexternal boolean DEFAULT false,
    CONSTRAINT jobapplications_pkey PRIMARY KEY (jobapplyid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS jobs.jobapplications
    OWNER to postgres;

    -- Table: jobs.applicationstages

-- DROP TABLE IF EXISTS jobs.applicationstages;

-- Table: jobs.applicationstages

-- DROP TABLE IF EXISTS jobs.applicationstages;

CREATE TABLE IF NOT EXISTS jobs.applicationstages
(
    applicationstageid uuid NOT NULL DEFAULT uuid_generate_v4(),
    jobapplyid uuid NOT NULL,
    stageid uuid NOT NULL,
    notes text COLLATE pg_catalog."default",
    interviewers text COLLATE pg_catalog."default",
    type character varying(50) COLLATE pg_catalog."default",
    updatedon timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedby uuid,
    interviewdate timestamp with time zone,
    interviewplace character varying(200) COLLATE pg_catalog."default",
    CONSTRAINT applicationstages_pkey PRIMARY KEY (applicationstageid)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS jobs.applicationstages
    OWNER to postgres;

    --// Table : Customise Roles
   CREATE TABLE IF NOT EXISTS auth.actions
(
    actionid uuid NOT NULL DEFAULT uuid_generate_v4(),
    submenuid uuid NOT NULL,
    actionname character varying(200) NOT NULL,  -- e.g., "Add Shift", "Request Swap"
    actionkey character varying(100) NOT NULL,   -- e.g., "add_shift", "request_swap"
    created_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT actions_pkey PRIMARY KEY (actionid),
    CONSTRAINT fk_submenu FOREIGN KEY (submenuid) REFERENCES auth.submenus(submenuid)
);


CREATE TABLE IF NOT EXISTS auth.permissions
(
    permissionid uuid NOT NULL DEFAULT uuid_generate_v4(),
    roleid uuid NOT NULL,
    actionid uuid NOT NULL,
    organizationid uuid NOT NULL,
    has_permission BOOLEAN DEFAULT false,
    created_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    createdby uuid,
    updatedby uuid
);


CREATE TABLE IF NOT EXISTS auth.menus
(
    menuid uuid NOT NULL DEFAULT uuid_generate_v4(),
    menuname character varying(200) NOT NULL,
    icon character varying(100),
    created_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT menus_pkey PRIMARY KEY (menuid)
);

CREATE TABLE IF NOT EXISTS auth.submenus
(
    submenuid uuid NOT NULL DEFAULT uuid_generate_v4(),
    menuid uuid NOT NULL,
    submenuname character varying(200) NOT NULL,
    icon character varying(100),
    created_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT sub_menus_pkey PRIMARY KEY (submenuid)

);

--Schema for the Payroll--
create table if not exists payroll.payrollperiods
(
    periodid        uuid primary key default uuid_generate_v4(),
    organizationid  uuid not null,
    periodname      character varying(100) not null,  -- e.g. 'July 2025', 'August 2025'
    startdate       date not null,
    enddate         date not null,
    paymentdate     date,                             -- actual salary disbursement date
    status          character varying(20) not null default 'Open'
    description     text,
    isactive        boolean default true,
    isdeleted       boolean default false,
    createdby       uuid,
    createdat       timestamptz default current_timestamp,
    updatedat       timestamptz default current_timestamp,
);
// overtime
CREATE TABLE IF NOT EXISTS payroll.overtimerules
(
    ruleid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid NOT NULL,
    rulename character varying(100) COLLATE pg_catalog."default",
    description text COLLATE pg_catalog."default",
    overtimetype character varying(20) COLLATE pg_catalog."default",
    fixedamount numeric(15,2),
    percentage numeric(5,2),
    isactive boolean DEFAULT true,
    isdeleted boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT overtimerules_pkey PRIMARY KEY (ruleid)
)

CREATE TABLE IF NOT EXISTS payroll.overtimeentries
(
    overtimeid uuid NOT NULL DEFAULT uuid_generate_v4(),
    employeeid uuid NOT NULL,
    periodid uuid NOT NULL,
    ruleid uuid,
    hoursworked numeric(5,2) NOT NULL,
    finalamount numeric(15,2),
    entrystatus character varying(20) COLLATE pg_catalog."default" DEFAULT 'pending'::character varying,
    approvedby uuid,
    overtimedate date NOT NULL,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT overtimeentries_pkey PRIMARY KEY (overtimeid),
    CONSTRAINT overtimeentries_ruleid_fkey FOREIGN KEY (ruleid)
        REFERENCES payroll.overtimerules (ruleid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT overtimeentries_entrystatus_check CHECK (entrystatus::text = ANY (ARRAY['pending'::character varying::text, 'approved'::character varying::text, 'rejected'::character varying::text]))
) // overtime

// attendance deduction
CREATE TABLE IF NOT EXISTS payroll.attendancedeductionrules
(
    ruleid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid NOT NULL,
    rulename character varying(100) COLLATE pg_catalog."default",
    description text COLLATE pg_catalog."default",
    fixeddeduction numeric(15,2),
    percentagededuction numeric(5,2),
    halfdaymultiplier numeric(3,2) DEFAULT 0.50,
    isactive boolean DEFAULT true,
    isdeleted boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT attendancedeductionrules_pkey PRIMARY KEY (ruleid)
)

create table if not exists payroll.attendancesummary
(
    id                  uuid primary key default uuid_generate_v4(),
    employeeid          uuid not null,
    periodid            uuid not null,
    ruleid              uuid references payroll.attendancedeductionrules(ruleid),
    shiftid             uuid,
    scheduleddays       integer default 0,
    totaldays           integer default 0,
    presentdays         integer default 0,
    absentdays          integer default 0,
    halfdays            integer default 0,
    effectiveabsents    numeric(6,2) default 0,
    absentdeduction     numeric(15,2) default 0,
    halfdaydeduction    numeric(15,2) default 0,
    totaldeduction      numeric(15,2) default 0,
    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp,
    constraint uqattendance unique (periodid, employeeid)
); // attendance deduction

// late attendance
CREATE TABLE IF NOT EXISTS payroll.lateattendancerules
(
    ruleid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid NOT NULL,
    rulename character varying(100) COLLATE pg_catalog."default",
    description text COLLATE pg_catalog."default",
    fixeddeduction numeric(15,2),
    percentagededuction numeric(5,2),
    graceminutes integer DEFAULT 0,
    isactive boolean DEFAULT true,
    isdeleted boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT lateattendancerules_pkey PRIMARY KEY (ruleid)
)

CREATE TABLE IF NOT EXISTS payroll.lateattendance
(
    id uuid NOT NULL DEFAULT uuid_generate_v4(),
    employeeid uuid NOT NULL,
    periodid uuid NOT NULL,
    ruleid uuid,
    lateminutes integer DEFAULT 0,
    deduction numeric(15,2) DEFAULT 0,
    isgrace boolean DEFAULT false,
    attendancedate date NOT NULL,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT lateattendance_pkey PRIMARY KEY (id),
    CONSTRAINT lateattendance_ruleid_fkey FOREIGN KEY (ruleid)
        REFERENCES payroll.lateattendancerules (ruleid) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
) // late attendance

// leaves
CREATE TABLE IF NOT EXISTS payroll.leaverules
(
    ruleid uuid NOT NULL DEFAULT uuid_generate_v4(),
    organizationid uuid NOT NULL,
    rulename character varying(100) COLLATE pg_catalog."default",
    description text COLLATE pg_catalog."default",
    unpaidmultiplier numeric(3,2) DEFAULT 1.00,
    halfpaidmultiplier numeric(3,2) DEFAULT 0.50,
    isactive boolean DEFAULT true,
    isdeleted boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT leaverules_pkey PRIMARY KEY (ruleid)
)

create table if not exists payroll.payrollleavesummary
(
    id                  uuid primary key default uuid_generate_v4(),
    employeeid          uuid not null,
    periodid            uuid not null,
    ruleid              uuid references payroll.leaverules(ruleid),
    paiddays            numeric(5,2) default 0,
    unpaiddays          numeric(5,2) default 0,
    halfpaiddays        numeric(5,2) default 0,
    unpaiddeduction     numeric(15,2) default 0,
    halfpaiddeduction   numeric(15,2) default 0,
    totalleavedays      numeric(5,2) generated always as (
                            paiddays + unpaiddays + halfpaiddays
                        ) stored,
    totaldeduction      numeric(15,2),
    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp,
    constraint uqleavesummary unique (periodid, employeeid)
); // leaves

// performance 
create table if not exists payroll.performancerules
(
    ruleid              uuid primary key default uuid_generate_v4(),
    organizationid      uuid not null,
    rulename          character varying(20),
    description         text,
    minscore            numeric(3,2),
    maxscore            numeric(3,2),
    fixedamount         numeric(15,2),
    percentage          numeric(5,2),
    isactive            boolean default true,
    isdeleted           boolean default false,
    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp,
    constraint uqperformancebracket unique (organizationid, ratingname)
);

create table if not exists payroll.performancepay
(
    performancepayid    uuid primary key default uuid_generate_v4(),
    employeeid          uuid not null,
    periodid            uuid not null,
    ruleid              uuid references payroll.performancerules(ruleid),
    score               numeric(5,2),
    finalamount         numeric(15,2) not null,
    calculatedat        timestamptz default current_timestamp,
    createdat           timestamptz default current_timestamp,
    constraint uqperformancepay unique (employeeid, periodid)
); // performance

// loan 
create table if not exists payroll.loanrules
(
    ruleid              uuid primary key default uuid_generate_v4(),
    rulename            character varying(100),
    description         text,
    organizationid      uuid not null,
    maxloanamount       numeric(15,2),
    maxinstallments     integer,
    interestrate        numeric(5,2) default 0,
    requiresapproval    boolean default true,
    isactive            boolean default true,
    isdeleted           boolean default false,
    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp,
    constraint uqorgloanrule unique (organizationid)
);

create table if not exists payroll.loans
(
    loanid              uuid primary key default uuid_generate_v4(),
    organizationid      uuid not null,
    employeeid          uuid not null,
    ruleid              uuid references payroll.loanrules(ruleid),
    totalamount         numeric(15,2) not null,
    monthlyinstallment  numeric(15,2),
    remainingamount     numeric(15,2) not null,
    paidamount          numeric(15,2) generated always as (
                            totalamount - remainingamount
                        ) stored,
    totalinstallments   integer not null default 0,
    paidinstallments    integer not null default 0,
    repaymenttype       varchar(20) default 'installment',
    requeststatus       varchar(20) default 'pending', 
    startdate           date,
    enddate             date,
    loanstatus          varchar(20) default 'active',
    description         text,
    createdby           uuid,
    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp
);

create table if not exists payroll.loanpayments
(
    id                  uuid primary key default uuid_generate_v4(),
    organizationid      uuid not null,
    loanid              uuid references payroll.loans(loanid),
    employeeid          uuid not null,
    periodid            uuid not null,
    installmentnumber   integer not null default 0,
    installmentamount   numeric(15,2) not null,
    remainingamount     numeric(15,2) not null,
    repaymenttype       varchar(20) default 'installment',
    paymentstatus       varchar(20) default 'deducted',
    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp,
    constraint uqloanpayments unique (loanid, periodid)
); // loan

// advance salary
create table if not exists payroll.salaryadvancerules
(
    ruleid              uuid primary key default uuid_generate_v4(),
    rulename            character varying(100),
    description         text,
    organizationid      uuid not null,
    maxpercentage       numeric(5,2),
    allowancegrace      integer,
    requiresapproval    boolean default true,
    isactive            boolean default true,
    isdeleted           boolean default false,
    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp,
    constraint uqorgadvancerule unique (organizationid)
);

create table if not exists payroll.salaryadvance
(
    advanceid           uuid primary key default uuid_generate_v4(),
    organizationid      uuid not null,
    employeeid          uuid not null,
    ruleid              uuid references payroll.salaryadvancerules(ruleid),
    periodid            uuid, 
    totalamount         numeric(15,2) not null,
    remainingamount     numeric(15,2) not null,
    monthlydeduction    numeric(15,2) default 0,
    reason              text,
    advancestatus       varchar(20) default 'pending'
                        check (advancestatus in ('pending', 'approved', 'completed', 'cancelled')),
    approvedby          uuid,
    createdby           uuid,
    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp
);

create table if not exists payroll.advancepayments
(
    id                  uuid primary key default uuid_generate_v4(),
    organizationid      uuid not null,
    advanceid           uuid references payroll.salaryadvance(advanceid),
    employeeid          uuid not null,
    periodid            uuid not null,
    deductionamount     numeric(15,2) not null,
    remainingamount     numeric(15,2) not null,
    paymentstatus       varchar(20) default 'deducted',
    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp,
    constraint uqadvancepayments unique (advanceid, periodid)
); // advance salary

// pf rules
create table if not exists payroll.pfrules
(
    ruleid              uuid primary key default uuid_generate_v4(),
    rulename            character varying(100),
    description         text,
    organizationid      uuid not null,
    defaultemployeepct  numeric(5,2) default 0,
    defaultemployerpct  numeric(5,2) default 0,
    mincontribution     numeric(15,2),
    maxcontribution     numeric(15,2),
    isactive            boolean default true,
    isdeleted           boolean default false,
    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp,
    constraint uqorgpfrule unique (organizationid)
);

create table if not exists payroll.providentfund
(
    pfid                uuid primary key default uuid_generate_v4(),
    organizationid      uuid not null,
    employeeid          uuid not null,
    ruleid              uuid references payroll.pfrules(ruleid),
    employeepct         numeric(5,2) not null,
    employerpct         numeric(5,2) not null,
    isenrolled          boolean default false,
    pfstatus            varchar(20) default 'pending'
                        check (pfstatus in ('pending', 'approved', 'completed', 'cancelled')),
    effectivefrom       date,
    isactive            boolean default true,
    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp,
    constraint uqprovidentfund unique (organizationid, employeeid)
);

create table if not exists payroll.pftransactions
(
    id                  uuid primary key default uuid_generate_v4(),
    organizationid      uuid not null,
    employeeid          uuid not null,
    periodid            uuid not null,
    pfid                uuid references payroll.providentfund(pfid),
    employeeamount      numeric(15,2) not null,
    employeramount      numeric(15,2) not null,
    totalamount         numeric(15,2) generated always as (
                            employeeamount + employeramount
                        ) stored,
    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp,
    constraint uqpftransactions unique (employeeid, periodid)
); // pf rules

// tax slabs 
create table if not exists payroll.taxslabs
(
    slabid          uuid primary key default uuid_generate_v4(),
    organizationid  uuid not null,
    slabrulename        character varying(100) not null,
    description         text,
    fiscalyear      character varying(20) not null,
    minincome       numeric(15,2) not null,
    maxincome       numeric(15,2),
    fixedamount     numeric(15,2) default 0,
    percentage      numeric(5,2) default 0,
    isactive        boolean default true,
    isdeleted       boolean default false,
    createdat       timestamptz default current_timestamp,
    updatedat       timestamptz default current_timestamp,
    constraint uqtaxslab unique (organizationid, fiscalyear, minincome)
);

create table if not exists payroll.taxtransactions
(
    id              uuid primary key default uuid_generate_v4(),
    organizationid  uuid not null,
    employeeid      uuid not null,
    periodid        uuid not null,
    slabid          uuid references payroll.taxslabs(slabid),
    annualsalary    numeric(15,2),
    taxableincome   numeric(15,2),
    fixedamount     numeric(15,2) default 0,
    taxpercentage   numeric(5,2) default 0,
    taxamount       numeric(15,2),
    createdat       timestamptz default current_timestamp,
    updatedat       timestamptz default current_timestamp,
    constraint uqtaxtransaction unique (employeeid, periodid)
);

// social security
create table if not exists payroll.socialsecurityconfig
(
    id                  uuid primary key default uuid_generate_v4(),
    organizationid      uuid not null,
    configrulename          varchar(100) not null,
    description         text,
    employeepercentage  numeric(5,2) not null default 0,
    employerpercentage  numeric(5,2) not null default 0,
    maxsalarylimit      numeric(15,2),
    isactive            boolean default true,
    isdeleted           boolean default false,
    effectivefrom       date,
    effectiveto         date,
    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp,
    constraint uqssconfig unique (organizationid, configname)
);

create table if not exists payroll.socialsecuritytransactions
(
    id                  uuid primary key default uuid_generate_v4(),
    organizationid      uuid not null,
    employeeid          uuid not null,
    periodid            uuid not null,
    configid            uuid references payroll.socialsecurityconfig(id),
    salarycapped        numeric(15,2),
    employeeamount      numeric(15,2) default 0,
    employeramount      numeric(15,2) default 0,
    totalamount         numeric(15,2) generated always as (
                            employeeamount + employeramount
                        ) stored,
    requeststatus       varchar(20) default 'pending', 
    isenrolled          boolean default false,         
    withdrawalrequest   boolean default false,         
    withdrawalstatus    varchar(20),                   
    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp,
    constraint uqsstransaction unique (employeeid, periodid)
); // social security

// gratuity
create table if not exists payroll.gratuityconfig
(
    id                  uuid primary key default uuid_generate_v4(),
    organizationid      uuid not null,
    configrulename          varchar(100) not null,
    description         text,
    yearsrequired       integer not null default 5,
    calculationtype     varchar(50) not null 
                        check (calculationtype in ('fixed', 'peryear', 'percentage')),
    calculationvalue    numeric(10,2) not null,
    isactive            boolean default true,
    isdeleted           boolean default false,
    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp,
    constraint uqgratuityconfig unique (organizationid, configname)
);

create table if not exists payroll.gratuitytransactions
(
    id                  uuid primary key default uuid_generate_v4(),
    organizationid      uuid not null,
    employeeid          uuid not null,
    periodid            uuid,
    configid            uuid references payroll.gratuityconfig(id),
    yearsworked         numeric(4,2) not null,
    lastsalary          numeric(15,2),
    totalamount         numeric(15,2) not null,
    paymentstatus       varchar(20) default 'calculated'
                        check (paymentstatus in ('calculated', 'approved', 'paid', 'cancelled')),
    paidat              timestamptz,
    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp,
    constraint uqgratuitytransaction unique (employeeid, organizationid) 
); // gratuity

// payrolldata 
create table if not exists payroll.payrolldata
(
    dataid              uuid primary key default uuid_generate_v4(),
    organizationid      uuid not null,
    employeeid          uuid not null,
    periodid            uuid not null,
    entryid             uuid,

    employeesnapshot    jsonb not null default '{}',

    basicsalary         numeric(15,2) default 0,
    earningsdetail      jsonb not null default '[]',
    totalearnings       numeric(15,2) default 0,
    overtimeamount      numeric(15,2) default 0,
    bonusamount         numeric(15,2) default 0,
    grosssalary         numeric(15,2) default 0,

    scheduleddays       integer default 0,
    presentdays         integer default 0,
    absentdays          numeric(6,2) default 0,
    halfdays            integer default 0,
    latedays            integer default 0,
    overtimehours       numeric(6,2) default 0,

    absentdeduction     numeric(15,2) default 0,
    latededuction       numeric(15,2) default 0,
    loandeduction       numeric(15,2) default 0,
    advancededuction    numeric(15,2) default 0,
    taxamount           numeric(15,2) default 0,
    pfemployee          numeric(15,2) default 0,
    socialsecurity      numeric(15,2) default 0,
    otherdeductions     numeric(15,2) default 0,
    deductionsdetail    jsonb not null default '[]',
    totaldeductions     numeric(15,2) default 0,

    pfemployer          numeric(15,2) default 0,
    ssemployer          numeric(15,2) default 0,

    ytdgross            numeric(15,2) default 0,
    ytdtax              numeric(15,2) default 0,
    ytdnet              numeric(15,2) default 0,

    netsalary           numeric(15,2) default 0,
    currency            character varying(3) default 'pkr',

    status              varchar(20) default 'draft'
                        check (status in ('draft', 'generated', 'sent', 'viewed')),
    generatedby         uuid,
    generatedat         timestamptz,

    emailsentat         timestamptz,
    emailsentto         varchar(255),

    payslippdfurl       text,
    pdfgeneratedat      timestamptz,

    isreissued          boolean default false,
    reissuedby          uuid,
    reissuedat          timestamptz,
    reissuenote         text,
    versionnumber       integer default 1,

    createdat           timestamptz default current_timestamp,
    updatedat           timestamptz default current_timestamp,

    constraint uqpayslips unique (employeeid, periodid, versionnumber)
);

// payslipemaillog
create table if not exists payroll.payslipemaillog
(
    id              uuid primary key default uuid_generate_v4(),
    payslipid       uuid not null references payroll.payrolldata(dataid),
    organizationid  uuid not null,
    employeeid      uuid not null,
    sentat          timestamptz default current_timestamp,
    sentto          varchar(255) not null,
    sentby          uuid,
    status          varchar(20) default 'sent'
                    check (status in ('sent', 'failed', 'bounced')),
    errormessage    text
); // payslipemaillog