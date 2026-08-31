using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class IpAddressController : ControllerBase
{
    private readonly IIpAddressService _ipAddressService;
    private readonly ILogger<IpAddressController> _logger;

    public IpAddressController(
        IIpAddressService ipAddressService,
        ILogger<IpAddressController> logger)
    {
        _ipAddressService = ipAddressService;
        _logger = logger;
    }

    [HttpGet("office-ips")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<IpAddress>>>> GetAll()
    {
        try
        {
            var ips = await _ipAddressService.GetAllIpAddressesAsync();

            return Ok(new ServiceResponse<IReadOnlyList<IpAddress>>
            {
                Success = true,
                Message = "IP addresses retrieved successfully.",
                Data = ips
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving IP addresses");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<IpAddress>>
            {
                Success = false,
                Message = "An error occurred while retrieving IP addresses."
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResponse<IpAddress>>> GetById(Guid id)
    {
        try
        {
            var ip = await _ipAddressService.GetIpByIdAsync(id);

            return Ok(new ServiceResponse<IpAddress>
            {
                Success = true,
                Message = "IP address retrieved successfully.",
                Data = ip
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid ID for IP retrieval");
            return BadRequest(new ServiceResponse<IpAddress>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving IP address");
            return StatusCode(500, new ServiceResponse<IpAddress>
            {
                Success = false,
                Message = "An error occurred while retrieving IP address."
            });
        }
    }

    [HttpPost("create-ip")]
    public async Task<ActionResult<ServiceResponse<bool>>> Create([FromBody] IpAddress dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var success = await _ipAddressService.CreateIpAddressAsync(dto);

            return Ok(new ServiceResponse<bool>
            {
                Success = success,
                Message = success ? "IP address created successfully." : "Failed to create IP address.",
                Data = success
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating IP");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message,
                Data = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating IP address");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while creating IP address.",
                Data = false
            });
        }
    }

    [HttpPut("update/{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> Update(Guid id, [FromBody] IpAddress dto)
    {
        try
        {
            var success = await _ipAddressService.UpdateIpAddressAsync(id, dto);

            return Ok(new ServiceResponse<bool>
            {
                Success = success,
                Message = success ? "IP address updated successfully." : "Failed to update IP address.",
                Data = success
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while updating IP");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message,
                Data = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating IP address");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while updating IP address.",
                Data = false
            });
        }
    }

    [HttpDelete("delete/{id}")]
    public async Task<ActionResult<ServiceResponse<bool>>> Delete(Guid id)
    {
        try
        {
            var success = await _ipAddressService.DeleteIpAddressAsync(id);

            return Ok(new ServiceResponse<bool>
            {
                Success = success,
                Message = success ? "IP address deleted successfully." : "Failed to delete IP address.",
                Data = success
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while deleting IP");
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message,
                Data = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting IP address");
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting IP address.",
                Data = false
            });
        }
    }
}
