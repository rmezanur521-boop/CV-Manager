using CVPlatform.Application.Common.Exceptions;
using CVPlatform.Application.Profile;
using CVPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CVPlatform.Infrastructure.Services;

public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<MeDto> GetMeAsync(string candidateId)
    {
        var user = await _userManager.FindByIdAsync(candidateId)
            ?? throw new NotFoundException("Candidate not found.");

        return ToDto(user);
    }

    public async Task<MeDto> UpdateMeAsync(string candidateId, UpdateMeRequest request)
    {
        var user = await _userManager.FindByIdAsync(candidateId)
            ?? throw new NotFoundException("Candidate not found.");

        if (user.ConcurrencyStamp != request.ConcurrencyStamp)
            throw new ConcurrencyConflictException();

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Location = request.Location;
        user.PhotoUrl = request.PhotoUrl;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new ConcurrencyConflictException();

        return ToDto(user);
    }

    private static MeDto ToDto(ApplicationUser user)
    {
        return new MeDto(user.FirstName, user.LastName, user.Location, user.PhotoUrl, user.ConcurrencyStamp);
    }
}