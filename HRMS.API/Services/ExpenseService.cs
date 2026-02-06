using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(IExpenseRepository expenseRepository, ILogger<ExpenseService> logger)
    {
        _expenseRepository = expenseRepository;
        _logger = logger;
    }

    // ==================== ExpenseCategories ====================
    public async Task<IEnumerable<ExpenseCategoryDto>> GetExpenseCategoriesAsync(Guid organizationId)
    {
        return await _expenseRepository.GetExpenseCategoriesAsync(organizationId);
    }

    public async Task<ExpenseCategoryDto?> GetExpenseCategoryByIdAsync(Guid categoryId)
    {
        return await _expenseRepository.GetExpenseCategoryByIdAsync(categoryId);
    }

    public async Task<ExpenseCategoryDto> CreateExpenseCategoryAsync(Guid organizationId, CreateExpenseCategoryRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Category name is required", nameof(request));

        var categoryId = await _expenseRepository.CreateExpenseCategoryAsync(organizationId, request.Name.Trim());
        var created = await _expenseRepository.GetExpenseCategoryByIdAsync(categoryId);
        if (created == null)
            throw new InvalidOperationException("Failed to retrieve created expense category");
        return created;
    }

    public async Task<ExpenseCategoryDto?> UpdateExpenseCategoryAsync(Guid categoryId, UpdateExpenseCategoryRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Category name is required", nameof(request));

        var updated = await _expenseRepository.UpdateExpenseCategoryAsync(categoryId, request.Name.Trim());
        if (!updated)
            return null;
        return await _expenseRepository.GetExpenseCategoryByIdAsync(categoryId);
    }

    public async Task<bool> DeleteExpenseCategoryAsync(Guid categoryId)
    {
        return await _expenseRepository.DeleteExpenseCategoryAsync(categoryId);
    }

    // ==================== Expenses ====================
    public async Task<IEnumerable<ExpenseDto>> GetExpensesAsync(Guid organizationId, Guid? employeeId = null, string? status = null)
    {
        return await _expenseRepository.GetExpensesAsync(organizationId, employeeId, status);
    }

    public async Task<PagedResult<ExpenseDto>> GetExpensesPagedAsync(Guid organizationId, Guid? employeeId, string? status, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        return await _expenseRepository.GetExpensesPagedAsync(organizationId, employeeId, status, pageNumber, pageSize);
    }

    public async Task<ExpenseDto?> GetExpenseByIdAsync(Guid expenseId)
    {
        return await _expenseRepository.GetExpenseByIdAsync(expenseId);
    }

    public async Task<ExpenseDto> CreateExpenseAsync(CreateExpenseRequest request, Guid organizationId, Guid employeeId, bool isSuperAdmin, Guid? approvedByEmployeeId)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        if (employeeId == Guid.Empty)
            throw new ArgumentException("Employee ID is required", nameof(employeeId));
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required", nameof(request));
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(request));

        var status = isSuperAdmin ? "Approved" : "Pending";
        var approvedBy = isSuperAdmin ? approvedByEmployeeId : null;

        var entity = new Expense
        {
            OrganizationId = organizationId,
            EmployeeId = employeeId,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Amount = request.Amount,
            BillDate = request.BillDate.Date,
            PaymentMethod = request.PaymentMethod?.Trim(),
            Merchant = request.Merchant?.Trim(),
            ReceiptUrl = request.ReceiptUrl?.Trim(),
            Status = status,
            ApprovedBy = approvedBy
        };

        var expenseId = await _expenseRepository.CreateExpenseAsync(entity);
        var created = await _expenseRepository.GetExpenseByIdAsync(expenseId);
        if (created == null)
            throw new InvalidOperationException("Failed to retrieve created expense");
        return created;
    }

    public async Task<ExpenseDto?> UpdateExpenseAsync(Guid expenseId, UpdateExpenseRequest request, Guid? approvedByEmployeeId)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var existing = await _expenseRepository.GetExpenseByIdAsync(expenseId);
        if (existing == null)
            return null;

        var entity = new Expense
        {
            ExpenseId = expenseId,
            OrganizationId = existing.OrganizationId,
            EmployeeId = existing.EmployeeId,
            CategoryId = request.CategoryId ?? existing.CategoryId,
            Title = request.Title ?? existing.Title,
            Description = request.Description ?? existing.Description,
            Amount = request.Amount ?? existing.Amount,
            BillDate = request.BillDate ?? existing.BillDate,
            PaymentMethod = request.PaymentMethod ?? existing.PaymentMethod,
            Merchant = request.Merchant ?? existing.Merchant,
            ReceiptUrl = request.ReceiptUrl ?? existing.ReceiptUrl,
            Status = request.Status ?? existing.Status,
            ApprovedBy = request.Status == "Approved" ? approvedByEmployeeId : existing.ApprovedBy
        };

        var updated = await _expenseRepository.UpdateExpenseAsync(entity);
        if (!updated)
            return null;
        return await _expenseRepository.GetExpenseByIdAsync(expenseId);
    }

    public async Task<bool> DeleteExpenseAsync(Guid expenseId)
    {
        return await _expenseRepository.DeleteExpenseAsync(expenseId);
    }

    // ==================== RecurringExpenses ====================
    public async Task<IEnumerable<RecurringExpenseDto>> GetRecurringExpensesAsync(Guid organizationId, Guid? employeeId = null, string? status = null)
    {
        return await _expenseRepository.GetRecurringExpensesAsync(organizationId, employeeId, status);
    }

    public async Task<RecurringExpenseDto?> GetRecurringExpenseByIdAsync(Guid recurringExpenseId)
    {
        return await _expenseRepository.GetRecurringExpenseByIdAsync(recurringExpenseId);
    }

    public async Task<RecurringExpenseDto> CreateRecurringExpenseAsync(CreateRecurringExpenseRequest request, Guid organizationId, Guid employeeId, bool isSuperAdmin, Guid? approvedByEmployeeId)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        if (employeeId == Guid.Empty)
            throw new ArgumentException("Employee ID is required", nameof(employeeId));
        if (request.CategoryId == Guid.Empty)
            throw new ArgumentException("Category ID is required", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required", nameof(request));
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Rotation))
            throw new ArgumentException("Rotation is required", nameof(request));

        var status = isSuperAdmin ? "Approved" : "Pending";
        var approvedBy = isSuperAdmin ? approvedByEmployeeId : null;

        var entity = new RecurringExpense
        {
            OrganizationId = organizationId,
            EmployeeId = employeeId,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Amount = request.Amount,
            Rotation = request.Rotation.Trim(),
            DayOfMonth = request.DayOfMonth?.Trim(),
            Status = status,
            ApprovedBy = approvedBy,
            PaymentMethod = request.PaymentMethod?.Trim(),
            Merchant = request.Merchant?.Trim(),
            ReceiptUrl = request.ReceiptUrl?.Trim()
        };

        var recurringExpenseId = await _expenseRepository.CreateRecurringExpenseAsync(entity);
        var created = await _expenseRepository.GetRecurringExpenseByIdAsync(recurringExpenseId);
        if (created == null)
            throw new InvalidOperationException("Failed to retrieve created recurring expense");
        return created;
    }

    public async Task<RecurringExpenseDto?> UpdateRecurringExpenseAsync(Guid recurringExpenseId, UpdateRecurringExpenseRequest request, Guid? approvedByEmployeeId)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var existing = await _expenseRepository.GetRecurringExpenseByIdAsync(recurringExpenseId);
        if (existing == null)
            return null;

        var entity = new RecurringExpense
        {
            RecurringExpenseId = recurringExpenseId,
            OrganizationId = existing.OrganizationId,
            EmployeeId = existing.EmployeeId,
            CategoryId = request.CategoryId ?? existing.CategoryId,
            Title = request.Title ?? existing.Title,
            Description = request.Description ?? existing.Description,
            Amount = request.Amount ?? existing.Amount,
            Rotation = request.Rotation ?? existing.Rotation,
            DayOfMonth = request.DayOfMonth ?? existing.DayOfMonth,
            Status = request.Status ?? existing.Status,
            ApprovedBy = request.Status == "Approved" ? approvedByEmployeeId : existing.ApprovedBy,
            PaymentMethod = request.PaymentMethod ?? existing.PaymentMethod,
            Merchant = request.Merchant ?? existing.Merchant,
            ReceiptUrl = request.ReceiptUrl ?? existing.ReceiptUrl
        };

        var updated = await _expenseRepository.UpdateRecurringExpenseAsync(entity);
        if (!updated)
            return null;
        return await _expenseRepository.GetRecurringExpenseByIdAsync(recurringExpenseId);
    }

    public async Task<bool> DeleteRecurringExpenseAsync(Guid recurringExpenseId)
    {
        return await _expenseRepository.DeleteRecurringExpenseAsync(recurringExpenseId);
    }
}
