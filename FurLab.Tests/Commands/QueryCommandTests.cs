using System;
using System.IO;
using System.Linq;
using FurLab.CLI.Services;
using FurLab.CLI.Services.Logging;
using FurLab.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FurLab.Tests.Commands;

[TestClass]
public class QueryCommandTests
{
    private static IConfigurationService? _configurationService;
    private static IDatabaseService? _databaseService;
    private static IFileService? _fileService;
    private static IWingetService? _wingetService;
    private static IProcessExecutor? _processExecutor;
    private static Core.Logging.ILogger? _logger;

    private string _testDirectory = null!;
    private string _sqlInputFile = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        var logger = new ConsoleLogger(useColors: false);
        _logger = logger;

        _configurationService = new Core.Services.ConfigurationService(logger);
        _processExecutor = new Core.Services.ProcessExecutor(logger);
        _databaseService = new Core.Services.DatabaseService(_processExecutor, logger);
        _fileService = new Core.Services.FileService(logger);
        _wingetService = new Core.Services.WingetService(_processExecutor, logger);

        var services = new ServiceCollection();
        services.AddSingleton(_configurationService);
        services.AddSingleton(_databaseService);
        services.AddSingleton(_fileService);
        services.AddSingleton(_wingetService);
        services.AddSingleton(_processExecutor);
        services.AddSingleton(_logger);

        var serviceProvider = services.BuildServiceProvider();

        Logger.SetServiceProvider(serviceProvider);
        ConfigurationService.SetServiceProvider(serviceProvider);
        PostgresDatabaseLister.SetServiceProvider(serviceProvider);
    }

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"QueryCommandTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);

        _sqlInputFile = Path.Combine(_testDirectory, "test.sql");
        File.WriteAllText(_sqlInputFile, "SELECT * FROM users;");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [TestMethod(DisplayName = "Build deve retornar comando com nome 'query'")]
    [Description("Verifica que o comando principal é construído com o nome correto.")]
    public void Build_ComandoPrincipal_RetornaNomeQuery()
    {
        var command = CLI.Commands.QueryCommand.Build();

        Assert.AreEqual("query", command.Name);
    }

    [TestMethod(DisplayName = "Build deve conter exatamente um subcomando 'run'")]
    [Description("Verifica que o comando query possui apenas o subcomando run registrado.")]
    public void Build_ComandoPrincipal_ContemUnicoSubcomandoRun()
    {
        var command = CLI.Commands.QueryCommand.Build();

        Assert.AreEqual(1, command.Children.Count());

        var runCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "run");
        Assert.IsNotNull(runCommand);
    }

    // --- UnescapeInlineQuery edge cases ---

    [TestMethod(DisplayName = "UnescapeInlineQuery strips outer double quotes")]
    public void UnescapeInlineQuery_OuterDoubleQuotes_Stripped()
    {
        var result = CLI.Commands.QueryCommand.UnescapeInlineQuery("\"SELECT 1\"");
        Assert.AreEqual("SELECT 1", result);
    }

    [TestMethod(DisplayName = "UnescapeInlineQuery strips outer single quotes")]
    public void UnescapeInlineQuery_OuterSingleQuotes_Stripped()
    {
        var result = CLI.Commands.QueryCommand.UnescapeInlineQuery("'SELECT 1'");
        Assert.AreEqual("SELECT 1", result);
    }

    [TestMethod(DisplayName = "UnescapeInlineQuery with no outer quotes returns unchanged")]
    public void UnescapeInlineQuery_NoOuterQuotes_ReturnsUnchanged()
    {
        var result = CLI.Commands.QueryCommand.UnescapeInlineQuery("SELECT 1");
        Assert.AreEqual("SELECT 1", result);
    }

    [TestMethod(DisplayName = "UnescapeInlineQuery unescapes backslash-escaped double quotes")]
    public void UnescapeInlineQuery_BackslashEscapedDoubleQuotes_Unescaped()
    {
        var result = CLI.Commands.QueryCommand.UnescapeInlineQuery(@"SELECT \""name\"" FROM t");
        Assert.AreEqual("SELECT \"name\" FROM t", result);
    }

    [TestMethod(DisplayName = "UnescapeInlineQuery unescapes backslash-escaped single quotes")]
    public void UnescapeInlineQuery_BackslashEscapedSingleQuotes_Unescaped()
    {
        var result = CLI.Commands.QueryCommand.UnescapeInlineQuery(@"SELECT \''val\'' FROM t");
        Assert.AreEqual("SELECT ''val'' FROM t", result);
    }

    [TestMethod(DisplayName = "UnescapeInlineQuery with mismatched quotes returns unchanged")]
    public void UnescapeInlineQuery_MismatchedQuotes_ReturnsUnchanged()
    {
        // starts with " ends with ' — should not strip anything
        var result = CLI.Commands.QueryCommand.UnescapeInlineQuery("\"SELECT 1'");
        Assert.AreEqual("\"SELECT 1'", result);
    }

    [TestMethod(DisplayName = "UnescapeInlineQuery on empty string returns empty")]
    public void UnescapeInlineQuery_EmptyString_ReturnsEmpty()
    {
        var result = CLI.Commands.QueryCommand.UnescapeInlineQuery(string.Empty);
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod(DisplayName = "UnescapeInlineQuery on single character in double quotes returns empty")]
    public void UnescapeInlineQuery_SingleCharInDoubleQuotes_ReturnsChar()
    {
        // "x" → x  (strips outer quotes, length 1 remains)
        var result = CLI.Commands.QueryCommand.UnescapeInlineQuery("\"x\"");
        Assert.AreEqual("x", result);
    }

    [TestMethod(DisplayName = "UnescapeInlineQuery on two double-quote chars returns empty")]
    public void UnescapeInlineQuery_TwoDoubleQuoteChars_ReturnsEmpty()
    {
        // "" → (empty after stripping outer pair)
        var result = CLI.Commands.QueryCommand.UnescapeInlineQuery("\"\"");
        Assert.AreEqual(string.Empty, result);
    }

}
