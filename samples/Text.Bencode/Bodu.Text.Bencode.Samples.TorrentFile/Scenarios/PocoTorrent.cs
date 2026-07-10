// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PocoTorrent.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode.Samples.TorrentFile.Scenarios;

/// <summary>
/// Demonstrates the typed layer: <c>BencodeSerializer</c> maps the metainfo dictionary straight
/// onto a POCO graph. Torrent keys contain spaces (<c>creation date</c>, <c>piece length</c>) —
/// exactly what <c>[BencodePropertyName]</c> exists for — and the binary <c>pieces</c> value
/// binds to <c>byte[]</c>, not <c>string</c>.
/// </summary>
public static class PocoTorrent
{
    /// <summary>
    /// The metainfo root. Keys that are not valid C# identifiers get explicit wire names.
    /// </summary>
    private sealed class TorrentMeta
    {
        [BencodePropertyName("announce")]
        public string Announce { get; set; } = string.Empty;

        [BencodePropertyName("comment")]
        public string Comment { get; set; } = string.Empty;

        [BencodePropertyName("created by")]
        public string CreatedBy { get; set; } = string.Empty;

        [BencodePropertyName("creation date")]
        public long CreationDate { get; set; }

        [BencodePropertyName("info")]
        public TorrentInfo Info { get; set; } = new();
    }

    /// <summary>
    /// The single-file <c>info</c> dictionary.
    /// </summary>
    private sealed class TorrentInfo
    {
        [BencodePropertyName("length")]
        public long Length { get; set; }

        [BencodePropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [BencodePropertyName("piece length")]
        public long PieceLength { get; set; }

        [BencodePropertyName("pieces")]
        public byte[] Pieces { get; set; } = [];
    }

    /// <summary>
    /// Deserializes the torrent to a POCO graph and round-trips it back to identical bytes.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Typed layer: BencodeSerializer -> POCO graph ---");

        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "sample.torrent"));
        var torrent = BencodeSerializer.Deserialize<TorrentMeta>(bytes);

        Console.WriteLine($"announce   : {torrent.Announce}");
        Console.WriteLine($"created by : {torrent.CreatedBy} at {DateTimeOffset.FromUnixTimeSeconds(torrent.CreationDate):yyyy-MM-dd}");
        Console.WriteLine($"payload    : {torrent.Info.Name} ({torrent.Info.Length} bytes, {torrent.Info.Pieces.Length / 20} pieces)");

        // Canonical encoding means the POCO round trip is also byte-exact: the serializer
        // re-emits the same sorted keys and the same binary 'pieces' value.
        var reEncoded = BencodeSerializer.Serialize(torrent);
        Console.WriteLine($"round trip : {reEncoded.Length} bytes, byte-identical -> {reEncoded.AsSpan().SequenceEqual(bytes)}");

        Console.WriteLine();
    }
}
