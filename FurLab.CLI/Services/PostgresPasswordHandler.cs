namespace FurLab.CLI.Services;

/// <summary>
/// Provides secure password handling for PostgreSQL operations.
/// </summary>
public static class PostgresPasswordHandler
{
    /// <summary>
    /// Reads a password from the console interactively without displaying the characters.
    /// </summary>
    /// <returns>The password entered by the user.</returns>
    public static string ReadPasswordInteractively()
    {
        var password = string.Empty;
        var consoleKeyInfo = Console.ReadKey(true);

        while (consoleKeyInfo.Key != ConsoleKey.Enter)
        {
            if (consoleKeyInfo.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password = password.Substring(0, password.Length - 1);
                }
            }
            else if (!char.IsControl(consoleKeyInfo.KeyChar))
            {
                password += consoleKeyInfo.KeyChar;
            }

            consoleKeyInfo = Console.ReadKey(true);
        }

        return password;
    }

    /// <summary>
    /// Creates a SecureString from console input.
    /// </summary>
    /// <returns>A SecureString containing the password.</returns>
    public static System.Security.SecureString ReadSecurePasswordInteractively()
    {
        Console.Write("Password: ");
        var pwd = new System.Security.SecureString();

        while (true)
        {
            var keyInfo = Console.ReadKey(true);
            if (keyInfo.Key == ConsoleKey.Enter)
            {
                break;
            }

            if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (pwd.Length > 0)
                {
                    pwd.RemoveAt(pwd.Length - 1);
                    Console.Write("\b \b");
                }
            }
            else
            {
                pwd.AppendChar(keyInfo.KeyChar);
                Console.Write("*");
            }
        }

        return pwd;
    }
}
