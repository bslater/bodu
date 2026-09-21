// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InfoHashRawSlice.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Security.Cryptography;
using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Writer;
using Bodu.Text.Encoding;

namespace Bodu.Text.Bencode.Samples.TorrentFile.Scenarios;

/// <summary>
/// Demonstrates the raw-slice surface with BitTorrent's most famous requirement: the info-hash is
/// the SHA-1 of the <c>info</c> dictionary's <em>exact encoded bytes</em>. Because Bencode is
/// canonical, <see cref="BencodeElement.GetRawBytes" /> hands back precisely that slice — and
/// <see cref="Utf8BencodeWriter.WriteRawValue" /> lets a new document embed it verbatim, so the
/// hash survives re-authoring.
/// </summary>
public static class InfoHashRawSlice
{
    /// <summary>
    /// Computes the torrent's info-hash and re-embeds the raw slice into a new document.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Info-hash - hashing an element's exact encoded bytes",
            what: "Takes the raw encoded slice of the 'info' dictionary straight out of the parsed document, "
                + "hashes it, then splices that untouched slice into a freshly authored torrent with a different "
                + "tracker and re-hashes the result.",
            why: "BitTorrent identifies a torrent by the SHA-1 of its info dictionary's encoded bytes, so the "
                + "hash is over a byte range rather than over a value. Recomputing it by re-serializing a parsed "
                + "object would be a bet that the serializer reproduces the original encoding exactly - true here "
                + "because Bencode is canonical, but a bet nonetheless, and false for most formats. GetRawBytes "
                + "removes the bet: it returns the bytes that were actually in the file. WriteRawValue is the "
                + "same idea on the writing side, letting a document carry a foreign sub-structure through "
                + "unmodified.",
            expect: "The info slice is a subset of the file - the tracker URL and comment sit outside it, which "
                + "is exactly why re-authoring those does not change the identity. After the rewrite the new "
                + "document carries a different announce URL and an unchanged info-hash, which is what makes "
                + "mirroring a torrent to another tracker possible.");

        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "sample.torrent"));
        using var document = BencodeDocument.Parse(bytes);

        // GetRawBytes returns the element's complete encoded form - the exact bytes BEP 3
        // says to hash. No re-serialization, no risk of drift.
        var infoSlice = document.RootElement.GetProperty("info").GetRawBytes();
        var infoHash = SHA1.HashData(infoSlice);

        Console.WriteLine($"  info slice : {infoSlice.Length} bytes of the {bytes.Length}-byte file"
            + "  (a slice of the parsed buffer, not a re-serialization - these are the bytes BEP 3 says to hash)");
        Console.WriteLine($"  info-hash  : {Base16.Encode(infoHash, Base16Variant.Lower)}"
            + "  (the torrent's identity on the network - every peer must derive this same value)");

        // Re-author the torrent (new tracker, same payload) by splicing the untouched slice
        // into a fresh document with WriteRawValue - the info-hash cannot change.
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);
        writer.WriteStartDictionary();
        writer.WriteString("announce", "http://mirror.example.org:6969/announce");
        writer.WritePropertyName("info");
        writer.WriteRawValue(infoSlice);
        writer.WriteEndDictionary();

        using var reAuthored = BencodeDocument.Parse(buffer.WrittenSpan.ToArray());
        var rehash = SHA1.HashData(reAuthored.RootElement.GetProperty("info").GetRawBytes());

        Console.WriteLine($"  re-authored: new announce '{reAuthored.RootElement.GetProperty("announce").GetString()}'"
            + "  (a different tracker - announce lives outside the info dictionary, so it is not part of the identity)");
        Console.WriteLine($"  hash intact: {rehash.AsSpan().SequenceEqual(infoHash)}"
            + "  (expected True - the slice was copied through verbatim, so peers still recognise the same torrent)");

        Console.WriteLine();
    }
}
