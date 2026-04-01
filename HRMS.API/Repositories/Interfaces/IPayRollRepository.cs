using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces;

public interface IPayRollRepository
{
    Task<PayRollRulesGroupedDto> GetAllRulesAsync(Guid organizationId);
    
    // Overtime Rules
    Task<IEnumerable<OvertimeRuleDto>> GetAllOvertimeRulesAsync(Guid organizationId);
    Task<IEnumerable<OvertimeRuleDto>> GetActiveOvertimeRulesAsync(Guid organizationId);
    Task<OvertimeRuleDto?> GetOvertimeRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<OvertimeRuleDto> CreateOvertimeRuleAsync(Guid organizationId, CreateOvertimeRuleDto dto);
    Task<OvertimeRuleDto?> UpdateOvertimeRuleAsync(Guid ruleId, Guid organizationId, UpdateOvertimeRuleDto dto);
    Task<bool> ToggleOvertimeRuleStatusAsync(Guid ruleId, Guid organizationId); // Active/Inactive
    Task<bool> ToggleOvertimeRuleDeletionAsync(Guid ruleId, Guid organizationId); // Delete/Restore

    // Overtime Entries
    Task<PagedResult<OvertimeEntryDto>> GetAllOvertimeEntriesAsync(Guid organizationId, OvertimeEntryFilterDto filter);
    Task<OvertimeEntryDto?> GetOvertimeEntryByIdAsync(Guid overtimeId, Guid organizationId);
    Task<OvertimeEntryDto> CreateOvertimeEntryAsync(Guid organizationId, CreateOvertimeEntryDto dto);
    Task<OvertimeEntryDto?> UpdateOvertimeEntryAsync(Guid overtimeId, Guid organizationId, UpdateOvertimeEntryDto dto);
    Task<bool> DeleteOvertimeEntryAsync(Guid overtimeId, Guid organizationId);
    
    // Payroll Periods
    Task<PayrollPeriodPagedResult> GetAllPayrollPeriodsAsync(Guid organizationId, PayrollPeriodFilterDto filter);
    Task<PayrollPeriodDto?> GetPayrollPeriodByIdAsync(Guid periodId, Guid organizationId);
    Task<PayrollPeriodDto> CreatePayrollPeriodAsync(Guid organizationId, Guid employeeId, CreatePayrollPeriodDto dto);
    Task<PayrollPeriodDto?> UpdatePayrollPeriodAsync(Guid periodId, Guid organizationId, UpdatePayrollPeriodDto dto);
    Task<bool> DeletePayrollPeriodAsync(Guid periodId, Guid organizationId);

    // Attendance Deduction Rules
    Task<IEnumerable<AttendanceDeductionRuleDto>> GetAllAttendanceDeductionRulesAsync(Guid organizationId);
    Task<IEnumerable<AttendanceDeductionRuleDto>> GetActiveAttendanceDeductionRulesAsync(Guid organizationId);
    Task<AttendanceDeductionRuleDto?> GetAttendanceDeductionRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<AttendanceDeductionRuleDto> CreateAttendanceDeductionRuleAsync(Guid organizationId, CreateAttendanceDeductionRuleDto dto);
    Task<AttendanceDeductionRuleDto?> UpdateAttendanceDeductionRuleAsync(Guid ruleId, Guid organizationId, UpdateAttendanceDeductionRuleDto dto);
    Task<bool> DeleteAttendanceDeductionRuleAsync(Guid ruleId, Guid organizationId);
    
    // Late Arrival Rules
    Task<IEnumerable<LateArrivalRuleDto>> GetAllLateArrivalRulesAsync(Guid organizationId);
    Task<IEnumerable<LateArrivalRuleDto>> GetActiveLateArrivalRulesAsync(Guid organizationId);
    Task<LateArrivalRuleDto?> GetLateArrivalRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<LateArrivalRuleDto> CreateLateArrivalRuleAsync(Guid organizationId, CreateLateArrivalRuleDto dto);
    Task<LateArrivalRuleDto?> UpdateLateArrivalRuleAsync(Guid ruleId, Guid organizationId, UpdateLateArrivalRuleDto dto);
    Task<bool> DeleteLateArrivalRuleAsync(Guid ruleId, Guid organizationId);

    // Attendance Summary
    Task<PagedResult<PayrollAttendanceSummaryDto>> GetAllAttendanceSummariesAsync(Guid organizationId, PayrollAttendanceSummaryFilterDto filter);
    Task<PayrollAttendanceSummaryDto?> GetAttendanceSummaryByIdAsync(Guid id, Guid organizationId);
    Task<PayrollAttendanceSummaryDto> CreateAttendanceSummaryAsync(Guid organizationId, CreatePayrollAttendanceSummaryDto dto, Guid? shiftId, int scheduledDays, int totalDays);
    Task<PayrollAttendanceSummaryDto?> UpdateAttendanceSummaryAsync(Guid id, Guid organizationId, UpdatePayrollAttendanceSummaryDto dto);
    Task<bool> DeleteAttendanceSummaryAsync(Guid id, Guid organizationId);
}
