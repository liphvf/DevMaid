using FurLab.CLI;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FurLab.Tests.Commands;

/// <summary>
/// Testes unitários para SecurityUtils — validações de host, porta e usuário,
/// incluindo suporte a curingas pgpass (*).
/// </summary>
[TestClass]
public class SecurityUtilsTests
{
    // =========================================================================
    // IsValidHost — curinga
    // =========================================================================

    [TestMethod]
    [TestCategory("pgpass-wildcard")]
    public void IsValidHost_Curinga_RetornaVerdadeiro()
    {
        Assert.IsTrue(SecurityUtils.IsValidHost("*"));
    }

    [TestMethod]
    [TestCategory("pgpass-wildcard")]
    public void IsValidHost_CuringaComEspacoAnteriores_RetornaFalso()
    {
        Assert.IsFalse(SecurityUtils.IsValidHost(" * "));
    }

    [TestMethod]
    [TestCategory("pgpass-wildcard")]
    public void IsValidHost_CuringaComEspacoFinal_RetornaFalso()
    {
        Assert.IsFalse(SecurityUtils.IsValidHost("* "));
    }

    // =========================================================================
    // IsValidPort — curinga
    // =========================================================================

    [TestMethod]
    [TestCategory("pgpass-wildcard")]
    public void IsValidPort_Curinga_RetornaVerdadeiro()
    {
        Assert.IsTrue(SecurityUtils.IsValidPort("*"));
    }

    [TestMethod]
    [TestCategory("pgpass-wildcard")]
    public void IsValidPort_CuringaComEspacos_RetornaFalso()
    {
        Assert.IsFalse(SecurityUtils.IsValidPort(" * "));
    }

    [TestMethod]
    [TestCategory("pgpass-wildcard")]
    public void IsValidPort_PortaNumericaValida_RetornaVerdadeiro()
    {
        Assert.IsTrue(SecurityUtils.IsValidPort("5432"));
    }

    [TestMethod]
    [TestCategory("pgpass-wildcard")]
    public void IsValidPort_PortaNegativa_RetornaFalso()
    {
        Assert.IsFalse(SecurityUtils.IsValidPort("-1"));
    }

    [TestMethod]
    [TestCategory("pgpass-wildcard")]
    public void IsValidPort_PortaAcimaDoMaximo_RetornaFalso()
    {
        Assert.IsFalse(SecurityUtils.IsValidPort("99999"));
    }

    // =========================================================================
    // IsValidUsername — curinga
    // =========================================================================

    [TestMethod]
    [TestCategory("pgpass-wildcard")]
    public void IsValidUsername_Curinga_RetornaVerdadeiro()
    {
        Assert.IsTrue(SecurityUtils.IsValidUsername("*"));
    }

    [TestMethod]
    [TestCategory("pgpass-wildcard")]
    public void IsValidUsername_CuringaComEspacos_RetornaFalso()
    {
        Assert.IsFalse(SecurityUtils.IsValidUsername(" * "));
    }

    [TestMethod]
    [TestCategory("pgpass-wildcard")]
    public void IsValidUsername_CuringaComEspacoFinal_RetornaFalso()
    {
        Assert.IsFalse(SecurityUtils.IsValidUsername("* "));
    }
}
