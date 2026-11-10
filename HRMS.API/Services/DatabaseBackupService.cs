using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Hangfire;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Npgsql;

namespace HRMS.API.Services;

public class DatabaseBackupOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    /// <summary>Server folder where dump files are written. On Railway, mount a volume here or files are lost on redeploy.</summary>
    public string StoragePath { get; set; } = "backups";
    /// <summary>Optional full path to pg_dump. Empty = auto-detect.</summary>
    public string? PgDumpPath { get; set; }
    public int TimeoutMinutes { get; set; } = 60;
}

public class DatabaseBackupService : IDatabaseBackupService
{
    public const string SchedulerJobId = "platform-database-backup-scheduler";

    private const int HistoryLimit = 100;

    private readonly IDatabaseBackupRepository _repository;
    private readonly IBackgroundJobClient _jobs;
    private readonly DatabaseBackupOptions _options;
    private readonly ILogger<DatabaseBackupService> _logger;

    public DatabaseBackupService(
        IDatabaseBackupRepository repository,
        IBackgroundJobClient jobs,
        DatabaseBackupOptions options,
        ILogger<DatabaseBackupService> logger)
    {
        _repository = repository;
        _jobs = jobs;
        _options = options;
        _logger = logger;
    }

    private TimeSpan StaleAfter => TimeSpan.FromMinutes(_options.TimeoutMinutes + 30);
    private string StorageRoot => Path.GetFullPath(_options.StoragePath);

    // ======================= SETTINGS =======================

    public async Task<ServiceResponse<BackupSettingsDto>> GetSettingsAsync()
    {
        await _repository.EnsureSchemaAsync();
        var row = await _repository.GetSettingsAsync() ?? DefaultSettings();
        return new ServiceResponse<BackupSettingsDto> { Data = ToDto(row) };
    }

    public async Task<ServiceResponse<BackupSettingsDto>> UpdateSettingsAsync(UpdateBackupSettingsRequest request, Guid? updatedBy)
    {
        await _repository.EnsureSchemaAsync();
        if (request.FrequencyDays is < 1 or > 365)
            return Fail<BackupSettingsDto>("Frequency must be between 1 and 365 days.");
        if (request.RetentionCount is < 1 or > 100)
            return Fail<BackupSettingsDto>("Number of backups to keep must be between 1 and 100.");
        if (!TryParseTimeOfDay(request.RunAtTime, out var runAt))
            return Fail<BackupSettingsDto>("Backup time must be in HH:mm format.");
        if (!TryFindTimeZone(request.TimeZone, out var timeZone))
            return Fail<BackupSettingsDto>($"Unknown time zone '{request.TimeZone}'.");

        var current = await _repository.GetSettingsAsync() ?? DefaultSettings();

        var scheduleChanged = !current.IsEnabled
                              || current.NextRunAt == null
                              || current.FrequencyDays != request.FrequencyDays
                              || current.RunAtTime != runAt
                              || !string.Equals(current.TimeZone, request.TimeZone, StringComparison.Ordinal);

        var updated = new BackupSettingsRow
        {
            IsEnabled = request.IsEnabled,
            FrequencyDays = request.FrequencyDays,
            RunAtTime = runAt,
            TimeZone = request.TimeZone,
            RetentionCount = request.RetentionCount,
            NextRunAt = !request.IsEnabled
                ? null
                : scheduleChanged
                    ? ComputeFirstRunUtc(DateTime.UtcNow, runAt, timeZone)
                    : current.NextRunAt
        };

        await _repository.SaveSettingsAsync(updated, updatedBy);

        // Retention may have been lowered; trim right away.
        await ApplyRetentionAsync(updated.RetentionCount);

        var saved = await _repository.GetSettingsAsync() ?? updated;
        return new ServiceResponse<BackupSettingsDto>
        {
            Data = ToDto(saved),
            Message = request.IsEnabled ? "Backup schedule saved." : "Automatic backups turned off."
        };
    }

    // ======================= BACKUPS =======================

    public async Task<ServiceResponse<List<DatabaseBackupDto>>> GetBackupsAsync(DateTime? fromUtc = null, DateTime? toUtc = null)
    {
        if (fromUtc.HasValue && toUtc.HasValue && fromUtc > toUtc)
            return Fail<List<DatabaseBackupDto>>("'From' must be before 'To'.");

        await _repository.EnsureSchemaAsync();
        await _repository.FailStaleRunningAsync(StaleAfter);
        var backups = (await _repository.GetRecentAsync(HistoryLimit, AsUtc(fromUtc), AsUtc(toUtc))).ToList();
        foreach (var backup in backups)
            backup.FileAvailable = backup.Status == BackupStatus.Completed && ResolveFilePath(backup.FileName) is { } p && File.Exists(p);

        return new ServiceResponse<List<DatabaseBackupDto>> { Data = backups };
    }

    public async Task<ServiceResponse<DatabaseBackupDto>> StartManualBackupAsync(Guid? requestedBy)
    {
        await _repository.EnsureSchemaAsync();
        await _repository.FailStaleRunningAsync(StaleAfter);
        if (await _repository.HasRunningAsync())
            return Fail<DatabaseBackupDto>("A backup is already running. Please wait for it to finish.");

        var backupId = await _repository.CreateAsync(NewFileName(), BackupTrigger.Manual, requestedBy);
        _jobs.Enqueue<IDatabaseBackupService>(service => service.ExecuteBackupAsync(backupId));

        return new ServiceResponse<DatabaseBackupDto>
        {
            Data = await _repository.GetByIdAsync(backupId),
            Message = "Backup started."
        };
    }

    public async Task<(string Path, string FileName)?> GetBackupFileAsync(Guid backupId)
    {
        await _repository.EnsureSchemaAsync();
        var backup = await _repository.GetByIdAsync(backupId);
        if (backup == null || backup.Status != BackupStatus.Completed)
            return null;

        var path = ResolveFilePath(backup.FileName);
        return path != null && File.Exists(path) ? (path, backup.FileName) : null;
    }

    public async Task<ServiceResponse<bool>> DeleteBackupAsync(Guid backupId)
    {
        await _repository.EnsureSchemaAsync();
        var backup = await _repository.GetByIdAsync(backupId);
        if (backup == null)
            return Fail<bool>("Backup not found.");
        if (backup.Status == BackupStatus.Running)
            return Fail<bool>("A running backup cannot be deleted.");

        DeleteFileQuietly(backup.FileName);
        await _repository.DeleteAsync(backupId);
        return new ServiceResponse<bool> { Data = true, Message = "Backup deleted." };
    }

    // ======================= JOBS =======================

    public async Task ExecuteBackupAsync(Guid backupId)
    {
        await _repository.EnsureSchemaAsync();
        var backup = await _repository.GetByIdAsync(backupId);
        if (backup == null || backup.Status != BackupStatus.Running)
            return;

        var finalPath = ResolveFilePath(backup.FileName);
        if (finalPath == null)
        {
            await _repository.MarkFailedAsync(backupId, "Invalid backup file name.");
            return;
        }

        var partialPath = finalPath + ".partial";
        try
        {
            Directory.CreateDirectory(StorageRoot);
            await RunPgDumpAsync(partialPath);

            File.Move(partialPath, finalPath, overwrite: true);
            var size = new FileInfo(finalPath).Length;
            await _repository.MarkCompletedAsync(backupId, size);
            _logger.LogInformation("Database backup {BackupId} completed ({Size} bytes)", backupId, size);

            var settings = await _repository.GetSettingsAsync() ?? DefaultSettings();
            await ApplyRetentionAsync(settings.RetentionCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database backup {BackupId} failed", backupId);
            TryDelete(partialPath);
            await _repository.MarkFailedAsync(backupId, Truncate(ex.Message, 2000));
        }
    }

    public async Task RunScheduledIfDueAsync()
    {
        await _repository.EnsureSchemaAsync();
        await _repository.FailStaleRunningAsync(StaleAfter);

        var settings = await _repository.GetSettingsAsync();
        if (settings is not { IsEnabled: true, NextRunAt: { } dueAt } || dueAt > DateTime.UtcNow)
            return;

        if (!TryFindTimeZone(settings.TimeZone, out var timeZone))
            timeZone = TimeZoneInfo.Utc;

        var next = ComputeNextRunUtc(dueAt, settings.FrequencyDays, settings.RunAtTime, timeZone, DateTime.UtcNow);
        if (!await _repository.TryClaimScheduledRunAsync(dueAt, next))
            return; // another server instance took this run

        if (await _repository.HasRunningAsync())
        {
            _logger.LogWarning("Scheduled database backup skipped: another backup is still running");
            return;
        }

        var backupId = await _repository.CreateAsync(NewFileName(), BackupTrigger.Scheduled, null);
        await ExecuteBackupAsync(backupId);
        await _repository.DeleteOldFailedAsync(TimeSpan.FromDays(30));
    }

    // ======================= SCHEDULE MATH =======================

    /// <summary>Next occurrence of <paramref name="runAt"/> (local time in <paramref name="tz"/>) strictly after now.</summary>
    public static DateTime ComputeFirstRunUtc(DateTime nowUtc, TimeSpan runAt, TimeZoneInfo tz)
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
        var candidate = localNow.Date + runAt;
        if (candidate <= localNow)
            candidate = candidate.AddDays(1);
        return LocalToUtc(candidate, tz);
    }

    /// <summary>Adds the frequency to the run that just happened, skipping any runs missed while the server was down.</summary>
    public static DateTime ComputeNextRunUtc(DateTime scheduledUtc, int frequencyDays, TimeSpan runAt, TimeZoneInfo tz, DateTime nowUtc)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(scheduledUtc, tz).Date + runAt;
        DateTime next;
        do
        {
            local = local.AddDays(Math.Max(1, frequencyDays));
            next = LocalToUtc(local, tz);
        } while (next <= nowUtc);
        return next;
    }

    private static DateTime LocalToUtc(DateTime local, TimeZoneInfo tz)
    {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (tz.IsInvalidTime(local)) // inside a DST gap
            local = local.AddHours(1);
        return TimeZoneInfo.ConvertTimeToUtc(local, tz);
    }

    // ======================= PG_DUMP =======================

    private async Task RunPgDumpAsync(string outputPath)
    {
        var db = new NpgsqlConnectionStringBuilder(_options.ConnectionString);

        var psi = new ProcessStartInfo(ResolvePgDumpPath())
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("--format=custom"); // compressed; restore with pg_restore
        psi.ArgumentList.Add("--no-password");
        psi.ArgumentList.Add("--host"); psi.ArgumentList.Add(db.Host ?? "localhost");
        psi.ArgumentList.Add("--port"); psi.ArgumentList.Add(db.Port.ToString(CultureInfo.InvariantCulture));
        psi.ArgumentList.Add("--username"); psi.ArgumentList.Add(db.Username ?? "postgres");
        psi.ArgumentList.Add("--dbname"); psi.ArgumentList.Add(db.Database ?? "postgres");
        psi.ArgumentList.Add("--file"); psi.ArgumentList.Add(outputPath);

        // Password goes through the environment, never the command line.
        psi.Environment["PGPASSWORD"] = db.Password ?? string.Empty;
        psi.Environment["PGSSLMODE"] = db.SslMode switch
        {
            SslMode.Disable => "disable",
            SslMode.Allow => "allow",
            SslMode.Require => "require",
            SslMode.VerifyCA => "verify-ca",
            SslMode.VerifyFull => "verify-full",
            _ => "prefer"
        };

        using var process = new Process { StartInfo = psi };
        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            throw new InvalidOperationException(
                "pg_dump was not found on the server. Install PostgreSQL client tools (version 17 or newer) " +
                "or set Backup:PgDumpPath to the pg_dump executable.");
        }

        var stderrTask = process.StandardError.ReadToEndAsync();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();

        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(_options.TimeoutMinutes));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
            throw new TimeoutException($"Backup took longer than {_options.TimeoutMinutes} minutes and was stopped.");
        }

        var stderr = (await stderrTask).Trim();
        await stdoutTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrEmpty(stderr)
                ? $"pg_dump exited with code {process.ExitCode}."
                : $"pg_dump failed: {stderr}");

        if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
            throw new InvalidOperationException("pg_dump finished but produced an empty file.");
    }

    private string ResolvePgDumpPath()
    {
        if (!string.IsNullOrWhiteSpace(_options.PgDumpPath))
            return _options.PgDumpPath;

        if (OperatingSystem.IsWindows())
        {
            // Local development: use the newest installed PostgreSQL if it is not on PATH.
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PostgreSQL");
            if (Directory.Exists(root))
            {
                var newest = Directory.GetDirectories(root)
                    .Select(dir => (dir, version: int.TryParse(Path.GetFileName(dir).Split('.')[0], out var v) ? v : 0))
                    .OrderByDescending(x => x.version)
                    .Select(x => Path.Combine(x.dir, "bin", "pg_dump.exe"))
                    .FirstOrDefault(File.Exists);
                if (newest != null)
                    return newest;
            }
            return "pg_dump.exe";
        }

        // Debian PGDG layout installed by the Dockerfile.
        const string pgdg17 = "/usr/lib/postgresql/17/bin/pg_dump";
        return File.Exists(pgdg17) ? pgdg17 : "pg_dump";
    }

    // ======================= HELPERS =======================

    private async Task ApplyRetentionAsync(int keepNewest)
    {
        var expired = await _repository.GetCompletedBeyondRetentionAsync(Math.Max(1, keepNewest));
        foreach (var backup in expired)
        {
            DeleteFileQuietly(backup.FileName);
            await _repository.DeleteAsync(backup.BackupId);
        }
    }

    private static string NewFileName() =>
        $"hrms-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}-utc.dump";

    /// <summary>Maps a stored file name to a path inside the storage folder; null if it would escape it.</summary>
    private string? ResolveFilePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName))
            return null;

        var root = StorageRoot;
        var full = Path.GetFullPath(Path.Combine(root, fileName));
        return full.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            ? full
            : null;
    }

    private void DeleteFileQuietly(string fileName)
    {
        var path = ResolveFilePath(fileName);
        if (path != null)
            TryDelete(path);
    }

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete backup file {Path}", path);
        }
    }

    private static bool TryParseTimeOfDay(string? value, out TimeSpan time)
    {
        time = default;
        if (!TimeSpan.TryParseExact(value?.Trim(), new[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss" }, CultureInfo.InvariantCulture, out var parsed))
            return false;
        if (parsed < TimeSpan.Zero || parsed >= TimeSpan.FromDays(1))
            return false;
        time = new TimeSpan(parsed.Hours, parsed.Minutes, 0);
        return true;
    }

    private static bool TryFindTimeZone(string? id, out TimeZoneInfo timeZone)
    {
        timeZone = TimeZoneInfo.Utc;
        if (string.IsNullOrWhiteSpace(id))
            return false;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static BackupSettingsRow DefaultSettings() => new()
    {
        IsEnabled = false,
        FrequencyDays = 1,
        RunAtTime = new TimeSpan(2, 0, 0),
        TimeZone = "UTC",
        RetentionCount = 10
    };

    private static BackupSettingsDto ToDto(BackupSettingsRow row) => new()
    {
        IsEnabled = row.IsEnabled,
        FrequencyDays = row.FrequencyDays,
        RunAtTime = row.RunAtTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
        TimeZone = row.TimeZone,
        RetentionCount = row.RetentionCount,
        NextRunAt = row.NextRunAt,
        UpdatedAt = row.UpdatedAt
    };

    private static DateTime? AsUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } v => v,
        { Kind: DateTimeKind.Local } v => v.ToUniversalTime(),
        var v => DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
    };

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static ServiceResponse<T> Fail<T>(string message) =>
        new() { Success = false, Message = message };
}
