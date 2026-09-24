namespace CVPlatform.Domain.Constants;

public static class BuiltInAttributes
{
    public const string FirstName = "First Name";
    public const string LastName = "Last Name";
    public const string Location = "Location";
    public const string PersonalPhoto = "Personal Photo";

    public static readonly string[] RequiredNames = { FirstName, LastName };
}