using FurLab.CLI;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FurLab.Tests.Commands;

/// <summary>
/// Testes unitários para SecurityUtils — validações de host, porta e usuários,
/// incluindo suporte a curingas pgpass (*).
/// </summary>
[TestClass]
public class SecurityUtilsTests
{
    // =========================================================================
    // IsValidHost — curinga
    // =========================================================================

    [TestMethod(DisplayName = "IsValidHost com curinga '*' deve retornar verdadeiro")]
    [Description("Verifica que o caractere curinga '*' é aceito como host válido no formato pgpass.")]
    [TestCategory("pgpass-wildcard")]
    public void IsValidHost_Curinga_RetornaVerdadeiro()
    {
        Assert.IsTrue(SecurityUtils.IsValidHost("*"));
    }

    [TestMethod(DisplayName = "IsValidHost com curinga e espaços deve retornar falso")]
    [Description("Verifica que espaços ao redor do curinga '*' tornam o host inválido.")]
    [TestCategory("pgpass-wildcard")]
    public void IsValidHost_CuringaComEspacos_RetornaFalso()
    {
        Assert.IsFalse(SecurityUtils.IsValidHost(" * "));
    }

    [TestMethod(DisplayName = "IsValidHost com curinga e espaço final deve retornar falso")]
    [Description("Verifica que espaço após o curinga '*' torna o host inválido.")]
    [TestCategory("pgpass-wildcard")]
    public void IsValidHost_CuringaComEspacoFinal_RetornaFalso()
    {
        Assert.IsFalse(SecurityUtils.IsValidHost("* "));
    }

    // =========================================================================
    // IsValidPort — curinga
    // =========================================================================

    [TestMethod(DisplayName = "IsValidPort com curinga '*' deve retornar verdadeiro")]
    [Description("Verifica que o caractere curinga '*' é aceito como porta válida no formato pgpass.")]
    [TestCategory("pgpass-wildcard")]
    public void IsValidPort_Curinga_RetornaVerdadeiro()
    {
        Assert.IsTrue(SecurityUtils.IsValidPort("*"));
    }

    [TestMethod(DisplayName = "IsValidPort com curinga e espaços deve retornar falso")]
    [Description("Verifica que espaços ao redor do curinga '*' na porta tornam o valor inválido.")]
    [TestCategory("pgpass-wildcard")]
    public void IsValidPort_CuringaComEspacos_RetornaFalso()
    {
        Assert.IsFalse(SecurityUtils.IsValidPort(" * "));
    }

    [TestMethod(DisplayName = "IsValidPort com valor numérico válido deve retornar verdadeiro")]
    [Description("Verifica que números de porta dentro do intervalo válido (1-65535) são aceitos.")]
    [TestCategory("pgpass-wildcard")]
    public void IsValidPort_PortaNumericaValida_RetornaVerdadeiro()
    {
        Assert.IsTrue(SecurityUtils.IsValidPort("5432"));
    }

    [TestMethod(DisplayName = "IsValidPort com valor negativo deve retornar falso")]
    [Description("Verifica que números de porta negativos são rejeitados.")]
    [TestCategory("pgpass-wildcard")]
    public void IsValidPort_PortaNegativa_RetornaFalso()
    {
        Assert.IsFalse(SecurityUtils.IsValidPort("-1"));
    }

    [TestMethod(DisplayName = "IsValidPort com valor acima do máximo deve retornar falso")]
    [Description("Verifica que números de porta acima de 65535 são rejeitados.")]
    [TestCategory("pgpass-wildcard")]
    public void IsValidPort_PortaAcimaDoMaximo_RetornaFalso()
    {
        Assert.IsFalse(SecurityUtils.IsValidPort("99999"));
    }

    // =========================================================================
    // IsValidUsername — curinga
    // =========================================================================

    [TestMethod(DisplayName = "IsValidUsername com curinga '*' deve retornar verdadeiro")]
    [Description("Verifica que o caractere curinga '*' é aceito como username válido no formato pgpass.")]
    [TestCategory("pgpass-wildcard")]
    public void IsValidUsername_Curinga_RetornaVerdadeiro()
    {
        Assert.IsTrue(SecurityUtils.IsValidUsername("*"));
    }

    [TestMethod(DisplayName = "IsValidUsername com curinga e espaços deve retornar falso")]
    [Description("Verifica que espaços ao redor do curinga '*' tornam o username inválido.")]
    [TestCategory("pgpass-wildcard")]
    public void IsValidUsername_CuringaComEspacos_RetornaFalso()
    {
        Assert.IsFalse(SecurityUtils.IsValidUsername(" * "));
    }

    [TestMethod(DisplayName = "IsValidUsername com curinga e espaço final deve retornar falso")]
    [Description("Verifica que espaço após o curinga '*' torna o username inválido.")]
    [TestCategory("pgpass-wildcard")]
    public void IsValidUsername_CuringaComEspacoFinal_RetornaFalso()
    {
        Assert.IsFalse(SecurityUtils.IsValidUsername("* "));
    }
}
