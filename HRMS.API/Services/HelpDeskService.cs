using AutoMapper;
using HRMS.API.Repositories;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using static HRMS.API.Models.DTOs.HelpDeskDto;

namespace HRMS.API.Services
{
    public class HelpDeskService : IHelpDeskService
    {

        private readonly IHelpDeskRepository _helpDeskRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<HelpDeskService> _logger;

        public HelpDeskService(IHelpDeskRepository helpDeskRepository, IMapper mapper, IAttendanceRepository attendanceRepository, ILogger<HelpDeskService> logger, IEmailService emailService, IEmployeeRepository employeeRepository)
        {
            _helpDeskRepository = helpDeskRepository;
            _mapper = mapper;
            _attendanceRepository = attendanceRepository;
            _logger = logger;
            _emailService = emailService;
            _employeeRepository = employeeRepository;
        }



        public async Task<TicketDto> CreateTicketAsync(TicketRequest request, Guid organizationId, Guid employeeId)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Ticket request cannot be null");

            if (string.IsNullOrWhiteSpace(request.TicketTitle))
                throw new ArgumentException("Ticket title is required", nameof(request.TicketTitle));

            // Call repository method
            var ticket = await _helpDeskRepository.CreateTicketAsync(request, organizationId, employeeId);

            // Map to DTO
            //return _mapper.Map<TicketDto>(ticket);
            // Create DTO manually
            var ticketDto = new TicketDto
            {
                Ticketid = ticket,
                departmentId = request.departmentId,
                TicketTitle = request.TicketTitle,
                TicketType = request.TicketType,
                Priority = request.Priority ?? "Medium",
                Description = request.Description,
                Attachment = request.Attachment
            };

            return ticketDto;
        }


        public async Task<List<TicketDto>> GetAllTicketsAsync(Guid organizationId, Guid employeeId, TicketSearchModel searchModel)
        {
            // Validation
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty", nameof(organizationId));
            if (employeeId == Guid.Empty)
                throw new ArgumentException("EmployeeId cannot be empty", nameof(employeeId));
            // Check role
            var isAdminOrHR = await _attendanceRepository.IsAdminOrHRAsync(employeeId, organizationId);
            List<TicketDto> tickets;

            if (isAdminOrHR)
            {
                // Admin/HR → all tickets
                tickets = await _helpDeskRepository.GetAllTicketsAsync(organizationId, searchModel);
            }
            else
            {
                // Employee → only their tickets
                tickets = await _helpDeskRepository.GetAllTicketsCreatedByEmployeeAsync(organizationId, employeeId, searchModel);
            }
            return tickets ?? new List<TicketDto>();


        }

        public async Task<bool> UpdateTicketAsync(UpdateTicketRequest request, Guid organizationId, Guid currentEmployeeId)
        {
            // 🔹 validations
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty");

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.TicketId == Guid.Empty)
                throw new ArgumentException("TicketId is required");

            // ✅ STEP 1: update ticket table
            await _helpDeskRepository.UpdateTicketDataAsync(organizationId, request);

            // ✅ STEP 2: update assignment (group + employees)
            var result = await _helpDeskRepository.UpdateTicketAssignmentsAsync(request, currentEmployeeId);

            return result;
        }

        public async Task<List<TicketDto>> GetAllAssignedTicket(Guid organizationId, Guid employeeId, TicketSearchModel searchModel)
        {
            // Validation
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty", nameof(organizationId));
            if (employeeId == Guid.Empty)
                throw new ArgumentException("EmployeeId cannot be empty", nameof(employeeId));
            // Check role
            var isAdminOrHR = await _attendanceRepository.IsAdminOrHRAsync(employeeId, organizationId);
            List<TicketDto> tickets;

            if (isAdminOrHR)
            {
                // Admin/HR → all tickets
                tickets = await _helpDeskRepository.GetAllTicketsAsync(organizationId, searchModel);

            }
            else
            {
                // Employee → only their tickets
                tickets = await _helpDeskRepository.GetAllEmployeeAssignedTickets(organizationId, employeeId, searchModel);
            }
            return tickets ?? new List<TicketDto>();


        }


        public async Task<TicketDto?> GetTicketByIdAsync(Guid organizationId, Guid employeeId, Guid ticketId)
        {
            try
            {
                // Call repository to get ticket
                var ticket = await _helpDeskRepository.GetTicketByIdAsync(organizationId, ticketId);

                if (ticket == null)
                    return null;

                return ticket;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching ticket {TicketId} for organization {OrganizationId}", ticketId, organizationId);
                throw;
            }
        }

        public async Task<TicketGroupDto> CreateTicketGroupAsync(Guid organizationId, CreateTicketGroupRequest request)
        {
            // 🔹 Validation (same pattern you used everywhere)
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty", nameof(organizationId));

            if (request == null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null");

            if (string.IsNullOrWhiteSpace(request.GroupTitle))
                throw new ArgumentException("Group title is required", nameof(request.GroupTitle));

            // Optional: ensure employee list is not empty
            if (request.EmployeeIds == null || !request.EmployeeIds.Any())
                throw new ArgumentException("At least one employee must be assigned to group");

            // 🔹 Call repository
            var group = await _helpDeskRepository.CreateTicketGroupAsync(organizationId, request);

            // 🔹 Return result (no mapping needed if repo already returns DTO)
            return group;
        }
        //Get all Agents/Ticket Group
        public async Task<List<TicketGroupDto>> GetAllTicketGroupsAsync(Guid organizationId, string? search, Guid? departmentId,Guid? categoryId)
        {
            try
            {
                // Call repository to fetch all active groups in this organization
                var groups = await _helpDeskRepository.GetAllTicketGroupsAsync(organizationId, search, departmentId,categoryId);

                return groups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all ticket groups for organization {OrganizationId}", organizationId);
                throw;
            }
        }

        //Get Group By Groupid
        public async Task<TicketGroupDto?> GetTicketGroupByIdAsync(Guid organizationId, Guid groupId)
        {
            try
            {
                // Call repository to get the group
                var group = await _helpDeskRepository.GetTicketGroupByIdAsync(organizationId, groupId);

                if (group == null)
                {
                    return null; // Group not found
                }
                return group;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching ticket group {GroupId} for organization {OrganizationId}", groupId, organizationId);
                throw;
            }
        }

        //Delete Group
        public async Task<bool> DeleteTicketGroupAsync(Guid organizationId, Guid groupId)
        {
            try
            {
                // Optional: check if the group exists before deletion
                var group = await _helpDeskRepository.GetTicketGroupByIdAsync(organizationId, groupId);
                if (group == null)
                {
                    return false; // Group does not exist
                }

                // Call repository to delete the group (soft delete recommended)
                var deleted = await _helpDeskRepository.DeleteTicketGroupAsync(organizationId, groupId);

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ticket group {GroupId} for organization {OrganizationId}", groupId, organizationId);
                throw;
            }
        }

        public async Task<bool> AssignTicketAsync(AssignTicketRequest request, Guid assignedBy)
        {
            // ✅ Validations
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Assign request cannot be null");

            if (request.TicketId == Guid.Empty)
                throw new ArgumentException("TicketId is required", nameof(request.TicketId));

            // ✅ Call repository
            var result = await _helpDeskRepository.AssignTicketAsync(request, assignedBy);

            return result;

        }

      
        public async Task<bool> CreateChatAsync(TicketMessageRequest request, Guid organizationId)
        {
            try
            {
                
                //First sending request into repo
                var result = await _helpDeskRepository.CreateMessageAsync(organizationId, request);

                //Implementing Email and sending email
                var ReciverEmails = new List<string>();

                var selectedEmployees = await _employeeRepository.GeSelectedtEmployeesByIdsAsync(request.RecipientIds);
                ReciverEmails = selectedEmployees.Where(e => e.Email != null).Select(e => e.Email).ToList();
                var ticket = await _helpDeskRepository.GetTicketByIdAsync(organizationId, request.TicketId);
                await _emailService.SendHelpDeskChatAsync(ReciverEmails, request, organizationId, ticket.TicketTitle);

                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error While chating");
                throw;
            }

        }

        public async Task<List<TicketMessageDto>> GetMessagesByEmployeeIdAsync(Guid organizationId, Guid employeeId, Guid ticketId)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty", nameof(organizationId));

            if (employeeId == Guid.Empty)
                throw new ArgumentException("EmployeeId cannot be empty", nameof(employeeId));

            var messages = await _helpDeskRepository.GetMessagesByTicketIdAsync(organizationId, ticketId);

            // Optional: any business logic, e.g., filtering, sorting, marking read status, etc.
            return messages;
        }

        public async Task<bool> CreateCategoriesAsync(Guid organizationId, Guid CurrentEmployeeId, CreateCategoryRequest request)
        {
            // ✅ Validations (same style as your method)
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty", nameof(organizationId));
            var categories = await _helpDeskRepository.CreateCategoriesAsync(organizationId, CurrentEmployeeId, request);
            return categories;

        }

        public async Task<List<CategoryDto>> GetCategoriesAsync(Guid organizationId, searchCateogry? search)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty", nameof(organizationId));

            var categories = await _helpDeskRepository.GetCategoriesAsync(organizationId, search);

            return categories;
        }

        public async Task<bool> UpdateCategoryAsync(Guid organizationId, UpdateCategoryRequest request)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty", nameof(organizationId));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.CategoryId == Guid.Empty)
                throw new ArgumentException("CategoryId cannot be empty", nameof(request.CategoryId));

            if (string.IsNullOrWhiteSpace(request.CategoryName))
                throw new ArgumentException("Category name cannot be empty", nameof(request.CategoryName));

            // ✅ Repository call
            var result = await _helpDeskRepository.UpdateCategoryAsync(organizationId, request);
            return result;
        }

        public async Task<bool> DeleteCategoryAsync(Guid organizationId, Guid categoryId)
        {
            // ✅ Validations
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty", nameof(organizationId));
            if (categoryId == Guid.Empty)
                throw new ArgumentException("CategoryId cannot be empty", nameof(categoryId));
            var result = await _helpDeskRepository.DeleteCategoryAsync(organizationId, categoryId);
            return result;
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(Guid organizationId, Guid categoryId)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty", nameof(organizationId));

            if (categoryId == Guid.Empty)
                throw new ArgumentException("CategoryId cannot be empty", nameof(categoryId));

            var category = await _helpDeskRepository.GetCategoryByIdAsync(organizationId, categoryId);

            return category;
        }



    }
}
