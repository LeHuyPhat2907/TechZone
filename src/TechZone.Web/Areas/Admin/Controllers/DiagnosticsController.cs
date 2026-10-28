using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using TechZone.Application.Common.Interfaces;
using TechZone.Infrastructure.Data;
using TechZone.Web.Areas.Admin.Models;

namespace TechZone.Web.Areas.Admin.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = "SuperAdmin")]
public class DiagnosticsController : AdminBaseController
{
    private readonly IMongoDbContext _mongoDb;
    private readonly ICacheService _cache;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        IMongoDbContext mongoDb,
        ICacheService cache,
        IWebHostEnvironment env,
        ILogger<DiagnosticsController> logger)
    {
        _mongoDb = mongoDb;
        _cache = cache;
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string level = "all", string? search = null, CancellationToken cancellationToken = default)
    {
        var model = new DiagnosticsViewModel
        {
            CurrentLevel = level,
            SearchKeyword = search,
            MachineName = Environment.MachineName,
            OsDescription = RuntimeInformation.OSDescription,
            DotNetVersion = Environment.Version.ToString()
        };

        // 1. Check MongoDB Health & Latency
        try
        {
            var sw = Stopwatch.StartNew();
            await _mongoDb.Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}", cancellationToken: cancellationToken);
            sw.Stop();
            model.IsMongoDbHealthy = true;
            model.MongoDbPingMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);
        }
        catch (Exception ex)
        {
            model.IsMongoDbHealthy = false;
            _logger.LogWarning(ex, "MongoDB health check ping failed");
        }

        // 2. Check Redis Health & Latency
        try
        {
            var sw = Stopwatch.StartNew();
            await _cache.SetAsync("health:ping", "ok", TimeSpan.FromSeconds(30), cancellationToken);
            var val = await _cache.GetAsync<string>("health:ping", cancellationToken);
            sw.Stop();
            model.IsRedisHealthy = val == "ok";
            model.RedisPingMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);
        }
        catch (Exception ex)
        {
            model.IsRedisHealthy = false;
            _logger.LogWarning(ex, "Redis health check ping failed");
        }

        // 3. Process Memory & Server Uptime
        try
        {
            using var proc = Process.GetCurrentProcess();
            model.MemoryUsageMb = Math.Round(proc.WorkingSet64 / (1024.0 * 1024.0), 1);
            model.ServerUptime = DateTime.UtcNow - proc.StartTime.ToUniversalTime();
        }
        catch
        {
            model.MemoryUsageMb = 0;
            model.ServerUptime = TimeSpan.Zero;
        }

        // 4. Parse Log Entries from Rolling File
        model.LogEntries = ReadLogEntries(level, search);

        return View(model);
    }

    [HttpGet]
    public IActionResult DownloadLog()
    {
        var logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        if (!Directory.Exists(logDir))
        {
            return NotFound("Chưa có tệp nhật ký nào được ghi.");
        }

        var files = Directory.GetFiles(logDir, "techzone-app-*.log");
        if (files.Length == 0)
        {
            return NotFound("Không tìm thấy tệp log.");
        }

        var latestFile = files.OrderByDescending(f => System.IO.File.GetLastWriteTimeUtc(f)).First();
        var fileName = Path.GetFileName(latestFile);

        return PhysicalFile(latestFile, "text/plain", fileName);
    }

    private List<LogEntryItem> ReadLogEntries(string levelFilter, string? searchKeyword)
    {
        var list = new List<LogEntryItem>();
        try
        {
            var logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            if (!Directory.Exists(logDir)) return list;

            var files = Directory.GetFiles(logDir, "techzone-app-*.log");
            if (files.Length == 0) return list;

            var latestFile = files.OrderByDescending(f => System.IO.File.GetLastWriteTimeUtc(f)).First();

            // Read with FileShare.ReadWrite to avoid locking active log writer
            using var fs = new FileStream(latestFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);

            var lines = new List<string>();
            while (reader.ReadLine() is { } line)
            {
                lines.Add(line);
            }

            // Parse last 250 log lines
            // Sample line: 2026-10-14 17:15:30.123 +07:00 [INF] [tz-corr-123456789abc] Starting TechZone Web Application...
            var regex = new Regex(@"^(?<timestamp>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}\.\d{3}\s[+-]\d{2}:\d{2})\s\[(?<level>\w{3})\]\s\[(?<corr>[^\]]*)\]\s(?<msg>.*)$", RegexOptions.Compiled);

            LogEntryItem? currentItem = null;

            for (int i = lines.Count - 1; i >= 0 && list.Count < 200; i--)
            {
                var line = lines[i];
                var match = regex.Match(line);

                if (match.Success)
                {
                    if (DateTime.TryParse(match.Groups["timestamp"].Value, out var dt))
                    {
                        var lvl = match.Groups["level"].Value.ToUpperInvariant();
                        var corr = match.Groups["corr"].Value;
                        var msg = match.Groups["msg"].Value;

                        currentItem = new LogEntryItem
                        {
                            Timestamp = dt,
                            Level = lvl,
                            CorrelationId = corr,
                            Message = msg
                        };

                        // Apply filters
                        if (PassesFilter(currentItem, levelFilter, searchKeyword))
                        {
                            list.Add(currentItem);
                        }
                    }
                }
                else if (currentItem != null && !string.IsNullOrWhiteSpace(line))
                {
                    // Multiline stack trace or details
                    currentItem.Exception = (currentItem.Exception != null ? line + "\n" + currentItem.Exception : line);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading diagnostics log file");
        }

        return list;
    }

    private static bool PassesFilter(LogEntryItem item, string levelFilter, string? searchKeyword)
    {
        // 1. Level filter
        var levelMatch = levelFilter.ToLowerInvariant() switch
        {
            "error" => item.Level is "ERR" or "FTL",
            "warning" => item.Level is "WRN",
            "info" => item.Level is "INF",
            _ => true
        };

        if (!levelMatch) return false;

        // 2. Keyword filter
        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = searchKeyword.Trim().ToLowerInvariant();
            var inMessage = item.Message.ToLowerInvariant().Contains(kw);
            var inCorr = item.CorrelationId.ToLowerInvariant().Contains(kw);
            var inException = item.Exception?.ToLowerInvariant().Contains(kw) ?? false;
            return inMessage || inCorr || inException;
        }

        return true;
    }
}
