using DevMaid.Core.Interfaces;
using DevMaid.Core.Models;

using Microsoft.Extensions.Logging;

namespace DevMaid.Core.Services;

/// <summary>
/// Serviço de gerenciamento do arquivo pgpass.conf no Windows.
/// Implementa AddEntry, ListEntries e RemoveEntry para as US1, US2 e US3.
/// </summary>
public class PgPassService(ILogger<PgPassService>? logger = null) : IPgPassService
{
    private readonly ILogger<PgPassService>? _logger = logger;

    // =========================================================================
    // US1 — AddEntry
    // =========================================================================

    /// <inheritdoc />
    public PgPassResult AddEntry(PgPassEntry entry, string? filePath = null)
    {
        filePath ??= ResolveDefaultPath();

        // Validação: senha não pode ser vazia (RF-011)
        if (string.IsNullOrEmpty(entry.Password))
        {
            return PgPassResult.Fail("Erro: a senha não pode ser vazia.");
        }

        try
        {
            // Garantir que o diretório existe (RF-002)
            var directory = Path.GetDirectoryName(filePath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger?.LogInformation("Diretório criado: {Directory}", directory);
            }

            // Ler entradas existentes para verificar duplicata (RF-006)
            var entradasExistentes = ReadAllEntries(filePath).ToList();
            if (entradasExistentes.Any(e => e.IdentityKey == entry.IdentityKey))
            {
                var chave = FormatKey(entry);
                _logger?.LogInformation("Entrada já existe: {Chave}", chave);
                return PgPassResult.Duplicate($"Entrada já existe: {chave}");
            }

            // Serializar e acrescentar ao arquivo (RF-003, RF-010)
            var linha = SerializeEntry(entry);
            File.AppendAllText(filePath, linha + Environment.NewLine);

            var chaveAdicionada = FormatKey(entry);
            _logger?.LogInformation("Entrada adicionada: {Chave}", chaveAdicionada);
            return PgPassResult.Ok($"Entrada adicionada: {chaveAdicionada}");
        }
        catch (UnauthorizedAccessException ex)
        {
            // RF-012: permissão negada
            _logger?.LogError(ex, "Permissão negada ao acessar {Path}", filePath);
            return PgPassResult.Fail(
                $"Erro: sem permissão para gravar em {Path.GetDirectoryName(filePath)}. " +
                "Execute o comando em um terminal com privilégios de administrador.");
        }
        catch (IOException ex)
        {
            // RF-013: arquivo somente-leitura ou travado
            _logger?.LogError(ex, "Erro de I/O ao gravar em {Path}", filePath);
            return PgPassResult.Fail(
                "Erro: não foi possível gravar em pgpass.conf — o arquivo pode estar " +
                "somente-leitura ou em uso por outro processo.");
        }
    }

    // =========================================================================
    // US2 — ListEntries
    // =========================================================================

    /// <inheritdoc />
    public IEnumerable<PgPassEntry> ListEntries(string? filePath = null)
    {
        filePath ??= ResolveDefaultPath();
        return ReadAllEntries(filePath);
    }

    // =========================================================================
    // US3 — RemoveEntry
    // =========================================================================

    /// <inheritdoc />
    public PgPassResult RemoveEntry(PgPassEntry key, string? filePath = null)
    {
        filePath ??= ResolveDefaultPath();

        // Arquivo não existe: nada a remover
        if (!File.Exists(filePath))
        {
            var chave = FormatKey(key);
            return PgPassResult.Fail($"Entrada não encontrada: {chave}");
        }

        try
        {
            var linhasOriginais = File.ReadAllLines(filePath);
            var linhasFiltradas = new List<string>();
            var encontrada = false;

            foreach (var linha in linhasOriginais)
            {
                if (linha.StartsWith('#') || string.IsNullOrWhiteSpace(linha))
                {
                    // Preservar comentários e linhas em branco
                    linhasFiltradas.Add(linha);
                    continue;
                }

                var entrada = ParseLine(linha);
                if (entrada != null && entrada.IdentityKey == key.IdentityKey)
                {
                    encontrada = true;
                    // Não adicionar a linha removida
                }
                else
                {
                    linhasFiltradas.Add(linha);
                }
            }

            if (!encontrada)
            {
                var chave = FormatKey(key);
                return PgPassResult.Fail($"Entrada não encontrada: {chave}");
            }

            File.WriteAllLines(filePath, linhasFiltradas);
            var chaveRemovida = FormatKey(key);
            _logger?.LogInformation("Entrada removida: {Chave}", chaveRemovida);
            return PgPassResult.Ok($"Entrada removida: {chaveRemovida}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger?.LogError(ex, "Permissão negada ao acessar {Path}", filePath);
            return PgPassResult.Fail(
                $"Erro: sem permissão para gravar em {Path.GetDirectoryName(filePath)}. " +
                "Execute o comando em um terminal com privilégios de administrador.");
        }
        catch (IOException ex)
        {
            _logger?.LogError(ex, "Erro de I/O ao gravar em {Path}", filePath);
            return PgPassResult.Fail(
                "Erro: não foi possível gravar em pgpass.conf — o arquivo pode estar " +
                "somente-leitura ou em uso por outro processo.");
        }
    }

    // =========================================================================
    // Métodos privados — serialização, parse, escape, path
    // =========================================================================

    /// <summary>
    /// Resolve o caminho padrão do pgpass.conf no Windows.
    /// %APPDATA%\postgresql\pgpass.conf
    /// </summary>
    private static string ResolveDefaultPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "postgresql", "pgpass.conf");
    }

    /// <summary>
    /// Lê todas as entradas válidas do arquivo. Retorna vazio se o arquivo não existir.
    /// Ignora linhas de comentário (iniciadas por '#') e linhas em branco.
    /// </summary>
    private static IEnumerable<PgPassEntry> ReadAllEntries(string filePath)
    {
        if (!File.Exists(filePath))
        {
            yield break;
        }

        foreach (var linha in File.ReadAllLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(linha) || linha.StartsWith('#'))
            {
                continue;
            }

            var entrada = ParseLine(linha);
            if (entrada != null)
            {
                yield return entrada;
            }
        }
    }

    /// <summary>
    /// Serializa uma <see cref="PgPassEntry"/> para o formato pgpass.
    /// Aplica escape na senha antes de gravar: ':' → '\:' e '\' → '\\'
    /// </summary>
    internal static string SerializeEntry(PgPassEntry entry)
    {
        var senhaCom = EscapePassword(entry.Password);
        return $"{entry.Hostname}:{entry.Port}:{entry.Database}:{entry.Username}:{senhaCom}";
    }

    /// <summary>
    /// Faz parse de uma linha pgpass para <see cref="PgPassEntry"/>.
    /// Aplica unescape na senha: '\:' → ':' e '\\' → '\'
    /// Retorna null se a linha for inválida (menos de 5 campos).
    /// </summary>
    internal static PgPassEntry? ParseLine(string linha)
    {
        // O formato pgpass usa ':' como separador mas ':' pode ser escapado como '\:'
        // Precisamos dividir apenas pelos ':' não escapados
        var campos = SplitEscaped(linha);
        if (campos.Count < 5)
        {
            return null;
        }

        return new PgPassEntry
        {
            Hostname = campos[0],
            Port = campos[1],
            Database = campos[2],
            Username = campos[3],
            Password = UnescapePassword(string.Join(":", campos.Skip(4)))
        };
    }

    /// <summary>
    /// Divide uma linha pgpass pelos ':' não escapados.
    /// Respeita o escape '\:' para não dividir no separador escapado.
    /// </summary>
    private static List<string> SplitEscaped(string linha)
    {
        var campos = new List<string>();
        var campoAtual = new System.Text.StringBuilder();

        for (var i = 0; i < linha.Length; i++)
        {
            if (linha[i] == '\\' && i + 1 < linha.Length)
            {
                // Sequência de escape: consumir os dois caracteres
                campoAtual.Append(linha[i]);
                campoAtual.Append(linha[i + 1]);
                i++;
            }
            else if (linha[i] == ':')
            {
                campos.Add(campoAtual.ToString());
                campoAtual.Clear();
            }
            else
            {
                campoAtual.Append(linha[i]);
            }
        }

        campos.Add(campoAtual.ToString());
        return campos;
    }

    /// <summary>
    /// Aplica escape na senha conforme especificação pgpass:
    /// '\' → '\\' (deve ser feito ANTES de escapar ':')
    /// ':' → '\:'
    /// </summary>
    internal static string EscapePassword(string password)
    {
        // Ordem importa: escapar '\' primeiro para não re-escapar '\:'
        return password
            .Replace(@"\", @"\\")
            .Replace(":", @"\:");
    }

    /// <summary>
    /// Remove escape da senha lida do arquivo pgpass:
    /// '\:' → ':'
    /// '\\' → '\'
    /// </summary>
    internal static string UnescapePassword(string escaped)
    {
        // Processar caracter a caracter para ordem correta
        var resultado = new System.Text.StringBuilder();
        for (var i = 0; i < escaped.Length; i++)
        {
            if (escaped[i] == '\\' && i + 1 < escaped.Length)
            {
                var proximo = escaped[i + 1];
                if (proximo == ':')
                {
                    resultado.Append(':');
                    i++;
                }
                else if (proximo == '\\')
                {
                    resultado.Append('\\');
                    i++;
                }
                else
                {
                    resultado.Append(escaped[i]);
                }
            }
            else
            {
                resultado.Append(escaped[i]);
            }
        }

        return resultado.ToString();
    }

    /// <summary>
    /// Formata a chave de identidade de uma entrada para exibição.
    /// Exemplo: "localhost:5432:meu_banco:postgres"
    /// </summary>
    private static string FormatKey(PgPassEntry entry)
        => $"{entry.Hostname}:{entry.Port}:{entry.Database}:{entry.Username}";
}
