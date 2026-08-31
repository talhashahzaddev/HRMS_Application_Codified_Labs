using HRMS.API.Authorization;
using HRMS.API.Models.DTOs;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuestionBankController : ControllerBase
{
    private readonly IQuestionBankService _questionBankService;
    private readonly ILogger<QuestionBankController> _logger;

    public QuestionBankController(IQuestionBankService questionBankService, ILogger<QuestionBankController> logger)
    {
        _questionBankService = questionBankService;
        _logger = logger;
    }

    // ==================== Categories ====================
    [HttpGet("categories")]
    [PermissionAuthorize("question_bank_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<QuestionCategoryDto>>>> GetCategories()
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var categories = await _questionBankService.GetCategoriesAsync(orgId);
            return Ok(new ServiceResponse<IReadOnlyList<QuestionCategoryDto>> { Success = true, Data = categories });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting question categories.");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<QuestionCategoryDto>> { Success = false, Message = ex.Message });
        }
    }

    [HttpPost("categories")]
    [PermissionAuthorize("add_question_category")]
    public async Task<ActionResult<ServiceResponse<QuestionCategoryDto>>> CreateCategory([FromBody] CreateQuestionCategoryRequest request)
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var category = await _questionBankService.CreateCategoryAsync(orgId, request);
            return Ok(new ServiceResponse<QuestionCategoryDto> { Success = true, Data = category });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating question category.");
            return StatusCode(500, new ServiceResponse<QuestionCategoryDto> { Success = false, Message = ex.Message });
        }
    }

    [HttpPut("categories/{id}")]
    [PermissionAuthorize("edit_question_category")]
    public async Task<ActionResult<ServiceResponse<QuestionCategoryDto>>> UpdateCategory(Guid id, [FromBody] UpdateQuestionCategoryRequest request)
    {
        try
        {
            var category = await _questionBankService.UpdateCategoryAsync(id, request);
            if (category == null) return NotFound(new ServiceResponse<QuestionCategoryDto> { Success = false, Message = "Category not found." });
            return Ok(new ServiceResponse<QuestionCategoryDto> { Success = true, Data = category });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question category.");
            return StatusCode(500, new ServiceResponse<QuestionCategoryDto> { Success = false, Message = ex.Message });
        }
    }

    [HttpDelete("categories/{id}")]
    [PermissionAuthorize("delete_question_category")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteCategory(Guid id)
    {
        try
        {
            var result = await _questionBankService.DeleteCategoryAsync(id);
            return Ok(new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully" : "Category not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting question category.");
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = ex.Message });
        }
    }

    // ==================== Questions ====================
    [HttpGet("questions")]
    [PermissionAuthorize("question_bank_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<QuestionDto>>>> GetQuestionsByOrg()
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var questions = await _questionBankService.GetQuestionsByOrganizationAsync(orgId);
            return Ok(new ServiceResponse<IReadOnlyList<QuestionDto>> { Success = true, Data = questions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions.");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<QuestionDto>> { Success = false, Message = ex.Message });
        }
    }

    [HttpGet("categories/{categoryId}/questions")]
    [PermissionAuthorize("question_bank_view")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<QuestionDto>>>> GetQuestionsByCategory(Guid categoryId)
    {
        try
        {
            var questions = await _questionBankService.GetQuestionsByCategoryAsync(categoryId);
            return Ok(new ServiceResponse<IReadOnlyList<QuestionDto>> { Success = true, Data = questions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions by category.");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<QuestionDto>> { Success = false, Message = ex.Message });
        }
    }

    [HttpPost("questions")]
    [PermissionAuthorize("add_question")]
    public async Task<ActionResult<ServiceResponse<QuestionDto>>> CreateQuestion([FromBody] CreateQuestionRequest request)
    {
        try
        {
            var orgId = GetCurrentOrganizationId();
            var question = await _questionBankService.CreateQuestionAsync(orgId, request);
            return Ok(new ServiceResponse<QuestionDto> { Success = true, Data = question });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating question.");
            return StatusCode(500, new ServiceResponse<QuestionDto> { Success = false, Message = ex.Message });
        }
    }

    [HttpPut("questions/{id}")]
    [PermissionAuthorize("edit_question")]
    public async Task<ActionResult<ServiceResponse<QuestionDto>>> UpdateQuestion(Guid id, [FromBody] UpdateQuestionRequest request)
    {
        try
        {
            var question = await _questionBankService.UpdateQuestionAsync(id, request);
            if (question == null) return NotFound(new ServiceResponse<QuestionDto> { Success = false, Message = "Question not found." });
            return Ok(new ServiceResponse<QuestionDto> { Success = true, Data = question });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question.");
            return StatusCode(500, new ServiceResponse<QuestionDto> { Success = false, Message = ex.Message });
        }
    }

    [HttpDelete("questions/{id}")]
    [PermissionAuthorize("delete_question")]
    public async Task<ActionResult<ServiceResponse<bool>>> DeleteQuestion(Guid id)
    {
        try
        {
            var result = await _questionBankService.DeleteQuestionAsync(id);
            return Ok(new ServiceResponse<bool> { Success = result, Data = result, Message = result ? "Deleted successfully" : "Question not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting question.");
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = ex.Message });
        }
    }

    // ==================== Job-Question Mapping ====================
    [HttpGet("jobs/{jobId}/questions")]
    [AllowAnonymous] // Allow public access to view questions on job application page
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<JobQuestionDto>>>> GetJobQuestions(Guid jobId)
    {
        try
        {
            var questions = await _questionBankService.GetJobQuestionsAsync(jobId);
            return Ok(new ServiceResponse<IReadOnlyList<JobQuestionDto>> { Success = true, Data = questions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job questions.");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<JobQuestionDto>> { Success = false, Message = ex.Message });
        }
    }

    [HttpPost("jobs/attach-questions")]
    [PermissionAuthorize("opnongs_edit")]
    public async Task<ActionResult<ServiceResponse<bool>>> AttachQuestionsToJob([FromBody] AttachQuestionsToJobRequest request)
    {
        try
        {
            await _questionBankService.AttachQuestionsToJobAsync(request);
            return Ok(new ServiceResponse<bool> { Success = true, Data = true, Message = "Attached successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching questions to job.");
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = ex.Message });
        }
    }

    [HttpPost("jobs/attach-category")]
    [PermissionAuthorize("opnongs_edit")]
    public async Task<ActionResult<ServiceResponse<bool>>> AttachCategoryToJob([FromBody] AttachCategoryToJobRequest request)
    {
        try
        {
            await _questionBankService.AttachCategoryToJobAsync(request);
            return Ok(new ServiceResponse<bool> { Success = true, Data = true, Message = "Attached category questions successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching category questions to job.");
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = ex.Message });
        }
    }

    [HttpDelete("jobs/{jobId}/questions/{questionId}")]
    [PermissionAuthorize("opnongs_edit")]
    public async Task<ActionResult<ServiceResponse<bool>>> DetachQuestionFromJob(Guid jobId, Guid questionId)
    {
        try
        {
            await _questionBankService.DetachQuestionFromJobAsync(jobId, questionId);
            return Ok(new ServiceResponse<bool> { Success = true, Data = true, Message = "Detached successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detaching question from job.");
            return StatusCode(500, new ServiceResponse<bool> { Success = false, Message = ex.Message });
        }
    }

    // ==================== Candidate Answers ====================
    [HttpGet("applications/{jobApplyId}/answers")]
    public async Task<ActionResult<ServiceResponse<IReadOnlyList<CandidateAnswerDto>>>> GetCandidateAnswers(Guid jobApplyId)
    {
        try
        {
            var answers = await _questionBankService.GetCandidateAnswersAsync(jobApplyId);
            return Ok(new ServiceResponse<IReadOnlyList<CandidateAnswerDto>> { Success = true, Data = answers });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting candidate answers.");
            return StatusCode(500, new ServiceResponse<IReadOnlyList<CandidateAnswerDto>> { Success = false, Message = ex.Message });
        }
    }

    // ==================== Parsed Resumes ====================
    [HttpGet("applications/{jobApplyId}/parsed-resume")]
    public async Task<ActionResult<ServiceResponse<ParsedResumeDto>>> GetParsedResume(Guid jobApplyId)
    {
        try
        {
            var resume = await _questionBankService.GetParsedResumeByJobApplyIdAsync(jobApplyId);
            return Ok(new ServiceResponse<ParsedResumeDto> { Success = true, Data = resume });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parsed resume.");
            return StatusCode(500, new ServiceResponse<ParsedResumeDto> { Success = false, Message = ex.Message });
        }
    }

    private Guid GetCurrentOrganizationId()
    {
        var orgIdClaim = User.FindFirstValue("organizationId");
        if (string.IsNullOrEmpty(orgIdClaim))
        {
            _logger.LogError("Organization ID claim not found in JWT token");
            throw new UnauthorizedAccessException("Organization ID not found in token");
        }
        if (!Guid.TryParse(orgIdClaim, out var organizationId))
        {
            _logger.LogError("Invalid organization ID format in JWT token: {OrganizationId}", orgIdClaim);
            throw new UnauthorizedAccessException("Invalid organization ID format");
        }
        return organizationId;
    }
}
