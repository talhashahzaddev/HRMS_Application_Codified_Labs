using AutoMapper;
using HRMS.API.Controllers;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Mailjet.Client.Resources;
using NPOI.SS.Formula.Functions;
using static HRMS.API.Models.DTOs.NotificationDto;

namespace HRMS.API.Services;

public class PerformanceService : IPerformanceService
{
    private readonly IPerformanceRepository _performanceRepository;
    private readonly INotificationService _notificationService;
    private readonly INotificationRepository _notificationRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PerformanceService> _logger;

    public PerformanceService(
        IPerformanceRepository performanceRepository,
        INotificationService notificationService,
        INotificationRepository notificationRepository,
        IAttendanceRepository attendanceRepository,
        IEmployeeRepository employeeRepository,
        IMapper mapper,
        ILogger<PerformanceService> logger)
    {
        _performanceRepository = performanceRepository;
        _notificationService = notificationService;
        _notificationRepository = notificationRepository;
        _attendanceRepository = attendanceRepository;
        _employeeRepository = employeeRepository;
        _mapper = mapper;
        _logger = logger;
    }


    public async Task<AppraisalCycleDto> CreateAppraisalCycleAsync(CreateAppraisalCycleRequest request, Guid organizationId, Guid currentEmployeeId)
    {

        // 🔹 Basic validations
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));

        if (string.IsNullOrWhiteSpace(request.CycleName))
            throw new ArgumentException("Cycle name is required", nameof(request.CycleName));

        if (request.EndDate < request.StartDate)
            throw new ArgumentException("End date cannot be earlier than start date", nameof(request.EndDate));

        // 🔹 Default status (if not provided)
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? "upcoming"
            : request.Status.ToLower();

        // 🔹 Create entity via repository
        var cycle = await _performanceRepository.CreateAppraisalCycleAsync(request, organizationId, currentEmployeeId);

        if (cycle == null)
            throw new InvalidOperationException("Failed to create appraisal cycle");

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
            throw new ArgumentException("Organization ID cannot be empty.", nameof(organizationId));
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.SkillName))
            throw new ArgumentException("Skill name is required.", nameof(request.SkillName));

        _logger.LogInformation("Creating skill set: {SkillName} for org: {OrgId}", request.SkillName, organizationId);

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
        if (employeeId == Guid.Empty)
            throw new ArgumentException("Employee ID cannot be empty.", nameof(employeeId));

        return await _performanceRepository.GetEmployeeSkillsAsync(employeeId);
    }

    public async Task<PerformanceSummaryDto> GetPerformanceSummaryAsync(Guid organizationId, Guid? cycleId = null)
    {
        _logger.LogInformation("Getting performance summary for organization: {OrganizationId}, cycle: {CycleId}", organizationId, cycleId);
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        return await _performanceRepository.GetPerformanceSummaryAsync(organizationId, cycleId);
    }
    //My performance summary
    public async Task<MYPerformanceSummary> GetMyPerformanceSummaryAsync(Guid organizationId, Guid currentEmpid)
    {
        //getting current cycleid of employee so by cycle id we can get current rating for its performance
        var CurrentCycleId = await _performanceRepository.GetCurrentCycleid(organizationId);
        return await _performanceRepository.GetMyPerformanceSummaryAsync(organizationId, currentEmpid, CurrentCycleId);
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
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty.", nameof(organizationId));
        if (page <= 0)
            throw new ArgumentException("Page must be greater than zero.", nameof(page));
        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than zero.", nameof(limit));

        _logger.LogInformation("Getting paged skill sets for org: {OrgId}, page: {Page}", organizationId, page);

        var (items, total) = await _performanceRepository.GetSkillSetsPagedAsync(organizationId, filter, page, limit);

        return new PagedResult<SkillSetDto>
        {
            Data = items,
            TotalCount = total,
            Page = page,
            PageSize = limit
        };
    }
    public async Task<SkillWithEmployeesDto?> GetSkillWithEmployeesAsync(Guid skillId)
    {
        if (skillId == Guid.Empty)
            throw new ArgumentException("Skill ID cannot be empty.", nameof(skillId));

        _logger.LogInformation("Getting skill with employees: {SkillId}", skillId);

        return await _performanceRepository.GetSkillWithEmployeesAsync(skillId);
    }
    public async Task<SkillSetDto> ToggleSkillStatusAsync(Guid skillId)
    {
        if (skillId == Guid.Empty)
            throw new ArgumentException("Skill ID cannot be empty.", nameof(skillId));

        _logger.LogInformation("Toggling skill status: {SkillId}", skillId);

        return await _performanceRepository.ToggleSkillStatusAsync(skillId);
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

    public async Task<bool> DeleteSkillSetAsync(Guid skillId)
    {
        if (skillId == Guid.Empty)
            throw new ArgumentException("Skill ID cannot be empty.", nameof(skillId));

        _logger.LogInformation("Deleting skill set: {SkillId}", skillId);

        return await _performanceRepository.DeleteSkillSetAsync(skillId);
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
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (request.EmployeeId == Guid.Empty || request.SkillId == Guid.Empty)
            throw new ArgumentException("EmployeeId and SkillId are required.");

        if (request.ProficiencyLevel < 1 || request.ProficiencyLevel > 10)
            throw new ArgumentException("Proficiency level must be between 1 and 10.");

        if (!request.LastAssessed.HasValue)
            request.LastAssessed = DateTime.UtcNow;

        // Check for duplicate
        var existingSkills = await _performanceRepository.GetEmployeeSkillsAsync(request.EmployeeId);
        if (existingSkills.Any(s => s.SkillId == request.SkillId))
            throw new InvalidOperationException("This skill has already been added to the employee.");

        _logger.LogInformation("Creating employee skill for employee: {EmployeeId}, skill: {SkillId}", request.EmployeeId, request.SkillId);

        var result = await _performanceRepository.CreateEmployeeSkillAsync(request);

        if (result == null)
            throw new Exception("Failed to create employee skill record.");

        return result;
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
        if (employeeSkillId == Guid.Empty)
            throw new ArgumentException("Employee skill ID cannot be empty.", nameof(employeeSkillId));
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.ProficiencyLevel < 1 || request.ProficiencyLevel > 10)
            throw new ArgumentException("Proficiency level must be between 1 and 10.");

        if (!request.LastAssessed.HasValue)
            request.LastAssessed = DateTime.UtcNow;

        var existing = await _performanceRepository.GetEmployeeSkillByIdAsync(employeeSkillId);
        if (existing == null)
            throw new KeyNotFoundException($"Employee skill with ID {employeeSkillId} not found.");

        _logger.LogInformation("Updating employee skill: {EmployeeSkillId}", employeeSkillId);

        return await _performanceRepository.UpdateEmployeeSkillAsync(employeeSkillId, request);
    }

    public async Task<bool> DeleteEmployeeSkillAsync(Guid employeeSkillId)
    {
        if (employeeSkillId == Guid.Empty)
            throw new ArgumentException("Employee skill ID cannot be empty.", nameof(employeeSkillId));

        _logger.LogInformation("Deleting employee skill: {EmployeeSkillId}", employeeSkillId);

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

    public async Task<KRADto> CreateKRAAsync(Guid organizationId, Guid CurrentEmpId, CreateKRARequest request)
    {
        _logger.LogInformation("Creating KRA: {Title}", request.Title);
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        if (request == null)
            throw new ArgumentNullException(nameof(request), "Request cannot be null");
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("KRA title is required", nameof(request.Title));

        var result = await _performanceRepository.CreateKRAAsync(request, organizationId, CurrentEmpId);


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

    public async Task<GoalDto> CreateGoalAsync(Guid organizationId, Guid createdBy, CreateGoalRequest request)
    {
        _logger.LogInformation("Creating Goal for Organization: {OrgId}, CreatedBy: {CreatedBy}", organizationId, createdBy);

        // Validation
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Goal title cannot be empty", nameof(request.Title));

        if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate < request.StartDate)
            throw new ArgumentException("EndDate cannot be earlier than StartDate");

        // Create the goal
        var goal = await _performanceRepository.CreateGoalAsync(request, organizationId, createdBy);

        if (request.KRAId != null)
        {
            // Fetch all goals for this KRA
            var allGoals = await _performanceRepository.GetCompletedGoalsByKRAAsync(request.KRAId);

            // Total goals count
            int totalGoals = allGoals.Count;

            // Count completed goals
            int completedCount = allGoals.Count(g => g.Progress?.ToLower() == "completed");

            // Calculate average progress rate (completed / total * 100)
            decimal averageRate = totalGoals == 0 ? 0 : (decimal)completedCount / totalGoals * 100;

            // Convert decimal average to string (e.g., "75.00")
            var krarateString = Math.Round(averageRate, 2).ToString();

            // Update KRA's progress and number of goals
            await _performanceRepository.UpdateKRAProgressAsync(request.KRAId, krarateString, totalGoals);
        }

        return goal;
    }

    public async Task<List<GoalDto>> GetAllGoalsAsync(Guid organizationId, Guid currentEmployeeId)
    {
      
     _logger.LogInformation("Fetching all Goals for Organization: {OrgId}", organizationId);
       return await _performanceRepository.GetAllGoalsAsync(organizationId);
      
    }

    public async Task<List<GoalDto>> GetEmployeeAssignedGoalsAsync(Guid organizationId, Guid currentEmployeeId)
    {
       return await _performanceRepository.GetEmployeeAssignedGoalAsync(organizationId, currentEmployeeId);
    }
    
    public async Task<GoalDto?> GetGoalByIdAsync(Guid organizationId, Guid goalId)
    {
        _logger.LogInformation("Fetching Goal {GoalId} for Organization: {OrgId}", goalId, organizationId);
        return await _performanceRepository.GetGoalByIdAsync(goalId);
    }

    public async Task<GoalDto?> UpdateGoalAsync(Guid organizationId, UpdateGoalRequest request)
    {

        if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate < request.StartDate)
            throw new ArgumentException("EndDate cannot be earlier than StartDate");

        var updated = await _performanceRepository.UpdateGoalAsync(organizationId, request);

        if (updated != null)
        {
            var allGoals = await _performanceRepository.GetCompletedGoalsByKRAAsync(request.KRAId);
            int totalGoals = allGoals.Count;
            int completedCount = allGoals.Count(g => g.Progress?.ToLower() == "completed");
            decimal averageRate = totalGoals == 0 ? 0 : (decimal)completedCount / totalGoals * 100;
            await _performanceRepository.UpdateKRAProgressAsync(request.KRAId, Math.Round(averageRate, 2).ToString(), totalGoals);
        }

        return updated;
    }

    public async Task DeleteGoalAsync(Guid organizationId, Guid goalId)
    {
        _logger.LogInformation("Deleting Goal {GoalId} for Organization: {OrgId}", goalId, organizationId);
        await _performanceRepository.DeleteGoalAsync(organizationId, goalId);
    }
    public async Task<bool> AssignGoalAsync(Guid organizationId, Guid goalId, Guid assignedTo, Guid assignedBy)
    {
        _logger.LogInformation(
            "Assigning Goal {GoalId} in Org: {OrgId} to {AssignedTo} by {AssignedBy}",
            goalId, organizationId, assignedTo, assignedBy);
        var result = await _performanceRepository
            .UpdateGoalAssignmentAsync(organizationId, goalId, assignedTo, assignedBy);

        return result;
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
        var appraisalCycle = await _performanceRepository.GetAppraisalCycleByIdAsync(cycleId);
        if (string.Equals(appraisalCycle.Status, "start", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot delete appraisal cycle because it has already started.");
        }

        return await _performanceRepository.UpdateAppraisalCycleAsync(cycleId, request);
    }

    public async Task<bool> DeleteAppraisalCycleAsync(Guid cycleId)
    {
        _logger.LogInformation("Deleting appraisal cycle: {CycleId}", cycleId);
        if (cycleId == Guid.Empty)
            throw new ArgumentException("Cycle ID cannot be empty", nameof(cycleId));
        var appraisalCycle = await _performanceRepository.GetAppraisalCycleByIdAsync(cycleId);
        if (string.Equals(appraisalCycle.Status, "start", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot delete appraisal cycle because it has already started.");
        }
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

    public async Task<bool> UpdateGoalStatusAsync(Guid goalId, string status)
    {
        var goal = await _performanceRepository.GetGoalByIdAsync(goalId);

        if (goal == null)
            return false;

        // 1. Update goal status first
        var result = await _performanceRepository.UpdateGoalStatusAsync(goalId, status);

        if (!result)
            return false;

        // 2. Get KRA ID from goal (IMPORTANT)
        var kraId = goal.KRAId; // adjust property name if different

        // 3. Fetch all goals under same KRA
        var allGoals = await _performanceRepository.GetCompletedGoalsByKRAAsync(kraId.Value);

        int totalGoals = allGoals.Count;

        int completedCount = allGoals.Count(g =>
            g.Progress?.ToLower() == "completed");

        decimal averageRate = totalGoals == 0
            ? 0
            : (decimal)completedCount / totalGoals * 100;

        var kraRateString = Math.Round(averageRate, 2).ToString();

        // 4. Update KRA progress
        await _performanceRepository.UpdateKRAProgressAsync(
            kraId.Value,
            kraRateString,
            totalGoals
        );

        return true;
    }

    public async Task<SelfAssessmentDto> CreateSelfAssessmentAsync(CreateSelfAssessmentRequest request, Guid organizationId, Guid CurrentEmployeeId)
    {
        var result = await _performanceRepository.CreateSelfAssessmentAsync(request, organizationId, CurrentEmployeeId);

        // Create notification for the reporting manager
        try
        {
            // Get employee details for the sender (current employee who created self-assessment)
            var employeeDetails = await _employeeRepository.GetEmployeedetailsAsync(CurrentEmployeeId);
            var notification = new NotificationCreateDto
            {
                Receiveruserid = new List<Guid> { employeeDetails.ReportingManagerId },
                Senderuserid = CurrentEmployeeId,
                Title = "Prfromanec Self Assessment",
                Message = $"{employeeDetails.FirstName} {employeeDetails.LastName} has completed and submitted their self-assessment for goals.",
                moduletype = "Performance",
                RequestId=result.SelfAssessmentId,
                Redirecturl= $"/performance/appraisals",
                Notificationtype = "Message",
                Createdby = CurrentEmployeeId,
            };
            await _notificationRepository.CreateNotificationAsync(organizationId, notification);

        }
        catch (Exception ex)
        {
            // Log the error but don't fail the self-assessment creation
            _logger.LogError(ex, "Error creating notification for self-assessment {SelfAssessmentId}", result.SelfAssessmentId);
            throw;
        }

        return result;
    }
    //my function
    public async Task<IEnumerable<SelfAssessmentDto>> GetSelfAssessmentsByEmployeeAsync(Guid employeeId, Guid organizationId)
    {
        return await _performanceRepository.GetSelfAssessmentsByEmployeeAsync(employeeId, organizationId);
    }
    //mm

    public async Task<IEnumerable<SelfAssessmentDto>> GetSelfAssessmentsForManagerAsync(Guid managerId, Guid organizationId)
    {
        // 1) Get all employees reporting to this manager
        var employees = await _employeeRepository.GetEmployeesReportingToManagerAsync(managerId);

        if (employees == null || !employees.Any())
            return Enumerable.Empty<SelfAssessmentDto>();

        var employeeIds = employees.Select(e => e.EmployeeId).ToList();

        // 2) Get self-assessments for these employees
        return await _performanceRepository.GetSelfAssessmentsByEmployeesAsync(employeeIds, organizationId);
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

    public async Task<ManagerReviewDto> SubmitManagerReviewAsync(ManagerReviewRequest request, Guid organizationId, Guid currentEmpId)
    {
        var result=await _performanceRepository.SubmitManagerReviewAsync(request, organizationId, currentEmpId);
        // Get employee details for the sender (current employee who created self-assessment)
        var employeeDetails = await _employeeRepository.GetEmployeedetailsAsync(currentEmpId);
        var notification = new NotificationCreateDto
        {
            Receiveruserid = new List<Guid> { request.EmployeeId },
            Senderuserid = currentEmpId,
            Title = "Prfromanec",
            Message = $"{employeeDetails.FirstName} {employeeDetails.LastName}  give you Manager Apraisals on your Self Assessment",
            moduletype = "Performance",
            RequestId = result.ManagerReviewId,
            Redirecturl = $"/performance/appraisals",
            Notificationtype = "Message",
            Createdby = currentEmpId,
        };
        await _notificationRepository.CreateNotificationAsync(organizationId, notification);
        return result;
    }

    //Manager review and employee reviews
    public async Task<List<ManagerReviewDto>> GetManagerReviewsAsync(Guid organizationId, Guid managerId)
    {
        return await _performanceRepository.GetManagerReviewsAsync(organizationId, managerId);
    }
    //Getting all record from managers table
    public async Task<List<ManagerReviewDto>> GetAllManagerReviewsAsync(Guid organizationId, Guid managerId, SearchManagersDto search)
    {
        return await _performanceRepository.GetAllManagerReviewsAsync(organizationId, search);
    }

    public async Task<List<ManagerReviewDto>> GetEmployeeReviewsAsync(Guid organizationId, Guid employeeId)
    {
        return await _performanceRepository.GetEmployeeReviewsAsync(organizationId, employeeId);
    }
    //Hr review

    public async Task<HrReviewDto> SubmitHrReviewAsync(
   HrReviewRequest request, Guid organizationId, Guid currentEmployeeId)
    {
        //get hr appraisal for cy
        var cycle = await _performanceRepository.GetAppraisalCycleByIdAsync(request.CycleId);
        if (cycle.AppraisalEnabled == false)
        {
            throw new InvalidOperationException("Appraisal is not allowed for this appraisal cycle.");
        }

        var alreadyAppraisal = await _performanceRepository.GetEmployeeAppraisal(organizationId, request.CycleId, request.EmployeeId);
        if (alreadyAppraisal != null)
        {
            throw new InvalidOperationException("Cannot submit HR appraisal because employee appraisal against this cycle already exists.");
        }
        var result=await _performanceRepository.SubmitHrReviewAsync(request, organizationId, currentEmployeeId);

        // Get employee details for the sender (current employee who created self-assessment)
        var employeeDetails = await _employeeRepository.GetEmployeedetailsAsync(currentEmployeeId);
        var notification = new NotificationCreateDto
        {
            Receiveruserid = new List<Guid> { request.EmployeeId},
            Senderuserid = currentEmployeeId,
            Title = "Prfromanec",
            Message = $"{employeeDetails.FirstName} {employeeDetails.LastName}  give you Hr Review on Appraisal Cycle",
            moduletype = "Performance",
            RequestId = result.HrReviewId,
            Redirecturl = $"/performance/appraisals",
            Notificationtype = "Message",
            Createdby = currentEmployeeId,
        };
        await _notificationRepository.CreateNotificationAsync(organizationId, notification);

        return result;
    }

    public async Task<List<HrReviewDto>> GetHrReviewsAsync(Guid organizationId, Guid CurrentEmpId)
    {
        return await _performanceRepository.GetHrReviewsAsync(organizationId, CurrentEmpId);
    }

    public async Task<List<HrReviewDto>> GetHrReviewsByEmployeeAsync(Guid organizationId, Guid employeeId)
    {
        return await _performanceRepository.GetHrReviewsByEmployeeAsync(organizationId, employeeId);
    }
    //Get all appraisals 

    public async Task<List<HrReviewDto>> GetAllAppraisaAsync(Guid organizationId, Guid employeeId, SearchAppraisalDto search)
    {
        return await _performanceRepository.GetAllAppraisalAsync(organizationId, employeeId, search);
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
    //public async Task<EmployeeAppraisalDto> ConsolidateAppraisalAsync(ConsolidateAppraisalRequest request)
    //{
    //    _logger.LogInformation("Consolidating appraisal for employee: {EmployeeId}, cycle: {CycleId}", request.EmployeeId, request.CycleId);
    //    return await _performanceRepository.GetActiveAppraisalCycleAsync(request);

    //}

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
    public async Task<PagedResult<EmployeeSkillSummaryDto>> GetEmployeeSkillsSummaryPagedAsync(Guid organizationId, EmployeeSkillFilterRequest filter, int page, int limit)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization ID cannot be empty.", nameof(organizationId));
        if (page <= 0)
            throw new ArgumentException("Page must be greater than zero.", nameof(page));
        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than zero.", nameof(limit));

        _logger.LogInformation("Getting employee skills summary for org: {OrgId}, page: {Page}", organizationId, page);

        var (items, total) = await _performanceRepository.GetEmployeeSkillsSummaryPagedAsync(organizationId, filter, page, limit);

        return new PagedResult<EmployeeSkillSummaryDto>
        {
            Data = items,
            TotalCount = total,
            Page = page,
            PageSize = limit
        };
    }

    public async Task<EmployeeSkillFullDetailDto> GetEmployeeSkillFullDetailAsync(Guid employeeId)
    {
        if (employeeId == Guid.Empty)
            throw new ArgumentException("Employee ID cannot be empty.", nameof(employeeId));

        _logger.LogInformation("Getting full skill detail for employee: {EmployeeId}", employeeId);

        return await _performanceRepository.GetEmployeeSkillFullDetailAsync(employeeId);
    }
}
