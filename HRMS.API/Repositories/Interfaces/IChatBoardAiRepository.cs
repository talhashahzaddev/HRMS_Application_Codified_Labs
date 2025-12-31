using HRMS.AI.Models;
using HRMS.API.Models;
using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces
{
    public interface IChatBoardAiRepository
    {
        Task<bool> SaveChatAsync(AiChatRequestDto request);
    }
}
