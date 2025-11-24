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

        public async Task<bool> CreateNotificationAsync(Guid organizationId, NotificationCreateDto request)
        {
          

            var result = await _notificationRepository.CreateNotificationAsync(organizationId, request);
            return result;
        }

        // Get notifications for a specific user (manager/admin)
        public async Task<List<Notification>> GetNotificationsForUserAsync(Guid userId, Guid organizationId)
        {
            // The repository handles filtering, sorting, and unread logic
            var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, organizationId);
            return notifications;
        }


        public async Task<bool> MarkNotificationAsReadAsync(Guid notificationId)
        {
            var result = await _notificationRepository.MarkNotificationAsReadAsync(notificationId);
            return result;
        }



    }
}
