namespace FurLab.Core.Models;

/// <summary>
/// Resultado de uma operação do PgPassService.
/// </summary>
public record PgPassResult
{
    /// <summary>Indica se a operação foi concluída sem erro (inclusive duplicata — que não é erro).</summary>
    public bool Success { get; init; }

    /// <summary>Mensagem descritiva do resultado para exibição ao usuário.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Indica que a entrada já existia no arquivo (estado desejado já presente).
    /// Success = true quando IsDuplicate = true — duplicata não é erro; código de saída permanece 0.
    /// </summary>
    public bool IsDuplicate { get; init; }

    /// <summary>Cria um resultado de sucesso.</summary>
    public static PgPassResult Ok(string message)
        => new() { Success = true, Message = message };

    /// <summary>Cria um resultado informativo de duplicata (sucesso, sem modificação do arquivo).</summary>
    public static PgPassResult Duplicate(string message)
        => new() { Success = true, IsDuplicate = true, Message = message };

    /// <summary>Cria um resultado de falha com mensagem acionável.</summary>
    public static PgPassResult Fail(string message)
        => new() { Success = false, Message = message };
}
