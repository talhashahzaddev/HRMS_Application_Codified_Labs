using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizationController : ControllerBase
    {
        private readonly IOrganizationService _organizationService;

        public OrganizationController(IOrganizationService organizationService)
        {
            _organizationService = organizationService;
        }

        [HttpPost]
        public async Task<ActionResult<ServiceResponse<OrganizationDTO>>> CreateOrganization([FromBody] CreateOrganizationDTO createOrganizationDTO)
        {
            var response = await _organizationService.CreateOrganizationAsync(createOrganizationDTO);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return CreatedAtAction(nameof(GetOrganizationById), new { organizationId = response.Data?.OrganizationId }, response);
        }

        [HttpGet("{organizationId:guid}")]
        public async Task<ActionResult<ServiceResponse<OrganizationDTO>>> GetOrganizationById(Guid organizationId)
        {
            var response = await _organizationService.GetOrganizationByIdAsync(organizationId);
            if (!response.Success)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpGet("name/{name}")]
        public async Task<ActionResult<ServiceResponse<OrganizationDTO>>> GetOrganizationByName(string name)
        {
            var response = await _organizationService.GetOrganizationByNameAsync(name);
            if (!response.Success)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpPut("{organizationId:guid}")]
        public async Task<ActionResult<ServiceResponse<OrganizationDTO>>> UpdateOrganization(Guid organizationId, [FromBody] UpdateOrganizationDTO updateOrganizationDTO)
        {
            var response = await _organizationService.UpdateOrganizationAsync(organizationId, updateOrganizationDTO);
            if (!response.Success)
            {
                if (response.Message == "Organization not found.")
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpDelete("{organizationId:guid}")]
        public async Task<ActionResult<ServiceResponse<bool>>> DeleteOrganization(Guid organizationId)
        {
            var response = await _organizationService.DeleteOrganizationAsync(organizationId);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return NoContent();
        }

        [HttpGet]
        public async Task<ActionResult<ServiceResponse<IReadOnlyList<OrganizationDTO>>>> GetAllOrganizations()
        {
            var response = await _organizationService.GetAllOrganizationsAsync();
            if (!response.Success)
            {
                return StatusCode(500, response); // Or appropriate error
            }
            return Ok(response);
        }
    }
}