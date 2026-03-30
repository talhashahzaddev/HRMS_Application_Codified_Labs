using static HRMS.API.Models.DTOs.HelpDeskDto;

namespace HRMS.API.Services.Interfaces
{
    public interface IHelpDeskService
    {
        Task<TicketDto> CreateTicketAsync(TicketRequest request, Guid organizationId, Guid employeeId);
        Task<TicketGroupDto> CreateTicketGroupAsync(Guid organizationId, CreateTicketGroupRequest request);
        Task<List<TicketDto>> GetAllTicketsAsync(Guid organizationId, Guid employeeId, TicketSearchModel searchModel);
        Task<bool> AssignTicketAsync(AssignTicketRequest request, Guid assignedBy);
        Task<TicketDto?> GetTicketByIdAsync(Guid organizationId, Guid employeeId, Guid ticketId);
        Task<List<TicketDto>> GetAllAssignedTicket(Guid organizationId, Guid employeeId, TicketSearchModel searchModel);
        Task<TicketGroupDto?> GetTicketGroupByIdAsync(Guid organizationId, Guid groupId);
        Task<List<TicketGroupDto>> GetAllTicketGroupsAsync(Guid organizationId,string? search,Guid? departmentId,Guid? cateogryId);
        Task<bool> DeleteTicketGroupAsync(Guid organizationId, Guid groupId);
        Task<bool> UpdateTicketAsync(UpdateTicketRequest request, Guid organizationId, Guid currentEmployeeId);
        Task<bool> CreateChatAsync(TicketMessageRequest request, Guid organizationId);
        Task<List<TicketMessageDto>> GetMessagesByEmployeeIdAsync(Guid organizationId, Guid employeeId, Guid ticketId);
        Task<bool> CreateCategoriesAsync(Guid organizationId, Guid CurrentEmployeeId, CreateCategoryRequest request);
        Task<List<CategoryDto>> GetCategoriesAsync(Guid organizationId, searchCateogry? search);
        Task<bool> UpdateCategoryAsync(Guid organizationId, UpdateCategoryRequest request);
        Task<bool> DeleteCategoryAsync(Guid organizationId, Guid categoryId);
        Task<CategoryDto?> GetCategoryByIdAsync(Guid organizationId, Guid categoryId);


    }
}
