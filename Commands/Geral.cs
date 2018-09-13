using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace DevMaid.Commands
{
    public static class Geral
    {
        public static void CsvToClass(string inputFile = @"./arquivo.csv")
        {
            if (!File.Exists(inputFile))
            {
                throw new System.ArgumentException("Can`t find the input file");
            }

            string[] lines = File.ReadAllLines(inputFile);

            /*
             * select column_name, data_type, is_nullable from information_schema.columns where table_name = '';
             */
            foreach (string line in lines)
            {
                // Console.WriteLine(line);
                var quebrando = line.Split(",");
                if (quebrando.Length <= 0 || string.IsNullOrEmpty(line) || quebrando[0].Contains("column_name"))
                {
                    continue;
                }

                var tipos = new Dictionary<string, string>
                {
                    { "bigint" , "long"  },
                    { "binary" , "byte[]"  },
                    { "bit" , "bool"  },
                    { "char" , "string"  },
                    { "date" , "DateTime"  },
                    { "datetime" , "DateTime"  },
                    { "datetime2" , "DateTime"  },
                    { "datetimeoffset" , "DateTimeOffset"  },
                    { "decimal" , "decimal"  },
                    { "float" , "float"  },
                    { "image" , "byte[]"  },
                    { "int" , "int"  },
                    { "money" , "decimal"  },
                    { "nchar" , "char"  },
                    { "ntext" , "string"  },
                    { "numeric" , "decimal"  },
                    { "nvarchar" , "string"  },
                    { "real" , "double"  },
                    { "smalldatetime" , "DateTime"  },
                    { "smallint" , "short"  },
                    { "smallmoney" , "decimal"  },
                    { "text" , "string"  },
                    { "time" , "TimeSpan"  },
                    { "timestamp" , "DateTime"  },
                    { "tinyint" , "byte"  },
                    { "uniqueidentifier" , "Guid"  },
                    { "\"character varying\"", "string" },
                    { "character", "string" }
                };

                var nulo = quebrando[2] == "YES" ? "?" : "";
                var tabelainfo = new { coluna = quebrando[0], tipo = tipos.GetValueOrDefault(quebrando[1].Trim()), Nulo = nulo };
                // quebrando[2] = quebrando[2].Replace("'","''");

                var strbuild = new StringBuilder();

                strbuild.Append($"[Column(\"{tabelainfo.coluna}\")]");
                strbuild.Append("\n");
                strbuild.Append($"public {tabelainfo.tipo}");
                if (tabelainfo.tipo != "string")
                {
                    strbuild.Append($"{tabelainfo.Nulo}");
                }
                strbuild.Append($" {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tabelainfo.coluna)} " + "{ get; set; }");
                strbuild.Append("\n");


                using (System.IO.StreamWriter file = new StreamWriter(@"./saida.class", true))
                {
                    // Console.WriteLine(template);
                    file.WriteLine(strbuild.ToString());
                    //file.WriteLine("\n");
                }
            }
        }

        public static async Task TableToClass(string connectionString, string tableName)
        {
            var tableColumns = await GetColumnsInfo(connectionString, tableName);
            if (tableColumns.Count <= 0)
            {
                throw new System.ArgumentException("Erro ao obter informações da tabela.");
            }

            foreach (var tableColumn in tableColumns)
            {
                var tiposDoBanco = new Dictionary<string, string>
                {
                    { "bigint" , "long"  },
                    { "binary" , "byte[]"  },
                    { "bit" , "bool"  },
                    { "char" , "string"  },
                    { "date" , "DateTime"  },
                    { "datetime" , "DateTime"  },
                    { "datetime2" , "DateTime"  },
                    { "datetimeoffset" , "DateTimeOffset"  },
                    { "decimal" , "decimal"  },
                    { "float" , "float"  },
                    { "image" , "byte[]"  },
                    { "int" , "int"  },
                    { "money" , "decimal"  },
                    { "nchar" , "char"  },
                    { "ntext" , "string"  },
                    { "numeric" , "decimal"  },
                    { "nvarchar" , "string"  },
                    { "real" , "double"  },
                    { "smalldatetime" , "DateTime"  },
                    { "smallint" , "short"  },
                    { "smallmoney" , "decimal"  },
                    { "text" , "string"  },
                    { "time" , "TimeSpan"  },
                    { "timestamp" , "DateTime"  },
                    { "tinyint" , "byte"  },
                    { "uniqueidentifier" , "Guid"  },
                    { "\"character varying\"", "string" },
                    { "character", "string" },
                    {"integer", "int"}
                };

                var strbuild = new StringBuilder();

                strbuild.Append($"[Column(\"{tableColumn.column_name}\")]");
                strbuild.Append("\n");
                var tipo = tiposDoBanco.GetValueOrDefault(tableColumn.data_type as string);
                strbuild.Append($"public {tipo}");
                if (tipo != "string")
                {
                    var nulo = tableColumn.is_nullable == "YES" ? "?" : "";
                    strbuild.Append($"{ nulo }");
                }
                strbuild.Append($" {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tableColumn.column_name)} " + "{ get; set; }");
                strbuild.Append("\n");


                using (System.IO.StreamWriter file = new StreamWriter(@"./tabela.class", true))
                {
                    file.WriteLine(strbuild.ToString());
                }
            }
        }

        public static string GetConnectionString(string host, string db, string user, SecureString password)
        {
            var strPassword = SecureStringToString(password);
            if (string.IsNullOrEmpty(db))
            {
                throw new ArgumentException("Miss database name.");
            }
            else if (string.IsNullOrEmpty(user))
            {
                throw new ArgumentException("Miss user name.");
            }
            else if (string.IsNullOrEmpty(strPassword))
            {
                throw new ArgumentException("Miss password.");
            }
            if (string.IsNullOrEmpty(host))
            {
                host = "localhost";
            }
            return $"Host={host};Username={user};Password={strPassword};Database={db}";
        }

        public static async Task<List<dynamic>> GetColumnsInfo(string connectionString, string tableName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Miss Connections String.");
            }
            var sqlQuery = $@"SELECT column_name, data_type, is_nullable FROM information_schema.columns where table_name = '{tableName}';";
            // var connectionString = "Host=baasu.db.elephantsql.com;Username=wzemlogc;Password=Izzk4VtPDnkz0y5gdgWzH6WL6Vf6vyXc;Database=wzemlogc";

            var parametros = new List<NpgsqlParameter>();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                using (var command = new NpgsqlCommand())
                {
                    command.CommandText = sqlQuery;
                    command.Connection = conn;
                    command.Parameters.AddRange(parametros.ToArray());
                    var lista = new List<dynamic>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                        foreach (IDataRecord record in reader as IEnumerable)
                        {
                            var expando = new ExpandoObject() as IDictionary<string, object>;
                            int i = 0;
                            foreach (var name in names)
                            {
                                expando.Add(name, record.IsDBNull(i) ? null : record[name]);
                                i++;
                            }
                            lista.Add(expando);
                        }
                    }
                    return lista;
                }
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

        public static void CombineMultipleFilesIntoSingleFile(string inputDirectoryPathWithPattern, string outputFilePath = null)
        {
            if (outputFilePath == null)
            {
                outputFilePath = Path.Join(Directory.GetCurrentDirectory(), "outputfile.txt");
            }

            var pattern = Path.GetFileName(inputDirectoryPathWithPattern);
            var directory = Path.GetDirectoryName(inputDirectoryPathWithPattern);
            var GetAllFileText = string.Empty;
            var currentEncoding = Encoding.UTF8;

            var inputFilePaths = Directory.GetFiles(directory, pattern);
            Console.WriteLine("Number of files: {0}.", inputFilePaths.Length);

            if (!inputFilePaths.Any())
            {
                throw new Exception("Files not Found");
            }

            foreach (var inputFilePath in inputFilePaths)
            {
                currentEncoding = GetCurrentFileEncoding(inputFilePath);
                GetAllFileText = File.ReadAllText(inputFilePath, currentEncoding);

                Console.WriteLine("The file {0} has been processed.", inputFilePath);
            }

            File.WriteAllText(outputFilePath, $"{GetAllFileText}{Environment.NewLine}", currentEncoding);
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
    }
}