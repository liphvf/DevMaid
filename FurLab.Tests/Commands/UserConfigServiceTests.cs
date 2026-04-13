using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using FurLab.Core.Interfaces;
using FurLab.Core.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FurLab.Tests.Commands;

[TestClass]
public class UserConfigServiceTests
{
    private string _testDirectory = null!;
    private Core.Logging.ILogger _logger = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"UserConfigServiceTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
        _logger = new TestLogger();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [TestMethod(DisplayName = "LoadConfig cria arquivo default quando não existe")]
    public void LoadConfig_NoFile_CreatesDefault()
    {
        var service = new TestableUserConfigService(_logger, _testDirectory);
        var config = service.LoadConfig();

        Assert.IsNotNull(config);
        Assert.IsNotNull(config.Defaults);
        Assert.AreEqual(0, config.Servers.Count);
        Assert.IsTrue(File.Exists(service.GetConfigFilePath()));
    }

    [TestMethod(DisplayName = "SaveConfig e LoadConfig round-trip preserva dados")]
    public void SaveAndLoad_RoundTrip_PreservesData()
    {
        var service = new TestableUserConfigService(_logger, _testDirectory);
        var config = new UserConfig
        {
            Servers = [new ServerConfigEntry
            {
                Name = "test-server",
                Host = "localhost",
                Port = 5432,
                Username = "postgres",
                Password = "secret",
                Databases = ["db1", "db2"]
            }],
            Defaults = new UserDefaults
            {
                OutputDirectory = "./output",
                MaxParallelism = 8
            }
        };

        service.SaveConfig(config);
        var loaded = service.LoadConfig();

        Assert.AreEqual(1, loaded.Servers.Count);
        Assert.AreEqual("test-server", loaded.Servers[0].Name);
        Assert.AreEqual("localhost", loaded.Servers[0].Host);
        Assert.AreEqual(5432, loaded.Servers[0].Port);
        Assert.AreEqual("postgres", loaded.Servers[0].Username);
        Assert.AreEqual("secret", loaded.Servers[0].Password);
        Assert.AreEqual(2, loaded.Servers[0].Databases.Count);
        Assert.AreEqual("./output", loaded.Defaults!.OutputDirectory);
        Assert.AreEqual(8, loaded.Defaults.MaxParallelism);
    }

    [TestMethod(DisplayName = "LoadConfig com JSONC (comentários) parseia corretamente")]
    public void LoadConfig_WithJsoncComments_ParsesCorrectly()
    {
        var service = new TestableUserConfigService(_logger, _testDirectory);
        var jsonc = @"{
  // This is a comment
  ""servers"": [
    {
      ""name"": ""dev"", /* inline comment */
      ""host"": ""localhost"",
      ""port"": 5432,
      ""username"": ""postgres""
    }
  ],
  ""defaults"": {
    ""outputDirectory"": ""./results""
  }
}";
        File.WriteAllText(service.GetConfigFilePath(), jsonc);

        var config = service.LoadConfig();

        Assert.AreEqual(1, config.Servers.Count);
        Assert.AreEqual("dev", config.Servers[0].Name);
    }

    [TestMethod(DisplayName = "LoadConfig com JSON inválido lança InvalidOperationException")]
    public void LoadConfig_InvalidJson_ThrowsInvalidOperationException()
    {
        var service = new TestableUserConfigService(_logger, _testDirectory);
        File.WriteAllText(service.GetConfigFilePath(), "{ invalid json }");

        var threwExpected = false;
        try { service.LoadConfig(); }
        catch (InvalidOperationException) { threwExpected = true; }
        catch (System.Text.Json.JsonException) { threwExpected = true; }
        Assert.IsTrue(threwExpected);
    }

    [TestMethod(DisplayName = "AddOrUpdateServer adiciona novo servidor")]
    public void AddOrUpdateServer_NewServer_AddsToList()
    {
        var service = new TestableUserConfigService(_logger, _testDirectory);
        var server = new ServerConfigEntry
        {
            Name = "new-server",
            Host = "db.example.com",
            Port = 5432,
            Username = "admin"
        };

        service.AddOrUpdateServer(server);
        var servers = service.GetServers();

        Assert.AreEqual(1, servers.Count);
        Assert.AreEqual("new-server", servers[0].Name);
    }

    [TestMethod(DisplayName = "AddOrUpdateServer atualiza servidor existente")]
    public void AddOrUpdateServer_ExistingServer_UpdatesInPlace()
    {
        var service = new TestableUserConfigService(_logger, _testDirectory);
        service.AddOrUpdateServer(new ServerConfigEntry { Name = "srv", Host = "old-host", Port = 5432, Username = "user" });
        service.AddOrUpdateServer(new ServerConfigEntry { Name = "srv", Host = "new-host", Port = 5433, Username = "admin" });

        var server = service.GetServer("srv");
        Assert.IsNotNull(server);
        Assert.AreEqual("new-host", server.Host);
        Assert.AreEqual(5433, server.Port);
        Assert.AreEqual("admin", server.Username);
    }

    [TestMethod(DisplayName = "RemoveServer remove servidor existente")]
    public void RemoveServer_ExistingServer_RemovesAndReturnsTrue()
    {
        var service = new TestableUserConfigService(_logger, _testDirectory);
        service.AddOrUpdateServer(new ServerConfigEntry { Name = "srv", Host = "host", Port = 5432, Username = "user" });

        var result = service.RemoveServer("srv");
        Assert.IsTrue(result);
        Assert.AreEqual(0, service.GetServers().Count);
    }

    [TestMethod(DisplayName = "RemoveServer retorna false para servidor inexistente")]
    public void RemoveServer_NonexistentServer_ReturnsFalse()
    {
        var service = new TestableUserConfigService(_logger, _testDirectory);
        var result = service.RemoveServer("nonexistent");
        Assert.IsFalse(result);
    }

    [TestMethod(DisplayName = "GetServer retorna servidor por nome case-insensitive")]
    public void GetServer_CaseInsensitive_ReturnsServer()
    {
        var service = new TestableUserConfigService(_logger, _testDirectory);
        service.AddOrUpdateServer(new ServerConfigEntry { Name = "MyServer", Host = "host", Port = 5432, Username = "user" });

        var server = service.GetServer("myserver");
        Assert.IsNotNull(server);
        Assert.AreEqual("MyServer", server.Name);
    }

    [TestMethod(DisplayName = "ValidateAndApplyDefaults aplica defaults para campos omitidos")]
    public void LoadConfig_MissingDefaults_AppliesDefaults()
    {
        var service = new TestableUserConfigService(_logger, _testDirectory);
        var json = @"{""servers"": [{""name"": ""srv"",""host"": ""host"",""username"": ""user""}]}";
        File.WriteAllText(service.GetConfigFilePath(), json);

        var config = service.LoadConfig();

        Assert.AreEqual(5432, config.Servers[0].Port);
        Assert.AreEqual("Prefer", config.Servers[0].SslMode);
        Assert.AreEqual(30, config.Servers[0].Timeout);
        Assert.AreEqual(300, config.Servers[0].CommandTimeout);
        Assert.AreEqual(4, config.Servers[0].MaxParallelism);
    }

    [TestMethod(DisplayName = "GetDefaults retorna defaults quando configurados")]
    public void GetDefaults_WhenConfigured_ReturnsValues()
    {
        var service = new TestableUserConfigService(_logger, _testDirectory);
        var config = new UserConfig { Defaults = new UserDefaults { OutputDirectory = "/tmp/out", MaxParallelism = 8 } };
        service.SaveConfig(config);

        var defaults = service.GetDefaults();
        Assert.AreEqual("/tmp/out", defaults.OutputDirectory);
        Assert.AreEqual(8, defaults.MaxParallelism);
    }

    [TestMethod(DisplayName = "ConfigFileExists retorna false quando arquivo não existe")]
    public void ConfigFileExists_NoFile_ReturnsFalse()
    {
        var service = new TestableUserConfigService(_logger, _testDirectory);
        Assert.IsFalse(service.ConfigFileExists());
    }

    [TestMethod(DisplayName = "ConfigFileExists retorna true após LoadConfig")]
    public void ConfigFileExists_AfterLoad_ReturnsTrue()
    {
        var service = new TestableUserConfigService(_logger, _testDirectory);
        service.LoadConfig();
        Assert.IsTrue(service.ConfigFileExists());
    }

    [TestMethod(DisplayName = "TryLoadLegacyConfig retorna null quando arquivo não existe")]
    public void TryLoadLegacyConfig_NoFile_ReturnsNull()
    {
        var service = new TestableUserConfigService(_logger, _testDirectory);
        var result = service.TryLoadLegacyConfig();
        Assert.IsNull(result);
    }

    [TestMethod(DisplayName = "TryLoadLegacyConfig migra servidores do appsettings.json legado")]
    public void TryLoadLegacyConfig_ValidLegacyFile_MigratesServers()
    {
        var legacyJson = @"{
  ""Servers"": {
    ""ServersList"": [
      {
        ""Name"": ""legacy-server"",
        ""Host"": ""db.legacy.com"",
        ""Port"": 5433,
        ""Username"": ""admin"",
        ""Password"": ""secret"",
        ""Databases"": [""app"", ""analytics""],
        ""SslMode"": ""Require"",
        ""Timeout"": 60,
        ""CommandTimeout"": 600
      }
    ]
  }
}";
        var legacyPath = Path.Combine(_testDirectory, "appsettings.json");
        File.WriteAllText(legacyPath, legacyJson);

        var service = new TestableUserConfigService(_logger, _testDirectory);
        var config = service.TryLoadLegacyConfig();

        Assert.IsNotNull(config);
        Assert.AreEqual(1, config.Servers.Count);

        var server = config.Servers[0];
        Assert.AreEqual("legacy-server", server.Name);
        Assert.AreEqual("db.legacy.com", server.Host);
        Assert.AreEqual(5433, server.Port);
        Assert.AreEqual("admin", server.Username);
        Assert.AreEqual("secret", server.Password);
        Assert.AreEqual(2, server.Databases.Count);
        Assert.AreEqual("app", server.Databases[0]);
        Assert.AreEqual("analytics", server.Databases[1]);
        Assert.AreEqual("Require", server.SslMode);
        Assert.AreEqual(60, server.Timeout);
        Assert.AreEqual(600, server.CommandTimeout);
    }

    [TestMethod(DisplayName = "TryLoadLegacyConfig retorna null quando appsettings.json não tem seção Servers")]
    public void TryLoadLegacyConfig_NoServersSection_ReturnsNull()
    {
        var legacyJson = @"{ ""OtherSection"": {} }";
        var legacyPath = Path.Combine(_testDirectory, "appsettings.json");
        File.WriteAllText(legacyPath, legacyJson);

        var service = new TestableUserConfigService(_logger, _testDirectory);
        var result = service.TryLoadLegacyConfig();

        Assert.IsNull(result);
    }

    [TestMethod(DisplayName = "TryLoadLegacyConfig retorna null quando appsettings.json tem JSON inválido")]
    public void TryLoadLegacyConfig_InvalidJson_ReturnsNull()
    {
        var legacyPath = Path.Combine(_testDirectory, "appsettings.json");
        File.WriteAllText(legacyPath, "{ not valid json");

        var service = new TestableUserConfigService(_logger, _testDirectory);
        var result = service.TryLoadLegacyConfig();

        Assert.IsNull(result);
    }

    private sealed class TestLogger : Core.Logging.ILogger
    {
        public void LogInformation(string message, params object[] args) { }
        public void LogWarning(string message, params object[] args) { }
        public void LogError(string message, Exception? exception = null, params object[] args) { }
        public void LogDebug(string message, params object[] args) { }
    }

    private sealed class TestableUserConfigService(Core.Logging.ILogger logger, string configFolder) : IUserConfigService
    {
        private readonly Core.Logging.ILogger _logger = logger;
        private readonly string _configFilePath = Path.Combine(configFolder, "furlab.jsonc");
        private readonly string _legacyConfigFilePath = Path.Combine(configFolder, "appsettings.json");
        private readonly string _configFolder = configFolder;

        public UserConfig LoadConfig()
        {
            if (!ConfigFileExists())
            {
                var config = new UserConfig { Defaults = new UserDefaults() };
                SaveConfig(config);
                return config;
            }

            var json = File.ReadAllText(_configFilePath);
            var strippedJson = StripJsonComments(json);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<UserConfig>(strippedJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (deserialized == null)
            {
                var config = new UserConfig { Defaults = new UserDefaults() };
                SaveConfig(config);
                return config;
            }

            ApplyDefaults(deserialized);
            return deserialized;
        }

        public void SaveConfig(UserConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);
            Directory.CreateDirectory(_configFolder);
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            File.WriteAllText(_configFilePath, json);
        }

        public IReadOnlyList<ServerConfigEntry> GetServers() => LoadConfig().Servers.AsReadOnly();

        public ServerConfigEntry? GetServer(string name) => LoadConfig().Servers.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public void AddOrUpdateServer(ServerConfigEntry server)
        {
            var config = LoadConfig();
            var idx = config.Servers.FindIndex(s => s.Name.Equals(server.Name, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) config.Servers[idx] = server;
            else config.Servers.Add(server);
            SaveConfig(config);
        }

        public bool RemoveServer(string name)
        {
            var config = LoadConfig();
            var server = config.Servers.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (server == null) return false;
            config.Servers.Remove(server);
            SaveConfig(config);
            return true;
        }

        public UserDefaults GetDefaults() => LoadConfig().Defaults ?? new UserDefaults();

        public string GetConfigFilePath() => _configFilePath;

        public bool ConfigFileExists() => File.Exists(_configFilePath);

        public UserConfig? TryLoadLegacyConfig()
        {
            if (!File.Exists(_legacyConfigFilePath)) return null;

            try
            {
                var json = File.ReadAllText(_legacyConfigFilePath);
                var strippedJson = StripJsonComments(json);

                using var doc = System.Text.Json.JsonDocument.Parse(strippedJson);
                var root = doc.RootElement;

                if (!root.TryGetProperty("Servers", out var serversSection))
                {
                    return null;
                }

                var config = new UserConfig();

                if (serversSection.TryGetProperty("ServersList", out var serversList) && serversList.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var serverJson in serversList.EnumerateArray())
                    {
                        var server = new ServerConfigEntry
                        {
                            Name = serverJson.TryGetProperty("Name", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty,
                            Host = serverJson.TryGetProperty("Host", out var hostProp) ? hostProp.GetString() ?? string.Empty : string.Empty,
                            Port = serverJson.TryGetProperty("Port", out var portProp) ? portProp.GetInt32() : 5432,
                            Username = serverJson.TryGetProperty("Username", out var userProp) ? userProp.GetString() ?? string.Empty : string.Empty,
                            Password = serverJson.TryGetProperty("Password", out var passProp) ? passProp.GetString() ?? string.Empty : string.Empty,
                            Databases = serverJson.TryGetProperty("Databases", out var dbProp) && dbProp.ValueKind == System.Text.Json.JsonValueKind.Array
                                ? dbProp.EnumerateArray().Select(d => d.GetString() ?? string.Empty).ToList()
                                : [],
                            SslMode = serverJson.TryGetProperty("SslMode", out var sslProp) ? sslProp.GetString() ?? "Prefer" : "Prefer",
                            Timeout = serverJson.TryGetProperty("Timeout", out var timeoutProp) ? timeoutProp.GetInt32() : 30,
                            CommandTimeout = serverJson.TryGetProperty("CommandTimeout", out var cmdTimeoutProp) ? cmdTimeoutProp.GetInt32() : 300
                        };

                        config.Servers.Add(server);
                    }
                }

                return config;
            }
            catch
            {
                return null;
            }
        }

        private static void ApplyDefaults(UserConfig config)
        {
            config.Defaults ??= new UserDefaults();
            foreach (var server in config.Servers)
            {
                if (server.Port == 0) server.Port = 5432;
                if (string.IsNullOrWhiteSpace(server.SslMode)) server.SslMode = "Prefer";
                if (server.Timeout == 0) server.Timeout = 30;
                if (server.CommandTimeout == 0) server.CommandTimeout = 300;
                if (server.MaxParallelism == 0) server.MaxParallelism = 4;
                if (server.ExcludePatterns == null || server.ExcludePatterns.Count == 0)
                    server.ExcludePatterns = ["template*", "postgres"];
            }
        }

        private static string StripJsonComments(string jsonc)
        {
            return System.Text.RegularExpressions.Regex.Replace(jsonc, @"//.*?$|/\*[\s\S]*?\*/", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        }
    }
}
