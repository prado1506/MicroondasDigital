namespace Microondas.Domain;

public interface IAquecimentoRepository
{
    Task<Aquecimento?> ObterAtualAsync();
    Task AdicionarAsync(Aquecimento aquecimento);
}
