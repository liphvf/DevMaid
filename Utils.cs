using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace DevMaid
{
    public static class Utils
    {
        public static string GetConnectionString(string host, string db, string user, string password)
        {
            if (string.IsNullOrEmpty(db))
            {
                throw new ArgumentException("Miss database name.");
            }
            else if (string.IsNullOrEmpty(user))
            {
                throw new ArgumentException("Miss user name.");
            }
            else if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Miss password.");
            }
            else if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException("Miss host.");
            }
            return $"Host={host};Username={user};Password={password};Database={db}";
        }

        public static Encoding GetCurrentFileEncoding(string filePath)
        {
            using (StreamReader sr = new StreamReader(filePath, true))
            {
                while (sr.Peek() >= 0)
                {
                    sr.Read();
                }
                //Test for the encoding after reading, or at least after the first read.
                return sr.CurrentEncoding;
            }
        }

        public static String SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
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
                else if (i.Key == ConsoleKey.Backspace)
                {
                    pwd.RemoveAt(pwd.Length - 1);
                    Console.Write("\b \b");
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