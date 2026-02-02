using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class ExpenseRepository : BaseRepository, IExpenseRepository
{
    public ExpenseRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    // ==================== ExpenseCategories ====================
    public async Task<IEnumerable<ExpenseCategoryDto>> GetExpenseCategoriesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT categoryid AS CategoryId, organizationid AS OrganizationId, name AS Name, createdat AS CreatedAt
            FROM expense.expensecategories
            WHERE organizationid = @organizationId
            ORDER BY name";
        var list = await QueryAsync<ExpenseCategoryDto>(sql, new { organizationId });
        return list;
    }

    public async Task<ExpenseCategoryDto?> GetExpenseCategoryByIdAsync(Guid categoryId)
    {
        var sql = @"
            SELECT categoryid AS CategoryId, organizationid AS OrganizationId, name AS Name, createdat AS CreatedAt
            FROM expense.expensecategories
            WHERE categoryid = @categoryId";
        return await QuerySingleOrDefaultAsync<ExpenseCategoryDto>(sql, new { categoryId });
    }

    public async Task<Guid> CreateExpenseCategoryAsync(Guid organizationId, string name)
    {
        var sql = @"
            INSERT INTO expense.expensecategories (organizationid, name)
            VALUES (@organizationId, @name)
            RETURNING categoryid";
        return await ExecuteScalarAsync<Guid>(sql, new { organizationId, name });
    }

    public async Task<bool> UpdateExpenseCategoryAsync(Guid categoryId, string name)
    {
        var sql = @"
            UPDATE expense.expensecategories
            SET name = @name
            WHERE categoryid = @categoryId";
        var rows = await ExecuteAsync(sql, new { categoryId, name });
        return rows > 0;
    }

    public async Task<bool> DeleteExpenseCategoryAsync(Guid categoryId)
    {
        var sql = "DELETE FROM expense.expensecategories WHERE categoryid = @categoryId";
        var rows = await ExecuteAsync(sql, new { categoryId });
        return rows > 0;
    }

    // ==================== Expenses ====================
    public async Task<IEnumerable<ExpenseDto>> GetExpensesAsync(Guid organizationId, Guid? employeeId = null, string? status = null)
    {
        var sql = @"
            SELECT e.expenseid AS ExpenseId, e.organizationid AS OrganizationId, e.employeeid AS EmployeeId,
                   e.categoryid AS CategoryId, c.name AS CategoryName, e.title AS Title, e.description AS Description,
                   e.amount AS Amount, e.billdate AS BillDate, e.paymentmethod AS PaymentMethod, e.merchant AS Merchant,
                   e.receipturl AS ReceiptUrl, e.status AS Status, e.approvedby AS ApprovedBy,
                   e.createdat AS CreatedAt, e.updatedat AS UpdatedAt
            FROM expense.expenses e
            LEFT JOIN expense.expensecategories c ON e.categoryid = c.categoryid
            WHERE e.organizationid = @organizationId
              AND (@employeeId IS NULL OR e.employeeid = @employeeId)
              AND (@status IS NULL OR e.status = @status)
            ORDER BY e.createdat DESC";
        var results = await QueryAsync<ExpenseDto>(sql, new { organizationId, employeeId, status });
        return results;
    }

    public async Task<ExpenseDto?> GetExpenseByIdAsync(Guid expenseId)
    {
        var sql = @"
            SELECT e.expenseid AS ExpenseId, e.organizationid AS OrganizationId, e.employeeid AS EmployeeId,
                   e.categoryid AS CategoryId, c.name AS CategoryName, e.title AS Title, e.description AS Description,
                   e.amount AS Amount, e.billdate AS BillDate, e.paymentmethod AS PaymentMethod, e.merchant AS Merchant,
                   e.receipturl AS ReceiptUrl, e.status AS Status, e.approvedby AS ApprovedBy,
                   e.createdat AS CreatedAt, e.updatedat AS UpdatedAt
            FROM expense.expenses e
            LEFT JOIN expense.expensecategories c ON e.categoryid = c.categoryid
            WHERE e.expenseid = @expenseId";
        return await QuerySingleOrDefaultAsync<ExpenseDto>(sql, new { expenseId });
    }

    public async Task<Guid> CreateExpenseAsync(Expense entity)
    {
        var sql = @"
            INSERT INTO expense.expenses (organizationid, employeeid, categoryid, title, description, amount, billdate,
                paymentmethod, merchant, receipturl, status, approvedby)
            VALUES (@OrganizationId, @EmployeeId, @CategoryId, @Title, @Description, @Amount, @BillDate,
                @PaymentMethod, @Merchant, @ReceiptUrl, @Status, @ApprovedBy)
            RETURNING expenseid";
        return await ExecuteScalarAsync<Guid>(sql, new
        {
            entity.OrganizationId,
            entity.EmployeeId,
            entity.CategoryId,
            entity.Title,
            entity.Description,
            entity.Amount,
            entity.BillDate,
            entity.PaymentMethod,
            entity.Merchant,
            entity.ReceiptUrl,
            entity.Status,
            entity.ApprovedBy
        });
    }

    public async Task<bool> UpdateExpenseAsync(Expense entity)
    {
        var sql = @"
            UPDATE expense.expenses
            SET categoryid = @CategoryId, title = @Title, description = @Description, amount = @Amount,
                billdate = @BillDate, paymentmethod = @PaymentMethod, merchant = @Merchant, receipturl = @ReceiptUrl,
                status = @Status, approvedby = @ApprovedBy, updatedat = CURRENT_TIMESTAMP
            WHERE expenseid = @ExpenseId";
        var rows = await ExecuteAsync(sql, new
        {
            entity.ExpenseId,
            entity.CategoryId,
            entity.Title,
            entity.Description,
            entity.Amount,
            entity.BillDate,
            entity.PaymentMethod,
            entity.Merchant,
            entity.ReceiptUrl,
            entity.Status,
            entity.ApprovedBy
        });
        return rows > 0;
    }

    public async Task<bool> DeleteExpenseAsync(Guid expenseId)
    {
        var sql = "DELETE FROM expense.expenses WHERE expenseid = @expenseId";
        var rows = await ExecuteAsync(sql, new { expenseId });
        return rows > 0;
    }

    // ==================== RecurringExpenses ====================
    public async Task<IEnumerable<RecurringExpenseDto>> GetRecurringExpensesAsync(Guid organizationId, Guid? employeeId = null, string? status = null)
    {
        var sql = @"
            SELECT r.recurringexpenseid AS RecurringExpenseId, r.organizationid AS OrganizationId, r.employeeid AS EmployeeId,
                   r.categoryid AS CategoryId, c.name AS CategoryName, r.title AS Title, r.description AS Description,
                   r.amount AS Amount, r.rotation AS Rotation, r.dayofmonth AS DayOfMonth, r.status AS Status,
                   r.approvedby AS ApprovedBy, r.paymentmethod AS PaymentMethod, r.merchant AS Merchant, r.receipturl AS ReceiptUrl,
                   r.createdat AS CreatedAt, r.updatedat AS UpdatedAt
            FROM expense.recurringexpenses r
            LEFT JOIN expense.expensecategories c ON r.categoryid = c.categoryid
            WHERE r.organizationid = @organizationId
              AND (@employeeId IS NULL OR r.employeeid = @employeeId)
              AND (@status IS NULL OR r.status = @status)
            ORDER BY r.createdat DESC";
        var results = await QueryAsync<RecurringExpenseDto>(sql, new { organizationId, employeeId, status });
        return results;
    }

    public async Task<RecurringExpenseDto?> GetRecurringExpenseByIdAsync(Guid recurringExpenseId)
    {
        var sql = @"
            SELECT r.recurringexpenseid AS RecurringExpenseId, r.organizationid AS OrganizationId, r.employeeid AS EmployeeId,
                   r.categoryid AS CategoryId, c.name AS CategoryName, r.title AS Title, r.description AS Description,
                   r.amount AS Amount, r.rotation AS Rotation, r.dayofmonth AS DayOfMonth, r.status AS Status,
                   r.approvedby AS ApprovedBy, r.paymentmethod AS PaymentMethod, r.merchant AS Merchant, r.receipturl AS ReceiptUrl,
                   r.createdat AS CreatedAt, r.updatedat AS UpdatedAt
            FROM expense.recurringexpenses r
            LEFT JOIN expense.expensecategories c ON r.categoryid = c.categoryid
            WHERE r.recurringexpenseid = @recurringExpenseId";
        return await QuerySingleOrDefaultAsync<RecurringExpenseDto>(sql, new { recurringExpenseId });
    }

    public async Task<Guid> CreateRecurringExpenseAsync(RecurringExpense entity)
    {
        var sql = @"
            INSERT INTO expense.recurringexpenses (organizationid, employeeid, categoryid, title, description, amount,
                rotation, dayofmonth, status, approvedby, paymentmethod, merchant, receipturl)
            VALUES (@OrganizationId, @EmployeeId, @CategoryId, @Title, @Description, @Amount,
                @Rotation, @DayOfMonth, @Status, @ApprovedBy, @PaymentMethod, @Merchant, @ReceiptUrl)
            RETURNING recurringexpenseid";
        return await ExecuteScalarAsync<Guid>(sql, new
        {
            entity.OrganizationId,
            entity.EmployeeId,
            entity.CategoryId,
            entity.Title,
            entity.Description,
            entity.Amount,
            entity.Rotation,
            entity.DayOfMonth,
            entity.Status,
            entity.ApprovedBy,
            entity.PaymentMethod,
            entity.Merchant,
            entity.ReceiptUrl
        });
    }

    public async Task<bool> UpdateRecurringExpenseAsync(RecurringExpense entity)
    {
        var sql = @"
            UPDATE expense.recurringexpenses
            SET categoryid = @CategoryId, title = @Title, description = @Description, amount = @Amount,
                rotation = @Rotation, dayofmonth = @DayOfMonth, status = @Status, approvedby = @ApprovedBy,
                paymentmethod = @PaymentMethod, merchant = @Merchant, receipturl = @ReceiptUrl, updatedat = CURRENT_TIMESTAMP
            WHERE recurringexpenseid = @RecurringExpenseId";
        var rows = await ExecuteAsync(sql, new
        {
            entity.RecurringExpenseId,
            entity.CategoryId,
            entity.Title,
            entity.Description,
            entity.Amount,
            entity.Rotation,
            entity.DayOfMonth,
            entity.Status,
            entity.ApprovedBy,
            entity.PaymentMethod,
            entity.Merchant,
            entity.ReceiptUrl
        });
        return rows > 0;
    }

    public async Task<bool> DeleteRecurringExpenseAsync(Guid recurringExpenseId)
    {
        var sql = "DELETE FROM expense.recurringexpenses WHERE recurringexpenseid = @recurringExpenseId";
        var rows = await ExecuteAsync(sql, new { recurringExpenseId });
        return rows > 0;
    }
}
