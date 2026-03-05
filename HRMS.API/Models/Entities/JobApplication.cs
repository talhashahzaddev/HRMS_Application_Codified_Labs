using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.API.Models.Entities;

[Table("jobapplications", Schema = "jobs")]
public class JobApplication
{
    [Column("jobapplyid")]
    public Guid JobApplyId { get; set; }

    [Column("jobid")]
    public Guid JobId { get; set; }

    [Column("organizationid")]
    public Guid OrganizationId { get; set; }

    [Column("employeeid")]
    public Guid? EmployeeId { get; set; }

    [Column("candidateid")]
    public Guid? CandidateId { get; set; }

    [Column("candidatename")]
    public string? CandidateName { get; set; }

    [Column("candidateemail")]
    public string? CandidateEmail { get; set; }

    [Column("linkedinurl")]
    public string? LinkedInUrl { get; set; }

    [Column("resumeurl")]
    public string? ResumeUrl { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("coverletter")]
    public string? CoverLetter { get; set; }

    [Column("currentstageid")]
    public Guid? CurrentStageId { get; set; }

    [Column("applicationsource")]
    public string? ApplicationSource { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Applied";

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }

    [Column("updateddate")]
    public DateTime? UpdatedDate { get; set; }
    [Column("isself")]
    public bool? IsSelf { get; set; }
    
    [Column("isexternal")]
    public bool? IsExternal { get; set; }
}
