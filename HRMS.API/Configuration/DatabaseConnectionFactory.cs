using Npgsql;
using System.Data;
using System.Globalization;

namespace HRMS.API.Configuration;

public interface IDatabaseConnectionFactory
{
    IDbConnection CreateConnection();
}

public class DatabaseConnectionFactory : IDatabaseConnectionFactory
{
    private readonly string _connectionString;

    public DatabaseConnectionFactory(string connectionString)
    {
        _connectionString = NormalizeConnectionString(connectionString);
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
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
