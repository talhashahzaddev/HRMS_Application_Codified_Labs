using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.ViewModels;
using HRMS.API.Services.Interfaces;
using HRMS.API.Models.Entities;
using HRMS.API.Requests;

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
    public async Task<ActionResult<ServiceResponse<PagedResult<LeaveRequestDto>>>> GetLeaveRequests([FromQuery] LeaveRequestSearchViewModel searchModel)
    {
        try
        {
            var currentUserId = GetCurrentEmployeeId();
            var organizationId = GetCurrentOrganizationId();
            var requests = await _leaveService.GetLeaveRequestsAsync(currentUserId,searchModel, organizationId);
            
            return Ok(new ServiceResponse<PagedResult<LeaveRequestDto>>
            {
                Success = true,
                Data = requests
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave requests");
            return StatusCode(500, new ServiceResponse<PagedResult<LeaveRequestDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving leave requests"
            });
        }
    }

    [HttpGet("requests/hr")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<LeaveRequestDto>>>> GetLeaveRequestsForHR([FromQuery] LeaveRequestSearchViewModel searchModel)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var requests = await _leaveService.GetLeaveRequestsByOrganizationAsync(searchModel, organizationId);
            
            return Ok(new ServiceResponse<PagedResult<LeaveRequestDto>>
            {
                Success = true,
                Data = requests
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave requests for HR");
            return StatusCode(500, new ServiceResponse<PagedResult<LeaveRequestDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving leave requests"
            });
        }
    }

    [HttpGet("requests/{id}")]
    public async Task<ActionResult<ServiceResponse<LeaveRequestDto>>> GetLeaveRequest(Guid id)
    {
        try
        {
            var request = await _leaveService.GetLeaveRequestByIdAsync(id);
            if (request == null)
            {
                return NotFound(new ServiceResponse<LeaveRequestDto>
                {
                    Success = false,
                    Message = "Leave request not found"
                });
            }

            return Ok(new ServiceResponse<LeaveRequestDto>
            {
                Success = true,
                Data = request
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave request {RequestId}", id);
            return StatusCode(500, new ServiceResponse<LeaveRequestDto>
            {
                Success = false,
                Message = "An error occurred while retrieving leave request"
            });
        }
    }

    [HttpGet("getleaverequestbyemployeeid")]
    public async Task<ActionResult<ServiceResponse<List<LeaveRequest>>>> GetLeaveRequestByEmployeeId()
    {
        var userId = GetCurrentEmployeeId();

        try
        {
            var requests = await _leaveService.GetLeaveRequestByEmployeeId(userId);

            if (requests == null || !requests.Any())
            {
                return NotFound(new ServiceResponse<List<LeaveRequest>>
                {
                    Success = false,
                    Message = "No leave requests found"
                });
            }

            return Ok(new ServiceResponse<List<LeaveRequest>>
            {
                Success = true,
                Data = requests
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave requests for employee {UserId}", userId);
            return StatusCode(500, new ServiceResponse<List<LeaveRequest>>
            {
                Success = false,
                Message = "An error occurred while retrieving leave requests"
            });
        }
    }


    [HttpPost("requests")]
    public async Task<ActionResult<ServiceResponse<LeaveRequestDto>>> CreateLeaveRequest([FromBody] CreateLeaveRequest request)
    {
        try
        {
            var UserId = GetCurrentEmployeeId();
            var leaveRequest = await _leaveService.CreateLeaveRequestAsync(request, UserId);
            
            return CreatedAtAction(nameof(GetLeaveRequest), new { id = leaveRequest.RequestId }, new ServiceResponse<LeaveRequestDto>
            {
                Success = true,
                Message = "Leave request submitted successfully",
                Data = leaveRequest
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ServiceResponse<LeaveRequestDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leave request");
            return StatusCode(500, new ServiceResponse<LeaveRequestDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpPut("requests/{id}/approve")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> ApproveLeaveRequest(Guid id)
    {
        try
        {
            var approvedBy = GetCurrentEmployeeId();
            var request = new UpdateLeaveRequestStatusRequest { Status = "approved" };
            var result = await _leaveService.UpdateLeaveRequestStatusAsync(id, request, approvedBy);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Leave request approved successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving leave request {RequestId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while approving leave request"
            });
        }
    }

    [HttpPut("requests/{id}/reject")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> RejectLeaveRequest(Guid id, [FromBody] UpdateLeaveRequestStatusRequest request)
    {
        try
        {
            var approvedBy = GetCurrentEmployeeId();
            request.Status = "rejected";
            var result = await _leaveService.UpdateLeaveRequestStatusAsync(id, request, approvedBy);
            
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Leave request rejected successfully",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting leave request {RequestId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while rejecting leave request"
            });
        }
    }

    [HttpPut("requests/{id}")]
    public async Task<ActionResult<ServiceResponse<LeaveRequestDto>>> UpdateLeaveRequest(Guid id, [FromBody] UpdateLeaveRequest request)
    {
        try
        {
            var employeeId = GetCurrentEmployeeId();
            var updatedRequest = await _leaveService.UpdateLeaveRequestAsync(id, request, employeeId);
            
            if (updatedRequest == null)
            {
                return NotFound(new ServiceResponse<LeaveRequestDto>
                {
                    Success = false,
                    Message = "Leave request not found or cannot be updated"
                });
            }

            return Ok(new ServiceResponse<LeaveRequestDto>
            {
                Success = true,
                Message = "Leave request updated successfully",
                Data = updatedRequest
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ServiceResponse<LeaveRequestDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating leave request {RequestId}", id);
            return StatusCode(500, new ServiceResponse<LeaveRequestDto>
            {
                Success = false,
                Message = "An error occurred while updating leave request"
            });
        }
    }

    [HttpDelete("requests/{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> CancelLeaveRequest(Guid id)
    {
        try
        {
            var employeeId = GetCurrentEmployeeId();
            var result = await _leaveService.CancelLeaveRequestAsync(id, employeeId);
            
            if (!result)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Leave request not found or cannot be cancelled"
                });
            }

            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Leave request cancelled successfully",
                Data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling leave request {RequestId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while cancelling leave request"
            });
        }
    }

    [HttpGet("types")]
    public async Task<ActionResult<ServiceResponse<List<LeaveTypeDto>>>> GetLeaveTypes()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var leaveTypes = await _leaveService.GetLeaveTypesAsync(organizationId);
            
            return Ok(new ServiceResponse<List<LeaveTypeDto>>
            {
                Success = true,
                Data = leaveTypes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave types");
            return StatusCode(500, new ServiceResponse<List<LeaveTypeDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving leave types"
            });
        }
    }
    [HttpPost("types")]
    public async Task<ActionResult<ServiceResponse<Guid>>> CreateLeaveType([FromBody] CreateLeaveTypeRequest request)
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var leaveTypeId = await _leaveService.CreateLeaveTypeAsync(organizationId, request);
            if(leaveTypeId != Guid.Empty)
            {
                return Ok(new ServiceResponse<Guid>
                {
                    Success = true,
                    Data = leaveTypeId,
                    Message = "Leave type created successfully"
                });
            }
            else
            {
                return StatusCode(500, new ServiceResponse<Guid>
                {
                    Success = false,
                    Message = "An error occurred while creating leave type"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leave type");
            return StatusCode(500, new ServiceResponse<Guid>
            {
                Success = false,
                Message = "An error occurred while creating leave type"
            });
        }
    }



    [HttpGet("entitlements")]
    public async Task<ActionResult<ServiceResponse<List<LeaveEntitlementDto>>>> GetLeaveEntitlements([FromQuery] Guid? employeeId = null, [FromQuery] int? year = null)
    {
        try
        {
            var empId = employeeId ?? GetCurrentEmployeeId();
            var entitlementYear = year ?? DateTime.Now.Year;
            var entitlements = await _leaveService.GetLeaveEntitlementsAsync(empId, entitlementYear);
            
            return Ok(new ServiceResponse<List<LeaveEntitlementDto>>
            {
                Success = true,
                Data = entitlements
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave entitlements");
            return StatusCode(500, new ServiceResponse<List<LeaveEntitlementDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving leave entitlements"
            });
        }
    }

    [HttpGet("balance")]
    public async Task<ActionResult<ServiceResponse<List<LeaveBalanceDto>>>> GetLeaveBalance([FromQuery] Guid? employeeId = null, [FromQuery] int? year = null)
    {
        try
        {
            var empId = employeeId ?? GetCurrentEmployeeId();
            var balanceYear = year ?? DateTime.Now.Year;
            var balance = await _leaveService.GetLeaveBalanceAsync(empId, balanceYear);
            
            return Ok(new ServiceResponse<List<LeaveBalanceDto>>
            {
                Success = true,
                Data = balance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave balance");
            return StatusCode(500, new ServiceResponse<List<LeaveBalanceDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving leave balance"
            });
        }
    }

    [HttpGet("calendar")]
    public async Task<ActionResult<ServiceResponse<List<LeaveCalendarDto>>>> GetLeaveCalendar([FromQuery] DateTime startDate,DateTime endDate)
    {
        try
        {
            var userId = GetCurrentEmployeeId();
            var data = await _leaveService.GetLeaveRequestsForCalendarAsync(userId, startDate, endDate);

            return Ok(new ServiceResponse<List<LeaveCalendarDto>>
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave calendar");
            return StatusCode(500, new ServiceResponse<List<LeaveCalendarDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving leave calendar"
            });
        }
    }

    [HttpGet("team/remaining-leaves")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<ActionResult<ServiceResponse<List<TeamRemainingLeavesDto>>>> GetTeamRemainingLeaves([FromQuery] int? year = null)
    {
        try
        {
            var managerUserId = GetCurrentEmployeeId();
            var organizationId = GetCurrentOrganizationId();
            var balanceYear = year ?? DateTime.Now.Year;
            
            var data = await _leaveService.GetTeamRemainingLeavesAsync(managerUserId, organizationId, balanceYear);

            return Ok(new ServiceResponse<List<TeamRemainingLeavesDto>>
            {
                Success = true,
                Data = data
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving team remaining leaves");
            return StatusCode(500, new ServiceResponse<List<TeamRemainingLeavesDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving team remaining leaves"
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
