using HRMS.AI.Models;
using HRMS.AI.Models.Enums;
using HRMS.AI.Services.Interface;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILeaveRepository _leaveRepository;
        private readonly IChatBoardStatsService _chatBoardStatsService;
        private readonly IGuidanceAIService _guidance_service;
        private readonly ILeaveService _leaveService;
        private readonly IChatBoardAiService _chatBoardAiService;
        private readonly IAuthRepository _authRepository;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService,ILeaveRepository leaveRepository, IChatBoardStatsService chatBoardStatsService,IGuidanceAIService guidanceAIService,ILeaveService leaveService,IChatBoardAiService chatBoardAiService,IAuthRepository authRepository, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _leaveRepository = leaveRepository;
            _chatBoardStatsService = chatBoardStatsService;
            _guidance_service = guidanceAIService;
            _leaveService = leaveService;
            _chatBoardAiService = chatBoardAiService;
            _authRepository = authRepository;
            _logger = logger;
        }

        [HttpPost("message")]
        public async Task<ActionResult<ChatResponseDto>> SendMessage([FromBody] ChatRequestDto request)
        {
            try
            {
                var currentUserId = GetCurrentEmployeeId();
                var organizationId = GetCurrentOrganizationId();
                Guid empid = await _leaveRepository.GetEmpId(currentUserId);
                string? roleName = await _leaveRepository.GetUserRoleNameAsync(currentUserId);
                string? domain = await _authRepository.GetDomainByOrganizationIdAsync(organizationId);
                var response = await _chatService.HandleChatAsync(request,roleName, domain);
                var ipAddress = GetClientIp(); // add this method in ChatController
                var tenant = !string.IsNullOrWhiteSpace(domain) ? domain.Trim().Split('.')[0] : "defaultdomain";
                var baseUrl = $"https://{tenant}.briskpeople.com";
                var leaveSubmittedFromApplyIntent = false;

                // Fail-safe: force deterministic intent routing when model drifts to Guidance/Unknown.
                var forcedIntent = DetectIntentOverride(request.Message);
                if (forcedIntent.HasValue && (response.Intent == ChatIntent.Guidance || response.Intent == ChatIntent.Unknown))
                {
                    response.Intent = forcedIntent.Value.intent;
                    response.Module = forcedIntent.Value.module;
                }


                // If intent is neither Guidance nor Greeting, delegate to ChatBoardStatsService for further handling
                if (response.Intent != ChatIntent.Guidance && response.Intent != ChatIntent.Greeting && response.Intent != ChatIntent.LeaveApplicationDetails)
                {
                    response = await _chatBoardStatsService.HandleNonGuidanceIntentAsync(
                        response.Intent,
                        response.Module,
                        empid,
                        currentUserId,
                        organizationId,
                        ipAddress,
                        request.Message,
                        baseUrl);
                }

                if (response.Intent == ChatIntent.EmployeeInfo)
                {
                    response = await _guidance_service.GenrateEmployeeInfoGuidance(request.Message, response.ResponseText);
                }
                if (response.Intent == ChatIntent.ApplyForLeave)
                {
                    var leaveTypes = await _leaveRepository.GetLeaveTypesAsync(organizationId);
                    string leaveTypeNames = string.Join(", ", leaveTypes.Select(x => x.TypeName));

                    var parsed = await _guidance_service.GenrateLeaveApplicationDetails(request.Message, leaveTypeNames);
                    if (parsed != null
                        && !string.IsNullOrWhiteSpace(parsed.leaveType)
                        && parsed.startDate != DateTime.MinValue
                        && parsed.endDate != DateTime.MinValue)
                    {
                        var leaveResponse = await _leaveService.CreateLeaveRequestByChatBotAsync(parsed, currentUserId, organizationId, baseUrl);
                        response.Intent = ChatIntent.LeaveApplicationDetails;
                        response.Module = "Leave";
                        response.ResponseText = leaveResponse.statustext;
                        leaveSubmittedFromApplyIntent = true;
                    }
                }
                if(response.Intent == ChatIntent.LeaveApplicationDetails && !leaveSubmittedFromApplyIntent)
                {
                    var leavetypes = await _leaveRepository.GetLeaveTypesAsync(organizationId);
                    string leaveTypeNames = string.Join(
                        ", ",
                        leavetypes.Select(x => x.TypeName)
                    );
                    var leavetypedetails =  await _guidance_service.GenrateLeaveApplicationDetails(request.Message, leaveTypeNames);
                    if(leavetypedetails != null)
                    {
                        var leaveResponse = await _leaveService.CreateLeaveRequestByChatBotAsync(leavetypedetails, currentUserId, organizationId, baseUrl);
                        response.ResponseText = leaveResponse.statustext;
                    }
                }
                if (response != null)
                {
                    var AiChatRequest = new AiChatRequestDto
                    {
                        employeeid = empid,
                        organizationid = organizationId,
                        rolename = roleName!,
                        usermessage = request.Message,
                        intent = response.Intent.ToString(),
                        airesponse = response.ResponseText,
                        modulename = response.Module,
                        createdat = DateTime.UtcNow
                    };
                    await _chatBoardAiService.SaveChatAsync(AiChatRequest);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat processing failed for employee");
                return StatusCode(500, new ChatResponseDto { ResponseText = "An error occurred while processing your message." });
            }
        }
        private Guid GetCurrentEmployeeId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogError("User ID claim not found in JWT token");
                throw new UnauthorizedAccessException("User ID not found in token");
            }

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogError("Invalid user ID format in JWT token: {UserId}", userIdClaim);
                throw new UnauthorizedAccessException("Invalid user ID format");
            }

            return userId;
        }
        private Guid GetCurrentOrganizationId()
        {
            var orgIdClaim = User.FindFirstValue("organizationId");

            if (string.IsNullOrEmpty(orgIdClaim))
            {
                _logger.LogError("Organization ID claim not found in JWT token");
                throw new UnauthorizedAccessException("Organization ID not found in token");
            }

            if (!Guid.TryParse(orgIdClaim, out var organizationId))
            {
                _logger.LogError("Invalid organization ID format in JWT token: {OrganizationId}", orgIdClaim);
                throw new UnauthorizedAccessException("Invalid organization ID format");
            }

            return organizationId;
        }
        private string GetClientIp()
        {
            var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0];
            }
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "UNKNOWN";
        }

        private static (ChatIntent intent, string module)? DetectIntentOverride(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            var text = message.Trim().ToLowerInvariant();

            if (text.Contains("payslip") || text.Contains("salary slip")
                || text.Contains("basic salary") || text.Contains("my salary")
                || text.Contains("net salary") || text.Contains("last payroll")
                || text.Contains("payroll history") || text.Contains("salary deduction")
                || (text.Contains("payroll") && (text.Contains("history") || text.Contains("deduction") || text.Contains("breakdown"))))
                return (ChatIntent.GetPayslip, "Payroll");

            if (text.Contains("expense") && (text.Contains("status") || text.Contains("claim") || text.Contains("reimb")))
                return (ChatIntent.GetExpenseStatus, "ExpensesManagement");

            if ((text.Contains("latest") && text.Contains("news")) || text.Contains("company news"))
                return (ChatIntent.GetNewsUpdates, "NewsManagement");

            if ((text.Contains("asset") || text.Contains("assets")) && (text.Contains("my") || text.Contains("assigned")))
                return (ChatIntent.GetMyAssets, "AssetsManagement");

            if (text.Contains("subscription") && text.Contains("status"))
                return (ChatIntent.GetSubscriptionStatus, "Subscription");

            if (text.Contains("timesheet") && (text.Contains("status") || text.Contains("finalized") || text.Contains("finalised")))
                return (ChatIntent.GetTimesheetStatus, "Attendance");

            if ((text.Contains("upcoming") || text.Contains("summary")) && text.Contains("holiday"))
                return (ChatIntent.GetHolidaySummary, "HolidayManagement");

            if (text.Contains("restricted") && text.Contains("holiday") && (text.Contains("how") || text.Contains("select")))
                return (ChatIntent.Guidance, "HolidayManagement");

            // ---- Expense intents (recurring FIRST to avoid false claims match) ----

            // Recurring expenses — must check before claims
            if (text.Contains("recurring expense") || text.Contains("recurring claim")
                || (text.Contains("recurring") && (text.Contains("my") || text.Contains("show") || text.Contains("list") || text.Contains("expense"))))
                return (ChatIntent.GetMyRecurringExpenses, "ExpensesManagement");

            // My expense claims list — must NOT contain 'recurring'
            if (!text.Contains("recurring")
                && ((text.Contains("my expense") && (text.Contains("claim") || text.Contains("show") || text.Contains("list") || text.Contains("recent")))
                    || text.Contains("show my claim") || text.Contains("my claims") || text.Contains("list my expense")))
                return (ChatIntent.GetMyExpenseClaims, "ExpensesManagement");

            // General expense status fallback
            if (!text.Contains("recurring")
                && text.Contains("expense") && (text.Contains("status") || text.Contains("reimb")))
                return (ChatIntent.GetExpenseStatus, "ExpensesManagement");

            // ATS score / knockout / stage detail
            if (text.Contains("ats score") || text.Contains("match score")
                || text.Contains("knockout")
                || (text.Contains("application") && text.Contains("stage") && (text.Contains("my") || text.Contains("what") || text.Contains("current")))
                || (text.Contains("application") && text.Contains("detail") && text.Contains("my")))
                return (ChatIntent.GetJobApplicationDetail, "JobsManagement");

            // Referred applications
            if (text.Contains("my refer") || text.Contains("i referred") || text.Contains("my referral")
                || (text.Contains("refer") && (text.Contains("application") || text.Contains("candidate") || text.Contains("status"))))
                return (ChatIntent.GetMyReferredApplications, "JobsManagement");

            // Self-applied applications
            if ((text.Contains("my job application") || text.Contains("my self application") || text.Contains("jobs i applied"))
                || (text.Contains("my recent job") && (text.Contains("application") || text.Contains("applied")))
                || (text.Contains("show me my") && text.Contains("application") && !text.Contains("refer")))
                return (ChatIntent.GetMyJobApplications, "JobsManagement");

            // General jobs summary fallback
            if ((text.Contains("job application") || text.Contains("job")) && text.Contains("status"))
                return (ChatIntent.GetJobsStatus, "JobsManagement");

            return null;
        }

    }

}
