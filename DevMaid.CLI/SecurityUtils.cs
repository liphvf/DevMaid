using System.Text.RegularExpressions;

namespace DevMaid.CLI;

/// <summary>
/// Security utilities for input validation and sanitization.
/// </summary>
public static partial class SecurityUtils
{
    // Path traversal patterns to detect
    private static readonly string[] PathTraversalPatterns =
    [
        "..",
        "../",
        "..\\",
        "%2e%2e",
        "%2e%2e%2f",
        "%2e%2e%5c",
        "..%2f",
        "..%5c",
        "%252e%252e",
        "....//",
        "..\\..\\"
    ];

    /// <summary>
    /// Validates if a string is a valid PostgreSQL identifier.
    /// PostgreSQL identifiers: letters, digits, underscores, cannot start with digit unless quoted.
    /// </summary>
    /// <param name="identifier">The identifier to validate.</param>
    /// <returns>True if the identifier is valid, false otherwise.</returns>
    public static bool IsValidPostgreSQLIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return false;
        }

        // Check maximum length (PostgreSQL limit is 63 characters)
        if (identifier.Length > 63)
        {
            return false;
        }

        // Allow only alphanumeric characters and underscores
        // Cannot start with a digit (unless quoted, but we're not using quotes here)
        return Regex.IsMatch(identifier, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
    }

    /// <summary>
    /// Escapes a string for safe use in SQL queries.
    /// </summary>
    /// <param name="input">The input string to escape.</param>
    /// <returns>The escaped string.</returns>
    public static string EscapeSqlString(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Escape single quotes by doubling them
        return input.Replace("'", "''");
    }

    /// <summary>
    /// Validates if a path is safe and doesn't contain path traversal attacks.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="basePath">Optional base path to validate against.</param>
    /// <returns>True if the path is valid, false otherwise.</returns>
    public static bool IsValidPath(string path, string? basePath = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            // Check for path traversal patterns
            var lowerPath = path.ToLowerInvariant();
            if (PathTraversalPatterns.Any(pattern => lowerPath.Contains(pattern)))
            {
                return false;
            }

            // Check for null bytes (can bypass security checks)
            if (path.Contains('\0'))
            {
                return false;
            }

            // Check for invalid characters
            var invalidChars = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0)
            {
                return false;
            }

            // Get full path
            var fullPath = Path.GetFullPath(path);

            // If base path is provided, ensure the path is within the base path
            if (!string.IsNullOrWhiteSpace(basePath))
            {
                var baseFullPath = Path.GetFullPath(basePath);
                var normalizedFull = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var normalizedBase = baseFullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                // Check if the path starts with the base path
                if (!normalizedFull.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Additional check: ensure the path doesn't resolve to sensitive system locations
            var sensitivePaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "/etc",
                "/usr/bin",
                "/usr/sbin",
                "/bin",
                "/sbin"
            };

            foreach (var sensitivePath in sensitivePaths)
            {
                if (!string.IsNullOrEmpty(sensitivePath) &&
                    fullPath.StartsWith(sensitivePath, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates if a port number is within valid range.
    /// </summary>
    /// <param name="port">The port string to validate.</param>
    /// <returns>True if the port is valid, false otherwise.</returns>
    public static bool IsValidPort(string port)
    {
        if (string.IsNullOrWhiteSpace(port))
        {
            return false;
        }

        // Curinga pgpass: aceito como porta válida (compatível com pgpass.conf nativo)
        if (port == "*")
        {
            return true;
        }

        return int.TryParse(port, out var portNum) && portNum > 0 && portNum <= 65535;
    }

    /// <summary>
    /// Validates if a host is a valid hostname or IP address.
    /// </summary>
    /// <param name="host">The host to validate.</param>
    /// <returns>True if the host is valid, false otherwise.</returns>
    public static bool IsValidHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        // Curinga pgpass: aceito como hostname válido (compatível com pgpass.conf nativo)
        if (host == "*")
        {
            return true;
        }

        // Check for null bytes
        if (host.Contains('\0', StringComparison.Ordinal))
        {
            return false;
        }

        // Check maximum length
        if (host.Length > 253)
        {
            return false;
        }

        // Basic validation for hostname or IP
        // Allow: localhost, domain names, IPv4, IPv6
        var pattern = @"^([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])(\.([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]{0,61}[a-zA-Z0-9]))*$|^localhost$|^(\d{1,3}\.){3}\d{1,3}$|^(\[?[0-9a-fA-F:]+\]?)$";
        return Regex.IsMatch(host, pattern);
    }

    /// <summary>
    /// Validates if a username contains only safe characters.
    /// </summary>
    /// <param name="username">The username to validate.</param>
    /// <returns>True if the username is valid, false otherwise.</returns>
    public static bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        // Curinga pgpass: aceito como usuário válido (compatível com pgpass.conf nativo)
        if (username == "*")
        {
            return true;
        }

        // Check maximum length
        if (username.Length > 63)
        {
            return false;
        }

        // Allow alphanumeric, underscore, hyphen, and dot
        return UsernameRegexGenerated().IsMatch(username);
    }

    /// <summary>
    /// Sanitizes a file path argument for command line usage.
    /// </summary>
    /// <param name="argument">The argument to sanitize.</param>
    /// <returns>The sanitized argument.</returns>
    public static string SanitizeCommandLineArgument(string argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            return argument;
        }

        // Escape special characters that could be interpreted by shell
        // This is a basic implementation; for production, use proper argument escaping
        return argument.Replace("\"", "\\\"").Replace("$", "\\$").Replace("`", "\\`").Replace("\\", "\\\\");
    }

    /// <summary>
    /// Validates and resolves a safe output path within a base directory.
    /// </summary>
    /// <param name="outputPath">The output path to resolve.</param>
    /// <param name="baseDirectory">The base directory.</param>
    /// <returns>The validated and resolved output path.</returns>
    /// <exception cref="ArgumentException">Thrown when the output path is invalid.</exception>
    public static string GetSafeOutputPath(string outputPath, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return Path.Combine(baseDirectory, "output");
        }

        try
        {
            // If outputPath is a relative path, combine with baseDirectory
            if (!Path.IsPathRooted(outputPath))
            {
                outputPath = Path.Combine(baseDirectory, outputPath);
            }

            // Get full path and validate
            var fullPath = Path.GetFullPath(outputPath);

            // Ensure it's within base directory
            if (!IsValidPath(fullPath, baseDirectory))
            {
                throw new ArgumentException("Output path is outside the allowed directory.");
            }

            return fullPath;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid output path: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Validates if a database name is safe to use.
    /// </summary>
    /// <param name="databaseName">The database name to validate.</param>
    /// <returns>True if the database name is valid, false otherwise.</returns>
    public static bool IsValidDatabaseName(string databaseName)
    {
        return IsValidPostgreSQLIdentifier(databaseName);
    }

    /// <summary>
    /// Validates if a table name is safe to use.
    /// </summary>
    /// <param name="tableName">The table name to validate.</param>
    /// <returns>True if the table name is valid, false otherwise.</returns>
    public static bool IsValidTableName(string tableName)
    {
        return IsValidPostgreSQLIdentifier(tableName);
    }

    [GeneratedRegex(@"^[a-zA-Z0-9_.\-]+$")]
    private static partial Regex UsernameRegexGenerated();

}
