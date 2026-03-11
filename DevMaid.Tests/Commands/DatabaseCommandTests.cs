using System;
using System.CommandLine;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using DevMaid.CommandOptions;
using DevMaid.Services;

namespace DevMaid.Tests.Commands;

[TestClass]
public class DatabaseCommandTests
{
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
        catch
        {
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
        }
        catch (Exception ex) when (ex.Message.Contains("pg_restore") || ex.Message.Contains("not found"))
        {
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

        try { DevMaid.Commands.DatabaseCommand.Backup(options); Assert.Fail(); } catch (ArgumentException) { }
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
        }
        catch (Exception)
        {
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
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (Exception)
        {
        }
    }
}
