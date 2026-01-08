namespace Microondas.Domain;

public class Programa
{
    public string Identificador { get; private set; } // Caractere único: "P", "L", "C", "F", "J" ou customizado
    public string Nome { get; private set; }
    public TimeSpan Tempo { get; private set; }
    public Potencia Potencia { get; private set; }
    public string Instrucoes { get; private set; }
    public bool EhCustomizado { get; private set; }
    public DateTime DataCriacao { get; private set; }

    private Programa() { }

    public Programa(string identificador, string nome, TimeSpan tempo, Potencia potencia,
                   string instrucoes, bool ehCustomizado = false)
    {
        ValidarIdentificador(identificador);

        Identificador = identificador.ToUpper();
        Nome = nome ?? throw new ArgumentNullException(nameof(nome));
        Tempo = tempo;
        Potencia = potencia ?? throw new ArgumentNullException(nameof(potencia));
        Instrucoes = instrucoes ?? string.Empty;
        EhCustomizado = ehCustomizado;
        DataCriacao = DateTime.UtcNow;
    }

    public Aquecimento CriarAquecimento()
    {
        var tempoVo = new Tempo(Tempo);
        return new Aquecimento(tempoVo, Potencia);
    }

    private void ValidarIdentificador(string identificador)
    {
        if (string.IsNullOrWhiteSpace(identificador) || identificador.Length != 1)
            throw new ArgumentException("Identificador deve ser um único caractere");
    }

    public override string ToString()
    {
        var estilo = EhCustomizado ? " (customizado)" : "";
        return $"[{Identificador}] {Nome}{estilo} - {Tempo.Minutes}m {Tempo.Seconds}s @ Potência {Potencia}";
    }
}
