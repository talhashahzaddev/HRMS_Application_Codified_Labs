using HRMS.API.Models.DTOs;

namespace HRMS.API.Repositories.Interfaces;

public interface ITransactionRepository
{
    // Transactions
    Task<Guid> CreateTransactionAsync(TransactionDto transaction);
    Task<bool> UpdateTransactionStatusAsync(Guid transactionId, string status, string? gatewayTransactionId = null, string? paymentMethod = null, string? cardLast4 = null, string? cardBrand = null, string? failureReason = null);
    Task<TransactionDto?> GetTransactionByIdAsync(Guid transactionId);
    Task<TransactionDetailDto?> GetTransactionDetailByIdAsync(Guid transactionId);
    Task<TransactionDto?> GetTransactionByGatewayOrderIdAsync(string gatewayOrderId);
    Task<(List<TransactionDto> Items, int TotalCount)> GetTransactionsPagedAsync(TransactionFilterRequest filter);
    Task<(List<TransactionDto> Items, int TotalCount)> GetTransactionsByCompanyPagedAsync(Guid companyId, int page, int pageSize);

    // Invoices
    Task<Guid> CreateInvoiceAsync(InvoiceDto invoice);
    Task<InvoiceDto?> GetInvoiceByIdAsync(Guid invoiceId);
    Task<InvoiceDto?> GetInvoiceByTransactionIdAsync(Guid transactionId);
    Task<(List<InvoiceDto> Items, int TotalCount)> GetInvoicesPagedAsync(InvoiceFilterRequest filter);
    Task<(List<InvoiceDto> Items, int TotalCount)> GetInvoicesByCompanyPagedAsync(Guid companyId, int page, int pageSize);
    Task<bool> UpdateInvoiceStatusAsync(Guid invoiceId, string status, DateTime? paidAt = null);
    Task<string> GenerateInvoiceNumberAsync();

    // Refunds
    Task<Guid> CreateRefundAsync(RefundDto refund);
    Task<RefundDto?> GetRefundByIdAsync(Guid refundId);
    Task<List<RefundDto>> GetRefundsByTransactionIdAsync(Guid transactionId);
    Task<bool> UpdateRefundStatusAsync(Guid refundId, string status, string? gatewayRefundId = null, DateTime? processedAt = null);

    // Dashboard / Analytics
    Task<PaymentDashboardDto> GetPaymentDashboardAsync();
    Task<RevenueAnalyticsDto> GetRevenueAnalyticsAsync(int months = 12);
}
