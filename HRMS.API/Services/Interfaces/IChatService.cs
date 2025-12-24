using HRMS.API.Services;

namespace HRMS.API.Services.Interfaces
{
    public interface IChatService
    {
            Task<string> ProcessUserMessageAsync(string userText, Guid employeeId);

    }
}
