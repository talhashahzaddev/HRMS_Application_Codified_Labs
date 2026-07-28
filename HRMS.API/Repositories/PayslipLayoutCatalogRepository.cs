using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class PayslipLayoutCatalogRepository : BaseRepository, IPayslipLayoutCatalogRepository
{
    public PayslipLayoutCatalogRepository(IDatabaseConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    public async Task<IReadOnlyList<PayslipLayoutCatalogDto>> GetActiveLayoutsAsync()
    {
        const string sql = @"
            SELECT layoutkey       AS LayoutKey,
                   displayname     AS DisplayName,
                   description     AS Description,
                   previewimageurl AS PreviewImageUrl,
                   rendererkey     AS RendererKey,
                   sortorder       AS SortOrder
            FROM payroll.paysliplayoutcatalog
            WHERE isactive = true
            ORDER BY sortorder ASC, displayname ASC;";

        var rows = await QueryAsync<PayslipLayoutCatalogDto>(sql);
        return rows.ToList();
    }

    public async Task<PayslipLayoutCatalogDto?> GetByLayoutKeyAsync(string layoutKey)
    {
        const string sql = @"
            SELECT layoutkey       AS LayoutKey,
                   displayname     AS DisplayName,
                   description     AS Description,
                   previewimageurl AS PreviewImageUrl,
                   rendererkey     AS RendererKey,
                   sortorder       AS SortOrder
            FROM payroll.paysliplayoutcatalog
            WHERE layoutkey = @layoutKey
              AND isactive = true
            LIMIT 1;";

        return await QuerySingleOrDefaultAsync<PayslipLayoutCatalogDto>(sql, new { layoutKey });
    }

    public async Task<bool> IsActiveLayoutKeyAsync(string layoutKey)
    {
        const string sql = @"
            SELECT EXISTS (
                SELECT 1
                FROM payroll.paysliplayoutcatalog
                WHERE layoutkey = @layoutKey
                  AND isactive = true
            );";

        return await QuerySingleOrDefaultAsync<bool>(sql, new { layoutKey });
    }
}
