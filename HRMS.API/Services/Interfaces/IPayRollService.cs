using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IPayRollService
{
    Task<ServiceResponse<PayRollRulesGroupedDto>> GetAllRulesAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<OvertimeRuleDto>>> GetAllOvertimeRulesAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<OvertimeRuleDto>>> GetActiveOvertimeRulesAsync(Guid organizationId);
    Task<ServiceResponse<OvertimeRuleDto>> GetOvertimeRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<ServiceResponse<OvertimeRuleDto>> CreateOvertimeRuleAsync(Guid organizationId, CreateOvertimeRuleDto dto);
    Task<ServiceResponse<OvertimeRuleDto>> UpdateOvertimeRuleAsync(Guid ruleId, Guid organizationId, UpdateOvertimeRuleDto dto);
    Task<ServiceResponse<bool>> ToggleOvertimeRuleStatusAsync(Guid ruleId, Guid organizationId);
    Task<ServiceResponse<bool>> ToggleOvertimeRuleDeletionAsync(Guid ruleId, Guid organizationId);

    // Overtime Entries
    Task<ServiceResponse<PagedResult<OvertimeEntryDto>>> GetAllOvertimeEntriesAsync(Guid organizationId, OvertimeEntryFilterDto filter);
    Task<ServiceResponse<OvertimeEntryDto>> GetOvertimeEntryByIdAsync(Guid overtimeId, Guid organizationId);
    Task<ServiceResponse<OvertimeEntryDto>> CreateOvertimeEntryAsync(Guid organizationId, CreateOvertimeEntryDto dto);
    Task<ServiceResponse<OvertimeEntryDto>> UpdateOvertimeEntryAsync(Guid overtimeId, Guid organizationId, UpdateOvertimeEntryDto dto);
    Task<ServiceResponse<bool>> DeleteOvertimeEntryAsync(Guid overtimeId, Guid organizationId);
    
    // Payroll Periods
    Task<ServiceResponse<PayrollPeriodPagedResult>> GetAllPayrollPeriodsAsync(Guid organizationId, PayrollPeriodFilterDto filter);
    Task<ServiceResponse<PayrollPeriodDto>> GetPayrollPeriodByIdAsync(Guid periodId, Guid organizationId);
    Task<ServiceResponse<PayrollPeriodDto>> CreatePayrollPeriodAsync(Guid organizationId, Guid employeeId, CreatePayrollPeriodDto dto);
    Task<ServiceResponse<PayrollPeriodDto>> UpdatePayrollPeriodAsync(Guid periodId, Guid organizationId, UpdatePayrollPeriodDto dto);
    Task<ServiceResponse<bool>> DeletePayrollPeriodAsync(Guid periodId, Guid organizationId);

    // Attendance Deduction Rules
    Task<ServiceResponse<IEnumerable<AttendanceDeductionRuleDto>>> GetAllAttendanceDeductionRulesAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<AttendanceDeductionRuleDto>>> GetActiveAttendanceDeductionRulesAsync(Guid organizationId);
    Task<ServiceResponse<AttendanceDeductionRuleDto>> GetAttendanceDeductionRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<ServiceResponse<AttendanceDeductionRuleDto>> CreateAttendanceDeductionRuleAsync(Guid organizationId, CreateAttendanceDeductionRuleDto dto);
    Task<ServiceResponse<AttendanceDeductionRuleDto>> UpdateAttendanceDeductionRuleAsync(Guid ruleId, Guid organizationId, UpdateAttendanceDeductionRuleDto dto);
    Task<ServiceResponse<bool>> DeleteAttendanceDeductionRuleAsync(Guid ruleId, Guid organizationId);
    
    // Late Arrival Rules
    Task<ServiceResponse<IEnumerable<LateArrivalRuleDto>>> GetAllLateArrivalRulesAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<LateArrivalRuleDto>>> GetActiveLateArrivalRulesAsync(Guid organizationId);
    Task<ServiceResponse<LateArrivalRuleDto>> GetLateArrivalRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<ServiceResponse<LateArrivalRuleDto>> CreateLateArrivalRuleAsync(Guid organizationId, CreateLateArrivalRuleDto dto);
    Task<ServiceResponse<LateArrivalRuleDto>> UpdateLateArrivalRuleAsync(Guid ruleId, Guid organizationId, UpdateLateArrivalRuleDto dto);
    Task<ServiceResponse<bool>> DeleteLateArrivalRuleAsync(Guid ruleId, Guid organizationId);

    // Attendance Summary
    Task<ServiceResponse<PagedResult<PayrollAttendanceSummaryDto>>> GetAllAttendanceSummariesAsync(Guid organizationId, PayrollAttendanceSummaryFilterDto filter);
    Task<ServiceResponse<PayrollAttendanceSummaryDto>> GetAttendanceSummaryByIdAsync(Guid id, Guid organizationId);
    Task<ServiceResponse<PayrollAttendanceSummaryDto>> CreateAttendanceSummaryAsync(Guid organizationId, CreatePayrollAttendanceSummaryDto dto);
    Task<ServiceResponse<PayrollAttendanceSummaryDto>> UpdateAttendanceSummaryAsync(Guid id, Guid organizationId, UpdatePayrollAttendanceSummaryDto dto);
    Task<ServiceResponse<bool>> DeleteAttendanceSummaryAsync(Guid id, Guid organizationId);
}
