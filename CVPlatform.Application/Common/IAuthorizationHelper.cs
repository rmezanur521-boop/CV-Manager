namespace CVPlatform.Application.Common;

public interface ICurrentUserContext
{
    bool IsAdmin { get; }
    string UserId { get; }
}