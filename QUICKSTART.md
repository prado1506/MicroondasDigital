# Quick Start - Micro-ondas Digital

## Você tem .NET 8.0 instalado! 🎉

O script `setup.ps1` já foi corrigido para usar .NET 8.0.

## 3 Passos Simples:

### 1️⃣ Atualize o repositório local

```powershell
cd D:\DEV\Github\DEV\MicroondasDigital
git pull origin main
```

### 2️⃣ Execute o script setup corrigido

```powershell
.\setup.ps1
```

### 3️⃣ Restaure e compile

```powershell
dotnet restore
dotnet build
```

## ✅ Próximo: Implementar Nível 1

Depois de completar os passos acima, abra a solução:

```powershell
start MicroondasDigital.sln
```

Ou use seu editor favorito (VS Code, Visual Studio, etc)

## 📚 Guias

- **SETUP.md** - Guia completo de configuração
- **docs/IMPLEMENTATION_PLAN.md** - Plano detalhado com exemplos de código

## ⚠️ Se tiver problemas

Se o setup falhar novamente, execute manualmente:

```powershell
# Crie os projetos um por um
dotnet new classlib -n Microondas.Domain -o src/Microondas.Domain
dotnet new classlib -n Microondas.Application -o src/Microondas.Application  
dotnet new classlib -n Microondas.Infrastructure -o src/Microondas.Infrastructure
dotnet new webapi -n Microondas.API -o src/Microondas.API
dotnet new console -n Microondas.UI -o src/Microondas.UI
dotnet new xunit -n Microondas.Tests -o tests/Microondas.Tests

# Adicione à solução
dotnet sln MicroondasDigital.sln add src/Microondas.Domain/Microondas.Domain.csproj
dotnet sln MicroondasDigital.sln add src/Microondas.Application/Microondas.Application.csproj
dotnet sln MicroondasDigital.sln add src/Microondas.Infrastructure/Microondas.Infrastructure.csproj
dotnet sln MicroondasDigital.sln add src/Microondas.API/Microondas.API.csproj
dotnet sln MicroondasDigital.sln add src/Microondas.UI/Microondas.UI.csproj
dotnet sln MicroondasDigital.sln add tests/Microondas.Tests/Microondas.Tests.csproj
```

Bom desenvolvimento! 🚀
