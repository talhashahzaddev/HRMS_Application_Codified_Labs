using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Dapper;

namespace HRMS.API.Repositories;

public class PayrollRepository : BaseRepository, IPayrollRepository
{
    public PayrollRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<PayrollPeriod> CreatePayrollPeriodAsync(Guid organizationId, string periodName, DateTime startDate, DateTime endDate, DateTime? payDate = null)
    {
        var periodId = await ExecuteScalarAsync<Guid>(
            "SELECT payroll.fn_create_payroll_period(@organizationId, @periodName, @startDate, @endDate, @payDate)",
            new { organizationId, periodName, startDate, endDate, payDate });

        return new PayrollPeriod
        {
            PeriodId = periodId,
            OrganizationId = organizationId,
            PeriodName = periodName,
            StartDate = startDate,
            EndDate = endDate,
            PayDate = payDate
        };
    }

    public async Task<List<PayrollPeriodDto>> GetPayrollPeriodsAsync(Guid organizationId)
    {
        var sql = @"
            SELECT period_id, period_name, start_date, end_date, pay_date, status,
                   total_employees, total_gross_amount, total_tax_amount, total_net_amount,
                   processed_at
            FROM payroll.payroll_periods
            WHERE organization_id = @organizationId
            ORDER BY start_date DESC";

        var results = await QueryAsync<PayrollPeriodDto>(sql, new { organizationId });
        return results.ToList();
    }

    public async Task<PayrollPeriod?> GetPayrollPeriodByIdAsync(Guid periodId)
    {
        var sql = @"
            SELECT period_id, organization_id, period_name, start_date, end_date, pay_date,
                   status, total_employees, total_gross_amount, total_tax_amount, total_net_amount,
                   processed_by, processed_at, created_at
            FROM payroll.payroll_periods
            WHERE period_id = @periodId";

        return await QuerySingleOrDefaultAsync<PayrollPeriod>(sql, new { periodId });
    }

    public async Task<int> CalculatePayrollAsync(Guid periodId)
    {
        return await ExecuteScalarAsync<int>(
            "SELECT payroll.fn_calculate_payroll(@periodId)",
            new { periodId });
    }

    public async Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesAsync(Guid periodId, int page = 1, int pageSize = 50)
    {
        var sql = @"
            SELECT pe.entry_id, pe.employee_id, pe.basic_salary, pe.overtime_amount,
                   pe.bonus_amount, pe.gross_salary, pe.tax_amount, pe.net_salary,
                   pe.currency, pe.status, pe.calculated_at, pe.paid_at,
                   CONCAT(e.first_name, ' ', e.last_name) as employee_name,
                   e.employee_code
            FROM payroll.payroll_entries pe
            INNER JOIN employee.employees e ON pe.employee_id = e.employee_id
            WHERE pe.period_id = @periodId
            ORDER BY e.first_name, e.last_name
            LIMIT @pageSize OFFSET @offset";

        var results = await QueryAsync<PayrollEntryDto>(sql, new 
        { 
            periodId, 
            pageSize, 
            offset = (page - 1) * pageSize 
        });

        var countSql = "SELECT COUNT(*) FROM payroll.payroll_entries WHERE period_id = @periodId";
        var totalCount = await ExecuteScalarAsync<int>(countSql, new { periodId });

        return new PagedResult<PayrollEntryDto>
        {
            Data = results.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PayrollEntry?> GetPayrollEntryAsync(Guid entryId)
    {
        var sql = @"
            SELECT entry_id, period_id, employee_id, basic_salary, allowances, deductions,
                   overtime_amount, bonus_amount, gross_salary, tax_amount, other_deductions,
                   net_salary, currency, status, calculated_at, paid_at, payment_method,
                   payment_reference, created_at, updated_at
            FROM payroll.payroll_entries
            WHERE entry_id = @entryId";

        return await QuerySingleOrDefaultAsync<PayrollEntry>(sql, new { entryId });
    }

    public async Task<bool> UpdatePayrollPeriodStatusAsync(Guid periodId, string status)
    {
        var sql = @"
            UPDATE payroll.payroll_periods 
            SET status = @status, updated_at = CURRENT_TIMESTAMP
            WHERE period_id = @periodId";

        var affected = await ExecuteAsync(sql, new { periodId, status });
        return affected > 0;
    }

    public async Task<PayrollSummaryDto> GetPayrollSummaryAsync(Guid periodId)
    {
        var sql = @"
            SELECT 
                COALESCE(SUM(gross_salary), 0) as total_gross_salary,
                COALESCE(SUM(tax_amount), 0) as total_tax,
                COALESCE(SUM(net_salary), 0) as total_net_salary,
                COUNT(*) as total_employees
            FROM payroll.payroll_entries
            WHERE period_id = @periodId";

        var result = await QuerySingleAsync<dynamic>(sql, new { periodId });

        return new PayrollSummaryDto
        {
            TotalGrossSalary = result.total_gross_salary,
            TotalTax = result.total_tax,
            TotalNetSalary = result.total_net_salary,
            TotalEmployees = result.total_employees
        };
    }

    public async Task<bool> ProcessPayrollPaymentAsync(Guid entryId, string paymentMethod, string paymentReference)
    {
        var sql = @"
            UPDATE payroll.payroll_entries 
            SET payment_method = @paymentMethod, payment_reference = @paymentReference,
                paid_at = CURRENT_TIMESTAMP, status = 'paid', updated_at = CURRENT_TIMESTAMP
            WHERE entry_id = @entryId";

        var affected = await ExecuteAsync(sql, new { entryId, paymentMethod, paymentReference });
        return affected > 0;
    }
}
