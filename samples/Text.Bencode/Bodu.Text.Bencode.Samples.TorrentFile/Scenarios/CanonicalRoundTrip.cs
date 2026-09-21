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
        SampleConsole.Scenario(
            "Canonical form - one value, exactly one encoding",
            what: "Re-emits the parsed torrent and compares it to the file byte for byte, then shows the writer "
                + "sorting keys as it closes a dictionary, rejecting a duplicate key, and the reader refusing "
                + "unsorted input unless the lenient option is set.",
            why: "Bencode requires a dictionary's keys to appear in ascending raw-byte order, which means a value "
                + "has one valid encoding rather than many. That is not a stylistic rule - it is what lets "
                + "BitTorrent identify a torrent by the SHA-1 of its info dictionary. If two encoders could "
                + "disagree about key order or integer padding, the same torrent would hash to different values "
                + "and the network would treat it as two different files. Canonical form is what makes hashing a "
                + "parsed-and-re-emitted structure safe at all.",
            expect: "The re-emitted bytes equal the file exactly, so nothing about the original encoding was "
                + "lost in the round trip. The writer emits 'length' before 'name' although they were written in "
                + "the other order, because it sorts on close rather than trusting the caller. A duplicate key "
                + "and unsorted input are both errors, since neither can produce a canonical encoding - the "
                + "lenient reader option exists for ingesting data someone else got wrong, not for writing it.");

        var original = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "sample.torrent"));

        // Re-emit the whole document through the writer.
        using var document = BencodeDocument.Parse(original);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);
        document.RootElement.WriteTo(writer);

        var identical = buffer.WrittenSpan.SequenceEqual(original);
        Console.WriteLine($"  re-emitted {buffer.WrittenCount} bytes; byte-identical to the file -> {identical}"
            + "  (expected True - the property that makes hashing a re-emitted structure safe)");

        // The writer produces canonical output no matter the write order: each dictionary's
        // entries are re-sorted into ascending bytewise key order as it closes...
        var sortedBuffer = new ArrayBufferWriter<byte>();
        var sorting = new Utf8BencodeWriter(sortedBuffer);
        sorting.WriteStartDictionary();
        sorting.WriteInteger("name", 1);   // written first,
        sorting.WriteInteger("length", 2); // but 'l' < 'n', so it emits first
        sorting.WriteEndDictionary();
        Console.WriteLine($"  writer sorts keys on close      -> {System.Text.Encoding.UTF8.GetString(sortedBuffer.WrittenSpan)}"
            + "  ('length' emits before 'name' despite the write order - the caller cannot produce non-canonical output by accident)");

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
            Console.WriteLine($"  duplicate key rejected on write -> {ex.Message}"
                + "  (no key order can encode a duplicate, so the writer fails rather than emitting something unparseable)");
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
            Console.WriteLine($"  unsorted keys rejected on read  -> {ex.Message}"
                + "  (the reader holds the same contract by default, so a non-canonical file cannot pass silently)");
        }

        var lenient = new Utf8BencodeReader(unsorted, new BencodeReaderOptions { AllowUnsortedKeys = true });
        var tokens = 0;
        while (lenient.Read())
        {
            tokens++;
        }

        Console.WriteLine($"  AllowUnsortedKeys = true accepts the same input ({tokens} tokens)"
            + "  (the opt-out for ingesting data another tool encoded wrong - re-emitting it will produce the canonical order)");

        Console.WriteLine();
    }
}
