using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface ITransactionService
{
    // Checkout flow
    Task<ServiceResponse<CheckoutSessionResponse>> CreateCheckoutSessionAsync(Guid companyId, CreateCheckoutSessionRequest request);
    Task<ServiceResponse<PaymentConfirmationDto>> VerifyPaymentAsync(VerifyPaymentRequest request);

    // Transaction queries (org-side)
    Task<ServiceResponse<PagedResult<TransactionDto>>> GetTransactionsByCompanyAsync(Guid companyId, int page, int pageSize);

    // Invoice queries (org-side)
    Task<ServiceResponse<PagedResult<InvoiceDto>>> GetInvoicesByCompanyAsync(Guid companyId, int page, int pageSize);
    Task<ServiceResponse<InvoiceDto>> GetInvoiceByIdAsync(Guid invoiceId);

    // Admin: Transactions
    Task<ServiceResponse<PagedResult<TransactionDto>>> GetAllTransactionsAsync(TransactionFilterRequest filter);
    Task<ServiceResponse<TransactionDetailDto>> GetTransactionDetailAsync(Guid transactionId);

    // Admin: Invoices
    Task<ServiceResponse<PagedResult<InvoiceDto>>> GetAllInvoicesAsync(InvoiceFilterRequest filter);

    // Admin: Refunds
    Task<ServiceResponse<RefundDto>> CreateRefundAsync(Guid adminId, CreateRefundRequest request);

    // Admin: Dashboard & Analytics
    Task<ServiceResponse<PaymentDashboardDto>> GetPaymentDashboardAsync();
    Task<ServiceResponse<RevenueAnalyticsDto>> GetRevenueAnalyticsAsync(int months = 12);
}
