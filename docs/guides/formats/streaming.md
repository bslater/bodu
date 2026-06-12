---
title: Streams and async I/O
---

# Streams and async I/O

`Bodu.Text.Formats` is span-first: every codec's `Parse` / `Format` works over an in-memory `ReadOnlySpan<char>` or `string`. For I/O-bound work, each format additionally exposes **forward-only streaming readers and writers** — `CreateReader` / `CreateWriter` factories over a `TextReader` / `TextWriter` that process one logical unit at a time, with synchronous and asynchronous (`…Async`) members, so a large file never has to materialize as a single in-memory document. The pairs live beside their codecs: `DelimitedReader` / `DelimitedWriter` in <xref:Bodu.Text.Delimited>, `IniReader` / `IniWriter` in <xref:Bodu.Text.Ini>, and `DotEnvReader` / `DotEnvWriter` in <xref:Bodu.Text.DotEnv>.

This guide covers the streaming surface. For the synchronous span / document surface of a specific format, see its per-format guide.

## The streaming surface

Each format's facade offers `CreateReader` / `CreateWriter` factory methods (an options overload selects the dialect):

| Format | Reader advances by | Writer emits |
|---|---|---|
| Delimited (CSV/TSV) | one row — `Read` / `ReadAsync`, fields via `Fields`, header via `Headers` | `WriteHeader` / `WriteRow` (+ `…Async`) |
| INI | one entry — `Read` / `ReadAsync`, exposing `Section` / `Key` / `Value` | `WriteSection` / `WriteEntry` / `WriteComment` (+ `…Async`) |
| DotEnv | one entry — `Read` / `ReadAsync`, exposing `Key` / `Value` | `WriteEntry` / `WriteComment` (+ `…Async`) |

The readers surface raw units in source order: document-level policies that require the whole document — duplicate-key resolution and INI section merging — are applied by the in-memory `Parse` entry points, not by the streaming readers (see [Parser policies](../../docs/formats/parser-policies.md)). Per-line strictness still applies: a malformed unit throws the same `…FormatException` the codec would.

## Pattern 1 — read a large delimited file row by row

```csharp
using System.Globalization;
using Bodu.Text.Delimited;

using var reader = Delimited.CreateReader(File.OpenText("transactions.csv"));

while (reader.Read())
{
    // Fields holds only the current row; process it before reading the next.
    string  id     = reader.Fields[0];
    decimal amount = decimal.Parse(reader.Fields[1], CultureInfo.InvariantCulture);

    Process(id, amount);
}
```

`DelimitedReader` is a forward-only buffered reader (4096-character internal buffer by default; constructor overloads accept another size). After each successful `Read()` the `Fields` property exposes the current row, so memory is bounded by the row width, not the document length. When the options declare `HasHeader: true` (the default), the header row is consumed on the first read and exposed through `Headers`. The reader strips a leading UTF-8 BOM, supports multiline quoted fields, and tracks `LineNumber` (1-based source position) and `RowNumber` (data rows returned so far — the header, blank lines, and comment lines are not counted).

## Pattern 2 — write delimited output row by row

```csharp
using Bodu.Text.Delimited;

using var writer = Delimited.CreateWriter(new StreamWriter("output.csv"));

writer.WriteHeader(["id", "amount"]);
writer.WriteRow(["T-1001", "19.95"]);
writer.WriteRow(["T-1002", "5.10"]);
// writer.RowsWritten → 2

// output.csv:
// id,amount
// T-1001,19.95
// T-1002,5.10
```

`WriteRow` applies RFC 4180 quoting automatically — fields containing the delimiter, the quote character, or a line terminator are emitted with surrounding quotes and embedded quotes doubled. `RowsWritten` counts data rows (the header is not counted).

## Pattern 3 — stream INI without a document

The INI pair is the same shape over sections and entries. The caller controls ordering on write — emit any global entries first, then each `WriteSection` followed by that section's entries:

```csharp
using Bodu.Text.Ini;

using (var writer = Ini.CreateWriter(File.CreateText("config.ini")))
{
    writer.WriteComment(" generated", '#');
    writer.WriteSection("database");
    writer.WriteEntry("host", "localhost");
    writer.WriteEntry("port", "5432");
}

using (var reader = Ini.CreateReader(File.OpenText("config.ini")))
{
    while (reader.Read())
        Console.WriteLine($"[{reader.Section}] {reader.Key} = {reader.Value}");
}
// → [database] host = localhost
// → [database] port = 5432
```

`IniReader.Read` skips comments and blank lines and yields one key/value entry per call; `Section` reports the section the entry belongs to (`string.Empty` for a global entry before the first header). Note that section headers themselves do not produce an entry — an empty section is invisible to the streaming reader.

## Pattern 4 — stream a DotEnv source

```csharp
using Bodu.Text.DotEnv;

using var reader = DotEnv.CreateReader(File.OpenText("app.env"));

while (reader.Read())
    Console.WriteLine($"{reader.Key} = {reader.Value} (line {reader.LineNumber})");
```

`DotEnvReader` parses incrementally even across the embedded newlines and `\`-continuations a double-quoted value may contain, exposing each `KEY=VALUE` entry through `Key` / `Value` with the 1-based `LineNumber`. `DotEnvWriter` mirrors the codec's `Format` output with `WriteEntry(key, value)` and `WriteComment(text)`.

## Pattern 5 — async reads and writes with cancellation

Every reader has `ReadAsync(CancellationToken)` and every writer has `…Async` counterparts (`WriteRowAsync`, `WriteEntryAsync`, `WriteSectionAsync`, …) that accept a `CancellationToken`:

```csharp
using Bodu.Text.Delimited;

using var reader = Delimited.CreateReader(File.OpenText("large.csv"));

while (await reader.ReadAsync(cancellationToken))
{
    Process(reader.Fields);
}
```

One asymmetry to know about:

| Concern | Synchronous | Asynchronous |
|---|---|---|
| Delimited read | Incremental — refills the 4096-character buffer on demand. | On the **first** `ReadAsync` call, the remainder of the source is drained into memory asynchronously; subsequent parsing performs no further I/O. |
| INI / DotEnv read | Line at a time. | Line at a time (`ReadLineAsync`), honoring the token per line. |
| Writers | Each `Write…` writes immediately. | Each `Write…Async` writes immediately, honoring the token. |

Because `DelimitedReader.ReadAsync` switches the instance to the drained in-memory source, do not interleave `Read` and `ReadAsync` on one instance — choose one access pattern per reader. For a delimited source that genuinely must not be buffered in full, use the synchronous reader.

## Lifetime and ownership

All streaming readers and writers **take ownership of the supplied `TextReader` / `TextWriter`** and dispose it when their own `Dispose` runs — one `using` over the reader/writer is sufficient; do not double-dispose or keep using the inner stream afterwards:

```csharp
// The StreamWriter (and its FileStream) are closed by the DelimitedWriter's Dispose.
using var writer = Delimited.CreateWriter(new StreamWriter("output.csv"));
```

Disposing a writer flushes any output buffered by the underlying `TextWriter`. Calling any member after disposal throws `ObjectDisposedException`. If you need the underlying stream to outlive the format reader/writer, hand in a wrapper you control (for example a `StreamWriter` constructed with `leaveOpen: true`).

## Mid-stream errors

A streaming reader validates each unit as it is consumed, so a malformed source fails at the offending unit — *after* earlier units have already been returned. Anything your loop processed before the throw has been delivered and will not be re-read; resuming the same reader after a format exception is not supported.

```csharp
using Bodu.Text.Ini;

using var reader = Ini.CreateReader(File.OpenText("config.ini"));

try
{
    while (reader.Read())
        Apply(reader.Section, reader.Key, reader.Value);
}
catch (IniFormatException ex)
{
    // Entries before the malformed line were already applied.
    log.Warn($"Stopped at line {ex.LineNumber}: {ex.Message}");
}
```

What each reader throws, with the 1-based line number on the exception:

| Reader | Throws | When |
|---|---|---|
| `DelimitedReader` | `DelimitedFormatException` | An unterminated quoted field (e.g. *"Unterminated quoted field starting on line 2."*). |
| `IniReader` | `IniFormatException` | A malformed section header, an empty key, or a global key while `AllowGlobalSection` is disabled. |
| `DotEnvReader` | `DotEnvFormatException` | A malformed `KEY=VALUE` line, invalid key syntax, or an unterminated quoted value. |

If a partially applied document is unacceptable, buffer the source and use the codec's `Parse` / `TryParse` instead — the in-memory path validates the whole document before you observe any of it.

## Pattern 6 — limit input size

The span `Parse` entry points operate on an in-memory source, so cap the size of an untrusted stream before reading it into memory:

```csharp
const long maxBytes = 16L * 1024 * 1024;

if (stream.CanSeek && stream.Length > maxBytes)
    throw new InvalidOperationException("Payload too large.");
```

For non-seekable streams (network sockets, decompressors), copy at most `maxBytes` into a `MemoryStream` yourself before decoding and parsing. The size limit is a transport-layer concern the parser cannot enforce. The same caution applies to `DelimitedReader.ReadAsync`, which drains its source into memory on first use.

## See also

- [Using delimited](delimited.md) — the `DelimitedReader` / `DelimitedWriter` dialect options in depth.
- [Using INI](ini.md) and [Using DotEnv](dotenv.md) — the other streaming readers and their document models.
- [Parser policies](../../docs/formats/parser-policies.md) — the document-level policies the streaming readers deliberately do not apply.
- [Core concepts](../../docs/formats/concepts.md) — vocabulary refresher.
- [Text & Serialization guides](../topics/text-and-serialization.md) and the [topic overview](../../docs/topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Delimited>, <xref:Bodu.Text.Ini>, <xref:Bodu.Text.DotEnv>.
