// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32.Encode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Encoding;

public static partial class Base32
{
    /// <summary>
    /// Encodes the entire byte array into a Base32 string using the supplied variant.
    /// </summary>
    /// <param name="bytes">The byte array to encode.</param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="options">
    /// Formatting options. Only <see cref="BaseFormattingOptions.InsertLineBreaks" /> and
    /// <see cref="BaseFormattingOptions.OmitPadding" /> are honoured; other flags are ignored.
    /// </param>
    /// <returns>A Base32 encoded string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bytes" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="variant" /> is not a defined value.
    /// </exception>
    public static string Encode(byte[] bytes, Base32Variant variant = Base32Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNull(bytes);
        return Encode(bytes.AsSpan(), variant, options);
    }

    /// <summary>
    /// Encodes a read-only span of bytes into a Base32 string using the supplied variant.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>A Base32 encoded string. Returns <see cref="string.Empty" /> when the input is empty.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="variant" /> is not a defined value.
    /// </exception>
    public static string Encode(ReadOnlySpan<byte> bytes, Base32Variant variant = Base32Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        if (bytes.IsEmpty)
            return string.Empty;

        (var alphabet, _) = GetVariantConfig(variant);
        var emitPadding = ShouldEmitPadding(variant, options);
        var emitLineBreaks = options.HasFlag(BaseFormattingOptions.InsertLineBreaks);

        StringBuilder sb = new(GetEncodedLength(bytes.Length, variant, options));
        EncodeCore(bytes, alphabet, emitPadding, emitLineBreaks, sb);

        return sb.ToString();
    }

    /// <summary>
    /// Encodes a read-only span of bytes directly into a pre-allocated character destination using the supplied
    /// variant. Line-break and padding decoration flags are honoured.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">
    /// The destination span. Must be at least
    /// <see cref="GetEncodedLength(int, Base32Variant, BaseFormattingOptions)" /> characters in size.
    /// </param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="destination" /> is too small.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="variant" /> is not a defined value.
    /// </exception>
    public static int Encode(ReadOnlySpan<byte> bytes, Span<char> destination, Base32Variant variant = Base32Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        var required = GetEncodedLength(bytes.Length, variant, options);
        if (destination.Length < required)
            throw new ArgumentException(EncodingResourceStrings.Arg_Invalid_DestinationTooSmallForEncoded, nameof(destination));

        if (bytes.IsEmpty)
            return 0;

        (var alphabet, _) = GetVariantConfig(variant);
        var emitPadding = ShouldEmitPadding(variant, options);
        var emitLineBreaks = options.HasFlag(BaseFormattingOptions.InsertLineBreaks);

        var written = EncodeIntoSpan(bytes, alphabet, emitPadding, emitLineBreaks, destination);
        return written;
    }

    /// <summary>
    /// Encodes a portion of a byte array into a Base32 string using the supplied variant.
    /// </summary>
    /// <param name="bytes">The byte array to encode.</param>
    /// <param name="offset">The zero-based offset in <paramref name="bytes" /> at which to begin encoding.</param>
    /// <param name="count">The number of bytes to encode.</param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>A Base32 encoded string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bytes" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="count" /> is out of range, or when
    /// <paramref name="variant" /> is not a defined value.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the segment defined by <paramref name="offset" /> and <paramref name="count" /> exceeds the
    /// available range of <paramref name="bytes" />.
    /// </exception>
    public static string Encode(byte[] bytes, int offset, int count, Base32Variant variant = Base32Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(bytes, offset, count);
        return Encode(bytes.AsSpan(offset, count), variant, options);
    }

    /// <summary>
    /// Attempts to encode binary bytes into Base32 characters using the provided destination span.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">
    /// When this method returns, contains the number of characters written, or <c>0</c> when the destination is too
    /// small.
    /// </param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>
    /// <see langword="true" /> when the destination is large enough; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="variant" /> is not a defined value.
    /// </exception>
    public static bool TryEncode(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten, Base32Variant variant = Base32Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        var required = GetEncodedLength(bytes.Length, variant, options);
        if (destination.Length < required)
        {
            charsWritten = 0;
            return false;
        }

        if (bytes.IsEmpty)
        {
            charsWritten = 0;
            return true;
        }

        (var alphabet, _) = GetVariantConfig(variant);
        var emitPadding = ShouldEmitPadding(variant, options);
        var emitLineBreaks = options.HasFlag(BaseFormattingOptions.InsertLineBreaks);

        charsWritten = EncodeIntoSpan(bytes, alphabet, emitPadding, emitLineBreaks, destination);
        return true;
    }

    /// <summary>
    /// Appends a single symbol to the string builder, inserting a line break beforehand when the line interval has been
    /// reached.
    /// </summary>
    /// <param name="sb">The destination string builder.</param>
    /// <param name="symbol">The symbol to append.</param>
    /// <param name="emitLineBreaks">Whether line breaking is requested.</param>
    /// <param name="charsThisLine">The running column count, updated by reference.</param>
    private static void AppendSymbol(StringBuilder sb, char symbol, bool emitLineBreaks, ref int charsThisLine)
    {
        if (emitLineBreaks && charsThisLine >= LineBreakInterval)
        {
            sb.Append("\r\n");
            charsThisLine = 0;
        }

        sb.Append(symbol);
        charsThisLine++;
    }

    /// <summary>
    /// Core bit-stream encoder that writes characters into <paramref name="sb" />.
    /// </summary>
    /// <param name="bytes">The input bytes.</param>
    /// <param name="alphabet">The variant alphabet.</param>
    /// <param name="emitPadding">Whether to emit <c>=</c> padding for incomplete final groups.</param>
    /// <param name="emitLineBreaks">
    /// Whether to insert line breaks at <see cref="LineBreakInterval" /> intervals.
    /// </param>
    /// <param name="sb">The destination string builder.</param>
    private static void EncodeCore(ReadOnlySpan<byte> bytes, string alphabet, bool emitPadding, bool emitLineBreaks, StringBuilder sb)
    {
        var accumulator = 0;
        var bitsAccumulated = 0;
        var charsThisLine = 0;
        var charsEmitted = 0;

        foreach (var b in bytes)
        {
            accumulator = (accumulator << 8) | b;
            bitsAccumulated += 8;

            while (bitsAccumulated >= 5)
            {
                bitsAccumulated -= 5;
                var symbolIndex = (accumulator >> bitsAccumulated) & 0x1F;
                AppendSymbol(sb, alphabet[symbolIndex], emitLineBreaks, ref charsThisLine);
                charsEmitted++;
            }
        }

        if (bitsAccumulated > 0)
        {
            var symbolIndex = (accumulator << (5 - bitsAccumulated)) & 0x1F;
            AppendSymbol(sb, alphabet[symbolIndex], emitLineBreaks, ref charsThisLine);
            charsEmitted++;
        }

        if (emitPadding)
        {
            var paddingChars = (8 - (charsEmitted % 8)) % 8;
            for (var i = 0; i < paddingChars; i++)
            {
                AppendSymbol(sb, PaddingChar, emitLineBreaks, ref charsThisLine);
            }
        }
    }

    /// <summary>
    /// Span-based variant of <see cref="EncodeCore" /> that writes directly into <paramref name="destination" />.
    /// </summary>
    /// <param name="bytes">The input bytes.</param>
    /// <param name="alphabet">The variant alphabet.</param>
    /// <param name="emitPadding">Whether to emit padding.</param>
    /// <param name="emitLineBreaks">Whether to insert line breaks.</param>
    /// <param name="destination">The destination span.</param>
    /// <returns>The number of characters written.</returns>
    private static int EncodeIntoSpan(ReadOnlySpan<byte> bytes, string alphabet, bool emitPadding, bool emitLineBreaks, Span<char> destination)
    {
        var accumulator = 0;
        var bitsAccumulated = 0;
        var charsThisLine = 0;
        var charsEmitted = 0;
        var dataChars = 0;

        foreach (var b in bytes)
        {
            accumulator = (accumulator << 8) | b;
            bitsAccumulated += 8;

            while (bitsAccumulated >= 5)
            {
                bitsAccumulated -= 5;
                var symbolIndex = (accumulator >> bitsAccumulated) & 0x1F;
                WriteSymbol(destination, ref charsEmitted, alphabet[symbolIndex], emitLineBreaks, ref charsThisLine);
                dataChars++;
            }
        }

        if (bitsAccumulated > 0)
        {
            var symbolIndex = (accumulator << (5 - bitsAccumulated)) & 0x1F;
            WriteSymbol(destination, ref charsEmitted, alphabet[symbolIndex], emitLineBreaks, ref charsThisLine);
            dataChars++;
        }

        if (emitPadding)
        {
            var paddingChars = (8 - (dataChars % 8)) % 8;
            for (var i = 0; i < paddingChars; i++)
            {
                WriteSymbol(destination, ref charsEmitted, PaddingChar, emitLineBreaks, ref charsThisLine);
            }
        }

        return charsEmitted;
    }

    /// <summary>
    /// Writes a single symbol into the destination span, inserting a line break beforehand when the line interval has
    /// been reached.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <param name="position">The current write position, updated by reference.</param>
    /// <param name="symbol">The symbol to write.</param>
    /// <param name="emitLineBreaks">Whether line breaking is requested.</param>
    /// <param name="charsThisLine">The running column count, updated by reference.</param>
    private static void WriteSymbol(Span<char> destination, ref int position, char symbol, bool emitLineBreaks, ref int charsThisLine)
    {
        if (emitLineBreaks && charsThisLine >= LineBreakInterval)
        {
            destination[position++] = '\r';
            destination[position++] = '\n';
            charsThisLine = 0;
        }

        destination[position++] = symbol;
        charsThisLine++;
    }
}
