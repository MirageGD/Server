using Microsoft.Extensions.Caching.Memory;

namespace GDMirage.Server.Features.Accounts.Services;

public sealed record Token(string AccountName, string CharacterName);

public sealed class CharacterTokenService(IMemoryCache memoryCache) : ICharacterTokenService
{
    private const int TokenExpirationMinutes = 1;


    public Guid Create(string accountName, string characterName)
    {
        var guid = Guid.NewGuid();

        memoryCache.Set(guid, new Token(accountName, characterName), TimeSpan.FromMinutes(TokenExpirationMinutes));

        return guid;
    }

    public Token? Exchange(Guid tokenGuid)
    {
        var token = memoryCache.Get<Token>(tokenGuid);
        if (token is null)
        {
            return null;
        }

        memoryCache.Remove(tokenGuid);

        return token;
    }
}
