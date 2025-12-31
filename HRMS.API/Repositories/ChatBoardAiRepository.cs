using HRMS.AI.Models;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories
{
    public class ChatBoardAiRepository : BaseRepository, IChatBoardAiRepository
    {
        public ChatBoardAiRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }


        public async Task<bool> SaveChatAsync(AiChatRequestDto request)
        {
            try
            {
                var createdAt = DateTime.UtcNow; // IMPORTANT for timestamptz

                var sql = @"
        INSERT INTO chatboard.chat_board_logs
        (
            employeeid,
            organizationid,
            rolename,
            usermessage,
            intent,
            airesponse,
            modulename,
            createdat
        )
        VALUES
        (
            @EmployeeId,
            @OrganizationId,
            @RoleName,
            @UserMessage,
            @Intent,
            @AIResponse,
            @modulename,
            @CreatedAt
        );";

                var affected = await ExecuteAsync(sql, new
                {
                    request.employeeid,
                    request.organizationid,
                    request.rolename,
                    request.usermessage,
                    request.intent,
                    request.airesponse,
                    request.modulename,
                    CreatedAt = createdAt
                });

                return affected > 0;
            }
            catch(Exception ex)
            {
                return false;
            }
        }
    }
}
