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
        Console.WriteLine("--- Token layer: Utf8TomlWriter -> bytes -> Utf8TomlReader ---");

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

        Console.WriteLine("emitted:");
        foreach (var line in Encoding.UTF8.GetString(buffer.WrittenSpan).Split('\n').Where(l => l.Length > 0))
        {
            Console.WriteLine($"  | {line.TrimEnd()}");
        }

        // Read: pull tokens forward-only; ValueSpan exposes the raw UTF-8 slice.
        Console.WriteLine("tokens:");
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
