---
title: Serialization callbacks
---

# Serialization callbacks

The YAML serializer lets a type participate in its own serialization lifecycle by implementing one or more callback interfaces. YAML exposes four hooks — <xref:Bodu.Text.Serialization.IOnSerializing>, <xref:Bodu.Text.Serialization.IOnSerialized>, <xref:Bodu.Text.Serialization.IOnDeserializing>, and <xref:Bodu.Text.Serialization.IOnDeserialized>. The serializer detects the interfaces on the value's type and invokes them at the matching point in the pipeline — no registration or attribute is required. The interfaces live in the shared <xref:Bodu.Text.Serialization> package, so the sibling libraries ([Bodu.Text.Toml](../toml/index.md), [Bodu.Text.Bencode](../bencode/index.md)) honor exactly the same four hooks.

| Hook | Runs | Typical use |
|---|---|---|
| `OnSerializing` | After a non-null value is selected for writing, before its mapping is opened. | Stamp or derive state that must appear in the output. |
| `OnSerialized` | After the value's mapping has been closed. | Release or restore state; count or log completed writes. |
| `OnDeserializing` | After the instance is constructed, before any member is assigned. | Establish defaults that survive omitted keys. |
| `OnDeserialized` | After every member and any extension data has been assigned. | Validate or finalize the materialized object. |

## Pattern 1 — Apply defaults that survive omitted keys

Member initializers run at construction, but a key present in the input then overwrites them — there is no way to distinguish "key absent" from "key set to the initializer value" after the fact. `OnDeserializing` runs after construction and *before* member assignment, so a value it assigns persists exactly when the input omits the key:

```csharp
public sealed class ServerConfig : IOnDeserializing
{
    public int Port { get; set; }

    void IOnDeserializing.OnDeserializing() =>
        Port = 8080;
}
```

```csharp
YamlSerializer.Deserialize<ServerConfig>("{}")!.Port           // 8080 — key omitted, default survives
YamlSerializer.Deserialize<ServerConfig>("Port: 9090")!.Port   // 9090 — key present, default overwritten
```

> [!NOTE]
> An *empty* YAML stream has no document, so `YamlSerializer.Deserialize<ServerConfig>("")` returns `null` without constructing anything — no callback fires. The empty flow mapping `{}` is the YAML spelling of "a document with every key omitted", which is why the sample uses it.

For a type built through a parameterized constructor the callback necessarily runs after the constructor has consumed its bound arguments — the instance does not exist any earlier.

## Pattern 2 — Validate after deserialization

`OnDeserialized` is the last step of deserialization for the instance, so it observes the fully materialized object — including required members, extension data, and populated collections. Throwing from it fails the deserialization, and the exception propagates as thrown:

```csharp
public sealed class ServerConfig : IOnDeserialized
{
    public int Port { get; set; }

    void IOnDeserialized.OnDeserialized()
    {
        if (Port is < 1 or > 65535)
            throw new InvalidOperationException("Port is out of range.");
    }
}
```

```csharp
YamlSerializer.Deserialize<ServerConfig>("Port: 70000");
// → throws InvalidOperationException: Port is out of range.
```

This complements `[Required]` (which checks presence, not validity): the attribute rejects an absent key, the callback rejects a present-but-invalid value. Pattern 5 shows the idiomatic choice of exception type.

## Pattern 3 — Derive state before serialization

`OnSerializing` runs before the value's mapping is opened, so any mutation it performs is reflected in the emitted output. Use it to stamp timestamps, recompute checksums, or normalize state at the moment of writing:

```csharp
public sealed class Snapshot : IOnSerializing
{
    public DateTime SavedAt { get; set; }

    void IOnSerializing.OnSerializing() =>
        SavedAt = DateTime.UtcNow;
}
```

```csharp
YamlSerializer.Serialize(new Snapshot());
// SavedAt: 2026-09-09T06:20:12.9773368Z
```

## Pattern 4 — Observe a completed write

`OnSerialized` runs after the value's mapping has been closed: it observes the completed write rather than influencing the output. Use it to restore state changed by `OnSerializing`, or to track writes:

```csharp
public sealed class Snapshot : IOnSerialized
{
    [Ignore]
    public int WriteCount { get; private set; }

    void IOnSerialized.OnSerialized() =>
        WriteCount++;
}
```

```csharp
var snapshot = new Snapshot();
YamlSerializer.Serialize(snapshot);
YamlSerializer.Serialize(snapshot);
// snapshot.WriteCount → 2
```

## Pattern 5 — End to end: a self-describing, validated manifest

The hooks combine naturally: `OnSerializing` keeps a computed member fresh at the moment of writing, and `OnDeserialized` rejects a document where the same invariant does not hold. Throw the format's serialization exception (<xref:Bodu.Text.Yaml.YamlSerializationException>) so callers handle validation failures in the same catch clause as every other binding error:

```csharp
using Bodu.Text.Serialization;
using Bodu.Text.Yaml;

public sealed class Manifest : IOnSerializing, IOnDeserialized
{
    public List<string> Files { get; set; } = [];

    public int FileCount { get; set; }

    void IOnSerializing.OnSerializing() =>
        FileCount = Files.Count;   // refreshed on every write — never stale

    void IOnDeserialized.OnDeserialized()
    {
        if (FileCount != Files.Count)
            throw new YamlSerializationException("FileCount does not match the number of entries in Files.");
    }
}
```

Serializing recomputes the count before the members are written:

```csharp
string text = YamlSerializer.Serialize(new Manifest { Files = ["a.txt", "b.txt", "c.txt"] });
// → Files:
//     - a.txt
//     - b.txt
//     - c.txt
//   FileCount: 3
```

Deserializing validates the fully materialized instance — a document whose count disagrees with its list fails, and a consistent one binds:

```csharp
YamlSerializer.Deserialize<Manifest>("FileCount: 2");
// → throws YamlSerializationException: FileCount does not match the number of entries in Files.

Manifest ok = YamlSerializer.Deserialize<Manifest>("""
    Files: [x.txt]
    FileCount: 1
    """)!;
// ok.FileCount → 1
```

## Invocation order

Where each hook sits in the pipeline, relative to the converter work the serializer performs for the members:

| Phase | Deserialization | Serialization |
|---|---|---|
| 1 | Member values are read from the composed document, each through its member converter. | A non-null value is selected for writing; a derived instance in a base-typed member is redirected to its runtime type's converter first. |
| 2 | Required members are checked; the instance is constructed (parameterless or the bound parameterized constructor). | The value is entered into the reference-cycle tracker. |
| 3 | **`OnDeserializing`** fires — after construction, before any member is assigned. | **`OnSerializing`** fires. |
| 4 | Settable members are assigned from the values read in phase 1. | The mapping is opened. |
| 5 | Extension data is populated. | Members are written, each through its member converter; extension data follows. |
| 6 | **`OnDeserialized`** fires on the fully materialized instance. | The mapping is closed. |
| 7 | | **`OnSerialized`** fires. |

Four consequences are worth noting.

- **The reader is buffered.** `Utf8YamlReader` parses the whole stream into a composed tree — anchors, aliases, and merge keys resolved — before the first token is handed to a converter. A syntactically malformed document therefore raises <xref:Bodu.Text.Yaml.YamlFormatException> before any converter or callback runs, and a callback never observes a partially parsed document. On read, the member *converters* also run before `OnDeserializing` (phase 1), so the hook cannot influence how values are parsed — only what happens to the instance before they are assigned.
- **`OnSerializing` runs before the mapping is opened**, so mutations it makes are always reflected in the output.
- **`OnSerialized` is not in a `finally` block.** The object converter fires it as the last statement of the successful write path. When a write fails part-way — a member converter throws, or the reference tracker detects a cycle (`YamlSerializationException`: *A reference cycle was detected while serializing an instance of type '…'; YAML cannot represent an object cycle.*) — `OnSerializing` has already fired but `OnSerialized` never does. Do not rely on the pair for resource cleanup; use it for state that only a completed write should update.
- **Runtime-type dispatch happens before the hooks.** A `Circle` held in a `Shape`-typed member serializes through `Circle`'s object converter, so callbacks declared on the derived type fire — exactly as they would for a member declared as `Circle`.

## Interplay with custom converters

The four hooks are invoked by the object-mapping converter — the catch-all that writes a plain class or struct as a mapping. A type claimed by a *custom* converter (via `[Converter]` or `options.Converters`) bypasses that path entirely: the serializer hands the value to your converter and never enters the member-mapping phase, so **none of the callbacks fire for that type, even when it implements the interfaces**. If a converter-handled type needs lifecycle behavior, perform it inside the converter's `Read` / `Write`.

Member-level converters and callbacks compose, however: a callback-bearing type whose *members* use custom converters still fires all four hooks — the custom converters simply do the per-member reading and writing in phases 1 and 5 above.

## Scope and ordering

- The callbacks apply to values written as mappings through the object mapping — the path a plain class or struct takes. A value claimed by a scalar converter has no member-writing phase and no callbacks.
- They fire for nested objects too, innermost completing first on write and on read. Serializing an `outer` node whose `Next` member holds an `inner` node runs `OnSerialized(inner)` before `OnSerialized(outer)`.
- Within one instance the order is always construct → `OnDeserializing` → member assignment → `OnDeserialized`, and `OnSerializing` → member writing → `OnSerialized`.
- The hooks pair naturally: state established in `OnSerializing` can be torn down in `OnSerialized` (on a successful write), and defaults set in `OnDeserializing` can be validated in `OnDeserialized`.

## Where to go next

- [Mapping attributes](attributes.md) — `[Required]` and friends; declarative presence checks that the callbacks complement with value validation.
- [Writing converters](converters.md) — the customization seam that *replaces* the object mapping (and with it, the callbacks) for a type.
- [Polymorphic converters](polymorphic-converters.md) — the factory pattern for members declared as a base type or interface.
- [Using YAML](using.md) — the format walk-through, including the error-handling pattern that catches the exception thrown from `OnDeserialized`.
- [Core concepts](../../../docs/serialization/yaml/concepts.md) — where the callbacks sit in the family vocabulary; the shared [callback order](../../../docs/serialization/core/concepts.md#callback-order) table.
- The sibling guides — [TOML callbacks](../toml/callbacks.md) and [Bencode callbacks](../bencode/callbacks.md) — for the same hooks against a token-strict format.
- API reference — <xref:Bodu.Text.Serialization.IOnSerializing>, <xref:Bodu.Text.Serialization.IOnSerialized>, <xref:Bodu.Text.Serialization.IOnDeserializing>, <xref:Bodu.Text.Serialization.IOnDeserialized>.
- [Bodu serializer guides](../index.md) and the [Text & Serialization guides](../../topics/text-and-serialization.md).
