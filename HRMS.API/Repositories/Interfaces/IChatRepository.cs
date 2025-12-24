using HRMS.API.Models.DTOs;
using HRMS.API.Models.Chatbot;

namespace HRMS.API.Repositories.Interfaces
{
    public interface IChatRepository
    {
        Task<List<LeaveBalanceDTO>> GetLeaveBalanceAsync(Guid employeeId);
        Task<ShiftStatusDto?> GetAttendanceStatusAsync(Guid employeeId);
        //Task<ManagerDto?> GetManagerInfoAsync(Guid employeeId);
        Task<PayslipDto?> GetLatestPayslipAsync(Guid employeeId);
    }

}
