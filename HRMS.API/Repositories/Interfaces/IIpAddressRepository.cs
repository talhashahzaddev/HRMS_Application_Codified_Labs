using HRMS.API.Models.DTOs;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
namespace HRMS.API.Repositories.Interfaces
{
    public interface IIpAddressRepository
    {
        Task<IReadOnlyList<IpAddress>> GetAllAsync();
        Task<IpAddress> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(IpAddress ipAddress);
        Task<bool> UpdateAsync(IpAddress ipAddress);
        Task<bool> DeleteAsync(Guid id);
        Task<IpAddress> GetByIpAsync(string ipAddressValue);

    }

}
