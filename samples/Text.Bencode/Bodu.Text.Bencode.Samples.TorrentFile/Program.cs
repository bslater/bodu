// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Samples.TorrentFile.Scenarios;

namespace Bodu.Text.Bencode.Samples.TorrentFile;

/// <summary>
/// Entry point for the torrent-file sample: reading a real BitTorrent metainfo file (BEP 3) with
/// <c>Bodu.Text.Bencode</c> — DOM inspection, canonical byte-exact round trips, computing the
/// info-hash from a raw element slice, and typed POCO mapping. Everything runs offline against
/// the committed <c>Data/sample.torrent</c>.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Text.Bencode.Samples.TorrentFile");
        Console.WriteLine("=====================================");
        Console.WriteLine();

        ParseTorrent.Run();
        CanonicalRoundTrip.Run();
        InfoHashRawSlice.Run();
        PocoTorrent.Run();

        Console.WriteLine("Done.");
    }
}
