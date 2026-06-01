// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base85
{
    /// <summary>
    /// Decodes UTF-8 Base85 bytes into a byte span using the <see cref="OperationStatus" /> return convention.
    /// </summary>
    /// <param name="source">The UTF-8 Base85 source.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of source bytes consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    /// <remarks>
    /// Base85 decoding is not streamable in a useful way (each group requires exactly five characters), so the decoder
    /// treats <paramref name="source" /> as a complete input and does not return
    /// <see cref="OperationStatus.NeedMoreData" />.
    /// </remarks>
    public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, Base85Variant variant = Base85Variant.Ascii85, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        EnsureValidVariant(variant);
        bytesConsumed = 0;
        bytesWritten = 0;

        if (source.IsEmpty)
            return OperationStatus.Done;

        var scratch = new char[source.Length];
        for (var i = 0; i < source.Length; i++)
            scratch[i] = (char)source[i];

        OperationStatus status = DecodeWithStatus(scratch.AsSpan(), destination, out var _, out bytesWritten, variant, styles);
        if (status == OperationStatus.Done)
            bytesConsumed = source.Length;

        return status;
    }

    /// <summary>
    /// Encodes <paramref name="source" /> into a UTF-8 Base85 byte array.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="variant">The variant.</param>
    /// <returns>The UTF-8 encoded Base85 bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="variant" /> is <see cref="Base85Variant.Z85" /> and the byte count is not a multiple
    /// of four.
    /// </exception>
    public static byte[] EncodeToUtf8(ReadOnlySpan<byte> source, Base85Variant variant = Base85Variant.Ascii85)
    {
        EnsureValidVariant(variant);

        if (source.IsEmpty)
            return [];

        if (variant == Base85Variant.Z85 && (source.Length & 3) != 0)
            throw new ArgumentException(EncodingResourceStrings.Arg_Invalid_Z85InputMultipleOfFour, nameof(source));

        var upperBound = GetMaxEncodedLength(source.Length, variant);
        var scratch = new char[upperBound];
        var written = EncodeIntoBuffer(source, variant, scratch);

        var result = new byte[written];
        for (var i = 0; i < written; i++)
            result[i] = (byte)scratch[i];

        return result;
    }

    /// <summary>
    /// Attempts to encode <paramref name="source" /> as UTF-8 Base85 bytes into <paramref name="destination" />.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination UTF-8 byte span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="variant">The variant.</param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when the destination is too small.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="variant" /> is <see cref="Base85Variant.Z85" /> and the byte count is not a multiple
    /// of four.
    /// </exception>
    public static bool TryEncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, Base85Variant variant = Base85Variant.Ascii85)
    {
        EnsureValidVariant(variant);

        if (source.IsEmpty)
        {
            bytesWritten = 0;
            return true;
        }

        if (variant == Base85Variant.Z85 && (source.Length & 3) != 0)
            throw new ArgumentException(EncodingResourceStrings.Arg_Invalid_Z85InputMultipleOfFour, nameof(source));

        var upperBound = GetMaxEncodedLength(source.Length, variant);
        var scratch = new char[upperBound];
        var written = EncodeIntoBuffer(source, variant, scratch);

        if (destination.Length < written)
        {
            bytesWritten = 0;
            return false;
        }

        for (var i = 0; i < written; i++)
            destination[i] = (byte)scratch[i];

        bytesWritten = written;
        return true;
    }

    /// <summary>
    /// Decodes Base85 characters into a byte span using the <see cref="OperationStatus" /> return convention.
    /// </summary>
    /// <param name="source">The character source.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="charsConsumed">When this method returns, contains the number of characters consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    private static OperationStatus DecodeWithStatus(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten, Base85Variant variant, BaseFormatStyles styles)
    {
        charsConsumed = 0;
        bytesWritten = 0;

        if (source.IsEmpty)
            return OperationStatus.Done;

        var buffer = new byte[GetMaxDecodedLength(source.Length)];
        if (!TryDecodeCore(source, variant, styles, buffer, out var written, out _))
            return OperationStatus.InvalidData;

        if (destination.Length < written)
            return OperationStatus.DestinationTooSmall;

        buffer.AsSpan(0, written).CopyTo(destination);
        bytesWritten = written;
        charsConsumed = source.Length;
        return OperationStatus.Done;
    }
}
