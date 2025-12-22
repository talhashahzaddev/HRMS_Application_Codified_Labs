using HRMS.API.Services.Interfaces;
using HRMS.AI.Models;
using HRMS.AI.Models.Enums;
using HRMS.API.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HRMS.API.Services
{
    public class ChatBoardStatsService: IChatBoardStatsService
    {
        private readonly ILeaveRepository _leaveRepository;
        private readonly ILogger<ChatBoardStatsService> _logger;

        public ChatBoardStatsService(ILeaveRepository leaveRepository, ILogger<ChatBoardStatsService> logger)
        {
            _leaveRepository = leaveRepository;
            _logger = logger;
        }

        public async Task<ChatResponseDto> HandleNonGuidanceIntentAsync(ChatIntent intent, string module, Guid employeeId, Guid organizationId)
        {
            switch (intent)
            {
                case ChatIntent.GetPerformanceSummary:
                    return await HandleGetPerformanceSummary(employeeId, organizationId);
                case ChatIntent.GetAttendance:
                    return await HandleGetAttendance(employeeId, organizationId);
                case ChatIntent.CheckMyShift:
                    return await HandleCheckMyShift(employeeId, organizationId);
                case ChatIntent.GetLeaveBalance:
                    return await HandleGetLeaveBalance(employeeId, organizationId);
                case ChatIntent.ApplyForLeave:
                    return await HandleApplyForLeave(employeeId, organizationId);
                case ChatIntent.GetPayslip:
                    return await HandleGetPayslip(employeeId, organizationId);
                case ChatIntent.Unknown:
                    _logger.LogWarning("Received Unknown intent for employee {EmployeeId}", employeeId);
                    return new ChatResponseDto
                    {
                        Intent = ChatIntent.Unknown,
                        ResponseText = "I am an HR assistant and can only help with HR-related queries.\r\nPlease ask questions related to attendance, leave management,payroll, performance, or HR policies.",
                        Module = module
                    };
                default:
                    // Return the intent name for any other/unhandled intents
                    return new ChatResponseDto
                    {
                        Intent = intent,
                        ResponseText = intent.ToString(),
                        Module = module
                    };
            }
        }

        private Task<ChatResponseDto> HandleGetPerformanceSummary(Guid employeeId, Guid organizationId)
        {
            // Placeholder: fetch performance summary from repository/service
            _logger.LogInformation("Fetching performance summary for employee {EmployeeId}", employeeId);
            return Task.FromResult(new ChatResponseDto { Intent = ChatIntent.GetPerformanceSummary, ResponseText = "GetPerformanceSummary", Module = "Performance" });
        }

        private Task<ChatResponseDto> HandleGetAttendance(Guid employeeId, Guid organizationId)
        {
            _logger.LogInformation("Fetching attendance for employee {EmployeeId}", employeeId);
            return Task.FromResult(new ChatResponseDto { Intent = ChatIntent.GetAttendance, ResponseText = "GetAttendance", Module = "Attendance" });
        }

        private Task<ChatResponseDto> HandleCheckMyShift(Guid employeeId, Guid organizationId)
        {
            _logger.LogInformation("Fetching shift info for employee {EmployeeId}", employeeId);
            return Task.FromResult(new ChatResponseDto { Intent = ChatIntent.CheckMyShift, ResponseText = "CheckMyShift", Module = "ShiftManagement" });
        }

        private async Task<ChatResponseDto> HandleGetLeaveBalance(Guid employeeId, Guid organizationId)
        {
            _logger.LogInformation("Fetching leave balance for employee {EmployeeId}", employeeId);
            try
            {
                var empId = await _leaveRepository.GetEmpId(employeeId);
                // Placeholder - real implementation should fetch leave balance by empId/orgId
                return new ChatResponseDto { Intent = ChatIntent.GetLeaveBalance, ResponseText = "GetLeaveBalance", Module = "Leave" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch leave balance for employee {EmployeeId}", employeeId);
                return new ChatResponseDto { Intent = ChatIntent.GetLeaveBalance, ResponseText = "Error fetching leave balance." };
            }
        }

        private Task<ChatResponseDto> HandleApplyForLeave(Guid employeeId, Guid organizationId)
        {
            _logger.LogInformation("Processing apply for leave request for employee {EmployeeId}", employeeId);
            return Task.FromResult(new ChatResponseDto { Intent = ChatIntent.ApplyForLeave, ResponseText = "ApplyForLeave", Module = "Leave" });
        }

        private Task<ChatResponseDto> HandleGetPayslip(Guid employeeId, Guid organizationId)
        {
            _logger.LogInformation("Fetching payslip for employee {EmployeeId}", employeeId);
            return Task.FromResult(new ChatResponseDto { Intent = ChatIntent.GetPayslip, ResponseText = "GetPayslip", Module = "Payroll" });
        }
    }
}
