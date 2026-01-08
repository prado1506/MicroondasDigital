namespace Microondas.Tests;

public class AquecimentoTests
{
    [Fact]
    public void DeveIniciarAquecimento()
    {
        // Arrange
        var tempo = new Tempo(TimeSpan.FromSeconds(30));
        var potencia = new Potencia(5);

        // Act
        var aquecimento = new Aquecimento(tempo, potencia);
        aquecimento.Iniciar();

        // Assert
        Assert.Equal(EstadoAquecimento.Aquecendo, aquecimento.Estado);
    }

    [Fact]
    public void DeveValidarTempoMinimo()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Tempo(TimeSpan.FromMilliseconds(500)));
    }
}