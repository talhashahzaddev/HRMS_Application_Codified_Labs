using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces;

public interface IDatabaseBackupRepository
{
    /// <summary>Creates the backup tables if they are missing (runs once per process).</summary>
    Task EnsureSchemaAsync();

    Task<BackupSettingsRow?> GetSettingsAsync();
    Task SaveSettingsAsync(BackupSettingsRow settings, Guid? updatedBy);

    /// <summary>Moves next_run_at forward only if it still equals <paramref name="expectedNextRunAt"/>. Returns true when this caller won the claim.</summary>
    Task<bool> TryClaimScheduledRunAsync(DateTime expectedNextRunAt, DateTime newNextRunAt);

    Task<Guid> CreateAsync(string fileName, string triggerType, Guid? createdBy);
    Task MarkCompletedAsync(Guid backupId, long sizeBytes);
    Task MarkFailedAsync(Guid backupId, string errorMessage);
    Task<int> FailStaleRunningAsync(TimeSpan olderThan);

    Task<DatabaseBackupDto?> GetByIdAsync(Guid backupId);
    Task<IReadOnlyList<DatabaseBackupDto>> GetRecentAsync(int limit, DateTime? fromUtc = null, DateTime? toUtc = null);
    Task<bool> HasRunningAsync();
    Task<IReadOnlyList<DatabaseBackupDto>> GetCompletedBeyondRetentionAsync(int keepNewest);
    Task<bool> DeleteAsync(Guid backupId);
    Task<int> DeleteOldFailedAsync(TimeSpan olderThan);
}
