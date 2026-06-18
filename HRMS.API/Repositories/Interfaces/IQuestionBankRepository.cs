using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories.Interfaces;

public interface IQuestionBankRepository
{
    // Categories
    Task<IEnumerable<QuestionCategoryDto>> GetCategoriesAsync(Guid organizationId);
    Task<QuestionCategoryDto?> GetCategoryByIdAsync(Guid categoryId);
    Task<Guid> CreateCategoryAsync(Guid organizationId, string name);
    Task<bool> UpdateCategoryAsync(Guid categoryId, string name);
    Task<bool> DeleteCategoryAsync(Guid categoryId);

    // Questions
    Task<IEnumerable<QuestionDto>> GetQuestionsByCategoryAsync(Guid categoryId);
    Task<IEnumerable<QuestionDto>> GetQuestionsByOrganizationAsync(Guid organizationId);
    Task<QuestionDto?> GetQuestionByIdAsync(Guid questionId);
    Task<Guid> CreateQuestionAsync(Guid organizationId, CreateQuestionRequest request);
    Task<bool> UpdateQuestionAsync(Guid questionId, UpdateQuestionRequest request);
    Task<bool> DeleteQuestionAsync(Guid questionId);

    // Job-Question Mapping
    Task<IEnumerable<JobQuestionDto>> GetJobQuestionsAsync(Guid jobId);
    Task AttachQuestionsToJobAsync(Guid jobId, List<Guid> questionIds);
    Task AttachCategoryToJobAsync(Guid jobId, Guid categoryId);
    Task DetachQuestionFromJobAsync(Guid jobId, Guid questionId);

    // Candidate Answers
    Task SaveCandidateAnswersAsync(Guid jobApplyId, List<SubmitCandidateAnswerRequest> answers);
    Task<IEnumerable<CandidateAnswerDto>> GetCandidateAnswersAsync(Guid jobApplyId);

    // Parsed Resume
    Task<Guid> CreateParsedResumeAsync(Guid jobApplyId);
    Task<bool> UpdateParsedResumeAsync(Guid parsedResumeId, string? contactDetails, string? skills, string? workExperience, string? education, string? rawText, string parseStatus, string? parseError);
    Task<ParsedResumeDto?> GetParsedResumeByJobApplyIdAsync(Guid jobApplyId);
    Task<IEnumerable<ParsedResumeDto>> GetParsedResumesAsync(IEnumerable<Guid> jobApplyIds);
}
