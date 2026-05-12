// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmExtensions.TryCreateEncryptor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu;

namespace Bodu.Security.Cryptography.Extensions;

public static partial class SymmetricAlgorithmExtensions
{
    /// <summary>
    /// Attempts to create an encryptor using the specified encryption key and initialisation vector (IV).
    /// </summary>
    /// <param name="algorithm">The symmetric algorithm to use for encryption. Must not be <see langword="null"/>.</param>
    /// <param name="key">The secret key to use for the cryptographic operation.</param>
    /// <param name="iv">The initialisation vector.</param>
    /// <param name="transform">
    /// When this method returns, contains the created <see cref="ICryptoTransform"/> if the operation succeeded; otherwise,
    /// <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if the encryptor was created successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method wraps <see cref="SymmetricAlgorithm.CreateEncryptor(byte[], byte[])"/> in a try/catch block. Any exception
    /// raised by the underlying algorithm — for example, due to an invalid key length or unsupported IV size — is suppressed and
    /// results in a <see langword="false"/> return value.
    /// </para>
    /// </remarks>
    public static bool TryCreateEncryptor(
        this SymmetricAlgorithm algorithm,
        byte[] key,
        byte[] iv,
        out ICryptoTransform? transform)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        try
        {
            transform = algorithm.CreateEncryptor(key, iv);
            return true;
        }
        catch
        {
            transform = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to create an encryptor using the algorithm's current <see cref="SymmetricAlgorithm.Key"/> and
    /// <see cref="SymmetricAlgorithm.IV"/> values.
    /// </summary>
    /// <param name="algorithm">The symmetric algorithm to use for encryption. Must not be <see langword="null"/>.</param>
    /// <param name="transform">
    /// When this method returns, contains the created <see cref="ICryptoTransform"/> if the operation succeeded; otherwise,
    /// <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if the encryptor was created successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithm"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use this overload when the algorithm instance has already been configured, for example via
    /// <see cref="SymmetricAlgorithm.GenerateKey"/> and <see cref="SymmetricAlgorithm.GenerateIV"/>. Any exception raised by
    /// the underlying algorithm is suppressed and results in a <see langword="false"/> return value.
    /// </para>
    /// </remarks>
    public static bool TryCreateEncryptor(
        this SymmetricAlgorithm algorithm,
        out ICryptoTransform? transform)
    {
        ThrowHelper.ThrowIfNull(algorithm);

        try
        {
            transform = algorithm.CreateEncryptor();
            return true;
        }
        catch
        {
            transform = null;
            return false;
        }
    }
}
