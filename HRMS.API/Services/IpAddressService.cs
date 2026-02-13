using DocumentFormat.OpenXml.Bibliography;
using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Models.ViewModels;
using HRMS.API.Services;
using HRMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

public class IpAddressService : IIpAddressService
{
    private readonly IIpAddressRepository _ipAddressRepository;

    public IpAddressService(IIpAddressRepository ipAddressRepository)
    {
        _ipAddressRepository = ipAddressRepository;
    }

    public async Task<List<IpAddress>> GetAllIpAddressesAsync()
    {
        return await _ipAddressRepository.GetAllAsync();
    }

    public async Task<IpAddress> GetIpByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Invalid IP address ID.");

        var ip = await _ipAddressRepository.GetByIdAsync(id);

        if (ip == null)
            throw new ArgumentException("IP address not found.");

        return ip;
    }

    public async Task<bool> CreateIpAddressAsync(IpAddress dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.IpAddressValue))
            throw new ArgumentException("IP Address is required.");

        // Prevent duplicate IP
        var existing = await _ipAddressRepository.GetByIpAsync(dto.IpAddressValue);
        if (existing != null)
            throw new ArgumentException("This IP address already exists.");

        dto.Id = Guid.NewGuid();
        dto.CreatedAt = DateTime.UtcNow;

        return await _ipAddressRepository.CreateAsync(dto);
    }

    public async Task<bool> UpdateIpAddressAsync(Guid id, IpAddress dto)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Invalid IP address ID.");

        var existing = await _ipAddressRepository.GetByIdAsync(id);
        if (existing == null)
            throw new ArgumentException("IP address not found.");

        existing.IpAddressValue = dto.IpAddressValue;
        existing.Name = dto.Name;

        return await _ipAddressRepository.UpdateAsync(existing);
    }

    public async Task<bool> DeleteIpAddressAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Invalid IP address ID.");

        var existing = await _ipAddressRepository.GetByIdAsync(id);
        if (existing == null)
            throw new ArgumentException("IP address not found.");

        return await _ipAddressRepository.DeleteAsync(id);
    }
}
