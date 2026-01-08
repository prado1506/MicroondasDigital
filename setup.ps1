# Script de setup do projeto Micro-ondas Digital
# Este script cria a estrutura completa do projeto .NET

Write-Host "=== Criando estrutura do projeto Micro-ondas Digital ===" -ForegroundColor Cyan

# Criar diretórios
Write-Host "Criando diretórios..." -ForegroundColor Green
$directories = @(
    'src/Microondas.Domain',
    'src/Microondas.Application',
    'src/Microondas.Infrastructure',
    'src/Microondas.API',
    'src/Microondas.UI',
    'tests/Microondas.Tests',
    'docs',
    '.github/workflows'
)

foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "Criado: $dir" -ForegroundColor Yellow
    }
}

# Criar arquivo .gitkeep em cada diretório
foreach ($dir in $directories) {
    $gitkeep = Join-Path $dir '.gitkeep'
    if (-not (Test-Path $gitkeep)) {
        New-Item -ItemType File -Path $gitkeep -Force | Out-Null
    }
}

# Criar solução
Write-Host "\nCriando solução .NET..." -ForegroundColor Green
if (-not (Test-Path 'MicroondasDigital.sln')) {
    & dotnet new sln -n MicroondasDigital
    Write-Host "Solução criada: MicroondasDigital.sln" -ForegroundColor Yellow
}

# Criar projetos
Write-Host "\nCriando projetos .NET..." -ForegroundColor Green
$projects = @(
    @{ Name = 'Microondas.Domain'; Path = 'src/Microondas.Domain'; Template = 'classlib' },
    @{ Name = 'Microondas.Application'; Path = 'src/Microondas.Application'; Template = 'classlib' },
    @{ Name = 'Microondas.Infrastructure'; Path = 'src/Microondas.Infrastructure'; Template = 'classlib' },
    @{ Name = 'Microondas.API'; Path = 'src/Microondas.API'; Template = 'webapi' },
    @{ Name = 'Microondas.UI'; Path = 'src/Microondas.UI'; Template = 'console' },
    @{ Name = 'Microondas.Tests'; Path = 'tests/Microondas.Tests'; Template = 'xunit' }
)

foreach ($project in $projects) {
    $csproj = Join-Path $project.Path "$($project.Name).csproj"
    if (-not (Test-Path $csproj)) {
        Write-Host "Criando projeto: $($project.Name)" -ForegroundColor Yellow
        & dotnet new $project.Template -n $project.Name -o $project.Path -f net6.0
        & dotnet sln add $csproj
    }
}

Write-Host "\n=== Setup concluído com sucesso! ===" -ForegroundColor Green
Write-Host "\nPróximos passos:" -ForegroundColor Cyan
Write-Host "1. Execute: dotnet restore" -ForegroundColor White
Write-Host "2. Execute: dotnet build" -ForegroundColor White
Write-Host "3. Comece a implementar as camadas" -ForegroundColor White
