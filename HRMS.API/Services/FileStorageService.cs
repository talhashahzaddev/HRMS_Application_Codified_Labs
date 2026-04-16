namespace HRMS.API.Services
{
    public class FileStorageService : IFileStorageService
    {
        //private readonly IWebHostEnvironment _environment;
        //public FileStorageService(IWebHostEnvironment environment)
        //{
        //    _environment = environment;
        //}
        public async Task<string> UploadFileAsync(IFormFile file, string folderPath)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file), "Uploaded file cannot be null.");

            if (string.IsNullOrEmpty(folderPath))
                throw new ArgumentNullException(nameof(folderPath), "Folder path cannot be null or empty.");

            var filePath = Path.Combine(folderPath, file.FileName);

            // Ensure directory exists
            Directory.CreateDirectory(folderPath);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return filePath;
        }
    }

    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string folder);
    }
}
