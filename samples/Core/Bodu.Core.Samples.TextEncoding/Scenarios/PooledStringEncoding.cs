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
        SampleConsole.Scenario(
            "StringEncodingExtensions - sizing, span, and pooled encoding",
            what: "Measures a phrase in both encodings, encodes it into a stack buffer through a Try pattern, and " +
                  "again into a pooled buffer, checking all three agree byte for byte.",
            why: "Encoding a string normally allocates a byte array per call, which on a hot path is garbage " +
                 "generated for data that is consumed and discarded immediately. Sizing first lets the caller " +
                 "supply the memory: a stack buffer when the bound is small and known, a pooled one when it is " +
                 "not. The Try form matters because it reports a buffer too small rather than throwing, which is " +
                 "the difference between a fallback path and an exception on a hot loop.",
            expect: "17 UTF-8 bytes against 32 UTF-16 for 16 characters. All three routes produce identical " +
                    "bytes - that equality is the claim, since a pooled buffer is longer than its content and only " +
                    "WrittenSpan is meaningful.");

        Console.WriteLine($"  Phrase length    : {Phrase.Length} chars  (characters, not bytes - the two differ the moment the text leaves ASCII)");

        // Size probes let a caller allocate (or rent) exactly the right buffer up front.
        var utf8Count = Phrase.GetUtf8ByteCount();
        var utf16Count = Phrase.GetEncodedByteCount(Encoding.Unicode);
        Console.WriteLine($"  UTF-8 byte count : {utf8Count}  (expected 17 - measured before encoding, which is what lets the caller own the buffer)");
        Console.WriteLine($"  UTF-16 byte count: {utf16Count}  (expected 32 - the same text costs nearly twice as much in the encoding .NET uses in memory)");

        // TryEncodeUtf8To writes straight into a caller-owned span - here one on the stack - with no allocation.
        Span<byte> stackBuffer = stackalloc byte[utf8Count];

        // The Try form reports a too-small destination by returning false instead of throwing.
        var ok = Phrase.TryEncodeUtf8To(stackBuffer, out var bytesWritten);
        Console.WriteLine($"  TryEncodeUtf8To  : ok={ok}, bytesWritten={bytesWritten}  (expected True and 17 - a short buffer would return False here rather than throw, so a caller can fall back)");

        // ToUtf8Bytes is the simple allocating form, useful as a reference to compare against.
        var allocated = Phrase.ToUtf8Bytes();
        Console.WriteLine($"  ToUtf8Bytes match: {stackBuffer.SequenceEqual(allocated)}  (expected True - the allocating convenience form and the stack form must not diverge)");

        // GetUtf8BytesPooled rents its backing array from the shared pool. The builder is disposable, so a
        // 'using' returns the rented storage; read the bytes through WrittenSpan before it is disposed.
        using (PooledBufferBuilder<byte> pooled = Phrase.GetUtf8BytesPooled())
        {
            Console.WriteLine($"  GetUtf8BytesPooled: WrittenCount={pooled.WrittenCount}, matches={pooled.WrittenSpan.SequenceEqual(allocated)}  (the rented array is longer than the content, so read WrittenSpan and never the whole buffer)");
        }

        Console.WriteLine();
    }
}
