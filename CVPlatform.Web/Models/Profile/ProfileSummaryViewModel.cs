namespace CVPlatform.Web.Models.Profile;

public class ProfileSummaryViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? PhotoUrl { get; set; }
    public int TotalCvs { get; set; }
    public int PublishedCvs { get; set; }
    public int AttributesFilled { get; set; }
    public int AttributesTotal { get; set; }
    public string? TargetId { get; set; }
    public bool IsAdminViewingOther { get; set; }

    public string Initials
    {
        get
        {
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var first = parts.Length > 0 ? parts[0][0].ToString() : "";
            var last = parts.Length > 1 ? parts[^1][0].ToString() : "";
            var initials = (first + last).ToUpperInvariant();
            return string.IsNullOrWhiteSpace(initials) ? "?" : initials;
        }
    }
}