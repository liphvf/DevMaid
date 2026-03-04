using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DevMaid.Services;

/// <summary>
/// Provides methods to list PostgreSQL databases.
/// </summary>
public static class PostgresDatabaseLister
{
    /// <summary>
    /// Lists all non-template databases on the PostgreSQL server.
    /// </summary>
    /// <param name="host">The database host address.</param>
    /// <param name="port">The database port.</param>
    /// <param name="username">The database username.</param>
    /// <param name="password">The database password.</param>
    /// <returns>A list of database names.</returns>
    /// <exception cref="Exception">Thrown when psql cannot be found or execution fails.</exception>
    public static List<string> ListAllDatabases(string host, string port, string username, string password)
    {
        var psqlPath = PostgresBinaryLocator.FindPsql();
        if (psqlPath == null)
        {
            throw new Exception("psql not found. Please ensure PostgreSQL is installed and psql is in your PATH.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = psqlPath,
            Arguments = $"-h \"{host}\" -p {port} -U \"{username}\" -d postgres -c \"SELECT datname FROM pg_database WHERE datistemplate = false ORDER BY datname;\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.Environment["PGPASSWORD"] = password;

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new Exception("Failed to start psql process.");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"psql failed with exit code {process.ExitCode}. Error: {error}");
            }

            // Parse output to get database names
            var databases = new List<string>();
            var lines = output.Split('\n');

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                // Skip header lines and empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine) ||
                    trimmedLine.StartsWith("datname") ||
                    trimmedLine.StartsWith("---") ||
                    trimmedLine.StartsWith("("))
                {
                    continue;
                }

                // Remove trailing | and whitespace
                if (trimmedLine.EndsWith("|"))
                {
                    trimmedLine = trimmedLine.Substring(0, trimmedLine.Length - 1).Trim();
                }

                if (!string.IsNullOrWhiteSpace(trimmedLine))
                {
                    databases.Add(trimmedLine);
                }
            }

            return databases;
        }
        finally
        {
            // Clear password from environment
            startInfo.Environment["PGPASSWORD"] = string.Empty;
        }
    }
}
