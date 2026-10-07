using System.Text;
using CVPlatform.Application.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CVPlatform.Infrastructure.Services;

public class SupportTicketWebhookNotifier : ISupportTicketWebhookNotifier
{
    private readonly HttpClient _httpClient;
    private readonly DropboxOptions _options;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SupportTicketWebhookNotifier> _logger;

    public SupportTicketWebhookNotifier(
        HttpClient httpClient,
        IOptions<DropboxOptions> options,
        IConfiguration configuration,
        ILogger<SupportTicketWebhookNotifier> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _options = options.Value;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task NotifyAsync(string jsonPayload, CancellationToken ct = default)
    {
        var webhookUrl = _options.SupportTicketWebhookUrl;
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            webhookUrl = _configuration["Dropbox:SupportTicketWebhookUrl"]
                ?? _configuration["SupportTicketWebhookUrl"]
                ?? _configuration["NotificationSettings:SupportTicketWebhookUrl"];
        }

        if (string.IsNullOrWhiteSpace(webhookUrl) || webhookUrl.StartsWith("PUT_"))
        {
            return;
        }

        try
        {
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(webhookUrl.Trim(), content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Support ticket webhook notification returned status code {StatusCode}: {ErrorBody}", (int)response.StatusCode, errorBody);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Support ticket webhook notification was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send support ticket webhook notification to {WebhookUrl}.", webhookUrl);
        }
    }
}
