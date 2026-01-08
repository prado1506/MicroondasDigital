namespace Microondas.Domain;

public class Aquecimento
{
    private const int TempoMinimoSegundos = 1;
    private const int TempoMaximoSegundos = 120; // 2 minutos
    private TimeSpan _tempoRestante;

    public int Id { get; private set; }
    public TimeSpan TempoTotal { get; private set; }

    public TimeSpan TempoRestante
    {
        get => _tempoRestante;
        private set => _tempoRestante = value;
    }

    public Potencia Potencia { get; private set; }
    public EstadoAquecimento Estado { get; private set; }
    public string StringInformativa { get; private set; }
    public Aquecimento(Tempo tempo, Potencia potencia)
    {
        if (tempo is null) throw new ArgumentNullException(nameof(tempo));
        if (potencia is null) throw new ArgumentNullException(nameof(potencia));

        Id = new Random().Next(1, 999999);
        TempoTotal = tempo.Valor;
        TempoRestante = tempo.Valor;
        Potencia = potencia;
        Estado = EstadoAquecimento.Parado;
        StringInformativa = GerarStringInformativa()!;
    }

    private string? GerarStringInformativa()
    {
        return Estado switch
        {
            EstadoAquecimento.Parado => $"Microondas parado. Tempo: {FormatarTempo(TempoTotal)} | Potência: {Potencia}",
            EstadoAquecimento.Aquecendo => $"Aquecendo... Tempo restante: {FormatarTempo(TempoRestante)} | Potência: {Potencia}",
            EstadoAquecimento.Pausado => $"Aquecimento pausado. Tempo restante: {FormatarTempo(TempoRestante)} | Potência: {Potencia}",
            EstadoAquecimento.Concluido => $"Aquecimento concluído! Tempo total: {FormatarTempo(TempoTotal)}",
            _ => "Estado desconhecido"
        };
    }


    public void Iniciar()
    {
        if (Estado != EstadoAquecimento.Parado && Estado != EstadoAquecimento.Pausado)
            throw new InvalidOperationException("Não é possível iniciar o aquecimento neste estado");

        Estado = EstadoAquecimento.Aquecendo;
        AtualizarStringInformativa();
    }

    public void Pausar()
    {
        if (Estado != EstadoAquecimento.Aquecendo)
            throw new InvalidOperationException("Não é possível pausar. Aquecimento não está ativo");

        Estado = EstadoAquecimento.Pausado;
        AtualizarStringInformativa();
    }

    public void Retomar()
    {
        if (Estado != EstadoAquecimento.Pausado)
            throw new InvalidOperationException("Não é possível retomar. Aquecimento não está pausado");

        Estado = EstadoAquecimento.Aquecendo;
        AtualizarStringInformativa();
    }

    public void Cancelar()
    {
        if (Estado == EstadoAquecimento.Concluido)
            throw new InvalidOperationException("Não é possível cancelar um aquecimento já concluído");

        Estado = EstadoAquecimento.Parado;
        TempoRestante = TimeSpan.Zero;
        AtualizarStringInformativa();
    }

    public void AdicionarTempo(TimeSpan tempo)
    {
        if (Estado == EstadoAquecimento.Concluido)
            throw new InvalidOperationException("Não é possível adicionar tempo a um aquecimento concluído");

        var novoTempo = TempoRestante.Add(tempo);
        ValidarTempo(novoTempo);

        TempoRestante = novoTempo;
        TempoTotal = TempoTotal.Add(tempo);
        AtualizarStringInformativa();
    }

    public void DecrementarTempo()
    {
        if (Estado != EstadoAquecimento.Aquecendo)
            return;

        if (TempoRestante.TotalSeconds > 0)
        {
            TempoRestante = TempoRestante.Subtract(TimeSpan.FromSeconds(1));

            if (TempoRestante.TotalSeconds <= 0)
            {
                TempoRestante = TimeSpan.Zero;
                Estado = EstadoAquecimento.Concluido;
            }

            AtualizarStringInformativa();
        }
    }

    public void AtualizarStringInformativa()
    {
        StringInformativa = Estado switch
        {
            EstadoAquecimento.Parado => $"Microondas parado. Tempo: {FormatarTempo(TempoTotal)} | Potência: {Potencia}",
            EstadoAquecimento.Aquecendo => $"Aquecendo... Tempo restante: {FormatarTempo(TempoRestante)} | Potência: {Potencia}",
            EstadoAquecimento.Pausado => $"Aquecimento pausado. Tempo restante: {FormatarTempo(TempoRestante)} | Potência: {Potencia}",
            EstadoAquecimento.Concluido => $"Aquecimento concluído! Tempo total: {FormatarTempo(TempoTotal)}",
            _ => "Estado desconhecido"
        };
    }

    private void ValidarTempo(TimeSpan tempo)
    {
        var segundos = (int)tempo.TotalSeconds;
        if (segundos < TempoMinimoSegundos || segundos > TempoMaximoSegundos)
            throw new ArgumentException($"Tempo deve estar entre {TempoMinimoSegundos}s e {TempoMaximoSegundos}s (2 minutos)");
    }

    private string FormatarTempo(TimeSpan tempo)
    {
        return tempo.TotalSeconds < 60
            ? $"{(int)tempo.TotalSeconds}s"
            : $"{tempo.Minutes}m {tempo.Seconds}s";
    }
}

public enum EstadoAquecimento
{
    Parado,
    Aquecendo,
    Pausado,
    Concluido
}
