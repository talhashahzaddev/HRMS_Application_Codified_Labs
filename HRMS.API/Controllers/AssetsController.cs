using HRMS.API.Authorization;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.ViewModels;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(IAssetService assetService, ILogger<AssetsController> logger)
    {
        _assetService = assetService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ServiceResponse<List<AssetDto>>>> GetAssets([FromQuery] AssetSearchViewModel searchModel)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var assets = await _assetService.GetAssetsAsync(organizationId, searchModel);

            return Ok(new ServiceResponse<List<AssetDto>>
            {
                Success = true,
                Data = assets
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assets");
            return StatusCode(500, new ServiceResponse<List<AssetDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving assets"
            });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ServiceResponse<AssetDto>>> GetAssetById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var asset = await _assetService.GetAssetByIdAsync(organizationId, id);

            if (asset == null)
            {
                return NotFound(new ServiceResponse<AssetDto>
                {
                    Success = false,
                    Message = "Asset not found"
                });
            }

            return Ok(new ServiceResponse<AssetDto>
            {
                Success = true,
                Data = asset
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving asset {AssetId}", id);
            return StatusCode(500, new ServiceResponse<AssetDto>
            {
                Success = false,
                Message = "An error occurred while retrieving asset"
            });
        }
    }

    [HttpPost]
    [PermissionAuthorize("assets_create")]

    public async Task<ActionResult<ServiceResponse<AssetDto>>> CreateAsset([FromBody] CreateAssetRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<AssetDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var organizationId = GetCurrentOrganizationId();
            var createdBy = GetCurrentUserId();
            var asset = await _assetService.CreateAssetAsync(organizationId, request, createdBy);

            return Ok(new ServiceResponse<AssetDto>
            {
                Success = true,
                Message = "Asset created successfully",
                Data = asset
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating asset");
            return StatusCode(500, new ServiceResponse<AssetDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize("assets_edit")]

    public async Task<ActionResult<ServiceResponse<AssetDto>>> UpdateAsset(Guid id, [FromBody] UpdateAssetRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<AssetDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var organizationId = GetCurrentOrganizationId();
            var updated = await _assetService.UpdateAssetAsync(organizationId, id, request);

            if (!updated)
            {
                return NotFound(new ServiceResponse<AssetDto>
                {
                    Success = false,
                    Message = "Asset not found"
                });
            }

            // Fetch the updated asset so client can update UI without an extra call
            var updatedAsset = await _assetService.GetAssetByIdAsync(organizationId, id);

            return Ok(new ServiceResponse<AssetDto>
            {
                Success = true,
                Message = "Asset updated successfully",
                Data = updatedAsset
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating asset {AssetId}", id);
            return StatusCode(500, new ServiceResponse<AssetDto>
            {
                Success = false,
                Message = "An error occurred while updating asset"
            });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteAsset(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var deletedBy = GetCurrentUserId();
            var deleted = await _assetService.SoftDeleteAssetAsync(organizationId, id, deletedBy);

            if (!deleted)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Asset not found"
                });
            }

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Asset deleted successfully",
                Data = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting asset {AssetId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting asset"
            });
        }
    }

    // Asset Types endpoints (moved from AssetTypesController)
    [HttpGet("asset-types")]
    public async Task<ActionResult<ServiceResponse<List<AssetTypeDto>>>> GetAssetTypes()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var assetTypes = await _assetService.GetAssetTypesAsync(organizationId);

            return Ok(new ServiceResponse<List<AssetTypeDto>>
            {
                Success = true,
                Data = assetTypes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving asset types");
            return StatusCode(500, new ServiceResponse<List<AssetTypeDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving asset types"
            });
        }
    }

    [HttpGet("asset-types/{id:guid}")]
    public async Task<ActionResult<ServiceResponse<AssetTypeDto>>> GetAssetTypeById(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var assetType = await _assetService.GetAssetTypeByIdAsync(organizationId, id);

            if (assetType == null)
            {
                return NotFound(new ServiceResponse<AssetTypeDto>
                {
                    Success = false,
                    Message = "Asset type not found"
                });
            }

            return Ok(new ServiceResponse<AssetTypeDto>
            {
                Success = true,
                Data = assetType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving asset type {AssetTypeId}", id);
            return StatusCode(500, new ServiceResponse<AssetTypeDto>
            {
                Success = false,
                Message = "An error occurred while retrieving asset type"
            });
        }
    }

    [HttpPost("asset-types")]
    [PermissionAuthorize("type_of_asset_Craete")]

    public async Task<ActionResult<ServiceResponse<AssetTypeDto>>> CreateAssetType([FromBody] CreateAssetTypeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<AssetTypeDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var organizationId = GetCurrentOrganizationId();
            var createdBy = GetCurrentUserId();
            var assetType = await _assetService.CreateAssetTypeAsync(organizationId, request, createdBy);

            return Ok(new ServiceResponse<AssetTypeDto>
            {
                Success = true,
                Message = "Asset type created successfully",
                Data = assetType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating asset type");
            return StatusCode(500, new ServiceResponse<AssetTypeDto>
            {
                Success = false,
                Message = "An error occurred while creating asset type"
            });
        }
    }

    [HttpPut("asset-types/{id:guid}")]
    [PermissionAuthorize("type_of_asset_edit")]

    public async Task<ActionResult<ServiceResponse<AssetTypeDto>>> UpdateAssetType(Guid id, [FromBody] UpdateAssetTypeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<AssetTypeDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var organizationId = GetCurrentOrganizationId();
            var updated = await _assetService.UpdateAssetTypeAsync(organizationId, id, request);

            if (!updated)
            {
                return NotFound(new ServiceResponse<AssetTypeDto>
                {
                    Success = false,
                    Message = "Asset type not found"
                });
            }

            // Fetch the updated asset type so client can update UI without an extra call
            var updatedType = await _assetService.GetAssetTypeByIdAsync(organizationId, id);

            return Ok(new ServiceResponse<AssetTypeDto>
            {
                Success = true,
                Message = "Asset type updated successfully",
                Data = updatedType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating asset type {AssetTypeId}", id);
            return StatusCode(500, new ServiceResponse<AssetTypeDto>
            {
                Success = false,
                Message = "An error occurred while updating asset type"
            });
        }
    }

    [HttpDelete("asset-types/{id:guid}")]
    [PermissionAuthorize("type_of_asset_delete")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteAssetType(Guid id)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var deletedBy = GetCurrentUserId();
            var deleted = await _assetService.SoftDeleteAssetTypeAsync(organizationId, id, deletedBy);

            if (!deleted)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Asset type not found"
                });
            }

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Asset type deleted successfully",
                Data = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting asset type {AssetTypeId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting asset type"
            });
        }
    }

    // Asset assignments endpoints (moved from AssetAssignmentsController)
    [HttpPost("asset-assignments/assign")]
    [PermissionAuthorize("assets_assign")]

    public async Task<ActionResult<ServiceResponse<AssetAssignmentDto>>> AssignAsset([FromBody] AssignAssetRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<AssetAssignmentDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var organizationId = GetCurrentOrganizationId();
            var createdBy = GetCurrentUserId();
            var assignment = await _assetService.AssignAssetAsync(organizationId, request, createdBy);

            return Ok(new ServiceResponse<AssetAssignmentDto>
            {
                Success = true,
                Message = "Asset assigned successfully",
                Data = assignment
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning asset");
            return StatusCode(500, new ServiceResponse<AssetAssignmentDto>
            {
                Success = false,
                Message = "An error occurred while assigning asset"
            });
        }
    }

    [HttpPost("asset-assignments/return")]
    [PermissionAuthorize("assets_return")]

    public async Task<ActionResult<ServiceResponse<AssetAssignmentDto>>> ReturnAsset([FromBody] ReturnAssetRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<AssetAssignmentDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var organizationId = GetCurrentOrganizationId();
            var updatedBy = GetCurrentUserId();
            var assignment = await _assetService.ReturnAssetAsync(organizationId, request, updatedBy);

            return Ok(new ServiceResponse<AssetAssignmentDto>
            {
                Success = true,
                Message = "Asset returned successfully",
                Data = assignment
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error returning asset");
            return StatusCode(500, new ServiceResponse<AssetAssignmentDto>
            {
                Success = false,
                Message = "An error occurred while returning asset"
            });
        }
    }

    [HttpGet("asset-assignments/history")]

    public async Task<ActionResult<ServiceResponse<List<AssetAssignmentDto>>>> GetAssignmentHistory([FromQuery] Guid? assetId, [FromQuery] Guid? employeeId)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var history = await _assetService.GetAssetAssignmentHistoryAsync(organizationId, assetId, employeeId);

            return Ok(new ServiceResponse<List<AssetAssignmentDto>>
            {
                Success = true,
                Data = history
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving asset assignment history");
            return StatusCode(500, new ServiceResponse<List<AssetAssignmentDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving asset assignment history"
            });
        }
    }

    private Guid GetCurrentOrganizationId()
    {
        var orgIdClaim = User.FindFirstValue("organizationId");

        if (string.IsNullOrEmpty(orgIdClaim))
        {
            _logger.LogError("Organization ID claim not found in JWT token");
            throw new UnauthorizedAccessException("Organization ID not found in token");
        }

        if (!Guid.TryParse(orgIdClaim, out var organizationId))
        {
            _logger.LogError("Invalid organization ID format in JWT token: {OrganizationId}", orgIdClaim);
            throw new UnauthorizedAccessException("Invalid organization ID format");
        }

        return organizationId;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
