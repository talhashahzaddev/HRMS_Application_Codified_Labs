using AutoMapper;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class PayrollService : IPayrollService
{
    private readonly IPayrollRepository _payrollRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PayrollService> _logger;

    public PayrollService(
        IPayrollRepository payrollRepository,
        IMapper mapper,
        ILogger<PayrollService> logger)
    {
        _payrollRepository = payrollRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PayrollPeriodDto> CreatePayrollPeriodAsync(CreatePayrollPeriodRequest request, Guid organizationId)
    {
        var payrollPeriod = await _payrollRepository.CreatePayrollPeriodAsync(
            organizationId, request.PeriodName, request.StartDate, request.EndDate, request.PayDate);

        return _mapper.Map<PayrollPeriodDto>(payrollPeriod);
    }

    public async Task<List<PayrollPeriodDto>> GetPayrollPeriodsAsync(Guid organizationId)
    {
        return await _payrollRepository.GetPayrollPeriodsAsync(organizationId);
    }

    public async Task<PayrollPeriodDto?> GetPayrollPeriodByIdAsync(Guid periodId)
    {
        var period = await _payrollRepository.GetPayrollPeriodByIdAsync(periodId);
        return period != null ? _mapper.Map<PayrollPeriodDto>(period) : null;
    }

    public async Task<int> CalculatePayrollAsync(Guid periodId)
    {
        return await _payrollRepository.CalculatePayrollAsync(periodId);
    }

    public async Task<PagedResult<PayrollEntryDto>> GetPayrollEntriesAsync(Guid periodId, int page = 1, int pageSize = 50)
    {
        return await _payrollRepository.GetPayrollEntriesAsync(periodId, page, pageSize);
    }

    public async Task<PayrollSummaryDto> GetPayrollSummaryAsync(Guid periodId)
    {
        return await _payrollRepository.GetPayrollSummaryAsync(periodId);
    }

    public async Task<bool> ProcessPayrollPaymentAsync(Guid entryId, string paymentMethod, string paymentReference)
    {
        return await _payrollRepository.ProcessPayrollPaymentAsync(entryId, paymentMethod, paymentReference);
    }
}
