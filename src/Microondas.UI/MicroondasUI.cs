public class MicroondasUI
{
    private readonly IniciarAquecimentoService _service;

    public void Exibir()
    {
        Console.WriteLine("=== MICRO-ONDAS DIGITAL ===");
        Console.WriteLine("1. Informar tempo e potência");
        Console.WriteLine("2. Quick Start (30s - Pot 10)");
        Console.WriteLine("3. Sair");

        var opcao = Console.ReadLine();
        // Processar opcoes
    }
}