public class Aquecimento
{
    public int Id { get; private set; }
    public TimeSpan TempoTotal { get; private set; }
    public TimeSpan TempoRestante { get; private set; }
    public int Potencia { get; private set; }
    public EstadoAquecimento Estado { get; private set; }
    public string StringInformativa { get; private set; }

    // Métodos
    public void Iniciar(TimeSpan tempo, int potencia)
    public void Pausar()
    public void Retomar()
    public void Cancelar()
    public void AdicionarTempo(TimeSpan tempo)
    public void AtualizarStringInformativa()
}

public enum EstadoAquecimento
{
    Parado,
    Aquecendo,
    Pausado,
    Concluido
}