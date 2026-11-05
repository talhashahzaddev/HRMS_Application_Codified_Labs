using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IGeoFenceRepository
{
    // Geo-Fences CRUD
    Task<IReadOnlyList<GeoFenceDto>> GetAllGeoFencesAsync(Guid organizationId);
    Task<GeoFenceDto?> GetGeoFenceByIdAsync(Guid geoFenceId);
    Task<Guid> CreateGeoFenceAsync(Guid organizationId, Guid createdBy, CreateGeoFenceDto dto);
    Task<bool> UpdateGeoFenceAsync(Guid geoFenceId, Guid organizationId, UpdateGeoFenceDto dto);
    Task<bool> SoftDeleteGeoFenceAsync(Guid geoFenceId, Guid organizationId);
    Task<bool> ToggleGeoFenceActiveAsync(Guid geoFenceId, Guid organizationId, bool isActive);

    // Shift ↔ Geo-Fence links
    Task<IReadOnlyList<ShiftGeoFenceDto>> GetGeoFencesByShiftAsync(Guid shiftId);
    Task<bool> LinkGeoFenceToShiftAsync(Guid shiftId, Guid geoFenceId);
    Task<bool> UnlinkGeoFenceFromShiftAsync(Guid shiftId, Guid geoFenceId);

    // Face descriptors
    Task<EmployeeFaceDescriptor?> GetActiveFaceDescriptorAsync(Guid employeeId);
    Task<Guid> SaveFaceDescriptorAsync(Guid employeeId, Guid generatedBy, SaveFaceDescriptorRequest dto);

    // Face recognition logs
    Task<Guid> CreateFaceLogAsync(Guid employeeId, SubmitFaceLogRequest dto);
    Task<IReadOnlyList<FaceRecognitionLogDto>> GetFaceLogsByEmployeeAsync(Guid employeeId, int limit = 10);

    // Geo-aware clock in/out
    Task<GeoClockInResponse> GeoClockInOutAsync(Guid employeeId, GeoClockInRequest request);

    // Views / reports
    Task<IReadOnlyList<TeamLiveStreamDto>> GetTeamLiveStreamAsync(Guid organizationId);
    Task<IReadOnlyList<GeoFenceViolationReportDto>> GetViolationsReportAsync(
        Guid organizationId, DateTime startDate, DateTime endDate, Guid? shiftId = null);
    Task<SystemHealthDto> GetSystemHealthAsync(Guid organizationId);

    // Helpers
    Task<decimal?> GetDistanceToFenceAsync(decimal latitude, decimal longitude, Guid geoFenceId);
    Task<bool> IsWithinGeoFenceAsync(decimal latitude, decimal longitude, Guid geoFenceId);
}