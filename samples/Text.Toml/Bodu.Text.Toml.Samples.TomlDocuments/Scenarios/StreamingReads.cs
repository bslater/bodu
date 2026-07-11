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
        Console.WriteLine("--- Streaming: two slices, one resumable TomlReaderState ---");

        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "server-config.toml"));

        // Split deliberately mid-document; the first slice ends part-way through a token.
        var splitAt = bytes.Length / 2;
        Console.WriteLine($"document is {bytes.Length} bytes; slice 1 = {splitAt}, slice 2 = {bytes.Length - splitAt}");

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
        Console.WriteLine($"slice 1: {tokens} tokens, consumed {consumed}/{splitAt} bytes (partial token held back)");

        // Slice 2: unconsumed tail + the rest, resuming from the captured state.
        var remainder = bytes.AsSpan(consumed);
        reader = new Utf8TomlReader(remainder, isFinalBlock: true, state);
        while (reader.Read())
        {
            tokens++;
        }

        Console.WriteLine($"slice 2: finished the document - {tokens} tokens total");

        // The same document in one shot yields the same token count.
        var single = new Utf8TomlReader(bytes);
        var singleTokens = 0;
        while (single.Read())
        {
            singleTokens++;
        }

        Console.WriteLine($"one-shot parse for comparison: {singleTokens} tokens ({(singleTokens == tokens ? "match" : "MISMATCH")})");

        Console.WriteLine();
    }
}
