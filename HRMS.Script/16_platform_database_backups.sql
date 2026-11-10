-- ============================================================
-- PLATFORM DATABASE BACKUPS (idempotent — safe to re-run)
-- Platform Admin can take a database backup at any time and
-- schedule automatic backups (every N days at a chosen time).
-- The API also creates these tables on first use if they are missing.
-- ============================================================

BEGIN;

CREATE SCHEMA IF NOT EXISTS admin;

-- Single-row schedule settings (id is always 1).
CREATE TABLE IF NOT EXISTS admin.backup_settings (
    id               SMALLINT    NOT NULL DEFAULT 1,
    is_enabled       BOOLEAN     NOT NULL DEFAULT false,
    frequency_days   INT         NOT NULL DEFAULT 1,
    run_at_time      TIME        NOT NULL DEFAULT '02:00',   -- local time in time_zone
    time_zone        VARCHAR(64) NOT NULL DEFAULT 'UTC',     -- IANA id, e.g. Asia/Karachi
    retention_count  INT         NOT NULL DEFAULT 10,        -- keep the newest N backups
    next_run_at      TIMESTAMP WITH TIME ZONE NULL,
    updated_at       TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_by       UUID NULL,
    CONSTRAINT pk_backup_settings PRIMARY KEY (id),
    CONSTRAINT ck_backup_settings_single_row CHECK (id = 1),
    CONSTRAINT ck_backup_settings_frequency CHECK (frequency_days BETWEEN 1 AND 365),
    CONSTRAINT ck_backup_settings_retention CHECK (retention_count BETWEEN 1 AND 100)
);

INSERT INTO admin.backup_settings (id) VALUES (1) ON CONFLICT (id) DO NOTHING;

CREATE TABLE IF NOT EXISTS admin.database_backups (
    backup_id      UUID         NOT NULL DEFAULT gen_random_uuid(),
    file_name      VARCHAR(255) NOT NULL,
    size_bytes     BIGINT       NULL,
    status         VARCHAR(20)  NOT NULL DEFAULT 'running',
    trigger_type   VARCHAR(20)  NOT NULL,
    error_message  TEXT         NULL,
    created_by     UUID         NULL,
    started_at     TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at   TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT pk_database_backups PRIMARY KEY (backup_id),
    CONSTRAINT ck_database_backups_status CHECK (status IN ('running', 'completed', 'failed')),
    CONSTRAINT ck_database_backups_trigger CHECK (trigger_type IN ('manual', 'scheduled'))
);

CREATE INDEX IF NOT EXISTS idx_database_backups_started_at
    ON admin.database_backups (started_at DESC);

COMMIT;
