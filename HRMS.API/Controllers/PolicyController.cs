using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers
{
    /// <summary>
    /// Company Policies API
    ///
    /// GET    /api/policies/get/all/policies  → Super Admin: all | Employee: published only
    /// GET    /api/policies/{id}              → Super Admin: any | Employee: published only
    /// POST   /api/policies/Create/Policy     → Super Admin only
    /// PUT    /api/policies/{id}              → Super Admin only
    /// DELETE /api/policies/{id}              → Super Admin only
    ///
    /// File upload flow:
    ///   1. Frontend calls POST /api/uploads/files  → receives public URL
    ///   2. Frontend passes that URL as [FromForm] string? fileUrl in create/update
    ///   3. This controller stores the URL string directly — no file processing here
    /// </summary>
    [ApiController]
    [Route("api/policies")]
    [Authorize]
    public class PoliciesController : ControllerBase
    {
        private readonly IPolicyService _service;
        private readonly ILogger<PoliciesController> _logger;

        public PoliciesController(IPolicyService service, ILogger<PoliciesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ── GET /api/policies/get/all/policies ───────────────────────────────

        [HttpGet("get/all/policies")]
        public async Task<IActionResult> GetPolicies()
        {
            try
            {
                var orgId = GetCurrentOrganizationId();
                var isSuperAdmin = IsSuperAdmin();
                var policies = await _service.GetPoliciesAsync(orgId, isSuperAdmin);
                return Ok(new { success = true, data = policies });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching policies");
                return StatusCode(500, new { success = false, message = "Failed to load policies." });
            }
        }

        // ── GET /api/policies/{id} ───────────────────────────────────────────

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPolicyById(Guid id)
        {
            try
            {
                var orgId = GetCurrentOrganizationId();
                var policy = await _service.GetByIdAsync(id, orgId);

                if (policy is null)
                    return NotFound(new { success = false, message = "Policy not found." });

                if (!IsSuperAdmin() && !policy.IsPublished)
                    return Forbid();

                return Ok(new { success = true, data = policy });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching policy {Id}", id);
                return StatusCode(500, new { success = false, message = "Failed to load policy." });
            }
        }

        // ── POST /api/policies/Create/Policy  (Super Admin only) ─────────────
        //
        //  Accepts multipart/form-data.
        //
        //  Form fields:
        //    policyName    (string)
        //    category      (string)
        //    policyContent (string)
        //    isPublished   (bool)
        //    fieldToggles  (JSON string, optional)
        //    fileUrl       (string, optional) ← public URL from /api/uploads/files
        //    fileName      (string, optional) ← original filename for display

        [HttpPost("Create/Policy")]
        [Authorize(Roles = "Super Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreatePolicy([FromForm] CreatePolicyRequest request)
        {
            try
            {
                var orgId = GetCurrentOrganizationId();
                var policy = await _service.CreateAsync(orgId, request);

                return CreatedAtAction(
                    nameof(GetPolicyById),
                    new { id = policy.PolicyId },
                    new
                    {
                        success = true,
                        message = request.IsPublished
                            ? "Policy created and published successfully."
                            : "Policy saved as draft.",
                        data = policy
                    });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        // ── PUT /api/policies/{id}  (Super Admin only) ───────────────────────

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Super Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePolicy(
            Guid id,
            [FromForm] string policyName,
            [FromForm] string category,
            [FromForm] string policyContent,
            [FromForm] bool isPublished,
            [FromForm] string? fieldToggles,
            [FromForm] string? fileUrl,
            [FromForm] string? fileName)
        {
            try
            {
                var request = new UpdatePolicyRequest
                {
                    PolicyName = policyName,
                    Category = category,
                    PolicyContent = policyContent,
                    IsPublished = isPublished,
                    FieldToggles = fieldToggles,
                    FileUrl = fileUrl,
                    FileName = fileName
                };

                var orgId = GetCurrentOrganizationId();
                var (found, updated) = await _service.UpdateAsync(id, orgId, request);

                if (!found)
                    return NotFound(new { success = false, message = "Policy not found." });

                return Ok(new
                {
                    success = updated,
                    message = isPublished ? "Policy updated and published." : "Policy saved as draft."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating policy {Id}", id);
                return StatusCode(500, new { success = false, message = "Failed to update policy." });
            }
        }

        // ── DELETE /api/policies/{id}  (Super Admin only) ────────────────────

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Super Admin")]
        public async Task<IActionResult> DeletePolicy(Guid id)
        {
            try
            {
                var orgId = GetCurrentOrganizationId();
                var deleted = await _service.DeleteAsync(id, orgId);

                if (!deleted)
                    return NotFound(new { success = false, message = "Policy not found." });

                return Ok(new { success = true, message = "Policy deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting policy {Id}", id);
                return StatusCode(500, new { success = false, message = "Failed to delete policy." });
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Guid GetCurrentOrganizationId()
        {
            var claim = User.FindFirstValue("organizationId");
            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("Organization ID not found in token.");
            return id;
        }

        private bool IsSuperAdmin() => User.IsInRole("Super Admin");
    }
}