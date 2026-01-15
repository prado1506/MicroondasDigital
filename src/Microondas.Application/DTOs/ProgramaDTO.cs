namespace Microondas.Application.DTOs;

public record ProgramaDTO(
    string Identificador,
    string Nome,
    string Tempo,
    int TempoSegundos,
    int Potencia,
    string Instrucoes,
    bool EhCustomizado,
    string CaractereProgresso
);

public record CriarProgramaDTO(
    string Identificador,
    string Nome,
    int TempoSegundos,
    int Potencia,
    string Instrucoes
);
