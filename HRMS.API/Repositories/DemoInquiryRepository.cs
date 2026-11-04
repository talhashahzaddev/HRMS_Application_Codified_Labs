using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class DemoInquiryRepository : BaseRepository, IDemoInquiryRepository
{
    public DemoInquiryRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<Guid> CreateAsync(DemoInquiry inquiry)
    {
        var sql = @"
            INSERT INTO admin.demo_inquiries 
                (full_name, email, phone, company_name, company_size, industry, country, message, status, source)
            VALUES 
                (@fullName, @email, @phone, @companyName, @companySize, @industry, @country, @message, @status, @source)
            RETURNING inquiry_id";

        return await ExecuteScalarAsync<Guid>(sql, new
        {
            fullName = inquiry.FullName,
            email = inquiry.Email,
            phone = inquiry.Phone,
            companyName = inquiry.CompanyName,
            companySize = inquiry.CompanySize,
            industry = inquiry.Industry,
            country = inquiry.Country,
            message = inquiry.Message,
            status = inquiry.Status,
            source = inquiry.Source
        });
    }

    public async Task<DemoInquiry?> GetByIdAsync(Guid inquiryId)
    {
        var sql = @"
            SELECT 
                inquiry_id AS InquiryId,
                full_name AS FullName,
                email,
                phone,
                company_name AS CompanyName,
                company_size AS CompanySize,
                industry,
                country,
                message,
                status,
                notes,
                source,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM admin.demo_inquiries 
            WHERE inquiry_id = @inquiryId";

        return await QuerySingleOrDefaultAsync<DemoInquiry>(sql, new { inquiryId });
    }

    public async Task<PagedResult<DemoInquiryDto>> GetAllPagedAsync(DemoInquiryFilterRequest filter)
    {
        var whereClause = "WHERE 1=1";
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            whereClause += " AND (LOWER(full_name) LIKE @search OR LOWER(email) LIKE @search OR LOWER(company_name) LIKE @search)";
            parameters.Add("search", $"%{filter.Search.ToLower()}%");
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            whereClause += " AND status = @status";
            parameters.Add("status", filter.Status);
        }

        // Whitelist sort columns to prevent SQL injection
        var allowedSortColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "createdat", "created_at" },
            { "fullname", "full_name" },
            { "email", "email" },
            { "companyname", "company_name" },
            { "status", "status" }
        };

        var sortColumn = allowedSortColumns.ContainsKey(filter.SortBy ?? "createdat")
            ? allowedSortColumns[filter.SortBy ?? "createdat"]
            : "created_at";
        var sortDir = filter.SortDirection?.ToLower() == "asc" ? "ASC" : "DESC";

        // Count query
        var countSql = $"SELECT COUNT(*) FROM admin.demo_inquiries {whereClause}";
        using var connection = CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        // Data query
        var offset = (filter.Page - 1) * filter.PageSize;
        parameters.Add("limit", filter.PageSize);
        parameters.Add("offset", offset);

        var dataSql = $@"
            SELECT 
                inquiry_id AS InquiryId,
                full_name AS FullName,
                email,
                phone,
                company_name AS CompanyName,
                company_size AS CompanySize,
                industry,
                country,
                message,
                status,
                notes,
                source,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM admin.demo_inquiries 
            {whereClause}
            ORDER BY {sortColumn} {sortDir}
            LIMIT @limit OFFSET @offset";

        var data = await connection.QueryAsync<DemoInquiryDto>(dataSql, parameters);

        return new PagedResult<DemoInquiryDto>
        {
            Data = data.ToArray(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<bool> UpdateStatusAsync(Guid inquiryId, string status, string? notes)
    {
        var sql = @"
            UPDATE admin.demo_inquiries 
            SET status = @status, 
                notes = COALESCE(@notes, notes),
                updated_at = NOW()
            WHERE inquiry_id = @inquiryId";

        var rows = await ExecuteAsync(sql, new { inquiryId, status, notes });
        return rows > 0;
    }

    public async Task<int> GetCountByStatusAsync(string? status = null)
    {
        var sql = "SELECT COUNT(*) FROM admin.demo_inquiries";
        if (!string.IsNullOrWhiteSpace(status))
        {
            sql += " WHERE status = @status";
            return await ExecuteScalarAsync<int>(sql, new { status });
        }
        return await ExecuteScalarAsync<int>(sql);
    }
}
