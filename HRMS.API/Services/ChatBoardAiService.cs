using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using HRMS.API.Models.DTOs;

namespace HRMS.API.Services
{
    public class ChatBoardAiService : IChatBoardAiService
    {
        private readonly IChatBoardAiRepository _chatBoardAiRepository;
        public ChatBoardAiService(IChatBoardAiRepository chatBoardAiRepository)
        {
            _chatBoardAiRepository = chatBoardAiRepository;

        }

        public async Task<bool> SaveChatAsync(AiChatRequestDto request)
        {
            return await  _chatBoardAiRepository.SaveChatAsync(request);
        }
    }
}
