using CommandLine;

namespace DevMaid.CommandOptions
{
    [Verb("TableParser", HelpText = "Copy dashboards between databases.")]
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

        [Option('o', "output", Required = false, HelpText = "Dashboard source guid")]
        public string Output { get; set; } = "./Table.class";

        public string ConnectionStringDatabase => Utils.GetConnectionString(Host, Database, User, Password);
    }
}