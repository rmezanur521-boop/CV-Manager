using CVPlatform.Domain.Enums;

namespace CVPlatform.Application.Common;

public record UploadedFileResult(Guid FileId, string ObjectKey, string PreviewUrl);

public interface IFileStorageService
{
    Task<UploadedFileResult> UploadAsync(
        Stream fileStream,
        string originalFileName,
        long declaredSizeBytes,
        FileAssetCategory category,
        string ownerId,
        string uploadedByUserId);

    string GetPresignedUrl(string objectKey, TimeSpan? expiry = null);
}