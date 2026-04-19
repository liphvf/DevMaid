using FurLab.Core.Services;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FurLab.Tests.Commands;

[TestClass]
public class CredentialServiceTests
{
    private IDataProtectionProvider _provider = null!;

    [TestInitialize]
    public void Setup()
    {
        // Use an in-memory DataProtection provider for tests (no filesystem keys)
        // Must use same ApplicationName as the real app for encryption/decryption to work
        var services = new ServiceCollection();
        services.AddDataProtection()
            .SetApplicationName("FurLab")
            .UseEphemeralDataProtectionProvider(); // Use in-memory keys for tests
        _provider = services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
    }

    [TestMethod(DisplayName = "Encrypt returns non-empty string different from plaintext")]
    public void Encrypt_ReturnsNonEmptyDifferentFromPlaintext()
    {
        var service = new CredentialService(_provider);
        var plaintext = "my-secret-password";

        var encrypted = service.Encrypt(plaintext);

        Assert.IsFalse(string.IsNullOrEmpty(encrypted));
        Assert.AreNotEqual(plaintext, encrypted);
    }

    [TestMethod(DisplayName = "TryDecrypt returns original plaintext after Encrypt")]
    public void TryDecrypt_AfterEncrypt_ReturnsOriginalPlaintext()
    {
        var service = new CredentialService(_provider);
        var plaintext = "my-secret-password";

        var encrypted = service.Encrypt(plaintext);
        var decrypted = service.TryDecrypt(encrypted);

        Assert.AreEqual(plaintext, decrypted);
    }

    [TestMethod(DisplayName = "TryDecrypt retorna null para input nulo")]
    public void TryDecrypt_NullInput_ReturnsNull()
    {
        var service = new CredentialService(_provider);

        var result = service.TryDecrypt(null);

        Assert.IsNull(result);
    }

    [TestMethod(DisplayName = "TryDecrypt retorna null para string vazia")]
    public void TryDecrypt_EmptyInput_ReturnsNull()
    {
        var service = new CredentialService(_provider);

        var result = service.TryDecrypt(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod(DisplayName = "TryDecrypt returns null for corrupted blob (does not throw exception)")]
    public void TryDecrypt_CorruptedBlob_ReturnsNullWithoutThrowing()
    {
        var service = new CredentialService(_provider);

        var result = service.TryDecrypt("isto-nao-e-um-blob-valido");

        Assert.IsNull(result);
    }

    [TestMethod(DisplayName = "Encrypt is non-deterministic (different blobs for same password — DataProtection uses random IV)")]
    public void Encrypt_SamePlaintext_ProducesDifferentBlobs()
    {
        // DataProtection uses random IV so each call produces a different blob,
        // both decryptable to the same value
        var service = new CredentialService(_provider);
        var plaintext = "my-password";

        var blob1 = service.Encrypt(plaintext);
        var blob2 = service.Encrypt(plaintext);

        // Different blobs (different IVs)
        Assert.AreNotEqual(blob1, blob2);

        // But both decrypt to the same plaintext
        Assert.AreEqual(plaintext, service.TryDecrypt(blob1));
        Assert.AreEqual(plaintext, service.TryDecrypt(blob2));
    }

    [TestMethod(DisplayName = "Encrypt with empty password works (does not throw exception)")]
    public void Encrypt_EmptyPassword_EncryptsSuccessfully()
    {
        var service = new CredentialService(_provider);

        var encrypted = service.Encrypt(string.Empty);
        var decrypted = service.TryDecrypt(encrypted);

        Assert.IsNotNull(encrypted);
        Assert.AreEqual(string.Empty, decrypted);
    }
}
