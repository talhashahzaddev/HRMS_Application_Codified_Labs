using HRMS.API.Models.DTOs;
using HRMS.API.Models.ViewModels;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class AssetService : IAssetService
{
    private readonly IAssetRepository _assetRepository;
    private readonly ILogger<AssetService> _logger;

    public AssetService(

        IAssetRepository assetRepository,
        ILogger<AssetService> logger)
    {
        _assetRepository = assetRepository;
        _logger = logger;
    }

    public async Task<AssetTypeDto> CreateAssetTypeAsync(Guid organizationId, CreateAssetTypeRequest request, Guid? createdBy)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Asset type name is required", nameof(request.Name));

        return await _assetRepository.CreateAssetTypeAsync(organizationId, request, createdBy);
    }

    public async Task<AssetTypeDto?> GetAssetTypeByIdAsync(Guid organizationId, Guid assetTypeId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (assetTypeId == Guid.Empty)
            throw new ArgumentException("Asset type ID cannot be empty", nameof(assetTypeId));

        return await _assetRepository.GetAssetTypeByIdAsync(organizationId, assetTypeId);
    }

    public async Task<IReadOnlyList<AssetTypeDto>> GetAssetTypesAsync(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));

        return await _assetRepository.GetAssetTypesAsync(organizationId);
    }

    public async Task<bool> UpdateAssetTypeAsync(Guid organizationId, Guid assetTypeId, UpdateAssetTypeRequest request)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (assetTypeId == Guid.Empty)
            throw new ArgumentException("Asset type ID cannot be empty", nameof(assetTypeId));
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return await _assetRepository.UpdateAssetTypeAsync(organizationId, assetTypeId, request);
    }

    public async Task<bool> SoftDeleteAssetTypeAsync(Guid organizationId, Guid assetTypeId, Guid? deletedBy)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (assetTypeId == Guid.Empty)
            throw new ArgumentException("Asset type ID cannot be empty", nameof(assetTypeId));

        return await _assetRepository.SoftDeleteAssetTypeAsync(organizationId, assetTypeId, deletedBy);
    }

    public async Task<AssetDto> CreateAssetAsync(Guid organizationId, CreateAssetRequest request, Guid? createdBy)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.AssetTag))
            throw new ArgumentException("Asset tag is required", nameof(request.AssetTag));
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Asset name is required", nameof(request.Name));

       //_= request.AssetTypeId ?? throw new ArgumentException("Asset Type Id Required");
        var result = _assetRepository.GetAssetTypeByIdAsync(organizationId ,request.AssetTypeId);
        _ = result.Result ?? throw new ArgumentException("Asset Type not found"); 
        return await _assetRepository.CreateAssetAsync(organizationId, request, createdBy);
    }

    public async Task<AssetDto?> GetAssetByIdAsync(Guid organizationId, Guid assetId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (assetId == Guid.Empty)
            throw new ArgumentException("Asset ID cannot be empty", nameof(assetId));

        return await _assetRepository.GetAssetByIdAsync(organizationId, assetId);
    }

    public async Task<IReadOnlyList<AssetDto>> GetAssetsAsync(Guid organizationId, AssetSearchViewModel searchModel)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (searchModel == null)
            throw new ArgumentNullException(nameof(searchModel));

        return await _assetRepository.GetAssetsAsync(organizationId, searchModel);
    }

    public async Task<bool> UpdateAssetAsync(Guid organizationId, Guid assetId, UpdateAssetRequest request)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (assetId == Guid.Empty)
            throw new ArgumentException("Asset ID cannot be empty", nameof(assetId));
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return await _assetRepository.UpdateAssetAsync(organizationId, assetId, request);
    }

    public async Task<bool> SoftDeleteAssetAsync(Guid organizationId, Guid assetId, Guid? deletedBy)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (assetId == Guid.Empty)
            throw new ArgumentException("Asset ID cannot be empty", nameof(assetId));

        return await _assetRepository.SoftDeleteAssetAsync(organizationId, assetId, deletedBy);
    }

    public async Task<AssetAssignmentDto> AssignAssetAsync(Guid organizationId, AssignAssetRequest request, Guid? createdBy)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (request.AssetId == Guid.Empty)
            throw new ArgumentException("Asset ID cannot be empty", nameof(request.AssetId));
        if (request.EmployeeId == Guid.Empty)
            throw new ArgumentException("Employee ID cannot be empty", nameof(request.EmployeeId));

        _logger.LogInformation("Assigning asset {AssetId} to employee {EmployeeId} for organization {OrganizationId}",
            request.AssetId, request.EmployeeId, organizationId);

        return await _assetRepository.AssignAssetAsync(organizationId, request, createdBy);
    }

    public async Task<AssetAssignmentDto> ReturnAssetAsync(Guid organizationId, ReturnAssetRequest request, Guid? updatedBy)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (request.AssignmentId == Guid.Empty)
            throw new ArgumentException("Assignment ID cannot be empty", nameof(request.AssignmentId));

        _logger.LogInformation("Returning asset assignment {AssignmentId} for organization {OrganizationId}",
            request.AssignmentId, organizationId);

        return await _assetRepository.ReturnAssetAsync(organizationId, request, updatedBy);
    }

    public async Task<IReadOnlyList<AssetAssignmentDto>> GetAssetAssignmentHistoryAsync(Guid organizationId, Guid? assetId, Guid? employeeId)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));

        return await _assetRepository.GetAssetAssignmentHistoryAsync(organizationId, assetId, employeeId);
    }
}
