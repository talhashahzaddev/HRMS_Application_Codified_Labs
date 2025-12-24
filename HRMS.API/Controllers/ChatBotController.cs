using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.API.Services.Interfaces;
using System.Security.Claims;

namespace HRMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("ask")]
        [Authorize]
        public async Task<IActionResult> AskChatbot([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserText))
                return BadRequest("Please type a message.");

            try
            {
                // 1. Get the Login User's ID (Not necessarily EmployeeId yet)
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(userIdClaim, out Guid userId))
                    return Unauthorized("User ID not found in token.");

                // 2. Pass UserId to Service
                string response = await _chatService.ProcessUserMessageAsync(request.UserText, userId);

                return Ok(new { response = response, intent = "Detected" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }

    public class ChatRequest
    {
        public string UserText { get; set; }
    }
}