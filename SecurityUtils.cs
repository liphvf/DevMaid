using System;
using System.IO;
using System.Text.RegularExpressions;

namespace DevMaid;

/// <summary>
/// Security utilities for input validation and sanitization.
/// </summary>
public static class SecurityUtils
{
    /// <summary>
    /// Validates if a string is a valid PostgreSQL identifier.
    /// PostgreSQL identifiers: letters, digits, underscores, cannot start with digit unless quoted.
    /// </summary>
    public static bool IsValidPostgreSQLIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
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
    public static bool IsValidPath(string path, string? basePath = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
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

            // Check for null bytes (can bypass security checks)
            if (path.Contains('\0'))
            {
                return false;
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
    public static bool IsValidPort(string port)
    {
        if (string.IsNullOrWhiteSpace(port))
        {
            return false;
        }

        return int.TryParse(port, out int portNum) && portNum > 0 && portNum <= 65535;
    }

    /// <summary>
    /// Validates if a host is a valid hostname or IP address.
    /// </summary>
    public static bool IsValidHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        // Check for null bytes
        if (host.Contains('\0'))
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
    public static bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        // Allow alphanumeric, underscore, hyphen, and dot
        return Regex.IsMatch(username, @"^[a-zA-Z0-9_.\-]+$");
    }

    /// <summary>
    /// Sanitizes a file path argument for command line usage.
    /// </summary>
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
}
