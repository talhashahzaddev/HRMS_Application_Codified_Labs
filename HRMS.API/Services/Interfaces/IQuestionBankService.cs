using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IQuestionBankService
{
    // Categories
    Task<IReadOnlyList<QuestionCategoryDto>> GetCategoriesAsync(Guid organizationId);
    Task<QuestionCategoryDto?> GetCategoryByIdAsync(Guid categoryId);
    Task<QuestionCategoryDto> CreateCategoryAsync(Guid organizationId, CreateQuestionCategoryRequest request);
    Task<QuestionCategoryDto?> UpdateCategoryAsync(Guid categoryId, UpdateQuestionCategoryRequest request);
    Task<bool> DeleteCategoryAsync(Guid categoryId);

    // Questions
    Task<IReadOnlyList<QuestionDto>> GetQuestionsByCategoryAsync(Guid categoryId);
    Task<IReadOnlyList<QuestionDto>> GetQuestionsByOrganizationAsync(Guid organizationId);
    Task<QuestionDto?> GetQuestionByIdAsync(Guid questionId);
    Task<QuestionDto> CreateQuestionAsync(Guid organizationId, CreateQuestionRequest request);
    Task<QuestionDto?> UpdateQuestionAsync(Guid questionId, UpdateQuestionRequest request);
    Task<bool> DeleteQuestionAsync(Guid questionId);

    // Job-Question Mapping
    Task<IReadOnlyList<JobQuestionDto>> GetJobQuestionsAsync(Guid jobId);
    Task AttachQuestionsToJobAsync(AttachQuestionsToJobRequest request);
    Task AttachCategoryToJobAsync(AttachCategoryToJobRequest request);
    Task DetachQuestionFromJobAsync(Guid jobId, Guid questionId);

    // Candidate Answers
    Task SaveCandidateAnswersAsync(Guid jobApplyId, List<SubmitCandidateAnswerRequest> answers);
    Task<IReadOnlyList<CandidateAnswerDto>> GetCandidateAnswersAsync(Guid jobApplyId);
    Task<bool> EvaluateKnockoutAsync(Guid jobApplyId);

    // Parsed Resume
    Task<ParsedResumeDto?> GetParsedResumeByJobApplyIdAsync(Guid jobApplyId);
}
