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

    // Armazena últimos valores digitados (para poder "limpar" quando o botão P for pressionado antes do início)
    private int? _ultimoTempoDigitado;
    private int? _ultimaPotenciaDigitada;

    private bool _aquecimentoPredefinido; // true se o aquecimento atual foi criado a partir de programa pré-definido

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

            // Botão único 'p' (caso o usuário digite 'p' no prompt do menu)
            if (string.Equals(opcao?.Trim(), "p", StringComparison.OrdinalIgnoreCase))
            {
                continuar = BotaoP();
            }
            else
            {
                continuar = ProcessarOpcaoMenuPrincipal(opcao);
            }
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
        // Se há um aquecimento em andamento ou pausado, exibir um menu restrito
        var emAndamento = _aquecimentoAtual != null &&
                          (_aquecimentoAtual.Estado == EstadoAquecimento.Aquecendo.ToString()
                           || _aquecimentoAtual.Estado == EstadoAquecimento.Pausado.ToString());

        lock (_consoleLock)
        {
            if (emAndamento)
            {
                Console.WriteLine("\n--- MENU (Aquecimento em andamento) ---");
                Console.WriteLine("3. Retomar Aquecimento");
                // Exibir opção de adicionar tempo apenas se o aquecimento atual permite (não predefinido)
                if (_aquecimentoPredefinido)
                    Console.WriteLine("4. Adicionar Tempo (+30s)  (NÃO PERMITIDO para programa pré-definido)");
                else
                    Console.WriteLine("4. Adicionar Tempo (+30s)");
                Console.WriteLine("5. Ver Status");
                Console.WriteLine("0. Sair");
                Console.WriteLine("P. Pausar/Cancelar");
                Console.Write("\nEscolha uma opção: ");
            }
            else
            {
                Console.WriteLine("\n--- MENU PRINCIPAL ---");
                Console.WriteLine("1. Iniciar Aquecimento Manual");
                Console.WriteLine("2. Quick Start (30s - Potência 10)");
                Console.WriteLine("3. Retomar Aquecimento");
                Console.WriteLine("4. Adicionar Tempo (+30s)");
                Console.WriteLine("5. Ver Status");
                Console.WriteLine("6. Selecionar Programa Pré-Definido");
                Console.WriteLine("0. Sair");
                Console.WriteLine("P. Pausar/Cancelar");
                Console.Write("\nEscolha uma opção: ");
            }
        }
    }

    private bool ProcessarOpcaoMenuPrincipal(string? opcao)
    {
        // trata null ou vazio como inválido
        if (string.IsNullOrWhiteSpace(opcao))
            return ExibirOpcaoInvalida();

        var emAndamento = _aquecimentoAtual != null &&
                          (_aquecimentoAtual.Estado == EstadoAquecimento.Aquecendo.ToString()
                           || _aquecimentoAtual.Estado == EstadoAquecimento.Pausado.ToString());

        if (emAndamento)
        {
            // Menu restrito quando há aquecimento em andamento/pausado
            return opcao switch
            {
                "3" => RetomarAquecimento(),
                "4" => // AdicionarTempo só permitido quando não for predefinido
                    _aquecimentoPredefinido
                        ? ExibirMensagemTemporaria("\n❌ Não é permitido adicionar tempo a aquecimentos iniciados por programa pré-definido.")
                        : AdicionarTempo(),
                "5" => VerStatus(),
                "0" => false,
                "p" or "P" => BotaoP(),
                _ => ExibirOpcaoInvalida()
            };
        }

        // Menu normal
        return opcao switch
        {
            "1" => IniciarAquecimentoManual(Get_aquecimentoService()),
            "2" => QuickStart(),
            "3" => RetomarAquecimento(),
            "4" => AdicionarTempo(),
            "5" => VerStatus(),
            "6" => SelecionarProgramaPredefinido(),
            "0" => false,
            "p" or "P" => BotaoP(),
            _ => ExibirOpcaoInvalida()
        };
    }

    // Helper para mostrar mensagem simples dentro do fluxo do menu restrito
    private bool ExibirMensagemTemporaria(string mensagem)
    {
        lock (_consoleLock)
        {
            Console.WriteLine(mensagem);
        }
        PauseComEspera();
        return true;
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

        // guarda últimos valores digitados (para limpeza pelo botão P antes do início)
        _ultimoTempoDigitado = segundos;
        _ultimaPotenciaDigitada = potencia;

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

            _aquecimentoPredefinido = false; // garantido aquecimento manual
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

        // guarda últimos valores padrão também
        _ultimoTempoDigitado = 30;
        _ultimaPotenciaDigitada = 10;

        if (_aquecimentoAtual != null && _aquecimentoAtual.Estado == EstadoAquecimento.Aquecendo.ToString())
        {
            // se já estava aquecendo, não iniciar outro -- cancelar primeiro
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

            _aquecimentoPredefinido = false; // QuickStart é considerado manual aqui
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
                Console.WriteLine("\nDigite 'P' para pausar/cancelar ou aguarde a conclusão...");
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
                    // durante aquecimento: P pausa
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

                    // permanece pausado; usuário pode apertar 'p' novamente no menu para cancelar/limpar
                    _suspendStatusDisplay = false;
                    break;
                }
            }

            Thread.Sleep(100);
        }
    }

    // Nova rotina: comportamento de um único botão P de acordo com o estado
    private bool BotaoP()
    {
        lock (_consoleLock)
        {
            if (_aquecimentoAtual == null)
            {
                // pressionado antes do início: limpa campos de tempo e potência
                _ultimoTempoDigitado = null;
                _ultimaPotenciaDigitada = null;
                Console.WriteLine("\n✅ Campos de tempo e potência limpos.");
                PauseComEspera();
                Console.Clear();
                return true;
            }

            var estado = _aquecimentoAtual.Estado;

            if (estado == EstadoAquecimento.Aquecendo.ToString())
            {
                // se estiver aquecendo, pausa
                _suspendStatusDisplay = true;
                _aquecimentoService.PausarAquecimento(_aquecimentoAtual.Id);
                _cts?.Cancel();
                PararSimulacao();
                _aquecimentoAtual = _aquecimentoService.ObterAquecimento(_aquecimentoAtual.Id);

                Console.WriteLine("\n⏸️ Aquecimento pausado!");
                if (_aquecimentoAtual != null)
                    Console.WriteLine(_aquecimentoAtual.StringInformativa);

                _suspendStatusDisplay = false;
                PauseComEspera();
                return true;
            }

            if (estado == EstadoAquecimento.Pausado.ToString())
            {
                // se estiver pausado e pressionar novamente, cancela e limpa estado
                _suspendStatusDisplay = true;
                _aquecimentoService.CancelarAquecimento(_aquecimentoAtual.Id);
                _cts?.Cancel();
                PararSimulacao();
                _aquecimentoAtual = null;
                _ultimoTempoDigitado = null;
                _ultimaPotenciaDigitada = null;
                _aquecimentoPredefinido = false;

                Console.WriteLine("\n❌ Aquecimento cancelado e estado limpo!");
                PauseComEspera();
                Console.Clear();
                _suspendStatusDisplay = false;
                return true;
            }

            // outros estados: informa que não há ação
            Console.WriteLine("\nℹ️ Botão P sem efeito no estado atual.");
            PauseComEspera();
            return true;
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
            _suspendStatusDisplay = false;
            return true;
        }
        catch (Exception ex)
        {
            lock (_consoleLock) Console.WriteLine($"\n❌ Erro: {ex.Message}");
            PauseComEspera();
            _suspendStatusDisplay = false;
            return true;
        }
    }

    private bool RetomarAquecimento()
    {
        if (_aquecimentoAtual == null)
        {
            lock (_consoleLock) Console.WriteLine("\n❌ Nenhum aquecimento pausado!");
            PauseComEspera();
            Console.Clear();
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
            _aquecimentoPredefinido = false;

            lock (_consoleLock)
            {
                Console.WriteLine("\n❌ Aquecimento cancelado!");
                PauseComEspera();
                Console.Clear();
            }
            _suspendStatusDisplay = false;
            return true;
        }
        catch (Exception ex)
        {
            lock (_consoleLock) Console.WriteLine($"\n❌ Erro ao cancelar: {ex.Message}");
            PauseComEspera();
            _suspendStatusDisplay = false;
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
                Console.Clear();
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

        if (_aquecimentoPredefinido)
        {
            lock (_consoleLock) Console.WriteLine("\n❌ Não é permitido adicionar tempo a aquecimentos iniciados por programa pré-definido.");
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

    private bool SelecionarProgramaPredefinido()
    {
        var programas = _programaService.ListarProgramasPreDefinidos().ToList();
        if (!programas.Any())
        {
            lock (_consoleLock) Console.WriteLine("\nNenhum programa pré-definido disponível.");
            PauseComEspera();
            return true;
        }

        lock (_consoleLock)
        {
            Console.WriteLine("\n--- PROGRAMAS PRÉ-DEFINIDOS ---");
            foreach (var p in programas)
            {
                Console.WriteLine($"[{p.Identificador}] {p.Nome} - {p.Tempo} @ Potência {p.Potencia} | Instruções: {p.Instrucoes}");
            }
            Console.Write("\nDigite o identificador do programa para selecionar (ex: X): ");
        }

        var escolha = (Console.ReadLine() ?? "").Trim().ToUpper();
        if (string.IsNullOrEmpty(escolha))
        {
            return true;
        }

        var programaDto = _programaService.ObterPrograma(escolha);
        if (programaDto == null)
        {
            lock (_consoleLock) Console.WriteLine("\nPrograma não encontrado.");
            PauseComEspera();
            return true;
        }

        // auto-preenche e bloqueia — criar aquecimento usando o caractere do programa e permitindo tempo >120s
        try
        {
            var dto = new CriarAquecimentoDTO(programaDto.TempoSegundos, programaDto.Potencia);
            var aqu = _aquecimentoService.CriarAquecimentoComCaractere(dto, programaDto.CaractereProgresso[0]);
            _aquecimentoAtual = _aquecimentoService.ObterAquecimento(aqu.Id);
            _aquecimentoPredefinido = true;

            lock (_consoleLock)
            {
                Console.WriteLine($"\n✅ Programa '{programaDto.Nome}' selecionado. Tempo e potência preenchidos e bloqueados.");
                Console.WriteLine(_aquecimentoAtual?.StringInformativa);
            }

            // Inicia automaticamente.
            IniciarAquecimento();
            return true;
        }
        catch (Exception ex)
        {
            lock (_consoleLock) Console.WriteLine($"\nErro ao iniciar programa: {ex.Message}");
            PauseComEspera();
            return true;
        }
    }
}