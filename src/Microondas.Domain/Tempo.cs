namespace Microondas.Domain;

public class Tempo
{
    private const int MinSegundos = 1;
    private const int MaxSegundos = 120;

    public TimeSpan Valor { get; }

    public Tempo(TimeSpan valor)
    {
        var segundos = (int)valor.TotalSeconds;

        if (segundos < MinSegundos || segundos > MaxSegundos)
            throw new ArgumentException(
                $"Tempo deve estar entre {MinSegundos}s e {MaxSegundos}s.",
                nameof(valor));

        Valor = valor;
    }
}
