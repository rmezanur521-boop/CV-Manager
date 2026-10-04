namespace CVPlatform.Application.Crm;

public interface ISalesforceService
{
    Task<CrmSyncResultDto> SyncUserAsync(string userId, CrmSyncRequestDto request);
    Task<CrmSyncResultDto> SyncWithCodeAsync(string userId, string code, string callbackUrl, string codeVerifier, CrmSyncRequestDto request);
    string GetAuthorizationUrl(string state, string callbackUrl, string codeChallenge);
    string GenerateCodeVerifier();
    string GenerateCodeChallenge(string codeVerifier);
    Task<CrmSyncResultDto?> GetLatestSyncAsync(string userId);
}

public record CrmSyncRequestDto(string Company, string JobTitle, string Phone, bool MarketingOptIn);

public record CrmSyncResultDto(string SalesforceAccountId, string SalesforceContactId, DateTime SyncedAt)
{
    public string? Company { get; init; }
    public string? JobTitle { get; init; }
    public string? Phone { get; init; }
    public bool MarketingOptIn { get; init; }
}
