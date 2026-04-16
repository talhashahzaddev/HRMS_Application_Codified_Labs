// ============================================================
// FILE: Repositories/OnboardingRepository.cs
// ============================================================
using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using Npgsql;

namespace HRMS.API.Repositories;

/// <summary>
/// All SQL goes through the same BaseRepository helpers (QueryAsync,
/// ExecuteAsync, ExecuteScalarAsync) that the existing EmployeeRepository uses.
/// Schema:   onboarding.sessions
///           onboarding.education
///           onboarding.work_experience
/// Personal info is kept in   employee.employees  (existing table).
/// </summary>
public class OnboardingRepository : BaseRepository, IOnboardingRepository
{
    public OnboardingRepository(IDatabaseConnectionFactory connectionFactory)
        : base(connectionFactory) { }

    // ── SESSION ──────────────────────────────────────────────────────────────

    public async Task<OnboardingSession?> GetSessionByEmployeeIdAsync(Guid employeeId)
    {
        const string sql = @"
            SELECT onboardingid, employeeid, organizationid, currentstep, status,
                   ispolicyaccepted, documentnotes, photourl, resumeurl, resumefilename,
                   jobtitle, department, idnumber, completionrate, createdat, updatedat
            FROM   onboarding.sessions
            WHERE  employeeid = @EmployeeId
            LIMIT  1";

        return await QuerySingleOrDefaultAsync<OnboardingSession>(sql, new { EmployeeId = employeeId });
    }

    public async Task<OnboardingSession> CreateSessionAsync(OnboardingSession session)
    {
        const string sql = @"
            INSERT INTO onboarding.sessions
                (employeeid, organizationid, currentstep, status, ispolicyaccepted,
                 documentnotes, photourl, resumeurl, resumefilename, jobtitle,
                 department, idnumber, completionrate)
            VALUES
                (@EmployeeId, @OrganizationId, @CurrentStep, @Status, @IsPolicyAccepted,
                 @DocumentNotes, @PhotoUrl, @ResumeUrl, @ResumeFileName, @JobTitle,
                 @Department, @IdNumber, @CompletionRate)
            RETURNING onboardingid";

        var id = await ExecuteScalarAsync<Guid>(sql, new
        {
            session.EmployeeId,
            session.OrganizationId,
            session.CurrentStep,
            session.Status,
            session.IsPolicyAccepted,
            session.DocumentNotes,
            session.PhotoUrl,
            session.ResumeUrl,
            session.ResumeFileName,
            session.JobTitle,
            session.Department,
            session.IdNumber,
            session.CompletionRate
        });

        session.OnboardingId = id;
        return session;
    }

    public async Task<bool> UpdateSessionAsync(OnboardingSession session)
    {
        const string sql = @"
            UPDATE onboarding.sessions
            SET    currentstep     = @CurrentStep,
                   status          = @Status,
                   ispolicyaccepted= @IsPolicyAccepted,
                   documentnotes   = @DocumentNotes,
                   photourl        = COALESCE(@PhotoUrl, photourl),
                   resumeurl       = COALESCE(@ResumeUrl, resumeurl),
                   resumefilename  = COALESCE(@ResumeFileName, resumefilename),
                   jobtitle        = COALESCE(@JobTitle, jobtitle),
                   department      = COALESCE(@Department, department),
                   idnumber        = COALESCE(@IdNumber, idnumber),
                   completionrate  = @CompletionRate,
                   updatedat       = CURRENT_TIMESTAMP
            WHERE  onboardingid = @OnboardingId";

        var affected = await ExecuteAsync(sql, new
        {
            session.OnboardingId,
            session.CurrentStep,
            session.Status,
            session.IsPolicyAccepted,
            session.DocumentNotes,
            session.PhotoUrl,
            session.ResumeUrl,
            session.ResumeFileName,
            session.JobTitle,
            session.Department,
            session.IdNumber,
            session.CompletionRate
        });

        return affected > 0;
    }

    // ── PERSONAL INFO ─────────────────────────────────────────────────────────
    // Writes back into the existing employee.employees table — no duplicate risk.

    public async Task<bool> UpsertPersonalInfoAsync(
        Guid employeeId, string fullName, string phone,
        string gender, string nationality, string idNumber,
        string? photoUrl, string? resumeUrl, string? resumeFileName,
        string? jobTitle, string? department)
    {
        var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = parts.Length > 0 ? parts[0] : string.Empty;
        var lastName = parts.Length > 1 ? parts[1] : string.Empty;

        // Update employees table
        const string empSql = @"
            UPDATE employee.employees
            SET    firstname   = @FirstName,
                   lastname    = @LastName,
                   phone       = @Phone,
                   gender      = @Gender,
                   nationality = @Nationality,
                   profilepictureurl = COALESCE(@PhotoUrl, profilepictureurl),
                   updatedat   = CURRENT_TIMESTAMP
            WHERE  employeeid  = @EmployeeId";

        await ExecuteAsync(empSql, new
        {
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Gender = gender,
            Nationality = nationality,
            PhotoUrl = photoUrl,
            EmployeeId = employeeId
        });

        // Store CNIC, job title, department, resume in onboarding.sessions
        const string sessionSql = @"
            UPDATE onboarding.sessions
            SET    idnumber       = COALESCE(@IdNumber, idnumber),
                   jobtitle       = COALESCE(@JobTitle, jobtitle),
                   department     = COALESCE(@Department, department),
                   resumeurl      = COALESCE(@ResumeUrl, resumeurl),
                   resumefilename = COALESCE(@ResumeFileName, resumefilename),
                   photourl       = COALESCE(@PhotoUrl, photourl),
                   updatedat      = CURRENT_TIMESTAMP
            WHERE  employeeid = @EmployeeId";

        var affected = await ExecuteAsync(sessionSql, new
        {
            IdNumber = string.IsNullOrWhiteSpace(idNumber) ? null : idNumber,
            JobTitle = string.IsNullOrWhiteSpace(jobTitle) ? null : jobTitle,
            Department = string.IsNullOrWhiteSpace(department) ? null : department,
            ResumeUrl = resumeUrl,
            ResumeFileName = resumeFileName,
            PhotoUrl = photoUrl,
            EmployeeId = employeeId
        });

        return affected > 0;
    }

    // ── EDUCATION ─────────────────────────────────────────────────────────────

    public async Task<List<OnboardingEducation>> GetEducationBySessionAsync(Guid onboardingId)
    {
        const string sql = @"
            SELECT educationid, onboardingid, degree, institution, field,
                   gpa, filename, fileurl, description, createdat, updatedat
            FROM   onboarding.education
            WHERE  onboardingid = @OnboardingId
            ORDER BY createdat";

        var results = await QueryAsync<OnboardingEducation>(sql, new { OnboardingId = onboardingId });
        return results.ToList();
    }

    public async Task<OnboardingEducation> AddEducationAsync(OnboardingEducation record)
    {
        const string sql = @"
            INSERT INTO onboarding.education
                (onboardingid, degree, institution, field, gpa, filename, fileurl, description)
            VALUES
                (@OnboardingId, @Degree, @Institution, @Field, @Gpa, @FileName, @FileUrl, @Description)
            RETURNING educationid";

        var id = await ExecuteScalarAsync<Guid>(sql, new
        {
            record.OnboardingId,
            record.Degree,
            record.Institution,
            record.Field,
            record.Gpa,
            record.FileName,
            record.FileUrl,
            record.Description
        });

        record.EducationId = id;
        return record;
    }

    public async Task<bool> UpdateEducationAsync(OnboardingEducation record)
    {
        const string sql = @"
            UPDATE onboarding.education
            SET    degree      = @Degree,
                   institution = @Institution,
                   field       = @Field,
                   gpa         = @Gpa,
                   filename    = COALESCE(@FileName, filename),
                   fileurl     = COALESCE(@FileUrl, fileurl),
                   description = @Description,
                   updatedat   = CURRENT_TIMESTAMP
            WHERE  educationid  = @EducationId
              AND  onboardingid = @OnboardingId";

        var affected = await ExecuteAsync(sql, new
        {
            record.EducationId,
            record.OnboardingId,
            record.Degree,
            record.Institution,
            record.Field,
            record.Gpa,
            record.FileName,
            record.FileUrl,
            record.Description
        });

        return affected > 0;
    }

    public async Task<bool> DeleteEducationAsync(Guid educationId, Guid onboardingId)
    {
        const string sql = @"
            DELETE FROM onboarding.education
            WHERE  educationid  = @EducationId
              AND  onboardingid = @OnboardingId";

        var affected = await ExecuteAsync(sql, new { EducationId = educationId, OnboardingId = onboardingId });
        return affected > 0;
    }

    // ── WORK EXPERIENCE ───────────────────────────────────────────────────────

    public async Task<List<OnboardingWorkExperience>> GetWorkExperienceBySessionAsync(Guid onboardingId)
    {
        const string sql = @"
            SELECT experienceid, onboardingid, jobtitle, company, employmenttype,
                   location, startdate, enddate, currentlyworking, responsibilities,
                   filename, fileurl, createdat, updatedat
            FROM   onboarding.work_experience
            WHERE  onboardingid = @OnboardingId
            ORDER BY createdat";

        var results = await QueryAsync<OnboardingWorkExperience>(sql, new { OnboardingId = onboardingId });
        return results.ToList();
    }

    public async Task<OnboardingWorkExperience> AddWorkExperienceAsync(OnboardingWorkExperience record)
    {
        const string sql = @"
            INSERT INTO onboarding.work_experience
                (onboardingid, jobtitle, company, employmenttype, location,
                 startdate, enddate, currentlyworking, responsibilities, filename, fileurl)
            VALUES
                (@OnboardingId, @JobTitle, @Company, @EmploymentType, @Location,
                 @StartDate, @EndDate, @CurrentlyWorking, @Responsibilities, @FileName, @FileUrl)
            RETURNING experienceid";

        var id = await ExecuteScalarAsync<Guid>(sql, new
        {
            record.OnboardingId,
            record.JobTitle,
            record.Company,
            record.EmploymentType,
            record.Location,
            record.StartDate,
            record.EndDate,
            record.CurrentlyWorking,
            record.Responsibilities,
            record.FileName,
            record.FileUrl
        });

        record.ExperienceId = id;
        return record;
    }

    public async Task<bool> UpdateWorkExperienceAsync(OnboardingWorkExperience record)
    {
        const string sql = @"
            UPDATE onboarding.work_experience
            SET    jobtitle         = @JobTitle,
                   company          = @Company,
                   employmenttype   = @EmploymentType,
                   location         = @Location,
                   startdate        = @StartDate,
                   enddate          = @EndDate,
                   currentlyworking = @CurrentlyWorking,
                   responsibilities = @Responsibilities,
                   filename         = COALESCE(@FileName, filename),
                   fileurl          = COALESCE(@FileUrl, fileurl),
                   updatedat        = CURRENT_TIMESTAMP
            WHERE  experienceid = @ExperienceId
              AND  onboardingid = @OnboardingId";

        var affected = await ExecuteAsync(sql, new
        {
            record.ExperienceId,
            record.OnboardingId,
            record.JobTitle,
            record.Company,
            record.EmploymentType,
            record.Location,
            record.StartDate,
            record.EndDate,
            record.CurrentlyWorking,
            record.Responsibilities,
            record.FileName,
            record.FileUrl
        });

        return affected > 0;
    }

    public async Task<bool> DeleteWorkExperienceAsync(Guid experienceId, Guid onboardingId)
    {
        const string sql = @"
            DELETE FROM onboarding.work_experience
            WHERE  experienceid = @ExperienceId
              AND  onboardingid = @OnboardingId";

        var affected = await ExecuteAsync(sql, new { ExperienceId = experienceId, OnboardingId = onboardingId });
        return affected > 0;
    }

    // ── FULL STATUS (GET /my-status) ──────────────────────────────────────────

    public async Task<OnboardingStatusResponse?> GetFullStatusAsync(Guid employeeId, Guid organizationId)
    {
        // 1. Load session
        var session = await GetSessionByEmployeeIdAsync(employeeId);
        if (session is null) return null;

        // 2. Load employee personal data from employee.employees
        const string empSql = @"
            SELECT e.employeeid, e.firstname, e.lastname, e.email, e.phone,
                   e.gender, e.nationality, e.profilepictureurl,
                   s.onboardingid, s.jobtitle, s.department, s.idnumber,
                   s.resumeurl, s.resumefilename, s.currentstep
            FROM   employee.employees e
            INNER JOIN onboarding.sessions s ON s.employeeid = e.employeeid
            WHERE  e.employeeid = @EmployeeId
            LIMIT  1";

        var raw = await QuerySingleOrDefaultAsync<dynamic>(empSql, new { EmployeeId = employeeId });
        if (raw is null) return null;

        // 3. Education + experience
        var education = await GetEducationBySessionAsync(session.OnboardingId);
        var experience = await GetWorkExperienceBySessionAsync(session.OnboardingId);

        // 4. Build response
        string firstName = raw.firstname ?? string.Empty;
        string lastName = raw.lastname ?? string.Empty;

        return new OnboardingStatusResponse
        {
            OnboardingId = session.OnboardingId,
            EmployeeId = employeeId,
            CurrentStep = session.CurrentStep,
            Status = session.Status,
            IsPolicyAccepted = session.IsPolicyAccepted,
            CompletionRate = session.CompletionRate,
            DocumentNotes = session.DocumentNotes,
            PersonalInfo = new OnboardingPersonalInfoDto
            {
                OnboardingId = session.OnboardingId,
                EmployeeId = employeeId,
                FullName = $"{firstName} {lastName}".Trim(),
                JobTitle = raw.jobtitle ?? string.Empty,
                Email = raw.email ?? string.Empty,
                Phone = raw.phone ?? string.Empty,
                Department = raw.department ?? string.Empty,
                Gender = raw.gender ?? string.Empty,
                Nationality = raw.nationality ?? string.Empty,
                IdNumber = raw.idnumber ?? string.Empty,
                PhotoUrl = raw.profilepictureurl,
                ResumeUrl = raw.resumeurl,
                ResumeFileName = raw.resumefilename,
                CurrentStep = (int)(raw.currentstep ?? 1)
            },
            EducationList = education.Select(e => new OnboardingEducationDto
            {
                EducationId = e.EducationId,
                OnboardingId = e.OnboardingId,
                Degree = e.Degree,
                Institution = e.Institution,
                Field = e.Field,
                Gpa = e.Gpa,
                FileName = e.FileName,
                FileUrl = e.FileUrl,
                Description = e.Description
            }).ToList(),
            WorkExperienceList = experience.Select(w => new OnboardingWorkExperienceDto
            {
                ExperienceId = w.ExperienceId,
                OnboardingId = w.OnboardingId,
                JobTitle = w.JobTitle,
                Company = w.Company,
                EmploymentType = w.EmploymentType,
                Location = w.Location,
                StartDate = w.StartDate,
                EndDate = w.EndDate,
                CurrentlyWorking = w.CurrentlyWorking,
                Responsibilities = w.Responsibilities,
                FileName = w.FileName,
                FileUrl = w.FileUrl
            }).ToList()
        };
    }
}