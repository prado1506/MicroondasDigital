using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microondas.API.Services;
using Microondas.API.Security;
using Microondas.API.Exceptions;

namespace Microondas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TokenService _tokenService;
    private readonly IConfiguration _configuration;

    public AuthController(TokenService tokenService, IConfiguration configuration)
    {
        _tokenService = tokenService;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new StandardError("invalid_request", "Usuário e senha são obrigatórios"));

        // credenciais configuradas em appsettings: Username e PasswordHash (SHA256 hex)
        var cfgUser = _configuration["Auth:Username"];
        var cfgHash = _configuration["Auth:PasswordHash"];

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
}

public record LoginRequest(string Username, string Password);