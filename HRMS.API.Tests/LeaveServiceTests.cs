using AutoMapper;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services;
using HRMS.API.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using static HRMS.API.Models.DTOs.NotificationDto;

namespace HRMS.API.Tests;

public sealed class LeaveServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid EmpId = Guid.NewGuid();
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid LeaveTypeId = Guid.NewGuid();
    private static readonly Guid ManagerId = Guid.NewGuid();

    private readonly Mock<ILeaveRepository> _leaveRepo = new();
    private readonly Mock<INotificationRepository> _notificationRepo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<IAttendanceRepository> _attendanceRepo = new();
    private readonly Mock<IHolidayRepository> _holidayRepo = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<IMapper> _mapper = new();

    private LeaveService CreateService() => new(
        _leaveRepo.Object,
        _notificationRepo.Object,
        _employeeRepo.Object,
        _attendanceRepo.Object,
        _holidayRepo.Object,
        _email.Object,
        _mapper.Object,
        NullLogger<LeaveService>.Instance);

    /// <summary>Sets up a happy path: employee exists, no holidays, no overlap, Mon-Fri shift, manager assigned.</summary>
    private void SetupHappyCreatePath(int[]? shiftDays = null)
    {
        _leaveRepo.Setup(r => r.GetEmpId(UserId)).ReturnsAsync(EmpId);
        _employeeRepo.Setup(r => r.GetEmployeeByEmployeeIdAsync(EmpId)).ReturnsAsync(new Employee
        {
            EmployeeId = EmpId,
            OrganizationId = OrgId,
            FirstName = "Ali",
            LastName = "Khan",
            DepartmentName = "IT"
        });
        _holidayRepo.Setup(r => r.GetHolidaysInRangeForEmployeeAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Array.Empty<HolidayRangeDto>());
        _leaveRepo.Setup(r => r.HasOverlappingLeaveAsync(EmpId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(false);
        _attendanceRepo.Setup(r => r.GetShiftByEmployeeIdAsync(EmpId))
            .ReturnsAsync(new ShiftDto { DaysOfWeek = shiftDays ?? new[] { 1, 2, 3, 4, 5 } });
        _leaveRepo.Setup(r => r.CreateLeaveRequestAsync(EmpId, LeaveTypeId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>()))
            .ReturnsAsync(new LeaveRequest { RequestId = Guid.NewGuid(), EmployeeId = EmpId });
        _leaveRepo.Setup(r => r.GetLeaveTypeNameByIdAsync(LeaveTypeId, OrgId)).ReturnsAsync("Annual");
        _notificationRepo.Setup(r => r.GetReciveridANdEmailAsync(EmpId)).ReturnsAsync(new leaveNotification
        {
            reportingmanagerid = ManagerId,
            email = "manager@test.com",
            receivername = "Manager"
        });
    }

    private static CreateLeaveRequest Request(DateTime start, DateTime end) => new()
    {
        LeaveTypeId = LeaveTypeId,
        StartDate = start,
        EndDate = end,
        Reason = "Family"
    };

    // ---------- CreateLeaveRequestAsync: validation ----------

    [Fact]
    public async Task Create_EmptyEmployeeId_Throws()
    {
        var svc = CreateService();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateLeaveRequestAsync(Request(DateTime.Today, DateTime.Today), Guid.Empty, OrgId));
    }

    [Fact]
    public async Task Create_NullRequest_Throws()
    {
        var svc = CreateService();
        await Assert.ThrowsAsync<ArgumentNullException>(() => svc.CreateLeaveRequestAsync(null!, UserId, OrgId));
    }

    [Fact]
    public async Task Create_MissingLeaveType_Throws()
    {
        var svc = CreateService();
        var req = Request(DateTime.Today, DateTime.Today);
        req.LeaveTypeId = Guid.Empty;
        await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateLeaveRequestAsync(req, UserId, OrgId));
    }

    [Fact]
    public async Task Create_StartAfterEnd_Throws()
    {
        var svc = CreateService();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateLeaveRequestAsync(Request(DateTime.Today.AddDays(2), DateTime.Today), UserId, OrgId));
    }

    // ---------- CreateLeaveRequestAsync: business rules ----------

    [Fact]
    public async Task Create_EmployeeNotFound_Throws()
    {
        _leaveRepo.Setup(r => r.GetEmpId(UserId)).ReturnsAsync(EmpId);
        _employeeRepo.Setup(r => r.GetEmployeeByEmployeeIdAsync(EmpId)).ReturnsAsync((Employee?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService().CreateLeaveRequestAsync(Request(DateTime.Today, DateTime.Today), UserId, OrgId));
        Assert.Equal("Employee profile not found", ex.Message);
    }

    [Fact]
    public async Task Create_OnHoliday_IsRejected()
    {
        SetupHappyCreatePath();
        _holidayRepo.Setup(r => r.GetHolidaysInRangeForEmployeeAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new[] { new HolidayRangeDto() });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService().CreateLeaveRequestAsync(Request(DateTime.Today, DateTime.Today), UserId, OrgId));
        Assert.Contains("holiday", ex.Message, StringComparison.OrdinalIgnoreCase);
        _leaveRepo.Verify(r => r.CreateLeaveRequestAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Create_Overlapping_IsRejected()
    {
        SetupHappyCreatePath();
        _leaveRepo.Setup(r => r.HasOverlappingLeaveAsync(EmpId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService().CreateLeaveRequestAsync(Request(DateTime.Today, DateTime.Today), UserId, OrgId));
        Assert.Contains("overlaps", ex.Message);
    }

    [Fact]
    public async Task Create_NoShiftAssigned_IsRejected()
    {
        SetupHappyCreatePath();
        _attendanceRepo.Setup(r => r.GetShiftByEmployeeIdAsync(EmpId)).ReturnsAsync((ShiftDto?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService().CreateLeaveRequestAsync(Request(DateTime.Today, DateTime.Today), UserId, OrgId));
        Assert.Equal("No Shift assigned", ex.Message);
    }

    [Fact]
    public async Task Create_DeductsOnlyShiftWorkingDays_MonToFri()
    {
        SetupHappyCreatePath();
        // Mon 2 Nov 2026 -> Sun 8 Nov 2026 = 7 calendar days, 5 working days
        var start = new DateTime(2026, 11, 2);
        var end = new DateTime(2026, 11, 8);

        await CreateService().CreateLeaveRequestAsync(Request(start, end), UserId, OrgId);

        _leaveRepo.Verify(r => r.SubstractLeaveDayAsync(LeaveTypeId, EmpId, 5), Times.Once);
    }

    [Fact]
    public async Task Create_DeductsOnlyShiftWorkingDays_SixDayWeek()
    {
        SetupHappyCreatePath(new[] { 1, 2, 3, 4, 5, 6 }); // Mon-Sat shift
        var start = new DateTime(2026, 11, 2);
        var end = new DateTime(2026, 11, 8);

        await CreateService().CreateLeaveRequestAsync(Request(start, end), UserId, OrgId);

        _leaveRepo.Verify(r => r.SubstractLeaveDayAsync(LeaveTypeId, EmpId, 6), Times.Once);
    }

    [Fact]
    public async Task Create_NotifiesAndEmailsManager()
    {
        SetupHappyCreatePath();
        var start = new DateTime(2026, 11, 2);

        await CreateService().CreateLeaveRequestAsync(Request(start, start), UserId, OrgId);

        _notificationRepo.Verify(r => r.CreateNotificationAsync(OrgId,
            It.Is<NotificationCreateDto>(n => n.Receiveruserid!.Contains(ManagerId) && n.moduletype == "Leave")), Times.Once);
        _email.Verify(e => e.SendLeaveRequestToManagerAsync("manager@test.com", "Manager", "Ali Khan", "Annual",
            start, start, "Family", OrgId), Times.Once);
    }

    [Fact]
    public async Task Create_NoManager_FallsBackToSuperAdmin()
    {
        SetupHappyCreatePath();
        var adminId = Guid.NewGuid();
        _notificationRepo.Setup(r => r.GetReciveridANdEmailAsync(EmpId)).ReturnsAsync((leaveNotification)null!);
        _notificationRepo.Setup(r => r.GetNoManagerReciveridAsync(EmpId)).ReturnsAsync(new leaveNotification
        {
            reportingmanagerid = adminId,
            email = "admin@test.com",
            receivername = "Admin"
        });

        await CreateService().CreateLeaveRequestAsync(Request(new DateTime(2026, 11, 2), new DateTime(2026, 11, 2)), UserId, OrgId);

        _notificationRepo.Verify(r => r.CreateNotificationAsync(OrgId,
            It.Is<NotificationCreateDto>(n => n.Receiveruserid!.Contains(adminId))), Times.Once);
    }

    [Fact]
    public async Task Create_NoManagerAndNoSuperAdmin_StillSavesLeaveWithoutCrashing()
    {
        SetupHappyCreatePath();
        _notificationRepo.Setup(r => r.GetReciveridANdEmailAsync(EmpId)).ReturnsAsync((leaveNotification)null!);
        _notificationRepo.Setup(r => r.GetNoManagerReciveridAsync(EmpId)).ReturnsAsync((leaveNotification)null!);
        _mapper.Setup(m => m.Map<LeaveRequestDto>(It.IsAny<LeaveRequest>())).Returns(new LeaveRequestDto());

        // Leave has already been saved and balance deducted at this point,
        // so a missing approver must not turn the request into a 500 error.
        var result = await CreateService().CreateLeaveRequestAsync(
            Request(new DateTime(2026, 11, 2), new DateTime(2026, 11, 2)), UserId, OrgId);

        Assert.NotNull(result);
    }

    // ---------- UpdateLeaveRequestStatusAsync ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("pending")]
    [InlineData("maybe")]
    public async Task UpdateStatus_InvalidStatus_Throws(string status)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateService().UpdateLeaveRequestStatusAsync(
            Guid.NewGuid(), new UpdateLeaveRequestStatusRequest { Status = status }, UserId, OrgId));
    }

    [Fact]
    public async Task UpdateStatus_EmptyRequestId_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateService().UpdateLeaveRequestStatusAsync(
            Guid.Empty, new UpdateLeaveRequestStatusRequest { Status = "approved" }, UserId, OrgId));
    }

    [Fact]
    public async Task UpdateStatus_RepositoryFails_ReturnsFalse_AndSendsNothing()
    {
        var requestId = Guid.NewGuid();
        _leaveRepo.Setup(r => r.GetEmpId(UserId)).ReturnsAsync(EmpId);
        _leaveRepo.Setup(r => r.UpdateLeaveRequestStatusAsync(requestId, "approved", EmpId, null)).ReturnsAsync(false);

        var ok = await CreateService().UpdateLeaveRequestStatusAsync(
            requestId, new UpdateLeaveRequestStatusRequest { Status = "approved" }, UserId, OrgId);

        Assert.False(ok);
        _email.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("approved", true)]
    [InlineData("Approved", true)]
    [InlineData("APPROVED", true)]
    [InlineData("rejected", false)]
    [InlineData("Rejected", false)]
    public async Task UpdateStatus_EmailReflectsDecision_RegardlessOfCase(string status, bool expectedApproval)
    {
        var requestId = Guid.NewGuid();
        _leaveRepo.Setup(r => r.GetEmpId(UserId)).ReturnsAsync(EmpId);
        _leaveRepo.Setup(r => r.UpdateLeaveRequestStatusAsync(requestId, It.IsAny<string>(), EmpId, It.IsAny<string?>()))
            .ReturnsAsync(true);
        _leaveRepo.Setup(r => r.GetEmployeeDetails(requestId)).ReturnsAsync(new PushEmailLeaveRequest
        {
            email = "emp@test.com",
            EmployeeName = "Ali Khan",
            typename = "Annual"
        });

        await CreateService().UpdateLeaveRequestStatusAsync(
            requestId, new UpdateLeaveRequestStatusRequest { Status = status }, UserId, OrgId);

        _email.Verify(e => e.SendLeaveApprovalRejectionEmailAsync("emp@test.com", "Ali Khan", "Annual",
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), expectedApproval, OrgId), Times.Once);
    }

    // ---------- CancelLeaveRequestAsync ----------

    [Fact]
    public async Task Cancel_NotFound_Throws()
    {
        _leaveRepo.Setup(r => r.GetLeaveRequestByIdAsync(It.IsAny<Guid>())).ReturnsAsync((LeaveRequest?)null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().CancelLeaveRequestAsync(Guid.NewGuid(), UserId));
    }

    [Theory]
    [InlineData("approved")]
    [InlineData("rejected")]
    public async Task Cancel_NonPending_Throws(string status)
    {
        _leaveRepo.Setup(r => r.GetLeaveRequestByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new LeaveRequest { Status = status, EmployeeId = EmpId });
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().CancelLeaveRequestAsync(Guid.NewGuid(), UserId));
    }

    [Fact]
    public async Task Cancel_SomeoneElsesRequest_IsUnauthorized()
    {
        _leaveRepo.Setup(r => r.GetLeaveRequestByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new LeaveRequest { Status = "pending", EmployeeId = Guid.NewGuid() });
        _leaveRepo.Setup(r => r.GetEmpId(UserId)).ReturnsAsync(EmpId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => CreateService().CancelLeaveRequestAsync(Guid.NewGuid(), UserId));
        _leaveRepo.Verify(r => r.CancelLeaveRequestAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_Pending_RestoresBalance()
    {
        var requestId = Guid.NewGuid();
        _leaveRepo.Setup(r => r.GetLeaveRequestByIdAsync(requestId)).ReturnsAsync(new LeaveRequest
        {
            RequestId = requestId, Status = "Pending", EmployeeId = EmpId, LeaveTypeId = LeaveTypeId, DaysRequested = 3
        });
        _leaveRepo.Setup(r => r.GetEmpId(UserId)).ReturnsAsync(EmpId);
        _leaveRepo.Setup(r => r.CancelLeaveRequestAsync(requestId)).ReturnsAsync(true);
        _notificationRepo.Setup(r => r.DeleteNotificationAsync(requestId)).ReturnsAsync(true);

        var ok = await CreateService().CancelLeaveRequestAsync(requestId, UserId);

        Assert.True(ok);
        _leaveRepo.Verify(r => r.EditingLeaveElementalsFordaysLess(EmpId, LeaveTypeId, 3m), Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenDbCancelFails_DoesNotRestoreBalance()
    {
        var requestId = Guid.NewGuid();
        _leaveRepo.Setup(r => r.GetLeaveRequestByIdAsync(requestId)).ReturnsAsync(new LeaveRequest
        {
            RequestId = requestId, Status = "pending", EmployeeId = EmpId, LeaveTypeId = LeaveTypeId, DaysRequested = 3
        });
        _leaveRepo.Setup(r => r.GetEmpId(UserId)).ReturnsAsync(EmpId);
        _leaveRepo.Setup(r => r.CancelLeaveRequestAsync(requestId)).ReturnsAsync(false);

        var ok = await CreateService().CancelLeaveRequestAsync(requestId, UserId);

        Assert.False(ok);
        _leaveRepo.Verify(r => r.EditingLeaveElementalsFordaysLess(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
    }

    // ---------- Calendar colours ----------

    [Fact]
    public async Task Calendar_MapsStatusToColour()
    {
        _leaveRepo.Setup(r => r.GetEmpId(UserId)).ReturnsAsync(EmpId);
        _leaveRepo.Setup(r => r.GetLeaveRequestsAsync(EmpId, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<LeaveCalendarDto>
            {
                new() { Status = "approved" },
                new() { Status = "rejected" },
                new() { Status = "pending" },
                new() { Status = "cancelled" }
            });

        var result = await CreateService().GetLeaveRequestsForCalendarAsync(UserId, DateTime.Today, DateTime.Today.AddDays(30));

        Assert.Equal(new[] { "#28a745", "#dc3545", "#ffc107", "#6c757d" }, result.Select(r => r.Color));
    }

    // ---------- Team remaining leaves ----------

    [Fact]
    public async Task TeamRemaining_UserWithoutRole_UsesManagerPath_InsteadOfCrashing()
    {
        _leaveRepo.Setup(r => r.GetUserRoleNameAsync(UserId)).ReturnsAsync((string?)null);
        _leaveRepo.Setup(r => r.GetEmpId(UserId)).ReturnsAsync(EmpId);
        _leaveRepo.Setup(r => r.GetTeamRemainingLeavesAsync(EmpId, OrgId, It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new PagedResult<EmployeeLeaveBalanceDto> { Data = new List<EmployeeLeaveBalanceDto>() });

        var result = await CreateService().GetTeamRemainingLeavesAsync(UserId, OrgId, new TeamRemainingLeavesSearchViewModel());

        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task TeamRemaining_GroupsBalancesPerEmployee()
    {
        var e1 = Guid.NewGuid();
        _leaveRepo.Setup(r => r.GetUserRoleNameAsync(UserId)).ReturnsAsync("HR Manager");
        _leaveRepo.Setup(r => r.GetTeamRemainingLeavesByOrganizationAsync(OrgId, It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new PagedResult<EmployeeLeaveBalanceDto>
            {
                TotalCount = 1,
                Data = new List<EmployeeLeaveBalanceDto>
                {
                    new() { EmployeeId = e1, EmployeeName = "A", LeaveTypeName = "Annual", TotalDays = 14, UsedDays = 4, RemainingDays = 10 },
                    new() { EmployeeId = e1, EmployeeName = "A", LeaveTypeName = "Sick", TotalDays = 8, UsedDays = 1, RemainingDays = 7 }
                }
            });

        var result = await CreateService().GetTeamRemainingLeavesAsync(UserId, OrgId, new TeamRemainingLeavesSearchViewModel());

        var emp = Assert.Single(result.Data);
        Assert.Equal(2, emp.LeaveBalances.Count);
    }
}
