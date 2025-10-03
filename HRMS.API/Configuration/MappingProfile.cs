using AutoMapper;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;

namespace HRMS.API.Configuration;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>();
        CreateMap<CreateUserRequest, User>();
        CreateMap<User, AuthResponse>()
            .ForMember(dest => dest.Token, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshToken, opt => opt.Ignore());

        // Organization mappings
        CreateMap<Organization, OrganizationDTO>();
            CreateMap<CreateOrganizationDTO, Organization>();
            CreateMap<UpdateOrganizationDTO, Organization>();

        // Employee mappings
        CreateMap<Employee, EmployeeDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
        CreateMap<CreateEmployeeRequest, Employee>()
            .ForMember(dest => dest.EmployeeNumber, opt => opt.MapFrom(src => src.EmployeeNumber));
        CreateMap<UpdateEmployeeRequest, Employee>();
        CreateMap<Employee, EmployeeListItemDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));

        // Department mappings
        CreateMap<Department, DepartmentDto>();
        CreateMap<CreateDepartmentRequest, Department>();

        // Position mappings
        CreateMap<Position, PositionDto>();
        CreateMap<CreatePositionRequest, Position>();

        // Attendance mappings
        CreateMap<Attendance, AttendanceDto>();
        CreateMap<CreateAttendanceRequest, Attendance>();

        // Leave mappings
        CreateMap<LeaveRequest, LeaveRequestDto>();
        CreateMap<CreateLeaveRequest, LeaveRequest>();
        CreateMap<LeaveType, LeaveTypeDto>();
        CreateMap<LeaveEntitlement, LeaveEntitlementDto>();

        // Payroll mappings
        CreateMap<PayrollPeriod, PayrollPeriodDto>();
        CreateMap<CreatePayrollPeriodRequest, PayrollPeriod>();
        CreateMap<PayrollEntry, PayrollEntryDto>();

        // Performance mappings
        CreateMap<SkillSet, SkillSetDto>();
        CreateMap<EmployeeSkill, EmployeeSkillDto>();
        CreateMap<AppraisalCycle, AppraisalCycleDto>();
        CreateMap<EmployeeAppraisal, EmployeeAppraisalDto>();

        // Role mappings
        CreateMap<Role, RoleDto>();
        CreateMap<CreateRoleRequest, Role>();

        // Additional attendance mappings
        CreateMap<ManualAttendanceRequest, Attendance>();
    }
}
