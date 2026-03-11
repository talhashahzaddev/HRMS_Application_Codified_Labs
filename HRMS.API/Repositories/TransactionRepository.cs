using System.Text;
using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class TransactionRepository : BaseRepository, ITransactionRepository
{
    public TransactionRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    // ========== TRANSACTIONS ==========

    public async Task<Guid> CreateTransactionAsync(TransactionDto transaction)
    {
        var sql = @"
            INSERT INTO payment.transactions
                (company_id, subscription_id, plan_id, gateway, gateway_transaction_id, gateway_order_id,
                 gateway_customer_id, amount, currency, billing_cycle, status, payment_method,
                 card_last4, card_brand, failure_reason, metadata)
            VALUES
                (@CompanyId, @SubscriptionId, @PlanId, @Gateway, @GatewayTransactionId, @GatewayOrderId,
                 NULL, @Amount, @Currency, @BillingCycle, @Status, @PaymentMethod,
                 @CardLast4, @CardBrand, @FailureReason, NULL)
            RETURNING transaction_id";

        return await ExecuteScalarAsync<Guid>(sql, new
        {
            transaction.CompanyId,
            transaction.SubscriptionId,
            transaction.PlanId,
            transaction.Gateway,
            transaction.GatewayTransactionId,
            transaction.GatewayOrderId,
            transaction.Amount,
            transaction.Currency,
            transaction.BillingCycle,
            transaction.Status,
            transaction.PaymentMethod,
            transaction.CardLast4,
            transaction.CardBrand,
            transaction.FailureReason
        });
    }

    public async Task<bool> UpdateTransactionStatusAsync(Guid transactionId, string status,
        string? gatewayTransactionId = null, string? paymentMethod = null,
        string? cardLast4 = null, string? cardBrand = null, string? failureReason = null)
    {
        var sql = @"
            UPDATE payment.transactions
            SET status = @status,
                gateway_transaction_id = COALESCE(@gatewayTransactionId, gateway_transaction_id),
                payment_method = COALESCE(@paymentMethod, payment_method),
                card_last4 = COALESCE(@cardLast4, card_last4),
                card_brand = COALESCE(@cardBrand, card_brand),
                failure_reason = COALESCE(@failureReason, failure_reason)
            WHERE transaction_id = @transactionId";

        var rows = await ExecuteAsync(sql, new { transactionId, status, gatewayTransactionId, paymentMethod, cardLast4, cardBrand, failureReason });
        return rows > 0;
    }

    public async Task<TransactionDto?> GetTransactionByIdAsync(Guid transactionId)
    {
        var sql = @"
            SELECT t.transaction_id as TransactionId, t.company_id as CompanyId, o.name as CompanyName,
                   t.subscription_id as SubscriptionId, t.plan_id as PlanId, sp.name as PlanName,
                   t.gateway as Gateway, t.gateway_transaction_id as GatewayTransactionId,
                   t.gateway_order_id as GatewayOrderId, t.amount as Amount, t.currency as Currency,
                   t.billing_cycle as BillingCycle, t.status as Status, t.payment_method as PaymentMethod,
                   t.card_last4 as CardLast4, t.card_brand as CardBrand, t.failure_reason as FailureReason,
                   t.created_at as CreatedAt, t.updated_at as UpdatedAt
            FROM payment.transactions t
            LEFT JOIN auth.organizations o ON t.company_id = o.organizationid
            LEFT JOIN payment.subscriptionplans sp ON t.plan_id = sp.planid
            WHERE t.transaction_id = @transactionId";

        return await QuerySingleOrDefaultAsync<TransactionDto>(sql, new { transactionId });
    }

    public async Task<TransactionDetailDto?> GetTransactionDetailByIdAsync(Guid transactionId)
    {
        var sql = @"
            SELECT t.transaction_id as TransactionId, t.company_id as CompanyId, o.name as CompanyName,
                   t.subscription_id as SubscriptionId, t.plan_id as PlanId, sp.name as PlanName,
                   t.gateway as Gateway, t.gateway_transaction_id as GatewayTransactionId,
                   t.gateway_order_id as GatewayOrderId, t.gateway_customer_id as GatewayCustomerId,
                   t.amount as Amount, t.currency as Currency, t.billing_cycle as BillingCycle,
                   t.status as Status, t.payment_method as PaymentMethod, t.card_last4 as CardLast4,
                   t.card_brand as CardBrand, t.failure_reason as FailureReason,
                   t.metadata as Metadata, t.created_at as CreatedAt, t.updated_at as UpdatedAt
            FROM payment.transactions t
            LEFT JOIN auth.organizations o ON t.company_id = o.organizationid
            LEFT JOIN payment.subscriptionplans sp ON t.plan_id = sp.planid
            WHERE t.transaction_id = @transactionId";

        var detail = await QuerySingleOrDefaultAsync<TransactionDetailDto>(sql, new { transactionId });
        if (detail == null) return null;

        detail.Invoice = await GetInvoiceByTransactionIdAsync(transactionId);
        detail.Refunds = await GetRefundsByTransactionIdAsync(transactionId);
        return detail;
    }

    public async Task<TransactionDto?> GetTransactionByGatewayOrderIdAsync(string gatewayOrderId)
    {
        var sql = @"
            SELECT t.transaction_id as TransactionId, t.company_id as CompanyId, o.name as CompanyName,
                   t.subscription_id as SubscriptionId, t.plan_id as PlanId, sp.name as PlanName,
                   t.gateway as Gateway, t.gateway_transaction_id as GatewayTransactionId,
                   t.gateway_order_id as GatewayOrderId, t.amount as Amount, t.currency as Currency,
                   t.billing_cycle as BillingCycle, t.status as Status, t.payment_method as PaymentMethod,
                   t.card_last4 as CardLast4, t.card_brand as CardBrand, t.failure_reason as FailureReason,
                   t.created_at as CreatedAt, t.updated_at as UpdatedAt
            FROM payment.transactions t
            LEFT JOIN auth.organizations o ON t.company_id = o.organizationid
            LEFT JOIN payment.subscriptionplans sp ON t.plan_id = sp.planid
            WHERE t.gateway_order_id = @gatewayOrderId";

        return await QuerySingleOrDefaultAsync<TransactionDto>(sql, new { gatewayOrderId });
    }

    public async Task<(List<TransactionDto> Items, int TotalCount)> GetTransactionsPagedAsync(TransactionFilterRequest filter)
    {
        var where = new StringBuilder("WHERE 1=1");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(filter.Status))
        {
            where.Append(" AND t.status = @status");
            parameters.Add("status", filter.Status);
        }
        if (!string.IsNullOrEmpty(filter.Gateway))
        {
            where.Append(" AND t.gateway = @gateway");
            parameters.Add("gateway", filter.Gateway);
        }
        if (filter.CompanyId.HasValue)
        {
            where.Append(" AND t.company_id = @companyId");
            parameters.Add("companyId", filter.CompanyId.Value);
        }
        if (filter.PlanId.HasValue)
        {
            where.Append(" AND t.plan_id = @planId");
            parameters.Add("planId", filter.PlanId.Value);
        }
        if (filter.DateFrom.HasValue)
        {
            where.Append(" AND t.created_at >= @dateFrom");
            parameters.Add("dateFrom", filter.DateFrom.Value);
        }
        if (filter.DateTo.HasValue)
        {
            where.Append(" AND t.created_at <= @dateTo");
            parameters.Add("dateTo", filter.DateTo.Value);
        }

        var allowedSorts = new HashSet<string> { "created_at", "amount", "status" };
        var sortCol = allowedSorts.Contains(filter.SortBy?.ToLower() ?? "") ? filter.SortBy!.ToLower() : "created_at";
        var sortDir = filter.SortDirection?.ToUpper() == "ASC" ? "ASC" : "DESC";

        var countSql = $"SELECT COUNT(*) FROM payment.transactions t {where}";
        var totalCount = await ExecuteScalarAsync<int>(countSql, parameters);

        var offset = (filter.Page - 1) * filter.PageSize;
        parameters.Add("offset", offset);
        parameters.Add("limit", filter.PageSize);

        var sql = $@"SELECT t.transaction_id as TransactionId, t.company_id as CompanyId, o.name as CompanyName,
                   t.subscription_id as SubscriptionId, t.plan_id as PlanId, sp.name as PlanName,
                   t.gateway as Gateway, t.gateway_transaction_id as GatewayTransactionId,
                   t.gateway_order_id as GatewayOrderId, t.amount as Amount, t.currency as Currency,
                   t.billing_cycle as BillingCycle, t.status as Status, t.payment_method as PaymentMethod,
                   t.card_last4 as CardLast4, t.card_brand as CardBrand, t.failure_reason as FailureReason,
                   t.created_at as CreatedAt, t.updated_at as UpdatedAt
            FROM payment.transactions t
            LEFT JOIN auth.organizations o ON t.company_id = o.organizationid
            LEFT JOIN payment.subscriptionplans sp ON t.plan_id = sp.planid
            {where}
            ORDER BY t.{sortCol} {sortDir}
            OFFSET @offset LIMIT @limit";

        var items = (await QueryAsync<TransactionDto>(sql, parameters)).ToList();
        return (items, totalCount);
    }

    public async Task<(List<TransactionDto> Items, int TotalCount)> GetTransactionsByCompanyPagedAsync(Guid companyId, int page, int pageSize)
    {
        var countSql = "SELECT COUNT(*) FROM payment.transactions WHERE company_id = @companyId";
        var totalCount = await ExecuteScalarAsync<int>(countSql, new { companyId });

        var offset = (page - 1) * pageSize;
        var sql = @"
            SELECT t.transaction_id as TransactionId, t.company_id as CompanyId, o.name as CompanyName,
                   t.subscription_id as SubscriptionId, t.plan_id as PlanId, sp.name as PlanName,
                   t.gateway as Gateway, t.gateway_transaction_id as GatewayTransactionId,
                   t.gateway_order_id as GatewayOrderId, t.amount as Amount, t.currency as Currency,
                   t.billing_cycle as BillingCycle, t.status as Status, t.payment_method as PaymentMethod,
                   t.card_last4 as CardLast4, t.card_brand as CardBrand, t.failure_reason as FailureReason,
                   t.created_at as CreatedAt, t.updated_at as UpdatedAt
            FROM payment.transactions t
            LEFT JOIN auth.organizations o ON t.company_id = o.organizationid
            LEFT JOIN payment.subscriptionplans sp ON t.plan_id = sp.planid
            WHERE t.company_id = @companyId
            ORDER BY t.created_at DESC
            OFFSET @offset LIMIT @limit";

        var items = (await QueryAsync<TransactionDto>(sql, new { companyId, offset, limit = pageSize })).ToList();
        return (items, totalCount);
    }

    // ========== INVOICES ==========

    public async Task<string> GenerateInvoiceNumberAsync()
    {
        var sql = "SELECT payment.generate_invoice_number()";
        return await ExecuteScalarAsync<string>(sql);
    }

    public async Task<Guid> CreateInvoiceAsync(InvoiceDto invoice)
    {
        var sql = @"
            INSERT INTO payment.invoices
                (transaction_id, company_id, invoice_number, amount, tax_amount, discount_amount,
                 total_amount, currency, billing_period_start, billing_period_end, plan_name,
                 billing_cycle, company_name, company_email, status, paid_at, due_date, notes)
            VALUES
                (@TransactionId, @CompanyId, @InvoiceNumber, @Amount, @TaxAmount, @DiscountAmount,
                 @TotalAmount, @Currency, @BillingPeriodStart, @BillingPeriodEnd, @PlanName,
                 @BillingCycle, @CompanyName, @CompanyEmail, @Status, @PaidAt, @DueDate, @Notes)
            RETURNING invoice_id";

        return await ExecuteScalarAsync<Guid>(sql, invoice);
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid invoiceId)
    {
        var sql = @"
            SELECT invoice_id as InvoiceId, transaction_id as TransactionId, company_id as CompanyId,
                   invoice_number as InvoiceNumber, amount as Amount, tax_amount as TaxAmount,
                   discount_amount as DiscountAmount, total_amount as TotalAmount, currency as Currency,
                   billing_period_start as BillingPeriodStart, billing_period_end as BillingPeriodEnd,
                   plan_name as PlanName, billing_cycle as BillingCycle, company_name as CompanyName,
                   company_email as CompanyEmail, status as Status, issued_at as IssuedAt,
                   paid_at as PaidAt, due_date as DueDate, notes as Notes
            FROM payment.invoices
            WHERE invoice_id = @invoiceId";

        return await QuerySingleOrDefaultAsync<InvoiceDto>(sql, new { invoiceId });
    }

    public async Task<InvoiceDto?> GetInvoiceByTransactionIdAsync(Guid transactionId)
    {
        var sql = @"
            SELECT invoice_id as InvoiceId, transaction_id as TransactionId, company_id as CompanyId,
                   invoice_number as InvoiceNumber, amount as Amount, tax_amount as TaxAmount,
                   discount_amount as DiscountAmount, total_amount as TotalAmount, currency as Currency,
                   billing_period_start as BillingPeriodStart, billing_period_end as BillingPeriodEnd,
                   plan_name as PlanName, billing_cycle as BillingCycle, company_name as CompanyName,
                   company_email as CompanyEmail, status as Status, issued_at as IssuedAt,
                   paid_at as PaidAt, due_date as DueDate, notes as Notes
            FROM payment.invoices
            WHERE transaction_id = @transactionId";

        return await QuerySingleOrDefaultAsync<InvoiceDto>(sql, new { transactionId });
    }

    public async Task<(List<InvoiceDto> Items, int TotalCount)> GetInvoicesPagedAsync(InvoiceFilterRequest filter)
    {
        var where = new StringBuilder("WHERE 1=1");
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(filter.Status))
        {
            where.Append(" AND i.status = @status");
            parameters.Add("status", filter.Status);
        }
        if (filter.CompanyId.HasValue)
        {
            where.Append(" AND i.company_id = @companyId");
            parameters.Add("companyId", filter.CompanyId.Value);
        }
        if (filter.DateFrom.HasValue)
        {
            where.Append(" AND i.issued_at >= @dateFrom");
            parameters.Add("dateFrom", filter.DateFrom.Value);
        }
        if (filter.DateTo.HasValue)
        {
            where.Append(" AND i.issued_at <= @dateTo");
            parameters.Add("dateTo", filter.DateTo.Value);
        }

        var sortDir = filter.SortDirection?.ToUpper() == "ASC" ? "ASC" : "DESC";
        var countSql = $"SELECT COUNT(*) FROM payment.invoices i {where}";
        var totalCount = await ExecuteScalarAsync<int>(countSql, parameters);

        var offset = (filter.Page - 1) * filter.PageSize;
        parameters.Add("offset", offset);
        parameters.Add("limit", filter.PageSize);

        var sql = $@"SELECT invoice_id as InvoiceId, transaction_id as TransactionId, company_id as CompanyId,
                   invoice_number as InvoiceNumber, amount as Amount, tax_amount as TaxAmount,
                   discount_amount as DiscountAmount, total_amount as TotalAmount, currency as Currency,
                   billing_period_start as BillingPeriodStart, billing_period_end as BillingPeriodEnd,
                   plan_name as PlanName, billing_cycle as BillingCycle, company_name as CompanyName,
                   company_email as CompanyEmail, status as Status, issued_at as IssuedAt,
                   paid_at as PaidAt, due_date as DueDate, notes as Notes
            FROM payment.invoices i
            {where}
            ORDER BY i.issued_at {sortDir}
            OFFSET @offset LIMIT @limit";

        var items = (await QueryAsync<InvoiceDto>(sql, parameters)).ToList();
        return (items, totalCount);
    }

    public async Task<(List<InvoiceDto> Items, int TotalCount)> GetInvoicesByCompanyPagedAsync(Guid companyId, int page, int pageSize)
    {
        var countSql = "SELECT COUNT(*) FROM payment.invoices WHERE company_id = @companyId";
        var totalCount = await ExecuteScalarAsync<int>(countSql, new { companyId });

        var offset = (page - 1) * pageSize;
        var sql = @"
            SELECT invoice_id as InvoiceId, transaction_id as TransactionId, company_id as CompanyId,
                   invoice_number as InvoiceNumber, amount as Amount, tax_amount as TaxAmount,
                   discount_amount as DiscountAmount, total_amount as TotalAmount, currency as Currency,
                   billing_period_start as BillingPeriodStart, billing_period_end as BillingPeriodEnd,
                   plan_name as PlanName, billing_cycle as BillingCycle, company_name as CompanyName,
                   company_email as CompanyEmail, status as Status, issued_at as IssuedAt,
                   paid_at as PaidAt, due_date as DueDate, notes as Notes
            FROM payment.invoices
            WHERE company_id = @companyId
            ORDER BY issued_at DESC
            OFFSET @offset LIMIT @limit";

        var items = (await QueryAsync<InvoiceDto>(sql, new { companyId, offset, limit = pageSize })).ToList();
        return (items, totalCount);
    }

    public async Task<bool> UpdateInvoiceStatusAsync(Guid invoiceId, string status, DateTime? paidAt = null)
    {
        var sql = @"
            UPDATE payment.invoices
            SET status = @status,
                paid_at = COALESCE(@paidAt, paid_at)
            WHERE invoice_id = @invoiceId";

        var rows = await ExecuteAsync(sql, new { invoiceId, status, paidAt });
        return rows > 0;
    }

    // ========== REFUNDS ==========

    public async Task<Guid> CreateRefundAsync(RefundDto refund)
    {
        var sql = @"
            INSERT INTO payment.refunds
                (transaction_id, amount, currency, reason, status, initiated_by, admin_id)
            VALUES
                (@TransactionId, @Amount, @Currency, @Reason, @Status, @InitiatedBy, @AdminId)
            RETURNING refund_id";

        return await ExecuteScalarAsync<Guid>(sql, refund);
    }

    public async Task<RefundDto?> GetRefundByIdAsync(Guid refundId)
    {
        var sql = @"
            SELECT refund_id as RefundId, transaction_id as TransactionId,
                   gateway_refund_id as GatewayRefundId, amount as Amount, currency as Currency,
                   reason as Reason, status as Status, initiated_by as InitiatedBy,
                   admin_id as AdminId, created_at as CreatedAt, processed_at as ProcessedAt
            FROM payment.refunds
            WHERE refund_id = @refundId";

        return await QuerySingleOrDefaultAsync<RefundDto>(sql, new { refundId });
    }

    public async Task<List<RefundDto>> GetRefundsByTransactionIdAsync(Guid transactionId)
    {
        var sql = @"
            SELECT refund_id as RefundId, transaction_id as TransactionId,
                   gateway_refund_id as GatewayRefundId, amount as Amount, currency as Currency,
                   reason as Reason, status as Status, initiated_by as InitiatedBy,
                   admin_id as AdminId, created_at as CreatedAt, processed_at as ProcessedAt
            FROM payment.refunds
            WHERE transaction_id = @transactionId
            ORDER BY created_at DESC";

        return (await QueryAsync<RefundDto>(sql, new { transactionId })).ToList();
    }

    public async Task<bool> UpdateRefundStatusAsync(Guid refundId, string status, string? gatewayRefundId = null, DateTime? processedAt = null)
    {
        var sql = @"
            UPDATE payment.refunds
            SET status = @status,
                gateway_refund_id = COALESCE(@gatewayRefundId, gateway_refund_id),
                processed_at = COALESCE(@processedAt, processed_at)
            WHERE refund_id = @refundId";

        var rows = await ExecuteAsync(sql, new { refundId, status, gatewayRefundId, processedAt });
        return rows > 0;
    }

    // ========== DASHBOARD / ANALYTICS ==========

    public async Task<PaymentDashboardDto> GetPaymentDashboardAsync()
    {
        var sql = @"
            SELECT
                COALESCE(SUM(CASE WHEN status = 'completed' THEN amount ELSE 0 END), 0) as TotalRevenue,
                COALESCE(SUM(CASE WHEN status = 'completed' AND billing_cycle = 'monthly'
                    AND created_at >= date_trunc('month', CURRENT_DATE) THEN amount ELSE 0 END), 0) as MonthlyRecurringRevenue,
                COALESCE(SUM(CASE WHEN status = 'completed' AND billing_cycle = 'annual'
                    AND created_at >= date_trunc('year', CURRENT_DATE) THEN amount ELSE 0 END), 0) as AnnualRecurringRevenue,
                COALESCE(SUM(CASE WHEN status = 'completed'
                    AND created_at >= date_trunc('month', CURRENT_DATE) THEN amount ELSE 0 END), 0) as RevenueThisMonth,
                COALESCE(SUM(CASE WHEN status = 'completed'
                    AND created_at >= date_trunc('month', CURRENT_DATE - interval '1 month')
                    AND created_at < date_trunc('month', CURRENT_DATE) THEN amount ELSE 0 END), 0) as RevenueLastMonth,
                COUNT(*) as TotalTransactions,
                COUNT(CASE WHEN status = 'completed' THEN 1 END) as SuccessfulTransactions,
                COUNT(CASE WHEN status = 'failed' THEN 1 END) as FailedTransactions,
                COUNT(CASE WHEN status = 'pending' THEN 1 END) as PendingTransactions
            FROM payment.transactions";

        var stats = await QuerySingleAsync<dynamic>(sql);

        var refundSql = @"
            SELECT COUNT(*) as TotalRefunds,
                   COALESCE(SUM(CASE WHEN status = 'completed' THEN amount ELSE 0 END), 0) as TotalRefundedAmount
            FROM payment.refunds";

        var refundStats = await QuerySingleAsync<dynamic>(refundSql);

        var recentSql = @"
            SELECT t.transaction_id as TransactionId, t.company_id as CompanyId, o.name as CompanyName,
                   t.subscription_id as SubscriptionId, t.plan_id as PlanId, sp.name as PlanName,
                   t.gateway as Gateway, t.gateway_transaction_id as GatewayTransactionId,
                   t.gateway_order_id as GatewayOrderId, t.amount as Amount, t.currency as Currency,
                   t.billing_cycle as BillingCycle, t.status as Status, t.payment_method as PaymentMethod,
                   t.card_last4 as CardLast4, t.card_brand as CardBrand, t.failure_reason as FailureReason,
                   t.created_at as CreatedAt, t.updated_at as UpdatedAt
            FROM payment.transactions t
            LEFT JOIN auth.organizations o ON t.company_id = o.organizationid
            LEFT JOIN payment.subscriptionplans sp ON t.plan_id = sp.planid
            ORDER BY t.created_at DESC
            LIMIT 10";

        var recentTransactions = (await QueryAsync<TransactionDto>(recentSql)).ToList();

        decimal revenueThisMonth = (decimal)(stats.revenuethismonth ?? 0m);
        decimal revenueLastMonth = (decimal)(stats.revenuelastmonth ?? 0m);
        decimal growthPercent = revenueLastMonth > 0
            ? Math.Round((revenueThisMonth - revenueLastMonth) / revenueLastMonth * 100, 2)
            : (revenueThisMonth > 0 ? 100 : 0);

        return new PaymentDashboardDto
        {
            TotalRevenue = (decimal)(stats.totalrevenue ?? 0m),
            MonthlyRecurringRevenue = (decimal)(stats.monthlyrecurringrevenue ?? 0m),
            AnnualRecurringRevenue = (decimal)(stats.annualrecurringrevenue ?? 0m),
            RevenueThisMonth = revenueThisMonth,
            RevenueLastMonth = revenueLastMonth,
            RevenueGrowthPercent = growthPercent,
            TotalTransactions = (int)(stats.totaltransactions ?? 0),
            SuccessfulTransactions = (int)(stats.successfultransactions ?? 0),
            FailedTransactions = (int)(stats.failedtransactions ?? 0),
            PendingTransactions = (int)(stats.pendingtransactions ?? 0),
            TotalRefunds = (int)(refundStats.totalrefunds ?? 0),
            TotalRefundedAmount = (decimal)(refundStats.totalrefundedamount ?? 0m),
            RecentTransactions = recentTransactions
        };
    }

    public async Task<RevenueAnalyticsDto> GetRevenueAnalyticsAsync(int months = 12)
    {
        var monthlyRevenueSql = @"
            SELECT TO_CHAR(date_trunc('month', created_at), 'YYYY-MM') as Month,
                   COALESCE(SUM(amount), 0) as Revenue,
                   COUNT(*) as TransactionCount
            FROM payment.transactions
            WHERE status = 'completed'
              AND created_at >= date_trunc('month', CURRENT_DATE - make_interval(months => @months))
            GROUP BY date_trunc('month', created_at)
            ORDER BY date_trunc('month', created_at)";

        var monthlyRevenue = (await QueryAsync<MonthlyRevenueDto>(monthlyRevenueSql, new { months })).ToList();

        var planRevenueSql = @"
            SELECT sp.name as PlanName,
                   COALESCE(SUM(t.amount), 0) as Revenue,
                   COUNT(*) as SubscriptionCount
            FROM payment.transactions t
            JOIN payment.subscriptionplans sp ON t.plan_id = sp.planid
            WHERE t.status = 'completed'
            GROUP BY sp.name
            ORDER BY Revenue DESC";

        var revenueByPlan = (await QueryAsync<PlanRevenueDto>(planRevenueSql)).ToList();

        var cycleRevenueSql = @"
            SELECT billing_cycle as BillingCycle,
                   COALESCE(SUM(amount), 0) as Revenue,
                   COUNT(*) as Count
            FROM payment.transactions
            WHERE status = 'completed'
            GROUP BY billing_cycle";

        var revenueByCycle = (await QueryAsync<CycleRevenueDto>(cycleRevenueSql)).ToList();

        return new RevenueAnalyticsDto
        {
            MonthlyRevenue = monthlyRevenue,
            RevenueByPlan = revenueByPlan,
            RevenueByCycle = revenueByCycle
        };
    }
}
