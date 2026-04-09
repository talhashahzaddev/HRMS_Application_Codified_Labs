using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class PayRollService : IPayRollService
{
    private readonly IPayRollRepository _payRollRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ILogger<PayRollService> _logger;

    public PayRollService(
        IPayRollRepository payRollRepository,
        IAttendanceRepository attendanceRepository,
        ILogger<PayRollService> logger)
    {
        _payRollRepository = payRollRepository;
        _attendanceRepository = attendanceRepository;
        _logger = logger;
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
            var endDate   = period.EndDate.Date;
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
                EmployeeId        = dto.EmployeeId,
                PeriodId          = dto.PeriodId,
                RuleId            = dto.RuleId,
                PresentDays       = dto.PresentDays,
                AbsentDays        = dto.AbsentDays,
                HalfDays          = dto.HalfDays,
                EffectiveAbsents  = dto.EffectiveAbsents,
                AbsentDeduction   = dto.AbsentDeduction,
                HalfDayDeduction  = dto.HalfDayDeduction,
                TotalDeduction    = dto.TotalDeduction
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

    // Salary Advance
    public async Task<ServiceResponse<SalaryAdvanceDto>> CreateSalaryAdvanceAsync(Guid organizationId, Guid employeeId, CreateSalaryAdvanceDto dto)
    {
        try
        {
            var result = await _payRollRepository.CreateSalaryAdvanceAsync(organizationId, employeeId, dto);
            return new ServiceResponse<SalaryAdvanceDto> { Success = true, Data = result };
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
}
