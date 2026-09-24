using CVPlatform.Application.Common;
using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Domain.Entities;
using CVPlatform.Domain.Enums;
using CVPlatform.Infrastructure.Persistence;
using CVPlatform.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CVPlatform.Web.Controllers;

[Authorize]
public class UploadController : Controller
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ApplicationDbContext _db;

    public UploadController(IFileStorageService fileStorageService, ApplicationDbContext db)
    {
        _fileStorageService = fileStorageService;
        _db = db;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Image(IFormFile file, string category, string? ownerId)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        if (!Enum.TryParse<FileAssetCategory>(category, out var parsedCategory))
            return BadRequest(new { message = "Invalid category." });

        var effectiveOwnerId = ownerId ?? User.GetUserId();

        try
        {
            await using var stream = file.OpenReadStream();

            var uploaded = await _fileStorageService.UploadAsync(
                stream,
                file.FileName,
                file.Length,
                parsedCategory,
                effectiveOwnerId,
                User.GetUserId());

            _db.FileAssets.Add(new FileAsset
            {
                Id = uploaded.FileId,
                BucketName = "cvplatform",
                ObjectKey = uploaded.ObjectKey,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                Category = parsedCategory,
                UploadedByUserId = User.GetUserId(),
                UploadedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return Json(new { objectKey = uploaded.ObjectKey, url = uploaded.PreviewUrl });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult ResolveUrl(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return BadRequest();

        var url = _fileStorageService.GetPresignedUrl(objectKey);
        return Json(new { url });
    }
}