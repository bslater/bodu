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
        Console.WriteLine("--- Read a .torrent with the BencodeDocument DOM ---");

        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "sample.torrent"));
        using var document = BencodeDocument.Parse(bytes);
        var root = document.RootElement;

        // Top-level metainfo keys. Bencode strings are byte strings; GetString decodes UTF-8.
        Console.WriteLine($"announce      : {root.GetProperty("announce").GetString()}");
        Console.WriteLine($"comment       : {root.GetProperty("comment").GetString()}");

        // 'creation date' is a unix timestamp - a Bencode integer.
        var createdAt = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("creation date").GetInt64());
        Console.WriteLine($"creation date : {createdAt:yyyy-MM-dd HH:mm}Z");

        // The 'info' dictionary describes the payload (single-file form here).
        var info = root.GetProperty("info");
        var length = info.GetProperty("length").GetInt64();
        var pieceLength = info.GetProperty("piece length").GetInt64();

        Console.WriteLine($"info.name     : {info.GetProperty("name").GetString()}");
        Console.WriteLine($"info.length   : {length} bytes across {(length + pieceLength - 1) / pieceLength} pieces of {pieceLength}");

        // 'pieces' is raw binary (20-byte SHA-1 per piece) - GetBytes, never GetString.
        var pieces = info.GetProperty("pieces").GetBytes();
        Console.WriteLine($"info.pieces   : {pieces.Length} bytes = {pieces.Length / 20} SHA-1 piece hashes");

        // Probe optional keys safely; this fixture has no 'announce-list'.
        Console.WriteLine($"announce-list : present = {root.TryGetProperty("announce-list", out _)}");

        Console.WriteLine();
    }
}
