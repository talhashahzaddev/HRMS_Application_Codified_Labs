using System.Data;
using System.Text.Json;
using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class PaymentRepository : BaseRepository, IPaymentRepository
{
    public PaymentRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<SubscriptionPlanDto?> GetSubscriptionPlanByIdAsync(Guid planId)
    {
        var sql = @"
            SELECT 
                planid,
                name,
                monthlyprice,
                annualprice,
                maxusers,
                features,
                iscustom,
                isactive
            FROM payment.subscriptionplans
            WHERE planid = @planId";

        var result = await QuerySingleOrDefaultAsync<dynamic>(sql, new { planId });
        
        if (result == null)
            return null;

        return MapToSubscriptionPlanDto(result);
    }

    public async Task<IEnumerable<SubscriptionPlanDto>> GetAllSubscriptionPlansAsync()
    {
        var sql = @"
            SELECT 
                planid,
                name,
                monthlyprice,
                annualprice,
                maxusers,
                features,
                iscustom,
                isactive
            FROM payment.subscriptionplans
            ORDER BY name";

        var results = await QueryAsync<dynamic>(sql);
        return results.Select(MapToSubscriptionPlanDto);
    }

    public async Task<IEnumerable<SubscriptionPlanDto>> GetActiveSubscriptionPlansAsync()
    {
        var sql = @"
            SELECT 
                planid,
                name,
                monthlyprice,
                annualprice,
                maxusers,
                features,
                iscustom,
                isactive
            FROM payment.subscriptionplans
            WHERE isactive = true
            ORDER BY name";

        var results = await QueryAsync<dynamic>(sql);
        return results.Select(MapToSubscriptionPlanDto);
    }

    public async Task<Guid> CreateSubscriptionPlanAsync(CreateSubscriptionPlanRequest request)
    {
        var sql = @"
            INSERT INTO payment.subscriptionplans 
                (name, monthlyprice, annualprice, maxusers, features, iscustom, isactive)
            VALUES 
                (@name, @monthlyPrice, @annualPrice, @maxUsers, @features::jsonb, @isCustom, @isActive)
            RETURNING planid";

        var featuresJson = request.Features != null ? JsonSerializer.Serialize(request.Features) : null;

        var planId = await ExecuteScalarAsync<Guid>(sql, new
        {
            name = request.Name,
            monthlyPrice = request.MonthlyPrice,
            annualPrice = request.AnnualPrice,
            maxUsers = request.MaxUsers,
            features = featuresJson,
            isCustom = request.IsCustom,
            isActive = request.IsActive
        });

        return planId;
    }

    public async Task<bool> UpdateSubscriptionPlanAsync(Guid planId, UpdateSubscriptionPlanRequest request)
    {
        var sql = @"
            UPDATE payment.subscriptionplans
            SET 
                name = @name,
                monthlyprice = @monthlyPrice,
                annualprice = @annualPrice,
                maxusers = @maxUsers,
                features = @features::jsonb,
                iscustom = @isCustom,
                isactive = @isActive
            WHERE planid = @planId";

        var featuresJson = request.Features != null ? JsonSerializer.Serialize(request.Features) : null;

        var affectedRows = await ExecuteAsync(sql, new
        {
            planId,
            name = request.Name,
            monthlyPrice = request.MonthlyPrice,
            annualPrice = request.AnnualPrice,
            maxUsers = request.MaxUsers,
            features = featuresJson,
            isCustom = request.IsCustom,
            isActive = request.IsActive
        });

        return affectedRows > 0;
    }

    public async Task<bool> InactivateSubscriptionPlanAsync(Guid planId)
    {
        var sql = @"
            UPDATE payment.subscriptionplans
            SET isactive = false
            WHERE planid = @planId";

        var affectedRows = await ExecuteAsync(sql, new { planId });
        return affectedRows > 0;
    }

    public async Task<bool> DeleteSubscriptionPlanAsync(Guid planId)
    {
        var sql = @"
            DELETE FROM payment.subscriptionplans
            WHERE planid = @planId";

        var affectedRows = await ExecuteAsync(sql, new { planId });
        return affectedRows > 0;
    }

    private SubscriptionPlanDto MapToSubscriptionPlanDto(dynamic row)
    {
        Dictionary<string, object>? features = null;
        
        if (row.features != null && !Convert.IsDBNull(row.features))
        {
            try
            {
                string raw = row.features.ToString();
                
                if (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{"))
                {
                    features = JsonSerializer.Deserialize<Dictionary<string, object>>(raw);
                }
            }
            catch
            {
                // If deserialization fails, leave features as null
                features = null;
            }
        }

        return new SubscriptionPlanDto
        {
            PlanId = row.planid,
            Name = row.name ?? string.Empty,
            MonthlyPrice = row.monthlyprice,
            AnnualPrice = row.annualprice,
            MaxUsers = row.maxusers,
            Features = features,
            IsCustom = row.iscustom ?? false,
            IsActive = row.isactive ?? true
        };
    }
}

