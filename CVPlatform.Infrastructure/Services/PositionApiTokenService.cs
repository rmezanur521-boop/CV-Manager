using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Application.Positions;
using CVPlatform.Domain.Entities;
using CVPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CVPlatform.Infrastructure.Services;

public class PositionApiTokenService : IPositionApiTokenService
{
    private const string TokenPrefixLiteral = "cvp_live_";
    private readonly ApplicationDbContext _db;

    public PositionApiTokenService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<GeneratedPositionTokenDto> GenerateAsync(int positionId, string userId)
    {
        var positionExists = await _db.Positions.AnyAsync(p => p.Id == positionId);
        if (!positionExists)
        {
            throw new NotFoundException($"Position with ID {positionId} not found.");
        }

        var randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        var encoded = WebEncoders.Base64UrlEncode(randomBytes);
        var plainToken = $"{TokenPrefixLiteral}{encoded}";
        var displayPrefix = plainToken.Substring(0, Math.Min(16, plainToken.Length));

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        var hashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var existingToken = await _db.PositionApiTokens.FirstOrDefaultAsync(t => t.PositionId == positionId);
        var now = DateTime.UtcNow;

        if (existingToken != null)
        {
            existingToken.TokenHash = hashHex;
            existingToken.TokenPrefix = displayPrefix;
            existingToken.CreatedAt = now;
            existingToken.CreatedByUserId = userId;
            existingToken.LastUsedAt = null;
            existingToken.RevokedAt = null;
        }
        else
        {
            var newToken = new PositionApiToken
            {
                PositionId = positionId,
                TokenHash = hashHex,
                TokenPrefix = displayPrefix,
                CreatedAt = now,
                CreatedByUserId = userId,
                LastUsedAt = null,
                RevokedAt = null
            };
            _db.PositionApiTokens.Add(newToken);
        }

        await _db.SaveChangesAsync();

        return new GeneratedPositionTokenDto(plainToken, displayPrefix, now);
    }

    public async Task RevokeAsync(int positionId)
    {
        var token = await _db.PositionApiTokens.FirstOrDefaultAsync(t => t.PositionId == positionId);
        if (token != null && token.RevokedAt == null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<PositionApiTokenStatusDto> GetStatusAsync(int positionId)
    {
        var token = await _db.PositionApiTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.PositionId == positionId);

        if (token == null)
        {
            return new PositionApiTokenStatusDto(false, null, null, null, false, null);
        }

        return new PositionApiTokenStatusDto(
            true,
            token.TokenPrefix,
            token.CreatedAt,
            token.LastUsedAt,
            token.RevokedAt != null,
            token.RevokedAt);
    }

    public async Task<int?> ValidateAsync(string plainToken)
    {
        if (string.IsNullOrWhiteSpace(plainToken))
        {
            return null;
        }

        var incomingHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        var incomingHashHex = Convert.ToHexString(incomingHashBytes).ToLowerInvariant();

        var token = await _db.PositionApiTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == incomingHashHex && t.RevokedAt == null);

        if (token == null)
        {
            return null;
        }

        var storedHashBytes = Convert.FromHexString(token.TokenHash);
        if (!CryptographicOperations.FixedTimeEquals(incomingHashBytes, storedHashBytes))
        {
            return null;
        }

        return token.PositionId;
    }

    public async Task RecordUsageAsync(int positionId)
    {
        var token = await _db.PositionApiTokens.FirstOrDefaultAsync(t => t.PositionId == positionId);
        if (token != null)
        {
            token.LastUsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
