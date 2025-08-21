using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        // For demo purposes, just log the email instead of actually sending it
        _logger.LogInformation("Email would be sent to: {To}, Subject: {Subject}", to, subject);
        _logger.LogInformation("Email body: {Body}", body);
        
        // In a real implementation, you would use a service like SendGrid, AWS SES, etc.
        await Task.Delay(100); // Simulate async operation
        return true;
    }

    public async Task<bool> SendWelcomeEmailAsync(string to, string firstName, string organizationName, string tempPassword)
    {
        var subject = $"Welcome to {organizationName} HRMS";
        var body = $@"
            <h2>Welcome {firstName}!</h2>
            <p>Your account has been created in the {organizationName} HRMS system.</p>
            <p>Your temporary password is: <strong>{tempPassword}</strong></p>
            <p>Please login and change your password as soon as possible.</p>
            <p>Best regards,<br/>{organizationName} HR Team</p>
        ";

        return await SendEmailAsync(to, subject, body, true);
    }

    public async Task<bool> SendLeaveApprovalNotificationAsync(string to, string employeeName, string leaveType, DateTime startDate, DateTime endDate, bool approved)
    {
        var status = approved ? "approved" : "rejected";
        var subject = $"Leave Request {status.ToUpper()}";
        var body = $@"
            <h2>Leave Request Update</h2>
            <p>Dear {employeeName},</p>
            <p>Your {leaveType} request from {startDate:MMM dd, yyyy} to {endDate:MMM dd, yyyy} has been <strong>{status}</strong>.</p>
            <p>Please contact HR if you have any questions.</p>
            <p>Best regards,<br/>HR Team</p>
        ";

        return await SendEmailAsync(to, subject, body, true);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string to, string firstName, string resetLink)
    {
        var subject = "Password Reset Request";
        var body = $@"
            <h2>Password Reset</h2>
            <p>Dear {firstName},</p>
            <p>You have requested to reset your password. Click the link below to reset it:</p>
            <p><a href=""{resetLink}"">Reset Password</a></p>
            <p>If you didn't request this, please ignore this email.</p>
            <p>Best regards,<br/>HRMS Team</p>
        ";

        return await SendEmailAsync(to, subject, body, true);
    }
}
