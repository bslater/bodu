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
| `char` | single-character string | single-character string | A source of any other length raises <xref:Bodu.Text.Yaml.YamlSerializationException>. |
| `Guid` | string, canonical 36-character form | any `Guid.Parse`-accepted form | |
| `Uri` | string, the original URI text | string | Relative and absolute URIs round-trip. |
| `bool` | Boolean (`true` / `false`) | Boolean | Under `SpecVersion = V1_1`, the read path also accepts `yes` / `no` / `on` / `off` and `y` / `n`. |
| `sbyte` `byte` `short` `ushort` `int` `uint` `long` `nint` `nuint` | integer scalar | integer | Checked conversions; a value outside the target type raises <xref:Bodu.Text.Yaml.YamlSerializationException>. |
| `ulong` | integer scalar, or a quoted string above `long.MaxValue` | integer | A value beyond `long.MaxValue` is emitted as a quoted decimal string so it round-trips without overflow. |
| `double` | float scalar | float | `NaN`, `+∞`, and `−∞` write as `.nan`, `.inf`, and `-.inf`. |
| `float` | float scalar | float | Widens to `double` on write; narrows on read. |
| `decimal` | quoted string of the exact invariant text | float scalar or string | Quoted because the exact text would otherwise resolve as a `double`; the quoting preserves the full `decimal` precision. |
| `DateTime` | string, round-trip (`"o"`) ISO-8601 | string | Read with `DateTimeStyles.RoundtripKind`. There is no native YAML timestamp type; the value travels as a string scalar. |
| `DateTimeOffset` | string, round-trip (`"o"`) ISO-8601 | string | As `DateTime`, preserving the offset. |
| `TimeSpan` | string, the invariant `TimeSpan` form | string | |
| `null` | the null scalar (`null`) | `null`, `~`, or the empty scalar | A null member is omitted instead when `IgnoreNullValues` is set. |
| `enum` (any) | string, the member name | string (case-insensitive) or integer | Integers instead of names when `WriteEnumsAsStrings = false`. |

> [!NOTE]
> `decimal`, `DateTime`, `DateTimeOffset`, and `TimeSpan` are not YAML core-schema kinds — there is no native timestamp or high-precision decimal scalar. The serializer carries each as a string (a `decimal` is additionally quoted so plain resolution does not turn it back into a `double`), and the read path parses that string with `CultureInfo.InvariantCulture`. They therefore round-trip exactly through `Serialize` / `Deserialize`, but a value typed `object` reads back as the underlying string, not as the original CLR type.

The <xref:Bodu.Text.Yaml.YamlNumberHandling> option governs the integer/float boundary on read: `Strict` (the default) rejects a non-integral or out-of-range float bound to an integer target, while `AllowFloatToInteger` truncates an integer-valued float toward zero. A plain integer scalar binds to a floating-point target under either policy. Integer scalars are resolved across the YAML radix forms — decimal, hexadecimal (`0x`), and the `0o` octal prefix (plus YAML 1.1's leading-zero octal under `SpecVersion = V1_1`) — so `0xFF`, `0o17`, and `255` all bind to the same `int`.

## Structural and document-model types

| .NET type | YAML representation | Notes |
|---|---|---|
| arrays, `List<T>`, `IEnumerable<T>` and its interfaces, sets, and concrete collections with a parameterless constructor and `Add` | sequence | Block-style on write; an empty collection writes as flow `[]`. On read the elements are bound into a `List<T>` and copied into the requested concrete type. |
| `IDictionary<TKey,TValue>` / `IReadOnlyDictionary<TKey,TValue>` and concrete dictionaries | mapping | Written in insertion order. Keys are stringified through `Convert.ToString`; on read a non-`string` key type is parsed back (enums by name, other keys via `Convert.ChangeType`). Mapping keys resolve to unique scalar strings (the Bodu YAML Core Tree Profile) — a duplicate stringified key on write raises <xref:Bodu.Text.Yaml.YamlSerializationException>. |
| plain classes and structs | mapping | The catch-all object converter, consulted last; properties first (reflection order), then public fields when `IncludeFields` is set. Read requires a public parameterless constructor and sets each writable member. |
| `object`-typed members | the runtime type's form on write | On **read**, an `object` target binds to a loosely-typed graph: a `Dictionary<string, object?>` for a mapping, a `List<object?>` for a sequence, and `bool` / `long` / `double` / `string` / `null` for scalars — **not** a <xref:Bodu.Text.Yaml.Document.YamlElement>. |
| `Nullable<T>` | the underlying value, or the null scalar | A null scalar binds to `null`; otherwise the value binds as `T`. |
| <xref:Bodu.Text.Yaml.Nodes.YamlNode> (and `YamlObject` / `YamlArray` / `YamlValue`) | the node's own kind | On write the node is an `IEnumerable` / `IDictionary` and is emitted structurally. To round-trip a document without a model, prefer `YamlNode.Parse` / `ToYamlString` directly (see [Using YAML](using.md)). |

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

On read an enum binds from a name (case-insensitively, through `Enum.Parse`) or from an integer scalar (through `Enum.ToObject`), regardless of `WriteEnumsAsStrings` — the flag affects only the write side. There is no per-member rename attribute (no `[YamlStringEnumMemberName]`) and no string/number enum converter type — the single `WriteEnumsAsStrings` flag is the whole enum surface. To rename individual enum members on the wire, write a [custom converter](converters.md).

## What YAML does not need a decision for

Unlike TOML, YAML has no `decimal`-handling or `byte[]`-handling option: `decimal` always travels as quoted exact text (above), and a `byte[]` is treated as an ordinary sequence of `byte` elements rather than a single encoded scalar. The only enum decision is the single `WriteEnumsAsStrings` flag. Anything outside the provisioned set — a value type rendered as one scalar, a type with a bespoke mapping shape, a base64 `byte[]`, or a per-member enum rename — is the job of a [custom converter](converters.md).

## Where to go next

- [Writing converters](converters.md) — overriding a built-in and the resolution order.
- [Mapping attributes](attributes.md) — the declarative layer over the converters.
- [Using YAML](using.md) — the walk-through the tables above back up.
- [Bodu.Text.Yaml core concepts](../../../docs/serialization/yaml/concepts.md) — the value-mapping summary in the family vocabulary.
- [Bodu serializer guides](../index.md) and the [Text & Serialization guides](../../topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Yaml.Serialization.YamlConverter`1>, <xref:Bodu.Text.Yaml.YamlSerializerOptions>, <xref:Bodu.Text.Yaml.YamlNumberHandling>.
