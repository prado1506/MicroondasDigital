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