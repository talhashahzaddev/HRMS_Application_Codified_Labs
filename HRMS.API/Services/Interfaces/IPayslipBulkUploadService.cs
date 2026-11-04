namespace HRMS.API.Services.Interfaces;

public interface IPayslipBulkUploadService
{
    Task ProcessBulkUploadAndSendAsync(
        string jobId,
        Guid organizationId,
        Guid payrollPeriodId,
        IReadOnlyList<Guid> employeeIds,
        Guid sentByUserId,
        string? connectionId,
        string? emailSubject,
        string? emailMessage,
        CancellationToken cancellationToken = default);
}
