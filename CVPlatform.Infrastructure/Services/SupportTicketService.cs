using System.Text.Json;
using CVPlatform.Application.Support;
using CVPlatform.Domain.Constants;
using CVPlatform.Infrastructure.Identity;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CVPlatform.Infrastructure.Services;

public class SupportTicketService : ISupportTicketService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IFileUploader _fileUploader;
    private readonly ISupportTicketWebhookNotifier _webhookNotifier;
    private readonly DropboxOptions _dropboxOptions;
    private readonly ILogger<SupportTicketService> _logger;

    public SupportTicketService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IFileUploader fileUploader,
        ISupportTicketWebhookNotifier webhookNotifier,
        IOptions<DropboxOptions> dropboxOptions,
        ILogger<SupportTicketService> logger)
    {
        _userManager = userManager;
        _db = db;
        _fileUploader = fileUploader;
        _webhookNotifier = webhookNotifier;
        _dropboxOptions = dropboxOptions.Value;
        _logger = logger;
    }

    public async Task<SupportTicketResult> CreateAsync(SupportTicketRequest request, string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new SupportTicketResult(false, null, "USER_NOT_FOUND");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var highestRole = ResolveHighestRole(roles);

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = user.UserName ?? user.Email ?? "User";
        }

        string? positionTitle = null;
        if (request.PositionId.HasValue && request.PositionId.Value > 0)
        {
            positionTitle = await _db.Positions
                .Where(p => p.Id == request.PositionId.Value)
                .Select(p => p.Title)
                .FirstOrDefaultAsync(ct);
        }

        var adminEmails = await (from ur in _db.UserRoles
                                 join r in _db.Roles on ur.RoleId equals r.Id
                                 join u in _db.Users on ur.UserId equals u.Id
                                 where r.Name == Roles.Administrator && u.EmailConfirmed && u.Email != null
                                 select u.Email!)
                                .AsNoTracking()
                                .Distinct()
                                .ToListAsync(ct);

        if (adminEmails.Count == 0)
        {
            var fallbackAdmin = await (from ur in _db.UserRoles
                                       join r in _db.Roles on ur.RoleId equals r.Id
                                       join u in _db.Users on ur.UserId equals u.Id
                                       where r.Name == Roles.Administrator && u.Email != null
                                       select u.Email!)
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync(ct);

            if (!string.IsNullOrWhiteSpace(fallbackAdmin))
            {
                adminEmails.Add(fallbackAdmin);
            }
            else if (!string.IsNullOrWhiteSpace(_dropboxOptions.SupportFallbackEmail))
            {
                adminEmails.Add(_dropboxOptions.SupportFallbackEmail);
            }
        }

        var ticketId = Guid.NewGuid().ToString();
        var shortTicketId = ticketId.Substring(0, 8);
        var nowUtc = DateTime.UtcNow;

        var payload = new
        {
            ticketId,
            createdAtUtc = nowUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            summary = request.Summary.Trim(),
            priority = request.Priority.ToString(),
            reportedBy = new
            {
                userId = user.Id,
                email = user.Email ?? string.Empty,
                fullName,
                role = highestRole
            },
            context = new
            {
                pageUrl = request.PageUrl,
                positionId = request.PositionId.HasValue && request.PositionId.Value > 0 ? request.PositionId.Value.ToString() : null,
                positionTitle
            },
            recipientEmails = adminEmails.ToArray()
        };

        var jsonContent = JsonSerializer.Serialize(payload, JsonOptions);
        var fileName = $"ticket-{nowUtc:yyyyMMdd-HHmmss}-{shortTicketId}.json";

        var uploadResult = await _fileUploader.UploadJsonAsync(fileName, jsonContent, ct);
        if (!uploadResult.Success)
        {
            _logger.LogError("Failed to upload support ticket {TicketId} to Dropbox: {ErrorCode}", ticketId, uploadResult.ErrorCode);
            return new SupportTicketResult(false, null, uploadResult.ErrorCode);
        }

        try
        {
            await _webhookNotifier.NotifyAsync(jsonContent, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch webhook notification for ticket {TicketId}", ticketId);
        }

        return new SupportTicketResult(true, shortTicketId);
    }

    private static string ResolveHighestRole(IList<string> roles)
    {
        if (roles.Contains(Roles.Administrator))
        {
            return Roles.Administrator;
        }

        if (roles.Contains(Roles.Recruiter))
        {
            return Roles.Recruiter;
        }

        return Roles.Candidate;
    }
}
