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
namespace HRMS.API.Services.Interfaces
{
    public interface IIpAddressService
    {
        Task<List<IpAddress>> GetAllIpAddressesAsync();
        Task<IpAddress> GetIpByIdAsync(Guid id);
        Task<bool> CreateIpAddressAsync(IpAddress dto);
        Task<bool> UpdateIpAddressAsync(Guid id, IpAddress dto);
        Task<bool> DeleteIpAddressAsync(Guid id);
    }

}
