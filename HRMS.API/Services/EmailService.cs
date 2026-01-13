using HRMS.API.Services.Interfaces;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Newtonsoft.Json.Linq;

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
            var publicKey = _configuration["MailJet:PublicKey"];
            var privateKey = _configuration["MailJet:PrivateKey"];
            var fromEmail = _configuration["MailJet:FromEmail"];
            var fromName = _configuration["MailJet:FromName"];

            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogError("MailJet FromEmail is missing or not verified.");
                return false;
            }

            var client = new MailjetClient(publicKey, privateKey);

            var request = new MailjetRequest
            {
                Resource = SendV31.Resource
            }
            .Property(Send.Messages, new JArray {
                new JObject {
                    { "From", new JObject {
                        { "Email", fromEmail },
                        { "Name", fromName }
                    }},
                    { "To", new JArray {
                        new JObject {
                            { "Email", to },
                            { "Name", to }
                        }
                    }},
                    { "Subject", subject },
                    { "HTMLPart", body }
                }
            });

            var response = await client.PostAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Mailjet email failed. Status: {Status}, Response: {Response}",
                    response.StatusCode,
                    response.GetData()
                );
                return false;
            }

            _logger.LogInformation("Email sent successfully to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mailjet email exception");
            return false;
        }
    }

    // ================= EXISTING METHODS (UNCHANGED) =================

    public async Task<bool> SendWelcomeEmailAsync(
        string to,
        string firstName,
        string organizationName,
        string tempPassword)
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

    public async Task<bool> SendLeaveApprovalNotificationAsync(
        string to,
        string employeeName,
        string leaveType,
        DateTime startDate,
        DateTime endDate,
        bool approved)
    {
        var status = approved ? "approved" : "rejected";
        var subject = $"Leave Request {status.ToUpper()}";
        var body = $@"
            <h2>Leave Request Update</h2>
            <p>Dear {employeeName},</p>
            <p>Your {leaveType} request from {startDate:MMM dd, yyyy} 
               to {endDate:MMM dd, yyyy} has been <strong>{status}</strong>.</p>
            <p>Please contact HR if you have any questions.</p>
            <p>Best regards,<br/>HR Team</p>
        ";

        return await SendEmailAsync(to, subject, body);
    }

    public async Task<bool> SendPasswordResetEmailAsync(
        string to,
        string firstName,
        string resetLink)
    {
        var subject = $"Password Reset Request - {Guid.NewGuid().ToString()[..6]}";
        var body = $@"
            <h2>Password Reset</h2>
            <p>Dear {firstName},</p>
            <p>You have requested to reset your password. Click the link below:</p>
            <p><a href=""{resetLink}"">Reset Password</a></p>
            <p>If you didn't request this, please ignore this email.</p>
            <p>Best regards,<br/>HRMS Team</p>
        ";

        return await SendEmailAsync(to, subject, body);
    }
}
