using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Application.Crm;
using CVPlatform.Domain.Entities;
using CVPlatform.Infrastructure.Identity;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CVPlatform.Infrastructure.Services;

public class SalesforceService : ISalesforceService
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<SalesforceOptions> _options;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public SalesforceService(
        HttpClient httpClient,
        IOptions<SalesforceOptions> options,
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _httpClient = httpClient;
        _options = options;
        _db = db;
        _userManager = userManager;
    }

    public string GetAuthorizationUrl(string state, string callbackUrl, string codeChallenge)
    {
        var options = _options.Value;
        var loginUrl = (options.LoginUrl ?? string.Empty).TrimEnd('/');
        var escapedClientId = Uri.EscapeDataString(options.ConsumerKey);
        var escapedRedirect = Uri.EscapeDataString(callbackUrl);
        var escapedState = Uri.EscapeDataString(state);
        var escapedChallenge = Uri.EscapeDataString(codeChallenge);

        return $"{loginUrl}/services/oauth2/authorize?response_type=code&client_id={escapedClientId}&redirect_uri={escapedRedirect}&state={escapedState}&code_challenge={escapedChallenge}&code_challenge_method=S256";
    }

    public string GenerateCodeVerifier()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    public string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var challengeBytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(challengeBytes);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public async Task<CrmSyncResultDto> SyncWithCodeAsync(string userId, string code, string callbackUrl, string codeVerifier, CrmSyncRequestDto request)
    {
        var options = _options.Value;
        var loginUrl = (options.LoginUrl ?? string.Empty).TrimEnd('/');
        var tokenEndpoint = $"{loginUrl}/services/oauth2/token";

        var tokenParams = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = options.ConsumerKey,
            ["client_secret"] = options.ConsumerSecret,
            ["redirect_uri"] = callbackUrl,
            ["code_verifier"] = codeVerifier
        };

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(tokenParams)
        };

        using var tokenResponse = await _httpClient.SendAsync(tokenRequest);
        var tokenResponseBody = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Salesforce token exchange failed: {(int)tokenResponse.StatusCode} {tokenResponse.ReasonPhrase}. Response: {tokenResponseBody}");
        }

        using var tokenDoc = JsonDocument.Parse(tokenResponseBody);
        var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Salesforce token response did not contain access_token.");
        var instanceUrl = (tokenDoc.RootElement.TryGetProperty("instance_url", out var instProp) ? instProp.GetString() : null) ?? loginUrl;

        instanceUrl = instanceUrl.TrimEnd('/');
        var apiVersion = string.IsNullOrWhiteSpace(options.ApiVersion) ? "v59.0" : options.ApiVersion.Trim('/');

        return await CreateRecordsAndSaveAsync(userId, accessToken, instanceUrl, apiVersion, request);
    }

    public async Task<CrmSyncResultDto> SyncUserAsync(string userId, CrmSyncRequestDto request)
    {
        var options = _options.Value;
        var loginUrl = (options.LoginUrl ?? string.Empty).TrimEnd('/');

        var tokenEndpoint = $"{loginUrl}/services/oauth2/token";
        var tokenParams = new Dictionary<string, string>
        {
            ["client_id"] = options.ConsumerKey,
            ["client_secret"] = options.ConsumerSecret
        };

        if (string.Equals(options.GrantType, "password", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(options.Username)
            && !string.IsNullOrWhiteSpace(options.PasswordWithToken))
        {
            tokenParams["grant_type"] = "password";
            tokenParams["username"] = options.Username;
            tokenParams["password"] = options.PasswordWithToken;
        }
        else
        {
            tokenParams["grant_type"] = "client_credentials";
        }

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(tokenParams)
        };

        using var tokenResponse = await _httpClient.SendAsync(tokenRequest);
        var tokenResponseBody = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Salesforce authentication failed: {(int)tokenResponse.StatusCode} {tokenResponse.ReasonPhrase}. Response: {tokenResponseBody}");
        }

        using var tokenDoc = JsonDocument.Parse(tokenResponseBody);
        var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Salesforce authentication response did not contain access_token.");
        var instanceUrl = (tokenDoc.RootElement.TryGetProperty("instance_url", out var instProp) ? instProp.GetString() : null) ?? loginUrl;

        instanceUrl = instanceUrl.TrimEnd('/');
        var apiVersion = string.IsNullOrWhiteSpace(options.ApiVersion) ? "v59.0" : options.ApiVersion.Trim('/');

        return await CreateRecordsAndSaveAsync(userId, accessToken, instanceUrl, apiVersion, request);
    }

    private async Task<CrmSyncResultDto> CreateRecordsAndSaveAsync(
        string userId,
        string accessToken,
        string instanceUrl,
        string apiVersion,
        CrmSyncRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException($"User with ID '{userId}' was not found.");

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = !string.IsNullOrWhiteSpace(user.Email) ? user.Email : "Unknown";
        }

        var accountEndpoint = $"{instanceUrl}/services/data/{apiVersion}/sobjects/Account/";
        var accountPayload = new { Name = fullName };
        using var accountRequest = new HttpRequestMessage(HttpMethod.Post, accountEndpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(accountPayload), Encoding.UTF8, "application/json")
        };
        accountRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var accountResponse = await _httpClient.SendAsync(accountRequest);
        var accountResponseBody = await accountResponse.Content.ReadAsStringAsync();

        if (!accountResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Salesforce Account creation failed: {(int)accountResponse.StatusCode} {accountResponse.ReasonPhrase}. Response: {accountResponseBody}");
        }

        using var accountDoc = JsonDocument.Parse(accountResponseBody);
        var accountId = accountDoc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Salesforce Account creation response did not contain id.");

        var contactEndpoint = $"{instanceUrl}/services/data/{apiVersion}/sobjects/Contact/";
        var contactPayload = new Dictionary<string, object?>
        {
            ["AccountId"] = accountId,
            ["FirstName"] = string.IsNullOrWhiteSpace(user.FirstName) ? null : user.FirstName,
            ["LastName"] = string.IsNullOrWhiteSpace(user.LastName)
                ? (string.IsNullOrWhiteSpace(user.FirstName) ? "User" : user.FirstName)
                : user.LastName,
            ["Email"] = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email,
            ["Phone"] = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone,
            ["Title"] = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle
        };

        var serializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        using var contactRequest = new HttpRequestMessage(HttpMethod.Post, contactEndpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(contactPayload, serializerOptions), Encoding.UTF8, "application/json")
        };
        contactRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var contactResponse = await _httpClient.SendAsync(contactRequest);
        var contactResponseBody = await contactResponse.Content.ReadAsStringAsync();

        if (!contactResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Salesforce Contact creation failed: {(int)contactResponse.StatusCode} {contactResponse.ReasonPhrase}. Response: {contactResponseBody}");
        }

        using var contactDoc = JsonDocument.Parse(contactResponseBody);
        var contactId = contactDoc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Salesforce Contact creation response did not contain id.");

        var record = await _db.CrmSyncRecords.FirstOrDefaultAsync(r => r.UserId == userId);
        var syncedAt = DateTime.UtcNow;

        if (record is not null)
        {
            record.Company = request.Company;
            record.JobTitle = request.JobTitle;
            record.Phone = request.Phone;
            record.MarketingOptIn = request.MarketingOptIn;
            record.SalesforceAccountId = accountId;
            record.SalesforceContactId = contactId;
            record.SyncedAt = syncedAt;
        }
        else
        {
            record = new CrmSyncRecord
            {
                UserId = userId,
                Company = request.Company,
                JobTitle = request.JobTitle,
                Phone = request.Phone,
                MarketingOptIn = request.MarketingOptIn,
                SalesforceAccountId = accountId,
                SalesforceContactId = contactId,
                SyncedAt = syncedAt
            };
            _db.CrmSyncRecords.Add(record);
        }

        await _db.SaveChangesAsync();

        return new CrmSyncResultDto(accountId, contactId, syncedAt)
        {
            Company = record.Company,
            JobTitle = record.JobTitle,
            Phone = record.Phone,
            MarketingOptIn = record.MarketingOptIn
        };
    }

    public async Task<CrmSyncResultDto?> GetLatestSyncAsync(string userId)
    {
        var record = await _db.CrmSyncRecords
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.SyncedAt)
            .FirstOrDefaultAsync();

        if (record is null)
        {
            return null;
        }

        return new CrmSyncResultDto(record.SalesforceAccountId, record.SalesforceContactId, record.SyncedAt)
        {
            Company = record.Company,
            JobTitle = record.JobTitle,
            Phone = record.Phone,
            MarketingOptIn = record.MarketingOptIn
        };
    }
}
