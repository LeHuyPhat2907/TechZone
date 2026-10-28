using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TechZone.Application.Common.Interfaces;
using TechZone.Infrastructure.Configuration;
using TechZone.Web.Areas.Admin.Models;

namespace TechZone.Web.Areas.Admin.Controllers;

public class MediaController : AdminBaseController
{
    private readonly IStorageService _storageService;
    private readonly StorageSettings _settings;
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        IStorageService storageService,
        IOptions<StorageSettings> settings,
        ILogger<MediaController> logger)
    {
        _storageService = storageService;
        _settings = settings.Value;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string folder = "all", CancellationToken cancellationToken = default)
    {
        var targetFolder = folder.Equals("all", StringComparison.OrdinalIgnoreCase) ? null : folder;
        var files = await _storageService.GetMediaLibraryAsync(targetFolder, cancellationToken);

        var viewModel = new MediaLibraryViewModel
        {
            Files = files,
            CurrentFolder = folder,
            Settings = _settings
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(
        IFormFile? file,
        string folder = "products",
        bool applyWatermark = false,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return Json(new { success = false, message = "Vui lòng chọn tệp tin ảnh để tải lên." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _storageService.UploadImageAsync(
            stream,
            file.FileName,
            file.ContentType,
            folder,
            applyWatermark,
            cancellationToken: cancellationToken);

        if (!result.Success)
        {
            return Json(new { success = false, message = result.ErrorMessage ?? "Không thể lưu tệp tin ảnh." });
        }

        return Json(new
        {
            success = true,
            message = "Tải lên tệp ảnh thành công!",
            file = new
            {
                url = result.FileUrl,
                name = result.FileName,
                size = result.FileSizeFormatted,
                folder = result.Folder,
                uploadedAt = result.UploadedAt.ToString("HH:mm dd/MM/yyyy")
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return Json(new { success = false, message = "Đường dẫn tệp tin không hợp lệ." });
        }

        var deleted = await _storageService.DeleteFileAsync(fileUrl, cancellationToken);
        if (!deleted)
        {
            return Json(new { success = false, message = "Không tìm thấy tệp hoặc không có quyền xóa tệp này." });
        }

        return Json(new { success = true, message = "Đã xóa ảnh vĩnh viễn khỏi hệ thống lưu trữ." });
    }

    [HttpGet]
    public async Task<IActionResult> GetFiles(string folder = "all", CancellationToken cancellationToken = default)
    {
        var targetFolder = folder.Equals("all", StringComparison.OrdinalIgnoreCase) ? null : folder;
        var files = await _storageService.GetMediaLibraryAsync(targetFolder, cancellationToken);

        return Json(files.Select(f => new
        {
            url = f.FileUrl,
            name = f.FileName,
            size = f.FileSizeFormatted,
            folder = f.Folder,
            uploadedAt = f.UploadedAt.ToString("HH:mm dd/MM/yyyy")
        }));
    }
}
