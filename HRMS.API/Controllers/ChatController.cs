using HRMS.AI.Models;
using HRMS.AI.Models.Enums;
using HRMS.AI.Services.Interface;
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
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService,ILeaveRepository leaveRepository, IChatBoardStatsService chatBoardStatsService,IGuidanceAIService guidanceAIService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _leaveRepository = leaveRepository;
            _chatBoardStatsService = chatBoardStatsService;
            _guidance_service = guidanceAIService;
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
                var response = await _chatService.HandleChatAsync(request,roleName);

                // If intent is neither Guidance nor Greeting, delegate to ChatBoardStatsService for further handling
                if (response.Intent != ChatIntent.Guidance && response.Intent != ChatIntent.Greeting)
                {
                    response = await _chatBoardStatsService.HandleNonGuidanceIntentAsync(response.Intent, response.Module, empid, currentUserId, organizationId);
                }

                if (response.Intent == ChatIntent.EmployeeInfo)
                {
                    response = await _guidance_service.GenrateEmployeeInfoGuidance(request.Message, response.ResponseText);
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
    }

}
