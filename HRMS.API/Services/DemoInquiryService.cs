using HRMS.API.Models.DTOs;
using HRMS.API.Models.Entities;
using HRMS.API.Repositories.Interfaces;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services;

public class DemoInquiryService : IDemoInquiryService
{
    private readonly IDemoInquiryRepository _repository;
    private readonly ILogger<DemoInquiryService> _logger;

    public DemoInquiryService(IDemoInquiryRepository repository, ILogger<DemoInquiryService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ServiceResponse<DemoInquiryDto>> CreateAsync(CreateDemoInquiryRequest request)
    {
        try
        {
            var inquiry = new DemoInquiry
            {
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                CompanyName = request.CompanyName,
                CompanySize = request.CompanySize,
                Industry = request.Industry,
                Country = request.Country,
                Message = request.Message,
                Status = "new",
                Source = "website"
            };

            var inquiryId = await _repository.CreateAsync(inquiry);
            inquiry.InquiryId = inquiryId;

            _logger.LogInformation("Demo inquiry created: {InquiryId} from {Email}", inquiryId, request.Email);

            var dto = new DemoInquiryDto
            {
                InquiryId = inquiryId,
                FullName = inquiry.FullName,
                Email = inquiry.Email,
                Phone = inquiry.Phone,
                CompanyName = inquiry.CompanyName,
                CompanySize = inquiry.CompanySize,
                Industry = inquiry.Industry,
                Country = inquiry.Country,
                Message = inquiry.Message,
                Status = inquiry.Status,
                Source = inquiry.Source,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return new ServiceResponse<DemoInquiryDto>
            {
                Success = true,
                Message = "Inquiry submitted successfully. We will contact you shortly.",
                Data = dto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating demo inquiry for {Email}", request.Email);
            return new ServiceResponse<DemoInquiryDto>
            {
                Success = false,
                Message = "Failed to submit inquiry. Please try again."
            };
        }
    }

    public async Task<ServiceResponse<DemoInquiryDto>> GetByIdAsync(Guid inquiryId)
    {
        var inquiry = await _repository.GetByIdAsync(inquiryId);
        if (inquiry == null)
        {
            return new ServiceResponse<DemoInquiryDto>
            {
                Success = false,
                Message = "Inquiry not found"
            };
        }

        var dto = new DemoInquiryDto
        {
            InquiryId = inquiry.InquiryId,
            FullName = inquiry.FullName,
            Email = inquiry.Email,
            Phone = inquiry.Phone,
            CompanyName = inquiry.CompanyName,
            CompanySize = inquiry.CompanySize,
            Industry = inquiry.Industry,
            Country = inquiry.Country,
            Message = inquiry.Message,
            Status = inquiry.Status,
            Notes = inquiry.Notes,
            Source = inquiry.Source,
            CreatedAt = inquiry.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = inquiry.UpdatedAt ?? DateTime.UtcNow
        };

        return new ServiceResponse<DemoInquiryDto> { Success = true, Data = dto };
    }

    public async Task<ServiceResponse<PagedResult<DemoInquiryDto>>> GetAllPagedAsync(DemoInquiryFilterRequest filter)
    {
        try
        {
            var result = await _repository.GetAllPagedAsync(filter);
            return new ServiceResponse<PagedResult<DemoInquiryDto>> { Success = true, Data = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting demo inquiries");
            return new ServiceResponse<PagedResult<DemoInquiryDto>>
            {
                Success = false,
                Message = "Failed to retrieve inquiries"
            };
        }
    }

    public async Task<ServiceResponse<bool>> UpdateStatusAsync(Guid inquiryId, UpdateDemoInquiryStatusRequest request)
    {
        var validStatuses = new[] { "new", "contacted", "demo_scheduled", "converted", "closed" };
        if (!validStatuses.Contains(request.Status.ToLower()))
        {
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = $"Invalid status. Allowed values: {string.Join(", ", validStatuses)}"
            };
        }

        var success = await _repository.UpdateStatusAsync(inquiryId, request.Status.ToLower(), request.Notes);
        if (!success)
        {
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "Inquiry not found or failed to update"
            };
        }

        _logger.LogInformation("Demo inquiry {InquiryId} status updated to {Status}", inquiryId, request.Status);

        return new ServiceResponse<bool>
        {
            Success = true,
            Data = true,
            Message = "Inquiry status updated successfully"
        };
    }
}
