using System;

namespace Microondas.Domain;

public class Programa
{
    public string Identificador { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Alimento { get; set; } = string.Empty;
    public TimeSpan Tempo { get; set; }
    public Potencia Potencia { get; set; } = new Potencia(Potencia.Padrao);
    public string? Instrucoes { get; set; }
    public bool EhCustomizado { get; set; }
    public char CaractereProgresso { get; set; } = '*';
    
    // Propriedades herdadas para compatibilidade
    public int Id => int.TryParse(Identificador, out var id) ? id : 0;
    public string CaracterAquecimento => CaractereProgresso.ToString();
    public TimeSpan TempoPadrao => Tempo;
    public bool Pausado { get; set; } = false;
    public bool Finalizado { get; set; } = false;
    public bool EmUso { get; set; } = false;
    public DateTime InicioAquecimento { get; set; } = DateTime.MinValue;
    
    public TimeSpan TempoRestante
    {
        get
        {
            if (!EmUso || InicioAquecimento == DateTime.MinValue)
                return TimeSpan.Zero;

            var elapsed = DateTime.Now - InicioAquecimento;
            var remaining = Tempo - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    // Construtor compatível com ProgramaService
    public Programa(
        string identificador, 
        string nome, 
        string alimento,
        TimeSpan tempo,
        Potencia potencia,
        string instrucoes,
        bool ehCustomizado,
        char caractereProgresso)
    {
        Identificador = identificador ?? throw new ArgumentNullException(nameof(identificador));
        Nome = nome ?? throw new ArgumentNullException(nameof(nome));
        Alimento = alimento ?? throw new ArgumentNullException(nameof(alimento));
        Tempo = tempo;
        Potencia = potencia ?? throw new ArgumentNullException(nameof(potencia));
        Instrucoes = instrucoes;
        EhCustomizado = ehCustomizado;
        CaractereProgresso = caractereProgresso;
    }

    // Construtor alternativo usado em outros locais
    public Programa(string nome, int tempoSegundos, string caracterAquecimento = "*")
    {
        Nome = nome ?? throw new ArgumentNullException(nameof(nome));
        Tempo = TimeSpan.FromSeconds(tempoSegundos);
        CaractereProgresso = caracterAquecimento?.Length > 0 ? caracterAquecimento[0] : '*';
    }

    public void IniciarAquecimento()
    {
        if (Finalizado)
            throw new InvalidOperationException("Não é possível reiniciar um programa já finalizado.");

        if (Pausado)
            InicioAquecimento = DateTime.Now.Add(TempoRestante.Negate());
        else
            InicioAquecimento = DateTime.Now;

        EmUso = true;
        Pausado = false;
    }

    public void Pausar()
    {
        if (!EmUso) return;
        Pausado = true;
        EmUso = false;
    }

    public void Parar()
    {
        EmUso = false;
        Pausado = false;
        Finalizado = true;
    }

    public bool VerificarConclusao()
    {
        if (!EmUso || InicioAquecimento == DateTime.MinValue) 
            return false;

        var elapsed = DateTime.Now - InicioAquecimento;
        var isComplete = elapsed >= Tempo;
        
        if (isComplete)
        {
            Finalizado = true;
            EmUso = false;
        }

        return isComplete;
    }

    // Método requerido pelo ProgramaService para iniciar um Aquecimento a partir do Programa
    public Aquecimento CriarAquecimento()
    {
        // Usa a classe Tempo do domínio para validar limites e criar o Aquecimento
        var tempoDominio = new Tempo(Tempo);
        return new Aquecimento(tempoDominio, Potencia, CaractereProgresso);
    }
}