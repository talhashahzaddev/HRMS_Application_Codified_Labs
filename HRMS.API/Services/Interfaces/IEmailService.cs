using HRMS.API.Models.Entities;
using static HRMS.API.Models.DTOs.HelpDeskDto;
using static HRMS.API.Models.DTOs.NewsDTOs;

namespace HRMS.API.Services.Interfaces;

public interface IEmailService
{
    Task<bool> SendWelcomeEmailAsync(string to, string firstName, string organizationName, string tempPassword);
    Task<bool> SendLeaveApprovalRejectionEmailAsync(string to, string employeeName, string leaveType, DateTime startDate, DateTime endDate, bool approved, Guid organizationid);
    Task<bool> SendLeaveRequestToManagerAsync(string managerEmail, string managerName, string employeeName, string leaveType, DateTime startDate, DateTime endDate, string reason, Guid organizationid);
    Task<bool> SendPasswordResetEmailAsync(string to, string firstName, string resetLink);
    Task<bool> SendEmployeeCredentialsAsync(string email, string employeeName, string temporaryPassword, string loginLink);
    Task<bool> SendVerificationEmailAsync(string email, string adminName, string verificationToken);
    Task<bool> SendNewsEmailAsync(List<string> receiverEmails, NewsRequest request,Guid OrganizationId,Guid? newsId);
    Task<bool> SendDemoEmailAsync(string toEmail, string fullName, string companyName, string fromEmail);
    Task<bool> SendAssignShiftEmailAsync(List<string> receiverEmails, string message, Guid organizationId);
    Task<bool> SendHelpDeskChatAsync(List<string> receiverEmails, TicketMessageRequest request, Guid organizationId, string ticketTitle);
    Task<bool> SendPayslipEmailAsync(
        string toEmail,
        string employeeName,
        string periodName,
        string payslipDownloadUrl,
        Guid organizationId,
        string? emailSubject = null,
        string? emailMessage = null);
    Task<bool> SendUserInvitationAsync(string email, string firstName, string organizationName, string setPasswordLink);
}
