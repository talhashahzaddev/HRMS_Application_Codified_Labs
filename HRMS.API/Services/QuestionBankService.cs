using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class QuestionBankService : IQuestionBankService
{
    private readonly IQuestionBankRepository _repository;
    private readonly IJobsRepository _jobsRepository;
    private readonly ILogger<QuestionBankService> _logger;

    public QuestionBankService(IQuestionBankRepository repository, IJobsRepository jobsRepository, ILogger<QuestionBankService> logger)
    {
        _repository = repository;
        _jobsRepository = jobsRepository;
        _logger = logger;
    }

    // ==================== Categories ====================
    public async Task<IReadOnlyList<QuestionCategoryDto>> GetCategoriesAsync(Guid organizationId)
        => await _repository.GetCategoriesAsync(organizationId);

    public async Task<QuestionCategoryDto?> GetCategoryByIdAsync(Guid categoryId)
        => await _repository.GetCategoryByIdAsync(categoryId);

    public async Task<QuestionCategoryDto> CreateCategoryAsync(Guid organizationId, CreateQuestionCategoryRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Category name is required");

        var id = await _repository.CreateCategoryAsync(organizationId, request.Name.Trim());
        var created = await _repository.GetCategoryByIdAsync(id);
        return created ?? throw new InvalidOperationException("Failed to retrieve created category");
    }

    public async Task<QuestionCategoryDto?> UpdateCategoryAsync(Guid categoryId, UpdateQuestionCategoryRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Category name is required");

        var updated = await _repository.UpdateCategoryAsync(categoryId, request.Name.Trim());
        if (!updated) return null;
        return await _repository.GetCategoryByIdAsync(categoryId);
    }

    public async Task<bool> DeleteCategoryAsync(Guid categoryId)
        => await _repository.DeleteCategoryAsync(categoryId);

    // ==================== Questions ====================
    public async Task<IReadOnlyList<QuestionDto>> GetQuestionsByCategoryAsync(Guid categoryId)
        => await _repository.GetQuestionsByCategoryAsync(categoryId);

    public async Task<IReadOnlyList<QuestionDto>> GetQuestionsByOrganizationAsync(Guid organizationId)
        => await _repository.GetQuestionsByOrganizationAsync(organizationId);

    public async Task<QuestionDto?> GetQuestionByIdAsync(Guid questionId)
        => await _repository.GetQuestionByIdAsync(questionId);

    public async Task<QuestionDto> CreateQuestionAsync(Guid organizationId, CreateQuestionRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.QuestionText)) throw new ArgumentException("Question text is required");
        if (request.CategoryId == Guid.Empty) throw new ArgumentException("Category ID is required");

        var id = await _repository.CreateQuestionAsync(organizationId, request);
        var created = await _repository.GetQuestionByIdAsync(id);
        return created ?? throw new InvalidOperationException("Failed to retrieve created question");
    }

    public async Task<QuestionDto?> UpdateQuestionAsync(Guid questionId, UpdateQuestionRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var updated = await _repository.UpdateQuestionAsync(questionId, request);
        if (!updated) return null;
        return await _repository.GetQuestionByIdAsync(questionId);
    }

    public async Task<bool> DeleteQuestionAsync(Guid questionId)
        => await _repository.DeleteQuestionAsync(questionId);

    // ==================== Job-Question Mapping ====================
    public async Task<IReadOnlyList<JobQuestionDto>> GetJobQuestionsAsync(Guid jobId)
        => await _repository.GetJobQuestionsAsync(jobId);

    public async Task AttachQuestionsToJobAsync(AttachQuestionsToJobRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (request.JobId == Guid.Empty) throw new ArgumentException("Job ID is required");
        if (request.QuestionIds == null || !request.QuestionIds.Any()) throw new ArgumentException("At least one question ID is required");

        await _repository.AttachQuestionsToJobAsync(request.JobId, request.QuestionIds);
    }

    public async Task AttachCategoryToJobAsync(AttachCategoryToJobRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (request.JobId == Guid.Empty) throw new ArgumentException("Job ID is required");
        if (request.CategoryId == Guid.Empty) throw new ArgumentException("Category ID is required");

        await _repository.AttachCategoryToJobAsync(request.JobId, request.CategoryId);
    }

    public async Task DetachQuestionFromJobAsync(Guid jobId, Guid questionId)
        => await _repository.DetachQuestionFromJobAsync(jobId, questionId);

    // ==================== Candidate Answers ====================
    public async Task SaveCandidateAnswersAsync(Guid jobApplyId, List<SubmitCandidateAnswerRequest> answers)
    {
        if (answers == null || !answers.Any()) return;
        await _repository.SaveCandidateAnswersAsync(jobApplyId, answers);
    }

    public async Task<IReadOnlyList<CandidateAnswerDto>> GetCandidateAnswersAsync(Guid jobApplyId)
    {
        var answers = await _repository.GetCandidateAnswersAsync(jobApplyId);

        // Calculate pass/fail for each answer
        foreach (var answer in answers)
        {
            if (answer.IsKnockout && !string.IsNullOrWhiteSpace(answer.ExpectedAnswer))
            {
                answer.Passed = string.Equals(answer.AnswerText?.Trim(), answer.ExpectedAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
            }
        }

        return answers;
    }

    /// <summary>
    /// Evaluates knockout answers for a job application and updates the PassedKnockout flag.
    /// Returns true if the candidate passed all knockout questions.
    /// </summary>
    public async Task<bool> EvaluateKnockoutAsync(Guid jobApplyId)
    {
        var answers = await GetCandidateAnswersAsync(jobApplyId);
        
        bool hasKnockouts = false;
        bool allPassed = true;

        foreach(var a in answers)
        {
            if (a.IsKnockout)
            {
                hasKnockouts = true;
                if (a.Passed != true)
                {
                    allPassed = false;
                    break;
                }
            }
        }

        // If no knockout questions, candidate passes by default
        if (!hasKnockouts)
            return true;

        var passed = allPassed;

        // Update the jobapplication record via the existing jobs repository
        await _jobsRepository.UpdatePassedKnockoutAsync(jobApplyId, passed);

        return passed;
    }

    // ==================== Parsed Resume ====================
    public async Task<ParsedResumeDto?> GetParsedResumeByJobApplyIdAsync(Guid jobApplyId)
        => await _repository.GetParsedResumeByJobApplyIdAsync(jobApplyId);
}
