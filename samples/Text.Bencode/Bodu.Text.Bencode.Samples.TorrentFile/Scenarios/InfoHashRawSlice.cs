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
        Console.WriteLine("--- Info-hash: SHA-1 over the raw 'info' slice ---");

        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "sample.torrent"));
        using var document = BencodeDocument.Parse(bytes);

        // GetRawBytes returns the element's complete encoded form - the exact bytes BEP 3
        // says to hash. No re-serialization, no risk of drift.
        var infoSlice = document.RootElement.GetProperty("info").GetRawBytes();
        var infoHash = SHA1.HashData(infoSlice);

        Console.WriteLine($"info slice : {infoSlice.Length} bytes of the {bytes.Length}-byte file");
        Console.WriteLine($"info-hash  : {Base16.Encode(infoHash, Base16Variant.Lower)}");

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

        Console.WriteLine($"re-authored: new announce '{reAuthored.RootElement.GetProperty("announce").GetString()}'");
        Console.WriteLine($"hash intact: {rehash.AsSpan().SequenceEqual(infoHash)}");

        Console.WriteLine();
    }
}
