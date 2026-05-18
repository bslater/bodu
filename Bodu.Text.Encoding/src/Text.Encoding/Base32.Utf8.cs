// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32.Utf8.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base32
{

    /// <summary>
    /// Decodes a UTF-8 Base32 byte span into a byte span with the <see cref="OperationStatus" /> return convention.
    /// </summary>
    /// <param name="source">The UTF-8 Base32 source.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of source bytes consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <param name="isFinalBlock">Whether <paramref name="source" /> represents the final block of a streamed
    /// input.</param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, Base32Variant variant = Base32Variant.Standard, BaseFormatStyles styles = BaseFormatStyles.None, bool isFinalBlock = true)
    {
        bytesConsumed = 0;
        bytesWritten = 0;

        if (source.IsEmpty)
            return OperationStatus.Done;

        (_, var lookup) = GetVariantConfig(variant);
        return DecodeBitStream<Utf8Source>(default, source, destination, lookup, styles, isFinalBlock, variant, ref bytesConsumed, ref bytesWritten);
    }
    /// <summary>
    /// Encodes <paramref name="source" /> into a UTF-8 Base32 byte array using the supplied variant and the variant's
    /// default padding convention.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="options">Formatting options. Only <see cref="BaseFormattingOptions.OmitPadding" /> has an effect
    /// on the UTF-8 path; line-break and spacing flags are not supported.</param>
    /// <returns>The UTF-8 encoded Base32 bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options" /> contains any flag other than
    /// <see cref="BaseFormattingOptions.OmitPadding" />.
    /// </exception>
    public static byte[] EncodeToUtf8(ReadOnlySpan<byte> source, Base32Variant variant = Base32Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        EnsureUtf8EncodeOptionsSupported(options);

        if (source.IsEmpty)
            return Array.Empty<byte>();

        var required = GetEncodedLength(source.Length, variant, options);
        var result = new byte[required];
        var written = EncodeIntoUtf8Span(source, result, variant, options);
        if (written == result.Length)
            return result;

        var trimmed = new byte[written];
        Buffer.BlockCopy(result, 0, trimmed, 0, written);
        return trimmed;
    }

    /// <summary>
    /// Attempts to encode <paramref name="source" /> as UTF-8 Base32 bytes into <paramref name="destination" />.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination UTF-8 byte span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="variant">The Base32 variant.</param>
    /// <param name="options">Formatting options. Only <see cref="BaseFormattingOptions.OmitPadding" /> is supported.</param>
    /// <returns><see langword="true" /> on success; <see langword="false" /> when the destination is too small.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> contains unsupported flags.</exception>
    public static bool TryEncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, Base32Variant variant = Base32Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        EnsureUtf8EncodeOptionsSupported(options);

        var required = GetEncodedLength(source.Length, variant, options);
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
    /// Generic bit-stream decoder shared by the char-source and UTF-8-source <see cref="OperationStatus" /> paths.
    /// </summary>
    /// <typeparam name="TSource">The source kind selector. Use <see cref="CharSource" /> for <paramref name="charSource" />
    /// or <see cref="Utf8Source" /> for <paramref name="utf8Source" />.</typeparam>
    /// <param name="charSource">The character source (used when <typeparamref name="TSource" /> is
    /// <see cref="CharSource" />).</param>
    /// <param name="utf8Source">The UTF-8 source (used when <typeparamref name="TSource" /> is
    /// <see cref="Utf8Source" />).</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="lookup">The variant lookup table.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <param name="isFinalBlock">Whether the input is the final block.</param>
    /// <param name="variant">The variant (used to determine padding behaviour).</param>
    /// <param name="consumed">Updated with the number of source units consumed.</param>
    /// <param name="written">Updated with the number of bytes written.</param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    private static OperationStatus DecodeBitStream<TSource>(
        ReadOnlySpan<char> charSource,
        ReadOnlySpan<byte> utf8Source,
        Span<byte> destination,
        sbyte[] lookup,
        BaseFormatStyles styles,
        bool isFinalBlock,
        Base32Variant variant,
        ref int consumed,
        ref int written)
        where TSource : struct
    {
        var isUtf8 = typeof(TSource) == typeof(Utf8Source);
        var length = isUtf8 ? utf8Source.Length : charSource.Length;
        var ignoreWhitespace = styles.HasFlag(BaseFormatStyles.IgnoreWhitespace);
        var padIsRequired = !styles.HasFlag(BaseFormatStyles.AllowMissingPadding) && variant is Base32Variant.Standard or Base32Variant.HexExtended;

        var accumulator = 0;
        var bitsAccumulated = 0;
        var symbolsConsumed = 0;
        var paddingSeen = 0;

        // Quantum-boundary checkpoint: every 8 Base32 symbols (40 bits = 5 bytes) align the accumulator back to 0.
        // On DestinationTooSmall / NeedMoreData / InvalidData we roll back to the last such boundary so partial-quantum
        // work is not claimed as consumed. Bytes already written into the destination past checkpointWritten are
        // abandoned and the caller treats them as garbage based on the returned bytesWritten.
        var checkpointConsumed = 0;
        var checkpointWritten = 0;

        for (var i = 0; i < length; i++)
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

            accumulator = (accumulator << 5) | symbolValue;
            bitsAccumulated += 5;
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

        // End of source consumed. Update consumption count.
        consumed = length;

        if (!isFinalBlock)
        {
            // Streaming: if we ended mid-group with leftover bits OR saw a partial padding sequence, defer to next
            // call by indicating more data is needed. Roll back the counters to the last quantum boundary so the
            // caller can re-feed the partial group cleanly.
            if (bitsAccumulated > 0 || (paddingSeen > 0 && paddingSeen < 8))
            {
                consumed = checkpointConsumed;
                written = checkpointWritten;
                return OperationStatus.NeedMoreData;
            }

            return OperationStatus.Done;
        }

        // Final block — validate alignment and padding.
        if (padIsRequired)
        {
            var totalSymbols = symbolsConsumed + paddingSeen;
            if ((totalSymbols % 8) != 0)
            {
                consumed = checkpointConsumed;
                written = checkpointWritten;
                return OperationStatus.InvalidData;
            }

            var expectedPadding = (8 - (symbolsConsumed % 8)) % 8;
            if (paddingSeen != expectedPadding)
            {
                consumed = checkpointConsumed;
                written = checkpointWritten;
                return OperationStatus.InvalidData;
            }
        }

        // RFC 4648 §6 — terminal quantum data character count must be 2, 4, 5, 7, or 8. Crockford and Z-Base32 do
        // not impose this rule, so the check is gated by variant.
        var strictQuantum = variant is Base32Variant.Standard or Base32Variant.HexExtended;
        var dataMod = symbolsConsumed % 8;
        if (strictQuantum && dataMod is 1 or 3 or 6)
        {
            consumed = checkpointConsumed;
            written = checkpointWritten;
            return OperationStatus.InvalidData;
        }

        // Canonical-encoding check: the final partial group's unused low bits must be zero. Without this check a
        // decoder accepts multiple distinct inputs that round-trip to the same byte sequence (RFC 4648 §3.5).
        if (styles.HasFlag(BaseFormatStyles.RequireCanonicalEncoding) && bitsAccumulated > 0)
        {
            var leftoverMask = (1 << bitsAccumulated) - 1;
            if ((accumulator & leftoverMask) != 0)
            {
                consumed = checkpointConsumed;
                written = checkpointWritten;
                return OperationStatus.InvalidData;
            }
        }

        return OperationStatus.Done;
    }

    /// <summary>
    /// Streaming Base32 decode from a character span using <see cref="OperationStatus" /> semantics.
    /// </summary>
    /// <param name="source">The character source.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsConsumed">When this method returns, contains the number of characters consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <param name="isFinalBlock">Whether <paramref name="source" /> represents the final block.</param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    private static OperationStatus DecodeWithStatus(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten, Base32Variant variant, BaseFormatStyles styles, bool isFinalBlock)
    {
        charsConsumed = 0;
        bytesWritten = 0;

        if (source.IsEmpty)
            return OperationStatus.Done;

        (_, var lookup) = GetVariantConfig(variant);
        return DecodeBitStream<CharSource>(source, default, destination, lookup, styles, isFinalBlock, variant, ref charsConsumed, ref bytesWritten);
    }

    /// <summary>
    /// Encodes <paramref name="source" /> into <paramref name="destination" /> using the variant alphabet, returning
    /// the count of UTF-8 bytes written.
    /// </summary>
    /// <param name="source">The input bytes.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="options">The encode options (only <see cref="BaseFormattingOptions.OmitPadding" /> is honoured).</param>
    /// <returns>The number of UTF-8 bytes written.</returns>
    private static int EncodeIntoUtf8Span(ReadOnlySpan<byte> source, Span<byte> destination, Base32Variant variant, BaseFormattingOptions options)
    {
        (var alphabet, _) = GetVariantConfig(variant);
        var emitPadding = ShouldEmitPadding(variant, options);

        var position = 0;
        var dataChars = 0;
        var accumulator = 0;
        var bitsAccumulated = 0;

        foreach (var b in source)
        {
            accumulator = (accumulator << 8) | b;
            bitsAccumulated += 8;

            while (bitsAccumulated >= 5)
            {
                bitsAccumulated -= 5;
                var symbolIndex = (accumulator >> bitsAccumulated) & 0x1F;
                destination[position++] = (byte)alphabet[symbolIndex];
                dataChars++;
            }
        }

        if (bitsAccumulated > 0)
        {
            var symbolIndex = (accumulator << (5 - bitsAccumulated)) & 0x1F;
            destination[position++] = (byte)alphabet[symbolIndex];
            dataChars++;
        }

        if (emitPadding)
        {
            var paddingChars = (8 - (dataChars % 8)) % 8;
            for (var i = 0; i < paddingChars; i++)
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
                EncodingResourceStrings.Arg_Invalid_Base32UnsupportedEncodeOptions,
                nameof(options));
        }
    }

    /// <summary>
    /// Marker struct used to select the character-source code path in <see cref="DecodeBitStream{TSource}" />.
    /// </summary>
    private readonly struct CharSource { }

    /// <summary>
    /// Marker struct used to select the UTF-8-source code path in <see cref="DecodeBitStream{TSource}" />.
    /// </summary>
    private readonly struct Utf8Source { }
}
