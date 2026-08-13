using static HRMS.API.Models.DTOs.HelpDeskDto;

namespace HRMS.API.Services.Interfaces
{
    public interface IHelpDeskService
    {
        Task<TicketDto> CreateTicketAsync(TicketRequest request, Guid organizationId, Guid employeeId);
        Task<TicketGroupDto> CreateTicketGroupAsync(Guid organizationId, CreateTicketGroupRequest request);
        Task<IReadOnlyList<TicketDto>> GetAllTicketsAsync(Guid organizationId, Guid employeeId, TicketSearchModel searchModel);
        Task<bool> AssignTicketAsync(AssignTicketRequest request, Guid assignedBy,Guid organizationId);
        Task<TicketDto?> GetTicketByIdAsync(Guid organizationId, Guid employeeId, Guid ticketId);
        Task<IReadOnlyList<TicketDto>> GetAllAssignedTicket(Guid organizationId, Guid employeeId, TicketSearchModel searchModel);
        Task<TicketGroupDto?> GetTicketGroupByIdAsync(Guid organizationId, Guid groupId);
        Task<IReadOnlyList<TicketGroupDto>> GetAllTicketGroupsAsync(Guid organizationId,string? search,Guid? departmentId,Guid? cateogryId);
        Task<bool> DeleteTicketGroupAsync(Guid organizationId, Guid groupId);
        Task<bool> UpdateTicketAsync(UpdateTicketRequest request, Guid organizationId, Guid currentEmployeeId);
        Task<bool> CreateChatAsync(TicketMessageRequest request, Guid organizationId);
        Task<IReadOnlyList<TicketMessageDto>> GetMessagesByEmployeeIdAsync(Guid organizationId, Guid employeeId, Guid ticketId);
        Task<bool> CreateCategoriesAsync(Guid organizationId, Guid CurrentEmployeeId, CreateCategoryRequest request);
        Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(Guid organizationId, searchCateogry? search);
        Task<bool> UpdateCategoryAsync(Guid organizationId, UpdateCategoryRequest request,Guid empId);
        Task<bool> DeleteCategoryAsync(Guid organizationId, Guid categoryId);
        Task<CategoryDto?> GetCategoryByIdAsync(Guid organizationId, Guid categoryId);
        Task<bool> UpdateTicketGroupAsync(Guid organizationId, UpdateTicketGroupRequest request);
        Task<bool> DeleteTicketAsync(Guid organizationId, Guid ticketId);


    }
}
