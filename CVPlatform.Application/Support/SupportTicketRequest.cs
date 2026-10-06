namespace CVPlatform.Application.Support;

public record SupportTicketRequest(
    string Summary,
    SupportTicketPriority Priority,
    string PageUrl,
    int? PositionId = null);
