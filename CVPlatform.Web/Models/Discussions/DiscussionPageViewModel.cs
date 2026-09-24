using CVPlatform.Application.Discussions;
using CVPlatform.Application.Positions;

namespace CVPlatform.Web.Models.Discussions;

public record DiscussionPageViewModel(PositionDto Position, IReadOnlyList<DiscussionPostDto> Posts);