using HRMS.AI.Models;
using HRMS.AI.Models.Enums;
using HRMS.AI.Services.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.AI.Services
{
    public class ChatService : IChatService
    {
        private readonly IIntentRecognitionService _intentService;
        private readonly IGuidanceAIService _guidance_service;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            IIntentRecognitionService intentService,
            IGuidanceAIService guidanceService,
            ILogger<ChatService> logger)
        {
            _intentService = intentService;
            _guidance_service = guidanceService;
            _logger = logger;
        }

        public async Task<ChatResponseDto> HandleChatAsync(ChatRequestDto request, string? roleName, string domain)
        {
            var intentResult = await _intentService.DetectIntentAsync(request.Message);

             if (intentResult.Intent == ChatIntent.Guidance || intentResult.Intent == ChatIntent.Greeting)
             {
                var guidance = await _guidance_service.GenerateGuidanceAsync(request.Message, intentResult.Module,roleName,domain);
                     return new ChatResponseDto { Intent = intentResult.Intent, ResponseText = guidance, Module = intentResult.Module };
             }

            // For all non-Guidance intents, return the detected intent name (and module if any) to the controller.
            return new ChatResponseDto
            {
                Intent = intentResult.Intent,
                ResponseText = intentResult.Intent.ToString(),
                Module = intentResult.Module
            };
        }
    }

}
