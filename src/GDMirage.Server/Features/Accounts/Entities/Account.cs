namespace GDMirage.Server.Features.Accounts.Entities;

public sealed record Account
{
    public string Name { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<string> Characters { get; set; } = [];
}
