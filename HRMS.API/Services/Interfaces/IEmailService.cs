namespace HRMS.API.Services.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body);
    Task<bool> SendWelcomeEmailAsync(string to, string firstName, string organizationName, string tempPassword);
    Task<bool> SendLeaveApprovalNotificationAsync(string to, string employeeName, string leaveType, DateTime startDate, DateTime endDate, bool approved);
    Task<bool> SendPasswordResetEmailAsync(string to, string firstName, string resetLink);
}
