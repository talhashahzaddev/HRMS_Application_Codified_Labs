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
    Task<ExpenseDto?> GetExpenseByIdAsync(Guid expenseId);
    Task<ExpenseDto> CreateExpenseAsync(CreateExpenseRequest request, Guid organizationId, Guid employeeId, bool isSuperAdmin, Guid? approvedByEmployeeId);
    Task<ExpenseDto?> UpdateExpenseAsync(Guid expenseId, UpdateExpenseRequest request, Guid? approvedByEmployeeId);
    Task<bool> DeleteExpenseAsync(Guid expenseId);

    // RecurringExpenses
    Task<IEnumerable<RecurringExpenseDto>> GetRecurringExpensesAsync(Guid organizationId, Guid? employeeId = null, string? status = null);
    Task<RecurringExpenseDto?> GetRecurringExpenseByIdAsync(Guid recurringExpenseId);
    Task<RecurringExpenseDto> CreateRecurringExpenseAsync(CreateRecurringExpenseRequest request, Guid organizationId, Guid employeeId, bool isSuperAdmin, Guid? approvedByEmployeeId);
    Task<RecurringExpenseDto?> UpdateRecurringExpenseAsync(Guid recurringExpenseId, UpdateRecurringExpenseRequest request, Guid? approvedByEmployeeId);
    Task<bool> DeleteRecurringExpenseAsync(Guid recurringExpenseId);
}
