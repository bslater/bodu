# Bodu.Text.Yaml.Samples.YamlDocuments

The layers beneath `YamlSerializer`, mirroring the `System.Text.Json` stack: the allocation-free
`Utf8YamlWriter`/`Utf8YamlReader` token surface (like `Utf8JsonWriter`/`Utf8JsonReader`), the
mutable `YamlNode` DOM (like `JsonNode`), the read-only `YamlDocument` DOM (like `JsonDocument`),
and the serializer's stream/buffer facade. Pick the layer that matches the job: token layer for
control, node DOM for edit-in-place, document DOM for inspection, serializer for typed graphs.

Everything runs offline against the committed `Data/server-config.yaml`.

```bash
dotnet run --project samples/Text.Yaml/Bodu.Text.Yaml.Samples.YamlDocuments
```

## Scenario 1 — TokenReaderWriter

**Intent.** Expose the lowest layer both DOMs and the serializer are built on: forward-only
token emission and pulling over raw UTF-8, with no intermediate tree and no allocation. This is
the layer for custom emitters, format converters, and hot paths.

**What it does.** Constructs a `Utf8YamlWriter` over an `ArrayBufferWriter<byte>` and emits a
document token by token — a key via `WritePropertyName` followed by its value, and a nested
`health` mapping via `WriteStartMapping` … `WriteEndMapping`. It prints the emitted YAML, then
walks the same bytes with `Utf8YamlReader`, printing each `TokenType` and, for property
names and scalars, the decoded value from the typed getters.

**What to expect.** The five emitted lines, then the token stream — note the shape: each
`key: value` pair surfaces as a `PropertyName` token followed by a value token, and each
mapping is bracketed by `StartMapping` / `EndMapping`:

```text
--- The token layer - writing and reading without a tree ---
  What   : Emits a mapping with a nested mapping token by token straight into a buffer, prints the resulting YAML,
           then pulls the same bytes back as a token stream reporting each token with its decoded value.
  Why    : Both DOMs and the serializer are built on this pair, so everything above it is a policy over these
           tokens. Reaching for it directly pays off when a document is produced or consumed in one pass and a tree
           would be pure overhead. The writer earns its place here more than in most formats: YAML block structure
           is indentation, so emitting it correctly means tracking depth, and the common alternative - building the
           text by hand - is where malformed YAML comes from. Both types are ref structs so none of this allocates:
           the writer appends into a caller-owned buffer, and the reader decodes from the input span.
  Expect : The written tokens produce correctly indented YAML with the nested mapping placed under its key, because
           the writer tracked depth rather than concatenating. Reading it back yields a property-name token and a
           typed value token per entry, with explicit start and end mapping tokens - the structure is in the stream,
           which is what the layers above it build on.

  emitted (the writer tracked depth, so the nested mapping is indented under its key):
  | service: edge-proxy
  | replicas: 3
  | health:
  |   enabled: true
  |   threshold: 0.75
  tokens (each scalar arrives already typed, and the mapping boundaries are explicit):
  StartMapping  
  PropertyName   'service'
  String         'edge-proxy'
  PropertyName   'replicas'
  Integer        3
  PropertyName   'health'
  StartMapping  
  PropertyName   'enabled'
  Boolean        True
  PropertyName   'threshold'
  Float          0.75
  EndMapping    
  EndMapping    
```

**APIs demonstrated.** `Utf8YamlWriter(IBufferWriter<byte>)`, `WriteStartMapping()` /
`WriteEndMapping()`, `WritePropertyName(name)`, `WriteString` / `WriteInteger` / `WriteBoolean`
/ `WriteDouble`; `Utf8YamlReader(ReadOnlySpan<byte>)`, `Read()`, `TokenType`, `GetString()`,
`GetInt64()`, `GetBoolean()`, `GetDouble()`.

## Scenario 2 — MutableDom

**Intent.** Show the `JsonNode`-style workflow: when you need to read *and rewrite* a YAML
document without defining a POCO — a config editor, a migration script, a tool that grafts
sections into existing files.

**What it does.** Parses `Data/server-config.yaml` into a `YamlNode` tree with `YamlNode.Parse`,
reads leaves through chained indexers plus `AsValue().GetValue<T>()`, then edits the tree three
ways: overwrites `workers` with a new `YamlValue`, and builds a whole `logging` mapping
bottom-up (a `YamlObject` with a string leaf and a `YamlArray` of two strings) and grafts it onto
the root with `Add`. Finally it re-emits the edited tree as YAML text via `ToYamlString()`.

**What to expect.** The two original values read back, then the full emitted document showing
all three edits — `workers: 16`, and the appended `logging` mapping with its nested sequence:

```text
--- The mutable DOM - editing a document without a POCO ---
  What   : Parses the config file into a node tree, reads two values through indexers, overwrites a leaf, grafts a
           whole new mapping built from nodes, and emits the edited tree as YAML.
  Why    : A typed model is right when the shape is known and stable. This layer is for when it is not: a tool that
           edits one key in whatever file it is handed, a migration that adds a section to documents it does not
           otherwise understand, a fixture built programmatically. Declaring a POCO for those means enumerating a
           schema you do not care about and that the next file will violate. The tree is mutable and self-describing
           instead, and the cost is that nothing validates the shape - the right trade only when you genuinely do
           not know it.
  Expect : The edited value and the grafted mapping both appear in the emitted document, with the untouched parts
           unchanged. The new mapping emits with correct block indentation and its sequence intact, so a tree built
           by hand produces the same wire form as a parsed one.

  title           : edge-proxy  (each indexer hop returns a node; GetValue<T> unwraps the leaf to a CLR value)
  tls.certificate : certs/edge.pem  (two hops into a nested mapping, with no type declared anywhere for its shape)
  edited document (workers overwritten, logging grafted in, everything else untouched):
  | title: edge-proxy
  | workers: 16
  | drain_timeout: 30
  | tls:
  |   enabled: true
  |   certificate: certs/edge.pem
  | limits:
  |   max_connections: 10000
  |   max_body_bytes: 1048576
  | logging:
  |   level: warning
  |   sinks:
  |     - console
  |     - file
```

**APIs demonstrated.** `YamlNode.Parse(string)`, node indexers (`root["tls"]!["certificate"]`),
`YamlNode.AsValue()`, `YamlValue.GetValue<T>()`, `YamlValue.Create`, `YamlObject` collection
initializers and `Add`, `YamlArray` collection initializer, `YamlNode.ToYamlString()`.

## Scenario 3 — ReadOnlyDom

**Intent.** Show the `JsonDocument`-style workflow: one parse, then cheap struct `YamlElement`
cursors over the parsed data — the right layer when you only need to *inspect* a document
(feature flags, tool config probes) and want neither a POCO nor a mutable tree. The document
owns the parsed data, hence `using`.

**What it does.** Parses the same file with `YamlDocument.Parse`, drills down with `GetProperty`
chains and reads leaves with the typed getters (`GetString`, `GetInt64`, `GetBoolean`). It then
enumerates the `limits` mapping with `EnumerateMapping()` — no knowledge of its keys required,
each property exposing `Name`, `Value`, and `ValueKind` — and probes for an absent `proxy` key
with `TryGetProperty` instead of catching an exception.

**What to expect.** The four typed leaves, the two enumerated limit entries tagged `Integer`,
and a `False` for the optional-key probe:

```text
--- The read-only DOM - inspecting without materializing ---
  What   : Parses the document once, reads four typed leaves including a nested one, enumerates a mapping whose keys
           are not known in advance, and probes for an optional key that is absent.
  Why    : This is the cheapest way to read a document you will not change. One parse produces one buffer, and the
           elements are cursors into it rather than objects built from it - so walking the tree allocates nothing
           further, and reading two keys out of a large document does not cost the whole document. Because the
           buffer is owned rather than borrowed, the document is disposable, which is the one thing to remember
           about this layer. TryGetProperty exists because optional keys are the normal case in configuration, and
           an exception is the wrong mechanism for something expected.
  Expect : The typed getters return CLR values rather than strings. Enumerating the mapping reports each value kind
           alongside it, so a consumer can branch on shape without a schema. The absent key reports False instead of
           throwing.

  title         : edge-proxy
  workers       : 8
  drain_timeout : 30
  tls.enabled   : True  (each GetProperty is a cursor move within the one parsed buffer, not a new object)
  limits        :
    max_connections = 10000 (Integer)
    max_body_bytes = 1048576 (Integer)
  (each value reports its kind, so a consumer can branch on shape without a schema)
  proxy present : False  (expected False - optional keys are the normal case in config, so probing beats catching)
```

**APIs demonstrated.** `YamlDocument.Parse(string)` (+ `IDisposable`),
`YamlDocument.RootElement`, `YamlElement.GetProperty` / `TryGetProperty`, typed getters,
`YamlElement.EnumerateMapping()`, `YamlProperty.Name` / `.Value`, `YamlElement.ValueKind`.

## Scenario 4 — StreamingReads

**Intent.** Show the serializer's stream and buffer facade — how to read a document straight
from a `Stream` (a file, a response body) and write straight into an `IBufferWriter<byte>`
without an intermediate string. Unlike JSON, the YAML reader is buffered rather than an
incremental scanner, so the stream overloads read the whole document into memory before
parsing; only the stream copy itself is asynchronous.

**What it does.** Opens `Data/server-config.yaml` as a `FileStream` and binds it to a
`ServerConfig` POCO through `Deserialize<T>(Stream)` and again through the awaited
`DeserializeAsync<T>(Stream)`, confirming the two agree. The unmapped `tls` and `limits` keys
are skipped by the default `UnmappedMemberHandling`. It then serializes the POCO straight into
an `ArrayBufferWriter<byte>` with `Serialize<T>(IBufferWriter<byte>)` and prints the UTF-8 bytes.

**What to expect.** The same three fields from both the sync and async reads (with a `match`
confirmation), then the re-emitted YAML written through the buffer writer:

```text
--- The stream and buffer-writer facade ---
  What   : Reads the committed file into a POCO through the synchronous stream overload and again through the
           asynchronous one, compares the results, then serializes straight into a buffer writer rather than into a
           string.
  Why    : Two things worth being explicit about. The POCO here binds only three of the document keys, and the rest
           are skipped rather than rejected - the default unmapped-member handling, which is what lets a consumer
           bind the part of a shared config file it cares about without owning the whole schema. The other is the
           honest shape of the async overload: it buffers the document in full and only the stream copy is
           asynchronous, because YAML block structure cannot be resolved without the surrounding indentation. Saying
           so matters, since a caller who assumes incremental parsing would size a document by what the parser can
           stream rather than by what fits in memory.
  Expect : Both stream overloads produce the same values from the same file, and the unmapped keys cause no error.
           Serializing back emits only the three bound members - the keys this POCO never saw are gone, which is the
           round-trip caveat of binding a subset: the object, not the source document, is what gets written.

  Deserialize(Stream)      : edge-proxy, workers 8, drain 30  (the tls and limits keys are in the file but not on this POCO - unmapped members are skipped, not rejected)
  DeserializeAsync(Stream) : edge-proxy, workers 8, drain 30 (match: True)  (the document is buffered in full either way - only the stream copy is asynchronous, so do not size input by this)
  Serialize(IBufferWriter) - straight into a pooled buffer, with no intermediate string:
  | title: edge-proxy
  | workers: 8
  | drain_timeout: 30
  (only the three bound members come back - binding a subset means the object, not the source document, is what gets written)
```

**APIs demonstrated.** `YamlSerializer.Deserialize<T>(Stream, YamlSerializerOptions)`,
`YamlSerializer.DeserializeAsync<T>(Stream, ...)`,
`YamlSerializer.Serialize<T>(IBufferWriter<byte>, T, ...)`,
`YamlSerializerOptions.PropertyNamingPolicy`, default `UnmappedMemberHandling`.

## Layout

```text
Bodu.Text.Yaml.Samples.YamlDocuments/
  Program.cs                      # runs the scenarios in order
  SampleConsole.cs                # the What / Why / Expect scenario banner
  Data/server-config.yaml         # the committed input document
  Scenarios/TokenReaderWriter.cs
  Scenarios/MutableDom.cs
  Scenarios/ReadOnlyDom.cs
  Scenarios/StreamingReads.cs
```

## Related

- `Bodu.Text.Yaml.Samples.YamlBasics` — the `YamlSerializer` POCO surface above these layers.
- Guides: `docs/guides/serialization/yaml/`.
