namespace HRMS.API.Services.Interfaces;

public interface IResumeParserService
{
    /// <summary>
    /// Parses the CV content from a public URL using Gemini API.
    /// Updates the ParsedResume table with the extracted skills, experience, education, and contact details.
    /// </summary>
    Task ParseResumeAsync(Guid jobApplyId, string resumeUrl, string aiApiKey);
}
