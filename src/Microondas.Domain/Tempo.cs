namespace Microondas.Domain;

public class Tempo
{
    private const int MinSegundos = 1;
    private const int MaxSegundos = 120;

    public TimeSpan Valor { get; }

    // Adicionado parâmetro opcional 'ignorarLimites' para permitir tempos maiores em programas pré-definidos
    public Tempo(TimeSpan valor, bool ignorarLimites = false)
    {
        var segundos = (int)valor.TotalSeconds;

        if (!ignorarLimites && (segundos < MinSegundos || segundos > MaxSegundos))
            throw new ArgumentException(
                $"Tempo deve estar entre {MinSegundos}s e {MaxSegundos}s.",
                nameof(valor));

        Valor = valor;
    }
}
