using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class GeoFenceService : IGeoFenceService
{
    private readonly IGeoFenceRepository _repo;
    private readonly ILogger<GeoFenceService> _logger;

    public GeoFenceService(IGeoFenceRepository repo, ILogger<GeoFenceService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<IEnumerable<GeoFenceDto>> GetAllGeoFencesAsync(Guid organizationId)
        => await _repo.GetAllGeoFencesAsync(organizationId);

    public async Task<GeoFenceDto?> GetGeoFenceByIdAsync(Guid geoFenceId)
        => await _repo.GetGeoFenceByIdAsync(geoFenceId);

    public async Task<ServiceResponse<Guid>> CreateGeoFenceAsync(
        Guid organizationId, Guid createdBy, CreateGeoFenceDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return new ServiceResponse<Guid> { Success = false, Message = "Geo-fence name is required." };

            if (dto.Shape == "circle" &&
                (dto.CenterLatitude == null || dto.CenterLongitude == null || dto.RadiusMeters == null))
                return new ServiceResponse<Guid> { Success = false, Message = "Circle fences require center coordinates and radius." };

            var id = await _repo.CreateGeoFenceAsync(organizationId, createdBy, dto);
            return new ServiceResponse<Guid> { Success = true, Data = id, Message = "Geo-fence created successfully." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating geo-fence for org {OrgId}", organizationId);
            return new ServiceResponse<Guid> { Success = false, Message = "Failed to create geo-fence." };
        }
    }

    public async Task<ServiceResponse<bool>> UpdateGeoFenceAsync(
        Guid geoFenceId, Guid organizationId, UpdateGeoFenceDto dto)
    {
        try
        {
            var result = await _repo.UpdateGeoFenceAsync(geoFenceId, organizationId, dto);
            return new ServiceResponse<bool>
            {
                Success = result,
                Data = result,
                Message = result ? "Geo-fence updated." : "Geo-fence not found."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating geo-fence {Id}", geoFenceId);
            return new ServiceResponse<bool> { Success = false, Message = "Failed to update geo-fence." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteGeoFenceAsync(Guid geoFenceId, Guid organizationId)
    {
        var result = await _repo.SoftDeleteGeoFenceAsync(geoFenceId, organizationId);
        return new ServiceResponse<bool>
        {
            Success = result,
            Data = result,
            Message = result ? "Geo-fence deleted." : "Not found."
        };
    }

    public async Task<ServiceResponse<bool>> ToggleActiveAsync(Guid geoFenceId, Guid organizationId, bool isActive)
    {
        var result = await _repo.ToggleGeoFenceActiveAsync(geoFenceId, organizationId, isActive);
        return new ServiceResponse<bool> { Success = result, Data = result };
    }

    public async Task<IEnumerable<ShiftGeoFenceDto>> GetGeoFencesByShiftAsync(Guid shiftId)
        => await _repo.GetGeoFencesByShiftAsync(shiftId);

    public async Task<ServiceResponse<bool>> LinkGeoFenceToShiftAsync(Guid shiftId, Guid geoFenceId)
    {
        var result = await _repo.LinkGeoFenceToShiftAsync(shiftId, geoFenceId);
        return new ServiceResponse<bool> { Success = result, Data = result };
    }

    public async Task<ServiceResponse<bool>> UnlinkGeoFenceFromShiftAsync(Guid shiftId, Guid geoFenceId)
    {
        var result = await _repo.UnlinkGeoFenceFromShiftAsync(shiftId, geoFenceId);
        return new ServiceResponse<bool> { Success = result, Data = result };
    }

    public async Task<ServiceResponse<Guid>> SaveFaceDescriptorAsync(
        Guid employeeId, Guid generatedBy, SaveFaceDescriptorRequest dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.DescriptorVector))
                return new ServiceResponse<Guid> { Success = false, Message = "Descriptor vector is required." };

            var id = await _repo.SaveFaceDescriptorAsync(employeeId, generatedBy, dto);
            return new ServiceResponse<Guid> { Success = true, Data = id, Message = "Face descriptor saved." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving face descriptor for employee {Id}", employeeId);
            return new ServiceResponse<Guid> { Success = false, Message = "Failed to save descriptor." };
        }
    }

    public async Task<ServiceResponse<Guid>> SubmitFaceLogAsync(Guid employeeId, SubmitFaceLogRequest dto)
    {
        try
        {
            var logId = await _repo.CreateFaceLogAsync(employeeId, dto);
            return new ServiceResponse<Guid> { Success = true, Data = logId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating face log for employee {Id}", employeeId);
            return new ServiceResponse<Guid> { Success = false, Message = "Failed to save face log." };
        }
    }

    public async Task<ServiceResponse<GeoClockInResponse>> GeoClockInOutAsync(
        Guid employeeId, GeoClockInRequest request)
    {
        try
        {
            // 1. Save the face recognition log first
            Guid? faceLogId = null;
            if (!string.IsNullOrWhiteSpace(request.FaceDescriptor))
            {
                faceLogId = await _repo.CreateFaceLogAsync(employeeId, new SubmitFaceLogRequest
                {
                    FaceDescriptor = request.FaceDescriptor,
                    MatchResult = request.MatchResult,
                    ConfidenceScore = request.ConfidenceScore,
                    DistanceScore = request.DistanceScore,
                    GeoFenceId = request.GeoFenceId,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    DeviceInfo = request.DeviceInfo
                });
            }

            // 2. Perform geo-aware clock in/out
            var response = await _repo.GeoClockInOutAsync(employeeId, request);

            // 3. If face match failed and location is out — it's already blocked by the DB function
            return new ServiceResponse<GeoClockInResponse>
            {
                Success = response.Success,
                Data = response,
                Message = response.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in geo clock in/out for employee {Id}", employeeId);
            return new ServiceResponse<GeoClockInResponse>
            {
                Success = false,
                Message = "Clock in/out failed. Please try again."
            };
        }
    }

    public async Task<IEnumerable<TeamLiveStreamDto>> GetTeamLiveStreamAsync(Guid organizationId)
        => await _repo.GetTeamLiveStreamAsync(organizationId);

    public async Task<IEnumerable<GeoFenceViolationReportDto>> GetViolationsReportAsync(
        Guid organizationId, DateTime startDate, DateTime endDate, Guid? shiftId = null)
        => await _repo.GetViolationsReportAsync(organizationId, startDate, endDate, shiftId);

    public async Task<SystemHealthDto> GetSystemHealthAsync(Guid organizationId)
        => await _repo.GetSystemHealthAsync(organizationId);
}