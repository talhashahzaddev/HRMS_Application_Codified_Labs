namespace HRMS.API.Services.Interfaces
{
    public interface ICloudinaryService
    {
        Task<string> UploadProfilePictureAsync(IFormFile file);
        Task<string> UploadReceiptAsync(IFormFile file);
        Task<bool> DeleteFileAsync(string fileUrl);
    }
}
