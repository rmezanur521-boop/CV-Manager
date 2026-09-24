using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using CVPlatform.Application.Common;
using CVPlatform.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace CVPlatform.Infrastructure.Services;

public class MinioFileStorageService : IFileStorageService
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public MinioFileStorageService(IConfiguration configuration)
    {
        var endpoint = configuration["MinIO:Endpoint"]
            ?? throw new InvalidOperationException("MinIO:Endpoint is not configured.");
        var accessKey = configuration["MinIO:AccessKey"]
            ?? throw new InvalidOperationException("MinIO:AccessKey is not configured.");
        var secretKey = configuration["MinIO:SecretKey"]
            ?? throw new InvalidOperationException("MinIO:SecretKey is not configured.");
        var useSsl = bool.TryParse(configuration["MinIO:UseSSL"], out var ssl) && ssl;

        _bucketName = configuration["MinIO:BucketName"] ?? "cvplatform";

        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            UseHttp = !useSsl
        };

        _client = new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), config);
    }

    public async Task<UploadedFileResult> UploadAsync(
        Stream fileStream,
        string originalFileName,
        long declaredSizeBytes,
        FileAssetCategory category,
        string ownerId,
        string uploadedByUserId)
    {
        if (declaredSizeBytes <= 0 || declaredSizeBytes > MaxFileSizeBytes)
            throw new InvalidOperationException("File size must be between 1 byte and 5MB.");

        var headerBuffer = new byte[16];
        var bytesRead = await fileStream.ReadAsync(headerBuffer, 0, headerBuffer.Length);
        var header = headerBuffer[..bytesRead];

        var detected = FileContentSniffer.Detect(header);
        if (detected is null)
            throw new InvalidOperationException("Unsupported or unrecognized file type.");

        fileStream.Position = 0;

        var fileId = Guid.NewGuid();
        var folder = category switch
        {
            FileAssetCategory.ProfileAttribute => "profiles",
            FileAssetCategory.ProjectImage => "projects",
            _ => "misc"
        };

        var objectKey = $"{folder}/{ownerId}/{fileId}{detected.Value.Extension}";

        await EnsureBucketExistsAsync();

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            InputStream = fileStream,
            ContentType = detected.Value.ContentType,
            AutoCloseStream = false
        };

        await _client.PutObjectAsync(putRequest);

        var previewUrl = GetPresignedUrl(objectKey);

        return new UploadedFileResult(fileId, objectKey, previewUrl);
    }

    public string GetPresignedUrl(string objectKey, TimeSpan? expiry = null)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            Expires = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromMinutes(30)),
            Verb = HttpVerb.GET
        };

        return _client.GetPreSignedURL(request);
    }

    private async Task EnsureBucketExistsAsync()
    {
        var exists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_client, _bucketName);
        if (!exists)
        {
            await _client.PutBucketAsync(new PutBucketRequest { BucketName = _bucketName });
        }
    }
}