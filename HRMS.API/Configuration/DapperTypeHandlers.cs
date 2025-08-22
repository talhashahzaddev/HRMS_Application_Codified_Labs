using Dapper;
using System.Data;
using System.Text.Json;

namespace HRMS.API.Configuration;

public class JsonDictionaryTypeHandler : SqlMapper.TypeHandler<Dictionary<string, object>>
{
    public override void SetValue(IDbDataParameter parameter, Dictionary<string, object>? value)
    {
        parameter.Value = value != null ? JsonSerializer.Serialize(value) : DBNull.Value;
    }

    public override Dictionary<string, object> Parse(object value)
    {
        if (value == null || value == DBNull.Value)
            return new Dictionary<string, object>();

        string json = value.ToString() ?? "{}";
        
        try
        {
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            return result ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}

public static class DapperConfiguration
{
    private static bool _isConfigured = false;

    public static void Configure()
    {
        if (_isConfigured) return;

        // Register type handlers for JSON columns
        SqlMapper.AddTypeHandler(new JsonDictionaryTypeHandler());

        // Register type mappings for underscore column names
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        _isConfigured = true;
    }
}
