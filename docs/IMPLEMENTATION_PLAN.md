# Plano de Implementação - Micro-ondas Digital

## Fase 1: Setup e Estrutura Base

### Tarefas Concluídas
- [✓] README.md documentado
- [✓] .gitignore criado
- [✓] Scripts de setup (PowerShell)
- [✓] Guia de setup (SETUP.md)

### Próximas Tarefas
- [ ] Executar setup.ps1 para criar estrutura de projetos
- [ ] Adicionar referências entre projetos

## Fase 2: Nível 1 - Aquecimento Básico

### Domain Layer (Microondas.Domain)

#### Entidades
```csharp
// Aquecimento.cs
public class Aquecimento
{
    public int Id { get; private set; }
    public TimeSpan TempoTotal { get; private set; }
    public TimeSpan TempoRestante { get; private set; }
    public int Potencia { get; private set; }
    public EstadoAquecimento Estado { get; private set; }
    public string StringInformativa { get; private set; }
    
    // Métodos
    public void Iniciar(TimeSpan tempo, int potencia)
    public void Pausar()
    public void Retomar()
    public void Cancelar()
    public void AdicionarTempo(TimeSpan tempo)
    public void AtualizarStringInformativa()
}

public enum EstadoAquecimento
{
    Parado,
    Aquecendo,
    Pausado,
    Concluido
}
```

#### Agregados e Value Objects
```csharp
// Potencia.cs (Value Object)
public class Potencia
{
    public int Valor { get; private set; }
    public const int Minimo = 1;
    public const int Maximo = 10;
    public const int Padrao = 10;
    
    public Potencia(int valor)
    {
        if (valor < Minimo || valor > Maximo)
            throw new ArgumentException($"Potência deve estar entre {Minimo} e {Maximo}");
        Valor = valor;
    }
}

// Tempo.cs (Value Object)
public class Tempo
{
    public TimeSpan Duracao { get; private set; }
    public static TimeSpan Minimo => TimeSpan.FromSeconds(1);
    public static TimeSpan Maximo => TimeSpan.FromSeconds(120);
    public static TimeSpan QuickStart => TimeSpan.FromSeconds(30);
    
    public Tempo(TimeSpan duracao)
    {
        if (duracao < Minimo || duracao > Maximo)
            throw new ArgumentException($"Tempo deve estar entre 1s e 2min");
        Duracao = duracao;
    }
}
```

### Application Layer (Microondas.Application)

#### Serviços de Aplicação
```csharp
// IniciarAquecimentoService.cs
public class IniciarAquecimentoService
{
    private readonly IAquecimentoRepository _repository;
    
    public IniciarAquecimentoService(IAquecimentoRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<Aquecimento> Executar(IniciarAquecimentoCommand comando)
    {
        var tempo = new Tempo(TimeSpan.FromSeconds(comando.TempoSegundos));
        var potencia = new Potencia(comando.Potencia ?? Potencia.Padrao);
        
        var aquecimento = new Aquecimento(tempo, potencia);
        aquecimento.Iniciar();
        
        await _repository.AdicionarAsync(aquecimento);
        return aquecimento;
    }
}
```

#### Commands e DTOs
```csharp
// IniciarAquecimentoCommand.cs
public class IniciarAquecimentoCommand
{
    public int TempoSegundos { get; set; }
    public int? Potencia { get; set; }
}

// AquecimentoDTO.cs
public class AquecimentoDTO
{
    public int Id { get; set; }
    public string TempoRestante { get; set; }
    public int Potencia { get; set; }
    public string Estado { get; set; }
    public string StringInformativa { get; set; }
}
```

### Infrastructure Layer (Microondas.Infrastructure)

#### Repositórios
```csharp
// AquecimentoRepository.cs
public class AquecimentoRepository : IAquecimentoRepository
{
    private List<Aquecimento> _aquecimentos = new();
    private Aquecimento _aquecimentoAtual;
    
    public async Task<Aquecimento> ObterAtualAsync()
    {
        return await Task.FromResult(_aquecimentoAtual);
    }
    
    public async Task AdicionarAsync(Aquecimento aquecimento)
    {
        _aquecimentos.Add(aquecimento);
        _aquecimentoAtual = aquecimento;
        await Task.CompletedTask;
    }
}
```

### UI Layer (Microondas.UI)

#### Interface Console
```csharp
// MicroondasUI.cs
public class MicroondasUI
{
    private readonly IniciarAquecimentoService _service;
    
    public void Exibir()
    {
        Console.WriteLine("=== MICRO-ONDAS DIGITAL ===");
        Console.WriteLine("1. Informar tempo e potência");
        Console.WriteLine("2. Quick Start (30s - Pot 10)");
        Console.WriteLine("3. Sair");
        
        var opcao = Console.ReadLine();
        // Processar opcoes
    }
}
```

### Tests (Microondas.Tests)

```csharp
public class AquecimentoTests
{
    [Fact]
    public void DeveIniciarAquecimento()
    {
        // Arrange
        var tempo = new Tempo(TimeSpan.FromSeconds(30));
        var potencia = new Potencia(5);
        
        // Act
        var aquecimento = new Aquecimento(tempo, potencia);
        aquecimento.Iniciar();
        
        // Assert
        Assert.Equal(EstadoAquecimento.Aquecendo, aquecimento.Estado);
    }
    
    [Fact]
    public void DeveValidarTempoMinimo()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Tempo(TimeSpan.FromMilliseconds(500)));
    }
}
```

## Fase 3: Nível 2 - Programas Pré-definidos

### Entidades
```csharp
public class ProgramaAquecimento
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Alimento { get; set; }
    public TimeSpan Tempo { get; set; }
    public Potencia Potencia { get; set; }
    public char CharacterAquecimento { get; set; }
    public string Instrucoes { get; set; }
    public bool EhPreDefinido { get; set; }
}
```

### Programas Iniciais
1. Pipoca: 3min, Pot 7, '#', "Observar estouros..."
2. Leite: 5min, Pot 5, '+', "Cuidado com líquidos..."
3. Carne Boi: 14min, Pot 4, '@', "Vire na metade..."
4. Frango: 8min, Pot 7, '*', "Vire na metade..."
5. Feijão: 8min, Pot 9, '&', "Deixe destampado..."

## Fase 4: Nível 3 - Programas Customizados

- [ ] Salvar programas em JSON
- [ ] Validar caracteres únicos
- [ ] Diferenciar visualmente customizados

## Fase 5: Nível 4 - Web API

- [ ] Controllers REST
- [ ] Autenticação Bearer Token
- [ ] Entity Framework Core
- [ ] SQL Server
- [ ] Logging e tratamento de erros

## Melhores Práticas a Seguir

### SOLID
- **S**ingle Responsibility: Cada classe tem uma responsábilidade
- **O**pen/Closed: Aberto para extensão, fechado para modificação
- **L**iskov Substitution: Interfaces bem definidas
- **I**nterface Segregation: Interfaces pequenas e específicas
- **D**ependency Inversion: Injetar depências

### Design Patterns
- Repository Pattern
- Factory Pattern
- Observer Pattern (atualização de estado)
- Strategy Pattern (diferentes aquecimentos)

## Checklist de Verificação

### Antes de Submeter
- [ ] Código compila sem erros
- [ ] Todos os testes passam
- [ ] Sem warnings no build
- [ ] Código documentado (XML comments)
- [ ] Commits com mensagens descritivas
- [ ] README atualizado com instruções de execução
