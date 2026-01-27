using System.Data;
using Dapper;
using HRMS.API.Configuration;
using HRMS.API.Models.DTOs;
using HRMS.API.Repositories.Interfaces;

namespace HRMS.API.Repositories;

public class DashboardRepository : BaseRepository, IDashboardRepository
{
    private readonly ILogger<DashboardRepository> _logger;

    public DashboardRepository(IDatabaseConnectionFactory connectionFactory, ILogger<DashboardRepository> logger) 
        : base(connectionFactory)
    {
        _logger = logger;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(Guid organizationId)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            var result = await connection.QuerySingleOrDefaultAsync<dynamic>("SELECT * FROM admin.fn_get_dashboard_stats(@organizationId)",
                new { organizationId });

            if (result == null)
            {
                return new DashboardStatsDto();
            }

            return new DashboardStatsDto
            {
                TotalEmployees = result.totalemployees ?? 0,
                ActiveEmployees = result.activeemployees ?? 0,
                PendingLeaveRequests = result.pendingleaverequests ?? 0,
                PresentToday = result.presenttoday ?? 0,
                TotalDepartments = result.totaldepartments ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(Guid organizationId, int count = 10)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            // Get recent audit logs for the organization
            var activities = await connection.QueryAsync<RecentActivityDto>(
                @"SELECT 
                    al.logId as Id,
                    al.tableName as Type,
                    CASE 
                        WHEN al.action = 'INSERT' THEN 'New ' || al.tableName || ' Added'
                        WHEN al.action = 'UPDATE' THEN al.tableName || ' Updated'
                        WHEN al.action = 'DELETE' THEN al.tableName || ' Deleted'
                        ELSE al.action || ' ' || al.tableName
                    END as Title,
                    COALESCE(al.newValues->>'description', al.newValues->>'name', 'Record modified') as Description,
                    CASE 
                        WHEN al.tableName = 'employees' THEN 'person_add'
                        WHEN al.tableName = 'leave_requests' THEN 'event_available'
                        WHEN al.tableName = 'payroll_periods' THEN 'account_balance_wallet'
                        ELSE 'update'
                    END as Icon,
                    al.createdAt as Timestamp,
                    COALESCE(u.firstName || ' ' || u.lastName, 'System') as UserName
                FROM admin.auditLogs al
                LEFT JOIN auth.users u ON al.userId = u.userId
                LEFT JOIN employee.employees e ON al.recordId = e.employeeId
                WHERE al.organizationId = @organizationId
                ORDER BY al.createdAt DESC
                LIMIT @count",
                new { organizationId, count });

            return activities.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activities for organization {OrganizationId}", organizationId);
            return new List<RecentActivityDto>();
        }
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid organizationId)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var stats = await GetDashboardStatsAsync(organizationId);
            
            // Calculate employee growth
            var employeeGrowth = await connection.QuerySingleOrDefaultAsync<dynamic>(
                @"SELECT 
                    ((COUNT(*) FILTER (WHERE hireDate >= CURRENT_DATE - INTERVAL '1 year')::float / 
                     NULLIF(COUNT(*) FILTER (WHERE hireDate < CURRENT_DATE - INTERVAL '1 year'), 0)) - 1) * 100 as growthRate
                    FROM employee.employees 
                    WHERE organizationId = @organizationId",
                new { organizationId });

            return new DashboardSummaryDto
            {
                TotalEmployees = stats.TotalEmployees,
                PresentToday = stats.PresentToday,
                OnLeaveToday = await GetOnLeaveTodayCountAsync(organizationId),
                PendingApprovals = stats.PendingLeaveRequests,
                EmployeeGrowth = employeeGrowth?.growthRate ?? 0m
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard summary for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<AttendanceStatsDto> GetAttendanceStatsAsync(Guid organizationId, string period = "month")
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var daysInPeriod = period.ToLower() switch
            {
                "week" => 7,
                "month" => 30,
                "quarter" => 90,
                "year" => 365,
                _ => 30
            };

            var startDate = DateTime.UtcNow.AddDays(-daysInPeriod);
            
            var attendanceData = await connection.QueryAsync<dynamic>(
                @"SELECT 
                    a.workDate as Date,
                    COUNT(*) FILTER (WHERE a.status = 'present') as PresentCount,
                    COUNT(*) FILTER (WHERE a.status = 'absent') as AbsentCount
                FROM attendance.attendance a
                INNER JOIN employee.employees e ON a.employeeId = e.employeeId
                WHERE e.organizationId = @organizationId
                    AND a.workDate >= @startDate
                GROUP BY a.workDate
                ORDER BY a.workDate",
                new { organizationId, startDate });

            var dates = new List<string>();
            var presentCounts = new List<int>();
            var absentCounts = new List<int>();

            foreach (var item in attendanceData)
            {
                dates.Add(((DateTime)item.date).ToString("yyyy-MM-dd"));
                presentCounts.Add((int)item.presentcount);
                absentCounts.Add((int)item.absentcount);
            }

            return new AttendanceStatsDto
            {
                Dates = dates,
                PresentCounts = presentCounts,
                AbsentCounts = absentCounts,
                AveragePresence = presentCounts.Count > 0 ? (decimal)presentCounts.Average() : 0m
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance stats for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<LeaveStatsDto> GetLeaveStatsAsync(Guid organizationId, string period = "month")
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var daysInPeriod = period.ToLower() switch
            {
                "week" => 7,
                "month" => 30,
                "quarter" => 90,
                "year" => 365,
                _ => 30
            };

            var startDate = DateTime.UtcNow.AddDays(-daysInPeriod);
            
            var leaveData = await connection.QueryAsync<dynamic>(
                @"SELECT 
                    lt.typeName as LeaveType,
                    COUNT(*) as Count,
                    lr.status as Status
                FROM attendance.leaveRequests lr
                INNER JOIN attendance.leaveTypes lt ON lr.leaveTypeId = lt.leaveTypeId
                INNER JOIN employee.employees e ON lr.employeeId = e.employeeId
                WHERE e.organizationId = @organizationId
                    AND lr.submittedAt >= @startDate
                GROUP BY lt.typeName, lr.status",
                new { organizationId, startDate });

            var leaveTypes = new Dictionary<string, int>();
            var totalRequests = 0;
            var approvedRequests = 0;
            var pendingRequests = 0;
            var rejectedRequests = 0;

            foreach (var item in leaveData)
            {
                var leaveType = (string)item.leavetype;
                var count = (int)item.count;
                var status = (string)item.status;

                if (!leaveTypes.ContainsKey(leaveType))
                    leaveTypes[leaveType] = 0;
                
                leaveTypes[leaveType] += count;
                totalRequests += count;

                switch (status.ToLower())
                {
                    case "approved":
                        approvedRequests += count;
                        break;
                    case "pending":
                        pendingRequests += count;
                        break;
                    case "rejected":
                        rejectedRequests += count;
                        break;
                }
            }

            return new LeaveStatsDto
            {
                TotalRequests = totalRequests,
                ApprovedRequests = approvedRequests,
                PendingRequests = pendingRequests,
                RejectedRequests = rejectedRequests,
                LeaveTypes = leaveTypes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leave stats for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<PayrollStatsDto> GetPayrollStatsAsync(Guid organizationId, string period = "month")
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var monthsInPeriod = period.ToLower() switch
            {
                "month" => 1,
                "quarter" => 3,
                "year" => 12,
                _ => 12
            };

            var payrollData = await connection.QueryAsync<dynamic>(
                @"SELECT 
                    DATE_TRUNC('month', pp.payDate) as Month,
                    SUM(pe.netSalary) as TotalAmount,
                    AVG(pe.netSalary) as AverageSalary,
                    COUNT(DISTINCT pe.employeeId) as EmployeeCount
                FROM payroll.payrollPeriods pp
                INNER JOIN payroll.payrollEntries pe ON pp.periodId = pe.periodId
                INNER JOIN employee.employees e ON pe.employeeId = e.employeeId
                WHERE e.organizationId = @organizationId
                    AND pp.payDate >= CURRENT_DATE - (@months * INTERVAL '1 month')
                GROUP BY DATE_TRUNC('month', pp.payDate)
                ORDER BY Month",
                new { organizationId, months = monthsInPeriod });

            var payrollCosts = new List<PayrollCostDto>();
            decimal totalPayroll = 0;

            foreach (var item in payrollData)
            {
                var month = ((DateTime)item.month).ToString("yyyy-MM");
                var amount = (decimal)item.totalamount;
                payrollCosts.Add(new PayrollCostDto
                {
                    Month = month,
                    Amount = amount
                });
                totalPayroll += amount;
            }

            var averageSalary = payrollData.Any()
    ? payrollData.Average(x => (decimal)x.averagesalary)
    : 0m;

            return new PayrollStatsDto
            {
                TotalPayroll = totalPayroll,
                AverageSalary = averageSalary,
                PayrollCosts = payrollCosts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll stats for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<PerformanceStatsDto> GetPerformanceStatsAsync(Guid organizationId)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var performanceData = await connection.QueryAsync<dynamic>(
                @"SELECT 
                    e.employeeId,
                    CONCAT(e.firstName, ' ', e.lastName) as EmployeeName,
                    d.departmentName as Department,
                    COALESCE(AVG(ea.overallRating), 0) as AverageRating
                FROM performance.employeeAppraisals ea
                INNER JOIN employee.employees e ON ea.employeeId = e.employeeId
                INNER JOIN employee.employmentDetails ed ON e.employeeId = ed.employeeId AND ed.isCurrent = true
                INNER JOIN employee.departments d ON ed.departmentId = d.departmentId
                WHERE e.organizationId = @organizationId
                    AND ea.status = 'Submitted'
                GROUP BY e.employeeId, e.firstName, e.lastName, d.departmentName
                ORDER BY AverageRating DESC
                LIMIT 10",
                new { organizationId });

            var completedReviews = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) 
                FROM performance.employeeAppraisals ea
                INNER JOIN employee.employees e ON ea.employeeId = e.employeeId
                WHERE e.organizationId = @organizationId AND ea.status = 'Submitted'",
                new { organizationId });

            var pendingReviews = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(*) 
                FROM performance.employeeAppraisals ea
                INNER JOIN employee.employees e ON ea.employeeId = e.employeeId
                WHERE e.organizationId = @organizationId AND ea.status = 'pending'",
                new { organizationId });

            var averageRating = await connection.ExecuteScalarAsync<decimal?>(
                @"SELECT AVG(overallRating) 
                FROM performance.employeeAppraisals ea
                INNER JOIN employee.employees e ON ea.employeeId = e.employeeId
                WHERE e.organizationId = @organizationId AND ea.status = 'Submitted'",
                new { organizationId }) ?? 0m;

            var topPerformers = performanceData.Select(p => new TopPerformerDto
            {
                EmployeeName = (string)p.employeename,
                Rating = (decimal)p.averagerating,
                Department = (string)p.department
            }).ToList();

            return new PerformanceStatsDto
            {
                AverageRating = averageRating,
                CompletedReviews = completedReviews,
                PendingReviews = pendingReviews,
                TopPerformers = topPerformers
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance stats for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<EmployeeGrowthDto> GetEmployeeGrowthAsync(Guid organizationId, string period = "12m")
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var monthsInPeriod = period.ToLower() switch
            {
                "6m" => 6,
                "12m" => 12,
                "24m" => 24,
                _ => 12
            };

            var growthData = await connection.QueryAsync<dynamic>(
                @"SELECT 
                    DATE_TRUNC('month', hireDate) as Month,
                    COUNT(*) as NewEmployees
                FROM employee.employees
                WHERE organizationId = @organizationId
                    AND hireDate >= CURRENT_DATE - (@months * INTERVAL '1 month')
                GROUP BY DATE_TRUNC('month', hireDate)
                ORDER BY Month",
                new { organizationId, months = monthsInPeriod });

            var months = new List<string>();
            var counts = new List<int>();
            var cumulative = new List<int>();

            int cumulativeCount = 0;
            foreach (var item in growthData)
            {
                months.Add(((DateTime)item.month).ToString("MMM yyyy"));
                counts.Add((int)item.newemployees);
                cumulativeCount += (int)item.newemployees;
                cumulative.Add(cumulativeCount);
            }

            return new EmployeeGrowthDto
            {
                Months = months,
                Counts = counts,
                GrowthRate = 0, // Calculate actual growth rate if needed
                CumulativeEmployees = cumulative,
                NewEmployees = counts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee growth for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<List<DepartmentStatsDto>> GetDepartmentStatsAsync(Guid organizationId)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var departmentData = await connection.QueryAsync<dynamic>(
                @"SELECT 
                    d.departmentName as DepartmentName,
                    COUNT(e.employeeId) as EmployeeCount,
                    AVG(ed.basicSalary) as AverageSalary,
                    COUNT(e.employeeId) FILTER (WHERE e.status = 'active') as ActiveEmployees
                FROM employee.departments d
                LEFT JOIN employee.employmentDetails ed ON d.departmentId = ed.departmentId AND ed.isCurrent = true
                LEFT JOIN employee.employees e ON ed.employeeId = e.employeeId
                WHERE d.organizationId = @organizationId AND d.isActive = true
                GROUP BY d.departmentId, d.departmentName
                ORDER BY EmployeeCount DESC",
                new { organizationId });

            return departmentData.Select(d => new DepartmentStatsDto
            {
                DepartmentName = (string)d.departmentname,
                EmployeeCount = (int)d.employeecount,
                AverageSalary = (decimal?)d.averagesalary,
                ActiveEmployees = (int)d.activeemployees
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting department stats for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    //public async Task<List<UpcomingEventDto>> GetUpcomingEventsAsync(Guid organizationId, int count = 5)
    //{
    //    try
    //    {
    //        using var connection = _connectionFactory.CreateConnection();

    //        // Get upcoming birthdays and work anniversaries
    //        var events = await connection.QueryAsync<UpcomingEventDto>(
    //            @"SELECT 
    //                'birthday' as Type,
    //                CONCAT(e.firstName, ' ', e.lastName, '''s Birthday') as Title,
    //                CONCAT('Birthday celebration for ', e.firstName, ' ', e.lastName) as Description,
    //                TO_CHAR(e.dateOfBirth + INTERVAL '1 year' * (EXTRACT(YEAR FROM CURRENT_DATE) - EXTRACT(YEAR FROM e.dateOfBirth)), 'YYYY-MM-DD') as Date,
    //                '09:00' as Time
    //            FROM employee.employees e
    //            WHERE e.organizationId = @organizationId
    //                AND e.status = 'active'
    //                AND e.dateOfBirth IS NOT NULL
    //                AND e.dateOfBirth + INTERVAL '1 year' * (EXTRACT(YEAR FROM CURRENT_DATE) - EXTRACT(YEAR FROM e.dateOfBirth)) >= CURRENT_DATE
    //                AND e.dateOfBirth + INTERVAL '1 year' * (EXTRACT(YEAR FROM CURRENT_DATE) - EXTRACT(YEAR FROM e.dateOfBirth)) <= CURRENT_DATE + INTERVAL '30 days'

    //            UNION ALL

    //            SELECT 
    //                'anniversary' as Type,
    //                CONCAT(e.firstName, ' ', e.lastName, '''s Work Anniversary') as Title,
    //                CONCAT('Work anniversary celebration for ', e.firstName, ' ', e.lastName) as Description,
    //                TO_CHAR(e.hireDate + INTERVAL '1 year' * (EXTRACT(YEAR FROM CURRENT_DATE) - EXTRACT(YEAR FROM e.hireDate)), 'YYYY-MM-DD') as Date,
    //                '15:00' as Time
    //            FROM employee.employees e
    //            WHERE e.organizationId = @organizationId
    //                AND e.status = 'active'
    //                AND e.hireDate IS NOT NULL
    //                AND e.hireDate + INTERVAL '1 year' * (EXTRACT(YEAR FROM CURRENT_DATE) - EXTRACT(YEAR FROM e.hireDate)) >= CURRENT_DATE
    //                AND e.hireDate + INTERVAL '1 year' * (EXTRACT(YEAR FROM CURRENT_DATE) - EXTRACT(YEAR FROM e.hireDate)) <= CURRENT_DATE + INTERVAL '30 days'

    //            ORDER BY Date ASC
    //            LIMIT @count",
    //            new { organizationId, count });

    //        return events.ToList();
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error getting upcoming events for organization {OrganizationId}", organizationId);
    //        return new List<UpcomingEventDto>();
    //    }
    //}




    public async Task<List<UpcomingEventDto>> GetUpcomingEventsAsync(Guid organizationId, int count = 5)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();

            var events = await connection.QueryAsync<UpcomingEventDto>(
                @"SELECT 
                'birthday' as Type,
                CONCAT(e.firstName, ' ', e.lastName, '''s Birthday') as Title,
                CONCAT('Birthday celebration for ', e.firstName, ' ', e.lastName) as Description,
                TO_CHAR(e.dateOfBirth + INTERVAL '1 year' * (EXTRACT(YEAR FROM CURRENT_DATE) - EXTRACT(YEAR FROM e.dateOfBirth)), 'YYYY-MM-DD') as Date,
                '09:00' as Time
            FROM employee.employees e
            WHERE e.organizationId = @organizationId
                AND e.status = 'active'
                AND e.dateOfBirth IS NOT NULL
                AND (EXTRACT(YEAR FROM CURRENT_DATE) - EXTRACT(YEAR FROM e.dateOfBirth)) >= 1
                AND e.dateOfBirth + INTERVAL '1 year' * (EXTRACT(YEAR FROM CURRENT_DATE) - EXTRACT(YEAR FROM e.dateOfBirth)) >= CURRENT_DATE
                AND e.dateOfBirth + INTERVAL '1 year' * (EXTRACT(YEAR FROM CURRENT_DATE) - EXTRACT(YEAR FROM e.dateOfBirth)) <= CURRENT_DATE + INTERVAL '30 days'

            UNION ALL

            SELECT 
                'anniversary' as Type,
                CONCAT(e.firstName, ' ', e.lastName, '''s Work Anniversary') as Title,
                CONCAT('Work anniversary celebration for ', e.firstName, ' ', e.lastName) as Description,
                TO_CHAR(e.hireDate + INTERVAL '1 year' * (EXTRACT(YEAR FROM CURRENT_DATE) - EXTRACT(YEAR FROM e.hireDate)), 'YYYY-MM-DD') as Date,
                '15:00' as Time
            FROM employee.employees e
            WHERE e.organizationId = @organizationId
                AND e.status = 'active'
                AND e.hireDate IS NOT NULL
                AND (EXTRACT(YEAR FROM CURRENT_DATE) - EXTRACT(YEAR FROM e.hireDate)) >= 1
                AND e.hireDate + INTERVAL '1 year' * (EXTRACT(YEAR FROM CURRENT_DATE) - EXTRACT(YEAR FROM e.hireDate)) >= CURRENT_DATE
                AND e.hireDate + INTERVAL '1 year' * (EXTRACT(YEAR FROM CURRENT_DATE) - EXTRACT(YEAR FROM e.hireDate)) <= CURRENT_DATE + INTERVAL '30 days'

            ORDER BY Date ASC
            LIMIT @count",
                new { organizationId, count });

            return events.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upcoming events for organization {OrganizationId}", organizationId);
            return new List<UpcomingEventDto>();
        }
    }




    public async Task<object> GetAttendanceTrendsAsync(Guid organizationId, string period = "30d")
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var daysInPeriod = period.ToLower() switch
            {
                "7d" => 7,
                "30d" => 30,
                "90d" => 90,
                _ => 30
            };

            var startDate = DateTime.UtcNow.AddDays(-daysInPeriod);
            
            var trends = await connection.QueryAsync<dynamic>(
                @"SELECT 
                    a.workDate as Date,
                    COUNT(*) FILTER (WHERE a.status = 'present') as PresentCount,
                    COUNT(*) FILTER (WHERE a.status = 'absent') as AbsentCount,
                    COUNT(*) FILTER (WHERE a.status = 'late') as LateCount
                FROM attendance.attendance a
                INNER JOIN employee.employees e ON a.employeeId = e.employeeId
                WHERE e.organizationId = @organizationId
                    AND a.workDate >= @startDate
                GROUP BY a.workDate
                ORDER BY a.workDate",
                new { organizationId, startDate });

            var dates = trends.Select(t => ((DateTime)t.date).ToString("yyyy-MM-dd")).ToList();
            var presentCounts = trends.Select(t => (int)t.presentcount).ToList();
            var absentCounts = trends.Select(t => (int)t.absentcount).ToList();
            var lateCounts = trends.Select(t => (int)t.latecount).ToList();

            return new
            {
                Dates = dates,
                PresentCounts = presentCounts,
                AbsentCounts = absentCounts,
                LateCounts = lateCounts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance trends for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<object> GetLeaveTrendsAsync(Guid organizationId, string period = "12m")
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var monthsInPeriod = period.ToLower() switch
            {
                "6m" => 6,
                "12m" => 12,
                "24m" => 24,
                _ => 12
            };

            var trends = await connection.QueryAsync<dynamic>(
                @"SELECT 
                    DATE_TRUNC('month', lr.startDate) as Month,
                    COUNT(*) as LeaveCount,
                    SUM(lr.daysRequested) as TotalDays
                FROM attendance.leaveRequests lr
                INNER JOIN employee.employees e ON lr.employeeId = e.employeeId
                WHERE e.organizationId = @organizationId
                    AND lr.startDate >= CURRENT_DATE - INTERVAL '@months months'
                    AND lr.status = 'approved'
                GROUP BY DATE_TRUNC('month', lr.startDate)
                ORDER BY Month",
                new { organizationId, months = monthsInPeriod });

            var months = trends.Select(t => ((DateTime)t.month).ToString("MMM yyyy")).ToList();
            var leaveCounts = trends.Select(t => (int)t.leavecount).ToList();
            var totalDays = trends.Select(t => (decimal)t.totaldays).ToList();

            return new
            {
                Months = months,
                LeaveCounts = leaveCounts,
                TotalDays = totalDays
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leave trends for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<object> GetPayrollTrendsAsync(Guid organizationId, string period = "12m")
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var monthsInPeriod = period.ToLower() switch
            {
                "6m" => 6,
                "12m" => 12,
                "24m" => 24,
                _ => 12
            };

            var trends = await connection.QueryAsync<dynamic>(
                @"SELECT 
                    DATE_TRUNC('month', pp.payDate) as Month,
                    SUM(pe.netSalary) as TotalCost,
                    COUNT(DISTINCT pe.employeeId) as EmployeeCount,
                    AVG(pe.netSalary) as AverageSalary
                FROM payroll.payrollPeriods pp
                INNER JOIN payroll.payrollEntries pe ON pp.periodId = pe.periodId
                INNER JOIN employee.employees e ON pe.employeeId = e.employeeId
                WHERE e.organizationId = @organizationId
                    AND pp.payDate >= CURRENT_DATE - INTERVAL '@months months'
                GROUP BY DATE_TRUNC('month', pp.payDate)
                ORDER BY Month",
                new { organizationId, months = monthsInPeriod });

            var months = trends.Select(t => ((DateTime)t.month).ToString("MMM yyyy")).ToList();
            var payrollCosts = trends.Select(t => (decimal)t.totalcost).ToList();
            var employeeCounts = trends.Select(t => (int)t.employeecount).ToList();
            var averageSalaries = trends.Select(t => (decimal)t.averagesalary).ToList();

            return new
            {
                Months = months,
                PayrollCosts = payrollCosts,
                EmployeeCounts = employeeCounts,
                AverageSalaries = averageSalaries
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payroll trends for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<List<object>> GetTopPerformersAsync(Guid organizationId, int limit = 5)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var performers = await connection.QueryAsync<dynamic>(
                @"SELECT 
                    e.employeeId,
                    CONCAT(e.firstName, ' ', e.lastName) as EmployeeName,
                    d.departmentName as Department,
                    AVG(ea.overallRating) as AverageRating,
                    COUNT(ea.appraisalId) as ReviewCount
                FROM performance.employeeAppraisals ea
                INNER JOIN employee.employees e ON ea.employeeId = e.employeeId
                INNER JOIN employee.employmentDetails ed ON e.employeeId = ed.employeeId AND ed.isCurrent = true
                INNER JOIN employee.departments d ON ed.departmentId = d.departmentId
                WHERE e.organizationId = @organizationId
                    AND ea.status = 'completed'
                GROUP BY e.employeeId, e.firstName, e.lastName, d.departmentName
                ORDER BY AverageRating DESC, ReviewCount DESC
                LIMIT @limit",
                new { organizationId, limit });

            return performers.Select(p => new
            {
                EmployeeId = (Guid)p.employeeid,
                EmployeeName = (string)p.employeename,
                Department = (string)p.department,
                AverageRating = (decimal)p.averagerating,
                ReviewCount = (int)p.reviewcount
            }).Cast<object>().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top performers for organization {OrganizationId}", organizationId);
            return new List<object>();
        }
    }

    public async Task<object> GetRemindersAsync(Guid organizationId)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var upcomingBirthdays = await connection.QuerySingleOrDefaultAsync<int>(
                @"SELECT COUNT(*) 
                FROM employee.employees 
                WHERE organizationId = @organizationId 
                    AND status = 'active'
                    AND EXTRACT(month FROM dateOfBirth) = EXTRACT(month FROM CURRENT_DATE)
                    AND EXTRACT(day FROM dateOfBirth) >= EXTRACT(day FROM CURRENT_DATE)",
                new { organizationId });

            var pendingReviews = await connection.QuerySingleOrDefaultAsync<int>(
                @"SELECT COUNT(*) 
                FROM performance.employeeAppraisals ea
                INNER JOIN employee.employees e ON ea.employeeId = e.employeeId
                WHERE e.organizationId = @organizationId AND ea.status = 'pending'",
                new { organizationId });

            var pendingLeaves = await connection.QuerySingleOrDefaultAsync<int>(
                @"SELECT COUNT(*) 
                FROM attendance.leaveRequests lr
                INNER JOIN employee.employees e ON lr.employeeId = e.employeeId
                WHERE e.organizationId = @organizationId AND lr.status = 'pending'",
                new { organizationId });

            return new
            {
                UpcomingBirthdays = upcomingBirthdays,
                PendingReviews = pendingReviews,
                PendingLeaves = pendingLeaves
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reminders for organization {OrganizationId}", organizationId);
            return new { };
        }
    }

    public async Task<object> GetQuickStatsAsync(Guid organizationId)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var stats = await GetDashboardStatsAsync(organizationId);
            
            var lastPayroll = await connection.QuerySingleOrDefaultAsync<dynamic>(
                @"SELECT 
                    pp.totalNetAmount as LastPayrollAmount,
                    pp.periodName as LastPeriodName
                FROM payroll.payrollPeriods pp
                INNER JOIN employee.employees e ON 1=1
                WHERE e.organizationId = @organizationId
                ORDER BY pp.endDate DESC
                LIMIT 1",
                new { organizationId });

            var thisMonthAttendance = await connection.QuerySingleOrDefaultAsync<dynamic>(
                @"SELECT 
                    COUNT(*) FILTER (WHERE a.status = 'present') as PresentCount,
                    COUNT(*) as TotalDays
                FROM attendance.attendance a
                INNER JOIN employee.employees e ON a.employeeId = e.employeeId
                WHERE e.organizationId = @organizationId
                    AND EXTRACT(month FROM a.workDate) = EXTRACT(month FROM CURRENT_DATE)
                    AND EXTRACT(year FROM a.workDate) = EXTRACT(year FROM CURRENT_DATE)",
                new { organizationId });

            return new
            {
                TotalEmployees = stats.TotalEmployees,
                ActiveEmployees = stats.ActiveEmployees,
                PresentToday = stats.PresentToday,
                PendingApprovals = stats.PendingLeaveRequests,
                LastPayrollAmount = lastPayroll?.lastpayrollamount ?? 0m,
                LastPeriodName = lastPayroll?.lastperiodname ?? "N/A",
                ThisMonthAttendance = thisMonthAttendance?.presentcount ?? 0,
                ThisMonthTotalDays = thisMonthAttendance?.totaldays ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quick stats for organization {OrganizationId}", organizationId);
            return new { };
        }
    }

    private async Task<int> GetOnLeaveTodayCountAsync(Guid organizationId)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            return await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(DISTINCT lr.employeeId)
                FROM attendance.leaveRequests lr
                INNER JOIN employee.employees e ON lr.employeeId = e.employeeId
                WHERE e.organizationId = @organizationId
                    AND lr.status = 'approved'
                    AND CURRENT_DATE BETWEEN lr.startDate AND lr.endDate",
                new { organizationId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting on leave today count for organization {OrganizationId}", organizationId);
            return 0;
        }
    }

    public async Task<OnboardingStatusDto> GetOnboardingStatusAsync(Guid organizationId)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            
            // Step 2: Create Team - Check if Departments, Positions, or Employees exist
            var createTeam = await connection.ExecuteScalarAsync<bool>(
                @"SELECT EXISTS(
                    SELECT 1 FROM employee.departments WHERE organizationid = @organizationId AND isactive = true
                    UNION
                    SELECT 1 FROM employee.positions WHERE organizationid = @organizationId AND isactive = true
                    UNION
                    SELECT 1 FROM employee.employees WHERE organizationid = @organizationId
                    LIMIT 1
                )",
                new { organizationId });

            // Step 3: Define Leave Types - Check if LeaveTypes exist
            var defineLeaveTypes = await connection.ExecuteScalarAsync<bool>(
                @"SELECT EXISTS(
                    SELECT 1 FROM attendance.leavetypes 
                    WHERE organizationid = @organizationId AND isactive = true
                    LIMIT 1
                )",
                new { organizationId });

            // Step 4: Mark Attendance - Check if any employee has marked attendance OR if shifts exist
            var markAttendance = await connection.ExecuteScalarAsync<bool>(
                @"SELECT EXISTS(
                    SELECT 1 FROM attendance.attendance a
                    INNER JOIN employee.employees e ON a.employeeid = e.employeeid
                    WHERE e.organizationid = @organizationId
                    UNION
                    SELECT 1 FROM attendance.shifts 
                    WHERE organizationid = @organizationId AND isactive = true
                    LIMIT 1
                )",
                new { organizationId });

            // Step 5: Process Payroll - Check if PayrollPeriods exist, then check if PayrollEntries exist for those periods
            var processPayroll = await connection.ExecuteScalarAsync<bool>(
                @"SELECT EXISTS(
                    SELECT 1 FROM payroll.payrollentries pe
                    INNER JOIN payroll.payrollperiods pp ON pe.periodid = pp.periodid
                    WHERE pp.organizationid = @organizationId
                    LIMIT 1
                )",
                new { organizationId });

            return new OnboardingStatusDto
            {
                SignUp = true, // Always true if user is on dashboard
                CreateTeam = createTeam,
                DefineLeaveTypes = defineLeaveTypes,
                MarkAttendance = markAttendance,
                ProcessPayroll = processPayroll
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding status for organization {OrganizationId}", organizationId);
            throw;
        }
    }
}