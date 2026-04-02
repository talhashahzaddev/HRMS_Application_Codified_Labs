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

    // Leave Rules
    Task<ServiceResponse<IEnumerable<LeaveRuleDto>>> GetAllLeaveRulesAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<LeaveRuleDto>>> GetActiveLeaveRulesAsync(Guid organizationId);
    Task<ServiceResponse<LeaveRuleDto>> GetLeaveRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<ServiceResponse<LeaveRuleDto>> CreateLeaveRuleAsync(Guid organizationId, CreateLeaveRuleDto dto);
    Task<ServiceResponse<LeaveRuleDto>> UpdateLeaveRuleAsync(Guid ruleId, Guid organizationId, UpdateLeaveRuleDto dto);
    Task<ServiceResponse<bool>> DeleteLeaveRuleAsync(Guid ruleId, Guid organizationId);

    // Attendance Summary
    Task<ServiceResponse<PagedResult<PayrollAttendanceSummaryDto>>> GetAllAttendanceSummariesAsync(Guid organizationId, PayrollAttendanceSummaryFilterDto filter);
    Task<ServiceResponse<PayrollAttendanceSummaryDto>> GetAttendanceSummaryByIdAsync(Guid id, Guid organizationId);
    Task<ServiceResponse<PayrollAttendanceSummaryDto>> CreateAttendanceSummaryAsync(Guid organizationId, CreatePayrollAttendanceSummaryDto dto);
    Task<ServiceResponse<PayrollAttendanceSummaryDto>> UpdateAttendanceSummaryAsync(Guid id, Guid organizationId, UpdatePayrollAttendanceSummaryDto dto);
    Task<ServiceResponse<bool>> DeleteAttendanceSummaryAsync(Guid id, Guid organizationId);

    // Late Attendance
    Task<ServiceResponse<PagedResult<LateAttendanceDto>>> GetAllLateAttendanceAsync(Guid organizationId, LateAttendanceFilterDto filter);
    Task<ServiceResponse<LateAttendanceDto>> GetLateAttendanceByIdAsync(Guid id, Guid organizationId);
    Task<ServiceResponse<LateAttendanceDto>> CreateLateAttendanceAsync(Guid organizationId, CreateLateAttendanceDto dto);
    Task<ServiceResponse<LateAttendanceDto>> UpdateLateAttendanceAsync(Guid id, Guid organizationId, UpdateLateAttendanceDto dto);
    Task<ServiceResponse<bool>> DeleteLateAttendanceAsync(Guid id, Guid organizationId);

    // Leave Summary
    Task<ServiceResponse<PagedResult<LeaveSummaryDto>>> GetLeaveSummariesAsync(Guid organizationId, LeaveSummaryFilterDto filter);
    Task<ServiceResponse<LeaveSummaryDto>> GetLeaveSummaryByIdAsync(Guid id, Guid organizationId);
    Task<ServiceResponse<LeaveSummaryDto>> CreateLeaveSummaryAsync(Guid organizationId, CreateLeaveSummaryDto dto);
    Task<ServiceResponse<LeaveSummaryDto>> UpdateLeaveSummaryAsync(Guid id, Guid organizationId, UpdateLeaveSummaryDto dto);
    Task<ServiceResponse<bool>> DeleteLeaveSummaryAsync(Guid id, Guid organizationId);
    // Performance Rules
    Task<ServiceResponse<IEnumerable<PerformanceRuleDto>>> GetAllPerformanceRulesAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<PerformanceRuleDto>>> GetActivePerformanceRulesAsync(Guid organizationId);
    Task<ServiceResponse<PerformanceRuleDto>> GetPerformanceRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<ServiceResponse<PerformanceRuleDto>> CreatePerformanceRuleAsync(Guid organizationId, CreatePerformanceRuleDto dto);
    Task<ServiceResponse<PerformanceRuleDto>> UpdatePerformanceRuleAsync(Guid ruleId, Guid organizationId, UpdatePerformanceRuleDto dto);
    Task<ServiceResponse<bool>> DeletePerformanceRuleAsync(Guid ruleId, Guid organizationId);
}
