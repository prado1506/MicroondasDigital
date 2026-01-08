using Microondas.Application.DTOs;
using Microondas.Domain;
using Microondas.Infrastructure.Repositories;

namespace Microondas.Application.Services;

public class ProgramaService
{
    private readonly IProgramaRepository _repository;

    public ProgramaService(IProgramaRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public ProgramaDTO CriarPrograma(CriarProgramaDTO dto)
    {
        try
        {
            if (_repository.Existe(dto.Identificador))
                throw new InvalidOperationException($"Programa com identificador '{dto.Identificador}' já existe");

            var potencia = new Potencia(dto.Potencia);
            var tempo = TimeSpan.FromSeconds(dto.TempoSegundos);
            var programa = new Programa(
                dto.Identificador,
                dto.Nome,
                tempo,
                potencia,
                dto.Instrucoes,
                ehCustomizado: true
            );

            _repository.Adicionar(programa);
            return MapearParaDTO(programa);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao criar programa: {ex.Message}", ex);
        }
    }

    public ProgramaDTO? ObterPrograma(string identificador)
    {
        var programa = _repository.ObterPorIdentificador(identificador);
        return programa != null ? MapearParaDTO(programa) : null;
    }

    public IEnumerable<ProgramaDTO> ListarTodos()
    {
        return _repository.ObterTodos().Select(MapearParaDTO);
    }

    public IEnumerable<ProgramaDTO> ListarProgramasPreDefinidos()
    {
        return _repository.ObterTodos()
            .Where(p => !p.EhCustomizado)
            .Select(MapearParaDTO);
    }

    public IEnumerable<ProgramaDTO> ListarCustomizados()
    {
        return _repository.ObterCustomizados().Select(MapearParaDTO);
    }

    public void DeletarPrograma(string identificador)
    {
        var programa = _repository.ObterPorIdentificador(identificador);
        if (programa == null)
            throw new InvalidOperationException($"Programa '{identificador}' não encontrado");

        if (!programa.EhCustomizado)
            throw new InvalidOperationException("Não é possível deletar programas pré-definidos");

        _repository.Remover(identificador);
    }

    public Aquecimento IniciarAquecimentoComPrograma(string identificadorPrograma)
    {
        var programa = _repository.ObterPorIdentificador(identificadorPrograma);
        if (programa == null)
            throw new InvalidOperationException($"Programa '{identificadorPrograma}' não encontrado");

        return programa.CriarAquecimento();
    }

    private static ProgramaDTO MapearParaDTO(Programa programa)
    {
        return new ProgramaDTO(
            programa.Identificador,
            programa.Nome,
            FormatarTempo(programa.Tempo),
            int.Parse(programa.Potencia.ToString()),
            programa.Instrucoes,
            programa.EhCustomizado
        );
    }

    private static string FormatarTempo(TimeSpan tempo)
    {
        return tempo.TotalSeconds < 60
            ? $"{(int)tempo.TotalSeconds}s"
            : $"{tempo.Minutes}m {tempo.Seconds}s";
    }
}
