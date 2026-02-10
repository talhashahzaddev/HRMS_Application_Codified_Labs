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
                   CONCAT(emp.firstname, ' ', emp.lastname) AS EmployeeName,
                   e.categoryid AS CategoryId, c.name AS CategoryName, e.title AS Title, e.description AS Description,
                   e.amount AS Amount, e.billdate AS BillDate, e.paymentmethod AS PaymentMethod, e.merchant AS Merchant,
                   e.receipturl AS ReceiptUrl, e.status AS Status, e.approvedby AS ApprovedBy,
                   CONCAT(approver.firstname, ' ', approver.lastname) AS ApprovedName,
                   e.createdat AS CreatedAt, e.updatedat AS UpdatedAt
            FROM expense.expenses e
            LEFT JOIN expense.expensecategories c ON e.categoryid = c.categoryid
            INNER JOIN employee.employees emp ON e.employeeid = emp.employeeid
            LEFT JOIN employee.employees approver ON e.approvedby = approver.employeeid
            WHERE e.organizationid = @organizationId
              AND (@employeeId IS NULL OR e.employeeid = @employeeId)
              AND (@status IS NULL OR e.status = @status)
            ORDER BY e.createdat DESC";
        var results = await QueryAsync<ExpenseDto>(sql, new { organizationId, employeeId, status });
        return results;
    }

    public async Task<PagedResult<ExpenseDto>> GetExpensesPagedAsync(Guid organizationId, Guid? employeeId, string? status, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var offset = (pageNumber - 1) * pageSize;

        var countSql = @"
            SELECT COUNT(*)
            FROM expense.expenses e
            WHERE e.organizationid = @organizationId
              AND (@employeeId IS NULL OR e.employeeid = @employeeId)
              AND (@status IS NULL OR e.status = @status)";
        var totalCount = await ExecuteScalarAsync<int>(countSql, new { organizationId, employeeId, status });

        var dataSql = @"
            SELECT e.expenseid AS ExpenseId, e.organizationid AS OrganizationId, e.employeeid AS EmployeeId,
                   CONCAT(emp.firstname, ' ', emp.lastname) AS EmployeeName,
                   e.categoryid AS CategoryId, c.name AS CategoryName, e.title AS Title, e.description AS Description,
                   e.amount AS Amount, e.billdate AS BillDate, e.paymentmethod AS PaymentMethod, e.merchant AS Merchant,
                   e.receipturl AS ReceiptUrl, e.status AS Status, e.approvedby AS ApprovedBy,
                   CONCAT(approver.firstname, ' ', approver.lastname) AS ApprovedName,
                   e.createdat AS CreatedAt, e.updatedat AS UpdatedAt
            FROM expense.expenses e
            LEFT JOIN expense.expensecategories c ON e.categoryid = c.categoryid
            INNER JOIN employee.employees emp ON e.employeeid = emp.employeeid
            LEFT JOIN employee.employees approver ON e.approvedby = approver.employeeid
            WHERE e.organizationid = @organizationId
              AND (@employeeId IS NULL OR e.employeeid = @employeeId)
              AND (@status IS NULL OR e.status = @status)
            ORDER BY e.createdat DESC
            LIMIT @pageSize OFFSET @offset";
        var data = await QueryAsync<ExpenseDto>(dataSql, new { organizationId, employeeId, status, pageSize, offset });
        return new PagedResult<ExpenseDto>
        {
            Data = data?.ToList() ?? new List<ExpenseDto>(),
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<ExpenseDto?> GetExpenseByIdAsync(Guid expenseId)
    {
        var sql = @"
            SELECT e.expenseid AS ExpenseId, e.organizationid AS OrganizationId, e.employeeid AS EmployeeId,
                   CONCAT(emp.firstname, ' ', emp.lastname) AS EmployeeName,
                   e.categoryid AS CategoryId, c.name AS CategoryName, e.title AS Title, e.description AS Description,
                   e.amount AS Amount, e.billdate AS BillDate, e.paymentmethod AS PaymentMethod, e.merchant AS Merchant,
                   e.receipturl AS ReceiptUrl, e.status AS Status, e.approvedby AS ApprovedBy,
                   CONCAT(approver.firstname, ' ', approver.lastname) AS ApprovedName,
                   e.createdat AS CreatedAt, e.updatedat AS UpdatedAt
            FROM expense.expenses e
            LEFT JOIN expense.expensecategories c ON e.categoryid = c.categoryid
            INNER JOIN employee.employees emp ON e.employeeid = emp.employeeid
            LEFT JOIN employee.employees approver ON e.approvedby = approver.employeeid
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
                   CONCAT(emp.firstname, ' ', emp.lastname) AS EmployeeName,
                   r.categoryid AS CategoryId, c.name AS CategoryName, r.title AS Title, r.description AS Description,
                   r.amount AS Amount, r.rotation AS Rotation, r.dayofmonth AS DayOfMonth, r.status AS Status,
                   r.approvedby AS ApprovedBy, CONCAT(approver.firstname, ' ', approver.lastname) AS ApprovedByName,
                   r.paymentmethod AS PaymentMethod, r.merchant AS Merchant, r.receipturl AS ReceiptUrl,
                   r.createdat AS CreatedAt, r.updatedat AS UpdatedAt
            FROM expense.recurringexpenses r
            LEFT JOIN expense.expensecategories c ON r.categoryid = c.categoryid
            INNER JOIN employee.employees emp ON r.employeeid = emp.employeeid
            LEFT JOIN employee.employees approver ON r.approvedby = approver.employeeid
            WHERE r.organizationid = @organizationId
              AND (@employeeId IS NULL OR r.employeeid = @employeeId)
              AND (@status IS NULL OR r.status = @status)
            ORDER BY r.createdat DESC";
        var results = await QueryAsync<RecurringExpenseDto>(sql, new { organizationId, employeeId, status });
        return results;
    }

    public async Task<PagedResult<RecurringExpenseDto>> GetRecurringExpensesPagedAsync(Guid organizationId, Guid? employeeId, string? status, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var offset = (pageNumber - 1) * pageSize;

        var countSql = @"
            SELECT COUNT(*)
            FROM expense.recurringexpenses r
            WHERE r.organizationid = @organizationId
              AND (@employeeId IS NULL OR r.employeeid = @employeeId)
              AND (@status IS NULL OR r.status = @status)";
        var totalCount = await ExecuteScalarAsync<int>(countSql, new { organizationId, employeeId, status });

        var dataSql = @"
            SELECT r.recurringexpenseid AS RecurringExpenseId, r.organizationid AS OrganizationId, r.employeeid AS EmployeeId,
                   CONCAT(emp.firstname, ' ', emp.lastname) AS EmployeeName,
                   r.categoryid AS CategoryId, c.name AS CategoryName, r.title AS Title, r.description AS Description,
                   r.amount AS Amount, r.rotation AS Rotation, r.dayofmonth AS DayOfMonth, r.status AS Status,
                   r.approvedby AS ApprovedBy, CONCAT(approver.firstname, ' ', approver.lastname) AS ApprovedByName,
                   r.paymentmethod AS PaymentMethod, r.merchant AS Merchant, r.receipturl AS ReceiptUrl,
                   r.createdat AS CreatedAt, r.updatedat AS UpdatedAt
            FROM expense.recurringexpenses r
            LEFT JOIN expense.expensecategories c ON r.categoryid = c.categoryid
            INNER JOIN employee.employees emp ON r.employeeid = emp.employeeid
            LEFT JOIN employee.employees approver ON r.approvedby = approver.employeeid
            WHERE r.organizationid = @organizationId
              AND (@employeeId IS NULL OR r.employeeid = @employeeId)
              AND (@status IS NULL OR r.status = @status)
            ORDER BY r.createdat DESC
            LIMIT @pageSize OFFSET @offset";
        var data = await QueryAsync<RecurringExpenseDto>(dataSql, new { organizationId, employeeId, status, pageSize, offset });
        return new PagedResult<RecurringExpenseDto>
        {
            Data = data?.ToList() ?? new List<RecurringExpenseDto>(),
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<RecurringExpenseDto?> GetRecurringExpenseByIdAsync(Guid recurringExpenseId)
    {
        var sql = @"
            SELECT r.recurringexpenseid AS RecurringExpenseId, r.organizationid AS OrganizationId, r.employeeid AS EmployeeId,
                   CONCAT(emp.firstname, ' ', emp.lastname) AS EmployeeName,
                   r.categoryid AS CategoryId, c.name AS CategoryName, r.title AS Title, r.description AS Description,
                   r.amount AS Amount, r.rotation AS Rotation, r.dayofmonth AS DayOfMonth, r.status AS Status,
                   r.approvedby AS ApprovedBy, CONCAT(approver.firstname, ' ', approver.lastname) AS ApprovedByName,
                   r.paymentmethod AS PaymentMethod, r.merchant AS Merchant, r.receipturl AS ReceiptUrl,
                   r.createdat AS CreatedAt, r.updatedat AS UpdatedAt
            FROM expense.recurringexpenses r
            LEFT JOIN expense.expensecategories c ON r.categoryid = c.categoryid
            INNER JOIN employee.employees emp ON r.employeeid = emp.employeeid
            LEFT JOIN employee.employees approver ON r.approvedby = approver.employeeid
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

    public async Task<IEnumerable<ExpensePieReportItemDto>> GetExpensePieReportAsync(
        Guid organizationId,
        DateTime fromDate,
        DateTime toDate,
        bool includeExpense,
        bool includeRecurring)
    {
        var categories = (await GetExpenseCategoriesAsync(organizationId)).ToList();
        var result = new Dictionary<Guid, ExpensePieReportItemDto>();

        foreach (var cat in categories)
        {
            result[cat.CategoryId] = new ExpensePieReportItemDto
            {
                CategoryId = cat.CategoryId,
                CategoryName = cat.Name,
                TotalCost = 0
            };
        }

        if (includeExpense)
        {
            var expenseSql = @"
                SELECT e.categoryid AS CategoryId, COALESCE(SUM(e.amount), 0) AS TotalCost
                FROM expense.expenses e
                WHERE e.organizationid = @organizationId
                  AND e.status = 'Approved'
                  AND e.createdat::date >= @fromDate
                  AND e.createdat::date <= @toDate
                  AND e.categoryid IS NOT NULL
                GROUP BY e.categoryid";
            var expenseTotals = await QueryAsync<CategoryTotalRow>(expenseSql, new { organizationId, fromDate = fromDate.Date, toDate = toDate.Date });
            foreach (var row in expenseTotals)
            {
                if (result.TryGetValue(row.CategoryId, out var item))
                    item.TotalCost += row.TotalCost;
                else
                {
                    var cat = categories.FirstOrDefault(c => c.CategoryId == row.CategoryId);
                    result[row.CategoryId] = new ExpensePieReportItemDto
                    {
                        CategoryId = row.CategoryId,
                        CategoryName = cat?.Name ?? string.Empty,
                        TotalCost = row.TotalCost
                    };
                }
            }
        }

        if (includeRecurring)
        {
            var recurringSql = @"
                SELECT r.categoryid AS CategoryId, COALESCE(SUM(r.amount), 0) AS TotalCost
                FROM expense.recurringexpenses r
                WHERE r.organizationid = @organizationId
                  AND r.status = 'Approved'
                  AND r.createdat::date >= @fromDate
                  AND r.createdat::date <= @toDate
                GROUP BY r.categoryid";
            var recurringTotals = await QueryAsync<CategoryTotalRow>(recurringSql, new { organizationId, fromDate = fromDate.Date, toDate = toDate.Date });
            foreach (var row in recurringTotals)
            {
                if (result.TryGetValue(row.CategoryId, out var item))
                    item.TotalCost += row.TotalCost;
                else
                {
                    var cat = categories.FirstOrDefault(c => c.CategoryId == row.CategoryId);
                    result[row.CategoryId] = new ExpensePieReportItemDto
                    {
                        CategoryId = row.CategoryId,
                        CategoryName = cat?.Name ?? string.Empty,
                        TotalCost = row.TotalCost
                    };
                }
            }
        }

        return result.Values.OrderBy(x => x.CategoryName).ToList();
    }

    private sealed class CategoryTotalRow
    {
        public Guid CategoryId { get; set; }
        public decimal TotalCost { get; set; }
    }
}
