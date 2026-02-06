using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpenseController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly IAttendanceService _attendanceService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<ExpenseController> _logger;

    public ExpenseController(
        IExpenseService expenseService,
        IAttendanceService attendanceService,
        ICloudinaryService cloudinaryService,
        ILogger<ExpenseController> logger)
    {
        _expenseService = expenseService;
        _attendanceService = attendanceService;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    // ==================== Expense Categories ====================
    [HttpGet("categories")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<List<ExpenseCategoryDto>>>> GetExpenseCategories()
    {
        try
        {
            var organizationId = GetCurrentOrganizationId();
            var categories = await _expenseService.GetExpenseCategoriesAsync(organizationId);
            return Ok(new ServiceResponse<List<ExpenseCategoryDto>>
            {
                Success = true,
                Data = categories?.ToList() ?? new List<ExpenseCategoryDto>()
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access getting expense categories");
            return Unauthorized(new ServiceResponse<List<ExpenseCategoryDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expense categories");
            return StatusCode(500, new ServiceResponse<List<ExpenseCategoryDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving expense categories"
            });
        }
    }

    [HttpGet("categories/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<ExpenseCategoryDto>>> GetExpenseCategoryById(Guid id)
    {
        try
        {
            var category = await _expenseService.GetExpenseCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound(new ServiceResponse<ExpenseCategoryDto>
                {
                    Success = false,
                    Message = "Expense category not found"
                });
            }
            return Ok(new ServiceResponse<ExpenseCategoryDto>
            {
                Success = true,
                Data = category
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expense category {CategoryId}", id);
            return StatusCode(500, new ServiceResponse<ExpenseCategoryDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the expense category"
            });
        }
    }

    [HttpPost("categories")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<ExpenseCategoryDto>>> CreateExpenseCategory([FromBody] CreateExpenseCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<ExpenseCategoryDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var organizationId = GetCurrentOrganizationId();
            var category = await _expenseService.CreateExpenseCategoryAsync(organizationId, request);
            return CreatedAtAction(nameof(GetExpenseCategoryById), new { id = category.CategoryId }, new ServiceResponse<ExpenseCategoryDto>
            {
                Success = true,
                Message = "Expense category created successfully",
                Data = category
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access creating expense category");
            return Unauthorized(new ServiceResponse<ExpenseCategoryDto> { Success = false, Message = ex.Message });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for creating expense category");
            return BadRequest(new ServiceResponse<ExpenseCategoryDto> { Success = false, Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating expense category");
            return BadRequest(new ServiceResponse<ExpenseCategoryDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense category");
            return StatusCode(500, new ServiceResponse<ExpenseCategoryDto>
            {
                Success = false,
                Message = "An error occurred while creating expense category"
            });
        }
    }

    [HttpPut("categories/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<ExpenseCategoryDto>>> UpdateExpenseCategory(Guid id, [FromBody] UpdateExpenseCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<ExpenseCategoryDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var category = await _expenseService.UpdateExpenseCategoryAsync(id, request);
            if (category == null)
            {
                return NotFound(new ServiceResponse<ExpenseCategoryDto>
                {
                    Success = false,
                    Message = "Expense category not found"
                });
            }
            return Ok(new ServiceResponse<ExpenseCategoryDto>
            {
                Success = true,
                Message = "Expense category updated successfully",
                Data = category
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for updating expense category");
            return BadRequest(new ServiceResponse<ExpenseCategoryDto> { Success = false, Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for updating expense category");
            return BadRequest(new ServiceResponse<ExpenseCategoryDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense category {CategoryId}", id);
            return StatusCode(500, new ServiceResponse<ExpenseCategoryDto>
            {
                Success = false,
                Message = "An error occurred while updating expense category"
            });
        }
    }

    [HttpDelete("categories/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteExpenseCategory(Guid id)
    {
        try
        {
            var deleted = await _expenseService.DeleteExpenseCategoryAsync(id);
            if (!deleted)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Expense category not found"
                });
            }
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Expense category deleted successfully",
                Data = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense category {CategoryId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting expense category"
            });
        }
    }

     //==================== Expenses ====================
    [HttpGet("expenses")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<PagedResult<ExpenseDto>>>> GetExpenses(
        [FromQuery] Guid? employeeId,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            var organizationId = GetCurrentOrganizationId();
            var result = await _expenseService.GetExpensesPagedAsync(organizationId, employeeId, status, pageNumber, pageSize);
            return Ok(new ServiceResponse<PagedResult<ExpenseDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access getting expenses");
            return Unauthorized(new ServiceResponse<PagedResult<ExpenseDto>> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expenses");
            return StatusCode(500, new ServiceResponse<PagedResult<ExpenseDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving expenses"
            });
        }
    }

    [HttpGet("expenses/me")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<PagedResult<ExpenseDto>>>> GetAllExpensesByEmployeeId(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var result = await _expenseService.GetExpensesPagedAsync(organizationId, employeeId, null, pageNumber, pageSize);
            return Ok(new ServiceResponse<PagedResult<ExpenseDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access getting expenses by employee");
            return Unauthorized(new ServiceResponse<PagedResult<ExpenseDto>> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expenses by employee");
            return StatusCode(500, new ServiceResponse<PagedResult<ExpenseDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving expenses"
            });
        }
    }

    [HttpGet("expenses/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<ExpenseDto>>> GetExpenseById(Guid id)
    {
        try
        {
            var expense = await _expenseService.GetExpenseByIdAsync(id);
            if (expense == null)
            {
                return NotFound(new ServiceResponse<ExpenseDto>
                {
                    Success = false,
                    Message = "Expense not found"
                });
            }
            return Ok(new ServiceResponse<ExpenseDto>
            {
                Success = true,
                Data = expense
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expense {ExpenseId}", id);
            return StatusCode(500, new ServiceResponse<ExpenseDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the expense"
            });
        }
    }

    [HttpPost("upload-receipt")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ServiceResponse<string>>> UploadReceipt(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ServiceResponse<string> { Success = false, Message = "No file provided." });
        }
        var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/jpg", "image/png", "image/gif" };
        if (!allowedTypes.Contains(file.ContentType?.ToLowerInvariant()))
        {
            return BadRequest(new ServiceResponse<string> { Success = false, Message = "Allowed formats: PDF, JPG, PNG, GIF." });
        }
        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new ServiceResponse<string> { Success = false, Message = "File size must be 5 MB or less." });
        }
        try
        {
            var url = await _cloudinaryService.UploadReceiptAsync(file);
            if (string.IsNullOrEmpty(url))
            {
                return StatusCode(500, new ServiceResponse<string> { Success = false, Message = "Upload failed." });
            }
            return Ok(new ServiceResponse<string> { Success = true, Data = url });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error uploading receipt");
            return BadRequest(new ServiceResponse<string> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading receipt");
            return StatusCode(500, new ServiceResponse<string> { Success = false, Message = "An error occurred while uploading the receipt." });
        }
    }

    [HttpPost("expenses")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<ExpenseDto>>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<ExpenseDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var isSuperAdmin = User.IsInRole("Super Admin");
            var approvedByEmployeeId = isSuperAdmin ? employeeId : (Guid?)null;

            var expense = await _expenseService.CreateExpenseAsync(request, organizationId, employeeId, isSuperAdmin, approvedByEmployeeId);
            return CreatedAtAction(nameof(GetExpenseById), new { id = expense.ExpenseId }, new ServiceResponse<ExpenseDto>
            {
                Success = true,
                Message = isSuperAdmin ? "Expense created and approved successfully" : "Expense created successfully",
                Data = expense
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access creating expense");
            return Unauthorized(new ServiceResponse<ExpenseDto> { Success = false, Message = ex.Message });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for creating expense");
            return BadRequest(new ServiceResponse<ExpenseDto> { Success = false, Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating expense");
            return BadRequest(new ServiceResponse<ExpenseDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            return StatusCode(500, new ServiceResponse<ExpenseDto>
            {
                Success = false,
                Message = "An error occurred while creating expense"
            });
        }
    }

    [HttpPut("expenses/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<ExpenseDto>>> UpdateExpense(Guid id, [FromBody] UpdateExpenseRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<ExpenseDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var approvedByEmployeeId = User.IsInRole("Super Admin") ? await GetCurrentEmployeeId() : (Guid?)null;
            var expense = await _expenseService.UpdateExpenseAsync(id, request, approvedByEmployeeId);
            if (expense == null)
            {
                return NotFound(new ServiceResponse<ExpenseDto>
                {
                    Success = false,
                    Message = "Expense not found"
                });
            }
            return Ok(new ServiceResponse<ExpenseDto>
            {
                Success = true,
                Message = "Expense updated successfully",
                Data = expense
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for updating expense");
            return BadRequest(new ServiceResponse<ExpenseDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense {ExpenseId}", id);
            return StatusCode(500, new ServiceResponse<ExpenseDto>
            {
                Success = false,
                Message = "An error occurred while updating expense"
            });
        }
    }

    [HttpPut("expenses/{id}/request-action")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<ExpenseDto>>> ExpenseRequestAction(Guid id, [FromBody] ExpenseRequestActionRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Action))
            {
                return BadRequest(new ServiceResponse<ExpenseDto>
                {
                    Success = false,
                    Message = "Action is required (approve or reject)"
                });
            }
            var action = request.Action.Trim().ToLowerInvariant();
            if (action != "approve" && action != "reject")
            {
                return BadRequest(new ServiceResponse<ExpenseDto>
                {
                    Success = false,
                    Message = "Action must be 'approve' or 'reject'"
                });
            }
            var approvedByEmployeeId = action == "approve" ? await GetCurrentEmployeeId() : (Guid?)null;
            var status = action == "approve" ? "Approved" : "Rejected";
            var updateRequest = new UpdateExpenseRequest { Status = status };
            var expense = await _expenseService.UpdateExpenseAsync(id, updateRequest, approvedByEmployeeId);
            if (expense == null)
            {
                return NotFound(new ServiceResponse<ExpenseDto>
                {
                    Success = false,
                    Message = "Expense not found"
                });
            }
            return Ok(new ServiceResponse<ExpenseDto>
            {
                Success = true,
                Message = action == "approve" ? "Claim approved successfully" : "Claim rejected",
                Data = expense
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access for expense request action");
            return Unauthorized(new ServiceResponse<ExpenseDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request action for expense {ExpenseId}", id);
            return StatusCode(500, new ServiceResponse<ExpenseDto>
            {
                Success = false,
                Message = "An error occurred while processing the request"
            });
        }
    }

    [HttpDelete("expenses/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager,Manager,Employee")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteExpense(Guid id)
    {
        try
        {
            var deleted = await _expenseService.DeleteExpenseAsync(id);
            if (!deleted)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Expense not found"
                });
            }
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Expense deleted successfully",
                Data = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense {ExpenseId}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting expense"
            });
        }
    }

    // ==================== Recurring Expenses ====================
    [HttpGet("recurring")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<PagedResult<RecurringExpenseDto>>>> GetRecurringExpenses(
        [FromQuery] Guid? employeeId,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            var organizationId = GetCurrentOrganizationId();
            var result = await _expenseService.GetRecurringExpensesPagedAsync(organizationId, employeeId, status, pageNumber, pageSize);
            return Ok(new ServiceResponse<PagedResult<RecurringExpenseDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access getting recurring expenses");
            return Unauthorized(new ServiceResponse<PagedResult<RecurringExpenseDto>> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recurring expenses");
            return StatusCode(500, new ServiceResponse<PagedResult<RecurringExpenseDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving recurring expenses"
            });
        }
    }

    [HttpGet("recurring/me")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<PagedResult<RecurringExpenseDto>>>> GetRecurringByEmployeeId(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var result = await _expenseService.GetRecurringExpensesPagedAsync(organizationId, employeeId, null, pageNumber, pageSize);
            return Ok(new ServiceResponse<PagedResult<RecurringExpenseDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access getting recurring expenses by employee");
            return Unauthorized(new ServiceResponse<PagedResult<RecurringExpenseDto>> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recurring expenses by employee");
            return StatusCode(500, new ServiceResponse<PagedResult<RecurringExpenseDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving recurring expenses"
            });
        }
    }

    [HttpGet("recurring/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<RecurringExpenseDto>>> GetRecurringExpenseById(Guid id)
    {
        try
        {
            var item = await _expenseService.GetRecurringExpenseByIdAsync(id);
            if (item == null)
            {
                return NotFound(new ServiceResponse<RecurringExpenseDto>
                {
                    Success = false,
                    Message = "Recurring expense not found"
                });
            }
            return Ok(new ServiceResponse<RecurringExpenseDto>
            {
                Success = true,
                Data = item
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recurring expense {Id}", id);
            return StatusCode(500, new ServiceResponse<RecurringExpenseDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the recurring expense"
            });
        }
    }

    [HttpPost("recurring")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<RecurringExpenseDto>>> CreateRecurringExpense([FromBody] CreateRecurringExpenseRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<RecurringExpenseDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var organizationId = GetCurrentOrganizationId();
            var employeeId = await GetCurrentEmployeeId();
            var isSuperAdmin = User.IsInRole("Super Admin");
            var approvedByEmployeeId = isSuperAdmin ? employeeId : (Guid?)null;

            var item = await _expenseService.CreateRecurringExpenseAsync(request, organizationId, employeeId, isSuperAdmin, approvedByEmployeeId);
            return CreatedAtAction(nameof(GetRecurringExpenseById), new { id = item.RecurringExpenseId }, new ServiceResponse<RecurringExpenseDto>
            {
                Success = true,
                Message = isSuperAdmin ? "Recurring expense created and approved successfully" : "Recurring expense created successfully",
                Data = item
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access creating recurring expense");
            return Unauthorized(new ServiceResponse<RecurringExpenseDto> { Success = false, Message = ex.Message });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for creating recurring expense");
            return BadRequest(new ServiceResponse<RecurringExpenseDto> { Success = false, Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for creating recurring expense");
            return BadRequest(new ServiceResponse<RecurringExpenseDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating recurring expense");
            return StatusCode(500, new ServiceResponse<RecurringExpenseDto>
            {
                Success = false,
                Message = "An error occurred while creating recurring expense"
            });
        }
    }

    [HttpPut("recurring/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<RecurringExpenseDto>>> UpdateRecurringExpense(Guid id, [FromBody] UpdateRecurringExpenseRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ServiceResponse<RecurringExpenseDto>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            var approvedByEmployeeId = User.IsInRole("Super Admin") ? await GetCurrentEmployeeId() : (Guid?)null;
            var item = await _expenseService.UpdateRecurringExpenseAsync(id, request, approvedByEmployeeId);
            if (item == null)
            {
                return NotFound(new ServiceResponse<RecurringExpenseDto>
                {
                    Success = false,
                    Message = "Recurring expense not found"
                });
            }
            return Ok(new ServiceResponse<RecurringExpenseDto>
            {
                Success = true,
                Message = "Recurring expense updated successfully",
                Data = item
            });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Null request provided for updating recurring expense");
            return BadRequest(new ServiceResponse<RecurringExpenseDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating recurring expense {Id}", id);
            return StatusCode(500, new ServiceResponse<RecurringExpenseDto>
            {
                Success = false,
                Message = "An error occurred while updating recurring expense"
            });
        }
    }

    [HttpPut("recurring/{id}/request-action")]
    [Authorize(Roles = "Super Admin")]
    public async Task<ActionResult<ServiceResponse<RecurringExpenseDto>>> RecurringRequestAction(Guid id, [FromBody] ExpenseRequestActionRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Action))
            {
                return BadRequest(new ServiceResponse<RecurringExpenseDto>
                {
                    Success = false,
                    Message = "Action is required (approve or reject)"
                });
            }
            var action = request.Action.Trim().ToLowerInvariant();
            if (action != "approve" && action != "reject")
            {
                return BadRequest(new ServiceResponse<RecurringExpenseDto>
                {
                    Success = false,
                    Message = "Action must be 'approve' or 'reject'"
                });
            }
            var approvedByEmployeeId = action == "approve" ? await GetCurrentEmployeeId() : (Guid?)null;
            var status = action == "approve" ? "Approved" : "Rejected";
            var updateRequest = new UpdateRecurringExpenseRequest { Status = status };
            var item = await _expenseService.UpdateRecurringExpenseAsync(id, updateRequest, approvedByEmployeeId);
            if (item == null)
            {
                return NotFound(new ServiceResponse<RecurringExpenseDto>
                {
                    Success = false,
                    Message = "Recurring expense not found"
                });
            }
            return Ok(new ServiceResponse<RecurringExpenseDto>
            {
                Success = true,
                Message = action == "approve" ? "Recurring expense approved successfully" : "Recurring expense rejected",
                Data = item
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access for recurring request action");
            return Unauthorized(new ServiceResponse<RecurringExpenseDto> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing recurring request action {Id}", id);
            return StatusCode(500, new ServiceResponse<RecurringExpenseDto>
            {
                Success = false,
                Message = "An error occurred while processing the request"
            });
        }
    }

    [HttpDelete("recurring/{id}")]
    [Authorize(Roles = "Super Admin,HR Manager")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteRecurringExpense(Guid id)
    {
        try
        {
            var deleted = await _expenseService.DeleteRecurringExpenseAsync(id);
            if (!deleted)
            {
                return NotFound(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Recurring expense not found"
                });
            }
            return Ok(new ServiceResponse<bool>
            {
                Success = true,
                Message = "Recurring expense deleted successfully",
                Data = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting recurring expense {Id}", id);
            return StatusCode(500, new ServiceResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting recurring expense"
            });
        }
    }

    private async Task<Guid> GetCurrentEmployeeId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            _logger.LogError("User ID claim not found in JWT token");
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogError("Invalid user ID format in JWT token: {UserId}", userIdClaim);
            throw new UnauthorizedAccessException("Invalid user ID format");
        }
        var employeeId = await _attendanceService.GetEmployeeFromTable(userId);
        return employeeId;
    }

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
}
