namespace CVPlatform.Application.Common.Exceptions;

public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
        : base("Someone changed this record while you were editing it. Reload the latest version and try again.")
    {
    }
}