using HRMS.API.Models.DTOs;

namespace HRMS.API.Services.Interfaces;

public interface IPlatformAdminAuthService
{
    Task<ServiceResponse<PlatformAdminAuthResponse>> AuthenticateAsync(PlatformAdminLoginRequest request);
}
