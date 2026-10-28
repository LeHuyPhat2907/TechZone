namespace TechZone.Infrastructure.Configuration;

public class StorageSettings
{
    public const string SectionName = "StorageSettings";

    public string Provider { get; set; } = "Local"; // "Local", "Cloudinary", "AwsS3"
    public string UploadDirectory { get; set; } = "uploads";
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5 MB
    public string[] AllowedExtensions { get; set; } = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };
    public CloudinarySettings Cloudinary { get; set; } = new();
}

public class CloudinarySettings
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public bool IsEnabled => !string.IsNullOrWhiteSpace(CloudName) && !string.IsNullOrWhiteSpace(ApiKey);
}
