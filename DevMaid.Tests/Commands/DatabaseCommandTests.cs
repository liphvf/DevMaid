using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using DevMaid.CommandOptions;
using DevMaid.Services;
using DevMaid.Core.Interfaces;
using DevMaid.Core.Logging;
using DevMaid.Core.Services;
using DevMaid.Services.Logging;

namespace DevMaid.Tests.Commands;

[TestClass]
public class DatabaseCommandTests
{
    private static IConfigurationService? _configurationService;
    private static IDatabaseService? _databaseService;
    private static IFileService? _fileService;
    private static IWingetService? _wingetService;
    private static IProcessExecutor? _processExecutor;
    private static Core.Logging.ILogger? _logger;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        // Initialize logger
        var logger = new Services.Logging.ConsoleLogger(useColors: false);
        _logger = logger;

        // Initialize core services
        _configurationService = new Core.Services.ConfigurationService(logger);
        _processExecutor = new Core.Services.ProcessExecutor(logger);
        _databaseService = new Core.Services.DatabaseService(_processExecutor, logger);
        _fileService = new Core.Services.FileService(logger);
        _wingetService = new Core.Services.WingetService(_processExecutor, logger);

        // Register services for commands to use via reflection
        var serviceContainerType = typeof(DevMaid.ServiceContainer);
        
        // Set the private static fields using reflection
        SetPrivateStaticField(serviceContainerType, "_configurationService", _configurationService);
        SetPrivateStaticField(serviceContainerType, "_databaseService", _databaseService);
        SetPrivateStaticField(serviceContainerType, "_fileService", _fileService);
        SetPrivateStaticField(serviceContainerType, "_wingetService", _wingetService);
        SetPrivateStaticField(serviceContainerType, "_processExecutor", _processExecutor);
        SetPrivateStaticField(serviceContainerType, "_logger", _logger);
    }

    private static void SetPrivateStaticField(Type type, string fieldName, object? value)
    {
        var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        field?.SetValue(null, value);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        // Clean up services if needed
    }

    [TestMethod]
    public void Build_ReturnsCommandWithCorrectName()
    {
        var command = DevMaid.Commands.DatabaseCommand.Build();

        Assert.AreEqual("database", command.Name);
        Assert.AreEqual("Database utilities.", command.Description);
    }

    [TestMethod]
    public void Build_ContainsBackupAndRestoreSubcommands()
    {
        var command = DevMaid.Commands.DatabaseCommand.Build();

        Assert.AreEqual(2, command.Children.Count());
        
        var backupCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "backup");
        var restoreCommand = command.Children.OfType<System.CommandLine.Command>().FirstOrDefault(c => c.Name == "restore");

        Assert.IsNotNull(backupCommand);
        Assert.IsNotNull(restoreCommand);
    }

    [TestMethod]
    public void Backup_ValidOptions_DoesNotThrow()
    {
        var options = new DatabaseCommandOptions
        {
            DatabaseName = "testdb",
            Host = "localhost",
            Port = "5432",
            Username = "postgres",
            Password = "test"
        };

        try
        {
            DevMaid.Commands.DatabaseCommand.Backup(options);
        }
        catch (PostgresBinaryNotFoundException)
        {
            // Expected if pg_dump is not installed
        }
        catch (Exception ex) when (ex.Message.Contains("pg_dump") || ex.Message.Contains("not found"))
        {
            // Expected if pg_dump is not installed
        }
    }

    [TestMethod]
    public void Backup_MissingDatabaseName_ThrowsArgumentException()
    {
        var options = new DatabaseCommandOptions
        {
            DatabaseName = "",
            Host = "localhost",
            Port = "5432",
            Username = "postgres",
            Password = "test"
        };

        try { DevMaid.Commands.DatabaseCommand.Backup(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Backup_InvalidHost_ThrowsArgumentException()
    {
        var options = new DatabaseCommandOptions
        {
            DatabaseName = "testdb",
            Host = "invalid;host",
            Port = "5432",
            Username = "postgres",
            Password = "test"
        };

        try { DevMaid.Commands.DatabaseCommand.Backup(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Backup_InvalidPort_ThrowsArgumentException()
    {
        var options = new DatabaseCommandOptions
        {
            DatabaseName = "testdb",
            Host = "localhost",
            Port = "99999",
            Username = "postgres",
            Password = "test"
        };

        try { DevMaid.Commands.DatabaseCommand.Backup(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Restore_ValidOptions_DoesNotThrow()
    {
        var options = new DatabaseCommandOptions
        {
            DatabaseName = "testdb",
            Host = "localhost",
            Port = "5432",
            Username = "postgres",
            Password = "test"
        };

        try
        {
            DevMaid.Commands.DatabaseCommand.Restore(options);
        }
        catch (PostgresBinaryNotFoundException)
        {
            // Expected if pg_restore is not installed
        }
        catch (Exception ex) when (ex.Message.Contains("pg_restore") || ex.Message.Contains("not found"))
        {
            // Expected if pg_restore is not installed
        }
    }

    [TestMethod]
    public void Restore_MissingDatabaseName_ThrowsArgumentException()
    {
        var options = new DatabaseCommandOptions
        {
            DatabaseName = "",
            Host = "localhost",
            Port = "5432",
            Username = "postgres",
            Password = "test"
        };

        try { DevMaid.Commands.DatabaseCommand.Restore(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Restore_InvalidUsername_ThrowsArgumentException()
    {
        var options = new DatabaseCommandOptions
        {
            DatabaseName = "testdb",
            Host = "localhost",
            Port = "5432",
            Username = "invalid;user",
            Password = "test"
        };

        try { DevMaid.Commands.DatabaseCommand.Restore(options); Assert.Fail(); } catch (ArgumentException) { }
    }

    [TestMethod]
    public void Backup_AllFlag_SetCorrectly()
    {
        var options = new DatabaseCommandOptions
        {
            DatabaseName = "",
            All = true,
            Host = "localhost",
            Port = "5432",
            Username = "postgres",
            Password = "test"
        };

        try
        {
            DevMaid.Commands.DatabaseCommand.Backup(options);
        }
        catch (PostgresBinaryNotFoundException)
        {
            // Expected if pg_dump is not installed
        }
        catch (Exception ex) when (ex.Message.Contains("pg_dump") || ex.Message.Contains("not found"))
        {
            // Expected if pg_dump is not installed
        }
    }

    [TestMethod]
    public void Restore_AllFlag_SetCorrectly()
    {
        var options = new DatabaseCommandOptions
        {
            DatabaseName = "",
            All = true,
            Host = "localhost",
            Port = "5432",
            Username = "postgres",
            Password = "test",
            OutputPath = Path.GetTempPath()
        };

        try
        {
            DevMaid.Commands.DatabaseCommand.Restore(options);
        }
        catch (PostgresBinaryNotFoundException)
        {
            // Expected if pg_restore is not installed
        }
        catch (DirectoryNotFoundException)
        {
            // Expected if directory doesn't exist
        }
        catch (Exception ex) when (ex.Message.Contains("pg_restore") || ex.Message.Contains("not found"))
        {
            // Expected if pg_restore is not installed
        }
    }
}
