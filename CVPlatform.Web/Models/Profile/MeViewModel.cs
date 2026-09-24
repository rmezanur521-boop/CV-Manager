using System.ComponentModel.DataAnnotations;

namespace CVPlatform.Web.Models.Profile;

public class MeViewModel
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Location { get; set; }

    public string? PhotoUrl { get; set; }

    public string ConcurrencyStamp { get; set; } = string.Empty;
}