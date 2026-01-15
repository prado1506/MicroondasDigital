using Microondas.Application.DTOs;
using Microondas.Application.Services;
using Microondas.Domain;
using Microondas.Infrastructure.Repositories;

namespace Microondas.UI;

public class MicroondasUI
{
    private readonly AquecimentoService _aquecimentoService;
    private readonly ProgramaService _programaService;

    private AquecimentoDTO? _aquecimentoAtual;
    private CancellationTokenSource? _cts;
    private Thread? _threadSimulacao;
    private readonly object _consoleLock = new(); // evita interleaving de escrita no console
    private volatile bool _suspendStatusDisplay;

    public MicroondasUI()
    {
        var programaRepository = new ProgramaRepository();
        _programaService = new ProgramaService(programaRepository);
        _aquecimentoService = new AquecimentoService();
    }

    public void Executar()
    {
        ExibirBemVindo();

        bool continuar = true;
        while (continuar)
        {
            _suspendStatusDisplay = true;
            ExibirMenuPrincipal();
            string opcao = Console.ReadLine() ?? "";
            _suspendStatusDisplay = false;

            continuar = ProcessarOpcaoMenuPrincipal(opcao);
        }

        lock (_consoleLock)
        {
            Console.WriteLine("\nObrigado por usar o Micro-ondas Digital!");
            Console.WriteLine("Pressione qualquer tecla para sair...");
        }
        Console.ReadKey();
    }

    private void ExibirBemVindo()
    {
        lock (_consoleLock)
        {
            Console.WriteLine("╔════════════════════════════════════╗");
            Console.WriteLine("║ *** MICRO-ONDAS DIGITAL ***        ║");
            Console.WriteLine("║ Bem-vindo ao Nível 1               ║");
            Console.WriteLine("║ Aquecimento Básico com Validações  ║");
            Console.WriteLine("╚════════════════════════════════════╝\n");
        }
    }

    private void ExibirMenuPrincipal()
    {
        lock (_consoleLock)
        {
            Console.WriteLine("\n--- MENU PRINCIPAL ---");
            Console.WriteLine("1. Iniciar Aquecimento Manual");
            Console.WriteLine("2. Quick Start (30s - Potência 10)");
            Console.WriteLine("3. Pausar Aquecimento");
            Console.WriteLine("4. Retomar Aquecimento");
            Console.WriteLine("5. Adicionar Tempo (+30s)");
            Console.WriteLine("6. Cancelar Aquecimento");
            Console.WriteLine("7. Ver Status");
            Console.WriteLine("0. Sair");
            Console.Write("\nEscolha uma opção: ");
        }
    }

    private bool ProcessarOpcaoMenuPrincipal(string opcao)
    {
        return opcao switch
        {
            "1" => IniciarAquecimentoManual(Get_aquecimentoService()),
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

    private AquecimentoService Get_aquecimentoService()
    {
        return _aquecimentoService;
    }

    private bool IniciarAquecimentoManual(AquecimentoService _aquecimentoService)
    {
        lock (_consoleLock)
        {
            Console.WriteLine("=== INICIAR AQUECIMENTO MANUAL ===\n");
        }

        if (_aquecimentoAtual != null && _aquecimentoAtual.Estado == EstadoAquecimento.Aquecendo.ToString())
        {
            _aquecimentoService.CancelarAquecimento(_aquecimentoAtual.Id);
            PararSimulacao();
            _aquecimentoAtual = _aquecimentoService.ObterAquecimento(_aquecimentoAtual.Id);
        }

        lock (_consoleLock)
        {
            Console.WriteLine("Tempo de aquecimento (em segundos):");
            Console.WriteLine("Mínimo: 1s | Máximo: 120s (2 minutos)");
            Console.Write("Digite o tempo: ");
        }

        if (!int.TryParse(Console.ReadLine(), out int segundos))
        {
            lock (_consoleLock) Console.WriteLine("❌ Entrada inválida! Digite um número inteiro.");
            PauseComEspera();
            return true;
        }

        lock (_consoleLock)
        {
            Console.WriteLine("\nPotência de aquecimento:");
            Console.WriteLine("Mínimo: 1 | Máximo: 10");
            Console.Write("Digite a potência: ");
        }
        if (!int.TryParse(Console.ReadLine(), out int potencia))
        {
            lock (_consoleLock) Console.WriteLine("❌ Entrada não informada, potencia padrão: 10.");
            potencia = 10;
        }

        try
        {
            var dto = new CriarAquecimentoDTO(segundos, potencia);
            var aquecimentoDto = _aquecimentoService.CriarAquecimento(dto);

            _aquecimentoAtual = _aquecimentoService.ObterAquecimento(aquecimentoDto.Id);

            if (_aquecimentoAtual == null)
            {
                lock (_consoleLock) Console.WriteLine("❌ Falha ao recuperar aquecimento criado.");
                PauseComEspera();
                return true;
            }

            IniciarAquecimento();
            return true;

        }
        catch (Exception ex)
        {
            lock (_consoleLock) Console.WriteLine($"❌ Erro: {ex.Message}");
            PauseComEspera();
            return true;
        }
    }

    private bool QuickStart()
    {
        lock (_consoleLock)
        {
            Console.Clear();
            Console.WriteLine("=== QUICK START ===\n");
            Console.WriteLine("Configuração: 30 segundos | Potência 10");
            Console.WriteLine("Pressione ENTER para iniciar...");
        }
        Console.ReadLine();

        if (_aquecimentoAtual != null && _aquecimentoAtual.Estado == EstadoAquecimento.Aquecendo.ToString())
        {
            _aquecimentoService.CancelarAquecimento(_aquecimentoAtual.Id);
            PararSimulacao();
            _aquecimentoAtual = _aquecimentoService.ObterAquecimento(_aquecimentoAtual.Id);
        }

        try
        {
            var dto = new Microondas.Application.DTOs.CriarAquecimentoDTO(30, 10);
            var aquecimentoDto = _aquecimentoService.CriarAquecimento(dto);
            _aquecimentoAtual = _aquecimentoService.ObterAquecimento(aquecimentoDto.Id);

            if (_aquecimentoAtual == null)
            {
                lock (_consoleLock) Console.WriteLine("Falha ao recuperar aquecimento criado");
                PauseComEspera();
                return true;
            }

            IniciarAquecimento();
            return true;
        }
        catch (Exception ex)
        {
            lock (_consoleLock) Console.WriteLine($"❌ Erro: {ex.Message}");
            PauseComEspera();
            return true;
        }
    }

    private void IniciarAquecimento()
    {
        if (_aquecimentoAtual == null)
        {
            lock (_consoleLock) Console.WriteLine("❌ Nenhum aquecimento disponível!");
            return;
        }

        try
        {
            _aquecimentoService.IniciarAquecimento(_aquecimentoAtual.Id);
            _aquecimentoAtual = _aquecimentoService.ObterAquecimento(_aquecimentoAtual.Id);

            lock (_consoleLock)
            {
                Console.WriteLine("\n✅ Aquecimento iniciado!");
                if (_aquecimentoAtual != null)
                    Console.WriteLine(_aquecimentoAtual.StringInformativa);
                else
                    Console.WriteLine("Informação do aquecimento indisponível.");
            }

            _cts = new CancellationTokenSource();
            _threadSimulacao = new Thread(() => SimularAquecimento(_cts.Token))
            {
                IsBackground = true
            };
            _threadSimulacao.Start();

            lock (_consoleLock) Console.WriteLine("\nDigite 'P' para pausar, 'C' para cancelar ou aguarde a conclusão...");
            AguardarEntrada();
        }
        catch (Exception ex)
        {
            lock (_consoleLock) Console.WriteLine($"❌ Erro ao iniciar: {ex.Message}");
            PauseComEspera();
        }
    }

    private void SimularAquecimento(CancellationToken token)
    {
        if (_aquecimentoAtual == null) return;

        while (!token.IsCancellationRequested && _aquecimentoAtual != null && _aquecimentoAtual.Estado == EstadoAquecimento.Aquecendo.ToString())
        {
            Thread.Sleep(1000);
            _aquecimentoAtual = _aquecimentoService.SimularPassagemTempo(_aquecimentoAtual.Id);

            // Não escreve o status enquanto o menu/entrada estiver ativa
            if (_suspendStatusDisplay)
                continue;

            lock (_consoleLock)
            {
                ExibirTelaAquecimento();
            }

            if (_aquecimentoAtual != null && _aquecimentoAtual.Estado == EstadoAquecimento.Concluido.ToString())
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("\n✅ *** AQUECIMENTO CONCLUÍDO! ***");
                    Console.WriteLine("🔔 Beep! Beep! Beep!");
                }
                Thread.Sleep(2000);
                break;
            }
        }
    }

    private void ExibirTelaAquecimento()
    {
        // Exibir apenas a seção de status — assume que quem chamou já adquiriu _consoleLock quando necessário
        Console.WriteLine("=== AQUECIMENTO EM ANDAMENTO ===\n");

        if (_aquecimentoAtual == null)
        {
            Console.WriteLine("Nenhum aquecimento em execução.");
            return;
        }

        Console.WriteLine(_aquecimentoAtual.StringInformativa);
    }

    private void AguardarEntrada()
    {
        while (_aquecimentoAtual != null && _aquecimentoAtual.Estado == EstadoAquecimento.Aquecendo.ToString())
        {
            if (Console.KeyAvailable)
            {
                var tecla = Console.ReadKey(true).KeyChar;
                if (char.ToUpper(tecla) == 'P')
                {
                    // garanta que nenhuma nova linha de status será exibida enquanto processamos a pausa
                    _suspendStatusDisplay = true;

                    _aquecimentoService.PausarAquecimento(_aquecimentoAtual.Id);

                    // sinaliza a thread de simulação e aguarda ela terminar para evitar prints concorrentes
                    _cts?.Cancel();
                    PararSimulacao(); // agora espera indefinidamente até a thread terminar

                    lock (_consoleLock)
                    {
                        Console.WriteLine("\n⏸️ Aquecimento pausado!");
                        if (_aquecimentoAtual != null)
                            Console.WriteLine(_aquecimentoAtual.StringInformativa);
                        else
                            Console.WriteLine("Informação do aquecimento indisponível.");
                    }

                    break;
                }
                else if (char.ToUpper(tecla) == 'C')
                {
                    _suspendStatusDisplay = true;

                    _aquecimentoService.CancelarAquecimento(_aquecimentoAtual.Id);

                    _cts?.Cancel();
                    PararSimulacao();

                    lock (_consoleLock)
                    {
                        Console.WriteLine("\n❌ Aquecimento cancelado!");
                    }

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
            lock (_consoleLock) Console.WriteLine("\n❌ Nenhum aquecimento em andamento!");
            PauseComEspera();
            return true;
        }

        try
        {
            _suspendStatusDisplay = true;

            _aquecimentoService.PausarAquecimento(_aquecimentoAtual.Id);
            _cts?.Cancel();
            PararSimulacao();
            _aquecimentoAtual = _aquecimentoService.ObterAquecimento(_aquecimentoAtual.Id);

            lock (_consoleLock)
            {
                Console.WriteLine("\n⏸️ Aquecimento pausado!");
                if (_aquecimentoAtual != null)
                    Console.WriteLine(_aquecimentoAtual.StringInformativa);
                else
                    Console.WriteLine("Informação do aquecimento indisponível.");
            }

            PauseComEspera();
            return true;
        }
        catch (Exception ex)
        {
            lock (_consoleLock) Console.WriteLine($"\n❌ Erro: {ex.Message}");
            PauseComEspera();
            return true;
        }
    }

    private bool RetomarAquecimento()
    {
        if (_aquecimentoAtual == null)
        {
            lock (_consoleLock) Console.WriteLine("\n❌ Nenhum aquecimento pausado!");
            PauseComEspera();
            return true;
        }

        try
        {
            _aquecimentoService.RetomarAquecimento(_aquecimentoAtual.Id);
            _aquecimentoAtual = _aquecimentoService.ObterAquecimento(_aquecimentoAtual.Id);

            lock (_consoleLock)
            {
                Console.WriteLine("\n▶️ Aquecimento retomado!");
                if (_aquecimentoAtual != null)
                    Console.WriteLine(_aquecimentoAtual.StringInformativa);
                else
                    Console.WriteLine("Informação do aquecimento indisponível.");
            }

            _suspendStatusDisplay = false;
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
            lock (_consoleLock) Console.WriteLine($"\n❌ Erro: {ex.Message}");
            PauseComEspera();
            return true;
        }
    }

    private void PararSimulacao()
    {
        try
        {
            _cts?.Cancel();

            if (_threadSimulacao != null && _threadSimulacao.IsAlive)
            {
                // aguarda até a thread terminar — evita prints residuais após pausa/cancelamento
                _threadSimulacao.Join();
            }
        }
        finally
        {
            _cts = null;
            _threadSimulacao = null;
        }
    }

    private bool CancelarAquecimento()
    {
        if (_aquecimentoAtual == null)
        {
            lock (_consoleLock) Console.WriteLine("\n❌ Nenhum aquecimento em andamento!");
            PauseComEspera();
            return true;
        }

        try
        {
            _suspendStatusDisplay = true;

            _aquecimentoService.CancelarAquecimento(_aquecimentoAtual.Id);
            _cts?.Cancel();
            PararSimulacao();
            _aquecimentoAtual = _aquecimentoService.ObterAquecimento(_aquecimentoAtual.Id);

            lock (_consoleLock)
            {
                Console.WriteLine("\n❌ Aquecimento cancelado!");
                PauseComEspera();
                Console.Clear();
            }
            return true;
        }
        catch (Exception ex)
        {
            lock (_consoleLock) Console.WriteLine($"\n❌ Erro ao cancelar: {ex.Message}");
            PauseComEspera();
            return true;
        }
    }

    private bool VerStatus()
    {
        lock (_consoleLock)
        {
            Console.WriteLine("=== STATUS DO AQUECIMENTO ===\n");

            if (_aquecimentoAtual == null)
            {
                Console.WriteLine("Nenhum aquecimento configurado.");
                PauseComEspera();
                return true;
            }

            Console.WriteLine($"ID: {_aquecimentoAtual.Id}");
            Console.WriteLine($"Estado: {_aquecimentoAtual.Estado}");
            Console.WriteLine(_aquecimentoAtual.StringInformativa);
        }

        PauseComEspera();
        return true;
    }

    private bool AdicionarTempo()
    {
        if (_aquecimentoAtual == null)
        {
            lock (_consoleLock) Console.WriteLine("\n❌ Nenhum aquecimento para adicionar tempo!");
            PauseComEspera();
            return true;
        }

        try
        {
            _aquecimentoService.AdicionarTempo(_aquecimentoAtual.Id);
            _aquecimentoAtual = _aquecimentoService.ObterAquecimento(_aquecimentoAtual.Id);

            lock (_consoleLock)
            {
                Console.WriteLine("\n✅ 30 segundos adicionados com sucesso!");
                if (_aquecimentoAtual != null)
                {
                    Console.WriteLine(_aquecimentoAtual.StringInformativa);
                }
            }

            PauseComEspera();
            RetomarAquecimento();
            return true;
        }
        catch (Exception ex)
        {
            lock (_consoleLock) Console.WriteLine($"\n❌ Erro ao adicionar tempo: {ex.Message}");
            PauseComEspera();
            return true;
        }
    }

    private bool ExibirOpcaoInvalida()
    {
        lock (_consoleLock) Console.WriteLine("\n❌ Opção inválida! Tente novamente.");
        PauseComEspera();
        return true;
    }

    private void PauseComEspera()
    {
        lock (_consoleLock)
        {
            Console.WriteLine("\nPressione qualquer tecla para Retornar/Continuar...");
        }
        Console.ReadKey(true);
    }
}