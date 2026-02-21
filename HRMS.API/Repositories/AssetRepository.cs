using System.Text;
using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.ViewModels;
using HRMS.API.Repositories.Interfaces;
using Npgsql;
using System.Linq;

namespace HRMS.API.Repositories;

public class AssetRepository : BaseRepository, IAssetRepository
{
    public AssetRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    // ==================== CREATE ASSET ====================
    public async Task<AssetDto> CreateAssetAsync(Guid organizationId, CreateAssetRequest request, Guid? createdBy)
    {
        var assetId = Guid.NewGuid();
        const string sql = @"
            INSERT INTO assets.assets (
                id, name, code, status, purchasedate, description, typeid, createdat, organization_id
            ) VALUES (
                @id, @name, @code, @status, @purchaseDate, @description, @typeId, CURRENT_TIMESTAMP, @organizationId
            )
            RETURNING id::uuid AS AssetId,
                      typeid::uuid AS AssetTypeId,
                      (SELECT name FROM assets.assettypes WHERE id = typeid) AS AssetTypeName,
                      code AS AssetTag,
                      name AS Name,
                      description AS Description,
                      purchasedate AS PurchaseDate,
                      status AS Status,
                      createdat AS CreatedAt,
                      updatedat AS UpdatedAt;";

        return await QuerySingleAsync<AssetDto>(sql, new
        {
            id = assetId.ToString(),
            name = request.Name.Trim(),
            code = request.AssetTag?.Trim(),
            status = "Available",
            purchaseDate = request.PurchaseDate,
            description = request.Description,
            typeId = request.AssetTypeId.ToString(), // varchar(36)
            organizationId
        });
    }

    // ==================== CREATE ASSET TYPE ====================
    public async Task<AssetTypeDto> CreateAssetTypeAsync(Guid organizationId, CreateAssetTypeRequest request, Guid? createdBy)
    {
        var typeId = Guid.NewGuid();
        const string sql = @"
            INSERT INTO assets.assettypes (id, name, description, createdat)
            VALUES (@id, @name, @description, CURRENT_TIMESTAMP)
            RETURNING id::uuid AS AssetTypeId,
                      name AS Name,
                      description AS Description,
                      true AS IsActive,
                      createdat AS CreatedAt,
                      updatedat AS UpdatedAt;";

        return await QuerySingleAsync<AssetTypeDto>(sql, new
        {
            id = typeId.ToString(),
            name = request.Name.Trim(),
            description = request.Description
        });
    }

    // ==================== GET ASSET TYPE BY ID ====================
    public async Task<AssetTypeDto?> GetAssetTypeByIdAsync(Guid organizationId, Guid assetTypeId)
    {
        const string sql = @"
            SELECT id::uuid AS AssetTypeId,
                   name AS Name,
                   description AS Description,
                   true AS IsActive,
                   createdat AS CreatedAt,
                   updatedat AS UpdatedAt
            FROM assets.assettypes
            WHERE id = @assetTypeId
              AND isdeleted = false;";

        return await QuerySingleOrDefaultAsync<AssetTypeDto>(sql, new { assetTypeId = assetTypeId.ToString() });
    }

    // ==================== GET ALL ASSET TYPES ====================
    public async Task<List<AssetTypeDto>> GetAssetTypesAsync(Guid organizationId)
    {
        const string sql = @"
            SELECT id::uuid AS AssetTypeId,
                   name AS Name,
                   description AS Description,
                   true AS IsActive,
                   createdat AS CreatedAt,
                   updatedat AS UpdatedAt
            FROM assets.assettypes
            WHERE isdeleted = false
            ORDER BY name;";

        var result = await QueryAsync<AssetTypeDto>(sql);
        return result.ToList();
    }

    // ==================== UPDATE ASSET TYPE ====================
    public async Task<bool> UpdateAssetTypeAsync(Guid organizationId, Guid assetTypeId, UpdateAssetTypeRequest request)
    {
        const string sql = @"
            UPDATE assets.assettypes
            SET name = @name,
                description = @description,
                updatedat = CURRENT_TIMESTAMP
            WHERE id = @assetTypeId
              AND isdeleted = false;";

        var affectedRows = await ExecuteAsync(sql, new
        {
            assetTypeId = assetTypeId.ToString(),
            name = request.Name.Trim(),
            description = request.Description
        });

        return affectedRows > 0;
    }

    // ==================== SOFT DELETE ASSET TYPE ====================
    public async Task<bool> SoftDeleteAssetTypeAsync(Guid organizationId, Guid assetTypeId, Guid? deletedBy)
    {
        const string sql = @"
            UPDATE assets.assettypes
            SET isdeleted = true,
                updatedat = CURRENT_TIMESTAMP
            WHERE id = @assetTypeId
              AND isdeleted = false;";

        var affectedRows = await ExecuteAsync(sql, new { assetTypeId = assetTypeId.ToString() });
        return affectedRows > 0;
    }

    // ==================== ASSIGN ASSET ====================
    public async Task<AssetAssignmentDto> AssignAssetAsync(Guid organizationId, AssignAssetRequest request, Guid? createdBy)
    {
        using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var assignment = await connection.QuerySingleAsync<AssetAssignmentDto>(@"
                INSERT INTO assets.asset_assignments (
                    organization_id, asset_id, employee_id, assigned_at, due_date, status, notes, created_by
                )
                VALUES (@organizationId, @assetId, @employeeId, @assignedAt, @dueDate, 'Active', @notes, @createdBy)
                RETURNING assignment_id AS AssignmentId,
                          asset_id AS AssetId,
                          employee_id AS EmployeeId,
                          assigned_at AS AssignedAt,
                          due_date AS DueDate,
                          returned_at AS ReturnedAt,
                          status AS Status,
                          notes AS Notes;",
                new
                {
                    organizationId,
                    assetId = request.AssetId, // uuid column
                    employeeId = request.EmployeeId,
                    assignedAt = request.AssignedAt ?? DateTime.UtcNow,
                    dueDate = request.DueDate,
                    notes = request.Notes,
                    createdBy
                }, transaction);

            await connection.ExecuteAsync(@"
                UPDATE assets.assets
                SET status = 'Assigned', updatedat = CURRENT_TIMESTAMP
                WHERE id = @assetId
                  AND isdeleted = false;",
                new { assetId = request.AssetId.ToString() }, transaction);

            await transaction.CommitAsync();
            return assignment;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ==================== RETURN ASSET ====================
    public async Task<AssetAssignmentDto> ReturnAssetAsync(Guid organizationId, ReturnAssetRequest request, Guid? updatedBy)
    {
        using var connection = (NpgsqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var assignment = await connection.QuerySingleAsync<AssetAssignmentDto>(@"
                UPDATE assets.asset_assignments
                SET returned_at = @returnedAt,
                    status = 'Returned',
                    notes = COALESCE(@notes, notes),
                    updated_at = CURRENT_TIMESTAMP
                WHERE assignment_id = @assignmentId
                  AND organization_id = @organizationId
                  AND is_deleted = false
                RETURNING assignment_id AS AssignmentId,
                          asset_id AS AssetId,
                          employee_id AS EmployeeId,
                          assigned_at AS AssignedAt,
                          due_date AS DueDate,
                          returned_at AS ReturnedAt,
                          status AS Status,
                          notes AS Notes;",
                new
                {
                    organizationId,
                    assignmentId = request.AssignmentId,
                    returnedAt = request.ReturnedAt ?? DateTime.UtcNow,
                    notes = request.Notes,
                    updatedBy
                }, transaction);

            await connection.ExecuteAsync(@"
                UPDATE assets.assets
                SET status = 'Available', updatedat = CURRENT_TIMESTAMP
                WHERE id = @assetId
                  AND isdeleted = false;",
                new { assetId = assignment.AssetId.ToString() }, transaction);

            await transaction.CommitAsync();
            return assignment;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ==================== GET ASSET ASSIGNMENT HISTORY ====================
    public async Task<List<AssetAssignmentDto>> GetAssetAssignmentHistoryAsync(Guid organizationId, Guid? assetId, Guid? employeeId)
    {
        const string sql = @"
            SELECT aa.assignment_id AS AssignmentId,
                   aa.asset_id AS AssetId,
                   a.code AS AssetTag,
                   a.name AS AssetName,
                   aa.employee_id AS EmployeeId,
                   CONCAT(e.firstname, ' ', e.lastname) AS EmployeeName,
                   aa.assigned_at AS AssignedAt,
                   aa.due_date AS DueDate,
                   aa.returned_at AS ReturnedAt,
                   CASE
                       WHEN aa.status = 'Active' AND aa.due_date IS NOT NULL AND aa.due_date < NOW() THEN 'Overdue'
                       ELSE aa.status
                   END AS Status,
                   aa.notes AS Notes
            FROM assets.asset_assignments aa
            INNER JOIN assets.assets a ON a.id = aa.asset_id::text
            INNER JOIN employee.employees e ON e.employeeid = aa.employee_id
            WHERE aa.organization_id = @organizationId
              AND aa.is_deleted = false
              AND a.isdeleted = false
              AND (@assetId IS NULL OR aa.asset_id = @assetId)
              AND (@employeeId IS NULL OR aa.employee_id = @employeeId)
            ORDER BY aa.assigned_at DESC;";

        var result = await QueryAsync<AssetAssignmentDto>(sql, new { organizationId, assetId, employeeId });
        return result.ToList();
    }

    // ==================== GET ASSET BY ID ====================
    public async Task<AssetDto?> GetAssetByIdAsync(Guid organizationId, Guid assetId)
    {
        const string sql = @"
            SELECT a.id::uuid AS AssetId,
                   a.typeid::uuid AS AssetTypeId,
                   t.name AS AssetTypeName,
                   a.code AS AssetTag,
                   a.name AS Name,
                   a.description AS Description,
                   a.purchasedate AS PurchaseDate,
                   a.status AS Status,
                   a.createdat AS CreatedAt,
                   a.updatedat AS UpdatedAt
            FROM assets.assets a
            INNER JOIN assets.assettypes t ON t.id = a.typeid
            WHERE a.id = @assetId::text
              AND a.isdeleted = false;";

        return await QuerySingleOrDefaultAsync<AssetDto>(sql, new { assetId = assetId.ToString() });
    }

    // ==================== GET ASSETS ====================
    public async Task<List<AssetDto>> GetAssetsAsync(Guid organizationId, AssetSearchViewModel searchModel)
    {
        var sql = new StringBuilder(@"
            SELECT a.id::uuid AS AssetId,
                   a.typeid::uuid AS AssetTypeId,
                   t.name AS AssetTypeName,
                   a.code AS AssetTag,
                   a.name AS Name,
                   a.description AS Description,
                   a.purchasedate AS PurchaseDate,
                   a.status AS Status,
                   a.createdat AS CreatedAt,
                   a.updatedat AS UpdatedAt
            FROM assets.assets a
            INNER JOIN assets.assettypes t ON t.id = a.typeid
            WHERE a.isdeleted = false
              AND t.isdeleted = false");

        var parameters = new DynamicParameters();
        parameters.Add("organizationId", organizationId);

        if (searchModel.AssetTypeId.HasValue)
        {
            sql.Append(" AND a.typeid = @assetTypeId");
            parameters.Add("assetTypeId", searchModel.AssetTypeId.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(searchModel.Status))
        {
            sql.Append(" AND a.status = @status");
            parameters.Add("status", searchModel.Status);
        }

        sql.Append(" ORDER BY a.createdat DESC");

        var result = await QueryAsync<AssetDto>(sql.ToString(), parameters);
        return result.ToList();
    }

    // ==================== UPDATE ASSET ====================
    public async Task<bool> UpdateAssetAsync(Guid organizationId, Guid assetId, UpdateAssetRequest request)
    {
        const string sql = @"
            UPDATE assets.assets
            SET typeid = @typeId,
                code = @code,
                name = @name,
                description = @description,
                purchasedate = @purchaseDate,
                status = @status,
                updatedat = CURRENT_TIMESTAMP
            WHERE id = @assetId
              AND isdeleted = false;";

        var affectedRows = await ExecuteAsync(sql, new
        {
            assetId = assetId.ToString(),
            typeId = request.AssetTypeId.ToString(),
            code = request.AssetTag.Trim(),
            name = request.Name.Trim(),
            description = request.Description,
            purchaseDate = request.PurchaseDate,
            status = request.Status
        });

        return affectedRows > 0;
    }

    // ==================== SOFT DELETE ASSET ====================
    public async Task<bool> SoftDeleteAssetAsync(Guid organizationId, Guid assetId, Guid? deletedBy)
    {
        const string sql = @"
            UPDATE assets.assets
            SET isdeleted = true,
                updatedat = CURRENT_TIMESTAMP
            WHERE id = @assetId
              AND isdeleted = false;";

        var affectedRows = await ExecuteAsync(sql, new { assetId = assetId.ToString() });
        return affectedRows > 0;
    }

    // ==================== UPDATE ASSET STATUS ====================
    public async Task<bool> UpdateAssetStatusAsync(Guid organizationId, Guid assetId, string status, Guid? updatedBy)
    {
        const string sql = @"
            UPDATE assets.assets
            SET status = @status,
                updatedat = CURRENT_TIMESTAMP
            WHERE id = @assetId
              AND isdeleted = false;";

        var affectedRows = await ExecuteAsync(sql, new { assetId = assetId.ToString(), status });
        return affectedRows > 0;
    }
}
