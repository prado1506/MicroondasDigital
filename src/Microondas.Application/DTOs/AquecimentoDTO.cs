namespace Microondas.Application.DTOs;

public record AquecimentoDTO(
    int Id,
    string TempoTotal,
    string TempoRestante,
    int Potencia,
    string Estado,
    string StringInformativa
);

public record CriarAquecimentoDTO(int TempoSegundos, int Potencia);
