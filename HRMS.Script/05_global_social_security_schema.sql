-- =============================================================
-- GLOBAL SOCIAL SECURITY SCHEMA UPGRADE (PAYROLL-ALIGNED)
-- =============================================================
-- Purpose:
-- 1) Keep existing social security APIs working (socialsecurityconfig + socialsecuritytransactions)
-- 2) Add global-ready, effective-dated social security model (jurisdiction/scheme/rule)
-- 3) Add employee lifecycle coverage (enrollment, requests, documents, claims)
-- 4) Add payroll linkage (payroll periods, payrolldata sync, remittance tracking)
-- 5) Keep migration idempotent and safe for repeated runs

-- =============================================================
-- A) MASTER / GLOBAL CONFIG TABLES
-- =============================================================

CREATE TABLE IF NOT EXISTS payroll.socialsecurityjurisdictions
(
    jurisdictionid      uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    organizationid      uuid NOT NULL,
    jurisdictioncode    varchar(30) NOT NULL,
    jurisdictionname    varchar(150) NOT NULL,
    countrycode         char(2) NOT NULL DEFAULT 'ZZ',
    currency            varchar(3) DEFAULT 'USD',
    timezone            varchar(60) DEFAULT 'UTC',
    isdefault           boolean DEFAULT false,
    isactive            boolean DEFAULT true,
    isdeleted           boolean DEFAULT false,
    metadata            jsonb DEFAULT '{}'::jsonb,
    createdat           timestamptz DEFAULT current_timestamp,
    updatedat           timestamptz DEFAULT current_timestamp,
    CONSTRAINT uq_ss_jurisdiction UNIQUE (organizationid, jurisdictioncode)
);

CREATE INDEX IF NOT EXISTS idx_ss_jurisdictions_org
    ON payroll.socialsecurityjurisdictions (organizationid, isactive, isdeleted);


CREATE TABLE IF NOT EXISTS payroll.socialsecurityauthorities
(
    authorityid         uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    organizationid      uuid NOT NULL,
    jurisdictionid      uuid NOT NULL REFERENCES payroll.socialsecurityjurisdictions(jurisdictionid),
    authoritycode       varchar(50) NOT NULL,
    authorityname       varchar(150) NOT NULL,
    portalurl           varchar(500),
    contactemail        varchar(255),
    remittancefrequency varchar(20) DEFAULT 'monthly'
                        CHECK (remittancefrequency IN ('weekly', 'biweekly', 'monthly', 'quarterly', 'yearly', 'on_payday')),
    filingmode          varchar(20) DEFAULT 'online'
                        CHECK (filingmode IN ('online', 'file_upload', 'api', 'manual')),
    paymentmode         varchar(30) DEFAULT 'bank_transfer'
                        CHECK (paymentmode IN ('bank_transfer', 'direct_debit', 'wallet', 'manual')),
    dueoffsetdays       integer DEFAULT 14,
    isactive            boolean DEFAULT true,
    isdeleted           boolean DEFAULT false,
    metadata            jsonb DEFAULT '{}'::jsonb,
    createdat           timestamptz DEFAULT current_timestamp,
    updatedat           timestamptz DEFAULT current_timestamp,
    CONSTRAINT uq_ss_authority UNIQUE (organizationid, jurisdictionid, authoritycode)
);

CREATE INDEX IF NOT EXISTS idx_ss_authorities_org
    ON payroll.socialsecurityauthorities (organizationid, jurisdictionid, isactive, isdeleted);


CREATE TABLE IF NOT EXISTS payroll.socialsecurityschemes
(
    schemeid                uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    organizationid          uuid NOT NULL,
    jurisdictionid          uuid NOT NULL REFERENCES payroll.socialsecurityjurisdictions(jurisdictionid),
    authorityid             uuid REFERENCES payroll.socialsecurityauthorities(authorityid),
    schemecode              varchar(50) NOT NULL,
    schemename              varchar(150) NOT NULL,
    schemetype              varchar(30) DEFAULT 'social_insurance'
                            CHECK (schemetype IN ('social_insurance', 'retirement_fund', 'health_insurance', 'unemployment_insurance', 'mixed')),
    mandatorymode           varchar(20) DEFAULT 'mandatory'
                            CHECK (mandatorymode IN ('mandatory', 'optional', 'mixed')),
    employeemandatory       boolean DEFAULT true,
    employermandatory       boolean DEFAULT true,
    allowvoluntarytopup     boolean DEFAULT false,
    allowcrossborder        boolean DEFAULT false,
    legalreference          varchar(255),
    versionstartdate        date NOT NULL DEFAULT current_date,
    versionenddate          date,
    metadata                jsonb DEFAULT '{}'::jsonb,
    isactive                boolean DEFAULT true,
    isdeleted               boolean DEFAULT false,
    createdat               timestamptz DEFAULT current_timestamp,
    updatedat               timestamptz DEFAULT current_timestamp,
    CONSTRAINT uq_ss_scheme UNIQUE (organizationid, schemecode, versionstartdate),
    CONSTRAINT chk_ss_scheme_dates CHECK (versionenddate IS NULL OR versionenddate >= versionstartdate)
);

CREATE INDEX IF NOT EXISTS idx_ss_schemes_org
    ON payroll.socialsecurityschemes (organizationid, jurisdictionid, authorityid, isactive, isdeleted);


CREATE TABLE IF NOT EXISTS payroll.socialsecurityrules
(
    ruleid                  uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    organizationid          uuid NOT NULL,
    schemeid                uuid NOT NULL REFERENCES payroll.socialsecurityschemes(schemeid),
    rulename                varchar(100) NOT NULL,
    description             text,
    contributionbasis       varchar(20) DEFAULT 'gross'
                            CHECK (contributionbasis IN ('basic', 'gross', 'fixed', 'component')),
    employeedefaultpct      numeric(5,2) NOT NULL DEFAULT 0,
    employerdefaultpct      numeric(5,2) NOT NULL DEFAULT 0,
    employeefixedamount     numeric(15,2),
    employerfixedamount     numeric(15,2),
    minsalarylimit          numeric(15,2),
    maxsalarylimit          numeric(15,2),
    annualsalarycap         numeric(15,2),
    roundingmode            varchar(20) DEFAULT 'half_up'
                            CHECK (roundingmode IN ('half_up', 'half_even', 'floor', 'ceiling')),
    eligibilityconfig       jsonb DEFAULT '{}'::jsonb,
    withdrawalconfig        jsonb DEFAULT '{}'::jsonb,
    claimconfig             jsonb DEFAULT '{}'::jsonb,
    complianceconfig        jsonb DEFAULT '{}'::jsonb,
    isactive                boolean DEFAULT true,
    isdeleted               boolean DEFAULT false,
    effectivefrom           date NOT NULL DEFAULT current_date,
    effectiveto             date,
    createdat               timestamptz DEFAULT current_timestamp,
    updatedat               timestamptz DEFAULT current_timestamp,
    CONSTRAINT uq_ss_rule UNIQUE (organizationid, schemeid, rulename, effectivefrom),
    CONSTRAINT chk_ss_rule_dates CHECK (effectiveto IS NULL OR effectiveto >= effectivefrom),
    CONSTRAINT chk_ss_rule_pcts CHECK (employeedefaultpct BETWEEN 0 AND 100 AND employerdefaultpct BETWEEN 0 AND 100),
    CONSTRAINT chk_ss_rule_salary_range CHECK (
        (minsalarylimit IS NULL OR minsalarylimit >= 0)
        AND (maxsalarylimit IS NULL OR maxsalarylimit >= 0)
        AND (annualsalarycap IS NULL OR annualsalarycap >= 0)
        AND (maxsalarylimit IS NULL OR minsalarylimit IS NULL OR maxsalarylimit >= minsalarylimit)
    )
);

CREATE INDEX IF NOT EXISTS idx_ss_rules_org
    ON payroll.socialsecurityrules (organizationid, schemeid, isactive, isdeleted, effectivefrom, effectiveto);


CREATE TABLE IF NOT EXISTS payroll.socialsecurityrulecomponents
(
    id                  uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    organizationid      uuid NOT NULL,
    ruleid              uuid NOT NULL REFERENCES payroll.socialsecurityrules(ruleid) ON DELETE CASCADE,
    componentid         uuid NOT NULL,
    treatment           varchar(20) DEFAULT 'include'
                        CHECK (treatment IN ('include', 'exclude', 'cap_only', 'informational')),
    contributionweight  numeric(8,4) DEFAULT 1,
    capapplies          boolean DEFAULT false,
    isactive            boolean DEFAULT true,
    createdat           timestamptz DEFAULT current_timestamp,
    updatedat           timestamptz DEFAULT current_timestamp,
    CONSTRAINT uq_ss_rule_component UNIQUE (ruleid, componentid)
);

CREATE INDEX IF NOT EXISTS idx_ss_rule_components_org
    ON payroll.socialsecurityrulecomponents (organizationid, ruleid, componentid, isactive);

DO $$
BEGIN
    IF to_regclass('payroll.salarycomponents') IS NOT NULL THEN
        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conname = 'fk_ss_rule_component_salarycomponent'
              AND conrelid = 'payroll.socialsecurityrulecomponents'::regclass
        ) THEN
            EXECUTE '
                ALTER TABLE payroll.socialsecurityrulecomponents
                ADD CONSTRAINT fk_ss_rule_component_salarycomponent
                FOREIGN KEY (componentid)
                REFERENCES payroll.salarycomponents(componentid)
                ON UPDATE NO ACTION
                ON DELETE NO ACTION
                NOT VALID
            ';
        END IF;
    ELSE
        RAISE NOTICE 'Skipping FK fk_ss_rule_component_salarycomponent because payroll.salarycomponents does not exist.';
    END IF;
END $$;


-- =============================================================
-- B) EXTEND EXISTING CONFIG TABLE (BACKWARD-COMPATIBLE)
-- =============================================================

ALTER TABLE payroll.socialsecurityconfig
    ADD COLUMN IF NOT EXISTS jurisdictionid uuid,
    ADD COLUMN IF NOT EXISTS authorityid uuid,
    ADD COLUMN IF NOT EXISTS schemeid uuid,
    ADD COLUMN IF NOT EXISTS ruleid uuid,
    ADD COLUMN IF NOT EXISTS contributionbasis varchar(20) DEFAULT 'gross',
    ADD COLUMN IF NOT EXISTS employeefixedamount numeric(15,2),
    ADD COLUMN IF NOT EXISTS employerfixedamount numeric(15,2),
    ADD COLUMN IF NOT EXISTS minsalarylimit numeric(15,2),
    ADD COLUMN IF NOT EXISTS currency varchar(3),
    ADD COLUMN IF NOT EXISTS isstatutory boolean DEFAULT true,
    ADD COLUMN IF NOT EXISTS allowvoluntary boolean DEFAULT false,
    ADD COLUMN IF NOT EXISTS allowwithdrawal boolean DEFAULT true,
    ADD COLUMN IF NOT EXISTS metadata jsonb DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS legalreference varchar(255),
    ADD COLUMN IF NOT EXISTS governmentcode varchar(100),
    ADD COLUMN IF NOT EXISTS eligibilityconfig jsonb DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS remittanceconfig jsonb DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS claimconfig jsonb DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS complianceconfig jsonb DEFAULT '{}'::jsonb;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'chk_ssconfig_contributionbasis'
          AND conrelid = 'payroll.socialsecurityconfig'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecurityconfig
            ADD CONSTRAINT chk_ssconfig_contributionbasis
            CHECK (contributionbasis IN ('basic', 'gross', 'fixed', 'component')) NOT VALID;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'chk_ssconfig_salary_range'
          AND conrelid = 'payroll.socialsecurityconfig'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecurityconfig
            ADD CONSTRAINT chk_ssconfig_salary_range
            CHECK (
                (minsalarylimit IS NULL OR minsalarylimit >= 0)
                AND (maxsalarylimit IS NULL OR maxsalarylimit >= 0)
                AND (maxsalarylimit IS NULL OR minsalarylimit IS NULL OR maxsalarylimit >= minsalarylimit)
            ) NOT VALID;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_ssconfig_jurisdiction'
          AND conrelid = 'payroll.socialsecurityconfig'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecurityconfig
            ADD CONSTRAINT fk_ssconfig_jurisdiction
            FOREIGN KEY (jurisdictionid)
            REFERENCES payroll.socialsecurityjurisdictions(jurisdictionid)
            ON UPDATE NO ACTION
            ON DELETE SET NULL
            NOT VALID;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_ssconfig_authority'
          AND conrelid = 'payroll.socialsecurityconfig'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecurityconfig
            ADD CONSTRAINT fk_ssconfig_authority
            FOREIGN KEY (authorityid)
            REFERENCES payroll.socialsecurityauthorities(authorityid)
            ON UPDATE NO ACTION
            ON DELETE SET NULL
            NOT VALID;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_ssconfig_scheme'
          AND conrelid = 'payroll.socialsecurityconfig'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecurityconfig
            ADD CONSTRAINT fk_ssconfig_scheme
            FOREIGN KEY (schemeid)
            REFERENCES payroll.socialsecurityschemes(schemeid)
            ON UPDATE NO ACTION
            ON DELETE SET NULL
            NOT VALID;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_ssconfig_rule'
          AND conrelid = 'payroll.socialsecurityconfig'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecurityconfig
            ADD CONSTRAINT fk_ssconfig_rule
            FOREIGN KEY (ruleid)
            REFERENCES payroll.socialsecurityrules(ruleid)
            ON UPDATE NO ACTION
            ON DELETE SET NULL
            NOT VALID;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_ssconfig_org_scheme
    ON payroll.socialsecurityconfig (organizationid, schemeid, isactive, isdeleted);

CREATE INDEX IF NOT EXISTS idx_ssconfig_rule
    ON payroll.socialsecurityconfig (ruleid);


-- Seed default jurisdiction/authority/scheme per org for legacy rows.
INSERT INTO payroll.socialsecurityjurisdictions
(
    organizationid,
    jurisdictioncode,
    jurisdictionname,
    countrycode,
    currency,
    timezone,
    isdefault,
    isactive,
    isdeleted
)
SELECT DISTINCT
    sc.organizationid,
    'ORG-DEFAULT',
    'Organization Default Jurisdiction',
    'ZZ',
    COALESCE(NULLIF(o.currency, ''), 'USD'),
    'UTC',
    true,
    true,
    false
FROM payroll.socialsecurityconfig sc
LEFT JOIN auth.organizations o ON o.organizationid = sc.organizationid
WHERE sc.organizationid IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM payroll.socialsecurityjurisdictions j
      WHERE j.organizationid = sc.organizationid
        AND j.jurisdictioncode = 'ORG-DEFAULT'
  );

INSERT INTO payroll.socialsecurityauthorities
(
    organizationid,
    jurisdictionid,
    authoritycode,
    authorityname,
    remittancefrequency,
    filingmode,
    paymentmode,
    dueoffsetdays,
    isactive,
    isdeleted
)
SELECT
    j.organizationid,
    j.jurisdictionid,
    'DEFAULT-AUTH',
    'Default Social Security Authority',
    'monthly',
    'online',
    'bank_transfer',
    14,
    true,
    false
FROM payroll.socialsecurityjurisdictions j
WHERE j.jurisdictioncode = 'ORG-DEFAULT'
  AND NOT EXISTS (
      SELECT 1
      FROM payroll.socialsecurityauthorities a
      WHERE a.organizationid = j.organizationid
        AND a.jurisdictionid = j.jurisdictionid
        AND a.authoritycode = 'DEFAULT-AUTH'
  );

INSERT INTO payroll.socialsecurityschemes
(
    organizationid,
    jurisdictionid,
    authorityid,
    schemecode,
    schemename,
    schemetype,
    mandatorymode,
    employeemandatory,
    employermandatory,
    allowvoluntarytopup,
    allowcrossborder,
    legalreference,
    versionstartdate,
    isactive,
    isdeleted
)
SELECT
    j.organizationid,
    j.jurisdictionid,
    a.authorityid,
    'SS-DEFAULT',
    'Default Social Security Scheme',
    'social_insurance',
    'mandatory',
    true,
    true,
    false,
    false,
    NULL,
    current_date,
    true,
    false
FROM payroll.socialsecurityjurisdictions j
LEFT JOIN payroll.socialsecurityauthorities a
       ON a.organizationid = j.organizationid
      AND a.jurisdictionid = j.jurisdictionid
      AND a.authoritycode = 'DEFAULT-AUTH'
WHERE j.jurisdictioncode = 'ORG-DEFAULT'
  AND NOT EXISTS (
      SELECT 1
      FROM payroll.socialsecurityschemes s
      WHERE s.organizationid = j.organizationid
        AND s.schemecode = 'SS-DEFAULT'
        AND s.versionstartdate = current_date
  );

-- Seed rule entries from existing config rows (legacy -> global rule model).
INSERT INTO payroll.socialsecurityrules
(
    organizationid,
    schemeid,
    rulename,
    description,
    contributionbasis,
    employeedefaultpct,
    employerdefaultpct,
    employeefixedamount,
    employerfixedamount,
    minsalarylimit,
    maxsalarylimit,
    eligibilityconfig,
    withdrawalconfig,
    claimconfig,
    complianceconfig,
    isactive,
    isdeleted,
    effectivefrom,
    effectiveto,
    createdat,
    updatedat
)
SELECT
    sc.organizationid,
    s.schemeid,
    sc.configrulename,
    sc.description,
    COALESCE(sc.contributionbasis, 'gross'),
    COALESCE(sc.employeepercentage, 0),
    COALESCE(sc.employerpercentage, 0),
    sc.employeefixedamount,
    sc.employerfixedamount,
    sc.minsalarylimit,
    sc.maxsalarylimit,
    COALESCE(sc.eligibilityconfig, '{}'::jsonb),
    '{}'::jsonb,
    COALESCE(sc.claimconfig, '{}'::jsonb),
    COALESCE(sc.complianceconfig, '{}'::jsonb),
    COALESCE(sc.isactive, true),
    COALESCE(sc.isdeleted, false),
    COALESCE(sc.effectivefrom, current_date),
    sc.effectiveto,
    COALESCE(sc.createdat, current_timestamp),
    COALESCE(sc.updatedat, current_timestamp)
FROM payroll.socialsecurityconfig sc
JOIN payroll.socialsecurityschemes s
  ON s.organizationid = sc.organizationid
 AND s.schemecode = 'SS-DEFAULT'
WHERE sc.configrulename IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM payroll.socialsecurityrules sr
      WHERE sr.organizationid = sc.organizationid
        AND LOWER(sr.rulename) = LOWER(sc.configrulename)
        AND sr.effectivefrom = COALESCE(sc.effectivefrom, current_date)
  );

-- Backfill config links to jurisdiction/authority/scheme/rule.
UPDATE payroll.socialsecurityconfig sc
SET
    jurisdictionid = COALESCE(sc.jurisdictionid, j.jurisdictionid),
    authorityid = COALESCE(sc.authorityid, a.authorityid),
    schemeid = COALESCE(sc.schemeid, s.schemeid),
    ruleid = COALESCE(
        sc.ruleid,
        (
            SELECT sr.ruleid
            FROM payroll.socialsecurityrules sr
            WHERE sr.organizationid = sc.organizationid
              AND LOWER(sr.rulename) = LOWER(sc.configrulename)
              AND sr.effectivefrom = COALESCE(sc.effectivefrom, current_date)
            ORDER BY sr.updatedat DESC NULLS LAST, sr.createdat DESC NULLS LAST
            LIMIT 1
        )
    ),
    contributionbasis = COALESCE(sc.contributionbasis, 'gross'),
    currency = COALESCE(sc.currency, j.currency),
    updatedat = current_timestamp
FROM payroll.socialsecurityjurisdictions j
LEFT JOIN payroll.socialsecurityauthorities a
       ON a.organizationid = j.organizationid
      AND a.jurisdictionid = j.jurisdictionid
      AND a.authoritycode = 'DEFAULT-AUTH'
LEFT JOIN payroll.socialsecurityschemes s
       ON s.organizationid = j.organizationid
      AND s.jurisdictionid = j.jurisdictionid
      AND s.schemecode = 'SS-DEFAULT'
WHERE sc.organizationid = j.organizationid
  AND j.jurisdictioncode = 'ORG-DEFAULT';


-- =============================================================
-- C) EMPLOYEE LIFECYCLE TABLES (ENROLLMENT / REQUEST / CLAIM)
-- =============================================================

CREATE TABLE IF NOT EXISTS payroll.socialsecurityemployeeenrollments
(
    enrollmentid         uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    organizationid       uuid NOT NULL,
    employeeid           uuid NOT NULL REFERENCES employee.employees(employeeid),
    ruleid               uuid NOT NULL REFERENCES payroll.socialsecurityrules(ruleid),
    configid             uuid REFERENCES payroll.socialsecurityconfig(id),
    externalmemberid     varchar(100),
    enrollmenttype       varchar(20) DEFAULT 'auto_enrolled'
                         CHECK (enrollmenttype IN ('mandatory', 'voluntary', 'auto_enrolled', 'opt_in', 'opt_out', 'exempt')),
    enrollmentstatus     varchar(20) DEFAULT 'pending'
                         CHECK (enrollmentstatus IN ('pending', 'active', 'suspended', 'withdrawn', 'closed', 'rejected')),
    effectivedate        date NOT NULL DEFAULT current_date,
    enddate              date,
    payrollstartperiodid uuid REFERENCES payroll.payrollperiods(periodid),
    payrollendperiodid   uuid REFERENCES payroll.payrollperiods(periodid),
    employeecustompct    numeric(5,2),
    employercustompct    numeric(5,2),
    salarycapoverride    numeric(15,2),
    iscurrent            boolean DEFAULT true,
    metadata             jsonb DEFAULT '{}'::jsonb,
    approvedby           uuid,
    approvedat           timestamptz,
    createdby            uuid,
    isdeleted            boolean DEFAULT false,
    createdat            timestamptz DEFAULT current_timestamp,
    updatedat            timestamptz DEFAULT current_timestamp,
    CONSTRAINT chk_ss_enrollment_dates CHECK (enddate IS NULL OR enddate >= effectivedate),
    CONSTRAINT chk_ss_enrollment_pct CHECK (
        (employeecustompct IS NULL OR employeecustompct BETWEEN 0 AND 100)
        AND (employercustompct IS NULL OR employercustompct BETWEEN 0 AND 100)
    )
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_ss_enrollment_current
    ON payroll.socialsecurityemployeeenrollments (organizationid, employeeid, ruleid)
    WHERE iscurrent = true AND isdeleted = false;

CREATE INDEX IF NOT EXISTS idx_ss_enrollments_org_emp
    ON payroll.socialsecurityemployeeenrollments (organizationid, employeeid, enrollmentstatus, isdeleted);


CREATE TABLE IF NOT EXISTS payroll.socialsecurityrequests
(
    requestid            uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    organizationid       uuid NOT NULL,
    employeeid           uuid NOT NULL REFERENCES employee.employees(employeeid),
    enrollmentid         uuid REFERENCES payroll.socialsecurityemployeeenrollments(enrollmentid) ON DELETE SET NULL,
    ruleid               uuid REFERENCES payroll.socialsecurityrules(ruleid),
    configid             uuid REFERENCES payroll.socialsecurityconfig(id),
    requesttype          varchar(30) NOT NULL
                         CHECK (requesttype IN ('enrollment', 'update', 'withdrawal', 'suspension', 'reactivation', 'exemption', 'benefit_claim', 'correction')),
    requeststatus        varchar(20) DEFAULT 'pending'
                         CHECK (requeststatus IN ('pending', 'approved', 'rejected', 'cancelled', 'processing', 'completed')),
    requestedat          timestamptz DEFAULT current_timestamp,
    requestedby          uuid,
    requestedvalues      jsonb DEFAULT '{}'::jsonb,
    reason               text,
    remarks              text,
    approvedby           uuid,
    approvedat           timestamptz,
    rejectionreason      text,
    processedat          timestamptz,
    processedby          uuid,
    metadata             jsonb DEFAULT '{}'::jsonb,
    createdat            timestamptz DEFAULT current_timestamp,
    updatedat            timestamptz DEFAULT current_timestamp
);

CREATE INDEX IF NOT EXISTS idx_ss_requests_org_emp
    ON payroll.socialsecurityrequests (organizationid, employeeid, requesttype, requeststatus, requestedat);

CREATE INDEX IF NOT EXISTS idx_ss_requests_enrollment
    ON payroll.socialsecurityrequests (enrollmentid);


CREATE TABLE IF NOT EXISTS payroll.socialsecurityrequestdocuments
(
    documentid           uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    requestid            uuid NOT NULL REFERENCES payroll.socialsecurityrequests(requestid) ON DELETE CASCADE,
    organizationid       uuid NOT NULL,
    employeeid           uuid NOT NULL REFERENCES employee.employees(employeeid),
    documenttype         varchar(100) NOT NULL,
    documentname         varchar(255) NOT NULL,
    fileurl              varchar(500) NOT NULL,
    mimetype             varchar(100),
    filesize             bigint,
    issuer               varchar(150),
    issueddate           date,
    expirydate           date,
    verifiedstatus       varchar(20) DEFAULT 'pending'
                         CHECK (verifiedstatus IN ('pending', 'verified', 'rejected', 'expired')),
    verifiedby           uuid,
    verifiedat           timestamptz,
    createdat            timestamptz DEFAULT current_timestamp,
    CONSTRAINT chk_ss_requestdoc_dates CHECK (expirydate IS NULL OR issueddate IS NULL OR expirydate >= issueddate)
);

CREATE INDEX IF NOT EXISTS idx_ss_request_docs_request
    ON payroll.socialsecurityrequestdocuments (requestid, verifiedstatus);


CREATE TABLE IF NOT EXISTS payroll.socialsecurityclaims
(
    claimid              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    organizationid       uuid NOT NULL,
    employeeid           uuid NOT NULL REFERENCES employee.employees(employeeid),
    enrollmentid         uuid REFERENCES payroll.socialsecurityemployeeenrollments(enrollmentid),
    requestid            uuid REFERENCES payroll.socialsecurityrequests(requestid),
    claimtype            varchar(30) NOT NULL
                         CHECK (claimtype IN ('retirement', 'disability', 'survivor', 'medical', 'unemployment', 'maternity', 'injury', 'other')),
    claimstatus          varchar(20) DEFAULT 'draft'
                         CHECK (claimstatus IN ('draft', 'submitted', 'under_review', 'approved', 'rejected', 'paid', 'closed')),
    claimdate            date NOT NULL DEFAULT current_date,
    incidentdate         date,
    periodfrom           date,
    periodto             date,
    currency             varchar(3) DEFAULT 'USD',
    claimedamount        numeric(15,2),
    approvedamount       numeric(15,2),
    paidamount           numeric(15,2),
    paymentreference     varchar(120),
    paymentdate          date,
    authorityreference   varchar(120),
    decisionnotes        text,
    rejectionreason      text,
    metadata             jsonb DEFAULT '{}'::jsonb,
    createdby            uuid,
    createdat            timestamptz DEFAULT current_timestamp,
    updatedat            timestamptz DEFAULT current_timestamp,
    CONSTRAINT chk_ss_claim_period CHECK (periodto IS NULL OR periodfrom IS NULL OR periodto >= periodfrom)
);

CREATE INDEX IF NOT EXISTS idx_ss_claims_org_emp
    ON payroll.socialsecurityclaims (organizationid, employeeid, claimstatus, claimdate);


-- =============================================================
-- D) EXTEND EXISTING TRANSACTION TABLE (BACKWARD-COMPATIBLE)
-- =============================================================

ALTER TABLE payroll.socialsecuritytransactions
    ADD COLUMN IF NOT EXISTS ruleid uuid,
    ADD COLUMN IF NOT EXISTS schemeid uuid,
    ADD COLUMN IF NOT EXISTS jurisdictionid uuid,
    ADD COLUMN IF NOT EXISTS authorityid uuid,
    ADD COLUMN IF NOT EXISTS enrollmentid uuid,
    ADD COLUMN IF NOT EXISTS requestid uuid,
    ADD COLUMN IF NOT EXISTS payrolldataid uuid,
    ADD COLUMN IF NOT EXISTS remittanceitemid uuid,
    ADD COLUMN IF NOT EXISTS contributionbasis varchar(20) DEFAULT 'gross',
    ADD COLUMN IF NOT EXISTS contributionbase numeric(15,2),
    ADD COLUMN IF NOT EXISTS employeerate numeric(5,2),
    ADD COLUMN IF NOT EXISTS employerrate numeric(5,2),
    ADD COLUMN IF NOT EXISTS employeefixedamount numeric(15,2),
    ADD COLUMN IF NOT EXISTS employerfixedamount numeric(15,2),
    ADD COLUMN IF NOT EXISTS transactiontype varchar(20) DEFAULT 'monthly',
    ADD COLUMN IF NOT EXISTS sourcetype varchar(20) DEFAULT 'system',
    ADD COLUMN IF NOT EXISTS ismanualoverride boolean DEFAULT false,
    ADD COLUMN IF NOT EXISTS overridereason text,
    ADD COLUMN IF NOT EXISTS overriddenby uuid,
    ADD COLUMN IF NOT EXISTS overriddenat timestamptz,
    ADD COLUMN IF NOT EXISTS legalperiodstart date,
    ADD COLUMN IF NOT EXISTS legalperiodend date,
    ADD COLUMN IF NOT EXISTS referencecode varchar(100),
    ADD COLUMN IF NOT EXISTS transactionpayload jsonb DEFAULT '{}'::jsonb;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'chk_sstrx_contributionbasis'
          AND conrelid = 'payroll.socialsecuritytransactions'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecuritytransactions
            ADD CONSTRAINT chk_sstrx_contributionbasis
            CHECK (contributionbasis IN ('basic', 'gross', 'fixed', 'component')) NOT VALID;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'chk_sstrx_transactiontype'
          AND conrelid = 'payroll.socialsecuritytransactions'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecuritytransactions
            ADD CONSTRAINT chk_sstrx_transactiontype
            CHECK (transactiontype IN ('monthly', 'adjustment', 'arrears', 'refund', 'settlement', 'manual')) NOT VALID;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'chk_sstrx_sourcetype'
          AND conrelid = 'payroll.socialsecuritytransactions'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecuritytransactions
            ADD CONSTRAINT chk_sstrx_sourcetype
            CHECK (sourcetype IN ('system', 'manual', 'import')) NOT VALID;
    END IF;
END $$;

-- Backfill extended transaction fields from current config rows.
UPDATE payroll.socialsecuritytransactions st
SET
    contributionbase = COALESCE(st.contributionbase, st.salarycapped, st.actualsalary, 0),
    employeerate = COALESCE(st.employeerate, sc.employeepercentage, 0),
    employerrate = COALESCE(st.employerrate, sc.employerpercentage, 0),
    employeefixedamount = COALESCE(st.employeefixedamount, sc.employeefixedamount),
    employerfixedamount = COALESCE(st.employerfixedamount, sc.employerfixedamount),
    contributionbasis = COALESCE(st.contributionbasis, sc.contributionbasis, 'gross'),
    ruleid = COALESCE(st.ruleid, sc.ruleid),
    schemeid = COALESCE(st.schemeid, sc.schemeid),
    jurisdictionid = COALESCE(st.jurisdictionid, sc.jurisdictionid),
    authorityid = COALESCE(st.authorityid, sc.authorityid),
    referencecode = COALESCE(NULLIF(st.referencecode, ''), 'SS-' || UPPER(SUBSTRING(REPLACE(st.id::text, '-', ''), 1, 10))),
    updatedat = current_timestamp
FROM payroll.socialsecurityconfig sc
WHERE st.configid = sc.id;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_sstrx_period'
          AND conrelid = 'payroll.socialsecuritytransactions'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecuritytransactions
            ADD CONSTRAINT fk_sstrx_period
            FOREIGN KEY (periodid)
            REFERENCES payroll.payrollperiods(periodid)
            ON UPDATE NO ACTION
            ON DELETE NO ACTION
            NOT VALID;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_sstrx_employee'
          AND conrelid = 'payroll.socialsecuritytransactions'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecuritytransactions
            ADD CONSTRAINT fk_sstrx_employee
            FOREIGN KEY (employeeid)
            REFERENCES employee.employees(employeeid)
            ON UPDATE NO ACTION
            ON DELETE NO ACTION
            NOT VALID;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_sstrx_rule'
          AND conrelid = 'payroll.socialsecuritytransactions'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecuritytransactions
            ADD CONSTRAINT fk_sstrx_rule
            FOREIGN KEY (ruleid)
            REFERENCES payroll.socialsecurityrules(ruleid)
            ON UPDATE NO ACTION
            ON DELETE SET NULL
            NOT VALID;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_sstrx_scheme'
          AND conrelid = 'payroll.socialsecuritytransactions'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecuritytransactions
            ADD CONSTRAINT fk_sstrx_scheme
            FOREIGN KEY (schemeid)
            REFERENCES payroll.socialsecurityschemes(schemeid)
            ON UPDATE NO ACTION
            ON DELETE SET NULL
            NOT VALID;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_sstrx_jurisdiction'
          AND conrelid = 'payroll.socialsecuritytransactions'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecuritytransactions
            ADD CONSTRAINT fk_sstrx_jurisdiction
            FOREIGN KEY (jurisdictionid)
            REFERENCES payroll.socialsecurityjurisdictions(jurisdictionid)
            ON UPDATE NO ACTION
            ON DELETE SET NULL
            NOT VALID;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_sstrx_authority'
          AND conrelid = 'payroll.socialsecuritytransactions'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecuritytransactions
            ADD CONSTRAINT fk_sstrx_authority
            FOREIGN KEY (authorityid)
            REFERENCES payroll.socialsecurityauthorities(authorityid)
            ON UPDATE NO ACTION
            ON DELETE SET NULL
            NOT VALID;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_sstrx_enrollment'
          AND conrelid = 'payroll.socialsecuritytransactions'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecuritytransactions
            ADD CONSTRAINT fk_sstrx_enrollment
            FOREIGN KEY (enrollmentid)
            REFERENCES payroll.socialsecurityemployeeenrollments(enrollmentid)
            ON UPDATE NO ACTION
            ON DELETE SET NULL
            NOT VALID;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_sstrx_request'
          AND conrelid = 'payroll.socialsecuritytransactions'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecuritytransactions
            ADD CONSTRAINT fk_sstrx_request
            FOREIGN KEY (requestid)
            REFERENCES payroll.socialsecurityrequests(requestid)
            ON UPDATE NO ACTION
            ON DELETE SET NULL
            NOT VALID;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_sstrx_payrolldata'
          AND conrelid = 'payroll.socialsecuritytransactions'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecuritytransactions
            ADD CONSTRAINT fk_sstrx_payrolldata
            FOREIGN KEY (payrolldataid)
            REFERENCES payroll.payrolldata(dataid)
            ON UPDATE NO ACTION
            ON DELETE SET NULL
            NOT VALID;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_sstrx_org_rule_period
    ON payroll.socialsecuritytransactions (organizationid, ruleid, periodid);

CREATE INDEX IF NOT EXISTS idx_sstrx_org_scheme_period
    ON payroll.socialsecuritytransactions (organizationid, schemeid, periodid);

CREATE INDEX IF NOT EXISTS idx_sstrx_org_emp_period
    ON payroll.socialsecuritytransactions (organizationid, employeeid, periodid);

CREATE INDEX IF NOT EXISTS idx_sstrx_request
    ON payroll.socialsecuritytransactions (requestid);

CREATE INDEX IF NOT EXISTS idx_sstrx_enrollment
    ON payroll.socialsecuritytransactions (enrollmentid);


-- =============================================================
-- E) MULTI-LINE CONTRIBUTIONS + REMITTANCE TRACKING
-- =============================================================

CREATE TABLE IF NOT EXISTS payroll.socialsecuritytransactionlines
(
    lineid               uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    transactionid        uuid NOT NULL REFERENCES payroll.socialsecuritytransactions(id) ON DELETE CASCADE,
    organizationid       uuid NOT NULL,
    employeeid           uuid NOT NULL REFERENCES employee.employees(employeeid),
    periodid             uuid NOT NULL REFERENCES payroll.payrollperiods(periodid),
    ruleid               uuid REFERENCES payroll.socialsecurityrules(ruleid),
    schemeid             uuid REFERENCES payroll.socialsecurityschemes(schemeid),
    componentid          uuid,
    basisamount          numeric(15,2) NOT NULL DEFAULT 0,
    employeerate         numeric(5,2) DEFAULT 0,
    employerrate         numeric(5,2) DEFAULT 0,
    employeeamount       numeric(15,2) NOT NULL DEFAULT 0,
    employeramount       numeric(15,2) NOT NULL DEFAULT 0,
    totalamount          numeric(15,2) GENERATED ALWAYS AS (employeeamount + employeramount) STORED,
    linekind             varchar(20) DEFAULT 'regular'
                         CHECK (linekind IN ('regular', 'adjustment', 'arrears', 'refund')),
    sourcetype           varchar(20) DEFAULT 'system'
                         CHECK (sourcetype IN ('system', 'manual', 'import')),
    isretro              boolean DEFAULT false,
    notes                text,
    createdat            timestamptz DEFAULT current_timestamp,
    updatedat            timestamptz DEFAULT current_timestamp
);

CREATE INDEX IF NOT EXISTS idx_sstrx_lines_txn
    ON payroll.socialsecuritytransactionlines (transactionid);

CREATE INDEX IF NOT EXISTS idx_sstrx_lines_org_emp_period
    ON payroll.socialsecuritytransactionlines (organizationid, employeeid, periodid);

CREATE INDEX IF NOT EXISTS idx_sstrx_lines_rule_scheme
    ON payroll.socialsecuritytransactionlines (ruleid, schemeid);

DO $$
BEGIN
    IF to_regclass('payroll.salarycomponents') IS NOT NULL THEN
        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conname = 'fk_sstrx_lines_salarycomponent'
              AND conrelid = 'payroll.socialsecuritytransactionlines'::regclass
        ) THEN
            EXECUTE '
                ALTER TABLE payroll.socialsecuritytransactionlines
                ADD CONSTRAINT fk_sstrx_lines_salarycomponent
                FOREIGN KEY (componentid)
                REFERENCES payroll.salarycomponents(componentid)
                ON UPDATE NO ACTION
                ON DELETE NO ACTION
                NOT VALID
            ';
        END IF;
    ELSE
        RAISE NOTICE 'Skipping FK fk_sstrx_lines_salarycomponent because payroll.salarycomponents does not exist.';
    END IF;
END $$;

-- Backfill one line per existing transaction (safe/idempotent).
INSERT INTO payroll.socialsecuritytransactionlines
(
    transactionid,
    organizationid,
    employeeid,
    periodid,
    ruleid,
    schemeid,
    basisamount,
    employeerate,
    employerrate,
    employeeamount,
    employeramount,
    linekind,
    sourcetype,
    isretro,
    notes
)
SELECT
    st.id,
    st.organizationid,
    st.employeeid,
    st.periodid,
    st.ruleid,
    st.schemeid,
    COALESCE(st.contributionbase, st.salarycapped, st.actualsalary, 0),
    COALESCE(st.employeerate, 0),
    COALESCE(st.employerrate, 0),
    COALESCE(st.employeeamount, 0),
    COALESCE(st.employeramount, 0),
    'regular',
    COALESCE(st.sourcetype, 'system'),
    false,
    'Backfilled from legacy socialsecuritytransactions'
FROM payroll.socialsecuritytransactions st
WHERE NOT EXISTS (
    SELECT 1
    FROM payroll.socialsecuritytransactionlines l
    WHERE l.transactionid = st.id
);


CREATE TABLE IF NOT EXISTS payroll.socialsecurityremittances
(
    remittanceid         uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    organizationid       uuid NOT NULL,
    periodid             uuid NOT NULL REFERENCES payroll.payrollperiods(periodid),
    jurisdictionid       uuid REFERENCES payroll.socialsecurityjurisdictions(jurisdictionid),
    authorityid          uuid REFERENCES payroll.socialsecurityauthorities(authorityid),
    schemeid             uuid REFERENCES payroll.socialsecurityschemes(schemeid),
    duedate              date,
    remittancedate       date,
    filingreference      varchar(100),
    paymentreference     varchar(100),
    employeetotal        numeric(15,2) NOT NULL DEFAULT 0,
    employertotal        numeric(15,2) NOT NULL DEFAULT 0,
    totalamount          numeric(15,2) GENERATED ALWAYS AS (employeetotal + employertotal) STORED,
    penaltyamount        numeric(15,2) DEFAULT 0,
    interestamount       numeric(15,2) DEFAULT 0,
    remittancestatus     varchar(20) DEFAULT 'draft'
                         CHECK (remittancestatus IN ('draft', 'prepared', 'filed', 'paid', 'partially_paid', 'failed', 'cancelled')),
    filingpayload        jsonb DEFAULT '{}'::jsonb,
    filingresponse       jsonb DEFAULT '{}'::jsonb,
    createdby            uuid,
    createdat            timestamptz DEFAULT current_timestamp,
    updatedat            timestamptz DEFAULT current_timestamp
);

CREATE INDEX IF NOT EXISTS idx_ss_remit_org_period
    ON payroll.socialsecurityremittances (organizationid, periodid, remittancestatus);

CREATE UNIQUE INDEX IF NOT EXISTS uq_ss_remit_period_scheme
    ON payroll.socialsecurityremittances
    (
        organizationid,
        periodid,
        COALESCE(schemeid, '00000000-0000-0000-0000-000000000000'::uuid)
    );


CREATE TABLE IF NOT EXISTS payroll.socialsecurityremittanceitems
(
    itemid               uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    remittanceid         uuid NOT NULL REFERENCES payroll.socialsecurityremittances(remittanceid) ON DELETE CASCADE,
    transactionid        uuid REFERENCES payroll.socialsecuritytransactions(id),
    transactionlineid    uuid REFERENCES payroll.socialsecuritytransactionlines(lineid),
    organizationid       uuid NOT NULL,
    employeeid           uuid NOT NULL REFERENCES employee.employees(employeeid),
    periodid             uuid NOT NULL REFERENCES payroll.payrollperiods(periodid),
    employeeamount       numeric(15,2) NOT NULL DEFAULT 0,
    employeramount       numeric(15,2) NOT NULL DEFAULT 0,
    totalamount          numeric(15,2) GENERATED ALWAYS AS (employeeamount + employeramount) STORED,
    itemstatus           varchar(20) DEFAULT 'included'
                         CHECK (itemstatus IN ('included', 'excluded', 'adjusted', 'paid', 'reversed')),
    notes                text,
    createdat            timestamptz DEFAULT current_timestamp,
    updatedat            timestamptz DEFAULT current_timestamp
);

CREATE INDEX IF NOT EXISTS idx_ss_remit_items_remittance
    ON payroll.socialsecurityremittanceitems (remittanceid, itemstatus);

CREATE INDEX IF NOT EXISTS idx_ss_remit_items_txn
    ON payroll.socialsecurityremittanceitems (transactionid, transactionlineid);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_sstrx_remittanceitem'
          AND conrelid = 'payroll.socialsecuritytransactions'::regclass
    ) THEN
        ALTER TABLE payroll.socialsecuritytransactions
            ADD CONSTRAINT fk_sstrx_remittanceitem
            FOREIGN KEY (remittanceitemid)
            REFERENCES payroll.socialsecurityremittanceitems(itemid)
            ON UPDATE NO ACTION
            ON DELETE SET NULL
            NOT VALID;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_sstrx_remittanceitem
    ON payroll.socialsecuritytransactions (remittanceitemid);


-- =============================================================
-- F) SYNC FUNCTIONS FOR PAYROLL INTEGRATION
-- =============================================================

-- Keep parent transaction totals in sync when line rows change.
CREATE OR REPLACE FUNCTION payroll.fn_sync_ss_transaction_totals()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_transactionid      uuid;
    v_basis_total        numeric(15,2);
    v_employee_total     numeric(15,2);
    v_employer_total     numeric(15,2);
BEGIN
    v_transactionid := COALESCE(NEW.transactionid, OLD.transactionid);

    SELECT
        COALESCE(SUM(basisamount), 0),
        COALESCE(SUM(employeeamount), 0),
        COALESCE(SUM(employeramount), 0)
    INTO
        v_basis_total,
        v_employee_total,
        v_employer_total
    FROM payroll.socialsecuritytransactionlines
    WHERE transactionid = v_transactionid;

    UPDATE payroll.socialsecuritytransactions
    SET
        contributionbase = v_basis_total,
        salarycapped = COALESCE(salarycapped, v_basis_total),
        employeeamount = v_employee_total,
        employeramount = v_employer_total,
        updatedat = current_timestamp
    WHERE id = v_transactionid;

    RETURN COALESCE(NEW, OLD);
END;
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_trigger
        WHERE tgname = 'trg_sync_ss_transaction_totals'
          AND tgrelid = 'payroll.socialsecuritytransactionlines'::regclass
    ) THEN
        CREATE TRIGGER trg_sync_ss_transaction_totals
        AFTER INSERT OR UPDATE OR DELETE
        ON payroll.socialsecuritytransactionlines
        FOR EACH ROW
        EXECUTE FUNCTION payroll.fn_sync_ss_transaction_totals();
    END IF;
END $$;


-- Keep payroll.payrolldata social security amounts synchronized.
CREATE OR REPLACE FUNCTION payroll.fn_sync_payrolldata_socialsecurity()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_organizationid     uuid;
    v_employeeid         uuid;
    v_periodid           uuid;
    v_employee_total     numeric(15,2);
    v_employer_total     numeric(15,2);
BEGIN
    v_organizationid := COALESCE(NEW.organizationid, OLD.organizationid);
    v_employeeid := COALESCE(NEW.employeeid, OLD.employeeid);
    v_periodid := COALESCE(NEW.periodid, OLD.periodid);

    SELECT
        COALESCE(SUM(employeeamount), 0),
        COALESCE(SUM(employeramount), 0)
    INTO
        v_employee_total,
        v_employer_total
    FROM payroll.socialsecuritytransactions
    WHERE organizationid = v_organizationid
      AND employeeid = v_employeeid
      AND periodid = v_periodid;

    UPDATE payroll.payrolldata pd
    SET
        socialsecurity = v_employee_total,
        ssemployer = v_employer_total,
        updatedat = current_timestamp
    WHERE pd.organizationid = v_organizationid
      AND pd.employeeid = v_employeeid
      AND pd.periodid = v_periodid
      AND pd.versionnumber = (
          SELECT MAX(p2.versionnumber)
          FROM payroll.payrolldata p2
          WHERE p2.organizationid = v_organizationid
            AND p2.employeeid = v_employeeid
            AND p2.periodid = v_periodid
      );

    RETURN COALESCE(NEW, OLD);
END;
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_trigger
        WHERE tgname = 'trg_sync_payrolldata_socialsecurity'
          AND tgrelid = 'payroll.socialsecuritytransactions'::regclass
    ) THEN
        CREATE TRIGGER trg_sync_payrolldata_socialsecurity
        AFTER INSERT OR UPDATE OR DELETE
        ON payroll.socialsecuritytransactions
        FOR EACH ROW
        EXECUTE FUNCTION payroll.fn_sync_payrolldata_socialsecurity();
    END IF;
END $$;

-- One-time backfill to align current payrolldata totals with current transactions.
WITH ss_totals AS
(
    SELECT
        organizationid,
        employeeid,
        periodid,
        COALESCE(SUM(employeeamount), 0) AS employee_total,
        COALESCE(SUM(employeramount), 0) AS employer_total
    FROM payroll.socialsecuritytransactions
    GROUP BY organizationid, employeeid, periodid
)
UPDATE payroll.payrolldata pd
SET
    socialsecurity = ss.employee_total,
    ssemployer = ss.employer_total,
    updatedat = current_timestamp
FROM ss_totals ss
WHERE pd.organizationid = ss.organizationid
  AND pd.employeeid = ss.employeeid
  AND pd.periodid = ss.periodid
  AND pd.versionnumber = (
      SELECT MAX(p2.versionnumber)
      FROM payroll.payrolldata p2
      WHERE p2.organizationid = ss.organizationid
        AND p2.employeeid = ss.employeeid
        AND p2.periodid = ss.periodid
  );

-- =============================================================
-- END OF SCRIPT
-- =============================================================
