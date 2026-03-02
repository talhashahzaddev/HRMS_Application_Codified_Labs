using System;
using System.Collections.Generic;
using System.Threading;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using HRMS.API.Configuration;
using HRMS.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace HRMS.API.Services;

public class CloudinaryService : ICloudinaryService, IFileUploadService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private const string ReceiptsFolder = "hrms/receipts";
    private const string ProfilePicturesFolder = "hrms/profile_pics";

    private static readonly HashSet<string> ReceiptAllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/gif",
        "application/pdf"
    };

    private static readonly HashSet<string> ImageAllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/gif"
    };

    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(IOptions<CloudinarySettings> options, ILogger<CloudinaryService> logger)
    {
        _logger = logger;

        var settings = options.Value ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(settings.CloudName) ||
            string.IsNullOrWhiteSpace(settings.ApiKey) ||
            string.IsNullOrWhiteSpace(settings.ApiSecret))
        {
            throw new InvalidOperationException("Cloudinary settings are not configured properly.");
        }

        var account = new Account(
            settings.CloudName.Trim(),
            settings.ApiKey.Trim(),
            settings.ApiSecret.Trim());

        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<string> UploadProfilePictureAsync(IFormFile file)
    {
        ValidateImageFile(file);

        try
        {
            var url = await UploadImageAsync(file, ProfilePicturesFolder);
            _logger.LogInformation("Profile image uploaded to Cloudinary at {Folder}", ProfilePicturesFolder);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile picture to Cloudinary.");
            throw;
        }
    }

    public Task<string> UploadReceiptAsync(IFormFile file) => UploadFileAsync(file);

    public async Task<string> UploadFileAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        ValidateReceiptFile(file);

        try
        {
            var url = await UploadToCloudinaryAsync(file, ReceiptsFolder, IsPdf(file), cancellationToken);
            _logger.LogInformation("File uploaded to Cloudinary receipts folder.");
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to Cloudinary.");
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            throw new ArgumentException("File URL cannot be null or empty.", nameof(fileUrl));
        }

        try
        {
            var publicId = ExtractPublicIdFromUrl(fileUrl);
            if (string.IsNullOrEmpty(publicId))
            {
                _logger.LogWarning("Could not extract public ID from URL: {Url}", fileUrl);
                return false;
            }

            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            if (result.Result == "ok")
            {
                _logger.LogInformation("File deleted from Cloudinary: {PublicId}", publicId);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to delete file from Cloudinary: {PublicId}. Result: {Result}", publicId, result.Result);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from Cloudinary: {Url}", fileUrl);
            throw;
        }
    }

    private async Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
        return ExtractSecureUrl(result);
    }

    private async Task<string> UploadToCloudinaryAsync(
        IFormFile file,
        string folder,
        bool isPdf,
        CancellationToken cancellationToken)
    {
        using var stream = file.OpenReadStream();

        if (isPdf)
        {
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            return ExtractSecureUrl(result);
        }
        else
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder
            };

            var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            return ExtractSecureUrl(result);
        }
    }

    private static string ExtractSecureUrl(UploadResult result)
    {
        if (result == null)
        {
            throw new InvalidOperationException("Cloudinary did not return an upload result.");
        }

        if (result.Error != null)
        {
            throw new InvalidOperationException(result.Error.Message);
        }

        if (result.SecureUrl == null)
        {
            throw new InvalidOperationException("Cloudinary did not return a secure URL.");
        }

        return result.SecureUrl.AbsoluteUri;
    }

    private static void ValidateReceiptFile(IFormFile file)
    {
        ValidateCommon(file);

        var contentType = file.ContentType?.ToLowerInvariant();
        if (string.IsNullOrEmpty(contentType) || !ReceiptAllowedTypes.Contains(contentType))
        {
            throw new ArgumentException("Allowed formats: JPG, JPEG, PNG, GIF, PDF.");
        }
    }

    private static void ValidateImageFile(IFormFile file)
    {
        ValidateCommon(file);

        var contentType = file.ContentType?.ToLowerInvariant();
        if (string.IsNullOrEmpty(contentType) || !ImageAllowedTypes.Contains(contentType))
        {
            throw new ArgumentException("Profile pictures must be JPG, JPEG, PNG, or GIF.");
        }
    }

    private static void ValidateCommon(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("No file provided.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException("File size must be 5 MB or less.");
        }
    }

    private static bool IsPdf(IFormFile file)
    {
        if (string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractPublicIdFromUrl(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return null;
        }

        try
        {
            // Cloudinary URLs typically follow this pattern:
            // https://res.cloudinary.com/{cloud_name}/image/upload/v{timestamp}/{public_id}.{extension}
            // or for PDFs: https://res.cloudinary.com/{cloud_name}/raw/upload/v{timestamp}/{public_id}.{extension}

            var uri = new Uri(fileUrl);
            var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Find the segment that contains the public ID
            // It's usually after the version segment (v{timestamp})
            for (int i = 0; i < pathSegments.Length; i++)
            {
                if (pathSegments[i].StartsWith("v") && pathSegments[i].Length > 1 && char.IsDigit(pathSegments[i][1]))
                {
                    // The next segment should be the public ID
                    if (i + 1 < pathSegments.Length)
                    {
                        var publicIdWithExtension = pathSegments[i + 1];
                        // Remove file extension if present
                        var publicId = publicIdWithExtension.Split('.')[0];
                        return publicId;
                    }
                }
            }

            // Fallback: try to get the last segment before extension
            var lastSegment = pathSegments.Length > 0 ? pathSegments[pathSegments.Length - 1] : string.Empty;
            return lastSegment.Split('.')[0];
        }
        catch
        {
            return null;
        }
    }
}
