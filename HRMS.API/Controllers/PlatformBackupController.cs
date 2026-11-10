using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Platform Admin database backups: take one now, schedule automatic ones, download or delete.
/// Lives under /api/SuperAdmin so the platform-admin frontend interceptor attaches its token.
/// </summary>
[ApiController]
[Route("api/SuperAdmin/backups")]
[Authorize(Roles = "Platform Admin")]
public class PlatformBackupController : ControllerBase
{
    private readonly IDatabaseBackupService _backupService;

    public PlatformBackupController(IDatabaseBackupService backupService)
    {
        _backupService = backupService;
    }

    [HttpGet]
    public async Task<ActionResult<ServiceResponse<List<DatabaseBackupDto>>>> GetBackups(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var response = await _backupService.GetBackupsAsync(from, to);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResponse<DatabaseBackupDto>>> StartBackup()
    {
        var response = await _backupService.StartManualBackupAsync(GetAdminId());
        return response.Success ? Ok(response) : Conflict(response);
    }

    [HttpGet("{backupId:guid}/download")]
    public async Task<IActionResult> Download(Guid backupId)
    {
        var file = await _backupService.GetBackupFileAsync(backupId);
        if (file == null)
            return NotFound(new ServiceResponse<bool> { Success = false, Message = "Backup file is not available." });

        return PhysicalFile(file.Value.Path, "application/octet-stream", file.Value.FileName, enableRangeProcessing: true);
    }

    [HttpDelete("{backupId:guid}")]
    public async Task<ActionResult<ServiceResponse<bool>>> Delete(Guid backupId)
    {
        var response = await _backupService.DeleteBackupAsync(backupId);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet("settings")]
    public async Task<ActionResult<ServiceResponse<BackupSettingsDto>>> GetSettings()
        => Ok(await _backupService.GetSettingsAsync());

    [HttpPut("settings")]
    public async Task<ActionResult<ServiceResponse<BackupSettingsDto>>> UpdateSettings([FromBody] UpdateBackupSettingsRequest request)
    {
        var response = await _backupService.UpdateSettingsAsync(request, GetAdminId());
        return response.Success ? Ok(response) : BadRequest(response);
    }

    private Guid? GetAdminId()
    {
        var value = User.FindFirstValue("adminId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
