using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces
{
    public interface IChatBoardAiService
    {
        Task<bool> SaveChatAsync(AiChatRequestDto request);
    }
}
