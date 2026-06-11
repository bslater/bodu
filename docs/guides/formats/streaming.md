---
title: Streams and async I/O
---

# Streams and async I/O

`Bodu.Text.Formats` is span-first: every codec's `Parse` / `Format` works over an in-memory `ReadOnlySpan<char>` or `string`. For I/O-bound work, each format additionally exposes **forward-only streaming readers and writers** — `CreateReader` / `CreateWriter` factories over a `TextReader` / `TextWriter` that process one logical unit at a time, with synchronous and asynchronous (`…Async`) members, so a large file never has to materialise as a single in-memory document.

This guide covers the streaming surface. For the synchronous span / document surface of a specific format, see its per-format guide.

## Streaming the text formats

The forward-only readers and writers process one logical unit at a time without materialising the whole document. Each format's façade offers `CreateReader` / `CreateWriter` factory methods:

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

The span `Parse` entry points operate on an in-memory source, so cap the size of an untrusted stream before reading it into memory:

```csharp
const long maxBytes = 16L * 1024 * 1024;

if (stream.CanSeek && stream.Length > maxBytes)
    throw new InvalidOperationException("Payload too large.");
```

For non-seekable streams (network sockets, decompressors), copy at most `maxBytes` into a `MemoryStream` yourself before decoding and parsing. The size limit is a transport-layer concern the parser cannot enforce.

## Where to go next

- **[Using delimited](delimited.md)** — the streaming `DelimitedReader` / `DelimitedWriter` in depth.
- **[Using INI](ini.md)** and **[Using DotEnv](dotenv.md)** — the other streaming readers.
- **[Core concepts](../../docs/formats/concepts.md)** — vocabulary refresher.
