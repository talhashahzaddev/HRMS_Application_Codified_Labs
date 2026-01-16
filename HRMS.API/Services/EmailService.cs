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

    private async Task<bool> SendEmailAsync(string to, string subject, string body)
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

    public async Task<bool> SendEmployeeCredentialsAsync(string email, string employeeName, string temporaryPassword, string loginLink)
    {
        var subject = "Login Credentials | BriskPeople";

        var body = $@"<!DOCTYPE html>
        <html>
            <head>
                <meta charset='UTF-8'>
            </head>
            <body style='margin:0; padding:0; background-color:#f4f6f8; font-family:Arial, Helvetica, sans-serif;'>
                <table width='100%' cellpadding='0' cellspacing='0' style='padding:30px 0;'>
                <tr>
                <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0'style='background:#ffffff; border-radius:8px; overflow:hidden;'>
                <tr>
                <td align=""center"" style=""padding:30px; background:#ffffff;"">
                    <table cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin:auto;"">
                    <tr>
                        <td style=""vertical-align:middle; padding-right:10px;"">
                            <img src=""https://www.briskpeople.com/hub.png"" alt=""BriskPeople"" style=""height:40px; display:block;"" />
                        </td>
                        <td style=""vertical-align:middle; font-size:22px; font-weight:bold; color:#1f4fd8;"">
                            BriskPeople
                        </td>
                    </tr>
                    </table>
                </td>
                </tr>
                <tr>
                    <td style='padding:30px; color:#333333; font-size:14px; line-height:1.7;'>
                    <p style='margin-top:0;'>Hi <strong>{employeeName}</strong>,</p>
                    <p>
                        Welcome to <strong>BriskPeople</strong>!  
                        Your organization has created an account for you on our platform.
                    </p>
                    <p>
                        Below are your login credentials:
                    </p>
                    <table width='100%' cellpadding='10' cellspacing='0' style='background:#f9fafb; border-radius:6px; margin:20px 0;'>
                    <tr>
                        <td width='30%' style='font-weight:bold;'>Email</td>
                        <td>{email}</td>
                    </tr>
                    <tr>
                        <td width='30%' style='font-weight:bold;'>Password</td>
                        <td>{temporaryPassword}</td>
                    </tr>
                </table>
                <p>
                    For security reasons, please change your password immediately
                    after your first login.
                </p>
                <table width='100%' cellpadding='0' cellspacing='0' style='margin:30px 0;'>
                    <tr>
                        <td align='center'>
                            <a href='{loginLink}'
                               style='background:#1f4fd8; color:#ffffff; padding:14px 28px; text-decoration:none; border-radius:6px; font-size:14px; font-weight:bold; display:inline-block;'>
                                Login to BriskPeople
                            </a>
                        </td>
                    </tr>
                </table>
                <p>
                    If you experience any issues accessing your account,
                    please contact your HR administrator.
                </p>
                <p style='margin-bottom:0;'>
                    Best regards,<br/>
                    <strong>BriskPeople Team</strong>
                </p>
                </td>
                </tr>
                <tr>
                <td align='center' style='padding:20px; background:#f4f6f8; font-size:12px; color:#777777;'>
                © {DateTime.UtcNow.Year} BriskPeople. All rights reserved.<br/>
                </td>
                </tr>
                </table>
                </td>
                </tr>
                </table>
            </body>
        </html>";
        return await SendEmailAsync(email, subject, body);
    }
    public async Task<bool> SendVerificationEmailAsync(string email,string adminName,string verificationToken)
    {
        var verifyLink =$"{_configuration["EmployeePortal:PortalUrl"]}verify-email?token={verificationToken}";

        var subject = "Verify Your Organization Email | BriskPeople";

        var body = $@"<!DOCTYPE html>
        <html>
            <body style='margin:0;padding:0;background:#f4f6f8;font-family:Arial,Helvetica,sans-serif;'>
                <table width='100%' cellpadding='0' cellspacing='0' style='padding:30px 0;'>
                <tr>
                    <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:8px;overflow:hidden;'>
                <tr>
                    <td style='background:#ffffff;padding:20px 30px;border-bottom:1px solid #e5e7eb;'>
                <table cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin:auto;"">
                    <tr>
                        <td style=""vertical-align:middle; padding-right:10px;"">
                            <img src=""https://www.briskpeople.com/hub.png"" alt=""BriskPeople"" style=""height:40px; display:block;"" />
                        </td>
                        <td style=""vertical-align:middle; font-size:22px; font-weight:bold; color:#1f4fd8;"">
                            BriskPeople
                        </td>
                    </tr>
                </table>
                    </td>
                </tr>
                <tr>
                    <td style='padding:35px;color:#333;font-size:14px;line-height:1.7;'>
                        <p style='margin-top:0;'>Hi <strong>{adminName}</strong>,</p>
                        <p>Thank you for registering your organization on<strong>BriskPeople</strong>.</p>
                        <p>To complete your registration and activate your account,please verify your email address by clicking the button below:</p>
                        <p style='text-align:center;margin:35px 0;'>
                            <a href='{verifyLink}' style='background:#1f4fd8;color:#ffffff;padding:14px 32px;text-decoration:none;border-radius:6px;font-weight:bold;display:inline-block;'>Verify Email</a>
                        </p>
                        <p>This verification link will remain valid for <strong>24 hours</strong>.If you did not request this, you may safely ignore this email.</p>
                        <p style='margin-bottom:0;'>Best regards,<br/><strong>BriskPeople Team</strong></p>
                    </td>
                </tr>
                <tr>
                    <td align='center' style='padding:18px;background:#f4f6f8;font-size:12px;color:#777;'>
                        © {DateTime.UtcNow.Year} BriskPeople. All rights reserved.
                    </td>
                </tr>
                </table>
                    </td>
                </tr>
                </table>
            </body>
        </html>";

        return await SendEmailAsync(email, subject, body);
    }
}
