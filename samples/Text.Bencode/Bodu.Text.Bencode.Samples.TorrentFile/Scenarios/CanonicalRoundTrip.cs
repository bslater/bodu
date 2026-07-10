// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CanonicalRoundTrip.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Samples.TorrentFile.Scenarios;

/// <summary>
/// Demonstrates Bencode's defining property: the encoding is canonical. A dictionary's keys must
/// appear in ascending raw-byte order, so a value has exactly one valid encoding — which is why
/// parse → re-emit reproduces the input byte for byte, why the writer sorts each dictionary's
/// entries as it closes, and why the strict reader rejects unsorted input.
/// </summary>
public static class CanonicalRoundTrip
{
    /// <summary>
    /// Re-emits the parsed torrent and proves byte equality, then shows the writer and reader
    /// enforcing the canonical contract.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Canonical form: parse -> re-emit == original bytes ---");

        var original = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "sample.torrent"));

        // Re-emit the whole document through the writer.
        using var document = BencodeDocument.Parse(original);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);
        document.RootElement.WriteTo(writer);

        var identical = buffer.WrittenSpan.SequenceEqual(original);
        Console.WriteLine($"re-emitted {buffer.WrittenCount} bytes; byte-identical to the file -> {identical}");

        // The writer produces canonical output no matter the write order: each dictionary's
        // entries are re-sorted into ascending bytewise key order as it closes...
        var sortedBuffer = new ArrayBufferWriter<byte>();
        var sorting = new Utf8BencodeWriter(sortedBuffer);
        sorting.WriteStartDictionary();
        sorting.WriteInteger("name", 1);   // written first,
        sorting.WriteInteger("length", 2); // but 'l' < 'n', so it emits first
        sorting.WriteEndDictionary();
        Console.WriteLine($"writer sorts keys on close      -> {System.Text.Encoding.UTF8.GetString(sortedBuffer.WrittenSpan)}");

        // ...and a duplicate key can never produce a valid encoding, so it throws.
        try
        {
            var rejected = new Utf8BencodeWriter(new ArrayBufferWriter<byte>());
            rejected.WriteStartDictionary();
            rejected.WriteInteger("name", 1);
            rejected.WriteInteger("name", 2);
            rejected.WriteEndDictionary();
        }
        catch (BencodeSerializationException ex)
        {
            Console.WriteLine($"duplicate key rejected on write -> {ex.Message}");
        }

        // The reader enforces it too - unless you opt out for lenient ingestion.
        var unsorted = "d4:namei1e6:lengthi2ee"u8; // 'name' before 'length': not canonical

        try
        {
            var strict = new Utf8BencodeReader(unsorted);
            while (strict.Read())
            {
            }
        }
        catch (BencodeFormatException ex)
        {
            Console.WriteLine($"unsorted keys rejected on read  -> {ex.Message}");
        }

        var lenient = new Utf8BencodeReader(unsorted, new BencodeReaderOptions { AllowUnsortedKeys = true });
        var tokens = 0;
        while (lenient.Read())
        {
            tokens++;
        }

        Console.WriteLine($"AllowUnsortedKeys = true accepts the same input ({tokens} tokens)");

        Console.WriteLine();
    }
}
