using Hangfire;
using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IDatabaseBackupService
{
    Task<ServiceResponse<BackupSettingsDto>> GetSettingsAsync();
    Task<ServiceResponse<BackupSettingsDto>> UpdateSettingsAsync(UpdateBackupSettingsRequest request, Guid? updatedBy);

    /// <summary>Backup history, optionally limited to backups started between the two UTC times.</summary>
    Task<ServiceResponse<List<DatabaseBackupDto>>> GetBackupsAsync(DateTime? fromUtc = null, DateTime? toUtc = null);

    /// <summary>Creates a backup record and queues the dump in the background.</summary>
    Task<ServiceResponse<DatabaseBackupDto>> StartManualBackupAsync(Guid? requestedBy);

    /// <summary>Returns the full path and download name of a completed backup, or null.</summary>
    Task<(string Path, string FileName)?> GetBackupFileAsync(Guid backupId);

    Task<ServiceResponse<bool>> DeleteBackupAsync(Guid backupId);

    // ---- Hangfire jobs (filters on interface methods are what Hangfire reads) ----

    [AutomaticRetry(Attempts = 0)]
    Task ExecuteBackupAsync(Guid backupId);

    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    Task RunScheduledIfDueAsync();
}
