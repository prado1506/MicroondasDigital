using Microondas.Application.DTOs;
using Microondas.Domain;

namespace Microondas.Application.Services;

public class AquecimentoService
{
    private readonly List<Aquecimento> _aquecimentos = new();

    public AquecimentoDTO CriarAquecimento(CriarAquecimentoDTO dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var tempo = new Tempo(TimeSpan.FromSeconds(dto.TempoSegundos));
        var potencia = new Potencia(dto.Potencia);

        // cria aquecimento manual com caractere padrão '.'
        var aquecimento = new Aquecimento(tempo, potencia, '.');
        _aquecimentos.Add(aquecimento);
        return MapearParaDTO(aquecimento);
    }

    // Novo: criar aquecimento usando caractere de progresso (para programas pré-definidos).
    // Usa 'ignorarLimites' ao criar Tempo para permitir durações > 120s quando necessário.
    public AquecimentoDTO CriarAquecimentoComCaractere(CriarAquecimentoDTO dto, char caractere)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        // permite tempos além do VO Tempo padrão — programas pré-definidos podem ter >120s
        var tempo = new Tempo(TimeSpan.FromSeconds(dto.TempoSegundos), ignorarLimites: true);
        var potencia = new Potencia(dto.Potencia);

        var aquecimento = new Aquecimento(tempo, potencia, caractere);
        _aquecimentos.Add(aquecimento);
        return MapearParaDTO(aquecimento);
    }

    public AquecimentoDTO? ObterAquecimento(int id)
    {
        var a = _aquecimentos.FirstOrDefault(x => x.Id == id);
        return a != null ? MapearParaDTO(a) : null;
    }

    public AquecimentoDTO? SimularPassagemTempo(int id)
    {
        var a = _aquecimentos.FirstOrDefault(x => x.Id == id);
        if (a == null) return null;

        a.DecrementarTempo();
        return MapearParaDTO(a);
    }

    // Agora AdicionarTempo sempre adiciona 30 segundos — regra do projeto
    public AquecimentoDTO? AdicionarTempo(int id)
    {
        var a = _aquecimentos.FirstOrDefault(x => x.Id == id);
        if (a == null) throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        a.AdicionarTempo(TimeSpan.FromSeconds(30)); // adiciona 30s fixos
        return MapearParaDTO(a);
    }

    public AquecimentoDTO? IniciarAquecimento(int id)
    {
        var a = _aquecimentos.FirstOrDefault(x => x.Id == id);
        if (a == null) throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        a.Iniciar();
        return MapearParaDTO(a);
    }

    public AquecimentoDTO? PausarAquecimento(int id)
    {
        var a = _aquecimentos.FirstOrDefault(x => x.Id == id);
        if (a == null) throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        a.Pausar();
        return MapearParaDTO(a);
    }

    public AquecimentoDTO? RetomarAquecimento(int id)
    {
        var a = _aquecimentos.FirstOrDefault(x => x.Id == id);
        if (a == null) throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        a.Retomar();
        return MapearParaDTO(a);
    }

    public AquecimentoDTO? CancelarAquecimento(int id)
    {
        var a = _aquecimentos.FirstOrDefault(x => x.Id == id);
        if (a == null) throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        a.Cancelar();
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