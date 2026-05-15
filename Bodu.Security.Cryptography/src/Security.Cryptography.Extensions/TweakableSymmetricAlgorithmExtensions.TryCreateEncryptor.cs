// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmExtensions.TryCreateEncryptor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

public static partial class TweakableSymmetricAlgorithmExtensions
{
    /// <summary>
    /// Attempts to create an encryptor using the current <see cref="SymmetricAlgorithm.Key"/>, <see cref="SymmetricAlgorithm.IV"/>,
    /// and <see cref="TweakableSymmetricAlgorithm.Tweak"/> values of the algorithm.
    /// </summary>
    /// <param name="algorithm">The tweakable symmetric algorithm to use for encryption. Must not be <see langword="null"/>.</param>
    /// <param name="transform">
    /// When this method returns, contains the created <see cref="ICryptoTransform"/> if the operation succeeded; otherwise,
    /// <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if the encryptor was successfully created; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method wraps <see cref="TweakableSymmetricAlgorithm.CreateEncryptor()"/> in a try/catch block, returning
    /// <see langword="false"/> if the operation fails due to an invalid or uninitialized key, IV, or tweak.
    /// </para>
    /// <para>
    /// Use this overload when the algorithm has already been fully configured. To supply keying material explicitly, use
    /// <see cref="TryCreateEncryptor(TweakableSymmetricAlgorithm, byte[], byte[], byte[], out ICryptoTransform)"/>.
    /// </para>
    /// </remarks>
    public static bool TryCreateEncryptor(
        this TweakableSymmetricAlgorithm algorithm,
        out ICryptoTransform? transform)
    {
        ArgumentNullException.ThrowIfNull(algorithm);

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

    /// <summary>
    /// Attempts to create an encryptor using the specified key, initialization vector, and tweak.
    /// </summary>
    /// <param name="algorithm">The tweakable symmetric algorithm to use for encryption. Must not be <see langword="null"/>.</param>
    /// <param name="key">The encryption key.</param>
    /// <param name="iv">The initialization vector.</param>
    /// <param name="tweak">The tweak value to apply during encryption.</param>
    /// <param name="transform">
    /// When this method returns, contains the created <see cref="ICryptoTransform"/> if the operation succeeded; otherwise,
    /// <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if the encryptor was successfully created; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method wraps <see cref="TweakableSymmetricAlgorithm.CreateEncryptor(byte[], byte[], byte[])"/> in a try/catch block,
    /// returning <see langword="false"/> if the operation fails due to an invalid key, IV, or tweak. Use this overload when all
    /// keying material must be supplied explicitly.
    /// </remarks>
    public static bool TryCreateEncryptor(
        this TweakableSymmetricAlgorithm algorithm,
        byte[] key,
        byte[] iv,
        byte[] tweak,
        out ICryptoTransform? transform)
    {
        ArgumentNullException.ThrowIfNull(algorithm);

        try
        {
            transform = algorithm.CreateEncryptor(key, iv, tweak);
            return true;
        }
        catch
        {
            transform = null;
            return false;
        }
    }
}
