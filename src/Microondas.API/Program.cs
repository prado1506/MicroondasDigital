using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microondas.API.Services;
using Microondas.API.Middleware;
using Microondas.Application.Services;
using Microondas.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Bind e registro de settings + TokenService
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<TokenService>();

// Registrar serviços de aplicação e repositórios
builder.Services.AddScoped<ProgramaService>();
builder.Services.AddScoped<AquecimentoService>();
builder.Services.AddSingleton<IProgramaRepository, ProgramaRepository>();
builder.Services.AddSingleton<IAquecimentoRepository, AquecimentoRepository>();

// Controllers e Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Autenticação JWT
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwt = jwtSection.Get<JwtSettings>() ?? new JwtSettings();

// Validação e preparação da chave JWT (aceita Base64 ou texto)
if (string.IsNullOrWhiteSpace(jwt.Key))
    throw new InvalidOperationException("JWT key não configurada. Defina Jwt:Key via user-secrets ou variável de ambiente.");

byte[] keyBytes;
try
{
    // tenta decodificar Base64 (recomendado)
    keyBytes = Convert.FromBase64String(jwt.Key);
    if (keyBytes.Length < 32)
        throw new InvalidOperationException("JWT key em Base64 é fraca (<256 bits). Use 32 bytes ou mais.");
}
catch (FormatException)
{
    // não era Base64, usa UTF8 como fallback (validar tamanho)
    keyBytes = Encoding.UTF8.GetBytes(jwt.Key);
    if (keyBytes.Length < 32)
        throw new InvalidOperationException("JWT key fraca (texto <32 bytes). Forneça uma chave com pelo menos 32 bytes ou use Base64 de 32 bytes.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware centralizado de exceção
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Somente redireciona para HTTPS se houver uma URL HTTPS configurada (evita aviso/erro ao usar perfil HTTP)
if (app.Urls.Any(u => u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Mapear controllers (necessário para ApiController)
app.MapControllers();

app.Run();
