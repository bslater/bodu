---
title: Serialization callbacks
---

# Serialization callbacks

The Bencode serializer lets a type participate in its own serialization lifecycle by implementing one or more callback interfaces: <xref:Bodu.Text.Bencode.Serialization.IBencodeOnSerializing>, <xref:Bodu.Text.Bencode.Serialization.IBencodeOnSerialized>, <xref:Bodu.Text.Bencode.Serialization.IBencodeOnDeserializing>, and <xref:Bodu.Text.Bencode.Serialization.IBencodeOnDeserialized>. The serializer detects the interfaces on the value's type and invokes them at the matching point in the pipeline — no registration or attribute is required. The sibling [TOML](../toml/index.md) and [YAML](../yaml/index.md) serializers expose the same four hooks with their own prefix.

| Hook | Runs | Typical use |
|---|---|---|
| `OnSerializing` | After a non-null value is selected for writing, before its first member is written. | Stamp or derive state that must appear in the output. |
| `OnSerialized` | After the value's dictionary has been closed. | Release or restore state; count or log completed writes. |
| `OnDeserializing` | After the instance is constructed, before any member is assigned. | Establish defaults that survive omitted keys. |
| `OnDeserialized` | After every member and any extension data has been assigned. | Validate or finalize the materialized object. |

## Pattern 1 — Apply defaults that survive omitted keys

Member initializers run at construction, but a key present in the input then overwrites them — there is no way to distinguish "key absent" from "key set to the initializer value" after the fact. `OnDeserializing` runs after construction and *before* member assignment, so a value it assigns persists exactly when the input omits the key:

```csharp
public sealed class ServerConfig : IBencodeOnDeserializing
{
    public int Port { get; set; }

    void IBencodeOnDeserializing.OnDeserializing() =>
        Port = 8080;
}
```

```csharp
BencodeSerializer.Deserialize<ServerConfig>(empty).Port    // 8080 — key omitted, default survives
BencodeSerializer.Deserialize<ServerConfig>(withPort).Port // 9090 — key present, default overwritten
```

For a type built through a parameterized constructor the callback necessarily runs after the constructor has consumed its bound arguments — the instance does not exist any earlier.

## Pattern 2 — Validate after deserialization

`OnDeserialized` is the last step of deserialization for the instance, so it observes the fully materialized object — including required members, extension data, and populated collections. Throwing from it fails the deserialization:

```csharp
public sealed class ServerConfig : IBencodeOnDeserialized
{
    public int Port { get; set; }

    void IBencodeOnDeserialized.OnDeserialized()
    {
        if (Port is < 1 or > 65535)
            throw new InvalidOperationException("Port is out of range.");
    }
}
```

This complements `[BencodeRequired]` (which checks presence, not validity): the attribute rejects an absent key, the callback rejects a present-but-invalid value.

## Pattern 3 — Derive state before serialization

`OnSerializing` runs before the value's members are written, so any mutation it performs is reflected in the emitted output. Use it to stamp timestamps, recompute checksums, or normalize state at the moment of writing. Bencode has no native date-time kind, so a timestamp is stored as a `long`:

```csharp
public sealed class Snapshot : IBencodeOnSerializing
{
    public long SavedAt { get; set; }

    void IBencodeOnSerializing.OnSerializing() =>
        SavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
```

## Pattern 4 — Observe a completed write

`OnSerialized` runs after the value's dictionary has been closed: it observes the completed write rather than influencing the output. Use it to restore state changed by `OnSerializing`, or to track writes:

```csharp
public sealed class Snapshot : IBencodeOnSerialized
{
    [BencodeIgnore]
    public int WriteCount { get; private set; }

    void IBencodeOnSerialized.OnSerialized() =>
        WriteCount++;
}
```

## Pattern 5 — End to end: a self-describing, validated manifest

The hooks combine naturally: `OnSerializing` keeps a computed member fresh at the moment of writing, and `OnDeserialized` rejects a document where the same invariant does not hold. Throw the serialization exception (<xref:Bodu.Text.Bencode.BencodeSerializationException>) so callers handle validation failures in the same catch clause as every other binding error:

```csharp
using Bodu.Text.Bencode;
using Bodu.Text.Bencode.Serialization;

public sealed class Manifest : IBencodeOnSerializing, IBencodeOnDeserialized
{
    public List<string> Files { get; set; } = [];

    public int FileCount { get; set; }

    void IBencodeOnSerializing.OnSerializing() =>
        FileCount = Files.Count;   // refreshed on every write — never stale

    void IBencodeOnDeserialized.OnDeserialized()
    {
        if (FileCount != Files.Count)
            throw new BencodeSerializationException("FileCount does not match the number of entries in Files.");
    }
}
```

Serializing recomputes the count before the members are written:

```csharp
byte[] payload = BencodeSerializer.Serialize(new Manifest { Files = ["a.txt", "b.txt", "c.txt"] });
// → d9:FileCounti3e5:Filesl5:a.txt5:b.txt5:c.txtee   (keys in canonical order)
```

Deserializing validates the fully materialized instance — a document whose count disagrees with its list fails, and a consistent one binds:

```csharp
BencodeSerializer.Deserialize<Manifest>(mismatched);
// → throws BencodeSerializationException: FileCount does not match the number of entries in Files.

Manifest ok = BencodeSerializer.Deserialize<Manifest>(consistent);
// ok.FileCount → 1
```

## Invocation order

Where each hook sits in the pipeline, relative to the converter work the serializer performs for the members:

| Phase | Deserialization | Serialization |
|---|---|---|
| 1 | Member values are read from the document, each through its member converter. | A non-null value is selected for writing. |
| 2 | Required members are checked; the instance is constructed (parameterless or the bound parameterized constructor). | **`OnSerializing`** fires. |
| 3 | **`OnDeserializing`** fires — after construction, before any member is assigned. | The dictionary is opened. |
| 4 | Settable members are assigned from the values read in phase 1. | Members are written, each through its member converter; extension data follows. |
| 5 | Extension data is populated. | The dictionary is closed (entries re-sorted into canonical key order). |
| 6 | **`OnDeserialized`** fires on the fully materialized instance. | **`OnSerialized`** fires. |

Two consequences worth noting. On read, the member *converters* run before `OnDeserializing` — the hook cannot influence how values are parsed, only what happens to the instance before they are assigned. On write, `OnSerializing` runs before the value's dictionary is opened, so mutations it makes are always reflected in the output.

## Interplay with custom converters

The four hooks are invoked by the object-mapping converter — the catch-all that writes a plain class or struct as a dictionary. A type claimed by a *custom* converter (via `[BencodeConverter]` or `options.Converters`) bypasses that path entirely: the serializer hands the value to your converter and never enters the member-mapping phase, so **none of the callbacks fire for that type, even when it implements the interfaces**. If a converter-handled type needs lifecycle behavior, perform it inside the converter's `Read` / `Write`.

Member-level converters and callbacks compose, however: a callback-bearing type whose *members* use custom converters still fires all four hooks — the custom converters simply do the per-member reading and writing in phases 1 and 4 above.

## Scope and ordering

- The callbacks apply to values written as dictionaries through the object mapping — the path a plain class or struct takes. A value claimed by a scalar converter has no member-writing phase and no callbacks.
- They fire for nested objects too, innermost completing first on write and on read.
- Within one instance the order is always construct → `OnDeserializing` → member assignment → `OnDeserialized`, and `OnSerializing` → member writing → `OnSerialized`.
- The hooks pair naturally: state established in `OnSerializing` can be torn down in `OnSerialized`, and defaults set in `OnDeserializing` can be validated in `OnDeserialized`.

## See also

- [Mapping attributes](attributes.md) — `[BencodeRequired]` and friends; declarative presence checks that the callbacks complement with value validation.
- [Writing converters](converters.md) — the customization seam that *replaces* the object mapping (and with it, the callbacks) for a type.
- [Using Bencode](using.md) — the format walk-through, including the error-handling pattern that catches the exception thrown from `OnDeserialized`.
- [Core concepts](../../../docs/serialization/bencode/concepts.md) — where the callbacks sit in the Bencode vocabulary.
- [Text & Serialization guides](../../topics/text-and-serialization.md) and the [topic overview](../../../docs/topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Bencode.Serialization.IBencodeOnSerializing>, <xref:Bodu.Text.Bencode.Serialization.IBencodeOnSerialized>, <xref:Bodu.Text.Bencode.Serialization.IBencodeOnDeserializing>, <xref:Bodu.Text.Bencode.Serialization.IBencodeOnDeserialized>.
