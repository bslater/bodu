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
--- The mutable DOM - editing a document without a POCO ---
  What   : Parses the config file into a node tree, reads two values through indexers, overwrites a leaf, grafts a
           whole new table built from nodes, and emits the edited tree as TOML.
  Why    : A typed model is the right answer when the shape is known and stable. This layer is for the cases where
           it is not: a tool that edits one key in whatever file it is handed, a migration that adds a section to
           documents it does not otherwise understand, a test fixture built programmatically. Defining a POCO for
           those means enumerating a schema you do not care about and that will reject the next file. The tree is
           mutable and self-describing instead, and the cost is that nothing validates the shape - which is the
           right trade only when you genuinely do not know it.
  Expect : The edited value and the grafted table both appear in the emitted document, and the parts that were not
           touched come through unchanged. The new table emits as a proper [logging] section with its array intact,
           so building a tree by hand produces the same wire form as parsing one.

  title            : edge-proxy  (each indexer hop returns a node; GetValue<T> unwraps the leaf to a CLR value)
  tls.certificate  : certs/edge.pem  (two hops into a nested table, with no type declared anywhere for its shape)
  edited document  (workers overwritten, [logging] grafted in, everything else untouched):
  | title = "edge-proxy"
  | workers = 16
  | drain_timeout = 00:00:30
  | [tls]
  | enabled = true
  | certificate = "certs/edge.pem"
  | [limits]
  | max_connections = 10000
  | max_body_bytes = 1048576
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
--- The read-only DOM - inspecting without materializing ---
  What   : Parses the document once, reads four typed leaves including a nested one, enumerates a table whose keys
           are not known in advance, and probes for an optional key that is not there.
  Why    : This is the cheapest way to read a document you are not going to change. One parse produces one buffer,
           and the elements are cursors into it rather than objects built from it - so walking the tree allocates
           nothing further, and reading two keys out of a large document does not cost the whole document. Because
           the buffer is owned rather than borrowed the document is disposable, which is the one thing to remember
           about this layer. TryGetProperty exists because optional keys are the normal case in configuration, and
           exceptions are the wrong mechanism for something expected.
  Expect : The typed getters return CLR values rather than strings, including the local time, which TOML models
           natively. Enumerating the table reports each value's kind alongside it, so a consumer can branch on shape
           without a schema. The absent key reports False instead of throwing - the probe is what optional keys
           should use.

  title         : edge-proxy
  workers       : 8
  drain_timeout : 00:00:30  (a native TOML local time, returned as a TimeOnly rather than as text to be re-parsed)
  tls.enabled   : True  (each GetProperty is a cursor move within the one parsed buffer, not a new object)
  limits        :
    max_connections = 10000 (Integer)
    max_body_bytes = 1048576 (Integer)
  (each value reports its kind, so a consumer can branch on shape without a schema)
  proxy present : False  (expected False - optional keys are the normal case in config, so probing beats catching)
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
--- The token layer - writing and reading without a tree ---
  What   : Emits a document token by token straight into a buffer, prints the resulting TOML, then pulls the same
           bytes back as a token stream and reports each token with its value.
  Why    : Both DOMs and the serializer are built on this pair, so everything above it can be understood as a policy
           over these tokens. Reaching for it directly is worth it when a document is produced or consumed in one
           pass and a tree would be pure overhead - emitting a large export, or scanning for a handful of keys in a
           file you will not otherwise use. The reader and writer are ref structs precisely so this costs no
           allocation: the writer appends into a caller-owned buffer and the reader exposes each value as a slice of
           the input rather than as a new string.
  Expect : The written tokens produce valid TOML with the nested table placed correctly, so the writer tracks
           structure rather than just concatenating. Reading it back yields a key token and a value token per entry,
           with a TableHeader token followed by the table's name marking where [health] begins - TOML tables run to
           the next header rather than being closed, so there is no end token to emit. The structure is in the
           stream, which is what lets the layers above it build whatever shape they want.

  emitted (the writer tracked the nesting, so [health] lands in the right place):
  | service = "edge-proxy"
  | replicas = 3
  | [health]
  | enabled = true
  | probe_at = 04:15:00
  tokens (structure is explicit in the stream, which is what the DOMs and the serializer build on):
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
--- Streaming - resuming a parse across a slice boundary ---
  What   : Splits the document in half at an arbitrary byte, reads the first slice with the final-block flag clear,
           carries the reader state and unconsumed tail into a second read, and compares the total token count
           against parsing the whole document at once.
  Why    : Data from a socket or a large file does not arrive on token boundaries, and the naive answers are both
           bad: buffering the whole document defeats the point of streaming, and parsing each chunk independently
           corrupts any token the split lands inside. The reader's answer is to stop at the last complete token,
           report how far it actually got, and hand back a state the next reader resumes from - so the caller
           re-presents the unconsumed tail rather than the parser holding a buffer. It is the same contract as
           Utf8JsonReader, for the same reason.
  Expect : The first slice consumes fewer bytes than it was given, which is the partial token being held back rather
           than guessed at. After resuming, the token count equals the one-shot parse exactly - that equality is the
           real assertion, since a reader that merely finishes without throwing could still have dropped or
           duplicated a token at the seam.

  document is 208 bytes; slice 1 = 104, slice 2 = 104  (split at an arbitrary byte, as a socket read would be - not on a token boundary)
  slice 1: 9 tokens, consumed 102/104 bytes (partial token held back)  (fewer bytes consumed than supplied - the caller re-presents the tail, so the parser holds no buffer)
  slice 2: finished the document - 19 tokens total  (resumed from the captured state, so the token split across the seam was read exactly once)
  one-shot parse for comparison: 19 tokens (match)  (the real assertion - finishing without throwing would not prove a token was not dropped or duplicated at the seam)
```

**APIs demonstrated.** `TomlReaderState`, `Utf8TomlReader(span, isFinalBlock, state)`,
`Utf8TomlReader.BytesConsumed`, `Utf8TomlReader.CurrentState`, resuming across buffer
boundaries.

## Layout

```text
Bodu.Text.Toml.Samples.TomlDocuments/
  Program.cs                      # runs the scenarios in order
  SampleConsole.cs                # the What / Why / Expect scenario banner
  Data/server-config.toml         # the committed input document
  Scenarios/MutableDom.cs
  Scenarios/ReadOnlyDom.cs
  Scenarios/TokenReaderWriter.cs
  Scenarios/StreamingReads.cs
```

## Related

- `Bodu.Text.Toml.Samples.TomlBasics` — the `TomlSerializer` POCO surface above these layers.
- Guides: `docs/guides/serialization/toml/`.
