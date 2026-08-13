using HRMS.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using static HRMS.API.Models.DTOs.NotificationDto;

namespace HRMS.API.Services.Interfaces
{
    public interface INotificationService
    {
        Task<ServiceResponse<bool>> CreateNotificationAsync(Guid organizationid, NotificationCreateDto request);
        Task<ServiceResponse<IReadOnlyList<Notification>>> GetNotificationsForUserAsync(Guid userId, Guid organizationId);
        Task<ServiceResponse<bool>> MarkNotificationAsReadAsync(Guid notificationId);
    }
}
