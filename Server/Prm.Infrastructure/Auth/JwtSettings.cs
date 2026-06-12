namespace Prm.Infrastructure.Auth;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "PrmApi";
    public string Audience { get; set; } = "PrmClient";
    public int ExpirationHours { get; set; } = 8;
}
