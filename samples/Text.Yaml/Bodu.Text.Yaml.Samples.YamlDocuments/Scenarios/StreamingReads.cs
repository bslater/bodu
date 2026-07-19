// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamingReads.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Serialization;
using Bodu.Text.Yaml;

namespace Bodu.Text.Yaml.Samples.YamlDocuments.Scenarios;

/// <summary>
/// Demonstrates the serializer's stream and buffer facade: read a POCO directly from a
/// <see cref="Stream" /> synchronously and asynchronously (<c>Deserialize</c> / <c>DeserializeAsync</c>),
/// and write UTF-8 YAML straight into an <see cref="IBufferWriter{T}" /> (<c>Serialize</c>). The
/// stream overloads buffer the whole document in memory; only the stream copy itself is async.
/// </summary>
public static class StreamingReads
{
    /// <summary>
    /// The subset of the server document this scenario binds. Unmapped keys (tls, limits) are
    /// skipped by the default <c>UnmappedMemberHandling</c>.
    /// </summary>
    private sealed class ServerConfig
    {
        public string Title { get; set; } = string.Empty;

        public int Workers { get; set; }

        public int DrainTimeout { get; set; }
    }

    /// <summary>
    /// Reads the committed file through the stream overloads, then re-emits it via the buffer writer.
    /// </summary>
    /// <returns>A task that completes when the scenario has run.</returns>
    public static async Task RunAsync()
    {
        Console.WriteLine("--- Stream + IBufferWriter facade: Deserialize(Stream) / Serialize(IBufferWriter) ---");

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "server-config.yaml");
        var options = new YamlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };

        // Synchronous stream read: the stream is buffered in full, then parsed.
        ServerConfig fromSync;
        using (var stream = File.OpenRead(path))
        {
            fromSync = YamlSerializer.Deserialize<ServerConfig>(stream, options)!;
        }

        Console.WriteLine($"Deserialize(Stream)      : {fromSync.Title}, workers {fromSync.Workers}, drain {fromSync.DrainTimeout}");

        // Asynchronous stream read: cancellation applies to the copy, not the parse.
        ServerConfig fromAsync;
        using (var stream = File.OpenRead(path))
        {
            fromAsync = (await YamlSerializer.DeserializeAsync<ServerConfig>(stream, options))!;
        }

        Console.WriteLine($"DeserializeAsync(Stream) : {fromAsync.Title}, workers {fromAsync.Workers}, drain {fromAsync.DrainTimeout} (match: {fromSync.Title == fromAsync.Title})");

        // Serialize straight into a pooled buffer writer - no intermediate string.
        var buffer = new ArrayBufferWriter<byte>();
        YamlSerializer.Serialize(buffer, fromSync, options);

        Console.WriteLine("Serialize(IBufferWriter) :");
        foreach (var line in Encoding.UTF8.GetString(buffer.WrittenSpan).Split('\n').Where(l => l.Length > 0))
        {
            Console.WriteLine($"  | {line.TrimEnd()}");
        }

        Console.WriteLine();
    }
}
