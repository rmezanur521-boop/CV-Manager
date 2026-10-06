using System.ComponentModel.DataAnnotations;
using CVPlatform.Application.Support;

namespace CVPlatform.Web.Models.Support;

public class SupportTicketViewModel
{
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Average;

    [Required]
    public string PageUrl { get; set; } = string.Empty;

    public int? PositionId { get; set; }
}
