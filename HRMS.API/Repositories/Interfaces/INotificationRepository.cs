using static HRMS.API.Models.DTOs.NotificationDto;

namespace HRMS.API.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task<bool> CreateNotificationAsync(Guid? organizationId, NotificationCreateDto request);
        Task<List<Notification>> GetUserNotificationsAsync(Guid userId, Guid organizationId);
        Task<bool> MarkNotificationAsReadAsync(Guid notificationId);
        Task<Guid> GetEmpId(Guid? employeeId);
        Task<Guid> GetReciveridAsync(Guid employeeId);
        Task<Notification> getnotificationdetailAsync(Guid requestId);
    }
}
