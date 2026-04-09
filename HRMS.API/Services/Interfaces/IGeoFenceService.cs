using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IGeoFenceService
{
    Task<IEnumerable<GeoFenceDto>> GetAllGeoFencesAsync(Guid organizationId);
    Task<GeoFenceDto?> GetGeoFenceByIdAsync(Guid geoFenceId);
    Task<ServiceResponse<Guid>> CreateGeoFenceAsync(Guid organizationId, Guid createdBy, CreateGeoFenceDto dto);
    Task<ServiceResponse<bool>> UpdateGeoFenceAsync(Guid geoFenceId, Guid organizationId, UpdateGeoFenceDto dto);
    Task<ServiceResponse<bool>> DeleteGeoFenceAsync(Guid geoFenceId, Guid organizationId);
    Task<ServiceResponse<bool>> ToggleActiveAsync(Guid geoFenceId, Guid organizationId, bool isActive);

    Task<IEnumerable<ShiftGeoFenceDto>> GetGeoFencesByShiftAsync(Guid shiftId);
    Task<ServiceResponse<bool>> LinkGeoFenceToShiftAsync(Guid shiftId, Guid geoFenceId);
    Task<ServiceResponse<bool>> UnlinkGeoFenceFromShiftAsync(Guid shiftId, Guid geoFenceId);

    Task<ServiceResponse<Guid>> SaveFaceDescriptorAsync(Guid employeeId, Guid generatedBy, SaveFaceDescriptorRequest dto);
    Task<ServiceResponse<Guid>> SubmitFaceLogAsync(Guid employeeId, SubmitFaceLogRequest dto);

    Task<ServiceResponse<GeoClockInResponse>> GeoClockInOutAsync(Guid employeeId, GeoClockInRequest request);

    Task<IEnumerable<TeamLiveStreamDto>> GetTeamLiveStreamAsync(Guid organizationId);
    Task<IEnumerable<GeoFenceViolationReportDto>> GetViolationsReportAsync(
        Guid organizationId, DateTime startDate, DateTime endDate, Guid? shiftId = null);
    Task<SystemHealthDto> GetSystemHealthAsync(Guid organizationId);
}