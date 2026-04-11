using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FurLab.CLI.CommandOptions;
using FurLab.CLI.Services;
using FurLab.CLI.Services.Logging;
using FurLab.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

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

    [TestMethod(DisplayName = "Run com InputFile vazio deve lançar ArgumentException")]
    [Description("Verifica a validação do parâmetro InputFile — string vazia deve ser rejeitada.")]
    public void Run_InputFileVazio_LancaArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = "",
            OutputFile = Path.Combine(_testDirectory, "output.csv")
        };

        try { CLI.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod(DisplayName = "Run com InputFile inexistente deve lançar FileNotFoundException")]
    [Description("Verifica que a execução falha corretamente quando o arquivo SQL de entrada não existe.")]
    public void Run_InputFileInexistente_LancaFileNotFoundException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = Path.Combine(_testDirectory, "nonexistent.sql"),
            OutputFile = Path.Combine(_testDirectory, "output.csv")
        };

        try { CLI.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (FileNotFoundException) { }
    }

    [TestMethod(DisplayName = "Run com InputFile vazio (conteúdo) deve lançar ArgumentException")]
    [Description("Verifica que arquivos SQL sem conteúdo são rejeitados antes da execução.")]
    public void Run_InputFileConteudoVazio_LancaArgumentException()
    {
        var emptyFile = Path.Combine(_testDirectory, "empty.sql");
        File.WriteAllText(emptyFile, "");

        var options = new QueryCommandOptions
        {
            InputFile = emptyFile,
            OutputFile = Path.Combine(_testDirectory, "output.csv")
        };

        try { CLI.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod(DisplayName = "Run com OutputFile vazio deve lançar ArgumentException")]
    [Description("Verifica a validação do parâmetro OutputFile — string vazia deve ser rejeitada.")]
    public void Run_OutputFileVazio_LancaArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = _sqlInputFile,
            OutputFile = ""
        };

        try { CLI.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod(DisplayName = "Run com path traversal no InputFile deve lançar ArgumentException")]
    [Description("Verifica que caminhos que tentam escapar do diretório permitido são rejeitados antes da verificação de existência do arquivo.")]
    public void Run_InputFileComPathTraversal_LancaArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = Path.Combine(_testDirectory, "..", "..", "test.sql"),
            OutputFile = Path.Combine(_testDirectory, "output.csv")
        };

        try { CLI.Commands.QueryCommand.Run(options); Assert.Fail(); }
        catch (ArgumentException) { }
        catch (FileNotFoundException) { Assert.Fail("Should throw ArgumentException before checking file existence"); }
    }

    [TestMethod(DisplayName = "Run com path traversal no OutputFile deve lançar ArgumentException")]
    [Description("Verifica que caminhos de saída que tentam escapar do diretório permitido são rejeitados.")]
    public void Run_OutputFileComPathTraversal_LancaArgumentException()
    {
        var options = new QueryCommandOptions
        {
            InputFile = _sqlInputFile,
            OutputFile = Path.Combine(_testDirectory, "..", "..", "output.csv")
        };

        try { CLI.Commands.QueryCommand.Run(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod(DisplayName = "BuildConnectionString com parâmetros explícitos não depende de configuração de servidores")]
    [Description("Verifica que quando todos os parâmetros de conexão são fornecidos explicitamente, o serviço de configuração não é consultado para dados do banco.")]
    public void BuildConnectionString_ParametrosExplicitos_NaoConsultaConfigurationService()
    {
        var mockConfigurationService = new Mock<IConfigurationService>();
        mockConfigurationService
            .SetupGet(x => x.Configuration)
            .Returns(new ConfigurationBuilder().AddInMemoryCollection().Build());
        mockConfigurationService
            .Setup(x => x.GetDatabaseConfig())
            .Returns(new Core.Models.DatabaseConnectionConfig());

        var services = new ServiceCollection();
        services.AddSingleton(mockConfigurationService.Object);
        ConfigurationService.SetServiceProvider(services.BuildServiceProvider());

        var options = new QueryCommandOptions
        {
            Host = "db.internal",
            Port = "5433",
            Database = "reporting",
            Username = "readonly_user",
            Password = "secret123"
        };

        var method = typeof(CLI.Commands.QueryCommand).GetMethod("BuildConnectionString", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);

        var connectionString = method.Invoke(null, [options]) as string;

        Assert.IsNotNull(connectionString);
        StringAssert.Contains(connectionString, "Host=db.internal");
        StringAssert.Contains(connectionString, "Port=5433");
        StringAssert.Contains(connectionString, "Database=reporting");
        StringAssert.Contains(connectionString, "Username=readonly_user");
        StringAssert.Contains(connectionString, "Password=secret123");

        mockConfigurationService.Verify(x => x.GetDatabaseConfig(), Times.Once);
        mockConfigurationService.VerifyGet(x => x.Configuration, Times.Never);
        mockConfigurationService.VerifyNoOtherCalls();
    }
}
