using FurLab.Core.Interfaces;

using Microsoft.Extensions.DependencyInjection;

namespace FurLab.CLI.Services;

/// <summary>
/// Provides credential encryption/decryption services for the FurLab CLI.
/// This is a compatibility wrapper around the Core <see cref="ICredentialService"/>.
/// </summary>
public static class CredentialService
{
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// Sets the service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    internal static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private static ICredentialService GetCredentialService()
    {
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider not initialized. Call SetServiceProvider first.");
        }

        return _serviceProvider.GetRequiredService<ICredentialService>();
    }

    /// <summary>
    /// Encrypts a plaintext password.
    /// </summary>
    public static string Encrypt(string plaintext) => GetCredentialService().Encrypt(plaintext);

    /// <summary>
    /// Attempts to decrypt an encrypted password blob.
    /// Returns <see langword="null"/> if decryption fails, the input is null, or the input is empty.
    /// </summary>
    public static string? TryDecrypt(string? encrypted) => GetCredentialService().TryDecrypt(encrypted);
}
