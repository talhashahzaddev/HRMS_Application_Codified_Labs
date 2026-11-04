-- ============================================================
-- USER INVITATIONS TABLE (idempotent — safe to re-run)
-- Platform Admin can invite users who set up their own password
-- Supports new users without organizations (org name stored for later)
-- ============================================================

BEGIN;

-- 1. Create table only if it doesn't already exist
CREATE TABLE IF NOT EXISTS auth.userinvitations (
    invitationid     UUID DEFAULT public.uuid_generate_v4() NOT NULL,
    userid           UUID NOT NULL,
    token            VARCHAR(128) NOT NULL,
    expiresat        TIMESTAMP WITH TIME ZONE NOT NULL,
    used             BOOLEAN DEFAULT false NOT NULL,
    organizationname VARCHAR(200) NULL,
    createdat        TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updatedat        TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP NOT NULL,
    CONSTRAINT pk_userinvitations PRIMARY KEY (invitationid),
    CONSTRAINT fk_userinvitations_user FOREIGN KEY (userid)
        REFERENCES auth.users(userid) ON DELETE CASCADE,
    CONSTRAINT uq_userinvitations_token UNIQUE (token)
);

-- 2. Add organizationname column if the table existed but it was missing
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'auth'
          AND table_name   = 'userinvitations'
          AND column_name  = 'organizationname'
    ) THEN
        ALTER TABLE auth.userinvitations
            ADD COLUMN organizationname VARCHAR(200) NULL;
        RAISE NOTICE 'Added column organizationname to auth.userinvitations';
    END IF;
END $$;

-- 3. Create indexes only if they don't already exist
CREATE INDEX IF NOT EXISTS idx_userinvitations_token    ON auth.userinvitations(token);
CREATE INDEX IF NOT EXISTS idx_userinvitations_userid   ON auth.userinvitations(userid);

COMMIT;
