using System;
using System.Threading;
using System.Threading.Tasks;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
public class UploadsController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(IFileUploadService fileUploadService, ILogger<UploadsController> logger)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    [HttpPost("files")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadFile([FromForm(Name = "file")] IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            var url = await _fileUploadService.UploadFileAsync(file, cancellationToken);
            return Ok(new { url });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error while uploading file.");
            return BadRequest(new { error = ex.Message });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("File upload request was cancelled.");
            return BadRequest(new { error = "Upload request was cancelled." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while uploading file.");
            return StatusCode(500, new { error = "Failed to upload file. Please try again later." });
        }
    }
}
