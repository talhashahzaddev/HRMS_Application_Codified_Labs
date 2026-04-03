using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.Util;
using System.Security.Claims;
using static HRMS.API.Models.DTOs.HelpDeskDto;

namespace HRMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HelpDeskController : ControllerBase
    {

        private readonly IAttendanceService _attendanceService;
        private readonly ILogger<AttendanceController> _logger;
        private readonly IHelpDeskService _helpDeskService;

        public HelpDeskController(IAttendanceService attendanceService, ILogger<AttendanceController> logger, IHelpDeskService helpDeskService)
        {
            _attendanceService = attendanceService;
            _logger = logger;
            _helpDeskService = helpDeskService;
        }

        [HttpPost("create-ticket")]
        public async Task<ActionResult<ServiceResponse<TicketDto>>> CreateTicket([FromBody] TicketRequest ticketRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ServiceResponse<TicketDto>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = ModelState.Values
                                         .SelectMany(v => v.Errors)
                                         .Select(e => e.ErrorMessage)
                                         .ToList()
                    });
                }

                var organizationId = GetCurrentOrganizationId();
                var currentEmployeeId = await GetCurrentEmployeeId();

                var createdTicket = await _helpDeskService.CreateTicketAsync(ticketRequest, organizationId, currentEmployeeId);

                return CreatedAtAction(nameof(CreateTicket),
                    //new { id = createdTicket.TicketId },
                    new ServiceResponse<TicketDto>
                    {
                        Success = true,
                        Message = "Ticket created successfully",
                        Data = createdTicket
                    });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "Null request provided for creating ticket");
                return BadRequest(new ServiceResponse<TicketDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for creating ticket");
                return BadRequest(new ServiceResponse<TicketDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Business logic error while creating ticket");
                return BadRequest(new ServiceResponse<TicketDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ticket");
                return StatusCode(500, new ServiceResponse<TicketDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the ticket"
                });
            }
        }

        [HttpPut("update-ticket")]
        public async Task<ActionResult<ServiceResponse<TicketDto>>> UpdateTicket([FromBody] UpdateTicketRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ServiceResponse<TicketDto>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = ModelState.Values
                                           .SelectMany(v => v.Errors)
                                           .Select(e => e.ErrorMessage)
                                           .ToList()
                    });
                }

                var organizationId = GetCurrentOrganizationId();
                var currentEmployeeId = await GetCurrentEmployeeId();

                var updatedTicket = await _helpDeskService.UpdateTicketAsync(
                    request,
                    organizationId,
                    currentEmployeeId
                );

                return Ok(new ServiceResponse<TicketDto>
                {
                    Success = true,
                    Message = "Ticket updated successfully",
                   
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ServiceResponse<TicketDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ServiceResponse<TicketDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ticket");

                return StatusCode(500, new ServiceResponse<TicketDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the ticket"
                });
            }
        }

        //Getting all tickets for employee and admin
        [HttpGet("get-allTicket")]
        public async Task<ActionResult<ServiceResponse<List<TicketDto>>>> GetAllTickets([FromQuery] TicketSearchModel? searchModel)
        {
            try
            {
                var organizationId = GetCurrentOrganizationId();
                var currentEmployeeId = await GetCurrentEmployeeId();
                var tickets = await _helpDeskService.GetAllTicketsAsync(organizationId, currentEmployeeId, searchModel);
                return Ok(new ServiceResponse<List<TicketDto>>
                {
                    Success = true,
                    Data = tickets
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while retrieving tickets");
                return BadRequest(new ServiceResponse<List<TicketDto>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tickets");
                return StatusCode(500, new ServiceResponse<List<TicketDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving tickets"
                });
            }
        }

        //Getting ticket by id
        [HttpGet("get-ticket/{ticketId}")]
        public async Task<ActionResult<ServiceResponse<TicketDto>>> GetTicketById(Guid ticketId)
        {
            try
            {
                var organizationId = GetCurrentOrganizationId();
                var currentEmployeeId = await GetCurrentEmployeeId();

                // Fetch the ticket using the service
                var ticket = await _helpDeskService.GetTicketByIdAsync(organizationId, currentEmployeeId, ticketId);
                if (ticket == null)
                {
                    return NotFound(new ServiceResponse<TicketDto>
                    {
                        Success = false,
                        Message = "Ticket not found"
                    });
                }
                return Ok(new ServiceResponse<TicketDto>
                {
                    Success = true,
                    Data = ticket
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while retrieving ticket {TicketId}", ticketId);
                return BadRequest(new ServiceResponse<TicketDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ticket {TicketId}", ticketId);
                return StatusCode(500, new ServiceResponse<TicketDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the ticket"
                });
            }
        }

        //Get Assigned tickets to employee and there own tickets too
        [HttpGet("get-AssignedTickets")]
        public async Task<ActionResult<ServiceResponse<List<TicketDto>>>> GetAssignedTickets([FromQuery] TicketSearchModel? searchModel)
        {
            try
            {
                var organizationId = GetCurrentOrganizationId();
                var currentEmployeeId = await GetCurrentEmployeeId();
                var tickets = await _helpDeskService.GetAllAssignedTicket(organizationId, currentEmployeeId, searchModel);
                return Ok(new ServiceResponse<List<TicketDto>>
                {
                    Success = true,
                    Data = tickets
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while retrieving tickets");
                return BadRequest(new ServiceResponse<List<TicketDto>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tickets");
                return StatusCode(500, new ServiceResponse<List<TicketDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving tickets"
                });
            }
        }

        [HttpDelete("delete-ticket/{ticketId}")]
        public async Task<ActionResult<ServiceResponse<bool>>> DeleteTicket(Guid ticketId)
        {
            try
            {
                if (ticketId == Guid.Empty)
                    throw new ArgumentException("TicketId is required");
                var organizationId = GetCurrentOrganizationId();
                // Call your service to delete the ticket
                var result = await _helpDeskService.DeleteTicketAsync(organizationId, ticketId);
                return Ok(new ServiceResponse<bool>
                {
                    Success = result,
                    Data = result,
                    Message = result ? "Ticket deleted successfully" : "Failed to delete ticket"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while deleting ticket");
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ticket");
                return StatusCode(500, new ServiceResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = "An error occurred while deleting the ticket"
                });
            }
        }

        [HttpPost("create-group")]
        public async Task<ActionResult<ServiceResponse<TicketGroupDto>>> CreateGroup([FromBody] CreateTicketGroupRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.GroupTitle))
                {
                    return BadRequest(new ServiceResponse<TicketGroupDto>
                    {
                        Success = false,
                        Message = "Group title is required"
                    });
                }
                var organizationId = GetCurrentOrganizationId();
                var result = await _helpDeskService.CreateTicketGroupAsync(organizationId, request);
                return Ok(new ServiceResponse<TicketGroupDto>
                {
                    Success = true,
                    Message = "Group created successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ticket group");
                return StatusCode(500, new ServiceResponse<TicketGroupDto>
                {
                    Success = false,
                    Message = "Error creating group"
                });
            }
        }

        [HttpPut("update-group")]
        public async Task<ActionResult<ServiceResponse<bool>>> UpdateGroup([FromBody] UpdateTicketGroupRequest request)
        {
            try
            {
                // 🔹 Validation
                if (request == null || request.GroupId == Guid.Empty)
                {
                    return BadRequest(new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "GroupId is required"
                    });
                }
                if (string.IsNullOrWhiteSpace(request.GroupTitle))
                {
                    return BadRequest(new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "Group title is required"
                    });
                }
                var organizationId = GetCurrentOrganizationId();
                // 🔹 Call service
                var result = await _helpDeskService.UpdateTicketGroupAsync(organizationId, request);
                return Ok(new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "Group updated successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ticket group");

                return StatusCode(500, new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Error updating group"
                });
            }
        }

        [HttpGet("get-all-groups")]
        public async Task<ActionResult<ServiceResponse<List<TicketGroupDto>>>> GetAllGroups([FromQuery] string? search = null,[FromQuery] Guid? departmentId = null,[FromQuery] Guid? categoryId=null)
        {
            try
            {
                var organizationId = GetCurrentOrganizationId();

                // Call service to fetch all groups for the organization
                var groups = await _helpDeskService.GetAllTicketGroupsAsync(organizationId, search,departmentId,categoryId);
                return Ok(new ServiceResponse<List<TicketGroupDto>>
                {
                    Success = true,
                    Data = groups
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all ticket groups for organization {OrganizationId}", GetCurrentOrganizationId());
                return StatusCode(500, new ServiceResponse<List<TicketGroupDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving ticket groups"
                });
            }
        }

        [HttpGet("get-group/{groupId}")]
        public async Task<ActionResult<ServiceResponse<TicketGroupDto>>> GetGroupById(Guid groupId)
        {
            try
            {
                var organizationId = GetCurrentOrganizationId();

                // Call service to fetch group
                var group = await _helpDeskService.GetTicketGroupByIdAsync(organizationId, groupId);

                if (group == null)
                {
                    return NotFound(new ServiceResponse<TicketGroupDto>
                    {
                        Success = false,
                        Message = "Ticket group not found"
                    });
                }

                return Ok(new ServiceResponse<TicketGroupDto>
                {
                    Success = true,
                    Data = group
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching ticket group {GroupId}", groupId);
                return StatusCode(500, new ServiceResponse<TicketGroupDto>
                {
                    Success = false,
                    Message = "Error retrieving group"
                });
            }
        }
        //Delete Group
        [HttpDelete("delete-group/{groupId}")]
        public async Task<ActionResult<ServiceResponse<bool>>> DeleteGroup(Guid groupId)
        {
            try
            {
                var organizationId = GetCurrentOrganizationId();
                // Call service to delete the group
                var result = await _helpDeskService.DeleteTicketGroupAsync(organizationId, groupId);
                if (!result)
                {
                    return NotFound(new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "Ticket group not found or could not be deleted"
                    });
                }

                return Ok(new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "Ticket group deleted successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ticket group {GroupId}", groupId);
                return StatusCode(500, new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting the group",
                    Data = false
                });
            }
        }

        [HttpPost("assign-ticket")]
        public async Task<ActionResult<ServiceResponse<bool>>> AssignTicket([FromBody] AssignTicketRequest request)
        {
            try
            {
                if (request == null || request.TicketId == Guid.Empty)
                {
                    return BadRequest(new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid request"
                    });
                }
                var assignedBy = await GetCurrentEmployeeId();
                var organizationId = GetCurrentOrganizationId();

                await _helpDeskService.AssignTicketAsync(request, assignedBy,organizationId);

                return Ok(new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "Ticket assigned successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning ticket");

                return StatusCode(500, new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Error assigning ticket"
                });
            }
        }

        [HttpPost("create-message")]
        public async Task<ActionResult<ServiceResponse<bool>>> CreateMessage([FromBody] TicketMessageRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Errors = ModelState.Values
                                          .SelectMany(v => v.Errors)
                                          .Select(e => e.ErrorMessage)
                                          .ToList()
                    });
                }

                var currentEmployeeId = await GetCurrentEmployeeId(); // assuming method exists
                var organizationId =  GetCurrentOrganizationId();

                // Optionally override sender if not provided
                    request.SenderId = currentEmployeeId;

                var createdMessage = await _helpDeskService.CreateChatAsync(request,organizationId);
                return Ok(new ServiceResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Message created successfully"
                });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "Null request provided for creating ticket message");
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for creating ticket message");
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Business logic error while creating ticket message");
                return BadRequest(new ServiceResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ticket message");
                return StatusCode(500, new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while creating the ticket message"
                });
            }
        }

        [HttpGet("my-messages")]
        public async Task<ActionResult<ServiceResponse<List<TicketMessageDto>>>> GetEmployeeMessages(Guid ticketId)
        {
            try
            {
                var currentEmployeeId = await GetCurrentEmployeeId();
                var organizationId = GetCurrentOrganizationId();
                if (currentEmployeeId == Guid.Empty || organizationId == Guid.Empty)
                {
                    return BadRequest(new ServiceResponse<List<TicketMessageDto>>
                    {
                        Success = false,
                        Message = "Unable to determine current employee or organization."
                    });
                }
                // Fetch messages sent or received by current employee
                var messages = await _helpDeskService.GetMessagesByEmployeeIdAsync(organizationId, currentEmployeeId, ticketId);

                return Ok(new ServiceResponse<List<TicketMessageDto>>
                {
                    Success = true,
                    Message = "Messages retrieved successfully",
                    Data = messages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving messages for current employee");
                return StatusCode(500, new ServiceResponse<List<TicketMessageDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving messages"
                });
            }
        }


        [HttpPost("create-category")]
        public async Task<ActionResult<ServiceResponse<bool>>> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            try
            {
                // ✅ Validation
                if (request == null || string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid category data"
                    });
                }
                // (Optional) Get logged-in employee if needed
                var CurrentEmpid = await GetCurrentEmployeeId();
                var oganizationId = GetCurrentOrganizationId();
                // ✅ Call service
                await _helpDeskService.CreateCategoriesAsync(oganizationId,CurrentEmpid, request );
                return Ok(new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "Category created successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");

                return StatusCode(500, new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Error creating category"
                });
            }
        }

        [HttpGet("get-categories")]
        public async Task<ActionResult<ServiceResponse<List<CategoryDto>>>> GetCategories([FromQuery] searchCateogry? search)
        {
            try
            {
                var organizationId = GetCurrentOrganizationId();
                
                var categories = await _helpDeskService.GetCategoriesAsync(organizationId, search);

                return Ok(new ServiceResponse<List<CategoryDto>>
                {
                    Success = true,
                    Message = "Categories fetched successfully",
                    Data = categories
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching categories");

                return StatusCode(500, new ServiceResponse<List<CategoryDto>>
                {
                    Success = false,
                    Message = "Error fetching categories"
                });
            }
        }

        [HttpPut("update-category")]
        public async Task<ActionResult<ServiceResponse<bool>>> UpdateCategory([FromBody] UpdateCategoryRequest request)
        {
            try
            {
                if (request == null || request.CategoryId == Guid.Empty || string.IsNullOrWhiteSpace(request.CategoryName))
                {
                    return BadRequest(new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid category data"
                    });
                }

                var organizationId = GetCurrentOrganizationId();
                var currentEmpid = await GetCurrentEmployeeId();

                await _helpDeskService.UpdateCategoryAsync(organizationId, request,currentEmpid);

                return Ok(new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "Category updated successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");

                return StatusCode(500, new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Error updating category"
                });
            }
        }

        [HttpDelete("delete-category/{categoryId}")]
        public async Task<ActionResult<ServiceResponse<bool>>> DeleteCategory(Guid categoryId)
        {
            try
            {
                if (categoryId == Guid.Empty)
                {
                    return BadRequest(new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid category ID"
                    });
                }

                var organizationId = GetCurrentOrganizationId();

                await _helpDeskService.DeleteCategoryAsync(organizationId, categoryId);

                return Ok(new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "Category deleted successfully",
                    Data = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");

                return StatusCode(500, new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Error deleting category"
                });
            }
        }

        [HttpGet("get-category/{categoryId}")]
        public async Task<ActionResult<ServiceResponse<CategoryDto>>> GetCategoryById(Guid categoryId)
        {
            try
            {
                if (categoryId == Guid.Empty)
                {
                    return BadRequest(new ServiceResponse<CategoryDto>
                    {
                        Success = false,
                        Message = "Invalid category ID"
                    });
                }

                var organizationId = GetCurrentOrganizationId();

                var category = await _helpDeskService.GetCategoryByIdAsync(organizationId, categoryId);

                if (category == null)
                {
                    return NotFound(new ServiceResponse<CategoryDto>
                    {
                        Success = false,
                        Message = "Category not found"
                    });
                }

                return Ok(new ServiceResponse<CategoryDto>
                {
                    Success = true,
                    Message = "Category fetched successfully",
                    Data = category
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching category");

                return StatusCode(500, new ServiceResponse<CategoryDto>
                {
                    Success = false,
                    Message = "Error fetching category"
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
}
