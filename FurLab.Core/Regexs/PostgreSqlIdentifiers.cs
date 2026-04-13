using System.Text.RegularExpressions;

namespace FurLab.Core.Regexs;

/// <summary>
/// Pre-compiled regular expressions for PostgreSQL identifier validation.
/// </summary>
public static partial class PostgreSqlIdentifiers
{
    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$")]
    public static partial Regex ValidIdentifier();
}
