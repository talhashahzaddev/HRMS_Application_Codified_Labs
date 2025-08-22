using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.ViewModels;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveController : ControllerBase
{
    private readonly ILeaveService _leaveService;
    private readonly ILogger<LeaveController> _logger;

    public LeaveController(ILeaveService leaveService, ILogger<LeaveController> logger)
    {
        _leaveService = leaveService;
        _logger = logger;
    }

    [HttpGet("requests")]
    public async Task<ActionResult<ApiResponse<PagedResult<LeaveRequestDto>>>> GetLeaveRequests([FromQuery] LeaveRequestSearchViewModel searchModel)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var requests = await _leaveService.GetLeaveRequestsAsync(searchModel, organizationId);
            
            return Ok(new ApiResponse<PagedResult<LeaveRequestDto>>
            {
                Success = true,
                Data = requests
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave requests");
            return StatusCode(500, new ApiResponse<PagedResult<LeaveRequestDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving leave requests"
            });
        }
    }

    [HttpGet("requests/{id}")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> GetLeaveRequest(Guid id)
    {
        try
        {
            var request = await _leaveService.GetLeaveRequestByIdAsync(id);
            if (request == null)
            {
                return NotFound(new ApiResponse<LeaveRequestDto>
                {
                    Success = false,
                    Message = "Leave request not found"
                });
            }

            return Ok(new ApiResponse<LeaveRequestDto>
            {
                Success = true,
                Data = request
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave request {RequestId}", id);
            return StatusCode(500, new ApiResponse<LeaveRequestDto>
            {
                Success = false,
                Message = "An error occurred while retrieving leave request"
            });
        }
    }

    [HttpPost("requests")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> CreateLeaveRequest([FromBody] CreateLeaveRequest request)
    {
        try
        {
            var employeeId = GetCurrentEmployeeId();
            var leaveRequest = await _leaveService.CreateLeaveRequestAsync(request, employeeId);
            
            return CreatedAtAction(nameof(GetLeaveRequest), new { id = leaveRequest.RequestId }, new ApiResponse<LeaveRequestDto>
            {
                Success = true,
                Message = "Leave request submitted successfully",
                Data = leaveRequest
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<LeaveRequestDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leave request");
            return StatusCode(500, new ApiResponse<LeaveRequestDto>
            {
                Success = false,
                Message = "An error occurred while creating leave request"
            });
        }
    }

    [HttpPut("requests/{id}/approve")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ApiResponse<bool>>> ApproveLeaveRequest(Guid id)
    {
        try
        {
            var approvedBy = GetCurrentEmployeeId();
            var request = new UpdateLeaveRequestStatusRequest { Status = "approved" };
            var result = await _leaveService.UpdateLeaveRequestStatusAsync(id, request, approvedBy);
            
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "Leave request approved successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving leave request {RequestId}", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while approving leave request"
            });
        }
    }

    [HttpPut("requests/{id}/reject")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ApiResponse<bool>>> RejectLeaveRequest(Guid id, [FromBody] UpdateLeaveRequestStatusRequest request)
    {
        try
        {
            var approvedBy = GetCurrentEmployeeId();
            request.Status = "rejected";
            var result = await _leaveService.UpdateLeaveRequestStatusAsync(id, request, approvedBy);
            
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "Leave request rejected successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting leave request {RequestId}", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while rejecting leave request"
            });
        }
    }

    [HttpGet("types")]
    public async Task<ActionResult<ApiResponse<List<LeaveTypeDto>>>> GetLeaveTypes()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var leaveTypes = await _leaveService.GetLeaveTypesAsync(organizationId);
            
            return Ok(new ApiResponse<List<LeaveTypeDto>>
            {
                Success = true,
                Data = leaveTypes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave types");
            return StatusCode(500, new ApiResponse<List<LeaveTypeDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving leave types"
            });
        }
    }

    [HttpGet("entitlements")]
    public async Task<ActionResult<ApiResponse<List<LeaveEntitlementDto>>>> GetLeaveEntitlements([FromQuery] Guid? employeeId = null, [FromQuery] int? year = null)
    {
        try
        {
            var empId = employeeId ?? GetCurrentEmployeeId();
            var entitlementYear = year ?? DateTime.Now.Year;
            var entitlements = await _leaveService.GetLeaveEntitlementsAsync(empId, entitlementYear);
            
            return Ok(new ApiResponse<List<LeaveEntitlementDto>>
            {
                Success = true,
                Data = entitlements
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave entitlements");
            return StatusCode(500, new ApiResponse<List<LeaveEntitlementDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving leave entitlements"
            });
        }
    }

    [HttpGet("balance")]
    public async Task<ActionResult<ApiResponse<List<LeaveBalanceDto>>>> GetLeaveBalance([FromQuery] Guid? employeeId = null, [FromQuery] int? year = null)
    {
        try
        {
            var empId = employeeId ?? GetCurrentEmployeeId();
            var balanceYear = year ?? DateTime.Now.Year;
            var balance = await _leaveService.GetLeaveBalanceAsync(empId, balanceYear);
            
            return Ok(new ApiResponse<List<LeaveBalanceDto>>
            {
                Success = true,
                Data = balance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave balance");
            return StatusCode(500, new ApiResponse<List<LeaveBalanceDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving leave balance"
            });
        }
    }

    private Guid GetCurrentEmployeeId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            _logger.LogError("User ID claim not found in JWT token");
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogError("Invalid user ID format in JWT token: {UserId}", userIdClaim);
            throw new UnauthorizedAccessException("Invalid user ID format");
        }
        
        return userId;
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
}
