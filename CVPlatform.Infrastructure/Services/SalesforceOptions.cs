namespace CVPlatform.Infrastructure.Services;

public class SalesforceOptions
{
    public string LoginUrl { get; set; } = "";
    public string ConsumerKey { get; set; } = "";
    public string ConsumerSecret { get; set; } = "";
    public string Username { get; set; } = "";
    public string PasswordWithToken { get; set; } = "";
    public string ApiVersion { get; set; } = "v59.0";
    public string GrantType { get; set; } = "client_credentials";
    public string CallbackUrl { get; set; } = "";
}
