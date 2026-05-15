// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85.Encode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class Base85
{

    /// <summary>
    /// Encodes <paramref name="bytes" /> into a Base85 string using the supplied variant and options.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <param name="options">Formatting options. When the variant is <see cref="Base85Variant.Ascii85" /> and
    /// <see cref="BaseFormattingOptions.IncludePrefix" /> is set, the output is wrapped in the Adobe Ascii85
    /// <c>&lt;~</c> and <c>~&gt;</c> delimiters.</param>
    /// <returns>A Base85 string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="variant" /> is <see cref="Base85Variant.Z85" /> and the byte count is not a
    /// multiple of four.
    /// </exception>
    public static string Encode(byte[] bytes, Base85Variant variant = Base85Variant.Ascii85, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNull(bytes);
        return Encode(bytes.AsSpan(), variant, options);
    }

    /// <summary>
    /// Encodes a span of bytes into a Base85 string.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <param name="options">Formatting options. When the variant is <see cref="Base85Variant.Ascii85" /> and
    /// <see cref="BaseFormattingOptions.IncludePrefix" /> is set, the output is wrapped in the Adobe Ascii85
    /// <c>&lt;~</c> and <c>~&gt;</c> delimiters. For <see cref="Base85Variant.Z85" /> the flag is ignored — Z85
    /// has no standard delimiter convention.</param>
    /// <returns>A Base85 string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="variant" /> is <see cref="Base85Variant.Z85" /> and the byte count is not a
    /// multiple of four.
    /// </exception>
    public static string Encode(ReadOnlySpan<byte> bytes, Base85Variant variant = Base85Variant.Ascii85, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        EnsureValidVariant(variant);

        bool emitDelimiters = ShouldEmitAscii85Delimiters(variant, options);

        if (bytes.IsEmpty)
            return emitDelimiters ? Ascii85DelimiterStart + Ascii85DelimiterEnd : string.Empty;

        if (variant == Base85Variant.Z85 && (bytes.Length & 3) != 0)
            throw new ArgumentException("Z85 input length must be a multiple of four bytes.", nameof(bytes));

        int upperBound = GetMaxEncodedLength(bytes.Length, variant);
        if (emitDelimiters)
            upperBound += Ascii85DelimiterLength;

        char[] buffer = new char[upperBound];
        int written = 0;

        if (emitDelimiters)
        {
            buffer[written++] = '<';
            buffer[written++] = '~';
        }

        written += EncodeIntoBuffer(bytes, variant, buffer.AsSpan(written));

        if (emitDelimiters)
        {
            buffer[written++] = '~';
            buffer[written++] = '>';
        }

        return new string(buffer, 0, written);
    }

    /// <summary>
    /// Encodes a span of bytes directly into a destination character span.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">The destination span. Must be at least
    /// <see cref="GetMaxEncodedLength(int, Base85Variant, BaseFormattingOptions)" /> characters in size.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <param name="options">Formatting options. See <see cref="Encode(ReadOnlySpan{byte}, Base85Variant, BaseFormattingOptions)" />.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is too small or, for Z85, when the byte count is not a multiple
    /// of four.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static int Encode(ReadOnlySpan<byte> bytes, Span<char> destination, Base85Variant variant = Base85Variant.Ascii85, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        EnsureValidVariant(variant);

        bool emitDelimiters = ShouldEmitAscii85Delimiters(variant, options);

        if (bytes.IsEmpty)
        {
            if (!emitDelimiters)
                return 0;

            if (destination.Length < Ascii85DelimiterLength)
                throw new ArgumentException(
                    "Destination must be at least four characters to fit the Ascii85 delimiter pair.",
                    nameof(destination));

            destination[0] = '<';
            destination[1] = '~';
            destination[2] = '~';
            destination[3] = '>';
            return Ascii85DelimiterLength;
        }

        if (variant == Base85Variant.Z85 && (bytes.Length & 3) != 0)
            throw new ArgumentException("Z85 input length must be a multiple of four bytes.", nameof(bytes));

        int upperBound = GetMaxEncodedLength(bytes.Length, variant);
        int totalUpper = emitDelimiters ? upperBound + Ascii85DelimiterLength : upperBound;

        // If destination already meets the worst-case bound we can encode directly into it. Otherwise we encode
        // into a rented scratch buffer and copy only when the ACTUAL written length fits — so callers that pre-size
        // destination for the actual output (which may be shorter when the Ascii85 'z' shortcut is used) succeed.
        if (destination.Length >= totalUpper)
        {
            int pos = 0;
            if (emitDelimiters)
            {
                destination[pos++] = '<';
                destination[pos++] = '~';
            }

            pos += EncodeIntoBuffer(bytes, variant, destination[pos..]);

            if (emitDelimiters)
            {
                destination[pos++] = '~';
                destination[pos++] = '>';
            }

            return pos;
        }

        char[] scratch = System.Buffers.ArrayPool<char>.Shared.Rent(upperBound);
        try
        {
            int written = EncodeIntoBuffer(bytes, variant, scratch);
            int required = emitDelimiters ? written + Ascii85DelimiterLength : written;
            if (destination.Length < required)
                throw new ArgumentException(
                    $"Destination must be at least {required} characters to encode the provided bytes.",
                    nameof(destination));

            int pos = 0;
            if (emitDelimiters)
            {
                destination[pos++] = '<';
                destination[pos++] = '~';
            }

            scratch.AsSpan(0, written).CopyTo(destination[pos..]);
            pos += written;

            if (emitDelimiters)
            {
                destination[pos++] = '~';
                destination[pos++] = '>';
            }

            return pos;
        }
        finally
        {
            System.Buffers.ArrayPool<char>.Shared.Return(scratch);
        }
    }

    /// <summary>
    /// Encodes a portion of <paramref name="bytes" /> into a Base85 string.
    /// </summary>
    /// <param name="bytes">The byte array.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="count">The number of bytes to encode.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <param name="options">Formatting options. See <see cref="Encode(ReadOnlySpan{byte}, Base85Variant, BaseFormattingOptions)" />.</param>
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
    public static string Encode(byte[] bytes, int offset, int count, Base85Variant variant = Base85Variant.Ascii85, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(bytes, offset, count);
        return Encode(bytes.AsSpan(offset, count), variant, options);
    }

    /// <summary>
    /// Attempts to encode a span of bytes into a destination character span.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <param name="options">Formatting options. See <see cref="Encode(ReadOnlySpan{byte}, Base85Variant, BaseFormattingOptions)" />.</param>
    /// <returns><see langword="true" /> when the destination is large enough; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown for Z85 when the byte count is not a multiple of four.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool TryEncode(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten, Base85Variant variant = Base85Variant.Ascii85, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        EnsureValidVariant(variant);

        bool emitDelimiters = ShouldEmitAscii85Delimiters(variant, options);

        if (bytes.IsEmpty)
        {
            if (!emitDelimiters)
            {
                charsWritten = 0;
                return true;
            }

            if (destination.Length < Ascii85DelimiterLength)
            {
                charsWritten = 0;
                return false;
            }

            destination[0] = '<';
            destination[1] = '~';
            destination[2] = '~';
            destination[3] = '>';
            charsWritten = Ascii85DelimiterLength;
            return true;
        }

        if (variant == Base85Variant.Z85 && (bytes.Length & 3) != 0)
            throw new ArgumentException("Z85 input length must be a multiple of four bytes.", nameof(bytes));

        int upperBound = GetMaxEncodedLength(bytes.Length, variant);
        int totalUpper = emitDelimiters ? upperBound + Ascii85DelimiterLength : upperBound;

        // Fast path: destination meets the worst-case bound — encode in place.
        if (destination.Length >= totalUpper)
        {
            int pos = 0;
            if (emitDelimiters)
            {
                destination[pos++] = '<';
                destination[pos++] = '~';
            }

            pos += EncodeIntoBuffer(bytes, variant, destination[pos..]);

            if (emitDelimiters)
            {
                destination[pos++] = '~';
                destination[pos++] = '>';
            }

            charsWritten = pos;
            return true;
        }

        // Slow path: encode into a rented scratch then copy if the ACTUAL output (which may be shorter when the
        // Ascii85 'z' shortcut is used) fits the caller's destination.
        char[] scratch = System.Buffers.ArrayPool<char>.Shared.Rent(upperBound);
        try
        {
            int written = EncodeIntoBuffer(bytes, variant, scratch);
            int required = emitDelimiters ? written + Ascii85DelimiterLength : written;
            if (destination.Length < required)
            {
                charsWritten = 0;
                return false;
            }

            int pos = 0;
            if (emitDelimiters)
            {
                destination[pos++] = '<';
                destination[pos++] = '~';
            }

            scratch.AsSpan(0, written).CopyTo(destination[pos..]);
            pos += written;

            if (emitDelimiters)
            {
                destination[pos++] = '~';
                destination[pos++] = '>';
            }

            charsWritten = pos;
            return true;
        }
        finally
        {
            System.Buffers.ArrayPool<char>.Shared.Return(scratch);
        }
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
