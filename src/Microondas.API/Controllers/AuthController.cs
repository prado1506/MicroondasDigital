using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microondas.API.Services;
using Microondas.API.Security;
using Microondas.API.Exceptions;
using System.Text.Json;

namespace Microondas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly string _filePath;

    public AuthController(TokenService tokenService, IConfiguration configuration)
    {
        _tokenService = tokenService;
        _configuration = configuration;
        _filePath = Path.Combine(AppContext.BaseDirectory, "auth_config.json");
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new StandardError("invalid_request", "Usuário e senha são obrigatórios"));

        // tenta carregar credenciais persistidas no disco; se não existir usa appsettings
        (string? cfgUser, string? cfgHash) = ReadCredentials();

        if (string.IsNullOrWhiteSpace(cfgUser) || string.IsNullOrWhiteSpace(cfgHash))
            return StatusCode(500, new StandardError("server_config", "Servidor sem credenciais configuradas"));

        if (!string.Equals(req.Username, cfgUser, StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new StandardError("auth_failed", "Credenciais inválidas"));

        var incomingHash = HashHelper.Sha256Hex(req.Password);
        if (!string.Equals(incomingHash, cfgHash, StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new StandardError("auth_failed", "Credenciais inválidas"));

        var token = _tokenService.CreateToken(req.Username);
        return Ok(new { access_token = token, token_type = "Bearer", expires_in = 60 * 60 });
    }

    // Configurar credenciais: permite gravar hash SHA-256 de senha.
    // Se já existir configuração, somente usuários autenticados poderão alterar (protegido).
    [HttpPost("configurar")]
    public IActionResult Configurar([FromBody] ConfigureRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new StandardError("invalid_request", "Usuário e senha são obrigatórios"));

        // Se já existe configuração, exigir autenticação para alterar
        if (System.IO.File.Exists(_filePath))
        {
            if (!(HttpContext.User.Identity?.IsAuthenticated ?? false))
            {
                return StatusCode(403, new StandardError(
                    "config_exists",
                    "Já existe configuração no servidor. É necessário autenticar-se para alterar as credenciais. " +
                    "Para criar credenciais iniciais, remova o arquivo 'auth_config.json' no diretório da API ou faça login com as credenciais existentes."
                ));
            }
        }

        var hash = HashHelper.Sha256Hex(req.Password);
        var cfg = new PersistedAuth { Username = req.Username, PasswordHash = hash };
        try
        {
            var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(_filePath, json);
            return Ok(new { message = "Credenciais configuradas com sucesso" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new StandardError("io_error", $"Falha ao gravar credenciais: {ex.Message}"));
        }
    }

    // Leitura das credenciais configuradas (arquivo) ou fallback para appsettings
    private (string? Username, string? PasswordHash) ReadCredentials()
    {
        try
        {
            if (System.IO.File.Exists(_filePath))
            {
                var json = System.IO.File.ReadAllText(_filePath);
                var obj = JsonSerializer.Deserialize<PersistedAuth>(json);
                if (obj != null) return (obj.Username, obj.PasswordHash);
            }
        }
        catch
        {
            // ignora e tenta fallback
        }

        var cfgUser = _configuration["Auth:Username"];
        var cfgHash = _configuration["Auth:PasswordHash"];
        return (cfgUser, cfgHash);
    }
}

public record LoginRequest(string Username, string Password);
public record ConfigureRequest(string Username, string Password);

internal class PersistedAuth
{
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}