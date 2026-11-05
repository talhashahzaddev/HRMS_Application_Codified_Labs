using Dapper;
using Microsoft.AspNetCore.Http;
using Npgsql;
using System.Data;
using System.Globalization;
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
        _connectionString = NormalizeConnectionString(connectionString);
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

        // Values go in as parameters (set_config), never spliced into SQL text.
        // Empty strings are set for anonymous requests so pooled connections never
        // carry a previous user's identity.
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var orgId = user?.FindFirstValue("organizationId") ?? string.Empty;
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty;

        connection.Execute(
            "SELECT set_config('hrms.current_user_id', @userId, false), " +
            "set_config('hrms.current_org_id', @orgId, false), " +
            "set_config('hrms.current_useragent', @userAgent, false)",
            new { userId, orgId, userAgent });

        return connection;
    }

    private static string NormalizeConnectionString(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new ArgumentException("The PostgreSQL connection URL is invalid.", nameof(connectionString));
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'))
        };

        var userInfo = Uri.UnescapeDataString(uri.UserInfo);
        var credentialSeparator = userInfo.IndexOf(':');
        if (credentialSeparator >= 0)
        {
            builder.Username = userInfo[..credentialSeparator];
            builder.Password = userInfo[(credentialSeparator + 1)..];
        }
        else
        {
            builder.Username = userInfo;
        }

        foreach (var parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = parameter.Split('=', 2);
            var name = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
            var value = parts.Length == 2
                ? Uri.UnescapeDataString(parts[1].Replace('+', ' '))
                : string.Empty;

            var keyword = name.ToLowerInvariant() switch
            {
                "sslmode" => "SSL Mode",
                "channel_binding" => "Channel Binding",
                _ => string.Join(' ', name.Split('_', StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(part)))
            };
            builder[keyword] = value;
        }

        return builder.ConnectionString;
    }
}
