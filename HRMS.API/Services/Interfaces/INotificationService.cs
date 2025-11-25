using HRMS.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using static HRMS.API.Models.DTOs.NotificationDto;

namespace HRMS.API.Services.Interfaces
{
    public interface INotificationService
    {
        Task<bool> CreateNotificationAsync(Guid organizationid, NotificationCreateDto request);
        Task<List<Notification>> GetNotificationsForUserAsync(Guid userId, Guid organizationId);
        Task<bool> MarkNotificationAsReadAsync(Guid notificationId);
    }
}
