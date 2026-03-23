namespace GDMirage.Server.Configuration;

public sealed record JwtOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ValidForMinutes { get; set; } = 60;
}
