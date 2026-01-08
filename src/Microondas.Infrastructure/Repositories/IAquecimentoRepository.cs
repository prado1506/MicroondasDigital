using Microondas.Domain;

namespace Microondas.Infrastructure.Repositories;

public interface IAquecimentoRepository
{
    void Adicionar(Aquecimento aquecimento);
    Aquecimento? ObterPorId(int id);
    IEnumerable<Aquecimento> ObterTodos();
    void Atualizar(Aquecimento aquecimento);
    void Remover(int id);
    void Limpar();
}
