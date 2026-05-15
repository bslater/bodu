// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58.Encode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Text.Encoding;

public static partial class Base58
{
    /// <summary>
    /// Encodes <paramref name="bytes" /> into a Base58 string using the supplied variant.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="variant">The Base58 variant.</param>
    /// <returns>A Base58 string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static string Encode(byte[] bytes, Base58Variant variant = Base58Variant.BitcoinFlickr)
    {
        ThrowHelper.ThrowIfNull(bytes);
        return Encode(bytes.AsSpan(), variant);
    }

    /// <summary>
    /// Encodes a portion of <paramref name="bytes" /> into a Base58 string.
    /// </summary>
    /// <param name="bytes">The byte array to encode.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="count">The number of bytes to encode.</param>
    /// <param name="variant">The Base58 variant.</param>
    /// <returns>A Base58 string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" />, <paramref name="count" />, or <paramref name="variant" /> is out of
    /// range.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the segment defined by <paramref name="offset" /> and <paramref name="count" /> exceeds the
    /// available range of <paramref name="bytes" />.
    /// </exception>
    public static string Encode(byte[] bytes, int offset, int count, Base58Variant variant = Base58Variant.BitcoinFlickr)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(bytes, offset, count);
        return Encode(bytes.AsSpan(offset, count), variant);
    }

    /// <summary>
    /// Encodes a span of bytes into a Base58 string.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="variant">The Base58 variant.</param>
    /// <returns>A Base58 string. Returns <see cref="string.Empty" /> for empty input.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static string Encode(ReadOnlySpan<byte> bytes, Base58Variant variant = Base58Variant.BitcoinFlickr)
    {
        string alphabet = GetAlphabet(variant);

        if (bytes.IsEmpty)
            return string.Empty;

        int upperBound = GetMaxEncodedLength(bytes.Length);
        char[] buffer = new char[upperBound];
        int written = EncodeIntoBuffer(bytes, alphabet, buffer);

        return new string(buffer, buffer.Length - written, written);
    }

    /// <summary>
    /// Encodes a span of bytes directly into a destination character span.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">The destination span. Must be at least
    /// <see cref="GetMaxEncodedLength(int)" /> characters in size for safe sizing.</param>
    /// <param name="variant">The Base58 variant.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="destination" /> is too small.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static int Encode(ReadOnlySpan<byte> bytes, Span<char> destination, Base58Variant variant = Base58Variant.BitcoinFlickr)
    {
        string alphabet = GetAlphabet(variant);

        if (bytes.IsEmpty)
            return 0;

        int upperBound = GetMaxEncodedLength(bytes.Length);
        if (destination.Length < upperBound)
            throw new ArgumentException(
                $"Destination must be at least {upperBound} characters to safely encode {bytes.Length} bytes.",
                nameof(destination));

        char[] scratch = new char[upperBound];
        int written = EncodeIntoBuffer(bytes, alphabet, scratch);
        scratch.AsSpan(scratch.Length - written, written).CopyTo(destination);
        return written;
    }

    /// <summary>
    /// Attempts to encode a span of bytes into a destination character span.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <param name="variant">The Base58 variant.</param>
    /// <returns><see langword="true" /> when the destination is large enough; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool TryEncode(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten, Base58Variant variant = Base58Variant.BitcoinFlickr)
    {
        string alphabet = GetAlphabet(variant);

        if (bytes.IsEmpty)
        {
            charsWritten = 0;
            return true;
        }

        int upperBound = GetMaxEncodedLength(bytes.Length);
        char[] scratch = new char[upperBound];
        int written = EncodeIntoBuffer(bytes, alphabet, scratch);

        if (destination.Length < written)
        {
            charsWritten = 0;
            return false;
        }

        scratch.AsSpan(scratch.Length - written, written).CopyTo(destination);
        charsWritten = written;
        return true;
    }

    /// <summary>
    /// Encodes <paramref name="bytes" /> into the trailing portion of <paramref name="buffer" /> and returns the
    /// character count written.
    /// </summary>
    /// <param name="bytes">The input bytes.</param>
    /// <param name="alphabet">The variant alphabet.</param>
    /// <param name="buffer">The scratch buffer; the encoder writes from the end towards the start.</param>
    /// <returns>The number of characters written into the buffer.</returns>
    private static int EncodeIntoBuffer(ReadOnlySpan<byte> bytes, string alphabet, char[] buffer)
    {
        int leadingZeros = 0;
        while (leadingZeros < bytes.Length && bytes[leadingZeros] == 0)
            leadingZeros++;

        BigInteger value;
        if (leadingZeros == bytes.Length)
        {
            value = BigInteger.Zero;
        }
        else
        {
            value = new BigInteger(bytes[leadingZeros..], isUnsigned: true, isBigEndian: true);
        }

        int position = buffer.Length;
        while (value > 0)
        {
            value = BigInteger.DivRem(value, 58, out BigInteger remainder);
            buffer[--position] = alphabet[(int)remainder];
        }

        for (int i = 0; i < leadingZeros; i++)
            buffer[--position] = alphabet[0];

        return buffer.Length - position;
    }
}
