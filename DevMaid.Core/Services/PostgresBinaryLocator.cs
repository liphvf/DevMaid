using System;
using System.IO;

namespace DevMaid.Core.Services;

/// <summary>
/// Provides methods to locate PostgreSQL binary executables.
/// </summary>
public static class PostgresBinaryLocator
{
    /// <summary>
    /// Finds the pg_dump executable in the system PATH or common installation directories.
    /// </summary>
    /// <returns>The full path to pg_dump, or null if not found.</returns>
    public static string? FindPgDump()
    {
        return FindExecutable("pg_dump");
    }

    /// <summary>
    /// Finds the pg_restore executable in the system PATH or common installation directories.
    /// </summary>
    /// <returns>The full path to pg_restore, or null if not found.</returns>
    public static string? FindPgRestore()
    {
        return FindExecutable("pg_restore");
    }

    /// <summary>
    /// Finds the psql executable in the system PATH or common installation directories.
    /// </summary>
    /// <returns>The full path to psql, or null if not found.</returns>
    public static string? FindPsql()
    {
        return FindExecutable("psql");
    }

    private static string? FindExecutable(string executableName)
    {
        // Try to find executable in PATH
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();

        foreach (var path in paths)
        {
            var exePath = Path.Combine(path, $"{executableName}.exe");
            if (File.Exists(exePath))
            {
                return exePath;
            }

            exePath = Path.Combine(path, executableName);
            if (File.Exists(exePath))
            {
                return exePath;
            }
        }

        // Try common PostgreSQL installation paths on Windows
        var commonPaths = new[]
        {
            @"C:\Program Files\PostgreSQL\*\bin",
            @"C:\PostgreSQL\*\bin"
        };

        foreach (var pattern in commonPaths)
        {
            var directory = Path.GetDirectoryName(pattern);
            if (directory != null && Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory, $"{executableName}.exe", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    return files[0];
                }
            }
        }

        // Try common paths on Unix-like systems
        var unixPaths = new[]
        {
            "/usr/bin",
            "/usr/local/bin",
            "/usr/local/pgsql/bin"
        };

        foreach (var unixPath in unixPaths)
        {
            if (Directory.Exists(unixPath))
            {
                var exePath = Path.Combine(unixPath, executableName);
                if (File.Exists(exePath))
                {
                    return exePath;
                }
            }
        }

        return null;
    }
}
