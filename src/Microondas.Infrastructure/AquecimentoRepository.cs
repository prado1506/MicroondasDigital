using Microondas.Domain;

namespace Microondas.Infrastructure;

public class AquecimentoRepository : IAquecimentoRepository
{
    private readonly List<Aquecimento> _aquecimentos = new();
    private Aquecimento? _aquecimentoAtual;

    public Task<Aquecimento?> ObterAtualAsync()
    {
        return Task.FromResult(_aquecimentoAtual);
    }

    public Task AdicionarAsync(Aquecimento aquecimento)
    {
        _aquecimentos.Add(aquecimento);
        _aquecimentoAtual = aquecimento;
        return Task.CompletedTask;
    }
}
