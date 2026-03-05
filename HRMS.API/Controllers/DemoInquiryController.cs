using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DemoInquiryController : ControllerBase
{
    private readonly IDemoInquiryService _inquiryService;
    private readonly ILogger<DemoInquiryController> _logger;

    public DemoInquiryController(IDemoInquiryService inquiryService, ILogger<DemoInquiryController> logger)
    {
        _inquiryService = inquiryService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a demo/inquiry request (public - no auth required)
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceResponse<DemoInquiryDto>>> SubmitInquiry([FromBody] CreateDemoInquiryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<DemoInquiryDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var response = await _inquiryService.CreateAsync(request);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return CreatedAtAction(nameof(GetInquiryById), new { inquiryId = response.Data?.InquiryId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting demo inquiry");
            return StatusCode(500, new ServiceResponse<DemoInquiryDto>
            {
                Success = false,
                Message = "An error occurred while submitting your inquiry"
            });
        }
    }

    /// <summary>
    /// Get all demo inquiries (Platform Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Platform Admin")]
    public async Task<ActionResult<ServiceResponse<PagedResult<DemoInquiryDto>>>> GetAllInquiries(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var filter = new DemoInquiryFilterRequest
            {
                Search = search,
                Status = status,
                SortBy = sortBy ?? "createdat",
                SortDirection = sortDirection ?? "desc",
                Page = page,
                PageSize = Math.Min(pageSize, 50)
            };

            var response = await _inquiryService.GetAllPagedAsync(filter);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting demo inquiries");
            return StatusCode(500, new ServiceResponse<PagedResult<DemoInquiryDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving inquiries"
            });
        }
    }

    /// <summary>
    /// Get a specific inquiry (Platform Admin only)
    /// </summary>
    [HttpGet("{inquiryId:guid}")]
    [Authorize(Roles = "Platform Admin")]
    public async Task<ActionResult<ServiceResponse<DemoInquiryDto>>> GetInquiryById(Guid inquiryId)
    {
        try
        {
            var response = await _inquiryService.GetByIdAsync(inquiryId);
            if (!response.Success)
            {
                return NotFound(response);
            }
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inquiry {InquiryId}", inquiryId);
            return StatusCode(500, new ServiceResponse<DemoInquiryDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the inquiry"
            });
        }
    }

    /// <summary>
    /// Update inquiry status (Platform Admin only)
    /// </summary>
    [HttpPut("{inquiryId:guid}/status")]
    [Authorize(Roles = "Platform Admin")]
    public async Task<ActionResult<ServiceResponse<bool>>> UpdateInquiryStatus(
        Guid inquiryId,
        [FromBody] UpdateDemoInquiryStatusRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invalid request data"
                });
            }

            var response = await _inquiryService.UpdateStatusAsync(inquiryId, request);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inquiry status {InquiryId}", inquiryId);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while updating the inquiry"
            });
        }
    }
}
