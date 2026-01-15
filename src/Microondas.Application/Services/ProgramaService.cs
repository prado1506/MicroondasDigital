using Microondas.Application.DTOs;
using Microondas.Domain;
using Microondas.Infrastructure.Repositories;
using System;

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
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var identificador = dto.Identificador?.Trim().ToUpper() ?? throw new ArgumentException("Identificador obrigatório");
            if (identificador.Length != 1)
                throw new InvalidOperationException("Identificador deve ser um único caractere");

            if (_repository.Existe(identificador))
                throw new InvalidOperationException($"Programa com identificador '{identificador}' já existe");

            if (string.IsNullOrWhiteSpace(dto.CaractereProgresso) || dto.CaractereProgresso.Length != 1)
                throw new InvalidOperationException("Caractere de aquecimento obrigatório (um único caractere)");

            var caract = dto.CaractereProgresso[0];
            if (caract == '.')
                throw new InvalidOperationException("Caractere '.' é reservado e não pode ser usado por programas customizados");

            if (_repository.ObterTodos().Any(p => p.CaractereProgresso == caract))
                throw new InvalidOperationException($"Caractere de aquecimento '{caract}' já está em uso");

            var potencia = new Potencia(dto.Potencia);
            var tempo = TimeSpan.FromSeconds(dto.TempoSegundos);

            var programa = new Programa(
                identificador,
                dto.Nome,
                dto.Alimento,
                tempo,
                potencia,
                dto.Instrucoes ?? string.Empty,
                ehCustomizado: true,
                caractereProgresso: caract
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
            programa.Alimento,
            FormatarTempo(programa.Tempo),
            (int)programa.Tempo.TotalSeconds,
            int.Parse(programa.Potencia.ToString()),
            programa.Instrucoes ?? string.Empty,
            programa.EhCustomizado,
            programa.CaractereProgresso.ToString()
        );
    }

    private static string FormatarTempo(TimeSpan tempo)
    {
        return tempo.TotalSeconds < 60
            ? $"{(int)tempo.TotalSeconds}s"
            : $"{tempo.Minutes}m {tempo.Seconds}s";
    }
}
