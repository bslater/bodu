---
title: Built-in converter catalog
---

# Built-in converter catalog

Every type that <xref:Bodu.Text.Toml.TomlSerializer> handles without a user converter is served by a **built-in converter** — internally an ordinary <xref:Bodu.Text.Toml.Serialization.TomlConverter`1>. This page catalogs that set — which .NET types are provisioned, how each is represented on the wire, and what the read path accepts. Resolution order and the rules for overriding a built-in with your own converter are covered in [Writing converters](converters.md). The sibling libraries ([Bodu.Text.Bencode](../bencode/index.md), [Bodu.Text.Yaml](../yaml/index.md)) ship their own catalog over the same machinery.

The design is: exact-type scalar converters first, factories for open type families (nullables, enums, dictionaries, collections, plain objects) last, with the document object model bridges ahead of everything so a DOM value is never claimed by a structural factory. The full ordering, top to bottom, is:

1. the **DOM bridges** — `TomlNode`, `TomlElement`, `TomlDocument` — so a DOM value always flows through its own bridge rather than the dictionary or object factory;
2. the **exact-type scalar converters** (`string`, `bool`, `char`, `Guid`, `Uri`, `Version`, `TimeSpan`, `double`, `float`, `Half`, `decimal`, and the four date-time types), so a scalar is never captured by the object factory;
3. the **byte-array and memory-of-byte converters**, ahead of the collection factory so binary data maps by its dedicated converter rather than as a sequence of integers;
4. the **integer**, **enum**, and **nullable** factories;
5. the **dictionary factory** ahead of the **collection factory**, so a string-keyed dictionary becomes a table rather than a collection;
6. the **`object` converter**, so an `object`-typed member dispatches on its runtime type instead of mapping to an empty table;
7. the **object factory** last, as the catch-all that writes a plain class or struct as a table.

A user converter, or a converter named by a `[TomlConverter]` attribute, is consulted ahead of this entire list — see [Writing converters](converters.md) for the precedence ladder.

## Scalars

| .NET type | TOML representation (write) | Read accepts | Notes |
|---|---|---|---|
| `string` | string | string | |
| `bool` | boolean | boolean | |
| `char` | single-character string | single-character string | Multi-character strings are rejected. |
| `Guid` | string, canonical 36-character `D` format | `D`-format string | |
| `Uri` | string, the original URI text | string | Relative and absolute URIs round-trip. |
| `Version` | string, the component form (`"1.2.3.4"`) | version string | Leading/trailing whitespace is rejected. |
| `TimeSpan` | string, invariant constant format (`"1.02:03:04.5670000"`) | `"c"`-format string | The round-trippable constant format. |
| `double` | float (including `inf` / `-inf` / `nan`) | float | |
| `float` | float | float | Widens to binary64 on write; narrows on read. |
| `Half` | float | float | Exact widening on write; **saturating** IEEE 754 narrow on read — an out-of-range finite float reads back as ±infinity. |
| `decimal` | float **or** invariant string, per <xref:Bodu.Text.Toml.TomlDecimalHandling> | float, integer, **or** string | `Float` (default) is native but lossy beyond binary64; `String` round-trips all 28 digits. Read accepts all three forms regardless of the setting. |
| `sbyte` `byte` `short` `ushort` `int` `uint` `long` `ulong` `nint` `nuint` | integer | integer | Checked conversions; a value outside the target type or TOML's signed 64-bit range is a serialization error. |
| `Int128` / `UInt128` | integer | integer | Confined by checked conversion to the signed 64-bit range TOML stores; larger values throw on write. |
| `DateTimeOffset` | offset date-time | offset date-time | |
| `DateTime` | local date-time (`Unspecified`) or offset date-time (`Utc` / `Local`) | matching kind | Kind-aware on write. |
| `DateOnly` | local date | local date | |
| `TimeOnly` | local time | local time | |
| `enum` (any) | string, the member name | string (case-insensitive) or integer | Per-member names via `[StringEnumMemberName]`; see the enum converters below. |

## Binary data

| .NET type | TOML representation (write) | Read accepts | Notes |
|---|---|---|---|
| `byte[]` | integer array **or** Base64 string, per <xref:Bodu.Text.Toml.TomlByteArrayHandling> | either form | Default is the integer array. |
| `Memory<byte>` / `ReadOnlyMemory<byte>` | same as `byte[]` | either form | Shares the byte-array logic and setting. |

## Structural and document-model types

| .NET type | TOML representation | Notes |
|---|---|---|
| `Nullable<T>` | underlying type's form | TOML has no null; a null member is omitted before any converter runs. |
| arrays, `List<T>`, list interfaces, concrete `ICollection<T>`, `Queue<T>` / `Stack<T>` / `ConcurrentQueue<T>` / `ConcurrentStack<T>` / `ConcurrentBag<T>` | array | A `Stack<T>` round-trip reverses: the writer emits pop order. |
| dictionaries with `string`, integer, `enum`, `Guid`, `bool`, or `char` keys | table | Non-string keys are written in invariant text. The newer scalars (`Version`, `TimeSpan`, `decimal`, `Half`, 128-bit integers) are deliberately not key types. |
| plain classes and structs | table | The catch-all object converter, consulted last. |
| `object`-typed members | runtime type's form on write; <xref:Bodu.Text.Toml.Document.TomlElement> on read | A bare `new object()` writes an empty table; null members are omitted. |
| <xref:Bodu.Text.Toml.Nodes.TomlNode> (and `TomlObject` / `TomlArray` / `TomlValue`) | the node's own kind | Mutable DOM bridge. |
| <xref:Bodu.Text.Toml.Document.TomlElement> | the element's own kind | Read produces an element backed by an internal, garbage-collected document — no disposal needed. |
| <xref:Bodu.Text.Toml.Document.TomlDocument> | the document's root | A deserialized document is **caller-owned**: dispose it when finished. |

## Public enum converters

Registered on the options or referenced from a `[Converter(...)]` attribute:

- <xref:Bodu.Text.Toml.Serialization.TomlStringEnumConverter> / `TomlStringEnumConverter<TEnum>` — member-name strings with an optional naming policy and integer-on-read flag.
- `TomlNumberEnumConverter<TEnum>` — the underlying numeric value as a TOML integer.

## Representation decisions at a glance

TOML's native scalar kinds — string, integer, float, boolean, and the four date-time forms — cover nearly every common BCL type without a choice to make. Two types carry a representation selector on the options:

| Type | Selector | Forms |
|---|---|---|
| `decimal` | <xref:Bodu.Text.Toml.TomlDecimalHandling> | native float (default, binary64-bounded) or a lossless invariant string |
| `byte[]` / memory-of-byte | <xref:Bodu.Text.Toml.TomlByteArrayHandling> | integer array (default) or a Base64 string |

The document root must map to a table, so a top-level scalar or array throws. The `object` / element / document bridges let a DOM value flow through the serializer untouched.

## See also

- [Writing converters](converters.md) — overriding a built-in, factories, and resolution order.
- [Using TOML](using.md) — the format walk-through the tables above back up.
- [Mapping attributes](attributes.md) — the declarative layer over the converters.
- [Core concepts](../../../docs/serialization/toml/concepts.md) — the value-mapping summary in the family vocabulary.
- [Text & Serialization guides](../../topics/text-and-serialization.md) and the [topic overview](../../../docs/topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Toml.Serialization.TomlConverter`1>, <xref:Bodu.Text.Toml.TomlByteArrayHandling>, <xref:Bodu.Text.Toml.TomlDecimalHandling>.
