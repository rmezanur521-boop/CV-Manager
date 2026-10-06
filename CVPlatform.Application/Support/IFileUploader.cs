namespace CVPlatform.Application.Support;

public record FileUploadResult(bool Success, string? RemotePath = null, string? ErrorCode = null);

public interface IFileUploader
{
    Task<FileUploadResult> UploadJsonAsync(string fileName, string jsonContent, CancellationToken ct = default);
}
