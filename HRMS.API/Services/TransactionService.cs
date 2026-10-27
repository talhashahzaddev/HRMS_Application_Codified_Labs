using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IEntitlementService _entitlementService;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        ITransactionRepository transactionRepository,
        IPaymentRepository paymentRepository,
        IEntitlementService entitlementService,
        ILogger<TransactionService> logger)
    {
        _transactionRepository = transactionRepository;
        _paymentRepository = paymentRepository;
        _entitlementService = entitlementService;
        _logger = logger;
    }

    // ========== CHECKOUT FLOW ==========

    public async Task<ServiceResponse<CheckoutSessionResponse>> CreateCheckoutSessionAsync(
        Guid companyId, CreateCheckoutSessionRequest request)
    {
        _logger.LogInformation("Creating checkout session for Company {CompanyId}, Plan {PlanId}", companyId, request.PlanId);

        try
        {
            // 1. Get the plan details
            var plan = await _paymentRepository.GetSubscriptionPlanByIdAsync(request.PlanId);
            if (plan == null || !plan.IsActive)
            {
                return new ServiceResponse<CheckoutSessionResponse>
                {
                    Success = false,
                    Message = "Subscription plan is unavailable or archived."
                };
            }

            var entitlementPlan = await _entitlementService.GetPlanAsync(request.PlanId);
            if (entitlementPlan == null || !string.Equals(
                    entitlementPlan.Status?.Trim(),
                    "active",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new ServiceResponse<CheckoutSessionResponse>
                {
                    Success = false,
                    Message = "Subscription plan is unavailable or archived."
                };
            }

            // 2. Calculate amount
            decimal amount = request.BillingCycle.ToLower() == "annual"
                ? (plan.AnnualPrice ?? 0)
                : (plan.MonthlyPrice ?? 0);

            if (amount <= 0)
            {
                return new ServiceResponse<CheckoutSessionResponse>
                {
                    Success = false,
                    Message = "Invalid plan price for the selected billing cycle"
                };
            }

            // 3. Create a pending transaction record
            var transaction = new TransactionDto
            {
                CompanyId = companyId,
                PlanId = request.PlanId,
                Gateway = "stripe",
                Amount = amount,
                Currency = plan.Currency,
                BillingCycle = request.BillingCycle.ToLower(),
                Status = "pending"
            };
            var transactionId = await _transactionRepository.CreateTransactionAsync(transaction);

            // 4. TODO: Create Stripe Checkout Session
            // For now, return a simulated checkout URL
            // In production, this would call Stripe API:
            // var session = await _stripeClient.Checkout.Sessions.CreateAsync(...)
            var sessionId = Guid.NewGuid().ToString();
            var checkoutUrl = $"/subscription/checkout?session_id={sessionId}&transaction_id={transactionId}";

            // Update transaction with gateway order ID
            await _transactionRepository.UpdateTransactionStatusAsync(
                transactionId, "pending", gatewayTransactionId: null);

            return new ServiceResponse<CheckoutSessionResponse>
            {
                Success = true,
                Data = new CheckoutSessionResponse
                {
                    SessionId = sessionId,
                    CheckoutUrl = checkoutUrl,
                    TransactionId = transactionId
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session");
            return new ServiceResponse<CheckoutSessionResponse>
            {
                Success = false,
                Message = "An error occurred while creating the checkout session"
            };
        }
    }

    public async Task<ServiceResponse<PaymentConfirmationDto>> VerifyPaymentAsync(VerifyPaymentRequest request)
    {
        _logger.LogInformation("Verifying payment for Transaction {TransactionId}", request.TransactionId);

        try
        {
            var transaction = await _transactionRepository.GetTransactionByIdAsync(request.TransactionId);
            if (transaction == null)
            {
                return new ServiceResponse<PaymentConfirmationDto>
                {
                    Success = false,
                    Message = "Transaction not found"
                };
            }

            var plan = await _entitlementService.GetPlanAsync(transaction.PlanId);
            if (plan == null || !string.Equals(
                    plan.Status?.Trim(),
                    "active",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new ServiceResponse<PaymentConfirmationDto>
                {
                    Success = false,
                    Message = "The selected subscription plan is unavailable or archived."
                };
            }

            // TODO: Verify with Stripe
            // var session = await _stripeClient.Checkout.Sessions.GetAsync(request.SessionId);
            // For now, simulate successful verification

            // Apply the subscription through the entitlement service so upgrades update
            // the tenant's effective plan and invalidate its cached access.
            var startDate = DateTime.UtcNow;
            var endDate = transaction.BillingCycle == "annual"
                ? startDate.AddYears(1)
                : startDate.AddMonths(1);
            var assigned = await _entitlementService.AssignTenantPlanAsync(
                transaction.CompanyId,
                new TenantPlanAssignmentRequest
                {
                    PlanId = transaction.PlanId,
                    Status = "active",
                    BillingCycle = transaction.BillingCycle,
                    StartDate = startDate,
                    EndDate = endDate,
                    CustomCurrency = plan.Currency
                },
                actor: null);
            if (!assigned)
            {
                throw new InvalidOperationException("The subscription could not be activated after payment.");
            }

            await _transactionRepository.UpdateTransactionStatusAsync(
                request.TransactionId, "completed",
                paymentMethod: "card",
                cardLast4: "4242",
                cardBrand: "visa");

            // Generate invoice
            var invoiceNumber = await _transactionRepository.GenerateInvoiceNumberAsync();

            // Get company name from transaction (already joined)
            var invoice = new InvoiceDto
            {
                TransactionId = request.TransactionId,
                CompanyId = transaction.CompanyId,
                InvoiceNumber = invoiceNumber,
                Amount = transaction.Amount,
                TaxAmount = 0,
                DiscountAmount = 0,
                TotalAmount = transaction.Amount,
                Currency = transaction.Currency,
                BillingPeriodStart = startDate,
                BillingPeriodEnd = endDate,
                PlanName = plan?.Name ?? "Unknown Plan",
                BillingCycle = transaction.BillingCycle,
                CompanyName = transaction.CompanyName ?? "Unknown",
                Status = "paid",
                PaidAt = DateTime.UtcNow
            };
            var invoiceId = await _transactionRepository.CreateInvoiceAsync(invoice);

            return new ServiceResponse<PaymentConfirmationDto>
            {
                Success = true,
                Data = new PaymentConfirmationDto
                {
                    TransactionId = request.TransactionId,
                    Status = "completed",
                    PlanName = plan?.Name ?? "Unknown Plan",
                    AmountPaid = transaction.Amount,
                    Currency = transaction.Currency,
                    BillingCycle = transaction.BillingCycle,
                    SubscriptionStartDate = startDate,
                    SubscriptionEndDate = endDate,
                    InvoiceNumber = invoiceNumber,
                    InvoiceId = invoiceId
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment");
            return new ServiceResponse<PaymentConfirmationDto>
            {
                Success = false,
                Message = "An error occurred while verifying the payment"
            };
        }
    }

    // ========== ORG-SIDE QUERIES ==========

    public async Task<ServiceResponse<PagedResult<TransactionDto>>> GetTransactionsByCompanyAsync(
        Guid companyId, int page, int pageSize)
    {
        var (items, totalCount) = await _transactionRepository.GetTransactionsByCompanyPagedAsync(companyId, page, pageSize);
        return new ServiceResponse<PagedResult<TransactionDto>>
        {
            Success = true,
            Data = new PagedResult<TransactionDto>
            {
                Data = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            }
        };
    }

    public async Task<ServiceResponse<PagedResult<InvoiceDto>>> GetInvoicesByCompanyAsync(
        Guid companyId, int page, int pageSize)
    {
        var (items, totalCount) = await _transactionRepository.GetInvoicesByCompanyPagedAsync(companyId, page, pageSize);
        return new ServiceResponse<PagedResult<InvoiceDto>>
        {
            Success = true,
            Data = new PagedResult<InvoiceDto>
            {
                Data = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            }
        };
    }

    public async Task<ServiceResponse<InvoiceDto>> GetInvoiceByIdAsync(Guid invoiceId)
    {
        var invoice = await _transactionRepository.GetInvoiceByIdAsync(invoiceId);
        if (invoice == null)
        {
            return new ServiceResponse<InvoiceDto>
            {
                Success = false,
                Message = "Invoice not found"
            };
        }
        return new ServiceResponse<InvoiceDto> { Success = true, Data = invoice };
    }

    // ========== ADMIN TRANSACTION MANAGEMENT ==========

    public async Task<ServiceResponse<PagedResult<TransactionDto>>> GetAllTransactionsAsync(TransactionFilterRequest filter)
    {
        var (items, totalCount) = await _transactionRepository.GetTransactionsPagedAsync(filter);
        return new ServiceResponse<PagedResult<TransactionDto>>
        {
            Success = true,
            Data = new PagedResult<TransactionDto>
            {
                Data = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            }
        };
    }

    public async Task<ServiceResponse<TransactionDetailDto>> GetTransactionDetailAsync(Guid transactionId)
    {
        var detail = await _transactionRepository.GetTransactionDetailByIdAsync(transactionId);
        if (detail == null)
        {
            return new ServiceResponse<TransactionDetailDto>
            {
                Success = false,
                Message = "Transaction not found"
            };
        }
        return new ServiceResponse<TransactionDetailDto> { Success = true, Data = detail };
    }

    // ========== ADMIN INVOICE MANAGEMENT ==========

    public async Task<ServiceResponse<PagedResult<InvoiceDto>>> GetAllInvoicesAsync(InvoiceFilterRequest filter)
    {
        var (items, totalCount) = await _transactionRepository.GetInvoicesPagedAsync(filter);
        return new ServiceResponse<PagedResult<InvoiceDto>>
        {
            Success = true,
            Data = new PagedResult<InvoiceDto>
            {
                Data = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            }
        };
    }

    // ========== ADMIN REFUNDS ==========

    public async Task<ServiceResponse<RefundDto>> CreateRefundAsync(Guid adminId, CreateRefundRequest request)
    {
        _logger.LogInformation("Admin {AdminId} creating refund for Transaction {TransactionId}", adminId, request.TransactionId);

        try
        {
            // Validate transaction exists and is completed
            var transaction = await _transactionRepository.GetTransactionByIdAsync(request.TransactionId);
            if (transaction == null)
            {
                return new ServiceResponse<RefundDto>
                {
                    Success = false,
                    Message = "Transaction not found"
                };
            }

            if (transaction.Status != "completed")
            {
                return new ServiceResponse<RefundDto>
                {
                    Success = false,
                    Message = $"Cannot refund a transaction with status '{transaction.Status}'. Only completed transactions can be refunded."
                };
            }

            // Check refund amount doesn't exceed transaction amount
            var existingRefunds = await _transactionRepository.GetRefundsByTransactionIdAsync(request.TransactionId);
            var totalRefunded = existingRefunds.Where(r => r.Status != "failed").Sum(r => r.Amount);
            if (totalRefunded + request.Amount > transaction.Amount)
            {
                return new ServiceResponse<RefundDto>
                {
                    Success = false,
                    Message = $"Refund amount ({request.Amount:C}) exceeds remaining refundable amount ({transaction.Amount - totalRefunded:C})"
                };
            }

            // Create refund record
            var refundDto = new RefundDto
            {
                TransactionId = request.TransactionId,
                Amount = request.Amount,
                Currency = transaction.Currency,
                Reason = request.Reason,
                Status = "completed", // In production, would be 'pending' until Stripe confirms
                InitiatedBy = "admin",
                AdminId = adminId,
                ProcessedAt = DateTime.UtcNow
            };
            var refundId = await _transactionRepository.CreateRefundAsync(refundDto);

            // TODO: Process refund via Stripe
            // var stripeRefund = await _stripeClient.Refunds.CreateAsync(...)
            // await _transactionRepository.UpdateRefundStatusAsync(refundId, "completed", stripeRefund.Id, DateTime.UtcNow);

            // Update transaction status if fully refunded
            if (totalRefunded + request.Amount >= transaction.Amount)
            {
                await _transactionRepository.UpdateTransactionStatusAsync(request.TransactionId, "refunded");
                // Update invoice status
                var invoice = await _transactionRepository.GetInvoiceByTransactionIdAsync(request.TransactionId);
                if (invoice != null)
                {
                    await _transactionRepository.UpdateInvoiceStatusAsync(invoice.InvoiceId, "refunded");
                }
            }

            var createdRefund = await _transactionRepository.GetRefundByIdAsync(refundId);
            return new ServiceResponse<RefundDto> { Success = true, Data = createdRefund };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refund");
            return new ServiceResponse<RefundDto>
            {
                Success = false,
                Message = "An error occurred while processing the refund"
            };
        }
    }

    // ========== ADMIN DASHBOARD & ANALYTICS ==========

    public async Task<ServiceResponse<PaymentDashboardDto>> GetPaymentDashboardAsync()
    {
        var dashboard = await _transactionRepository.GetPaymentDashboardAsync();
        return new ServiceResponse<PaymentDashboardDto> { Success = true, Data = dashboard };
    }

    public async Task<ServiceResponse<RevenueAnalyticsDto>> GetRevenueAnalyticsAsync(int months = 12)
    {
        var analytics = await _transactionRepository.GetRevenueAnalyticsAsync(months);
        return new ServiceResponse<RevenueAnalyticsDto> { Success = true, Data = analytics };
    }
}
