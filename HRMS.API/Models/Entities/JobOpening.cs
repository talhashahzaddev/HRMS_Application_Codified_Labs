using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("jobopenings", Schema = "jobs")]
public class JobOpening
{
    [Column("jobid")]
    public Guid JobId { get; set; }

    [Column("organizationid")]
    public Guid OrganizationId { get; set; }

    [Column("jobrolename")]
    public string JobRoleName { get; set; } = string.Empty;

    [Column("jobcode")]
    public string? JobCode { get; set; }

    [Column("departmentid")]
    public Guid? DepartmentId { get; set; }

    [Column("experiencemin")]
    public int? ExperienceMin { get; set; }

    [Column("experiencemax")]
    public int? ExperienceMax { get; set; }

    [Column("ctcmin")]
    public decimal? CtcMin { get; set; }

    [Column("ctcmax")]
    public decimal? CtcMax { get; set; }

    [Column("currency")]
    public string? Currency { get; set; }

    [Column("vacancies")]
    public int Vacancies { get; set; } = 1;

    [Column("location")]
    public string? Location { get; set; }

    [Column("workmode")]
    public string? WorkMode { get; set; }

    [Column("employmenttype")]
    public string? EmploymentType { get; set; }

    [Column("jobintroduction")]
    public string? JobIntroduction { get; set; }

    [Column("responsibilities")]
    public string? Responsibilities { get; set; }

    [Column("skillset")]
    public string? Skillset { get; set; }

    [Column("lastdate")]
    public DateTime? LastDate { get; set; }

    [Column("posteddate")]
    public DateTime? PostedDate { get; set; }

    [Column("postedas")]
    public string PostedAs { get; set; } = string.Empty;

    [Column("externallink")]
    public string? ExternalLink { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Open";

    [Column("createdby")]
    public Guid? CreatedBy { get; set; }

    [Column("createdat")]
    public DateTime? CreatedAt { get; set; }

    [Column("updatedat")]
    public DateTime? UpdatedAt { get; set; }

    [Column("mandatoryskills")]
    public string? MandatorySkills { get; set; }
}
