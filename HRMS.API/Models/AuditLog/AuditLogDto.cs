namespace HRMS.API.Models.AuditLog;

/// <summary>
/// A single audit log entry returned to the frontend.
/// Includes the user's name (joined from auth.users) so the UI
/// can display "Updated by: John Smith" instead of just a raw UUID.
/// </summary>
public class AuditLogDto
{
    public Guid LogId { get; set; }
    public Guid? OrganizationId { get; set; }

    // Who made the change
    public Guid? UserId { get; set; }
    public string? UserFirstName { get; set; }
    public string? UserLastName { get; set; }
    public string? UserEmail { get; set; }

    // What was changed
    public string TableName { get; set; } = string.Empty;
    public Guid? RecordId { get; set; }
    public string Action { get; set; } = string.Empty; // INSERT | UPDATE | DELETE

    // The actual data diff (JSON strings for the frontend diff viewer)
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }

    // When it happened
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Query filters for the GET /api/v1/auditlogs endpoint.
/// All filters are optional — omit to retrieve all logs.
/// </summary>
public class AuditLogFilter
{
    /// <summary>Filter by module/schema (e.g. "payroll.loans", "employee.employees")</summary>
    public string? TableName { get; set; }

    /// <summary>Filter by specific user who made the change</summary>
    public Guid? UserId { get; set; }

    /// <summary>Filter by the type of operation</summary>
    public string? Action { get; set; } // INSERT | UPDATE | DELETE

    /// <summary>Filter by a specific record's full history (e.g. one specific Loan)</summary>
    public Guid? RecordId { get; set; }

    /// <summary>Start date for the time range filter</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>End date for the time range filter</summary>
    public DateTime? EndDate { get; set; }

    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Paginated response wrapper for audit logs.
/// </summary>
public class AuditLogPagedResult
{
    public IEnumerable<AuditLogDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
