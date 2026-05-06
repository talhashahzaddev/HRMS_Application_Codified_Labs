-- =============================================================
-- TAX + SOCIAL SECURITY FLOW HARDENING
-- =============================================================
-- Purpose:
-- 1) Ensure tax/social schema is consistent for API-first admin + employee flow
-- 2) Backfill legacy drift safely (idempotent)
-- 3) Add indexes/constraints needed for performance and integrity

-- =========================
-- TAX SLABS
-- =========================
ALTER TABLE payroll.taxslabs
    ADD COLUMN IF NOT EXISTS slabrulename varchar(100);

UPDATE payroll.taxslabs
SET slabrulename = COALESCE(NULLIF(slabrulename, ''), 'Tax Slab')
WHERE slabrulename IS NULL OR slabrulename = '';

ALTER TABLE payroll.taxslabs
    ALTER COLUMN slabrulename SET NOT NULL;

ALTER TABLE payroll.taxslabs
    ADD COLUMN IF NOT EXISTS isactive boolean DEFAULT true,
    ADD COLUMN IF NOT EXISTS isdeleted boolean DEFAULT false,
    ADD COLUMN IF NOT EXISTS createdat timestamptz DEFAULT current_timestamp,
    ADD COLUMN IF NOT EXISTS updatedat timestamptz DEFAULT current_timestamp;

-- =========================
-- TAX TRANSACTIONS
-- =========================
ALTER TABLE payroll.taxtransactions
    ADD COLUMN IF NOT EXISTS createdat timestamptz DEFAULT current_timestamp,
    ADD COLUMN IF NOT EXISTS updatedat timestamptz DEFAULT current_timestamp;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'uqtaxtransaction'
          AND conrelid = 'payroll.taxtransactions'::regclass
    ) THEN
        ALTER TABLE payroll.taxtransactions
            ADD CONSTRAINT uqtaxtransaction UNIQUE (employeeid, periodid);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_taxslabs_org_active
    ON payroll.taxslabs (organizationid, isactive, isdeleted);

CREATE INDEX IF NOT EXISTS idx_taxslabs_org_fiscal
    ON payroll.taxslabs (organizationid, fiscalyear);

CREATE INDEX IF NOT EXISTS idx_taxtransactions_org_period
    ON payroll.taxtransactions (organizationid, periodid);

CREATE INDEX IF NOT EXISTS idx_taxtransactions_org_employee
    ON payroll.taxtransactions (organizationid, employeeid);

-- =========================
-- SOCIAL SECURITY CONFIG
-- =========================
ALTER TABLE payroll.socialsecurityconfig
    ADD COLUMN IF NOT EXISTS configrulename varchar(100);

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'payroll'
          AND table_name = 'socialsecurityconfig'
          AND column_name = 'configname'
    ) THEN
        EXECUTE '
            UPDATE payroll.socialsecurityconfig
               SET configrulename = COALESCE(NULLIF(configrulename, ''''), configname)
             WHERE configrulename IS NULL OR configrulename = ''''';
        ';
    END IF;
END $$;

UPDATE payroll.socialsecurityconfig
SET configrulename = COALESCE(NULLIF(configrulename, ''), 'Social Security Rule')
WHERE configrulename IS NULL OR configrulename = '';

ALTER TABLE payroll.socialsecurityconfig
    ALTER COLUMN configrulename SET NOT NULL;

ALTER TABLE payroll.socialsecurityconfig
    ADD COLUMN IF NOT EXISTS isactive boolean DEFAULT true,
    ADD COLUMN IF NOT EXISTS isdeleted boolean DEFAULT false,
    ADD COLUMN IF NOT EXISTS createdat timestamptz DEFAULT current_timestamp,
    ADD COLUMN IF NOT EXISTS updatedat timestamptz DEFAULT current_timestamp;

ALTER TABLE payroll.socialsecurityconfig
    DROP CONSTRAINT IF EXISTS uqssconfig;

ALTER TABLE payroll.socialsecurityconfig
    ADD CONSTRAINT uqssconfig UNIQUE (organizationid, configrulename);

-- =========================
-- SOCIAL SECURITY TRANSACTIONS
-- =========================
ALTER TABLE payroll.socialsecuritytransactions
    ADD COLUMN IF NOT EXISTS actualsalary numeric(15,2),
    ADD COLUMN IF NOT EXISTS requeststatus varchar(20) DEFAULT 'pending',
    ADD COLUMN IF NOT EXISTS isenrolled boolean DEFAULT false,
    ADD COLUMN IF NOT EXISTS withdrawalrequest boolean DEFAULT false,
    ADD COLUMN IF NOT EXISTS withdrawalstatus varchar(20),
    ADD COLUMN IF NOT EXISTS createdat timestamptz DEFAULT current_timestamp,
    ADD COLUMN IF NOT EXISTS updatedat timestamptz DEFAULT current_timestamp;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'uqsstransaction'
          AND conrelid = 'payroll.socialsecuritytransactions'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecuritytransactions
            ADD CONSTRAINT uqsstransaction UNIQUE (employeeid, periodid);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_socialsecurityconfig_org_active
    ON payroll.socialsecurityconfig (organizationid, isactive, isdeleted);

CREATE INDEX IF NOT EXISTS idx_socialsecuritytransactions_org_period
    ON payroll.socialsecuritytransactions (organizationid, periodid);

CREATE INDEX IF NOT EXISTS idx_socialsecuritytransactions_org_employee
    ON payroll.socialsecuritytransactions (organizationid, employeeid);

CREATE INDEX IF NOT EXISTS idx_socialsecuritytransactions_org_config
    ON payroll.socialsecuritytransactions (organizationid, configid);
