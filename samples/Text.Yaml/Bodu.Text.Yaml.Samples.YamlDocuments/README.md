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
emitted:
  | service: edge-proxy
  | replicas: 3
  | health:
  |   enabled: true
  |   threshold: 0.75
tokens:
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
title           : edge-proxy
tls.certificate : certs/edge.pem
edited document :
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
title         : edge-proxy
workers       : 8
drain_timeout : 30
tls.enabled   : True
limits        :
  max_connections = 10000 (Integer)
  max_body_bytes = 1048576 (Integer)
proxy present : False
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
Deserialize(Stream)      : edge-proxy, workers 8, drain 30
DeserializeAsync(Stream) : edge-proxy, workers 8, drain 30 (match: True)
Serialize(IBufferWriter) :
  | title: edge-proxy
  | workers: 8
  | drain_timeout: 30
```

**APIs demonstrated.** `YamlSerializer.Deserialize<T>(Stream, YamlSerializerOptions)`,
`YamlSerializer.DeserializeAsync<T>(Stream, ...)`,
`YamlSerializer.Serialize<T>(IBufferWriter<byte>, T, ...)`,
`YamlSerializerOptions.PropertyNamingPolicy`, default `UnmappedMemberHandling`.

## Layout

```text
Bodu.Text.Yaml.Samples.YamlDocuments/
  Program.cs                      # runs the scenarios in order
  Data/server-config.yaml         # the committed input document
  Scenarios/TokenReaderWriter.cs
  Scenarios/MutableDom.cs
  Scenarios/ReadOnlyDom.cs
  Scenarios/StreamingReads.cs
```

## Related

- `Bodu.Text.Yaml.Samples.YamlBasics` — the `YamlSerializer` POCO surface above these layers.
- Guides: `docs/guides/serialization/yaml/`.
