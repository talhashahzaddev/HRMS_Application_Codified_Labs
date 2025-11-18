using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using HRMS.API.Services.Interfaces;

namespace HRMS.API.Services
{
    public class CloudinaryService: ICloudinaryService
    {


        private readonly ILogger<CloudinaryService> _logger;
        private readonly IConfiguration _configuration;

        public CloudinaryService(ILogger<CloudinaryService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }


        public async Task<string> UploadProfilePictureAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file provided for upload.");
                return null;
            }

            try
            {
                // Read Cloudinary configuration inside the method
                var cloudName = _configuration["CloudinarySettings:CloudName"]?.Trim();
                var apiKey = _configuration["CloudinarySettings:ApiKey"];
                var apiSecret = _configuration["CloudinarySettings:ApiSecret"];

                var account = new Account(cloudName, apiKey, apiSecret);
                var cloudinary = new Cloudinary(account);

                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "hrms/profile_pics"
                };

                var result = await cloudinary.UploadAsync(uploadParams);

                _logger.LogInformation("File uploaded successfully to Cloudinary: {Url}", result.SecureUrl);

                return result.SecureUrl.AbsoluteUri;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to Cloudinary");
                return null;
            }
        }

    }
}
