---
title: Serialization callbacks
---

# Serialization callbacks

Both serializers let a type participate in its own serialization lifecycle by implementing one or more callback interfaces. Each format exposes the same four hooks — TOML as <xref:Bodu.Text.Toml.Serialization.ITomlOnSerializing>, <xref:Bodu.Text.Toml.Serialization.ITomlOnSerialized>, <xref:Bodu.Text.Toml.Serialization.ITomlOnDeserializing>, and <xref:Bodu.Text.Toml.Serialization.ITomlOnDeserialized>; Bencode as the like-named `IBencodeOn…` interfaces. The serializer detects the interfaces on the value's type and invokes them at the matching point in the pipeline — no registration or attribute is required.

| Hook | Runs | Typical use |
|---|---|---|
| `OnSerializing` | After a non-null value is selected for writing, before its first member is written. | Stamp or derive state that must appear in the output. |
| `OnSerialized` | After the value's table/dictionary has been closed. | Release or restore state; count or log completed writes. |
| `OnDeserializing` | After the instance is constructed, before any member is assigned. | Establish defaults that survive omitted keys. |
| `OnDeserialized` | After every member and any extension data has been assigned. | Validate or finalize the materialized object. |

Each pattern below shows the TOML form; the Bencode form is identical with the `Bencode` prefix.

## Pattern 1 — Apply defaults that survive omitted keys

Member initializers run at construction, but a key present in the input then overwrites them — there is no way to distinguish "key absent" from "key set to the initializer value" after the fact. `OnDeserializing` runs after construction and *before* member assignment, so a value it assigns persists exactly when the input omits the key:

```csharp
public sealed class ServerConfig : ITomlOnDeserializing
{
    public int Port { get; set; }

    void ITomlOnDeserializing.OnDeserializing() =>
        Port = 8080;
}
```

```csharp
TomlSerializer.Deserialize<ServerConfig>("").Port            // 8080 — key omitted, default survives
TomlSerializer.Deserialize<ServerConfig>("Port = 9090").Port // 9090 — key present, default overwritten
```

For a type built through a parameterized constructor the callback necessarily runs after the constructor has consumed its bound arguments — the instance does not exist any earlier.

## Pattern 2 — Validate after deserialization

`OnDeserialized` is the last step of deserialization for the instance, so it observes the fully materialized object — including required members, extension data, and populated collections. Throwing from it fails the deserialization:

```csharp
public sealed class ServerConfig : ITomlOnDeserialized
{
    public int Port { get; set; }

    void ITomlOnDeserialized.OnDeserialized()
    {
        if (Port is < 1 or > 65535)
            throw new InvalidOperationException("Port is out of range.");
    }
}
```

This complements `[TomlRequired]` (which checks presence, not validity): the attribute rejects an absent key, the callback rejects a present-but-invalid value.

## Pattern 3 — Derive state before serialization

`OnSerializing` runs before the value's members are written, so any mutation it performs is reflected in the emitted output. Use it to stamp timestamps, recompute checksums, or normalize state at the moment of writing:

```csharp
public sealed class Snapshot : ITomlOnSerializing
{
    public DateTime SavedAt { get; set; }

    void ITomlOnSerializing.OnSerializing() =>
        SavedAt = DateTime.UtcNow;
}
```

> [!NOTE]
> Bencode has no native date-time kind, so the Bencode form of this pattern stores a `long` instead — for example `SavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();`.

## Pattern 4 — Observe a completed write

`OnSerialized` runs after the value's table/dictionary has been closed: it observes the completed write rather than influencing the output. Use it to restore state changed by `OnSerializing`, or to track writes:

```csharp
public sealed class Snapshot : ITomlOnSerialized
{
    [TomlIgnore]
    public int WriteCount { get; private set; }

    void ITomlOnSerialized.OnSerialized() =>
        WriteCount++;
}
```

## Scope and ordering

- The callbacks apply to values written as tables/dictionaries through the object mapping — the path a plain class or struct takes. A value claimed by a scalar converter has no member-writing phase and no callbacks.
- They fire for nested objects too, innermost completing first on write and on read.
- Within one instance the order is always construct → `OnDeserializing` → member assignment → `OnDeserialized`, and `OnSerializing` → member writing → `OnSerialized`.
- The hooks pair naturally: state established in `OnSerializing` can be torn down in `OnSerialized`, and defaults set in `OnDeserializing` can be validated in `OnDeserialized`.

The same four hooks with the same semantics exist for Bencode: <xref:Bodu.Text.Bencode.Serialization.IBencodeOnSerializing>, <xref:Bodu.Text.Bencode.Serialization.IBencodeOnSerialized>, <xref:Bodu.Text.Bencode.Serialization.IBencodeOnDeserializing>, and <xref:Bodu.Text.Bencode.Serialization.IBencodeOnDeserialized>.
