namespace Microondas.Domain;

public class Potencia
{
    private const int MinimaPotencia = 1;
    private const int MaximaPotencia = 10;

    public int Valor { get; private set; }

    public Potencia(int valor)
    {
        if (!IsValida(valor))
            throw new ArgumentException($"Potência deve estar entre {MinimaPotencia} e {MaximaPotencia}");

        Valor = valor;
    }

    public static bool IsValida(int valor)
    {
        return valor >= MinimaPotencia && valor <= MaximaPotencia;
    }

    public override string ToString() => Valor.ToString();
}
