using Microondas.Domain;

namespace Microondas.Infrastructure.Repositories;

public interface IProgramaRepository
{
    void Adicionar(Programa programa);
    Programa? ObterPorIdentificador(string identificador);
    IEnumerable<Programa> ObterTodos();
    IEnumerable<Programa> ObterCustomizados();
    void Atualizar(Programa programa);
    void Remover(string identificador);
    bool Existe(string identificador);
    void Limpar();
}
