using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Models.DTOs
{
    public class NewsDTOs
    {
        public class NewsRequest
        {
            public string Title { get; set; }                    // title
            public string Description { get; set; }             // description / content
            public string Category { get; set; }                // category
            public string? ImageUrl { get; set; }               // image_url
            public string Status { get; set; }                  //Draft/Published
            public string VisibleTo { get; set; }               // visible_to
            public Guid[]? SelectedEmployees { get; set; }      // selected_employees (UUID array)
        }
        public class NewsDto
        {
            public Guid? NewsId { get; set; }                     // newsid
            public string Title { get; set; }                    // title
            public Guid? OrganizationId { get; set; }
            public string Description { get; set; }             // description / content
            public Guid CreatedBy { get; set; }                 // created_by (user ID)
            public string Category { get; set; }                // category
            public string? ImageUrl { get; set; }               // image_url
            public DateTime? PublishedAt { get; set; }           // published_at
            public string Status { get; set; }                  //Draft/Published
            public int? Views { get; set; }                      // views
            public string VisibleTo { get; set; }               // visible_to
            public Guid[]? SelectedEmployees { get; set; }      // selected_employees (UUID array)
            public DateTime? CreatedAt { get; set; }             // created_at
            public DateTime? UpdatedAt { get; set; }             // updated_at
            public bool IsRead { get; set; }
        }
        public class NewsSearchViewModel
        {
            public string? SearchTerm { get; set; }
            public string? Status { get; set; }
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 50;
            public string? SortBy { get; set; }
            public string? SortOrder { get; set; } = "asc";
        }

    }
}
