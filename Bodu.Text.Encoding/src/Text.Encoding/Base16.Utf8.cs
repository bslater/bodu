// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base16
{
    /// <summary>
    /// Streaming decode of a UTF-8 hexadecimal byte span into a byte span, with leniency styles and a streaming
    /// indicator.
    /// </summary>
    /// <param name="source">The UTF-8 hexadecimal source bytes.</param>
    /// <param name="destination">The destination span receiving decoded bytes.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of source bytes consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of destination bytes written.</param>
    /// <param name="styles">Parsing styles influencing leniency.</param>
    /// <param name="isFinalBlock">
    /// When <see langword="true" />, the decoder enforces that the input forms complete byte pairs and any trailing
    /// partial pair is reported as <see cref="OperationStatus.InvalidData" />. When <see langword="false" />, a
    /// trailing single character is reported as <see cref="OperationStatus.NeedMoreData" /> so the caller can resume on
    /// more input.
    /// </param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, BaseFormatStyles styles = BaseFormatStyles.None, bool isFinalBlock = true)
    {
        if (source.IsEmpty)
        {
            bytesConsumed = 0;
            bytesWritten = 0;
            return OperationStatus.Done;
        }

        if (styles == BaseFormatStyles.None)
            return DecodeUtf8WithStatus(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);

        // Lenient path: project UTF-8 bytes through a transient char buffer that omits decorations. Track a parallel
        // map from kept-buffer index to original source position so partial-progress outcomes can report the correct
        // source-byte boundary.
        Span<char> scratch = source.Length <= StackallocThreshold
            ? stackalloc char[source.Length]
            : new char[source.Length];

        Span<int> keptToSource = source.Length <= StackallocThreshold
            ? stackalloc int[source.Length]
            : new int[source.Length];

        int kept = 0;
        bool ignoreWhitespace = styles.HasFlag(BaseFormatStyles.IgnoreWhitespace);
        int start = 0;

        if (styles.HasFlag(BaseFormatStyles.AllowPrefix) &&
            source.Length >= 2 &&
            source[0] == (byte)'0' &&
            (source[1] == (byte)'x' || source[1] == (byte)'X'))
        {
            start = 2;
        }

        for (int i = start; i < source.Length; i++)
        {
            byte b = source[i];
            if (ignoreWhitespace && b is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
                continue;

            keptToSource[kept] = i;
            scratch[kept++] = (char)b;
        }

        OperationStatus status = DecodeStrictWithStatus(scratch.Slice(0, kept), destination, out int charsConsumed, out bytesWritten, isFinalBlock);

        bytesConsumed = status switch
        {
            OperationStatus.Done => source.Length,
            OperationStatus.DestinationTooSmall => charsConsumed < kept ? keptToSource[charsConsumed] : source.Length,
            OperationStatus.NeedMoreData => charsConsumed < kept ? keptToSource[charsConsumed] : source.Length,
            _ => 0,
        };

        return status;
    }

    /// <summary>
    /// Encodes <paramref name="source" /> into a UTF-8 hexadecimal byte array using lower case characters.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>A UTF-8 encoded hexadecimal byte array. Each input byte produces two output bytes.</returns>
    /// <remarks>
    /// Hexadecimal characters are ASCII, so each UTF-8 byte equals the ASCII code of its corresponding character.
    /// </remarks>
    public static byte[] EncodeToUtf8(ReadOnlySpan<byte> source)
    {
        if (source.IsEmpty)
            return [];

        byte[] result = new byte[source.Length * 2];
        EncodeToUtf8Core(source, result, upperCase: false);
        return result;
    }

    /// <summary>
    /// Attempts to encode <paramref name="source" /> into <paramref name="destination" /> as UTF-8 hexadecimal bytes.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination UTF-8 byte span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of UTF-8 bytes written.</param>
    /// <param name="options">
    /// Formatting options. Only <see cref="BaseFormattingOptions.UpperCase" /> is honoured; line-break / spacing /
    /// prefix flags are not supported on the UTF-8 fast path.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="destination" /> is large enough; otherwise <see langword="false" />
    /// .
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options" /> contains any flag other than
    /// <see cref="BaseFormattingOptions.UpperCase" />.
    /// </exception>
    public static bool TryEncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        EnsureSpanOutputOptionsSupported(options);

        int required = source.Length * 2;
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

        EncodeToUtf8Core(source, destination, options.HasFlag(BaseFormattingOptions.UpperCase));
        bytesWritten = required;
        return true;
    }

    /// <summary>
    /// Encodes <paramref name="source" /> into a UTF-8 hexadecimal byte array using the supplied variant.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="variant">The Base16 alphabet case.</param>
    /// <returns>A UTF-8 encoded hexadecimal byte array. Each input byte produces two output bytes.</returns>
    /// <remarks>
    /// This is the variant-based form of <see cref="EncodeToUtf8(ReadOnlySpan{byte})" />. Hexadecimal characters are
    /// ASCII, so each UTF-8 byte equals the ASCII code of its corresponding character.
    /// </remarks>
    public static byte[] EncodeToUtf8(ReadOnlySpan<byte> source, Base16Variant variant)
    {
        if (source.IsEmpty)
            return [];

        byte[] result = new byte[source.Length * 2];
        EncodeToUtf8Core(source, result, upperCase: variant == Base16Variant.Upper);
        return result;
    }

    /// <summary>
    /// Attempts to encode <paramref name="source" /> into <paramref name="destination" /> as UTF-8 hexadecimal bytes
    /// using the supplied variant.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination UTF-8 byte span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of UTF-8 bytes written.</param>
    /// <param name="variant">The Base16 alphabet case.</param>
    /// <param name="options">
    /// Additional formatting options. Only <see cref="BaseFormattingOptions.UpperCase" /> is honoured;
    /// <paramref name="variant" /> takes precedence over it for the alphabet case.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="destination" /> is large enough; otherwise <see langword="false" />
    /// .
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options" /> contains any flag other than
    /// <see cref="BaseFormattingOptions.UpperCase" />.
    /// </exception>
    public static bool TryEncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, Base16Variant variant, BaseFormattingOptions options = BaseFormattingOptions.None) =>
        TryEncodeToUtf8(source, destination, out bytesWritten, ApplyVariant(variant, options));

    /// <summary>
    /// Decodes consecutive pairs of UTF-8 hex bytes into the destination span. Used by the strict-mode
    /// <c>FromHexString(ReadOnlySpan{byte})</c> overload.
    /// </summary>
    /// <param name="source">The UTF-8 hex source.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <returns>
    /// <see langword="true" /> when every UTF-8 byte is a valid hex digit; otherwise <see langword="false" />.
    /// </returns>
    private static bool DecodeHexPairsFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        int bi = 0;
        for (int i = 0; i < source.Length; i += 2)
        {
            int hi = NibbleFromByte(source[i]);
            int lo = NibbleFromByte(source[i + 1]);
            if ((hi | lo) < 0)
                return false;

            destination[bi++] = (byte)((hi << 4) | lo);
        }

        return true;
    }

    /// <summary>
    /// Strict-mode streaming decode for character source. Used by both
    /// <see cref="Base16.FromHexString(ReadOnlySpan{char}, Span{byte}, out int, out int)" /> and the lenient path of
    /// <see cref="DecodeFromUtf8" />.
    /// </summary>
    /// <param name="source">The hexadecimal characters.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="charsConsumed">When this method returns, contains the number of characters consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="isFinalBlock">Whether the input represents the final block in a streaming decode.</param>
    /// <returns>An <see cref="OperationStatus" />.</returns>
    private static OperationStatus DecodeStrictWithStatus(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten, bool isFinalBlock)
    {
        charsConsumed = 0;
        bytesWritten = 0;

        int sourcePos = 0;
        while (sourcePos + 1 < source.Length)
        {
            int hi = NibbleFromChar(source[sourcePos]);
            int lo = NibbleFromChar(source[sourcePos + 1]);
            if ((hi | lo) < 0)
                return OperationStatus.InvalidData;

            if (bytesWritten >= destination.Length)
                return OperationStatus.DestinationTooSmall;

            destination[bytesWritten++] = (byte)((hi << 4) | lo);
            sourcePos += 2;
            charsConsumed = sourcePos;
        }

        if (sourcePos == source.Length)
            return OperationStatus.Done;

        // One trailing character.
        return isFinalBlock ? OperationStatus.InvalidData : OperationStatus.NeedMoreData;
    }

    /// <summary>
    /// Strict-mode streaming decode for UTF-8 source.
    /// </summary>
    /// <param name="source">The UTF-8 hexadecimal source.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of source bytes consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="isFinalBlock">Whether the input represents the final block in a streaming decode.</param>
    /// <returns>An <see cref="OperationStatus" />.</returns>
    private static OperationStatus DecodeUtf8WithStatus(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
    {
        bytesConsumed = 0;
        bytesWritten = 0;

        int sourcePos = 0;
        while (sourcePos + 1 < source.Length)
        {
            int hi = NibbleFromByte(source[sourcePos]);
            int lo = NibbleFromByte(source[sourcePos + 1]);
            if ((hi | lo) < 0)
                return OperationStatus.InvalidData;

            if (bytesWritten >= destination.Length)
                return OperationStatus.DestinationTooSmall;

            destination[bytesWritten++] = (byte)((hi << 4) | lo);
            sourcePos += 2;
            bytesConsumed = sourcePos;
        }

        return sourcePos == source.Length
            ? OperationStatus.Done
            : isFinalBlock
                ? OperationStatus.InvalidData
                : OperationStatus.NeedMoreData;
    }

    /// <summary>
    /// Writes the hexadecimal representation of <paramref name="source" /> as UTF-8 bytes into
    /// <paramref name="destination" />.
    /// </summary>
    /// <param name="source">The source bytes.</param>
    /// <param name="destination">The destination span. Must be at least <c>source.Length * 2</c> bytes.</param>
    /// <param name="upperCase">Whether to emit upper case digits.</param>
    private static void EncodeToUtf8Core(ReadOnlySpan<byte> source, Span<byte> destination, bool upperCase)
    {
        string alphabet = upperCase ? HexUpperAlphabet : HexLowerAlphabet;
        for (int i = 0; i < source.Length; i++)
        {
            byte b = source[i];
            destination[i * 2] = (byte)alphabet[b >> 4];
            destination[(i * 2) + 1] = (byte)alphabet[b & 0x0F];
        }
    }

    /// <summary>
    /// Maps a UTF-8 byte to its hexadecimal nibble value.
    /// </summary>
    /// <param name="b">The byte.</param>
    /// <returns>The nibble value (0-15) or <c>-1</c> when the byte is not a valid ASCII hex digit.</returns>
    private static int NibbleFromByte(byte b) => b switch
    {
        >= (byte)'0' and <= (byte)'9' => b - (byte)'0',
        >= (byte)'A' and <= (byte)'F' => b - (byte)'A' + 10,
        >= (byte)'a' and <= (byte)'f' => b - (byte)'a' + 10,
        _ => -1,
    };

    /// <summary>
    /// Maps a character to its hexadecimal nibble value.
    /// </summary>
    /// <param name="c">The character.</param>
    /// <returns>The nibble value (0-15) or <c>-1</c> when the character is not a hex digit.</returns>
    private static int NibbleFromChar(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'A' and <= 'F' => c - 'A' + 10,
        >= 'a' and <= 'f' => c - 'a' + 10,
        _ => -1,
    };
}
