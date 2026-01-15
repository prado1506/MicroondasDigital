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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Mapear controllers (necessário para ApiController)
app.MapControllers();

app.Run();
