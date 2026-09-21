// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TokenReaderWriter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Samples.TomlDocuments.Scenarios;

/// <summary>
/// Demonstrates the lowest layer: the allocation-free ref structs <see cref="Utf8TomlWriter" />
/// (forward-only token emission into an <see cref="IBufferWriter{T}" />) and
/// <see cref="Utf8TomlReader" /> (forward-only token pull). This is the surface the serializer
/// and both DOMs are built on — reach for it when you need maximum control or minimum overhead.
/// </summary>
public static class TokenReaderWriter
{
    /// <summary>
    /// Writes a document token by token, then reads the emitted bytes back as a token stream.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "The token layer - writing and reading without a tree",
            what: "Emits a document token by token straight into a buffer, prints the resulting TOML, then pulls "
                + "the same bytes back as a token stream and reports each token with its value.",
            why: "Both DOMs and the serializer are built on this pair, so everything above it can be understood "
                + "as a policy over these tokens. Reaching for it directly is worth it when a document is "
                + "produced or consumed in one pass and a tree would be pure overhead - emitting a large export, "
                + "or scanning for a handful of keys in a file you will not otherwise use. The reader and writer "
                + "are ref structs precisely so this costs no allocation: the writer appends into a caller-owned "
                + "buffer and the reader exposes each value as a slice of the input rather than as a new string.",
            expect: "The written tokens produce valid TOML with the nested table placed correctly, so the writer "
                + "tracks structure rather than just concatenating. Reading it back yields a key token and a "
                + "value token per entry, with a TableHeader token followed by the table's name marking where "
                + "[health] begins - TOML tables run to the next header rather than being closed, so there is no "
                + "end token to emit. The structure is in the stream, which is what lets the layers above it "
                + "build whatever shape they want.");

        // Write: tokens go straight into the buffer, no intermediate tree.
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8TomlWriter(buffer);

        writer.WriteStartTable();
        writer.WriteString("service", "edge-proxy");
        writer.WriteInteger("replicas", 3);

        writer.WriteStartTable("health");
        writer.WriteBoolean("enabled", true);
        writer.WriteLocalTime("probe_at", new TimeOnly(4, 15));
        writer.WriteEndTable();

        writer.WriteEndTable();
        writer.Flush();

        Console.WriteLine("  emitted (the writer tracked the nesting, so [health] lands in the right place):");
        foreach (var line in Encoding.UTF8.GetString(buffer.WrittenSpan).Split('\n').Where(l => l.Length > 0))
        {
            Console.WriteLine($"  | {line.TrimEnd()}");
        }

        // Read: pull tokens forward-only; ValueSpan exposes the raw UTF-8 slice.
        Console.WriteLine("  tokens (structure is explicit in the stream, which is what the DOMs and the serializer build on):");
        var reader = new Utf8TomlReader(buffer.WrittenSpan);
        while (reader.Read())
        {
            var detail = reader.TokenType switch
            {
                TomlTokenType.Key or TomlTokenType.String =>
                    $" '{Encoding.UTF8.GetString(reader.ValueSpan)}'",
                TomlTokenType.Integer => $" {reader.GetInt64()}",
                _ => string.Empty,
            };
            Console.WriteLine($"  {reader.TokenType,-14}{detail}");
        }

        Console.WriteLine();
    }
}
