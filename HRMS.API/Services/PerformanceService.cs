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
}
