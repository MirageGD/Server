using GDMirage.Server.Features.Accounts.Entities;
using LanguageExt;
using LanguageExt.Common;

namespace GDMirage.Server.Features.Accounts.Services;

public interface ICharacterService
{
    Task SaveAsync(Character character);
    Task<Either<Error, Character>> CreateAsync(string characterName);
    Task<Character?> GetAsync(string characterName);
    Task DeleteAsync(string characterName);
}
