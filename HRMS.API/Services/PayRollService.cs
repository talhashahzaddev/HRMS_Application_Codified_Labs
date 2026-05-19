using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace HRMS.API.Services;

public class PayRollService : IPayRollService
{
    private readonly IPayRollRepository _payRollRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<PayRollService> _logger;
    private readonly IWebHostEnvironment _hostEnvironment;

    public PayRollService(
        IPayRollRepository payRollRepository,
        IAttendanceRepository attendanceRepository,
        ISettingsRepository settingsRepository,
        ILogger<PayRollService> logger,
        IWebHostEnvironment hostEnvironment)
    {
        _payRollRepository = payRollRepository;
        _attendanceRepository = attendanceRepository;
        _settingsRepository = settingsRepository;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<ServiceResponse<PayRollRulesGroupedDto>> GetAllRulesAsync(Guid organizationId)
    {
        try
        {
            _logger.LogInformation("Getting all payroll rules for organization: {OrganizationId}", organizationId);

            if (organizationId == Guid.Empty)
            {
                return new ServiceResponse<PayRollRulesGroupedDto>
                {
                    Success = false,
                    Message = "Organization ID is required"
                };
            }

            var data = await _payRollRepository.GetAllRulesAsync(organizationId);

            return new ServiceResponse<PayRollRulesGroupedDto>
            {
                Success = true,
                Data = data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll rules for organization: {OrganizationId}", organizationId);
            return new ServiceResponse<PayRollRulesGroupedDto>
            {
                Success = false,
                Message = "An error occurred while retrieving payroll rules"
            };
        }
    }

    public async Task<ServiceResponse<IEnumerable<OvertimeRuleDto>>> GetAllOvertimeRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAllOvertimeRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<OvertimeRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overtime rules");
            return new ServiceResponse<IEnumerable<OvertimeRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<OvertimeRuleDto>>> GetActiveOvertimeRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActiveOvertimeRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<OvertimeRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active overtime rules");
            return new ServiceResponse<IEnumerable<OvertimeRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<OvertimeRuleDto>> GetOvertimeRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetOvertimeRuleByIdAsync(ruleId, organizationId);
            if (result == null)
            {
                return new ServiceResponse<OvertimeRuleDto> { Success = false, Message = "Overtime rule not found." };
            }
            return new ServiceResponse<OvertimeRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overtime rule");
            return new ServiceResponse<OvertimeRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<OvertimeRuleDto>> CreateOvertimeRuleAsync(Guid organizationId, CreateOvertimeRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateOvertimeRuleAsync(organizationId, dto);
            return new ServiceResponse<OvertimeRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating overtime rule");
            return new ServiceResponse<OvertimeRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<OvertimeRuleDto>> UpdateOvertimeRuleAsync(Guid ruleId, Guid organizationId, UpdateOvertimeRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateOvertimeRuleAsync(ruleId, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<OvertimeRuleDto> { Success = false, Message = "Overtime rule not found." };
            }
            return new ServiceResponse<OvertimeRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating overtime rule");
            return new ServiceResponse<OvertimeRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> ToggleOvertimeRuleStatusAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.ToggleOvertimeRuleStatusAsync(ruleId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Status toggled successfully." : "Rule not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling overtime rule status");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> ToggleOvertimeRuleDeletionAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.ToggleOvertimeRuleDeletionAsync(ruleId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deletion status toggled successfully." : "Rule not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling overtime rule deletion");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<OvertimeEntryDto>>> GetAllOvertimeEntriesAsync(Guid organizationId, OvertimeEntryFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllOvertimeEntriesAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<OvertimeEntryDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overtime entries");
            return new ServiceResponse<PagedResult<OvertimeEntryDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<OvertimeEntryDto>> GetOvertimeEntryByIdAsync(Guid overtimeId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetOvertimeEntryByIdAsync(overtimeId, organizationId);
            if (result == null)
            {
                return new ServiceResponse<OvertimeEntryDto> { Success = false, Message = "Overtime entry not found." };
            }
            return new ServiceResponse<OvertimeEntryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overtime entry");
            return new ServiceResponse<OvertimeEntryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<OvertimeEntryDto>> CreateOvertimeEntryAsync(Guid organizationId, CreateOvertimeEntryDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateOvertimeEntryAsync(organizationId, dto);
            return new ServiceResponse<OvertimeEntryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating overtime entry");
            return new ServiceResponse<OvertimeEntryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<OvertimeEntryDto>> UpdateOvertimeEntryAsync(Guid overtimeId, Guid organizationId, UpdateOvertimeEntryDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateOvertimeEntryAsync(overtimeId, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<OvertimeEntryDto> { Success = false, Message = "Overtime entry not found." };
            }
            return new ServiceResponse<OvertimeEntryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating overtime entry");
            return new ServiceResponse<OvertimeEntryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteOvertimeEntryAsync(Guid overtimeId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteOvertimeEntryAsync(overtimeId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Entry not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting overtime entry");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Payroll Periods
    public async Task<ServiceResponse<PayrollPeriodPagedResult>> GetAllPayrollPeriodsAsync(Guid organizationId, PayrollPeriodFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllPayrollPeriodsAsync(organizationId, filter);
            return new ServiceResponse<PayrollPeriodPagedResult> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll periods");
            return new ServiceResponse<PayrollPeriodPagedResult> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PayrollPeriodDto>> GetPayrollPeriodByIdAsync(Guid periodId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetPayrollPeriodByIdAsync(periodId, organizationId);
            if (result == null)
            {
                return new ServiceResponse<PayrollPeriodDto> { Success = false, Message = "Payroll period not found." };
            }
            return new ServiceResponse<PayrollPeriodDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll period by ID");
            return new ServiceResponse<PayrollPeriodDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PayrollPeriodDto>> CreatePayrollPeriodAsync(Guid organizationId, Guid employeeId, CreatePayrollPeriodDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreatePayrollPeriodAsync(organizationId, employeeId, dto);
            return new ServiceResponse<PayrollPeriodDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payroll period");
            return new ServiceResponse<PayrollPeriodDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PayrollPeriodDto>> UpdatePayrollPeriodAsync(Guid periodId, Guid organizationId, UpdatePayrollPeriodDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdatePayrollPeriodAsync(periodId, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<PayrollPeriodDto> { Success = false, Message = "Payroll period not found." };
            }
            return new ServiceResponse<PayrollPeriodDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payroll period");
            return new ServiceResponse<PayrollPeriodDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeletePayrollPeriodAsync(Guid periodId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeletePayrollPeriodAsync(periodId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Period not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payroll period");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Attendance Deduction Rules
    public async Task<ServiceResponse<IEnumerable<AttendanceDeductionRuleDto>>> GetAllAttendanceDeductionRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAllAttendanceDeductionRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<AttendanceDeductionRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance deduction rules");
            return new ServiceResponse<IEnumerable<AttendanceDeductionRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<AttendanceDeductionRuleDto>>> GetActiveAttendanceDeductionRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActiveAttendanceDeductionRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<AttendanceDeductionRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active attendance deduction rules");
            return new ServiceResponse<IEnumerable<AttendanceDeductionRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<AttendanceDeductionRuleDto>> GetAttendanceDeductionRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAttendanceDeductionRuleByIdAsync(ruleId, organizationId);
            if (result == null)
            {
                return new ServiceResponse<AttendanceDeductionRuleDto> { Success = false, Message = "Rule not found." };
            }
            return new ServiceResponse<AttendanceDeductionRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance deduction rule");
            return new ServiceResponse<AttendanceDeductionRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<AttendanceDeductionRuleDto>> CreateAttendanceDeductionRuleAsync(Guid organizationId, CreateAttendanceDeductionRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateAttendanceDeductionRuleAsync(organizationId, dto);
            return new ServiceResponse<AttendanceDeductionRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating attendance deduction rule");
            return new ServiceResponse<AttendanceDeductionRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<AttendanceDeductionRuleDto>> UpdateAttendanceDeductionRuleAsync(Guid ruleId, Guid organizationId, UpdateAttendanceDeductionRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateAttendanceDeductionRuleAsync(ruleId, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<AttendanceDeductionRuleDto> { Success = false, Message = "Rule not found." };
            }
            return new ServiceResponse<AttendanceDeductionRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attendance deduction rule");
            return new ServiceResponse<AttendanceDeductionRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAttendanceDeductionRuleAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteAttendanceDeductionRuleAsync(ruleId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Rule not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attendance deduction rule");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Late Arrival Rules
    public async Task<ServiceResponse<IEnumerable<LateArrivalRuleDto>>> GetAllLateArrivalRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAllLateArrivalRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<LateArrivalRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting late arrival rules");
            return new ServiceResponse<IEnumerable<LateArrivalRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<LateArrivalRuleDto>>> GetActiveLateArrivalRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActiveLateArrivalRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<LateArrivalRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active late arrival rules");
            return new ServiceResponse<IEnumerable<LateArrivalRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LateArrivalRuleDto>> GetLateArrivalRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetLateArrivalRuleByIdAsync(ruleId, organizationId);
            if (result == null)
            {
                return new ServiceResponse<LateArrivalRuleDto> { Success = false, Message = "Rule not found." };
            }
            return new ServiceResponse<LateArrivalRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting late arrival rule");
            return new ServiceResponse<LateArrivalRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LateArrivalRuleDto>> CreateLateArrivalRuleAsync(Guid organizationId, CreateLateArrivalRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateLateArrivalRuleAsync(organizationId, dto);
            return new ServiceResponse<LateArrivalRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating late arrival rule");
            return new ServiceResponse<LateArrivalRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LateArrivalRuleDto>> UpdateLateArrivalRuleAsync(Guid ruleId, Guid organizationId, UpdateLateArrivalRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateLateArrivalRuleAsync(ruleId, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<LateArrivalRuleDto> { Success = false, Message = "Rule not found." };
            }
            return new ServiceResponse<LateArrivalRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating late arrival rule");
            return new ServiceResponse<LateArrivalRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteLateArrivalRuleAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteLateArrivalRuleAsync(ruleId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Rule not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting late arrival rule");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Leave Rules
    public async Task<ServiceResponse<IEnumerable<LeaveRuleDto>>> GetAllLeaveRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAllLeaveRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<LeaveRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leave rules");
            return new ServiceResponse<IEnumerable<LeaveRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<LeaveRuleDto>>> GetActiveLeaveRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActiveLeaveRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<LeaveRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active leave rules");
            return new ServiceResponse<IEnumerable<LeaveRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LeaveRuleDto>> GetLeaveRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetLeaveRuleByIdAsync(ruleId, organizationId);
            if (result == null)
            {
                return new ServiceResponse<LeaveRuleDto> { Success = false, Message = "Rule not found." };
            }
            return new ServiceResponse<LeaveRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leave rule by ID");
            return new ServiceResponse<LeaveRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LeaveRuleDto>> CreateLeaveRuleAsync(Guid organizationId, CreateLeaveRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateLeaveRuleAsync(organizationId, dto);
            return new ServiceResponse<LeaveRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leave rule");
            return new ServiceResponse<LeaveRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LeaveRuleDto>> UpdateLeaveRuleAsync(Guid ruleId, Guid organizationId, UpdateLeaveRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateLeaveRuleAsync(ruleId, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<LeaveRuleDto> { Success = false, Message = "Rule not found." };
            }
            return new ServiceResponse<LeaveRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating leave rule");
            return new ServiceResponse<LeaveRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteLeaveRuleAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteLeaveRuleAsync(ruleId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Rule not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting leave rule");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Attendance Summary
    public async Task<ServiceResponse<PagedResult<PayrollAttendanceSummaryDto>>> GetAllAttendanceSummariesAsync(Guid organizationId, PayrollAttendanceSummaryFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllAttendanceSummariesAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<PayrollAttendanceSummaryDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance summaries");
            return new ServiceResponse<PagedResult<PayrollAttendanceSummaryDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PayrollAttendanceSummaryDto>> GetAttendanceSummaryByIdAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAttendanceSummaryByIdAsync(id, organizationId);
            if (result == null)
            {
                return new ServiceResponse<PayrollAttendanceSummaryDto> { Success = false, Message = "Summary row not found." };
            }
            return new ServiceResponse<PayrollAttendanceSummaryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance summary row");
            return new ServiceResponse<PayrollAttendanceSummaryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PayrollAttendanceSummaryDto>> CreateAttendanceSummaryAsync(Guid organizationId, CreatePayrollAttendanceSummaryDto dto)
    {
        try
        {
            // 1. Fetch the employee's current shift
            var shift = await _attendanceRepository.GetShiftByEmployeeIdAsync(dto.EmployeeId);
            if (shift == null)
            {
                return new ServiceResponse<PayrollAttendanceSummaryDto>
                {
                    Success = false,
                    Message = "No active shift found for the specified employee."
                };
            }

            // 2. Fetch the payroll period to get start/end dates
            var period = await _payRollRepository.GetPayrollPeriodByIdAsync(dto.PeriodId, organizationId);
            if (period == null)
            {
                return new ServiceResponse<PayrollAttendanceSummaryDto>
                {
                    Success = false,
                    Message = "Payroll period not found."
                };
            }

            // 3. Build a HashSet of the shift's working day-of-week numbers
            //    DaysOfWeek: 1=Monday ... 7=Sunday (matching ISO standard)
            var workingDays = new HashSet<int>(shift.DaysOfWeek ?? Array.Empty<int>());

            // 4. Count total calendar days and scheduled working days in the period
            var startDate = period.StartDate.Date;
            var endDate = period.EndDate.Date;
            int totalDays = (endDate - startDate).Days + 1;
            int scheduledDays = 0;

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // DayOfWeek: Sunday=0, Monday=1 ... Saturday=6
                // Map to ISO: Monday=1 ... Sunday=7
                int iso = date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
                if (workingDays.Contains(iso))
                    scheduledDays++;
            }

            // 5. Build an enriched DTO that includes the auto-computed fields
            var enrichedDto = new CreatePayrollAttendanceSummaryDto
            {
                EmployeeId = dto.EmployeeId,
                PeriodId = dto.PeriodId,
                RuleId = dto.RuleId,
                PresentDays = dto.PresentDays,
                AbsentDays = dto.AbsentDays,
                HalfDays = dto.HalfDays,
                EffectiveAbsents = dto.EffectiveAbsents,
                AbsentDeduction = dto.AbsentDeduction,
                HalfDayDeduction = dto.HalfDayDeduction,
                TotalDeduction = dto.TotalDeduction
            };

            var result = await _payRollRepository.CreateAttendanceSummaryAsync(
                organizationId, enrichedDto, shift.ShiftId, scheduledDays, totalDays);

            return new ServiceResponse<PayrollAttendanceSummaryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating attendance summary row");
            return new ServiceResponse<PayrollAttendanceSummaryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PayrollAttendanceSummaryDto>> UpdateAttendanceSummaryAsync(Guid id, Guid organizationId, UpdatePayrollAttendanceSummaryDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateAttendanceSummaryAsync(id, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<PayrollAttendanceSummaryDto> { Success = false, Message = "Summary row not found." };
            }
            return new ServiceResponse<PayrollAttendanceSummaryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attendance summary row");
            return new ServiceResponse<PayrollAttendanceSummaryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAttendanceSummaryAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteAttendanceSummaryAsync(id, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Summary row not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attendance summary row");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Late Attendance
    public async Task<ServiceResponse<PagedResult<LateAttendanceDto>>> GetAllLateAttendanceAsync(Guid organizationId, LateAttendanceFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllLateAttendanceAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<LateAttendanceDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching late attendance records");
            return new ServiceResponse<PagedResult<LateAttendanceDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LateAttendanceDto>> GetLateAttendanceByIdAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetLateAttendanceByIdAsync(id, organizationId);
            if (result == null)
            {
                return new ServiceResponse<LateAttendanceDto> { Success = false, Message = "Late attendance record not found." };
            }
            return new ServiceResponse<LateAttendanceDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching late attendance record by ID");
            return new ServiceResponse<LateAttendanceDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LateAttendanceDto>> CreateLateAttendanceAsync(Guid organizationId, CreateLateAttendanceDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateLateAttendanceAsync(organizationId, dto);
            return new ServiceResponse<LateAttendanceDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating late attendance record");
            return new ServiceResponse<LateAttendanceDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LateAttendanceDto>> UpdateLateAttendanceAsync(Guid id, Guid organizationId, UpdateLateAttendanceDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateLateAttendanceAsync(id, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<LateAttendanceDto> { Success = false, Message = "Late attendance record not found." };
            }
            return new ServiceResponse<LateAttendanceDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating late attendance record");
            return new ServiceResponse<LateAttendanceDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteLateAttendanceAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteLateAttendanceAsync(id, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Late attendance record not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting late attendance record");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Leave Summary
    public async Task<ServiceResponse<PagedResult<LeaveSummaryDto>>> GetLeaveSummariesAsync(Guid organizationId, LeaveSummaryFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetLeaveSummariesAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<LeaveSummaryDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leave summaries");
            return new ServiceResponse<PagedResult<LeaveSummaryDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LeaveSummaryDto>> GetLeaveSummaryByIdAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetLeaveSummaryByIdAsync(id, organizationId);
            if (result == null)
            {
                return new ServiceResponse<LeaveSummaryDto> { Success = false, Message = "Leave summary not found." };
            }
            return new ServiceResponse<LeaveSummaryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leave summary by id");
            return new ServiceResponse<LeaveSummaryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LeaveSummaryDto>> CreateLeaveSummaryAsync(Guid organizationId, CreateLeaveSummaryDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateLeaveSummaryAsync(organizationId, dto);
            return new ServiceResponse<LeaveSummaryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leave summary");
            return new ServiceResponse<LeaveSummaryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LeaveSummaryDto>> UpdateLeaveSummaryAsync(Guid id, Guid organizationId, UpdateLeaveSummaryDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateLeaveSummaryAsync(id, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<LeaveSummaryDto> { Success = false, Message = "Leave summary not found." };
            }
            return new ServiceResponse<LeaveSummaryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating leave summary");
            return new ServiceResponse<LeaveSummaryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteLeaveSummaryAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteLeaveSummaryAsync(id, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Leave summary not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting leave summary");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }


    // Performance Rules
    public async Task<ServiceResponse<IEnumerable<PerformanceRuleDto>>> GetAllPerformanceRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAllPerformanceRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<PerformanceRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance rules");
            return new ServiceResponse<IEnumerable<PerformanceRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<PerformanceRuleDto>>> GetActivePerformanceRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActivePerformanceRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<PerformanceRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active performance rules");
            return new ServiceResponse<IEnumerable<PerformanceRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PerformanceRuleDto>> GetPerformanceRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetPerformanceRuleByIdAsync(ruleId, organizationId);
            if (result == null)
            {
                return new ServiceResponse<PerformanceRuleDto> { Success = false, Message = "Rule not found." };
            }
            return new ServiceResponse<PerformanceRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance rule by ID");
            return new ServiceResponse<PerformanceRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PerformanceRuleDto>> CreatePerformanceRuleAsync(Guid organizationId, CreatePerformanceRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreatePerformanceRuleAsync(organizationId, dto);
            return new ServiceResponse<PerformanceRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating performance rule");
            return new ServiceResponse<PerformanceRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PerformanceRuleDto>> UpdatePerformanceRuleAsync(Guid ruleId, Guid organizationId, UpdatePerformanceRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdatePerformanceRuleAsync(ruleId, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<PerformanceRuleDto> { Success = false, Message = "Rule not found." };
            }
            return new ServiceResponse<PerformanceRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating performance rule");
            return new ServiceResponse<PerformanceRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeletePerformanceRuleAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeletePerformanceRuleAsync(ruleId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Rule not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting performance rule");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Bonus Rules
    public async Task<ServiceResponse<IEnumerable<BonusRuleDto>>> GetAllBonusRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAllBonusRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<BonusRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bonus rules");
            return new ServiceResponse<IEnumerable<BonusRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<BonusRuleDto>>> GetActiveBonusRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActiveBonusRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<BonusRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active bonus rules");
            return new ServiceResponse<IEnumerable<BonusRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<BonusRuleDto>> GetBonusRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetBonusRuleByIdAsync(ruleId, organizationId);
            if (result == null)
            {
                return new ServiceResponse<BonusRuleDto> { Success = false, Message = "Rule not found." };
            }
            return new ServiceResponse<BonusRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bonus rule by ID");
            return new ServiceResponse<BonusRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<BonusRuleDto>> CreateBonusRuleAsync(Guid organizationId, CreateBonusRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateBonusRuleAsync(organizationId, dto);
            return new ServiceResponse<BonusRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bonus rule");
            return new ServiceResponse<BonusRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<BonusRuleDto>> UpdateBonusRuleAsync(Guid ruleId, Guid organizationId, UpdateBonusRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateBonusRuleAsync(ruleId, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<BonusRuleDto> { Success = false, Message = "Rule not found." };
            }
            return new ServiceResponse<BonusRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bonus rule");
            return new ServiceResponse<BonusRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteBonusRuleAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteBonusRuleAsync(ruleId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Rule not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bonus rule");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Performance Pay
    public async Task<ServiceResponse<PagedResult<PerformancePayDto>>> GetAllPerformancePayAsync(Guid organizationId, PerformancePayFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllPerformancePayAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<PerformancePayDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance pay records");
            return new ServiceResponse<PagedResult<PerformancePayDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PerformancePayDto>> GetPerformancePayByIdAsync(Guid performancePayId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetPerformancePayByIdAsync(performancePayId, organizationId);
            if (result == null)
                return new ServiceResponse<PerformancePayDto> { Success = false, Message = "Performance pay record not found." };
            return new ServiceResponse<PerformancePayDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance pay by ID");
            return new ServiceResponse<PerformancePayDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PerformancePayDto>> CreatePerformancePayAsync(Guid organizationId, CreatePerformancePayDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreatePerformancePayAsync(organizationId, dto);
            return new ServiceResponse<PerformancePayDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating performance pay");
            return new ServiceResponse<PerformancePayDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PerformancePayDto>> UpdatePerformancePayAsync(Guid performancePayId, Guid organizationId, UpdatePerformancePayDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdatePerformancePayAsync(performancePayId, organizationId, dto);
            if (result == null)
                return new ServiceResponse<PerformancePayDto> { Success = false, Message = "Performance pay record not found." };
            return new ServiceResponse<PerformancePayDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating performance pay");
            return new ServiceResponse<PerformancePayDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeletePerformancePayAsync(Guid performancePayId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeletePerformancePayAsync(performancePayId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Record not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting performance pay");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Bonus Entries
    public async Task<ServiceResponse<PagedResult<BonusEntryDto>>> GetAllBonusEntriesAsync(Guid organizationId, BonusEntryFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllBonusEntriesAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<BonusEntryDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bonus entries");
            return new ServiceResponse<PagedResult<BonusEntryDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<BonusEntryDto>> GetBonusEntryByIdAsync(Guid bonusId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetBonusEntryByIdAsync(bonusId, organizationId);
            if (result == null)
                return new ServiceResponse<BonusEntryDto> { Success = false, Message = "Bonus entry not found." };
            return new ServiceResponse<BonusEntryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bonus entry by ID");
            return new ServiceResponse<BonusEntryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<BonusEntryDto>> CreateBonusEntryAsync(Guid organizationId, CreateBonusEntryDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateBonusEntryAsync(organizationId, dto);
            return new ServiceResponse<BonusEntryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bonus entry");
            return new ServiceResponse<BonusEntryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<BonusEntryDto>> UpdateBonusEntryAsync(Guid bonusId, Guid organizationId, UpdateBonusEntryDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateBonusEntryAsync(bonusId, organizationId, dto);
            if (result == null)
                return new ServiceResponse<BonusEntryDto> { Success = false, Message = "Bonus entry not found." };
            return new ServiceResponse<BonusEntryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bonus entry");
            return new ServiceResponse<BonusEntryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteBonusEntryAsync(Guid bonusId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteBonusEntryAsync(bonusId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Record not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bonus entry");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Loan Rules
    public async Task<ServiceResponse<IEnumerable<LoanRuleDto>>> GetAllLoanRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAllLoanRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<LoanRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all loan rules");
            return new ServiceResponse<IEnumerable<LoanRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<LoanRuleDto>>> GetActiveLoanRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActiveLoanRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<LoanRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active loan rules");
            return new ServiceResponse<IEnumerable<LoanRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LoanRuleDto>> GetLoanRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetLoanRuleByIdAsync(ruleId, organizationId);
            if (result == null)
                return new ServiceResponse<LoanRuleDto> { Success = false, Message = "Loan rule not found." };
            return new ServiceResponse<LoanRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loan rule by ID");
            return new ServiceResponse<LoanRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LoanRuleDto>> CreateLoanRuleAsync(Guid organizationId, CreateLoanRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateLoanRuleAsync(organizationId, dto);
            return new ServiceResponse<LoanRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating loan rule");
            return new ServiceResponse<LoanRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LoanRuleDto>> UpdateLoanRuleAsync(Guid ruleId, Guid organizationId, UpdateLoanRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateLoanRuleAsync(ruleId, organizationId, dto);
            if (result == null)
                return new ServiceResponse<LoanRuleDto> { Success = false, Message = "Loan rule not found." };
            return new ServiceResponse<LoanRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating loan rule");
            return new ServiceResponse<LoanRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteLoanRuleAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteLoanRuleAsync(ruleId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Record not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting loan rule");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Salary Advance Rules
    public async Task<ServiceResponse<IEnumerable<SalaryAdvanceRuleDto>>> GetAllSalaryAdvanceRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAllSalaryAdvanceRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<SalaryAdvanceRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all salary advance rules");
            return new ServiceResponse<IEnumerable<SalaryAdvanceRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<SalaryAdvanceRuleDto>>> GetActiveSalaryAdvanceRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActiveSalaryAdvanceRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<SalaryAdvanceRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active salary advance rules");
            return new ServiceResponse<IEnumerable<SalaryAdvanceRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SalaryAdvanceRuleDto>> GetSalaryAdvanceRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetSalaryAdvanceRuleByIdAsync(ruleId, organizationId);
            if (result == null)
                return new ServiceResponse<SalaryAdvanceRuleDto> { Success = false, Message = "Salary advance rule not found." };
            return new ServiceResponse<SalaryAdvanceRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting salary advance rule by ID");
            return new ServiceResponse<SalaryAdvanceRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SalaryAdvanceRuleDto>> CreateSalaryAdvanceRuleAsync(Guid organizationId, CreateSalaryAdvanceRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateSalaryAdvanceRuleAsync(organizationId, dto);
            return new ServiceResponse<SalaryAdvanceRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating salary advance rule");
            return new ServiceResponse<SalaryAdvanceRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SalaryAdvanceRuleDto>> UpdateSalaryAdvanceRuleAsync(Guid ruleId, Guid organizationId, UpdateSalaryAdvanceRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateSalaryAdvanceRuleAsync(ruleId, organizationId, dto);
            if (result == null)
                return new ServiceResponse<SalaryAdvanceRuleDto> { Success = false, Message = "Salary advance rule not found." };
            return new ServiceResponse<SalaryAdvanceRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating salary advance rule");
            return new ServiceResponse<SalaryAdvanceRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteSalaryAdvanceRuleAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteSalaryAdvanceRuleAsync(ruleId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Record not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting salary advance rule");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Provident Fund Rules
    public async Task<ServiceResponse<IEnumerable<ProvidentFundRuleDto>>> GetAllProvidentFundRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAllProvidentFundRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<ProvidentFundRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all provident fund rules");
            return new ServiceResponse<IEnumerable<ProvidentFundRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<ProvidentFundRuleDto>>> GetActiveProvidentFundRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActiveProvidentFundRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<ProvidentFundRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active provident fund rules");
            return new ServiceResponse<IEnumerable<ProvidentFundRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<ProvidentFundRuleDto>> GetProvidentFundRuleByIdAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetProvidentFundRuleByIdAsync(ruleId, organizationId);
            if (result == null)
                return new ServiceResponse<ProvidentFundRuleDto> { Success = false, Message = "Provident fund rule not found." };
            return new ServiceResponse<ProvidentFundRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provident fund rule by ID");
            return new ServiceResponse<ProvidentFundRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<ProvidentFundRuleDto>> CreateProvidentFundRuleAsync(Guid organizationId, CreateProvidentFundRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateProvidentFundRuleAsync(organizationId, dto);
            return new ServiceResponse<ProvidentFundRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provident fund rule");
            return new ServiceResponse<ProvidentFundRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<ProvidentFundRuleDto>> UpdateProvidentFundRuleAsync(Guid ruleId, Guid organizationId, UpdateProvidentFundRuleDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateProvidentFundRuleAsync(ruleId, organizationId, dto);
            if (result == null)
                return new ServiceResponse<ProvidentFundRuleDto> { Success = false, Message = "Provident fund rule not found." };
            return new ServiceResponse<ProvidentFundRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provident fund rule");
            return new ServiceResponse<ProvidentFundRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteProvidentFundRuleAsync(Guid ruleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteProvidentFundRuleAsync(ruleId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Record not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting provident fund rule");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Tax Regimes
    public async Task<ServiceResponse<IEnumerable<TaxRegimeDto>>> GetAllTaxRegimesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAllTaxRegimesAsync(organizationId);
            return new ServiceResponse<IEnumerable<TaxRegimeDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tax regimes");
            return new ServiceResponse<IEnumerable<TaxRegimeDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<TaxRegimeDto>>> GetActiveTaxRegimesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActiveTaxRegimesAsync(organizationId);
            return new ServiceResponse<IEnumerable<TaxRegimeDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active tax regimes");
            return new ServiceResponse<IEnumerable<TaxRegimeDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<TaxRegimeDto>> GetTaxRegimeByIdAsync(Guid regimeId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetTaxRegimeByIdAsync(regimeId, organizationId);
            if (result == null)
                return new ServiceResponse<TaxRegimeDto> { Success = false, Message = "Tax regime not found." };
            return new ServiceResponse<TaxRegimeDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tax regime by ID");
            return new ServiceResponse<TaxRegimeDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<TaxRegimeDto>> CreateTaxRegimeAsync(Guid organizationId, CreateTaxRegimeDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateTaxRegimeAsync(organizationId, dto);
            return new ServiceResponse<TaxRegimeDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tax regime");
            return new ServiceResponse<TaxRegimeDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<TaxRegimeDto>> UpdateTaxRegimeAsync(Guid regimeId, Guid organizationId, UpdateTaxRegimeDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateTaxRegimeAsync(regimeId, organizationId, dto);
            if (result == null)
                return new ServiceResponse<TaxRegimeDto> { Success = false, Message = "Tax regime not found." };
            return new ServiceResponse<TaxRegimeDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax regime");
            return new ServiceResponse<TaxRegimeDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteTaxRegimeAsync(Guid regimeId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteTaxRegimeAsync(regimeId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Record not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tax regime");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Tax Categories
    public async Task<ServiceResponse<IEnumerable<TaxCategoryDto>>> GetAllTaxCategoriesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAllTaxCategoriesAsync(organizationId);
            return new ServiceResponse<IEnumerable<TaxCategoryDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tax categories");
            return new ServiceResponse<IEnumerable<TaxCategoryDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<TaxCategoryDto>>> GetActiveTaxCategoriesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActiveTaxCategoriesAsync(organizationId);
            return new ServiceResponse<IEnumerable<TaxCategoryDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active tax categories");
            return new ServiceResponse<IEnumerable<TaxCategoryDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<TaxCategoryDto>>> GetTaxCategoriesByRegimeIdAsync(Guid regimeId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetTaxCategoriesByRegimeIdAsync(regimeId, organizationId);
            return new ServiceResponse<IEnumerable<TaxCategoryDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tax categories by regime ID");
            return new ServiceResponse<IEnumerable<TaxCategoryDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<TaxCategoryDto>> GetTaxCategoryByIdAsync(Guid categoryId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetTaxCategoryByIdAsync(categoryId, organizationId);
            if (result == null)
                return new ServiceResponse<TaxCategoryDto> { Success = false, Message = "Tax category not found." };
            return new ServiceResponse<TaxCategoryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tax category by ID");
            return new ServiceResponse<TaxCategoryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<TaxCategoryDto>> CreateTaxCategoryAsync(Guid organizationId, CreateTaxCategoryDto dto)
    {
        try
        {
            var regime = await _payRollRepository.GetTaxRegimeByIdAsync(dto.RegimeId, organizationId);
            if (regime == null)
                return new ServiceResponse<TaxCategoryDto> { Success = false, Message = "Tax regime not found." };

            var result = await _payRollRepository.CreateTaxCategoryAsync(organizationId, dto);
            return new ServiceResponse<TaxCategoryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tax category");
            return new ServiceResponse<TaxCategoryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<TaxCategoryDto>> UpdateTaxCategoryAsync(Guid categoryId, Guid organizationId, UpdateTaxCategoryDto dto)
    {
        try
        {
            var regime = await _payRollRepository.GetTaxRegimeByIdAsync(dto.RegimeId, organizationId);
            if (regime == null)
                return new ServiceResponse<TaxCategoryDto> { Success = false, Message = "Tax regime not found." };

            var result = await _payRollRepository.UpdateTaxCategoryAsync(categoryId, organizationId, dto);
            if (result == null)
                return new ServiceResponse<TaxCategoryDto> { Success = false, Message = "Tax category not found." };
            return new ServiceResponse<TaxCategoryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax category");
            return new ServiceResponse<TaxCategoryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteTaxCategoryAsync(Guid categoryId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteTaxCategoryAsync(categoryId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Record not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tax category");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Tax Slabs
    public async Task<ServiceResponse<IEnumerable<TaxSlabDto>>> GetAllTaxSlabsAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAllTaxSlabsAsync(organizationId);
            return new ServiceResponse<IEnumerable<TaxSlabDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tax slabs");
            return new ServiceResponse<IEnumerable<TaxSlabDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<TaxSlabDto>>> GetActiveTaxSlabsAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActiveTaxSlabsAsync(organizationId);
            return new ServiceResponse<IEnumerable<TaxSlabDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active tax slabs");
            return new ServiceResponse<IEnumerable<TaxSlabDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<TaxSlabDto>> GetTaxSlabByIdAsync(Guid slabId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetTaxSlabByIdAsync(slabId, organizationId);
            if (result == null)
                return new ServiceResponse<TaxSlabDto> { Success = false, Message = "Tax slab not found." };
            return new ServiceResponse<TaxSlabDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tax slab by ID");
            return new ServiceResponse<TaxSlabDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<TaxSlabDto>> CreateTaxSlabAsync(Guid organizationId, CreateTaxSlabDto dto)
    {
        try
        {
            var category = await _payRollRepository.GetTaxCategoryByIdAsync(dto.CategoryId, organizationId);
            if (category == null)
                return new ServiceResponse<TaxSlabDto> { Success = false, Message = "Tax category not found." };

            var result = await _payRollRepository.CreateTaxSlabAsync(organizationId, dto);
            return new ServiceResponse<TaxSlabDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tax slab");
            return new ServiceResponse<TaxSlabDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<TaxSlabDto>> UpdateTaxSlabAsync(Guid slabId, Guid organizationId, UpdateTaxSlabDto dto)
    {
        try
        {
            var category = await _payRollRepository.GetTaxCategoryByIdAsync(dto.CategoryId, organizationId);
            if (category == null)
                return new ServiceResponse<TaxSlabDto> { Success = false, Message = "Tax category not found." };

            var result = await _payRollRepository.UpdateTaxSlabAsync(slabId, organizationId, dto);
            if (result == null)
                return new ServiceResponse<TaxSlabDto> { Success = false, Message = "Tax slab not found." };
            return new ServiceResponse<TaxSlabDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax slab");
            return new ServiceResponse<TaxSlabDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteTaxSlabAsync(Guid slabId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteTaxSlabAsync(slabId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Record not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tax slab");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Tax Component Rules
    public async Task<ServiceResponse<IEnumerable<TaxComponentRuleDto>>> GetAllTaxComponentRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAllTaxComponentRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<TaxComponentRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tax component rules");
            return new ServiceResponse<IEnumerable<TaxComponentRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<TaxComponentRuleDto>>> GetActiveTaxComponentRulesAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActiveTaxComponentRulesAsync(organizationId);
            return new ServiceResponse<IEnumerable<TaxComponentRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active tax component rules");
            return new ServiceResponse<IEnumerable<TaxComponentRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<TaxComponentRuleDto>> GetTaxComponentRuleByIdAsync(Guid componentRuleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetTaxComponentRuleByIdAsync(componentRuleId, organizationId);
            if (result == null)
                return new ServiceResponse<TaxComponentRuleDto> { Success = false, Message = "Tax component rule not found." };
            return new ServiceResponse<TaxComponentRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tax component rule by ID");
            return new ServiceResponse<TaxComponentRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<TaxComponentRuleDto>> CreateTaxComponentRuleAsync(Guid organizationId, CreateTaxComponentRuleDto dto)
    {
        try
        {
            var regime = await _payRollRepository.GetTaxRegimeByIdAsync(dto.RegimeId, organizationId);
            if (regime == null)
                return new ServiceResponse<TaxComponentRuleDto> { Success = false, Message = "Tax regime not found." };

            var result = await _payRollRepository.CreateTaxComponentRuleAsync(organizationId, dto);
            return new ServiceResponse<TaxComponentRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tax component rule");
            return new ServiceResponse<TaxComponentRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<TaxComponentRuleDto>> UpdateTaxComponentRuleAsync(Guid componentRuleId, Guid organizationId, UpdateTaxComponentRuleDto dto)
    {
        try
        {
            var regime = await _payRollRepository.GetTaxRegimeByIdAsync(dto.RegimeId, organizationId);
            if (regime == null)
                return new ServiceResponse<TaxComponentRuleDto> { Success = false, Message = "Tax regime not found." };

            var result = await _payRollRepository.UpdateTaxComponentRuleAsync(componentRuleId, organizationId, dto);
            if (result == null)
                return new ServiceResponse<TaxComponentRuleDto> { Success = false, Message = "Tax component rule not found." };
            return new ServiceResponse<TaxComponentRuleDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax component rule");
            return new ServiceResponse<TaxComponentRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteTaxComponentRuleAsync(Guid componentRuleId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteTaxComponentRuleAsync(componentRuleId, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Record not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tax component rule");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // Tax Transactions
    public async Task<ServiceResponse<IEnumerable<TaxTransactionDto>>> GetAllTaxTransactionsAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAllTaxTransactionsAsync(organizationId);
            return new ServiceResponse<IEnumerable<TaxTransactionDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tax transactions");
            return new ServiceResponse<IEnumerable<TaxTransactionDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<TaxTransactionDto>>> GetActiveTaxTransactionsAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActiveTaxTransactionsAsync(organizationId);
            return new ServiceResponse<IEnumerable<TaxTransactionDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active tax transactions");
            return new ServiceResponse<IEnumerable<TaxTransactionDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<TaxTransactionDto>> GetTaxTransactionByIdAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetTaxTransactionByIdAsync(id, organizationId);
            if (result == null)
                return new ServiceResponse<TaxTransactionDto> { Success = false, Message = "Tax transaction not found." };
            return new ServiceResponse<TaxTransactionDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tax transaction by ID");
            return new ServiceResponse<TaxTransactionDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<TaxTransactionDto>> CreateTaxTransactionAsync(Guid organizationId, CreateTaxTransactionDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateTaxTransactionAsync(organizationId, dto);
            return new ServiceResponse<TaxTransactionDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tax transaction");
            return new ServiceResponse<TaxTransactionDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<TaxTransactionDto>> UpdateTaxTransactionAsync(Guid id, Guid organizationId, UpdateTaxTransactionDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateTaxTransactionAsync(id, organizationId, dto);
            if (result == null)
                return new ServiceResponse<TaxTransactionDto> { Success = false, Message = "Tax transaction not found." };
            return new ServiceResponse<TaxTransactionDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax transaction");
            return new ServiceResponse<TaxTransactionDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteTaxTransactionAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteTaxTransactionAsync(id, organizationId);
            return new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully." : "Record not found." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tax transaction");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<ProvidentFundRequestDto>> CreateProvidentFundEnrollmentAsync(Guid organizationId, Guid employeeId, CreateProvidentFundEnrollmentDto dto)
    {
        try
        {
            var rule = await _payRollRepository.GetProvidentFundRuleByIdAsync(dto.RuleId, organizationId);
            if (rule == null || !rule.IsActive)
            {
                return new ServiceResponse<ProvidentFundRequestDto>
                {
                    Success = false,
                    Message = "Provident fund rule not found or inactive."
                };
            }

            var result = await _payRollRepository.CreateProvidentFundEnrollmentAsync(organizationId, employeeId, dto, rule.DefaultEmployerPct);
            return new ServiceResponse<ProvidentFundRequestDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provident fund enrollment request");
            return new ServiceResponse<ProvidentFundRequestDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<ProvidentFundRequestDto>> UpdateProvidentFundEnrollmentRequestAsync(Guid requestId, Guid organizationId, Guid employeeId, CreateProvidentFundEnrollmentDto dto)
    {
        try
        {
            var rule = await _payRollRepository.GetProvidentFundRuleByIdAsync(dto.RuleId, organizationId);
            if (rule == null || !rule.IsActive)
            {
                return new ServiceResponse<ProvidentFundRequestDto>
                {
                    Success = false,
                    Message = "Provident fund rule not found or inactive."
                };
            }

            var result = await _payRollRepository.UpdateProvidentFundEnrollmentRequestAsync(requestId, organizationId, employeeId, dto, rule.DefaultEmployerPct);
            return new ServiceResponse<ProvidentFundRequestDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provident fund enrollment request");
            return new ServiceResponse<ProvidentFundRequestDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<ProvidentFundRequestDto>> UpdateProvidentFundPercentageAsync(Guid organizationId, Guid employeeId, UpdateProvidentFundPercentageDto dto)
    {
        try
        {
            if (dto.RuleId == Guid.Empty)
            {
                return new ServiceResponse<ProvidentFundRequestDto>
                {
                    Success = false,
                    Message = "RuleId is required."
                };
            }

            if (dto.EmployeePct <= 0)
            {
                return new ServiceResponse<ProvidentFundRequestDto>
                {
                    Success = false,
                    Message = "Employee percentage must be greater than zero."
                };
            }

            var result = await _payRollRepository.UpdateProvidentFundPercentageAsync(organizationId, employeeId, dto);
            return new ServiceResponse<ProvidentFundRequestDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provident fund percentage request");
            return new ServiceResponse<ProvidentFundRequestDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<ProvidentFundRequestDto>> CreateProvidentFundWithdrawRequestAsync(Guid organizationId, Guid employeeId, CreateProvidentFundWithdrawRequestDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateProvidentFundWithdrawRequestAsync(organizationId, employeeId, dto);
            return new ServiceResponse<ProvidentFundRequestDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provident fund withdrawal request");
            return new ServiceResponse<ProvidentFundRequestDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteProvidentFundRequestAsync(Guid requestId, Guid organizationId, Guid employeeId)
    {
        try
        {
            var result = await _payRollRepository.DeleteProvidentFundRequestAsync(requestId, organizationId, employeeId);
            return new ServiceResponse<bool>
            {
                Success = result,
                Data = result,
                Message = result ? "Provident fund request deleted successfully." : "Pending provident fund request not found."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting provident fund request");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<ProvidentFundRequestDto>>> GetMyProvidentFundRequestsAsync(Guid organizationId, Guid employeeId, ProvidentFundRequestFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetMyProvidentFundRequestsAsync(organizationId, employeeId, filter);
            return new ServiceResponse<PagedResult<ProvidentFundRequestDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my provident fund requests");
            return new ServiceResponse<PagedResult<ProvidentFundRequestDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<ProvidentFundAccountDto>> GetMyActiveProvidentFundAsync(Guid organizationId, Guid employeeId)
    {
        try
        {
            var result = await _payRollRepository.GetMyActiveProvidentFundAsync(organizationId, employeeId);
            if (result == null)
            {
                return new ServiceResponse<ProvidentFundAccountDto>
                {
                    Success = false,
                    Message = "No active provident fund account found."
                };
            }

            return new ServiceResponse<ProvidentFundAccountDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my active provident fund account");
            return new ServiceResponse<ProvidentFundAccountDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<ProvidentFundTransactionDto>>> GetMyProvidentFundTransactionsAsync(Guid organizationId, Guid employeeId, ProvidentFundTransactionFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetMyProvidentFundTransactionsAsync(organizationId, employeeId, filter);
            return new ServiceResponse<PagedResult<ProvidentFundTransactionDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my provident fund transactions");
            return new ServiceResponse<PagedResult<ProvidentFundTransactionDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<AdminProvidentFundRequestDto>>> GetAllProvidentFundRequestsAsync(Guid organizationId, AdminProvidentFundRequestFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllProvidentFundRequestsAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<AdminProvidentFundRequestDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all provident fund requests");
            return new ServiceResponse<PagedResult<AdminProvidentFundRequestDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<AdminProvidentFundAccountDto>>> GetAllProvidentFundAccountsAsync(Guid organizationId, AdminProvidentFundAccountFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllProvidentFundAccountsAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<AdminProvidentFundAccountDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all provident fund accounts");
            return new ServiceResponse<PagedResult<AdminProvidentFundAccountDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<AdminProvidentFundRepaymentDto>>> GetAllProvidentFundRepaymentsAsync(Guid organizationId, AdminProvidentFundRepaymentFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllProvidentFundRepaymentsAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<AdminProvidentFundRepaymentDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all provident fund repayments");
            return new ServiceResponse<PagedResult<AdminProvidentFundRepaymentDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> ApproveProvidentFundRequestAsync(Guid organizationId, Guid approvedBy, ApproveProvidentFundRequestDto dto)
    {
        try
        {
            var result = await _payRollRepository.ApproveProvidentFundRequestAsync(organizationId, approvedBy, dto);
            return new ServiceResponse<bool>
            {
                Success = result,
                Data = result,
                Message = result ? "Provident fund request approved successfully." : "Pending provident fund request not found or invalid request type."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving provident fund request");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> RejectProvidentFundRequestAsync(Guid requestId, Guid organizationId, RejectProvidentFundRequestDto dto)
    {
        try
        {
            var result = await _payRollRepository.RejectProvidentFundRequestAsync(requestId, organizationId, dto);
            return new ServiceResponse<bool>
            {
                Success = result,
                Data = result,
                Message = result ? "Provident fund request rejected successfully." : "Pending provident fund request not found."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting provident fund request");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> AddProvidentFundMonthlyTransactionAsync(Guid organizationId, CreateProvidentFundMonthlyTransactionDto dto)
    {
        try
        {
            var result = await _payRollRepository.AddProvidentFundMonthlyTransactionAsync(organizationId, dto);
            return new ServiceResponse<bool>
            {
                Success = result,
                Data = result,
                Message = result ? "Provident fund monthly transaction added successfully." : "Active provident fund or basic salary not found, or transaction already exists for this period."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding provident fund monthly transaction");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<ProvidentFundEnrollmentDto>> ManualProvidentFundEnrollmentAsync(Guid organizationId, ManualProvidentFundEnrollmentDto dto)
    {
        try
        {
            var result = await _payRollRepository.ManualProvidentFundEnrollmentAsync(organizationId, dto);
            return new ServiceResponse<ProvidentFundEnrollmentDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating manual provident fund enrollment");
            return new ServiceResponse<ProvidentFundEnrollmentDto> { Success = false, Message = "An error occurred." };
        }
    }

    // Salary Advance
    public async Task<ServiceResponse<SalaryAdvanceDto>> CreateSalaryAdvanceAsync(Guid organizationId, Guid employeeId, CreateSalaryAdvanceDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateSalaryAdvanceAsync(organizationId, employeeId, dto);
            return new ServiceResponse<SalaryAdvanceDto> { Success = true, Data = result };
        }
        catch (InvalidOperationException ex)
        {
            return new ServiceResponse<SalaryAdvanceDto> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating salary advance request");
            return new ServiceResponse<SalaryAdvanceDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SalaryAdvanceDto>> UpdateMySalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid employeeId, CreateSalaryAdvanceDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateMySalaryAdvanceAsync(advanceId, organizationId, employeeId, dto);
            if (result == null)
                return new ServiceResponse<SalaryAdvanceDto> { Success = false, Message = "Salary advance request not found or cannot be updated." };
            return new ServiceResponse<SalaryAdvanceDto> { Success = true, Data = result };
        }
        catch (InvalidOperationException ex)
        {
            return new ServiceResponse<SalaryAdvanceDto> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating salary advance request");
            return new ServiceResponse<SalaryAdvanceDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> CancelMySalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid employeeId)
    {
        try
        {
            var result = await _payRollRepository.CancelMySalaryAdvanceAsync(advanceId, organizationId, employeeId);
            return new ServiceResponse<bool>
            {
                Success = result,
                Data = result,
                Message = result ? "Salary advance request cancelled successfully." : "Salary advance request not found or cannot be cancelled."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling salary advance request");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<MySalaryAdvanceDto>>> GetMySalaryAdvancesAsync(Guid organizationId, Guid employeeId, SalaryAdvanceFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetMySalaryAdvancesAsync(organizationId, employeeId, filter);
            return new ServiceResponse<PagedResult<MySalaryAdvanceDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting salary advance requests for employee");
            return new ServiceResponse<PagedResult<MySalaryAdvanceDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<MySalaryAdvanceDto>> GetMySalaryAdvanceByIdAsync(Guid advanceId, Guid organizationId, Guid employeeId)
    {
        try
        {
            var result = await _payRollRepository.GetMySalaryAdvanceByIdAsync(advanceId, organizationId, employeeId);
            if (result == null)
                return new ServiceResponse<MySalaryAdvanceDto> { Success = false, Message = "Salary advance request not found." };
            return new ServiceResponse<MySalaryAdvanceDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting salary advance request by ID for employee");
            return new ServiceResponse<MySalaryAdvanceDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SalaryAdvanceSummaryDto>> GetMySalaryAdvanceSummaryAsync(Guid organizationId, Guid employeeId)
    {
        try
        {
            var result = await _payRollRepository.GetMySalaryAdvanceSummaryAsync(organizationId, employeeId);
            return new ServiceResponse<SalaryAdvanceSummaryDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting salary advance summary for employee");
            return new ServiceResponse<SalaryAdvanceSummaryDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<AdminSalaryAdvanceDto>>> GetAllSalaryAdvanceRequestsAsync(Guid organizationId, AdminSalaryAdvanceFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllSalaryAdvanceRequestsAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<AdminSalaryAdvanceDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all salary advance requests");
            return new ServiceResponse<PagedResult<AdminSalaryAdvanceDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<AdminSalaryAdvanceDto>> GetSalaryAdvanceRequestByIdAsync(Guid advanceId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetSalaryAdvanceRequestByIdAsync(advanceId, organizationId);
            if (result == null)
                return new ServiceResponse<AdminSalaryAdvanceDto> { Success = false, Message = "Salary advance request not found." };
            return new ServiceResponse<AdminSalaryAdvanceDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting salary advance request by ID");
            return new ServiceResponse<AdminSalaryAdvanceDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> ApproveSalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid approvedBy)
    {
        try
        {
            var result = await _payRollRepository.ApproveSalaryAdvanceAsync(advanceId, organizationId, approvedBy);
            return new ServiceResponse<bool>
            {
                Success = result,
                Data = result,
                Message = result ? "Salary advance request approved successfully." : "Salary advance request not found or not in pending status."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving salary advance request");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> RejectSalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid rejectedBy, RejectSalaryAdvanceDto dto)
    {
        try
        {
            var result = await _payRollRepository.RejectSalaryAdvanceAsync(advanceId, organizationId, rejectedBy, dto);
            return new ServiceResponse<bool>
            {
                Success = result,
                Data = result,
                Message = result ? "Salary advance request rejected successfully." : "Salary advance request not found or not in pending status."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting salary advance request");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DisburseSalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid disbursedBy, DisburseSalaryAdvanceDto dto)
    {
        try
        {
            var result = await _payRollRepository.DisburseSalaryAdvanceAsync(advanceId, organizationId, disbursedBy, dto);
            return new ServiceResponse<bool>
            {
                Success = result,
                Data = result,
                Message = result ? "Salary advance request marked as disbursed successfully." : "Salary advance request not found or not in approved status."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disbursing salary advance request");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LoanDto>> GetLoanByIdAsync(Guid loanId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetLoanByIdAsync(loanId, organizationId);
            if (result == null)
            {
                return new ServiceResponse<LoanDto> { Success = false, Message = "Loan not found." };
            }
            return new ServiceResponse<LoanDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loan by ID");
            return new ServiceResponse<LoanDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LoanDto>> CreateLoanRequestAsync(Guid organizationId, Guid employeeId, CreateLoanRequestDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateLoanRequestAsync(organizationId, employeeId, dto);
            return new ServiceResponse<LoanDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating loan request");
            return new ServiceResponse<LoanDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<LoanDto>>> GetMyLoansAsync(Guid organizationId, Guid employeeId, LoanFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetMyLoansAsync(organizationId, employeeId, filter);
            return new ServiceResponse<PagedResult<LoanDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my loan requests");
            return new ServiceResponse<PagedResult<LoanDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LoanDto>> UpdateLoanStatusAsync(Guid organizationId, Guid approvedBy, UpdateLoanStatusDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateLoanStatusAsync(organizationId, approvedBy, dto);
            if (result == null)
            {
                return new ServiceResponse<LoanDto> { Success = false, Message = "Loan request not found or failed to update." };
            }

            return new ServiceResponse<LoanDto>
            {
                Success = true,
                Message = $"Loan request {dto.RequestStatus} successfully.",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating loan status");
            return new ServiceResponse<LoanDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<LoanDto>>> GetAllLoansAsync(Guid organizationId, LoanFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllLoansAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<LoanDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all loan requests");
            return new ServiceResponse<PagedResult<LoanDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<MyActiveLoanDto>>> GetMyActiveLoansAsync(Guid organizationId, Guid employeeId)
    {
        try
        {
            var result = await _payRollRepository.GetMyActiveLoansAsync(organizationId, employeeId);
            return new ServiceResponse<IEnumerable<MyActiveLoanDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my active loans");
            return new ServiceResponse<IEnumerable<MyActiveLoanDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<MyActiveLoanDto>>> GetMyPendingLoansAsync(Guid organizationId, Guid employeeId)
    {
        try
        {
            var result = await _payRollRepository.GetMyPendingLoansAsync(organizationId, employeeId);
            return new ServiceResponse<IEnumerable<MyActiveLoanDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my pending loans");
            return new ServiceResponse<IEnumerable<MyActiveLoanDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LoanDto>> DisburseLoanAsync(Guid organizationId, Guid createdBy, DisburseLoanDto dto)
    {
        try
        {
            var result = await _payRollRepository.DisburseLoanAsync(organizationId, createdBy, dto);
            if (result == null)
            {
                return new ServiceResponse<LoanDto> { Success = false, Message = "Loan disbursement failed. Ensure the request is accepted." };
            }
            return new ServiceResponse<LoanDto> { Success = true, Message = "Loan disbursed successfully.", Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disbursing loan");
            return new ServiceResponse<LoanDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<RepaymentHistoryDto>>> GetAllRepaymentsAsync(Guid organizationId, RepaymentFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllRepaymentsAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<RepaymentHistoryDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all repayments");
            return new ServiceResponse<PagedResult<RepaymentHistoryDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> UpdateLoanPaymentAsync(Guid organizationId, UpdateLoanPaymentDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateLoanPaymentAsync(organizationId, dto);
            if (!result)
            {
                return new ServiceResponse<bool> { Success = false, Message = "Loan payment record not found." };
            }
            return new ServiceResponse<bool> { Success = true, Message = "Loan payment record updated and sync successfully.", Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating loan payment record");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LoanPaymentDto>> GetLoanPaymentByIdAsync(Guid paymentId, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetLoanPaymentByIdAsync(paymentId, organizationId);
            if (result == null)
            {
                return new ServiceResponse<LoanPaymentDto> { Success = false, Message = "Loan payment record not found." };
            }
            return new ServiceResponse<LoanPaymentDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loan payment record by ID");
            return new ServiceResponse<LoanPaymentDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> AddLoanPaymentAsync(Guid organizationId, AddLoanPaymentDto dto)
    {
        try
        {
            var result = await _payRollRepository.AddLoanPaymentAsync(organizationId, dto);
            if (!result)
            {
                return new ServiceResponse<bool> { Success = false, Message = "Loan not found or already completed." };
            }
            return new ServiceResponse<bool> { Success = true, Message = "Loan payment recorded and status updated successfully.", Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding loan payment");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PayrollCalculationResultDto>> CalculatePayrollAsync(Guid organizationId, CalculatePayrollDto dto)
    {
        try
        {
            var result = await _payRollRepository.CalculatePayrollAsync(organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<PayrollCalculationResultDto>
                {
                    Success = false,
                    Message = "Payroll period not found for this organization."
                };
            }

            return new ServiceResponse<PayrollCalculationResultDto>
            {
                Success = true,
                Message = "Payroll calculated successfully (loan + salary advance + provident fund modules).",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating payroll");
            return new ServiceResponse<PayrollCalculationResultDto>
            {
                Success = false,
                Message = "An error occurred."
            };
        }
    }

    public async Task<ServiceResponse<PagedResult<MyActiveLoanDto>>> GetDisbursedLoansAsync(Guid organizationId, DisbursedLoanFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetDisbursedLoansAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<MyActiveLoanDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting disbursed loans");
            return new ServiceResponse<PagedResult<MyActiveLoanDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<MyActiveLoanDto>>> GetDisbursedActiveLoansAsync(Guid organizationId, DisbursedLoanFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetDisbursedActiveLoansAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<MyActiveLoanDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting disbursed active loans");
            return new ServiceResponse<PagedResult<MyActiveLoanDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<MyLoanPaymentDto>>> GetLoanHistoryAsync(Guid organizationId, Guid employeeId, MyLoanHistoryFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetLoanHistoryAsync(organizationId, employeeId, filter);
            return new ServiceResponse<PagedResult<MyLoanPaymentDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loan history");
            return new ServiceResponse<PagedResult<MyLoanPaymentDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<LoanReferenceDto>>> GetMyLoanReferencesAsync(Guid organizationId, Guid employeeId)
    {
        try
        {
            var result = await _payRollRepository.GetMyLoanReferencesAsync(organizationId, employeeId);
            return new ServiceResponse<IEnumerable<LoanReferenceDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loan references");
            return new ServiceResponse<IEnumerable<LoanReferenceDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LoanDto>> GetLoanByIdAndEmployeeAsync(Guid loanId, Guid organizationId, Guid employeeId)
    {
        try
        {
            var result = await _payRollRepository.GetLoanByIdAndEmployeeAsync(loanId, organizationId, employeeId);
            if (result == null)
            {
                return new ServiceResponse<LoanDto> { Success = false, Message = "Loan request not found or does not belong to you." };
            }
            return new ServiceResponse<LoanDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loan by ID and employee");
            return new ServiceResponse<LoanDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<LoanDto>> UpdateLoanRequestAsync(Guid loanId, Guid organizationId, Guid employeeId, CreateLoanRequestDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateLoanRequestAsync(loanId, organizationId, employeeId, dto);
            if (result == null)
            {
                return new ServiceResponse<LoanDto> { Success = false, Message = "Loan request not found, does not belong to you, or is not in pending status." };
            }
            return new ServiceResponse<LoanDto> { Success = true, Message = "Loan request updated successfully.", Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating loan request");
            return new ServiceResponse<LoanDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteLoanRequestAsync(Guid loanId, Guid organizationId, Guid employeeId)
    {
        try
        {
            var result = await _payRollRepository.DeleteLoanRequestAsync(loanId, organizationId, employeeId);
            if (!result)
            {
                return new ServiceResponse<bool> { Success = false, Message = "Loan request not found, does not belong to you, or is not in pending status." };
            }
            return new ServiceResponse<bool> { Success = true, Message = "Loan request deleted successfully.", Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting loan request");
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // ─── Social Security Config ──────────────────────────────────────────────

    public async Task<ServiceResponse<IEnumerable<SocialSecurityJurisdictionDto>>> GetSocialSecurityJurisdictionsAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetSocialSecurityJurisdictionsAsync(organizationId);
            return new ServiceResponse<IEnumerable<SocialSecurityJurisdictionDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social security jurisdictions");
            return new ServiceResponse<IEnumerable<SocialSecurityJurisdictionDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<SocialSecurityAuthorityDto>>> GetSocialSecurityAuthoritiesAsync(Guid organizationId, Guid? jurisdictionId)
    {
        try
        {
            var result = await _payRollRepository.GetSocialSecurityAuthoritiesAsync(organizationId, jurisdictionId);
            return new ServiceResponse<IEnumerable<SocialSecurityAuthorityDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social security authorities");
            return new ServiceResponse<IEnumerable<SocialSecurityAuthorityDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<SocialSecuritySchemeDto>>> GetSocialSecuritySchemesAsync(Guid organizationId, Guid? jurisdictionId, Guid? authorityId)
    {
        try
        {
            var result = await _payRollRepository.GetSocialSecuritySchemesAsync(organizationId, jurisdictionId, authorityId);
            return new ServiceResponse<IEnumerable<SocialSecuritySchemeDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social security schemes");
            return new ServiceResponse<IEnumerable<SocialSecuritySchemeDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<SocialSecurityRuleDto>>> GetSocialSecurityRulesAsync(Guid organizationId, Guid? schemeId)
    {
        try
        {
            var result = await _payRollRepository.GetSocialSecurityRulesAsync(organizationId, schemeId);
            return new ServiceResponse<IEnumerable<SocialSecurityRuleDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social security rules");
            return new ServiceResponse<IEnumerable<SocialSecurityRuleDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SocialSecurityJurisdictionDto>> CreateSocialSecurityJurisdictionAsync(Guid organizationId, CreateSocialSecurityJurisdictionDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.JurisdictionCode) || string.IsNullOrWhiteSpace(dto.JurisdictionName))
            {
                return new ServiceResponse<SocialSecurityJurisdictionDto> { Success = false, Message = "Jurisdiction code and name are required." };
            }

            var result = await _payRollRepository.CreateSocialSecurityJurisdictionAsync(organizationId, dto);
            return new ServiceResponse<SocialSecurityJurisdictionDto>
            {
                Success = true,
                Message = "Social security jurisdiction created successfully.",
                Data = result
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while creating social security jurisdiction");
            return new ServiceResponse<SocialSecurityJurisdictionDto> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social security jurisdiction");
            return new ServiceResponse<SocialSecurityJurisdictionDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SocialSecurityJurisdictionDto>> UpdateSocialSecurityJurisdictionAsync(Guid id, Guid organizationId, UpdateSocialSecurityJurisdictionDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.JurisdictionCode) || string.IsNullOrWhiteSpace(dto.JurisdictionName))
            {
                return new ServiceResponse<SocialSecurityJurisdictionDto> { Success = false, Message = "Jurisdiction code and name are required." };
            }

            var result = await _payRollRepository.UpdateSocialSecurityJurisdictionAsync(id, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<SocialSecurityJurisdictionDto> { Success = false, Message = "Social security jurisdiction not found." };
            }

            return new ServiceResponse<SocialSecurityJurisdictionDto>
            {
                Success = true,
                Message = "Social security jurisdiction updated successfully.",
                Data = result
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while updating social security jurisdiction: {Id}", id);
            return new ServiceResponse<SocialSecurityJurisdictionDto> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating social security jurisdiction: {Id}", id);
            return new ServiceResponse<SocialSecurityJurisdictionDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteSocialSecurityJurisdictionAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteSocialSecurityJurisdictionAsync(id, organizationId);
            if (!result)
            {
                return new ServiceResponse<bool> { Success = false, Message = "Social security jurisdiction not found." };
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Social security jurisdiction deleted successfully.",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting social security jurisdiction: {Id}", id);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SocialSecurityAuthorityDto>> CreateSocialSecurityAuthorityAsync(Guid organizationId, CreateSocialSecurityAuthorityDto dto)
    {
        try
        {
            if (dto.JurisdictionId == Guid.Empty || string.IsNullOrWhiteSpace(dto.AuthorityCode) || string.IsNullOrWhiteSpace(dto.AuthorityName))
            {
                return new ServiceResponse<SocialSecurityAuthorityDto> { Success = false, Message = "Jurisdiction, authority code, and authority name are required." };
            }

            var result = await _payRollRepository.CreateSocialSecurityAuthorityAsync(organizationId, dto);
            return new ServiceResponse<SocialSecurityAuthorityDto>
            {
                Success = true,
                Message = "Social security authority created successfully.",
                Data = result
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while creating social security authority");
            return new ServiceResponse<SocialSecurityAuthorityDto> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social security authority");
            return new ServiceResponse<SocialSecurityAuthorityDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SocialSecurityAuthorityDto>> UpdateSocialSecurityAuthorityAsync(Guid id, Guid organizationId, UpdateSocialSecurityAuthorityDto dto)
    {
        try
        {
            if (dto.JurisdictionId == Guid.Empty || string.IsNullOrWhiteSpace(dto.AuthorityCode) || string.IsNullOrWhiteSpace(dto.AuthorityName))
            {
                return new ServiceResponse<SocialSecurityAuthorityDto> { Success = false, Message = "Jurisdiction, authority code, and authority name are required." };
            }

            var result = await _payRollRepository.UpdateSocialSecurityAuthorityAsync(id, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<SocialSecurityAuthorityDto> { Success = false, Message = "Social security authority not found." };
            }

            return new ServiceResponse<SocialSecurityAuthorityDto>
            {
                Success = true,
                Message = "Social security authority updated successfully.",
                Data = result
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while updating social security authority: {Id}", id);
            return new ServiceResponse<SocialSecurityAuthorityDto> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating social security authority: {Id}", id);
            return new ServiceResponse<SocialSecurityAuthorityDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteSocialSecurityAuthorityAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteSocialSecurityAuthorityAsync(id, organizationId);
            if (!result)
            {
                return new ServiceResponse<bool> { Success = false, Message = "Social security authority not found." };
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Social security authority deleted successfully.",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting social security authority: {Id}", id);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SocialSecuritySchemeDto>> CreateSocialSecuritySchemeAsync(Guid organizationId, CreateSocialSecuritySchemeDto dto)
    {
        try
        {
            if (dto.JurisdictionId == Guid.Empty || string.IsNullOrWhiteSpace(dto.SchemeCode) || string.IsNullOrWhiteSpace(dto.SchemeName))
            {
                return new ServiceResponse<SocialSecuritySchemeDto> { Success = false, Message = "Jurisdiction, scheme code, and scheme name are required." };
            }

            var result = await _payRollRepository.CreateSocialSecuritySchemeAsync(organizationId, dto);
            return new ServiceResponse<SocialSecuritySchemeDto>
            {
                Success = true,
                Message = "Social security scheme created successfully.",
                Data = result
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while creating social security scheme");
            return new ServiceResponse<SocialSecuritySchemeDto> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social security scheme");
            return new ServiceResponse<SocialSecuritySchemeDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SocialSecuritySchemeDto>> UpdateSocialSecuritySchemeAsync(Guid id, Guid organizationId, UpdateSocialSecuritySchemeDto dto)
    {
        try
        {
            if (dto.JurisdictionId == Guid.Empty || string.IsNullOrWhiteSpace(dto.SchemeCode) || string.IsNullOrWhiteSpace(dto.SchemeName))
            {
                return new ServiceResponse<SocialSecuritySchemeDto> { Success = false, Message = "Jurisdiction, scheme code, and scheme name are required." };
            }

            var result = await _payRollRepository.UpdateSocialSecuritySchemeAsync(id, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<SocialSecuritySchemeDto> { Success = false, Message = "Social security scheme not found." };
            }

            return new ServiceResponse<SocialSecuritySchemeDto>
            {
                Success = true,
                Message = "Social security scheme updated successfully.",
                Data = result
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while updating social security scheme: {Id}", id);
            return new ServiceResponse<SocialSecuritySchemeDto> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating social security scheme: {Id}", id);
            return new ServiceResponse<SocialSecuritySchemeDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteSocialSecuritySchemeAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteSocialSecuritySchemeAsync(id, organizationId);
            if (!result)
            {
                return new ServiceResponse<bool> { Success = false, Message = "Social security scheme not found." };
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Social security scheme deleted successfully.",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting social security scheme: {Id}", id);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SocialSecurityRuleDto>> CreateSocialSecurityRuleAsync(Guid organizationId, CreateSocialSecurityRuleDto dto)
    {
        try
        {
            if (dto.SchemeId == Guid.Empty || string.IsNullOrWhiteSpace(dto.RuleName))
            {
                return new ServiceResponse<SocialSecurityRuleDto> { Success = false, Message = "Scheme and rule name are required." };
            }

            var result = await _payRollRepository.CreateSocialSecurityRuleAsync(organizationId, dto);
            return new ServiceResponse<SocialSecurityRuleDto>
            {
                Success = true,
                Message = "Social security rule created successfully.",
                Data = result
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while creating social security rule");
            return new ServiceResponse<SocialSecurityRuleDto> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social security rule");
            return new ServiceResponse<SocialSecurityRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SocialSecurityRuleDto>> UpdateSocialSecurityRuleAsync(Guid id, Guid organizationId, UpdateSocialSecurityRuleDto dto)
    {
        try
        {
            if (dto.SchemeId == Guid.Empty || string.IsNullOrWhiteSpace(dto.RuleName))
            {
                return new ServiceResponse<SocialSecurityRuleDto> { Success = false, Message = "Scheme and rule name are required." };
            }

            var result = await _payRollRepository.UpdateSocialSecurityRuleAsync(id, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<SocialSecurityRuleDto> { Success = false, Message = "Social security rule not found." };
            }

            return new ServiceResponse<SocialSecurityRuleDto>
            {
                Success = true,
                Message = "Social security rule updated successfully.",
                Data = result
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while updating social security rule: {Id}", id);
            return new ServiceResponse<SocialSecurityRuleDto> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating social security rule: {Id}", id);
            return new ServiceResponse<SocialSecurityRuleDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteSocialSecurityRuleAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteSocialSecurityRuleAsync(id, organizationId);
            if (!result)
            {
                return new ServiceResponse<bool> { Success = false, Message = "Social security rule not found." };
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Social security rule deleted successfully.",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting social security rule: {Id}", id);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<SocialSecurityConfigDto>>> GetAllSocialSecurityConfigsAsync(Guid organizationId, SocialSecurityConfigFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllSocialSecurityConfigsAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<SocialSecurityConfigDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social security configs");
            return new ServiceResponse<PagedResult<SocialSecurityConfigDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<SocialSecurityConfigDto>>> GetActiveSocialSecurityConfigsAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActiveSocialSecurityConfigsAsync(organizationId);
            return new ServiceResponse<IEnumerable<SocialSecurityConfigDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active social security configs");
            return new ServiceResponse<IEnumerable<SocialSecurityConfigDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SocialSecurityConfigDto>> GetSocialSecurityConfigByIdAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetSocialSecurityConfigByIdAsync(id, organizationId);
            if (result == null)
            {
                return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Social security config not found." };
            }

            return new ServiceResponse<SocialSecurityConfigDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social security config by ID: {Id}", id);
            return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SocialSecurityConfigDto>> CreateSocialSecurityConfigAsync(Guid organizationId, CreateSocialSecurityConfigDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.ConfigName))
            {
                return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Config name is required." };
            }

            if (!dto.RuleId.HasValue && !dto.SchemeId.HasValue)
            {
                return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Scheme or rule selection is required." };
            }

            if (dto.EmployeeContributionPct < 0 || dto.EmployeeContributionPct > 100 || dto.EmployerContributionPct < 0 || dto.EmployerContributionPct > 100)
            {
                return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Contribution percentages must be between 0 and 100." };
            }

            if (!string.IsNullOrWhiteSpace(dto.ContributionBasis))
            {
                var basis = dto.ContributionBasis.Trim().ToLowerInvariant();
                if (basis != "basic" && basis != "gross" && basis != "fixed" && basis != "component")
                {
                    return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Contribution basis must be one of: basic, gross, fixed, component." };
                }
            }

            if (dto.MinSalaryLimit.HasValue && dto.MinSalaryLimit.Value < 0)
            {
                return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Minimum salary limit cannot be negative." };
            }

            if (dto.MaxSalaryCap.HasValue && dto.MaxSalaryCap.Value <= 0)
            {
                return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Max salary cap must be greater than zero." };
            }

            if (dto.MinSalaryLimit.HasValue && dto.MaxSalaryCap.HasValue && dto.MaxSalaryCap.Value < dto.MinSalaryLimit.Value)
            {
                return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Max salary cap cannot be less than minimum salary limit." };
            }

            if (dto.EffectiveFrom.HasValue && dto.EffectiveTo.HasValue && dto.EffectiveTo.Value < dto.EffectiveFrom.Value)
            {
                return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Effective to date cannot be earlier than effective from date." };
            }

            var result = await _payRollRepository.CreateSocialSecurityConfigAsync(organizationId, dto);
            return new ServiceResponse<SocialSecurityConfigDto>
            {
                Success = true,
                Message = "Social security config created successfully.",
                Data = result
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while creating social security config");
            return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social security config");
            return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SocialSecurityConfigDto>> UpdateSocialSecurityConfigAsync(Guid id, Guid organizationId, UpdateSocialSecurityConfigDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.ConfigName))
            {
                return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Config name is required." };
            }

            if (!dto.RuleId.HasValue && !dto.SchemeId.HasValue)
            {
                return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Scheme or rule selection is required." };
            }

            if (dto.EmployeeContributionPct < 0 || dto.EmployeeContributionPct > 100 || dto.EmployerContributionPct < 0 || dto.EmployerContributionPct > 100)
            {
                return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Contribution percentages must be between 0 and 100." };
            }

            if (!string.IsNullOrWhiteSpace(dto.ContributionBasis))
            {
                var basis = dto.ContributionBasis.Trim().ToLowerInvariant();
                if (basis != "basic" && basis != "gross" && basis != "fixed" && basis != "component")
                {
                    return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Contribution basis must be one of: basic, gross, fixed, component." };
                }
            }

            if (dto.MinSalaryLimit.HasValue && dto.MinSalaryLimit.Value < 0)
            {
                return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Minimum salary limit cannot be negative." };
            }

            if (dto.MaxSalaryCap.HasValue && dto.MaxSalaryCap.Value <= 0)
            {
                return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Max salary cap must be greater than zero." };
            }

            if (dto.MinSalaryLimit.HasValue && dto.MaxSalaryCap.HasValue && dto.MaxSalaryCap.Value < dto.MinSalaryLimit.Value)
            {
                return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Max salary cap cannot be less than minimum salary limit." };
            }

            if (dto.EffectiveFrom.HasValue && dto.EffectiveTo.HasValue && dto.EffectiveTo.Value < dto.EffectiveFrom.Value)
            {
                return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Effective to date cannot be earlier than effective from date." };
            }

            var result = await _payRollRepository.UpdateSocialSecurityConfigAsync(id, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "Social security config not found." };
            }

            return new ServiceResponse<SocialSecurityConfigDto>
            {
                Success = true,
                Message = "Social security config updated successfully.",
                Data = result
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while updating social security config: {Id}", id);
            return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating social security config: {Id}", id);
            return new ServiceResponse<SocialSecurityConfigDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteSocialSecurityConfigAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteSocialSecurityConfigAsync(id, organizationId);
            if (!result)
            {
                return new ServiceResponse<bool> { Success = false, Message = "Social security config not found." };
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Social security config deleted successfully.",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting social security config: {Id}", id);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // ─── Social Security Transactions ────────────────────────────────────────

    public async Task<ServiceResponse<PagedResult<SocialSecurityTransactionDto>>> GetAllSocialSecurityTransactionsAsync(Guid organizationId, SocialSecurityTransactionFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllSocialSecurityTransactionsAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<SocialSecurityTransactionDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social security transactions");
            return new ServiceResponse<PagedResult<SocialSecurityTransactionDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SocialSecurityTransactionDto>> GetSocialSecurityTransactionByIdAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetSocialSecurityTransactionByIdAsync(id, organizationId);
            if (result == null)
            {
                return new ServiceResponse<SocialSecurityTransactionDto> { Success = false, Message = "Social security transaction not found." };
            }

            return new ServiceResponse<SocialSecurityTransactionDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social security transaction by ID: {Id}", id);
            return new ServiceResponse<SocialSecurityTransactionDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SocialSecurityTransactionDto>> CreateSocialSecurityTransactionAsync(Guid organizationId, CreateSocialSecurityTransactionDto dto)
    {
        try
        {
            if (!dto.ConfigId.HasValue && !dto.RuleId.HasValue)
            {
                return new ServiceResponse<SocialSecurityTransactionDto> { Success = false, Message = "Config or rule is required for transaction creation." };
            }

            if (dto.ActualSalary < 0)
            {
                return new ServiceResponse<SocialSecurityTransactionDto> { Success = false, Message = "Actual salary cannot be negative." };
            }

            var result = await _payRollRepository.CreateSocialSecurityTransactionAsync(organizationId, dto);
            return new ServiceResponse<SocialSecurityTransactionDto>
            {
                Success = true,
                Message = "Social security transaction created successfully.",
                Data = result
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while creating social security transaction");
            return new ServiceResponse<SocialSecurityTransactionDto> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social security transaction");
            return new ServiceResponse<SocialSecurityTransactionDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<SocialSecurityTransactionDto>> UpdateSocialSecurityTransactionAsync(Guid id, Guid organizationId, UpdateSocialSecurityTransactionDto dto)
    {
        try
        {
            if (!dto.ConfigId.HasValue && !dto.RuleId.HasValue)
            {
                return new ServiceResponse<SocialSecurityTransactionDto> { Success = false, Message = "Config or rule is required for transaction update." };
            }

            if (dto.ActualSalary < 0)
            {
                return new ServiceResponse<SocialSecurityTransactionDto> { Success = false, Message = "Actual salary cannot be negative." };
            }

            var result = await _payRollRepository.UpdateSocialSecurityTransactionAsync(id, organizationId, dto);
            if (result == null)
            {
                return new ServiceResponse<SocialSecurityTransactionDto> { Success = false, Message = "Social security transaction not found." };
            }

            return new ServiceResponse<SocialSecurityTransactionDto>
            {
                Success = true,
                Message = "Social security transaction updated successfully.",
                Data = result
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while updating social security transaction: {Id}", id);
            return new ServiceResponse<SocialSecurityTransactionDto> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating social security transaction: {Id}", id);
            return new ServiceResponse<SocialSecurityTransactionDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteSocialSecurityTransactionAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteSocialSecurityTransactionAsync(id, organizationId);
            if (!result)
            {
                return new ServiceResponse<bool> { Success = false, Message = "Social security transaction not found." };
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Social security transaction deleted successfully.",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting social security transaction: {Id}", id);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<SocialSecurityTransactionDto>>> GetMySocialSecurityTransactionsAsync(Guid organizationId, Guid employeeId, MySocialSecurityTransactionFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetMySocialSecurityTransactionsAsync(organizationId, employeeId, filter);
            return new ServiceResponse<PagedResult<SocialSecurityTransactionDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my social security transactions for employee: {EmployeeId}", employeeId);
            return new ServiceResponse<PagedResult<SocialSecurityTransactionDto>> { Success = false, Message = "An error occurred." };
        }
    }


    public async Task<ServiceResponse<PayrollOverviewDto>> GetPayrollOverviewAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetPayrollOverviewAsync(organizationId);
            return new ServiceResponse<PayrollOverviewDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll overview");
            return new ServiceResponse<PayrollOverviewDto> { Success = false, Message = "An error occurred." };
        }
    }

    // ─── Gratuity Config ─────────────────────────────────────────────────────

    public async Task<ServiceResponse<IEnumerable<GratuityConfigDto>>> GetAllGratuityConfigsAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetAllGratuityConfigsAsync(organizationId);
            return new ServiceResponse<IEnumerable<GratuityConfigDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gratuity configs");
            return new ServiceResponse<IEnumerable<GratuityConfigDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<GratuityConfigDto>>> GetActiveGratuityConfigsAsync(Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetActiveGratuityConfigsAsync(organizationId);
            return new ServiceResponse<IEnumerable<GratuityConfigDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active gratuity configs");
            return new ServiceResponse<IEnumerable<GratuityConfigDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<GratuityConfigDto>> GetGratuityConfigByIdAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetGratuityConfigByIdAsync(id, organizationId);
            if (result == null)
                return new ServiceResponse<GratuityConfigDto> { Success = false, Message = "Gratuity config not found." };
            return new ServiceResponse<GratuityConfigDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gratuity config by ID: {Id}", id);
            return new ServiceResponse<GratuityConfigDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<GratuityConfigDto>> CreateGratuityConfigAsync(Guid organizationId, CreateGratuityConfigDto dto)
    {
        try
        {
            var validTypes = new[] { "fixed", "peryear", "percentage" };
            if (!validTypes.Contains(dto.CalculationType))
                return new ServiceResponse<GratuityConfigDto> { Success = false, Message = "Invalid calculation type. Must be 'fixed', 'peryear', or 'percentage'." };

            var result = await _payRollRepository.CreateGratuityConfigAsync(organizationId, dto);
            return new ServiceResponse<GratuityConfigDto> { Success = true, Message = "Gratuity config created successfully.", Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating gratuity config");
            return new ServiceResponse<GratuityConfigDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<GratuityConfigDto>> UpdateGratuityConfigAsync(Guid id, Guid organizationId, UpdateGratuityConfigDto dto)
    {
        try
        {
            var validTypes = new[] { "fixed", "peryear", "percentage" };
            if (!validTypes.Contains(dto.CalculationType))
                return new ServiceResponse<GratuityConfigDto> { Success = false, Message = "Invalid calculation type." };

            var result = await _payRollRepository.UpdateGratuityConfigAsync(id, organizationId, dto);
            if (result == null)
                return new ServiceResponse<GratuityConfigDto> { Success = false, Message = "Gratuity config not found." };
            return new ServiceResponse<GratuityConfigDto> { Success = true, Message = "Gratuity config updated successfully.", Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating gratuity config: {Id}", id);
            return new ServiceResponse<GratuityConfigDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteGratuityConfigAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteGratuityConfigAsync(id, organizationId);
            if (!result)
                return new ServiceResponse<bool> { Success = false, Message = "Gratuity config not found." };
            return new ServiceResponse<bool> { Success = true, Message = "Gratuity config deleted successfully.", Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting gratuity config: {Id}", id);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    // ─── Gratuity Transactions ────────────────────────────────────────────────

    public async Task<ServiceResponse<PagedResult<GratuityTransactionDto>>> GetAllGratuityTransactionsAsync(Guid organizationId, GratuityTransactionFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetAllGratuityTransactionsAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<GratuityTransactionDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gratuity transactions");
            return new ServiceResponse<PagedResult<GratuityTransactionDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<GratuityTransactionDto>> GetGratuityTransactionByIdAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.GetGratuityTransactionByIdAsync(id, organizationId);
            if (result == null)
                return new ServiceResponse<GratuityTransactionDto> { Success = false, Message = "Gratuity transaction not found." };
            return new ServiceResponse<GratuityTransactionDto> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gratuity transaction by ID: {Id}", id);
            return new ServiceResponse<GratuityTransactionDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<GratuityTransactionDto>> CreateGratuityTransactionAsync(Guid organizationId, Guid createdBy, CreateGratuityTransactionDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateGratuityTransactionAsync(organizationId, createdBy, dto);
            return new ServiceResponse<GratuityTransactionDto> { Success = true, Message = "Gratuity record created successfully.", Data = result };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while creating gratuity transaction");
            return new ServiceResponse<GratuityTransactionDto> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating gratuity transaction");
            return new ServiceResponse<GratuityTransactionDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<GratuityTransactionDto>> UpdateGratuityTransactionAsync(Guid id, Guid organizationId, UpdateGratuityTransactionDto dto)
    {
        try
        {
            var result = await _payRollRepository.UpdateGratuityTransactionAsync(id, organizationId, dto);
            if (result == null)
                return new ServiceResponse<GratuityTransactionDto> { Success = false, Message = "Gratuity transaction not found." };
            return new ServiceResponse<GratuityTransactionDto> { Success = true, Message = "Gratuity record updated successfully.", Data = result };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while updating gratuity transaction: {Id}", id);
            return new ServiceResponse<GratuityTransactionDto> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating gratuity transaction: {Id}", id);
            return new ServiceResponse<GratuityTransactionDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteGratuityTransactionAsync(Guid id, Guid organizationId)
    {
        try
        {
            var result = await _payRollRepository.DeleteGratuityTransactionAsync(id, organizationId);
            if (!result)
                return new ServiceResponse<bool> { Success = false, Message = "Gratuity transaction not found." };
            return new ServiceResponse<bool> { Success = true, Message = "Gratuity record deleted successfully.", Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting gratuity transaction: {Id}", id);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> ApproveGratuityTransactionAsync(Guid id, Guid organizationId, Guid approvedBy, ApproveGratuityDto dto)
    {
        try
        {
            var result = await _payRollRepository.ApproveGratuityTransactionAsync(id, organizationId, approvedBy, dto);
            if (!result)
                return new ServiceResponse<bool> { Success = false, Message = "Gratuity record not found or is not in 'calculated' status." };
            return new ServiceResponse<bool> { Success = true, Message = "Gratuity record approved successfully.", Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving gratuity transaction: {Id}", id);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> MarkGratuityPaidAsync(Guid id, Guid organizationId, Guid paidBy, MarkGratuityPaidDto dto)
    {
        try
        {
            var result = await _payRollRepository.MarkGratuityPaidAsync(id, organizationId, paidBy, dto);
            if (!result)
                return new ServiceResponse<bool> { Success = false, Message = "Gratuity record not found or is not in 'approved' status." };
            return new ServiceResponse<bool> { Success = true, Message = "Gratuity record marked as paid.", Data = true };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while marking gratuity paid: {Id}", id);
            return new ServiceResponse<bool> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking gratuity as paid: {Id}", id);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<GratuityTransactionDto>> GetMyGratuityStatusAsync(Guid employeeId, Guid organizationId)
    {
        try
        {
            var record = await _payRollRepository.GetMyGratuityStatusAsync(employeeId, organizationId);
            if (record == null)
                return new ServiceResponse<GratuityTransactionDto> { Success = false, Message = "No gratuity record found for this employee." };
            return new ServiceResponse<GratuityTransactionDto> { Success = true, Data = record };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching gratuity status for employee: {EmployeeId}", employeeId);
            return new ServiceResponse<GratuityTransactionDto> { Success = false, Message = "An error occurred." };
        }
    }

    // ============================================================================
    //  SOCIAL SECURITY — Employee lifecycle services
    // ============================================================================

    public async Task<ServiceResponse<SocialSecurityEnrollmentDto>> GetMySocialSecurityEnrollmentAsync(Guid organizationId, Guid employeeId)
    {
        try
        {
            var data = await _payRollRepository.GetMySocialSecurityEnrollmentAsync(organizationId, employeeId);
            return new ServiceResponse<SocialSecurityEnrollmentDto> { Success = true, Data = data! };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching social security enrollment for employee {EmployeeId}", employeeId);
            return new ServiceResponse<SocialSecurityEnrollmentDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<SocialSecurityEnrollmentRosterDto>>> GetSocialSecurityEnrollmentsAsync(Guid organizationId, SocialSecurityEnrollmentFilterDto filter)
    {
        try
        {
            var data = await _payRollRepository.GetSocialSecurityEnrollmentsAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<SocialSecurityEnrollmentRosterDto>> { Success = true, Data = data };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching social security enrollments for organization {OrganizationId}", organizationId);
            return new ServiceResponse<PagedResult<SocialSecurityEnrollmentRosterDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<SocialSecurityEnrollmentRequestDto>>> GetMySocialSecurityRequestsAsync(Guid organizationId, Guid employeeId, MySocialSecurityRequestFilterDto filter)
    {
        try
        {
            var data = await _payRollRepository.GetMySocialSecurityRequestsAsync(organizationId, employeeId, filter);
            return new ServiceResponse<PagedResult<SocialSecurityEnrollmentRequestDto>> { Success = true, Data = data };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching social security requests for employee {EmployeeId}", employeeId);
            return new ServiceResponse<PagedResult<SocialSecurityEnrollmentRequestDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<SocialSecurityEnrollmentRequestDto>>> GetSocialSecurityEnrollmentRequestsAsync(Guid organizationId, SocialSecurityEnrollmentRequestFilterDto filter)
    {
        try
        {
            var data = await _payRollRepository.GetSocialSecurityEnrollmentRequestsAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<SocialSecurityEnrollmentRequestDto>> { Success = true, Data = data };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching social security enrollment requests for organization {OrganizationId}", organizationId);
            return new ServiceResponse<PagedResult<SocialSecurityEnrollmentRequestDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<Guid>> CreateSocialSecurityEnrollmentRequestAsync(Guid organizationId, Guid employeeId, Guid requestedByUserId, CreateSocialSecurityEnrollmentRequestDto dto)
    {
        try
        {
            var id = await _payRollRepository.CreateSocialSecurityEnrollmentRequestAsync(organizationId, employeeId, requestedByUserId, dto);
            if (id == null || id == Guid.Empty)
            {
                return new ServiceResponse<Guid> { Success = false, Message = "Could not create the request." };
            }
            return new ServiceResponse<Guid> { Success = true, Data = id.Value, Message = "Request submitted successfully." };
        }
        catch (InvalidOperationException ex)
        {
            return new ServiceResponse<Guid> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social security enrollment request for employee {EmployeeId}", employeeId);
            return new ServiceResponse<Guid> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> ApproveSocialSecurityEnrollmentRequestAsync(Guid organizationId, Guid requestId, Guid approvedByUserId, ApproveSocialSecurityEnrollmentRequestDto dto)
    {
        try
        {
            var ok = await _payRollRepository.ApproveSocialSecurityEnrollmentRequestAsync(organizationId, requestId, approvedByUserId, dto);
            return new ServiceResponse<bool>
            {
                Success = ok, Data = ok,
                Message = ok ? "Request approved successfully." : "Pending request not found."
            };
        }
        catch (InvalidOperationException ex)
        {
            return new ServiceResponse<bool> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving social security enrollment request {RequestId}", requestId);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> RejectSocialSecurityEnrollmentRequestAsync(Guid organizationId, Guid requestId, Guid rejectedByUserId, RejectSocialSecurityEnrollmentRequestDto dto)
    {
        try
        {
            var ok = await _payRollRepository.RejectSocialSecurityEnrollmentRequestAsync(organizationId, requestId, rejectedByUserId, dto);
            return new ServiceResponse<bool>
            {
                Success = ok, Data = ok,
                Message = ok ? "Request rejected." : "Pending request not found."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting social security enrollment request {RequestId}", requestId);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<SocialSecurityClaimDto>>> GetMySocialSecurityClaimsAsync(Guid organizationId, Guid employeeId, MySocialSecurityClaimFilterDto filter)
    {
        try
        {
            var data = await _payRollRepository.GetMySocialSecurityClaimsAsync(organizationId, employeeId, filter);
            return new ServiceResponse<PagedResult<SocialSecurityClaimDto>> { Success = true, Data = data };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching social security claims for employee {EmployeeId}", employeeId);
            return new ServiceResponse<PagedResult<SocialSecurityClaimDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<SocialSecurityClaimDto>>> GetSocialSecurityClaimsAsync(Guid organizationId, SocialSecurityClaimFilterDto filter)
    {
        try
        {
            var data = await _payRollRepository.GetSocialSecurityClaimsAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<SocialSecurityClaimDto>> { Success = true, Data = data };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching social security claims for organization {OrganizationId}", organizationId);
            return new ServiceResponse<PagedResult<SocialSecurityClaimDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<Guid>> CreateMySocialSecurityClaimAsync(Guid organizationId, Guid employeeId, Guid createdByUserId, CreateSocialSecurityClaimDto dto)
    {
        try
        {
            var id = await _payRollRepository.CreateMySocialSecurityClaimAsync(organizationId, employeeId, createdByUserId, dto);
            if (id == null || id == Guid.Empty)
            {
                return new ServiceResponse<Guid> { Success = false, Message = "Could not create the claim." };
            }
            return new ServiceResponse<Guid> { Success = true, Data = id.Value, Message = "Claim submitted successfully." };
        }
        catch (InvalidOperationException ex)
        {
            return new ServiceResponse<Guid> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social security claim for employee {EmployeeId}", employeeId);
            return new ServiceResponse<Guid> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> ApproveSocialSecurityClaimAsync(Guid organizationId, Guid claimId, Guid approvedByUserId, ApproveSocialSecurityClaimDto dto)
    {
        try
        {
            var ok = await _payRollRepository.ApproveSocialSecurityClaimAsync(organizationId, claimId, approvedByUserId, dto);
            return new ServiceResponse<bool>
            {
                Success = ok, Data = ok,
                Message = ok ? "Claim approved successfully." : "Claim not in an approvable state."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving social security claim {ClaimId}", claimId);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> RejectSocialSecurityClaimAsync(Guid organizationId, Guid claimId, Guid rejectedByUserId, RejectSocialSecurityClaimDto dto)
    {
        try
        {
            var ok = await _payRollRepository.RejectSocialSecurityClaimAsync(organizationId, claimId, rejectedByUserId, dto);
            return new ServiceResponse<bool>
            {
                Success = ok, Data = ok,
                Message = ok ? "Claim rejected." : "Claim not in a rejectable state."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting social security claim {ClaimId}", claimId);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> MarkSocialSecurityClaimPaidAsync(Guid organizationId, Guid claimId, MarkSocialSecurityClaimPaidDto dto)
    {
        try
        {
            var ok = await _payRollRepository.MarkSocialSecurityClaimPaidAsync(organizationId, claimId, dto);
            return new ServiceResponse<bool>
            {
                Success = ok, Data = ok,
                Message = ok ? "Claim marked as paid." : "Claim must be approved before payment."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking social security claim paid {ClaimId}", claimId);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<IEnumerable<SocialSecurityRequestDocumentDto>>> GetSocialSecurityRequestDocumentsAsync(Guid organizationId, Guid requestId)
    {
        try
        {
            var data = await _payRollRepository.GetSocialSecurityRequestDocumentsAsync(organizationId, requestId);
            return new ServiceResponse<IEnumerable<SocialSecurityRequestDocumentDto>> { Success = true, Data = data };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching social security request documents for request {RequestId}", requestId);
            return new ServiceResponse<IEnumerable<SocialSecurityRequestDocumentDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> VerifySocialSecurityRequestDocumentAsync(Guid organizationId, Guid documentId, Guid verifiedByUserId, VerifySocialSecurityRequestDocumentDto dto)
    {
        try
        {
            var ok = await _payRollRepository.VerifySocialSecurityRequestDocumentAsync(organizationId, documentId, verifiedByUserId, dto);
            return new ServiceResponse<bool>
            {
                Success = ok, Data = ok,
                Message = ok ? "Document verification updated." : "Document not found."
            };
        }
        catch (InvalidOperationException ex)
        {
            return new ServiceResponse<bool> { Success = false, Message = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying social security request document {DocumentId}", documentId);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteSocialSecurityRequestDocumentAsync(Guid organizationId, Guid documentId, Guid employeeId)
    {
        try
        {
            var ok = await _payRollRepository.DeleteSocialSecurityRequestDocumentAsync(organizationId, documentId, employeeId);
            return new ServiceResponse<bool>
            {
                Success = ok, Data = ok,
                Message = ok ? "Document deleted." : "Document not found or not yours."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting social security request document {DocumentId}", documentId);
            return new ServiceResponse<bool> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<PayrollResultItemDto>>> GetPayrollResultsAsync(Guid organizationId, PayrollResultsFilterDto filter)
    {
        try
        {
            var result = await _payRollRepository.GetPayrollResultsAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<PayrollResultItemDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll results");
            return new ServiceResponse<PagedResult<PayrollResultItemDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PagedResult<CompliancePayslipRowDto>>> GetCompliancePayslipsAsync(Guid organizationId, CompliancePayslipFilterDto filter)
    {
        try
        {
            var data = await _payRollRepository.GetCompliancePayslipsAsync(organizationId, filter);
            return new ServiceResponse<PagedResult<CompliancePayslipRowDto>> { Success = true, Data = data };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading compliance payslips");
            return new ServiceResponse<PagedResult<CompliancePayslipRowDto>> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<CompliancePayslipStatsDto>> GetCompliancePayslipStatsAsync(Guid organizationId, CompliancePayslipFilterDto filter)
    {
        try
        {
            var data = await _payRollRepository.GetCompliancePayslipStatsAsync(organizationId, filter);
            return new ServiceResponse<CompliancePayslipStatsDto> { Success = true, Data = data };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading compliance payslip stats");
            return new ServiceResponse<CompliancePayslipStatsDto> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<PayrollResultItemDto?>> GetPayrollPayslipSourceAsync(Guid organizationId, Guid resultId)
    {
        try
        {
            var row = await _payRollRepository.GetPayrollResultByResultIdAsync(organizationId, resultId);
            return new ServiceResponse<PayrollResultItemDto?> { Success = true, Data = row };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading payslip payroll row {ResultId}", resultId);
            return new ServiceResponse<PayrollResultItemDto?> { Success = false, Message = "An error occurred." };
        }
    }

    public async Task<ServiceResponse<BulkPayslipPdfResponseDto>> BulkGeneratePayslipPdfsAsync(Guid organizationId, BulkPayslipPdfRequestDto dto)
    {
        var resp = new BulkPayslipPdfResponseDto();
        if (dto.PayrollPeriodId == Guid.Empty || dto.EmployeeIds == null || dto.EmployeeIds.Count == 0)
        {
            return new ServiceResponse<BulkPayslipPdfResponseDto>
            {
                Success = false,
                Message = "Payroll period and at least one employee are required.",
                Data = resp
            };
        }

        try
        {
            var webRoot = _hostEnvironment.WebRootPath ?? Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot");
            var orgFolder = Path.Combine(webRoot, "payslips", organizationId.ToString("D"));
            Directory.CreateDirectory(orgFolder);

            var currencyLabel = await ResolvePayslipCurrencyDisplayPrefixAsync(organizationId);

            foreach (var employeeId in dto.EmployeeIds.Distinct())
            {
                var row = await _payRollRepository.GetPayrollResultForEmployeePeriodAsync(organizationId, dto.PayrollPeriodId, employeeId);
                if (row == null)
                {
                    resp.SkippedCount++;
                    resp.Messages.Add($"No payroll result for employee {employeeId}.");
                    continue;
                }

                var existingUrl = await _payRollRepository.GetPayslipUrlByResultIdAsync(organizationId, row.ResultId);
                if (!string.IsNullOrWhiteSpace(existingUrl) && !dto.OverwriteExisting)
                {
                    resp.SkippedCount++;
                    continue;
                }

                var periodLabel = string.IsNullOrWhiteSpace(row.PeriodName) ? dto.PayrollPeriodId.ToString() : row.PeriodName;
                var pdf = PayslipPdfBuilder.Build(row, periodLabel, currencyLabel);
                var relativeUrl = $"/payslips/{organizationId:D}/{row.ResultId:D}.pdf";
                var physical = Path.Combine(orgFolder, $"{row.ResultId:D}.pdf");
                await File.WriteAllBytesAsync(physical, pdf);

                try
                {
                    await _payRollRepository.UpsertPayslipUrlAsync(organizationId, row.ResultId, employeeId, relativeUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Payslip metadata upsert failed for result {ResultId}; PDF file was still written.", row.ResultId);
                }

                resp.GeneratedCount++;
            }

            return new ServiceResponse<BulkPayslipPdfResponseDto> { Success = true, Data = resp };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk payslip PDF generation");
            return new ServiceResponse<BulkPayslipPdfResponseDto> { Success = false, Message = "An error occurred while generating payslips.", Data = resp };
        }
    }

    public async Task<byte[]?> GetCompliancePayslipPdfBytesAsync(Guid organizationId, Guid resultId)
    {
        var row = await _payRollRepository.GetPayrollResultByResultIdAsync(organizationId, resultId);
        if (row == null)
        {
            return null;
        }

        var currencyLabel = await ResolvePayslipCurrencyDisplayPrefixAsync(organizationId);
        var periodLabel = string.IsNullOrWhiteSpace(row.PeriodName) ? row.PeriodId.ToString("D") : row.PeriodName;
        return PayslipPdfBuilder.Build(row, periodLabel, currencyLabel);
    }

    private async Task<string> ResolvePayslipCurrencyDisplayPrefixAsync(Guid organizationId)
    {
        try
        {
            var code = await _settingsRepository.GetCurrencyAsync(organizationId);
            return PayslipCurrencyDisplay.GetPdfHelveticaSafePrefix(code);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read organization currency for payslip PDF; using default symbol.");
            return PayslipCurrencyDisplay.GetPdfHelveticaSafePrefix(null);
        }
    }
}
