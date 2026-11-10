using Hangfire;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HRMS.API.Tests;

public sealed class DatabaseBackupServiceTests
{
    private static readonly TimeZoneInfo Karachi = TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi"); // UTC+5, no DST

    private static DateTime Utc(int y, int m, int d, int h, int min = 0) => new(y, m, d, h, min, 0, DateTimeKind.Utc);

    [Fact]
    public void First_run_is_tomorrow_when_todays_time_has_passed()
    {
        // 15:00 in Karachi, backup at 02:00 -> tomorrow 02:00 PKT = 21:00 UTC today
        var next = DatabaseBackupService.ComputeFirstRunUtc(Utc(2026, 11, 10, 10), new TimeSpan(2, 0, 0), Karachi);
        Assert.Equal(Utc(2026, 11, 10, 21), next);
    }

    [Fact]
    public void First_run_is_today_when_time_is_still_ahead()
    {
        var next = DatabaseBackupService.ComputeFirstRunUtc(Utc(2026, 11, 10, 10), new TimeSpan(18, 0, 0), Karachi);
        Assert.Equal(Utc(2026, 11, 10, 13), next);
    }

    [Fact]
    public void Next_run_adds_frequency_days()
    {
        var next = DatabaseBackupService.ComputeNextRunUtc(
            Utc(2026, 11, 10, 21), 15, new TimeSpan(2, 0, 0), Karachi, Utc(2026, 11, 10, 21, 5));
        Assert.Equal(Utc(2026, 11, 25, 21), next);
    }

    [Fact]
    public void Next_run_skips_runs_missed_while_server_was_down()
    {
        var next = DatabaseBackupService.ComputeNextRunUtc(
            Utc(2026, 1, 1, 21), 1, new TimeSpan(2, 0, 0), Karachi, Utc(2026, 1, 5, 22));
        Assert.Equal(Utc(2026, 1, 6, 21), next);
    }

    [Theory]
    [InlineData(0, 10, "02:00", "Asia/Karachi")]
    [InlineData(1, 0, "02:00", "Asia/Karachi")]
    [InlineData(1, 10, "25:00", "Asia/Karachi")]
    [InlineData(1, 10, "02:00", "Not/AZone")]
    public async Task Invalid_settings_are_rejected(int frequencyDays, int retention, string time, string zone)
    {
        var repo = new Mock<IDatabaseBackupRepository>();
        var service = CreateService(repo);

        var result = await service.UpdateSettingsAsync(new UpdateBackupSettingsRequest
        {
            IsEnabled = true,
            FrequencyDays = frequencyDays,
            RetentionCount = retention,
            RunAtTime = time,
            TimeZone = zone
        }, null);

        Assert.False(result.Success);
        repo.Verify(r => r.SaveSettingsAsync(It.IsAny<BackupSettingsRow>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task Manual_backup_is_refused_while_another_is_running()
    {
        var repo = new Mock<IDatabaseBackupRepository>();
        repo.Setup(r => r.HasRunningAsync()).ReturnsAsync(true);
        var service = CreateService(repo);

        var result = await service.StartManualBackupAsync(Guid.NewGuid());

        Assert.False(result.Success);
        repo.Verify(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
    }

    private static DatabaseBackupService CreateService(Mock<IDatabaseBackupRepository> repo) =>
        new(repo.Object,
            new Mock<IBackgroundJobClient>().Object,
            new DatabaseBackupOptions { ConnectionString = "Host=localhost;Database=x;Username=u;Password=p", StoragePath = Path.GetTempPath() },
            NullLogger<DatabaseBackupService>.Instance);
}
