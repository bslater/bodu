---
title: Mapping attributes
---

# Mapping attributes

**Bodu.Text.Yaml** shapes how a type maps to the wire with a small, deliberate surface: two attributes, the naming policies, and a handful of options flags. This is narrower than the [TOML](../toml/index.md) and [Bencode](../bencode/index.md) siblings, which carry a larger attribute family — YAML provides **only** `[YamlPropertyName]` and `[YamlIgnore]`, and routes anything beyond renaming and ignoring to a [custom converter](converters.md). The two attributes live in the `Bodu.Text.Yaml.Serialization` namespace.

## Pattern 1 — Rename a member

<xref:Bodu.Text.Yaml.Serialization.YamlPropertyNameAttribute> pins the serialized key for one member, beating any naming policy:

```csharp
using Bodu.Text.Yaml.Serialization;

public sealed class Profile
{
    [YamlPropertyName("display-name")]
    public string DisplayName { get; set; } = "Ada";
}
```

```yaml
display-name: Ada
```

The explicit name always wins over the options-level naming policy.

## Pattern 2 — Apply a naming policy

The options-level <xref:Bodu.Text.Yaml.YamlSerializerOptions.PropertyNamingPolicy> renames every member that does not carry an explicit `[YamlPropertyName]`. The policies are `CamelCase`, `SnakeCaseLower`, and `KebabCaseLower` — lower-cased snake and kebab plus camel; there are no upper-case variants and no scenario preset:

```csharp
var options = new YamlSerializerOptions
{
    PropertyNamingPolicy = YamlNamingPolicy.SnakeCaseLower,
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

## Pattern 3 — Exclude a member

<xref:Bodu.Text.Yaml.Serialization.YamlIgnoreAttribute> drops a member unconditionally — it is never written and never read:

```csharp
using Bodu.Text.Yaml.Serialization;

public sealed class Account
{
    public string Name { get; set; } = "svc";

    [YamlIgnore]
    public string? Secret { get; set; }
}
```

```yaml
Name: svc
```

There is no conditional form of `[YamlIgnore]`. To omit members whose value happens to be `null` across the whole document, set <xref:Bodu.Text.Yaml.YamlSerializerOptions.IgnoreNullValues> on the options instead (Pattern 4).

## Pattern 4 — Options flags

The remaining shaping is on <xref:Bodu.Text.Yaml.YamlSerializerOptions>:

| Flag | Effect |
|---|---|
| `IgnoreNullValues` | Omits any member whose value is `null` on write. |
| `WriteEnumsAsStrings` | `true` (default) writes enums as member-name strings; `false` writes the underlying integer. |
| `PropertyNameCaseInsensitive` | Matches mapping keys to members case-insensitively on read. |
| `IncludeFields` | Includes public fields alongside properties. |
| `UnmappedMemberHandling` | <xref:Bodu.Text.Yaml.YamlUnmappedMemberHandling> — `Skip` (default) ignores keys that map to no member; `Disallow` raises <xref:Bodu.Text.Yaml.YamlSerializationException>. |

```csharp
var options = new YamlSerializerOptions
{
    IgnoreNullValues = true,
    WriteEnumsAsStrings = false,         // enums as integers
    IncludeFields = true,                // public fields participate
    PropertyNameCaseInsensitive = true,  // case-insensitive key matching
    UnmappedMemberHandling = YamlUnmappedMemberHandling.Disallow,
};
```

With `IncludeFields` set, a public field maps exactly like a property — it honors the naming policy and `[YamlPropertyName]` / `[YamlIgnore]`:

```csharp
public sealed class Counter
{
    public int Total { get; set; }
    public int Retries;   // included when IncludeFields is true
}
```

## What YAML deliberately leaves out

YAML does **not** carry the wider attribute family the siblings expose. There is no `[YamlConverter]`, no `[YamlPropertyOrder]`, no `[YamlRequired]`, no `[YamlInclude]`, no `[YamlExtensionData]`, no `[YamlConstructor]`, no `[YamlObjectCreationHandling]`, and no `[YamlStringEnumMemberName]`. For anything beyond renaming, ignoring, and the options flags — a custom wire form, a value type rendered as a single scalar, or a type the defaults reject — write a [custom converter](converters.md) and register it on `options.Converters`.

## Precedence at a glance

When several settings could govern the same member, the closest one wins:

1. a member-level `[YamlPropertyName]` (name) or `[YamlIgnore]` (exclusion);
2. the serializer options (`PropertyNamingPolicy`, `IgnoreNullValues`, `IncludeFields`, `UnmappedMemberHandling`, …).

## Where to go next

- [Writing converters](converters.md) — the route for everything beyond renaming and ignoring.
- [Built-in converter catalog](builtin-converters.md) — which types map without any attribute at all.
- [Using YAML](using.md) — the end-to-end walk-through.
- [Bodu.Text.Yaml core concepts](../../../docs/serialization/yaml/concepts.md) — where attributes fit in the converter-resolution picture.
- [Bodu serializer guides](../index.md) and the [Text & Serialization guides](../../topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Yaml.Serialization.YamlPropertyNameAttribute>, <xref:Bodu.Text.Yaml.Serialization.YamlIgnoreAttribute>, <xref:Bodu.Text.Yaml.YamlNamingPolicy>.
