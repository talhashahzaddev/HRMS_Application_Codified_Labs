using HRMS.API.Models.DTOs;
using HRMS.API.Services;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static HRMS.API.Models.DTOs.NewsDTOs;

namespace HRMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly ILogger<NewsController> _logger;
        private readonly INewsService _newsService;

        public NewsController(ILogger<NewsController> logger, INewsService newsService)
        {
            _logger = logger;
            _newsService = newsService;
        }


        [HttpPost("createNews")]
        [Authorize(Roles = "SuperAdmin,Hr Manager")]
        public async Task<ActionResult<ServiceResponse<NewsDto>>> CreateNews([FromBody] NewsRequest request)
        {
            try
            {
                // Validate the model
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ServiceResponse<NewsDto>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }
                var currentEmployeeId = await GetCurrentEmployeeId();
                var organizationId = GetCurrentOrganizationId();
                var newsItem = await _newsService.CreateNewsAsync(currentEmployeeId, organizationId, request);
                return Ok(new ServiceResponse<NewsDto>
                {
                    Success = true,
                    Message = "News created successfully",
                    Data = newsItem
                });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "Null request provided for creating news");
                return BadRequest(new ServiceResponse<NewsDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for creating news");
                return BadRequest(new ServiceResponse<NewsDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operation failed while creating news");
                return BadRequest(new ServiceResponse<NewsDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to create news");
                return Unauthorized(new ServiceResponse<NewsDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating news");
                return StatusCode(StatusCodes.Status500InternalServerError, new ServiceResponse<NewsDto>
                {
                    Success = false,
                    Message = "An unexpected error occurred while creating news"
                });
            }
        }

        [HttpGet("getAllNews")]
        public async Task<ActionResult<ServiceResponse<PagedResult<NewsDto>>>> GetAllNews([FromQuery] NewsSearchViewModel? searchModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ServiceResponse<PagedResult<NewsDto>>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }
                var organizationId = GetCurrentOrganizationId();
                var currentEmployeeId = await GetCurrentEmployeeId();
                var newsItem = await _newsService.GetAllNewsAsync(currentEmployeeId, organizationId, searchModel);
                return Ok(new ServiceResponse<PagedResult<NewsDto>>
                {
                    Success = true,
                    Message = "News Get successfully",
                    Data = newsItem
                });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "Null request provided for Getting news");
                return BadRequest(new ServiceResponse<PagedResult<NewsDto>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for getting news");
                return BadRequest(new ServiceResponse<PagedResult<NewsDto>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operation failed while getting news");
                return BadRequest(new ServiceResponse<PagedResult<NewsDto>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to getting news");
                return Unauthorized(new ServiceResponse<PagedResult<NewsDto>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating news");
                return StatusCode(StatusCodes.Status500InternalServerError, new ServiceResponse<PagedResult<NewsDto>>
                {
                    Success = false,
                    Message = "An unexpected error occurred while creating news"
                });
            }
        }

        [HttpPut("edit/{newsId}")]
        public async Task<ActionResult<ServiceResponse<bool>>> EditNews(Guid newsId, [FromBody] NewsRequest request)
        {
            try
            {
                // Validate request
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
                var currentEmployeeId = await GetCurrentEmployeeId();
                var organizationId = GetCurrentOrganizationId();
                var isUpdated = await _newsService.UpdateNewsAsync(newsId, currentEmployeeId, organizationId, request);
                return Ok(new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "News updated successfully",
                    Data = isUpdated
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "News not found for update. NewsId: {NewsId}", newsId);
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = false
                });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "Null request provided for updating news");
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = false
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for updating news");
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = false
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to update news");
                return Unauthorized(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating news");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "An unexpected error occurred while updating news",
                        Data = false
                    });
            }
        }

        [HttpGet("{newsId}")]
        public async Task<ActionResult<ServiceResponse<NewsDto>>> GetNewsByNewsId(Guid newsId)
        {
            try
            {
                // Validate request
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ServiceResponse<NewsDto>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }
                var currentEmployeeId = await GetCurrentEmployeeId();
                var organizationId = GetCurrentOrganizationId();
                var results = await _newsService.GetNewsByNewsIdAsync(newsId);
                return Ok(new ServiceResponse<NewsDto>
                {
                    Success = true,
                    Message = "News updated successfully",
                    Data = results
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "News not found for update. NewsId: {NewsId}", newsId);
                return NotFound(new ServiceResponse<NewsDto>
                {
                    Success = false,
                    Message = ex.Message,
                });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "Null request provided for updating news");
                return BadRequest(new ServiceResponse<NewsDto>
                {
                    Success = false,
                    Message = ex.Message,
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument provided for updating news");
                return BadRequest(new ServiceResponse<NewsDto>
                {
                    Success = false,
                    Message = ex.Message,
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to update news");
                return Unauthorized(new ServiceResponse<NewsDto>
                {
                    Success = false,
                    Message = ex.Message,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating news");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ServiceResponse<NewsDto>
                    {
                        Success = false,
                        Message = "An unexpected error occurred while updating news",
                    });
            }
        }

        [HttpDelete("{newsId}")]
        public async Task<ActionResult<ServiceResponse<bool>>> DeleteNews(Guid newsId)
        {
            try
            {
                var isDeleted = await _newsService.DeleteNewsAsync(newsId);

                return Ok(new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "News deleted successfully",
                    Data = isDeleted
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "News not found for delete. NewsId: {NewsId}", newsId);
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = false
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized delete attempt. NewsId: {NewsId}", newsId);
                return Unauthorized(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting news");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "An unexpected error occurred while deleting news",
                        Data = false
                    });
            }
        }

        [HttpPost("{newsId}/view")]
        public async Task<ActionResult<ServiceResponse<bool>>> AddNewsView(Guid newsId)
        {
            try
            {
                var isUpdated = await _newsService.AddNewsViewAsync(newsId);

                return Ok(new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "News view count updated successfully",
                    Data = isUpdated
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "News not found for view increment. NewsId: {NewsId}", newsId);
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = false
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized view increment attempt. NewsId: {NewsId}", newsId);
                return Unauthorized(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing news view count");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "An unexpected error occurred while updating news view count",
                        Data = false
                    });
            }
        }

        [HttpPost("{newsId}/mark-read")]
        public async Task<ActionResult<ServiceResponse<bool>>> MarkNewsAsRead(Guid newsId)
        {
            try
            {
                var employeeId = await GetCurrentEmployeeId(); // logged-in user

                // Call service: add read record + increment views if first read
                var isFirstRead = await _newsService.MarkNewsAsReadAsync(newsId, employeeId);

                return Ok(new ServiceResponse<bool>
                {
                    Success = true,
                    Message = isFirstRead ? "News marked as read & view incremented" : "Already read",
                    Data = isFirstRead
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "News not found for read. NewsId: {NewsId}", newsId);
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = false
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized read attempt. NewsId: {NewsId}", newsId);
                return Unauthorized(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking news as read. NewsId: {NewsId}", newsId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "An unexpected error occurred while marking news as read",
                        Data = false
                    });
            }
        }

        [HttpPatch("{newsId}/status")]
        public async Task<ActionResult<ServiceResponse<bool>>> UpdateNewsStatus(Guid newsId,string? status)   // directly from query string
        {
            try
            {
                if (string.IsNullOrWhiteSpace(status))
                    return BadRequest(new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "Status is required",
                        Data = false
                    });

                var isUpdated = await _newsService.UpdateNewsStatusAsync(newsId, status);

                return Ok(new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "News status updated successfully",
                    Data = isUpdated
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = false
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "News not found. NewsId: {NewsId}", newsId);
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating news status");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "An unexpected error occurred while updating news status",
                        Data = false
                    });
            }
        }
        private async Task<Guid> GetCurrentEmployeeId()
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
            var employeeId = await _newsService.GetEmployeeFromTable(userId);
            return employeeId;
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
}
