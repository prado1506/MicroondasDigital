namespace Microondas.Domain;

public class Potencia
{
    private const int MinPotencia = 1;
    private const int MaxPotencia = 10;

    public int Valor { get; }

    public Potencia(int valor)
    {
        if (valor < MinPotencia || valor > MaxPotencia)
            throw new ArgumentException(
                $"Potência deve estar entre {MinPotencia} e {MaxPotencia}",
                nameof(valor));

        Valor = valor;
    }

    public override string ToString() => Valor.ToString();
}
