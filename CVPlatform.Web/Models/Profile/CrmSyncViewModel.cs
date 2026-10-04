namespace CVPlatform.Web.Models.Profile;

public class CrmSyncViewModel
{
    public string? TargetId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public bool MarketingOptIn { get; set; }

    public string? LastSyncedSalesforceAccountId { get; set; }
    public string? LastSyncedSalesforceContactId { get; set; }
    public DateTime? LastSyncedAt { get; set; }

    public bool IsAlreadySynced => !string.IsNullOrWhiteSpace(LastSyncedSalesforceAccountId);
}
