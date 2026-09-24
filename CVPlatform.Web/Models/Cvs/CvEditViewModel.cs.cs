using CVPlatform.Application.Cvs;
using CVPlatform.Domain.Enums;

namespace CVPlatform.Web.Models.Cvs;

public class CvEditViewModel
{
    public int CvId { get; set; }
    public string PositionTitle { get; set; } = string.Empty;
    public CvStatus Status { get; set; }
    public int Version { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<CVPlatform.Web.Models.Shared.AttributeInputViewModel> Attributes { get; set; } = new();
    public IReadOnlyList<CvProjectDto> Projects { get; set; } = new List<CvProjectDto>();
    public int MissingCount => Attributes.Count(a => a.IsMissing);
}