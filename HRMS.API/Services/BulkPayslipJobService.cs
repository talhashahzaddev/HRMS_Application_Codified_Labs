using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class BulkPayslipJobService : IBulkPayslipJobService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BulkPayslipJobService> _logger;

    public BulkPayslipJobService(IServiceScopeFactory scopeFactory, ILogger<BulkPayslipJobService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ProcessBulkUploadAndSendAsync(
        string jobId,
        Guid organizationId,
        Guid payrollPeriodId,
        IReadOnlyList<Guid> employeeIds,
        Guid sentByUserId,
        string? connectionId,
        string? emailSubject,
        string? emailMessage)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IPayslipBulkUploadService>();
            await processor.ProcessBulkUploadAndSendAsync(
                jobId,
                organizationId,
                payrollPeriodId,
                employeeIds,
                sentByUserId,
                connectionId,
                emailSubject,
                emailMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hangfire bulk payslip job {JobId} failed", jobId);
            throw;
        }
    }
}
