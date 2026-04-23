
-- ============================================================
-- SEQUENCES
-- ============================================================
-- admin.schema_migrations_id_seq
CREATE SEQUENCE admin.schema_migrations_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- payment.invoice_number_seq
CREATE SEQUENCE payment.invoice_number_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


-- ============================================================
-- SCHEMA: ADMIN
-- ============================================================

-- Table: admin.auditlogs
CREATE TABLE admin.auditlogs (
    logid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid,
    userid uuid,
    tablename character varying(100) NOT NULL,
    recordid uuid NOT NULL,
    action character varying(20) NOT NULL,
    oldvalues jsonb,
    newvalues jsonb,
    ipaddress inet,
    useragent text,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY admin.auditlogs
    ADD CONSTRAINT auditlogs_pkey PRIMARY KEY (logid);

-- Table: admin.demo_inquiries
CREATE TABLE admin.demo_inquiries (
    inquiry_id uuid DEFAULT gen_random_uuid() NOT NULL,
    full_name character varying NOT NULL,
    email character varying NOT NULL,
    phone character varying,
    company_name character varying,
    company_size character varying,
    industry character varying,
    country character varying,
    message text,
    status character varying(50) DEFAULT 'requested'::character varying,
    notes text,
    source character varying(100) DEFAULT 'website'::character varying,
    created_at timestamp with time zone DEFAULT now(),
    updated_at timestamp with time zone DEFAULT now()
);

ALTER TABLE ONLY admin.demo_inquiries
    ADD CONSTRAINT demo_inquiries_pkey PRIMARY KEY (inquiry_id);

-- Table: admin.platform_admins
CREATE TABLE admin.platform_admins (
    admin_id uuid DEFAULT gen_random_uuid() NOT NULL,
    email character varying(255) NOT NULL,
    password_hash character varying(500) NOT NULL,
    full_name character varying(200) NOT NULL,
    is_active boolean DEFAULT true,
    last_login_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT now(),
    updated_at timestamp with time zone DEFAULT now()
);

ALTER TABLE ONLY admin.platform_admins
    ADD CONSTRAINT platform_admins_email_key UNIQUE (email);
ALTER TABLE ONLY admin.platform_admins
    ADD CONSTRAINT platform_admins_pkey PRIMARY KEY (admin_id);

-- Table: admin.schema_migrations
CREATE TABLE admin.schema_migrations (
    id integer DEFAULT nextval('admin.schema_migrations_id_seq'::regclass) NOT NULL,
    version character varying(100) NOT NULL,
    description text,
    applied_by character varying(100),
    applied_at timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY admin.schema_migrations
    ADD CONSTRAINT schema_migrations_pkey PRIMARY KEY (id);
ALTER TABLE ONLY admin.schema_migrations
    ADD CONSTRAINT schema_migrations_version_key UNIQUE (version);

-- Table: admin.system_jobs
CREATE TABLE admin.system_jobs (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    jobkey character varying(100) NOT NULL,
    year integer NOT NULL,
    runat timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY admin.system_jobs
    ADD CONSTRAINT system_jobs_pkey PRIMARY KEY (id);


-- ============================================================
-- SCHEMA: AUTH
-- ============================================================

-- Table: auth.actions
CREATE TABLE auth.actions (
    actionid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    submenuid uuid NOT NULL,
    actionname character varying(200) NOT NULL,
    actionkey character varying(100) NOT NULL,
    created_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY auth.actions
    ADD CONSTRAINT actions_pkey PRIMARY KEY (actionid);
ALTER TABLE ONLY auth.actions
    ADD CONSTRAINT fk_submenu FOREIGN KEY (submenuid) REFERENCES auth.submenus(submenuid);

-- Table: auth.menus
CREATE TABLE auth.menus (
    menuid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    menuname character varying(200) NOT NULL,
    icon character varying(100),
    created_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY auth.menus
    ADD CONSTRAINT menus_pkey PRIMARY KEY (menuid);

-- Table: auth.organizations
CREATE TABLE auth.organizations (
    organizationid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    name character varying(255) NOT NULL,
    subscriptiontype character varying(50) DEFAULT 'basic'::character varying,
    domain character varying(255),
    logourl character varying(500),
    status character varying(20) DEFAULT 'active'::character varying,
    settings jsonb DEFAULT '{}'::jsonb,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    currency character varying(50) DEFAULT NULL::character varying,
    careerbgimageurl character varying(500),
    careerheadertext character varying(255),
    careerdescription text,
    industry character varying(100),
    country character varying(100),
    admin_email character varying(255),
    company_size character varying(50),
    timezone character varying(100),
    culture character varying(20)
);

ALTER TABLE ONLY auth.organizations
    ADD CONSTRAINT organizations_pkey PRIMARY KEY (organizationid);

-- Table: auth.password_reset_tokens
CREATE TABLE auth.password_reset_tokens (
    tokenid uuid DEFAULT gen_random_uuid() NOT NULL,
    userid uuid NOT NULL,
    token text NOT NULL,
    expiresat timestamp with time zone NOT NULL,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    used boolean DEFAULT false
);

ALTER TABLE ONLY auth.password_reset_tokens
    ADD CONSTRAINT password_reset_tokens_pkey PRIMARY KEY (tokenid);
ALTER TABLE ONLY auth.password_reset_tokens
    ADD CONSTRAINT password_reset_tokens_userid_fkey FOREIGN KEY (userid) REFERENCES auth.users(userid) ON DELETE CASCADE;

-- Table: auth.permissions
CREATE TABLE auth.permissions (
    permissionid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    roleid uuid NOT NULL,
    actionid uuid NOT NULL,
    organizationid uuid NOT NULL,
    has_permission boolean DEFAULT false,
    created_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    createdby uuid,
    updatedby uuid
);

-- Table: auth.roles
CREATE TABLE auth.roles (
    roleid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid,
    rolename character varying(100) NOT NULL,
    description text,
    permissions jsonb DEFAULT '{}'::jsonb,
    issystemrole boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY auth.roles
    ADD CONSTRAINT roles_pkey PRIMARY KEY (roleid);
ALTER TABLE ONLY auth.roles
    ADD CONSTRAINT uq_roles UNIQUE (organizationid, rolename);

-- Table: auth.submenus
CREATE TABLE auth.submenus (
    submenuid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    menuid uuid NOT NULL,
    submenuname character varying(200) NOT NULL,
    icon character varying(100),
    created_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_date timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY auth.submenus
    ADD CONSTRAINT sub_menus_pkey PRIMARY KEY (submenuid);

-- Table: auth.users
CREATE TABLE auth.users (
    userid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid,
    email character varying(255) NOT NULL,
    passwordhash character varying(255) NOT NULL,
    roleid uuid,
    firstname character varying(100),
    lastname character varying(100),
    isactive boolean DEFAULT true,
    emailverified boolean DEFAULT false,
    lastloginat timestamp with time zone,
    passwordchangedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    refreshtoken character varying(500),
    refreshtokenexpiresat timestamp with time zone,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    emailverify boolean,
    emailverifytoken character varying,
    emailverifyexpiry timestamp with time zone,
    "FaceEmbedding" jsonb
);

ALTER TABLE ONLY auth.users
    ADD CONSTRAINT users_email_key UNIQUE (email);
ALTER TABLE ONLY auth.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (userid);


-- ============================================================
-- SCHEMA: EMPLOYEE
-- ============================================================

-- Table: employee.departments
CREATE TABLE employee.departments (
    departmentid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid,
    departmentname character varying(100) NOT NULL,
    description text,
    managerid uuid,
    parentdepartmentid uuid,
    budget numeric(15,2),
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY employee.departments
    ADD CONSTRAINT departments_pkey PRIMARY KEY (departmentid);
ALTER TABLE ONLY employee.departments
    ADD CONSTRAINT uq_departments UNIQUE (organizationid, departmentname);

-- Table: employee.documents
CREATE TABLE employee.documents (
    documentid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    employeeid uuid,
    documenttype character varying(100) NOT NULL,
    documentname character varying(255) NOT NULL,
    fileurl character varying(500) NOT NULL,
    filesize bigint,
    mimetype character varying(100),
    uploadedby uuid,
    isconfidential boolean DEFAULT false,
    expirydate date,
    status character varying(20) DEFAULT 'active'::character varying,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY employee.documents
    ADD CONSTRAINT documents_pkey PRIMARY KEY (documentid);

-- Table: employee.employee_face_descriptors
CREATE TABLE employee.employee_face_descriptors (
    descriptorid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    employeeid uuid NOT NULL,
    descriptor_vector jsonb NOT NULL,
    source_photo_url character varying(500) NOT NULL,
    isactive boolean DEFAULT true NOT NULL,
    generatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    generatedby uuid,
    superseded_at timestamp with time zone
);

ALTER TABLE ONLY employee.employee_face_descriptors
    ADD CONSTRAINT employee_face_descriptors_pkey PRIMARY KEY (descriptorid);
ALTER TABLE ONLY employee.employee_face_descriptors
    ADD CONSTRAINT fk_efd_employee FOREIGN KEY (employeeid) REFERENCES employee.employees(employeeid) ON DELETE CASCADE;

-- Table: employee.employees
CREATE TABLE employee.employees (
    employeeid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid,
    userid uuid,
    employeecode character varying(50) NOT NULL,
    firstname character varying(100) NOT NULL,
    lastname character varying(100) NOT NULL,
    email character varying(255) NOT NULL,
    phone character varying(20),
    address jsonb DEFAULT '{}'::jsonb,
    dateofbirth date,
    gender character varying(20),
    nationality character varying(50),
    maritalstatus character varying(20),
    emergencycontact jsonb DEFAULT '{}'::jsonb,
    hiredate date NOT NULL,
    terminationdate date,
    status character varying(20) DEFAULT 'active'::character varying,
    profilepictureurl character varying(500),
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY employee.employees
    ADD CONSTRAINT employees_pkey PRIMARY KEY (employeeid);
ALTER TABLE ONLY employee.employees
    ADD CONSTRAINT uq_employeecode UNIQUE (organizationid, employeecode);
ALTER TABLE ONLY employee.employees
    ADD CONSTRAINT uq_employeeemail UNIQUE (organizationid, email);

-- Table: employee.employmentdetails
CREATE TABLE employee.employmentdetails (
    detailid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    employeeid uuid,
    departmentid uuid,
    positionid uuid,
    shiftid uuid,
    employmenttype character varying(50) DEFAULT 'full_time'::character varying,
    paytype character varying(20) DEFAULT 'salary'::character varying,
    basicsalary numeric(15,2),
    currency character varying(3) DEFAULT 'USD'::character varying,
    worklocation character varying(100),
    reportingmanagerid uuid,
    effectivedate date NOT NULL,
    enddate date,
    iscurrent boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY employee.employmentdetails
    ADD CONSTRAINT employmentdetails_pkey PRIMARY KEY (detailid);

-- Table: employee.positions
CREATE TABLE employee.positions (
    positionid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid,
    positiontitle character varying(100) NOT NULL,
    departmentid uuid,
    description text,
    requirements jsonb DEFAULT '{}'::jsonb,
    salaryrange jsonb DEFAULT '{}'::jsonb,
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    roleid uuid
);

ALTER TABLE ONLY employee.positions
    ADD CONSTRAINT positions_pkey PRIMARY KEY (positionid);


-- ============================================================
-- SCHEMA: ATTENDANCE
-- ============================================================

-- Table: attendance.attendance
CREATE TABLE attendance.attendance (
    attendanceid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    employeeid uuid,
    workdate date NOT NULL,
    checkintime timestamp with time zone,
    checkouttime timestamp with time zone,
    breakstarttime timestamp with time zone,
    breakendtime timestamp with time zone,
    totalhours numeric(5,2) DEFAULT 0,
    overtimehours numeric(5,2) DEFAULT 0,
    breakhours numeric(5,2) DEFAULT 0,
    status character varying(20) DEFAULT 'absent'::character varying,
    location jsonb DEFAULT '{}'::jsonb,
    notes text,
    approvedby uuid,
    approvedat timestamp with time zone,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    islate boolean DEFAULT false,
    is_finalized boolean DEFAULT false,
    geofenceid uuid,
    clock_in_latitude numeric(10,7),
    clock_in_longitude numeric(10,7),
    clock_in_location_status character varying(30),
    clock_in_distance_m numeric(10,2),
    clock_out_latitude numeric(10,7),
    clock_out_longitude numeric(10,7),
    clock_out_location_status character varying(30),
    clock_out_distance_m numeric(10,2),
    clock_in_face_log_id uuid,
    clock_out_face_log_id uuid,
    CONSTRAINT attendance_clock_in_location_status_check CHECK (((clock_in_location_status)::text = ANY (ARRAY[('in_zone'::character varying)::text, ('out_of_bounds'::character varying)::text, ('remote'::character varying)::text, ('unknown'::character varying)::text]))),
    CONSTRAINT attendance_clock_out_location_status_check CHECK (((clock_out_location_status)::text = ANY (ARRAY[('in_zone'::character varying)::text, ('out_of_bounds'::character varying)::text, ('remote'::character varying)::text, ('unknown'::character varying)::text])))
);

ALTER TABLE ONLY attendance.attendance
    ADD CONSTRAINT attendance_pkey PRIMARY KEY (attendanceid);
ALTER TABLE ONLY attendance.attendance
    ADD CONSTRAINT uq_attendance UNIQUE (employeeid, workdate);
ALTER TABLE ONLY attendance.attendance
    ADD CONSTRAINT fk_att_geofence FOREIGN KEY (geofenceid) REFERENCES attendance.geo_fences(geofenceid) ON DELETE SET NULL;
ALTER TABLE ONLY attendance.attendance
    ADD CONSTRAINT fk_att_in_facelog FOREIGN KEY (clock_in_face_log_id) REFERENCES attendance.face_recognition_logs(logid) ON DELETE SET NULL;
ALTER TABLE ONLY attendance.attendance
    ADD CONSTRAINT fk_att_out_facelog FOREIGN KEY (clock_out_face_log_id) REFERENCES attendance.face_recognition_logs(logid) ON DELETE SET NULL;

-- Table: attendance.attendance_update_requests
CREATE TABLE attendance.attendance_update_requests (
    request_id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    attendance_id uuid,
    employee_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    requested_checkin timestamp with time zone,
    requested_checkout timestamp with time zone,
    requested_status character varying(20),
    requested_notes text,
    reason_for_edit text NOT NULL,
    status character varying(20) DEFAULT 'pending'::character varying,
    submitted_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    actioned_by uuid,
    actioned_at timestamp with time zone,
    rejection_reason text,
    timesheet_id uuid,
    workdate date
);

ALTER TABLE ONLY attendance.attendance_update_requests
    ADD CONSTRAINT attendance_update_requests_pkey PRIMARY KEY (request_id);
ALTER TABLE ONLY attendance.attendance_update_requests
    ADD CONSTRAINT attendance_update_requests_actioned_by_fkey FOREIGN KEY (actioned_by) REFERENCES employee.employees(employeeid);
ALTER TABLE ONLY attendance.attendance_update_requests
    ADD CONSTRAINT attendance_update_requests_attendance_id_fkey FOREIGN KEY (attendance_id) REFERENCES attendance.attendance(attendanceid) ON DELETE CASCADE;
ALTER TABLE ONLY attendance.attendance_update_requests
    ADD CONSTRAINT attendance_update_requests_employee_id_fkey FOREIGN KEY (employee_id) REFERENCES employee.employees(employeeid);
ALTER TABLE ONLY attendance.attendance_update_requests
    ADD CONSTRAINT attendance_update_requests_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES auth.organizations(organizationid);

-- Table: attendance.attendancesessions
CREATE TABLE attendance.attendancesessions (
    sessionid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    attendanceid uuid NOT NULL,
    checkintime timestamp with time zone NOT NULL,
    checkouttime timestamp with time zone,
    location jsonb DEFAULT '{}'::jsonb,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    checkinip character varying(45),
    checkoutip character varying(45),
    geofenceid uuid,
    latitude numeric(10,7),
    longitude numeric(10,7),
    location_status character varying(30),
    distance_from_fence_m numeric(10,2),
    face_log_id uuid,
    CONSTRAINT attendancesessions_location_status_check CHECK (((location_status)::text = ANY (ARRAY[('in_zone'::character varying)::text, ('out_of_bounds'::character varying)::text, ('remote'::character varying)::text, ('unknown'::character varying)::text])))
);

ALTER TABLE ONLY attendance.attendancesessions
    ADD CONSTRAINT attendancesessions_pkey PRIMARY KEY (sessionid);
ALTER TABLE ONLY attendance.attendancesessions
    ADD CONSTRAINT attendancesessions_attendanceid_fkey FOREIGN KEY (attendanceid) REFERENCES attendance.attendance(attendanceid) ON DELETE CASCADE;
ALTER TABLE ONLY attendance.attendancesessions
    ADD CONSTRAINT fk_sess_facelog FOREIGN KEY (face_log_id) REFERENCES attendance.face_recognition_logs(logid) ON DELETE SET NULL;
ALTER TABLE ONLY attendance.attendancesessions
    ADD CONSTRAINT fk_sess_geofence FOREIGN KEY (geofenceid) REFERENCES attendance.geo_fences(geofenceid) ON DELETE SET NULL;

-- Table: attendance.company_holidays
CREATE TABLE attendance.company_holidays (
    holidayid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    holidayname character varying(200) NOT NULL,
    holidaydate date NOT NULL,
    holidaytype character varying(20) DEFAULT 'mandatory'::character varying NOT NULL,
    description text,
    ishalfday boolean DEFAULT false,
    year integer NOT NULL,
    applicableto character varying(20) DEFAULT 'all'::character varying NOT NULL,
    applicablevalue text,
    catalogholidayid uuid,
    createdby uuid,
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_applicable_to CHECK (((applicableto)::text = ANY ((ARRAY['all'::character varying, 'department'::character varying, 'location'::character varying, 'employee'::character varying])::text[]))),
    CONSTRAINT chk_holiday_type CHECK (((holidaytype)::text = ANY ((ARRAY['mandatory'::character varying, 'restricted'::character varying, 'optional'::character varying])::text[])))
);

ALTER TABLE ONLY attendance.company_holidays
    ADD CONSTRAINT company_holidays_pkey PRIMARY KEY (holidayid);
ALTER TABLE ONLY attendance.company_holidays
    ADD CONSTRAINT uq_company_holiday UNIQUE (organizationid, holidayname, holidaydate);
ALTER TABLE ONLY attendance.company_holidays
    ADD CONSTRAINT fk_company_holidays_catalog FOREIGN KEY (catalogholidayid) REFERENCES attendance.holiday_catalogs(catalogid) ON DELETE SET NULL;
ALTER TABLE ONLY attendance.company_holidays
    ADD CONSTRAINT fk_company_holidays_org FOREIGN KEY (organizationid) REFERENCES auth.organizations(organizationid) ON DELETE CASCADE;

-- Table: attendance.employee_restricted_holidays
CREATE TABLE attendance.employee_restricted_holidays (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    companyholidayid uuid NOT NULL,
    status character varying(20) DEFAULT 'selected'::character varying NOT NULL,
    selectedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_erh_status CHECK (((status)::text = ANY ((ARRAY['selected'::character varying, 'cancelled'::character varying])::text[])))
);

ALTER TABLE ONLY attendance.employee_restricted_holidays
    ADD CONSTRAINT employee_restricted_holidays_pkey PRIMARY KEY (id);
ALTER TABLE ONLY attendance.employee_restricted_holidays
    ADD CONSTRAINT uq_employee_restricted UNIQUE (employeeid, companyholidayid);
ALTER TABLE ONLY attendance.employee_restricted_holidays
    ADD CONSTRAINT fk_erh_employee FOREIGN KEY (employeeid) REFERENCES employee.employees(employeeid) ON DELETE CASCADE;
ALTER TABLE ONLY attendance.employee_restricted_holidays
    ADD CONSTRAINT fk_erh_holiday FOREIGN KEY (companyholidayid) REFERENCES attendance.company_holidays(holidayid) ON DELETE CASCADE;
ALTER TABLE ONLY attendance.employee_restricted_holidays
    ADD CONSTRAINT fk_erh_org FOREIGN KEY (organizationid) REFERENCES auth.organizations(organizationid) ON DELETE CASCADE;

-- Table: attendance.face_recognition_logs
CREATE TABLE attendance.face_recognition_logs (
    logid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    employeeid uuid NOT NULL,
    attempttime timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    face_descriptor jsonb,
    reference_photo_url character varying(500),
    match_result character varying(20) DEFAULT 'error'::character varying NOT NULL,
    confidence_score numeric(5,4),
    distance_score numeric(5,4),
    threshold_used numeric(5,4) DEFAULT 0.6 NOT NULL,
    geofenceid uuid,
    latitude numeric(10,7),
    longitude numeric(10,7),
    device_info jsonb DEFAULT '{}'::jsonb,
    sessionid uuid,
    attendanceid uuid,
    manually_overridden boolean DEFAULT false NOT NULL,
    overridden_by uuid,
    override_reason text,
    overridden_at timestamp with time zone,
    CONSTRAINT face_recognition_logs_match_result_check CHECK (((match_result)::text = ANY (ARRAY[('match'::character varying)::text, ('no_match'::character varying)::text, ('low_confidence'::character varying)::text, ('error'::character varying)::text])))
);

ALTER TABLE ONLY attendance.face_recognition_logs
    ADD CONSTRAINT face_recognition_logs_pkey PRIMARY KEY (logid);
ALTER TABLE ONLY attendance.face_recognition_logs
    ADD CONSTRAINT fk_frl_attendance FOREIGN KEY (attendanceid) REFERENCES attendance.attendance(attendanceid) ON DELETE SET NULL;
ALTER TABLE ONLY attendance.face_recognition_logs
    ADD CONSTRAINT fk_frl_employee FOREIGN KEY (employeeid) REFERENCES employee.employees(employeeid) ON DELETE CASCADE;
ALTER TABLE ONLY attendance.face_recognition_logs
    ADD CONSTRAINT fk_frl_geofence FOREIGN KEY (geofenceid) REFERENCES attendance.geo_fences(geofenceid) ON DELETE SET NULL;
ALTER TABLE ONLY attendance.face_recognition_logs
    ADD CONSTRAINT fk_frl_session FOREIGN KEY (sessionid) REFERENCES attendance.attendancesessions(sessionid) ON DELETE SET NULL;

-- Table: attendance.finalized_timesheet_records
CREATE TABLE attendance.finalized_timesheet_records (
    record_id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    timesheet_id uuid,
    employee_id uuid NOT NULL,
    organization_id uuid NOT NULL,
    work_date date NOT NULL,
    final_checkin timestamp with time zone,
    final_checkout timestamp with time zone,
    final_total_hours numeric(5,2),
    final_overtime_hours numeric(5,2),
    final_status character varying(20),
    approved_by uuid,
    approved_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    attendance_id uuid,
    override_reason text,
    record_status character varying(20) DEFAULT 'finalized'::character varying NOT NULL,
    final_late_hours numeric(5,2)
);

ALTER TABLE ONLY attendance.finalized_timesheet_records
    ADD CONSTRAINT finalized_timesheet_records_pkey PRIMARY KEY (record_id);
ALTER TABLE ONLY attendance.finalized_timesheet_records
    ADD CONSTRAINT uq_ftr_timesheet_employee_workdate UNIQUE (timesheet_id, employee_id, work_date);
ALTER TABLE ONLY attendance.finalized_timesheet_records
    ADD CONSTRAINT finalized_timesheet_records_approved_by_fkey FOREIGN KEY (approved_by) REFERENCES employee.employees(employeeid);
ALTER TABLE ONLY attendance.finalized_timesheet_records
    ADD CONSTRAINT finalized_timesheet_records_attendance_id_fkey FOREIGN KEY (attendance_id) REFERENCES attendance.attendance(attendanceid) ON DELETE CASCADE;
ALTER TABLE ONLY attendance.finalized_timesheet_records
    ADD CONSTRAINT finalized_timesheet_records_employee_id_fkey FOREIGN KEY (employee_id) REFERENCES employee.employees(employeeid);
ALTER TABLE ONLY attendance.finalized_timesheet_records
    ADD CONSTRAINT finalized_timesheet_records_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES auth.organizations(organizationid);

-- Table: attendance.geo_fence_violations
CREATE TABLE attendance.geo_fence_violations (
    violationid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    employeeid uuid NOT NULL,
    shiftid uuid,
    geofenceid uuid,
    attendanceid uuid,
    face_log_id uuid,
    organizationid uuid NOT NULL,
    violatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    actual_latitude numeric(10,7) NOT NULL,
    actual_longitude numeric(10,7) NOT NULL,
    distance_over_meters numeric(10,2) NOT NULL,
    action_taken character varying(30) DEFAULT 'blocked'::character varying NOT NULL,
    manager_notified boolean DEFAULT false NOT NULL,
    manager_notified_at timestamp with time zone,
    employee_notified boolean DEFAULT false NOT NULL,
    resolved boolean DEFAULT false NOT NULL,
    resolved_by uuid,
    resolvedat timestamp with time zone,
    resolution_notes text,
    CONSTRAINT geo_fence_violations_action_taken_check CHECK (((action_taken)::text = ANY (ARRAY[('blocked'::character varying)::text, ('allowed_with_warning'::character varying)::text, ('escalated'::character varying)::text])))
);

ALTER TABLE ONLY attendance.geo_fence_violations
    ADD CONSTRAINT geo_fence_violations_pkey PRIMARY KEY (violationid);
ALTER TABLE ONLY attendance.geo_fence_violations
    ADD CONSTRAINT fk_gfv_attendance FOREIGN KEY (attendanceid) REFERENCES attendance.attendance(attendanceid) ON DELETE SET NULL;
ALTER TABLE ONLY attendance.geo_fence_violations
    ADD CONSTRAINT fk_gfv_employee FOREIGN KEY (employeeid) REFERENCES employee.employees(employeeid) ON DELETE CASCADE;
ALTER TABLE ONLY attendance.geo_fence_violations
    ADD CONSTRAINT fk_gfv_facelog FOREIGN KEY (face_log_id) REFERENCES attendance.face_recognition_logs(logid) ON DELETE SET NULL;
ALTER TABLE ONLY attendance.geo_fence_violations
    ADD CONSTRAINT fk_gfv_fence FOREIGN KEY (geofenceid) REFERENCES attendance.geo_fences(geofenceid) ON DELETE SET NULL;
ALTER TABLE ONLY attendance.geo_fence_violations
    ADD CONSTRAINT fk_gfv_shift FOREIGN KEY (shiftid) REFERENCES attendance.shifts(shiftid) ON DELETE SET NULL;

-- Table: attendance.geo_fences
CREATE TABLE attendance.geo_fences (
    geofenceid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    name character varying(120) NOT NULL,
    description text,
    address character varying(300),
    center_latitude numeric(10,7),
    center_longitude numeric(10,7),
    radius_meters integer,
    polygon_coords jsonb,
    shape character varying(20) DEFAULT 'circle'::character varying NOT NULL,
    isactive boolean DEFAULT true NOT NULL,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    deleted_at timestamp with time zone,
    createdby uuid,
    CONSTRAINT chk_geofence_circle CHECK ((((shape)::text <> 'circle'::text) OR ((center_latitude IS NOT NULL) AND (center_longitude IS NOT NULL) AND (radius_meters IS NOT NULL) AND (radius_meters > 0)))),
    CONSTRAINT chk_geofence_polygon CHECK ((((shape)::text <> 'polygon'::text) OR (polygon_coords IS NOT NULL))),
    CONSTRAINT geo_fences_shape_check CHECK (((shape)::text = ANY (ARRAY[('circle'::character varying)::text, ('polygon'::character varying)::text])))
);

ALTER TABLE ONLY attendance.geo_fences
    ADD CONSTRAINT geo_fences_pkey PRIMARY KEY (geofenceid);

-- Table: attendance.holiday_catalogs
CREATE TABLE attendance.holiday_catalogs (
    catalogid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    countrycode character varying(5) NOT NULL,
    countryname character varying(100) NOT NULL,
    holidayname character varying(200) NOT NULL,
    holidaydate date NOT NULL,
    isrecurring boolean DEFAULT true,
    description text,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY attendance.holiday_catalogs
    ADD CONSTRAINT holiday_catalogs_pkey PRIMARY KEY (catalogid);
ALTER TABLE ONLY attendance.holiday_catalogs
    ADD CONSTRAINT uq_holiday_catalog UNIQUE (countrycode, holidayname, holidaydate);

-- Table: attendance.holiday_employee_assignments
CREATE TABLE attendance.holiday_employee_assignments (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    holiday_id uuid NOT NULL,
    employee_id uuid NOT NULL,
    assigned_by uuid,
    assigned_at timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY attendance.holiday_employee_assignments
    ADD CONSTRAINT holiday_employee_assignments_pkey PRIMARY KEY (id);
ALTER TABLE ONLY attendance.holiday_employee_assignments
    ADD CONSTRAINT uq_holiday_employee UNIQUE (holiday_id, employee_id);
ALTER TABLE ONLY attendance.holiday_employee_assignments
    ADD CONSTRAINT holiday_employee_assignments_employee_id_fkey FOREIGN KEY (employee_id) REFERENCES employee.employees(employeeid) ON DELETE CASCADE;
ALTER TABLE ONLY attendance.holiday_employee_assignments
    ADD CONSTRAINT holiday_employee_assignments_holiday_id_fkey FOREIGN KEY (holiday_id) REFERENCES attendance.company_holidays(holidayid) ON DELETE CASCADE;

-- Table: attendance.ipaddresses
CREATE TABLE attendance.ipaddresses (
    id uuid NOT NULL,
    ipaddressvalue character varying(45) NOT NULL,
    name character varying(150) NOT NULL,
    createdat timestamp without time zone DEFAULT (now() AT TIME ZONE 'UTC'::text) NOT NULL
);

ALTER TABLE ONLY attendance.ipaddresses
    ADD CONSTRAINT ipaddresses_pkey PRIMARY KEY (id);

-- Table: attendance.leaveentitlements
CREATE TABLE attendance.leaveentitlements (
    entitlementid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    employeeid uuid,
    leavetypeid uuid,
    year integer NOT NULL,
    totaldays numeric(5,2) DEFAULT 0,
    useddays numeric(5,2) DEFAULT 0,
    remainingdays numeric(5,2) DEFAULT 0,
    carryforwarddays numeric(5,2) DEFAULT 0,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY attendance.leaveentitlements
    ADD CONSTRAINT leaveentitlements_pkey PRIMARY KEY (entitlementid);
ALTER TABLE ONLY attendance.leaveentitlements
    ADD CONSTRAINT uq_leaveentitlements UNIQUE (employeeid, leavetypeid, year);

-- Table: attendance.leaverequests
CREATE TABLE attendance.leaverequests (
    requestid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    employeeid uuid,
    leavetypeid uuid,
    startdate date NOT NULL,
    enddate date NOT NULL,
    daysrequested numeric(5,2) NOT NULL,
    reason text,
    attachmenturl character varying(500),
    status character varying(20) DEFAULT 'pending'::character varying,
    submittedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    approvedby uuid,
    approvedat timestamp with time zone,
    rejectionreason text,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY attendance.leaverequests
    ADD CONSTRAINT leaverequests_pkey PRIMARY KEY (requestid);

-- Table: attendance.leavetypes
CREATE TABLE attendance.leavetypes (
    leavetypeid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid,
    typename character varying(100) NOT NULL,
    description text,
    maxdaysperyear integer DEFAULT 0,
    ispaid boolean DEFAULT true,
    carryforwardallowed boolean DEFAULT false,
    maxcarryforwarddays integer DEFAULT 0,
    requiresapproval boolean DEFAULT true,
    advancenoticedays integer DEFAULT 0,
    color character varying(7) DEFAULT '#2196F3'::character varying,
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY attendance.leavetypes
    ADD CONSTRAINT leavetypes_pkey PRIMARY KEY (leavetypeid);
ALTER TABLE ONLY attendance.leavetypes
    ADD CONSTRAINT uq_leavetypes UNIQUE (organizationid, typename);

-- Table: attendance.monthly_timesheets
CREATE TABLE attendance.monthly_timesheets (
    timesheet_id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organization_id uuid NOT NULL,
    timesheet_name character varying(150) NOT NULL,
    month integer NOT NULL,
    year integer NOT NULL,
    generated_by uuid NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    status character varying(20) DEFAULT 'Draft'::character varying NOT NULL,
    CONSTRAINT chk_timesheet_status CHECK (((status)::text = ANY (ARRAY[('Draft'::character varying)::text, ('Submitted'::character varying)::text, ('Finalized'::character varying)::text, ('Archived'::character varying)::text]))),
    CONSTRAINT monthly_timesheets_month_check CHECK (((month >= 1) AND (month <= 12)))
);

ALTER TABLE ONLY attendance.monthly_timesheets
    ADD CONSTRAINT monthly_timesheets_pkey PRIMARY KEY (timesheet_id);
ALTER TABLE ONLY attendance.monthly_timesheets
    ADD CONSTRAINT uq_org_month_year_name UNIQUE (organization_id, month, year, timesheet_name);
ALTER TABLE ONLY attendance.monthly_timesheets
    ADD CONSTRAINT monthly_timesheets_generated_by_fkey FOREIGN KEY (generated_by) REFERENCES employee.employees(employeeid);
ALTER TABLE ONLY attendance.monthly_timesheets
    ADD CONSTRAINT monthly_timesheets_organization_id_fkey FOREIGN KEY (organization_id) REFERENCES auth.organizations(organizationid);

-- Table: attendance.shift_geo_fences
CREATE TABLE attendance.shift_geo_fences (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    shiftid uuid NOT NULL,
    geofenceid uuid NOT NULL,
    isactive boolean DEFAULT true NOT NULL,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);

ALTER TABLE ONLY attendance.shift_geo_fences
    ADD CONSTRAINT shift_geo_fences_pkey PRIMARY KEY (id);
ALTER TABLE ONLY attendance.shift_geo_fences
    ADD CONSTRAINT uq_shift_geo_fence UNIQUE (shiftid, geofenceid);
ALTER TABLE ONLY attendance.shift_geo_fences
    ADD CONSTRAINT fk_sgf_fence FOREIGN KEY (geofenceid) REFERENCES attendance.geo_fences(geofenceid) ON DELETE CASCADE;
ALTER TABLE ONLY attendance.shift_geo_fences
    ADD CONSTRAINT fk_sgf_shift FOREIGN KEY (shiftid) REFERENCES attendance.shifts(shiftid) ON DELETE CASCADE;

-- Table: attendance.shifts
CREATE TABLE attendance.shifts (
    shiftid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid,
    shiftname character varying(100) NOT NULL,
    starttime time without time zone NOT NULL,
    endtime time without time zone NOT NULL,
    breakduration integer DEFAULT 0,
    daysofweek integer[] DEFAULT '{1,2,3,4,5}'::integer[],
    timezone character varying(50) DEFAULT 'UTC'::character varying,
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    breakstarttime time without time zone,
    breakendtime time without time zone,
    overtimestarttime time without time zone,
    maxovertimeinminutes integer DEFAULT 0,
    marginhours integer DEFAULT 0 NOT NULL,
    applymarginhours boolean DEFAULT true NOT NULL,
    geofence_enforced boolean DEFAULT false NOT NULL,
    face_recog_required boolean DEFAULT false NOT NULL
);

ALTER TABLE ONLY attendance.shifts
    ADD CONSTRAINT shifts_pkey PRIMARY KEY (shiftid);
ALTER TABLE ONLY attendance.shifts
    ADD CONSTRAINT uq_shifts UNIQUE (organizationid, shiftname);

-- Table: attendance.shiftswaprequests
CREATE TABLE attendance.shiftswaprequests (
    requestid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    currentshiftid uuid,
    requestedshiftid uuid NOT NULL,
    reason text,
    status character varying(20) DEFAULT 'pending'::character varying,
    submittedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    approvedby uuid,
    approvedat timestamp with time zone,
    rejectionreason text,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    reportingmanagerid uuid
);

ALTER TABLE ONLY attendance.shiftswaprequests
    ADD CONSTRAINT shiftswaprequests_pkey PRIMARY KEY (requestid);

-- Table: attendance.system_health_snapshots
CREATE TABLE attendance.system_health_snapshots (
    snapshotid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    snapshot_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    attendance_health_pct numeric(5,2),
    total_employees_tracked integer,
    employees_clocked_in integer,
    geo_fence_violations_today integer,
    raw_data jsonb DEFAULT '{}'::jsonb
);

ALTER TABLE ONLY attendance.system_health_snapshots
    ADD CONSTRAINT system_health_snapshots_pkey PRIMARY KEY (snapshotid);


-- ============================================================
-- SCHEMA: ASSETS
-- ============================================================

-- Table: assets.asset_assignments
CREATE TABLE assets.asset_assignments (
    assignment_id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organization_id uuid NOT NULL,
    asset_id uuid NOT NULL,
    employee_id uuid NOT NULL,
    assigned_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    due_date timestamp with time zone,
    returned_at timestamp with time zone,
    status character varying(30) DEFAULT 'Active'::character varying,
    notes character varying(500),
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    created_by uuid,
    is_deleted boolean DEFAULT false,
    condition character varying(50)
);

-- Table: assets.assets
CREATE TABLE assets.assets (
    id character varying(36) NOT NULL,
    name character varying(200) NOT NULL,
    code character varying(100) NOT NULL,
    status character varying(50) DEFAULT 'Available'::character varying NOT NULL,
    purchasedate timestamp without time zone,
    description character varying(1000),
    typeid character varying(36) NOT NULL,
    createdat timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updatedat timestamp without time zone,
    isdeleted boolean DEFAULT false,
    organization_id uuid
);

-- Table: assets.assettypes
CREATE TABLE assets.assettypes (
    id character varying(36) NOT NULL,
    name character varying(100) NOT NULL,
    description character varying(500),
    createdat timestamp without time zone NOT NULL,
    updatedat timestamp without time zone,
    isdeleted boolean DEFAULT false,
    organization_id uuid
);

ALTER TABLE ONLY assets.assettypes
    ADD CONSTRAINT assettypes_pkey PRIMARY KEY (id);


-- ============================================================
-- SCHEMA: CHATBOARD
-- ============================================================

-- Table: chatboard.chat_board_logs
CREATE TABLE chatboard.chat_board_logs (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    employeeid uuid NOT NULL,
    organizationid uuid NOT NULL,
    rolename character varying(100),
    usermessage text,
    intent character varying(100),
    airesponse text,
    createdat timestamp with time zone DEFAULT now(),
    modulename character varying(100)
);

ALTER TABLE ONLY chatboard.chat_board_logs
    ADD CONSTRAINT chat_board_logs_pkey PRIMARY KEY (id);


-- ============================================================
-- SCHEMA: EXPENSE
-- ============================================================

-- Table: expense.expensecategories
CREATE TABLE expense.expensecategories (
    categoryid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    name character varying(100) NOT NULL,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY expense.expensecategories
    ADD CONSTRAINT expensecategories_pkey PRIMARY KEY (categoryid);

-- Table: expense.expenses
CREATE TABLE expense.expenses (
    expenseid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    categoryid uuid,
    title character varying(150) NOT NULL,
    description text,
    amount numeric(12,2) NOT NULL,
    billdate date NOT NULL,
    paymentmethod character varying(50),
    merchant character varying(150),
    receipturl text,
    status character varying(30) DEFAULT 'Pending'::character varying NOT NULL,
    approvedby uuid,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY expense.expenses
    ADD CONSTRAINT expenses_pkey PRIMARY KEY (expenseid);

-- Table: expense.recurringexpenses
CREATE TABLE expense.recurringexpenses (
    recurringexpenseid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    categoryid uuid NOT NULL,
    title character varying(150) NOT NULL,
    description text,
    amount numeric(12,2) NOT NULL,
    rotation character varying(20) NOT NULL,
    dayofmonth character varying(20),
    status character varying(30) DEFAULT 'Pending'::character varying NOT NULL,
    approvedby uuid,
    paymentmethod character varying(50),
    merchant character varying(150),
    receipturl text,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY expense.recurringexpenses
    ADD CONSTRAINT recurringexpenses_pkey PRIMARY KEY (recurringexpenseid);


-- ============================================================
-- SCHEMA: HELPDESK
-- ============================================================

-- Table: helpdesk.categories
CREATE TABLE helpdesk.categories (
    categoryid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    departmentid uuid NOT NULL,
    employeeid uuid NOT NULL,
    name character varying(100) NOT NULL,
    status boolean DEFAULT true,
    createdat timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY helpdesk.categories
    ADD CONSTRAINT categories_pkey PRIMARY KEY (categoryid);

-- Table: helpdesk.ticket_assigned_employees
CREATE TABLE helpdesk.ticket_assigned_employees (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    ticketid uuid NOT NULL,
    employeeid uuid NOT NULL,
    assignmentid uuid NOT NULL,
    createdat timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY helpdesk.ticket_assigned_employees
    ADD CONSTRAINT ticket_assigned_employees_pkey PRIMARY KEY (id);
ALTER TABLE ONLY helpdesk.ticket_assigned_employees
    ADD CONSTRAINT uq_ticket_employee UNIQUE (ticketid, employeeid);
ALTER TABLE ONLY helpdesk.ticket_assigned_employees
    ADD CONSTRAINT ticket_assigned_employees_assignmentid_fkey FOREIGN KEY (assignmentid) REFERENCES helpdesk.ticket_assignments(assignmentid) ON DELETE CASCADE;
ALTER TABLE ONLY helpdesk.ticket_assigned_employees
    ADD CONSTRAINT ticket_assigned_employees_ticketid_fkey FOREIGN KEY (ticketid) REFERENCES helpdesk.tickets(ticketid) ON DELETE CASCADE;

-- Table: helpdesk.ticket_assignments
CREATE TABLE helpdesk.ticket_assignments (
    assignmentid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    ticketid uuid NOT NULL,
    groupid uuid,
    assigned_employees uuid[],
    assignedby uuid NOT NULL,
    reason text,
    createdat timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ticket_assignments_check CHECK (((groupid IS NOT NULL) OR ((assigned_employees IS NOT NULL) AND (array_length(assigned_employees, 1) > 0))))
);

ALTER TABLE ONLY helpdesk.ticket_assignments
    ADD CONSTRAINT ticket_assignments_pkey PRIMARY KEY (assignmentid);
ALTER TABLE ONLY helpdesk.ticket_assignments
    ADD CONSTRAINT ticket_assignments_groupid_fkey FOREIGN KEY (groupid) REFERENCES helpdesk.ticket_group(groupid) ON DELETE CASCADE;
ALTER TABLE ONLY helpdesk.ticket_assignments
    ADD CONSTRAINT ticket_assignments_ticketid_fkey FOREIGN KEY (ticketid) REFERENCES helpdesk.tickets(ticketid) ON DELETE CASCADE;

-- Table: helpdesk.ticket_group
CREATE TABLE helpdesk.ticket_group (
    groupid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    grouptitle character varying(100) NOT NULL,
    employeeids uuid[],
    createdat timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    isactive boolean DEFAULT true,
    departmentid uuid,
    categoryid uuid
);

ALTER TABLE ONLY helpdesk.ticket_group
    ADD CONSTRAINT ticket_group_pkey PRIMARY KEY (groupid);

-- Table: helpdesk.ticket_message_recipients
CREATE TABLE helpdesk.ticket_message_recipients (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    message_id uuid NOT NULL,
    recipient_id uuid NOT NULL,
    is_read boolean DEFAULT false,
    read_at timestamp without time zone
);

ALTER TABLE ONLY helpdesk.ticket_message_recipients
    ADD CONSTRAINT ticket_message_recipients_pkey PRIMARY KEY (id);
ALTER TABLE ONLY helpdesk.ticket_message_recipients
    ADD CONSTRAINT ticket_message_recipients_message_id_fkey FOREIGN KEY (message_id) REFERENCES helpdesk.ticket_messages(message_id) ON DELETE CASCADE;

-- Table: helpdesk.ticket_messages
CREATE TABLE helpdesk.ticket_messages (
    message_id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    ticket_id uuid NOT NULL,
    sender_id uuid NOT NULL,
    message text NOT NULL,
    message_type character varying(20) NOT NULL,
    subject text,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    organizationid uuid NOT NULL,
    attachment_url character varying(500)
);

ALTER TABLE ONLY helpdesk.ticket_messages
    ADD CONSTRAINT ticket_messages_pkey PRIMARY KEY (message_id);
ALTER TABLE ONLY helpdesk.ticket_messages
    ADD CONSTRAINT ticket_messages_ticket_id_fkey FOREIGN KEY (ticket_id) REFERENCES helpdesk.tickets(ticketid) ON DELETE CASCADE;

-- Table: helpdesk.tickets
CREATE TABLE helpdesk.tickets (
    ticketid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    title character varying(100) NOT NULL,
    tickettype character varying(50),
    priority character varying(20) DEFAULT 'Medium'::character varying,
    description text NOT NULL,
    attachment character varying(500),
    createdat timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    isactive boolean DEFAULT true,
    departmentid uuid NOT NULL,
    status character varying(50) DEFAULT 'Pending'::character varying NOT NULL,
    categoryid uuid
);

ALTER TABLE ONLY helpdesk.tickets
    ADD CONSTRAINT tickets_pkey PRIMARY KEY (ticketid);


-- ============================================================
-- SCHEMA: JOBS
-- ============================================================

-- Table: jobs.applicationstages
CREATE TABLE jobs.applicationstages (
    applicationstageid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    jobapplyid uuid NOT NULL,
    stageid uuid NOT NULL,
    notes text,
    interviewers text,
    type character varying(50),
    updatedon timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedby uuid,
    interviewdate timestamp with time zone,
    interviewplace character varying(200)
);

ALTER TABLE ONLY jobs.applicationstages
    ADD CONSTRAINT applicationstages_pkey PRIMARY KEY (applicationstageid);

-- Table: jobs.jobapplications
CREATE TABLE jobs.jobapplications (
    jobapplyid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    jobid uuid NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid,
    candidateid uuid DEFAULT public.uuid_generate_v4(),
    candidatename character varying(200),
    candidateemail character varying(200),
    linkedinurl text,
    resumeurl text,
    phone character varying(50),
    coverletter text,
    currentstageid uuid,
    applicationsource character varying(100),
    status character varying(50) DEFAULT 'Applied'::character varying,
    createddate timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updateddate timestamp with time zone,
    isself boolean DEFAULT false,
    isexternal boolean DEFAULT false
);

ALTER TABLE ONLY jobs.jobapplications
    ADD CONSTRAINT jobapplications_pkey PRIMARY KEY (jobapplyid);

-- Table: jobs.jobopenings
CREATE TABLE jobs.jobopenings (
    jobid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    jobrolename character varying(200) NOT NULL,
    departmentid uuid,
    experiencemin integer,
    experiencemax integer,
    ctcmin numeric(12,2),
    ctcmax numeric(12,2),
    currency character varying(10),
    vacancies integer DEFAULT 1 NOT NULL,
    location character varying(200),
    workmode character varying(50),
    employmenttype character varying(50),
    jobintroduction text,
    responsibilities text,
    skillset text,
    lastdate date,
    posteddate timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    postedas character varying(20) NOT NULL,
    externallink text,
    status character varying(50) DEFAULT 'Open'::character varying,
    createdby uuid,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone,
    jobcode character varying(100)
);

ALTER TABLE ONLY jobs.jobopenings
    ADD CONSTRAINT jobopenings_pkey PRIMARY KEY (jobid);

-- Table: jobs.stagemaster
CREATE TABLE jobs.stagemaster (
    stageid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    stagename character varying(100) NOT NULL,
    stageorder integer,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    isinterviewstage boolean DEFAULT false
);

ALTER TABLE ONLY jobs.stagemaster
    ADD CONSTRAINT stagemaster_pkey PRIMARY KEY (stageid);


-- ============================================================
-- SCHEMA: NEWS
-- ============================================================

-- Table: news.news
CREATE TABLE news.news (
    newsid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    title character varying(255) NOT NULL,
    description text NOT NULL,
    createdby uuid NOT NULL,
    category character varying(50) DEFAULT 'Other'::character varying,
    imageurl character varying(255),
    publishedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    status character varying(20) DEFAULT 'Draft'::character varying,
    views integer DEFAULT 0,
    visibleto character varying(20) DEFAULT 'Everyone'::character varying,
    selectedemployees uuid[] DEFAULT '{}'::uuid[],
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY news.news
    ADD CONSTRAINT news_pkey PRIMARY KEY (newsid);

-- Table: news.news_reads_count
CREATE TABLE news.news_reads_count (
    newsid uuid NOT NULL,
    employeeid uuid NOT NULL,
    readat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY news.news_reads_count
    ADD CONSTRAINT pk_news_reads PRIMARY KEY (newsid, employeeid);


-- ============================================================
-- SCHEMA: NOTIFICATION
-- ============================================================

-- Table: notification.notifications
CREATE TABLE notification.notifications (
    notificationid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    receiveruserid uuid[] NOT NULL,
    senderuserid uuid,
    title character varying(50),
    notificationtype character varying(50),
    message character varying(2000) NOT NULL,
    redirecturl character varying(200),
    isread boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    createdby uuid,
    requestid uuid,
    moduletype character varying(50),
    requeststatus character varying(20) DEFAULT 'pending'::character varying
);

ALTER TABLE ONLY notification.notifications
    ADD CONSTRAINT notifications_pkey PRIMARY KEY (notificationid);


-- ============================================================
-- SCHEMA: PAYMENT
-- ============================================================

-- Table: payment.companysubscriptions
CREATE TABLE payment.companysubscriptions (
    subscriptionid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    companyid uuid,
    planid uuid,
    status text,
    billingcycle text,
    startdate timestamp without time zone,
    enddate timestamp without time zone
);

ALTER TABLE ONLY payment.companysubscriptions
    ADD CONSTRAINT companysubscriptions_pkey PRIMARY KEY (subscriptionid);

-- Table: payment.invoices
CREATE TABLE payment.invoices (
    invoice_id uuid DEFAULT gen_random_uuid() NOT NULL,
    transaction_id uuid NOT NULL,
    company_id uuid NOT NULL,
    invoice_number character varying(50) NOT NULL,
    amount numeric(12,2) NOT NULL,
    tax_amount numeric(12,2) DEFAULT 0.00 NOT NULL,
    discount_amount numeric(12,2) DEFAULT 0.00 NOT NULL,
    total_amount numeric(12,2) NOT NULL,
    currency character varying(10) DEFAULT 'USD'::character varying NOT NULL,
    billing_period_start timestamp with time zone NOT NULL,
    billing_period_end timestamp with time zone NOT NULL,
    plan_name character varying(100) NOT NULL,
    billing_cycle character varying(20) NOT NULL,
    company_name character varying(255) NOT NULL,
    company_email character varying(255),
    status character varying(20) DEFAULT 'draft'::character varying NOT NULL,
    issued_at timestamp with time zone DEFAULT now() NOT NULL,
    paid_at timestamp with time zone,
    due_date timestamp with time zone,
    notes text
);

ALTER TABLE ONLY payment.invoices
    ADD CONSTRAINT invoices_invoice_number_key UNIQUE (invoice_number);
ALTER TABLE ONLY payment.invoices
    ADD CONSTRAINT invoices_pkey PRIMARY KEY (invoice_id);
ALTER TABLE ONLY payment.invoices
    ADD CONSTRAINT fk_invoices_company FOREIGN KEY (company_id) REFERENCES auth.organizations(organizationid) ON DELETE RESTRICT;
ALTER TABLE ONLY payment.invoices
    ADD CONSTRAINT fk_invoices_transaction FOREIGN KEY (transaction_id) REFERENCES payment.transactions(transaction_id) ON DELETE RESTRICT;

-- Table: payment.refunds
CREATE TABLE payment.refunds (
    refund_id uuid DEFAULT gen_random_uuid() NOT NULL,
    transaction_id uuid NOT NULL,
    gateway_refund_id character varying(255),
    amount numeric(12,2) NOT NULL,
    currency character varying(10) DEFAULT 'USD'::character varying NOT NULL,
    reason text NOT NULL,
    status character varying(30) DEFAULT 'pending'::character varying NOT NULL,
    initiated_by character varying(20) DEFAULT 'admin'::character varying NOT NULL,
    admin_id uuid,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    processed_at timestamp with time zone
);

ALTER TABLE ONLY payment.refunds
    ADD CONSTRAINT refunds_pkey PRIMARY KEY (refund_id);
ALTER TABLE ONLY payment.refunds
    ADD CONSTRAINT fk_refunds_transaction FOREIGN KEY (transaction_id) REFERENCES payment.transactions(transaction_id) ON DELETE RESTRICT;

-- Table: payment.subscription_history
CREATE TABLE payment.subscription_history (
    history_id uuid DEFAULT gen_random_uuid() NOT NULL,
    subscription_id uuid NOT NULL,
    company_id uuid NOT NULL,
    action character varying(50) NOT NULL,
    old_plan_id uuid,
    new_plan_id uuid,
    old_end_date timestamp with time zone,
    new_end_date timestamp with time zone,
    performed_by uuid,
    notes text,
    created_at timestamp with time zone DEFAULT now()
);

ALTER TABLE ONLY payment.subscription_history
    ADD CONSTRAINT subscription_history_pkey PRIMARY KEY (history_id);

-- Table: payment.subscriptionplans
CREATE TABLE payment.subscriptionplans (
    planid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    name text NOT NULL,
    monthlyprice numeric,
    annualprice numeric,
    maxusers integer,
    features jsonb,
    iscustom boolean DEFAULT false,
    isactive boolean DEFAULT true
);

ALTER TABLE ONLY payment.subscriptionplans
    ADD CONSTRAINT subscriptionplans_pkey PRIMARY KEY (planid);

-- Table: payment.transactions
CREATE TABLE payment.transactions (
    transaction_id uuid DEFAULT gen_random_uuid() NOT NULL,
    company_id uuid NOT NULL,
    subscription_id uuid,
    plan_id uuid NOT NULL,
    gateway character varying(50) DEFAULT 'stripe'::character varying NOT NULL,
    gateway_transaction_id character varying(255),
    gateway_order_id character varying(255),
    gateway_customer_id character varying(255),
    amount numeric(12,2) NOT NULL,
    currency character varying(10) DEFAULT 'USD'::character varying NOT NULL,
    billing_cycle character varying(20) NOT NULL,
    status character varying(30) DEFAULT 'pending'::character varying NOT NULL,
    payment_method character varying(50),
    card_last4 character varying(4),
    card_brand character varying(30),
    failure_reason text,
    metadata jsonb,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);

ALTER TABLE ONLY payment.transactions
    ADD CONSTRAINT transactions_pkey PRIMARY KEY (transaction_id);
ALTER TABLE ONLY payment.transactions
    ADD CONSTRAINT fk_transactions_company FOREIGN KEY (company_id) REFERENCES auth.organizations(organizationid) ON DELETE RESTRICT;
ALTER TABLE ONLY payment.transactions
    ADD CONSTRAINT fk_transactions_plan FOREIGN KEY (plan_id) REFERENCES payment.subscriptionplans(planid) ON DELETE RESTRICT;
ALTER TABLE ONLY payment.transactions
    ADD CONSTRAINT fk_transactions_subscription FOREIGN KEY (subscription_id) REFERENCES payment.companysubscriptions(subscriptionid) ON DELETE SET NULL;


-- ============================================================
-- SCHEMA: PAYROLL
-- ============================================================

-- Table: payroll.attendancedeductionrules
CREATE TABLE payroll.attendancedeductionrules (
    ruleid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    rulename character varying(100),
    description text,
    fixeddeduction numeric(15,2),
    percentagededuction numeric(5,2),
    halfdaymultiplier numeric(3,2) DEFAULT 0.50,
    isactive boolean DEFAULT true,
    isdeleted boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY payroll.attendancedeductionrules
    ADD CONSTRAINT attendancedeductionrules_pkey PRIMARY KEY (ruleid);

-- Table: payroll.attendancesummary
CREATE TABLE payroll.attendancesummary (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    employeeid uuid NOT NULL,
    periodid uuid NOT NULL,
    ruleid uuid,
    shiftid uuid,
    scheduleddays integer DEFAULT 0,
    totaldays integer DEFAULT 0,
    presentdays integer DEFAULT 0,
    absentdays integer DEFAULT 0,
    halfdays integer DEFAULT 0,
    effectiveabsents numeric(6,2) DEFAULT 0,
    absentdeduction numeric(15,2) DEFAULT 0,
    halfdaydeduction numeric(15,2) DEFAULT 0,
    totaldeduction numeric(15,2) DEFAULT 0,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY payroll.attendancesummary
    ADD CONSTRAINT attendancesummary_pkey PRIMARY KEY (id);
ALTER TABLE ONLY payroll.attendancesummary
    ADD CONSTRAINT uqattendance UNIQUE (periodid, employeeid);
ALTER TABLE ONLY payroll.attendancesummary
    ADD CONSTRAINT attendancesummary_ruleid_fkey FOREIGN KEY (ruleid) REFERENCES payroll.attendancedeductionrules(ruleid);

-- Table: payroll.bonusentries
CREATE TABLE payroll.bonusentries (
    bonusid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    periodid uuid NOT NULL,
    ruleid uuid,
    bonousamount numeric(15,2) NOT NULL,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY payroll.bonusentries
    ADD CONSTRAINT bonusentries_pkey PRIMARY KEY (bonusid);
ALTER TABLE ONLY payroll.bonusentries
    ADD CONSTRAINT uqbonusentry UNIQUE (employeeid, periodid, ruleid);
ALTER TABLE ONLY payroll.bonusentries
    ADD CONSTRAINT bonusentries_ruleid_fkey FOREIGN KEY (ruleid) REFERENCES payroll.bonusrules(ruleid);

-- Table: payroll.bonusrules
CREATE TABLE payroll.bonusrules (
    ruleid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    rulename character varying(100) NOT NULL,
    description text,
    bonustype character varying(50) NOT NULL,
    bonusamount numeric(15,2),
    bonuspercentage numeric(5,2),
    isactive boolean DEFAULT true,
    isdeleted boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT bonusrules_bonustype_check CHECK (((bonustype)::text = ANY ((ARRAY['fixed'::character varying, 'percentage'::character varying])::text[])))
);

ALTER TABLE ONLY payroll.bonusrules
    ADD CONSTRAINT bonusrules_pkey PRIMARY KEY (ruleid);

-- Table: payroll.gratuityconfig
CREATE TABLE payroll.gratuityconfig (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    configrulename character varying(100) NOT NULL,
    description text,
    yearsrequired integer DEFAULT 5 NOT NULL,
    calculationtype character varying(50) NOT NULL,
    calculationvalue numeric(10,2) NOT NULL,
    isactive boolean DEFAULT true,
    isdeleted boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT gratuityconfig_calculationtype_check CHECK (((calculationtype)::text = ANY (ARRAY[('fixed'::character varying)::text, ('peryear'::character varying)::text, ('percentage'::character varying)::text])))
);

ALTER TABLE ONLY payroll.gratuityconfig
    ADD CONSTRAINT gratuityconfig_pkey PRIMARY KEY (id);
ALTER TABLE ONLY payroll.gratuityconfig
    ADD CONSTRAINT uqgratuityconfig UNIQUE (organizationid, configrulename);

-- Table: payroll.gratuitytransactions
CREATE TABLE payroll.gratuitytransactions (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    periodid uuid,
    configid uuid,
    yearsworked numeric(4,2) NOT NULL,
    lastsalary numeric(15,2),
    totalamount numeric(15,2) NOT NULL,
    paymentstatus character varying(20) DEFAULT 'calculated'::character varying,
    paidat timestamp with time zone,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    paidby uuid,
    cancellationreason text,
    createdby uuid,
    paymentmethod character varying(50) DEFAULT 'payroll_credit'::character varying,
    isdeleted boolean DEFAULT false,
    approvedby uuid,
    approvedat timestamp with time zone,
    notes text,
    CONSTRAINT ck_gratuitytransactions_paymentmethod CHECK (((paymentmethod)::text = ANY (ARRAY[('payroll_credit'::character varying)::text, ('bank_transfer'::character varying)::text, ('cash'::character varying)::text]))),
    CONSTRAINT ck_gratuitytransactions_paymentstatus CHECK (((paymentstatus)::text = ANY (ARRAY[('calculated'::character varying)::text, ('approved'::character varying)::text, ('paid'::character varying)::text, ('cancelled'::character varying)::text]))),
    CONSTRAINT gratuitytransactions_paymentstatus_check CHECK (((paymentstatus)::text = ANY (ARRAY[('calculated'::character varying)::text, ('approved'::character varying)::text, ('paid'::character varying)::text, ('cancelled'::character varying)::text])))
);

ALTER TABLE ONLY payroll.gratuitytransactions
    ADD CONSTRAINT gratuitytransactions_pkey PRIMARY KEY (id);
ALTER TABLE ONLY payroll.gratuitytransactions
    ADD CONSTRAINT gratuitytransactions_configid_fkey FOREIGN KEY (configid) REFERENCES payroll.gratuityconfig(id);

-- Table: payroll.lateattendance
CREATE TABLE payroll.lateattendance (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    employeeid uuid NOT NULL,
    periodid uuid NOT NULL,
    ruleid uuid,
    lateminutes integer DEFAULT 0,
    deduction numeric(15,2) DEFAULT 0,
    isgrace boolean DEFAULT false,
    attendancedate date NOT NULL,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY payroll.lateattendance
    ADD CONSTRAINT lateattendance_pkey PRIMARY KEY (id);
ALTER TABLE ONLY payroll.lateattendance
    ADD CONSTRAINT uqlateattendance UNIQUE (employeeid, attendancedate);
ALTER TABLE ONLY payroll.lateattendance
    ADD CONSTRAINT lateattendance_ruleid_fkey FOREIGN KEY (ruleid) REFERENCES payroll.lateattendancerules(ruleid);

-- Table: payroll.lateattendancerules
CREATE TABLE payroll.lateattendancerules (
    ruleid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    rulename character varying(100),
    description text,
    fixeddeduction numeric(15,2),
    percentagededuction numeric(5,2),
    graceminutes integer DEFAULT 0,
    isactive boolean DEFAULT true,
    isdeleted boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY payroll.lateattendancerules
    ADD CONSTRAINT lateattendancerules_pkey PRIMARY KEY (ruleid);
ALTER TABLE ONLY payroll.lateattendancerules
    ADD CONSTRAINT uqorglaterule UNIQUE (organizationid);

-- Table: payroll.leaverules
CREATE TABLE payroll.leaverules (
    ruleid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    rulename character varying(100),
    description text,
    unpaidmultiplier numeric(3,2) DEFAULT 1.00,
    halfpaidmultiplier numeric(3,2) DEFAULT 0.50,
    isactive boolean DEFAULT true,
    isdeleted boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY payroll.leaverules
    ADD CONSTRAINT leaverules_pkey PRIMARY KEY (ruleid);
ALTER TABLE ONLY payroll.leaverules
    ADD CONSTRAINT uqorgleaverule UNIQUE (organizationid);

-- Table: payroll.loanpayments
CREATE TABLE payroll.loanpayments (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    loanid uuid,
    employeeid uuid NOT NULL,
    periodid uuid,
    installmentnumber integer DEFAULT 0 NOT NULL,
    installmentamount numeric(15,2) NOT NULL,
    remainingamount numeric(15,2) NOT NULL,
    repaymenttype character varying(20) DEFAULT 'installment'::character varying,
    paymentdate date DEFAULT CURRENT_DATE NOT NULL,
    paymentmethod character varying(50) DEFAULT 'payroll_deduction'::character varying,
    paymentstatus character varying(20) DEFAULT 'deducted'::character varying,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY payroll.loanpayments
    ADD CONSTRAINT loanpayments_pkey PRIMARY KEY (id);
ALTER TABLE ONLY payroll.loanpayments
    ADD CONSTRAINT loanpayments_loanid_fkey FOREIGN KEY (loanid) REFERENCES payroll.loans(loanid);

-- Table: payroll.loanrules
CREATE TABLE payroll.loanrules (
    ruleid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    rulename character varying(100) NOT NULL,
    description text,
    organizationid uuid NOT NULL,
    maxloanamount numeric(15,2),
    maxinstallments integer,
    interestrate numeric(5,2) DEFAULT 0,
    isactive boolean DEFAULT true,
    isdeleted boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY payroll.loanrules
    ADD CONSTRAINT loanrules_pkey PRIMARY KEY (ruleid);
ALTER TABLE ONLY payroll.loanrules
    ADD CONSTRAINT uqorgloanrule UNIQUE (organizationid, rulename);

-- Table: payroll.loans
CREATE TABLE payroll.loans (
    loanid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    ruleid uuid,
    principalamount numeric(15,2) NOT NULL,
    totalinterest numeric(15,2) DEFAULT 0,
    totalamount numeric(15,2) GENERATED ALWAYS AS ((principalamount + totalinterest)) STORED,
    monthlyinstallment numeric(15,2),
    remainingamount numeric(15,2) NOT NULL,
    paidamount numeric(15,2) GENERATED ALWAYS AS (((principalamount + totalinterest) - remainingamount)) STORED,
    totalinstallments integer DEFAULT 0 NOT NULL,
    paidinstallments integer DEFAULT 0 NOT NULL,
    repaymenttype character varying(20) DEFAULT 'installment'::character varying,
    requeststatus character varying(20) DEFAULT 'pending'::character varying,
    loanstatus character varying(20) DEFAULT 'active'::character varying,
    approvedby uuid,
    approvedat timestamp with time zone,
    rejectionreason text,
    disbursementstatus character varying(20) DEFAULT 'pending'::character varying,
    disbursementdate date,
    startdate date,
    enddate date,
    description text,
    createdby uuid,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    referenceid character varying(20) DEFAULT ('LN-'::text || upper("substring"(replace((public.uuid_generate_v4())::text, '-'::text, ''::text), 1, 10))) NOT NULL
);

ALTER TABLE ONLY payroll.loans
    ADD CONSTRAINT loans_pkey PRIMARY KEY (loanid);
ALTER TABLE ONLY payroll.loans
    ADD CONSTRAINT loans_ruleid_fkey FOREIGN KEY (ruleid) REFERENCES payroll.loanrules(ruleid);

-- Table: payroll.overtimeentries
CREATE TABLE payroll.overtimeentries (
    overtimeid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    employeeid uuid NOT NULL,
    periodid uuid NOT NULL,
    ruleid uuid,
    hoursworked numeric(5,2) NOT NULL,
    finalamount numeric(15,2),
    entrystatus character varying(20) DEFAULT 'pending'::character varying,
    approvedby uuid,
    overtimedate date NOT NULL,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT overtimeentries_entrystatus_check CHECK (((entrystatus)::text = ANY ((ARRAY['pending'::character varying, 'approved'::character varying, 'rejected'::character varying])::text[])))
);

ALTER TABLE ONLY payroll.overtimeentries
    ADD CONSTRAINT overtimeentries_pkey PRIMARY KEY (overtimeid);
ALTER TABLE ONLY payroll.overtimeentries
    ADD CONSTRAINT uqovertime UNIQUE (employeeid, periodid, overtimedate);
ALTER TABLE ONLY payroll.overtimeentries
    ADD CONSTRAINT overtimeentries_ruleid_fkey FOREIGN KEY (ruleid) REFERENCES payroll.overtimerules(ruleid);

-- Table: payroll.overtimerules
CREATE TABLE payroll.overtimerules (
    ruleid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    rulename character varying(100),
    description text,
    overtimetype character varying(20),
    fixedamount numeric(15,2),
    percentage numeric(5,2),
    isactive boolean DEFAULT true,
    isdeleted boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT overtimerules_overtimetype_check CHECK (((overtimetype)::text = ANY ((ARRAY['regular'::character varying, 'holiday'::character varying, 'weekend'::character varying])::text[])))
);

ALTER TABLE ONLY payroll.overtimerules
    ADD CONSTRAINT overtimerules_pkey PRIMARY KEY (ruleid);
ALTER TABLE ONLY payroll.overtimerules
    ADD CONSTRAINT uqruletype UNIQUE (organizationid, overtimetype);

-- Table: payroll.payrolldata
CREATE TABLE payroll.payrolldata (
    dataid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    periodid uuid NOT NULL,
    entryid uuid,
    employeesnapshot jsonb DEFAULT '{}'::jsonb NOT NULL,
    basicsalary numeric(15,2) DEFAULT 0,
    earningsdetail jsonb DEFAULT '[]'::jsonb NOT NULL,
    totalearnings numeric(15,2) DEFAULT 0,
    overtimeamount numeric(15,2) DEFAULT 0,
    bonusamount numeric(15,2) DEFAULT 0,
    grosssalary numeric(15,2) DEFAULT 0,
    scheduleddays integer DEFAULT 0,
    presentdays integer DEFAULT 0,
    absentdays numeric(6,2) DEFAULT 0,
    halfdays integer DEFAULT 0,
    latedays integer DEFAULT 0,
    overtimehours numeric(6,2) DEFAULT 0,
    absentdeduction numeric(15,2) DEFAULT 0,
    latededuction numeric(15,2) DEFAULT 0,
    loandeduction numeric(15,2) DEFAULT 0,
    advancededuction numeric(15,2) DEFAULT 0,
    taxamount numeric(15,2) DEFAULT 0,
    pfemployee numeric(15,2) DEFAULT 0,
    socialsecurity numeric(15,2) DEFAULT 0,
    otherdeductions numeric(15,2) DEFAULT 0,
    deductionsdetail jsonb DEFAULT '[]'::jsonb NOT NULL,
    totaldeductions numeric(15,2) DEFAULT 0,
    pfemployer numeric(15,2) DEFAULT 0,
    ssemployer numeric(15,2) DEFAULT 0,
    ytdgross numeric(15,2) DEFAULT 0,
    ytdtax numeric(15,2) DEFAULT 0,
    ytdnet numeric(15,2) DEFAULT 0,
    netsalary numeric(15,2) DEFAULT 0,
    currency character varying(3) DEFAULT 'pkr'::character varying,
    status character varying(20) DEFAULT 'draft'::character varying,
    generatedby uuid,
    generatedat timestamp with time zone,
    emailsentat timestamp with time zone,
    emailsentto character varying(255),
    payslippdfurl text,
    pdfgeneratedat timestamp with time zone,
    isreissued boolean DEFAULT false,
    reissuedby uuid,
    reissuedat timestamp with time zone,
    reissuenote text,
    versionnumber integer DEFAULT 1,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    gratuityamount numeric(15,2) DEFAULT 0,
    CONSTRAINT payrolldata_status_check CHECK (((status)::text = ANY ((ARRAY['draft'::character varying, 'generated'::character varying, 'sent'::character varying, 'viewed'::character varying])::text[])))
);

ALTER TABLE ONLY payroll.payrolldata
    ADD CONSTRAINT payrolldata_pkey PRIMARY KEY (dataid);
ALTER TABLE ONLY payroll.payrolldata
    ADD CONSTRAINT uqpayslips UNIQUE (employeeid, periodid, versionnumber);

-- Table: payroll.payrollleavesummary
CREATE TABLE payroll.payrollleavesummary (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    employeeid uuid NOT NULL,
    periodid uuid NOT NULL,
    ruleid uuid,
    paiddays numeric(5,2) DEFAULT 0,
    unpaiddays numeric(5,2) DEFAULT 0,
    halfpaiddays numeric(5,2) DEFAULT 0,
    unpaiddeduction numeric(15,2) DEFAULT 0,
    halfpaiddeduction numeric(15,2) DEFAULT 0,
    totalleavedays numeric(5,2) GENERATED ALWAYS AS (((paiddays + unpaiddays) + halfpaiddays)) STORED,
    totaldeduction numeric(15,2),
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY payroll.payrollleavesummary
    ADD CONSTRAINT payrollleavesummary_pkey PRIMARY KEY (id);
ALTER TABLE ONLY payroll.payrollleavesummary
    ADD CONSTRAINT uqleavesummary UNIQUE (periodid, employeeid);
ALTER TABLE ONLY payroll.payrollleavesummary
    ADD CONSTRAINT payrollleavesummary_ruleid_fkey FOREIGN KEY (ruleid) REFERENCES payroll.leaverules(ruleid);

-- Table: payroll.payrollperiods
CREATE TABLE payroll.payrollperiods (
    periodid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    periodname character varying(100) NOT NULL,
    startdate date NOT NULL,
    enddate date NOT NULL,
    paymentdate date,
    status character varying(20) DEFAULT 'open'::character varying NOT NULL,
    description text,
    isactive boolean DEFAULT true,
    isdeleted boolean DEFAULT false,
    createdby uuid,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY payroll.payrollperiods
    ADD CONSTRAINT payrollperiods_pkey PRIMARY KEY (periodid);

-- Table: payroll.payslipemaillog
CREATE TABLE payroll.payslipemaillog (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    payslipid uuid NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    sentat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    sentto character varying(255) NOT NULL,
    sentby uuid,
    status character varying(20) DEFAULT 'sent'::character varying,
    errormessage text,
    CONSTRAINT payslipemaillog_status_check CHECK (((status)::text = ANY ((ARRAY['sent'::character varying, 'failed'::character varying, 'bounced'::character varying])::text[])))
);

ALTER TABLE ONLY payroll.payslipemaillog
    ADD CONSTRAINT payslipemaillog_pkey PRIMARY KEY (id);
ALTER TABLE ONLY payroll.payslipemaillog
    ADD CONSTRAINT payslipemaillog_payslipid_fkey FOREIGN KEY (payslipid) REFERENCES payroll.payrolldata(dataid);

-- Table: payroll.performancepay
CREATE TABLE payroll.performancepay (
    performancepayid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    employeeid uuid NOT NULL,
    periodid uuid NOT NULL,
    ruleid uuid,
    score numeric(5,2),
    finalamount numeric(15,2) NOT NULL,
    calculatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY payroll.performancepay
    ADD CONSTRAINT performancepay_pkey PRIMARY KEY (performancepayid);
ALTER TABLE ONLY payroll.performancepay
    ADD CONSTRAINT uqperformancepay UNIQUE (employeeid, periodid);
ALTER TABLE ONLY payroll.performancepay
    ADD CONSTRAINT performancepay_ruleid_fkey FOREIGN KEY (ruleid) REFERENCES payroll.performancerules(ruleid);

-- Table: payroll.performancerules
CREATE TABLE payroll.performancerules (
    ruleid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    rulename character varying(20),
    description text,
    minscore numeric(3,2),
    maxscore numeric(3,2),
    fixedamount numeric(15,2),
    percentage numeric(5,2),
    isactive boolean DEFAULT true,
    isdeleted boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY payroll.performancerules
    ADD CONSTRAINT performancerules_pkey PRIMARY KEY (ruleid);
ALTER TABLE ONLY payroll.performancerules
    ADD CONSTRAINT uqperformancebracket UNIQUE (organizationid, rulename);

-- Table: payroll.pfrequests
CREATE TABLE payroll.pfrequests (
    requestid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    ruleid uuid NOT NULL,
    pfid uuid,
    requesttype character varying(20) NOT NULL,
    status character varying(20) DEFAULT 'pending'::character varying,
    requestedemployeepct numeric(5,2),
    requestedemployerpct numeric(5,2),
    withdrawaltype character varying(20),
    reason character varying(100),
    requestedamount numeric(15,2),
    remarks text,
    rejectionreason text,
    approvedby uuid,
    approvedat timestamp with time zone,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pfrequests_requesttype_check CHECK (((requesttype)::text = ANY ((ARRAY['enrollment'::character varying, 'update'::character varying, 'withdrawal'::character varying])::text[]))),
    CONSTRAINT pfrequests_status_check CHECK (((status)::text = ANY ((ARRAY['pending'::character varying, 'approved'::character varying, 'rejected'::character varying])::text[]))),
    CONSTRAINT pfrequests_withdrawaltype_check CHECK (((withdrawaltype)::text = ANY ((ARRAY['temporary'::character varying, 'permanent'::character varying])::text[])))
);

ALTER TABLE ONLY payroll.pfrequests
    ADD CONSTRAINT pfrequests_pkey PRIMARY KEY (requestid);
ALTER TABLE ONLY payroll.pfrequests
    ADD CONSTRAINT pfrequests_pfid_fkey FOREIGN KEY (pfid) REFERENCES payroll.providentfund(pfid);
ALTER TABLE ONLY payroll.pfrequests
    ADD CONSTRAINT pfrequests_ruleid_fkey FOREIGN KEY (ruleid) REFERENCES payroll.pfrules(ruleid);

-- Table: payroll.pfrules
CREATE TABLE payroll.pfrules (
    ruleid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    rulename character varying(100) NOT NULL,
    description text,
    defaultemployeepct numeric(5,2) NOT NULL,
    defaultemployerpct numeric(5,2) NOT NULL,
    contributionbasis character varying(20) DEFAULT 'basic'::character varying,
    allowpartialwithdraw boolean DEFAULT false,
    vestingmonths integer DEFAULT 0,
    withdrawal_config jsonb,
    isactive boolean DEFAULT true,
    isdeleted boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pfrules_contributionbasis_check CHECK (((contributionbasis)::text = ANY ((ARRAY['basic'::character varying, 'gross'::character varying, 'fixed'::character varying])::text[])))
);

ALTER TABLE ONLY payroll.pfrules
    ADD CONSTRAINT pfrules_pkey PRIMARY KEY (ruleid);

-- Table: payroll.pftransactions
CREATE TABLE payroll.pftransactions (
    transactionid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    periodid uuid NOT NULL,
    pfid uuid NOT NULL,
    pfrequestid uuid,
    salarybasisamount numeric(15,2) NOT NULL,
    employeepct numeric(5,2) NOT NULL,
    employerpct numeric(5,2) NOT NULL,
    employeeamount numeric(15,2) NOT NULL,
    employeramount numeric(15,2) NOT NULL,
    totalamount numeric(15,2) GENERATED ALWAYS AS ((employeeamount + employeramount)) STORED,
    runningbalance numeric(15,2) NOT NULL,
    transactiontype character varying(20) DEFAULT 'monthly'::character varying,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pftransactions_transactiontype_check CHECK (((transactiontype)::text = ANY ((ARRAY['monthly'::character varying, 'profit'::character varying, 'withdrawal'::character varying, 'adjustment'::character varying, 'settlement'::character varying])::text[])))
);

ALTER TABLE ONLY payroll.pftransactions
    ADD CONSTRAINT pftransactions_pkey PRIMARY KEY (transactionid);
ALTER TABLE ONLY payroll.pftransactions
    ADD CONSTRAINT pftransactions_pfid_fkey FOREIGN KEY (pfid) REFERENCES payroll.providentfund(pfid);
ALTER TABLE ONLY payroll.pftransactions
    ADD CONSTRAINT pftransactions_pfrequestid_fkey FOREIGN KEY (pfrequestid) REFERENCES payroll.pfrequests(requestid);

-- Table: payroll.providentfund
CREATE TABLE payroll.providentfund (
    pfid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    ruleid uuid NOT NULL,
    employeepct numeric(5,2) NOT NULL,
    employerpct numeric(5,2) NOT NULL,
    pfstatus character varying(20) DEFAULT 'pending'::character varying,
    effectivefrom date NOT NULL,
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT providentfund_pfstatus_check CHECK (((pfstatus)::text = ANY ((ARRAY['active'::character varying, 'closed'::character varying, 'pending'::character varying])::text[])))
);

ALTER TABLE ONLY payroll.providentfund
    ADD CONSTRAINT providentfund_pkey PRIMARY KEY (pfid);
ALTER TABLE ONLY payroll.providentfund
    ADD CONSTRAINT uq_pf_employee UNIQUE (organizationid, employeeid);
ALTER TABLE ONLY payroll.providentfund
    ADD CONSTRAINT providentfund_ruleid_fkey FOREIGN KEY (ruleid) REFERENCES payroll.pfrules(ruleid);

-- Table: payroll.salaryadvance
CREATE TABLE payroll.salaryadvance (
    advanceid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    ruleid uuid,
    amount numeric(15,2) NOT NULL,
    reason text,
    advancestatus character varying(20) DEFAULT 'pending'::character varying,
    approvedby uuid,
    approvedat timestamp with time zone,
    rejectedby uuid,
    rejectedat timestamp with time zone,
    rejectionreason text,
    deductedperiodid uuid,
    deductedat timestamp with time zone,
    createdby uuid,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    disbursedby uuid,
    disbursedat timestamp with time zone,
    disbursementnote text,
    CONSTRAINT salaryadvance_advancestatus_check CHECK (((advancestatus)::text = ANY ((ARRAY['pending'::character varying, 'approved'::character varying, 'disbursed'::character varying, 'rejected'::character varying, 'deducted'::character varying, 'cancelled'::character varying])::text[])))
);

ALTER TABLE ONLY payroll.salaryadvance
    ADD CONSTRAINT salaryadvance_pkey PRIMARY KEY (advanceid);
ALTER TABLE ONLY payroll.salaryadvance
    ADD CONSTRAINT salaryadvance_ruleid_fkey FOREIGN KEY (ruleid) REFERENCES payroll.salaryadvancerules(ruleid);

-- Table: payroll.salaryadvancerules
CREATE TABLE payroll.salaryadvancerules (
    ruleid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    rulename character varying(100) NOT NULL,
    description text,
    organizationid uuid NOT NULL,
    maxpercentage numeric(5,2),
    isactive boolean DEFAULT true,
    isdeleted boolean DEFAULT false,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY payroll.salaryadvancerules
    ADD CONSTRAINT salaryadvancerules_pkey PRIMARY KEY (ruleid);

-- Table: payroll.socialsecurityconfig
CREATE TABLE payroll.socialsecurityconfig (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    configrulename character varying(100) NOT NULL,
    description text,
    employeepercentage numeric(5,2) DEFAULT 0 NOT NULL,
    employerpercentage numeric(5,2) DEFAULT 0 NOT NULL,
    maxsalarylimit numeric(15,2),
    isactive boolean DEFAULT true,
    isdeleted boolean DEFAULT false,
    effectivefrom date,
    effectiveto date,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY payroll.socialsecurityconfig
    ADD CONSTRAINT socialsecurityconfig_pkey PRIMARY KEY (id);
ALTER TABLE ONLY payroll.socialsecurityconfig
    ADD CONSTRAINT uqssconfig UNIQUE (organizationid, configrulename);

-- Table: payroll.socialsecuritytransactions
CREATE TABLE payroll.socialsecuritytransactions (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    periodid uuid NOT NULL,
    configid uuid,
    salarycapped numeric(15,2),
    employeeamount numeric(15,2) DEFAULT 0,
    employeramount numeric(15,2) DEFAULT 0,
    totalamount numeric(15,2) GENERATED ALWAYS AS ((employeeamount + employeramount)) STORED,
    requeststatus character varying(20) DEFAULT 'pending'::character varying,
    isenrolled boolean DEFAULT false,
    withdrawalrequest boolean DEFAULT false,
    withdrawalstatus character varying(20),
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY payroll.socialsecuritytransactions
    ADD CONSTRAINT socialsecuritytransactions_pkey PRIMARY KEY (id);
ALTER TABLE ONLY payroll.socialsecuritytransactions
    ADD CONSTRAINT uqsstransaction UNIQUE (employeeid, periodid);
ALTER TABLE ONLY payroll.socialsecuritytransactions
    ADD CONSTRAINT socialsecuritytransactions_configid_fkey FOREIGN KEY (configid) REFERENCES payroll.socialsecurityconfig(id);

-- Table: payroll.taxcategories
CREATE TABLE payroll.taxcategories (
    categoryid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    regimeid uuid NOT NULL,
    categoryname character varying(100),
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    isdeleted boolean DEFAULT false
);

ALTER TABLE ONLY payroll.taxcategories
    ADD CONSTRAINT taxcategories_pkey PRIMARY KEY (categoryid);

-- Table: payroll.taxcomponentrules
CREATE TABLE payroll.taxcomponentrules (
    componentruleid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    regimeid uuid NOT NULL,
    organizationid uuid NOT NULL,
    componentname character varying(50),
    taxability character varying(20),
    limitamount numeric(15,2) DEFAULT 0,
    limitpercentage numeric(5,2) DEFAULT 0,
    applystage character varying(20),
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    isdeleted boolean DEFAULT false
);

ALTER TABLE ONLY payroll.taxcomponentrules
    ADD CONSTRAINT taxcomponentrules_pkey PRIMARY KEY (componentruleid);

-- Table: payroll.taxregimes
CREATE TABLE payroll.taxregimes (
    regimeid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    country character varying(50),
    regimename character varying(100),
    startdate date NOT NULL,
    enddate date NOT NULL,
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    isdeleted boolean DEFAULT false
);

ALTER TABLE ONLY payroll.taxregimes
    ADD CONSTRAINT taxregimes_pkey PRIMARY KEY (regimeid);

-- Table: payroll.taxslabs
CREATE TABLE payroll.taxslabs (
    slabid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    categoryid uuid NOT NULL,
    slaborder integer NOT NULL,
    minincome numeric(15,2) NOT NULL,
    maxincome numeric(15,2),
    fixedamount numeric(15,2) DEFAULT 0,
    percentage numeric(5,2) DEFAULT 0,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    isdeleted boolean DEFAULT false
);

ALTER TABLE ONLY payroll.taxslabs
    ADD CONSTRAINT taxslabs_pkey PRIMARY KEY (slabid);

-- Table: payroll.taxtransactions
CREATE TABLE payroll.taxtransactions (
    id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    employeeid uuid NOT NULL,
    periodid uuid NOT NULL,
    categoryid uuid,
    grossincome numeric(15,2),
    taxableincome numeric(15,2),
    taxamount numeric(15,2),
    monthlytax numeric(15,2),
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY payroll.taxtransactions
    ADD CONSTRAINT taxtransactions_pkey PRIMARY KEY (id);


-- ============================================================
-- SCHEMA: PERFORMANCE
-- ============================================================

-- Table: performance.appraisal_cycle
CREATE TABLE performance.appraisal_cycle (
    cycleid uuid DEFAULT gen_random_uuid() NOT NULL,
    cyclename character varying,
    cycletype character varying,
    startdate timestamp with time zone,
    enddate timestamp with time zone,
    reviewstartdate timestamp with time zone,
    reviewenddate timestamp with time zone,
    status character varying,
    isselfassessmentenable boolean,
    managerreview boolean,
    isappraisalenable boolean,
    createdby uuid,
    createdat timestamp with time zone,
    updatedat timestamp with time zone,
    organizationid uuid,
    description text
);

ALTER TABLE ONLY performance.appraisal_cycle
    ADD CONSTRAINT appraisal_cycle_cyclename_key UNIQUE (cyclename);
ALTER TABLE ONLY performance.appraisal_cycle
    ADD CONSTRAINT appraisal_cycle_pkey PRIMARY KEY (cycleid);

-- Table: performance.employeeappraisals
CREATE TABLE performance.employeeappraisals (
    appraisalid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    cycleid uuid,
    employeeid uuid,
    reviewerid uuid,
    reviewtype character varying(20) DEFAULT 'manager'::character varying,
    overallrating numeric(3,2),
    kraratings jsonb DEFAULT '{}'::jsonb,
    skillratings jsonb DEFAULT '{}'::jsonb,
    goalsachieved jsonb DEFAULT '{}'::jsonb,
    feedback text,
    improvementareas text,
    developmentplan text,
    status character varying(20) DEFAULT 'draft'::character varying,
    submittedat timestamp with time zone,
    reviewedat timestamp with time zone,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY performance.employeeappraisals
    ADD CONSTRAINT employeeappraisals_pkey PRIMARY KEY (appraisalid);
ALTER TABLE ONLY performance.employeeappraisals
    ADD CONSTRAINT uq_employeeappraisals UNIQUE (cycleid, employeeid, reviewerid, reviewtype);

-- Table: performance.employeeskills
CREATE TABLE performance.employeeskills (
    employeeskillid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    employeeid uuid,
    skillid uuid,
    proficiencylevel integer DEFAULT 1,
    assessedby uuid,
    lastassessed date,
    notes text,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY performance.employeeskills
    ADD CONSTRAINT employeeskills_pkey PRIMARY KEY (employeeskillid);
ALTER TABLE ONLY performance.employeeskills
    ADD CONSTRAINT uq_employeeskills UNIQUE (employeeid, skillid);

-- Table: performance.goals
CREATE TABLE performance.goals (
    goalid uuid DEFAULT gen_random_uuid() NOT NULL,
    title character varying,
    description character varying,
    isactive boolean DEFAULT true,
    createdby uuid,
    createdat timestamp with time zone DEFAULT now(),
    updatedat timestamp with time zone,
    progress character varying,
    startdate timestamp with time zone,
    enddate timestamp with time zone,
    kraid uuid,
    organizationid uuid,
    assignedto uuid,
    assignedby uuid,
    CONSTRAINT chk_goal_dates CHECK (((enddate IS NULL) OR (startdate IS NULL) OR (enddate >= startdate)))
);

ALTER TABLE ONLY performance.goals
    ADD CONSTRAINT goals_pkey PRIMARY KEY (goalid);
ALTER TABLE ONLY performance.goals
    ADD CONSTRAINT fk_goals_kra FOREIGN KEY (kraid) REFERENCES performance.kra(kraid) ON DELETE CASCADE;

-- Table: performance.hrappraisals
CREATE TABLE performance.hrappraisals (
    hrreviewid uuid DEFAULT gen_random_uuid() NOT NULL,
    organizationid uuid NOT NULL,
    cycleid uuid NOT NULL,
    hrid uuid NOT NULL,
    employeeid uuid NOT NULL,
    finalrating numeric(3,2),
    hrcomments text,
    improvementarea text,
    feedback text,
    status character varying(20) DEFAULT 'PENDING'::character varying,
    createdat timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY performance.hrappraisals
    ADD CONSTRAINT hrreviews_pkey PRIMARY KEY (hrreviewid);

-- Table: performance.kra
CREATE TABLE performance.kra (
    kraid uuid DEFAULT gen_random_uuid() NOT NULL,
    kraname character varying,
    kradescription character varying,
    isactive boolean DEFAULT true,
    createdby uuid,
    createdat timestamp with time zone DEFAULT now(),
    updatedat timestamp with time zone,
    krarate character varying DEFAULT '100%'::character varying,
    cycleid uuid,
    organizationid uuid,
    noofgoals integer DEFAULT 0
);

ALTER TABLE ONLY performance.kra
    ADD CONSTRAINT kra_kraname_key UNIQUE (kraname);
ALTER TABLE ONLY performance.kra
    ADD CONSTRAINT kra_pkey PRIMARY KEY (kraid);
ALTER TABLE ONLY performance.kra
    ADD CONSTRAINT fk_kra_cycle FOREIGN KEY (cycleid) REFERENCES performance.appraisal_cycle(cycleid);

-- Table: performance.managerreviews
CREATE TABLE performance.managerreviews (
    managerreviewid uuid DEFAULT gen_random_uuid() NOT NULL,
    organizationid uuid NOT NULL,
    cycleid uuid NOT NULL,
    kraid uuid NOT NULL,
    goalid uuid NOT NULL,
    managerid uuid NOT NULL,
    employeeid uuid NOT NULL,
    improvementarea text,
    rating numeric(3,2) NOT NULL,
    feedback text,
    status character varying(20) DEFAULT 'SUBMITTED'::character varying,
    createdat timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY performance.managerreviews
    ADD CONSTRAINT managerreviews_pkey PRIMARY KEY (managerreviewid);
ALTER TABLE ONLY performance.managerreviews
    ADD CONSTRAINT fk_mgr_goal FOREIGN KEY (goalid) REFERENCES performance.goals(goalid) ON DELETE CASCADE;
ALTER TABLE ONLY performance.managerreviews
    ADD CONSTRAINT fk_mgr_kra FOREIGN KEY (kraid) REFERENCES performance.kra(kraid) ON DELETE CASCADE;

-- Table: performance.selfassessments
CREATE TABLE performance.selfassessments (
    selfassessmentid uuid DEFAULT gen_random_uuid() NOT NULL,
    goalid uuid NOT NULL,
    kraid uuid NOT NULL,
    organizationid uuid NOT NULL,
    selfrating numeric(3,2) NOT NULL,
    selfcomment text,
    status character varying(20) DEFAULT 'SUBMITTED'::character varying,
    createdat timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    employeeid uuid,
    CONSTRAINT selfassessments_selfrating_check CHECK (((selfrating >= (1)::numeric) AND (selfrating <= (5)::numeric)))
);

ALTER TABLE ONLY performance.selfassessments
    ADD CONSTRAINT selfassessments_pkey PRIMARY KEY (selfassessmentid);
ALTER TABLE ONLY performance.selfassessments
    ADD CONSTRAINT fk_self_goal FOREIGN KEY (goalid) REFERENCES performance.goals(goalid) ON DELETE CASCADE;
ALTER TABLE ONLY performance.selfassessments
    ADD CONSTRAINT fk_self_kra FOREIGN KEY (kraid) REFERENCES performance.kra(kraid) ON DELETE CASCADE;

-- Table: performance.skillsets
CREATE TABLE performance.skillsets (
    skillid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid,
    skillname character varying(100) NOT NULL,
    category character varying(100),
    description text,
    skilllevelscale jsonb DEFAULT '[1, 2, 3, 4, 5]'::jsonb,
    isactive boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE ONLY performance.skillsets
    ADD CONSTRAINT skillsets_pkey PRIMARY KEY (skillid);
ALTER TABLE ONLY performance.skillsets
    ADD CONSTRAINT uq_skillsets UNIQUE (organizationid, skillname);


-- ============================================================
-- VIEWS
-- ============================================================
-- View: attendance.vw_employee_timesheet_details
CREATE VIEW attendance.vw_employee_timesheet_details AS
 SELECT part1.employeeid,
    part1.attendanceid,
    part1.workdate,
    part1.checkintime,
    part1.checkouttime,
    part1.status,
    part1.totalhours,
    part1.is_finalized,
    part1.is_manager_override,
    part1.has_pending_request,
    part1.has_draft_request,
    part1.has_approved_request,
    part1.requested_checkin,
    part1.requested_checkout,
    part1.requested_status
   FROM ( SELECT DISTINCT ON (e.employeeid, a.workdate) e.employeeid,
            a.attendanceid,
            a.workdate,
            COALESCE(apr.requested_checkin, a.checkintime) AS checkintime,
            COALESCE(apr.requested_checkout, a.checkouttime) AS checkouttime,
            COALESCE(apr.requested_status, a.status) AS status,
            (
                CASE
                    WHEN ((COALESCE(apr.requested_status, a.status))::text = 'absent'::text) THEN 0.0
                    WHEN ((apr.requested_checkin IS NOT NULL) AND (apr.requested_checkout IS NOT NULL)) THEN round((EXTRACT(epoch FROM (apr.requested_checkout - apr.requested_checkin)) / 3600.0), 2)
                    ELSE a.totalhours
                END)::numeric(5,2) AS totalhours,
            (EXISTS ( SELECT 1
                   FROM attendance.finalized_timesheet_records f
                  WHERE (((f.attendance_id = a.attendanceid) OR ((f.employee_id = e.employeeid) AND (f.work_date = a.workdate) AND (f.attendance_id IS NULL))) AND ((f.record_status)::text = ANY (ARRAY[('finalized'::character varying)::text, ('overridden'::character varying)::text]))))) AS is_finalized,
            (EXISTS ( SELECT 1
                   FROM attendance.finalized_timesheet_records f_ov
                  WHERE (((f_ov.attendance_id = a.attendanceid) OR ((f_ov.employee_id = e.employeeid) AND (f_ov.work_date = a.workdate) AND (f_ov.attendance_id IS NULL))) AND ((f_ov.record_status)::text = 'overridden'::text)))) AS is_manager_override,
            (EXISTS ( SELECT 1
                   FROM attendance.attendance_update_requests rp
                  WHERE ((rp.attendance_id = a.attendanceid) AND ((rp.status)::text = 'pending'::text)))) AS has_pending_request,
            (EXISTS ( SELECT 1
                   FROM attendance.attendance_update_requests rd
                  WHERE ((rd.attendance_id = a.attendanceid) AND ((rd.status)::text = 'Draft'::text)))) AS has_draft_request,
            (EXISTS ( SELECT 1
                   FROM attendance.attendance_update_requests ra
                  WHERE ((ra.attendance_id = a.attendanceid) AND ((ra.status)::text = 'approved'::text)))) AS has_approved_request,
            ( SELECT rl.requested_checkin
                   FROM attendance.attendance_update_requests rl
                  WHERE ((rl.attendance_id = a.attendanceid) AND ((rl.status)::text = ANY (ARRAY[('pending'::character varying)::text, ('Draft'::character varying)::text, ('approved'::character varying)::text])))
                  ORDER BY rl.submitted_at DESC
                 LIMIT 1) AS requested_checkin,
            ( SELECT rl.requested_checkout
                   FROM attendance.attendance_update_requests rl
                  WHERE ((rl.attendance_id = a.attendanceid) AND ((rl.status)::text = ANY (ARRAY[('pending'::character varying)::text, ('Draft'::character varying)::text, ('approved'::character varying)::text])))
                  ORDER BY rl.submitted_at DESC
                 LIMIT 1) AS requested_checkout,
            ( SELECT rl.requested_status
                   FROM attendance.attendance_update_requests rl
                  WHERE ((rl.attendance_id = a.attendanceid) AND ((rl.status)::text = ANY (ARRAY[('pending'::character varying)::text, ('Draft'::character varying)::text, ('approved'::character varying)::text])))
                  ORDER BY rl.submitted_at DESC
                 LIMIT 1) AS requested_status
           FROM ((employee.employees e
             JOIN attendance.attendance a ON ((e.employeeid = a.employeeid)))
             LEFT JOIN LATERAL ( SELECT rl.requested_checkin,
                    rl.requested_checkout,
                    rl.requested_status
                   FROM attendance.attendance_update_requests rl
                  WHERE ((rl.attendance_id = a.attendanceid) AND ((rl.status)::text = 'approved'::text))
                  ORDER BY rl.submitted_at DESC
                 LIMIT 1) apr ON (true))
          ORDER BY e.employeeid, a.workdate) part1
UNION ALL
 SELECT part2.employeeid,
    part2.attendanceid,
    part2.workdate,
    part2.checkintime,
    part2.checkouttime,
    part2.status,
    part2.totalhours,
    part2.is_finalized,
    part2.is_manager_override,
    part2.has_pending_request,
    part2.has_draft_request,
    part2.has_approved_request,
    part2.requested_checkin,
    part2.requested_checkout,
    part2.requested_status
   FROM ( SELECT DISTINCT ON (e.employeeid, r.workdate) e.employeeid,
            NULL::uuid AS attendanceid,
            r.workdate,
            NULL::timestamp with time zone AS checkintime,
            NULL::timestamp with time zone AS checkouttime,
            COALESCE(r.requested_status, 'absent'::character varying) AS status,
            0.0::numeric(5,2) AS totalhours,
            (EXISTS ( SELECT 1
                   FROM attendance.finalized_timesheet_records f
                  WHERE ((f.employee_id = e.employeeid) AND (f.attendance_id IS NULL) AND (f.work_date = r.workdate) AND ((f.record_status)::text = ANY (ARRAY[('finalized'::character varying)::text, ('overridden'::character varying)::text]))))) AS is_finalized,
            (EXISTS ( SELECT 1
                   FROM attendance.finalized_timesheet_records f_ov
                  WHERE ((f_ov.employee_id = e.employeeid) AND (f_ov.attendance_id IS NULL) AND (f_ov.work_date = r.workdate) AND ((f_ov.record_status)::text = 'overridden'::text)))) AS is_manager_override,
            false AS has_pending_request,
            false AS has_draft_request,
            true AS has_approved_request,
            r.requested_checkin,
            r.requested_checkout,
            r.requested_status
           FROM (employee.employees e
             JOIN attendance.attendance_update_requests r ON (((r.employee_id = e.employeeid) AND ((r.status)::text = 'approved'::text) AND (r.attendance_id IS NULL))))
          WHERE (NOT (EXISTS ( SELECT 1
                   FROM attendance.attendance a
                  WHERE ((a.employeeid = e.employeeid) AND (a.workdate = r.workdate)))))
          ORDER BY e.employeeid, r.workdate, r.submitted_at DESC) part2
UNION ALL
 SELECT part3.employeeid,
    part3.attendanceid,
    part3.workdate,
    part3.checkintime,
    part3.checkouttime,
    part3.status,
    part3.totalhours,
    part3.is_finalized,
    part3.is_manager_override,
    part3.has_pending_request,
    part3.has_draft_request,
    part3.has_approved_request,
    part3.requested_checkin,
    part3.requested_checkout,
    part3.requested_status
   FROM ( SELECT DISTINCT ON (e.employeeid, fo.work_date) e.employeeid,
            NULL::uuid AS attendanceid,
            fo.work_date AS workdate,
            fo.final_checkin AS checkintime,
            fo.final_checkout AS checkouttime,
            COALESCE(fo.final_status, 'absent'::character varying) AS status,
            (
                CASE
                    WHEN ((fo.final_checkin IS NOT NULL) AND (fo.final_checkout IS NOT NULL)) THEN round((EXTRACT(epoch FROM (fo.final_checkout - fo.final_checkin)) / 3600.0), 2)
                    ELSE 0.0
                END)::numeric(5,2) AS totalhours,
            true AS is_finalized,
            ((fo.record_status)::text = 'overridden'::text) AS is_manager_override,
            false AS has_pending_request,
            false AS has_draft_request,
            false AS has_approved_request,
            fo.final_checkin AS requested_checkin,
            fo.final_checkout AS requested_checkout,
            fo.final_status AS requested_status
           FROM (employee.employees e
             JOIN attendance.finalized_timesheet_records fo ON (((fo.employee_id = e.employeeid) AND (fo.attendance_id IS NULL) AND ((fo.record_status)::text = ANY (ARRAY['overridden'::text, 'finalized'::text])))))
          WHERE ((NOT (EXISTS ( SELECT 1
                   FROM attendance.attendance a
                  WHERE ((a.employeeid = e.employeeid) AND (a.workdate = fo.work_date))))) AND (NOT (EXISTS ( SELECT 1
                   FROM attendance.attendance_update_requests r
                  WHERE ((r.employee_id = e.employeeid) AND (r.workdate = fo.work_date) AND ((r.status)::text = 'approved'::text) AND (r.attendance_id IS NULL))))))
          ORDER BY e.employeeid, fo.work_date) part3;

-- View: attendance.vw_geo_fence_violations_report
CREATE VIEW attendance.vw_geo_fence_violations_report AS
 SELECT v.violationid,
    v.violatedat,
    v.action_taken,
    v.distance_over_meters,
    v.actual_latitude,
    v.actual_longitude,
    v.resolved,
    v.organizationid,
    e.employeeid,
    e.employeecode,
    (((e.firstname)::text || ' '::text) || (e.lastname)::text) AS employee_name,
    e.profilepictureurl,
    gf.geofenceid,
    gf.name AS geo_fence_name,
    gf.center_latitude,
    gf.center_longitude,
    gf.radius_meters,
    sh.shiftid,
    sh.shiftname,
    fl.logid AS face_log_id,
    fl.match_result AS face_match_result,
    fl.confidence_score AS face_confidence_score,
    fl.reference_photo_url AS face_reference_photo_url
   FROM ((((attendance.geo_fence_violations v
     JOIN employee.employees e ON ((e.employeeid = v.employeeid)))
     LEFT JOIN attendance.geo_fences gf ON ((gf.geofenceid = v.geofenceid)))
     LEFT JOIN attendance.shifts sh ON ((sh.shiftid = v.shiftid)))
     LEFT JOIN attendance.face_recognition_logs fl ON ((fl.logid = v.face_log_id)));

-- View: attendance.vw_team_live_stream
CREATE VIEW attendance.vw_team_live_stream AS
 SELECT e.employeeid,
    e.employeecode,
    (((e.firstname)::text || ' '::text) || (e.lastname)::text) AS employee_name,
    e.profilepictureurl,
    e.organizationid,
    latest_sess.sessionid,
    latest_sess.checkintime AS last_checkin,
    latest_sess.location_status,
    latest_sess.distance_from_fence_m,
    gf.name AS geo_fence_name,
    latest_sess.latitude,
    latest_sess.longitude,
    latest_sess.checkinip AS device_ip,
    att.attendanceid,
    att.checkintime AS shift_start,
    att.totalhours,
    att.status AS attendance_status,
        CASE
            WHEN ((att.checkintime IS NOT NULL) AND (att.checkouttime IS NULL)) THEN LEAST(round((((EXTRACT(epoch FROM (now() - att.checkintime)) / 3600.0) / 8.0) * (100)::numeric), 1), (100)::numeric)
            WHEN (att.totalhours IS NOT NULL) THEN LEAST(round(((att.totalhours / 8.0) * (100)::numeric), 1), (100)::numeric)
            ELSE (0)::numeric
        END AS compliance_pct,
    sh.shiftname
   FROM (((((employee.employees e
     LEFT JOIN attendance.attendance att ON (((att.employeeid = e.employeeid) AND (att.workdate = CURRENT_DATE))))
     LEFT JOIN LATERAL ( SELECT s.sessionid,
            s.attendanceid,
            s.checkintime,
            s.checkouttime,
            s.location,
            s.createdat,
            s.updatedat,
            s.checkinip,
            s.checkoutip,
            s.geofenceid,
            s.latitude,
            s.longitude,
            s.location_status,
            s.distance_from_fence_m,
            s.face_log_id
           FROM attendance.attendancesessions s
          WHERE (s.attendanceid = att.attendanceid)
          ORDER BY s.checkintime DESC
         LIMIT 1) latest_sess ON (true))
     LEFT JOIN attendance.geo_fences gf ON ((gf.geofenceid = latest_sess.geofenceid)))
     LEFT JOIN employee.employmentdetails ed ON (((ed.employeeid = e.employeeid) AND (ed.iscurrent = true))))
     LEFT JOIN attendance.shifts sh ON ((sh.shiftid = ed.shiftid)))
  WHERE ((e.status)::text = 'active'::text);

-- View: attendance.vw_timesheet_consolidation
CREATE VIEW attendance.vw_timesheet_consolidation AS
 SELECT a.attendanceid,
    a.employeeid,
    a.workdate,
    COALESCE(f.final_checkin, a.checkintime) AS checkintime,
    COALESCE(f.final_checkout, a.checkouttime) AS checkouttime,
    COALESCE(f.final_total_hours, a.totalhours) AS totalhours,
    COALESCE(f.final_status, a.status) AS status,
    a.notes,
    (COALESCE(a.is_finalized, false) OR (f.record_id IS NOT NULL)) AS is_finalized,
        CASE
            WHEN ((f.record_id IS NOT NULL) AND ((a.checkintime IS DISTINCT FROM f.final_checkin) OR (a.checkouttime IS DISTINCT FROM f.final_checkout) OR ((a.status)::text IS DISTINCT FROM (f.final_status)::text))) THEN true
            ELSE false
        END AS is_manager_override,
    (EXISTS ( SELECT 1
           FROM attendance.attendance_update_requests r
          WHERE ((r.attendance_id = a.attendanceid) AND ((r.status)::text = 'pending'::text)))) AS has_pending_request,
    (EXISTS ( SELECT 1
           FROM attendance.attendance_update_requests r
          WHERE ((r.attendance_id = a.attendanceid) AND ((r.status)::text = 'draft'::text)))) AS has_draft_request,
    (EXISTS ( SELECT 1
           FROM attendance.attendance_update_requests r
          WHERE ((r.attendance_id = a.attendanceid) AND ((r.status)::text = 'approved'::text)))) AS has_approved_request
   FROM (attendance.attendance a
     LEFT JOIN attendance.finalized_timesheet_records f ON ((a.attendanceid = f.attendance_id)))
UNION ALL
 SELECT NULL::uuid AS attendanceid,
    f.employee_id AS employeeid,
    f.work_date AS workdate,
    f.final_checkin AS checkintime,
    f.final_checkout AS checkouttime,
    f.final_total_hours AS totalhours,
    f.final_status AS status,
    NULL::text AS notes,
    true AS is_finalized,
    true AS is_manager_override,
    false AS has_pending_request,
    false AS has_draft_request,
    false AS has_approved_request
   FROM attendance.finalized_timesheet_records f
  WHERE ((f.attendance_id IS NULL) AND (NOT (EXISTS ( SELECT 1
           FROM attendance.attendance a
          WHERE ((a.employeeid = f.employee_id) AND (a.workdate = f.work_date))))));


-- ============================================================
-- INDEXES
-- ============================================================
CREATE INDEX idx_company_holidays_date ON attendance.company_holidays USING btree (holidaydate);
CREATE INDEX idx_company_holidays_org_year ON attendance.company_holidays USING btree (organizationid, year);
CREATE INDEX idx_companysubscriptions_company_id ON payment.companysubscriptions USING btree (companyid);
CREATE INDEX idx_companysubscriptions_status ON payment.companysubscriptions USING btree (status);
CREATE INDEX idx_erh_employee ON attendance.employee_restricted_holidays USING btree (employeeid);
CREATE INDEX idx_erh_org_year ON attendance.employee_restricted_holidays USING btree (organizationid);
CREATE INDEX idx_frl_attempttime ON attendance.face_recognition_logs USING btree (attempttime DESC);
CREATE INDEX idx_frl_employee ON attendance.face_recognition_logs USING btree (employeeid);
CREATE INDEX idx_frl_result ON attendance.face_recognition_logs USING btree (match_result);
CREATE INDEX idx_geo_fences_active ON attendance.geo_fences USING btree (isactive) WHERE (deleted_at IS NULL);
CREATE INDEX idx_geo_fences_org ON attendance.geo_fences USING btree (organizationid) WHERE (deleted_at IS NULL);
CREATE INDEX idx_gfv_employee ON attendance.geo_fence_violations USING btree (employeeid);
CREATE INDEX idx_gfv_org ON attendance.geo_fence_violations USING btree (organizationid);
CREATE INDEX idx_gfv_unresolved ON attendance.geo_fence_violations USING btree (resolved) WHERE (resolved = false);
CREATE INDEX idx_gfv_violatedat ON attendance.geo_fence_violations USING btree (violatedat DESC);
CREATE INDEX idx_gratuitytransactions_periodid ON payroll.gratuitytransactions USING btree (periodid) WHERE (isdeleted = false);
CREATE INDEX idx_holiday_catalogs_country ON attendance.holiday_catalogs USING btree (countrycode);
CREATE INDEX idx_holiday_emp_assign_employee ON attendance.holiday_employee_assignments USING btree (employee_id);
CREATE INDEX idx_holiday_emp_assign_holiday ON attendance.holiday_employee_assignments USING btree (holiday_id);
CREATE INDEX idx_invoices_company_id ON payment.invoices USING btree (company_id);
CREATE INDEX idx_invoices_invoice_number ON payment.invoices USING btree (invoice_number);
CREATE INDEX idx_invoices_issued_at ON payment.invoices USING btree (issued_at DESC);
CREATE INDEX idx_invoices_status ON payment.invoices USING btree (status);
CREATE INDEX idx_invoices_transaction_id ON payment.invoices USING btree (transaction_id);
CREATE INDEX idx_refunds_created_at ON payment.refunds USING btree (created_at DESC);
CREATE INDEX idx_refunds_status ON payment.refunds USING btree (status);
CREATE INDEX idx_refunds_transaction_id ON payment.refunds USING btree (transaction_id);
CREATE INDEX idx_sgf_fence ON attendance.shift_geo_fences USING btree (geofenceid);
CREATE INDEX idx_sgf_shift ON attendance.shift_geo_fences USING btree (shiftid);
CREATE INDEX idx_shs_org_at ON attendance.system_health_snapshots USING btree (organizationid, snapshot_at DESC);
CREATE INDEX idx_transactions_company_id ON payment.transactions USING btree (company_id);
CREATE INDEX idx_transactions_created_at ON payment.transactions USING btree (created_at DESC);
CREATE INDEX idx_transactions_gateway ON payment.transactions USING btree (gateway);
CREATE INDEX idx_transactions_gateway_order ON payment.transactions USING btree (gateway_order_id) WHERE (gateway_order_id IS NOT NULL);
CREATE INDEX idx_transactions_gateway_txn_id ON payment.transactions USING btree (gateway_transaction_id) WHERE (gateway_transaction_id IS NOT NULL);
CREATE INDEX idx_transactions_status ON payment.transactions USING btree (status);
CREATE UNIQUE INDEX idx_unique_draft_request ON attendance.attendance_update_requests USING btree (employee_id, workdate) WHERE ((status)::text = 'draft'::text);
CREATE UNIQUE INDEX uq_active_request_per_date ON attendance.attendance_update_requests USING btree (employee_id, workdate) WHERE ((status)::text = ANY (ARRAY[('draft'::character varying)::text, ('pending'::character varying)::text]));
CREATE UNIQUE INDEX uq_face_descriptor_active_employee ON employee.employee_face_descriptors USING btree (employeeid) WHERE (isactive = true);
CREATE UNIQUE INDEX uq_finalized_absence_emp_date ON attendance.finalized_timesheet_records USING btree (employee_id, work_date) WHERE (attendance_id IS NULL);
CREATE UNIQUE INDEX uq_finalized_attendance_id ON attendance.finalized_timesheet_records USING btree (attendance_id) WHERE (attendance_id IS NOT NULL);
CREATE UNIQUE INDEX uq_ftr_attendance_id ON attendance.finalized_timesheet_records USING btree (attendance_id) WHERE (attendance_id IS NOT NULL);
CREATE UNIQUE INDEX uq_ftr_emp_workdate_null ON attendance.finalized_timesheet_records USING btree (employee_id, work_date) WHERE (attendance_id IS NULL);
CREATE UNIQUE INDEX uq_pending_attendance_request ON attendance.attendance_update_requests USING btree (attendance_id) WHERE ((status)::text = 'pending'::text);


-- ============================================================
-- TRIGGERS
-- ============================================================
CREATE TRIGGER trg_transactions_updated_at BEFORE UPDATE ON payment.transactions FOR EACH ROW EXECUTE FUNCTION payment.update_updated_at_column();