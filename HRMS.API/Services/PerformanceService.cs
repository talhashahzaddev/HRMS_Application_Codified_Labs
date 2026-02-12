using AutoMapper;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using static HRMS.API.Models.DTOs.NotificationDto;

namespace HRMS.API.Services;

public class PerformanceService : IPerformanceService
{
    private readonly IPerformanceRepository _performanceRepository;
    private readonly INotificationService _notificationService;
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PerformanceService> _logger;

    public PerformanceService(
        IPerformanceRepository performanceRepository,
        INotificationService notificationService,
        INotificationRepository notificationRepository,
        IEmployeeRepository employeeRepository,
        IMapper mapper,
        ILogger<PerformanceService> logger)
    {
        _performanceRepository = performanceRepository;
        _notificationService = notificationService;
        _notificationRepository = notificationRepository;
        _employeeRepository = employeeRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppraisalCycleDto> CreateAppraisalCycleAsync(string cycleName, DateTime startDate, DateTime endDate, Guid organizationId, string? description = null)
    {
        _logger.LogInformation("Creating appraisal cycle: {CycleName}", cycleName);
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        }
        if (string.IsNullOrWhiteSpace(cycleName))
        {
            throw new ArgumentException("Cycle name is required", nameof(cycleName));
        }
        if (endDate < startDate)
        {
            throw new ArgumentException("End date cannot be earlier than start date", nameof(endDate));
        }

        var cycle = await _performanceRepository.CreateAppraisalCycleAsync(organizationId, cycleName, startDate, endDate, description);
        if (cycle == null)
        {
            throw new InvalidOperationException("Failed to create appraisal cycle");
        }
        return _mapper.Map<AppraisalCycleDto>(cycle);
    }

    public async Task<List<AppraisalCycleDto>> GetAppraisalCyclesAsync(Guid organizationId)
    {
        _logger.LogInformation("Getting appraisal cycles for organization: {OrganizationId}", organizationId);
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        }
        return await _performanceRepository.GetAppraisalCyclesAsync(organizationId);
    }

    public async Task<EmployeeAppraisalDto> CreateEmployeeAppraisalAsync(
     CreateAppraisalRequest request,
     Guid reviewerId,
     Guid organizationId)
    {
        try
        {
            var appraisal = new EmployeeAppraisal
            {
                AppraisalId = Guid.NewGuid(),
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
                Status = "Submitted",
                GoalsAchieved = new Dictionary<string, object>()
            };

            var createdAppraisal =
                await _performanceRepository.CreateEmployeeAppraisalAsync(appraisal);

            if (createdAppraisal == null)
                throw new Exception("Failed to create appraisal");

            return new EmployeeAppraisalDto
            {
                AppraisalId = createdAppraisal.AppraisalId,
                CycleId = createdAppraisal.CycleId,
                EmployeeId = createdAppraisal.EmployeeId,
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
        }
        catch (InvalidOperationException)
        {
            // 👈 BUSINESS WARNING → pass upward
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee appraisal");
            throw;
        }
    }


    public async Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsAsync(Guid cycleId, Guid employeeId)
    {
        return await _performanceRepository.GetEmployeeAppraisalsAsync(cycleId, employeeId);
    }

    public async Task<SkillSetDto?> CreateSkillSetAsync(CreateSkillSetRequest request, Guid organizationId)
    {
        _logger.LogInformation("Creating skill set: {SkillName}", request?.SkillName);
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        }
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Request cannot be null");
        }
        if (string.IsNullOrWhiteSpace(request.SkillName))
        {
            throw new ArgumentException("Skill name is required", nameof(request.SkillName));
        }
        try
        {
            return await _performanceRepository.CreateSkillSetAsync(request, organizationId);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
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
        _logger.LogInformation("Getting performance summary for organization: {OrganizationId}, cycle: {CycleId}", organizationId, cycleId);
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        return await _performanceRepository.GetPerformanceSummaryAsync(organizationId, cycleId);
    }

    public async Task<List<TopPerformerDto>> GetTopPerformersAsync(Guid organizationId, int count = 10)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (count <= 0)
            throw new ArgumentException("Count must be greater than zero", nameof(count));
        return await _performanceRepository.GetTopPerformersAsync(organizationId, count);
    }

    // New missing methods implementation
    public async Task<PerformanceSummaryDto> GetPerformanceDashboardSummaryAsync(Guid organizationId)
    {
        _logger.LogInformation("Getting performance dashboard summary for organization: {OrganizationId}", organizationId);
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        // Get active cycle if available
        var activeCycle = await _performanceRepository.GetActiveAppraisalCycleAsync(organizationId);
        return await GetPerformanceSummaryAsync(organizationId, activeCycle?.CycleId);
    }

    public async Task<PagedResult<SkillSetDto>> GetSkillSetsPagedAsync(Guid organizationId, SkillSetFilterRequest filter, int page, int limit)
    {
        _logger.LogInformation("Getting paged skill sets for organization: {OrganizationId}", organizationId);
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (page <= 0)
            throw new ArgumentException("Page must be greater than zero", nameof(page));
        if (limit <= 0)
            throw new ArgumentException("Page size must be greater than zero", nameof(limit));
        var skillSets = await GetSkillSetsAsync(organizationId);

        // Apply filters
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
        return await _performanceRepository.GetSkillSetByIdAsync(skillSetId);
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

        return await _performanceRepository.UpdateSkillSetAsync(skillSetId, request);
    }

    public async Task<bool> DeleteSkillSetAsync(Guid skillSetId)
    {
        _logger.LogInformation("Deleting skill set: {SkillSetId}", skillSetId);
        return await _performanceRepository.DeleteSkillSetAsync(skillSetId);
    }

    public async Task<PagedResult<EmployeeSkillDto>> GetEmployeeSkillsPagedAsync(Guid organizationId, EmployeeSkillFilterRequest filter, int page, int limit)
    {
        _logger.LogInformation("Getting paged employee skills for organization: {OrganizationId}", organizationId);
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (page <= 0)
            throw new ArgumentException("Page must be greater than zero", nameof(page));
        if (limit <= 0)
            throw new ArgumentException("Page size must be greater than zero", nameof(limit));

        // Get all employee skills for the organization with filters
        var allSkills = await _performanceRepository.GetEmployeeSkillsByOrganizationAsync(organizationId, filter);

        // Apply additional filters that can't be done in SQL easily
        if (filter.ProficiencyLevel.HasValue)
            allSkills = allSkills.Where(s => s.ProficiencyLevel == filter.ProficiencyLevel.Value).ToList();
        if (!string.IsNullOrEmpty(filter.Search))
            allSkills = allSkills.Where(s => 
                s.SkillName.Contains(filter.Search, StringComparison.OrdinalIgnoreCase) ||
                s.EmployeeName.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)).ToList();

        var totalItems = allSkills.Count;
        var pagedItems = allSkills.Skip((page - 1) * limit).Take(limit).ToList();

        return new PagedResult<EmployeeSkillDto>
        {
            Data = pagedItems,
            TotalCount = totalItems,
            Page = page,
            PageSize = limit
        };
    }

    public async Task<PagedResult<EmployeeSkillDto>> GetEmployeeSkillsByManagerPagedAsync(Guid managerEmployeeId, EmployeeSkillFilterRequest filter, int page, int limit)
    {
        _logger.LogInformation("Getting paged employee skills for manager: {ManagerEmployeeId}", managerEmployeeId);
        if (managerEmployeeId == Guid.Empty)
            throw new ArgumentException("Manager employee ID cannot be empty", nameof(managerEmployeeId));
        if (page <= 0)
            throw new ArgumentException("Page must be greater than zero", nameof(page));
        if (limit <= 0)
            throw new ArgumentException("Page size must be greater than zero", nameof(limit));

        // Get employee skills for the manager's team with filters
        var allSkills = await _performanceRepository.GetEmployeeSkillsByManagerAsync(managerEmployeeId, filter);

        var totalItems = allSkills.Count;
        var pagedItems = allSkills.Skip((page - 1) * limit).Take(limit).ToList();

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
        return await _performanceRepository.GetEmployeeSkillByIdAsync(employeeSkillId);
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

            // AssessedBy should already be set by the controller from the current user context
            // Only validate it's not empty GUID here
            //if (!request.AssessedBy.HasValue || request.AssessedBy == Guid.Empty)
            //{
            //    _logger.LogWarning("AssessedBy is not set. This should be handled by the controller.");
            //    throw new ArgumentException("AssessedBy must be provided. The assessor ID is required.");
            //}

            // ✅ 3. Check for duplicate skill
            var existingSkills = await _performanceRepository.GetEmployeeSkillsAsync(request.EmployeeId);
            if (existingSkills.Any(s => s.SkillId == request.SkillId))
            {
                throw new InvalidOperationException("This skill has already been added to the employee. Cannot add duplicate skills.");
            }

            // ✅ 4. Call repository
            var result = await _performanceRepository.CreateEmployeeSkillAsync(request);

            if (result == null)
                throw new Exception("Failed to create employee skill record.");

            _logger.LogInformation("✅ Successfully created skill for employee {EmployeeId}", request.EmployeeId);

            return result;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "⚠ Duplicate skill detected for employee");
            throw;
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
        if (employeeSkillId == Guid.Empty)
            throw new ArgumentException("Employee skill ID cannot be empty", nameof(employeeSkillId));
        if (request == null)
            throw new ArgumentNullException(nameof(request), "Request cannot be null");
        if (request.ProficiencyLevel < 1 || request.ProficiencyLevel > 5)
            throw new ArgumentException("Proficiency level must be between 1 and 5.", nameof(request.ProficiencyLevel));
        if (!request.LastAssessed.HasValue)
            request.LastAssessed = DateTime.UtcNow;

        var skill = await GetEmployeeSkillByIdAsync(employeeSkillId);
        if (skill == null)
            throw new KeyNotFoundException($"Employee skill with ID {employeeSkillId} not found");

        return await _performanceRepository.UpdateEmployeeSkillAsync(employeeSkillId, request);
    }

    public async Task<bool> DeleteEmployeeSkillAsync(Guid employeeSkillId)
    {
        _logger.LogInformation("Deleting employee skill: {EmployeeSkillId}", employeeSkillId);
        if (employeeSkillId == Guid.Empty)
            throw new ArgumentException("Employee skill ID cannot be empty", nameof(employeeSkillId));
        var existing = await GetEmployeeSkillByIdAsync(employeeSkillId);
        if (existing == null)
            throw new KeyNotFoundException($"Employee skill with ID {employeeSkillId} not found");
        return await _performanceRepository.DeleteEmployeeSkillAsync(employeeSkillId);
    }

    public async Task<PagedResult<KRADto>> GetKRAsAsync(Guid organizationId, int page, int limit, string? search)
    {
        _logger.LogInformation("Getting KRAs for organization: {OrganizationId}", organizationId);
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (page <= 0)
            throw new ArgumentException("Page must be greater than zero", nameof(page));
        if (limit <= 0)
            throw new ArgumentException("Page size must be greater than zero", nameof(limit));

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
        if (kraId == Guid.Empty)
            throw new ArgumentException("KRA ID cannot be empty", nameof(kraId));

        var result = await _performanceRepository.GetKraByIdAsync(kraId);
        return result;

    }

    public async Task<KRADto> CreateKRAAsync(Guid organizationId, CreateKRARequest request)
    {
        _logger.LogInformation("Creating KRA: {Title}", request.Title);
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (request == null)
            throw new ArgumentNullException(nameof(request), "Request cannot be null");
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("KRA title is required", nameof(request.Title));

        var result = await _performanceRepository.CreateKRAAsync(request, organizationId);


        return result;
    }

    public async Task<KRADto> UpdateKRAAsync(Guid kraId, UpdateKRARequest request)
    {
        _logger.LogInformation("Checking Weather KRa with this id exisits or not");
        if (kraId == Guid.Empty)
            throw new ArgumentException("KRA ID cannot be empty", nameof(kraId));
        if (request == null)
            throw new ArgumentNullException(nameof(request), "Request cannot be null");

        var kra = await _performanceRepository.GetKraByIdAsync(kraId);
        if (kra == null)
        {
            throw new KeyNotFoundException($"KRA with ID {kraId} not found");
        }

        var result = await _performanceRepository.UpdatekraAsync(kraId, request);
        return result;

    }

    public async Task<bool> UpdateKRAStatusAsync(Guid kraId, bool isActive)
    {
        if (kraId == Guid.Empty)
            throw new ArgumentException("KRA ID cannot be empty", nameof(kraId));
        var kra = await _performanceRepository.GetKraByIdAsync(kraId);
        if (kra == null)
        {
            throw new KeyNotFoundException($"KRA with ID {kraId} not found");
        }

        return await _performanceRepository.UpdateKRAStatusAsync(kraId, isActive);
    }





    public async Task<bool> DeleteKRAAsync(Guid kraId)
    {
        _logger.LogInformation("Deleting KRA: {KRAId}", kraId);
        if (kraId == Guid.Empty)
            throw new ArgumentException("KRA ID cannot be empty", nameof(kraId));
        var existing = await _performanceRepository.GetKraByIdAsync(kraId);
        if (existing == null)
            throw new KeyNotFoundException($"KRA with ID {kraId} not found");
        return await _performanceRepository.DeleteKRAAsync(kraId);
    }

    public async Task<PagedResult<AppraisalCycleDto>> GetAppraisalCyclesPagedAsync(Guid organizationId, int page, int limit, string? search)
    {
        _logger.LogInformation("Getting paged appraisal cycles for organization: {OrganizationId}", organizationId);
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (page <= 0)
            throw new ArgumentException("Page must be greater than zero", nameof(page));
        if (limit <= 0)
            throw new ArgumentException("Page size must be greater than zero", nameof(limit));

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
        if (cycleId == Guid.Empty)
            throw new ArgumentException("Cycle ID cannot be empty", nameof(cycleId));
        return await _performanceRepository.GetAppraisalCycleByIdAsync(cycleId);
    }

    public async Task<AppraisalCycleDto> UpdateAppraisalCycleAsync(Guid cycleId, UpdateAppraisalCycleRequest request)
    {
        _logger.LogInformation("Updating appraisal cycle: {CycleId}", cycleId);
        if (cycleId == Guid.Empty)
            throw new ArgumentException("Cycle ID cannot be empty", nameof(cycleId));
        if (request == null)
            throw new ArgumentNullException(nameof(request), "Request cannot be null");
        return await _performanceRepository.UpdateAppraisalCycleAsync(cycleId, request);
    }

    public async Task<bool> DeleteAppraisalCycleAsync(Guid cycleId)
    {
        _logger.LogInformation("Deleting appraisal cycle: {CycleId}", cycleId);
        if (cycleId == Guid.Empty)
            throw new ArgumentException("Cycle ID cannot be empty", nameof(cycleId));
        var existing = await _performanceRepository.GetAppraisalCycleByIdAsync(cycleId);
        if (existing == null)
            throw new KeyNotFoundException($"Appraisal cycle with ID {cycleId} not found");
        return await _performanceRepository.DeleteAppraisalCycleAsync(cycleId);
    }

    public async Task<PagedResult<EmployeeAppraisalDto>> GetEmployeeAppraisalsPagedAsync(Guid organizationId, AppraisalFilterRequest filter, int page, int limit)
    {
        _logger.LogInformation("Getting paged employee appraisals for organization: {OrganizationId}", organizationId);
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (page <= 0)
            throw new ArgumentException("Page must be greater than zero", nameof(page));
        if (limit <= 0)
            throw new ArgumentException("Page size must be greater than zero", nameof(limit));

        var appraisals = await _performanceRepository.GetEmployeeAppraisalsPagedAsync(organizationId, filter, page, limit);
        var totalCount = await _performanceRepository.GetEmployeeAppraisalsCountAsync(organizationId, filter);

        return new PagedResult<EmployeeAppraisalDto>
        {
            Data = appraisals,
            TotalCount = totalCount,
            Page = page,
            PageSize = limit
        };
    }

    public async Task<List<EmployeeAppraisalDto>> GetEmployeeAppraisalsByEmployeeAsync(Guid employeeId)
    {
        _logger.LogInformation("Getting employee appraisals for employee: {EmployeeId}", employeeId);
        if (employeeId == Guid.Empty)
            throw new ArgumentException("Employee ID cannot be empty", nameof(employeeId));
        return await _performanceRepository.GetEmployeeAppraisalsByEmployeeAsync(employeeId);
    }

    public async Task<EmployeeAppraisalDto?> GetEmployeeAppraisalByIdAsync(Guid appraisalId)
    {
        _logger.LogInformation("Getting employee appraisal by ID: {AppraisalId}", appraisalId);
        if (appraisalId == Guid.Empty)
            throw new ArgumentException("Appraisal ID cannot be empty", nameof(appraisalId));
        return await _performanceRepository.GetEmployeeAppraisalByIdAsync(appraisalId);
    }

    public async Task<bool> DeleteEmployeeAppraisalAsync(Guid appraisalId)
    {
        _logger.LogInformation("Deleting employee appraisal: {AppraisalId}", appraisalId);
        if (appraisalId == Guid.Empty)
            throw new ArgumentException("Appraisal ID cannot be empty", nameof(appraisalId));
        var existing = await _performanceRepository.GetEmployeeAppraisalByIdAsync(appraisalId);
        if (existing == null)
            throw new KeyNotFoundException($"Employee appraisal with ID {appraisalId} not found");
        return await _performanceRepository.DeleteEmployeeAppraisalAsync(appraisalId);
    }

    public async Task<EmployeeAppraisalDto> SubmitAppraisalAsync(Guid appraisalId, SubmitAppraisalRequest request)
    {
        _logger.LogInformation("Submitting appraisal: {AppraisalId}", appraisalId);
        if (appraisalId == Guid.Empty)
            throw new ArgumentException("Appraisal ID cannot be empty", nameof(appraisalId));
        if (request == null)
            throw new ArgumentNullException(nameof(request), "Request cannot be null");

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
        if (appraisalId == Guid.Empty)
            throw new ArgumentException("Appraisal ID cannot be empty", nameof(appraisalId));
        if (request == null)
            throw new ArgumentNullException(nameof(request), "Request cannot be null");

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
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (filter.StartDate.HasValue && filter.EndDate.HasValue && filter.EndDate.Value < filter.StartDate.Value)
            throw new ArgumentException("End date cannot be earlier than start date", nameof(filter.EndDate));

        // Get appraisals within the date range
        var cycleFilter = new AppraisalFilterRequest
        {
            AppraisalCycleId = filter.AppraisalCycleId
        };

        // Get all appraisals for the organization
        var allAppraisals = await _performanceRepository.GetEmployeeAppraisalsPagedAsync(
            organizationId, 
            cycleFilter, 
            1, 
            10000); // Get all appraisals

        // Filter by date range if provided
        var filteredAppraisals = allAppraisals.Where(a => 
            (!filter.StartDate.HasValue || (a.SubmittedAt ?? a.ReviewedAt ?? DateTime.MinValue) >= filter.StartDate.Value) &&
            (!filter.EndDate.HasValue || (a.SubmittedAt ?? a.ReviewedAt ?? DateTime.MinValue) <= filter.EndDate.Value)
        ).ToList();

        // Filter by department if provided
        if (!string.IsNullOrEmpty(filter.Department))
        {
            // Note: This would require joining with employee departments
            // For now, we'll keep all appraisals and filter can be done at report level
        }

        // Filter by rating if provided
        if (filter.Rating.HasValue)
        {
            filteredAppraisals = filteredAppraisals.Where(a => 
                a.OverallRating.HasValue && 
                Math.Floor(a.OverallRating.Value) == Math.Floor(filter.Rating.Value)
            ).ToList();
        }

        // Get department breakdown
        var departmentBreakdown = await _performanceRepository.GetDepartmentPerformanceBreakdownAsync(
            organizationId, 
            filter.StartDate, 
            filter.EndDate);

        // Get summary
        var summary = await GetPerformanceSummaryAsync(organizationId, filter.AppraisalCycleId);

        return new PerformanceReportDto
        {
            StartDate = filter.StartDate ?? DateTime.Now.AddMonths(-3),
            EndDate = filter.EndDate ?? DateTime.Now,
            Appraisals = filteredAppraisals,
            Summary = summary,
            DepartmentBreakdown = departmentBreakdown,
            TopPerformers = await GetTopPerformersAsync(organizationId, 10)
        };
    }

    public async Task<byte[]> ExportPerformanceReportAsync(Guid organizationId, PerformanceReportFilterRequest filter)
    {
        _logger.LogInformation("Exporting performance report for organization: {OrganizationId}", organizationId);

        var report = await GeneratePerformanceReportAsync(organizationId, filter);

        // Generate CSV from actual data
        var csvData = "Employee,Department,OverallRating,Status,CompletionDate,Cycle\n";
        
        foreach (var appraisal in report.Appraisals)
        {
            // Get department for employee
            var departmentName = await _performanceRepository.GetEmployeeDepartmentAsync(appraisal.EmployeeId) ?? "N/A";
            
            var completionDate = appraisal.ReviewedAt?.ToString("yyyy-MM-dd") 
                ?? appraisal.SubmittedAt?.ToString("yyyy-MM-dd") 
                ?? "N/A";
            
            csvData += $"{appraisal.EmployeeName},{departmentName},{appraisal.OverallRating?.ToString("F2") ?? "N/A"},{appraisal.Status},{completionDate},{appraisal.CycleName}\n";
        }

        return System.Text.Encoding.UTF8.GetBytes(csvData);
    }


    public async Task<SelfAssessmentDto> CreateSelfAssessmentAsync(CreateSelfAssessmentRequest request, Guid organizationId)
    {
        var result = await _performanceRepository.CreateSelfAssessmentAsync(request);
        
        // Create notification for the reporting manager
        try
        {
            // Get employee details for the sender (current employee who created self-assessment)
            var employeeDetails = await _employeeRepository.GetEmployeedetailsAsync(request.EmployeeId);
            
            if (employeeDetails != null)
            {
                // Get reporting manager ID from employment details
                var reportingManagerEmployeeId = await _notificationRepository.GetReciveridAsync(request.EmployeeId);
                
                if (reportingManagerEmployeeId != Guid.Empty)
                {
                    // Get reporting manager employee details
                    var managerDetails = await _employeeRepository.GetEmployeedetailsAsync(reportingManagerEmployeeId);
                    
                    if (managerDetails != null)
                    {
                        var employeeName = $"{employeeDetails.FirstName} {employeeDetails.LastName}".Trim();
                        
                        var notification = new NotificationCreateDto
                        {
                            Receiveruserid = new List<Guid> { reportingManagerEmployeeId }, // Reporting manager
                            Senderuserid = request.EmployeeId, // Employee who created self-assessment
                            Title = "Self-Assessment Created",
                            Notificationtype = "Performance",
                            Message = $"{employeeName} has submitted a self-assessment",
                            Redirecturl = "/performance/appraisals",
                            moduletype = "Performance",
                            RequestId = result.SelfAssessmentId,
                            Createdby = request.EmployeeId,
                            organizationId = organizationId
                        };

                        await _notificationService.CreateNotificationAsync(organizationId, notification);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the self-assessment creation
            _logger.LogError(ex, "Error creating notification for self-assessment {SelfAssessmentId}", result.SelfAssessmentId);
            throw;
        }
        
        return result;
    }

    public async Task<IEnumerable<SelfAssessmentDto>> GetSelfAssessmentsAsync(Guid employeeId, Guid cycleId)
    {
        return await _performanceRepository.GetSelfAssessmentsAsync(employeeId, cycleId);
    }

    public async Task<IEnumerable<SelfAssessmentDto>> GetMySelfAssessmentsAsync(Guid employeeId, Guid? cycleId = null, Guid? kraId = null, string? search = null)
    {
        return await _performanceRepository.GetMySelfAssessmentsAsync(employeeId, cycleId, kraId, search);
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

    public async Task<IEnumerable<SelfAssessmentDto>> GetAllEmployeeSelfAssessmentsAsync(Guid organizationId, Guid? cycleId = null, Guid? employeeId = null, Guid? kraId = null, string? search = null)
    {
        return await _performanceRepository.GetAllEmployeeSelfAssessmentsAsync(organizationId, cycleId, employeeId, kraId, search);
    }

    public async Task<IEnumerable<SelfAssessmentDto>> GetMyTeamSelfAssessmentsAsync(Guid managerEmployeeId, Guid? cycleId = null, Guid? employeeId = null, Guid? kraId = null, string? search = null)
    {
        return await _performanceRepository.GetMyTeamSelfAssessmentsAsync(managerEmployeeId, cycleId, employeeId, kraId, search);
    }

    public async Task<ManagerReviewDto> SubmitManagerReviewAsync(ManagerReviewRequest request)
    {
        return await _performanceRepository.SubmitManagerReviewAsync(request);
    }

    public async Task<IEnumerable<EmployeeAppraisalForEmployeeDto>> GetEmployeeAppraisalsForEmployeeAsync(Guid employeeId, Guid? cycleId = null, Guid? kraId = null, string? search = null, string? status = null)
    {
        return await _performanceRepository.GetEmployeeAppraisalsForEmployeeAsync(employeeId, cycleId, kraId, search, status);
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
    public async Task<Guid> GetEmployeeFromTable(Guid Userid)
    {
        _logger.LogInformation("Getting Employee Id against User id: {EmployeeId}", Userid);
        return await _performanceRepository.GetEmployeeFromTable(Userid);
    }

    public async Task<TeamPerformanceOverviewDto> GetTeamPerformanceOverviewAsync(Guid managerEmployeeId, Guid organizationId)
    {
        _logger.LogInformation("Getting team performance overview for manager: {ManagerEmployeeId}, organization: {OrganizationId}", managerEmployeeId, organizationId);
        return await _performanceRepository.GetTeamPerformanceOverviewAsync(managerEmployeeId, organizationId);
    }
}
