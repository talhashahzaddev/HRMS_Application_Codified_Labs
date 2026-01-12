using HRMS.API.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

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


    public async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _configuration["Email:FromName"],
                _configuration["Email:FromEmail"]
            ));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = body
            };

            using var smtp = new SmtpClient();
            smtp.Timeout = 20000;
            var smtpHost = _configuration["Email:SmtpHost"];
            _logger.LogInformation("SMTP HOST: {Host}", smtpHost);
            await smtp.ConnectAsync(
                _configuration["Email:SmtpHost"],
                int.Parse(_configuration["Email:SmtpPort"]),
                SecureSocketOptions.StartTls
            );

            await smtp.AuthenticateAsync(
                _configuration["Email:Username"],
                _configuration["Email:Password"]
            );
            try
            {
                await smtp.SendAsync(email);
                _logger.LogInformation("Email send successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {To}", to);
                throw;
            }
            await smtp.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email failed");
            return false;
        }
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

        return await SendEmailAsync(to, subject, body);
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

        return await SendEmailAsync(to, subject, body);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string to, string firstName, string resetLink)
    {
        var subject = $"Password Reset Request - {Guid.NewGuid().ToString().Substring(0, 6)}"; // unique subject to avoid 
        var body = $@"
            <h2>Password Reset</h2>
            <p>Dear {firstName},</p>
            <p>You have requested to reset your password. Click the link below to reset it:</p>
            <p><a href=""{resetLink}"">Reset Password</a></p>
            <p>If you didn't request this, please ignore this email.</p>
            <p>Best regards,<br/>HRMS Team</p>
        ";

        return await SendEmailAsync(to, subject, body);
    }
}
