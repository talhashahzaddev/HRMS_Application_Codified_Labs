using System.Net;
using DocumentFormat.OpenXml.Wordprocessing;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Newtonsoft.Json.Linq;
using static HRMS.API.Models.DTOs.HelpDeskDto;
using static HRMS.API.Models.DTOs.NewsDTOs;

namespace HRMS.API.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAuthRepository _authRepository;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration, IAuthRepository authRepository)
    {
        _logger = logger;
        _configuration = configuration;
        _authRepository = authRepository;
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

    public async Task<bool> SendLeaveApprovalRejectionEmailAsync(
    string to,
    string employeeName,
    string leaveType,
    DateTime startDate,
    DateTime endDate,
    bool approved,
    Guid organizationid)
    {
        var domain = await _authRepository.GetDomainByOrganizationIdAsync(organizationid);
        var cleanDomain = domain.Trim().ToLower().Replace("http://", "").Replace("https://", "").Replace(".com", "");
        var status = approved ? "approved" : "rejected";
        var statusColor = approved ? "#28a745" : "#dc3545"; // Green for approved, red for rejected
        var subject = $"Leave Request {status.ToUpper()} | BriskPeople";
        var dashboardLink = $"https://{cleanDomain}.briskpeople.com/leave/dashboard";

        var body = $@"<!DOCTYPE html>
    <html>
        <head>
            <meta charset='UTF-8'>
        </head>
        <body style='margin:0; padding:0; background-color:#f4f6f8; font-family:Arial, Helvetica, sans-serif;'>
            <table width='100%' cellpadding='0' cellspacing='0' style='padding:30px 0;'>
            <tr>
            <td align='center'>
            <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff; border-radius:8px; overflow:hidden;'>
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
                    
                    <p>Your leave request has been:</p>
                    
                    <!-- Status Badge -->
                    <table width='100%' cellpadding='0' cellspacing='0' style='margin:20px 0;'>
                        <tr>
                            <td align='center'>
                                <span style='
                                    background:{statusColor}; 
                                    color:#ffffff; 
                                    padding:10px 30px; 
                                    border-radius:50px; 
                                    font-size:18px; 
                                    font-weight:bold; 
                                    display:inline-block;
                                    text-transform:uppercase;'>
                                    {status}
                                </span>
                            </td>
                        </tr>
                    </table>
                    
                    <!-- Leave Details Card -->
                    <table width='100%' cellpadding='15' cellspacing='0' style='background:#f9fafb; border-radius:8px; margin:20px 0; border:1px solid #e9ecef;'>
                        <tr>
                            <td colspan='2' style='border-bottom:1px solid #e9ecef; font-weight:bold; color:#1f4fd8; font-size:15px;'>
                                📋 Leave Request Details
                            </td>
                        </tr>
                        <tr>
                            <td width='35%' style='font-weight:bold; color:#555555;'>Leave Type</td>
                            <td>{leaveType}</td>
                        </tr>
                        <tr>
                            <td width='35%' style='font-weight:bold; color:#555555;'>Start Date</td>
                            <td>{startDate:dddd, MMMM dd, yyyy}</td>
                        </tr>
                        <tr>
                            <td width='35%' style='font-weight:bold; color:#555555;'>End Date</td>
                            <td>{endDate:dddd, MMMM dd, yyyy}</td>
                        </tr>
                        <tr>
                            <td width='35%' style='font-weight:bold; color:#555555;'>Duration</td>
                            <td>{(endDate - startDate).Days + 1} day(s)</td>
                        </tr>
                        <tr>
                            <td width='35%' style='font-weight:bold; color:#555555;'>Status</td>
                            <td style='color:{statusColor}; font-weight:bold; text-transform:uppercase;'>{status}</td>
                        </tr>
                    </table>
                    
                    <p style='margin:20px 0;'>
                        {(approved ?
                              "Congratulations! Your leave request has been approved. You can view the details in your dashboard." :
                              "Unfortunately, your leave request has been rejected. Please contact your HR administrator for more information.")}
                    </p>
                    
                    <!-- Dashboard Button -->
                    <table width='100%' cellpadding='0' cellspacing='0' style='margin:30px 0;'>
                        <tr>
                            <td align='center'>
                                <a href='{dashboardLink}'
                                   style='background:#1f4fd8; color:#ffffff; padding:14px 28px; text-decoration:none; border-radius:6px; font-size:14px; font-weight:bold; display:inline-block;'>
                                    View Leave Dashboard →
                                </a>
                            </td>
                        </tr>
                    </table>
                    
                    <!-- Alternative text link for email clients that block images/buttons -->
                    <p style='text-align:center; font-size:12px; color:#777777; margin:10px 0;'>
                        If the button doesn't work, copy and paste this link: <br>
                        <a href='{dashboardLink}' style='color:#1f4fd8;'>{dashboardLink}</a>
                    </p>
                    
                    <p>
                        If you have any questions regarding this decision, 
                        please don't hesitate to contact your HR administrator.
                    </p>
                    
                    <p style='margin-bottom:0;'>
                        Best regards,<br/>
                        <strong>HR Team</strong><br/>
                        BriskPeople
                    </p>
                </td>
            </tr>
            <tr>
                <td align='center' style='padding:20px; background:#f4f6f8; font-size:12px; color:#777777;'>
                    © {DateTime.UtcNow.Year} BriskPeople. All rights reserved.<br/>
                    This is an automated message, please do not reply to this email.
                </td>
            </tr>
            </table>
            </td>
            </tr>
            </table>
        </body>
    </html>";

        return await SendEmailAsync(to, subject, body);
    }

    public async Task<bool> SendLeaveRequestToManagerAsync(
    string managerEmail,
    string managerName,
    string employeeName,
    string leaveType,
    DateTime startDate,
    DateTime endDate,
    string reason, Guid organizationid)
    {
        var domain = await _authRepository.GetDomainByOrganizationIdAsync(organizationid);
        var cleanDomain = domain.Trim().ToLower().Replace("http://", "").Replace("https://", "").Replace(".com", "");

        var subject = $"Leave Request Pending Approval | {employeeName} | BriskPeople";
        var pendingColor = "#ffc107"; // Yellow/amber for pending
        var approvalLink = $"https://{cleanDomain}.briskpeople.com/leave/team";

        var body = $@"<!DOCTYPE html>
    <html>
        <head>
            <meta charset='UTF-8'>
        </head>
        <body style='margin:0; padding:0; background-color:#f4f6f8; font-family:Arial, Helvetica, sans-serif;'>
            <table width='100%' cellpadding='0' cellspacing='0' style='padding:30px 0;'>
            <tr>
            <td align='center'>
            <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff; border-radius:8px; overflow:hidden;'>
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
                    <p style='margin-top:0;'>Hi <strong>{managerName}</strong>,</p>
                    
                    <p>A leave request requires your attention:</p>
                    
                    <!-- Pending Status Badge -->
                    <table width='100%' cellpadding='0' cellspacing='0' style='margin:20px 0;'>
                        <tr>
                            <td align='center'>
                                <span style='
                                    background:{pendingColor}; 
                                    color:#000000; 
                                    padding:10px 30px; 
                                    border-radius:50px; 
                                    font-size:18px; 
                                    font-weight:bold; 
                                    display:inline-block;
                                    text-transform:uppercase;'>
                                    ⏳ PENDING APPROVAL
                                </span>
                            </td>
                        </tr>
                    </table>
                    
                    <!-- Employee Info Card -->
                    <table width='100%' cellpadding='15' cellspacing='0' style='background:#f9fafb; border-radius:8px; margin:20px 0; border:1px solid #e9ecef;'>
                        <tr>
                            <td colspan='2' style='border-bottom:1px solid #e9ecef; font-weight:bold; color:#1f4fd8; font-size:15px;'>
                                👤 Employee Information
                            </td>
                        </tr>
                        <tr>
                            <td width='35%' style='font-weight:bold; color:#555555;'>Employee</td>
                            <td>
                                <strong>{employeeName}</strong> 
                            </td>
                        </tr>
                    </table>
                    
                    <!-- Leave Request Details Card -->
                    <table width='100%' cellpadding='15' cellspacing='0' style='background:#f9fafb; border-radius:8px; margin:20px 0; border:1px solid #e9ecef;'>
                        <tr>
                            <td colspan='2' style='border-bottom:1px solid #e9ecef; font-weight:bold; color:#1f4fd8; font-size:15px;'>
                                📋 Leave Request Details
                            </td>
                        </tr>
                        <tr>
                            <td width='35%' style='font-weight:bold; color:#555555;'>Leave Type</td>
                            <td>{leaveType}</td>
                        </tr>
                        <tr>
                            <td width='35%' style='font-weight:bold; color:#555555;'>Start Date</td>
                            <td>{startDate:dddd, MMMM dd, yyyy}</td>
                        </tr>
                        <tr>
                            <td width='35%' style='font-weight:bold; color:#555555;'>End Date</td>
                            <td>{endDate:dddd, MMMM dd, yyyy}</td>
                        </tr>
                        <tr>
                            <td width='35%' style='font-weight:bold; color:#555555;'>Duration</td>
                            <td>{(endDate - startDate).Days + 1} day(s)</td>
                        </tr>
                    </table>
                    
                    <!-- Reason Card (if reason is provided) -->
                    {(string.IsNullOrEmpty(reason) ? "" : $@"
                    <table width='100%' cellpadding='15' cellspacing='0' style='background:#f9fafb; border-radius:8px; margin:20px 0; border:1px solid #e9ecef;'>
                        <tr>
                            <td style='border-bottom:1px solid #e9ecef; font-weight:bold; color:#1f4fd8; font-size:15px;'>
                                📝 Reason for Leave
                            </td>
                        </tr>
                        <tr>
                            <td style='color:#555555; font-style:italic;'>
                                ""{reason}""
                            </td>
                        </tr>
                    </table>
                    ")}
                    
                    
                    
                    <!-- Alternative text links -->
                    <p style='text-align:center; font-size:12px; color:#777777; margin:10px 0;'>
                        Quick actions:<br>
                        <a href='{approvalLink}' style='color:#1f4fd8;'>View All Pending Requests</a>
                    </p>
                    
                    <p style='margin-top:25px; padding-top:15px; border-top:1px solid #e9ecef; font-size:13px; color:#777777;'>
                        <strong>Next Steps:</strong> Please review this leave request and take action at your earliest convenience. 
                        The employee will be notified of your decision via email.
                    </p>
                    
                    <p style='margin-bottom:0;'>
                        Best regards,<br/>
                        <strong>BriskPeople System</strong>
                    </p>
                </td>
            </tr>
            <tr>
                <td align='center' style='padding:20px; background:#f4f6f8; font-size:12px; color:#777777;'>
                    © {DateTime.UtcNow.Year} BriskPeople. All rights reserved.<br/>
                    This is an automated notification. Please do not reply to this email.
                </td>
            </tr>
            </table>
            </td>
            </tr>
            </table>
        </body>
    </html>";

        return await SendEmailAsync(managerEmail, subject, body);
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
    public async Task<bool> SendVerificationEmailAsync(string email, string adminName, string verificationToken)
    {
        var verifyLink = $"{_configuration["EmployeePortal:PortalUrl"]}verify-email?token={verificationToken}";

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

    public async Task<bool> SendNewsEmailAsync(List<string> receiverEmails, NewsRequest request, Guid organizationId, Guid? newsid)
    {
        //Getting domain for the user 
        var domain = await _authRepository.GetDomainByOrganizationIdAsync(organizationId);
        if (receiverEmails == null || !receiverEmails.Any())
            return false; // no recipients

        var subject = $"News Update: {request.Title} | BriskPeople";
        // Fallback image if no image provided
        var newsImage = string.IsNullOrWhiteSpace(request.ImageUrl)
            ? "https://via.placeholder.com/650x300?text=BriskPeople+News"
            : request.ImageUrl;

        var cleanDomain = domain
    .Trim()
    .ToLower()
    .Replace("http://", "")
    .Replace("https://", "")
    .Replace(".com", "");

        var dashboardUrl = $"http://{cleanDomain}.briskpeople.com/news/view-news/{newsid}";
        // Use full description
        // Clean & limit description to first 25 words (remove HTML if present)
        var fullDescription = string.IsNullOrWhiteSpace(request.Description)
            ? ""
            : System.Text.RegularExpressions.Regex.Replace(request.Description, "<.*?>", ""); // remove HTML tags

        var words = fullDescription
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        var description = string.Join(" ", words.Take(25));

        if (words.Length > 25)
        {
            description += "...";
        }
        var body = $@"<!DOCTYPE html>
        <html>
        <head>
        <meta charset='UTF-8'>
        </head>
        <body style='margin:0; padding:0; background-color:#f4f6f8; font-family:Arial, Helvetica, sans-serif;'>
        <table width='100%' cellpadding='0' cellspacing='0' style='padding:30px 0;'>
        <tr>
        <td align='center'>
        <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff; border-radius:8px; overflow:hidden;'>
    <!-- Header -->
        <tr>
        <td align='center' style='padding:30px; background:#ffffff;'>
            <table cellspacing='0' cellpadding='0' border='0' style='margin:auto;'>
                <tr>
                    <td style='vertical-align:middle; padding-right:10px;'>
                        <img src='https://www.briskpeople.com/hub.png' 
                             alt='BriskPeople' 
                             style='height:40px; display:block;' />
                    </td>
                    <td style='vertical-align:middle; font-size:22px; font-weight:bold; color:#1f4fd8;'>
                        BriskPeople
                    </td>
                </tr>
            </table>
        </td>
        </tr>

    <!-- Content -->
        <tr>
        <td style='padding:30px; color:#333333; font-size:14px; line-height:1.7;'>

            <p style='margin-top:0;'>Hi,</p>

            <p>
                A new {request.Title} has been published on <strong>BriskPeople</strong>:
            </p>

            <h3 style='margin:15px 0; color:#1f4fd8;'>
                {request.Title}
            </h3>

            <p>
                {fullDescription}
            </p>

            <!-- Image (Only if exists) -->
            {(string.IsNullOrWhiteSpace(request.ImageUrl) ? "" : $@"
            <div style='text-align:center; margin:20px 0;'>
                <img src='{request.ImageUrl}' 
                     alt='News Image' 
                     style='max-width:350px; width:100%; height:auto; border-radius:8px; display:block; margin:0 auto;' />
            </div>
            ")}

            <!-- Button -->
            <p style='text-align:center; margin:25px 0;'>
                <a href='{dashboardUrl}'
                   style='background:#1f4fd8; 
                          color:#ffffff; 
                          padding:14px 28px; 
                          text-decoration:none; 
                          border-radius:6px; 
                          font-size:14px; 
                          font-weight:bold; 
                          display:inline-block;'>
                    Read More
                </a>
            </p>

            <p style='margin-bottom:0;'>
                Best regards,<br/>
                <strong>BriskPeople Team</strong>
            </p>

        </td>
        </tr>
    <!-- Footer -->
        <tr>
        <td align='center' style='padding:20px; background:#f4f6f8; font-size:12px; color:#777777;'>
            © {DateTime.UtcNow.Year} BriskPeople. All rights reserved.
        </td>
        </tr>
        </table>
        </td>
        </tr>
        </table>
        </body>
        </html>";
        // Send email to all recipients individually (or use BCC if preferred)
        foreach (var email in receiverEmails)
        {
            await SendEmailAsync(email, subject, body);
        }

        return true;
    }

    public async Task<bool> SendDemoEmailAsync(string toEmail, string fullName, string companyName, string fromEmail)
    {
        var subject = $"New Demo Inquiry from {fullName} | BriskPeople";
        var body = $@"<!DOCTYPE html>
            <html>
                <body style='margin:0;padding:0;background:#f4f6f8;font-family:Arial,Helvetica,sans-serif;'>
                    <table width='100%' cellpadding='0' cellspacing='0' style='padding:30px 0;'>
                        <tr>
                        <td align='center'>
                            <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:8px;overflow:hidden;'>
                                <tr>
                                <td style='background:#ffffff;padding:20px 30px;border-bottom:1px solid #e5e7eb;'>
                                    <table cellspacing='0' cellpadding='0' border='0' style='margin:auto;'>
                                        <tr>
                                        <td style='vertical-align:middle; padding-right:10px;'>
                                            <img src='https://www.briskpeople.com/hub.png' alt='BriskPeople' style='height:40px; display:block;' />
                                        </td>
                                        <td style='vertical-align:middle; font-size:22px; font-weight:bold; color:#1f4fd8;'>
                                            BriskPeople
                                        </td>
                                        </tr>
                                    </table>
                                </td>
                                </tr>
                                <tr>
                                <td style='padding:35px;color:#333;font-size:14px;line-height:1.7;'>
                                    <p style='margin-top:0;'>Hello <strong>Support Team</strong>,</p>
                                    <p>You have received a new <strong>Demo Request</strong> from a potential client on <strong>BriskPeople</strong>. The user's details are provided below:</p>
                                    <table width='100%' cellpadding='8' cellspacing='0' style='margin-top:15px;border:1px solid #e5e7eb;border-collapse:collapse;font-size:14px;'>
                                        <tr style='background:#f9fafb;'>
                                            <td style='border:1px solid #e5e7eb;'><strong>Name</strong></td>
                                            <td style='border:1px solid #e5e7eb;'>{fullName}</td>
                                        </tr>
                                        <tr>
                                            <td style='border:1px solid #e5e7eb;'><strong>Company</strong></td>
                                            <td style='border:1px solid #e5e7eb;'>{companyName}</td>
                                        </tr>
                                        <tr style='background:#f9fafb;'>
                                            <td style='border:1px solid #e5e7eb;'><strong>Email</strong></td>
                                            <td style='border:1px solid #e5e7eb;'>{fromEmail}</td>
                                        </tr>
                                    </table>
                                    <p style='margin-top:25px;'>
                                        Please review this request and follow up with the client to schedule a product demo at the earliest convenience.
                                    </p>
                                    <p style='margin-bottom:0;'>Best regards,<br/><strong>BriskPeople</strong></p>
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
        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendAssignShiftEmailAsync(List<string> receiverEmails, string message, Guid organizationId)
    {
        // Getting domain
        var domain = await _authRepository.GetDomainByOrganizationIdAsync(organizationId);

        if (receiverEmails == null || !receiverEmails.Any())
            return false;

        var subject = $"Shift Assigned | BriskPeople";

        var cleanDomain = domain
            .Trim()
            .ToLower()
            .Replace("http://", "")
            .Replace("https://", "")
            .Replace(".com", "");

        var dashboardUrl = $"http://{cleanDomain}.briskpeople.com/help-desk/ticket-invlovement";

        var body = $@"<!DOCTYPE html>
                  <html>
                 <head>
                <meta charset='UTF-8'>
                 </head>
               <body style='margin:0; padding:0; background-color:#f4f6f8; font-family:Arial, Helvetica, sans-serif;'>

             <table width='100%' cellpadding='0' cellspacing='0' style='padding:30px 0;'>     
                  <tr>
           <td align='center'>
         <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff; border-radius:8px; overflow:hidden;'>
      <!-- Header -->
           <tr>
         <td align='center' style='padding:30px; background:#ffffff;'>
            <table cellspacing='0' cellpadding='0' border='0' style='margin:auto;'>
         <tr>
            <td style='vertical-align:middle; padding-right:10px;'>
        <img src='https://www.briskpeople.com/hub.png' 
           alt='BriskPeople' 
        style='height:40px; display:block;' />
        </td>
        <td style='vertical-align:middle; font-size:22px; font-weight:bold; color:#1f4fd8;'>
      BriskPeople
          </td>
            </tr>
            </table>
            </td>
             </tr>
          <!-- Content -->
              <tr>
             <td style='padding:30px; color:#333333; font-size:14px; line-height:1.7;'>
       <p style='margin-top:0;'>Hi,</p>
        <p>
           A new shift has been assigned to you on <strong>BriskPeople</strong>. 
         </p>
           <h3 style='margin:15px 0; color:#1f4fd8;'>
        Shift Assignment
           </h3>
          <p>
        {message}
        </p>
        <!-- Button -->
         <p style='text-align:center; margin:25px 0;'>
       <a href='{dashboardUrl}'
   style='background:#1f4fd8;
          color:#ffffff;
          padding:14px 28px;
          text-decoration:none;
          border-radius:6px;
          font-size:14px;
          font-weight:bold;
          display:inline-block;'>
     View Shift
       </a>
          </p>
        <p>
         You can view your assigned shifts by clicking the button above.
       </p>
           <p style='margin-bottom:0;'>
       Best regards,<br/>
       <strong>BriskPeople Team</strong>
       </p>
        </td>
          </tr>
<!-- Footer -->
            <tr>
           <td align='center' style='padding:20px; background:#f4f6f8; font-size:12px; color:#777777;'>
       © {DateTime.UtcNow.Year} BriskPeople. All rights reserved.
       </td>
         </tr>
      </table>
         </td>
        </tr>
        </table>
         </body>
          </html>";

        foreach (var email in receiverEmails)
        {
            await SendEmailAsync(email, subject, body);
        }

        return true;
    }

    public async Task<bool> SendHelpDeskChatAsync(List<string> receiverEmails,TicketMessageRequest request,Guid organizationId, string ticketTitle)
    {
        if (receiverEmails == null || !receiverEmails.Any())
            return false; // no recipients

        // Get domain for the organization
        var domain = await _authRepository.GetDomainByOrganizationIdAsync(organizationId);
        var cleanDomain = domain.Trim().ToLower()
            .Replace("http://", "")
            .Replace("https://", "")
            .Replace(".com", "");

        var ticketUrl = $"http://{cleanDomain}.briskpeople.com/help-desk/tickets/view/{request.TicketId}";
        var subject = $"New HelpDesk Message for Ticket: {ticketTitle}";

        // Clean message content (remove HTML if any)
        var messageContent = string.IsNullOrWhiteSpace(request.Message)
            ? ""
            : System.Text.RegularExpressions.Regex.Replace(request.Message, "<.*?>", "");

        var body = $@"<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
</head>
<body style='margin:0; padding:0; background-color:#f4f6f8; font-family:Arial, Helvetica, sans-serif;'>
<table width='100%' cellpadding='0' cellspacing='0' style='padding:30px 0;'>
<tr>
<td align='center'>
<table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff; border-radius:8px; overflow:hidden;'>

<tr>
<td align='center' style='padding:30px; background:#ffffff;'>
    <table cellspacing='0' cellpadding='0' border='0' style='margin:auto;'>
        <tr>
            <td style='vertical-align:middle; padding-right:10px;'>
                <img src='https://www.briskpeople.com/hub.png' 
                     alt='BriskPeople' 
                     style='height:40px; display:block;' />
            </td>
            <td style='vertical-align:middle; font-size:22px; font-weight:bold; color:#1f4fd8;'>
                BriskPeople
            </td>
        </tr>
    </table>
</td>
</tr>

<tr>
<td style='padding:30px; color:#333333; font-size:14px; line-height:1.7;'>
    <p style='margin-top:0;'>Hi,</p>

    <p>
        You have a new message on HelpDesk ticket <strong>{ticketTitle}</strong>:
    </p>

    <p style='padding:10px; background:#f1f3f5; border-left:4px solid #1f4fd8;'>
        {messageContent}
    </p>

    <p style='text-align:center; margin:25px 0;'>
        <a href='{ticketUrl}'
           style='background:#1f4fd8; 
                  color:#ffffff; 
                  padding:14px 28px; 
                  text-decoration:none; 
                  border-radius:6px; 
                  font-size:14px; 
                  font-weight:bold; 
                  display:inline-block;'>
            View Ticket
        </a>
    </p>

    <p style='margin-bottom:0;'>
        Best regards,<br/>
        <strong>BriskPeople Team</strong>
    </p>
</td>
</tr>

<tr>
<td align='center' style='padding:20px; background:#f4f6f8; font-size:12px; color:#777777;'>
    © {DateTime.UtcNow.Year} BriskPeople. All rights reserved.
</td>
</tr>

</table>
</td>
</tr>
</table>
</body>
</html>";

        // Send email to all recipients individually
        foreach (var email in receiverEmails)
        {
            await SendEmailAsync(email, subject, body);
        }

        return true;
    }

    public async Task<bool> SendPayslipEmailAsync(
        string toEmail,
        string employeeName,
        string periodName,
        string payslipDownloadUrl,
        Guid organizationId,
        string? emailSubject = null,
        string? emailMessage = null)
    {
        var domain = await _authRepository.GetDomainByOrganizationIdAsync(organizationId);
        var cleanDomain = domain.Trim().ToLower()
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace(".com", "", StringComparison.OrdinalIgnoreCase);
        var portalLink = $"https://{cleanDomain}.briskpeople.com";
        var safeEmployeeName = WebUtility.HtmlEncode(employeeName);
        var safePeriodName = WebUtility.HtmlEncode(periodName);
        var subject = string.IsNullOrWhiteSpace(emailSubject)
            ? $"Your Payslip — {periodName} | BriskPeople"
            : emailSubject.Trim();

        var messageHtml = string.IsNullOrWhiteSpace(emailMessage)
            ? $"<p>Your payslip for <strong>{safePeriodName}</strong> is ready.</p>"
            : $"<div style='margin:0 0 1rem;'>{FormatPayslipEmailMessageHtml(emailMessage)}</div>";

        var body = $@"<!DOCTYPE html>
<html>
<head><meta charset='UTF-8'></head>
<body style='margin:0;padding:0;background:#f4f6f8;font-family:Arial,Helvetica,sans-serif;'>
<table width='100%' cellpadding='0' cellspacing='0' style='padding:30px 0;'>
<tr><td align='center'>
<table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:8px;overflow:hidden;'>
<tr>
<td align='center' style='padding:30px;background:#ffffff;'>
    <table cellspacing='0' cellpadding='0' border='0' style='margin:auto;'>
    <tr>
        <td style='vertical-align:middle;padding-right:10px;'>
            <img src='https://www.briskpeople.com/hub.png' alt='BriskPeople' style='height:40px;display:block;' />
        </td>
        <td style='vertical-align:middle;font-size:22px;font-weight:bold;color:#1f4fd8;'>BriskPeople</td>
    </tr>
    </table>
</td>
</tr>
<tr>
<td style='padding:30px;color:#333;font-size:14px;line-height:1.7;'>
    <p style='margin-top:0;'>Hi <strong>{safeEmployeeName}</strong>,</p>
    {messageHtml}
    <table width='100%' cellpadding='0' cellspacing='0' style='margin:30px 0;'>
    <tr>
        <td align='center'>
            <a href='{payslipDownloadUrl}'
               style='background:#1f4fd8;color:#fff;padding:14px 28px;text-decoration:none;border-radius:6px;font-size:14px;font-weight:bold;display:inline-block;'>
                Download Payslip (PDF)
            </a>
        </td>
    </tr>
    </table>
    <p>You can also sign in to the employee portal: <a href='{portalLink}'>{portalLink}</a></p>
    <p style='margin-bottom:0;'>Best regards,<br/><strong>BriskPeople Team</strong></p>
</td>
</tr>
<tr>
<td align='center' style='padding:20px;background:#f4f6f8;font-size:12px;color:#777;'>
© {DateTime.UtcNow.Year} BriskPeople. All rights reserved.
</td>
</tr>
</table>
</td></tr>
</table>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    private static string FormatPayslipEmailMessageHtml(string message)
    {
        return WebUtility.HtmlEncode(message.Trim())
            .Replace("\r\n", "<br/>", StringComparison.Ordinal)
            .Replace("\n", "<br/>", StringComparison.Ordinal);
    }
}

