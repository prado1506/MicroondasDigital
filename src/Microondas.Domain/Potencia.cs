public class Potencia
{
    public int Valor { get; private set; }
    public const int Minimo = 1;
    public const int Maximo = 10;
    public const int Padrao = 10;

    public Potencia(int valor)
    {
        if (valor < Minimo || valor > Maximo)
            throw new ArgumentException($"Potência deve estar entre {Minimo} e {Maximo}");
        Valor = valor;
    }
}

// Tempo.cs (Value Object)
public class Tempo
{
    public TimeSpan Duracao { get; private set; }
    public static TimeSpan Minimo => TimeSpan.FromSeconds(1);
    public static TimeSpan Maximo => TimeSpan.FromSeconds(120);
    public static TimeSpan QuickStart => TimeSpan.FromSeconds(30);

    public Tempo(TimeSpan duracao)
    {
        if (duracao < Minimo || duracao > Maximo)
            throw new ArgumentException($"Tempo deve estar entre 1s e 2min");
        Duracao = duracao;
    }
}