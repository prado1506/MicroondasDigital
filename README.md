# Micro-ondas Digital

## Descrição
Sistema de micro-ondas digital desenvolvido em .NET com arquitetura em camadas, implementando conceitos de Orientação a Objetos, SOLID principles e Design Patterns.

## Status do Projeto
- [x] Nível 1: Aquecimento básico com validações
- [x] Nível 2: Programas pré-definidos
- [x] Nível 3: Cadastro de programas customizados
- [x] Nível 4: Web API com autenticação

## Estrutura do Projeto


```
MicroondasDigital/
├── src/
│   ├── Microondas.Domain/          # Entidades e lógica de negócio
│   ├── Microondas.Application/     # Casos de uso e orquestração
│   ├── Microondas.Infrastructure/  # Persistência e serviços externos
│   ├── Microondas.API/             # Web API (Nível 4)
│   └── Microondas.UI/              # Interface (Console/Web)
├── tests/
│   └── Microondas.Tests/           # Testes unitários
├── docs/
├── .gitignore
└── README.md
```

## Tecnologias
- **.NET 8**
- **C# 12**
- **xUnit** para testes
- **SQL Server / JSON** para persistência

## Requisitos Obrigatórios
✓ Orientação a Objetos  
✓ .NET 6.0 ou superior  
✓ Separação de camadas (UI + Negócio)  
✓ Funcionamento conforme especificações  

## Como Executar
    ```bash
    dotnet run --project src/Microondas.API/Microondas.API.csproj
    dotnet run --project src\Microondas.UI\Microondas.UI.csproj
    ```

### Pré-requisitos
- .NET SDK 8.0+ ou Visual Studio 2022/2024/2026
- Git

### Passos rápidos (linha de comando)

1. Clone o repositório:
   ```bash
   git clone https://github.com/prado1506/MicroondasDigital.git
   cd MicroondasDigital
   ```

2. Crie a solução:
   ```bash
   dotnet new sln -n MicroondasDigital
   ```

3. Execute a aplicação:
   ```bash
   dotnet run --project src/Microondas.UI
   ```

4. Em outra sessão/terminal, execute a UI:
   - Se a API rodar em http://localhost:5123:
     ```powershell
     $env:MICROONDAS_API_URL = "http://localhost:5123/"
     dotnet run --project src/Microondas.UI/Microondas.UI.csproj
     ```
   - Ou ajuste `MICROONDAS_API_URL` conforme a URL informada pelo Kestrel.

5. Execute os testes:
   ```bash
   dotnet test
   ```

## Principais Funcionalidades

### Nível 1
- Interface para entrada de tempo e potência
- Teclado digital e input por teclado
- Validação de tempo (1s - 2min) e potência (1-10)
- Quick start (30s com potência 10)
- String informativa de aquecimento
- Pausa e cancelamento

### Nível 2
- 5 programas pré-definidos (Pipoca, Leite, Carne, Frango, Feijão)
- Strings de aquecimento diferenciadas
- Instruções complementares
- Proteção contra alteração de tempo em programas pré-definidos

### Nível 3
- Cadastro de programas customizados
- Persistência em JSON ou SQL Server
- Validação de caracteres únicos
- Diferenciação visual (itálico) de programas customizados

### Nível 4
- Web API REST com autenticação Bearer Token
- Endpoints para todos os métodos de negócio
- Tratamento estruturado de exceções
- Logging de erros
- Criptografia de senhas usando SHA-256 (detalhes abaixo)
- Connection string criptografada (quando aplicável)

Observação sobre hashing de senha
- O requisito textual mencionava "SHA1 (256 bits)", o que é inconsistente (SHA‑1 não tem 256 bits). Este projeto utiliza SHA‑256 para persistência de senha, por ser um algoritmo robusto e amplamente recomendado atualmente.
- Implementação: `src/Microondas.API/Security/HashHelper.cs` fornece `Sha256Hex(string)` que retorna o hash em formato hexadecimal (lowercase).
- Raciocínio: SHA‑256 é preferível em relação a SHA‑1 por motivos de segurança. Se for necessário cumprir uma especificação externa que exija outro algoritmo, favor indicar explicitamente — mas a recomendação é manter SHA‑256.
- Como verificar/generar hash localmente:
  - Em PowerShell:
    ```powershell
    $pwd = "suaSenha"
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($pwd)
    $hash = [System.Security.Cryptography.SHA256]::Create().ComputeHash($bytes)
    [System.BitConverter]::ToString($hash).Replace("-","").ToLower()
    ```
  - O endpoint `POST /api/auth/configurar` recebe `username` e `password` em texto e grava o hash SHA‑256 no arquivo `auth_config.json` (em runtime). Em produção, recomenda-se usar um secret manager (Key Vault/Secrets Manager) em vez de arquivo.

## Arquitetura

### Camadas
1. **Domain**: Entidades, agregados e lógica core
2. **Application**: Serviços, DTOs e orquestração
3. **Infrastructure**: Repositórios, banco de dados, APIs externas
4. **API**: Endpoints e controllers
5. **UI**: Interface com usuário

### Design Patterns
- Repository Pattern
- Factory Pattern
- Observer Pattern (para atualizações de estado)
- Strategy Pattern (diferentes modos de aquecimento)
- Dependency Injection

## Segurança e Configuração
- JWT: chave `Jwt:Key` deve ser configurada via `dotnet user-secrets` em desenvolvimento ou variável de ambiente em produção. Não comitar a chave no repositório.
- Se optar por usar Base64 para a chave JWT, decodifique-a em `Program.cs` antes de criar `SymmetricSecurityKey`.
- Em produção, mover armazenamento de credenciais (atualmente `auth_config.json`) para um cofre de segredos.

## Tratamento de Erros e Logging
- Middleware centralizado trata exceções e retorna JSON padrão (`StandardError`).
- `BusinessException` existe para regras de negócio (retorna 400).
- Exceções não tratadas são logadas em `logs/exceptions-YYYYMMDD.log` com stacktrace e inner exceptions.

## Contribuindo
Este é um projeto de avaliação Coodesh. Siga as boas práticas de código e submeta suas implementações via commits bem estruturados.

## Autor
**Guilherme Prado** - Desenvolvedor  
Data: Janeiro 2026

## Aviso
Este projeto é fornecido como avaliação Pessoal.

## Nível 4 — Observações Rápidas
- Autenticação via JWT Bearer; configure `Jwt:Key` via user-secrets ou variável de ambiente.
- Senhas persistidas com SHA‑256 (veja `src/Microondas.API/Security/HashHelper.cs`).
- Middleware centralizado trata exceções e grava logs em `logs/`.

## Segurança e Configuração
  - `Jwt:Key`.
- já faz a decodificação automática e valida mínimo de 32 bytes.

