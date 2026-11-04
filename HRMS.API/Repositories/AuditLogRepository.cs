using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.AuditLog;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

/// <summary>
/// Read-only repository for the admin.auditlogs table.
/// Audit records are NEVER written here — they are written automatically
/// by the PostgreSQL trigger function admin.fn_audit_trigger().
/// This repository only reads and filters those records for the frontend.
/// </summary>
public class AuditLogRepository : BaseRepository, IAuditLogRepository
{
    public AuditLogRepository(IDatabaseConnectionFactory connectionFactory)
        : base(connectionFactory) { }

    /// <inheritdoc/>
    public async Task<AuditLogPagedResult> GetAuditLogsAsync(AuditLogFilter filter)
    {
        // Build the base WHERE clause dynamically based on which filters are provided.
        // All conditions use parameterized queries to prevent SQL injection.
        var conditions = new List<string> { "1=1" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filter.TableName))
        {
            conditions.Add("a.tablename = @TableName");
            parameters.Add("TableName", filter.TableName);
        }

        if (filter.UserId.HasValue)
        {
            conditions.Add("a.userid = @UserId");
            parameters.Add("UserId", filter.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            conditions.Add("a.action = @Action");
            parameters.Add("Action", filter.Action.ToUpper());
        }

        if (filter.RecordId.HasValue)
        {
            conditions.Add("a.recordid = @RecordId");
            parameters.Add("RecordId", filter.RecordId.Value);
        }

        if (filter.StartDate.HasValue)
        {
            conditions.Add("a.createdat >= @StartDate");
            parameters.Add("StartDate", filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            conditions.Add("a.createdat <= @EndDate");
            parameters.Add("EndDate", filter.EndDate.Value.Date.AddDays(1).AddTicks(-1));
        }

        var whereClause = string.Join(" AND ", conditions);
        var offset = (filter.Page - 1) * filter.PageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", filter.PageSize);

        // Count query for pagination metadata
        var countSql = $@"
            SELECT COUNT(*)
            FROM admin.auditlogs a
            WHERE {whereClause}";

        // Data query — joins auth.users to show the user's name alongside the log entry
        var dataSql = $@"
            SELECT
                a.logid       AS LogId,
                a.organizationid AS OrganizationId,
                a.userid      AS UserId,
                u.firstname   AS UserFirstName,
                u.lastname    AS UserLastName,
                u.email       AS UserEmail,
                a.tablename   AS TableName,
                a.recordid    AS RecordId,
                a.action      AS Action,
                a.oldvalues::text AS OldValues,
                a.newvalues::text AS NewValues,
                a.createdat   AS CreatedAt
            FROM admin.auditlogs a
            LEFT JOIN auth.users u ON a.userid = u.userid
            WHERE {whereClause}
            ORDER BY a.createdat DESC
            LIMIT @PageSize OFFSET @Offset";

        var totalCount = await ExecuteScalarAsync<int>(countSql, parameters);
        var items = await QueryAsync<AuditLogDto>(dataSql, parameters);

        return new AuditLogPagedResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AuditLogDto>> GetRecordHistoryAsync(Guid recordId)
    {
        // Returns the complete chronological history of a single record.
        // e.g. "Show me every status change this Loan ever had."
        const string sql = @"
            SELECT
                a.logid       AS LogId,
                a.organizationid AS OrganizationId,
                a.userid      AS UserId,
                u.firstname   AS UserFirstName,
                u.lastname    AS UserLastName,
                u.email       AS UserEmail,
                a.tablename   AS TableName,
                a.recordid    AS RecordId,
                a.action      AS Action,
                a.oldvalues::text AS OldValues,
                a.newvalues::text AS NewValues,
                a.createdat   AS CreatedAt
            FROM admin.auditlogs a
            LEFT JOIN auth.users u ON a.userid = u.userid
            WHERE a.recordid = @RecordId
            ORDER BY a.createdat ASC";

        return await QueryAsync<AuditLogDto>(sql, new { RecordId = recordId });
    }
}
