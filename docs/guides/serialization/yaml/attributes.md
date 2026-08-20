---
title: Mapping attributes
---

# Mapping attributes

**Bodu.Text.Yaml** shapes how a type maps to the wire with the same shared attribute family as its [TOML](../toml/index.md) and [Bencode](../bencode/index.md) siblings: every attribute lives in the `Bodu.Text.Serialization` namespace and derives <xref:Bodu.Text.Serialization.SerializationAttribute>. The patterns below show the most common members; the wider family — `[Converter]`, `[PropertyOrder]`, `[Required]`, `[Include]`, `[ExtensionData]`, `[Constructor]`, `[ObjectCreationHandling]`, `[NamingPolicy]`, and `[StringEnumMemberName]` — works exactly as it does in the siblings.

## Pattern 1 — Rename a member

<xref:Bodu.Text.Serialization.PropertyNameAttribute> pins the serialized key for one member, beating any naming policy:

```csharp
using Bodu.Text.Serialization;

public sealed class Profile
{
    [PropertyName("display-name")]
    public string DisplayName { get; set; } = "Ada";
}
```

```yaml
display-name: Ada
```

The explicit name always wins over the options-level naming policy.

## Pattern 2 — Apply a naming policy

The options-level <xref:Bodu.Text.Yaml.YamlSerializerOptions.PropertyNamingPolicy> renames every member that does not carry an explicit `[PropertyName]`. The shared <xref:Bodu.Text.Serialization.NamingPolicy> set applies — `CamelCase`, `SnakeCaseLower`/`SnakeCaseUpper`, and `KebabCaseLower`/`KebabCaseUpper` — a type can pin its own policy with `[NamingPolicy(KnownNamingPolicy…)]`, and the <xref:Bodu.Text.Yaml.YamlSerializerDefaults> scenario presets configure it for you (`Web` selects camel-case naming with case-insensitive matching):

```csharp
var options = new YamlSerializerOptions
{
    PropertyNamingPolicy = NamingPolicy.SnakeCaseLower,
};

public sealed class RetryPolicy
{
    public int MaxRetryCount { get; set; } = 5;
}

string yaml = YamlSerializer.Serialize(new RetryPolicy(), options);
```

```yaml
max_retry_count: 5
```

On the read path, <xref:Bodu.Text.Yaml.YamlSerializerOptions.PropertyNameCaseInsensitive> lets mapping keys match members regardless of case.

> [!NOTE]
> If two members resolve to the same wire key under the active policy and attributes — for example a `[PropertyName("name")]` that collides with another member's policy-derived `name` — serialization throws `InvalidOperationException` rather than silently emitting a duplicate key. Choose keys that stay unique after the policy is applied.

## Pattern 3 — Exclude a member

<xref:Bodu.Text.Serialization.IgnoreAttribute> drops a member unconditionally, or under a per-member condition:

```csharp
using Bodu.Text.Serialization;

public sealed class Account
{
    public string Name { get; set; } = "svc";

    [Ignore]
    public string? Secret { get; set; }

    [Ignore(Condition = IgnoreCondition.WhenWritingNull)]
    public string? Comment { get; set; }
}
```

```yaml
Name: svc
```

To omit members whose value happens to be `null` (or the type default) across the whole document, set the serializer-wide <xref:Bodu.Text.Yaml.YamlSerializerOptions.DefaultIgnoreCondition> instead (Pattern 4); a member-level `[Ignore(Condition = …)]` overrides it.

## Pattern 4 — Options flags

The remaining shaping is on <xref:Bodu.Text.Yaml.YamlSerializerOptions>:

| Flag | Effect |
|---|---|
| `DefaultIgnoreCondition` | <xref:Bodu.Text.Serialization.IgnoreCondition> — `Never` (default) writes every member; `WhenWritingNull` omits null members; `WhenWritingDefault` also omits type-default values. |
| `WriteEnumsAsStrings` | `true` (default) writes enums as member-name strings; `false` writes the underlying integer. |
| `PropertyNameCaseInsensitive` | Matches mapping keys to members case-insensitively on read. |
| `IncludeFields` | Includes public fields alongside properties. |
| `UnmappedMemberHandling` | <xref:Bodu.Text.Serialization.UnmappedMemberHandling> — `Skip` (default) ignores keys that map to no member; `Disallow` raises <xref:Bodu.Text.Yaml.YamlSerializationException>. |

```csharp
var options = new YamlSerializerOptions
{
    DefaultIgnoreCondition = IgnoreCondition.WhenWritingNull,
    WriteEnumsAsStrings = false,         // enums as integers
    IncludeFields = true,                // public fields participate
    PropertyNameCaseInsensitive = true,  // case-insensitive key matching
    UnmappedMemberHandling = UnmappedMemberHandling.Disallow,
};
```

With `IncludeFields` set, a public field maps exactly like a property — it honours the naming policy and `[PropertyName]` / `[Ignore]`:

```csharp
public sealed class Counter
{
    public int Total { get; set; }
    public int Retries;   // included when IncludeFields is true
}
```

A property is **written** whenever it has a public getter, but it is only **read back** when it also has a public setter — a get-only property serialises out and is silently skipped on deserialisation. Members are emitted properties-first (in reflection order) and then fields, so a field never interleaves with the properties even when `IncludeFields` is set.

## The wider attribute family

The remaining shared attributes work in YAML exactly as in the siblings:

| Attribute | Effect |
|---|---|
| `[Converter(typeof(…))]` | Binds a converter or converter factory to a member, property, or type — for example `[Converter(typeof(YamlStringEnumConverter<Status>))]`. |
| `[PropertyOrder(n)]` | Orders members on write (lower first; unattributed members keep reflection order at order `0`). |
| `[Required]` | A missing key on read raises <xref:Bodu.Text.Yaml.YamlSerializationException> (the C# `required` keyword is honored too). |
| `[Include]` | Surfaces a member with a non-public setter, or a field without `IncludeFields`. |
| `[ExtensionData]` | Captures unmapped keys into a `Dictionary<string, object?>` member and writes them back out. |
| `[Constructor]` | Selects the deserialization constructor for parameterized/immutable types. |
| `[ObjectCreationHandling(…)]` | Per-member replace-vs-populate on read, overriding <xref:Bodu.Text.Yaml.YamlSerializerOptions.PreferredObjectCreationHandling>. |
| `[NamingPolicy(…)]` | Pins a naming policy for one type. |
| `[StringEnumMemberName("…")]` | Renames one enum member on the wire (honored by the default enum handling and the [enum converters](converters.md)). |

For a custom wire form the attributes cannot express — a value type rendered as a single scalar, or a type the defaults reject — write a [custom converter](converters.md) and register it on `options.Converters`.

## Precedence at a glance

When several settings could govern the same member, the closest one wins:

1. a member-level `[PropertyName]` (name), `[Ignore]` (exclusion), or `[Converter]` (wire form);
2. a type-level `[NamingPolicy]` or `[Converter]`;
3. the serializer options (`PropertyNamingPolicy`, `DefaultIgnoreCondition`, `IncludeFields`, `UnmappedMemberHandling`, …).

## Where to go next

- [Writing converters](converters.md) — the route for everything beyond renaming and ignoring.
- [Built-in converter catalog](builtin-converters.md) — which types map without any attribute at all.
- [Using YAML](using.md) — the end-to-end walk-through.
- [Bodu.Text.Yaml core concepts](../../../docs/serialization/yaml/concepts.md) — where attributes fit in the converter-resolution picture.
- [Bodu serializer guides](../index.md) and the [Text & Serialization guides](../../topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Serialization.PropertyNameAttribute>, <xref:Bodu.Text.Serialization.IgnoreAttribute>, <xref:Bodu.Text.Serialization.NamingPolicy>.
