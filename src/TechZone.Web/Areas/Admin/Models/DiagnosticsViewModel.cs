namespace TechZone.Web.Areas.Admin.Models;

public class DiagnosticsViewModel
{
    // Health Metrics
    public bool IsMongoDbHealthy { get; set; }
    public double MongoDbPingMs { get; set; }

    public bool IsRedisHealthy { get; set; }
    public double RedisPingMs { get; set; }

    public double MemoryUsageMb { get; set; }
    public TimeSpan ServerUptime { get; set; }
    public string UptimeFormatted => $"{ServerUptime.Days} ngày {ServerUptime.Hours} giờ {ServerUptime.Minutes} phút";

    public string MachineName { get; set; } = string.Empty;
    public string OsDescription { get; set; } = string.Empty;
    public string DotNetVersion { get; set; } = string.Empty;

    // Log Statistics
    public int TotalLogsCount => LogEntries.Count;
    public int ErrorLogsCount => LogEntries.Count(l => l.Level.Equals("ERR", StringComparison.OrdinalIgnoreCase) || l.Level.Equals("FTL", StringComparison.OrdinalIgnoreCase));
    public int WarningLogsCount => LogEntries.Count(l => l.Level.Equals("WRN", StringComparison.OrdinalIgnoreCase));
    public int InfoLogsCount => LogEntries.Count(l => l.Level.Equals("INF", StringComparison.OrdinalIgnoreCase));

    // Active Filters
    public string CurrentLevel { get; set; } = "all";
    public string? SearchKeyword { get; set; }

    // Log Records
    public List<LogEntryItem> LogEntries { get; set; } = new();
}

public class LogEntryItem
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "INF";
    public string CorrelationId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
}
