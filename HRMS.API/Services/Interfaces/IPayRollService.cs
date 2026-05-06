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

    // Bonus Rules
    Task<ServiceResponse<IEnumerable<BonusRuleDto>>> GetAllBonusRulesAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<BonusRuleDto>>> GetActiveBonusRulesAsync(Guid organizationId);
    Task<ServiceResponse<BonusRuleDto>> GetBonusRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<ServiceResponse<BonusRuleDto>> CreateBonusRuleAsync(Guid organizationId, CreateBonusRuleDto dto);
    Task<ServiceResponse<BonusRuleDto>> UpdateBonusRuleAsync(Guid ruleId, Guid organizationId, UpdateBonusRuleDto dto);
    Task<ServiceResponse<bool>> DeleteBonusRuleAsync(Guid ruleId, Guid organizationId);

    // Performance Pay
    Task<ServiceResponse<PagedResult<PerformancePayDto>>> GetAllPerformancePayAsync(Guid organizationId, PerformancePayFilterDto filter);
    Task<ServiceResponse<PerformancePayDto>> GetPerformancePayByIdAsync(Guid performancePayId, Guid organizationId);
    Task<ServiceResponse<PerformancePayDto>> CreatePerformancePayAsync(Guid organizationId, CreatePerformancePayDto dto);
    Task<ServiceResponse<PerformancePayDto>> UpdatePerformancePayAsync(Guid performancePayId, Guid organizationId, UpdatePerformancePayDto dto);
    Task<ServiceResponse<bool>> DeletePerformancePayAsync(Guid performancePayId, Guid organizationId);

    // Bonus Entries
    Task<ServiceResponse<PagedResult<BonusEntryDto>>> GetAllBonusEntriesAsync(Guid organizationId, BonusEntryFilterDto filter);
    Task<ServiceResponse<BonusEntryDto>> GetBonusEntryByIdAsync(Guid bonusId, Guid organizationId);
    Task<ServiceResponse<BonusEntryDto>> CreateBonusEntryAsync(Guid organizationId, CreateBonusEntryDto dto);
    Task<ServiceResponse<BonusEntryDto>> UpdateBonusEntryAsync(Guid bonusId, Guid organizationId, UpdateBonusEntryDto dto);
    Task<ServiceResponse<bool>> DeleteBonusEntryAsync(Guid bonusId, Guid organizationId);

    // Loan Rules
    Task<ServiceResponse<IEnumerable<LoanRuleDto>>> GetAllLoanRulesAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<LoanRuleDto>>> GetActiveLoanRulesAsync(Guid organizationId);
    Task<ServiceResponse<LoanRuleDto>> GetLoanRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<ServiceResponse<LoanRuleDto>> CreateLoanRuleAsync(Guid organizationId, CreateLoanRuleDto dto);
    Task<ServiceResponse<LoanRuleDto>> UpdateLoanRuleAsync(Guid ruleId, Guid organizationId, UpdateLoanRuleDto dto);
    Task<ServiceResponse<bool>> DeleteLoanRuleAsync(Guid ruleId, Guid organizationId);

    // Salary Advance Rules
    Task<ServiceResponse<IEnumerable<SalaryAdvanceRuleDto>>> GetAllSalaryAdvanceRulesAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<SalaryAdvanceRuleDto>>> GetActiveSalaryAdvanceRulesAsync(Guid organizationId);
    Task<ServiceResponse<SalaryAdvanceRuleDto>> GetSalaryAdvanceRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<ServiceResponse<SalaryAdvanceRuleDto>> CreateSalaryAdvanceRuleAsync(Guid organizationId, CreateSalaryAdvanceRuleDto dto);
    Task<ServiceResponse<SalaryAdvanceRuleDto>> UpdateSalaryAdvanceRuleAsync(Guid ruleId, Guid organizationId, UpdateSalaryAdvanceRuleDto dto);
    Task<ServiceResponse<bool>> DeleteSalaryAdvanceRuleAsync(Guid ruleId, Guid organizationId);

    // Provident Fund Rules
    Task<ServiceResponse<IEnumerable<ProvidentFundRuleDto>>> GetAllProvidentFundRulesAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<ProvidentFundRuleDto>>> GetActiveProvidentFundRulesAsync(Guid organizationId);
    Task<ServiceResponse<ProvidentFundRuleDto>> GetProvidentFundRuleByIdAsync(Guid ruleId, Guid organizationId);
    Task<ServiceResponse<ProvidentFundRuleDto>> CreateProvidentFundRuleAsync(Guid organizationId, CreateProvidentFundRuleDto dto);
    Task<ServiceResponse<ProvidentFundRuleDto>> UpdateProvidentFundRuleAsync(Guid ruleId, Guid organizationId, UpdateProvidentFundRuleDto dto);
    Task<ServiceResponse<bool>> DeleteProvidentFundRuleAsync(Guid ruleId, Guid organizationId);

    // Tax Regimes
    Task<ServiceResponse<IEnumerable<TaxRegimeDto>>> GetAllTaxRegimesAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<TaxRegimeDto>>> GetActiveTaxRegimesAsync(Guid organizationId);
    Task<ServiceResponse<TaxRegimeDto>> GetTaxRegimeByIdAsync(Guid regimeId, Guid organizationId);
    Task<ServiceResponse<TaxRegimeDto>> CreateTaxRegimeAsync(Guid organizationId, CreateTaxRegimeDto dto);
    Task<ServiceResponse<TaxRegimeDto>> UpdateTaxRegimeAsync(Guid regimeId, Guid organizationId, UpdateTaxRegimeDto dto);
    Task<ServiceResponse<bool>> DeleteTaxRegimeAsync(Guid regimeId, Guid organizationId);

    // Tax Categories
    Task<ServiceResponse<IEnumerable<TaxCategoryDto>>> GetAllTaxCategoriesAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<TaxCategoryDto>>> GetActiveTaxCategoriesAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<TaxCategoryDto>>> GetTaxCategoriesByRegimeIdAsync(Guid regimeId, Guid organizationId);
    Task<ServiceResponse<TaxCategoryDto>> GetTaxCategoryByIdAsync(Guid categoryId, Guid organizationId);
    Task<ServiceResponse<TaxCategoryDto>> CreateTaxCategoryAsync(Guid organizationId, CreateTaxCategoryDto dto);
    Task<ServiceResponse<TaxCategoryDto>> UpdateTaxCategoryAsync(Guid categoryId, Guid organizationId, UpdateTaxCategoryDto dto);
    Task<ServiceResponse<bool>> DeleteTaxCategoryAsync(Guid categoryId, Guid organizationId);

    // Tax Slabs
    Task<ServiceResponse<IEnumerable<TaxSlabDto>>> GetAllTaxSlabsAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<TaxSlabDto>>> GetActiveTaxSlabsAsync(Guid organizationId);
    Task<ServiceResponse<TaxSlabDto>> GetTaxSlabByIdAsync(Guid slabId, Guid organizationId);
    Task<ServiceResponse<TaxSlabDto>> CreateTaxSlabAsync(Guid organizationId, CreateTaxSlabDto dto);
    Task<ServiceResponse<TaxSlabDto>> UpdateTaxSlabAsync(Guid slabId, Guid organizationId, UpdateTaxSlabDto dto);
    Task<ServiceResponse<bool>> DeleteTaxSlabAsync(Guid slabId, Guid organizationId);

    // Tax Component Rules
    Task<ServiceResponse<IEnumerable<TaxComponentRuleDto>>> GetAllTaxComponentRulesAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<TaxComponentRuleDto>>> GetActiveTaxComponentRulesAsync(Guid organizationId);
    Task<ServiceResponse<TaxComponentRuleDto>> GetTaxComponentRuleByIdAsync(Guid componentRuleId, Guid organizationId);
    Task<ServiceResponse<TaxComponentRuleDto>> CreateTaxComponentRuleAsync(Guid organizationId, CreateTaxComponentRuleDto dto);
    Task<ServiceResponse<TaxComponentRuleDto>> UpdateTaxComponentRuleAsync(Guid componentRuleId, Guid organizationId, UpdateTaxComponentRuleDto dto);
    Task<ServiceResponse<bool>> DeleteTaxComponentRuleAsync(Guid componentRuleId, Guid organizationId);

    // Tax Transactions
    Task<ServiceResponse<IEnumerable<TaxTransactionDto>>> GetAllTaxTransactionsAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<TaxTransactionDto>>> GetActiveTaxTransactionsAsync(Guid organizationId);
    Task<ServiceResponse<TaxTransactionDto>> GetTaxTransactionByIdAsync(Guid id, Guid organizationId);
    Task<ServiceResponse<TaxTransactionDto>> CreateTaxTransactionAsync(Guid organizationId, CreateTaxTransactionDto dto);
    Task<ServiceResponse<TaxTransactionDto>> UpdateTaxTransactionAsync(Guid id, Guid organizationId, UpdateTaxTransactionDto dto);
    Task<ServiceResponse<bool>> DeleteTaxTransactionAsync(Guid id, Guid organizationId);

    // Provident Fund (Employee Side)
    Task<ServiceResponse<ProvidentFundRequestDto>> CreateProvidentFundEnrollmentAsync(Guid organizationId, Guid employeeId, CreateProvidentFundEnrollmentDto dto);
    Task<ServiceResponse<ProvidentFundRequestDto>> UpdateProvidentFundEnrollmentRequestAsync(Guid requestId, Guid organizationId, Guid employeeId, CreateProvidentFundEnrollmentDto dto);
    Task<ServiceResponse<ProvidentFundRequestDto>> UpdateProvidentFundPercentageAsync(Guid organizationId, Guid employeeId, UpdateProvidentFundPercentageDto dto);
    Task<ServiceResponse<ProvidentFundRequestDto>> CreateProvidentFundWithdrawRequestAsync(Guid organizationId, Guid employeeId, CreateProvidentFundWithdrawRequestDto dto);
    Task<ServiceResponse<bool>> DeleteProvidentFundRequestAsync(Guid requestId, Guid organizationId, Guid employeeId);
    Task<ServiceResponse<PagedResult<ProvidentFundRequestDto>>> GetMyProvidentFundRequestsAsync(Guid organizationId, Guid employeeId, ProvidentFundRequestFilterDto filter);
    Task<ServiceResponse<ProvidentFundAccountDto>> GetMyActiveProvidentFundAsync(Guid organizationId, Guid employeeId);
    Task<ServiceResponse<PagedResult<ProvidentFundTransactionDto>>> GetMyProvidentFundTransactionsAsync(Guid organizationId, Guid employeeId, ProvidentFundTransactionFilterDto filter);
    Task<ServiceResponse<PagedResult<AdminProvidentFundRequestDto>>> GetAllProvidentFundRequestsAsync(Guid organizationId, AdminProvidentFundRequestFilterDto filter);
    Task<ServiceResponse<PagedResult<AdminProvidentFundAccountDto>>> GetAllProvidentFundAccountsAsync(Guid organizationId, AdminProvidentFundAccountFilterDto filter);
    Task<ServiceResponse<PagedResult<AdminProvidentFundRepaymentDto>>> GetAllProvidentFundRepaymentsAsync(Guid organizationId, AdminProvidentFundRepaymentFilterDto filter);
    Task<ServiceResponse<bool>> ApproveProvidentFundRequestAsync(Guid organizationId, Guid approvedBy, ApproveProvidentFundRequestDto dto);
    Task<ServiceResponse<bool>> RejectProvidentFundRequestAsync(Guid requestId, Guid organizationId, RejectProvidentFundRequestDto dto);
    Task<ServiceResponse<bool>> AddProvidentFundMonthlyTransactionAsync(Guid organizationId, CreateProvidentFundMonthlyTransactionDto dto);
    Task<ServiceResponse<ProvidentFundEnrollmentDto>> ManualProvidentFundEnrollmentAsync(Guid organizationId, ManualProvidentFundEnrollmentDto dto);

    // Salary Advance
    Task<ServiceResponse<SalaryAdvanceDto>> CreateSalaryAdvanceAsync(Guid organizationId, Guid employeeId, CreateSalaryAdvanceDto dto);
    Task<ServiceResponse<SalaryAdvanceDto>> UpdateMySalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid employeeId, CreateSalaryAdvanceDto dto);
    Task<ServiceResponse<bool>> CancelMySalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid employeeId);
    Task<ServiceResponse<PagedResult<MySalaryAdvanceDto>>> GetMySalaryAdvancesAsync(Guid organizationId, Guid employeeId, SalaryAdvanceFilterDto filter);
    Task<ServiceResponse<MySalaryAdvanceDto>> GetMySalaryAdvanceByIdAsync(Guid advanceId, Guid organizationId, Guid employeeId);
    Task<ServiceResponse<PagedResult<AdminSalaryAdvanceDto>>> GetAllSalaryAdvanceRequestsAsync(Guid organizationId, AdminSalaryAdvanceFilterDto filter);
    Task<ServiceResponse<AdminSalaryAdvanceDto>> GetSalaryAdvanceRequestByIdAsync(Guid advanceId, Guid organizationId);
    Task<ServiceResponse<bool>> ApproveSalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid approvedBy);
    Task<ServiceResponse<bool>> RejectSalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid rejectedBy, RejectSalaryAdvanceDto dto);
    Task<ServiceResponse<bool>> DisburseSalaryAdvanceAsync(Guid advanceId, Guid organizationId, Guid disbursedBy, DisburseSalaryAdvanceDto dto);

    // Social Security Config
    Task<ServiceResponse<IEnumerable<SocialSecurityJurisdictionDto>>> GetSocialSecurityJurisdictionsAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<SocialSecurityAuthorityDto>>> GetSocialSecurityAuthoritiesAsync(Guid organizationId, Guid? jurisdictionId);
    Task<ServiceResponse<IEnumerable<SocialSecuritySchemeDto>>> GetSocialSecuritySchemesAsync(Guid organizationId, Guid? jurisdictionId, Guid? authorityId);
    Task<ServiceResponse<IEnumerable<SocialSecurityRuleDto>>> GetSocialSecurityRulesAsync(Guid organizationId, Guid? schemeId);
    Task<ServiceResponse<SocialSecurityJurisdictionDto>> CreateSocialSecurityJurisdictionAsync(Guid organizationId, CreateSocialSecurityJurisdictionDto dto);
    Task<ServiceResponse<SocialSecurityJurisdictionDto>> UpdateSocialSecurityJurisdictionAsync(Guid id, Guid organizationId, UpdateSocialSecurityJurisdictionDto dto);
    Task<ServiceResponse<bool>> DeleteSocialSecurityJurisdictionAsync(Guid id, Guid organizationId);
    Task<ServiceResponse<SocialSecurityAuthorityDto>> CreateSocialSecurityAuthorityAsync(Guid organizationId, CreateSocialSecurityAuthorityDto dto);
    Task<ServiceResponse<SocialSecurityAuthorityDto>> UpdateSocialSecurityAuthorityAsync(Guid id, Guid organizationId, UpdateSocialSecurityAuthorityDto dto);
    Task<ServiceResponse<bool>> DeleteSocialSecurityAuthorityAsync(Guid id, Guid organizationId);
    Task<ServiceResponse<SocialSecuritySchemeDto>> CreateSocialSecuritySchemeAsync(Guid organizationId, CreateSocialSecuritySchemeDto dto);
    Task<ServiceResponse<SocialSecuritySchemeDto>> UpdateSocialSecuritySchemeAsync(Guid id, Guid organizationId, UpdateSocialSecuritySchemeDto dto);
    Task<ServiceResponse<bool>> DeleteSocialSecuritySchemeAsync(Guid id, Guid organizationId);
    Task<ServiceResponse<SocialSecurityRuleDto>> CreateSocialSecurityRuleAsync(Guid organizationId, CreateSocialSecurityRuleDto dto);
    Task<ServiceResponse<SocialSecurityRuleDto>> UpdateSocialSecurityRuleAsync(Guid id, Guid organizationId, UpdateSocialSecurityRuleDto dto);
    Task<ServiceResponse<bool>> DeleteSocialSecurityRuleAsync(Guid id, Guid organizationId);
    Task<ServiceResponse<PagedResult<SocialSecurityConfigDto>>> GetAllSocialSecurityConfigsAsync(Guid organizationId, SocialSecurityConfigFilterDto filter);
    Task<ServiceResponse<IEnumerable<SocialSecurityConfigDto>>> GetActiveSocialSecurityConfigsAsync(Guid organizationId);
    Task<ServiceResponse<SocialSecurityConfigDto>> GetSocialSecurityConfigByIdAsync(Guid id, Guid organizationId);
    Task<ServiceResponse<SocialSecurityConfigDto>> CreateSocialSecurityConfigAsync(Guid organizationId, CreateSocialSecurityConfigDto dto);
    Task<ServiceResponse<SocialSecurityConfigDto>> UpdateSocialSecurityConfigAsync(Guid id, Guid organizationId, UpdateSocialSecurityConfigDto dto);
    Task<ServiceResponse<bool>> DeleteSocialSecurityConfigAsync(Guid id, Guid organizationId);

    // Social Security Transactions
    Task<ServiceResponse<PagedResult<SocialSecurityTransactionDto>>> GetAllSocialSecurityTransactionsAsync(Guid organizationId, SocialSecurityTransactionFilterDto filter);
    Task<ServiceResponse<SocialSecurityTransactionDto>> GetSocialSecurityTransactionByIdAsync(Guid id, Guid organizationId);
    Task<ServiceResponse<SocialSecurityTransactionDto>> CreateSocialSecurityTransactionAsync(Guid organizationId, CreateSocialSecurityTransactionDto dto);
    Task<ServiceResponse<SocialSecurityTransactionDto>> UpdateSocialSecurityTransactionAsync(Guid id, Guid organizationId, UpdateSocialSecurityTransactionDto dto);
    Task<ServiceResponse<bool>> DeleteSocialSecurityTransactionAsync(Guid id, Guid organizationId);
    Task<ServiceResponse<PagedResult<SocialSecurityTransactionDto>>> GetMySocialSecurityTransactionsAsync(Guid organizationId, Guid employeeId, MySocialSecurityTransactionFilterDto filter);


    // Loans
    Task<ServiceResponse<LoanDto>> GetLoanByIdAsync(Guid loanId, Guid organizationId);
    Task<ServiceResponse<LoanDto>> CreateLoanRequestAsync(Guid organizationId, Guid employeeId, CreateLoanRequestDto dto);
    Task<ServiceResponse<PagedResult<LoanDto>>> GetMyLoansAsync(Guid organizationId, Guid employeeId, LoanFilterDto filter);
    Task<ServiceResponse<LoanDto>> UpdateLoanStatusAsync(Guid organizationId, Guid approvedBy, UpdateLoanStatusDto dto);
    Task<ServiceResponse<PagedResult<LoanDto>>> GetAllLoansAsync(Guid organizationId, LoanFilterDto filter);
    Task<ServiceResponse<LoanDto>> DisburseLoanAsync(Guid organizationId, Guid createdBy, DisburseLoanDto dto);
    Task<ServiceResponse<IEnumerable<MyActiveLoanDto>>> GetMyActiveLoansAsync(Guid organizationId, Guid employeeId);
    Task<ServiceResponse<IEnumerable<MyActiveLoanDto>>> GetMyPendingLoansAsync(Guid organizationId, Guid employeeId);
    Task<ServiceResponse<PagedResult<MyActiveLoanDto>>> GetDisbursedLoansAsync(Guid organizationId, DisbursedLoanFilterDto filter);
    Task<ServiceResponse<PagedResult<MyActiveLoanDto>>> GetDisbursedActiveLoansAsync(Guid organizationId, DisbursedLoanFilterDto filter);
    Task<ServiceResponse<PagedResult<RepaymentHistoryDto>>> GetAllRepaymentsAsync(Guid organizationId, RepaymentFilterDto filter);
    Task<ServiceResponse<bool>> AddLoanPaymentAsync(Guid organizationId, AddLoanPaymentDto dto);
    Task<ServiceResponse<PayrollCalculationResultDto>> CalculatePayrollAsync(Guid organizationId, CalculatePayrollDto dto);
    Task<ServiceResponse<bool>> UpdateLoanPaymentAsync(Guid organizationId, UpdateLoanPaymentDto dto);
    Task<ServiceResponse<LoanPaymentDto>> GetLoanPaymentByIdAsync(Guid paymentId, Guid organizationId);

    // Personal Loan Operations
    Task<ServiceResponse<LoanDto>> GetLoanByIdAndEmployeeAsync(Guid loanId, Guid organizationId, Guid employeeId);
    Task<ServiceResponse<LoanDto>> UpdateLoanRequestAsync(Guid loanId, Guid organizationId, Guid employeeId, CreateLoanRequestDto dto);
    Task<ServiceResponse<bool>> DeleteLoanRequestAsync(Guid loanId, Guid organizationId, Guid employeeId);
    Task<ServiceResponse<PagedResult<MyLoanPaymentDto>>> GetLoanHistoryAsync(Guid organizationId, Guid employeeId, MyLoanHistoryFilterDto filter);
    Task<ServiceResponse<IEnumerable<LoanReferenceDto>>> GetMyLoanReferencesAsync(Guid organizationId, Guid employeeId);

    // Overview
    Task<ServiceResponse<PayrollOverviewDto>> GetPayrollOverviewAsync(Guid organizationId);

    // Gratuity Config
    Task<ServiceResponse<IEnumerable<GratuityConfigDto>>> GetAllGratuityConfigsAsync(Guid organizationId);
    Task<ServiceResponse<IEnumerable<GratuityConfigDto>>> GetActiveGratuityConfigsAsync(Guid organizationId);
    Task<ServiceResponse<GratuityConfigDto>> GetGratuityConfigByIdAsync(Guid id, Guid organizationId);
    Task<ServiceResponse<GratuityConfigDto>> CreateGratuityConfigAsync(Guid organizationId, CreateGratuityConfigDto dto);
    Task<ServiceResponse<GratuityConfigDto>> UpdateGratuityConfigAsync(Guid id, Guid organizationId, UpdateGratuityConfigDto dto);
    Task<ServiceResponse<bool>> DeleteGratuityConfigAsync(Guid id, Guid organizationId);

    // Gratuity Transactions
    Task<ServiceResponse<PagedResult<GratuityTransactionDto>>> GetAllGratuityTransactionsAsync(Guid organizationId, GratuityTransactionFilterDto filter);
    Task<ServiceResponse<GratuityTransactionDto>> GetGratuityTransactionByIdAsync(Guid id, Guid organizationId);
    Task<ServiceResponse<GratuityTransactionDto>> CreateGratuityTransactionAsync(Guid organizationId, Guid createdBy, CreateGratuityTransactionDto dto);
    Task<ServiceResponse<GratuityTransactionDto>> UpdateGratuityTransactionAsync(Guid id, Guid organizationId, UpdateGratuityTransactionDto dto);
    Task<ServiceResponse<bool>> DeleteGratuityTransactionAsync(Guid id, Guid organizationId);
    Task<ServiceResponse<bool>> ApproveGratuityTransactionAsync(Guid id, Guid organizationId, Guid approvedBy, ApproveGratuityDto dto);
    Task<ServiceResponse<bool>> MarkGratuityPaidAsync(Guid id, Guid organizationId, Guid paidBy, MarkGratuityPaidDto dto);
    Task<ServiceResponse<GratuityTransactionDto>> GetMyGratuityStatusAsync(Guid employeeId, Guid organizationId);

    // Social Security — Employee lifecycle
    Task<ServiceResponse<SocialSecurityEnrollmentDto>> GetMySocialSecurityEnrollmentAsync(Guid organizationId, Guid employeeId);
    Task<ServiceResponse<PagedResult<SocialSecurityEnrollmentRequestDto>>> GetMySocialSecurityRequestsAsync(Guid organizationId, Guid employeeId, MySocialSecurityRequestFilterDto filter);
    Task<ServiceResponse<PagedResult<SocialSecurityEnrollmentRequestDto>>> GetSocialSecurityEnrollmentRequestsAsync(Guid organizationId, SocialSecurityEnrollmentRequestFilterDto filter);
    Task<ServiceResponse<Guid>> CreateSocialSecurityEnrollmentRequestAsync(Guid organizationId, Guid employeeId, Guid requestedByUserId, CreateSocialSecurityEnrollmentRequestDto dto);
    Task<ServiceResponse<bool>> ApproveSocialSecurityEnrollmentRequestAsync(Guid organizationId, Guid requestId, Guid approvedByUserId, ApproveSocialSecurityEnrollmentRequestDto dto);
    Task<ServiceResponse<bool>> RejectSocialSecurityEnrollmentRequestAsync(Guid organizationId, Guid requestId, Guid rejectedByUserId, RejectSocialSecurityEnrollmentRequestDto dto);

    Task<ServiceResponse<PagedResult<SocialSecurityClaimDto>>> GetMySocialSecurityClaimsAsync(Guid organizationId, Guid employeeId, MySocialSecurityClaimFilterDto filter);
    Task<ServiceResponse<PagedResult<SocialSecurityClaimDto>>> GetSocialSecurityClaimsAsync(Guid organizationId, SocialSecurityClaimFilterDto filter);
    Task<ServiceResponse<Guid>> CreateMySocialSecurityClaimAsync(Guid organizationId, Guid employeeId, Guid createdByUserId, CreateSocialSecurityClaimDto dto);
    Task<ServiceResponse<bool>> ApproveSocialSecurityClaimAsync(Guid organizationId, Guid claimId, Guid approvedByUserId, ApproveSocialSecurityClaimDto dto);
    Task<ServiceResponse<bool>> RejectSocialSecurityClaimAsync(Guid organizationId, Guid claimId, Guid rejectedByUserId, RejectSocialSecurityClaimDto dto);
    Task<ServiceResponse<bool>> MarkSocialSecurityClaimPaidAsync(Guid organizationId, Guid claimId, MarkSocialSecurityClaimPaidDto dto);

    Task<ServiceResponse<IEnumerable<SocialSecurityRequestDocumentDto>>> GetSocialSecurityRequestDocumentsAsync(Guid organizationId, Guid requestId);
    Task<ServiceResponse<bool>> VerifySocialSecurityRequestDocumentAsync(Guid organizationId, Guid documentId, Guid verifiedByUserId, VerifySocialSecurityRequestDocumentDto dto);
    Task<ServiceResponse<bool>> DeleteSocialSecurityRequestDocumentAsync(Guid organizationId, Guid documentId, Guid employeeId);
}
