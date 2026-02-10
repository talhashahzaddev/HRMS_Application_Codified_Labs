using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IExpenseRepository
{
    // ExpenseCategories
    Task<IEnumerable<ExpenseCategoryDto>> GetExpenseCategoriesAsync(Guid organizationId);
    Task<ExpenseCategoryDto?> GetExpenseCategoryByIdAsync(Guid categoryId);
    Task<Guid> CreateExpenseCategoryAsync(Guid organizationId, string name);
    Task<bool> UpdateExpenseCategoryAsync(Guid categoryId, string name);
    Task<bool> DeleteExpenseCategoryAsync(Guid categoryId);

    // Expenses
    Task<IEnumerable<ExpenseDto>> GetExpensesAsync(Guid organizationId, Guid? employeeId = null, string? status = null);
    Task<PagedResult<ExpenseDto>> GetExpensesPagedAsync(Guid organizationId, Guid? employeeId, string? status, int pageNumber, int pageSize);
    Task<ExpenseDto?> GetExpenseByIdAsync(Guid expenseId);
    Task<Guid> CreateExpenseAsync(Expense entity);
    Task<bool> UpdateExpenseAsync(Expense entity);
    Task<bool> DeleteExpenseAsync(Guid expenseId);

    // RecurringExpenses
    Task<IEnumerable<RecurringExpenseDto>> GetRecurringExpensesAsync(Guid organizationId, Guid? employeeId = null, string? status = null);
    Task<PagedResult<RecurringExpenseDto>> GetRecurringExpensesPagedAsync(Guid organizationId, Guid? employeeId, string? status, int pageNumber, int pageSize);
    Task<RecurringExpenseDto?> GetRecurringExpenseByIdAsync(Guid recurringExpenseId);
    Task<Guid> CreateRecurringExpenseAsync(RecurringExpense entity);
    Task<bool> UpdateRecurringExpenseAsync(RecurringExpense entity);
    Task<bool> DeleteRecurringExpenseAsync(Guid recurringExpenseId);

    /// <summary>
    /// Gets category-wise cost for pie report: Approved expenses/recurring in date range, by organization.
    /// </summary>
    Task<IEnumerable<ExpensePieReportItemDto>> GetExpensePieReportAsync(
        Guid organizationId,
        DateTime fromDate,
        DateTime toDate,
        bool includeExpense,
        bool includeRecurring);
}
