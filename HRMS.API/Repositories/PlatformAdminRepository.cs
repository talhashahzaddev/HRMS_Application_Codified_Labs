using HRMS.API.Models.Entities;
using HRMS.API.Configuration;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class PlatformAdminRepository : BaseRepository, IPlatformAdminRepository
{
    public PlatformAdminRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<PlatformAdmin?> GetByEmailAsync(string email)
    {
        var sql = @"
            SELECT 
                admin_id AS AdminId,
                email,
                password_hash AS PasswordHash,
                full_name AS FullName,
                is_active AS IsActive,
                last_login_at AS LastLoginAt,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM admin.platform_admins 
            WHERE LOWER(email) = LOWER(@email) AND is_active = true";

        return await QuerySingleOrDefaultAsync<PlatformAdmin>(sql, new { email });
    }

    public async Task<bool> UpdateLastLoginAsync(Guid adminId)
    {
        var sql = @"
            UPDATE admin.platform_admins 
            SET last_login_at = NOW(), updated_at = NOW()
            WHERE admin_id = @adminId";

        var rows = await ExecuteAsync(sql, new { adminId });
        return rows > 0;
    }
}
