namespace HRMS.API.Services.Interfaces;

public interface IBulkPayslipJobService
{
    Task ProcessBulkUploadAndSendAsync(
        string jobId,
        Guid organizationId,
        Guid payrollPeriodId,
        IReadOnlyList<Guid> employeeIds,
        Guid sentByUserId,
        string? connectionId,
        string? emailSubject,
        string? emailMessage);
}
