BEGIN;

CREATE SCHEMA IF NOT EXISTS entitlement;

CREATE TABLE IF NOT EXISTS entitlement.migration_plan_backup AS
SELECT planid, name, monthlyprice, annualprice, maxusers, features, iscustom, isactive
FROM payment.subscriptionplans
WITH NO DATA;

INSERT INTO entitlement.migration_plan_backup
    (planid, name, monthlyprice, annualprice, maxusers, features, iscustom, isactive)
SELECT planid, name, monthlyprice, annualprice, maxusers, features, iscustom, isactive
FROM payment.subscriptionplans p
WHERE NOT EXISTS (
    SELECT 1
    FROM entitlement.migration_plan_backup b
    WHERE b.planid = p.planid
);

ALTER TABLE payment.subscriptionplans
    ADD COLUMN IF NOT EXISTS code character varying(100),
    ADD COLUMN IF NOT EXISTS currency character varying(3) NOT NULL DEFAULT 'BHD',
    ADD COLUMN IF NOT EXISTS user_count_type character varying(30) NOT NULL DEFAULT 'active_only',
    ADD COLUMN IF NOT EXISTS is_public boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS is_recommended boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS status character varying(20) NOT NULL DEFAULT 'active',
    ADD COLUMN IF NOT EXISTS version integer NOT NULL DEFAULT 1;

UPDATE payment.subscriptionplans
SET code = regexp_replace(lower(name), '[^a-z0-9]+', '_', 'g')
WHERE code IS NULL;

WITH duplicate_codes AS (
    SELECT code
    FROM payment.subscriptionplans
    GROUP BY code
    HAVING count(*) > 1
)
UPDATE payment.subscriptionplans p
SET code = p.code || '_' || left(replace(p.planid::text, '-', ''), 8)
FROM duplicate_codes d
WHERE p.code = d.code;

ALTER TABLE payment.subscriptionplans
    ALTER COLUMN code SET NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS uq_subscriptionplans_code
    ON payment.subscriptionplans (code);

DO $constraints$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_subscriptionplans_user_count_type'
          AND conrelid = 'payment.subscriptionplans'::regclass
    ) THEN
        ALTER TABLE payment.subscriptionplans
            ADD CONSTRAINT ck_subscriptionplans_user_count_type
            CHECK (user_count_type IN ('active_only', 'active_and_inactive'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_subscriptionplans_status'
          AND conrelid = 'payment.subscriptionplans'::regclass
    ) THEN
        ALTER TABLE payment.subscriptionplans
            ADD CONSTRAINT ck_subscriptionplans_status
            CHECK (status IN ('active', 'archived'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_subscriptionplans_version'
          AND conrelid = 'payment.subscriptionplans'::regclass
    ) THEN
        ALTER TABLE payment.subscriptionplans
            ADD CONSTRAINT ck_subscriptionplans_version CHECK (version > 0);
    END IF;
END;
$constraints$;

UPDATE payment.subscriptionplans
SET status = CASE WHEN isactive THEN 'active' ELSE 'archived' END;

ALTER TABLE payment.companysubscriptions
    ADD COLUMN IF NOT EXISTS trial_ends_at timestamp with time zone,
    ADD COLUMN IF NOT EXISTS custom_price numeric(12,2),
    ADD COLUMN IF NOT EXISTS custom_max_users integer,
    ADD COLUMN IF NOT EXISTS custom_currency character varying(3) DEFAULT 'BHD';

DO $constraints$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_companysubscriptions_custom_max_users'
          AND conrelid = 'payment.companysubscriptions'::regclass
    ) THEN
        ALTER TABLE payment.companysubscriptions
            ADD CONSTRAINT ck_companysubscriptions_custom_max_users
            CHECK (custom_max_users IS NULL OR custom_max_users > 0);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_companysubscriptions_custom_price'
          AND conrelid = 'payment.companysubscriptions'::regclass
    ) THEN
        ALTER TABLE payment.companysubscriptions
            ADD CONSTRAINT ck_companysubscriptions_custom_price
            CHECK (custom_price IS NULL OR custom_price >= 0);
    END IF;
END;
$constraints$;

CREATE TABLE IF NOT EXISTS entitlement.features (
    feature_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    key character varying(100) NOT NULL UNIQUE,
    name character varying(150) NOT NULL,
    module_key character varying(100) NOT NULL,
    description text,
    type character varying(20) NOT NULL DEFAULT 'boolean'
        CHECK (type IN ('boolean', 'limit')),
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS entitlement.plan_features (
    plan_id uuid NOT NULL REFERENCES payment.subscriptionplans(planid) ON DELETE CASCADE,
    feature_id uuid NOT NULL REFERENCES entitlement.features(feature_id) ON DELETE CASCADE,
    enabled boolean NOT NULL DEFAULT true,
    limit_value integer CHECK (limit_value IS NULL OR limit_value >= 0),
    PRIMARY KEY (plan_id, feature_id)
);

CREATE TABLE IF NOT EXISTS entitlement.tenant_feature_overrides (
    tenant_id uuid NOT NULL REFERENCES auth.organizations(organizationid) ON DELETE CASCADE,
    feature_id uuid NOT NULL REFERENCES entitlement.features(feature_id) ON DELETE CASCADE,
    enabled boolean NOT NULL,
    limit_value integer CHECK (limit_value IS NULL OR limit_value >= 0),
    reason text NOT NULL,
    created_by uuid,
    updated_at timestamp with time zone NOT NULL DEFAULT now(),
    PRIMARY KEY (tenant_id, feature_id)
);

CREATE INDEX IF NOT EXISTS idx_tenant_feature_overrides_feature
    ON entitlement.tenant_feature_overrides (feature_id);

CREATE TABLE IF NOT EXISTS entitlement.default_role_permissions (
    feature_id uuid NOT NULL REFERENCES entitlement.features(feature_id) ON DELETE CASCADE,
    role character varying(30) NOT NULL
        CHECK (role IN ('employee', 'manager', 'hr_admin', 'finance_admin', 'super_admin', 'auditor')),
    actions jsonb NOT NULL DEFAULT '[]'::jsonb,
    data_scope character varying(10) NOT NULL DEFAULT 'own'
        CHECK (data_scope IN ('own', 'team', 'all')),
    PRIMARY KEY (feature_id, role)
);

CREATE TABLE IF NOT EXISTS entitlement.audit_logs (
    audit_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid REFERENCES auth.organizations(organizationid) ON DELETE SET NULL,
    actor uuid,
    action character varying(100) NOT NULL,
    entity character varying(100) NOT NULL,
    entity_id uuid,
    before_state jsonb,
    after_state jsonb,
    created_at timestamp with time zone NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS entitlement.ai_query_usage (
    tenant_id uuid NOT NULL REFERENCES auth.organizations(organizationid) ON DELETE CASCADE,
    usage_month date NOT NULL,
    query_count integer NOT NULL DEFAULT 0 CHECK (query_count >= 0),
    PRIMARY KEY (tenant_id, usage_month)
);

CREATE INDEX IF NOT EXISTS idx_entitlement_audit_tenant_created
    ON entitlement.audit_logs (tenant_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_entitlement_audit_action_created
    ON entitlement.audit_logs (action, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_entitlement_audit_entity
    ON entitlement.audit_logs (entity, entity_id);
CREATE INDEX IF NOT EXISTS idx_companysubscriptions_tenant_status
    ON payment.companysubscriptions (companyid, status, enddate DESC);

INSERT INTO entitlement.features (key, name, module_key, description, type)
VALUES
    ('dashboard', 'Dashboard', 'dashboard', 'HRMS dashboard and widgets.', 'boolean'),
    ('employee_management', 'Employee Management', 'employee_management', 'Employee records and management.', 'boolean'),
    ('document_expiry_alerts', 'Document Expiry Alerts', 'employee_management', 'Alerts for expiring employee documents.', 'boolean'),
    ('attendance', 'Attendance', 'attendance', 'Core attendance tracking.', 'boolean'),
    ('basic_attendance', 'Basic Attendance', 'attendance', 'Basic clock-in and attendance records.', 'boolean'),
    ('shifts_overtime', 'Shifts and Overtime', 'attendance', 'Shift scheduling and overtime.', 'boolean'),
    ('biometric_integration', 'Biometric Integration', 'attendance', 'Biometric device integration.', 'boolean'),
    ('geo_fencing', 'Geo-fencing', 'attendance', 'Location-bound attendance checks.', 'boolean'),
    ('timesheet', 'Timesheets', 'timesheet', 'Timesheet entry and approvals.', 'boolean'),
    ('leave_management', 'Leave Management', 'leave_management', 'Leave requests and balances.', 'boolean'),
    ('assets_management', 'Asset Management', 'assets_management', 'Company asset tracking.', 'boolean'),
    ('performance', 'Performance', 'performance', 'Performance reviews and goals.', 'boolean'),
    ('expense', 'Expense Management', 'expense', 'Expense claims and approvals.', 'boolean'),
    ('news', 'Company News', 'news', 'Company news and announcements.', 'boolean'),
    ('help_desk', 'Help Desk', 'help_desk', 'Employee help desk and ticketing.', 'boolean'),
    ('jobs', 'Jobs', 'jobs', 'Job and career management.', 'boolean'),
    ('bahrainisation_tracking', 'Bahrainisation Tracking', 'jobs', 'Bahrainisation workforce tracking.', 'boolean'),
    ('payroll', 'Payroll', 'payroll', 'Payroll module access.', 'boolean'),
    ('payroll_processing', 'Payroll Processing', 'payroll', 'Payroll runs, policies, and administration.', 'boolean'),
    ('payslips', 'Payslips', 'payroll', 'Employee payslip access.', 'boolean'),
    ('sio_calculation', 'SIO Calculation', 'payroll', 'Social Insurance Organization calculations.', 'boolean'),
    ('eosb', 'End of Service Benefits', 'payroll', 'End of service benefit calculations.', 'boolean'),
    ('wps_file_export', 'WPS File Export', 'payroll', 'Wage Protection System file export.', 'boolean'),
    ('loans_advances', 'Loans and Advances', 'payroll', 'Employee loans and salary advances.', 'boolean'),
    ('onboarding', 'Onboarding', 'onboarding', 'Employee onboarding workflows.', 'boolean'),
    ('ai_assistant', 'AI Assistant', 'ai_assistant', 'AI assistant access and monthly query allowance.', 'limit'),
    ('approval_workflows', 'Approval Workflows', 'approval_workflows', 'Configurable approval workflows.', 'boolean'),
    ('reports', 'Reports', 'reports', 'Advanced reports and exports.', 'boolean'),
    ('multi_company', 'Multi-company', 'platform', 'Manage multiple companies under one tenant.', 'boolean'),
    ('api_access', 'API Access', 'platform', 'Access to public integration APIs.', 'boolean'),
    ('audit_logs', 'Audit Logs', 'platform', 'Tenant audit history.', 'boolean'),
    ('custom_roles', 'Custom Roles', 'platform', 'Create custom tenant roles.', 'boolean')
ON CONFLICT (key) DO UPDATE SET
    name = EXCLUDED.name,
    module_key = EXCLUDED.module_key,
    description = EXCLUDED.description,
    type = EXCLUDED.type,
    is_active = true;

INSERT INTO payment.subscriptionplans
    (name, code, monthlyprice, annualprice, maxusers, features, iscustom, isactive,
     currency, user_count_type, is_public, is_recommended, status)
VALUES
    ('Starter', 'starter', 0, NULL, 5, '{}'::jsonb, false, true, 'BHD', 'active_and_inactive', true, false, 'active'),
    ('Premium', 'premium', 13, NULL, 25, '{}'::jsonb, false, true, 'BHD', 'active_only', true, false, 'active'),
    ('Business', 'business', 26, NULL, 50, '{}'::jsonb, false, true, 'BHD', 'active_only', true, true, 'active'),
    ('Pro', 'pro', 45, NULL, 100, '{}'::jsonb, false, true, 'BHD', 'active_only', true, false, 'active'),
    ('Custom', 'custom', NULL, NULL, NULL, '{}'::jsonb, true, true, 'BHD', 'active_only', false, false, 'active')
ON CONFLICT (code) DO UPDATE SET
    name = EXCLUDED.name,
    monthlyprice = EXCLUDED.monthlyprice,
    maxusers = EXCLUDED.maxusers,
    iscustom = EXCLUDED.iscustom,
    isactive = true,
    currency = EXCLUDED.currency,
    user_count_type = EXCLUDED.user_count_type,
    is_public = EXCLUDED.is_public,
    is_recommended = EXCLUDED.is_recommended,
    status = 'active';

WITH plan_features(plan_code, feature_key, enabled, limit_value) AS (
    VALUES
        ('starter', 'dashboard', true, NULL::integer),
        ('starter', 'employee_management', true, NULL::integer),
        ('starter', 'document_expiry_alerts', true, NULL::integer),
        ('starter', 'attendance', true, NULL::integer),
        ('starter', 'basic_attendance', true, NULL::integer),
        ('starter', 'leave_management', true, NULL::integer),
        ('starter', 'news', true, NULL::integer),
        ('starter', 'ai_assistant', true, 50),
        ('premium', 'dashboard', true, NULL::integer),
        ('premium', 'employee_management', true, NULL::integer),
        ('premium', 'document_expiry_alerts', true, NULL::integer),
        ('premium', 'attendance', true, NULL::integer),
        ('premium', 'basic_attendance', true, NULL::integer),
        ('premium', 'shifts_overtime', true, NULL::integer),
        ('premium', 'timesheet', true, NULL::integer),
        ('premium', 'leave_management', true, NULL::integer),
        ('premium', 'news', true, NULL::integer),
        ('premium', 'assets_management', true, NULL::integer),
        ('premium', 'help_desk', true, NULL::integer),
        ('premium', 'onboarding', true, NULL::integer),
        ('premium', 'payroll', true, NULL::integer),
        ('premium', 'payslips', true, NULL::integer),
        ('premium', 'ai_assistant', true, 50),
        ('business', 'dashboard', true, NULL::integer),
        ('business', 'employee_management', true, NULL::integer),
        ('business', 'document_expiry_alerts', true, NULL::integer),
        ('business', 'attendance', true, NULL::integer),
        ('business', 'basic_attendance', true, NULL::integer),
        ('business', 'shifts_overtime', true, NULL::integer),
        ('business', 'timesheet', true, NULL::integer),
        ('business', 'leave_management', true, NULL::integer),
        ('business', 'news', true, NULL::integer),
        ('business', 'assets_management', true, NULL::integer),
        ('business', 'help_desk', true, NULL::integer),
        ('business', 'onboarding', true, NULL::integer),
        ('business', 'payroll', true, NULL::integer),
        ('business', 'payroll_processing', true, NULL::integer),
        ('business', 'payslips', true, NULL::integer),
        ('business', 'sio_calculation', true, NULL::integer),
        ('business', 'eosb', true, NULL::integer),
        ('business', 'wps_file_export', true, NULL::integer),
        ('business', 'loans_advances', true, NULL::integer),
        ('business', 'ai_assistant', true, 50),
        ('business', 'expense', true, NULL::integer),
        ('business', 'performance', true, NULL::integer),
        ('business', 'approval_workflows', true, NULL::integer),
        ('business', 'reports', true, NULL::integer),
        ('pro', 'dashboard', true, NULL::integer),
        ('pro', 'employee_management', true, NULL::integer),
        ('pro', 'document_expiry_alerts', true, NULL::integer),
        ('pro', 'attendance', true, NULL::integer),
        ('pro', 'basic_attendance', true, NULL::integer),
        ('pro', 'shifts_overtime', true, NULL::integer),
        ('pro', 'biometric_integration', true, NULL::integer),
        ('pro', 'timesheet', true, NULL::integer),
        ('pro', 'leave_management', true, NULL::integer),
        ('pro', 'news', true, NULL::integer),
        ('pro', 'assets_management', true, NULL::integer),
        ('pro', 'help_desk', true, NULL::integer),
        ('pro', 'onboarding', true, NULL::integer),
        ('pro', 'payroll', true, NULL::integer),
        ('pro', 'payroll_processing', true, NULL::integer),
        ('pro', 'payslips', true, NULL::integer),
        ('pro', 'sio_calculation', true, NULL::integer),
        ('pro', 'eosb', true, NULL::integer),
        ('pro', 'wps_file_export', true, NULL::integer),
        ('pro', 'loans_advances', true, NULL::integer),
        ('pro', 'ai_assistant', true, NULL::integer),
        ('pro', 'expense', true, NULL::integer),
        ('pro', 'performance', true, NULL::integer),
        ('pro', 'approval_workflows', true, NULL::integer),
        ('pro', 'reports', true, NULL::integer),
        ('pro', 'jobs', true, NULL::integer),
        ('pro', 'bahrainisation_tracking', true, NULL::integer),
        ('pro', 'multi_company', true, NULL::integer),
        ('pro', 'api_access', true, NULL::integer),
        ('pro', 'audit_logs', true, NULL::integer),
        ('pro', 'custom_roles', true, NULL::integer)
)
INSERT INTO entitlement.plan_features (plan_id, feature_id, enabled, limit_value)
SELECT p.planid, f.feature_id, pf.enabled, pf.limit_value
FROM plan_features pf
JOIN payment.subscriptionplans p ON p.code = pf.plan_code
JOIN entitlement.features f ON f.key = pf.feature_key
ON CONFLICT (plan_id, feature_id) DO UPDATE SET
    enabled = EXCLUDED.enabled,
    limit_value = EXCLUDED.limit_value;

INSERT INTO entitlement.default_role_permissions (feature_id, role, actions, data_scope)
SELECT
    f.feature_id,
    roles.role,
    CASE
        WHEN roles.role = 'super_admin' THEN '["view","create","edit","delete","approve","export","configure"]'::jsonb
        WHEN roles.role = 'auditor' THEN '["view","export"]'::jsonb
        WHEN roles.role = 'employee' AND f.key = 'payslips' THEN '["view"]'::jsonb
        WHEN roles.role = 'employee' AND f.module_key IN ('dashboard', 'employee_management', 'attendance', 'timesheet', 'leave_management', 'news', 'help_desk', 'expense', 'ai_assistant') THEN '["view","create"]'::jsonb
        WHEN roles.role = 'manager' AND f.module_key IN ('payroll', 'platform') THEN '[]'::jsonb
        WHEN roles.role = 'manager' THEN '["view","approve"]'::jsonb
        WHEN roles.role = 'hr_admin' AND f.module_key = 'payroll' THEN '["view"]'::jsonb
        WHEN roles.role = 'hr_admin' AND f.module_key <> 'platform' THEN '["view","create","edit","delete","approve","export","configure"]'::jsonb
        WHEN roles.role = 'finance_admin' AND f.module_key IN ('payroll', 'expense') THEN '["view","create","edit","delete","approve","export","configure"]'::jsonb
        WHEN roles.role = 'finance_admin' THEN '["view"]'::jsonb
        ELSE '[]'::jsonb
    END,
    CASE
        WHEN roles.role = 'super_admin' THEN 'all'
        WHEN roles.role = 'auditor' THEN 'all'
        WHEN roles.role = 'manager' THEN 'team'
        WHEN roles.role = 'employee' THEN 'own'
        ELSE 'all'
    END
FROM entitlement.features f
CROSS JOIN (VALUES
    ('employee'), ('manager'), ('hr_admin'), ('finance_admin'), ('super_admin'), ('auditor')
) AS roles(role)
ON CONFLICT (feature_id, role) DO NOTHING;

INSERT INTO payment.companysubscriptions
    (companyid, planid, status, billingcycle, startdate, enddate)
SELECT o.organizationid, p.planid, 'active', 'monthly', now(), NULL
FROM auth.organizations o
CROSS JOIN payment.subscriptionplans p
WHERE p.code = 'starter'
  AND NOT EXISTS (
      SELECT 1
      FROM payment.companysubscriptions s
      WHERE s.companyid = o.organizationid
  );

CREATE OR REPLACE FUNCTION entitlement.assign_starter_plan_to_new_tenant()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO payment.companysubscriptions
        (companyid, planid, status, billingcycle, startdate, enddate)
    SELECT NEW.organizationid, p.planid, 'active', 'monthly', now(), NULL
    FROM payment.subscriptionplans p
    WHERE p.code = 'starter'
      AND p.status = 'active';
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_assign_starter_plan_to_new_tenant ON auth.organizations;
CREATE TRIGGER trg_assign_starter_plan_to_new_tenant
AFTER INSERT ON auth.organizations
FOR EACH ROW
EXECUTE FUNCTION entitlement.assign_starter_plan_to_new_tenant();

CREATE OR REPLACE FUNCTION entitlement.enforce_tenant_user_limit()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    tenant_limit integer;
    count_type character varying(30);
    current_count integer;
    excluded_employee_id uuid;
BEGIN
    PERFORM pg_advisory_xact_lock(hashtextextended(NEW.organizationid::text, 0));

    SELECT
        COALESCE(s.custom_max_users, p.maxusers),
        p.user_count_type
    INTO tenant_limit, count_type
    FROM payment.companysubscriptions s
    JOIN payment.subscriptionplans p ON p.planid = s.planid
    WHERE s.companyid = NEW.organizationid
    ORDER BY
        CASE lower(COALESCE(s.status, '')) WHEN 'active' THEN 0 WHEN 'trial' THEN 1 ELSE 2 END,
        s.enddate DESC NULLS FIRST
    LIMIT 1;

    IF tenant_limit IS NULL THEN
        RETURN NEW;
    END IF;

    IF count_type = 'active_only' AND lower(COALESCE(NEW.status, '')) <> 'active' THEN
        RETURN NEW;
    END IF;

    IF TG_OP = 'UPDATE' THEN
        excluded_employee_id := OLD.employeeid;
    END IF;

    SELECT count(*)::integer
    INTO current_count
    FROM employee.employees e
    WHERE e.organizationid = NEW.organizationid
      AND (count_type <> 'active_only' OR lower(COALESCE(e.status, '')) = 'active')
      AND (excluded_employee_id IS NULL OR e.employeeid <> excluded_employee_id);

    IF current_count >= tenant_limit THEN
        RAISE EXCEPTION 'Tenant user limit of % has been reached.', tenant_limit
            USING ERRCODE = '23514';
    END IF;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_enforce_tenant_user_limit ON employee.employees;
CREATE TRIGGER trg_enforce_tenant_user_limit
BEFORE INSERT OR UPDATE OF organizationid, status ON employee.employees
FOR EACH ROW
EXECUTE FUNCTION entitlement.enforce_tenant_user_limit();

COMMIT;
