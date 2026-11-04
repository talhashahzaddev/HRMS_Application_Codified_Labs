using HRMS.API.Models.DTOs;
using HRMS.API.Models.ViewModels;

namespace HRMS.API.Services.Interfaces;

public interface IAssetService
{
    Task<AssetTypeDto> CreateAssetTypeAsync(Guid organizationId, CreateAssetTypeRequest request, Guid? createdBy);
    Task<AssetTypeDto?> GetAssetTypeByIdAsync(Guid organizationId, Guid assetTypeId);
    Task<IReadOnlyList<AssetTypeDto>> GetAssetTypesAsync(Guid organizationId);
    Task<bool> UpdateAssetTypeAsync(Guid organizationId, Guid assetTypeId, UpdateAssetTypeRequest request);
    Task<bool> SoftDeleteAssetTypeAsync(Guid organizationId, Guid assetTypeId, Guid? deletedBy);

    Task<AssetDto> CreateAssetAsync(Guid organizationId, CreateAssetRequest request, Guid? createdBy);
    Task<AssetDto?> GetAssetByIdAsync(Guid organizationId, Guid assetId);
    Task<IReadOnlyList<AssetDto>> GetAssetsAsync(Guid organizationId, AssetSearchViewModel searchModel);
    Task<bool> UpdateAssetAsync(Guid organizationId, Guid assetId, UpdateAssetRequest request);
    Task<bool> SoftDeleteAssetAsync(Guid organizationId, Guid assetId, Guid? deletedBy);

    Task<AssetAssignmentDto> AssignAssetAsync(Guid organizationId, AssignAssetRequest request, Guid? createdBy);
    Task<AssetAssignmentDto> ReturnAssetAsync(Guid organizationId, ReturnAssetRequest request, Guid? updatedBy);
    Task<IReadOnlyList<AssetAssignmentDto>> GetAssetAssignmentHistoryAsync(Guid organizationId, Guid? assetId, Guid? employeeId);
}
