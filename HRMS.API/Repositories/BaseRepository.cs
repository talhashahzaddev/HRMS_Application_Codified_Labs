using System.Data;
using Dapper;
using HRMS.API.Configuration;

namespace HRMS.API.Repositories;

public abstract class BaseRepository
{
    protected readonly IDatabaseConnectionFactory _connectionFactory;

    protected BaseRepository(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    protected IDbConnection CreateConnection()
    {
        return _connectionFactory.CreateConnection();
    }

    protected async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null)
    {
        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
    }

    protected async Task<T> QuerySingleAsync<T>(string sql, object? parameters = null)
    {
        using var connection = CreateConnection();
        return await connection.QuerySingleAsync<T>(sql, parameters);
    }

    protected async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, int? commandTimeout = null)
    {
        using var connection = CreateConnection();
        var result = await connection.QueryAsync<T>(sql, parameters, commandTimeout: commandTimeout);
        // Dapper's QueryAsync returns a List<T> cast as IEnumerable<T> internally.
        // We cast it directly to IReadOnlyList to expose it without allocating a new list.
        return result as IReadOnlyList<T> ?? result.ToList();
    }

    protected async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteAsync(sql, parameters);
    }

    protected async Task<T> ExecuteScalarAsync<T>(string sql, object? parameters = null)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<T>(sql, parameters);
    }
}
