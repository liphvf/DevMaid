using FurLab.Core.Models;

namespace FurLab.Core.Interfaces;

/// <summary>
/// Contrato do serviço de gerenciamento do arquivo pgpass.conf.
/// </summary>
public interface IPgPassService
{
    /// <summary>
    /// Adiciona uma nova entrada ao pgpass.conf.
    /// Cria o diretório e o arquivo se necessário.
    /// Detecta duplicatas e retorna <see cref="PgPassResult.Duplicate"/> sem modificar o arquivo.
    /// </summary>
    /// <param name="entry">Entrada a adicionar. Senha nunca pode ser vazia.</param>
    /// <param name="filePath">Caminho completo do pgpass.conf. Se nulo, usa o caminho padrão do Windows.</param>
    /// <returns>Resultado da operação.</returns>
    PgPassResult AddEntry(PgPassEntry entry, string? filePath = null);

    /// <summary>
    /// Lista todas as entradas do pgpass.conf.
    /// Retorna enumerável vazio se o arquivo não existir ou estiver vazio.
    /// Linhas de comentário (iniciadas por '#') são ignoradas.
    /// </summary>
    /// <param name="filePath">Caminho completo do pgpass.conf. Se nulo, usa o caminho padrão do Windows.</param>
    /// <returns>Enumerável de entradas parseadas.</returns>
    IEnumerable<PgPassEntry> ListEntries(string? filePath = null);

    /// <summary>
    /// Remove a entrada identificada pela chave (Hostname, Port, Database, Username).
    /// Se a entrada não for encontrada, retorna resultado informativo sem modificar o arquivo.
    /// </summary>
    /// <param name="key">Entrada contendo a chave de identidade para localizar e remover.</param>
    /// <param name="filePath">Caminho completo do pgpass.conf. Se nulo, usa o caminho padrão do Windows.</param>
    /// <returns>Resultado da operação.</returns>
    PgPassResult RemoveEntry(PgPassEntry key, string? filePath = null);
}
