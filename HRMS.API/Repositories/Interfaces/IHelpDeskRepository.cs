using static HRMS.API.Models.DTOs.HelpDeskDto;

namespace HRMS.API.Repositories.Interfaces
{
    public interface IHelpDeskRepository
    {
        Task<Guid> CreateTicketAsync(TicketRequest request, Guid organizationId, Guid employeeId);
        Task<TicketGroupDto> CreateTicketGroupAsync(Guid organizationId, CreateTicketGroupRequest request);
        Task<IReadOnlyList<TicketDto>> GetAllTicketsAsync(Guid organizationId, TicketSearchModel search);
        Task<IReadOnlyList<TicketDto>> GetAllTicketsCreatedByEmployeeAsync(Guid organizationId, Guid employeeId, TicketSearchModel search);
        Task<bool> AssignTicketAsync(AssignTicketRequest request, Guid assignedBy);
        Task<TicketDto?> GetTicketByIdAsync(Guid organizationId, Guid ticketId);
        Task<IReadOnlyList<TicketDto>> GetAllEmployeeAssignedTickets(Guid organizationId, Guid employeeId, TicketSearchModel search);
        Task<TicketGroupDto?> GetTicketGroupByIdAsync(Guid organizationId, Guid groupId);
        Task<IReadOnlyList<TicketGroupDto>> GetAllTicketGroupsAsync(Guid organizationId, string? search, Guid? departmentId,Guid? categoryId);
        Task<bool> DeleteTicketGroupAsync(Guid organizationId, Guid groupId);
        Task<bool> UpdateTicketDataAsync(Guid organizationId, UpdateTicketRequest request);
        Task<bool> UpdateTicketAssignmentsAsync(UpdateTicketRequest request, Guid currentEmployeeId);
        Task<bool> CreateMessageAsync(Guid organizationId, TicketMessageRequest request);
        Task<IReadOnlyList<TicketMessageDto>> GetMessagesByTicketIdAsync(Guid organizationId, Guid ticketId);
        Task<bool> CreateCategoriesAsync(Guid organizationId, Guid createdBy, CreateCategoryRequest request);
        Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(Guid organizationId, searchCateogry? search);
        Task<bool> UpdateCategoryAsync(Guid organizationId, UpdateCategoryRequest request,Guid empId);
        Task<bool> DeleteCategoryAsync(Guid organizationId, Guid categoryId);
        Task<CategoryDto?> GetCategoryByIdAsync(Guid organizationId, Guid categoryId);
        Task<bool> UpdateTicketGroupAsync(Guid organizationId, UpdateTicketGroupRequest request);
        Task<bool> UpdateGroupEmployeesAsync(Guid groupId, List<Guid> employeeIds, List<Guid> oldGroupEmpId);
        Task<bool> DeleteTicketByIdAsync(Guid organizationId, Guid ticketId);
        Task<IReadOnlyList<Guid>> GetAssignmentsByGroupIdAsync(Guid groupId);
        Task<bool> DeleteOldGroupEmployeesFromAssignmentsAsync(List<Guid> oldGroupEmployeeIds, List<Guid> assignmentIds);
    }
}
