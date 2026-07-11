---
title: Mapping attributes
---

# Mapping attributes

The TOML serializer exposes an attribute family for shaping how a type maps to the wire: every TOML attribute derives <xref:Bodu.Text.Toml.Serialization.TomlAttribute>. The sibling libraries ([Bodu.Text.Bencode](../bencode/index.md), [Bodu.Text.Yaml](../yaml/index.md)) mirror this family with their own prefix — see the [serializer guides hub](../index.md). Each pattern below shows the TOML form and its output.

## Pattern 1 — Rename a member

<xref:Bodu.Text.Toml.Serialization.PropertyNameAttribute> pins the serialized key for one member, beating any naming policy:

```csharp
public sealed class Profile
{
    [PropertyName("display-name")]
    public string DisplayName { get; set; } = "Ada";
}
```

```toml
display-name = "Ada"
```

## Pattern 2 — Apply a naming policy per type

<xref:Bodu.Text.Toml.Serialization.NamingPolicyAttribute> overrides the options-level `PropertyNamingPolicy` for one type:

```csharp
[NamingPolicy(KnownNamingPolicy.SnakeCaseLower)]
public sealed class RetryPolicy
{
    public int MaxRetryCount { get; set; } = 5;
}
```

```toml
max_retry_count = 5
```

A member carrying `[PropertyName]` is unaffected — the explicit name always wins. `KnownNamingPolicy.Unspecified` defers back to the options.

## Pattern 3 — Exclude members

<xref:Bodu.Text.Toml.Serialization.IgnoreAttribute> drops a member unconditionally, or under a condition:

```csharp
public sealed class Account
{
    public string Name { get; set; } = "svc";

    [Ignore]
    public string? Secret { get; set; }

    [Ignore(Condition = IgnoreCondition.WhenWritingNull)]
    public string? Comment { get; set; }
}
```

```toml
Name = "svc"
```

`Secret` is never written; `Comment` appears only when non-null. `WhenWritingDefault` extends the rule to default value-type values.

## Pattern 4 — Force a member in

<xref:Bodu.Text.Toml.Serialization.IncludeAttribute> binds non-public property accessors and surfaces public fields without turning on `IncludeFields` for the whole options:

```csharp
public sealed class Counter
{
    [Include]
    public int Total { get; private set; }

    [Include]
    public int Retries;
}
```

Both members now round-trip — the private setter is assigned on read, and the field participates like a property, following the same naming, ordering, ignore, required, and converter rules.

## Pattern 5 — Control write order

<xref:Bodu.Text.Toml.Serialization.PropertyOrderAttribute> reorders the emitted key/value lines; members without the attribute default to order zero and keep declaration order:

```csharp
public sealed class Manifest
{
    [PropertyOrder(2)]
    public string Name { get; set; } = "demo";

    [PropertyOrder(1)]
    public int Version { get; set; } = 3;
}
```

```toml
Version = 3
Name = "demo"
```

## Pattern 6 — Require a key

<xref:Bodu.Text.Toml.Serialization.RequiredAttribute> makes deserialization fail when the key is absent, with the same effect as declaring the member with the C# `required` keyword:

```csharp
public sealed class ServerConfig
{
    [Required]
    public string Host { get; set; } = string.Empty;
}

// Input without a "Host" key throws TomlSerializationException.
```

## Pattern 7 — Pick the deserialization constructor

When a type declares more than one constructor, <xref:Bodu.Text.Toml.Serialization.ConstructorAttribute> resolves the ambiguity:

```csharp
public sealed class Endpoint
{
    [Constructor]
    public Endpoint(string host, int port) => (Host, Port) = (host, port);

    public Endpoint(Uri uri) : this(uri.Host, uri.Port) { }

    public string Host { get; }
    public int Port { get; }
}
```

Without the attribute the serializer prefers a public parameterless constructor, then a single declared constructor, then the constructor with the most parameters.

## Pattern 8 — Capture unknown keys

<xref:Bodu.Text.Toml.Serialization.ExtensionDataAttribute> designates one member that collects every key that maps to no other member, and writes the collected entries back out on serialization:

```csharp
public sealed class ServerConfig
{
    public int Port { get; set; }

    [ExtensionData]
    public Dictionary<string, TomlNode>? Extra { get; set; }
}
```

The member must be a `TomlObject` or an `(I)Dictionary<string, TomlNode>`, and a type may declare at most one.

## Pattern 9 — Reject unknown keys

<xref:Bodu.Text.Toml.Serialization.UnmappedMemberHandlingAttribute> chooses, per type, between skipping a key that maps to no member (the default) and failing:

```csharp
[UnmappedMemberHandling(UnmappedMemberHandling.Disallow)]
public sealed class StrictConfig
{
    public int Port { get; set; }
}

// Input containing an unrecognised key throws TomlSerializationException.
```

A type with an extension-data member (Pattern 8) still captures unmapped keys into that member regardless of this setting.

## Pattern 10 — Populate instead of replace

<xref:Bodu.Text.Toml.Serialization.ObjectCreationHandlingAttribute> controls whether deserialization replaces a member's value with a fresh instance or populates the instance already held — useful for get-only collection properties:

```csharp
public sealed class Pipeline
{
    [ObjectCreationHandling(ObjectCreationHandling.Populate)]
    public List<string> Steps { get; } = new() { "restore" };
}
```

Deserialized entries are appended to the existing list instead of replacing it. The attribute applies to a member or a whole type; member beats type, and both beat the options-level `PreferredObjectCreationHandling`.

## Pattern 11 — Choose a converter

<xref:Bodu.Text.Toml.Serialization.ConverterAttribute> selects the converter for a member, or for every use of a type:

```csharp
public sealed class Package
{
    [Converter(typeof(VersionConverter))]
    public Version Version { get; set; } = new(1, 2, 3);
}
```

```toml
Version = "1.2.3"
```

The referenced type must derive from the format's converter base and expose a public parameterless constructor. Writing converters — including the factory pattern and resolution order — is covered in [Writing converters](converters.md).

## Pattern 12 — Name enum members

<xref:Bodu.Text.Toml.Serialization.StringEnumMemberNameAttribute> renames an individual enumeration member when the enum is serialized by name:

```csharp
[Converter(typeof(TomlStringEnumConverter<Status>))]
public enum Status
{
    Active,

    [StringEnumMemberName("on-hold")]
    OnHold,
}
```

```toml
Status = "on-hold"
```

The attribute applies only to by-name serialization (the default enum handling, or an explicit string-enum converter); it is a no-op when the enum is written as an integer.

## Precedence at a glance

When several settings could govern the same member, the closest one wins:

1. a member-level attribute (`[PropertyName]`, `[Ignore]`, `[TomlConverter]`, `[ObjectCreationHandling]`, …);
2. a type-level attribute (`[NamingPolicy]`, `[TomlConverter]`, `[UnmappedMemberHandling]`, `[ObjectCreationHandling]`);
3. the serializer options (`PropertyNamingPolicy`, `Converters`, `UnmappedMemberHandling`, `PreferredObjectCreationHandling`, `IncludeFields`).

## See also

- **[Using TOML](using.md)**, **[Writing converters](converters.md)**, **[Serialization callbacks](callbacks.md)**, **[Built-in converter catalog](builtin-converters.md)** — the other TOML guides.
- **[Text & Serialization guides](../../topics/text-and-serialization.md)** — every guide in this topic, across Bodu.Text.Encoding, Bodu.Text.Formats, and the serializers.
- **[TOML guides](index.md)** — the full guide index for Bodu.Text.Toml.
