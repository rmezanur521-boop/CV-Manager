using System.Net;
using System.Net.Mail;
using CVPlatform.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CVPlatform.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogError("SMTP host not configured. Could not send to {Email}: {Subject}", toEmail, subject);
            throw new InvalidOperationException("SMTP is not configured. Set the 'Smtp' section in appsettings.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Timeout = 15000,
            Credentials = new NetworkCredential(_options.Username, _options.Password.Replace(" ", string.Empty))
        };

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {Email} | Subject: {Subject}", toEmail, subject);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} | Subject: {Subject}", toEmail, subject);
            throw;
        }
    }
}