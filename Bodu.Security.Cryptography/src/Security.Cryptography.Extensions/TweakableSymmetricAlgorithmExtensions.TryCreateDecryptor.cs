// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmExtensions.TryCreateDecryptor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;
using Bodu.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

public static partial class TweakableSymmetricAlgorithmExtensions
{
    /// <summary>
    /// Attempts to create a decryptor using the current <see cref="SymmetricAlgorithm.Key" />, <see cref="SymmetricAlgorithm.IV" />,
    /// and <see cref="TweakableSymmetricAlgorithm.Tweak" /> values of the algorithm.
    /// </summary>
    /// <param name="algorithm">The tweakable symmetric algorithm to use for decryption. Must not be <see langword="null" />.</param>
    /// <param name="transform">
    /// When this method returns, contains the created <see cref="ICryptoTransform" /> if the operation succeeded; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> if the decryptor was successfully created; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// This method wraps <see cref="TweakableSymmetricAlgorithm.CreateDecryptor()" /> in a try/catch block, returning
    /// <see langword="false" /> if the operation fails due to an invalid or uninitialised key, IV, or tweak.
    /// </para>
    /// <para>
    /// Use this overload when the algorithm has already been fully configured. To supply keying material explicitly, use
    /// <see cref="TryCreateDecryptor(TweakableSymmetricAlgorithm, byte[], byte[], byte[], out ICryptoTransform)" />.
    /// </para>
    /// </remarks>
    public static bool TryCreateDecryptor(
        this TweakableSymmetricAlgorithm algorithm,
        out ICryptoTransform? transform)
    {
        ArgumentNullException.ThrowIfNull(algorithm);

        try
        {
            transform = algorithm.CreateDecryptor();
            return true;
        }
        catch
        {
            transform = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to create a decryptor using the specified key, initialisation vector, and tweak.
    /// </summary>
    /// <param name="algorithm">The tweakable symmetric algorithm to use for decryption. Must not be <see langword="null" />.</param>
    /// <param name="key">The decryption key.</param>
    /// <param name="iv">The initialisation vector.</param>
    /// <param name="tweak">The tweak value to apply during decryption.</param>
    /// <param name="transform">
    /// When this method returns, contains the created <see cref="ICryptoTransform" /> if the operation succeeded; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> if the decryptor was successfully created; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="algorithm" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This method wraps <see cref="TweakableSymmetricAlgorithm.CreateDecryptor(byte[], byte[], byte[])" /> in a try/catch block,
    /// returning <see langword="false" /> if the operation fails due to an invalid key, IV, or tweak. Use this overload when all
    /// keying material must be supplied explicitly.
    /// </remarks>
    public static bool TryCreateDecryptor(
        this TweakableSymmetricAlgorithm algorithm,
        byte[] key,
        byte[] iv,
        byte[] tweak,
        out ICryptoTransform? transform)
    {
        ArgumentNullException.ThrowIfNull(algorithm);

        try
        {
            transform = algorithm.CreateDecryptor(key, iv, tweak);
            return true;
        }
        catch
        {
            transform = null;
            return false;
        }
    }
}
