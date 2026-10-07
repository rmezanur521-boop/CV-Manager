namespace CVPlatform.Application.Support;

public interface ISupportTicketWebhookNotifier
{
    Task NotifyAsync(string jsonPayload, CancellationToken ct = default);
}
