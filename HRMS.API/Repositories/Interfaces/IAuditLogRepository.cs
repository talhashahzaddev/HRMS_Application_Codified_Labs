using HRMS.API.Models.AuditLog;

namespace HRMS.API.Repositories.Interfaces;

/// <summary>
/// Repository interface for querying the admin.auditlogs table.
/// This is a read-only repository — audit records are always written
/// automatically by PostgreSQL triggers, never by application code.
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// Returns a paginated, filtered list of audit log entries with the user's name joined.
    /// </summary>
    Task<AuditLogPagedResult> GetAuditLogsAsync(AuditLogFilter filter);

    /// <summary>
    /// Returns all audit history for one specific record (by recordid).
    /// Use this to show the full lifecycle of a Loan, Leave Request, etc.
    /// </summary>
    Task<IReadOnlyList<AuditLogDto>> GetRecordHistoryAsync(Guid recordId);
}
