using static HRMS.API.Models.DTOs.NotificationDto;

namespace HRMS.API.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task<bool> DeleteNotificationAsync(Guid id);
        Task<bool> CreateNotificationAsync(Guid? organizationId, NotificationCreateDto request);
        Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(Guid userId, Guid organizationId);
        Task<bool> MarkNotificationAsReadAsync(Guid notificationId);
        Task<Guid> GetEmpId(Guid? employeeId);
        Task<Guid> GetReciveridAsync(Guid employeeId);
        Task<leaveNotification> GetReciveridANdEmailAsync(Guid employeeId);
        Task<leaveNotification> GetNoManagerReciveridAsync(Guid employeeId);
        Task<Notification> getnotificationdetailAsync(Guid requestId);
        Task<(Guid userId, Guid organizationId)?> GetEmployeeUserInfoAsync(Guid employeeId);
        Task<bool> UpdateNotificationRequestStatusAsync(Guid requestid, string status);
    }
}
