using CommandLine;

namespace DevMaid.CommandOptions
{
    [Verb("TableParser", HelpText = "Convert a database table in C# propreties class")]
    public class TableParserOptions
    {
        [Option('u', "User", Required = false, HelpText = "Set database user.")]
        public string User { get; set; } = "postgres";

        [Option('d', "db", Required = true, HelpText = "Set source database.")]
        public string Database { get; set; }

        [Option('p', "Password", Required = false, HelpText = "Set user password.")]
        public string Password { get; set; }

        [Option('h', "host", Required = false, HelpText = "Set database host.")]
        public string Host { get; set; } = "localhost";

        [Option('o', "output", Required = false, HelpText = "Set output file")]
        public string Output { get; set; } = "./Table.class";


        [Option('t', "table", Required = false, HelpText = "Table Name")]
        public string Table { get; internal set; }

        public string ConnectionStringDatabase => Utils.GetConnectionString(Host, Database, User, Password);
    }
}