using System.Diagnostics.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using GDMirage.Server.Configuration;
using GDMirage.Server.Features.Accounts.Entities;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GDMirage.Server.Features.Accounts.Services;

public sealed partial class AccountService : IAccountService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly ILogger<AccountService> _logger;
    private readonly IOptions<JwtOptions> _options;

    public AccountService(ILogger<AccountService> logger, IOptions<JwtOptions> options)
    {
        Directory.CreateDirectory("Accounts");

        _logger = logger;
        _options = options;
    }

    public async Task<Either<Error, Account>> CreateAsync(string accountName, string password)
    {
        var path = GetAccountPath(accountName);
        if (File.Exists(path))
        {
            return Error.New(409, "An account with this name already exists.");
        }

        var account = new Account
        {
            Name = accountName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        await using var stream = File.Open(path, FileMode.Create, FileAccess.Write);

        await JsonSerializer.SerializeAsync(stream, account, JsonSerializerOptions);

        LogNewAccountRegistered(accountName, path);

        return account;
    }

    public async Task<Account?> GetAsync(string accountName)
    {
        var path = GetAccountPath(accountName);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);

        return await JsonSerializer.DeserializeAsync<Account>(stream, JsonSerializerOptions);
    }

    private static async Task SaveAsync(Account account)
    {
        var path = GetAccountPath(account.Name);

        await using var stream = File.Open(path, FileMode.Create, FileAccess.Write);

        await JsonSerializer.SerializeAsync(stream, account, JsonSerializerOptions);
    }

    public async Task<bool> AddCharacterToAccountAsync(string accountName, string characterName)
    {
        var account = await GetAsync(accountName);
        if (account is null)
        {
            return false;
        }

        account.Characters.Add(characterName.ToLowerInvariant());

        await SaveAsync(account);

        return true;
    }

    public async Task<bool> RemoveCharacterFromAccountAsync(string accountName, string characterName)
    {
        var account = await GetAsync(accountName);
        if (account is null)
        {
            return false;
        }

        if (!account.Characters.Remove(characterName.ToLowerInvariant()))
        {
            return false;
        }

        await SaveAsync(account);

        return true;
    }

    public string CreateToken(Account account)
    {
        var signingKeyBytes = Encoding.UTF8.GetBytes(_options.Value.SecretKey);
        var signingKey = new SymmetricSecurityKey(signingKeyBytes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, account.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_options.Value.ValidForMinutes),
            Issuer = _options.Value.Issuer,
            Audience = _options.Value.Audience,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    [Pure]
    private static string GetAccountPath(ReadOnlySpan<char> accountName)
    {
        return Path.Combine("Accounts", GetSafeFileName(accountName));
    }

    [Pure]
    private static string GetSafeFileName(ReadOnlySpan<char> accountName)
    {
        var stringBuilder = new StringBuilder();

        foreach (var ch in accountName)
        {
            stringBuilder.Append(char.IsAsciiLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_');
        }

        stringBuilder.Append(".json");

        return stringBuilder.ToString();
    }

    [LoggerMessage(LogLevel.Information, "New account registered (AccountName = '{@accountName}', Path = '{path}')")]
    partial void LogNewAccountRegistered(string accountName, string path);
}
