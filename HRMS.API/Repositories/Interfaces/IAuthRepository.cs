using DocumentFormat.OpenXml.Spreadsheet;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;

namespace HRMS.API.Repositories.Interfaces;

public interface IAuthRepository
{

    ////adding this line here
    Task<string?> GetDomainByOrganizationIdAsync(Guid organizationId);
    Task<Guid> AdminIntoEmployeeDetailsAsync( Guid positionId, Guid employeeId);
    Task<Guid> GetEmployeeIdByEmailAsync(string email);
    Task<Guid> GetPositionByEmailAsync(string email);
    Task<bool> SavePasswordResetTokenAsync(Guid userId, string token, DateTime expiresAt);
    Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token);
    Task<bool> UpdateUserPasswordAsync(Guid userId, string newPasswordHash);
    Task<bool> MarkResetTokenAsUsedAsync(string token);
    Task<bool> ChanegPasswordAsync(Guid userId,string newPasswordHash);

    // Adding changes For Profile now
    Task<Employee?> GetEmployeeByIdAsync(Guid userId);

    Task<bool> UpdateProfile_EmployeeAsync(Guid employeeId, UpdateProfileRequest request);
    Task<bool> UpdateProfile_EmployeeDetailsAsync(Guid employeeId, UpdateProfileRequest request);

    Task<bool> UpdateProfile_UserAsync(Guid userId, UpdateProfileRequest request);
    Task<User?> AuthenticateAsync(string email, string passwordHash);
    Task<string> GetOrganizationDomainAsync(Guid? organizationId);
    Task<User?> GetUserByIdAsync(Guid? userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User> CreateUserAsync(User user);
    Task<Guid> CreatePositionAsync(Guid organizationId, Guid roleId,Guid? departmentId, string? title);
    Task<bool> UpdateRefreshTokenAsync(Guid? userId, string refreshToken, DateTime expiresAt);
    Task<bool> UpdateLastLoginAsync(Guid? userId);
    Task<string> GetHashedPassword(string email);
    //Inserting Admin into Employee table
    Task<bool> adminIntoEmployeeAsync(User createUser);
    //Task<string> GenerateEmployeeCodeAsync()
    Task<Role?> GetRoleByNameAsync(Guid? organizationId, string roleName);
    Task<List<Role>> GetOrganizationRolesAsync(Guid? organizationId);
    Task<Role> CreateRoleAsync(Role role);
    Task<bool> UpdateUserRoleAsync(Guid? userId, Guid? roleId);
    Task<bool> VerifyEmail(string token);
}
