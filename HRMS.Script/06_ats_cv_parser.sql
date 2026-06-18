-- =============================================================
-- ATS QUESTION BANK + CV PARSER SCHEMA
-- =============================================================
-- Purpose:
-- 1) Add mandatoryskills to jobopenings for AI match scoring
-- 2) Add passedknockout to jobapplications for knockout tracking
-- 3) Create Question Bank tables (categories, questions, job mapping, candidate answers)
-- 4) Create parsedresumes table for AI-extracted CV data
-- 5) Add indexes for performance
-- All statements are idempotent (safe to re-run).

-- =========================
-- ALTER: jobs.jobopenings
-- =========================
ALTER TABLE jobs.jobopenings
    ADD COLUMN IF NOT EXISTS mandatoryskills jsonb;

COMMENT ON COLUMN jobs.jobopenings.mandatoryskills IS 'JSON array of mandatory skill strings for AI match scoring, e.g. ["Angular", ".NET Core", "SQL"]';

-- =========================
-- ALTER: jobs.jobapplications
-- =========================
ALTER TABLE jobs.jobapplications
    ADD COLUMN IF NOT EXISTS passedknockout boolean;

COMMENT ON COLUMN jobs.jobapplications.passedknockout IS 'Whether candidate passed all knockout questions. NULL = not yet evaluated.';

-- =========================
-- TABLE: jobs.questioncategories
-- =========================
CREATE TABLE IF NOT EXISTS jobs.questioncategories (
    categoryid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    organizationid uuid NOT NULL,
    name character varying(200) NOT NULL,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone,
    isdeleted boolean DEFAULT false
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'questioncategories_pkey'
          AND conrelid = 'jobs.questioncategories'::regclass
    ) THEN
        ALTER TABLE ONLY jobs.questioncategories
            ADD CONSTRAINT questioncategories_pkey PRIMARY KEY (categoryid);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_questioncategories_org
    ON jobs.questioncategories (organizationid);

-- =========================
-- TABLE: jobs.questions
-- =========================
CREATE TABLE IF NOT EXISTS jobs.questions (
    questionid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    categoryid uuid NOT NULL,
    organizationid uuid NOT NULL,
    questiontext text NOT NULL,
    questiontype character varying(50) NOT NULL DEFAULT 'Text',
    options jsonb,
    isknockout boolean DEFAULT false,
    expectedanswer text,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone,
    isdeleted boolean DEFAULT false
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'questions_pkey'
          AND conrelid = 'jobs.questions'::regclass
    ) THEN
        ALTER TABLE ONLY jobs.questions
            ADD CONSTRAINT questions_pkey PRIMARY KEY (questionid);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_questions_category'
          AND conrelid = 'jobs.questions'::regclass
    ) THEN
        ALTER TABLE jobs.questions
            ADD CONSTRAINT fk_questions_category
            FOREIGN KEY (categoryid) REFERENCES jobs.questioncategories(categoryid);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_questions_category
    ON jobs.questions (categoryid);

CREATE INDEX IF NOT EXISTS idx_questions_org
    ON jobs.questions (organizationid);

-- =========================
-- TABLE: jobs.jobquestions (Mapping: Job <-> Question)
-- =========================
CREATE TABLE IF NOT EXISTS jobs.jobquestions (
    jobquestionid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    jobid uuid NOT NULL,
    questionid uuid NOT NULL,
    isrequired boolean DEFAULT true,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'jobquestions_pkey'
          AND conrelid = 'jobs.jobquestions'::regclass
    ) THEN
        ALTER TABLE ONLY jobs.jobquestions
            ADD CONSTRAINT jobquestions_pkey PRIMARY KEY (jobquestionid);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_jobquestions_job'
          AND conrelid = 'jobs.jobquestions'::regclass
    ) THEN
        ALTER TABLE jobs.jobquestions
            ADD CONSTRAINT fk_jobquestions_job
            FOREIGN KEY (jobid) REFERENCES jobs.jobopenings(jobid) ON DELETE CASCADE;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_jobquestions_question'
          AND conrelid = 'jobs.jobquestions'::regclass
    ) THEN
        ALTER TABLE jobs.jobquestions
            ADD CONSTRAINT fk_jobquestions_question
            FOREIGN KEY (questionid) REFERENCES jobs.questions(questionid) ON DELETE CASCADE;
    END IF;
END $$;

-- Prevent duplicate assignment of same question to same job
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'uq_jobquestions_job_question'
          AND conrelid = 'jobs.jobquestions'::regclass
    ) THEN
        ALTER TABLE jobs.jobquestions
            ADD CONSTRAINT uq_jobquestions_job_question UNIQUE (jobid, questionid);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_jobquestions_job
    ON jobs.jobquestions (jobid);

CREATE INDEX IF NOT EXISTS idx_jobquestions_question
    ON jobs.jobquestions (questionid);

-- =========================
-- TABLE: jobs.candidateanswers
-- =========================
CREATE TABLE IF NOT EXISTS jobs.candidateanswers (
    answerid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    jobapplyid uuid NOT NULL,
    questionid uuid NOT NULL,
    answertext text,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'candidateanswers_pkey'
          AND conrelid = 'jobs.candidateanswers'::regclass
    ) THEN
        ALTER TABLE ONLY jobs.candidateanswers
            ADD CONSTRAINT candidateanswers_pkey PRIMARY KEY (answerid);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_candidateanswers_application'
          AND conrelid = 'jobs.candidateanswers'::regclass
    ) THEN
        ALTER TABLE jobs.candidateanswers
            ADD CONSTRAINT fk_candidateanswers_application
            FOREIGN KEY (jobapplyid) REFERENCES jobs.jobapplications(jobapplyid) ON DELETE CASCADE;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_candidateanswers_question'
          AND conrelid = 'jobs.candidateanswers'::regclass
    ) THEN
        ALTER TABLE jobs.candidateanswers
            ADD CONSTRAINT fk_candidateanswers_question
            FOREIGN KEY (questionid) REFERENCES jobs.questions(questionid);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_candidateanswers_jobapply
    ON jobs.candidateanswers (jobapplyid);

CREATE INDEX IF NOT EXISTS idx_candidateanswers_question
    ON jobs.candidateanswers (questionid);

-- =========================
-- TABLE: jobs.parsedresumes
-- =========================
CREATE TABLE IF NOT EXISTS jobs.parsedresumes (
    parsedresumeid uuid DEFAULT public.uuid_generate_v4() NOT NULL,
    jobapplyid uuid NOT NULL,
    contactdetails jsonb,
    skills jsonb,
    workexperience jsonb,
    education jsonb,
    rawtext text,
    parsestatus character varying(50) DEFAULT 'Pending',
    parseerror text,
    createdat timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updatedat timestamp with time zone
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'parsedresumes_pkey'
          AND conrelid = 'jobs.parsedresumes'::regclass
    ) THEN
        ALTER TABLE ONLY jobs.parsedresumes
            ADD CONSTRAINT parsedresumes_pkey PRIMARY KEY (parsedresumeid);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_parsedresumes_application'
          AND conrelid = 'jobs.parsedresumes'::regclass
    ) THEN
        ALTER TABLE jobs.parsedresumes
            ADD CONSTRAINT fk_parsedresumes_application
            FOREIGN KEY (jobapplyid) REFERENCES jobs.jobapplications(jobapplyid) ON DELETE CASCADE;
    END IF;
END $$;

-- One parsed resume per application
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'uq_parsedresumes_jobapply'
          AND conrelid = 'jobs.parsedresumes'::regclass
    ) THEN
        ALTER TABLE jobs.parsedresumes
            ADD CONSTRAINT uq_parsedresumes_jobapply UNIQUE (jobapplyid);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_parsedresumes_jobapply
    ON jobs.parsedresumes (jobapplyid);

CREATE INDEX IF NOT EXISTS idx_parsedresumes_status
    ON jobs.parsedresumes (parsestatus);

COMMENT ON COLUMN jobs.parsedresumes.contactdetails IS 'AI-extracted contact info: {"name":"...","email":"...","phone":"...","linkedin":"..."}';
COMMENT ON COLUMN jobs.parsedresumes.skills IS 'AI-extracted skills as JSON array: ["Angular", "SQL", "Docker"]';
COMMENT ON COLUMN jobs.parsedresumes.workexperience IS 'AI-extracted work history as JSON array: [{"company":"...","title":"...","startDate":"...","endDate":"..."}]';
COMMENT ON COLUMN jobs.parsedresumes.education IS 'AI-extracted education as JSON array: [{"institution":"...","degree":"...","year":"..."}]';
COMMENT ON COLUMN jobs.parsedresumes.rawtext IS 'Raw text extracted from PDF/DOCX before AI processing';
COMMENT ON COLUMN jobs.parsedresumes.parsestatus IS 'Pending | Processing | Completed | Failed';
