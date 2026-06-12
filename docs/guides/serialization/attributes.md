---
title: Mapping attributes
---

# Mapping attributes

Both serializers expose the same attribute family for shaping how a type maps to the wire: every TOML attribute derives <xref:Bodu.Text.Toml.Serialization.TomlAttribute> and every Bencode attribute derives <xref:Bodu.Text.Bencode.Serialization.BencodeAttribute>. The two families mirror each other exactly — each pattern below shows the TOML form and its output; the Bencode form is the same shape with the `Bencode` prefix, with the wire form noted where it differs in an interesting way.

## Pattern 1 — Rename a member

<xref:Bodu.Text.Toml.Serialization.TomlPropertyNameAttribute> pins the serialized key for one member, beating any naming policy:

```csharp
public sealed class Profile
{
    [TomlPropertyName("display-name")]
    public string DisplayName { get; set; } = "Ada";
}
```

```toml
display-name = "Ada"
```

Bencode (`[BencodePropertyName("display-name")]`) writes the dictionary entry `12:display-name3:Ada`.

## Pattern 2 — Apply a naming policy per type

<xref:Bodu.Text.Toml.Serialization.TomlNamingPolicyAttribute> overrides the options-level `PropertyNamingPolicy` for one type:

```csharp
[TomlNamingPolicy(TomlKnownNamingPolicy.SnakeCaseLower)]
public sealed class RetryPolicy
{
    public int MaxRetryCount { get; set; } = 5;
}
```

```toml
max_retry_count = 5
```

A member carrying `[TomlPropertyName]` is unaffected — the explicit name always wins. `TomlKnownNamingPolicy.Unspecified` defers back to the options.

## Pattern 3 — Exclude members

<xref:Bodu.Text.Toml.Serialization.TomlIgnoreAttribute> drops a member unconditionally, or under a condition:

```csharp
public sealed class Account
{
    public string Name { get; set; } = "svc";

    [TomlIgnore]
    public string? Secret { get; set; }

    [TomlIgnore(Condition = TomlIgnoreCondition.WhenWritingNull)]
    public string? Comment { get; set; }
}
```

```toml
Name = "svc"
```

`Secret` is never written; `Comment` appears only when non-null. `WhenWritingDefault` extends the rule to default value-type values.

## Pattern 4 — Force a member in

<xref:Bodu.Text.Toml.Serialization.TomlIncludeAttribute> binds non-public property accessors and surfaces public fields without turning on `IncludeFields` for the whole options:

```csharp
public sealed class Counter
{
    [TomlInclude]
    public int Total { get; private set; }

    [TomlInclude]
    public int Retries;
}
```

Both members now round-trip — the private setter is assigned on read, and the field participates like a property, following the same naming, ordering, ignore, required, and converter rules.

## Pattern 5 — Control write order

<xref:Bodu.Text.Toml.Serialization.TomlPropertyOrderAttribute> reorders the emitted key/value lines; members without the attribute default to order zero and keep declaration order:

```csharp
public sealed class Manifest
{
    [TomlPropertyOrder(2)]
    public string Name { get; set; } = "demo";

    [TomlPropertyOrder(1)]
    public int Version { get; set; } = 3;
}
```

```toml
Version = 3
Name = "demo"
```

> [!NOTE]
> In Bencode the attribute governs only the order members are *presented to the writer* — the writer re-sorts dictionary entries into canonical ascending key order when the dictionary closes, so the example above still emits `d4:Name4:demo7:Versioni3ee` regardless of `[BencodePropertyOrder]`.

## Pattern 6 — Require a key

<xref:Bodu.Text.Toml.Serialization.TomlRequiredAttribute> makes deserialization fail when the key is absent, with the same effect as declaring the member with the C# `required` keyword:

```csharp
public sealed class ServerConfig
{
    [TomlRequired]
    public string Host { get; set; } = string.Empty;
}

// Input without a "Host" key throws TomlSerializationException.
```

## Pattern 7 — Pick the deserialization constructor

When a type declares more than one constructor, <xref:Bodu.Text.Toml.Serialization.TomlConstructorAttribute> resolves the ambiguity:

```csharp
public sealed class Endpoint
{
    [TomlConstructor]
    public Endpoint(string host, int port) => (Host, Port) = (host, port);

    public Endpoint(Uri uri) : this(uri.Host, uri.Port) { }

    public string Host { get; }
    public int Port { get; }
}
```

Without the attribute the serializer prefers a public parameterless constructor, then a single declared constructor, then the constructor with the most parameters.

## Pattern 8 — Capture unknown keys

<xref:Bodu.Text.Toml.Serialization.TomlExtensionDataAttribute> designates one member that collects every key that maps to no other member, and writes the collected entries back out on serialization:

```csharp
public sealed class ServerConfig
{
    public int Port { get; set; }

    [TomlExtensionData]
    public Dictionary<string, TomlNode>? Extra { get; set; }
}
```

The member must be a `TomlObject` or an `(I)Dictionary<string, TomlNode>` (Bencode: `BencodeObject` / `(I)Dictionary<string, BencodeNode>`), and a type may declare at most one.

## Pattern 9 — Reject unknown keys

<xref:Bodu.Text.Toml.Serialization.TomlUnmappedMemberHandlingAttribute> chooses, per type, between skipping a key that maps to no member (the default) and failing:

```csharp
[TomlUnmappedMemberHandling(TomlUnmappedMemberHandling.Disallow)]
public sealed class StrictConfig
{
    public int Port { get; set; }
}

// Input containing an unrecognised key throws TomlSerializationException.
```

A type with an extension-data member (Pattern 8) still captures unmapped keys into that member regardless of this setting.

## Pattern 10 — Populate instead of replace

<xref:Bodu.Text.Toml.Serialization.TomlObjectCreationHandlingAttribute> controls whether deserialization replaces a member's value with a fresh instance or populates the instance already held — useful for get-only collection properties:

```csharp
public sealed class Pipeline
{
    [TomlObjectCreationHandling(TomlObjectCreationHandling.Populate)]
    public List<string> Steps { get; } = new() { "restore" };
}
```

Deserialized entries are appended to the existing list instead of replacing it. The attribute applies to a member or a whole type; member beats type, and both beat the options-level `PreferredObjectCreationHandling`.

## Pattern 11 — Choose a converter

<xref:Bodu.Text.Toml.Serialization.TomlConverterAttribute> selects the converter for a member, or for every use of a type:

```csharp
public sealed class Package
{
    [TomlConverter(typeof(VersionConverter))]
    public Version Version { get; set; } = new(1, 2, 3);
}
```

```toml
Version = "1.2.3"
```

The referenced type must derive from the format's converter base and expose a public parameterless constructor. Writing converters — including the factory pattern and resolution order — is covered in [Writing converters](converters.md).

## Pattern 12 — Name enum members

<xref:Bodu.Text.Toml.Serialization.TomlStringEnumMemberNameAttribute> renames an individual enumeration member when the enum is serialized by name:

```csharp
[TomlConverter(typeof(TomlStringEnumConverter<Status>))]
public enum Status
{
    Active,

    [TomlStringEnumMemberName("on-hold")]
    OnHold,
}
```

```toml
Status = "on-hold"
```

The attribute applies only to by-name serialization (the default enum handling, or an explicit string-enum converter); it is a no-op when the enum is written as an integer.

## Precedence at a glance

When several settings could govern the same member, the closest one wins:

1. a member-level attribute (`[TomlPropertyName]`, `[TomlIgnore]`, `[TomlConverter]`, `[TomlObjectCreationHandling]`, …);
2. a type-level attribute (`[TomlNamingPolicy]`, `[TomlConverter]`, `[TomlUnmappedMemberHandling]`, `[TomlObjectCreationHandling]`);
3. the serializer options (`PropertyNamingPolicy`, `Converters`, `UnmappedMemberHandling`, `PreferredObjectCreationHandling`, `IncludeFields`).

The same ladder applies to the `Bencode` family.

## See also

- **[Text & Serialization guides](../topics/text-and-serialization.md)** — every guide in this topic, across Bodu.Text.Encoding, Bodu.Text.Formats, and the Bencode / TOML serializers.
- **[Bodu serializer guides](index.md)** — the full guide index for the Bencode and TOML serializers.
