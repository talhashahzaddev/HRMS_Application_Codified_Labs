using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Security.Claims;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ITransactionService _transactionService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentService paymentService,
        ITransactionService transactionService,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _transactionService = transactionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all subscription plans
    /// </summary>
    [HttpGet("subscription-plans")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<SubscriptionPlanDto>>>> GetAllSubscriptionPlans()
    {
        try
        {
            var response = await _paymentService.GetAllSubscriptionPlansAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all subscription plans");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<SubscriptionPlanDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving subscription plans"
            });
        }
    }

    /// <summary>
    /// Get active subscription plans only
    /// </summary>
    [HttpGet("subscription-plans/active")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<SubscriptionPlanDto>>>> GetActiveSubscriptionPlans()
    {
        try
        {
            var response = await _paymentService.GetActiveSubscriptionPlansAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active subscription plans");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<SubscriptionPlanDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving active subscription plans"
            });
        }
    }

    /// <summary>
    /// Get subscription plan by ID
    /// </summary>
    [HttpGet("subscription-plans/{planId:guid}")]
    public async Task<ActionResult<ServiceResponse<SubscriptionPlanDto>>> GetSubscriptionPlanById(Guid planId)
    {
        try
        {
            var response = await _paymentService.GetSubscriptionPlanByIdAsync(planId);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for getting subscription plan: {PlanId}", planId);
            return BadRequest(new ServiceResponse<SubscriptionPlanDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription plan {PlanId}", planId);
            return StatusCode(500, new ServiceResponse<SubscriptionPlanDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the subscription plan"
            });
        }
    }

    /// <summary>
    /// Create a new subscription plan
    /// </summary>
    [HttpPost("subscription-plans")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<SubscriptionPlanDto>>> CreateSubscriptionPlan([FromBody] CreateSubscriptionPlanRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<SubscriptionPlanDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var response = await _paymentService.CreateSubscriptionPlanAsync(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return CreatedAtAction(
                nameof(GetSubscriptionPlanById),
                new { planId = response.Data?.PlanId },
                response);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for creating subscription plan");
            return BadRequest(new ServiceResponse<SubscriptionPlanDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating subscription plan");
            return BadRequest(new ServiceResponse<SubscriptionPlanDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error creating subscription plan");
            return BadRequest(new ServiceResponse<SubscriptionPlanDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating subscription plan");
            return StatusCode(500, new ServiceResponse<SubscriptionPlanDto>
            {
                Success = false,
                Message = "An error occurred while creating the subscription plan"
            });
        }
    }

    /// <summary>
    /// Update an existing subscription plan
    /// </summary>
    [HttpPut("subscription-plans/{planId:guid}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<SubscriptionPlanDto>>> UpdateSubscriptionPlan(
        Guid planId,
        [FromBody] UpdateSubscriptionPlanRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<SubscriptionPlanDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var response = await _paymentService.UpdateSubscriptionPlanAsync(planId, request);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for updating subscription plan: {PlanId}", planId);
            return BadRequest(new ServiceResponse<SubscriptionPlanDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for updating subscription plan: {PlanId}", planId);
            return BadRequest(new ServiceResponse<SubscriptionPlanDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error updating subscription plan: {PlanId}", planId);
            return BadRequest(new ServiceResponse<SubscriptionPlanDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating subscription plan {PlanId}", planId);
            return StatusCode(500, new ServiceResponse<SubscriptionPlanDto>
            {
                Success = false,
                Message = "An error occurred while updating the subscription plan"
            });
        }
    }

    /// <summary>
    /// Inactivate a subscription plan (soft delete)
    /// </summary>
    [HttpPatch("subscription-plans/{planId:guid}/inactivate")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<bool>>> InactivateSubscriptionPlan(Guid planId)
    {
        try
        {
            var response = await _paymentService.InactivateSubscriptionPlanAsync(planId);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for inactivating subscription plan: {PlanId}", planId);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error inactivating subscription plan: {PlanId}", planId);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error inactivating subscription plan {PlanId}", planId);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while inactivating the subscription plan"
            });
        }
    }

    /// <summary>
    /// Delete a subscription plan (hard delete)
    /// </summary>
    [HttpDelete("subscription-plans/{planId:guid}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteSubscriptionPlan(Guid planId)
    {
        try
        {
            var response = await _paymentService.DeleteSubscriptionPlanAsync(planId);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for deleting subscription plan: {PlanId}", planId);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error deleting subscription plan: {PlanId}", planId);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting subscription plan {PlanId}", planId);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting the subscription plan"
            });
        }
    }

    /// <summary>
    /// Get all company subscriptions
    /// </summary>
    [HttpGet("company-subscriptions")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<CompanySubscriptionDto>>>> GetAllCompanySubscriptions()
    {
        try
        {
            var response = await _paymentService.GetAllCompanySubscriptionsAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all company subscriptions");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<CompanySubscriptionDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving company subscriptions"
            });
        }
    }

    /// <summary>
    /// Get company subscription by ID
    /// </summary>
    [HttpGet("company-subscriptions/{subscriptionId:guid}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<CompanySubscriptionDto>>> GetCompanySubscriptionById(Guid subscriptionId)
    {
        try
        {
            var response = await _paymentService.GetCompanySubscriptionByIdAsync(subscriptionId);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for getting company subscription: {SubscriptionId}", subscriptionId);
            return BadRequest(new ServiceResponse<CompanySubscriptionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new ServiceResponse<CompanySubscriptionDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the company subscription"
            });
        }
    }

    /// <summary>
    /// Get company subscriptions by company ID
    /// </summary>
    [HttpGet("company-subscriptions/company/{companyId:guid}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<CompanySubscriptionDto>>>> GetCompanySubscriptionsByCompanyId(Guid companyId)
    {
        try
        {
            Guid organizationId = GetCurrentOrganizationId();
            companyId = organizationId;
            var response = await _paymentService.GetCompanySubscriptionsByCompanyIdAsync(companyId);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for getting subscriptions for company: {CompanyId}", companyId);
            return BadRequest(new ServiceResponse<IReadOnlyList<CompanySubscriptionDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscriptions for company {CompanyId}", companyId);
            return StatusCode(500, new ServiceResponse<IReadOnlyList<CompanySubscriptionDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving company subscriptions"
            });
        }
    }

    /// <summary>
    /// Get detailed company subscription info (active) by company ID (for header display)
    /// </summary>
    [HttpGet("company-subscription-details/{companyId:guid}")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<CompanySubscriptionDetailsDto>>> GetCompanySubscriptionDetailsByCompanyId(Guid companyId)
    {
        try
        {
            Guid organizationId = GetCurrentOrganizationId();
            companyId = organizationId; // Ensure user can only access their own org data
            
            var response = await _paymentService.GetCompanySubscriptionDetailsByCompanyIdAsync(companyId);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for getting subscription details for company: {CompanyId}", companyId);
            return BadRequest(new ServiceResponse<CompanySubscriptionDetailsDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription details for company {CompanyId}", companyId);
            return StatusCode(500, new ServiceResponse<CompanySubscriptionDetailsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving company subscription details"
            });
        }
    }

    /// <summary>
    /// Create a new company subscription
    /// </summary>
    [HttpPost("company-subscriptions")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<CompanySubscriptionDto>>> CreateCompanySubscription([FromBody] CreateCompanySubscriptionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<CompanySubscriptionDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var organizationId = GetCurrentOrganizationId();
            request.CompanyId = organizationId;
            var response = await _paymentService.CreateCompanySubscriptionAsync(request);

            if (!response.Success)
            {
                return BadRequest(response); // Or whatever status is appropriate for failure
            }

            return CreatedAtAction(
                nameof(GetCompanySubscriptionById),
                new { subscriptionId = response.Data?.SubscriptionId },
                response);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for creating company subscription");
            return BadRequest(new ServiceResponse<CompanySubscriptionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating company subscription");
            return BadRequest(new ServiceResponse<CompanySubscriptionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error creating company subscription");
            return BadRequest(new ServiceResponse<CompanySubscriptionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating company subscription");
            return StatusCode(500, new ServiceResponse<CompanySubscriptionDto>
            {
                Success = false,
                Message = "An error occurred while creating the company subscription"
            });
        }
    }

    /// <summary>
    /// Update an existing company subscription
    /// </summary>
    [HttpPut("company-subscriptions/{subscriptionId:guid}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<CompanySubscriptionDto>>> UpdateCompanySubscription(
        Guid subscriptionId,
        [FromBody] UpdateCompanySubscriptionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<CompanySubscriptionDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var response = await _paymentService.UpdateCompanySubscriptionAsync(subscriptionId, request);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for updating company subscription: {SubscriptionId}", subscriptionId);
            return BadRequest(new ServiceResponse<CompanySubscriptionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for updating company subscription: {SubscriptionId}", subscriptionId);
            return BadRequest(new ServiceResponse<CompanySubscriptionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error updating company subscription: {SubscriptionId}", subscriptionId);
            return BadRequest(new ServiceResponse<CompanySubscriptionDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating company subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new ServiceResponse<CompanySubscriptionDto>
            {
                Success = false,
                Message = "An error occurred while updating the company subscription"
            });
        }
    }

    /// <summary>
    /// Delete a company subscription
    /// </summary>
    [HttpDelete("company-subscriptions/{subscriptionId:guid}")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteCompanySubscription(Guid subscriptionId)
    {
        try
        {
            var response = await _paymentService.DeleteCompanySubscriptionAsync(subscriptionId);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for deleting company subscription: {SubscriptionId}", subscriptionId);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Business logic error deleting company subscription: {SubscriptionId}", subscriptionId);
            return BadRequest(new ServiceResponse<bool>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting company subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting the company subscription"
            });
        }
    }
    /// <summary>
    /// Get Current Company
    /// </summary>
    /// private Guid GetCurrentOrganizationId()
    private Guid GetCurrentOrganizationId()
    {
        var orgIdClaim = User.FindFirstValue("organizationId");

        if (string.IsNullOrEmpty(orgIdClaim))
        {
            _logger.LogError("Organization ID claim not found in JWT token");
            throw new UnauthorizedAccessException("Organization ID not found in token");
        }

        if (!Guid.TryParse(orgIdClaim, out var organizationId))
        {
            _logger.LogError("Invalid organization ID format in JWT token: {OrganizationId}", orgIdClaim);
            throw new UnauthorizedAccessException("Invalid organization ID format");
        }

        return organizationId;
    }

    // ========== PAYMENT MANAGEMENT ENDPOINTS ==========

    private Guid GetCompanyIdFromToken()
    {
        var companyIdClaim = User.FindFirstValue("organizationId")
            ?? User.FindFirstValue("companyId");
        if (string.IsNullOrEmpty(companyIdClaim) || !Guid.TryParse(companyIdClaim, out var companyId))
        {
            throw new UnauthorizedAccessException("Company ID not found in token");
        }
        return companyId;
    }

    /// <summary>
    /// Create a checkout session for plan purchase
    /// </summary>
    [HttpPost("create-checkout-session")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<CheckoutSessionResponse>>> CreateCheckoutSession(
        [FromBody] CreateCheckoutSessionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ServiceResponse<CheckoutSessionResponse> { Success = false, Message = "Invalid request" });

            var companyId = GetCompanyIdFromToken();
            var response = await _transactionService.CreateCheckoutSessionAsync(companyId, request);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ServiceResponse<CheckoutSessionResponse> { Success = false, Message = "Unauthorized" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session");
            return StatusCode(500, new ServiceResponse<CheckoutSessionResponse> { Success = false, Message = "An error occurred" });
        }
    }

    /// <summary>
    /// Verify payment after gateway redirect
    /// </summary>
    [HttpPost("verify-payment")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<PaymentConfirmationDto>>> VerifyPayment(
        [FromBody] VerifyPaymentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ServiceResponse<PaymentConfirmationDto> { Success = false, Message = "Invalid request" });

            var response = await _transactionService.VerifyPaymentAsync(request);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment");
            return StatusCode(500, new ServiceResponse<PaymentConfirmationDto> { Success = false, Message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get transaction history for the authenticated company
    /// </summary>
    [HttpGet("transactions")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<PagedResult<TransactionDto>>>> GetMyTransactions(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var companyId = GetCompanyIdFromToken();
            var response = await _transactionService.GetTransactionsByCompanyAsync(companyId, page, Math.Min(pageSize, 50));
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ServiceResponse<PagedResult<TransactionDto>> { Success = false, Message = "Unauthorized" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions");
            return StatusCode(500, new ServiceResponse<PagedResult<TransactionDto>> { Success = false, Message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get invoices for the authenticated company
    /// </summary>
    [HttpGet("invoices")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<PagedResult<InvoiceDto>>>> GetMyInvoices(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var companyId = GetCompanyIdFromToken();
            var response = await _transactionService.GetInvoicesByCompanyAsync(companyId, page, Math.Min(pageSize, 50));
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ServiceResponse<PagedResult<InvoiceDto>> { Success = false, Message = "Unauthorized" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices");
            return StatusCode(500, new ServiceResponse<PagedResult<InvoiceDto>> { Success = false, Message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get a specific invoice by ID
    /// </summary>
    [HttpGet("invoices/{invoiceId:guid}")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse<InvoiceDto>>> GetInvoice(Guid invoiceId)
    {
        try
        {
            var response = await _transactionService.GetInvoiceByIdAsync(invoiceId);
            if (!response.Success)
                return NotFound(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice {InvoiceId}", invoiceId);
            return StatusCode(500, new ServiceResponse<InvoiceDto> { Success = false, Message = "An error occurred" });
        }
    }

}


