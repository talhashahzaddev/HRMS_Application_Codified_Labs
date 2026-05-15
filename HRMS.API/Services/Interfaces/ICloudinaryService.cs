namespace HRMS.API.Services.Interfaces
{
    public interface ICloudinaryService
    {
        Task<string> UploadProfilePictureAsync(IFormFile file);
        Task<string> UploadReceiptAsync(IFormFile file);
        Task<string> UploadPayslipPdfFromPathAsync(string filePath, CancellationToken cancellationToken = default);
        Task<bool> DeleteFileAsync(string fileUrl);
    }
}
