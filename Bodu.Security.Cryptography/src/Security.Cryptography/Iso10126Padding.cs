// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso10126Padding.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the ISO 10126 padding scheme, which appends <c>N - 1</c> cryptographically random bytes followed by a
/// trailing byte holding the padding length <c>N</c>.
/// </summary>
/// <remarks>
/// <para>
/// A full block of padding is always added when the input is already block-aligned so that <see cref="Unpad" /> can
/// unambiguously recover the original length. The interior pad bytes are not reconstructable on decryption, so only the
/// trailing length byte is validated. ISO 10126 was withdrawn by ISO in 2007; it is supported for interoperability with
/// existing ciphertexts.
/// </para>
/// <para>
/// <strong>When to choose ISO 10126.</strong> Only for legacy interop. The random pad bytes carry no security benefit
/// over <see cref="Pkcs7Padding" /> and the standard has been formally withdrawn. For new designs use PKCS#7 with an
/// authenticated mode, or pair an AEAD mode with <see cref="NoPadding" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Security.Cryptography;
///
/// // Legacy interop only — padding bytes are random; only the final length byte is validated.
/// IPaddingStrategy padding = new Iso10126Padding();
/// byte[] padded = padding.Pad(plaintext, blockSize: 128); // 128 bits = 16 bytes
/// byte[] recovered = padding.Unpad(padded, blockSize: 128);
///]]>
/// </code>
/// </example>
public sealed class Iso10126Padding
    : IPaddingStrategy
{
    /// <inheritdoc />
    public bool StripsPaddingOnUnpad => true;

    /// <summary>
    /// Applies ISO 10126 padding to the input data, ensuring the total output is a multiple of the block size.
    /// </summary>
    /// <param name="input">The data to pad.</param>
    /// <param name="blockSize">The block size in bits. Must be a positive multiple of 8.</param>
    /// <returns>The padded data as a byte array.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="blockSize" /> is not a positive multiple of 8.
    /// </exception>
    public byte[] Pad(ReadOnlySpan<byte> input, int blockSize)
    {
        ThrowHelper.ThrowIfNotPositiveMultipleOf(blockSize, 8);

        var size = blockSize / 8;
        var paddingLength = size - (input.Length % size);
        if (paddingLength == 0)
            paddingLength = size;

        var result = new byte[input.Length + paddingLength];
        input.CopyTo(result);

        // Fill the interior pad region with random bytes, then overwrite the final byte
        // with the pad length. paddingLength - 1 can be zero (when paddingLength == 1),
        // in which case the interior region is empty and only the length byte is written.
        var interiorLength = paddingLength - 1;
        if (interiorLength > 0)
        {
            Span<byte> interior = result.AsSpan(input.Length, interiorLength);
            RandomNumberGenerator.Fill(interior);
        }

        result[^1] = (byte)paddingLength;

        return result;
    }

    /// <summary>
    /// Validates and removes ISO 10126 padding from the specified input data.
    /// </summary>
    /// <param name="input">The padded data.</param>
    /// <param name="blockSize">The block size in bits. Must be a positive multiple of 8.</param>
    /// <returns>The unpadded data as a byte array.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="blockSize" /> is not a positive multiple of 8.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="input" /> is empty or not aligned to the block size.
    /// </exception>
    /// <exception cref="CryptographicException">Thrown if the trailing length byte is out of range.</exception>
    public byte[] Unpad(ReadOnlySpan<byte> input, int blockSize)
    {
        ThrowHelper.ThrowIfNotPositiveMultipleOf(blockSize, 8);

        var size = blockSize / 8;

        if (input.Length == 0 || input.Length % size != 0)
            CryptoHelpers.ThrowInvalidPaddedSequence("ISO 10126", nameof(input));

        var length = input.Length;
        int padLen = input[length - 1];

        // Only the trailing length byte can be validated; interior pad bytes are random.
        if (padLen < 1 || padLen > size)
            CryptoHelpers.ThrowInvalidPadding("ISO 10126");

        return input[.. (length - padLen)].ToArray();
    }
}
