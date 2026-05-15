using HRMS.API.Hubs;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace HRMS.API.Services;

public class PayslipBulkUploadService : IPayslipBulkUploadService
{
    private readonly IPayRollRepository _payRollRepository;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IEmailService _emailService;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly IHubContext<PayslipBulkHub> _hubContext;
    private readonly ILogger<PayslipBulkUploadService> _logger;

    public PayslipBulkUploadService(
        IPayRollRepository payRollRepository,
        ICloudinaryService cloudinaryService,
        IEmailService emailService,
        IWebHostEnvironment hostEnvironment,
        IHubContext<PayslipBulkHub> hubContext,
        ILogger<PayslipBulkUploadService> logger)
    {
        _payRollRepository = payRollRepository;
        _cloudinaryService = cloudinaryService;
        _emailService = emailService;
        _hostEnvironment = hostEnvironment;
        _hubContext = hubContext;
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
        string? emailMessage,
        CancellationToken cancellationToken = default)
    {
        var distinctEmployeeIds = employeeIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
        var payslips = await _payRollRepository.GetPendingPayslipsForBulkSendAsync(
            organizationId,
            payrollPeriodId,
            distinctEmployeeIds);

        var total = payslips.Count;
        var processed = 0;
        var failed = 0;

        await ReportProgressAsync(jobId, connectionId, total, processed, failed, isComplete: false);

        if (total == 0)
        {
            await ReportProgressAsync(jobId, connectionId, 0, 0, 0, isComplete: true, lastError: "No pending payslips found for the selected employees and period.");
            return;
        }

        var webRoot = _hostEnvironment.WebRootPath ?? Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot");

        foreach (var payslip in payslips)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? lastError = null;

            try
            {
                var physicalPath = ResolvePhysicalPath(webRoot, payslip.PayslipUrl);
                if (!File.Exists(physicalPath))
                {
                    throw new FileNotFoundException($"Payslip file not found at {physicalPath}");
                }

                var cloudinaryUrl = await _cloudinaryService.UploadPayslipPdfFromPathAsync(physicalPath, cancellationToken);

                var employeeName = $"{payslip.EmployeeFirstName} {payslip.EmployeeLastName}".Trim();
                var periodLabel = string.IsNullOrWhiteSpace(payslip.PeriodName) ? "Payroll" : payslip.PeriodName;
                var emailSent = await _emailService.SendPayslipEmailAsync(
                    payslip.EmployeeEmail,
                    employeeName,
                    periodLabel,
                    cloudinaryUrl,
                    organizationId,
                    emailSubject,
                    emailMessage);

                if (!emailSent)
                {
                    await _payRollRepository.UpdatePayslipAfterSendAsync(
                        payslip.PayslipId,
                        cloudinaryUrl,
                        "failed",
                        DateTime.UtcNow,
                        sentByUserId);

                    failed++;
                    lastError = $"Email delivery failed for employee {payslip.EmployeeId}.";
                }
                else
                {
                    await _payRollRepository.UpdatePayslipAfterSendAsync(
                        payslip.PayslipId,
                        cloudinaryUrl,
                        "sent",
                        DateTime.UtcNow,
                        sentByUserId);

                    TryDeleteLocalPayslip(physicalPath);
                    processed++;
                }
            }
            catch (Exception ex)
            {
                failed++;
                lastError = ex.Message;
                _logger.LogError(ex, "Bulk payslip send failed for payslip {PayslipId}, employee {EmployeeId}", payslip.PayslipId, payslip.EmployeeId);

                try
                {
                    await _payRollRepository.UpdatePayslipAfterSendAsync(
                        payslip.PayslipId,
                        null,
                        "failed",
                        DateTime.UtcNow,
                        sentByUserId);
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Failed to update payslip status after error for {PayslipId}", payslip.PayslipId);
                }
            }

            await ReportProgressAsync(
                jobId,
                connectionId,
                total,
                processed,
                failed,
                isComplete: false,
                currentEmployeeId: payslip.EmployeeId,
                lastError: lastError);
        }

        await ReportProgressAsync(jobId, connectionId, total, processed, failed, isComplete: true);
    }

    private static string ResolvePhysicalPath(string webRoot, string payslipUrl)
    {
        var relative = payslipUrl.Trim().TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(webRoot, relative);
    }

    private void TryDeleteLocalPayslip(string physicalPath)
    {
        try
        {
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
                _logger.LogInformation("Deleted local payslip file {Path}", physicalPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete local payslip file {Path}", physicalPath);
        }
    }

    private async Task ReportProgressAsync(
        string jobId,
        string? connectionId,
        int total,
        int processed,
        int failed,
        bool isComplete,
        Guid? currentEmployeeId = null,
        string? lastError = null)
    {
        var completedCount = processed + failed;
        var percentage = total <= 0
            ? (isComplete ? 100m : 0m)
            : Math.Round((decimal)completedCount / total * 100m, 2);

        var progress = new PayslipBulkProgressDto
        {
            JobId = jobId,
            Total = total,
            Processed = processed,
            Failed = failed,
            PercentageCompleted = percentage,
            IsComplete = isComplete,
            CurrentEmployeeId = currentEmployeeId,
            LastError = lastError
        };

        await _hubContext.Clients.Group(jobId).SendAsync(PayslipBulkHub.ProgressEventName, progress);

        if (!string.IsNullOrWhiteSpace(connectionId))
        {
            await _hubContext.Clients.Client(connectionId).SendAsync(PayslipBulkHub.ProgressEventName, progress);
        }
    }
}
