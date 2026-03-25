using System.Diagnostics.Contracts;
using System.Text;
using System.Text.Json;
using GDMirage.Server.Configuration;
using GDMirage.Server.Features.Accounts.Entities;
using GDMirage.Server.Features.Game.Stats;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Options;

namespace GDMirage.Server.Features.Accounts.Services;

public sealed partial class CharacterService : ICharacterService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly ILogger<CharacterService> _logger;
    private readonly ServerOptions _options;

    public CharacterService(ILogger<CharacterService> logger, IOptions<ServerOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        Directory.CreateDirectory("Characters");
    }

    public async Task SaveAsync(Character character)
    {
        var path = GetCharacterPath(character.Name);

        await using var stream = File.Open(path, FileMode.Create, FileAccess.Write);

        await JsonSerializer.SerializeAsync(stream, character, JsonSerializerOptions);

        LogCharacterSaved(character.Name);
    }

    public async Task<Either<Error, Character>> CreateAsync(string characterName)
    {
        var path = GetCharacterPath(characterName);
        if (File.Exists(path))
        {
            return Error.New(409, "A character with this name already exists.");
        }

        const int defaultStamina = 10;
        const int defaultIntelligence = 10;

        var maxHealth = VitalCalculator.CalculateMaxHealth(defaultStamina, 1);
        var maxMana = VitalCalculator.CalculateMaxMana(defaultIntelligence, 1);

        var character = new Character
        {
            Name = characterName,
            Health = maxHealth,
            Mana = maxMana,
            Strength = 10,
            Stamina = defaultStamina,
            Intelligence = defaultIntelligence,
            Experience = 0,
            Map = _options.StartMap,
            X = _options.StartX,
            Y = _options.StartY
        };

        await using var stream = File.Open(path, FileMode.Create, FileAccess.Write);

        await JsonSerializer.SerializeAsync(stream, character, JsonSerializerOptions);

        LogCharacterCreated(character.Name);

        return character;
    }

    public async Task<Character?> GetAsync(string characterName)
    {
        var path = GetCharacterPath(characterName);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);

        return await JsonSerializer.DeserializeAsync<Character>(stream, JsonSerializerOptions);
    }

    public Task DeleteAsync(string characterName)
    {
        try
        {
            var path = GetCharacterPath(characterName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException<bool>(exception);
        }
    }

    [Pure]
    private static string GetCharacterPath(ReadOnlySpan<char> characterName)
    {
        return Path.Combine("Characters", GetSafeFileName(characterName));
    }

    [Pure]
    private static string GetSafeFileName(ReadOnlySpan<char> characterName)
    {
        var stringBuilder = new StringBuilder();

        foreach (var ch in characterName)
        {
            stringBuilder.Append(char.IsAsciiLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_');
        }

        stringBuilder.Append(".json");

        return stringBuilder.ToString();
    }

    [LoggerMessage(LogLevel.Information, "Character '{CharacterName}' has been saved")]
    partial void LogCharacterSaved(string characterName);

    [LoggerMessage(LogLevel.Information, "Character '{CharacterName}' has been created")]
    partial void LogCharacterCreated(string characterName);
}
