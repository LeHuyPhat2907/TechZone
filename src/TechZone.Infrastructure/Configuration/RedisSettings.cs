namespace TechZone.Infrastructure.Configuration;

public class RedisSettings
{
    public const string SectionName = "RedisSettings";

    public string ConnectionString { get; set; } = "localhost:6379";
    public string InstanceName { get; set; } = "TechZone:";
    public bool Enabled { get; set; } = true;
}
