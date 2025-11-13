using AutoMapper;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class PerformanceService : IPerformanceService
{
    private readonly IPerformanceRepository _performanceRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PerformanceService> _logger;

    public PerformanceService(
        IPerformanceRepository performanceRepository,
        IMapper mapper,
        ILogger<PerformanceService> logger)
    {
        _performanceRepository = performanceRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppraisalCycleDto> CreateAppraisalCycleAsync(string cycleName, DateTime startDate, DateTime endDate, Guid organizationId, string? description = null)
    {
        var cycle = await _performanceRepository.CreateAppraisalCycleAsync(organizationId, cycleName, startDate, endDate, description);
        return _mapper.Map<AppraisalCycleDto>(cycle);
    }

    public async Task<List<AppraisalCycleDto>> GetAppraisalCyclesAsync(Guid organizationId)
    {
        return await _performanceRepository.GetAppraisalCyclesAsync(organizationId);
    }

    public async Task<EmployeeAppraisalDto> CreateEmployeeAppraisalAsync(CreateAppraisalRequest request, Guid reviewerId)
    {
        try
        {
            var appraisal = new EmployeeAppraisal
            {
                AppraisalId = Guid.NewGuid(),      // generate new Id
                CycleId = request.CycleId,
                EmployeeId = request.EmployeeId,
                ReviewerId = reviewerId,
                ReviewType = request.ReviewType,
                OverallRating = request.OverallRating,
                KraRatings = request.KraRatings,
                SkillRatings = request.SkillRatings,
                Feedback = request.Feedback,
                ImprovementAreas = request.ImprovementAreas,
                DevelopmentPlan = request.DevelopmentPlan,
                Status = "draft",
                SubmittedAt = null,
                ReviewedAt = null,
                GoalsAchieved = new Dictionary<string, object>() // keep empty for now
            };
            appraisal.ReviewerId = reviewerId;

            var createdAppraisal = await _performanceRepository.CreateEmployeeAppraisalAsync(appraisal);
            var dto = new EmployeeAppraisalDto
            {
                AppraisalId = createdAppraisal.AppraisalId,
                CycleId = createdAppraisal.CycleId,
                EmployeeId = createdAppraisal.EmployeeId,
                EmployeeName = createdAppraisal.Employee?.FullName ?? string.Empty,  // <-- assuming Employee has FullName
                CycleName = createdAppraisal.AppraisalCycle?.CycleName ?? string.Empty,   // <-- assuming Cycle has Name
                ReviewerName = createdAppraisal.Reviewer?.FullName,                  // nullable
                ReviewType = createdAppraisal.ReviewType,
                OverallRating = createdAppraisal.OverallRating,
                KraRatings = createdAppraisal.KraRatings,
                SkillRatings = createdAppraisal.SkillRatings,
                Feedback = createdAppraisal.Feedback,
                ImprovementAreas = createdAppraisal.ImprovementAreas,
                Status = createdAppraisal.Status,
                SubmittedAt = createdAppraisal.SubmittedAt,
                ReviewedAt = createdAppraisal.ReviewedAt
            };

            return dto;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsAsync(Guid cycleId, Guid? employeeId = null)
    {
        return await _performanceRepository.GetEmployeeAppraisalsAsync(cycleId, employeeId);
    }

    public async Task<SkillSetDto?> CreateSkillSetAsync(CreateSkillSetRequest request, Guid organizationId)
    {
        try
        {
            return await _performanceRepository.CreateSkillSetAsync(request, organizationId);
        }
        catch (InvalidOperationException ex)
        {
            // Forward duplicate error to controller
            throw;
        }
        //return new SkillSetDto
        //{
        //    SkillId = Guid.NewGuid(),
        //    SkillName = request.SkillName,
        //    Category = request.Category ?? "General",
        //    Description = request.Description,
        //    IsActive = request.IsActive,
        //    CreatedAt = DateTime.UtcNow
        //};
    }



    public async Task<List<SkillSetDto>> GetSkillSetsAsync(Guid organizationId)
    {
        return await _performanceRepository.GetSkillSetsAsync(organizationId);
    }



    public async Task<List<EmployeeSkillDto>> GetEmployeeSkillsAsync(Guid employeeId)
    {
        return await _performanceRepository.GetEmployeeSkillsAsync(employeeId);
    }

    public async Task<PerformanceSummaryDto> GetPerformanceSummaryAsync(Guid organizationId, Guid? cycleId = null)
    {
        return await _performanceRepository.GetPerformanceSummaryAsync(organizationId, cycleId);
    }

    public async Task<List<TopPerformerDto>> GetTopPerformersAsync(Guid organizationId, int count = 10)
    {
        return await _performanceRepository.GetTopPerformersAsync(organizationId, count);
    }

    // New missing methods implementation
    public async Task<PerformanceSummaryDto> GetPerformanceDashboardSummaryAsync(Guid organizationId)
    {
        _logger.LogInformation("Getting performance dashboard summary for organization: {OrganizationId}", organizationId);
        return await GetPerformanceSummaryAsync(organizationId);
    }

    public async Task<PagedResult<SkillSetDto>> GetSkillSetsPagedAsync(Guid organizationId, SkillSetFilterRequest filter, int page, int limit)
    {
        _logger.LogInformation("Getting paged skill sets for organization: {OrganizationId}", organizationId);
        var skillSets = await GetSkillSetsAsync(organizationId);

        if (!string.IsNullOrEmpty(filter.Category))
            skillSets = skillSets.Where(s => s.Category == filter.Category).ToList();
        if (filter.IsActive.HasValue)
            skillSets = skillSets.Where(s => s.IsActive == filter.IsActive.Value).ToList();
        if (!string.IsNullOrEmpty(filter.Search))
            skillSets = skillSets.Where(s => s.SkillName.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)).ToList();

        var totalItems = skillSets.Count;
        var pagedItems = skillSets.Skip((page - 1) * limit).Take(limit).ToList();

        return new PagedResult<SkillSetDto>
        {
            Data = pagedItems,
            TotalCount = totalItems,
            Page = page,
            PageSize = limit
        };
    }

    public async Task<SkillSetDto?> GetSkillSetByIdAsync(Guid skillSetId)
    {
        _logger.LogInformation("Getting skill set by ID: {SkillSetId}", skillSetId);
        var skillSets = await GetSkillSetsAsync(Guid.NewGuid());
        return skillSets.FirstOrDefault(s => s.SkillId == skillSetId);
    }

    //public async Task<SkillSetDto> CreateSkillSetAsync(CreateSkillSetRequest request, Guid organizationId)
    //{
    //    _logger.LogInformation("Creating skill set: {SkillName}", request.SkillName);

    //    return new SkillSetDto
    //    {
    //        SkillId = Guid.NewGuid(),
    //        SkillName = request.SkillName,
    //        Category = request.Category ?? "General",
    //        Description = request.Description,
    //        IsActive = request.IsActive,
    //        CreatedAt = DateTime.UtcNow
    //    };
    //}

    public async Task<SkillSetDto> UpdateSkillSetAsync(Guid skillSetId, UpdateSkillSetRequest request)
    {
        _logger.LogInformation("Updating skill set: {SkillSetId}", skillSetId);

        var skillSet = await GetSkillSetByIdAsync(skillSetId);
        if (skillSet == null)
            throw new KeyNotFoundException($"Skill set with ID {skillSetId} not found");

        skillSet.SkillName = request.SkillName;
        skillSet.Category = request.Category ?? skillSet.Category;
        skillSet.Description = request.Description;
        skillSet.IsActive = request.IsActive;

        return skillSet;
    }

    public async Task<bool> DeleteSkillSetAsync(Guid skillSetId)
    {
        _logger.LogInformation("Deleting skill set: {SkillSetId}", skillSetId);
        return true;
    }

    public async Task<PagedResult<EmployeeSkillDto>> GetEmployeeSkillsPagedAsync(Guid organizationId, EmployeeSkillFilterRequest filter, int page, int limit)
    {
        _logger.LogInformation("Getting paged employee skills for organization: {OrganizationId}", organizationId);

        var skills = new List<EmployeeSkillDto>
        {
            new() { EmployeeSkillId = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), SkillId = Guid.NewGuid(), ProficiencyLevel = 4, AssessedBy = Guid.NewGuid(), LastAssessed = DateTime.UtcNow.AddDays(-30) },
            new() { EmployeeSkillId = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), SkillId = Guid.NewGuid(), ProficiencyLevel = 3, AssessedBy = Guid.NewGuid(), LastAssessed = DateTime.UtcNow.AddDays(-45) }
        };

        if (filter.EmployeeId.HasValue)
            skills = skills.Where(s => s.EmployeeId == filter.EmployeeId).ToList();
        if (filter.ProficiencyLevel.HasValue)
            skills = skills.Where(s => s.ProficiencyLevel == filter.ProficiencyLevel).ToList();

        var totalItems = skills.Count;
        var pagedItems = skills.Skip((page - 1) * limit).Take(limit).ToList();

        return new PagedResult<EmployeeSkillDto>
        {
            Data = pagedItems,
            TotalCount = totalItems,
            Page = page,
            PageSize = limit
        };
    }

    public async Task<List<EmployeeSkillDto>> GetEmployeeSkillsByEmployeeAsync(Guid employeeId)
    {
        _logger.LogInformation("Getting employee skills for employee: {EmployeeId}", employeeId);
        return await GetEmployeeSkillsAsync(employeeId);
    }

    public async Task<EmployeeSkillDto?> GetEmployeeSkillByIdAsync(Guid employeeSkillId)
    {
        _logger.LogInformation("Getting employee skill by ID: {EmployeeSkillId}", employeeSkillId);

        return new EmployeeSkillDto
        {
            EmployeeSkillId = employeeSkillId,
            EmployeeId = Guid.NewGuid(),
            SkillId = Guid.NewGuid(),
            ProficiencyLevel = 4,
            AssessedBy = Guid.NewGuid(),
            LastAssessed = DateTime.UtcNow.AddDays(-30)
        };
    }

    public async Task<EmployeeSkillDto> CreateEmployeeSkillAsync(CreateEmployeeSkillRequest request)
    {
        try
        {
            // ✅ 1. Basic validation
            if (request.EmployeeId == Guid.Empty || request.SkillId == Guid.Empty)
                throw new ArgumentException("EmployeeId and SkillId are required.");

            if (request.ProficiencyLevel < 1 || request.ProficiencyLevel > 5)
                throw new ArgumentException("Proficiency level must be between 1 and 5.");

            // ✅ 2. Default values
            if (!request.LastAssessed.HasValue)
                request.LastAssessed = DateTime.UtcNow;

            // agar assessedBy empty hai to default (e.g., system user / HR)
            if (!request.AssessedBy.HasValue)
                request.AssessedBy = Guid.Parse("00000000-0000-0000-0000-000000000000");

            // ✅ 3. Call repository
            var result = await _performanceRepository.CreateEmployeeSkillAsync(request);

            if (result == null)
                throw new Exception("Failed to create employee skill record.");

            _logger.LogInformation("✅ Successfully created skill for employee {EmployeeId}", request.EmployeeId);

            return result;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "⚠ Validation failed for employee skill creation");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error while creating employee skill");
            throw;
        }
    }


    //public async Task<EmployeeSkillDto> CreateEmployeeSkillAsync(CreateEmployeeSkillRequest request)
    //{
    //    try
    //    {
    //        // 🔍 Basic validation (extra safety)
    //        if (request.EmployeeId == Guid.Empty || request.SkillId == Guid.Empty)
    //            throw new ArgumentException("EmployeeId and SkillId are required.");

    //        if (request.ProficiencyLevel < 1 || request.ProficiencyLevel > 5)
    //            throw new ArgumentException("Proficiency level must be between 1 and 5.");

    //        // 🧠 Call repository to insert record
    //        var result = await _performanceRepository.CreateEmployeeSkillAsync(request);

    //        if (result == null)
    //            throw new Exception("Failed to create employee skill record.");

    //        _logger.LogInformation("Successfully created skill for employee {EmployeeId}", request.EmployeeId);
    //        return result;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error while creating employee skill");
    //        throw;
    //    }
    //}

    //    //_logger.LogInformation("Creating employee skill for employee: {EmployeeId}", request.EmployeeId);

    //return new EmployeeSkillDto
    //{
    //    EmployeeSkillId = Guid.NewGuid(),
    //    EmployeeId = request.EmployeeId,
    //    SkillId = request.SkillId,
    //    ProficiencyLevel = request.ProficiencyLevel,
    //    AssessorName = request.AssessorName,
    //    LastAssessed = request.LastAssessed ?? DateTime.UtcNow,
    //    Notes = request.Notes
    //};


    public async Task<EmployeeSkillDto> UpdateEmployeeSkillAsync(Guid employeeSkillId, UpdateEmployeeSkillRequest request)
    {
        _logger.LogInformation("Updating employee skill: {EmployeeSkillId}", employeeSkillId);

        var skill = await GetEmployeeSkillByIdAsync(employeeSkillId);
        if (skill == null)
            throw new KeyNotFoundException($"Employee skill with ID {employeeSkillId} not found");

        skill.ProficiencyLevel = request.ProficiencyLevel;
        skill.AssessedBy = request.AssessedBy;
        skill.LastAssessed = request.LastAssessed ?? skill.LastAssessed;
        skill.Notes = request.Notes;

        return skill;
    }

    public async Task<bool> DeleteEmployeeSkillAsync(Guid employeeSkillId)
    {
        _logger.LogInformation("Deleting employee skill: {EmployeeSkillId}", employeeSkillId);
        return true;
    }

    public async Task<PagedResult<KRADto>> GetKRAsAsync(Guid organizationId, int page, int limit, string? search)
    {
        _logger.LogInformation("Getting KRAs for organization: {OrganizationId}", organizationId);

        var kras = await _performanceRepository.GetKRAsAsync(organizationId);



        if (!string.IsNullOrEmpty(search))
            kras = kras.Where(k => k.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        var totalItems = kras.Count;
        var pagedItems = kras.Skip((page - 1) * limit).Take(limit).ToList();

        return new PagedResult<KRADto>
        {
            Data = pagedItems,
            TotalCount = totalItems,
            Page = page,
            PageSize = limit
        };
    }

    public async Task<KRADto?> GetKRAByIdAsync(Guid kraId)
    {
        _logger.LogInformation("Getting KRA by ID: {KRAId}", kraId);

        var result = await _performanceRepository.GetKraByIdAsync(kraId);
        return result;

    }

    public async Task<KRADto> CreateKRAAsync(Guid organizationId, CreateKRARequest request)
    {
        _logger.LogInformation("Creating KRA: {Title}", request.Title);

        var result = await _performanceRepository.CreateKRAAsync(request, organizationId);


        return result;
    }

    public async Task<KRADto> UpdateKRAAsync(Guid kraId, UpdateKRARequest request)
    {
        _logger.LogInformation("Checking Weather KRa with this id exisits or not");

        var kra = await _performanceRepository.GetKraByIdAsync(kraId);
        if (kra == null)
        {
            return null;
        }

        else
        {
            var result = await _performanceRepository.UpdatekraAsync(kraId, request);
            return result;
        }

    }

    public async Task<bool> UpdateKRAStatusAsync(Guid kraId, bool isActive)
    {
        var kra = await _performanceRepository.GetKraByIdAsync(kraId);
        if (kra == null)
        {
            return false;
        }

        return await _performanceRepository.UpdateKRAStatusAsync(kraId, isActive);
    }





    public async Task<bool> DeleteKRAAsync(Guid kraId)
    {
        _logger.LogInformation("Deleting KRA: {KRAId}", kraId);
        return await _performanceRepository.DeleteKRAAsync(kraId);
    }

    public async Task<PagedResult<AppraisalCycleDto>> GetAppraisalCyclesPagedAsync(Guid organizationId, int page, int limit, string? search)
    {
        _logger.LogInformation("Getting paged appraisal cycles for organization: {OrganizationId}", organizationId);

        var cycles = await GetAppraisalCyclesAsync(organizationId);

        if (!string.IsNullOrEmpty(search))
            cycles = cycles.Where(c => c.CycleName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        var totalItems = cycles.Count;
        var pagedItems = cycles.Skip((page - 1) * limit).Take(limit).ToList();

        return new PagedResult<AppraisalCycleDto>
        {
            Data = pagedItems,
            TotalCount = totalItems,
            Page = page,
            PageSize = limit
        };
    }

    public async Task<AppraisalCycleDto?> GetAppraisalCycleByIdAsync(Guid cycleId)
    {
        _logger.LogInformation("Getting appraisal cycle by ID: {CycleId}", cycleId);
        return await _performanceRepository.GetAppraisalCycleByIdAsync(cycleId);
    }

    public async Task<AppraisalCycleDto> UpdateAppraisalCycleAsync(Guid cycleId, UpdateAppraisalCycleRequest request)
    {
        _logger.LogInformation("Updating appraisal cycle: {CycleId}", cycleId);
        return await _performanceRepository.UpdateAppraisalCycleAsync(cycleId, request);
    }

    public async Task<bool> DeleteAppraisalCycleAsync(Guid cycleId)
    {
        _logger.LogInformation("Deleting appraisal cycle: {CycleId}", cycleId);
        return await _performanceRepository.DeleteAppraisalCycleAsync(cycleId);
    }

    public async Task<PagedResult<EmployeeAppraisalDto>> GetEmployeeAppraisalsPagedAsync(Guid organizationId, AppraisalFilterRequest filter, int page, int limit)
    {
        _logger.LogInformation("Getting paged employee appraisals for organization: {OrganizationId}", organizationId);

        var appraisals = new List<EmployeeAppraisalDto>
        {
            new() { AppraisalId = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), CycleId = Guid.NewGuid(), Status = "Completed", OverallRating = 4.5m, SubmittedAt = DateTime.UtcNow.AddDays(-10) },
            new() { AppraisalId = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), CycleId = Guid.NewGuid(), Status = "Pending", OverallRating = null, SubmittedAt = null }
        };

        if (filter.AppraisalCycleId.HasValue)
            appraisals = appraisals.Where(a => a.CycleId == filter.AppraisalCycleId).ToList();
        if (!string.IsNullOrEmpty(filter.Status))
            appraisals = appraisals.Where(a => a.Status == filter.Status).ToList();
        if (filter.EmployeeId.HasValue)
            appraisals = appraisals.Where(a => a.EmployeeId == filter.EmployeeId).ToList();

        var totalItems = appraisals.Count;
        var pagedItems = appraisals.Skip((page - 1) * limit).Take(limit).ToList();

        return new PagedResult<EmployeeAppraisalDto>
        {
            Data = pagedItems,
            TotalCount = totalItems,
            Page = page,
            PageSize = limit
        };
    }

    public async Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsByEmployeeAsync(Guid employeeId)
    {
        _logger.LogInformation("Getting employee appraisals for employee: {EmployeeId}", employeeId);

        return new List<EmployeeAppraisalDto>
        {
            new() { AppraisalId = Guid.NewGuid(), EmployeeId = employeeId, CycleId = Guid.NewGuid(), Status = "Completed", OverallRating = 4.5m, SubmittedAt = DateTime.UtcNow.AddDays(-10) }
        };
    }

    public async Task<EmployeeAppraisalDto?> GetEmployeeAppraisalByIdAsync(Guid appraisalId)
    {
        _logger.LogInformation("Getting employee appraisal by ID: {AppraisalId}", appraisalId);
        return await _performanceRepository.GetEmployeeAppraisalByIdAsync(appraisalId);
    }

    public async Task<EmployeeAppraisalDto> SubmitAppraisalAsync(Guid appraisalId, SubmitAppraisalRequest request)
    {
        _logger.LogInformation("Submitting appraisal: {AppraisalId}", appraisalId);

        var appraisal = await GetEmployeeAppraisalByIdAsync(appraisalId);
        if (appraisal == null)
            throw new KeyNotFoundException($"Employee appraisal with ID {appraisalId} not found");

        appraisal.Status = "Submitted";
        appraisal.OverallRating = request.OverallRating;
        appraisal.SubmittedAt = DateTime.UtcNow;

        return appraisal;
    }

    public async Task<EmployeeAppraisalDto> ReviewAppraisalAsync(Guid appraisalId, ReviewAppraisalRequest request)
    {
        _logger.LogInformation("Reviewing appraisal: {AppraisalId}", appraisalId);

        var appraisal = await GetEmployeeAppraisalByIdAsync(appraisalId);
        if (appraisal == null)
            throw new KeyNotFoundException($"Employee appraisal with ID {appraisalId} not found");

        appraisal.Status = request.Status;
        appraisal.OverallRating = request.OverallRating ?? appraisal.OverallRating;

        return appraisal;
    }

    public async Task<PerformanceReportDto> GeneratePerformanceReportAsync(Guid organizationId, PerformanceReportFilterRequest filter)
    {
        _logger.LogInformation("Generating performance report for organization: {OrganizationId}", organizationId);

        return new PerformanceReportDto
        {
            StartDate = filter.StartDate ?? DateTime.Now.AddMonths(-3),
            EndDate = filter.EndDate ?? DateTime.Now,
            Appraisals = new List<EmployeeAppraisalDto>(),
            Summary = await GetPerformanceSummaryAsync(organizationId),
            DepartmentBreakdown = new List<DepartmentPerformanceDto>
            {
                new() { DepartmentName = "Engineering", AverageRating = 4.2m, EmployeeCount = 60, CompletedAppraisals = 55, PendingAppraisals = 5 },
                new() { DepartmentName = "Sales", AverageRating = 4.0m, EmployeeCount = 30, CompletedAppraisals = 28, PendingAppraisals = 2 }
            },
            TopPerformers = await GetTopPerformersAsync(organizationId, 10)
        };
    }

    public async Task<byte[]> ExportPerformanceReportAsync(Guid organizationId, PerformanceReportFilterRequest filter)
    {
        _logger.LogInformation("Exporting performance report for organization: {OrganizationId}", organizationId);

        var report = await GeneratePerformanceReportAsync(organizationId, filter);

        // Mock CSV export
        var csvData = "Employee,Department,OverallRating,Status,CompletionDate\n";
        csvData += "John Doe,Engineering,4.5,Completed,2024-01-15\n";
        csvData += "Jane Smith,Sales,4.2,Completed,2024-01-20\n";

        return System.Text.Encoding.UTF8.GetBytes(csvData);
    }


    public async Task<SelfAssessmentDto> CreateSelfAssessmentAsync(CreateSelfAssessmentRequest request)
    {
        return await _performanceRepository.CreateSelfAssessmentAsync(request);
    }

    public async Task<IEnumerable<SelfAssessmentDto>> GetSelfAssessmentsAsync(Guid employeeId, Guid cycleId)
    {
        return await _performanceRepository.GetSelfAssessmentsAsync(employeeId, cycleId);
    }

    public async Task<string> SubmitSelfAssessmentAsync(Guid employeeId, Guid cycleId)
    {
        await _performanceRepository.UpdateSelfAssessmentStatusAsync(employeeId, cycleId, "submitted");
        return "submitted";
    }
    public async Task<IEnumerable<SelfAssessmentDto>> GetTeamSelfAssessmentsAsync(Guid managerId, Guid cycleId)
    {
        return await _performanceRepository.GetTeamSelfAssessmentsAsync(managerId, cycleId);
    }

    public async Task<ManagerReviewDto> SubmitManagerReviewAsync(ManagerReviewRequest request)
    {
        return await _performanceRepository.SubmitManagerReviewAsync(request);
    }

    // Appraisal Cycle Management - Enhanced (New methods only)
    public async Task<AppraisalCycleDto?> GetActiveAppraisalCycleAsync(Guid organizationId)
    {
        _logger.LogInformation("Getting active appraisal cycle for organization: {OrganizationId}", organizationId);
        return await _performanceRepository.GetActiveAppraisalCycleAsync(organizationId);
    }

    // Appraisal Consolidation
    public async Task<EmployeeAppraisalDto> ConsolidateAppraisalAsync(ConsolidateAppraisalRequest request)
    {
        _logger.LogInformation("Consolidating appraisal for employee: {EmployeeId}, cycle: {CycleId}", request.EmployeeId, request.CycleId);
        return await _performanceRepository.ConsolidateAppraisalAsync(request);
    }

    // Reports and Analytics
    public async Task<EmployeePerformanceHistoryDto> GetEmployeePerformanceHistoryAsync(Guid employeeId)
    {
        _logger.LogInformation("Getting performance history for employee: {EmployeeId}", employeeId);
        return await _performanceRepository.GetEmployeePerformanceHistoryAsync(employeeId);
    }
}
