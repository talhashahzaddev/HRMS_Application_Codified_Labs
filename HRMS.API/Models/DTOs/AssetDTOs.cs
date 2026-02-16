using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HRMS.API.Models.DTOs;

public class AssetTypeDto
{
    public Guid AssetTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAssetTypeRequest
{
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateAssetTypeRequest
{
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AssetDto
{
    public Guid AssetId { get; set; }
    public Guid AssetTypeId { get; set; }
    public string AssetTypeName { get; set; } = string.Empty;
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public string Status { get; set; } = "Available";
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAssetRequest
{
    [JsonPropertyName("typeId")]
    [Required]
    public Guid AssetTypeId { get; set; }

    [Required]
    [StringLength(50)]
    public string AssetTag { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(120)]
    public string? SerialNumber { get; set; }

    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
}

public class UpdateAssetRequest
{
    [JsonPropertyName("typeId")]
    [Required]
    public Guid AssetTypeId { get; set; }

    [Required]
    [StringLength(50)]
    public string AssetTag { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(120)]
    public string? SerialNumber { get; set; }

    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Available";
}

public class AssetAssignmentDto
{
    public Guid AssignmentId { get; set; }
    public Guid AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }
}

public class AssignAssetRequest
{
    [Required]
    public Guid AssetId { get; set; }

    [Required]
    public Guid EmployeeId { get; set; }

    public DateTime? AssignedAt { get; set; }
    public DateTime? DueDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class ReturnAssetRequest
{
    [Required]
    public Guid AssignmentId { get; set; }

    public DateTime? ReturnedAt { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
