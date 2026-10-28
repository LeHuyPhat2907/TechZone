using TechZone.Application.Common.Interfaces;
using TechZone.Infrastructure.Configuration;

namespace TechZone.Web.Areas.Admin.Models;

public class MediaLibraryViewModel
{
    public IReadOnlyList<FileUploadResult> Files { get; set; } = Array.Empty<FileUploadResult>();
    public string CurrentFolder { get; set; } = "all";
    public long TotalFilesCount => Files.Count;
    public long TotalBytes => Files.Sum(f => f.FileSizeBytes);
    public string TotalBytesFormatted => FormatBytes(TotalBytes);
    public StorageSettings Settings { get; set; } = new();

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 MB";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }
}
