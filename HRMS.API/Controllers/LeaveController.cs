using DocumentFormat.OpenXml.Office2016.Excel;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Requests;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Security.Claims;

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

    public async Task<IActionResult> GetLeaveRequests([FromQuery] LeaveRequestSearchViewModel searchModel)
    {

        _logger.LogInformation("Getting Current Employee");
        var currentUserId = GetCurrentEmployeeId();
        _logger.LogInformation("Getting Current Organization");
        var organizationId = GetCurrentOrganizationId();
        _logger.LogInformation("Getting Leave For user :{currentUserId}", currentUserId);

        var requests = await _leaveService.GetLeaveRequestsAsync(currentUserId, searchModel, organizationId);

        _logger.LogInformation("Response: {response}", requests);
        return new ObjectResult(requests);

    }




    [HttpGet("requests/hr")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<IActionResult> GetLeaveRequestsForHR([FromQuery] LeaveRequestSearchViewModel searchModel)
    {
        _logger.LogInformation("Getting Current Organization");
        var organizationId = GetCurrentOrganizationId();

        _logger.LogInformation("Getting Leave Requests For HR. OrganizationId: {organizationId}", organizationId);

        var requests = await _leaveService.GetLeaveRequestsByOrganizationAsync(searchModel, organizationId);

        _logger.LogInformation("Response: {response}", requests);
        return new ObjectResult(requests);

    }



    [HttpGet("requests/{id}")]
    public async Task<IActionResult> GetLeaveRequest(Guid id)
    {

        _logger.LogInformation("Getting leave request for Request.Rrequestid :{requestid}", id);
        var request = await _leaveService.GetLeaveRequestByIdAsync(id);

        _logger.LogInformation("Response: {response}", request);

        //return StatusCode(request.StatusCode, request);
        return new ObjectResult(request);

    }


    [HttpGet("getleaverequestbyemployeeid")]
    public async Task<IActionResult> GetLeaveRequestByEmployeeId()
    {
        _logger.LogInformation("Getting Leave Requests for current employee");

        var userId = GetCurrentEmployeeId();

        var response = await _leaveService.GetLeaveRequestByEmployeeId(userId);

        _logger.LogInformation("Response: {response}", response);

        //return StatusCode(response.StatusCode, response);
        return new ObjectResult(response);
    }



    [HttpPost("requests")]
    public async Task<ActionResult<ServiceResponse<LeaveRequestDto>>> CreateLeaveRequest([FromBody] CreateLeaveRequest request)
    {
        try
        {
            var UserId = GetCurrentEmployeeId();

            var idd = Guid.Parse("5579f3d7-5f3c-48b6-9927-54cf9e9edf59");
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
    public async Task<IActionResult> ApproveLeaveRequest(Guid id)
    {
        _logger.LogInformation("Approving Leave Request. RequestId: {requestId}", id);

        var approvedBy = GetCurrentEmployeeId();
        var request = new UpdateLeaveRequestStatusRequest { Status = "approved" };

        var response = await _leaveService
            .UpdateLeaveRequestStatusAsync(id, request, approvedBy);

        _logger.LogInformation("Response: {response}", response);

        //return StatusCode(response.StatusCode, response);
        return new ObjectResult(response);
    }





    [HttpPut("requests/{id}/reject")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<IActionResult> RejectLeaveRequest(
    Guid id,
    [FromBody] UpdateLeaveRequestStatusRequest request)
    {
        _logger.LogInformation("Rejecting Leave Request. RequestId: {requestId}", id);

        var approvedBy = GetCurrentEmployeeId();
        request.Status = "rejected";

        var response = await _leaveService
            .UpdateLeaveRequestStatusAsync(id, request, approvedBy);

        _logger.LogInformation("Response: {response}", response);

        //return StatusCode(response.StatusCode, response);
        return new ObjectResult(response);

    }


    [HttpPut("requests/{id}")]
    public async Task<IActionResult> UpdateLeaveRequest(Guid id,[FromBody] UpdateLeaveRequest request)
    {

        _logger.LogInformation("Getting Current Employeeid");
        var employeeId = GetCurrentEmployeeId();

        _logger.LogInformation("Updating Leave Request. RequestId: {RequestId}", id);
        var response = await _leaveService.UpdateLeaveRequestAsync(id, request, employeeId);

        _logger.LogInformation("Response: {@Response}", response);

        return StatusCode(response.StatusCode, response);
    }


    [HttpDelete("requests/{id}")]
    public async Task<IActionResult> CancelLeaveRequest(Guid id)
    {

        _logger.LogInformation("getting current employee", id);

        var employeeId = GetCurrentEmployeeId();

        _logger.LogInformation("Cancelling Leave Request. RequestId: {RequestId}", id);
          var response = await _leaveService.CancelLeaveRequestAsync(id, employeeId);

        _logger.LogInformation("Response: {@Response}", response);

        //return StatusCode(response.StatusCode, response);
        return new ObjectResult(response);

    }




    [HttpGet("types")]
    public async Task<IActionResult> GetLeaveTypes()
    {
        _logger.LogInformation("Fetching Leave Types");

        var organizationId = GetCurrentOrganizationId();

        var response = await _leaveService.GetLeaveTypesAsync(organizationId);

        _logger.LogInformation("Response: {@Response}", response);

        //return StatusCode(response.StatusCode, response);
        return new ObjectResult(response);

    }



    [HttpPost("types")]
    public async Task<IActionResult> CreateLeaveType([FromBody] CreateLeaveTypeRequest request)
    {
     
        var organizationId = GetCurrentOrganizationId();
        _logger.LogInformation("Creating Leave Type");
        var response = await _leaveService.CreateLeaveTypeAsync(organizationId, request);

        _logger.LogInformation("Response: {@Response}", response);

        return StatusCode(response.StatusCode, response);
    }


    [HttpPut("types/{leaveTypeId:guid}")]
    public async Task<IActionResult> EditLeaveType(Guid leaveTypeId,[FromBody] EditLeaveTypeRequest request)
    {
        _logger.LogInformation("Editing Leave Type. LeaveTypeId: {LeaveTypeId}", leaveTypeId);

        var organizationId = GetCurrentOrganizationId();

        var response = await _leaveService.EditLeaveTypeAsync(organizationId, leaveTypeId, request);

        _logger.LogInformation("Response: {@Response}", response);

        //return StatusCode(response.StatusCode, response);
        return new ObjectResult(response);

    }


    [HttpGet("entitlements")]
    public async Task<IActionResult> GetLeaveEntitlements([FromQuery] Guid? employeeId = null, [FromQuery] int? year = null)
    {
        var empId = employeeId ?? GetCurrentEmployeeId();
        var entitlementYear = year ?? DateTime.Now.Year;

        _logger.LogInformation(
            "GetLeaveEntitlements | EmpId: {EmpId}, Year: {Year}",
            empId, entitlementYear);

        var response = await _leaveService.GetLeaveEntitlementsAsync(
            empId, entitlementYear);

        //return StatusCode(response.StatusCode, response);
        return new ObjectResult(response);

    }


    [HttpGet("balance")]
   
    public async Task<IActionResult> GetLeaveBalance([FromQuery] Guid? employeeId = null,[FromQuery] int? year = null)
    {
        _logger.LogInformation("Fetching Leave Balance");

        var empId = employeeId ?? GetCurrentEmployeeId();
        var balanceYear = year ?? DateTime.Now.Year;

        var response = await _leaveService.GetLeaveBalanceAsync(empId, balanceYear);

        _logger.LogInformation("Response: {@Response}", response);

        //return StatusCode(response.StatusCode, response);
        return new ObjectResult(response);

    }


    [HttpGet("calendar")]
 
    public async Task<IActionResult> GetLeaveCalendar([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        _logger.LogInformation("Fetching Leave Calendar. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);

        var userId = GetCurrentEmployeeId();


        var response = await _leaveService.GetLeaveRequestsForCalendarAsync(userId, startDate, endDate);

        _logger.LogInformation("Response: {@Response}", response);

        //return StatusCode(response.StatusCode, response);
        return new ObjectResult(response);

    }

    [HttpGet("team/remaining-leaves")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager")]
    public async Task<IActionResult> GetTeamRemainingLeaves([FromQuery] TeamRemainingLeavesSearchViewModel searchModel)
    {
        var managerUserId = GetCurrentEmployeeId();
        var organizationId = GetCurrentOrganizationId();

        _logger.LogInformation("Fetching Team Remaining Leaves. ManagerUserId: {ManagerUserId}, OrganizationId: {OrganizationId}",
            managerUserId, organizationId);

        var response = await _leaveService.GetTeamRemainingLeavesAsync(managerUserId, organizationId, searchModel);

        _logger.LogInformation("Response: {@Response}", response);

        //return StatusCode(response.StatusCode, response);
        return new ObjectResult(response);

    }

    [HttpGet("check-overlap")]

    public async Task<IActionResult> CheckLeaveOverlap([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] Guid? excludeRequestId = null)
    {
        var employeeId = GetCurrentEmployeeId();

        _logger.LogInformation(
            "CheckLeaveOverlap | EmployeeId: {EmployeeId}, StartDate: {StartDate}, EndDate: {EndDate}, ExcludeRequestId: {ExcludeRequestId}",
            employeeId, startDate, endDate, excludeRequestId);

        var response = await _leaveService.HasOverlappingLeaveAsync(employeeId, startDate, endDate, excludeRequestId);

        //return StatusCode(response.StatusCode, response);
        return new ObjectResult(response);

    }

    //public async Task<ActionResult<ServiceResponse<bool>>> CheckLeaveOverlap([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] Guid? excludeRequestId = null)
    //{
    //    try
    //    {
    //        var employeeId = GetCurrentEmployeeId();
    //        var hasOverlap = await _leaveService.HasOverlappingLeaveAsync(employeeId, startDate, endDate, excludeRequestId);

    //        return Ok(new ServiceResponse<bool>
    //        {
    //            Success = true,
    //            Data = hasOverlap
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error checking leave overlap");
    //        return StatusCode(500, new ServiceResponse<bool>
    //        {
    //            Success = false,
    //            Message = "An error occurred while checking leave overlap"
    //        });
    //    }
    //}



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
