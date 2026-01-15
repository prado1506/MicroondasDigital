using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microondas.API.Services;

public class JwtSettings
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpirationMinutes { get; set; } = 60;
}

public class TokenService
{
    private readonly JwtSettings _settings;
    private readonly byte[] _keyBytes;

    public TokenService(IOptions<JwtSettings> opt)
    {
        _settings = opt.Value ?? throw new ArgumentNullException(nameof(opt));

        if (string.IsNullOrWhiteSpace(_settings.Key))
            throw new InvalidOperationException("JWT key não configurada. Defina Jwt:Key via user-secrets ou variável de ambiente.");

        // Mesma lógica de Program.cs: aceita Base64 ou texto puro (UTF8).
        try
        {
            // tenta decodificar Base64 (recomendado)
            _keyBytes = Convert.FromBase64String(_settings.Key);
            if (_keyBytes.Length < 32)
                throw new InvalidOperationException("JWT key em Base64 é fraca (<256 bits). Use 32 bytes ou mais.");
        }
        catch (FormatException)
        {
            // fallback para texto UTF8 (mantendo compatibilidade)
            _keyBytes = Encoding.UTF8.GetBytes(_settings.Key);
            if (_keyBytes.Length < 32)
                throw new InvalidOperationException("JWT key fraca (texto <32 bytes). Forneça uma chave com pelo menos 32 bytes ou use Base64 de 32 bytes.");
        }
    }

    public string CreateToken(string username)
    {
        var key = new SymmetricSecurityKey(_keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username)
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}