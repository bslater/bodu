// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IPaddingStrategy.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Defines methods for applying and removing padding to data blocks in block cipher operations.
/// </summary>
/// <remarks>
/// <para>
/// Block ciphers typically require input to be a multiple of a fixed block size. This interface provides a strategy pattern to apply or
/// remove padding schemes such as PKCS#7, zero-padding, or no padding.
/// </para>
/// <para>
/// Implementations must ensure that padding is applied correctly and that unpadding validates or strips padding bytes in accordance
/// with the scheme’s rules.
/// </para>
/// <para>
/// <strong>Built-in implementations.</strong>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Pkcs7Padding"/> — RFC 5652 / PKCS#7 (the de-facto standard).</description></item>
///   <item><description><see cref="Ansix923Padding"/> — ANSI X.923 (zero pad with length byte).</description></item>
///   <item><description><see cref="Iso10126Padding"/> — ISO 10126 (random pad with length byte).</description></item>
///   <item><description><see cref="Iso7816_4Padding"/> — ISO/IEC 7816-4 (<c>0x80</c> sentinel followed by zeros).</description></item>
///   <item><description><see cref="ZeroPadding"/> — pad with zero bytes; <strong>not</strong> length-recoverable.</description></item>
///   <item><description><see cref="NoPadding"/> — pass-through; the caller guarantees alignment.</description></item>
/// </list>
/// <para>
/// <strong>Choosing a scheme.</strong> Pick <see cref="Pkcs7Padding"/> for any new design that requires
/// length-recoverable padding under a confidentiality-only mode (CBC, ECB). Pick <see cref="Iso7816_4Padding"/>
/// when interoperating with smartcard / EMV / ISO crypto tooling. Pick <see cref="ZeroPadding"/> only when the
/// plaintext format itself encodes its length (so the trailing zeros can be discarded by the application
/// layer) — and prefer one of the self-describing schemes otherwise. <see cref="NoPadding"/> is appropriate
/// when the surrounding mode handles alignment itself (CTR, CTS, AEAD modes).
/// </para>
/// <para>
/// Most callers go through <see cref="System.Security.Cryptography.SymmetricAlgorithm.Padding"/> (which
/// dispatches via <see cref="PaddingFactory"/>) rather than constructing implementations directly. Direct
/// use is appropriate when wiring custom transforms that do not extend
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm"/>. Implementations are stateless and safe to
/// share across threads.
/// </para>
/// </remarks>
public interface IPaddingStrategy
{
    /// <summary>
    /// Gets a value indicating whether <see cref="Unpad" /> inspects the final block
    /// and may return fewer bytes than it received. Self-describing schemes such as
    /// PKCS#7, ANSI X.923, ISO 10126 and ISO/IEC 7816-4 return <see langword="true" />;
    /// pass-through schemes such as zero padding and no padding return <see langword="false" />.
    /// </summary>
    /// <remarks>
    /// Streaming block-cipher transforms use this flag to decide whether the final
    /// ciphertext block must be deferred during decryption so that padding validation
    /// and removal can happen at the stream boundary.
    /// </remarks>
    bool StripsPaddingOnUnpad { get; }

    /// <summary>
    /// Applies padding to the input data to align it with the specified block size.
    /// </summary>
    /// <param name="input">The input data to be padded.</param>
    /// <param name="blockSize">The block size in bytes that the padded output must align to.</param>
    /// <returns>A new byte array containing the padded data. The length of the result will be a multiple of <paramref name="blockSize" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="blockSize" /> is less than or equal to zero.</exception>
    byte[] Pad(ReadOnlySpan<byte> input, int blockSize);

    /// <summary>
    /// Removes padding from the input data based on the strategy's padding rules.
    /// </summary>
    /// <param name="input">The padded input data to unpad. Must be a multiple of the block size.</param>
    /// <param name="blockSize">The block size in bytes used during the original padding operation.</param>
    /// <returns>A new byte array containing the unpadded data.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the input does not conform to the expected padding scheme or is not a multiple of <paramref name="blockSize" />.
    /// </exception>
    byte[] Unpad(ReadOnlySpan<byte> input, int blockSize);
}
