namespace CVPlatform.Application.Support;

public interface ISupportTicketService
{
    Task<SupportTicketResult> CreateAsync(SupportTicketRequest request, string userId, CancellationToken ct = default);
}
