using Microondas.Domain;
using System.Xml.Linq;

namespace Microondas.Infrastructure.Repositories;

public class ProgramaRepository : IProgramaRepository
{
    private readonly List<Programa> _programas = new();

    public ProgramaRepository()
    {
        // Inicializar com programas pré-definidos
        InitializarProgramasPadroes();
    }

    public void Adicionar(Programa programa)
    {
        if (programa == null)
            throw new ArgumentNullException(nameof(programa));

        if (Existe(programa.Identificador))
            throw new InvalidOperationException($"Programa '{programa.Identificador}' já existe");

        _programas.Add(programa);
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

        var indice = _programas.IndexOf(existente);
        _programas[indice] = programa;
    }

    public void Remover(string identificador)
    {
        var programa = ObterPorIdentificador(identificador);
        if (programa != null)
        {
            if (!programa.EhCustomizado)
                throw new InvalidOperationException("Programas pré-definidos não podem ser removidos");

            _programas.Remove(programa);
        }
    }

    public bool Existe(string identificador)
    {
        return ObterPorIdentificador(identificador) != null;
    }

    public void Limpar()
    {
        _programas.Clear();
        InitializarProgramasPadroes();
    }

    private void InitializarProgramasPadroes()
    {
        // Remover apenas customizados (se houver)
        var customizados = _programas.Where(p => p.EhCustomizado).ToList();
        foreach (var prog in customizados)
            _programas.Remove(prog);

        // Programas Pré-definidos (Nível 2)
        _programas.Add(new Programa(
            "X", "Pipoca",
            TimeSpan.FromMinutes(3), // 3 min
            new Potencia(7),
            "Pipoca: pare quando o estouro diminuir.",
            false,
            '*' // caractere de aquecimento
        ));

        _programas.Add(new Programa(
            "M", "Leite",
            TimeSpan.FromMinutes(5), // 5 min
            new Potencia(5),
            "Leite: cuidado com líquidos quentes. Não superaquecer.",
            false,
            '~'
        ));

        _programas.Add(new Programa(
            "B", "Carne",
            TimeSpan.FromMinutes(14), // 14 min
            new Potencia(4),
            "Carne: vire na metade do tempo.",
            false,
            '#'
        ));

        _programas.Add(new Programa(
            "C", "Frango",
            TimeSpan.FromMinutes(8), // 8 min
            new Potencia(7),
            "Frango: vire na metade do tempo.",
            false,
            '+'
        ));

        _programas.Add(new Programa(
            "J", "Feijão",
            TimeSpan.FromMinutes(8), // 8 min
            new Potencia(9),
            "Feijão: aqueça descoberto. Cuidado com recipientes plásticos.",
            false,
            '!' 
        ));
    }
}
