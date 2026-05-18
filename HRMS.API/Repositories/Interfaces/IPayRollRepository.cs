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

    // Tax Regimes
    Task<IEnumerable<TaxRegimeDto>> GetAllTaxRegimesAsync(Guid organizationId);
    Task<IEnumerable<TaxRegimeDto>> GetActiveTaxRegimesAsync(Guid organizationId);
    Task<TaxRegimeDto?> GetTaxRegimeByIdAsync(Guid regimeId, Guid organizationId);
    Task<TaxRegimeDto> CreateTaxRegimeAsync(Guid organizationId, CreateTaxRegimeDto dto);
    Task<TaxRegimeDto?> UpdateTaxRegimeAsync(Guid regimeId, Guid organizationId, UpdateTaxRegimeDto dto);
    Task<bool> DeleteTaxRegimeAsync(Guid regimeId, Guid organizationId);

    // Tax Categories
    Task<IEnumerable<TaxCategoryDto>> GetAllTaxCategoriesAsync(Guid organizationId);
    Task<IEnumerable<TaxCategoryDto>> GetActiveTaxCategoriesAsync(Guid organizationId);
    Task<IEnumerable<TaxCategoryDto>> GetTaxCategoriesByRegimeIdAsync(Guid regimeId, Guid organizationId);
    Task<TaxCategoryDto?> GetTaxCategoryByIdAsync(Guid categoryId, Guid organizationId);
    Task<TaxCategoryDto> CreateTaxCategoryAsync(Guid organizationId, CreateTaxCategoryDto dto);
    Task<TaxCategoryDto?> UpdateTaxCategoryAsync(Guid categoryId, Guid organizationId, UpdateTaxCategoryDto dto);
    Task<bool> DeleteTaxCategoryAsync(Guid categoryId, Guid organizationId);

    // Tax Slabs
    Task<IEnumerable<TaxSlabDto>> GetAllTaxSlabsAsync(Guid organizationId);
    Task<IEnumerable<TaxSlabDto>> GetActiveTaxSlabsAsync(Guid organizationId);
    Task<TaxSlabDto?> GetTaxSlabByIdAsync(Guid slabId, Guid organizationId);
    Task<TaxSlabDto> CreateTaxSlabAsync(Guid organizationId, CreateTaxSlabDto dto);
    Task<TaxSlabDto?> UpdateTaxSlabAsync(Guid slabId, Guid organizationId, UpdateTaxSlabDto dto);
    Task<bool> DeleteTaxSlabAsync(Guid slabId, Guid organizationId);

    // Tax Component Rules
    Task<IEnumerable<TaxComponentRuleDto>> GetAllTaxComponentRulesAsync(Guid organizationId);
    Task<IEnumerable<TaxComponentRuleDto>> GetActiveTaxComponentRulesAsync(Guid organizationId);
    Task<TaxComponentRuleDto?> GetTaxComponentRuleByIdAsync(Guid componentRuleId, Guid organizationId);
    Task<TaxComponentRuleDto> CreateTaxComponentRuleAsync(Guid organizationId, CreateTaxComponentRuleDto dto);
    Task<TaxComponentRuleDto?> UpdateTaxComponentRuleAsync(Guid componentRuleId, Guid organizationId, UpdateTaxComponentRuleDto dto);
    Task<bool> DeleteTaxComponentRuleAsync(Guid componentRuleId, Guid organizationId);

    // Tax Transactions
    Task<IEnumerable<TaxTransactionDto>> GetAllTaxTransactionsAsync(Guid organizationId);
    Task<IEnumerable<TaxTransactionDto>> GetActiveTaxTransactionsAsync(Guid organizationId);
    Task<TaxTransactionDto?> GetTaxTransactionByIdAsync(Guid id, Guid organizationId);
    Task<TaxTransactionDto> CreateTaxTransactionAsync(Guid organizationId, CreateTaxTransactionDto dto);
    Task<TaxTransactionDto?> UpdateTaxTransactionAsync(Guid id, Guid organizationId, UpdateTaxTransactionDto dto);
    Task<bool> DeleteTaxTransactionAsync(Guid id, Guid organizationId);

    // Provident Fund (Employee Side)
    Task<ProvidentFundRequestDto> CreateProvidentFundEnrollmentAsync(Guid organizationId, Guid employeeId, CreateProvidentFundEnrollmentDto dto, decimal employerPct);
    Task<ProvidentFundRequestDto> UpdateProvidentFundEnrollmentRequestAsync(Guid requestId, Guid organizationId, Guid employeeId, CreateProvidentFundEnrollmentDto dto, decimal employerPct);
    Task<ProvidentFundRequestDto> UpdateProvidentFundPercentageAsync(Guid organizationId, Guid employeeId, UpdateProvidentFundPercentageDto dto);
    Task<ProvidentFundRequestDto> CreateProvidentFundWithdrawRequestAsync(Guid organizationId, Guid employeeId, CreateProvidentFundWithdrawRequestDto dto);
    Task<bool> DeleteProvidentFundRequestAsync(Guid requestId, Guid organizationId, Guid employeeId);
    Task<PagedResult<ProvidentFundRequestDto>> GetMyProvidentFundRequestsAsync(Guid organizationId, Guid employeeId, ProvidentFundRequestFilterDto filter);
    Task<ProvidentFundAccountDto?> GetMyActiveProvidentFundAsync(Guid organizationId, Guid employeeId);
    Task<PagedResult<ProvidentFundTransactionDto>> GetMyProvidentFundTransactionsAsync(Guid organizationId, Guid employeeId, ProvidentFundTransactionFilterDto filter);
    Task<PagedResult<AdminProvidentFundRequestDto>> GetAllProvidentFundRequestsAsync(Guid organizationId, AdminProvidentFundRequestFilterDto filter);
    Task<PagedResult<AdminProvidentFundAccountDto>> GetAllProvidentFundAccountsAsync(Guid organizationId, AdminProvidentFundAccountFilterDto filter);
    Task<PagedResult<AdminProvidentFundRepaymentDto>> GetAllProvidentFundRepaymentsAsync(Guid organizationId, AdminProvidentFundRepaymentFilterDto filter);
    Task<bool> ApproveProvidentFundRequestAsync(Guid organizationId, Guid approvedBy, ApproveProvidentFundRequestDto dto);
    Task<bool> RejectProvidentFundRequestAsync(Guid requestId, Guid organizationId, RejectProvidentFundRequestDto dto);
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

    // Social Security Config
    Task<IEnumerable<SocialSecurityJurisdictionDto>> GetSocialSecurityJurisdictionsAsync(Guid organizationId);
    Task<IEnumerable<SocialSecurityAuthorityDto>> GetSocialSecurityAuthoritiesAsync(Guid organizationId, Guid? jurisdictionId);
    Task<IEnumerable<SocialSecuritySchemeDto>> GetSocialSecuritySchemesAsync(Guid organizationId, Guid? jurisdictionId, Guid? authorityId);
    Task<IEnumerable<SocialSecurityRuleDto>> GetSocialSecurityRulesAsync(Guid organizationId, Guid? schemeId);
    Task<SocialSecurityJurisdictionDto> CreateSocialSecurityJurisdictionAsync(Guid organizationId, CreateSocialSecurityJurisdictionDto dto);
    Task<SocialSecurityJurisdictionDto?> UpdateSocialSecurityJurisdictionAsync(Guid id, Guid organizationId, UpdateSocialSecurityJurisdictionDto dto);
    Task<bool> DeleteSocialSecurityJurisdictionAsync(Guid id, Guid organizationId);
    Task<SocialSecurityAuthorityDto> CreateSocialSecurityAuthorityAsync(Guid organizationId, CreateSocialSecurityAuthorityDto dto);
    Task<SocialSecurityAuthorityDto?> UpdateSocialSecurityAuthorityAsync(Guid id, Guid organizationId, UpdateSocialSecurityAuthorityDto dto);
    Task<bool> DeleteSocialSecurityAuthorityAsync(Guid id, Guid organizationId);
    Task<SocialSecuritySchemeDto> CreateSocialSecuritySchemeAsync(Guid organizationId, CreateSocialSecuritySchemeDto dto);
    Task<SocialSecuritySchemeDto?> UpdateSocialSecuritySchemeAsync(Guid id, Guid organizationId, UpdateSocialSecuritySchemeDto dto);
    Task<bool> DeleteSocialSecuritySchemeAsync(Guid id, Guid organizationId);
    Task<SocialSecurityRuleDto> CreateSocialSecurityRuleAsync(Guid organizationId, CreateSocialSecurityRuleDto dto);
    Task<SocialSecurityRuleDto?> UpdateSocialSecurityRuleAsync(Guid id, Guid organizationId, UpdateSocialSecurityRuleDto dto);
    Task<bool> DeleteSocialSecurityRuleAsync(Guid id, Guid organizationId);
    Task<PagedResult<SocialSecurityConfigDto>> GetAllSocialSecurityConfigsAsync(Guid organizationId, SocialSecurityConfigFilterDto filter);
    Task<IEnumerable<SocialSecurityConfigDto>> GetActiveSocialSecurityConfigsAsync(Guid organizationId);
    Task<SocialSecurityConfigDto?> GetSocialSecurityConfigByIdAsync(Guid id, Guid organizationId);
    Task<SocialSecurityConfigDto> CreateSocialSecurityConfigAsync(Guid organizationId, CreateSocialSecurityConfigDto dto);
    Task<SocialSecurityConfigDto?> UpdateSocialSecurityConfigAsync(Guid id, Guid organizationId, UpdateSocialSecurityConfigDto dto);
    Task<bool> DeleteSocialSecurityConfigAsync(Guid id, Guid organizationId);

    // Social Security Transactions
    Task<PagedResult<SocialSecurityTransactionDto>> GetAllSocialSecurityTransactionsAsync(Guid organizationId, SocialSecurityTransactionFilterDto filter);
    Task<SocialSecurityTransactionDto?> GetSocialSecurityTransactionByIdAsync(Guid id, Guid organizationId);
    Task<SocialSecurityTransactionDto> CreateSocialSecurityTransactionAsync(Guid organizationId, CreateSocialSecurityTransactionDto dto);
    Task<SocialSecurityTransactionDto?> UpdateSocialSecurityTransactionAsync(Guid id, Guid organizationId, UpdateSocialSecurityTransactionDto dto);
    Task<bool> DeleteSocialSecurityTransactionAsync(Guid id, Guid organizationId);
    Task<PagedResult<SocialSecurityTransactionDto>> GetMySocialSecurityTransactionsAsync(Guid organizationId, Guid employeeId, MySocialSecurityTransactionFilterDto filter);

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
    Task<PayrollCalculationResultDto?> CalculatePayrollAsync(Guid organizationId, CalculatePayrollDto dto);
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

    // Gratuity Config
    Task<IEnumerable<GratuityConfigDto>> GetAllGratuityConfigsAsync(Guid organizationId);
    Task<IEnumerable<GratuityConfigDto>> GetActiveGratuityConfigsAsync(Guid organizationId);
    Task<GratuityConfigDto?> GetGratuityConfigByIdAsync(Guid id, Guid organizationId);
    Task<GratuityConfigDto> CreateGratuityConfigAsync(Guid organizationId, CreateGratuityConfigDto dto);
    Task<GratuityConfigDto?> UpdateGratuityConfigAsync(Guid id, Guid organizationId, UpdateGratuityConfigDto dto);
    Task<bool> DeleteGratuityConfigAsync(Guid id, Guid organizationId);

    // Gratuity Transactions
    Task<PagedResult<GratuityTransactionDto>> GetAllGratuityTransactionsAsync(Guid organizationId, GratuityTransactionFilterDto filter);
    Task<GratuityTransactionDto?> GetGratuityTransactionByIdAsync(Guid id, Guid organizationId);
    Task<GratuityTransactionDto> CreateGratuityTransactionAsync(Guid organizationId, Guid createdBy, CreateGratuityTransactionDto dto);
    Task<GratuityTransactionDto?> UpdateGratuityTransactionAsync(Guid id, Guid organizationId, UpdateGratuityTransactionDto dto);
    Task<bool> DeleteGratuityTransactionAsync(Guid id, Guid organizationId);
    Task<bool> ApproveGratuityTransactionAsync(Guid id, Guid organizationId, Guid approvedBy, ApproveGratuityDto dto);
    Task<bool> MarkGratuityPaidAsync(Guid id, Guid organizationId, Guid paidBy, MarkGratuityPaidDto dto);
    Task<GratuityTransactionDto?> GetMyGratuityStatusAsync(Guid employeeId, Guid organizationId);

    // Social Security — Employee lifecycle (enrollments, requests, claims, documents)
    Task<SocialSecurityEnrollmentDto?> GetMySocialSecurityEnrollmentAsync(Guid organizationId, Guid employeeId);
    Task<PagedResult<SocialSecurityEnrollmentRequestDto>> GetMySocialSecurityRequestsAsync(Guid organizationId, Guid employeeId, MySocialSecurityRequestFilterDto filter);
    Task<PagedResult<SocialSecurityEnrollmentRequestDto>> GetSocialSecurityEnrollmentRequestsAsync(Guid organizationId, SocialSecurityEnrollmentRequestFilterDto filter);
    Task<Guid?> CreateSocialSecurityEnrollmentRequestAsync(Guid organizationId, Guid employeeId, Guid requestedByUserId, CreateSocialSecurityEnrollmentRequestDto dto);
    Task<bool> ApproveSocialSecurityEnrollmentRequestAsync(Guid organizationId, Guid requestId, Guid approvedByUserId, ApproveSocialSecurityEnrollmentRequestDto dto);
    Task<bool> RejectSocialSecurityEnrollmentRequestAsync(Guid organizationId, Guid requestId, Guid rejectedByUserId, RejectSocialSecurityEnrollmentRequestDto dto);

    Task<PagedResult<SocialSecurityClaimDto>> GetMySocialSecurityClaimsAsync(Guid organizationId, Guid employeeId, MySocialSecurityClaimFilterDto filter);
    Task<PagedResult<SocialSecurityClaimDto>> GetSocialSecurityClaimsAsync(Guid organizationId, SocialSecurityClaimFilterDto filter);
    Task<Guid?> CreateMySocialSecurityClaimAsync(Guid organizationId, Guid employeeId, Guid createdByUserId, CreateSocialSecurityClaimDto dto);
    Task<bool> ApproveSocialSecurityClaimAsync(Guid organizationId, Guid claimId, Guid approvedByUserId, ApproveSocialSecurityClaimDto dto);
    Task<bool> RejectSocialSecurityClaimAsync(Guid organizationId, Guid claimId, Guid rejectedByUserId, RejectSocialSecurityClaimDto dto);
    Task<bool> MarkSocialSecurityClaimPaidAsync(Guid organizationId, Guid claimId, MarkSocialSecurityClaimPaidDto dto);

    Task<IEnumerable<SocialSecurityRequestDocumentDto>> GetSocialSecurityRequestDocumentsAsync(Guid organizationId, Guid requestId);
    Task<bool> VerifySocialSecurityRequestDocumentAsync(Guid organizationId, Guid documentId, Guid verifiedByUserId, VerifySocialSecurityRequestDocumentDto dto);
    Task<bool> DeleteSocialSecurityRequestDocumentAsync(Guid organizationId, Guid documentId, Guid employeeId);
    Task<PagedResult<PayrollResultItemDto>> GetPayrollResultsAsync(Guid organizationId, PayrollResultsFilterDto filter);
    Task<PagedResult<CompliancePayslipRowDto>> GetCompliancePayslipsAsync(Guid organizationId, CompliancePayslipFilterDto filter);
    Task<CompliancePayslipStatsDto> GetCompliancePayslipStatsAsync(Guid organizationId, CompliancePayslipFilterDto filter);
    Task<PayrollResultItemDto?> GetPayrollResultByResultIdAsync(Guid organizationId, Guid resultId);
    Task<PayrollResultItemDto?> GetPayrollResultForEmployeePeriodAsync(Guid organizationId, Guid periodId, Guid employeeId);
    Task<string?> GetPayslipUrlByResultIdAsync(Guid organizationId, Guid resultId);
    Task UpsertPayslipUrlAsync(Guid organizationId, Guid resultId, Guid employeeId, string payslipUrl);
    Task<IReadOnlyList<PendingPayslipForSendDto>> GetPendingPayslipsForBulkSendAsync(
        Guid organizationId,
        Guid payrollPeriodId,
        IReadOnlyList<Guid> employeeIds);
    Task UpdatePayslipAfterSendAsync(
        Guid payslipId,
        string? payslipUploadUrl,
        string mailsentstatus,
        DateTime? mailsentat,
        Guid mailsentby);
    Task<PagedResult<MyPayslipDto>> GetMyPayslipsAsync(Guid organizationId, Guid employeeId, MyPayslipFilterDto filter);
}
