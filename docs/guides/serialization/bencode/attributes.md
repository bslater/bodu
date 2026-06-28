---
title: Mapping attributes
---

# Mapping attributes

The Bencode serializer exposes a family of attributes for shaping how a type maps to the wire — every one derives <xref:Bodu.Text.Bencode.Serialization.BencodeAttribute>. The sibling [TOML](../toml/index.md) and [YAML](../yaml/index.md) serializers expose the same family with their own prefix; the patterns transfer directly. Each pattern below shows the Bencode form and the dictionary entry it writes.

## Pattern 1 — Rename a member

<xref:Bodu.Text.Bencode.Serialization.BencodePropertyNameAttribute> pins the serialized key for one member, beating any naming policy:

```csharp
public sealed class Profile
{
    [BencodePropertyName("display-name")]
    public string DisplayName { get; set; } = "Ada";
}
```

This writes the dictionary entry `12:display-name3:Ada`.

## Pattern 2 — Apply a naming policy per type

<xref:Bodu.Text.Bencode.Serialization.BencodeNamingPolicyAttribute> overrides the options-level `PropertyNamingPolicy` for one type:

```csharp
[BencodeNamingPolicy(BencodeKnownNamingPolicy.SnakeCaseLower)]
public sealed class RetryPolicy
{
    public int MaxRetryCount { get; set; } = 5;
}

// → d15:max_retry_counti5ee
```

A member carrying `[BencodePropertyName]` is unaffected — the explicit name always wins. `BencodeKnownNamingPolicy.Unspecified` defers back to the options.

## Pattern 3 — Exclude members

<xref:Bodu.Text.Bencode.Serialization.BencodeIgnoreAttribute> drops a member unconditionally, or under a condition:

```csharp
public sealed class Account
{
    public string Name { get; set; } = "svc";

    [BencodeIgnore]
    public string? Secret { get; set; }

    [BencodeIgnore(Condition = BencodeIgnoreCondition.WhenWritingNull)]
    public string? Comment { get; set; }
}

// → d4:Name3:svce
```

`Secret` is never written; `Comment` appears only when non-null. `WhenWritingDefault` extends the rule to default value-type values.

## Pattern 4 — Force a member in

<xref:Bodu.Text.Bencode.Serialization.BencodeIncludeAttribute> binds non-public property accessors and surfaces public fields without turning on `IncludeFields` for the whole options:

```csharp
public sealed class Counter
{
    [BencodeInclude]
    public int Total { get; private set; }

    [BencodeInclude]
    public int Retries;
}
```

Both members now round-trip — the private setter is assigned on read, and the field participates like a property, following the same naming, ordering, ignore, required, and converter rules.

## Pattern 5 — Control write order

<xref:Bodu.Text.Bencode.Serialization.BencodePropertyOrderAttribute> reorders the order members are presented to the writer; members without the attribute default to order zero and keep declaration order:

```csharp
public sealed class Manifest
{
    [BencodePropertyOrder(2)]
    public string Name { get; set; } = "demo";

    [BencodePropertyOrder(1)]
    public int Version { get; set; } = 3;
}
```

> [!NOTE]
> The attribute governs only the order members are *presented to the writer* — the writer re-sorts dictionary entries into canonical ascending key order when the dictionary closes, so the example above still emits `d4:Name4:demo7:Versioni3ee` regardless of `[BencodePropertyOrder]`. The attribute matters when a custom converter walks members in presentation order.

## Pattern 6 — Require a key

<xref:Bodu.Text.Bencode.Serialization.BencodeRequiredAttribute> makes deserialization fail when the key is absent, with the same effect as declaring the member with the C# `required` keyword:

```csharp
public sealed class ServerConfig
{
    [BencodeRequired]
    public string Host { get; set; } = string.Empty;
}

// Input without a "Host" key throws BencodeSerializationException.
```

## Pattern 7 — Pick the deserialization constructor

When a type declares more than one constructor, <xref:Bodu.Text.Bencode.Serialization.BencodeConstructorAttribute> resolves the ambiguity:

```csharp
public sealed class Endpoint
{
    [BencodeConstructor]
    public Endpoint(string host, int port) => (Host, Port) = (host, port);

    public Endpoint(Uri uri) : this(uri.Host, uri.Port) { }

    public string Host { get; }
    public int Port { get; }
}
```

Without the attribute the serializer resolves the constructor in this order: a constructor carrying `[BencodeConstructor]`, then a public parameterless constructor (or, for a value type, default construction), then the public constructor with the most parameters. The chosen constructor's parameters are bound to members by matching parameter name to member name **case-insensitively**, so a `host`/`port` constructor binds the `Host`/`Port` members above.

## Pattern 8 — Capture unknown keys

<xref:Bodu.Text.Bencode.Serialization.BencodeExtensionDataAttribute> designates one member that collects every key that maps to no other member, and writes the collected entries back out on serialization:

```csharp
public sealed class ServerConfig
{
    public int Port { get; set; }

    [BencodeExtensionData]
    public Dictionary<string, BencodeNode>? Extra { get; set; }
}
```

The member must be a <xref:Bodu.Text.Bencode.Nodes.BencodeObject>, an `IDictionary<string, BencodeNode>`, or a `Dictionary<string, BencodeNode>`, and a type may declare at most one. Because the writer re-sorts dictionary entries on close, the captured extension keys merge into the correct canonical position alongside the mapped members on the way back out.

## Pattern 9 — Reject unknown keys

<xref:Bodu.Text.Bencode.Serialization.BencodeUnmappedMemberHandlingAttribute> chooses, per type, between skipping a key that maps to no member (the default) and failing:

```csharp
[BencodeUnmappedMemberHandling(BencodeUnmappedMemberHandling.Disallow)]
public sealed class StrictConfig
{
    public int Port { get; set; }
}

// Input containing an unrecognised key throws BencodeSerializationException.
```

A type with an extension-data member (Pattern 8) still captures unmapped keys into that member regardless of this setting.

## Pattern 10 — Populate instead of replace

<xref:Bodu.Text.Bencode.Serialization.BencodeObjectCreationHandlingAttribute> controls whether deserialization replaces a member's value with a fresh instance or populates the instance already held — useful for get-only collection properties:

```csharp
public sealed class Pipeline
{
    [BencodeObjectCreationHandling(BencodeObjectCreationHandling.Populate)]
    public List<string> Steps { get; } = new() { "restore" };
}
```

Deserialized entries are appended to the existing list instead of replacing it. The attribute applies to a member or a whole type; member beats type, and both beat the options-level `PreferredObjectCreationHandling`.

## Pattern 11 — Choose a converter

<xref:Bodu.Text.Bencode.Serialization.BencodeConverterAttribute> selects the converter for a member, or for every use of a type:

```csharp
public sealed class Stamped
{
    [BencodeConverter(typeof(UnixSecondsConverter))]
    public DateTimeOffset CreatedAt { get; set; }
}

// → d9:CreatedAti1700000000ee
```

The referenced type must derive from <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1> (or its factory) and expose a public parameterless constructor. Writing converters — including the factory pattern and resolution order — is covered in [Writing converters](converters.md).

## Pattern 12 — Name enum members

<xref:Bodu.Text.Bencode.Serialization.BencodeStringEnumMemberNameAttribute> renames an individual enumeration member when the enum is serialized by name:

```csharp
[BencodeConverter(typeof(BencodeStringEnumConverter<Status>))]
public enum Status
{
    Active,

    [BencodeStringEnumMemberName("on-hold")]
    OnHold,
}

// Status.OnHold serializes as the byte string 7:on-hold
```

The attribute applies only to by-name serialization (the default enum handling, or an explicit string-enum converter); it is a no-op when the enum is written as an integer.

## Precedence at a glance

When several settings could govern the same member, the closest one wins:

1. a member-level attribute (`[BencodePropertyName]`, `[BencodeIgnore]`, `[BencodeConverter]`, `[BencodeObjectCreationHandling]`, …);
2. a type-level attribute (`[BencodeNamingPolicy]`, `[BencodeConverter]`, `[BencodeUnmappedMemberHandling]`, `[BencodeObjectCreationHandling]`);
3. the serializer options (`PropertyNamingPolicy`, `PropertyNameCaseInsensitive`, `Converters`, `DefaultIgnoreCondition`, `UnmappedMemberHandling`, `PreferredObjectCreationHandling`, `IncludeFields`).

A member with no explicit `[BencodeIgnore]` falls back to the options-level `DefaultIgnoreCondition` (default `Never`); an absent `[BencodeNamingPolicy]` falls back to `PropertyNamingPolicy`. The `[BencodePropertyName]` name always wins over every policy at any level.

## See also

- **[Using Bencode](using.md)** — the format walk-through these attributes shape.
- **[Writing converters](converters.md)** — `[BencodeConverter]` placement and the resolution order in detail.
- **[Bencode guides](index.md)** — the full guide index for the Bencode serializer.
- **[Text & Serialization guides](../../topics/text-and-serialization.md)** — every guide in this topic, across Bodu.Text.Encoding, Bodu.Text.Formats, and the serializers.
