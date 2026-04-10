using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace FurLab.CLI;

/// <summary>
/// General utility methods for the FurLab CLI.
/// </summary>
public static class Utils
{
    /// <summary>
    /// Builds a PostgreSQL connection string from individual parameters.
    /// </summary>
    /// <param name="host">The database host.</param>
    /// <param name="db">The database name.</param>
    /// <param name="user">The database username.</param>
    /// <param name="password">The database password.</param>
    /// <returns>A formatted PostgreSQL connection string.</returns>
    /// <exception cref="ArgumentException">Thrown when any required parameter is null or whitespace.</exception>
    public static string GetConnectionString(string? host, string? db, string? user, string? password)
    {
        if (string.IsNullOrWhiteSpace(db))
        {
            throw new ArgumentException("Miss database name.");
        }

        if (string.IsNullOrWhiteSpace(user))
        {
            throw new ArgumentException("Miss user name.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Miss password.");
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Miss host.");
        }

        return $"Host={host};Username={user};Password={password};Database={db}";
    }

    /// <summary>
    /// Detects and returns the encoding of a file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>The detected <see cref="Encoding"/> of the file.</returns>
    public static Encoding GetCurrentFileEncoding(string filePath)
    {
        using var sr = new StreamReader(filePath, true);
        while (sr.Peek() >= 0)
        {
            sr.Read();
        }

        return sr.CurrentEncoding;
    }

    /// <summary>
    /// Converts a <see cref="SecureString"/> to a plain <see cref="string"/>.
    /// </summary>
    /// <param name="value">The <see cref="SecureString"/> to convert.</param>
    /// <returns>The plain string representation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string SecureStringToString(SecureString value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var valuePtr = IntPtr.Zero;
        try
        {
            valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
            return Marshal.PtrToStringUni(valuePtr) ?? string.Empty;
        }
        finally
        {
            Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
        }
    }

    /// <summary>
    /// Prompts the user to enter a password securely via the console, masking input.
    /// </summary>
    /// <returns>A <see cref="SecureString"/> containing the entered password.</returns>
    public static SecureString GetConsoleSecurePassword()
    {
        Console.Write("Password: ");
        var pwd = new SecureString();
        while (true)
        {
            var i = Console.ReadKey(true);
            if (i.Key == ConsoleKey.Enter)
            {
                break;
            }

            if (i.Key == ConsoleKey.Backspace)
            {
                if (pwd.Length > 0)
                {
                    pwd.RemoveAt(pwd.Length - 1);
                    Console.Write("\b \b");
                }
            }
            else
            {
                pwd.AppendChar(i.KeyChar);
                Console.Write("*");
            }
        }

        return pwd;
    }
}
