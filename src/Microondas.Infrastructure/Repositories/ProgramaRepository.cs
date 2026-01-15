using Microondas.Domain;
using System.Text.Json;

namespace Microondas.Infrastructure.Repositories;

public class ProgramaRepository : IProgramaRepository
{
    private readonly List<Programa> _programas = new();
    private readonly string _filePathCustom;

    public ProgramaRepository()
    {
        _filePathCustom = Path.Combine(AppContext.BaseDirectory, "programas_customizados.json");
        // Inicializar com programas pré-definidos + carregar customizados do disco
        InicializarProgramasPadroes();
        CarregarProgramasCustomizados();
    }

    public void Adicionar(Programa programa)
    {
        if (programa == null)
            throw new ArgumentNullException(nameof(programa));

        if (Existe(programa.Identificador))
            throw new InvalidOperationException($"Programa '{programa.Identificador}' já existe");

        // valida unicidade do caractere de progresso e proíbe '.'
        if (programa.CaractereProgresso == '.')
            throw new InvalidOperationException("Caractere de aquecimento '.' é reservado e não pode ser usado.");

        if (_programas.Any(p => p.CaractereProgresso == programa.CaractereProgresso))
            throw new InvalidOperationException($"Caractere de aquecimento '{programa.CaractereProgresso}' já está em uso por outro programa.");

        _programas.Add(programa);

        if (programa.EhCustomizado)
            SalvarProgramasCustomizados();
    }

    public Programa? ObterPorIdentificador(string identificador)
    {
        if (string.IsNullOrWhiteSpace(identificador))
            return null;

        return _programas.FirstOrDefault(p =>
            p.Identificador.Equals(identificador, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<Programa> ObterTodos()
    {
        // exposição como IEnumerable já impede alteração da lista por fora
        return _programas;
    }

    public IEnumerable<Programa> ObterCustomizados()
    {
        return _programas.Where(p => p.EhCustomizado);
    }

    public void Atualizar(Programa programa)
    {
        if (programa == null)
            throw new ArgumentNullException(nameof(programa));

        var existente = ObterPorIdentificador(programa.Identificador);
        if (existente == null)
            throw new InvalidOperationException($"Programa '{programa.Identificador}' não encontrado");

        if (!existente.EhCustomizado)
            throw new InvalidOperationException("Programas pré-definidos não podem ser atualizados");

        // valida unicidade caractere (exceto contra si mesmo)
        if (programa.CaractereProgresso == '.')
            throw new InvalidOperationException("Caractere de aquecimento '.' é reservado e não pode ser usado.");
        if (_programas.Any(p => p.Identificador != existente.Identificador && p.CaractereProgresso == programa.CaractereProgresso))
            throw new InvalidOperationException($"Caractere de aquecimento '{programa.CaractereProgresso}' já está em uso por outro programa.");

        var indice = _programas.IndexOf(existente);
        _programas[indice] = programa;
        SalvarProgramasCustomizados();
    }

    public void Remover(string identificador)
    {
        var programa = ObterPorIdentificador(identificador);
        if (programa != null)
        {
            if (!programa.EhCustomizado)
                throw new InvalidOperationException("Programas pré-definidos não podem ser removidos");

            _programas.Remove(programa);
            SalvarProgramasCustomizados();
        }
    }

    public bool Existe(string identificador)
    {
        return ObterPorIdentificador(identificador) != null;
    }

    public void Limpar()
    {
        _programas.Clear();
        InicializarProgramasPadroes();
        // mantém o arquivo de customizados como está — re-carregar a seguir
        CarregarProgramasCustomizados();
    }

    private void InicializarProgramasPadroes()
    {
        // Remover apenas customizados (se houver)
        var customizados = _programas.Where(p => p.EhCustomizado).ToList();
        foreach (var prog in customizados)
            _programas.Remove(prog);

        // Programas Pré-definidos (exemplos)
        _programas.Add(new Programa(
            "X", "Pipoca", "Pipoca",
            TimeSpan.FromMinutes(3), // 3 min
            new Potencia(7),
            "Pipoca: pare quando o estouro diminuir.",
            false,
            '*' // caractere de aquecimento
        ));

        _programas.Add(new Programa(
            "M", "Leite", "Leite",
            TimeSpan.FromMinutes(5), // 5 min
            new Potencia(5),
            "Leite: cuidado com líquidos quentes. Não superaquecer.",
            false,
            '~'
        ));

        _programas.Add(new Programa(
            "B", "Carne", "Carne",
            TimeSpan.FromMinutes(14), // 14 min
            new Potencia(4),
            "Carne: vire na metade do tempo.",
            false,
            '#'
        ));

        _programas.Add(new Programa(
            "C", "Frango", "Frango",
            TimeSpan.FromMinutes(8), // 8 min
            new Potencia(7),
            "Frango: vire na metade do tempo.",
            false,
            '+'
        ));

        _programas.Add(new Programa(
            "J", "Feijão", "Feijão",
            TimeSpan.FromMinutes(8), // 8 min
            new Potencia(9),
            "Feijão: aqueça descoberto. Cuidado com recipientes plásticos.",
            false,
            '!'
        ));
    }

    private void SalvarProgramasCustomizados()
    {
        try
        {
            var custom = _programas.Where(p => p.EhCustomizado)
                .Select(p => new SerializablePrograma
                {
                    Identificador = p.Identificador,
                    Nome = p.Nome,
                    Alimento = p.Alimento,
                    TempoSegundos = (int)p.Tempo.TotalSeconds,
                    Potencia = int.Parse(p.Potencia.ToString()),
                    Instrucoes = p.Instrucoes ?? string.Empty,
                    CaractereProgresso = p.CaractereProgresso
                }).ToList();

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(custom, options);
            File.WriteAllText(_filePathCustom, json);
        }
        catch
        {
            // Não propagar exceções de I/O para não quebrar fluxo principal;
            // log poderia ser adicionado aqui.
        }
    }

    private void CarregarProgramasCustomizados()
    {
        try
        {
            if (!File.Exists(_filePathCustom))
                return;

            var json = File.ReadAllText(_filePathCustom);
            var list = JsonSerializer.Deserialize<List<SerializablePrograma>>(json);
            if (list == null) return;

            foreach (var s in list)
            {
                // valida unicidade do identificador e do caractere
                if (string.IsNullOrWhiteSpace(s.Identificador) || s.Identificador.Length != 1)
                    continue;

                if (_programas.Any(p => p.Identificador.Equals(s.Identificador, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (s.CaractereProgresso == '.' || _programas.Any(p => p.CaractereProgresso == s.CaractereProgresso))
                    continue;

                try
                {
                    var programa = new Programa(
                        s.Identificador,
                        s.Nome,
                        s.Alimento,
                        TimeSpan.FromSeconds(s.TempoSegundos),
                        new Potencia(s.Potencia),
                        s.Instrucoes,
                        true,
                        s.CaractereProgresso
                    );

                    _programas.Add(programa);
                }
                catch
                {
                    // ignora entradas inválidas
                }
            }
        }
        catch
        {
            // ignora erros de I/O/parse
        }
    }

    private class SerializablePrograma
    {
        public string Identificador { get; set; } = "";
        public string Nome { get; set; } = "";
        public string Alimento { get; set; } = "";
        public int TempoSegundos { get; set; }
        public int Potencia { get; set; }
        public string Instrucoes { get; set; } = "";
        public char CaractereProgresso { get; set; }
    }
}