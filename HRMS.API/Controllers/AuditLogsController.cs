using HRMS.API.Models.AuditLog;
using HRMS.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

/// <summary>
/// Provides read-only access to the system's audit log records.
///
/// Audit records are written automatically by PostgreSQL database triggers —
/// this controller only exposes endpoints to READ and FILTER those records.
///
/// Intended for:
///   - System Administrators (viewing all activity)
///   - HR Managers (auditing payroll or leave approvals)
///   - Compliance teams (exporting logs for a date range)
///
/// Typical Angular usage:
///   GET /api/v1/auditlogs?tableName=payroll.loans&page=1&pageSize=50
///   GET /api/v1/auditlogs/record/{recordId}
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Require JWT authentication on all endpoints
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogRepository _repository;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(IAuditLogRepository repository, ILogger<AuditLogsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Returns a paginated, filterable list of all audit log entries.
    /// </summary>
    /// <param name="tableName">Filter by schema.table (e.g. "payroll.loans", "employee.employees")</param>
    /// <param name="userId">Filter by the User ID who made the change</param>
    /// <param name="action">Filter by action type: INSERT | UPDATE | DELETE</param>
    /// <param name="recordId">Filter to show all history of one specific record</param>
    /// <param name="startDate">Start of date range filter (inclusive)</param>
    /// <param name="endDate">End of date range filter (inclusive)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Records per page (default: 50, max: 200)</param>
    /// <returns>Paginated audit log result with user display names.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(AuditLogPagedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? tableName,
        [FromQuery] Guid? userId,
        [FromQuery] string? action,
        [FromQuery] Guid? recordId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Guard against absurdly large page sizes
            if (pageSize > 200) pageSize = 200;
            if (page < 1) page = 1;

            var filter = new AuditLogFilter
            {
                TableName = tableName,
                UserId = userId,
                Action = action,
                RecordId = recordId,
                StartDate = startDate,
                EndDate = endDate,
                Page = page,
                PageSize = pageSize
            };

            var result = await _repository.GetAuditLogsAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, new { message = "An error occurred while retrieving audit logs." });
        }
    }

    /// <summary>
    /// Returns the complete chronological history of a single record.
    /// Use this to show "Who changed this Loan and when" on a detail page in Angular.
    /// </summary>
    /// <param name="recordId">The UUID of the specific record to trace.</param>
    /// <returns>All audit entries for that record, sorted oldest to newest.</returns>
    [HttpGet("record/{recordId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRecordHistory(Guid recordId)
    {
        try
        {
            var history = await _repository.GetRecordHistoryAsync(recordId);

            if (!history.Any())
                return NotFound(new { message = $"No audit history found for record {recordId}." });

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving record history for {RecordId}", recordId);
            return StatusCode(500, new { message = "An error occurred while retrieving record history." });
        }
    }
}
