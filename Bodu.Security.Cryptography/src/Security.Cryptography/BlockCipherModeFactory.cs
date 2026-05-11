// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherModeFactory.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Creates <see cref="IBlockCipherModeTransform"/> instances that wrap an <see cref="IBlockCipher"/> with a standard chaining mode.
/// </summary>
/// <remarks>
/// <para>
/// The factory dispatches a <see cref="CipherBlockMode"/> enumeration value to the matching mode-transform
/// implementation. Used internally by every <see cref="System.Security.Cryptography.SymmetricAlgorithm"/> in
/// this library when its <see cref="System.Security.Cryptography.SymmetricAlgorithm.Mode"/> is set, so that
/// the same enum value selects the appropriate transform regardless of which cipher is in play.
/// </para>
/// <para>
/// <strong>What this factory covers and does not cover.</strong> Only the classic confidentiality-only modes
/// (ECB, CBC, CFB, OFB, CTR) are dispatched here. <see cref="CtsModeTransform"/> and <see cref="XtsModeTransform"/>
/// are not in the <see cref="CipherBlockMode"/> enumeration and must be constructed directly. Authenticated
/// modes have their own contract and lifecycle — construct an
/// <see cref="IAeadBlockCipherModeTransform"/> implementation directly, or use the helpers on
/// <see cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions"/>.
/// </para>
/// </remarks>
/// <example>
/// The following example composes a block cipher, a CBC mode transform, and PKCS#7 padding to encrypt a message:
/// <code>
/// using IBlockCipher cipher = /* construct an IBlockCipher, e.g. an AES wrapper */;
/// IBlockCipherModeTransform mode = BlockCipherModeFactory.Create(CipherBlockMode.CBC, cipher, iv);
/// IPaddingStrategy padding = PaddingFactory.Create(PaddingMode.PKCS7);
///
/// byte[] padded = padding.Pad(plaintext, cipher.BlockSize);
/// byte[] ciphertext = new byte[padded.Length];
/// mode.Transform(padded, ciphertext, encrypt: true);
/// </code>
/// </example>
public static class BlockCipherModeFactory
{
    /// <summary>
    /// Creates a new <see cref="IBlockCipherModeTransform"/> instance for the specified block cipher mode.
    /// </summary>
    /// <param name="mode">The cipher mode to apply (for example <see cref="CipherBlockMode.CBC"/>, <see cref="CipherBlockMode.CFB"/>,
    /// <see cref="CipherBlockMode.OFB"/>, <see cref="CipherBlockMode.ECB"/>, or <see cref="CipherBlockMode.CTR"/>).</param>
    /// <param name="cipher">The underlying block cipher to wrap.</param>
    /// <param name="iv">The initialisation vector or initial counter. Required by all modes except <see cref="CipherBlockMode.ECB"/>
    /// and must have the same length as <see cref="IBlockCipher.BlockSize"/>.</param>
    /// <returns>An <see cref="IBlockCipherModeTransform"/> that applies <paramref name="mode"/> over <paramref name="cipher"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="iv"/> is required but <see langword="null"/> or of the wrong length.</exception>
    /// <exception cref="NotSupportedException">Thrown if <paramref name="mode"/> is not a supported <see cref="CipherBlockMode"/> value.</exception>
    public static IBlockCipherModeTransform Create(
        CipherBlockMode mode,
        IBlockCipher cipher,
        byte[]? iv = null)
    {
        ThrowHelper.ThrowIfNull(cipher);

        var blockSize = cipher.BlockSize;

        switch (mode)
        {
            case CipherBlockMode.ECB:
                return new EcbModeTransform(cipher);

            case CipherBlockMode.CBC:
                ValidateIv(nameof(iv), iv, blockSize);
                return new CbcModeTransform(cipher, iv!);

            case CipherBlockMode.CFB:
                ValidateIv(nameof(iv), iv, blockSize);
                return new CfbModeTransform(cipher, iv!);

            case CipherBlockMode.OFB:
                ValidateIv(nameof(iv), iv, blockSize);
                return new OfbModeTransform(cipher, iv!);

            case CipherBlockMode.CTR:
                ValidateIv(nameof(iv), iv, blockSize);
                return new CtrModeTransform(cipher, iv!);

            default:
                throw new NotSupportedException($"The cipher mode '{mode}' is not supported.");
        }
    }

    /// <summary>
    /// Validates that <paramref name="iv"/> is non-null and exactly
    /// <paramref name="requiredLength"/> bytes long; otherwise throws
    /// <see cref="ArgumentException"/> against the supplied parameter name.
    /// </summary>
    /// <param name="name">The caller-visible parameter name reported in any exception.</param>
    /// <param name="iv">The initialisation vector to validate.</param>
    /// <param name="requiredLength">The required length of <paramref name="iv"/> in bytes.</param>
    /// <exception cref="ArgumentException"><paramref name="iv"/> is <see langword="null"/> or
    /// does not have the required length.</exception>
    private static void ValidateIv(string name, byte[]? iv, int requiredLength)
    {
        if (iv is null)
        {
            throw new ArgumentException("An initialisation vector is required for this mode.", name);
        }

        if (iv.Length != requiredLength)
        {
            throw new ArgumentException(
                $"The initialisation vector must be {requiredLength * 8} bits ({requiredLength} bytes) long; the supplied IV is {iv.Length * 8} bits ({iv.Length} bytes).",
                name);
        }
    }
}
