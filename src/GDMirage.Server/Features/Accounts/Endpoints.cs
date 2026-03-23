using System.Security.Claims;
using GDMirage.Server.Features.Accounts.Dtos;
using GDMirage.Server.Features.Accounts.Entities;
using GDMirage.Server.Features.Accounts.Services;
using LanguageExt.Common;

namespace GDMirage.Server.Features.Accounts;

public static class Endpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        var accounts = app.MapGroup("/api/v1").WithTags("Accounts");

        accounts.MapPost("/auth", Auth);
        accounts.MapPost("/register", Register);

        accounts.MapGet("/characters", GetCharacterList)
            .RequireAuthorization();

        accounts.MapPost("/characters", CreateCharacter)
            .RequireAuthorization();

        accounts.MapGet("/characters/{characterName}/token", CreateCharacterToken)
            .RequireAuthorization();

        accounts.MapDelete("/characters/{characterName}", DeleteCharacter)
            .RequireAuthorization();
    }

    private static async Task<IResult> Auth(LoginRequest request, IAccountService accountService)
    {
        var account = await accountService.GetAsync(request.AccountName);
        if (account is null || !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
        {
            return Results.Unauthorized();
        }

        var token = accountService.CreateToken(account);

        return Results.Ok(new LoginResponse(token));
    }

    private static async Task<IResult> Register(CreateAccountRequest request, IAccountService accountService)
    {
        var result = await accountService
            .CreateAsync(request.AccountName, request.Password)
            .MapAsync(x =>
            {
                var token = accountService.CreateToken(x);

                return new CreateAccountResponse(token);
            });

        return result.Match(Results.Ok, error => Results.StatusCode(error.Code));
    }

    private static async Task<IResult> GetCharacterList(ClaimsPrincipal user, IAccountService accountService, ICharacterService characterService)
    {
        var accountName = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountName))
        {
            return Results.Unauthorized();
        }

        var account = await accountService.GetAsync(accountName);
        if (account is null)
        {
            return Results.Unauthorized();
        }

        var characters = new List<CharacterDto>();
        foreach (var characterName in account.Characters)
        {
            var character = await characterService.GetAsync(characterName);
            if (character is not null)
            {
                characters.Add(new CharacterDto(character.Name, character.Level));
            }
        }

        return Results.Ok(new CharacterListDto(characters));
    }

    private static async Task<IResult> CreateCharacter(CreateCharacterRequest request, ClaimsPrincipal user, IAccountService accountService, ICharacterService characterService)
    {
        var accountName = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountName))
        {
            return Results.Unauthorized();
        }

        var account = await accountService.GetAsync(accountName);
        if (account is null)
        {
            return Results.Unauthorized();
        }

        var result = await characterService.CreateAsync(request.CharacterName);
        switch (result.Case)
        {
            case Error error:
                return Results.Conflict(error.Message);

            case Character character:
                var added = await accountService.AddCharacterToAccountAsync(accountName, character.Name);
                if (!added)
                {
                    return Results.Conflict("Failed to add character to account");
                }

                return Results.Ok(new CreateCharacterResponse(
                    new CharacterDto(character.Name, character.Level)));
        }

        return Results.BadRequest("Invalid character creation request");
    }

    private static async Task<IResult> CreateCharacterToken(
        string characterName, ClaimsPrincipal user,
        IAccountService accountService,
        ICharacterService characterService,
        ICharacterTokenService characterTokenService)
    {
        var accountName = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountName))
        {
            return Results.Unauthorized();
        }

        var account = await accountService.GetAsync(accountName);
        if (account is null)
        {
            return Results.Unauthorized();
        }

        var normalizedCharacterName = characterName.ToLowerInvariant();
        if (!account.Characters.Contains(normalizedCharacterName))
        {
            return Results.NotFound("Character not found or does not belong to this account");
        }

        var character = await characterService.GetAsync(normalizedCharacterName);
        if (character is null)
        {
            return Results.NotFound("Character not found");
        }

        var token = characterTokenService.Create(account.Name, character.Name);
        
        return Results.Ok(new CharacterTokenDto(token));
    }

    private static async Task<IResult> DeleteCharacter(string characterName, ClaimsPrincipal user, IAccountService accountService, ICharacterService characterService)
    {
        var accountName = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountName))
        {
            return Results.Unauthorized();
        }

        var account = await accountService.GetAsync(accountName);
        if (account is null)
        {
            return Results.Unauthorized();
        }

        if (await accountService.RemoveCharacterFromAccountAsync(accountName, characterName))
        {
            await characterService.DeleteAsync(characterName);
        }

        return Results.Ok();
    }
}
