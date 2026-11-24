namespace EnterpriseFlow.Infrastructure.Identity;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Secret { get; set; } = "EnterpriseFlowSuperSecretKey2026!WithSufficientLengthForHmacSha256";
    public string Issuer { get; set; } = "EnterpriseFlowApi";
    public string Audience { get; set; } = "EnterpriseFlowClients";
    public int ExpiryMinutes { get; set; } = 15;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
