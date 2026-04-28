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
