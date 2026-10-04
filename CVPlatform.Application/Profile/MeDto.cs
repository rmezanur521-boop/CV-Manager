namespace CVPlatform.Application.Profile;

public record MeDto(
    string FirstName,
    string LastName,
    string? Location,
    string? PhotoUrl,
    string ConcurrencyStamp,
    string? Email = null);

public class UpdateMeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? PhotoUrl { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}