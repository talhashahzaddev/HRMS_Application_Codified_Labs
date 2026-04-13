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

    // Leave Rules
    Task<IEnumerable<LeaveRuleDto>> GetAllLeaveRulesAsync(Guid organizationId);
    Task<IEnumerable<LeaveRuleDto>> GetActiveLeaveRulesAsync(Guid organizationId);
    Task<LeaveRuleDto?> GetLeaveRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<LeaveRuleDto> CreateLeaveRuleAsync(Guid organizationId, CreateLeaveRuleDto dto);
    Task<LeaveRuleDto?> UpdateLeaveRuleAsync(Guid ruleId, Guid organizationId, UpdateLeaveRuleDto dto);
    Task<bool> DeleteLeaveRuleAsync(Guid ruleId, Guid organizationId);

    // Attendance Summary
    Task<PagedResult<PayrollAttendanceSummaryDto>> GetAllAttendanceSummariesAsync(Guid organizationId, PayrollAttendanceSummaryFilterDto filter);
    Task<PayrollAttendanceSummaryDto?> GetAttendanceSummaryByIdAsync(Guid id, Guid organizationId);
    Task<PayrollAttendanceSummaryDto> CreateAttendanceSummaryAsync(Guid organizationId, CreatePayrollAttendanceSummaryDto dto, Guid? shiftId, int scheduledDays, int totalDays);
    Task<PayrollAttendanceSummaryDto?> UpdateAttendanceSummaryAsync(Guid id, Guid organizationId, UpdatePayrollAttendanceSummaryDto dto);
    Task<bool> DeleteAttendanceSummaryAsync(Guid id, Guid organizationId);

    // Late Attendance
    Task<PagedResult<LateAttendanceDto>> GetAllLateAttendanceAsync(Guid organizationId, LateAttendanceFilterDto filter);
    Task<LateAttendanceDto?> GetLateAttendanceByIdAsync(Guid id, Guid organizationId);
    Task<LateAttendanceDto> CreateLateAttendanceAsync(Guid organizationId, CreateLateAttendanceDto dto);
    Task<LateAttendanceDto?> UpdateLateAttendanceAsync(Guid id, Guid organizationId, UpdateLateAttendanceDto dto);
    Task<bool> DeleteLateAttendanceAsync(Guid id, Guid organizationId);

    // Leave Summary
    Task<PagedResult<LeaveSummaryDto>> GetLeaveSummariesAsync(Guid organizationId, LeaveSummaryFilterDto filter);
    Task<LeaveSummaryDto?> GetLeaveSummaryByIdAsync(Guid id, Guid organizationId);
    Task<LeaveSummaryDto> CreateLeaveSummaryAsync(Guid organizationId, CreateLeaveSummaryDto dto);
    Task<LeaveSummaryDto?> UpdateLeaveSummaryAsync(Guid id, Guid organizationId, UpdateLeaveSummaryDto dto);
    Task<bool> DeleteLeaveSummaryAsync(Guid id, Guid organizationId);

    // Performance Rules
    Task<IEnumerable<PerformanceRuleDto>> GetAllPerformanceRulesAsync(Guid organizationId);
    Task<IEnumerable<PerformanceRuleDto>> GetActivePerformanceRulesAsync(Guid organizationId);
    Task<PerformanceRuleDto?> GetPerformanceRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<PerformanceRuleDto> CreatePerformanceRuleAsync(Guid organizationId, CreatePerformanceRuleDto dto);
    Task<PerformanceRuleDto?> UpdatePerformanceRuleAsync(Guid ruleId, Guid organizationId, UpdatePerformanceRuleDto dto);
    Task<bool> DeletePerformanceRuleAsync(Guid ruleId, Guid organizationId);

    // Bonus Rules
    Task<IEnumerable<BonusRuleDto>> GetAllBonusRulesAsync(Guid organizationId);
    Task<IEnumerable<BonusRuleDto>> GetActiveBonusRulesAsync(Guid organizationId);
    Task<BonusRuleDto?> GetBonusRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<BonusRuleDto> CreateBonusRuleAsync(Guid organizationId, CreateBonusRuleDto dto);
    Task<BonusRuleDto?> UpdateBonusRuleAsync(Guid ruleId, Guid organizationId, UpdateBonusRuleDto dto);
    Task<bool> DeleteBonusRuleAsync(Guid ruleId, Guid organizationId);

    // Performance Pay
    Task<PagedResult<PerformancePayDto>> GetAllPerformancePayAsync(Guid organizationId, PerformancePayFilterDto filter);
    Task<PerformancePayDto?> GetPerformancePayByIdAsync(Guid performancePayId, Guid organizationId);
    Task<PerformancePayDto> CreatePerformancePayAsync(Guid organizationId, CreatePerformancePayDto dto);
    Task<PerformancePayDto?> UpdatePerformancePayAsync(Guid performancePayId, Guid organizationId, UpdatePerformancePayDto dto);
    Task<bool> DeletePerformancePayAsync(Guid performancePayId, Guid organizationId);

    // Bonus Entries
    Task<PagedResult<BonusEntryDto>> GetAllBonusEntriesAsync(Guid organizationId, BonusEntryFilterDto filter);
    Task<BonusEntryDto?> GetBonusEntryByIdAsync(Guid bonusId, Guid organizationId);
    Task<BonusEntryDto> CreateBonusEntryAsync(Guid organizationId, CreateBonusEntryDto dto);
    Task<BonusEntryDto?> UpdateBonusEntryAsync(Guid bonusId, Guid organizationId, UpdateBonusEntryDto dto);
    Task<bool> DeleteBonusEntryAsync(Guid bonusId, Guid organizationId);

    // Loan Rules
    Task<IEnumerable<LoanRuleDto>> GetAllLoanRulesAsync(Guid organizationId);
    Task<IEnumerable<LoanRuleDto>> GetActiveLoanRulesAsync(Guid organizationId);
    Task<LoanRuleDto?> GetLoanRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<LoanRuleDto> CreateLoanRuleAsync(Guid organizationId, CreateLoanRuleDto dto);
    Task<LoanRuleDto?> UpdateLoanRuleAsync(Guid ruleId, Guid organizationId, UpdateLoanRuleDto dto);
    Task<bool> DeleteLoanRuleAsync(Guid ruleId, Guid organizationId);

    // Salary Advance Rules
    Task<IEnumerable<SalaryAdvanceRuleDto>> GetAllSalaryAdvanceRulesAsync(Guid organizationId);
    Task<IEnumerable<SalaryAdvanceRuleDto>> GetActiveSalaryAdvanceRulesAsync(Guid organizationId);
    Task<SalaryAdvanceRuleDto?> GetSalaryAdvanceRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<SalaryAdvanceRuleDto> CreateSalaryAdvanceRuleAsync(Guid organizationId, CreateSalaryAdvanceRuleDto dto);
    Task<SalaryAdvanceRuleDto?> UpdateSalaryAdvanceRuleAsync(Guid ruleId, Guid organizationId, UpdateSalaryAdvanceRuleDto dto);
    Task<bool> DeleteSalaryAdvanceRuleAsync(Guid ruleId, Guid organizationId);

    // Provident Fund Rules
    Task<IEnumerable<ProvidentFundRuleDto>> GetAllProvidentFundRulesAsync(Guid organizationId);
    Task<IEnumerable<ProvidentFundRuleDto>> GetActiveProvidentFundRulesAsync(Guid organizationId);
    Task<ProvidentFundRuleDto?> GetProvidentFundRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<ProvidentFundRuleDto> CreateProvidentFundRuleAsync(Guid organizationId, CreateProvidentFundRuleDto dto);
    Task<ProvidentFundRuleDto?> UpdateProvidentFundRuleAsync(Guid ruleId, Guid organizationId, UpdateProvidentFundRuleDto dto);
    Task<bool> DeleteProvidentFundRuleAsync(Guid ruleId, Guid organizationId);

    // Provident Fund (Employee Side)
    Task<ProvidentFundEnrollmentDto> CreateProvidentFundEnrollmentAsync(Guid organizationId, Guid employeeId, CreateProvidentFundEnrollmentDto dto, decimal employerPct);
    Task<ProvidentFundEnrollmentDto?> UpdateProvidentFundEnrollmentRequestAsync(Guid pfId, Guid organizationId, Guid employeeId, CreateProvidentFundEnrollmentDto dto, decimal employerPct);
    Task<ProvidentFundEnrollmentDto?> UpdateProvidentFundPercentageAsync(Guid organizationId, Guid employeeId, UpdateProvidentFundPercentageDto dto);
    Task<ProvidentFundEnrollmentDto?> CreateProvidentFundWithdrawRequestAsync(Guid organizationId, Guid employeeId, CreateProvidentFundWithdrawRequestDto dto);
    Task<ProvidentFundEnrollmentDto?> CreateProvidentFundSettlementRequestAsync(Guid organizationId, Guid employeeId, CreateProvidentFundSettlementRequestDto dto);
    Task<bool> DeleteProvidentFundRequestAsync(Guid pfId, Guid organizationId, Guid employeeId);
    Task<ProvidentFundEnrollmentDto?> GetMyPendingProvidentFundRequestAsync(Guid organizationId, Guid employeeId);
    Task<PagedResult<ProvidentFundTransactionDto>> GetMyProvidentFundTransactionsAsync(Guid organizationId, Guid employeeId, ProvidentFundTransactionFilterDto filter);
    Task<PagedResult<AdminProvidentFundRequestDto>> GetAllProvidentFundRequestsAsync(Guid organizationId, AdminProvidentFundRequestFilterDto filter);
    Task<bool> ApproveProvidentFundRequestAsync(Guid organizationId, Guid approvedBy, ApproveProvidentFundRequestDto dto);
    Task<bool> RejectProvidentFundRequestAsync(Guid pfId, Guid organizationId, RejectProvidentFundRequestDto dto);
    Task<bool> AddProvidentFundMonthlyTransactionAsync(Guid organizationId, CreateProvidentFundMonthlyTransactionDto dto);
    Task<ProvidentFundEnrollmentDto> ManualProvidentFundEnrollmentAsync(Guid organizationId, ManualProvidentFundEnrollmentDto dto);

    // Salary Advance
    Task<SalaryAdvanceDto> CreateSalaryAdvanceAsync(Guid organizationId, Guid employeeId, CreateSalaryAdvanceDto dto);
    Task<SalaryAdvanceDto?> UpdateMySalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid employeeId, CreateSalaryAdvanceDto dto);
    Task<bool> CancelMySalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid employeeId);
    Task<PagedResult<MySalaryAdvanceDto>> GetMySalaryAdvancesAsync(Guid organizationId, Guid employeeId, SalaryAdvanceFilterDto filter);
    Task<MySalaryAdvanceDto?> GetMySalaryAdvanceByIdAsync(Guid advanceId, Guid organizationId, Guid employeeId);
    Task<PagedResult<AdminSalaryAdvanceDto>> GetAllSalaryAdvanceRequestsAsync(Guid organizationId, AdminSalaryAdvanceFilterDto filter);
    Task<AdminSalaryAdvanceDto?> GetSalaryAdvanceRequestByIdAsync(Guid advanceId, Guid organizationId);
    Task<bool> ApproveSalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid approvedBy);
    Task<bool> RejectSalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid rejectedBy, RejectSalaryAdvanceDto dto);
    Task<bool> DisburseSalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid disbursedBy, DisburseSalaryAdvanceDto dto);

    // Loans
    Task<LoanDto?> GetLoanByIdAsync(Guid loanId, Guid organizationId);
    Task<LoanDto> CreateLoanRequestAsync(Guid organizationId, Guid employeeId, CreateLoanRequestDto dto);
    Task<PagedResult<LoanDto>> GetMyLoansAsync(Guid organizationId, Guid employeeId, LoanFilterDto filter);
    Task<LoanDto?> UpdateLoanStatusAsync(Guid organizationId, Guid approvedBy, UpdateLoanStatusDto dto);
    Task<PagedResult<LoanDto>> GetAllLoansAsync(Guid organizationId, LoanFilterDto filter);
    Task<LoanDto?> DisburseLoanAsync(Guid organizationId, Guid createdBy, DisburseLoanDto dto);
    Task<IEnumerable<MyActiveLoanDto>> GetMyActiveLoansAsync(Guid organizationId, Guid employeeId);
    Task<IEnumerable<MyActiveLoanDto>> GetMyPendingLoansAsync(Guid organizationId, Guid employeeId);
    Task<PagedResult<MyActiveLoanDto>> GetDisbursedLoansAsync(Guid organizationId, DisbursedLoanFilterDto filter);
    Task<PagedResult<MyActiveLoanDto>> GetDisbursedActiveLoansAsync(Guid organizationId, DisbursedLoanFilterDto filter);
    Task<PagedResult<RepaymentHistoryDto>> GetAllRepaymentsAsync(Guid organizationId, RepaymentFilterDto filter);
    Task<bool> AddLoanPaymentAsync(Guid organizationId, AddLoanPaymentDto dto);
    Task<bool> UpdateLoanPaymentAsync(Guid organizationId, UpdateLoanPaymentDto dto);
    Task<LoanPaymentDto?> GetLoanPaymentByIdAsync(Guid paymentId, Guid organizationId);

    // Personal Loan Operations
    Task<LoanDto?> GetLoanByIdAndEmployeeAsync(Guid loanId, Guid organizationId, Guid employeeId);
    Task<LoanDto?> UpdateLoanRequestAsync(Guid loanId, Guid organizationId, Guid employeeId, CreateLoanRequestDto dto);
    Task<bool> DeleteLoanRequestAsync(Guid loanId, Guid organizationId, Guid employeeId);
    Task<PagedResult<MyLoanPaymentDto>> GetLoanHistoryAsync(Guid organizationId, Guid employeeId, MyLoanHistoryFilterDto filter);
    Task<IEnumerable<LoanReferenceDto>> GetMyLoanReferencesAsync(Guid organizationId, Guid employeeId);

    // Overview
    Task<PayrollOverviewDto> GetPayrollOverviewAsync(Guid organizationId);
}
