// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParseTorrent.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode.Samples.TorrentFile.Scenarios;

/// <summary>
/// Demonstrates reading a torrent's metainfo with the read-only <see cref="BencodeDocument" />
/// DOM: one parse over the raw bytes, then cheap <see cref="BencodeElement" /> cursors — the
/// right layer for inspecting a file whose exact shape you discover as you go.
/// </summary>
public static class ParseTorrent
{
    /// <summary>
    /// Parses <c>Data/sample.torrent</c> and walks the metainfo dictionary.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Reading a .torrent with the read-only BencodeDocument DOM",
            what: "Parses the committed torrent once, then walks the metainfo dictionary through cheap element "
                + "cursors: tracker URL, creation timestamp, the info dictionary's payload description, the raw "
                + "piece hashes, and a safe probe for a key this fixture does not have.",
            why: "This is the layer for a file whose exact shape you discover as you read it. A torrent is a "
                + "dictionary of mostly-optional keys, so binding it to a fixed type up front means either a "
                + "schema that rejects real files or one padded with nullables. The document DOM parses once and "
                + "hands out cursors into that single buffer, so walking it costs no further allocation. The other "
                + "reason to read at this layer is Bencode's byte-string model: a value is a length-prefixed run "
                + "of bytes, and whether it is text or binary is something only the spec tells you - the encoding "
                + "does not.",
            expect: "Every field resolves from one parse. Note the split between GetString for the text keys and "
                + "GetBytes for 'pieces': that value is a concatenation of 20-byte SHA-1 hashes, and decoding it "
                + "as UTF-8 would corrupt it. TryGetProperty reports the absent key as False rather than throwing, "
                + "which is how optional metainfo keys are meant to be probed.");

        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "sample.torrent"));
        using var document = BencodeDocument.Parse(bytes);
        var root = document.RootElement;

        // Top-level metainfo keys. Bencode strings are byte strings; GetString decodes UTF-8.
        Console.WriteLine($"  announce      : {root.GetProperty("announce").GetString()}"
            + "  (a text byte string - GetString decodes it as UTF-8)");
        Console.WriteLine($"  comment       : {root.GetProperty("comment").GetString()}");

        // 'creation date' is a unix timestamp - a Bencode integer.
        var createdAt = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("creation date").GetInt64());
        Console.WriteLine($"  creation date : {createdAt:yyyy-MM-dd HH:mm}Z"
            + "  (Bencode has one numeric type, a signed integer - there is no date type, so the wire value is a unix timestamp)");

        // The 'info' dictionary describes the payload (single-file form here).
        var info = root.GetProperty("info");
        var length = info.GetProperty("length").GetInt64();
        var pieceLength = info.GetProperty("piece length").GetInt64();

        Console.WriteLine($"  info.name     : {info.GetProperty("name").GetString()}");
        Console.WriteLine($"  info.length   : {length} bytes across {(length + pieceLength - 1) / pieceLength} pieces of {pieceLength}");

        // 'pieces' is raw binary (20-byte SHA-1 per piece) - GetBytes, never GetString.
        var pieces = info.GetProperty("pieces").GetBytes();
        Console.WriteLine($"  info.pieces   : {pieces.Length} bytes = {pieces.Length / 20} SHA-1 piece hashes"
            + "  (read with GetBytes, never GetString - this is binary, and a UTF-8 decode would silently corrupt it)");

        // Probe optional keys safely; this fixture has no 'announce-list'.
        Console.WriteLine($"  announce-list : present = {root.TryGetProperty("announce-list", out _)}"
            + "  (expected False - most metainfo keys are optional, so probing beats catching)");

        Console.WriteLine();
    }
}
