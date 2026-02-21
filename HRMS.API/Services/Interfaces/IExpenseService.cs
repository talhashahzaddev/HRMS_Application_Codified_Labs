using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IExpenseService
{
    // ExpenseCategories
    Task<IEnumerable<ExpenseCategoryDto>> GetExpenseCategoriesAsync(Guid organizationId);
    Task<ExpenseCategoryDto?> GetExpenseCategoryByIdAsync(Guid categoryId);
    Task<ExpenseCategoryDto> CreateExpenseCategoryAsync(Guid organizationId, CreateExpenseCategoryRequest request);
    Task<ExpenseCategoryDto?> UpdateExpenseCategoryAsync(Guid categoryId, UpdateExpenseCategoryRequest request);
    Task<bool> DeleteExpenseCategoryAsync(Guid categoryId);

    // Expenses
    Task<IEnumerable<ExpenseDto>> GetExpensesAsync(Guid organizationId, Guid? employeeId = null, string? status = null);
    Task<PagedResult<ExpenseDto>> GetExpensesPagedAsync(Guid organizationId, Guid? employeeId, string? status, int pageNumber, int pageSize);
    Task<ExpenseDto?> GetExpenseByIdAsync(Guid expenseId);
    Task<ExpenseDto> CreateExpenseAsync(CreateExpenseRequest request, Guid organizationId, Guid employeeId, bool isSuperAdmin, Guid? approvedByEmployeeId);
    Task<ExpenseDto?> UpdateExpenseAsync(Guid expenseId, UpdateExpenseRequest request, Guid? approvedByEmployeeId);
    Task<bool> DeleteExpenseAsync(Guid expenseId);

    // RecurringExpenses
    Task<IEnumerable<RecurringExpenseDto>> GetRecurringExpensesAsync(Guid organizationId, Guid? employeeId = null, string? status = null);
    Task<PagedResult<RecurringExpenseDto>> GetRecurringExpensesPagedAsync(Guid organizationId, Guid? employeeId, string? status, int pageNumber, int pageSize);
    Task<RecurringExpenseDto?> GetRecurringExpenseByIdAsync(Guid recurringExpenseId);
    Task<RecurringExpenseDto> CreateRecurringExpenseAsync(CreateRecurringExpenseRequest request, Guid organizationId, Guid employeeId, bool isSuperAdmin, Guid? approvedByEmployeeId);
    Task<RecurringExpenseDto?> UpdateRecurringExpenseAsync(Guid recurringExpenseId, UpdateRecurringExpenseRequest request, Guid? approvedByEmployeeId);
    Task<bool> DeleteRecurringExpenseAsync(Guid recurringExpenseId);

    /// <summary>
    /// Gets category-wise cost for pie report (Approved only, by created date range, for organization).
    /// </summary>
    Task<IEnumerable<ExpensePieReportItemDto>> GetExpensePieReportAsync(
        Guid organizationId,
        DateTime? fromDate,
        DateTime? toDate,
        bool expense,
        bool recurring);

    /// <summary>
    /// Gets cost per month for a year (line chart): Approved only, by organization.
    /// </summary>
    Task<IEnumerable<ExpenseLineChartItemDto>> GetExpenseLineChartAsync(
        Guid organizationId,
        int year,
        bool expense,
        bool recurring);

    /// <summary>
    /// Gets cost per year for a range of years (bar chart): Approved only, by organization.
    /// </summary>
    Task<IEnumerable<ExpenseBarChartItemDto>> GetExpenseBarChartAsync(
        Guid organizationId,
        int fromYear,
        int toYear,
        bool expense,
        bool recurring);
}
