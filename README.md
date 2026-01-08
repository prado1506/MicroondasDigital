# Micro-ondas Digital

## Descrição
Sistema de micro-ondas digital desenvolvido em .NET com arquitetura em camadas, implementando conceitos de Orientação a Objetos, SOLID principles e Design Patterns.

## Status do Projeto
- [x] Nível 1: Aquecimento básico com validações
- [ ] Nível 2: Programas pré-definidos
- [ ] Nível 3: Cadastro de programas customizados
- [ ] Nível 4: Web API com autenticação

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
- **.NET 6.0+** ou **.NET Framework 4.0+**
- **C# 10+**
- **xUnit** para testes
- **SQL Server / JSON** para persistência

## Requisitos Obrigatórios
✓ Orientação a Objetos  
✓ .NET Framework 4.0 ou superior  
✓ Separação de camadas (UI + Negócio)  
✓ Funcionamento conforme especificações  

## Requisitos Desejáveis
✓ SOLID principles  
✓ Design Patterns  
✓ Testes unitários  
✓ Documentação de código  
✓ Proteção de dados e métodos  

## Como Executar

### Pré-requisitos
- .NET SDK 6.0+ ou Visual Studio
- Git

### Passos
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

4. Execute os testes:
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
- Criptografia SHA256 de senhas
- Connection string criptografada (SQL Server)

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

## Contribuindo
Este é um projeto de avaliação Coodesh. Siga as boas práticas de código e submeta suas implementações via commits bem estruturados.

## Autor
**[seu_nome]** - Desenvolvedor  
Data: Janeiro 2026

## Licença
Este projeto é fornecido como avaliação acadêmica.
