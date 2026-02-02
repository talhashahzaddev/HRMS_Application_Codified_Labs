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
    Task<ExpenseDto?> GetExpenseByIdAsync(Guid expenseId);
    Task<Guid> CreateExpenseAsync(Expense entity);
    Task<bool> UpdateExpenseAsync(Expense entity);
    Task<bool> DeleteExpenseAsync(Guid expenseId);

    // RecurringExpenses
    Task<IEnumerable<RecurringExpenseDto>> GetRecurringExpensesAsync(Guid organizationId, Guid? employeeId = null, string? status = null);
    Task<RecurringExpenseDto?> GetRecurringExpenseByIdAsync(Guid recurringExpenseId);
    Task<Guid> CreateRecurringExpenseAsync(RecurringExpense entity);
    Task<bool> UpdateRecurringExpenseAsync(RecurringExpense entity);
    Task<bool> DeleteRecurringExpenseAsync(Guid recurringExpenseId);
}
