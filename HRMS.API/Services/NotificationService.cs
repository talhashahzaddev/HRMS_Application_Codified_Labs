using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using static HRMS.API.Models.DTOs.NotificationDto;

namespace HRMS.API.Services
{
    public class NotificationService : INotificationService
    {

        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notificationRepository,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse<bool>> CreateNotificationAsync(Guid organizationId, NotificationCreateDto request)
        {
            _logger.LogInformation("Creating notification for organization: {OrganizationId}", organizationId);

            // Validate request
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Request cannot be null");
            }

            if (organizationId == Guid.Empty)
            {
                throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                throw new ArgumentException("Notification title is required", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                throw new ArgumentException("Notification message is required", nameof(request));
            }

            try
            {
                var result = await _notificationRepository.CreateNotificationAsync(organizationId, request);
                
                if (!result)
                {
                    _logger.LogError("Failed to create notification for organization: {OrganizationId}", organizationId);
                    throw new InvalidOperationException("Failed to create notification");
                }

                _logger.LogInformation("Successfully created notification for organization: {OrganizationId}", organizationId);

                return new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "Notification sent successfully",
                    Data = true
                };
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw business logic exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating notification for organization: {OrganizationId}", organizationId);
                throw new InvalidOperationException("An error occurred while creating the notification", ex);
            }
        }

        // Get notifications for a specific user (manager/admin)
        public async Task<ServiceResponse<IReadOnlyList<Notification>>> GetNotificationsForUserAsync(Guid userId, Guid organizationId)
        {
            _logger.LogInformation("Getting notifications for user: {UserId}, organization: {OrganizationId}", userId, organizationId);

            // Validate parameters
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }

            if (organizationId == Guid.Empty)
            {
                throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
            }

            try
            {
                // The repository handles filtering, sorting, and unread logic
                var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, organizationId);

                _logger.LogInformation("Successfully retrieved {Count} notifications for user: {UserId}", notifications?.Count ?? 0, userId);

                return new ServiceResponse<IReadOnlyList<Notification>>
                {
                    Success = true,
                    Message = "Notifications retrieved successfully",
                    Data = notifications
                };
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for user: {UserId}, organization: {OrganizationId}", userId, organizationId);
                throw new InvalidOperationException("An error occurred while retrieving notifications", ex);
            }
        }


        public async Task<ServiceResponse<bool>> MarkNotificationAsReadAsync(Guid notificationId)
        {
            _logger.LogInformation("Marking notification as read: {NotificationId}", notificationId);

            // Validate parameters
            if (notificationId == Guid.Empty)
            {
                throw new ArgumentException("Notification ID cannot be empty", nameof(notificationId));
            }

            try
            {
                var result = await _notificationRepository.MarkNotificationAsReadAsync(notificationId);
                
                if (!result)
                {
                    _logger.LogWarning("Notification not found for marking as read: {NotificationId}", notificationId);
                    return new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "Notification not found"
                    };
                }

                _logger.LogInformation("Successfully marked notification as read: {NotificationId}", notificationId);

                return new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "Notification marked as read",
                    Data = true
                };
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read: {NotificationId}", notificationId);
                throw new InvalidOperationException("An error occurred while marking notification as read", ex);
            }
        }



    }
}
