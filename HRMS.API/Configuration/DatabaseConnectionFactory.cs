using Npgsql;
using System.Data;

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
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
