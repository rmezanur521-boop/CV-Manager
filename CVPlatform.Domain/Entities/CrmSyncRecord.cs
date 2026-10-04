namespace CVPlatform.Domain.Entities;

public class CrmSyncRecord
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public bool MarketingOptIn { get; set; }
    public string SalesforceAccountId { get; set; } = string.Empty;
    public string SalesforceContactId { get; set; } = string.Empty;
    public DateTime SyncedAt { get; set; }
}
