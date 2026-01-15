# setup.ps1
# Uso: Execute em PowerShell. Para confiar no certificado o Windows pode pedir permissão de administrador.
$ErrorActionPreference = 'Stop'

Write-Host "== Setup do ambiente de desenvolvimento – MicroondasDigital =="

# Verifica se dotnet está instalado
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet não encontrado. Instale o .NET SDK e tente novamente."
    exit 1
}

Write-Host "1) Gerando e confiando no certificado de desenvolvimento (dotnet dev-certs https --trust)"
try {
    dotnet dev-certs https --trust | Out-Null
    Write-Host "→ Certificado de desenvolvimento confiado (se solicitado, aceite a elevação de privilégio)."
} catch {
    Write-Warning "Falha ao executar 'dotnet dev-certs https --trust'. Você pode executá-lo manualmente."
}

# Definir variáveis de ambiente para a sessão atual do PowerShell
# Altere se sua API usar outra porta/URL
$env:MICROONDAS_API_URL = "http://localhost:5123/"
$env:MICROONDAS_ALLOW_INSECURE = "0"

Write-Host "`n2) Variáveis de ambiente definidas para esta sessão:"
Write-Host "   MICROONDAS_API_URL = $($env:MICROONDAS_API_URL)"
Write-Host "   MICROONDAS_ALLOW_INSECURE = $($env:MICROONDAS_ALLOW_INSECURE)"

Write-Host "`n3) Como iniciar (em terminais separados):"
Write-Host "   API -> dotnet run --project src/Microondas.API/Microondas.API.csproj"
Write-Host "   UI  -> dotnet run --project src/Microondas.UI/Microondas.UI.csproj"

Write-Host "`nObservações:"
Write-Host " - Este script define variáveis somente para a sessão atual. Para persistir, adicione ao seu perfil do PowerShell (Ex: $PROFILE)."
Write-Host " - Se preferir usar HTTP (sem certificado), defina MICROONDAS_API_URL para http://localhost:5123/ antes de rodar a UI."
Write-Host "`n== Pronto =="
