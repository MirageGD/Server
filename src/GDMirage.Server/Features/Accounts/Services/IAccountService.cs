using GDMirage.Server.Features.Accounts.Entities;
using LanguageExt;
using LanguageExt.Common;

namespace GDMirage.Server.Features.Accounts.Services;

public interface IAccountService
{
    Task<Either<Error, Account>> CreateAsync(string accountName, string password);
    Task<Account?> GetAsync(string accountName);
    Task<bool> AddCharacterToAccountAsync(string accountName, string characterName);
    Task<bool> RemoveCharacterFromAccountAsync(string accountName, string characterName);
    string CreateToken(Account account);
}
