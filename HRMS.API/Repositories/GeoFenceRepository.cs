using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class GeoFenceRepository : BaseRepository, IGeoFenceRepository
{
    private readonly ILogger<GeoFenceRepository> _logger;

    public GeoFenceRepository(
        IDatabaseConnectionFactory connectionFactory,
        ILogger<GeoFenceRepository> logger) : base(connectionFactory)
    {
        _logger = logger;
    }

    // ── Geo-Fence CRUD ──────────────────────────────────────────────────

    public async Task<IEnumerable<GeoFenceDto>> GetAllGeoFencesAsync(Guid organizationId)
    {
        const string sql = @"
            SELECT
                geofenceid      AS GeoFenceId,
                organizationid  AS OrganizationId,
                name            AS Name,
                description     AS Description,
                address         AS Address,
                center_latitude AS CenterLatitude,
                center_longitude AS CenterLongitude,
                radius_meters   AS RadiusMeters,
                polygon_coords::text AS PolygonCoords,
                shape           AS Shape,
                isactive        AS IsActive,
                createdat       AS CreatedAt
            FROM attendance.geo_fences
            WHERE organizationid = @organizationId
              AND deleted_at IS NULL
            ORDER BY createdat DESC";

        return await QueryAsync<GeoFenceDto>(sql, new { organizationId });
    }

    public async Task<GeoFenceDto?> GetGeoFenceByIdAsync(Guid geoFenceId)
    {
        const string sql = @"
            SELECT
                geofenceid      AS GeoFenceId,
                organizationid  AS OrganizationId,
                name            AS Name,
                description     AS Description,
                address         AS Address,
                center_latitude AS CenterLatitude,
                center_longitude AS CenterLongitude,
                radius_meters   AS RadiusMeters,
                polygon_coords::text AS PolygonCoords,
                shape           AS Shape,
                isactive        AS IsActive,
                createdat       AS CreatedAt
            FROM attendance.geo_fences
            WHERE geofenceid = @geoFenceId
              AND deleted_at IS NULL";

        return await QuerySingleOrDefaultAsync<GeoFenceDto>(sql, new { geoFenceId });
    }

    public async Task<Guid> CreateGeoFenceAsync(Guid organizationId, Guid createdBy, CreateGeoFenceDto dto)
    {
        const string sql = @"
            INSERT INTO attendance.geo_fences
                (organizationid, name, description, address,
                 center_latitude, center_longitude, radius_meters,
                 polygon_coords, shape, isactive, createdby)
            VALUES
                (@organizationId, @Name, @Description, @Address,
                 @CenterLatitude, @CenterLongitude, @RadiusMeters,
                 @PolygonCoords::jsonb, @Shape, TRUE, @createdBy)
            RETURNING geofenceid";

        return await ExecuteScalarAsync<Guid>(sql, new
        {
            organizationId,
            createdBy,
            dto.Name,
            dto.Description,
            dto.Address,
            dto.CenterLatitude,
            dto.CenterLongitude,
            dto.RadiusMeters,
            PolygonCoords = dto.PolygonCoords ?? "null",
            dto.Shape
        });
    }

    public async Task<bool> UpdateGeoFenceAsync(Guid geoFenceId, Guid organizationId, UpdateGeoFenceDto dto)
    {
        const string sql = @"
            UPDATE attendance.geo_fences SET
                name             = @Name,
                description      = @Description,
                address          = @Address,
                center_latitude  = @CenterLatitude,
                center_longitude = @CenterLongitude,
                radius_meters    = @RadiusMeters,
                polygon_coords   = @PolygonCoords::jsonb,
                shape            = @Shape,
                isactive         = @IsActive,
                updatedat        = NOW()
            WHERE geofenceid    = @geoFenceId
              AND organizationid = @organizationId
              AND deleted_at IS NULL";

        var rows = await ExecuteAsync(sql, new
        {
            geoFenceId,
            organizationId,
            dto.Name,
            dto.Description,
            dto.Address,
            dto.CenterLatitude,
            dto.CenterLongitude,
            dto.RadiusMeters,
            PolygonCoords = dto.PolygonCoords ?? "null",
            dto.Shape,
            dto.IsActive
        });
        return rows > 0;
    }

    public async Task<bool> SoftDeleteGeoFenceAsync(Guid geoFenceId, Guid organizationId)
    {
        const string sql = @"
            UPDATE attendance.geo_fences
            SET deleted_at = NOW(), isactive = FALSE
            WHERE geofenceid = @geoFenceId AND organizationid = @organizationId";

        return await ExecuteAsync(sql, new { geoFenceId, organizationId }) > 0;
    }

    public async Task<bool> ToggleGeoFenceActiveAsync(Guid geoFenceId, Guid organizationId, bool isActive)
    {
        const string sql = @"
            UPDATE attendance.geo_fences
            SET isactive = @isActive, updatedat = NOW()
            WHERE geofenceid = @geoFenceId AND organizationid = @organizationId AND deleted_at IS NULL";

        return await ExecuteAsync(sql, new { geoFenceId, organizationId, isActive }) > 0;
    }

    // ── Shift ↔ Geo-Fence ────────────────────────────────────────────────

    public async Task<IEnumerable<ShiftGeoFenceDto>> GetGeoFencesByShiftAsync(Guid shiftId)
    {
        const string sql = @"
            SELECT
                sgf.id           AS Id,
                sgf.shiftid      AS ShiftId,
                sgf.geofenceid   AS GeoFenceId,
                gf.name          AS GeoFenceName,
                gf.radius_meters AS RadiusMeters,
                sgf.isactive     AS IsActive
            FROM attendance.shift_geo_fences sgf
            JOIN attendance.geo_fences gf ON gf.geofenceid = sgf.geofenceid
            WHERE sgf.shiftid = @shiftId AND sgf.isactive = TRUE AND gf.deleted_at IS NULL";

        return await QueryAsync<ShiftGeoFenceDto>(sql, new { shiftId });
    }

    public async Task<bool> LinkGeoFenceToShiftAsync(Guid shiftId, Guid geoFenceId)
    {
        const string sql = @"
            INSERT INTO attendance.shift_geo_fences (shiftid, geofenceid, isactive)
            VALUES (@shiftId, @geoFenceId, TRUE)
            ON CONFLICT (shiftid, geofenceid) DO UPDATE SET isactive = TRUE";

        return await ExecuteAsync(sql, new { shiftId, geoFenceId }) > 0;
    }

    public async Task<bool> UnlinkGeoFenceFromShiftAsync(Guid shiftId, Guid geoFenceId)
    {
        const string sql = @"
            UPDATE attendance.shift_geo_fences
            SET isactive = FALSE
            WHERE shiftid = @shiftId AND geofenceid = @geoFenceId";

        return await ExecuteAsync(sql, new { shiftId, geoFenceId }) > 0;
    }

    // ── Face Descriptors ─────────────────────────────────────────────────

    public async Task<EmployeeFaceDescriptor?> GetActiveFaceDescriptorAsync(Guid employeeId)
    {
        const string sql = @"
            SELECT
                descriptorid      AS DescriptorId,
                employeeid        AS EmployeeId,
                descriptor_vector::text AS DescriptorVector,
                source_photo_url  AS SourcePhotoUrl,
                isactive          AS IsActive,
                generatedat       AS GeneratedAt
            FROM employee.employee_face_descriptors
            WHERE employeeid = @employeeId AND isactive = TRUE
            LIMIT 1";

        return await QuerySingleOrDefaultAsync<EmployeeFaceDescriptor>(sql, new { employeeId });
    }

    public async Task<Guid> SaveFaceDescriptorAsync(Guid employeeId, Guid generatedBy, SaveFaceDescriptorRequest dto)
    {
        // Deactivate previous descriptor first
        const string deactivateSql = @"
            UPDATE employee.employee_face_descriptors
            SET isactive = FALSE, superseded_at = NOW()
            WHERE employeeid = @employeeId AND isactive = TRUE";

        await ExecuteAsync(deactivateSql, new { employeeId });

        const string insertSql = @"
            INSERT INTO employee.employee_face_descriptors
                (employeeid, descriptor_vector, source_photo_url, isactive, generatedby)
            VALUES
                (@employeeId, @DescriptorVector::jsonb, @SourcePhotoUrl, TRUE, @generatedBy)
            RETURNING descriptorid";

        return await ExecuteScalarAsync<Guid>(insertSql, new
        {
            employeeId,
            generatedBy,
            dto.DescriptorVector,
            dto.SourcePhotoUrl
        });
    }

    // ── Face Recognition Logs ────────────────────────────────────────────

    public async Task<Guid> CreateFaceLogAsync(Guid employeeId, SubmitFaceLogRequest dto)
    {
        var safeConfidence = dto.ConfidenceScore.HasValue
    ? Math.Clamp(dto.ConfidenceScore.Value, 0m, 1m)
    : (decimal?)null;

        var safeDistance = dto.DistanceScore.HasValue
            ? Math.Clamp(dto.DistanceScore.Value, 0m, 1m)
            : (decimal?)null;
        const string sql = @"
            INSERT INTO attendance.face_recognition_logs
                (employeeid, face_descriptor, reference_photo_url,
                 match_result, confidence_score, distance_score, threshold_used,
                 geofenceid, latitude, longitude, device_info)
            VALUES
                (@employeeId, @FaceDescriptor::jsonb, @ReferencePhotoUrl,
                 @MatchResult, @ConfidenceScore, @DistanceScore, 0.6,
                 @GeoFenceId, @Latitude, @Longitude, @DeviceInfo::jsonb)
            RETURNING logid";

        return await ExecuteScalarAsync<Guid>(sql, new
        {
            employeeId,
            FaceDescriptor = dto.FaceDescriptor ?? "null",
            dto.ReferencePhotoUrl,
            dto.MatchResult,
            ConfidenceScore = safeConfidence,   // ← use clamped values
            DistanceScore = safeDistance,
            dto.GeoFenceId,
            dto.Latitude,
            dto.Longitude,
            DeviceInfo = dto.DeviceInfo ?? "{}"
        });
    }

    public async Task<IEnumerable<FaceRecognitionLogDto>> GetFaceLogsByEmployeeAsync(Guid employeeId, int limit = 10)
    {
        const string sql = @"
            SELECT
                fl.logid              AS LogId,
                fl.employeeid         AS EmployeeId,
                (e.firstname || ' ' || e.lastname) AS EmployeeName,
                e.profilepictureurl   AS ProfilePhotoUrl,
                fl.attempttime        AS AttemptTime,
                fl.match_result       AS MatchResult,
                fl.confidence_score   AS ConfidenceScore,
                fl.distance_score     AS DistanceScore,
                fl.latitude           AS Latitude,
                fl.longitude          AS Longitude,
                fl.manually_overridden AS ManuallyOverridden,
                fl.attendanceid       AS AttendanceId
            FROM attendance.face_recognition_logs fl
            JOIN employee.employees e ON e.employeeid = fl.employeeid
            WHERE fl.employeeid = @employeeId
            ORDER BY fl.attempttime DESC
            LIMIT @limit";

        return await QueryAsync<FaceRecognitionLogDto>(sql, new { employeeId, limit });
    }

    // ── Geo-Aware Clock In/Out ───────────────────────────────────────────

    public async Task<GeoClockInResponse> GeoClockInOutAsync(Guid employeeId, GeoClockInRequest request)
    {
        const string sql = @"
            SELECT
                success          AS Success,
                attendanceid     AS AttendanceId,
                sessionid        AS SessionId,
                location_status  AS LocationStatus,
                distance_meters  AS DistanceMeters,
                violation_id     AS ViolationId,
                message          AS Message
            FROM attendance.fn_clock_in_out_geo(
                @employeeId,
                @Action,
                NOW(),
                @Location::jsonb,
                @Notes,
                @GeoFenceId,
                @Latitude,
                @Longitude,
                @FaceLogId
            )";

        var locationJson = System.Text.Json.JsonSerializer.Serialize(
            new { source = "geo_app", timestamp = DateTime.UtcNow });

        return await QuerySingleAsync<GeoClockInResponse>(sql, new
        {
            employeeId,
            request.Action,
            Location = locationJson,
            request.Notes,
            request.GeoFenceId,
            request.Latitude,
            request.Longitude,
            FaceLogId = (Guid?)null   // filled in by service layer after face log created
        });
    }

    // ── Views ────────────────────────────────────────────────────────────

    public async Task<IEnumerable<TeamLiveStreamDto>> GetTeamLiveStreamAsync(Guid organizationId)
    {
        const string sql = @"
            SELECT
                employeeid         AS EmployeeId,
                employeecode       AS EmployeeCode,
                employee_name      AS EmployeeName,
                profilepictureurl  AS ProfilePhotoUrl,
                organizationid     AS OrganizationId,
                last_checkin       AS LastCheckIn,
                location_status    AS LocationStatus,
                distance_from_fence_m AS DistanceFromFenceM,
                geo_fence_name     AS GeoFenceName,
                latitude           AS Latitude,
                longitude          AS Longitude,
                device_ip          AS DeviceIp,
                total_hours_worked AS TotalHours,
                compliance_pct     AS CompliancePct,
                shiftname          AS ShiftName,
                attendance_status  AS AttendanceStatus
            FROM attendance.vw_team_live_stream
            WHERE organizationid = @organizationId";

        return await QueryAsync<TeamLiveStreamDto>(sql, new { organizationId });
    }

    // ── BUG FIX #1: Violations query ────────────────────────────────────
    // PROBLEMS FIXED:
    //   1. endDate was passed as midnight (00:00:00) so the entire selected end-day
    //      was excluded from BETWEEN. We now add 1 day minus 1 tick to include
    //      the full end day.
    //   2. Dapper passes C# nullable Guid as a typed NULL which PostgreSQL
    //      cannot match to uuid column with "IS NULL OR shift_id = @shiftId".
    //      We explicitly cast to uuid using DynamicParameters so Postgres
    //      receives a proper typed NULL.
    public async Task<IEnumerable<GeoFenceViolationReportDto>> GetViolationsReportAsync(
        Guid organizationId, DateTime startDate, DateTime endDate, Guid? shiftId = null)
    {
        // Include the full end day (up to 23:59:59.999)
        var endDateInclusive = endDate.Date.AddDays(1).AddTicks(-1);

        const string sql = @"
            SELECT
                violation_id         AS ViolationId,
                violated_at          AS ViolatedAt,
                action_taken         AS ActionTaken,
                distance_over_meters AS DistanceOverMeters,
                actual_latitude      AS ActualLatitude,
                actual_longitude     AS ActualLongitude,
                resolved             AS Resolved,
                employee_id          AS EmployeeId,
                employee_code        AS EmployeeCode,
                employee_name        AS EmployeeName,
                profilepictureurl    AS ProfilePhotoUrl,
                geo_fence_id         AS GeoFenceId,
                geo_fence_name       AS GeoFenceName,
                center_latitude      AS CenterLatitude,
                center_longitude     AS CenterLongitude,
                radius_meters        AS RadiusMeters,
                shift_id             AS ShiftId,
                shift_name           AS ShiftName,
                face_match_result    AS FaceMatchResult,
                face_confidence_score AS FaceConfidenceScore,
                face_reference_photo_url AS FaceReferencePhotoUrl
            FROM attendance.vw_geo_fence_violations_report
            WHERE organizationid = @organizationId
              AND violated_at BETWEEN @startDate AND @endDate
              AND (@shiftId IS NULL OR shift_id = @shiftId::uuid)
            ORDER BY violated_at DESC";

        // Use DynamicParameters so Dapper sends the nullable Guid with
        // the correct DbType, preventing Postgres type-mismatch errors.
        var p = new DynamicParameters();
        p.Add("organizationId", organizationId);
        p.Add("startDate", startDate.Date);              // midnight of start day
        p.Add("endDate", endDateInclusive);              // 23:59:59.999 of end day
        p.Add("shiftId", shiftId, System.Data.DbType.Guid);

        return await QueryAsync<GeoFenceViolationReportDto>(sql, p);
    }

    public async Task<SystemHealthDto> GetSystemHealthAsync(Guid organizationId)
    {
        const string sql = @"
            SELECT
                totalemployees       AS TotalEmployees,
                activeemployees      AS ActiveEmployees,
                presenttoday         AS PresentToday,
                totaldepartments     AS TotalDepartments,
                geofenceviolationstoday AS GeoFenceViolationsToday,
                employeesinzone      AS EmployeesInZone
            FROM admin.fn_get_dashboard_stats_v2(@organizationId)";

        var row = await QuerySingleOrDefaultAsync<dynamic>(sql, new { organizationId });

        if (row == null) return new SystemHealthDto();

        int total = (int)(row.TotalEmployees ?? 0);
        int present = (int)(row.PresentToday ?? 0);
        decimal health = total > 0 ? Math.Round((decimal)present / total * 100, 2) : 0;

        return new SystemHealthDto
        {
            TotalEmployees = total,
            ActiveEmployees = (int)(row.ActiveEmployees ?? 0),
            PresentToday = present,
            TotalDepartments = (int)(row.TotalDepartments ?? 0),
            GeoFenceViolationsToday = (int)(row.GeoFenceViolationsToday ?? 0),
            EmployeesInZone = (int)(row.EmployeesInZone ?? 0),
            AttendanceHealthPct = health
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    public async Task<decimal?> GetDistanceToFenceAsync(decimal latitude, decimal longitude, Guid geoFenceId)
    {
        const string sql = @"
            SELECT attendance.fn_distance_to_fence_meters(@latitude, @longitude, @geoFenceId)";

        return await ExecuteScalarAsync<decimal?>(sql, new { latitude, longitude, geoFenceId });
    }

    public async Task<bool> IsWithinGeoFenceAsync(decimal latitude, decimal longitude, Guid geoFenceId)
    {
        const string sql = @"
            SELECT attendance.fn_is_within_geo_fence(@latitude, @longitude, @geoFenceId)";

        return await ExecuteScalarAsync<bool>(sql, new { latitude, longitude, geoFenceId });
    }
}
