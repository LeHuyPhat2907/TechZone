using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechZone.Application.Common.Interfaces;
using TechZone.Infrastructure.Configuration;

namespace TechZone.Infrastructure.Services;

public class LocalStorageService : IStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly StorageSettings _settings;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(
        IWebHostEnvironment env,
        IOptions<StorageSettings> settings,
        ILogger<LocalStorageService> logger)
    {
        _env = env;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<FileUploadResult> UploadImageAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        string folder = "products",
        bool applyWatermark = false,
        int? maxWidth = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (stream == null || stream.Length == 0)
            {
                return new FileUploadResult { Success = false, ErrorMessage = "Tệp tin tải lên rỗng." };
            }

            if (stream.Length > _settings.MaxFileSizeBytes)
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Dung lượng tệp vượt quá giới hạn cho phép ({_settings.MaxFileSizeBytes / (1024 * 1024)} MB)."
                };
            }

            var rawExt = Path.GetExtension(originalFileName)?.ToLowerInvariant() ?? "";
            if (!_settings.AllowedExtensions.Contains(rawExt))
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Định dạng tệp '{rawExt}' không được hỗ trợ. Chỉ chấp nhận các định dạng ảnh: {string.Join(", ", _settings.AllowedExtensions)}."
                };
            }

            // Read header bytes to validate Magic Bytes
            var header = new byte[16];
            var bytesRead = await stream.ReadAsync(header.AsMemory(0, Math.Min(16, (int)stream.Length)), cancellationToken);
            stream.Position = 0; // Reset stream pointer

            if (!IsValidImageMagicBytes(header, rawExt))
            {
                _logger.LogWarning("Potential malicious upload detected: File {Name} failed magic bytes inspection.", originalFileName);
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = "Tệp tin không đúng định dạng ảnh hợp lệ hoặc bị lỗi header (Magic Bytes Mismatch)."
                };
            }

            // Sanitize folder (alphanumeric only)
            var safeFolder = new string(folder.Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrWhiteSpace(safeFolder))
            {
                safeFolder = "products";
            }

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadDir = Path.Combine(webRoot, _settings.UploadDirectory, safeFolder);
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            // Generate clean unique filename: tz_yyyyMMdd_HHmmss_guid.ext
            var uniqueFileName = $"tz_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}{rawExt}";
            var destinationFilePath = Path.Combine(uploadDir, uniqueFileName);

            await using (var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            var relativeUrl = $"/{_settings.UploadDirectory}/{safeFolder}/{uniqueFileName}";

            _logger.LogInformation("Image uploaded successfully: {Url} ({Size} bytes, Watermark: {Watermark})",
                relativeUrl, stream.Length, applyWatermark);

            return new FileUploadResult
            {
                Success = true,
                FileUrl = relativeUrl,
                FileName = uniqueFileName,
                ContentType = contentType,
                FileSizeBytes = stream.Length,
                Folder = safeFolder,
                UploadedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload image {Name}", originalFileName);
            return new FileUploadResult
            {
                Success = false,
                ErrorMessage = $"Đã xảy ra lỗi khi lưu tệp tin: {ex.Message}"
            };
        }
    }

    public Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return Task.FromResult(false);

            var cleanUrl = fileUrl.TrimStart('/');
            var expectedPrefix = _settings.UploadDirectory + "/";
            if (!cleanUrl.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Rejected delete request for unauthorized path: {Path}", fileUrl);
                return Task.FromResult(false);
            }

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var normalizedPath = Path.GetFullPath(Path.Combine(webRoot, cleanUrl.Replace('/', Path.DirectorySeparatorChar)));
            var allowedRoot = Path.GetFullPath(Path.Combine(webRoot, _settings.UploadDirectory));

            // Prevent Path Traversal attacks
            if (!normalizedPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt detected in DeleteFile: {Path}", fileUrl);
                return Task.FromResult(false);
            }

            if (File.Exists(normalizedPath))
            {
                File.Delete(normalizedPath);
                _logger.LogInformation("Deleted storage file: {Path}", normalizedPath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {Url}", fileUrl);
            return Task.FromResult(false);
        }
    }

    public Task<IReadOnlyList<FileUploadResult>> GetMediaLibraryAsync(string? folder = null, CancellationToken cancellationToken = default)
    {
        var results = new List<FileUploadResult>();
        try
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var baseUploadDir = Path.Combine(webRoot, _settings.UploadDirectory);

            if (!Directory.Exists(baseUploadDir))
            {
                return Task.FromResult<IReadOnlyList<FileUploadResult>>(results);
            }

            var searchDir = string.IsNullOrWhiteSpace(folder)
                ? baseUploadDir
                : Path.Combine(baseUploadDir, new string(folder.Where(char.IsLetterOrDigit).ToArray()));

            if (!Directory.Exists(searchDir))
            {
                return Task.FromResult<IReadOnlyList<FileUploadResult>>(results);
            }

            var files = Directory.GetFiles(searchDir, "*.*", SearchOption.AllDirectories)
                .Where(f => _settings.AllowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)
                .Take(100);

            foreach (var fi in files)
            {
                var rel = Path.GetRelativePath(webRoot, fi.FullName).Replace('\\', '/');
                var folderName = Path.GetDirectoryName(Path.GetRelativePath(baseUploadDir, fi.FullName))?.Replace('\\', '/') ?? "";

                results.Add(new FileUploadResult
                {
                    Success = true,
                    FileName = fi.Name,
                    FileUrl = "/" + rel,
                    FileSizeBytes = fi.Length,
                    Folder = string.IsNullOrEmpty(folderName) ? "root" : folderName,
                    UploadedAt = fi.CreationTimeUtc,
                    ContentType = GetContentType(fi.Extension)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading media library files");
        }

        return Task.FromResult<IReadOnlyList<FileUploadResult>>(results);
    }

    private static bool IsValidImageMagicBytes(byte[] header, string extension)
    {
        if (header.Length < 4) return false;

        return extension switch
        {
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            ".gif" => header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38,
            ".webp" => header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46, // RIFF
            ".svg" => true, // Text-based vector format
            _ => false
        };
    }

    private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        _ => "application/octet-stream"
    };
}
