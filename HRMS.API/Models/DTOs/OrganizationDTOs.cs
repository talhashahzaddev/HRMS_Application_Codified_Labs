using System.ComponentModel.DataAnnotations;

namespace HRMS.API.Models.DTOs
{
    public class OrganizationDTO
    {
        public Guid OrganizationId { get; set; }
        public string Name { get; set; }
        public string? Domain { get; set; }
        public string SubscriptionType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateOrganizationDTO
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        [MaxLength(100)]
        public string? Domain { get; set; }
        [Required]
        [MaxLength(50)]
        public string SubscriptionType { get; set; }
    }

    public class UpdateOrganizationDTO
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        [MaxLength(100)]
        public string? Domain { get; set; }
        [Required]
        [MaxLength(50)]
        public string SubscriptionType { get; set; }
    }
}