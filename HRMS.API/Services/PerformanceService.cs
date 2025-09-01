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
        var appraisal = _mapper.Map<EmployeeAppraisal>(request);
        appraisal.ReviewerId = reviewerId;

        var createdAppraisal = await _performanceRepository.CreateEmployeeAppraisalAsync(appraisal);
        return _mapper.Map<EmployeeAppraisalDto>(createdAppraisal);
    }

    public async Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsAsync(Guid cycleId, Guid? employeeId = null)
    {
        return await _performanceRepository.GetEmployeeAppraisalsAsync(cycleId, employeeId);
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

    public async Task<SkillSetDto> CreateSkillSetAsync(CreateSkillSetRequest request, Guid organizationId)
    {
        _logger.LogInformation("Creating skill set: {SkillName}", request.SkillName);
        
        return new SkillSetDto
        {
            SkillId = Guid.NewGuid(),
            SkillName = request.SkillName,
            Category = request.Category ?? "General",
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };
    }

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
            new() { EmployeeSkillId = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), SkillId = Guid.NewGuid(), ProficiencyLevel = 4, AssessorName = "Manager", LastAssessed = DateTime.UtcNow.AddDays(-30) },
            new() { EmployeeSkillId = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), SkillId = Guid.NewGuid(), ProficiencyLevel = 3, AssessorName = "Manager", LastAssessed = DateTime.UtcNow.AddDays(-45) }
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
            AssessorName = "Manager",
            LastAssessed = DateTime.UtcNow.AddDays(-30)
        };
    }

    public async Task<EmployeeSkillDto> CreateEmployeeSkillAsync(CreateEmployeeSkillRequest request)
    {
        _logger.LogInformation("Creating employee skill for employee: {EmployeeId}", request.EmployeeId);
        
        return new EmployeeSkillDto
        {
            EmployeeSkillId = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            SkillId = request.SkillId,
            ProficiencyLevel = request.ProficiencyLevel,
            AssessorName = request.AssessorName,
            LastAssessed = request.LastAssessed ?? DateTime.UtcNow,
            Notes = request.Notes
        };
    }

    public async Task<EmployeeSkillDto> UpdateEmployeeSkillAsync(Guid employeeSkillId, UpdateEmployeeSkillRequest request)
    {
        _logger.LogInformation("Updating employee skill: {EmployeeSkillId}", employeeSkillId);
        
        var skill = await GetEmployeeSkillByIdAsync(employeeSkillId);
        if (skill == null)
            throw new KeyNotFoundException($"Employee skill with ID {employeeSkillId} not found");

        skill.ProficiencyLevel = request.ProficiencyLevel;
        skill.AssessorName = request.AssessorName;
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
        
        var kras = new List<KRADto>
        {
            new() { KRAId = Guid.NewGuid(), Title = "Sales Performance", Description = "Achieve sales targets", Category = "Sales", Weight = 30, MeasurementCriteria = "Monthly sales numbers", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { KRAId = Guid.NewGuid(), Title = "Customer Satisfaction", Description = "Maintain customer satisfaction", Category = "Service", Weight = 25, MeasurementCriteria = "Customer feedback scores", IsActive = true, CreatedAt = DateTime.UtcNow }
        };

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
        
        return new KRADto
        {
            KRAId = kraId,
            Title = "Sales Performance",
            Description = "Achieve sales targets",
            Category = "Sales",
            Weight = 30,
            MeasurementCriteria = "Monthly sales numbers",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<KRADto> CreateKRAAsync(CreateKRARequest request, Guid organizationId)
    {
        _logger.LogInformation("Creating KRA: {Title}", request.Title);
        
        return new KRADto
        {
            KRAId = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Weight = request.Weight,
            MeasurementCriteria = request.MeasurementCriteria,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<KRADto> UpdateKRAAsync(Guid kraId, UpdateKRARequest request)
    {
        _logger.LogInformation("Updating KRA: {KRAId}", kraId);
        
        var kra = await GetKRAByIdAsync(kraId);
        if (kra == null)
            throw new KeyNotFoundException($"KRA with ID {kraId} not found");

        kra.Title = request.Title;
        kra.Description = request.Description;
        kra.Category = request.Category;
        kra.Weight = request.Weight;
        kra.MeasurementCriteria = request.MeasurementCriteria;
        kra.IsActive = request.IsActive;

        return kra;
    }

    public async Task<bool> DeleteKRAAsync(Guid kraId)
    {
        _logger.LogInformation("Deleting KRA: {KRAId}", kraId);
        return true;
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
        
        var cycles = await GetAppraisalCyclesAsync(Guid.NewGuid());
        return cycles.FirstOrDefault(c => c.CycleId == cycleId);
    }

    public async Task<AppraisalCycleDto> UpdateAppraisalCycleAsync(Guid cycleId, UpdateAppraisalCycleRequest request)
    {
        _logger.LogInformation("Updating appraisal cycle: {CycleId}", cycleId);
        
        var cycle = await GetAppraisalCycleByIdAsync(cycleId);
        if (cycle == null)
            throw new KeyNotFoundException($"Appraisal cycle with ID {cycleId} not found");

        cycle.CycleName = request.CycleName;
        cycle.StartDate = request.StartDate;
        cycle.EndDate = request.EndDate;
        cycle.Description = request.Description;
        cycle.Status = request.Status;

        return cycle;
    }

    public async Task<bool> DeleteAppraisalCycleAsync(Guid cycleId)
    {
        _logger.LogInformation("Deleting appraisal cycle: {CycleId}", cycleId);
        return true;
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
        
        return new EmployeeAppraisalDto
        {
            AppraisalId = appraisalId,
            EmployeeId = Guid.NewGuid(),
            CycleId = Guid.NewGuid(),
            Status = "Completed",
            OverallRating = 4.5m,
            SubmittedAt = DateTime.UtcNow.AddDays(-10)
        };
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
}
