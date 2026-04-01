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
}
