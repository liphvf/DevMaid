using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace DevMaid
{
    public static class Utils
    {
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

        public static Encoding GetCurrentFileEncoding(string filePath)
        {
            using var sr = new StreamReader(filePath, true);
            while (sr.Peek() >= 0)
            {
                sr.Read();
            }

            return sr.CurrentEncoding;
        }

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

        public static SecureString GetConsoleSecurePassword()
        {
            Console.Write("Password: ");
            SecureString pwd = new SecureString();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
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
}
