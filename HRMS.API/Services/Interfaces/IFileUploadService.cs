using System.Threading;
using Microsoft.AspNetCore.Http;

namespace HRMS.API.Services.Interfaces;

public interface IFileUploadService
{
    Task<string> UploadFileAsync(IFormFile file, CancellationToken cancellationToken = default);
}
