// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85.Encode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class Base85
{
    /// <summary>
    /// Encodes <paramref name="bytes" /> into a Base85 string using the supplied variant.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <returns>A Base85 string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="variant" /> is <see cref="Base85Variant.Z85" /> and the byte count is not a
    /// multiple of four.
    /// </exception>
    public static string Encode(byte[] bytes, Base85Variant variant = Base85Variant.Ascii85)
    {
        ThrowHelper.ThrowIfNull(bytes);
        return Encode(bytes.AsSpan(), variant);
    }

    /// <summary>
    /// Encodes a portion of <paramref name="bytes" /> into a Base85 string.
    /// </summary>
    /// <param name="bytes">The byte array.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="count">The number of bytes to encode.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <returns>A Base85 string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" />, <paramref name="count" />, or <paramref name="variant" /> is out of
    /// range.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the segment defined by <paramref name="offset" /> and <paramref name="count" /> exceeds the
    /// available range of <paramref name="bytes" />, or the Z85 variant receives a byte count not divisible by four.
    /// </exception>
    public static string Encode(byte[] bytes, int offset, int count, Base85Variant variant = Base85Variant.Ascii85)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(bytes, offset, count);
        return Encode(bytes.AsSpan(offset, count), variant);
    }

    /// <summary>
    /// Encodes a span of bytes into a Base85 string.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <returns>A Base85 string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="variant" /> is <see cref="Base85Variant.Z85" /> and the byte count is not a
    /// multiple of four.
    /// </exception>
    public static string Encode(ReadOnlySpan<byte> bytes, Base85Variant variant = Base85Variant.Ascii85)
    {
        EnsureValidVariant(variant);

        if (bytes.IsEmpty)
            return string.Empty;

        if (variant == Base85Variant.Z85 && (bytes.Length & 3) != 0)
            throw new ArgumentException("Z85 input length must be a multiple of four bytes.", nameof(bytes));

        int upperBound = GetMaxEncodedLength(bytes.Length, variant);
        char[] buffer = new char[upperBound];
        int written = EncodeIntoBuffer(bytes, variant, buffer);

        return new string(buffer, 0, written);
    }

    /// <summary>
    /// Encodes a span of bytes directly into a destination character span.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">The destination span. Must be at least
    /// <see cref="GetMaxEncodedLength(int, Base85Variant)" /> characters in size.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is too small or, for Z85, when the byte count is not a multiple
    /// of four.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static int Encode(ReadOnlySpan<byte> bytes, Span<char> destination, Base85Variant variant = Base85Variant.Ascii85)
    {
        EnsureValidVariant(variant);

        if (bytes.IsEmpty)
            return 0;

        if (variant == Base85Variant.Z85 && (bytes.Length & 3) != 0)
            throw new ArgumentException("Z85 input length must be a multiple of four bytes.", nameof(bytes));

        int upperBound = GetMaxEncodedLength(bytes.Length, variant);
        if (destination.Length < upperBound)
            throw new ArgumentException(
                $"Destination must be at least {upperBound} characters to safely encode {bytes.Length} bytes.",
                nameof(destination));

        return EncodeIntoBuffer(bytes, variant, destination);
    }

    /// <summary>
    /// Attempts to encode a span of bytes into a destination character span.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <returns><see langword="true" /> when the destination is large enough; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown for Z85 when the byte count is not a multiple of four.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool TryEncode(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten, Base85Variant variant = Base85Variant.Ascii85)
    {
        EnsureValidVariant(variant);

        if (bytes.IsEmpty)
        {
            charsWritten = 0;
            return true;
        }

        if (variant == Base85Variant.Z85 && (bytes.Length & 3) != 0)
            throw new ArgumentException("Z85 input length must be a multiple of four bytes.", nameof(bytes));

        int upperBound = GetMaxEncodedLength(bytes.Length, variant);
        if (destination.Length < upperBound)
        {
            charsWritten = 0;
            return false;
        }

        charsWritten = EncodeIntoBuffer(bytes, variant, destination);
        return true;
    }

    /// <summary>
    /// Core encoder writing into <paramref name="destination" />.
    /// </summary>
    /// <param name="bytes">The input bytes.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="destination">The destination span.</param>
    /// <returns>The number of characters written.</returns>
    private static int EncodeIntoBuffer(ReadOnlySpan<byte> bytes, Base85Variant variant, Span<char> destination)
    {
        string alphabet = GetAlphabet(variant);
        bool allowShortcut = variant == Base85Variant.Ascii85;

        int position = 0;
        int sourcePos = 0;

        while (sourcePos + 4 <= bytes.Length)
        {
            uint value =
                ((uint)bytes[sourcePos] << 24) |
                ((uint)bytes[sourcePos + 1] << 16) |
                ((uint)bytes[sourcePos + 2] << 8) |
                bytes[sourcePos + 3];

            if (allowShortcut && value == 0)
            {
                destination[position++] = ZeroShortcut;
            }
            else
            {
                Span<char> group = stackalloc char[5];
                for (int i = 4; i >= 0; i--)
                {
                    group[i] = alphabet[(int)(value % 85)];
                    value /= 85;
                }

                group.CopyTo(destination[position..]);
                position += 5;
            }

            sourcePos += 4;
        }

        int remaining = bytes.Length - sourcePos;
        if (remaining > 0)
        {
            uint value = 0;
            for (int i = 0; i < remaining; i++)
            {
                value |= (uint)bytes[sourcePos + i] << ((3 - i) * 8);
            }

            Span<char> group = stackalloc char[5];
            for (int i = 4; i >= 0; i--)
            {
                group[i] = alphabet[(int)(value % 85)];
                value /= 85;
            }

            group[..(remaining + 1)].CopyTo(destination[position..]);
            position += remaining + 1;
        }

        return position;
    }
}
