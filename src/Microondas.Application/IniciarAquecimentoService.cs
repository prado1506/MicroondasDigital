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