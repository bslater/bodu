// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PocoTorrent.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Serialization;
using Bodu.Text.Serialization;

namespace Bodu.Text.Bencode.Samples.TorrentFile.Scenarios;

/// <summary>
/// Demonstrates the typed layer: <c>BencodeSerializer</c> maps the metainfo dictionary straight
/// onto a POCO graph. Torrent keys contain spaces (<c>creation date</c>, <c>piece length</c>) —
/// exactly what <c>[PropertyName]</c> exists for — and the binary <c>pieces</c> value
/// binds to <c>byte[]</c>, not <c>string</c>.
/// </summary>
public static class PocoTorrent
{
    /// <summary>
    /// The metainfo root. Keys that are not valid C# identifiers get explicit wire names.
    /// </summary>
    private sealed class TorrentMeta
    {
        [PropertyName("announce")]
        public string Announce { get; set; } = string.Empty;

        [PropertyName("comment")]
        public string Comment { get; set; } = string.Empty;

        [PropertyName("created by")]
        public string CreatedBy { get; set; } = string.Empty;

        [PropertyName("creation date")]
        public long CreationDate { get; set; }

        [PropertyName("info")]
        public TorrentInfo Info { get; set; } = new();
    }

    /// <summary>
    /// The single-file <c>info</c> dictionary.
    /// </summary>
    private sealed class TorrentInfo
    {
        [PropertyName("length")]
        public long Length { get; set; }

        [PropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [PropertyName("piece length")]
        public long PieceLength { get; set; }

        [PropertyName("pieces")]
        public byte[] Pieces { get; set; } = [];
    }

    /// <summary>
    /// Deserializes the torrent to a POCO graph and round-trips it back to identical bytes.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Typed layer - BencodeSerializer onto a POCO graph",
            what: "Deserializes the same torrent into a two-class object graph, reads the fields as ordinary "
                + "properties, and serializes the graph back to compare against the original file.",
            why: "Once the shape is known, a typed model beats cursor-walking: the keys are validated in one "
                + "place, the values arrive as the right CLR types, and the rest of the program is ordinary C#. "
                + "Two things are worth noticing in the model. Torrent keys contain spaces, so they cannot be C# "
                + "identifiers - [PropertyName] carries the wire name, which is the mechanism for every format "
                + "whose keys are not identifiers. And 'pieces' binds to byte[] rather than string, because "
                + "Bencode byte strings are not text; choosing string there would corrupt the hashes on the way "
                + "in and again on the way out.",
            expect: "The re-serialized bytes equal the original file. That is a stronger result than it looks - "
                + "it means the serializer emitted the same canonical key order and preserved the binary value "
                + "intact, so a torrent can be deserialized, inspected, and written back without changing its "
                + "info-hash.");

        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "sample.torrent"));
        var torrent = BencodeSerializer.Deserialize<TorrentMeta>(bytes);

        Console.WriteLine($"  announce   : {torrent.Announce}");
        Console.WriteLine($"  created by : {torrent.CreatedBy} at {DateTimeOffset.FromUnixTimeSeconds(torrent.CreationDate):yyyy-MM-dd}");
        Console.WriteLine($"  payload    : {torrent.Info.Name} ({torrent.Info.Length} bytes, {torrent.Info.Pieces.Length / 20} pieces)"
            + "  (Pieces bound to byte[], so the 20-byte SHA-1 hashes survive the trip through the object model)");

        // Canonical encoding means the POCO round trip is also byte-exact: the serializer
        // re-emits the same sorted keys and the same binary 'pieces' value.
        var reEncoded = BencodeSerializer.Serialize(torrent);
        Console.WriteLine($"  round trip : {reEncoded.Length} bytes, byte-identical -> {reEncoded.AsSpan().SequenceEqual(bytes)}"
            + "  (expected True - deserialize, inspect and re-serialize without changing the torrent's identity)");

        Console.WriteLine();
    }
}
