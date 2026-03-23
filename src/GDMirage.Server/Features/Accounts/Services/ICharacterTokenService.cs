namespace GDMirage.Server.Features.Accounts.Services;

public interface ICharacterTokenService
{
    Guid Create(string accountName, string characterName);
    Token? Exchange(Guid tokenGuid);
}
