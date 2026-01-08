namespace Microondas.Application.DTOs;

public record ProgramaDTO(
    string Identificador,
    string Nome,
    string Tempo,
    int Potencia,
    string Instrucoes,
    bool EhCustomizado
);

public record CriarProgramaDTO(
    string Identificador,
    string Nome,
    int TempoSegundos,
    int Potencia,
    string Instrucoes
);
