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
        SampleConsole.Scenario(
            "The stream and buffer-writer facade",
            what: "Reads the committed file into a POCO through the synchronous stream overload and again "
                + "through the asynchronous one, compares the results, then serializes straight into a buffer "
                + "writer rather than into a string.",
            why: "Two things worth being explicit about. The POCO here binds only three of the document keys, "
                + "and the rest are skipped rather than rejected - the default unmapped-member handling, which "
                + "is what lets a consumer bind the part of a shared config file it cares about without owning "
                + "the whole schema. The other is the honest shape of the async overload: it buffers the "
                + "document in full and only the stream copy is asynchronous, because YAML block structure "
                + "cannot be resolved without the surrounding indentation. Saying so matters, since a caller "
                + "who assumes incremental parsing would size a document by what the parser can stream rather "
                + "than by what fits in memory.",
            expect: "Both stream overloads produce the same values from the same file, and the unmapped keys "
                + "cause no error. Serializing back emits only the three bound members - the keys this POCO "
                + "never saw are gone, which is the round-trip caveat of binding a subset: the object, not the "
                + "source document, is what gets written.");

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "server-config.yaml");
        var options = new YamlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };

        // Synchronous stream read: the stream is buffered in full, then parsed.
        ServerConfig fromSync;
        using (var stream = File.OpenRead(path))
        {
            fromSync = YamlSerializer.Deserialize<ServerConfig>(stream, options)!;
        }

        Console.WriteLine($"  Deserialize(Stream)      : {fromSync.Title}, workers {fromSync.Workers}, drain {fromSync.DrainTimeout}"
            + "  (the tls and limits keys are in the file but not on this POCO - unmapped members are skipped, not rejected)");

        // Asynchronous stream read: cancellation applies to the copy, not the parse.
        ServerConfig fromAsync;
        using (var stream = File.OpenRead(path))
        {
            fromAsync = (await YamlSerializer.DeserializeAsync<ServerConfig>(stream, options))!;
        }

        Console.WriteLine($"  DeserializeAsync(Stream) : {fromAsync.Title}, workers {fromAsync.Workers}, drain {fromAsync.DrainTimeout} (match: {fromSync.Title == fromAsync.Title})"
            + "  (the document is buffered in full either way - only the stream copy is asynchronous, so do not size input by this)");

        // Serialize straight into a pooled buffer writer - no intermediate string.
        var buffer = new ArrayBufferWriter<byte>();
        YamlSerializer.Serialize(buffer, fromSync, options);

        Console.WriteLine("  Serialize(IBufferWriter) - straight into a pooled buffer, with no intermediate string:");
        foreach (var line in Encoding.UTF8.GetString(buffer.WrittenSpan).Split('\n').Where(l => l.Length > 0))
        {
            Console.WriteLine($"  | {line.TrimEnd()}");
        }

        Console.WriteLine("  (only the three bound members come back - binding a subset means the object, not the source document, is what gets written)");

        Console.WriteLine();
    }
}
