namespace HRMS.API.Models.DTOs;

public static class BackupStatus
{
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
}

public static class BackupTrigger
{
    public const string Manual = "manual";
    public const string Scheduled = "scheduled";
}

public class DatabaseBackupDto
{
    public Guid BackupId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long? SizeBytes { get; set; }
    public string Status { get; set; } = BackupStatus.Running;
    public string TriggerType { get; set; } = BackupTrigger.Manual;
    public string? ErrorMessage { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    /// <summary>False when the row exists but the file is gone from server storage (e.g. after a redeploy without a volume).</summary>
    public bool FileAvailable { get; set; }
}

public class BackupSettingsDto
{
    public bool IsEnabled { get; set; }
    public int FrequencyDays { get; set; } = 1;
    /// <summary>Time of day in "HH:mm", in <see cref="TimeZone"/>.</summary>
    public string RunAtTime { get; set; } = "02:00";
    /// <summary>IANA time zone id, e.g. "Asia/Karachi".</summary>
    public string TimeZone { get; set; } = "UTC";
    public int RetentionCount { get; set; } = 10;
    public DateTime? NextRunAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdateBackupSettingsRequest
{
    public bool IsEnabled { get; set; }
    public int FrequencyDays { get; set; } = 1;
    public string RunAtTime { get; set; } = "02:00";
    public string TimeZone { get; set; } = "UTC";
    public int RetentionCount { get; set; } = 10;
}

/// <summary>Row shape of admin.backup_settings.</summary>
public class BackupSettingsRow
{
    public bool IsEnabled { get; set; }
    public int FrequencyDays { get; set; }
    public TimeSpan RunAtTime { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public int RetentionCount { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
