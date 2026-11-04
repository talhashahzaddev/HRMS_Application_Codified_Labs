using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.AspNetCore.SignalR;
using System.Text;
using System.Text.Json;

namespace HRMS.API.Services;

public class ResumeParserService : IResumeParserService
{
    private readonly IQuestionBankRepository _repository;
    private readonly ILogger<ResumeParserService> _logger;
    private readonly HttpClient _httpClient;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<HRMS.API.Hubs.CvParsingHub> _hubContext;

    public ResumeParserService(
        IQuestionBankRepository repository,
        ILogger<ResumeParserService> logger,
        HttpClient httpClient,
        Microsoft.AspNetCore.SignalR.IHubContext<HRMS.API.Hubs.CvParsingHub> hubContext)
    {
        _repository = repository;
        _logger = logger;
        _httpClient = httpClient;
        _hubContext = hubContext;
    }

    public async Task ParseResumeAsync(Guid jobApplyId, string resumeUrl, string aiApiKey)
    {
        try
        {
            _logger.LogInformation("Starting resume parsing for JobApplyId: {JobApplyId}, URL: {ResumeUrl}", jobApplyId, resumeUrl);

            // 1. Download Resume
            var fileBytes = await DownloadFileAsync(resumeUrl);
            if (fileBytes == null || fileBytes.Length == 0)
            {
                await _repository.UpdateParsedResumeAsync(Guid.Empty, null, null, null, null, null, "Failed", "Could not download resume from URL.");
                await NotifyClientAsync(jobApplyId, "Failed");
                return;
            }

            var parsedResume = await _repository.GetParsedResumeByJobApplyIdAsync(jobApplyId);
            var parsedResumeId = parsedResume?.ParsedResumeId ?? await _repository.CreateParsedResumeAsync(jobApplyId);

            // 2. Extract Text (assuming PDF for now)
            string rawText;
            try
            {
                rawText = ExtractTextFromPdf(fileBytes);
                if (string.IsNullOrWhiteSpace(rawText))
                {
                    await _repository.UpdateParsedResumeAsync(parsedResumeId, null, null, null, null, null, "Failed", "No text extracted from PDF.");
                    await NotifyClientAsync(jobApplyId, "Failed");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from PDF for JobApplyId: {JobApplyId}", jobApplyId);
                await _repository.UpdateParsedResumeAsync(parsedResumeId, null, null, null, null, null, "Failed", "Error extracting text: " + ex.Message);
                await NotifyClientAsync(jobApplyId, "Failed");
                return;
            }

            // 3. Call Gemini AI
            var (parsedData, errorMsg) = await CallGeminiApiAsync(rawText, aiApiKey);
            if (parsedData == null)
            {
                await _repository.UpdateParsedResumeAsync(parsedResumeId, null, null, null, null, rawText, "Failed", "Gemini API Error: " + (errorMsg ?? "Unknown"));
                await NotifyClientAsync(jobApplyId, "Failed");
                return;
            }

            // 4. Update Database
            await _repository.UpdateParsedResumeAsync(
                parsedResumeId,
                parsedData.ContactDetails,
                parsedData.Skills,
                parsedData.WorkExperience,
                parsedData.Education,
                rawText,
                "Completed",
                null
            );

            _logger.LogInformation("Successfully parsed resume for JobApplyId: {JobApplyId}", jobApplyId);
            await NotifyClientAsync(jobApplyId, "Completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error parsing resume for JobApplyId: {JobApplyId}", jobApplyId);
            var parsedResume = await _repository.GetParsedResumeByJobApplyIdAsync(jobApplyId);
            if (parsedResume != null)
            {
                await _repository.UpdateParsedResumeAsync(parsedResume.ParsedResumeId, null, null, null, null, null, "Failed", ex.Message);
            }
            await NotifyClientAsync(jobApplyId, "Failed");
        }
    }

    private async Task NotifyClientAsync(Guid jobApplyId, string status)
    {
        await _hubContext.Clients.Group(jobApplyId.ToString()).SendAsync("ParsingStatus", status);
    }

    private async Task<byte[]> DownloadFileAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from URL: {Url}", url);
            return Array.Empty<byte>();
        }
    }

    private string ExtractTextFromPdf(byte[] pdfBytes)
    {
        using var reader = new PdfReader(pdfBytes);
        var text = new StringBuilder();

        for (int i = 1; i <= reader.NumberOfPages; i++)
        {
            text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
            text.Append("\n");
        }

        return text.ToString();
    }

    private async Task<(ParsedResumeData? Data, string? Error)> CallGeminiApiAsync(string resumeText, string aiApiKey)
    {
        var prompt = @"
You are an expert ATS (Applicant Tracking System) parser. Extract the following information from the provided resume text.
Return ONLY a valid JSON object matching the EXACT structure below. Do not return markdown, do not return any conversational text, just the raw JSON.

Structure:
{
  ""contactDetails"": {
    ""email"": ""string"",
    ""phone"": ""string"",
    ""linkedin"": ""string""
  },
  ""skills"": [
    ""string""
  ],
  ""workExperience"": [
    {
      ""company"": ""string"",
      ""jobTitle"": ""string"",
      ""startDate"": ""YYYY-MM"",
      ""endDate"": ""YYYY-MM or Present"",
      ""description"": ""string""
    }
  ],
  ""education"": [
    {
      ""institution"": ""string"",
      ""degree"": ""string"",
      ""yearOfPassing"": ""YYYY""
    }
  ]
}

Resume Text:
" + resumeText;

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new { response_mime_type = "application/json" }
        };

        var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";
        
        var requestContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = requestContent;
        request.Headers.Add("x-goog-api-key", aiApiKey);
        
        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API call failed: {StatusCode} {Reason}", response.StatusCode, response.ReasonPhrase);
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError("Gemini API Error: {Error}", err);
            return (null, $"HTTP {response.StatusCode}: {err}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        
        // Parse Gemini response structure
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var textResult = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text").GetString();

            if (string.IsNullOrWhiteSpace(textResult)) return (null, "AI returned empty text content.");

            textResult = textResult.Trim();
            
            // Robust extraction: find the first { and last } to ignore any markdown or conversational text
            int firstBrace = textResult.IndexOf('{');
            int lastBrace = textResult.LastIndexOf('}');
            
            JsonDocument? resultDoc = null;
            bool parsed = false;

            while (!parsed && lastBrace > firstBrace)
            {
                try
                {
                    string candidate = textResult.Substring(firstBrace, lastBrace - firstBrace + 1);
                    resultDoc = JsonDocument.Parse(candidate);
                    parsed = true;
                    textResult = candidate;
                }
                catch (JsonException)
                {
                    // If parsing fails (e.g. extra trailing '}' from LLM), try the previous '}'
                    lastBrace = textResult.LastIndexOf('}', lastBrace - 1);
                }
            }

            if (!parsed || resultDoc == null)
            {
                return (null, "AI returned malformed JSON that could not be repaired.");
            }
            
            var contactDetails = resultDoc.RootElement.TryGetProperty("contactDetails", out var cd) ? cd.GetRawText() : "{}";
            var skills = resultDoc.RootElement.TryGetProperty("skills", out var sk) ? sk.GetRawText() : "[]";
            var workExperience = resultDoc.RootElement.TryGetProperty("workExperience", out var we) ? we.GetRawText() : "[]";
            var education = resultDoc.RootElement.TryGetProperty("education", out var ed) ? ed.GetRawText() : "[]";

            // Ensure resultDoc is disposed after we extract RawText, since GetRawText() creates a string copy
            resultDoc.Dispose();

            return (new ParsedResumeData
            {
                ContactDetails = contactDetails,
                Skills = skills,
                WorkExperience = workExperience,
                Education = education
            }, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse JSON from Gemini API response. Response: {Response}", responseJson);
            return (null, $"Parse Exception: {ex.Message}. Raw API Response: {responseJson}");
        }
    }

    private class ParsedResumeData
    {
        public string? ContactDetails { get; set; }
        public string? Skills { get; set; }
        public string? WorkExperience { get; set; }
        public string? Education { get; set; }
    }
}
