// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base45.Encode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class Base45
{
    /// <summary>
    /// Encodes <paramref name="bytes" /> into a Base45 string.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A Base45 string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes" /> is <see langword="null" />.</exception>
    public static string Encode(byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(bytes);
        return Encode(bytes.AsSpan());
    }

    /// <summary>
    /// Encodes a span of bytes into a Base45 string.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A Base45 string. Returns <see cref="string.Empty" /> for empty input.</returns>
    public static string Encode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
            return string.Empty;

        var length = GetEncodedLength(bytes.Length);
        var buffer = System.Buffers.ArrayPool<char>.Shared.Rent(length);
        try
        {
            EncodeInto(bytes, buffer.AsSpan(0, length));
            return new string(buffer, 0, length);
        }
        finally
        {
            System.Buffers.ArrayPool<char>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Encodes a portion of <paramref name="bytes" /> into a Base45 string.
    /// </summary>
    /// <param name="bytes">The byte array to encode.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="count">The number of bytes to encode.</param>
    /// <returns>A Base45 string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="offset" /> or <paramref name="count" /> is out of range.</exception>
    /// <exception cref="ArgumentException">Thrown when the segment defined by <paramref name="offset" /> and <paramref name="count" /> exceeds the available range of <paramref name="bytes" />.</exception>
    public static string Encode(byte[] bytes, int offset, int count)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(bytes, offset, count);
        return Encode(bytes.AsSpan(offset, count));
    }

    /// <summary>
    /// Encodes a span of bytes directly into a destination character span.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">
    /// The destination span; must be at least <see cref="GetEncodedLength(int)" /> characters.
    /// </param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="destination" /> is too small.</exception>
    public static int Encode(ReadOnlySpan<byte> bytes, Span<char> destination)
    {
        if (bytes.IsEmpty)
            return 0;

        var length = GetEncodedLength(bytes.Length);
        if (destination.Length < length)
            throw new ArgumentException(
                string.Format(System.Globalization.CultureInfo.CurrentCulture, EncodingResourceStrings.Arg_Invalid_Base45DestinationSize, length),
                nameof(destination));

        EncodeInto(bytes, destination.Slice(0, length));
        return length;
    }

    /// <summary>
    /// Attempts to encode a span of bytes into a destination character span without throwing.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <returns>
    /// <see langword="true" /> when the destination is large enough; otherwise <see langword="false" />.
    /// </returns>
    public static bool TryEncode(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten)
    {
        if (bytes.IsEmpty)
        {
            charsWritten = 0;
            return true;
        }

        var length = GetEncodedLength(bytes.Length);
        if (destination.Length < length)
        {
            charsWritten = 0;
            return false;
        }

        EncodeInto(bytes, destination.Slice(0, length));
        charsWritten = length;
        return true;
    }

    /// <summary>
    /// Encodes <paramref name="bytes" /> into <paramref name="destination" />, which must be exactly
    /// <see cref="GetEncodedLength(int)" /> characters in length.
    /// </summary>
    /// <param name="bytes">The input bytes.</param>
    /// <param name="destination">The destination span sized to the exact encoded length.</param>
    private static void EncodeInto(ReadOnlySpan<byte> bytes, Span<char> destination)
    {
        var position = 0;
        var i = 0;

        // Encode whole byte pairs as three least-significant-first base-45 digits.
        for (; i + 1 < bytes.Length; i += 2)
        {
            var n = (bytes[i] << 8) | bytes[i + 1];
            destination[position++] = Alphabet[n % Radix];
            n /= Radix;
            destination[position++] = Alphabet[n % Radix];
            destination[position++] = Alphabet[n / Radix];
        }

        // A trailing odd byte encodes as two base-45 digits.
        if (i < bytes.Length)
        {
            var n = bytes[i];
            destination[position++] = Alphabet[n % Radix];
            destination[position] = Alphabet[n / Radix];
        }
    }
}
