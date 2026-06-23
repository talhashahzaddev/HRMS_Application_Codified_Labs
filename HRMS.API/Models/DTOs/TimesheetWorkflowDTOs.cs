using System.Text.Json.Serialization;

namespace HRMS.API.Models.DTOs;

public class TimesheetApprovalWorkflowDto
{
    [JsonPropertyName("workflowId")]
    public Guid WorkflowId { get; set; }

    [JsonPropertyName("organizationId")]
    public Guid OrganizationId { get; set; }

    [JsonPropertyName("workflowName")]
    public string WorkflowName { get; set; } = string.Empty;

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("steps")]
    public List<TimesheetApprovalStepDto> Steps { get; set; } = new();
}

public class TimesheetApprovalStepDto
{
    [JsonPropertyName("stepId")]
    public Guid StepId { get; set; }

    [JsonPropertyName("stepOrder")]
    public int StepOrder { get; set; }

    [JsonPropertyName("approverRole")]
    public string ApproverRole { get; set; } = string.Empty;

    [JsonPropertyName("approverSource")]
    public string ApproverSource { get; set; } = "ReportingManager";

    [JsonPropertyName("escalationHours")]
    public int? EscalationHours { get; set; }

    [JsonPropertyName("isOptional")]
    public bool IsOptional { get; set; }
}

public class TimesheetApprovalProgressDto
{
    [JsonPropertyName("progressId")]
    public Guid ProgressId { get; set; }

    [JsonPropertyName("timesheetId")]
    public Guid TimesheetId { get; set; }

    [JsonPropertyName("employeeId")]
    public Guid EmployeeId { get; set; }

    [JsonPropertyName("employeeName")]
    public string EmployeeName { get; set; } = string.Empty;

    [JsonPropertyName("stepOrder")]
    public int StepOrder { get; set; }

    [JsonPropertyName("approverRole")]
    public string ApproverRole { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pending";

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

public class TimesheetReopenRequestDto
{
    [JsonPropertyName("reopenRequestId")]
    public Guid ReopenRequestId { get; set; }

    [JsonPropertyName("timesheetId")]
    public Guid TimesheetId { get; set; }

    [JsonPropertyName("timesheetName")]
    public string TimesheetName { get; set; } = string.Empty;

    [JsonPropertyName("requestedBy")]
    public Guid RequestedBy { get; set; }

    [JsonPropertyName("requestedByName")]
    public string RequestedByName { get; set; } = string.Empty;

    [JsonPropertyName("requestedAt")]
    public DateTime RequestedAt { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pending";
}
