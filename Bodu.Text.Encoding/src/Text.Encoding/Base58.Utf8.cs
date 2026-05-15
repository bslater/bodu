// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58.Utf8.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base58
{
    /// <summary>
    /// Encodes <paramref name="source" /> into a UTF-8 Base58 byte array using the supplied variant.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="variant">The Base58 variant.</param>
    /// <returns>The UTF-8 encoded Base58 bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static byte[] EncodeToUtf8(ReadOnlySpan<byte> source, Base58Variant variant = Base58Variant.BitcoinFlickr)
    {
        string alphabet = GetAlphabet(variant);

        if (source.IsEmpty)
            return Array.Empty<byte>();

        int upperBound = GetMaxEncodedLength(source.Length);
        char[] scratch = new char[upperBound];
        int written = EncodeIntoBuffer(source, alphabet, scratch);

        byte[] result = new byte[written];
        for (int i = 0; i < written; i++)
            result[i] = (byte)scratch[scratch.Length - written + i];

        return result;
    }

    /// <summary>
    /// Attempts to encode <paramref name="source" /> as UTF-8 Base58 bytes into <paramref name="destination" />.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The destination UTF-8 byte span.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="variant">The Base58 variant.</param>
    /// <returns><see langword="true" /> on success; <see langword="false" /> when the destination is too small.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool TryEncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, Base58Variant variant = Base58Variant.BitcoinFlickr)
    {
        string alphabet = GetAlphabet(variant);

        if (source.IsEmpty)
        {
            bytesWritten = 0;
            return true;
        }

        int upperBound = GetMaxEncodedLength(source.Length);
        char[] scratch = new char[upperBound];
        int written = EncodeIntoBuffer(source, alphabet, scratch);

        if (destination.Length < written)
        {
            bytesWritten = 0;
            return false;
        }

        for (int i = 0; i < written; i++)
            destination[i] = (byte)scratch[scratch.Length - written + i];

        bytesWritten = written;
        return true;
    }

    /// <summary>
    /// Decodes UTF-8 Base58 bytes into a byte span using the <see cref="OperationStatus" /> return convention.
    /// </summary>
    /// <param name="source">The UTF-8 Base58 source.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of source bytes consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    /// <remarks>
    /// Base58 is not a streamable encoding; this overload always treats <paramref name="source" /> as a complete
    /// input. <see cref="OperationStatus.NeedMoreData" /> is never returned.
    /// </remarks>
    public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, Base58Variant variant = Base58Variant.BitcoinFlickr, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        sbyte[] lookup = GetLookup(variant);
        bytesConsumed = 0;
        bytesWritten = 0;

        if (source.IsEmpty)
            return OperationStatus.Done;

        // Project the UTF-8 source through a temporary char buffer so the BigInteger decode path can be reused.
        char[] scratch = new char[source.Length];
        for (int i = 0; i < source.Length; i++)
            scratch[i] = (char)source[i];

        OperationStatus status = DecodeWithStatus(scratch.AsSpan(), destination, out int _, out bytesWritten, variant, styles);
        if (status == OperationStatus.Done || status == OperationStatus.DestinationTooSmall)
            bytesConsumed = source.Length;

        return status;
    }

    /// <summary>
    /// Decodes a Base58 character span into a byte span using the <see cref="OperationStatus" /> return convention.
    /// </summary>
    /// <param name="source">The Base58 character span.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <param name="charsConsumed">When this method returns, contains the number of characters consumed.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    private static OperationStatus DecodeWithStatus(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten, Base58Variant variant, BaseFormatStyles styles)
    {
        sbyte[] lookup = GetLookup(variant);
        charsConsumed = 0;
        bytesWritten = 0;

        if (source.IsEmpty)
            return OperationStatus.Done;

        if (!TryDecodeCore(source, lookup, styles, out byte[]? result, out _))
            return OperationStatus.InvalidData;

        if (destination.Length < result!.Length)
            return OperationStatus.DestinationTooSmall;

        result.AsSpan().CopyTo(destination);
        bytesWritten = result.Length;
        charsConsumed = source.Length;
        return OperationStatus.Done;
    }
}
