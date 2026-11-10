using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class DatabaseBackupRepository : BaseRepository, IDatabaseBackupRepository
{
    private const string BackupColumns = @"
        backup_id     AS BackupId,
        file_name     AS FileName,
        size_bytes    AS SizeBytes,
        status        AS Status,
        trigger_type  AS TriggerType,
        error_message AS ErrorMessage,
        created_by    AS CreatedBy,
        started_at    AS StartedAt,
        completed_at  AS CompletedAt";

    // Same DDL as HRMS.Script/16_platform_database_backups.sql, so the feature works
    // even where that script has not been run yet. Every statement is idempotent.
    private const string SchemaSql = @"
        CREATE SCHEMA IF NOT EXISTS admin;

        CREATE TABLE IF NOT EXISTS admin.backup_settings (
            id               SMALLINT    NOT NULL DEFAULT 1,
            is_enabled       BOOLEAN     NOT NULL DEFAULT false,
            frequency_days   INT         NOT NULL DEFAULT 1,
            run_at_time      TIME        NOT NULL DEFAULT '02:00',
            time_zone        VARCHAR(64) NOT NULL DEFAULT 'UTC',
            retention_count  INT         NOT NULL DEFAULT 10,
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
            ON admin.database_backups (started_at DESC);";

    private static volatile bool _schemaReady;
    private static readonly SemaphoreSlim SchemaLock = new(1, 1);

    public DatabaseBackupRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task EnsureSchemaAsync()
    {
        if (_schemaReady) return;
        await SchemaLock.WaitAsync();
        try
        {
            if (_schemaReady) return;
            await ExecuteAsync(SchemaSql);
            _schemaReady = true;
        }
        finally
        {
            SchemaLock.Release();
        }
    }

    public Task<BackupSettingsRow?> GetSettingsAsync()
    {
        const string sql = @"
            SELECT is_enabled      AS IsEnabled,
                   frequency_days  AS FrequencyDays,
                   run_at_time     AS RunAtTime,
                   time_zone       AS TimeZone,
                   retention_count AS RetentionCount,
                   next_run_at     AS NextRunAt,
                   updated_at      AS UpdatedAt
            FROM admin.backup_settings
            WHERE id = 1";
        return QuerySingleOrDefaultAsync<BackupSettingsRow>(sql);
    }

    public Task SaveSettingsAsync(BackupSettingsRow settings, Guid? updatedBy)
    {
        const string sql = @"
            INSERT INTO admin.backup_settings
                (id, is_enabled, frequency_days, run_at_time, time_zone, retention_count, next_run_at, updated_at, updated_by)
            VALUES
                (1, @IsEnabled, @FrequencyDays, CAST(@RunAtTime AS time), @TimeZone, @RetentionCount, @NextRunAt, CURRENT_TIMESTAMP, @updatedBy)
            ON CONFLICT (id) DO UPDATE SET
                is_enabled      = EXCLUDED.is_enabled,
                frequency_days  = EXCLUDED.frequency_days,
                run_at_time     = EXCLUDED.run_at_time,
                time_zone       = EXCLUDED.time_zone,
                retention_count = EXCLUDED.retention_count,
                next_run_at     = EXCLUDED.next_run_at,
                updated_at      = CURRENT_TIMESTAMP,
                updated_by      = EXCLUDED.updated_by";
        return ExecuteAsync(sql, new
        {
            settings.IsEnabled,
            settings.FrequencyDays,
            // Sent as text and cast in SQL: Npgsql would send a TimeSpan as interval.
            RunAtTime = settings.RunAtTime.ToString(@"hh\:mm\:ss"),
            settings.TimeZone,
            settings.RetentionCount,
            settings.NextRunAt,
            updatedBy
        });
    }

    public async Task<bool> TryClaimScheduledRunAsync(DateTime expectedNextRunAt, DateTime newNextRunAt)
    {
        const string sql = @"
            UPDATE admin.backup_settings
            SET next_run_at = @newNextRunAt
            WHERE id = 1 AND is_enabled = true AND next_run_at = @expectedNextRunAt";
        return await ExecuteAsync(sql, new { expectedNextRunAt, newNextRunAt }) == 1;
    }

    public Task<Guid> CreateAsync(string fileName, string triggerType, Guid? createdBy)
    {
        const string sql = @"
            INSERT INTO admin.database_backups (file_name, status, trigger_type, created_by)
            VALUES (@fileName, 'running', @triggerType, @createdBy)
            RETURNING backup_id";
        return ExecuteScalarAsync<Guid>(sql, new { fileName, triggerType, createdBy });
    }

    public Task MarkCompletedAsync(Guid backupId, long sizeBytes)
    {
        const string sql = @"
            UPDATE admin.database_backups
            SET status = 'completed', size_bytes = @sizeBytes, completed_at = CURRENT_TIMESTAMP, error_message = NULL
            WHERE backup_id = @backupId";
        return ExecuteAsync(sql, new { backupId, sizeBytes });
    }

    public Task MarkFailedAsync(Guid backupId, string errorMessage)
    {
        const string sql = @"
            UPDATE admin.database_backups
            SET status = 'failed', error_message = @errorMessage, completed_at = CURRENT_TIMESTAMP
            WHERE backup_id = @backupId";
        return ExecuteAsync(sql, new { backupId, errorMessage });
    }

    public Task<int> FailStaleRunningAsync(TimeSpan olderThan)
    {
        const string sql = @"
            UPDATE admin.database_backups
            SET status = 'failed', error_message = 'Backup did not finish (server restarted or timed out).', completed_at = CURRENT_TIMESTAMP
            WHERE status = 'running' AND started_at < CURRENT_TIMESTAMP - make_interval(secs => @seconds)";
        return ExecuteAsync(sql, new { seconds = olderThan.TotalSeconds });
    }

    public Task<DatabaseBackupDto?> GetByIdAsync(Guid backupId)
    {
        var sql = $"SELECT {BackupColumns} FROM admin.database_backups WHERE backup_id = @backupId";
        return QuerySingleOrDefaultAsync<DatabaseBackupDto>(sql, new { backupId });
    }

    public Task<IReadOnlyList<DatabaseBackupDto>> GetRecentAsync(int limit, DateTime? fromUtc = null, DateTime? toUtc = null)
    {
        var sql = $@"
            SELECT {BackupColumns} FROM admin.database_backups
            WHERE (CAST(@fromUtc AS timestamptz) IS NULL OR started_at >= CAST(@fromUtc AS timestamptz))
              AND (CAST(@toUtc   AS timestamptz) IS NULL OR started_at <= CAST(@toUtc   AS timestamptz))
            ORDER BY started_at DESC
            LIMIT @limit";
        return QueryAsync<DatabaseBackupDto>(sql, new { limit, fromUtc, toUtc });
    }

    public async Task<bool> HasRunningAsync()
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM admin.database_backups WHERE status = 'running')";
        return await ExecuteScalarAsync<bool>(sql);
    }

    public Task<IReadOnlyList<DatabaseBackupDto>> GetCompletedBeyondRetentionAsync(int keepNewest)
    {
        var sql = $@"
            SELECT {BackupColumns} FROM admin.database_backups
            WHERE status = 'completed'
            ORDER BY started_at DESC
            OFFSET @keepNewest";
        return QueryAsync<DatabaseBackupDto>(sql, new { keepNewest });
    }

    public async Task<bool> DeleteAsync(Guid backupId)
    {
        const string sql = "DELETE FROM admin.database_backups WHERE backup_id = @backupId";
        return await ExecuteAsync(sql, new { backupId }) == 1;
    }

    public Task<int> DeleteOldFailedAsync(TimeSpan olderThan)
    {
        const string sql = @"
            DELETE FROM admin.database_backups
            WHERE status = 'failed' AND started_at < CURRENT_TIMESTAMP - make_interval(secs => @seconds)";
        return ExecuteAsync(sql, new { seconds = olderThan.TotalSeconds });
    }
}
