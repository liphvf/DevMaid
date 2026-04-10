namespace FurLab.Core.Models;

/// <summary>
/// Representa uma única linha no arquivo pgpass.conf.
/// </summary>
public record PgPassEntry
{
    /// <summary>Hostname ou endereço IP do servidor PostgreSQL. Padrão: "localhost". Aceita "*" como curinga.</summary>
    public string Hostname { get; init; } = "localhost";

    /// <summary>Porta TCP do servidor PostgreSQL. Padrão: "5432". Aceita "*" como curinga.</summary>
    public string Port { get; init; } = "5432";

    /// <summary>Nome do banco de dados. Padrão: "*" (curinga). Não pode ser vazio se fornecido explicitamente.</summary>
    public string Database { get; init; } = "*";

    /// <summary>Nome de usuário PostgreSQL. Padrão: "postgres". Aceita "*" como curinga.</summary>
    public string Username { get; init; } = "postgres";

    /// <summary>Senha PostgreSQL. Nunca vazio. Nunca curinga. Armazenado sem escape (escape aplicado na serialização).</summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Chave de identidade para detecção de duplicatas.
    /// Dois registros com a mesma chave são considerados duplicatas, independente da senha.
    /// </summary>
    public (string Hostname, string Port, string Database, string Username) IdentityKey
        => (Hostname, Port, Database, Username);
}
