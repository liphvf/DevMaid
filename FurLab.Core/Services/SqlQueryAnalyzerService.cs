using System.Text.RegularExpressions;
using FurLab.Core.Models;

namespace FurLab.Core.Services;

/// <summary>
/// Analyzes SQL queries to detect potentially destructive operations.
/// Uses simple regex-based parsing to identify the first meaningful SQL keyword.
/// </summary>
public static partial class SqlQueryAnalyzerService
{
    private static readonly HashSet<string> DestructiveKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "INSERT", "UPDATE", "DELETE", "ALTER", "DROP", "CREATE",
        "TRUNCATE", "MERGE", "GRANT", "REVOKE"
    };

    [GeneratedRegex(@"--.*?$|/\*[\s\S]*?\*/", RegexOptions.Multiline)]
    private static partial Regex SqlCommentRegex();

    [GeneratedRegex(@"^\s*(\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex FirstKeywordRegex();

    [GeneratedRegex(@"^\s*SET\s+ROLE", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SetRoleRegex();

    /// <summary>
    /// Analyzes a SQL query and returns its type (Safe or Destructive).
    /// </summary>
    public static QueryType AnalyzeQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return QueryType.Safe;

        var cleanedQuery = StripComments(query);
        var firstKeyword = GetFirstKeyword(cleanedQuery);

        if (string.IsNullOrWhiteSpace(firstKeyword))
            return QueryType.Safe;

        if (DestructiveKeywords.Contains(firstKeyword))
            return QueryType.Destructive;

        if (firstKeyword.Equals("SET", StringComparison.OrdinalIgnoreCase))
        {
            if (SetRoleRegex().IsMatch(cleanedQuery))
                return QueryType.Destructive;
        }

        return QueryType.Safe;
    }

    /// <summary>
    /// Gets a human-readable description of the query type.
    /// </summary>
    public static string GetQueryTypeDescription(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "UNKNOWN";

        var cleanedQuery = StripComments(query);
        var keyword = GetFirstKeyword(cleanedQuery);
        return keyword?.ToUpperInvariant() ?? "UNKNOWN";
    }

    private static string StripComments(string query)
    {
        return SqlCommentRegex().Replace(query, string.Empty);
    }

    private static string? GetFirstKeyword(string query)
    {
        var match = FirstKeywordRegex().Match(query.Trim());
        if (!match.Success)
            return null;

        var keyword = match.Groups[1].Value;

        if (keyword.Equals("WITH", StringComparison.OrdinalIgnoreCase))
            return ExtractKeywordFromCte(query);

        return keyword;
    }

    private static string ExtractKeywordFromCte(string query)
    {
        var asIndex = query.IndexOf("AS", StringComparison.OrdinalIgnoreCase);
        if (asIndex < 0)
            return "WITH";

        var parenOpen = query.IndexOf('(', asIndex);
        if (parenOpen < 0)
            return "WITH";

        var remaining = query.Substring(parenOpen + 1);
        var innerMatch = FirstKeywordRegex().Match(remaining.Trim());

        if (innerMatch.Success)
        {
            var innerKeyword = innerMatch.Groups[1].Value;

            if (innerKeyword.Equals("SELECT", StringComparison.OrdinalIgnoreCase))
                return "SELECT";

            if (DestructiveKeywords.Contains(innerKeyword))
                return innerKeyword;
        }

        return "WITH";
    }
}
