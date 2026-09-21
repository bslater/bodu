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
        SampleConsole.Scenario(
            "Checksummed schemes - encodings that refuse corrupted input",
            what: "Encodes one payload as a Base58Check string and as a Bech32 string, alters the final character "
                + "of each, and attempts to decode both; then decodes the intact Bech32 string back to its "
                + "human-readable part and bytes.",
            why: "A plain encoding has no opinion about whether its input is the string you meant. Change a "
                + "character in a Base58 address and you usually get a different, perfectly valid byte sequence - "
                + "which for a payment address means funds sent somewhere unrecoverable. These schemes add a "
                + "checksum inside the encoding so that mistake becomes a decode failure instead. Bech32 goes "
                + "further with a BCH code that is designed to catch the specific error patterns humans and OCR "
                + "produce, plus a human-readable prefix so a string carries what network it belongs to.",
            expect: "Both corrupted strings are rejected with a FormatException rather than returning plausible "
                + "wrong bytes - that difference is the entire point of the scheme. The intact string round-trips "
                + "to the original payload and reports its prefix and which Bech32 variant encoded it.");

        var payload = new byte[] { 0x00, 0x14, 0x75, 0x1E, 0x76, 0xE8, 0x19, 0x91, 0x96, 0xD4 };

        // Base58Check appends a 4-byte checksum before Base58-encoding.
        var address = Base58Check.Encode(payload);
        Console.WriteLine($"  Base58Check : {address}"
            + "  (payload plus a 4-byte double-SHA-256 checksum, then Base58 over the whole thing)");

        var corrupted = address[..^1] + (address[^1] == '1' ? '2' : '1');
        try
        {
            Base58Check.Decode(corrupted);
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"  corrupt last char -> {ex.Message}"
                + "  (rejected, not silently decoded to different bytes - the failure mode a plain Base58 string does not have)");
        }

        // Bech32 carries a human-readable part (hrp) and a BCH checksum.
        var bech = Bech32.EncodeFromBytes("sample", payload);
        Console.WriteLine($"  Bech32      : {bech}"
            + "  (the 'sample' prefix before the separator names the context, so a string cannot be used on the wrong network)");

        var bechCorrupted = bech[..^1] + (bech[^1] == 'q' ? 'p' : 'q');
        try
        {
            Bech32.DecodeToBytes(bechCorrupted, out _, out _, out _);
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"  corrupt last char -> {ex.Message}"
                + "  (the BCH code is tuned for the errors people actually make re-typing or scanning a value)");
        }

        // Intact input round-trips, returning the hrp and payload.
        Bech32.DecodeToBytes(bech, out var hrp, out var data, out var encoding);
        Console.WriteLine($"  intact decode     -> hrp '{hrp}', {data.Length} bytes, {encoding} ({data.AsSpan().SequenceEqual(payload)})"
            + "  (expected True - an untouched string returns its prefix, its payload and which Bech32 variant produced it)");

        Console.WriteLine();
    }
}
