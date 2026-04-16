namespace FurLab.CLI.CommandOptions;

/// <summary>
/// Opções para o subcomando <c>FurLab database pgpass add</c>.
/// </summary>
public class PgPassAddOptions
{
    /// <summary>Nome do banco de dados (obrigatório). Aceita "*" para curinga.</summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>Senha PostgreSQL. Opcional; prompt interativo se ausente.</summary>
    public string? Password { get; set; }

    /// <summary>Hostname ou endereço IP. Padrão: "localhost".</summary>
    public string? Hostname { get; set; }

    /// <summary>Porta TCP. Padrão: "5432".</summary>
    public string? Port { get; set; }

    /// <summary>Nome de usuário PostgreSQL. Padrão: "postgres".</summary>
    public string? Username { get; set; }
}

/// <summary>
/// Opções para o subcomando <c>FurLab database pgpass remove</c>.
/// </summary>
public class PgPassRemoveOptions
{
    /// <summary>Nome do banco de dados da entrada a remover (obrigatório). Aceita "*" para curinga.</summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>Hostname da entrada a remover. Padrão: "localhost".</summary>
    public string? Hostname { get; set; }

    /// <summary>Porta da entrada a remover. Padrão: "5432".</summary>
    public string? Port { get; set; }

    /// <summary>Usuário da entrada a remover. Padrão: "postgres".</summary>
    public string? Username { get; set; }
}
