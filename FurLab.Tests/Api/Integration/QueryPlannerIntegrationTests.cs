using FurLab.Core.Models;
using FurLab.Core.Services;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace FurLab.Tests.Api.Integration;

[TestClass]
[TestCategory("Integration")]
public class QueryPlannerIntegrationTests : PostgreSqlIntegrationTestBase
{
    private QueryPlannerService _planner = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
        await InitializeAsync();

        var credentialMock = new Mock<Core.Interfaces.ICredentialService>();
        credentialMock.Setup(x => x.TryDecrypt(It.IsAny<string?>())).Returns("testpassword");

        _planner = new QueryPlannerService(credentialMock.Object, NullLogger<QueryPlannerService>.Instance);
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        await DisposeAsync();
    }

    [TestMethod(DisplayName = "ListDatabasesAsync_LocalContainer_ReturnsTestDatabases")]
    public async Task ListDatabasesAsync_LocalContainer_ReturnsTestDatabases()
    {
        var server = new ServerConfigEntry
        {
            Name = "local",
            Host = "localhost",
            Port = MappedPort,
            Username = "postgres",
            EncryptedPassword = null,
            ExcludePatterns = ["template*"]
        };

        var databases = await _planner.ListDatabasesAsync(server);

        Assert.IsTrue(databases.Count > 0);
        CollectionAssert.Contains(databases.ToList(), "furlab_test_db1");
        CollectionAssert.Contains(databases.ToList(), "furlab_test_db2");
        CollectionAssert.Contains(databases.ToList(), "furlab_test_db3");
    }

    [TestMethod(DisplayName = "GetDatabasesForServerAsync_FetchAll_ExcludesTemplates")]
    public async Task GetDatabasesForServerAsync_FetchAll_ExcludesTemplates()
    {
        var server = new ServerConfigEntry
        {
            Name = "local",
            Host = "localhost",
            Port = MappedPort,
            Username = "postgres",
            EncryptedPassword = null,
            FetchAllDatabases = true,
            ExcludePatterns = ["template*"]
        };

        var databases = await _planner.GetDatabasesForServerAsync(server, []);

        Assert.IsFalse(databases.Any(d => d.StartsWith("template", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod(DisplayName = "GetDatabasesForServerAsync_WithExcludeNames_FiltersOut")]
    public async Task GetDatabasesForServerAsync_WithExcludeNames_FiltersOut()
    {
        var server = new ServerConfigEntry
        {
            Name = "local",
            Host = "localhost",
            Port = MappedPort,
            Username = "postgres",
            EncryptedPassword = null,
            FetchAllDatabases = true,
            ExcludePatterns = ["template*"]
        };

        var exclude = new HashSet<string>(["furlab_test_db1"], StringComparer.OrdinalIgnoreCase);
        var databases = await _planner.GetDatabasesForServerAsync(server, exclude);

        Assert.IsFalse(databases.Contains("furlab_test_db1"));
        Assert.IsTrue(databases.Contains("furlab_test_db2"));
    }

    [TestMethod(DisplayName = "BuildConnectionStringForServer_UsesCorrectPort")]
    public void BuildConnectionStringForServer_UsesCorrectPort()
    {
        var server = new ServerConfigEntry
        {
            Name = "local",
            Host = "localhost",
            Port = MappedPort,
            Username = "postgres",
            EncryptedPassword = null
        };

        var connectionString = _planner.BuildConnectionStringForServer(server, "furlab_test_db1");

        StringAssert.Contains(connectionString, $"Port={MappedPort}");
        StringAssert.Contains(connectionString, "Database=furlab_test_db1");
    }
}
