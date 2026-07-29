using AutoMapper;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using static HRMS.API.Models.DTOs.HelpDeskDto;
using static HRMS.API.Models.DTOs.NotificationDto;

namespace HRMS.API.Services
{
    public class HelpDeskService : IHelpDeskService
    {

        private readonly IHelpDeskRepository _helpDeskRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<HelpDeskService> _logger;

        public HelpDeskService(IHelpDeskRepository helpDeskRepository, IMapper mapper, IAttendanceRepository attendanceRepository, ILogger<HelpDeskService> logger, IEmailService emailService, IEmployeeRepository employeeRepository,INotificationRepository notificationRepository)
        {
            _helpDeskRepository = helpDeskRepository;
            _mapper = mapper;
            _attendanceRepository = attendanceRepository;
            _logger = logger;
            _emailService = emailService;
            _employeeRepository = employeeRepository;
            _notificationRepository = notificationRepository;

        }
        public async Task<TicketDto> CreateTicketAsync(TicketRequest request, Guid organizationId, Guid employeeId)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Ticket request cannot be null");

            if (string.IsNullOrWhiteSpace(request.TicketTitle))
                throw new ArgumentException("Ticket title is required", nameof(request.TicketTitle));

            // Call repository method
            var ticket = await _helpDeskRepository.CreateTicketAsync(request, organizationId, employeeId);

            //Implementing notification
            var employee = await _employeeRepository.GetEmployeeByEmployeeIdAsync(employeeId);
            var reportingmangerid = await _notificationRepository.GetReciveridAsync(employee.EmployeeId);
            var reportingManagerDetails = await _employeeRepository.GetEmployeeByEmployeeIdAsync(reportingmangerid);
            var ticketInof = await _helpDeskRepository.GetTicketByIdAsync(organizationId, ticket);
                      
                //get employees og group from groupid
                var groupEmp = await _helpDeskRepository.GetTicketGroupByIdAsync(organizationId, request.assignedGroup);
                var GroupEmpid = new List<string>();
            var groupEmpIds = groupEmp?.EmployeeIds?.ToList() ?? new List<Guid>();
            //Implementing Notification to Group employee that are assigend in ticket 
          
            
                var notification = new NotificationCreateDto
                {
                    Receiveruserid = groupEmpIds,
                    Senderuserid = employee.EmployeeId,
                    Title = "Help Desk Ticket",
                    Message = $"You have assigned HelpDesk ticket {request.TicketTitle} created by {employee.FirstName} {employee.LastName}",
                    Notificationtype = "Message",
                    Createdby = employeeId,
                    moduletype = "help desk",
                    Redirecturl = $"/help-desk/tickets/view/{ticket}",
                    RequestId = ticket,
                    organizationId = organizationId
                };
                await _notificationRepository.CreateNotificationAsync(organizationId, notification);

      
          

            //Sending Mails to group Employees
            var ReciverEmails = new List<string>();
            var selectedEmployees = await _employeeRepository.GeSelectedtEmployeesByIdsAsync(groupEmpIds);
            ReciverEmails = selectedEmployees.Where(e => e.Email != null).Select(e => e.Email).ToList();
            var EmailMessage = new TicketMessageRequest
            {
                TicketId = ticket,
                Message = notification.Message,
                MessageType = "Email",
                Subject = request.TicketTitle,
            };
            await _emailService.SendHelpDeskChatAsync(ReciverEmails, EmailMessage, organizationId, request.TicketTitle);

            //Creating mail for reporting manager that New Ticket is created
            if (reportingManagerDetails != null)
            {
                var MailForReportngManager = new TicketMessageRequest
                {
                    TicketId = ticket,
                    Message = $"New  HelpDesk  ticket {request.TicketTitle} is created {employee.FirstName} {employee.LastName}",
                    MessageType = "Email",
                    Subject = request.TicketTitle,
                };
                ReciverEmails = new List<string> { reportingManagerDetails?.Email };
                //Sending Mail to reporting Manaager
                await _emailService.SendHelpDeskChatAsync(ReciverEmails, MailForReportngManager, organizationId, request.TicketTitle);



                var ManagerNotification = new NotificationCreateDto
                {
                    Receiveruserid = new List<Guid> { reportingmangerid },
                    Senderuserid = employee.EmployeeId,
                    Title = "Help Desk Ticket",
                    Message = $"A new HelpDesk  {request.TicketTitle} ticket is  created by {employee.FirstName} {employee.LastName}",
                    Notificationtype = "Message",
                    Createdby = employeeId,
                    moduletype = "help desk",
                    Redirecturl = $"/help-desk/tickets/view/{ticket}",
                    RequestId = ticket,
                    organizationId = organizationId
                };
                await _notificationRepository.CreateNotificationAsync(organizationId, ManagerNotification);

            }
          
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
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty", nameof(organizationId));
            if (employeeId == Guid.Empty)
                throw new ArgumentException("EmployeeId cannot be empty", nameof(employeeId));
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

            //Implementing notification
            //first gettig employeeInfo who creates notification
            //var employee = await _employeeRepository.GetEmployeeByIdAsync(currentEmployeeId);
            //var ticketInof = await _helpDeskRepository.GetTicketByIdAsync(organizationId, request.TicketId);
    
            ////get employees of group from groupid
            //var groupEmp = await _helpDeskRepository.GetTicketGroupByIdAsync(organizationId, request.GroupId);
            //var GroupEmpid = new List<string>();
            //var groupEmpIds = groupEmp?.EmployeeIds?.ToList() ?? new List<Guid>();
            //var manualEmpIds = request.AssignedEmployees?.ToList() ?? new List<Guid>();
            //// Merge both + remove duplicates
            //var receiverEmpIds = groupEmpIds.Concat(manualEmpIds).Distinct().ToList();
            //var notification = new NotificationCreateDto
            //    {
            //        Receiveruserid = receiverEmpIds,
            //        Senderuserid = employee.EmployeeId,
            //        Title = "Help Desk Ticket",
            //        Message = $"You have assigned ticket {request.TicketTitle} created by {employee.FirstName} {employee.LastName} ",
            //        Notificationtype = "Message",
            //        Createdby = currentEmployeeId,
            //        moduletype = "help desk",
            //        Redirecturl = "/help-desk/ticket-invlovement",
            //        RequestId = request.TicketId,
            //        organizationId = organizationId
            //    };
            //    await _notificationRepository.CreateNotificationAsync(organizationId, notification);
            ////Implementing Email
            //var ReciverEmails = new List<string>();
            //var selectedEmployees = await _employeeRepository.GeSelectedtEmployeesByIdsAsync(receiverEmpIds);
            //ReciverEmails = selectedEmployees.Where(e => e.Email != null).Select(e => e.Email).ToList();
            // var EmailMessage = new TicketMessageRequest
            //{
            //    TicketId=request.TicketId,
            //    Message=notification.Message,
            //    MessageType="Email",
            //    Subject=request.TicketTitle,
            // };
            //await _emailService.SendHelpDeskChatAsync(ReciverEmails, EmailMessage, organizationId, request.TicketTitle);

            return result;
        }
        public async Task<bool> UpdateTicketGroupAsync(Guid organizationId, UpdateTicketGroupRequest request)
        {
            // 🔹 Validations
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty");

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.GroupId == Guid.Empty)
                throw new ArgumentException("GroupId is required");
            // Get the old group details from the repository before update
            var oldGroupDetails = await _helpDeskRepository.GetTicketGroupByIdAsync(organizationId, request.GroupId);

            // Convert old group EmployeeIds array to a List<Guid> safely
            var oldGroupEmployeeIds = oldGroupDetails?.EmployeeIds?.ToList() ?? new List<Guid>();
            // ✅ STEP 1: Update group main data (title, department, category)
            await _helpDeskRepository.UpdateTicketGroupAsync(organizationId, request);

            // ✅ STEP 2: Update group employees (remove old + insert new)
          var result=  await _helpDeskRepository.UpdateGroupEmployeesAsync(request.GroupId, request.EmployeeIds,oldGroupEmployeeIds);
            return true ;
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
                tickets = await _helpDeskRepository.GetAllTicketsAsync(organizationId, searchModel);
            }
            else
            {
                tickets = await _helpDeskRepository.GetAllEmployeeAssignedTickets(organizationId, employeeId, searchModel);
            }
            return tickets ?? new List<TicketDto>();
        }
        public async Task<TicketDto?> GetTicketByIdAsync(Guid organizationId, Guid employeeId, Guid ticketId)
        {
            try
            {
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
        public async Task<bool> DeleteTicketAsync(Guid organizationId, Guid ticketId)
        {
            try
            {
                // Step 1: Check if ticket exists
                var ticket = await _helpDeskRepository.GetTicketByIdAsync(organizationId, ticketId);
                if (ticket == null)
                {
                    _logger.LogWarning("Ticket {TicketId} not found in organization {OrganizationId}", ticketId, organizationId);
                    return false;
                }
                var deleted = await _helpDeskRepository.DeleteTicketByIdAsync(organizationId, ticketId);
                if (deleted)
                {
                    _logger.LogInformation("Ticket {TicketId} deleted successfully from organization {OrganizationId}", ticketId, organizationId);
                }
                else
                {
                    _logger.LogWarning("Failed to delete ticket {TicketId} from organization {OrganizationId}", ticketId, organizationId);
                }
                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ticket {TicketId} from organization {OrganizationId}", ticketId, organizationId);
                throw;
            }
        }

        public async Task<TicketGroupDto> CreateTicketGroupAsync(Guid organizationId, CreateTicketGroupRequest request)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty", nameof(organizationId));
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null");
            if (string.IsNullOrWhiteSpace(request.GroupTitle))
                throw new ArgumentException("Group title is required", nameof(request.GroupTitle));
            if (request.EmployeeIds == null || !request.EmployeeIds.Any())
                throw new ArgumentException("At least one employee must be assigned to group");
            var group = await _helpDeskRepository.CreateTicketGroupAsync(organizationId, request);
            return group;
        }
        //Get all Agents/Ticket Group
        public async Task<List<TicketGroupDto>> GetAllTicketGroupsAsync(Guid organizationId, string? search, Guid? departmentId,Guid? categoryId)
        {
            try
            {
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
                var group = await _helpDeskRepository.GetTicketGroupByIdAsync(organizationId, groupId);
                if (group == null)
                {
                    return null; 
                }
                return group;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching ticket group {GroupId} for organization {OrganizationId}", groupId, organizationId);
                throw;
            }
        }
        public async Task<bool> DeleteTicketGroupAsync(Guid organizationId, Guid groupId)
        {
           
            if (organizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId cannot be empty");
            if (groupId == Guid.Empty)
                throw new ArgumentException("GroupId is required");
           // Get old group details
            var oldGroupDetails = await _helpDeskRepository.GetTicketGroupByIdAsync(organizationId, groupId);

            // Safely convert EmployeeIds array to a List<Guid>
            var oldGroupEmployeeIds = oldGroupDetails.EmployeeIds?.ToList() ?? new List<Guid>();
            var AssignmentidsBelongtoGroupId = await _helpDeskRepository.GetAssignmentsByGroupIdAsync(groupId);

            //Delete the group 
            var deleted = await _helpDeskRepository.DeleteTicketGroupAsync(organizationId, groupId);
            if (!deleted)
            {
                return false; 
            }
            // Remove old group employees from ticket_assigned_employees
            if (oldGroupEmployeeIds.Any())
            {
                await _helpDeskRepository.DeleteOldGroupEmployeesFromAssignmentsAsync(oldGroupEmployeeIds, AssignmentidsBelongtoGroupId);
            }
            return true;
        }

        public async Task<bool> AssignTicketAsync(AssignTicketRequest request, Guid assignedBy,Guid organizationId)
        {
            // ✅ Validations
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Assign request cannot be null");

            if (request.TicketId == Guid.Empty)
                throw new ArgumentException("TicketId is required", nameof(request.TicketId));
            var result = await _helpDeskRepository.AssignTicketAsync(request, assignedBy);
            //Get assigned Employee Information
            var employee = await _employeeRepository.GetEmployeeByEmployeeIdAsync(assignedBy);
            //Get Ticket information
            var Ticket = await _helpDeskRepository.GetTicketByIdAsync(organizationId, request.TicketId);
            if (Ticket != null)
            {
                //Involving Ticket
                var ManagerNotification = new NotificationCreateDto
                {
                    Receiveruserid = request.AssignedEmployees,
                    Senderuserid = assignedBy,
                    Title = "Help Desk Ticket",
                    Message = $"{employee?.FirstName} {employee?.LastName} invloved you in HelpDesk ticket  {Ticket.TicketTitle} ",
                    Notificationtype = "Message",
                    Createdby = assignedBy,
                    moduletype = "help desk",
                    Redirecturl = $"/help-desk/tickets/view/{request.TicketId}",
                    RequestId = request.TicketId,
                    organizationId = organizationId
                };
                await _notificationRepository.CreateNotificationAsync(organizationId, ManagerNotification);
            }
            //Implementing Email

            var ReciverEmails = new List<string>();
            var selectedEmployees = await _employeeRepository.GeSelectedtEmployeesByIdsAsync(request.AssignedEmployees);
            ReciverEmails = selectedEmployees.Where(e => e.Email != null).Select(e => e.Email).ToList();
            var EmailMessage = new TicketMessageRequest
            {
                TicketId = request.TicketId,
                Message = $"{employee?.FirstName} {employee?.LastName} {employee?.LastName} invloved you in HelpDesk ticket  {Ticket.TicketTitle}",
                MessageType = "Email",
                Subject = Ticket.TicketTitle,
            };
            await _emailService.SendHelpDeskChatAsync(ReciverEmails, EmailMessage, organizationId, Ticket.TicketTitle);
            return result;

        } 
        public async Task<bool> CreateChatAsync(TicketMessageRequest request, Guid organizationId)
        {
            try
            {

                //First sending request into repo
                var result = await _helpDeskRepository.CreateMessageAsync(organizationId, request);
                //Get employee information who creates message
                var employee = await _employeeRepository.GetEmployeeByEmployeeIdAsync(request.SenderId);
                //Get Ticket information
                var Ticket = await _helpDeskRepository.GetTicketByIdAsync(organizationId, request.TicketId);

                //Implementing notification
                var ManagerNotification = new NotificationCreateDto
                {
                    Receiveruserid = request.RecipientIds,
                    Senderuserid = request.SenderId,
                    Title = "Help Desk Ticket",
                    Message = $"{employee?.FirstName} {employee.LastName} send you a message on helpdesk ticket {Ticket.TicketTitle} ",
                    Notificationtype = "Message",
                    Createdby = request.SenderId,
                    moduletype = "help desk",
                    Redirecturl = $"/help-desk/tickets/view/{request.TicketId}",
                    RequestId = request.TicketId,
                    organizationId = organizationId
                };
                await _notificationRepository.CreateNotificationAsync(organizationId, ManagerNotification);

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

        public async Task<bool> UpdateCategoryAsync(Guid organizationId, UpdateCategoryRequest request,Guid empId)
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
            var result = await _helpDeskRepository.UpdateCategoryAsync(organizationId, request,empId);
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
