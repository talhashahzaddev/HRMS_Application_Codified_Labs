using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
                var organizationid = GetCurrentOrganizationId();

                await _notificationService.CreateNotificationAsync(organizationid, request);

                return Ok(new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "Notification sent successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
                return StatusCode(500, new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while sending the notification"
                });
            }
        }

        [HttpGet("user")]
        public async Task<ActionResult<ServiceResponse<List<Notification>>>> GetUserNotifications(Guid userId)
        {
            try
            {
                var organizationId = GetCurrentOrganizationId(); // optional if needed

                // Repository/service will handle filtering, sorting, etc.
                var notifications = await _notificationService.GetNotificationsForUserAsync(userId, organizationId);

                return Ok(new ServiceResponse<List<Notification>>
                {
                    Success = true,
                    Message = "Notifications retrieved successfully",
                    Data = notifications
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                return StatusCode(500, new ServiceResponse<List<Notification>>
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
                var result = await _notificationService.MarkNotificationAsReadAsync(notificationId);
                
                return Ok(new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "Notification marked as read",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
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
