using Microondas.Domain;
using System.Xml.Linq;

namespace Microondas.Infrastructure.Repositories;

public class AquecimentoRepository : IAquecimentoRepository
{
    private readonly List<Aquecimento> _aquecimentos = new();

    public void Adicionar(Aquecimento aquecimento)
    {
        if (aquecimento == null)
            throw new ArgumentNullException(nameof(aquecimento));

        _aquecimentos.Add(aquecimento);
    }

    public Aquecimento? ObterPorId(int id)
    {
        return _aquecimentos.FirstOrDefault(a => a.Id == id);
    }

    public IEnumerable<Aquecimento> ObterTodos()
    {
        return _aquecimentos.AsReadOnly();
    }

    public void Atualizar(Aquecimento aquecimento)
    {
        if (aquecimento == null)
            throw new ArgumentNullException(nameof(aquecimento));

        var existente = ObterPorId(aquecimento.Id);
        if (existente == null)
            throw new InvalidOperationException($"Aquecimento {aquecimento.Id} não encontrado");

        var indice = _aquecimentos.IndexOf(existente);
        _aquecimentos[indice] = aquecimento;
    }

    public void Remover(int id)
    {
        var aquecimento = ObterPorId(id);
        if (aquecimento != null)
            _aquecimentos.Remove(aquecimento);
    }

    public void Limpar()
    {
        _aquecimentos.Clear();
    }
}
