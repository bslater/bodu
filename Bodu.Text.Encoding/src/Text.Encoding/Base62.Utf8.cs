// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base62.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base62
{
    /// <summary>
    /// Decodes UTF-8 Base62 text into a byte span using the <see cref="OperationStatus" /> return convention.
    /// </summary>
    /// <param name="source">The UTF-8 Base62 text.</param>
    /// <param name="destination">The span that receives the decoded bytes.</param>
    /// <param name="bytesConsumed">
    /// When this method returns, contains the number of bytes of <paramref name="source" /> consumed.
    /// </param>
    /// <param name="bytesWritten">
    /// When this method returns, contains the number of bytes written to <paramref name="destination" />.
    /// </param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>An <see cref="OperationStatus" /> describing the outcome.</returns>
    /// <remarks>
    /// The whole of <paramref name="source" /> is decoded as one input, so <see cref="OperationStatus.NeedMoreData" />
    /// is never returned, and the result is all or nothing: on <see cref="OperationStatus.InvalidData" /> or
    /// <see cref="OperationStatus.DestinationTooSmall" /> nothing is consumed or written. A byte from <c>0x80</c> up,
    /// which UTF-8 uses only within multi-byte sequences, is outside the alphabet and yields
    /// <see cref="OperationStatus.InvalidData" />.
    /// </remarks>
    public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        bytesConsumed = 0;
        bytesWritten = 0;

        if (source.IsEmpty)
            return OperationStatus.Done;

        char[] chars = ArrayPool<char>.Shared.Rent(source.Length);
        try
        {
            // Each byte maps to the character of the same value. The alphabet is ASCII, so a byte of a multi-byte
            // sequence maps to no symbol and fails as any other character outside the alphabet does.
            for (int i = 0; i < source.Length; i++)
                chars[i] = (char)source[i];

            if (!TryDecodeCore(chars.AsSpan(0, source.Length), styles, out byte[]? result, out _))
                return OperationStatus.InvalidData;

            if (destination.Length < result!.Length)
                return OperationStatus.DestinationTooSmall;

            result.CopyTo(destination);
            bytesConsumed = source.Length;
            bytesWritten = result.Length;
            return OperationStatus.Done;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(chars);
        }
    }

    /// <summary>
    /// Encodes <paramref name="source" /> as UTF-8 Base62 text.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <returns>The UTF-8 Base62 text, or an empty array for empty input.</returns>
    public static byte[] EncodeToUtf8(ReadOnlySpan<byte> source)
    {
        if (source.IsEmpty)
            return [];

        int upperBound = GetMaxEncodedLength(source.Length);
        char[] scratch = ArrayPool<char>.Shared.Rent(upperBound);
        try
        {
            int written = EncodeIntoBuffer(source, scratch, upperBound);
            byte[] result = new byte[written];
            Narrow(scratch.AsSpan(upperBound - written, written), result);
            return result;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(scratch);
        }
    }

    /// <summary>
    /// Attempts to encode <paramref name="source" /> as UTF-8 Base62 text into <paramref name="destination" />.
    /// </summary>
    /// <param name="source">The bytes to encode.</param>
    /// <param name="destination">The span that receives the UTF-8 text.</param>
    /// <param name="bytesWritten">
    /// When this method returns, contains the number of bytes written, or <c>0</c> on failure.
    /// </param>
    /// <returns>
    /// <see langword="true" /> on success; <see langword="false" /> when <paramref name="destination" /> is too small.
    /// </returns>
    public static bool TryEncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
    {
        if (source.IsEmpty)
        {
            bytesWritten = 0;
            return true;
        }

        int upperBound = GetMaxEncodedLength(source.Length);
        char[] scratch = ArrayPool<char>.Shared.Rent(upperBound);
        try
        {
            int written = EncodeIntoBuffer(source, scratch, upperBound);
            if (destination.Length < written)
            {
                bytesWritten = 0;
                return false;
            }

            Narrow(scratch.AsSpan(upperBound - written, written), destination);
            bytesWritten = written;
            return true;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(scratch);
        }
    }

    /// <summary>
    /// Copies Base62 characters into <paramref name="destination" /> as the UTF-8 bytes of the same values.
    /// </summary>
    /// <param name="chars">The Base62 characters, every one of them ASCII.</param>
    /// <param name="destination">
    /// The span that receives the bytes; at least as long as <paramref name="chars" />.
    /// </param>
    private static void Narrow(ReadOnlySpan<char> chars, Span<byte> destination)
    {
        for (int i = 0; i < chars.Length; i++)
            destination[i] = (byte)chars[i];
    }
}
