// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamingReads.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml.Samples.TomlDocuments.Scenarios;

/// <summary>
/// Demonstrates streaming reads: when a document arrives in chunks (a socket, a large file), the
/// reader processes each slice as far as it can, reports how many bytes it consumed, and hands
/// its resumable state (<see cref="TomlReaderState" />) to the reader for the next slice —
/// exactly the <c>Utf8JsonReader</c> pattern.
/// </summary>
public static class StreamingReads
{
    /// <summary>
    /// Feeds <c>Data/server-config.toml</c> to the reader in two slices split mid-token.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Streaming - resuming a parse across a slice boundary",
            what: "Splits the document in half at an arbitrary byte, reads the first slice with the final-block "
                + "flag clear, carries the reader state and unconsumed tail into a second read, and compares the "
                + "total token count against parsing the whole document at once.",
            why: "Data from a socket or a large file does not arrive on token boundaries, and the naive answers "
                + "are both bad: buffering the whole document defeats the point of streaming, and parsing each "
                + "chunk independently corrupts any token the split lands inside. The reader's answer is to stop "
                + "at the last complete token, report how far it actually got, and hand back a state the next "
                + "reader resumes from - so the caller re-presents the unconsumed tail rather than the parser "
                + "holding a buffer. It is the same contract as Utf8JsonReader, for the same reason.",
            expect: "The first slice consumes fewer bytes than it was given, which is the partial token being "
                + "held back rather than guessed at. After resuming, the token count equals the one-shot parse "
                + "exactly - that equality is the real assertion, since a reader that merely finishes without "
                + "throwing could still have dropped or duplicated a token at the seam.");

        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "server-config.toml"));

        // Split deliberately mid-document; the first slice ends part-way through a token.
        var splitAt = bytes.Length / 2;
        Console.WriteLine($"  document is {bytes.Length} bytes; slice 1 = {splitAt}, slice 2 = {bytes.Length - splitAt}"
            + "  (split at an arbitrary byte, as a socket read would be - not on a token boundary)");

        var state = new TomlReaderState();
        var tokens = 0;

        // Slice 1: isFinalBlock = false - the reader stops cleanly at the last complete token.
        var reader = new Utf8TomlReader(bytes.AsSpan(0, splitAt), isFinalBlock: false, state);
        while (reader.Read())
        {
            tokens++;
        }

        var consumed = (int)reader.BytesConsumed;
        state = reader.CurrentState;
        Console.WriteLine($"  slice 1: {tokens} tokens, consumed {consumed}/{splitAt} bytes (partial token held back)"
            + "  (fewer bytes consumed than supplied - the caller re-presents the tail, so the parser holds no buffer)");

        // Slice 2: unconsumed tail + the rest, resuming from the captured state.
        var remainder = bytes.AsSpan(consumed);
        reader = new Utf8TomlReader(remainder, isFinalBlock: true, state);
        while (reader.Read())
        {
            tokens++;
        }

        Console.WriteLine($"  slice 2: finished the document - {tokens} tokens total"
            + "  (resumed from the captured state, so the token split across the seam was read exactly once)");

        // The same document in one shot yields the same token count.
        var single = new Utf8TomlReader(bytes);
        var singleTokens = 0;
        while (single.Read())
        {
            singleTokens++;
        }

        Console.WriteLine($"  one-shot parse for comparison: {singleTokens} tokens ({(singleTokens == tokens ? "match" : "MISMATCH")})"
            + "  (the real assertion - finishing without throwing would not prove a token was not dropped or duplicated at the seam)");

        Console.WriteLine();
    }
}
