using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Dapper;
using HRMS.API.Configuration;

namespace HRMS.API.Services
{
    public class LeaveBalanceResetHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LeaveBalanceResetHostedService> _logger;

        private const string ResetJobKey = "LeaveBalanceReset";

        public LeaveBalanceResetHostedService(
            IServiceProvider serviceProvider,
            ILogger<LeaveBalanceResetHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nowUtc = DateTime.UtcNow;
                var nextRun = new DateTime(nowUtc.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var delay = nextRun - nowUtc;

                //_logger.LogInformation("Leave reset job sleeping until {NextRun}", nextRun);
                //await Task.Delay(delay, stoppingToken);
                _logger.LogInformation("Leave reset job sleeping until {NextRun}", nextRun);

                while (delay > TimeSpan.Zero && !stoppingToken.IsCancellationRequested)
                {
                    // Delay in chunks of max 24 days
                    var chunk = delay > TimeSpan.FromDays(24) ? TimeSpan.FromDays(24) : delay;
                    await Task.Delay(chunk, stoppingToken);

                    delay -= chunk;
                }


                try
                {
                    await AddNewYearLeaveEntitlementsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Yearly leave balance reset failed");
                }
            }
        }
        private async Task AddNewYearLeaveEntitlementsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDatabaseConnectionFactory>();

            using var connection = factory.CreateConnection();
            connection.Open();

            var year = DateTime.UtcNow.Year;

            //1️⃣ Idempotency check
                var alreadyRun = await connection.ExecuteScalarAsync<bool>(
                    @"SELECT EXISTS (
                        SELECT 1 FROM admin.system_jobs 
                        WHERE jobkey = @JobKey AND year = @Year
                      )",
                    new { JobKey = ResetJobKey, Year = year });

            if (alreadyRun)
            {
                _logger.LogInformation("Leave balances already reset for {Year}", year);
                return;
            }

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1️⃣ Get all organizations
                var organizations = await connection.QueryAsync<Guid>(
                    "SELECT organizationid FROM auth.organizations;",
                    transaction: transaction
                );

                foreach (var orgId in organizations)
                {
                    // 2️⃣ Get leave types for organization
                    var leaveTypes = await connection.QueryAsync<LeaveType>(
                        @"SELECT leavetypeid,
                         maxdaysperyear,
                         carryforwardallowed,
                         maxcarryforwarddays
                  FROM attendance.leavetypes
                  WHERE organizationid = @OrgId;",
                        new { OrgId = orgId }, transaction
                    );

                    foreach (var lt in leaveTypes)
                    {
                        // 3️⃣ Get employees of organization
                        var employees = await connection.QueryAsync<Guid>(
                            @"SELECT employeeid
                      FROM employee.employees
                      WHERE organizationid = @OrgId;",
                            new { OrgId = orgId }, transaction
                        );

                        foreach (var empId in employees)
                        {
                            // 4️⃣ Skip if current year entitlement already exists
                            var exists = await connection.ExecuteScalarAsync<bool>(
                                @"SELECT EXISTS (
                            SELECT 1
                            FROM attendance.leaveentitlements
                            WHERE employeeid = @EmpId
                              AND leavetypeid = @LeaveTypeId
                              AND year = @Year
                          );",
                                new
                                {
                                    EmpId = empId,
                                    LeaveTypeId = lt.LeaveTypeId,
                                    Year = year
                                }, transaction
                            );

                            if (exists)
                                continue;

                            // 5️⃣ Calculate carry forward days
                            int carryForwardDays = 0;

                            if (lt.CarryForwardAllowed)
                            {
                                var previousRemaining = await connection.ExecuteScalarAsync<int>(
                                    @"SELECT COALESCE(remainingdays, 0)
                              FROM attendance.leaveentitlements
                              WHERE employeeid = @EmpId
                                AND leavetypeid = @LeaveTypeId
                                AND year = @PrevYear;",
                                    new
                                    {
                                        EmpId = empId,
                                        LeaveTypeId = lt.LeaveTypeId,
                                        PrevYear = year - 1
                                    }, transaction
                                );

                                carryForwardDays = Math.Min(
                                    previousRemaining,
                                    lt.MaxCarryForwardDays
                                );
                            }

                            // 6️⃣ Final totals
                            int totalDays = lt.MaxDaysPerYear + carryForwardDays;

                            // 7️⃣ Insert NEW entitlement (no update)
                            await connection.ExecuteAsync(
                                @"INSERT INTO attendance.leaveentitlements
                          (employeeid,
                           leavetypeid,
                           year,
                           totaldays,
                           useddays,
                           remainingdays,
                           carryforwarddays,
                           createdat)
                          VALUES
                          (@EmpId,
                           @LeaveTypeId,
                           @Year,
                           @TotalDays,
                           0,
                           @TotalDays,
                           @CarryForwardDays,
                           NOW());",
                                new
                                {
                                    EmpId = empId,
                                    LeaveTypeId = lt.LeaveTypeId,
                                    Year = year,
                                    TotalDays = totalDays,
                                    CarryForwardDays = carryForwardDays
                                }, transaction
                            );
                        }
                    }
                }

                await connection.ExecuteAsync(@"
                            INSERT INTO admin.system_jobs (jobkey, year, runat)
                            VALUES (@JobKey, @Year, NOW());
                        ", new { JobKey = ResetJobKey, Year = year }, transaction);

                transaction.Commit();
                _logger.LogInformation("New year leave entitlements created successfully for {Year}", year);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        // DTOs for Dapper mapping
        private class LeaveType
        {
            public Guid LeaveTypeId { get; set; }
            public int MaxDaysPerYear { get; set; }
            public bool CarryForwardAllowed { get; set; }
            public int MaxCarryForwardDays { get; set; }
        }

    }
}
