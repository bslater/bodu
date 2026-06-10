---
title: Streams and async I/O
---

# Streams and async I/O

`Bodu.Text.Formats` offers two ways to work with a `Stream`:

- **Buffered codec overloads.** Every codec's `Parse` / `Format` (or `Decode` / `Encode`) has `Stream` and async `Stream` overloads that read or write the whole document through a pooled buffer. These are convenience wrappers around the span path.
- **Forward-only streaming readers / writers.** The line- and record-oriented formats (Delimited, INI, DotEnv) additionally expose `CreateReader` / `CreateWriter` factories that process one logical unit at a time, so a large file never has to materialise as a single in-memory document.

This guide covers both. For the synchronous span / document surface of a specific format, see its per-format guide.

## Buffered stream overloads

The document codecs buffer the whole payload. For TOML, for example:

```csharp
using Bodu.Text.Toml;

// Read a UTF-8 document from a stream.
await using FileStream fs = File.OpenRead("config.toml");
TomlTable config = await Toml.ParseAsync(fs, cancellationToken);

// Write canonical TOML back to a stream.
await using FileStream outFs = File.Create("config.out.toml");
await Toml.FormatAsync(config, outFs, cancellationToken);
```

These overloads:

- Throw `ArgumentNullException` for a `null` stream or value.
- Throw `ArgumentException` if the stream does not support the required direction.
- Throw a format-specific `*FormatException` (deriving from <xref:Bodu.Text.TextFormatException>) for any structural violation on the read path.
- Throw `OperationCanceledException` if `cancellationToken` is signalled during the async copy.
- **Do not close the stream** — lifetime is the caller's responsibility.

The buffered read path stages the source into a pooled buffer (returned to `ArrayPool<byte>.Shared` before the method returns), then dispatches to the span parser. The parsed model owns its own copies and is safe to keep beyond the call.

## Streaming the text formats

Unlike the buffered codecs, the line- and record-oriented formats expose forward-only readers and writers that process one logical unit at a time without materialising the whole document. Each format's façade offers `CreateReader` / `CreateWriter` factory methods:

| Format | Reader | Writer |
|---|---|---|
| Delimited (CSV/TSV) | `DelimitedReader.Read` / `ReadAsync` (one row) | `DelimitedWriter.WriteHeader` / `WriteRow` (+ `…Async`) |
| INI | `IniReader.Read` / `ReadAsync` (one entry, exposing `Section`/`Key`/`Value`) | `IniWriter.WriteSection` / `WriteEntry` / `WriteComment` (+ `…Async`) |
| DotEnv | `DotEnvReader.Read` / `ReadAsync` (one entry) | `DotEnvWriter.WriteEntry` / `WriteComment` (+ `…Async`) |

```csharp
using var reader = Ini.CreateReader(File.OpenText("config.ini"));
while (reader.Read())
    Console.WriteLine($"[{reader.Section}] {reader.Key} = {reader.Value}");
```

These readers surface raw units in source order: document-level policies that require the whole document — duplicate-key resolution and INI section merging — are applied by the in-memory `Parse` entry points, not by the streaming readers. `DotEnvReader` parses incrementally even across the embedded newlines and `\`-continuations a double-quoted value may contain; `DelimitedReader.ReadAsync` drains its source asynchronously before parsing (do not interleave `Read` and `ReadAsync` on one instance). All readers and writers take ownership of the supplied `TextReader` / `TextWriter` and dispose it.

## Pattern — stream a large delimited file

```csharp
using Bodu.Text.Delimited;

await using FileStream input = File.OpenRead("large.csv");
using var reader = Delimited.CreateReader(new StreamReader(input));

while (await reader.ReadAsync(cancellationToken))
{
    // reader exposes the current row's fields; process and discard before reading the next.
    Process(reader.Current);
}
```

The streaming reader holds only the current row in memory, so file size is bounded by the row width, not the document length.

## Pattern — limit input size

The buffered `Parse(Stream)` overloads read to end of stream. For untrusted streams, cap the size before calling:

```csharp
const long maxBytes = 16L * 1024 * 1024;

if (stream.CanSeek && stream.Length > maxBytes)
    throw new InvalidOperationException("Payload too large.");
```

For non-seekable streams (network sockets, decompressors), copy at most `maxBytes` into a `MemoryStream` yourself, then pass that to `Parse`. The size limit is a transport-layer concern the parser cannot enforce.

## Where to go next

- **[Using delimited](delimited.md)** — the streaming `DelimitedReader` / `DelimitedWriter` in depth.
- **[Using INI](ini.md)** and **[Using DotEnv](dotenv.md)** — the other streaming readers.
- **[Core concepts](../../docs/formats/concepts.md)** — vocabulary refresher.
