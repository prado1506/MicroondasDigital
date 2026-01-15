using Microondas.Application.DTOs;
using Microondas.Domain;

namespace Microondas.Application.Services;

public class AquecimentoService
{
    private readonly Microondas.Infrastructure.Repositories.IAquecimentoRepository _repo;

    public AquecimentoService(Microondas.Infrastructure.Repositories.IAquecimentoRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    public AquecimentoDTO CriarAquecimento(CriarAquecimentoDTO dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var tempo = new Tempo(TimeSpan.FromSeconds(dto.TempoSegundos));
        var potencia = new Potencia(dto.Potencia);

        var aquecimento = new Aquecimento(tempo, potencia, '.');
        _repo.Adicionar(aquecimento);
        return MapearParaDTO(aquecimento);
    }

    public AquecimentoDTO CriarAquecimentoComCaractere(CriarAquecimentoDTO dto, char caractere)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var tempo = new Tempo(TimeSpan.FromSeconds(dto.TempoSegundos), ignorarLimites: true);
        var potencia = new Potencia(dto.Potencia);

        var aquecimento = new Aquecimento(tempo, potencia, caractere);
        _repo.Adicionar(aquecimento);
        return MapearParaDTO(aquecimento);
    }

    public AquecimentoDTO? ObterAquecimento(int id)
    {
        var a = _repo.ObterPorId(id);
        return a != null ? MapearParaDTO(a) : null;
    }

    public AquecimentoDTO? SimularPassagemTempo(int id)
    {
        var a = _repo.ObterPorId(id);
        if (a == null) return null;

        a.DecrementarTempo();
        _repo.Atualizar(a);
        return MapearParaDTO(a);
    }

    public AquecimentoDTO? AdicionarTempo(int id)
    {
        var a = _repo.ObterPorId(id);
        if (a == null) throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        a.AdicionarTempo(TimeSpan.FromSeconds(30));
        _repo.Atualizar(a);
        return MapearParaDTO(a);
    }

    public AquecimentoDTO? IniciarAquecimento(int id)
    {
        var a = _repo.ObterPorId(id);
        if (a == null) throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        a.Iniciar();
        _repo.Atualizar(a);
        return MapearParaDTO(a);
    }       

    public AquecimentoDTO? PausarAquecimento(int id)
    {
        var a = _repo.ObterPorId(id);
        if (a == null) throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        a.Pausar();
        _repo.Atualizar(a);
        return MapearParaDTO(a);
    }

    public AquecimentoDTO? RetomarAquecimento(int id)
    {
        var a = _repo.ObterPorId(id);
        if (a == null) throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        a.Retomar();
        _repo.Atualizar(a);
        return MapearParaDTO(a);
    }

    public AquecimentoDTO? CancelarAquecimento(int id)
    {
        var a = _repo.ObterPorId(id);
        if (a == null) throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        a.Cancelar();
        _repo.Atualizar(a);
        return MapearParaDTO(a);
    }

    private static AquecimentoDTO MapearParaDTO(Aquecimento a)
    {
        return new AquecimentoDTO(
            a.Id,
            FormatarTempo(a.TempoTotal),
            FormatarTempo(a.TempoRestante),
            int.Parse(a.Potencia.ToString()),
            a.Estado.ToString(),
            a.StringInformativa
        );
    }

    private static string FormatarTempo(TimeSpan tempo)
    {
        return tempo.TotalSeconds < 60
            ? $"{(int)tempo.TotalSeconds}s"
            : $"{tempo.Minutes}m {tempo.Seconds}s";
    }
}