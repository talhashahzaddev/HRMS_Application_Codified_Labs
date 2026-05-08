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

    protected async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null)
    {
        using var connection = CreateConnection();
        return await connection.QueryAsync<T>(sql, parameters);
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
