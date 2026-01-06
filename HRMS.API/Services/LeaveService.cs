using AutoMapper;
using HRMS.AI.Models;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Repositories;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Requests;
using HRMS.API.Services.Interfaces;
using System.Net;
using static HRMS.API.Models.DTOs.NotificationDto;

namespace HRMS.API.Services;

public class LeaveService : ILeaveService
{
    private readonly ILeaveRepository _leaveRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(
        ILeaveRepository leaveRepository,
        INotificationRepository notificationRepository,
        IEmployeeRepository employeeRepository,
        IMapper mapper,
        ILogger<LeaveService> logger)
    {
        _leaveRepository = leaveRepository;
        _notificationRepository = notificationRepository;
        _employeeRepository = employeeRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<LeaveRequestDto> CreateLeaveRequestAsync(CreateLeaveRequest request, Guid employeeId)
    {
        var empId = await _leaveRepository.GetEmpId(employeeId);
        // Check for overlapping leave requests
        var hasOverlap = await _leaveRepository.HasOverlappingLeaveAsync(empId, request.StartDate, request.EndDate);
        if (hasOverlap)
        {
            throw new InvalidOperationException("Leave request overlaps with existing leave");
        }
        var leaveRequest = await _leaveRepository.CreateLeaveRequestAsync(
            empId, request.LeaveTypeId, request.StartDate, request.EndDate, request.Reason);

        //Create Notification
        var employee = await _employeeRepository.GetEmployeeByIdAsync(empId);
        //Getting reciverid
        var reportingmangerid = await _notificationRepository.GetReciveridAsync(employee.EmployeeId);

        //var reciverid = await _notificationRepository.GetEmpId(reportingmangerid);
        if (leaveRequest != null)

        {
            var notification = new NotificationCreateDto
            {
                Receiveruserid = reportingmangerid,
                Senderuserid = employee.EmployeeId,
                Ttitle = "Leave Request",
                RequestId = leaveRequest.RequestId,
                Message = $"Employee {employee.FirstName} Department {employee.DepartmentName}  request a Leave ",
                Notificationtype = "Request",
                Createdby = employee.EmployeeId,
                moduletype = "Leave",
                Redirecturl = "/leave/team",
                organizationId = employee.OrganizationId
            };
            await _notificationRepository.CreateNotificationAsync(employee.OrganizationId, notification);
        }

        return _mapper.Map<LeaveRequestDto>(leaveRequest);
    }





    //public async Task<ServiceResponse<LeaveRequestDto>> CreateLeaveRequestAsync(
    //CreateLeaveRequest request,
    //Guid employeeId)
    //{
    //    try
    //    {
    //        _logger.LogInformation("Creating Leave Request for EmployeeId: {employeeId}", employeeId);

    //        var empId = await _leaveRepository.GetEmpId(employeeId);

    //        // Check for overlapping leave requests
    //        var hasOverlap = await _leaveRepository.HasOverlappingLeaveAsync(
    //            empId, request.StartDate, request.EndDate);

    //        if (hasOverlap)
    //        {
    //            _logger.LogInformation("Leave request overlaps with existing leave. EmployeeId: {employeeId}", employeeId);

    //            return new ServiceResponse<LeaveRequestDto>
    //            {
    //                Success = false,
    //                StatusCode = (int)HttpStatusCode.BadRequest,
    //                Message = "Leave request overlaps with existing leave",
    //                Data = null
    //            };
    //        }

    //        var leaveRequest = await _leaveRepository.CreateLeaveRequestAsync(
    //            empId, request.LeaveTypeId, request.StartDate, request.EndDate, request.Reason);

    //        // Create Notification
    //        var employee = await _employeeRepository.GetEmployeeByIdAsync(empId);
    //        var reportingManagerId = await _notificationRepository.GetReciveridAsync(employee.EmployeeId);

    //        if (leaveRequest != null)
    //        {
    //            var notification = new NotificationCreateDto
    //            {
    //                Receiveruserid = reportingManagerId,
    //                Senderuserid = employee.EmployeeId,
    //                Ttitle = "Leave Request",
    //                RequestId = leaveRequest.RequestId,
    //                Message = $"Employee {employee.FirstName} Department {employee.DepartmentName} request a Leave",
    //                Notificationtype = "Request",
    //                Createdby = employee.EmployeeId,
    //                moduletype = "Leave",
    //                Redirecturl = "/leave/team",
    //                organizationId = employee.OrganizationId
    //            };

    //            await _notificationRepository.CreateNotificationAsync(employee.OrganizationId, notification);
    //        }

    //        _logger.LogInformation("Leave Request created successfully. RequestId: {requestId}", leaveRequest.RequestId);

    //        return new ServiceResponse<LeaveRequestDto>
    //        {
    //            Success = true,
    //            StatusCode = (int)HttpStatusCode.Created,
    //            Message = "Leave request submitted successfully",
    //            Data = _mapper.Map<LeaveRequestDto>(leaveRequest)
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error while creating leave request for EmployeeId: {employeeId}", employeeId);

    //        return new ServiceResponse<LeaveRequestDto>
    //        {
    //            Success = false,
    //            StatusCode = (int)HttpStatusCode.InternalServerError,
    //            Message = "An unexpected error occurred",
    //            Data = null
    //        };
    //    }
    //}








    public async Task<LeaveApplicationDetails> CreateLeaveRequestByChatBotAsync(LeaveApplicationDetails request, Guid employeeId, Guid organizationId)
    {
        try
        {
            var leavetypeId = await _leaveRepository.GetLeaveTypeIdByNameAsync(request.leaveType, organizationId);
            var typeId = leavetypeId.FirstOrDefault().LeaveTypeId;
            var requestDto = new CreateLeaveRequest
            {
                LeaveTypeId = typeId,
                StartDate = request.startDate,
                EndDate = request.endDate,
                Reason = request.reason,
                AttachmentUrl = null
            };
            var startDate = request.startDate.Date;
            var endDate = request.endDate.Date;
            var today = DateTime.Today;
            var response = await CreateLeaveRequestAsync(requestDto, employeeId);
            var daysrequested = CalculateWorkingDays(request.startDate, request.endDate);

            if (daysrequested > 0)
            {
                response.DaysRequested = daysrequested;
            }

            if (startDate <= today)
            {
                throw new InvalidOperationException("Leave start date must be a future date.");
            }

            if (response != null)
            {
                return new LeaveApplicationDetails
                {
                    leavetypeid = response.LeaveTypeId,
                    leaveType = response.LeaveTypeName,
                    startDate = response.StartDate,
                    endDate = response.EndDate,
                    reason = response.Reason,
                    status = response.Status,
                    statustext = $"✅ Your leave request from {response.StartDate:dd MMM yyyy} to {response.EndDate:dd MMM yyyy} " +
                          $"for {response.DaysRequested} day(s) has been submitted with {response.Status} status \nfor more details check your calander https://frontend-hrms-phi.vercel.app/leave/calendar"
                };
            }
            else
            {
                return new LeaveApplicationDetails
                {
                    leavetypeid = Guid.Empty,
                    leaveType = request.leaveType, // use original request
                    startDate = request.startDate,
                    endDate = request.endDate,
                    reason = request.reason,
                    status = "Failed",
                    statustext = "❌ Unable to submit leave request. Please try again or check details."
                };
            }
        }
        catch (Exception ex)
        {
            return new LeaveApplicationDetails
            {
                leavetypeid = Guid.Empty,
                leaveType = request.leaveType, // use original request
                startDate = request.startDate,
                endDate = request.endDate,
                reason = request.reason,
                status = "Failed",
                statustext = ex.Message
            };
        }
    }


    public async Task<ServiceResponse<List<LeaveRequest>>>GetLeaveRequestByEmployeeId(Guid employeeId)
    {
        try
        {
            _logger.LogInformation( "Getting Leave Requests for Employee. EmployeeId: {employeeId}",employeeId);

            var empId = await _leaveRepository.GetEmpId(employeeId);

            if (empId == Guid.Empty)
            {
                _logger.LogInformation("Employee not found. EmployeeId: {employeeId}", employeeId);

                return new ServiceResponse<List<LeaveRequest>>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Employee not found",
                    Data = null
                };
            }

            var leaveRequests =
                await _leaveRepository.GetLeaveRequestsByEmployeeId(empId);

            if (leaveRequests == null || !leaveRequests.Any())
            {
                _logger.LogInformation("No Leave Requests found for EmployeeId: {employeeId}",employeeId);

                return new ServiceResponse<List<LeaveRequest>>
                {
                    Success = true,
                    StatusCode = (int)HttpStatusCode.OK,
                    Message = "No leave requests found",
                    Data = null
                };
            }

            _logger.LogInformation("Leave Requests fetched successfully for EmployeeId: {employeeId}",employeeId);

            return new ServiceResponse<List<LeaveRequest>>
            {
                Success = true,
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Leave requests retrieved successfully",
                Data = leaveRequests
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Error while fetching leave requests for EmployeeId: {employeeId}",employeeId);

            return new ServiceResponse<List<LeaveRequest>>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An unexpected error occurred",
                Data = null
            };
        }
    }


    public async Task<ServiceResponse<bool>> UpdateLeaveRequestStatusAsync(
    Guid requestId,
    UpdateLeaveRequestStatusRequest request,
    Guid approvedBy)
    {
        try
        {
            _logger.LogInformation("Getting Approved by id");
            var empId = await _leaveRepository.GetEmpId(approvedBy);
            if (empId == Guid.Empty)
            {
                _logger.LogInformation("Employee not found :{empId}", empId);
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Leave request approved by not Found",
                    StatusCode = (int)HttpStatusCode.OK,
                    Data = false
                };
            }

            // First, update the leave request status using the requestId parameter directly
            _logger.LogInformation("Updating leave request in Table");
            var result = await _leaveRepository.UpdateLeaveRequestStatusAsync(requestId, request.Status, empId, request.RejectionReason);

            if (!result)
            {
                _logger.LogWarning("Failed to update leave request status for RequestId: {RequestId}", requestId);
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Failed to update Leave Request",
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Data = false
                };
            }

            _logger.LogInformation("Getting Notifcation for : {RequestId}", requestId);

            // Try to get the original notification (if it exists)
            var Originalnotification = await _notificationRepository.getnotificationdetailAsync(requestId);

            // Build the notification message based on status
            string message;
            if (request.Status.ToLower() == "approved")
            {
                message = "Your leave request has been approved";
            }
            else if (request.Status.ToLower() == "rejected")
            {
                message = $"Your leave request has been rejected. Reason: {request.RejectionReason}";
            }
            else
            {
                message = $"Your leave request status has been updated to {request.Status}";
            }

            // If original notification exists, use it to create response notification
            if (Originalnotification != null)
            {
                var notification = new NotificationCreateDto
                {
                    Receiveruserid = Originalnotification.SenderUserId,
                    Senderuserid = Originalnotification.ReceiverUserId,
                    Ttitle = "Leave Request",
                    RequestId = Originalnotification.requestid,
                    Message = message,
                    Notificationtype = "Message",
                    Createdby = Originalnotification.SenderUserId,
                };

                _logger.LogInformation("Creating notification for organizationId: {OrganizationId}", Originalnotification.organizationId);
                var notifcation = _notificationRepository.CreateNotificationAsync(Originalnotification.organizationId, notification);
                if (notifcation == null)
                {
                    _logger.LogInformation("Notification not created for organizationId: {OrganizationId}", Originalnotification.organizationId);
                    return new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "Notification not created",
                        StatusCode = (int)HttpStatusCode.OK,
                        Data = false
                    };
                }
            }
            else
            {
                // If no original notification exists (e.g., approved/rejected from pending approvals section),
                // get leave request details to find the employee and create notification
                _logger.LogInformation("No original notification found. Fetching leave request details for RequestId: {RequestId}", requestId);
                var leaveRequest = await _leaveRepository.GetLeaveRequestByIdAsync(requestId);
                if (leaveRequest != null)
                {
                    // Get employee userId and organizationId from employeeId
                    _logger.LogInformation("Getting employee user info for EmployeeId: {EmployeeId}", leaveRequest.EmployeeId);

                    var employeeInfo = await _notificationRepository.GetEmployeeUserInfoAsync(leaveRequest.EmployeeId);
                    if (employeeInfo.HasValue)
                    {
                        var notification = new NotificationCreateDto
                        {
                            Receiveruserid = employeeInfo.Value.userId, // Employee who requested leave (userId)
                            Senderuserid = empId, // Manager/HR who approved/rejected (userId)
                            Ttitle = "Leave Request",
                            RequestId = requestId,
                            Message = message,
                            Notificationtype = "Message",
                            Createdby = empId,
                            moduletype = "Leave",
                            Redirecturl = "/leave/my-leaves",
                            organizationId = employeeInfo.Value.organizationId
                        };
                        _logger.LogInformation("Creating notification for organizationId: {OrganizationId}", employeeInfo.Value.organizationId);
                        var notifcation = await _notificationRepository.CreateNotificationAsync(employeeInfo.Value.organizationId, notification);
                        return new ServiceResponse<bool>
                        {
                            Success = true,
                            Message = "Leave request updated and notification sent",
                            StatusCode = (int)HttpStatusCode.OK,
                            Data = true
                        };
                    }
                }
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Leave request updated successfully",
                StatusCode = (int)HttpStatusCode.OK,
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating leave request status for RequestId: {RequestId}", requestId);
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "An unexpected error occurred while updating leave request",
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Data = false
            };
        }
    }



    public async Task<ServiceResponse<PagedResult<LeaveRequestDto>>> GetLeaveRequestsAsync(Guid userId, LeaveRequestSearchViewModel searchModel, Guid organizationId)
    {
        try
        {

            _logger.LogInformation("Getting Employeeid");
            Guid? empid = await _leaveRepository.GetEmpId(userId);
            if (empid == null)
            {
                _logger.LogInformation("Employee not Found");
                return new ServiceResponse<PagedResult<LeaveRequestDto>>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Employee Not found ",
                    Data = null
                };

            }

            searchModel.EmployeeId = empid;
            var result = await _leaveRepository.GetLeaveRequestsAsync(
                 searchModel.EmployeeId,
                 organizationId,
                 searchModel.Status,
                 searchModel.Page,
                 searchModel.PageSize);

            if (result == null)
            {
                return new ServiceResponse<PagedResult<LeaveRequestDto>>
                {
                    Success = true,
                    StatusCode = (int)HttpStatusCode.OK,
                    Message = "No request Found ",
                    Data = result
                };
            }
         
            return new ServiceResponse<PagedResult<LeaveRequestDto>>
            {
                Success = true,
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Leave Fetched Sucessfully",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching leave requests");

            return new ServiceResponse<PagedResult<LeaveRequestDto>>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An unexpected error occurred",
                Data = null
            };
        }
    }


    public async Task<ServiceResponse<PagedResult<LeaveRequestDto>>>GetLeaveRequestsByOrganizationAsync(LeaveRequestSearchViewModel searchModel,
        Guid organizationId)
    {
        try
        {
            _logger.LogInformation(
                "Getting Leave Requests By Organization. OrganizationId: {organizationId}",
                organizationId);

            var result = await _leaveRepository.GetLeaveRequestsByOrganizationAsync(
                organizationId,
                searchModel.Status,
                searchModel.Page,
                searchModel.PageSize);

            _logger.LogInformation("Leave Requests fetched successfully");

            return new ServiceResponse<PagedResult<LeaveRequestDto>>
            {
                Success = true,
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Leave requests retrieved successfully",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error while fetching leave requests by organization. OrganizationId: {organizationId}",
                organizationId);

            return new ServiceResponse<PagedResult<LeaveRequestDto>>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An unexpected error occurred",
                Data = null
            };
        }
    }


    public async Task<ServiceResponse<List<LeaveTypeDto>>> GetLeaveTypesAsync(Guid organizationId)
    {
        try
        {
            _logger.LogInformation(
                "Fetching Leave Types for OrganizationId: {OrganizationId}",
                organizationId);

            var leaveTypes =
                await _leaveRepository.GetLeaveTypesAsync(organizationId);

            return new ServiceResponse<List<LeaveTypeDto>>
            {
                Success = true,
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Leave types fetched successfully",
                Data = leaveTypes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error fetching leave types for OrganizationId: {OrganizationId}",
                organizationId);

            return new ServiceResponse<List<LeaveTypeDto>>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An unexpected error occurred while retrieving leave types",
                Data = null
            };
        }
    }



    public async Task<ServiceResponse<Guid>> CreateLeaveTypeAsync(Guid organizationId, CreateLeaveTypeRequest request)
    {
        try
        {
            var leaveTypeId = Guid.NewGuid();
            _logger.LogInformation("Creating Leave DTO}");

            var dto = new LeaveTypeDto
            {
                LeaveTypeId = leaveTypeId,
                TypeName = request.TypeName,
                Description = request.Description,
                MaxDaysPerYear = request.MaxDaysPerYear,
                IsPaid = request.IsPaid,
                CarryForwardAllowed = request.CarryForwardAllowed,
                MaxCarryForwardDays = request.MaxCarryForwardDays,
                Color = request.Color,
                IsActive = true
            };

            _logger.LogInformation("Creating Leave Request}");

            var result = await _leaveRepository.CreateLeaveTypeAsync(organizationId, dto);
            if (result)
            {
                return new ServiceResponse<Guid>
                {
                    Success = true,
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Leave type created successfully",
                    Data = leaveTypeId
                };
            }
            else
            {
                _logger.LogWarning(
                    "Failed to create Leave Type for OrganizationId: {OrganizationId}",
                    organizationId);

                return new ServiceResponse<Guid>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = "Failed to create leave type",
                    Data = Guid.Empty
                };
            }
        }

        catch (Exception ex)
        {
            _logger.LogError(ex,"Error creating Leave Type for OrganizationId: {OrganizationId}",organizationId);

            return new ServiceResponse<Guid>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An unexpected error occurred while creating leave type",
                Data = Guid.Empty
            };
        }

    }

    public async Task<ServiceResponse<bool>> EditLeaveTypeAsync(Guid organizationId,Guid leaveTypeId,EditLeaveTypeRequest request)
    {
        try
        {
            if (leaveTypeId == Guid.Empty)
            {

                return new ServiceResponse<bool>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Invalid LeaveTypeId",
                    Data = false
                };

            }

            // 🔹 BUSINESS RULE ENFORCEMENT (MUST BE HERE)
            if (!request.CarryForwardAllowed)
                request.MaxCarryForwardDays = 0;

            if (request.MaxCarryForwardDays < 0)
                request.MaxCarryForwardDays = 0;

            var dto = new LeaveTypeDto
            {
                LeaveTypeId = leaveTypeId,
                TypeName = request.TypeName,
                Description = request.Description,
                MaxDaysPerYear = request.MaxDaysPerYear,
                IsPaid = request.IsPaid,
                CarryForwardAllowed = request.CarryForwardAllowed,
                MaxCarryForwardDays = request.MaxCarryForwardDays,
                Color = request.Color,
                IsActive = request.IsActive
            };

            _logger.LogInformation("Editing Leave Type. LeaveTypeId: {LeaveTypeId}, OrganizationId: {OrgId}", leaveTypeId, organizationId);


            var updated = await _leaveRepository.EditLeaveTypeAsync(organizationId, dto);
            return new ServiceResponse<bool>
            {
                Success = updated,
                StatusCode = updated ? (int)HttpStatusCode.OK : (int)HttpStatusCode.InternalServerError 
,
                Message = updated ? "Leave type updated successfully" : "Failed to update leave type",
                Data = updated
            };
        }
        catch(Exception ex)
        {
            _logger.LogError(ex,"Error editing Leave Type. LeaveTypeId: {LeaveTypeId}, OrganizationId: {OrganizationId}",leaveTypeId,organizationId);

            return new ServiceResponse<bool>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An unexpected error occurred while editing leave type",
                Data = false
            };
        }

    }

    public async Task<ServiceResponse<List<LeaveEntitlementDto>>> GetLeaveEntitlementsAsync(Guid employeeId, int year)
    {
        try {
            _logger.LogInformation("GetLeaveEntitlementsAsync | EmpId: {EmpId}, Year: {Year}", employeeId, year);

            var entitlements = await _leaveRepository.GetLeaveEntitlementsAsync(employeeId, year);
            return new ServiceResponse<List<LeaveEntitlementDto>>
            {
                Success = true,
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Elementals get sucessfully",
                Data = entitlements
            };
        }
        catch(Exception ex)
        {
            _logger.LogInformation("FAILED to get leve elemental");

            return new ServiceResponse<List<LeaveEntitlementDto>>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "Failed getting elemental",
                Data = null
            };
        }

    }


    public async Task<ServiceResponse<List<LeaveBalanceDto>>> GetLeaveBalanceAsync(Guid employeeId,int year)
    {
        try
        {
            _logger.LogInformation("Fetching Leave Balance. EmployeeId: {EmployeeId}, Year: {Year}", employeeId, year);
            var idd = Guid.Parse("5579f3d7-5f3c-48b6-9927-54cf9e9edf59");
            var empId = await _leaveRepository.GetEmpId(employeeId);
            if (empId == Guid.Empty)
                return new ServiceResponse<List<LeaveBalanceDto>> { Success = false, StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Employee not found", Data = null };

            var balance = await _leaveRepository.GetLeaveBalanceAsync(empId, year);

            return new ServiceResponse<List<LeaveBalanceDto>> { Success = true, StatusCode = (int)HttpStatusCode.OK, 
                Message = "Leave balance fetched successfully", Data = balance };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Leave Balance. EmployeeId: {EmployeeId}, Year: {Year}", employeeId, year);

            return new ServiceResponse<List<LeaveBalanceDto>> { Success = false, StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An unexpected error occurred while retrieving leave balance", Data = null };
        }
    }





    public async Task<ServiceResponse<LeaveRequestDto?>> GetLeaveRequestByIdAsync(Guid requestId)
    {
        try
        {
            _logger.LogInformation("Getting Leave Request for  requests id : {requestId}", requestId);
            var leaveRequest = await _leaveRepository.GetLeaveRequestByIdAsync(requestId);
            if (leaveRequest == null)
            {
                _logger.LogInformation("no Leave Request Found ");

                return new ServiceResponse<LeaveRequestDto?>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Leave Request not Found",
                    Data = null,

                };
            }

            var result = _mapper.Map<LeaveRequestDto>(leaveRequest);

            _logger.LogInformation("Leave Request fetched successfully. RequestId: {requestId}", requestId);

            return new ServiceResponse<LeaveRequestDto?>
            {
                Success = true,
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Leave request retrieved successfully",
                Data = result
            };


        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
          "Error while fetching leave request. RequestId: {requestId}",
          requestId);

            return new ServiceResponse<LeaveRequestDto?>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An unexpected error occurred",
                Data = null
            };

        }

    }

    //public async Task<LeaveRequestDto?> UpdateLeaveRequestAsync(Guid requestId, UpdateLeaveRequest request, Guid employeeId)
    //{
    //    try
    //    {

    //    }
    //    catch (Exception ex)
    //    {
    //    }
    //    // First check if the request exists and is pending
    //    var existingRequest = await _leaveRepository.GetLeaveRequestByIdAsync(requestId);
    //    if (existingRequest == null)
    //    {
    //        throw new InvalidOperationException("Leave request not found");
    //    }

    //    if (existingRequest.Status.ToLower() != "pending")
    //    {
    //        throw new InvalidOperationException("Only pending leave requests can be updated");
    //    }

    //    // Verify the request belongs to the employee
    //    var empId = await _leaveRepository.GetEmpId(employeeId);
    //    if (existingRequest.EmployeeId != empId)
    //    {
    //        throw new UnauthorizedAccessException("You can only update your own leave requests");
    //    }

    //    // Check for overlapping leave requests (excluding current request)
    //    var hasOverlap = await _leaveRepository.HasOverlappingLeaveAsync(empId, request.StartDate, request.EndDate, requestId);
    //    if (hasOverlap)
    //    {
    //        throw new InvalidOperationException("Leave request overlaps with existing leave");
    //    }

    //    // Calculate days requested
    //    var daysRequested = (decimal)(request.EndDate - request.StartDate).TotalDays + 1;

    //    // Update the request
    //    var updated = await _leaveRepository.UpdateLeaveRequestAsync(
    //        requestId,
    //        request.LeaveTypeId,
    //        request.StartDate,
    //        request.EndDate,
    //        daysRequested,
    //        request.Reason
    //    );

    //    if (!updated)
    //    {
    //        throw new InvalidOperationException("Failed to update leave request");
    //    }

    //    // Return updated request
    //    var updatedRequest = await _leaveRepository.GetLeaveRequestByIdAsync(requestId);
    //    return updatedRequest != null ? _mapper.Map<LeaveRequestDto>(updatedRequest) : null;
    //}




    public async Task<ServiceResponse<LeaveRequestDto?>> UpdateLeaveRequestAsync(Guid requestId, UpdateLeaveRequest request, Guid employeeId)
    {
        try
        {
            // First check if the request exists and is pending
            _logger.LogInformation("First check if the request exists and is pending");

            var existingRequest = await _leaveRepository.GetLeaveRequestByIdAsync(requestId);
            if (existingRequest == null)
            {
                _logger.LogInformation("No leave request found of this requestid :{requestid}", requestId);
                return new ServiceResponse<LeaveRequestDto?>
                {
                    Success = false,StatusCode = (int)HttpStatusCode.NotFound , Message = "Leave request not found",Data = null
                };
            }

            if (existingRequest.Status.ToLower() != "pending")
            {
                _logger.LogInformation("Only pending leave requests can be updated");
                return new ServiceResponse<LeaveRequestDto?>
                {
                    Success = true,StatusCode = (int)HttpStatusCode.Conflict, Message = "Request must be pending ",Data = null
                };
               
            }

            // Verify the request belongs to the employee

            _logger.LogInformation("Verify the request belongs to the employee");
            var empId = await _leaveRepository.GetEmpId(employeeId);
            if (existingRequest.EmployeeId != empId)
            {
                _logger.LogInformation("You can update only your Leavee  request");
                return new ServiceResponse<LeaveRequestDto?>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = "You can update only your Leave  request",
                    Data = null
                };
            }

            // Check for overlapping leave requests (excluding current request)

            _logger.LogInformation("Checking for leave overlap");
            var hasOverlap = await _leaveRepository.HasOverlappingLeaveAsync(empId, request.StartDate, request.EndDate, requestId);
            if (hasOverlap)
            {
                _logger.LogInformation("You Leave OverLap");

                return new ServiceResponse<LeaveRequestDto?>
                {
                    Success = true,
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = "Leave request overlaps with existing leave",
                    Data = null
                };
            }

            // Calculate days requested
            _logger.LogInformation("Calculating Requested Days");
            var daysRequested = (decimal)(request.EndDate - request.StartDate).TotalDays + 1;

            // Update the request
            _logger.LogInformation("Updating Leave request :{requestid}",requestId);
            var updated = await _leaveRepository.UpdateLeaveRequestAsync(
                requestId,
                request.LeaveTypeId,
                request.StartDate,
                request.EndDate,
                daysRequested,
                request.Reason
            );

            if (!updated)
            {
                _logger.LogInformation("Faled to update leave request");
                return new ServiceResponse<LeaveRequestDto?>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Failed To update Leave Request",
                    Data = null
                };

            }

            // Return updated request
            var updatedRequest = await _leaveRepository.GetLeaveRequestByIdAsync(requestId);
            //updatedRequest != null ? _mapper.Map<LeaveRequestDto>(updatedRequest) : null;
            return new ServiceResponse<LeaveRequestDto?>
            {
                Success = true,
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Leave Request Update Sucessfully",
                Data = updatedRequest == null ? null : _mapper.Map<LeaveRequestDto>(updatedRequest)

            };

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating leave request. RequestId: {RequestId}", requestId);

            return new ServiceResponse<LeaveRequestDto?>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An unexpected error occurred",
                Data = null
            };
        }

    }

    public async Task<ServiceResponse<bool>> CancelLeaveRequestAsync(Guid requestId, Guid employeeId)
    {
        try 
        {
            // First check if the request exists and is pending
            _logger.LogInformation("check if the request exists and is pending");
            var existingRequest = await _leaveRepository.GetLeaveRequestByIdAsync(requestId);

            if (existingRequest == null)
            {
                _logger.LogInformation("Leave request not found");
                return new ServiceResponse<bool>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Leave request not found",
                    Data = false
                };

             
            }

            if (existingRequest.Status.ToLower() != "pending")
            {
                _logger.LogInformation("Leave request is pending ");
                return new ServiceResponse<bool>
                {
                    Success = true,
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = "Only pending leave requests can be cancelled",
                    Data = false
                };
            }

            // Verify the request belongs to the employee
            var empId = await _leaveRepository.GetEmpId(employeeId);
            if (existingRequest.EmployeeId != empId)
            {

                _logger.LogInformation("You can update only your  request");
                return new ServiceResponse<bool>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = "Update only yours Request",
                    Data = false
                };
                throw new UnauthorizedAccessException("You can only cancel your own leave requests");
            }

            // Cancel the request
            var cancelled=  await _leaveRepository.CancelLeaveRequestAsync(requestId);
            if (!cancelled)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = "Failed to cancel leave request",
                    Data = false
                };
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                StatusCode = (int)HttpStatusCode.OK,
                Message = "Leave request cancelled successfully",
                Data = true
            };
        }


        catch (Exception ex) 
        
        {
            _logger.LogError(
           ex,
           "Error cancelling leave request. RequestId: {RequestId}",
           requestId);

            return new ServiceResponse<bool>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An unexpected error occurred",
                Data = false
            };
        }

    }



    public async Task<ServiceResponse<List<LeaveCalendarDto>>> GetLeaveRequestsForCalendarAsync(Guid employeeId, DateTime startDate, DateTime endDate)
    {
        try
        {
            _logger.LogInformation("Fetching Leave Calendar. EmployeeId: {EmployeeId}, StartDate: {StartDate}, EndDate: {EndDate}", employeeId, startDate, endDate);

            _logger.LogInformation("Getting Employee  id");
            var idd = Guid.Parse("5579f3d7-5f3c-48b6-9927-54cf9e9edf59");
            var EMPId = await _leaveRepository.GetEmpId(idd);
            if (EMPId== Guid.Empty)
            {
                return new ServiceResponse<List<LeaveCalendarDto>>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Employee not found",
                    Data = null
                };
            }
            var requests = await _leaveRepository.GetLeaveRequestsAsync(EMPId, startDate, endDate);

            var data = requests.Select(r => new LeaveCalendarDto
            {
                RequestId = r.RequestId,
                EmployeeId = r.EmployeeId,
                LeaveTypeId = r.LeaveTypeId,
                LeaveTypeName = r.LeaveTypeName,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                DaysRequested = r.DaysRequested,
                Status = r.Status,
                Color = GetStatusColor(r.Status),
                Reason = r.Reason,
                SubmittedAt = r.SubmittedAt
            }).ToList();

            return new ServiceResponse<List<LeaveCalendarDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Leave calendar fetched successfully",
                Data = data
            };
        }

        catch (Exception ex)
        {
            return new ServiceResponse<List<LeaveCalendarDto>>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An unexpected error occurred while retrieving leave calendar",
                Data = null
            };
        }        


    }

    private string GetStatusColor(string status)
    {
        return status switch
        {
            "approved" => "#28a745", // Green
            "rejected" => "#dc3545", // Red
            "pending" => "#ffc107", // Yellow
            _ => "#6c757d"           // Gray (default)
        };
    }

    public async Task<ServiceResponse<PagedResult<TeamRemainingLeavesDto>>> GetTeamRemainingLeavesAsync(
        Guid managerUserId,
        Guid organizationId,
        TeamRemainingLeavesSearchViewModel searchModel)
    {
        try
        {
            // Get user role name
            _logger.LogInformation("Getting user Role name");
            var roleName = await _leaveRepository.GetUserRoleNameAsync(managerUserId);

            var year = searchModel.Year ?? DateTime.Now.Year;

            PagedResult<EmployeeLeaveBalanceDto> rawData;

            // Check if user is HR Manager
            _logger.LogInformation("Checking Weather user is Hr or Manager");
            if (roleName != null && roleName.Equals("HR Manager", StringComparison.OrdinalIgnoreCase) || roleName.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
            {
                // HR Manager: Get all employees in the organization
                _logger.LogInformation("Gettin all Emploee for Hr");
                rawData = await _leaveRepository.GetTeamRemainingLeavesByOrganizationAsync(
                    organizationId,
                    year,
                    searchModel.EmployeeName,
                    searchModel.LeaveTypeId,
                    searchModel.Page,
                    searchModel.PageSize,
                    searchModel.SortBy,
                    searchModel.SortOrder);
            }
            else
            {
                // Regular Manager: Get employees reporting to this manager
                var managerEmployeeId = await _leaveRepository.GetEmpId(managerUserId);

                if (managerEmployeeId == Guid.Empty)
                {
                    _logger.LogInformation("Manager idnot found");
                    return new ServiceResponse<PagedResult<TeamRemainingLeavesDto>>
                    {
                        Success = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Manager employee ID not found",
                        Data = null
                    };

                }
                _logger.LogInformation("Gettin all Emploee for Mnager");
                rawData = await _leaveRepository.GetTeamRemainingLeavesAsync(
                    managerEmployeeId,
                    organizationId,
                    year,
                    searchModel.EmployeeName,
                    searchModel.LeaveTypeId,
                    searchModel.Page,
                    searchModel.PageSize,
                    searchModel.SortBy,
                    searchModel.SortOrder);
            }

            // Group by employee and transform to TeamRemainingLeavesDto
            var groupedData = rawData.Data
                .GroupBy(x => new { x.EmployeeId, x.EmployeeName })
                .Select(g => new TeamRemainingLeavesDto
                {
                    EmployeeId = g.Key.EmployeeId,
                    EmployeeName = g.Key.EmployeeName,
                    LeaveBalances = g.Select(x => new LeaveTypeBalanceDto
                    {
                        LeaveTypeId = x.LeaveTypeId,
                        LeaveTypeName = x.LeaveTypeName,
                        TotalDays = x.TotalDays,
                        UsedDays = x.UsedDays,
                        RemainingDays = x.RemainingDays
                    }).ToList()
                })
                .ToList();

            return new ServiceResponse<PagedResult<TeamRemainingLeavesDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Team remaining leaves fetched successfully",
                Data = new PagedResult<TeamRemainingLeavesDto>
                {
                    Data = groupedData,
                    TotalCount = rawData.TotalCount,
                    Page = rawData.Page,
                    PageSize = rawData.PageSize
                }
            };

        }
        catch(Exception ex) 
        {
            _logger.LogError(ex,"Error fetching Team Remaining Leaves. ManagerUserId: {ManagerUserId}, OrganizationId: {OrganizationId}", managerUserId, organizationId);

            return new ServiceResponse<PagedResult<TeamRemainingLeavesDto>>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An unexpected error occurred while fetching team remaining leaves",
                Data = null
            };

        }
       
    }

    public async Task<ServiceResponse<bool>> HasOverlappingLeaveAsync(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeRequestId = null)
    {
        try
        {
            _logger.LogInformation("Getting Current Employee");
            var empId = await _leaveRepository.GetEmpId(employeeId);
            if (empId == Guid.Empty)
            {
                _logger.LogInformation("Employee not found");
                return new ServiceResponse<bool>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Employeee not found",
                    Data = false,
                };
            }
            _logger.LogInformation("Checkcing Overlapping ");


            var result = await _leaveRepository.HasOverlappingLeaveAsync(empId, startDate, endDate, excludeRequestId);
            if (result == false)
            {
                _logger.LogInformation("No leave Overlap");
                return new ServiceResponse<bool>
                {
                    Success = true,
                    StatusCode = (int)HttpStatusCode.OK,
                    Message = "No Overlap",
                    Data = result,
                };
            }
            else
            {
                _logger.LogInformation("Leave overlap");
                return new ServiceResponse<bool>
                {
                    Success = false,
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = "Leave Overlap with existing Leave",
                    Data = result,
                };

            }

        }
        catch (Exception ex)
        {
            return new ServiceResponse<bool>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "Internal server error",
                Data = false,
            };
        }

    }




    //public async Task<bool> HasOverlappingLeaveAsync(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeRequestId = null)
    //{
    //    var empId = await _leaveRepository.GetEmpId(employeeId);
    //    return await _leaveRepository.HasOverlappingLeaveAsync(empId, startDate, endDate, excludeRequestId);
    //}



    private static int CalculateWorkingDays(DateTime startDate, DateTime endDate)
    {
        int workingDays = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday &&
                date.DayOfWeek != DayOfWeek.Sunday)
            {
                workingDays++;
            }
        }

        return workingDays;
    }

}
