namespace TechZone.Application.Common.Interfaces;

public class FileUploadResult
{
    public bool Success { get; set; }
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public string? FileSizeFormatted => FormatFileSize(FileSizeBytes);
    public string? Folder { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }

    private static string FormatFileSize(long bytes)
    {
        if (bytes <= 0) return "0 B";
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }
}

public interface IStorageService
{
    Task<FileUploadResult> UploadImageAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        string folder = "products",
        bool applyWatermark = false,
        int? maxWidth = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileUploadResult>> GetMediaLibraryAsync(string? folder = null, CancellationToken cancellationToken = default);
}
