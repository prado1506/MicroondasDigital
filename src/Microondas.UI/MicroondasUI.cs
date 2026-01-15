using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microondas.Application.DTOs;
using Microondas.Application.Services;
using Microondas.Domain;
using Microondas.Infrastructure.Repositories;

namespace Microondas.UI;

public class MicroondasUI
{
    private readonly AquecimentoService _aquecimentoService; // mantido para compatibilidade local, mas a UI usará a API
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

    private readonly HttpClient _http;
    private string? _accessToken;
    private bool IsAuthenticated => !string.IsNullOrWhiteSpace(_accessToken);

    public MicroondasUI()
    {
        // URLs: preferida (env ou padrão HTTPS) e fallbacks conhecidos (HTTP)
        var apiUrlEnv = Environment.GetEnvironmentVariable("MICROONDAS_API_URL");
        var preferredUrl = apiUrlEnv ?? "https://localhost:7198/";
        var fallbackUrls = new[] { "http://localhost:5123/", "https://localhost:7198/" };

        var allowInsecure = Environment.GetEnvironmentVariable("MICROONDAS_ALLOW_INSECURE") == "1";

        HttpClient? chosen = null;

        // Build candidates: preferred primeiro, depois fallbacks sem duplicatas
        var candidates = new List<string> { preferredUrl };
        foreach (var f in fallbackUrls)
        {
            if (!candidates.Contains(f, StringComparer.OrdinalIgnoreCase))
                candidates.Add(f);
        }

        foreach (var url in candidates)
        {
            HttpClientHandler? handler = null;
            HttpClient? client = null;

            try
            {
                // Cria HttpClient (aceita certs self-signed apenas se permitido)
                if (allowInsecure && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                    client = new HttpClient(handler) { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(2) };
                }
                else
                {
                    client = new HttpClient() { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(2) };
                }

                Console.WriteLine($"[DEBUG] Tentando conectar em {url} ...");
                var resp = client.GetAsync("api/programa").GetAwaiter().GetResult();

                // Considera válido: sucesso ou resposta de autenticação/autorização (servidor está presente)
                if (resp.IsSuccessStatusCode ||
                    resp.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    resp.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                    resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    chosen = client;
                    Console.WriteLine($"[DEBUG] Conectou: {url} (status {resp.StatusCode})");
                    break;
                }

                client.Dispose();
                handler?.Dispose();
            }
            catch (Exception ex)
            {
                // log curto para depuração e tenta próximo candidate
                Console.WriteLine($"[DEBUG] Falha ao conectar em {url}: {ex.Message}");
                try { client?.Dispose(); } catch { }
                try { handler?.Dispose(); } catch { }
            }
        }

        // Se não encontrou nenhum, usa a preferredUrl e deixará erros serem tratados nas chamadas
        _http = chosen ?? new HttpClient { BaseAddress = new Uri(preferredUrl), Timeout = TimeSpan.FromSeconds(10) };

        Console.WriteLine($"[DEBUG] MICROONDAS_API_URL = {_http.BaseAddress}");

        var programaRepository = new ProgramaRepository();
        _programaService = new ProgramaService(programaRepository);

        // Corrigido: cria e injeta o repositório no serviço de aquecimento
        var aquecimentoRepository = new AquecimentoRepository();
        _aquecimentoService = new AquecimentoService(aquecimentoRepository);
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
        if (!IsAuthenticated)
        {
            lock (_consoleLock)
            {
                Console.WriteLine("\n--- AUTENTICAÇÃO NECESSÁRIA ---");
                Console.WriteLine("L. Login");
                Console.WriteLine("C. Configurar credenciais");
                Console.WriteLine("0. Sair");
                Console.Write("\nEscolha uma opção: ");
            }
            return;
        }

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
                Console.WriteLine("7. Registrar Programa Customizado");
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

        // quando não autenticado, só aceitar L, C ou 0
        if (!IsAuthenticated)
        {
            return opcao.ToUpperInvariant() switch
            {
                "L" => LoginAsync().GetAwaiter().GetResult(),
                "C" => ConfigureAsync().GetAwaiter().GetResult(),
                "0" => false,
                _ => ExibirOpcaoInvalida()
            };
        }

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
            "7" => RegistrarProgramaCustomizado(),
            "0" => false,
            "p" or "P" => BotaoP(),
            _ => ExibirOpcaoInvalida()
        };
    }

    // método para login (assincrono)
    private async Task<bool> LoginAsync()
    {
        lock (_consoleLock) Console.Write("Usuário: ");
        var user = Console.ReadLine() ?? "";

        lock (_consoleLock) Console.Write("Senha: ");
        var pwd = LerSenhaMascarada();

        var payload = new { Username = user, Password = pwd };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            var resp = await _http.PostAsync("api/auth/login", content);
            if (!resp.IsSuccessStatusCode)
            {
                lock (_consoleLock) Console.WriteLine("\n❌ Falha no login: credenciais inválidas ou erro no servidor.");
                PauseComEspera();
                return false;
            }

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("access_token", out var tokenEl))
            {
                _accessToken = tokenEl.GetString();
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                lock (_consoleLock) Console.WriteLine("\n✅ Autenticação bem-sucedida.");
                PauseComEspera();
                return true;
            }

            lock (_consoleLock) Console.WriteLine("\n❌ Resposta inválida do servidor.");
            PauseComEspera();
            return false;
        }
        catch (HttpRequestException ex)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"\n❌ Erro de conexão ao autenticar: {ex.Message}");
                Console.WriteLine("→ Verifique se a API está rodando e a variável de ambiente MICROONDAS_API_URL está correta.");
                Console.WriteLine("→ Inicie a API: `dotnet run --project src/Microondas.API/Microondas.API.csproj` ou use o Visual Studio.");
            }
            PauseComEspera();
            return false;
        }
        catch (Exception ex)
        {
            lock (_consoleLock) Console.WriteLine($"\n❌ Erro ao autenticar: {ex.Message}");
            PauseComEspera();
            return false;
        }
    }

    // método para configurar credenciais na API (assincrono)
    private async Task<bool> ConfigureAsync()
    {
        lock (_consoleLock) Console.Write("Novo usuário: ");
        var user = Console.ReadLine() ?? "";

        lock (_consoleLock) Console.Write("Nova senha: ");
        var pwd = LerSenhaMascarada();

        var payload = new { Username = user, Password = pwd };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            var resp = await _http.PostAsync("api/auth/configurar", content);
            if (!resp.IsSuccessStatusCode)
            {
                // Situação comum: já existe configuração e a API exige autenticação para alterar
                if (resp.StatusCode == HttpStatusCode.Forbidden)
                {
                    var detalhe = await resp.Content.ReadAsStringAsync();
                    lock (_consoleLock)
                    {
                        Console.WriteLine("\n❌ Falha ao configurar: acesso negado (já existe configuração).");
                        Console.WriteLine("→ Faça login com o usuário atual para alterar as credenciais, ou remova o arquivo 'auth_config.json' no diretório de execução da API para recriar credenciais.");
                        if (!string.IsNullOrWhiteSpace(detalhe))
                            Console.WriteLine($"→ Detalhe do servidor: {detalhe}");
                    }
                    PauseComEspera();
                    return false;
                }

                var body = await resp.Content.ReadAsStringAsync();
                lock (_consoleLock) Console.WriteLine($"\n❌ Falha ao configurar: {resp.StatusCode}. {body}");
                PauseComEspera();
                return false;
            }

            lock (_consoleLock) Console.WriteLine("\n✅ Credenciais configuradas com sucesso.");
            PauseComEspera();
            return true;
        }
        catch (HttpRequestException ex)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"\n❌ Erro de conexão ao configurar credenciais: {ex.Message}");
                Console.WriteLine("→ Verifique se a API está rodando e se a URL está correta.");
                Console.WriteLine("→ Para executar a API localmente: `dotnet run --project src/Microondas.API/Microondas.API.csproj`");
                Console.WriteLine("→ Se a API usa HTTP em vez de HTTPS, defina MICROONDAS_API_URL (ex: http://localhost:5123/).");
            }
            PauseComEspera();
            return false;
        }
        catch (Exception ex)
        {
            lock (_consoleLock) Console.WriteLine($"\n❌ Erro ao configurar credenciais: {ex.Message}");
            PauseComEspera();
            return false;
        }
    }

    private string LerSenhaMascarada()
    {
        var sb = new StringBuilder();
        ConsoleKeyInfo key;
        while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
            {
                sb.Length--;
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                sb.Append(key.KeyChar);
                Console.Write('*');
            }
        }
        Console.WriteLine();
        return sb.ToString();
    }

    // Reintroduzido helper que faltava — exibe mensagem simples e faz pausa
    private bool ExibirMensagemTemporaria(string mensagem)
    {
        lock (_consoleLock)
        {
            Console.WriteLine(mensagem);
        }
        PauseComEspera();
        return true;
    }

    // Helper para fazer e desserializar uma resposta JSON
    private T? ReadJsonResponse<T>(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode)
        {
            var s = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<T>(s, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            _accessToken = null;
            _http.DefaultRequestHeaders.Authorization = null;
            lock (_consoleLock) Console.WriteLine("\n🔒 Sessão expirada ou não autorizada. Faça login novamente.");
            PauseComEspera();
        }
        else
        {
            lock (_consoleLock) Console.WriteLine($"\n❌ Erro: {resp.StatusCode} ({resp.ReasonPhrase})");
            PauseComEspera();
        }

        return default;
    }

    // Helper para obter aquecimento via API (sincrono)
    private AquecimentoDTO? GetAquecimentoFromApi(int id)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"api/aquecimento/{id}");
        if (!string.IsNullOrWhiteSpace(_accessToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var resp = _http.SendAsync(req).GetAwaiter().GetResult();
        return ReadJsonResponse<AquecimentoDTO>(resp);
    }

    // Helper para listar programas via API
    private IEnumerable<ProgramaDTO> ListarProgramasFromApi()
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "api/programa");
        if (!string.IsNullOrWhiteSpace(_accessToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var resp = _http.SendAsync(req).GetAwaiter().GetResult();
        var list = ReadJsonResponse<IEnumerable<ProgramaDTO>>(resp);
        return list ?? Array.Empty<ProgramaDTO>();
    }

    // Helper para criar aquecimento via API
    private AquecimentoDTO? CriarAquecimentoViaApi(CriarAquecimentoDTO dto)
    {
        var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        using var req = new HttpRequestMessage(HttpMethod.Post, "api/aquecimento/criar") { Content = content };
        if (!string.IsNullOrWhiteSpace(_accessToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var resp = _http.SendAsync(req).GetAwaiter().GetResult();
        return ReadJsonResponse<AquecimentoDTO>(resp);
    }

    // Helper para criar aquecimento com caractere via API
    private AquecimentoDTO? CriarAquecimentoComCaractereViaApi(CriarAquecimentoDTO dto, char caract)
    {
        var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        using var req = new HttpRequestMessage(HttpMethod.Post, $"api/aquecimento/criar-com-caractere?caractere={WebUtility.UrlEncode(caract.ToString())}") { Content = content };
        if (!string.IsNullOrWhiteSpace(_accessToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var resp = _http.SendAsync(req).GetAwaiter().GetResult();
        return ReadJsonResponse<AquecimentoDTO>(resp);
    }

    // Helper para iniciar/pausar/retomar/cancelar/adicionar/simular via API
    private AquecimentoDTO? PostAquecimentoAction(int id, string action)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"api/aquecimento/{id}/{action}");
        if (!string.IsNullOrWhiteSpace(_accessToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var resp = _http.SendAsync(req).GetAwaiter().GetResult();
        return ReadJsonResponse<AquecimentoDTO>(resp);
    }

    // Helper para criar/obter programas via API
    private ProgramaDTO? CriarProgramaViaApi(CriarProgramaDTO dto)
    {
        var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
        using var req = new HttpRequestMessage(HttpMethod.Post, "api/programa") { Content = content };
        if (!string.IsNullOrWhiteSpace(_accessToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var resp = _http.SendAsync(req).GetAwaiter().GetResult();
        return ReadJsonResponse<ProgramaDTO>(resp);
    }

    private bool DeletarProgramaViaApi(string id)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"api/programa/{id}");
        if (!string.IsNullOrWhiteSpace(_accessToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var resp = _http.SendAsync(req).GetAwaiter().GetResult();
        if (resp.IsSuccessStatusCode) return true;
        ReadJsonResponse<object>(resp);
        return false;
    }

    // Helper para simular passagem de tempo via API
    private AquecimentoDTO? SimularPassagemTempoViaApi(int id)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"api/aquecimento/{id}/simular");
        if (!string.IsNullOrWhiteSpace(_accessToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var resp = _http.SendAsync(req).GetAwaiter().GetResult();
        return ReadJsonResponse<AquecimentoDTO>(resp);
    }

    // restante do código usa agora os helpers acima para operar via API

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
            // cancelar via API também
            var canceled = PostAquecimentoAction(_aquecimentoAtual.Id, "cancelar");
            PararSimulacao();
            _aquecimentoAtual = canceled ?? GetAquecimentoFromApi(_aquecimentoAtual.Id);
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
            var aquecimentoDto = CriarAquecimentoViaApi(dto);

            _aquecimentoAtual = aquecimentoDto;

            if (_aquecimentoAtual == null)
            {
                lock (_consoleLock) Console.WriteLine("❌ Falha ao criar aquecimento via API.");
                PauseComEspera();
                return true;
            }

            _aquecimentoPredefinido = false; // garantido aquecimento manual

            // inicia via API
            var started = PostAquecimentoAction(_aquecimentoAtual.Id, "iniciar");
            _aquecimentoAtual = started ?? _aquecimentoAtual;

            if (_aquecimentoAtual != null && _aquecimentoAtual.Estado == EstadoAquecimento.Aquecendo.ToString())
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("\n✅ Aquecimento iniciado!");
                    Console.WriteLine("\nDigite 'P' para pausar/cancelar ou aguarde a conclusão...");
                    Console.WriteLine(_aquecimentoAtual.StringInformativa);
                }

                _cts = new CancellationTokenSource();
                _threadSimulacao = new Thread(() => SimularAquecimento(_cts.Token))
                {
                    IsBackground = true
                };
                _threadSimulacao.Start();
                AguardarEntrada();
            }

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
            // cancelar via API
            var canceled = PostAquecimentoAction(_aquecimentoAtual.Id, "cancelar");
            PararSimulacao();
            _aquecimentoAtual = canceled ?? GetAquecimentoFromApi(_aquecimentoAtual.Id);
        }

        try
        {
            var dto = new CriarAquecimentoDTO(30, 10);
            var aquecimentoDto = CriarAquecimentoViaApi(dto);
            _aquecimentoAtual = aquecimentoDto;

            if (_aquecimentoAtual == null)
            {
                lock (_consoleLock) Console.WriteLine("Falha ao criar aquecimento via API");
                PauseComEspera();
                return true;
            }

            _aquecimentoPredefinido = false; // QuickStart é considerado manual aqui

            var started = PostAquecimentoAction(_aquecimentoAtual.Id, "iniciar");
            _aquecimentoAtual = started ?? _aquecimentoAtual;

            if (_aquecimentoAtual != null && _aquecimentoAtual.Estado == EstadoAquecimento.Aquecendo.ToString())
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("\n✅ Aquecimento iniciado!");
                    Console.WriteLine(_aquecimentoAtual.StringInformativa);
                }

                _cts = new CancellationTokenSource();
                _threadSimulacao = new Thread(() => SimularAquecimento(_cts.Token))
                {
                    IsBackground = true
                };
                _threadSimulacao.Start();
                AguardarEntrada();
            }

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
        // agora o fluxo de início é feito por IniciarAquecimentoManual / QuickStart que chamam a API
        if (_aquecimentoAtual == null)
        {
            lock (_consoleLock) Console.WriteLine("❌ Nenhum aquecimento disponível!");
            return;
        }

        lock (_consoleLock)
        {
            Console.WriteLine("\nℹ️ Use o menu para iniciar/retomar o aquecimento via API.");
        }
    }

    private void SimularAquecimento(CancellationToken token)
    {
        if (_aquecimentoAtual == null) return;

        while (!token.IsCancellationRequested && _aquecimentoAtual != null && _aquecimentoAtual.Estado == EstadoAquecimento.Aquecendo.ToString())
        {
            Thread.Sleep(1000);

            var updated = SimularPassagemTempoViaApi(_aquecimentoAtual.Id);
            if (updated != null)
                _aquecimentoAtual = updated;

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
                    // durante aquecimento: P pausa (via API)
                    _suspendStatusDisplay = true;
                    var paused = PostAquecimentoAction(_aquecimentoAtual.Id, "pausar");
                    _cts?.Cancel();
                    PararSimulacao();
                    _aquecimentoAtual = paused ?? GetAquecimentoFromApi(_aquecimentoAtual.Id);

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
                // se estiver aquecendo, pausa via API
                _suspendStatusDisplay = true;
                var paused = PostAquecimentoAction(_aquecimentoAtual.Id, "pausar");
                _cts?.Cancel();
                PararSimulacao();
                _aquecimentoAtual = paused ?? GetAquecimentoFromApi(_aquecimentoAtual.Id);

                Console.WriteLine("\n⏸️ Aquecimento pausado!");
                if (_aquecimentoAtual != null)
                    Console.WriteLine(_aquecimentoAtual.StringInformativa);

                _suspendStatusDisplay = false;
                PauseComEspera();
                return true;
            }

            if (estado == EstadoAquecimento.Pausado.ToString())
            {
                // se estiver pausado e pressionar novamente, cancela e limpa estado (via API)
                _suspendStatusDisplay = true;
                var canceled = PostAquecimentoAction(_aquecimentoAtual.Id, "cancelar");
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

            var paused = PostAquecimentoAction(_aquecimentoAtual.Id, "pausar");
            _cts?.Cancel();
            PararSimulacao();
            _aquecimentoAtual = paused ?? GetAquecimentoFromApi(_aquecimentoAtual.Id);

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
            var resumed = PostAquecimentoAction(_aquecimentoAtual.Id, "retomar");
            _aquecimentoAtual = resumed ?? GetAquecimentoFromApi(_aquecimentoAtual.Id);

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

            var canceled = PostAquecimentoAction(_aquecimentoAtual.Id, "cancelar");
            _cts?.Cancel();
            PararSimulacao();
            _aquecimentoAtual = null;
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
            Console.Clear();
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
            var added = PostAquecimentoAction(_aquecimentoAtual.Id, "adicionar-tempo");
            _aquecimentoAtual = added ?? GetAquecimentoFromApi(_aquecimentoAtual.Id);

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
        var programas = ListarProgramasFromApi().ToList();
        if (!programas.Any())
        {
            lock (_consoleLock) Console.WriteLine("\nNenhum programa disponível.");
            PauseComEspera();
            return true;
        }

        lock (_consoleLock)
        {
            Console.WriteLine("\n--- PROGRAMAS (Pré-definidos e Customizados) ---");
            foreach (var p in programas)
            {
                if (p.EhCustomizado)
                    Console.WriteLine($"[{p.Identificador}] {p.Nome} (customizado) - {p.Alimento} - {p.Tempo} @ Potência {p.Potencia} | Instruções: {p.Instrucoes}");
                else
                    Console.WriteLine($"[{p.Identificador}] {p.Nome} - {p.Alimento} - {p.Tempo} @ Potência {p.Potencia} | Instruções: {p.Instrucoes}");
            }
            Console.Write("\nDigite o identificador do programa para selecionar (ex: X): ");
        }

        var escolha = (Console.ReadLine() ?? "").Trim().ToUpper();
        if (string.IsNullOrEmpty(escolha))
            return true;

        var programaDto = programas.FirstOrDefault(p => p.Identificador == escolha);
        if (programaDto == null)
        {
            lock (_consoleLock) Console.WriteLine("\nPrograma não encontrado.");
            PauseComEspera();
            return true;
        }

        // criar aquecimento com o DTO correto (tempo + potência) e usar o caractere do programa
        try
        {
            var criarDto = new CriarAquecimentoDTO(programaDto.TempoSegundos, programaDto.Potencia);

            // valida CaractereProgresso antes de indexar
            var caractereProgresso = programaDto.CaractereProgresso;
            if (string.IsNullOrEmpty(caractereProgresso))
            {
                lock (_consoleLock)
                {
                    Console.WriteLine("\n❌ Programa selecionado não possui caractere de progresso definido.");
                }
                PauseComEspera();
                return true;
            }

            var aqu = CriarAquecimentoComCaractereViaApi(criarDto, caractereProgresso[0]);
            _aquecimentoAtual = aqu;
            _aquecimentoPredefinido = true;

            lock (_consoleLock)
            {
                Console.WriteLine($"\n✅ Programa '{programaDto.Nome}' selecionado. Tempo e potência preenchidos e bloqueados.");
                Console.WriteLine(_aquecimentoAtual?.StringInformativa);
            }

            // inicia automaticamente (mantido comportamento atual)
            var started = PostAquecimentoAction(_aquecimentoAtual.Id, "iniciar");
            _aquecimentoAtual = started ?? _aquecimentoAtual;

            if (_aquecimentoAtual != null && _aquecimentoAtual.Estado == EstadoAquecimento.Aquecendo.ToString())
            {
                _cts = new CancellationTokenSource();
                _threadSimulacao = new Thread(() => SimularAquecimento(_cts.Token))
                {
                    IsBackground = true
                };
                _threadSimulacao.Start();
            }

            return true;
        }
        catch (Exception ex)
        {
            lock (_consoleLock) Console.WriteLine($"\nErro ao iniciar programa: {ex.Message}");
            PauseComEspera();
            return true;
        }
    }

    private bool RegistrarProgramaCustomizado()
    {
        // Exibe informações úteis antes de solicitar dados ao usuário
        lock (_consoleLock)
        {
            Console.WriteLine("\n=== REGISTRAR PROGRAMA CUSTOMIZADO ===\n");

            var todos = ListarProgramasFromApi().ToList();

            // Identificadores já em uso
            var ids = todos.Any() ? string.Join(", ", todos.Select(p => p.Identificador)) : "(nenhum)";
            Console.WriteLine($"Caracteres já em uso (identificadores): {ids}");

            // Caractere de aquecimento indisponíveis (inclui '.' por ser reservado)
            var charsIndisponiveis = todos
                .Select(p => (p.CaractereProgresso ?? "."))
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s[0])
                .Distinct()
                .ToList();

            if (!charsIndisponiveis.Contains('.'))
                charsIndisponiveis.Insert(0, '.');

            var charsStr = charsIndisponiveis.Any() ? string.Join(", ", charsIndisponiveis) : "(nenhum)";
            Console.WriteLine($"Caractere(s) de aquecimento indisponíveis: {charsStr}\n");

            Console.Write("Identificador (um caractere): ");
        }

        var id = (Console.ReadLine() ?? "").Trim().ToUpper();
        if (string.IsNullOrEmpty(id) || id.Length != 1)
        {
            lock (_consoleLock) Console.WriteLine("\n❌ Identificador inválido.");
            PauseComEspera();
            return true;
        }

        lock (_consoleLock) Console.Write("Nome do programa: ");
        var nome = (Console.ReadLine() ?? "").Trim();
        if (string.IsNullOrEmpty(nome))
        {
            lock (_consoleLock) Console.WriteLine("\n❌ Nome obrigatório.");
            PauseComEspera();
            return true;
        }

        lock (_consoleLock) Console.Write("Alimento (ex: Pipoca, Leite): ");
        var alimento = (Console.ReadLine() ?? "").Trim();

        lock (_consoleLock)
        {
            Console.Write("Tempo (segundos): ");
        }
        if (!int.TryParse(Console.ReadLine(), out int tempoSegundos) || tempoSegundos < 1)
        {
            lock (_consoleLock) Console.WriteLine("\n❌ Tempo inválido.");
            PauseComEspera();
            return true;
        }

        lock (_consoleLock)
        {
            Console.Write("Potência (1-10): ");
        }
        if (!int.TryParse(Console.ReadLine(), out int potencia) || potencia < 1 || potencia > 10)
        {
            lock (_consoleLock) Console.WriteLine("\n❌ Potência inválida.");
            PauseComEspera();
            return true;
        }

        lock (_consoleLock)
        {
            Console.Write("Caractere de aquecimento (um caractere, diferente de '.': ");
        }
        var caractInput = (Console.ReadLine() ?? "");
        if (string.IsNullOrWhiteSpace(caractInput) || caractInput.Length != 1)
        {
            lock (_consoleLock) Console.WriteLine("\n❌ Caractere inválido.");
            PauseComEspera();
            return true;
        }
        var caract = caractInput[0];
        if (caract == '.')
        {
            lock (_consoleLock) Console.WriteLine("\n❌ Caractere '.' é reservado.");
            PauseComEspera();
            return true;
        }

        // Verifica duplicidade do caractere de aquecimento antes de tentar criar (feedback imediato)
        var indisponiveis = ListarProgramasFromApi()
            .Select(p => p.CaractereProgresso)
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s[0])
            .Distinct()
            .ToList();
        if (indisponiveis.Contains(caract))
        {
            lock (_consoleLock) Console.WriteLine($"\n❌ Caractere '{caract}' já em uso. Escolha outro.");
            PauseComEspera();
            return true;
        }

        lock (_consoleLock) Console.Write("Instruções (opcional): ");
        var instrucoes = (Console.ReadLine() ?? "").Trim();

        try
        {
            var dto = new CriarProgramaDTO(id, nome, alimento, tempoSegundos, potencia, caract.ToString(), instrucoes);
            var programaDto = CriarProgramaViaApi(dto);

            if (programaDto == null)
            {
                lock (_consoleLock) Console.WriteLine("\n❌ Falha ao criar programa via API.");
                PauseComEspera();
                return true;
            }

            lock (_consoleLock)
            {
                Console.WriteLine($"\n✅ Programa customizado '{programaDto.Nome}' criado com identificador [{programaDto.Identificador}]");
            }
            PauseComEspera();
            return true;
        }
        catch (Exception ex)
        {
            lock (_consoleLock) Console.WriteLine($"\n❌ Erro ao criar programa: {ex.Message}");
            PauseComEspera();
            return true;
        }
    }

    // helper para chamar antes de executar qualquer ação que exija autenticação
    private bool GarantirAutenticacao()
    {
        if (IsAuthenticated) return true;
        lock (_consoleLock) Console.WriteLine("\n🔒 Você precisa autenticar-se antes. Escolha 'L' para login.");
        PauseComEspera();
        return false;
    }
}