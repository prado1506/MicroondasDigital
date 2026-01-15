namespace Microondas.Domain;

public class Programa
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TimeSpan TempoPadrao { get; set; }
    public string CaracterAquecimento { get; set; } = "*";
    public bool Pausado { get; set; } = false;
    public bool Finalizado { get; set; } = false;
    public bool EmUso { get; set; } = false;
    public DateTime InicioAquecimento { get; set; } = DateTime.MinValue;
    public int Potencia { get; set; } = 10;
    
    // Adicionando as propriedades esperadas pelo repositório
    public int Identificador => Id;  // Mapeamento para Id
    public string CaractereProgresso => CaracterAquecimento;  // Mapeamento para CaracterAquecimento
    public bool EhCustomizado => Id > 5;  // Lógica provisória - ajuste conforme necessário
    
    public TimeSpan TempoRestante
    {
        get
        {
            if (!EmUso || InicioAquecimento == DateTime.MinValue)
                return TimeSpan.Zero;

            var elapsed = DateTime.Now - InicioAquecimento;
            var remaining = TempoPadrao - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    // Construtor com 8 parâmetros conforme esperado pelo repositório
    public Programa(int id, string nome, int tempoSegundos, string caractere, bool pausado, bool finalizado, bool emUso, int potencia)
    {
        Id = id;
        Nome = nome ?? string.Empty;
        TempoPadrao = TimeSpan.FromSeconds(tempoSegundos);
        CaracterAquecimento = caractere;
        Pausado = pausado;
        Finalizado = finalizado;
        EmUso = emUso;
        Potencia = potencia;
    }

    // Mantendo o construtor original também
    public Programa(string nome, int tempoSegundos, string caracterAquecimento = "*")
    {
        Nome = nome ?? throw new ArgumentNullException(nameof(nome));
        TempoPadrao = TimeSpan.FromSeconds(tempoSegundos);
        CaracterAquecimento = caracterAquecimento;
    }

    public void IniciarAquecimento()
    {
        if (Finalizado)
        {
            throw new InvalidOperationException("Não é possível reiniciar um programa já finalizado.");
        }

        if (Pausado)
        {
            InicioAquecimento = DateTime.Now.Add(TempoRestante.Negate());
        }
        else
        {
            InicioAquecimento = DateTime.Now;
        }

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
        var isComplete = elapsed >= TempoPadrao;
        
        if (isComplete)
        {
            Finalizado = true;
            EmUso = false;
        }

        return isComplete;
    }
}