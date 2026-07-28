using Dapper;
using Microsoft.AspNetCore.Http;
using Npgsql;
using System.Data;
using System.Security.Claims;

namespace HRMS.API.Configuration;

public interface IDatabaseConnectionFactory
{
    IDbConnection CreateConnection();
}

/// <summary>
/// Creates PostgreSQL connections for Dapper.
/// Automatically injects the currently logged-in user's ID into the PostgreSQL session
/// variable 'hrms.current_user_id' so that database audit triggers can capture WHO made
/// a change without any modifications to business controllers or repositories.
/// </summary>
public class DatabaseConnectionFactory : IDatabaseConnectionFactory
{
    private readonly string _connectionString;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DatabaseConnectionFactory(string connectionString, IHttpContextAccessor httpContextAccessor)
    {
        _connectionString = connectionString;
        _httpContextAccessor = httpContextAccessor;
    }

    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);

        // Open immediately so we can run the session variable commands
        connection.Open();

        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User;

        // ---------------------------------------------------------------
        // IMPORTANT: We use SET (not SET LOCAL) because Dapper does not
        // wrap queries in explicit transactions by default. SET LOCAL only
        // lasts for the current transaction, and since each Dapper statement
        // is its own auto-commit transaction, SET LOCAL would be lost before
        // the actual INSERT/UPDATE/DELETE triggers fire.
        //
        // Safety: Every new request creates a fresh connection via CreateConnection().
        // The variable is overwritten at the start of each request, so there is
        // no risk of one user's ID leaking into another user's request.
        // ---------------------------------------------------------------

        // 1. Inject User ID (fixes: userid NULL in audit logs)
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            connection.Execute($"SET hrms.current_user_id = '{userId}'");
        }
        else
        {
            // CRITICAL: Prevent connection pool leakage for unauthenticated requests
            connection.Execute("SET hrms.current_user_id = ''");
        }

        // 2. Inject Organization ID (fixes: organizationid NULL in audit logs)
        var orgId = user?.FindFirstValue("organizationId");
        if (!string.IsNullOrEmpty(orgId))
        {
            connection.Execute($"SET hrms.current_org_id = '{orgId}'");
        }
        else
        {
            connection.Execute("SET hrms.current_org_id = ''");
        }

        // 3. Inject User-Agent browser string (fixes: useragent NULL in audit logs)
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();
        if (!string.IsNullOrEmpty(userAgent))
        {
            // Escape single quotes in the user agent string to prevent SQL issues
            var safeUserAgent = userAgent.Replace("'", "''");
            connection.Execute($"SET hrms.current_useragent = '{safeUserAgent}'");
        }
        else
        {
            connection.Execute("SET hrms.current_useragent = ''");
        }

        return connection;
    }
}
