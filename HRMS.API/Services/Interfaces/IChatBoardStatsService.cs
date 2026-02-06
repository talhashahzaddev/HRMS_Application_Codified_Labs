using HRMS.AI.Models;
using HRMS.AI.Models.Enums;
using System;
using System.Threading.Tasks;

namespace HRMS.API.Services.Interfaces
{
    public interface IChatBoardStatsService
    {
        Task<ChatResponseDto> HandleNonGuidanceIntentAsync(ChatIntent intent, string module, Guid employeeId,Guid currentUserId, Guid organizationId, string ipAddress);
    }
}
