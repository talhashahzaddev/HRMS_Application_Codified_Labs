using HRMS.AI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.AI.Services.Interface
{
    public interface IChatService
    {
        Task<ChatResponseDto> HandleChatAsync(ChatRequestDto request);
    }
}
