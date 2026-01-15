namespace Microondas.Domain;

public class Programa
{
    public string Identificador { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public string Alimento { get; private set; } = null!;
    public TimeSpan Tempo { get; private set; }
    public Potencia Potencia { get; private set; } = null!;
    public string Instrucoes { get; private set; } = null!;
    public bool EhCustomizado { get; private set; }
    public DateTime DataCriacao { get; private set; }

    // Caractere usado para exibir progresso quando aquecimento for criado a partir deste programa
    public char CaractereProgresso { get; private set; }

    private Programa() { }

    public Programa(string identificador, string nome, string alimento, TimeSpan tempo, Potencia potencia,
                   string instrucoes, bool ehCustomizado = false, char caractereProgresso = '.')
    {
        ValidarIdentificador(identificador);

        Identificador = identificador.ToUpper();
        Nome = nome ?? throw new ArgumentNullException(nameof(nome));
        Alimento = alimento ?? string.Empty;
        Tempo = tempo;
        Potencia = potencia ?? throw new ArgumentNullException(nameof(potencia));
        Instrucoes = instrucoes ?? string.Empty;
        EhCustomizado = ehCustomizado;
        DataCriacao = DateTime.UtcNow;
        CaractereProgresso = caractereProgresso;
    }

    public Aquecimento CriarAquecimento()
    {
        // Para programas pré-definidos, permitir ignorar limites do VO Tempo
        var tempoVo = new Tempo(Tempo, ignorarLimites: !EhCustomizado);
        return new Aquecimento(tempoVo, Potencia, CaractereProgresso);
    }

    private void ValidarIdentificador(string identificador)
    {
        if (string.IsNullOrWhiteSpace(identificador) || identificador.Length != 1)
            throw new ArgumentException("Identificador deve ser um único caractere");
    }

    public override string ToString()
    {
        var estilo = EhCustomizado ? " (customizado)" : "";
        var alimento = string.IsNullOrWhiteSpace(Alimento) ? "" : $" - {Alimento}";
        return $"[{Identificador}] {Nome}{alimento}{estilo} - {Tempo.Minutes}m {Tempo.Seconds}s @ Potência {Potencia}";
    }
}
