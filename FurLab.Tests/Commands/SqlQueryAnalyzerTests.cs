using FurLab.CLI.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FurLab.Tests.Commands;

[TestClass]
public class SqlQueryAnalyzerTests
{
    [TestMethod(DisplayName = "SELECT é classificada como Safe")]
    public void AnalyzeQuery_Select_ReturnsSafe()
    {
        Assert.AreEqual(QueryType.Safe, SqlQueryAnalyzer.AnalyzeQuery("SELECT * FROM users"));
    }

    [TestMethod(DisplayName = "SHOW é classificada como Safe")]
    public void AnalyzeQuery_Show_ReturnsSafe()
    {
        Assert.AreEqual(QueryType.Safe, SqlQueryAnalyzer.AnalyzeQuery("SHOW tables"));
    }

    [TestMethod(DisplayName = "EXPLAIN é classificada como Safe")]
    public void AnalyzeQuery_Explain_ReturnsSafe()
    {
        Assert.AreEqual(QueryType.Safe, SqlQueryAnalyzer.AnalyzeQuery("EXPLAIN SELECT * FROM users"));
    }

    [TestMethod(DisplayName = "INSERT é classificada como Destructive")]
    public void AnalyzeQuery_Insert_ReturnsDestructive()
    {
        Assert.AreEqual(QueryType.Destructive, SqlQueryAnalyzer.AnalyzeQuery("INSERT INTO users (name) VALUES ('test')"));
    }

    [TestMethod(DisplayName = "UPDATE é classificada como Destructive")]
    public void AnalyzeQuery_Update_ReturnsDestructive()
    {
        Assert.AreEqual(QueryType.Destructive, SqlQueryAnalyzer.AnalyzeQuery("UPDATE users SET name = 'test' WHERE id = 1"));
    }

    [TestMethod(DisplayName = "DELETE é classificada como Destructive")]
    public void AnalyzeQuery_Delete_ReturnsDestructive()
    {
        Assert.AreEqual(QueryType.Destructive, SqlQueryAnalyzer.AnalyzeQuery("DELETE FROM users WHERE id = 1"));
    }

    [TestMethod(DisplayName = "ALTER é classificada como Destructive")]
    public void AnalyzeQuery_Alter_ReturnsDestructive()
    {
        Assert.AreEqual(QueryType.Destructive, SqlQueryAnalyzer.AnalyzeQuery("ALTER TABLE users ADD COLUMN email TEXT"));
    }

    [TestMethod(DisplayName = "DROP é classificada como Destructive")]
    public void AnalyzeQuery_Drop_ReturnsDestructive()
    {
        Assert.AreEqual(QueryType.Destructive, SqlQueryAnalyzer.AnalyzeQuery("DROP TABLE users"));
    }

    [TestMethod(DisplayName = "CREATE é classificada como Destructive")]
    public void AnalyzeQuery_Create_ReturnsDestructive()
    {
        Assert.AreEqual(QueryType.Destructive, SqlQueryAnalyzer.AnalyzeQuery("CREATE TABLE test (id INT)"));
    }

    [TestMethod(DisplayName = "TRUNCATE é classificada como Destructive")]
    public void AnalyzeQuery_Truncate_ReturnsDestructive()
    {
        Assert.AreEqual(QueryType.Destructive, SqlQueryAnalyzer.AnalyzeQuery("TRUNCATE TABLE users"));
    }

    [TestMethod(DisplayName = "MERGE é classificada como Destructive")]
    public void AnalyzeQuery_Merge_ReturnsDestructive()
    {
        Assert.AreEqual(QueryType.Destructive, SqlQueryAnalyzer.AnalyzeQuery("MERGE INTO target USING source ON (target.id = source.id) WHEN MATCHED THEN UPDATE SET target.val = source.val"));
    }

    [TestMethod(DisplayName = "GRANT é classificada como Destructive")]
    public void AnalyzeQuery_Grant_ReturnsDestructive()
    {
        Assert.AreEqual(QueryType.Destructive, SqlQueryAnalyzer.AnalyzeQuery("GRANT SELECT ON users TO readonly"));
    }

    [TestMethod(DisplayName = "REVOKE é classificada como Destructive")]
    public void AnalyzeQuery_Revoke_ReturnsDestructive()
    {
        Assert.AreEqual(QueryType.Destructive, SqlQueryAnalyzer.AnalyzeQuery("REVOKE SELECT ON users FROM readonly"));
    }

    [TestMethod(DisplayName = "SET ROLE é classificada como Destructive")]
    public void AnalyzeQuery_SetRole_ReturnsDestructive()
    {
        Assert.AreEqual(QueryType.Destructive, SqlQueryAnalyzer.AnalyzeQuery("SET ROLE admin"));
    }

    [TestMethod(DisplayName = "SET sem ROLE é classificada como Safe")]
    public void AnalyzeQuery_SetWithoutRole_ReturnsSafe()
    {
        Assert.AreEqual(QueryType.Safe, SqlQueryAnalyzer.AnalyzeQuery("SET search_path TO public"));
    }

    [TestMethod(DisplayName = "Query vazia é classificada como Safe")]
    public void AnalyzeQuery_Empty_ReturnsSafe()
    {
        Assert.AreEqual(QueryType.Safe, SqlQueryAnalyzer.AnalyzeQuery(""));
        Assert.AreEqual(QueryType.Safe, SqlQueryAnalyzer.AnalyzeQuery("   "));
    }

    [TestMethod(DisplayName = "Comentário de linha é ignorado antes da classificação")]
    public void AnalyzeQuery_LineComment_IgnoresComment()
    {
        Assert.AreEqual(QueryType.Safe, SqlQueryAnalyzer.AnalyzeQuery("-- This is a comment\nSELECT * FROM users"));
    }

    [TestMethod(DisplayName = "Comentário de bloco é ignorado antes da classificação")]
    public void AnalyzeQuery_BlockComment_IgnoresComment()
    {
        Assert.AreEqual(QueryType.Safe, SqlQueryAnalyzer.AnalyzeQuery("/* block comment */ SELECT * FROM users"));
    }

    [TestMethod(DisplayName = "Comentário antes de DELETE não impede detecção")]
    public void AnalyzeQuery_CommentBeforeDelete_StillDetectsDestructive()
    {
        Assert.AreEqual(QueryType.Destructive, SqlQueryAnalyzer.AnalyzeQuery("-- careful!\nDELETE FROM users"));
    }

    [TestMethod(DisplayName = "CTE com SELECT interno é classificada como Safe")]
    public void AnalyzeQuery_CteWithSelect_ReturnsSafe()
    {
        Assert.AreEqual(QueryType.Safe, SqlQueryAnalyzer.AnalyzeQuery("WITH temp AS (SELECT * FROM users) SELECT * FROM temp"));
    }

    [TestMethod(DisplayName = "CTE com INSERT interno é classificada como Destructive")]
    public void AnalyzeQuery_CteWithInsert_ReturnsDestructive()
    {
        Assert.AreEqual(QueryType.Destructive, SqlQueryAnalyzer.AnalyzeQuery("WITH temp AS (INSERT INTO users VALUES (1)) SELECT * FROM temp"));
    }

    [TestMethod(DisplayName = "GetQueryTypeDescription retorna keyword em maiúscula")]
    public void GetQueryTypeDescription_ReturnsUppercaseKeyword()
    {
        Assert.AreEqual("SELECT", SqlQueryAnalyzer.GetQueryTypeDescription("select * from users"));
        Assert.AreEqual("DELETE", SqlQueryAnalyzer.GetQueryTypeDescription("delete from users"));
    }

    [TestMethod(DisplayName = "GetQueryTypeDescription retorna UNKNOWN para query vazia")]
    public void GetQueryTypeDescription_Empty_ReturnsUnknown()
    {
        Assert.AreEqual("UNKNOWN", SqlQueryAnalyzer.GetQueryTypeDescription(""));
    }
}
