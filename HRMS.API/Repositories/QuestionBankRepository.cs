using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class QuestionBankRepository : BaseRepository, IQuestionBankRepository
{
    public QuestionBankRepository(IDatabaseConnectionFactory connectionFactory) : base(connectionFactory) { }

    // ==================== Categories ====================
    public async Task<IEnumerable<QuestionCategoryDto>> GetCategoriesAsync(Guid organizationId)
    {
        var sql = @"
            SELECT c.categoryid AS CategoryId, c.organizationid AS OrganizationId, c.name AS Name,
                   c.createdat AS CreatedAt, c.updatedat AS UpdatedAt,
                   (SELECT COUNT(*) FROM jobs.questions q WHERE q.categoryid = c.categoryid AND q.isdeleted = false) AS QuestionCount
            FROM jobs.questioncategories c
            WHERE c.organizationid = @organizationId AND c.isdeleted = false
            ORDER BY c.createdat DESC";
        return await QueryAsync<QuestionCategoryDto>(sql, new { organizationId });
    }

    public async Task<QuestionCategoryDto?> GetCategoryByIdAsync(Guid categoryId)
    {
        var sql = @"
            SELECT c.categoryid AS CategoryId, c.organizationid AS OrganizationId, c.name AS Name,
                   c.createdat AS CreatedAt, c.updatedat AS UpdatedAt,
                   (SELECT COUNT(*) FROM jobs.questions q WHERE q.categoryid = c.categoryid AND q.isdeleted = false) AS QuestionCount
            FROM jobs.questioncategories c
            WHERE c.categoryid = @categoryId AND c.isdeleted = false";
        return await QuerySingleOrDefaultAsync<QuestionCategoryDto>(sql, new { categoryId });
    }

    public async Task<Guid> CreateCategoryAsync(Guid organizationId, string name)
    {
        var sql = @"
            INSERT INTO jobs.questioncategories (organizationid, name)
            VALUES (@organizationId, @name)
            RETURNING categoryid";
        return await ExecuteScalarAsync<Guid>(sql, new { organizationId, name });
    }

    public async Task<bool> UpdateCategoryAsync(Guid categoryId, string name)
    {
        var sql = @"
            UPDATE jobs.questioncategories
            SET name = @name, updatedat = CURRENT_TIMESTAMP
            WHERE categoryid = @categoryId AND isdeleted = false";
        var rows = await ExecuteAsync(sql, new { categoryId, name });
        return rows > 0;
    }

    public async Task<bool> DeleteCategoryAsync(Guid categoryId)
    {
        var sql = @"
            UPDATE jobs.questioncategories
            SET isdeleted = true, updatedat = CURRENT_TIMESTAMP
            WHERE categoryid = @categoryId";
        var rows = await ExecuteAsync(sql, new { categoryId });
        return rows > 0;
    }

    // ==================== Questions ====================
    public async Task<IEnumerable<QuestionDto>> GetQuestionsByCategoryAsync(Guid categoryId)
    {
        var sql = @"
            SELECT q.questionid AS QuestionId, q.categoryid AS CategoryId, c.name AS CategoryName,
                   q.organizationid AS OrganizationId, q.questiontext AS QuestionText,
                   q.questiontype AS QuestionType, q.options::text AS Options,
                   q.isknockout AS IsKnockout, q.expectedanswer AS ExpectedAnswer,
                   q.createdat AS CreatedAt, q.updatedat AS UpdatedAt
            FROM jobs.questions q
            LEFT JOIN jobs.questioncategories c ON q.categoryid = c.categoryid
            WHERE q.categoryid = @categoryId AND q.isdeleted = false
            ORDER BY q.createdat ASC";
        return await QueryAsync<QuestionDto>(sql, new { categoryId });
    }

    public async Task<IEnumerable<QuestionDto>> GetQuestionsByOrganizationAsync(Guid organizationId)
    {
        var sql = @"
            SELECT q.questionid AS QuestionId, q.categoryid AS CategoryId, c.name AS CategoryName,
                   q.organizationid AS OrganizationId, q.questiontext AS QuestionText,
                   q.questiontype AS QuestionType, q.options::text AS Options,
                   q.isknockout AS IsKnockout, q.expectedanswer AS ExpectedAnswer,
                   q.createdat AS CreatedAt, q.updatedat AS UpdatedAt
            FROM jobs.questions q
            LEFT JOIN jobs.questioncategories c ON q.categoryid = c.categoryid
            WHERE q.organizationid = @organizationId AND q.isdeleted = false
            ORDER BY c.name, q.createdat ASC";
        return await QueryAsync<QuestionDto>(sql, new { organizationId });
    }

    public async Task<QuestionDto?> GetQuestionByIdAsync(Guid questionId)
    {
        var sql = @"
            SELECT q.questionid AS QuestionId, q.categoryid AS CategoryId, c.name AS CategoryName,
                   q.organizationid AS OrganizationId, q.questiontext AS QuestionText,
                   q.questiontype AS QuestionType, q.options::text AS Options,
                   q.isknockout AS IsKnockout, q.expectedanswer AS ExpectedAnswer,
                   q.createdat AS CreatedAt, q.updatedat AS UpdatedAt
            FROM jobs.questions q
            LEFT JOIN jobs.questioncategories c ON q.categoryid = c.categoryid
            WHERE q.questionid = @questionId AND q.isdeleted = false";
        return await QuerySingleOrDefaultAsync<QuestionDto>(sql, new { questionId });
    }

    public async Task<Guid> CreateQuestionAsync(Guid organizationId, CreateQuestionRequest request)
    {
        var sql = @"
            INSERT INTO jobs.questions (categoryid, organizationid, questiontext, questiontype, options, isknockout, expectedanswer)
            VALUES (@CategoryId, @organizationId, @QuestionText, @QuestionType, CAST(@Options AS jsonb), @IsKnockout, @ExpectedAnswer)
            RETURNING questionid";
        return await ExecuteScalarAsync<Guid>(sql, new
        {
            request.CategoryId,
            organizationId,
            request.QuestionText,
            request.QuestionType,
            request.Options,
            request.IsKnockout,
            request.ExpectedAnswer
        });
    }

    public async Task<bool> UpdateQuestionAsync(Guid questionId, UpdateQuestionRequest request)
    {
        var existing = await GetQuestionByIdAsync(questionId);
        if (existing == null) return false;

        var sql = @"
            UPDATE jobs.questions
            SET questiontext = @QuestionText, questiontype = @QuestionType, options = CAST(@Options AS jsonb),
                isknockout = @IsKnockout, expectedanswer = @ExpectedAnswer, categoryid = @CategoryId,
                updatedat = CURRENT_TIMESTAMP
            WHERE questionid = @questionId AND isdeleted = false";
        var rows = await ExecuteAsync(sql, new
        {
            questionId,
            QuestionText = request.QuestionText ?? existing.QuestionText,
            QuestionType = request.QuestionType ?? existing.QuestionType,
            Options = request.Options ?? existing.Options,
            IsKnockout = request.IsKnockout ?? existing.IsKnockout,
            ExpectedAnswer = request.ExpectedAnswer ?? existing.ExpectedAnswer,
            CategoryId = request.CategoryId ?? existing.CategoryId
        });
        return rows > 0;
    }

    public async Task<bool> DeleteQuestionAsync(Guid questionId)
    {
        var sql = @"
            UPDATE jobs.questions
            SET isdeleted = true, updatedat = CURRENT_TIMESTAMP
            WHERE questionid = @questionId";
        var rows = await ExecuteAsync(sql, new { questionId });
        return rows > 0;
    }

    // ==================== Job-Question Mapping ====================
    public async Task<IEnumerable<JobQuestionDto>> GetJobQuestionsAsync(Guid jobId)
    {
        var sql = @"
            SELECT jq.jobquestionid AS JobQuestionId, jq.jobid AS JobId, jq.questionid AS QuestionId,
                   q.questiontext AS QuestionText, q.questiontype AS QuestionType, q.options::text AS Options,
                   q.isknockout AS IsKnockout, q.expectedanswer AS ExpectedAnswer, jq.isrequired AS IsRequired,
                   q.categoryid AS CategoryId, c.name AS CategoryName
            FROM jobs.jobquestions jq
            INNER JOIN jobs.questions q ON jq.questionid = q.questionid
            LEFT JOIN jobs.questioncategories c ON q.categoryid = c.categoryid
            WHERE jq.jobid = @jobId AND q.isdeleted = false
            ORDER BY q.createdat ASC";
        return await QueryAsync<JobQuestionDto>(sql, new { jobId });
    }

    public async Task AttachQuestionsToJobAsync(Guid jobId, List<Guid> questionIds)
    {
        foreach (var questionId in questionIds)
        {
            var sql = @"
                INSERT INTO jobs.jobquestions (jobid, questionid)
                VALUES (@jobId, @questionId)
                ON CONFLICT (jobid, questionid) DO NOTHING";
            await ExecuteAsync(sql, new { jobId, questionId });
        }
    }

    public async Task AttachCategoryToJobAsync(Guid jobId, Guid categoryId)
    {
        var sql = @"
            INSERT INTO jobs.jobquestions (jobid, questionid)
            SELECT @jobId, q.questionid
            FROM jobs.questions q
            WHERE q.categoryid = @categoryId AND q.isdeleted = false
            ON CONFLICT (jobid, questionid) DO NOTHING";
        await ExecuteAsync(sql, new { jobId, categoryId });
    }

    public async Task DetachQuestionFromJobAsync(Guid jobId, Guid questionId)
    {
        var sql = "DELETE FROM jobs.jobquestions WHERE jobid = @jobId AND questionid = @questionId";
        await ExecuteAsync(sql, new { jobId, questionId });
    }

    // ==================== Candidate Answers ====================
    public async Task SaveCandidateAnswersAsync(Guid jobApplyId, List<SubmitCandidateAnswerRequest> answers)
    {
        foreach (var answer in answers)
        {
            var sql = @"
                INSERT INTO jobs.candidateanswers (jobapplyid, questionid, answertext)
                VALUES (@jobApplyId, @QuestionId, @AnswerText)";
            await ExecuteAsync(sql, new { jobApplyId, answer.QuestionId, answer.AnswerText });
        }
    }

    public async Task<IEnumerable<CandidateAnswerDto>> GetCandidateAnswersAsync(Guid jobApplyId)
    {
        var sql = @"
            SELECT ca.answerid AS AnswerId, ca.jobapplyid AS JobApplyId, ca.questionid AS QuestionId,
                   q.questiontext AS QuestionText, q.questiontype AS QuestionType,
                   q.isknockout AS IsKnockout, q.expectedanswer AS ExpectedAnswer,
                   ca.answertext AS AnswerText
            FROM jobs.candidateanswers ca
            INNER JOIN jobs.questions q ON ca.questionid = q.questionid
            WHERE ca.jobapplyid = @jobApplyId
            ORDER BY q.createdat ASC";
        return await QueryAsync<CandidateAnswerDto>(sql, new { jobApplyId });
    }

    // ==================== Parsed Resumes ====================
    public async Task<Guid> CreateParsedResumeAsync(Guid jobApplyId)
    {
        var sql = @"
            INSERT INTO jobs.parsedresumes (jobapplyid, parsestatus)
            VALUES (@jobApplyId, 'Pending')
            ON CONFLICT (jobapplyid) DO UPDATE SET parsestatus = 'Pending', updatedat = CURRENT_TIMESTAMP
            RETURNING parsedresumeid";
        return await ExecuteScalarAsync<Guid>(sql, new { jobApplyId });
    }

    public async Task<bool> UpdateParsedResumeAsync(Guid parsedResumeId, string? contactDetails, string? skills, string? workExperience, string? education, string? rawText, string parseStatus, string? parseError)
    {
        var sql = @"
            UPDATE jobs.parsedresumes
            SET contactdetails = CAST(@contactDetails AS jsonb),
                skills = CAST(@skills AS jsonb),
                workexperience = CAST(@workExperience AS jsonb),
                education = CAST(@education AS jsonb),
                rawtext = @rawText,
                parsestatus = @parseStatus,
                parseerror = @parseError,
                updatedat = CURRENT_TIMESTAMP
            WHERE parsedresumeid = @parsedResumeId";
        var rows = await ExecuteAsync(sql, new { parsedResumeId, contactDetails, skills, workExperience, education, rawText, parseStatus, parseError });
        return rows > 0;
    }

    public async Task<ParsedResumeDto?> GetParsedResumeByJobApplyIdAsync(Guid jobApplyId)
    {
        var sql = @"
            SELECT parsedresumeid AS ParsedResumeId, jobapplyid AS JobApplyId,
                   contactdetails::text AS ContactDetails, skills::text AS Skills,
                   workexperience::text AS WorkExperience, education::text AS Education,
                   parsestatus AS ParseStatus, parseerror AS ParseError,
                   createdat AS CreatedAt, updatedat AS UpdatedAt
            FROM jobs.parsedresumes
            WHERE jobapplyid = @jobApplyId";
        return await QuerySingleOrDefaultAsync<ParsedResumeDto>(sql, new { jobApplyId });
    }

    public async Task<IEnumerable<ParsedResumeDto>> GetParsedResumesAsync(IEnumerable<Guid> jobApplyIds)
    {
        if (jobApplyIds == null || !jobApplyIds.Any()) return new List<ParsedResumeDto>();
        var sql = @"
            SELECT parsedresumeid AS ParsedResumeId, jobapplyid AS JobApplyId,
                   contactdetails::text AS ContactDetails, skills::text AS Skills,
                   workexperience::text AS WorkExperience, education::text AS Education,
                   parsestatus AS ParseStatus, parseerror AS ParseError,
                   createdat AS CreatedAt, updatedat AS UpdatedAt
            FROM jobs.parsedresumes
            WHERE jobapplyid = ANY(@ids)";
        return await QueryAsync<ParsedResumeDto>(sql, new { ids = jobApplyIds.ToArray() });
    }
}
