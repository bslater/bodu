// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChecksummedSchemes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Encoding;

namespace Bodu.Samples.Text.Encoding.EncodingTour.Scenarios;

/// <summary>
/// Demonstrates the checksummed schemes — encodings for identifiers humans re-type:
/// <see cref="Base58Check" /> (Bitcoin addresses; a 4-byte double-SHA-256 checksum) and
/// <see cref="Bech32" /> (BIP 173; a BCH code plus a human-readable part). A single corrupted
/// character makes decode fail instead of silently yielding wrong bytes.
/// </summary>
public static class ChecksummedSchemes
{
    /// <summary>
    /// Encodes with both schemes, then corrupts one character and shows the rejection.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Checksummed schemes: corruption detection ---");

        var payload = new byte[] { 0x00, 0x14, 0x75, 0x1E, 0x76, 0xE8, 0x19, 0x91, 0x96, 0xD4 };

        // Base58Check appends a 4-byte checksum before Base58-encoding.
        var address = Base58Check.Encode(payload);
        Console.WriteLine($"Base58Check : {address}");

        var corrupted = address[..^1] + (address[^1] == '1' ? '2' : '1');
        try
        {
            Base58Check.Decode(corrupted);
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"  corrupt last char -> {ex.Message}");
        }

        // Bech32 carries a human-readable part (hrp) and a BCH checksum.
        var bech = Bech32.EncodeFromBytes("sample", payload);
        Console.WriteLine($"Bech32      : {bech}");

        var bechCorrupted = bech[..^1] + (bech[^1] == 'q' ? 'p' : 'q');
        try
        {
            Bech32.DecodeToBytes(bechCorrupted, out _, out _, out _);
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"  corrupt last char -> {ex.Message}");
        }

        // Intact input round-trips, returning the hrp and payload.
        Bech32.DecodeToBytes(bech, out var hrp, out var data, out var encoding);
        Console.WriteLine($"  intact decode     -> hrp '{hrp}', {data.Length} bytes, {encoding} ({data.AsSpan().SequenceEqual(payload)})");

        Console.WriteLine();
    }
}
