using Microondas.Application.DTOs;
using Microondas.Domain;

namespace Microondas.Application.Services;

public class AquecimentoService
{
    private readonly List<Aquecimento> _aquecimentos = new();

    public AquecimentoDTO CriarAquecimento(CriarAquecimentoDTO dto)
    {
        try
        {
            var potencia = new Potencia(dto.Potencia);
            var tempo = TimeSpan.FromSeconds(dto.TempoSegundos);
            var aquecimento = new Aquecimento(tempo, potencia);

            _aquecimentos.Add(aquecimento);
            return MapearParaDTO(aquecimento);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao criar aquecimento: {ex.Message}", ex);
        }
    }

    public AquecimentoDTO IniciarAquecimento(int id)
    {
        var aquecimento = _aquecimentos.FirstOrDefault(a => a.Id == id)
            ?? throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        aquecimento.Iniciar();
        return MapearParaDTO(aquecimento);
    }

    public AquecimentoDTO PausarAquecimento(int id)
    {
        var aquecimento = _aquecimentos.FirstOrDefault(a => a.Id == id)
            ?? throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        aquecimento.Pausar();
        return MapearParaDTO(aquecimento);
    }

    public AquecimentoDTO RetomarAquecimento(int id)
    {
        var aquecimento = _aquecimentos.FirstOrDefault(a => a.Id == id)
            ?? throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        aquecimento.Retomar();
        return MapearParaDTO(aquecimento);
    }

    public AquecimentoDTO CancelarAquecimento(int id)
    {
        var aquecimento = _aquecimentos.FirstOrDefault(a => a.Id == id)
            ?? throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        aquecimento.Cancelar();
        return MapearParaDTO(aquecimento);
    }

    public AquecimentoDTO AdicionarTempo(int id, int segundos)
    {
        var aquecimento = _aquecimentos.FirstOrDefault(a => a.Id == id)
            ?? throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        aquecimento.AdicionarTempo(TimeSpan.FromSeconds(segundos));
        return MapearParaDTO(aquecimento);
    }

    public AquecimentoDTO ObterAquecimento(int id)
    {
        var aquecimento = _aquecimentos.FirstOrDefault(a => a.Id == id)
            ?? throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        return MapearParaDTO(aquecimento);
    }

    public void SimularPassagemTempo(int id)
    {
        var aquecimento = _aquecimentos.FirstOrDefault(a => a.Id == id)
            ?? throw new InvalidOperationException($"Aquecimento {id} não encontrado");

        aquecimento.DecrementarTempo();
    }

    private static AquecimentoDTO MapearParaDTO(Aquecimento aquecimento)
    {
        return new AquecimentoDTO(
            aquecimento.Id,
            FormatarTempo(aquecimento.TempoTotal),
            FormatarTempo(aquecimento.TempoRestante),
            int.Parse(aquecimento.Potencia.ToString()),
            aquecimento.Estado.ToString(),
            aquecimento.StringInformativa
        );
    }

    private static string FormatarTempo(TimeSpan tempo)
    {
        return tempo.TotalSeconds < 60
            ? $"{(int)tempo.TotalSeconds}s"
            : $"{tempo.Minutes}m {tempo.Seconds}s";
    }
}
