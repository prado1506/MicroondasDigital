# Script de setup do projeto Micro-ondas Digital
# Este script cria a estrutura completa do projeto .NET
# Corrigido para .NET 8.0 (versão detectada)

Write-Host "=== Criando estrutura do projeto Micro-ondas Digital ===" -ForegroundColor Cyan

# Criar diretórios
Write-Host "\nCriando diretórios..." -ForegroundColor Green
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

# Criar projetos (sem -f, .NET 8.0 é o padrão agora)
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
        & dotnet new $project.Template -n $project.Name -o $project.Path
        $result = $LASTEXITCODE
        if ($result -eq 0) {
            & dotnet sln add $csproj
        } else {
            Write-Host "Erro ao criar $($project.Name)" -ForegroundColor Red
        }
    }
}

Write-Host "\n=== Setup concluído! ===" -ForegroundColor Green
Write-Host "\nPróximos passos:" -ForegroundColor Cyan
Write-Host "1. Execute: dotnet restore" -ForegroundColor White
Write-Host "2. Execute: dotnet build" -ForegroundColor White
Write-Host "3. Comece a implementar as camadas" -ForegroundColor White
Write-Host "\nPara adicionar referências entre projetos, use:" -ForegroundColor Cyan
Write-Host "dotnet add src/Microondas.Application/Microondas.Application.csproj reference src/Microondas.Domain/Microondas.Domain.csproj" -ForegroundColor Gray
