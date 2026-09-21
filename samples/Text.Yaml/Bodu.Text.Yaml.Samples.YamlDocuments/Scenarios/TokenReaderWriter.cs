// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TokenReaderWriter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Samples.YamlDocuments.Scenarios;

/// <summary>
/// Demonstrates the lowest layer: the allocation-free ref structs <see cref="Utf8YamlWriter" />
/// (forward-only token emission into an <see cref="IBufferWriter{T}" />) and
/// <see cref="Utf8YamlReader" /> (forward-only token pull). This is the surface the serializer and
/// both DOMs are built on — reach for it when you need maximum control or minimum overhead.
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
            what: "Emits a mapping with a nested mapping token by token straight into a buffer, prints the "
                + "resulting YAML, then pulls the same bytes back as a token stream reporting each token with "
                + "its decoded value.",
            why: "Both DOMs and the serializer are built on this pair, so everything above it is a policy over "
                + "these tokens. Reaching for it directly pays off when a document is produced or consumed in "
                + "one pass and a tree would be pure overhead. The writer earns its place here more than in "
                + "most formats: YAML block structure is indentation, so emitting it correctly means tracking "
                + "depth, and the common alternative - building the text by hand - is where malformed YAML "
                + "comes from. Both types are ref structs so none of this allocates: the writer appends into a "
                + "caller-owned buffer, and the reader decodes from the input span.",
            expect: "The written tokens produce correctly indented YAML with the nested mapping placed under "
                + "its key, because the writer tracked depth rather than concatenating. Reading it back yields "
                + "a property-name token and a typed value token per entry, with explicit start and end mapping "
                + "tokens - the structure is in the stream, which is what the layers above it build on.");

        // Write: tokens go straight into the buffer, no intermediate tree. A key is written on its
        // own, then the next write supplies its value.
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8YamlWriter(buffer);

        writer.WriteStartMapping();
        writer.WritePropertyName("service");
        writer.WriteString("edge-proxy");
        writer.WritePropertyName("replicas");
        writer.WriteInteger(3);

        writer.WritePropertyName("health");
        writer.WriteStartMapping();
        writer.WritePropertyName("enabled");
        writer.WriteBoolean(true);
        writer.WritePropertyName("threshold");
        writer.WriteDouble(0.75);
        writer.WriteEndMapping();

        writer.WriteEndMapping();

        Console.WriteLine("  emitted (the writer tracked depth, so the nested mapping is indented under its key):");
        foreach (var line in Encoding.UTF8.GetString(buffer.WrittenSpan).Split('\n').Where(l => l.Length > 0))
        {
            Console.WriteLine($"  | {line.TrimEnd()}");
        }

        // Read: pull tokens forward-only; the typed getters decode the scalar at the cursor.
        Console.WriteLine("  tokens (each scalar arrives already typed, and the mapping boundaries are explicit):");
        var reader = new Utf8YamlReader(buffer.WrittenSpan);
        while (reader.Read())
        {
            var detail = reader.TokenType switch
            {
                YamlTokenType.PropertyName or YamlTokenType.String => $" '{reader.GetString()}'",
                YamlTokenType.Integer => $" {reader.GetInt64()}",
                YamlTokenType.Float => $" {reader.GetDouble()}",
                YamlTokenType.Boolean => $" {reader.GetBoolean()}",
                _ => string.Empty,
            };
            Console.WriteLine($"  {reader.TokenType,-14}{detail}");
        }

        Console.WriteLine();
    }
}
