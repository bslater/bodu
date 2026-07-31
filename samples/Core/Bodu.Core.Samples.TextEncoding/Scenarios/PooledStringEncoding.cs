// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledStringEncoding.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Buffers;
using Bodu.Text;

namespace Bodu.Core.Samples.TextEncoding.Scenarios;

/// <summary>
/// Demonstrates <see cref="StringEncodingExtensions" />: the string-side encoding helpers that let a caller size
/// a buffer exactly (<c>GetUtf8ByteCount</c> / <c>GetEncodedByteCount</c>), encode straight into a caller-owned
/// span (<c>TryEncodeUtf8To</c>), or rent the output buffer from the pool instead of allocating a fresh array
/// (<c>GetUtf8BytesPooled</c>).
/// </summary>
public static class PooledStringEncoding
{
    // 'é' costs two UTF-8 bytes, so the byte count is one more than the character count.
    private const string Phrase = "Hello, Bodu café";

    /// <summary>
    /// Probes the encoded size, encodes into a stack buffer, and encodes into a pooled buffer.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- StringEncodingExtensions: sizing, span, and pooled encoding ---");

        Console.WriteLine($"Phrase length    : {Phrase.Length} chars");

        // Size probes let a caller allocate (or rent) exactly the right buffer up front.
        var utf8Count = Phrase.GetUtf8ByteCount();
        var utf16Count = Phrase.GetEncodedByteCount(Encoding.Unicode);
        Console.WriteLine($"UTF-8 byte count : {utf8Count}");
        Console.WriteLine($"UTF-16 byte count: {utf16Count}");

        // TryEncodeUtf8To writes straight into a caller-owned span - here one on the stack - with no allocation.
        Span<byte> stackBuffer = stackalloc byte[utf8Count];

        // The Try form reports a too-small destination by returning false instead of throwing.
        var ok = Phrase.TryEncodeUtf8To(stackBuffer, out var bytesWritten);
        Console.WriteLine($"TryEncodeUtf8To  : ok={ok}, bytesWritten={bytesWritten}");

        // ToUtf8Bytes is the simple allocating form, useful as a reference to compare against.
        var allocated = Phrase.ToUtf8Bytes();
        Console.WriteLine($"ToUtf8Bytes match: {stackBuffer.SequenceEqual(allocated)}");

        // GetUtf8BytesPooled rents its backing array from the shared pool. The builder is disposable, so a
        // 'using' returns the rented storage; read the bytes through WrittenSpan before it is disposed.
        using (PooledBufferBuilder<byte> pooled = Phrase.GetUtf8BytesPooled())
        {
            Console.WriteLine($"GetUtf8BytesPooled: WrittenCount={pooled.WrittenCount}, matches={pooled.WrittenSpan.SequenceEqual(allocated)}");
        }

        Console.WriteLine();
    }
}
