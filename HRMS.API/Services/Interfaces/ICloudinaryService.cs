namespace HRMS.API.Services.Interfaces
{
    public interface ICloudinaryService
    {
        Task<string> UploadProfilePictureAsync(IFormFile file);
    }
}
