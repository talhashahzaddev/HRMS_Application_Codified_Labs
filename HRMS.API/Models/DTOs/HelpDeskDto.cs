using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Models.DTOs
{
    public class HelpDeskDto
    {


        public class TicketDto
        {
            public Guid Ticketid { get; set; }
            public Guid departmentId { get; set; }
            public string? DepartmentName { get; set; }
            public string TicketTitle { get; set; } = string.Empty;
            public string? TicketType { get; set; }
            public string? Category { get; set; }
            public Guid? CategoryId { get; set; }
            public string? GroupName { get; set; }   // ✅ group
            public Guid? GroupId { get; set; }
            public string[] GroupEmployeeNames { get; set; } = Array.Empty<string>();
            public Guid[] AssignedEmployees { get; set; } = Array.Empty<Guid>();
            public string[] AssignedEmployeeNames { get; set; } = Array.Empty<string>();

            public string? Priority { get; set; }
            public DateTime Createdat { get; set; }
            public string Description { get; set; } = string.Empty;
            public string? Attachment { get; set; }
            public string? status { get; set; }
            public string? CreatedByName { get; set; }
        }


        public class TicketRequest
        {
            public Guid departmentId { get; set; }
            public string TicketTitle { get; set; } = string.Empty;
            public string? TicketType { get; set; }
            public Guid? CategoryId { get; set; }
            public Guid? assignedGroup { get; set; }
            public string? Priority { get; set; }
            public string? Description { get; set; } = string.Empty;
            public string? Attachment { get; set; }
        }

        public class UpdateTicketRequest
        {
            public Guid TicketId { get; set; }

            public Guid DepartmentId { get; set; }

            public string TicketTitle { get; set; } = string.Empty;

            public string? TicketType { get; set; }

            //public string? Category { get; set; }
            public Guid? CategoryId { get; set; }
            public Guid? GroupId { get; set; }

            public Guid[] AssignedEmployees { get; set; } = Array.Empty<Guid>();

            public string? Priority { get; set; }

            public string Description { get; set; } = string.Empty;

            public string? Attachment { get; set; }

            public string? Status { get; set; }
        }

        public class TicketSearchModel
        {
            public string? SearchTerm { get; set; }
            public Guid? DepartmentId { get; set; }
            public string? Status { get; set; }
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 50;
            public string? SortBy { get; set; }
            public string? SortOrder { get; set; } = "asc";
        }


        public class CreateTicketGroupRequest
        {
            public string GroupTitle { get; set; } = string.Empty;
            public Guid departmentId { get; set; }
            public Guid? categoryId { get; set; }
            public List<Guid> EmployeeIds { get; set; } = new();
        }

        public class TicketGroupDto
        {
            public Guid GroupId { get; set; }
            public string GroupTitle { get; set; } = string.Empty;
            public Guid[] EmployeeIds { get; set; } = Array.Empty<Guid>();
            public Guid? DepartmentId { get; set; }
            public string? DepartmentName { get; set; }
            public Guid? categoryId { get; set; }
            public string? CategoryName { get; set; }
            public DateTime CreatedAt { get; set; }
            public string EmployeeNames { get; set; } = string.Empty;
        }

        public class AssignTicketRequest
        {
            public Guid TicketId { get; set; }

            public Guid? GroupId { get; set; }

            //public List<Guid>? AssignedEmployees { get; set; }
            public List<Guid>? AssignedEmployees { get; set; }

            public string? Reason { get; set; }
        }

        public class TicketAssignmentDto
        {
            public Guid TicketId { get; set; }
            public Guid AssignmentId { get; set; }
            public Guid? GroupId { get; set; }
            public Guid[] AssignedEmployees { get; set; } = Array.Empty<Guid>();
            public Guid AssignedBy { get; set; }
            public string? Reason { get; set; }
        }

        // Request DTO
        public class TicketMessageRequest
        {
            public Guid TicketId { get; set; }
            public Guid SenderId { get; set; } // optional if you get current employee from context
            public string Message { get; set; }
            public string MessageType { get; set; } // 'chat' | 'email' | 'system'
            public string? Subject { get; set; } // optional, for email only
            public List<Guid>? RecipientIds { get; set; } // for sending to multiple recipients
        }

        // Response DTO
        public class TicketMessageDto
        {
            public Guid MessageId { get; set; }
            public Guid TicketId { get; set; }
            public Guid SenderId { get; set; }
            public string Message { get; set; }
            public string MessageType { get; set; }
            public string? Subject { get; set; }
            public DateTime CreatedAt { get; set; }
            public Guid[]? RecipientIds { get; set; }
            // New fields
            public string SenderFullName { get; set; } = string.Empty;
            public string SenderEmail { get; set; } = string.Empty;
        }


        public class CreateCategoryRequest
        {
            public Guid DepartmentId { get; set; }

            public string Name { get; set; } = string.Empty;

            public bool Status { get; set; } = true;
        }

        public class CategoryDto
        {
            public Guid CategoryId { get; set; }
            public Guid OrganizationId { get; set; }
            public Guid DepartmentId { get; set; }
            public string DepartmentName { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public bool Status { get; set; }
            public DateTime CreatedAt { get; set; }
        }
        public class UpdateCategoryRequest
        {
            public Guid CategoryId { get; set; }

            public Guid DepartmentId { get; set; }

            public string CategoryName { get; set; } = string.Empty;

            public bool Status { get; set; }
        }
        public class searchCateogry
        {
            public Guid? departmentId { get; set; }
            public string? SearchTerm { get; set; }
            public int? Page { get; set; } = 1;
            public int? PageSize { get; set; } = 50;
        }




    }
}
