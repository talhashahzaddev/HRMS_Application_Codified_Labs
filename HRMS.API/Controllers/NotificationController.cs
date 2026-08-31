using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using static HRMS.API.Models.DTOs.NotificationDto;

namespace HRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }


        [HttpPost("sendNotification")]
        public async Task<ActionResult<ServiceResponse<bool>>> SendNotification([FromBody] NotificationCreateDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                var organizationid = GetCurrentOrganizationId();

                var response = await _notificationService.CreateNotificationAsync(organizationid, request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "Null request provided for sending notification");
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for sending notification");
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access while sending notification");
                return StatusCode(403, new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Business logic error sending notification");
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending notification");
                return StatusCode(500, new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while sending the notification"
                });
            }
        }

        [HttpGet("user")]
        public async Task<ActionResult<ServiceResponse<IReadOnlyList<Notification>>>> GetUserNotifications(Guid userId)
        {
            try
            {
                if (userId == Guid.Empty)
                {
                    return BadRequest(new ServiceResponse<IReadOnlyList<Notification>>
                    {
                        Success = false,
                        Message = "User ID is required"
                    });
                }

                var organizationId = GetCurrentOrganizationId(); // optional if needed

                // Repository/service will handle filtering, sorting, etc.
                var response = await _notificationService.GetNotificationsForUserAsync(userId, organizationId);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for getting user notifications: {UserId}", userId);
                return BadRequest(new ServiceResponse<IReadOnlyList<Notification>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access while getting user notifications: {UserId}", userId);
                return StatusCode(403, new ServiceResponse<IReadOnlyList<Notification>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for user: {UserId}", userId);
                return StatusCode(500, new ServiceResponse<IReadOnlyList<Notification>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving notifications"
                });
            }
        }

        [HttpPut("NotificationMark-read")]
        public async Task<ActionResult<ServiceResponse<bool>>> MarkNotificationAsRead(Guid notificationId)
        {
            try
            {
                if (notificationId == Guid.Empty)
                {
                    return BadRequest(new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "Notification ID is required"
                    });
                }

                var response = await _notificationService.MarkNotificationAsReadAsync(notificationId);
                
                if (!response.Success)
                {
                    return NotFound(response);
                }

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for marking notification as read: {NotificationId}", notificationId);
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Business logic error marking notification as read: {NotificationId}", notificationId);
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read: {NotificationId}", notificationId);
                return StatusCode(500, new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while marking notification as read"
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
    }
}
