---
title: Built-in converter catalog
---

# Built-in converter catalog

Every type that <xref:Bodu.Text.Yaml.YamlSerializer> handles without a user converter is served by a **built-in converter**. This page catalogs that set — which .NET types are provisioned, how each appears in YAML on write, and what the read path accepts. Resolution order and the rules for overriding a built-in with your own converter are in [Writing converters](converters.md).

YAML carries the JSON-compatible core scalar kinds — string, integer, float, Boolean, and null — so most everyday .NET types map without any converter at all. The writer emits **block-style** collections (an empty container falls back to flow `[]` / `{}`), and mappings preserve insertion order.

## Scalars

| .NET type | YAML representation (write) | Read accepts | Notes |
|---|---|---|---|
| `string` | string scalar | string | The scalar style (plain / quoted / block) is chosen by the writer and recorded on read by <xref:Bodu.Text.Yaml.Document.YamlElement.ScalarStyle>. |
| `char` | single-character string | single-character string | Multi-character strings are rejected. |
| `Guid` | string, canonical 36-character form | `D`-format string | |
| `Uri` | string, the original URI text | string | Relative and absolute URIs round-trip. |
| `bool` | Boolean (`true` / `false`) | Boolean | Under `SpecVersion = V1_1`, the read path also accepts `yes` / `no` / `on` / `off`. |
| `sbyte` `byte` `short` `ushort` `int` `uint` `long` `ulong` `nint` `nuint` | integer scalar | integer | Checked conversions; a value outside the target type is a serialization error. |
| `double` | float scalar | float | |
| `float` | float scalar | float | Widens on write; narrows on read. |
| `null` | the null scalar | null | A null member is omitted instead when `IgnoreNullValues` is set. |
| `enum` (any) | string, the member name | string (case-insensitive) or integer | Integers instead of names when `WriteEnumsAsStrings = false`. |

The <xref:Bodu.Text.Yaml.YamlNumberHandling> option governs the integer/float boundary on read: `Strict` (the default) requires a float scalar for a floating-point target, while `AllowFloatToInteger` lets an integer-valued float bind to an integer member.

## Structural and document-model types

| .NET type | YAML representation | Notes |
|---|---|---|
| arrays, `List<T>`, list interfaces, sets, and the common collection types | sequence | Block-style on write; an empty collection writes as flow `[]`. |
| dictionaries with string-convertible keys | mapping | Written in insertion order. Mapping keys resolve to unique scalar strings (the Bodu YAML Core Tree Profile). |
| plain classes and structs | mapping | The catch-all object converter, consulted last; members in declaration order. |
| `object`-typed members | runtime type's form on write; <xref:Bodu.Text.Yaml.Document.YamlElement> on read | A bare `new object()` writes an empty mapping; null members are omitted. |
| <xref:Bodu.Text.Yaml.Nodes.YamlNode> (and `YamlObject` / `YamlArray` / `YamlValue`) | the node's own kind | Mutable DOM bridge. |
| <xref:Bodu.Text.Yaml.Document.YamlElement> | the element's own kind | Read produces an element over the parsed document. |

## Fields

Properties map by default. Public **fields** participate when <xref:Bodu.Text.Yaml.YamlSerializerOptions.IncludeFields> is `true`, following the same naming-policy, `[YamlPropertyName]`, and `[YamlIgnore]` rules as properties:

```csharp
public sealed class Counter
{
    public int Total { get; set; }
    public int Retries;   // included when IncludeFields is true
}

var options = new YamlSerializerOptions { IncludeFields = true };
string yaml = YamlSerializer.Serialize(new Counter { Total = 3, Retries = 1 }, options);
```

```yaml
Total: 3
Retries: 1
```

## Enums

By default an enum is written as its **member-name string** and read back case-insensitively (or as an integer). Set <xref:Bodu.Text.Yaml.YamlSerializerOptions.WriteEnumsAsStrings> to `false` to write the underlying integer instead:

```csharp
public enum Status { Active, OnHold }

string asString = YamlSerializer.Serialize(new { State = Status.OnHold });
// State: OnHold

var asInt = new YamlSerializerOptions { WriteEnumsAsStrings = false };
string asInteger = YamlSerializer.Serialize(new { State = Status.OnHold }, asInt);
// State: 1
```

There is no per-member rename attribute (no `[YamlStringEnumMemberName]`) and no string/number enum converter type — the single `WriteEnumsAsStrings` flag is the whole enum surface. To rename individual enum members on the wire, write a [custom converter](converters.md).

## What YAML does not need a decision for

Unlike TOML, YAML has no `decimal`-handling or `byte[]`-handling option — the catalog above is the complete provisioned set, and the only enum decision is the single `WriteEnumsAsStrings` flag. Anything outside this set — a value type rendered as one scalar, a type with a bespoke mapping shape, or a per-member enum rename — is the job of a [custom converter](converters.md).

## Where to go next

- [Writing converters](converters.md) — overriding a built-in and the resolution order.
- [Mapping attributes](attributes.md) — the declarative layer over the converters.
- [Using YAML](using.md) — the walk-through the tables above back up.
- [Bodu.Text.Yaml core concepts](../../../docs/serialization/yaml/concepts.md) — the value-mapping summary in the family vocabulary.
- [Bodu serializer guides](../index.md) and the [Text & Serialization guides](../../topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1>, <xref:Bodu.Text.Yaml.YamlSerializerOptions>, <xref:Bodu.Text.Yaml.YamlNumberHandling>.
