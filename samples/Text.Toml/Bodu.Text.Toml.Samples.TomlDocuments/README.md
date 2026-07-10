# Bodu.Text.Toml.Samples.TomlDocuments

The layers beneath `TomlSerializer`, mirroring the `System.Text.Json` stack: the mutable
`TomlNode` DOM (like `JsonNode`), the read-only `TomlDocument` DOM (like `JsonDocument`), and
the allocation-free `Utf8TomlWriter`/`Utf8TomlReader` token surface (like `Utf8JsonWriter`/
`Utf8JsonReader`) — including streaming reads across buffer boundaries. Pick the layer that
matches the job: serializer for typed graphs, node DOM for edit-in-place, document DOM for
inspection, token layer for control.

Everything runs offline against the committed `Data/server-config.toml`.

```bash
dotnet run --project samples/Text.Toml/Bodu.Text.Toml.Samples.TomlDocuments
```

## Scenario 1 — MutableDom

**Intent.** Show the `JsonNode`-style workflow: when you need to read *and rewrite* a TOML
document without defining a POCO — a config editor, a migration script, a tool that grafts
sections into existing files.

**What it does.** Parses `Data/server-config.toml` into a `TomlNode` tree with
`TomlNode.Parse`, reads leaves through chained indexers plus `GetValue<T>()`, then edits the
tree three ways: overwrites `workers` with a new `TomlValue`, and builds a whole `[logging]`
table bottom-up (a `TomlObject` with a string leaf and a `TomlArray` of two strings) and
grafts it onto the root with `Add`. Finally it re-emits the edited tree as TOML text via
`ToUtf8Bytes()`.

**What to expect.** The two original values read back, then the full emitted document showing
all three edits — `workers = 16`, and the appended `[logging]` table with its inline array:

```text
title            : edge-proxy
tls.certificate  : certs/edge.pem
edited document  :
  | title = "edge-proxy"
  | workers = 16
  ...
  | [logging]
  | level = "warning"
  | sinks = ["console", "file"]
```

**APIs demonstrated.** `TomlNode.Parse(ReadOnlySpan<byte>)`, node indexers
(`root["tls"]!["certificate"]`), `TomlNode.GetValue<T>()`, `TomlValue.Create`,
`TomlObject` collection initializers and `Add`, `TomlArray(params)`, `TomlNode.ToUtf8Bytes()`.

## Scenario 2 — ReadOnlyDom

**Intent.** Show the `JsonDocument`-style workflow: one parse, then cheap struct
`TomlElement` cursors over the parsed data — the right layer when you only need to *inspect*
a document (feature flags, tool config probes) and want neither a POCO nor a mutable tree.
The document owns the parsed data, hence `using`.

**What it does.** Parses the same file with `TomlDocument.Parse`, drills down with
`GetProperty` chains and reads leaves with the typed getters (`GetString`, `GetInt64`,
`GetTimeOnly`, `GetBoolean`). It then enumerates the `[limits]` table with
`EnumerateObject()` — no knowledge of its keys required, each property exposing `Name`,
`Value`, and `ValueKind` — and probes for an absent `proxy` key with `TryGetProperty`
instead of catching an exception.

**What to expect.** The four typed leaves (note `drain_timeout` arriving as a real
`TimeOnly`, printed `00:00:30`), the two enumerated limit entries tagged `Integer`, and a
`False` for the optional-key probe:

```text
title         : edge-proxy
workers       : 8
drain_timeout : 00:00:30
tls.enabled   : True
limits        :
  max_connections = 10000 (Integer)
  max_body_bytes = 1048576 (Integer)
proxy present : False
```

**APIs demonstrated.** `TomlDocument.Parse(string)` (+ `IDisposable`),
`TomlDocument.RootElement`, `TomlElement.GetProperty` / `TryGetProperty`, typed getters,
`TomlElement.EnumerateObject()`, `TomlProperty.Name` / `.Value`, `TomlElement.ValueKind`.

## Scenario 3 — TokenReaderWriter

**Intent.** Expose the lowest layer both DOMs and the serializer are built on: forward-only
token emission and pulling over raw UTF-8, with no intermediate tree and no allocation. This
is the layer for custom emitters, format converters, and hot paths.

**What it does.** Constructs a `Utf8TomlWriter` over an `ArrayBufferWriter<byte>` and emits a
document token by token — root scalars via the `(name, value)` convenience overloads, then a
nested `[health]` table via `WriteStartTable("health")` … `WriteEndTable()`, and `Flush()`.
It prints the emitted TOML, then walks the same bytes with `Utf8TomlReader`, printing each
`TokenType` and, for keys/strings/integers, the decoded value straight from `ValueSpan`.

**What to expect.** The five emitted lines, then the token stream — note the shape: each
`key = value` pair surfaces as a `Key` token followed by a value token, and the table header
surfaces as `TableHeader` followed by the `Key` carrying its name:

```text
emitted:
  | service = "edge-proxy"
  | replicas = 3
  | [health]
  | enabled = true
  | probe_at = 04:15:00
tokens:
  Key            'service'
  String         'edge-proxy'
  Key            'replicas'
  Integer        3
  TableHeader
  Key            'health'
  Key            'enabled'
  Boolean
  Key            'probe_at'
  LocalTime
```

**APIs demonstrated.** `Utf8TomlWriter(IBufferWriter<byte>)`, `WriteStartTable()` /
`WriteStartTable(name)` / `WriteEndTable()`, the `(name, value)` scalar overloads,
`WriteLocalTime`, `Flush()`; `Utf8TomlReader(ReadOnlySpan<byte>)`, `Read()`, `TokenType`,
`ValueSpan`, `GetInt64()`.

## Scenario 4 — StreamingReads

**Intent.** Show how to parse TOML that arrives in chunks — a socket, a pipeline, a file too
large to buffer — using the `Utf8JsonReader` resumable-state pattern: the reader consumes
each slice as far as it can, holds back any partial token, and resumes from a captured
`TomlReaderState`.

**What it does.** Splits the committed document in half deliberately mid-token. It reads
slice 1 with `isFinalBlock: false`, counting tokens; the reader stops at the last *complete*
token, and `BytesConsumed` reveals it left the partial token unconsumed. The scenario
captures `CurrentState`, then constructs a new reader over the unconsumed tail plus the rest
with `isFinalBlock: true` and finishes the count. A one-shot parse of the whole document
confirms the streamed pass saw exactly the same token stream.

**What to expect.** Slice 1 consuming slightly less than its 104 bytes (the held-back
partial token), and the streamed total matching the one-shot total:

```text
document is 208 bytes; slice 1 = 104, slice 2 = 104
slice 1: 9 tokens, consumed 102/104 bytes (partial token held back)
slice 2: finished the document - 19 tokens total
one-shot parse for comparison: 19 tokens (match)
```

**APIs demonstrated.** `TomlReaderState`, `Utf8TomlReader(span, isFinalBlock, state)`,
`Utf8TomlReader.BytesConsumed`, `Utf8TomlReader.CurrentState`, resuming across buffer
boundaries.

## Layout

```text
Bodu.Text.Toml.Samples.TomlDocuments/
  Program.cs                      # runs the scenarios in order
  Data/server-config.toml         # the committed input document
  Scenarios/MutableDom.cs
  Scenarios/ReadOnlyDom.cs
  Scenarios/TokenReaderWriter.cs
  Scenarios/StreamingReads.cs
```

## Related

- `Bodu.Text.Toml.Samples.TomlBasics` — the `TomlSerializer` POCO surface above these layers.
- Guides: `docs/guides/serialization/toml/`.
