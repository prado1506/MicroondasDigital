using Microondas.Application.Services;
using Microondas.Domain;
using Microondas.Infrastructure.Repositories;

namespace Microondas.UI;

public class MicroondasUI
{
    private readonly AquecimentoService _aquecimentoService;
    private readonly ProgramaService _programaService;
    private Aquecimento? _aquecimentoAtual;
    private CancellationTokenSource? _cts;
    private Thread? _threadSimulacao;

    public MicroondasUI()
    {
        var programaRepository = new ProgramaRepository();
        _programaService = new ProgramaService(programaRepository);
        _aquecimentoService = new AquecimentoService();
    }

    public void Executar()
    {
        Console.Clear();
        ExibirBemVindo();

        bool continuar = true;
        while (continuar)
        {
            ExibirMenuPrincipal();
            string opcao = Console.ReadLine() ?? "";
            continuar = ProcessarOpcaoMenuPrincipal(opcao);
        }

        Console.WriteLine("\nObrigado por usar o Micro-ondas Digital!");
        Console.WriteLine("Pressione qualquer tecla para sair...");
        Console.ReadKey();
    }

    private void ExibirBemVindo()
    {
        Console.WriteLine("╔════════════════════════════════════╗");
        Console.WriteLine("║  *** MICRO-ONDAS DIGITAL ***       ║");
        Console.WriteLine("║  Bem-vindo ao Nível 1              ║");
        Console.WriteLine("║  Aquecimento Básico com Validações ║");
        Console.WriteLine("╚════════════════════════════════════╝\n");
    }

    private void ExibirMenuPrincipal()
    {
        Console.WriteLine("\n--- MENU PRINCIPAL ---");
        Console.WriteLine("1. Iniciar Aquecimento Manual");
        Console.WriteLine("2. Quick Start (30s - Potência 10)");
        Console.WriteLine("3. Pausar Aquecimento");
        Console.WriteLine("4. Retomar Aquecimento");
        Console.WriteLine("5. Adicionar Tempo");
        Console.WriteLine("6. Cancelar Aquecimento");
        Console.WriteLine("7. Ver Status");
        Console.WriteLine("0. Sair");
        Console.Write("\nEscolha uma opção: ");
    }

    private bool ProcessarOpcaoMenuPrincipal(string opcao)
    {
        return opcao switch
        {
            "1" => IniciarAquecimentoManual(),
            "2" => QuickStart(),
            "3" => PausarAquecimento(),
            "4" => RetomarAquecimento(),
            "5" => AdicionarTempo(),
            "6" => CancelarAquecimento(),
            "7" => VerStatus(),
            "0" => false,
            _ => ExibirOpcaoInvalida()
        };
    }

    private bool IniciarAquecimentoManual()
    {
        Console.Clear();
        Console.WriteLine("=== INICIAR AQUECIMENTO MANUAL ===\n");

        if (_aquecimentoAtual != null && _aquecimentoAtual.Estado == EstadoAquecimento.Aquecendo)
        {
            Console.WriteLine("⚠️  Há um aquecimento em andamento. Cancelando...\n");
            _aquecimentoAtual.Cancelar();
            PararSimulacao();
        }

        Console.WriteLine("Tempo de aquecimento (em segundos):");
        Console.WriteLine("Mínimo: 1s | Máximo: 120s (2 minutos)");
        Console.Write("Digite o tempo: ");

        if (!int.TryParse(Console.ReadLine(), out int segundos))
        {
            Console.WriteLine("❌ Entrada inválida! Digite um número inteiro.");
            PauseComEspera();
            return true;
        }

        Console.WriteLine("\nPotência de aquecimento:");
        Console.WriteLine("Mínimo: 1 | Máximo: 10");
        Console.Write("Digite a potência: ");

        if (!int.TryParse(Console.ReadLine(), out int potencia))
        {
            Console.WriteLine("❌ Entrada inválida! Digite um número inteiro.");
            PauseComEspera();
            return true;
        }

        try
        {
            var dto = new Microondas.Application.DTOs.CriarAquecimentoDTO(segundos, potencia);
            var aquecimentoDto = _aquecimentoService.CriarAquecimento(dto);
            _aquecimentoAtual = _aquecimentoService.ObterAquecimento(aquecimentoDto.Id);

            if (_aquecimentoAtual == null)
                throw new InvalidOperationException("Falha ao recuperar aquecimento criado");

            IniciarAquecimento();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro: {ex.Message}");
            PauseComEspera();
            return true;
        }
    }

    private bool QuickStart()
    {
        Console.Clear();
        Console.WriteLine("=== QUICK START ===\n");
        Console.WriteLine("Configuração: 30 segundos | Potência 10");
        Console.WriteLine("Pressione ENTER para iniciar...");
        Console.ReadLine();

        if (_aquecimentoAtual != null && _aquecimentoAtual.Estado == EstadoAquecimento.Aquecendo)
        {
            Console.WriteLine("⚠️  Cancelando aquecimento anterior...\n");
            _aquecimentoAtual.Cancelar();
            PararSimulacao();
        }

        try
        {
            var dto = new Microondas.Application.DTOs.CriarAquecimentoDTO(30, 10);
            var aquecimentoDto = _aquecimentoService.CriarAquecimento(dto);
            _aquecimentoAtual = _aquecimentoService.ObterAquecimento(aquecimentoDto.Id);

            if (_aquecimentoAtual == null)
                throw new InvalidOperationException("Falha ao recuperar aquecimento criado");

            IniciarAquecimento();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro: {ex.Message}");
            PauseComEspera();
            return true;
        }
    }

    private void IniciarAquecimento()
    {
        if (_aquecimentoAtual == null)
        {
            Console.WriteLine("❌ Nenhum aquecimento disponível!");
            return;
        }

        try
        {
            _aquecimentoAtual.Iniciar();
            Console.WriteLine("\n✅ Aquecimento iniciado!");
            Console.WriteLine(_aquecimentoAtual.StringInformativa);

            _cts = new CancellationTokenSource();
            _threadSimulacao = new Thread(() => SimularAquecimento(_cts.Token))
            {
                IsBackground = true
            };
            _threadSimulacao.Start();

            Console.WriteLine("\nDigite 'P' para pausar, 'C' para cancelar ou aguarde a conclusão...");
            AguardarEntrada();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao iniciar: {ex.Message}");
            PauseComEspera();
        }
    }

    private void SimularAquecimento(CancellationToken token)
    {
        if (_aquecimentoAtual == null)
            return;

        while (!token.IsCancellationRequested &&
               _aquecimentoAtual.Estado == EstadoAquecimento.Aquecendo)
        {
            Thread.Sleep(1000);
            _aquecimentoService.SimularPassagemTempo(_aquecimentoAtual.Id);

            Console.Clear();
            ExibirTelaAquecimento();

            if (_aquecimentoAtual.Estado == EstadoAquecimento.Concluido)
            {
                Console.WriteLine("\n✅ *** AQUECIMENTO CONCLUÍDO! ***");
                Console.WriteLine("🔔 Beep! Beep! Beep!");
                Thread.Sleep(2000);
                break;
            }
        }
    }

    private void AguardarEntrada()
    {
        while (_aquecimentoAtual != null &&
               _aquecimentoAtual.Estado == EstadoAquecimento.Aquecendo)
        {
            if (Console.KeyAvailable)
            {
                var tecla = Console.ReadKey(true).KeyChar;

                if (char.ToUpper(tecla) == 'P')
                {
                    _aquecimentoAtual.Pausar();
                    _cts?.Cancel();
                    Console.WriteLine("\n⏸️  Aquecimento pausado!");
                    Console.WriteLine(_aquecimentoAtual.StringInformativa);
                    break;
                }
                else if (char.ToUpper(tecla) == 'C')
                {
                    _aquecimentoAtual.Cancelar();
                    _cts?.Cancel();
                    Console.WriteLine("\n❌ Aquecimento cancelado!");
                    break;
                }
            }

            Thread.Sleep(100);
        }
    }

    private bool PausarAquecimento()
    {
        if (_aquecimentoAtual == null)
        {
            Console.WriteLine("\n❌ Nenhum aquecimento em andamento!");
            PauseComEspera();
            return true;
        }

        try
        {
            _aquecimentoAtual.Pausar();
            _cts?.Cancel();
            PararSimulacao();
            Console.WriteLine("\n⏸️  Aquecimento pausado!");
            Console.WriteLine(_aquecimentoAtual.StringInformativa);
            PauseComEspera();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Erro: {ex.Message}");
            PauseComEspera();
            return true;
        }
    }

    private bool RetomarAquecimento()
    {
        if (_aquecimentoAtual == null)
        {
            Console.WriteLine("\n❌ Nenhum aquecimento pausado!");
            PauseComEspera();
            return true;
        }

        try
        {
            _aquecimentoAtual.Retomar();
            Console.WriteLine("\n▶️  Aquecimento retomado!");
            Console.WriteLine(_aquecimentoAtual.StringInformativa);

            _cts = new CancellationTokenSource();
            _threadSimulacao = new Thread(() => SimularAquecimento(_cts.Token))
            {
                IsBackground = true
            };
            _threadSimulacao.Start();

            AguardarEntrada();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Erro: {ex.Message}");
            PauseComEspera();
            return true;
        }
    }

    private bool AdicionarTempo()
    {
        if (_aquecimentoAtual == null)
        {
            Console.WriteLine("\n❌ Nenhum aquecimento em andamento!");
            PauseComEspera();
            return true;
        }

        Console.WriteLine("\nTempo adicional (em segundos): ");
        Console.Write("Digite o tempo: ");

        if (!int.TryParse(Console.ReadLine(), out int segundos))
        {
            Console.WriteLine("❌ Entrada inválida!");
            PauseComEspera();
            return true;
        }

        try
        {
            var dto = _aquecimentoService.AdicionarTempo(_aquecimentoAtual.Id, segundos);
            _aquecimentoAtual = _aquecimentoService.ObterAquecimento(dto.Id);
            Console.WriteLine($"\n✅ Tempo adicionado!");
            Console.WriteLine(_aquecimentoAtual?.StringInformativa);
            PauseComEspera();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro: {ex.Message}");
            PauseComEspera();
            return true;
        }
    }

    private bool CancelarAquecimento()
    {
        if (_aquecimentoAtual == null)
        {
            Console.WriteLine("\n❌ Nenhum aquecimento em andamento!");
            PauseComEspera();
            return true;
        }

        try
        {
            _aquecimentoAtual.Cancelar();
            _cts?.Cancel();
            PararSimulacao();
            Console.WriteLine("\n❌ Aquecimento cancelado!");
            Console.WriteLine(_aquecimentoAtual.StringInformativa);
            _aquecimentoAtual = null;
            PauseComEspera();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Erro: {ex.Message}");
            PauseComEspera();
            return true;
        }
    }

    private bool VerStatus()
    {
        Console.Clear();
        Console.WriteLine("=== STATUS DO AQUECIMENTO ===\n");

        if (_aquecimentoAtual == null)
        {
            Console.WriteLine("❌ Nenhum aquecimento em andamento!");
        }
        else
        {
            ExibirTelaAquecimento();
        }

        PauseComEspera();
        return true;
    }

    private void ExibirTelaAquecimento()
    {
        if (_aquecimentoAtual == null)
            return;

        Console.WriteLine("┌─────────────────────────────┐");
        Console.WriteLine("│  MICRO-ONDAS DIGITAL        │");
        Console.WriteLine("└─────────────────────────────┘");
        Console.WriteLine();
        Console.WriteLine($"ID: {_aquecimentoAtual.Id}");
        Console.WriteLine($"Status: {_aquecimentoAtual.Estado}");
        Console.WriteLine($"Tempo Total: {FormatarTempo(_aquecimentoAtual.TempoTotal)}");
        Console.WriteLine($"Tempo Restante: {FormatarTempo(_aquecimentoAtual.TempoRestante)}");
        Console.WriteLine($"Potência: {_aquecimentoAtual.Potencia}/10");
        Console.WriteLine();
        Console.WriteLine(_aquecimentoAtual.StringInformativa);
        Console.WriteLine();
    }

    private bool ExibirOpcaoInvalida()
    {
        Console.WriteLine("❌ Opção inválida! Tente novamente.");
        PauseComEspera();
        return true;
    }

    private void PararSimulacao()


