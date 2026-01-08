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
        return _programas.AsReadOnly();
    }

    public IEnumerable<Programa> ObterCustomizados()
    {
        return _programas.Where(p => p.EhCustomizado).AsReadOnly();
    }

    public void Atualizar(Programa programa)
    {
        if (programa == null)
            throw new ArgumentNullException(nameof(programa));

        var existente = ObterPorIdentificador(programa.Identificador);
        if (existente == null)
            throw new InvalidOperationException($"Programa '{programa.Identificador}' não encontrado");

        var indice = _programas.IndexOf(existente);
        _programas[indice] = programa;
    }

    public void Remover(string identificador)
    {
        var programa = ObterPorIdentificador(identificador);
        if (programa != null)
            _programas.Remove(programa);
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
        // Limpar apenas programas customizados
        var customizados = _programas.Where(p => p.EhCustomizado).ToList();
        foreach (var prog in customizados)
            _programas.Remove(prog);

        // Programas Pré-definidos (Nível 2)
        _programas.Add(new Programa(
            "P", "Pipoca",
            TimeSpan.FromSeconds(180), // 3 minutos
            new Potencia(8),
            "Pipoca de micro-ondas. Não remova durante o aquecimento.",
            false
        ));

        _programas.Add(new Programa(
            "L", "Leite",
            TimeSpan.FromSeconds(300), // 5 minutos
            new Potencia(3),
            "Leite para bebê. Aquecimento suave e seguro.",
            false
        ));

        _programas.Add(new Programa(
            "C", "Carne",
            TimeSpan.FromSeconds(420), // 7 minutos
            new Potencia(9),
            "Descongelamento de carne. Vire na metade do tempo.",
            false
        ));

        _programas.Add(new Programa(
            "F", "Frango",
            TimeSpan.FromSeconds(360), // 6 minutos
            new Potencia(8),
            "Descongelamento de frango. Monitore durante o processo.",
            false
        ));

        _programas.Add(new Programa(
            "J", "Feijão",
            TimeSpan.FromSeconds(480), // 8 minutos
            new Potencia(7),
            "Aquecimento de feijão. Aqueça gradualmente para evitar respingos.",
            false
        ));
    }
}
