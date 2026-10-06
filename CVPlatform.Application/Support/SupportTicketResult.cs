namespace CVPlatform.Application.Support;

public record SupportTicketResult(
    bool Success,
    string? TicketId = null,
    string? ErrorMessage = null);
