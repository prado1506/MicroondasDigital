# Guia de Setup - Micro-ondas Digital

## Pré-requisitos

- **Windows/Linux/macOS**: Sistema operacional com suporte a .NET
- **.NET SDK 6.0+** ou **.NET Framework 4.7.2+**: [Baixar aqui](https://dotnet.microsoft.com/download)
- **Git**: Para controle de versão
- **Visual Studio** ou **Visual Studio Code** (opcional): Para desenvolvimento
- **SQL Server Express** (opcional): Para Nível 3/4

## Passos de Configuração

### 1. Clone o Repositório

```bash
git clone https://github.com/prado1506/MicroondasDigital.git
cd MicroondasDigital
```

### 2. Execute o Script de Setup (Windows)

Abra o PowerShell como administrador e execute:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
.\setup.ps1
```

### 3. Execute o Script de Setup (Linux/macOS)

```bash
chmod +x setup.sh
./setup.sh
```

### 4. Restaure as Dependências

```bash
dotnet restore
```

### 5. Compile o Projeto

```bash
dotnet build
```

## Estrutura de Projetos Criada

Após executar o setup, você terá:

```
MicroondasDigital/
├── MicroondasDigital.sln
├── src/
│   ├── Microondas.Domain/
│   ├── Microondas.Application/
│   ├── Microondas.Infrastructure/
│   ├── Microondas.API/
│   └── Microondas.UI/
├── tests/
│   └── Microondas.Tests/
└── docs/
```

## Execução

### Executar Aplicação Console (UI)

```bash
dotnet run --project src/Microondas.UI
```

### Executar Testes Unitários

```bash
dotnet test
```

### Executar Web API (Nível 4)

```bash
dotnet run --project src/Microondas.API
```

## Configuração do Banco de Dados (Opcional)

Para usar SQL Server com Entity Framework Core:

```bash
# Instale o EF Core Tools
dotnet tool install --global dotnet-ef

# Crie as migrações
dotnet ef migrations add InitialMigration --project src/Microondas.Infrastructure

# Aplique as migrações
dotnet ef database update --project src/Microondas.Infrastructure
```

## Documentação das Camadas

### Microondas.Domain
Contém as entidades, agregados e lógica de domínio central.

### Microondas.Application
Contém serviços de aplicação, DTOs e lógica de negócios de coordenação.

### Microondas.Infrastructure
Contém acesso a dados, repositórios, e implementação de persistência.

### Microondas.API
Web API REST com autenticação Bearer Token (Nível 4).

### Microondas.UI
Interface de usuário (Console, WPF ou Web).

### Microondas.Tests
Testes unitários usando xUnit.

## Resolução de Problemas

### "dotnet: comando não encontrado"
- Instale o .NET SDK corretamente
- Adicione o SDK ao PATH do sistema

### Erro ao clonar repositório
- Verifique sua conexão de internet
- Confirme que tem permissão de leitura no repositório

### Erro ao compilar
- Execute `dotnet clean`
- Execute `dotnet restore` novamente
- Verifique se todas as dependências estão instaladas

## Próximos Passos

1. Leia a documentaão no `README.md`
2. Estude a arquitetura em `docs/ARCHITECTURE.md`
3. Comece implementando a camada Domain
4. Implemente os testes unitários
5. Progredua para os níveis de complexidade

## Suporte

Para dúvidas ou problemas:
- Consulte a documentação oficial do .NET: https://docs.microsoft.com/dotnet
- Abra uma issue no repositório
- Verifique os commits anteriores para exemplos de implementação
