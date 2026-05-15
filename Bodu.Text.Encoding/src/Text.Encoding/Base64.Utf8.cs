// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64.Utf8.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base64
{
    /// <summary>
    /// The Standard variant alphabet, used for UTF-8 encode paths.
    /// </summary>
    private const string StandardAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    /// <summary>
    /// The URL- and filename-safe variant alphabet.
    /// </summary>
    private const string UrlSafeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    private static readonly sbyte[] s_standardLookup = BuildLookup(StandardAlphabet);
    private static readonly sbyte[] s_urlSafeLookup = BuildLookup(UrlSafeAlphabet);

    /// <summary>
    /// Encodes <paramref name="source" /> into a UTF-8 Base64 byte array.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="options">Formatting options. Only <see cref="BaseFormattingOptions.OmitPadding" /> has an effect
    /// on the UTF-8 fast path.</param>
    /// <returns>The UTF-8 encoded Base64 bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options" /> contains any flag other than
    /// <see cref="BaseFormattingOptions.OmitPadding" />.
    /// </exception>
    public static byte[] EncodeToUtf8(ReadOnlySpan<byte> source, Base64Variant variant = Base64Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        EnsureValidVariant(variant);
        EnsureUtf8EncodeOptionsSupported(options);

        if (source.IsEmpty)
            return Array.Empty<byte>();

        int required = GetEncodedLength(source.Length, variant, options);
        byte[] result = new byte[required];
        int written = EncodeIntoUtf8Span(source, result, variant, options);
        if (written == result.Length)
            return result;

        byte[] trimmed = new byte[written];
        Buffer.BlockCopy(result, 0, trimmed, 0, written);
        return trimmed;
    }

    /// <summary>
    /// Attempts to encode <paramref name="source" /> as UTF-8 Base64 bytes into <paramref name="destination" />.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination UTF-8 byte span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="options">Formatting options. Only <see cref="BaseFormattingOptions.OmitPadding" /> is supported.</param>
    /// <returns><see langword="true" /> on success; <see langword="false" /> when the destination is too small.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="ArgumentException">Thrown when unsupported flags are set in <paramref name="options" />.</exception>
    public static bool TryEncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, Base64Variant variant = Base64Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        EnsureValidVariant(variant);
        EnsureUtf8EncodeOptionsSupported(options);

        int required = GetEncodedLength(source.Length, variant, options);
        if (destination.Length < required)
        {
            bytesWritten = 0;
            return false;
        }

        if (source.IsEmpty)
        {
            bytesWritten = 0;
            return true;
        }

        bytesWritten = EncodeIntoUtf8Span(source, destination, variant, options);
        return true;
    }

    /// <summary>
    /// Decodes UTF-8 Base64 bytes into a byte span using the <see cref="OperationStatus" /> return convention.
    /// </summary>
    /// <param name="source">The UTF-8 Base64 source.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of source bytes consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <param name="isFinalBlock">Whether <paramref name="source" /> represents the final block.</param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, Base64Variant variant = Base64Variant.Standard, BaseFormatStyles styles = BaseFormatStyles.None, bool isFinalBlock = true)
    {
        EnsureValidVariant(variant);
        bytesConsumed = 0;
        bytesWritten = 0;

        if (source.IsEmpty)
            return OperationStatus.Done;

        sbyte[] lookup = GetLookup(variant);
        return DecodeBitStream<Utf8Source>(default, source, destination, lookup, styles, isFinalBlock, variant, ref bytesConsumed, ref bytesWritten);
    }

    /// <summary>
    /// Decodes Base64 characters into a byte span using the <see cref="OperationStatus" /> return convention.
    /// </summary>
    /// <param name="source">The character source.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsConsumed">When this method returns, contains the number of characters consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <param name="isFinalBlock">Whether <paramref name="source" /> represents the final block.</param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    private static OperationStatus DecodeWithStatus(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten, Base64Variant variant, BaseFormatStyles styles, bool isFinalBlock)
    {
        EnsureValidVariant(variant);
        charsConsumed = 0;
        bytesWritten = 0;

        if (source.IsEmpty)
            return OperationStatus.Done;

        sbyte[] lookup = GetLookup(variant);
        return DecodeBitStream<CharSource>(source, default, destination, lookup, styles, isFinalBlock, variant, ref charsConsumed, ref bytesWritten);
    }

    /// <summary>
    /// Encodes <paramref name="source" /> into <paramref name="destination" /> as UTF-8 Base64 bytes.
    /// </summary>
    /// <param name="source">The input bytes.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="options">The encode options (only <see cref="BaseFormattingOptions.OmitPadding" /> is honoured).</param>
    /// <returns>The number of UTF-8 bytes written.</returns>
    private static int EncodeIntoUtf8Span(ReadOnlySpan<byte> source, Span<byte> destination, Base64Variant variant, BaseFormattingOptions options)
    {
        string alphabet = variant == Base64Variant.UrlSafe ? UrlSafeAlphabet : StandardAlphabet;
        bool emitPadding = ShouldEmitPadding(variant, options);

        int position = 0;
        int dataChars = 0;
        int accumulator = 0;
        int bitsAccumulated = 0;

        foreach (byte b in source)
        {
            accumulator = (accumulator << 8) | b;
            bitsAccumulated += 8;

            while (bitsAccumulated >= 6)
            {
                bitsAccumulated -= 6;
                int symbolIndex = (accumulator >> bitsAccumulated) & 0x3F;
                destination[position++] = (byte)alphabet[symbolIndex];
                dataChars++;
            }
        }

        if (bitsAccumulated > 0)
        {
            int symbolIndex = (accumulator << (6 - bitsAccumulated)) & 0x3F;
            destination[position++] = (byte)alphabet[symbolIndex];
            dataChars++;
        }

        if (emitPadding)
        {
            int paddingChars = (4 - (dataChars % 4)) % 4;
            for (int i = 0; i < paddingChars; i++)
                destination[position++] = (byte)PaddingChar;
        }

        return position;
    }

    /// <summary>
    /// Validates that <paramref name="options" /> contains no flags incompatible with the UTF-8 fast-path encoder.
    /// </summary>
    /// <param name="options">The encode options.</param>
    /// <exception cref="ArgumentException">Thrown when an unsupported flag is set.</exception>
    private static void EnsureUtf8EncodeOptionsSupported(BaseFormattingOptions options)
    {
        if ((options & ~BaseFormattingOptions.OmitPadding) != 0)
        {
            throw new ArgumentException(
                "Only OmitPadding is supported on the Base64 UTF-8 encoder. Use the string-returning Encode overloads to apply other flags.",
                nameof(options));
        }
    }

    /// <summary>
    /// Returns the lookup table for the supplied variant.
    /// </summary>
    /// <param name="variant">The variant.</param>
    /// <returns>The lookup table.</returns>
    private static sbyte[] GetLookup(Base64Variant variant) =>
        variant == Base64Variant.UrlSafe ? s_urlSafeLookup : s_standardLookup;

    /// <summary>
    /// Builds a lookup table for the supplied alphabet, plus UrlSafe aliases for the standard alphabet (and vice
    /// versa) so the decoder tolerates either form when the user is lenient.
    /// </summary>
    /// <param name="alphabet">The alphabet.</param>
    /// <returns>A 128-entry lookup table.</returns>
    private static sbyte[] BuildLookup(string alphabet)
    {
        sbyte[] table = new sbyte[128];
        Array.Fill(table, (sbyte)-1);

        for (int i = 0; i < alphabet.Length; i++)
        {
            char c = alphabet[i];
            table[c] = (sbyte)i;
        }

        return table;
    }

    /// <summary>
    /// Generic bit-stream decoder shared by the char-source and UTF-8-source <see cref="OperationStatus" /> paths.
    /// </summary>
    /// <typeparam name="TSource">Marker type to select source kind.</typeparam>
    /// <param name="charSource">Character source.</param>
    /// <param name="utf8Source">UTF-8 source.</param>
    /// <param name="destination">Destination byte span.</param>
    /// <param name="lookup">Variant lookup table.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <param name="isFinalBlock">Whether this is the final block.</param>
    /// <param name="variant">The variant (used for padding policy).</param>
    /// <param name="consumed">Updated with consumed count.</param>
    /// <param name="written">Updated with written count.</param>
    /// <returns>An <see cref="OperationStatus" />.</returns>
    private static OperationStatus DecodeBitStream<TSource>(
        ReadOnlySpan<char> charSource,
        ReadOnlySpan<byte> utf8Source,
        Span<byte> destination,
        sbyte[] lookup,
        BaseFormatStyles styles,
        bool isFinalBlock,
        Base64Variant variant,
        ref int consumed,
        ref int written)
        where TSource : struct
    {
        bool isUtf8 = typeof(TSource) == typeof(Utf8Source);
        int length = isUtf8 ? utf8Source.Length : charSource.Length;
        bool ignoreWhitespace = styles.HasFlag(BaseFormatStyles.IgnoreWhitespace) || variant == Base64Variant.Mime;
        bool padIsRequired = !styles.HasFlag(BaseFormatStyles.AllowMissingPadding) && variant != Base64Variant.UrlSafe;

        int accumulator = 0;
        int bitsAccumulated = 0;
        int symbolsConsumed = 0;
        int paddingSeen = 0;

        // Quantum-boundary checkpoint: every 4 Base64 symbols (24 bits = 3 bytes) align the accumulator back to 0.
        // On DestinationTooSmall / NeedMoreData / InvalidData we roll back to the last such boundary so partial-quantum
        // work is not claimed as consumed.
        int checkpointConsumed = 0;
        int checkpointWritten = 0;

        for (int i = 0; i < length; i++)
        {
            int rawValue = isUtf8 ? utf8Source[i] : charSource[i];

            if (ignoreWhitespace && rawValue is ' ' or '\t' or '\r' or '\n')
                continue;

            if (rawValue == PaddingChar)
            {
                paddingSeen++;
                continue;
            }

            if (paddingSeen > 0)
            {
                consumed = checkpointConsumed;
                written = checkpointWritten;
                return OperationStatus.InvalidData;
            }

            if ((uint)rawValue >= (uint)lookup.Length)
            {
                consumed = checkpointConsumed;
                written = checkpointWritten;
                return OperationStatus.InvalidData;
            }

            int symbolValue = lookup[rawValue];
            if (symbolValue < 0)
            {
                consumed = checkpointConsumed;
                written = checkpointWritten;
                return OperationStatus.InvalidData;
            }

            accumulator = (accumulator << 6) | symbolValue;
            bitsAccumulated += 6;
            symbolsConsumed++;

            if (bitsAccumulated >= 8)
            {
                bitsAccumulated -= 8;
                if (written >= destination.Length)
                {
                    consumed = checkpointConsumed;
                    written = checkpointWritten;
                    return OperationStatus.DestinationTooSmall;
                }

                destination[written++] = (byte)((accumulator >> bitsAccumulated) & 0xFF);
            }

            if (bitsAccumulated == 0)
            {
                checkpointConsumed = i + 1;
                checkpointWritten = written;
            }
        }

        consumed = length;

        if (!isFinalBlock)
        {
            if (bitsAccumulated > 0 || (paddingSeen > 0 && paddingSeen < 4))
            {
                consumed = checkpointConsumed;
                written = checkpointWritten;
                return OperationStatus.NeedMoreData;
            }

            return OperationStatus.Done;
        }

        if (padIsRequired)
        {
            int totalSymbols = symbolsConsumed + paddingSeen;
            if ((totalSymbols % 4) != 0)
            {
                consumed = checkpointConsumed;
                written = checkpointWritten;
                return OperationStatus.InvalidData;
            }

            int expectedPadding = (4 - (symbolsConsumed % 4)) % 4;
            if (paddingSeen != expectedPadding)
            {
                consumed = checkpointConsumed;
                written = checkpointWritten;
                return OperationStatus.InvalidData;
            }
        }

        // Canonical-encoding check: the final partial group's unused low bits must be zero. Without this check a
        // decoder accepts multiple distinct inputs that round-trip to the same byte sequence (RFC 4648 §3.5).
        if (styles.HasFlag(BaseFormatStyles.RequireCanonicalEncoding) && bitsAccumulated > 0)
        {
            int leftoverMask = (1 << bitsAccumulated) - 1;
            if ((accumulator & leftoverMask) != 0)
            {
                consumed = checkpointConsumed;
                written = checkpointWritten;
                return OperationStatus.InvalidData;
            }
        }

        return OperationStatus.Done;
    }

    /// <summary>Marker struct used to select the character-source code path.</summary>
    private readonly struct CharSource { }

    /// <summary>Marker struct used to select the UTF-8-source code path.</summary>
    private readonly struct Utf8Source { }
}
