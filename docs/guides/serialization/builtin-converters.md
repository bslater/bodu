---
title: Built-in converter catalog
---

# Built-in converter catalog

Every type the serializers handle without a user converter is served by a **built-in converter**. This page catalogs that set for each library — which .NET types are provisioned, how each is represented on the wire, and what the read path accepts. Resolution order and the rules for overriding a built-in with your own converter are covered in [Writing converters](converters.md).

Both libraries follow the same `System.Text.Json`-shaped design: exact-type scalar converters first, factories for open type families (nullables, enums, dictionaries, collections, plain objects) last, with the document object model bridges ahead of everything so a DOM value is never claimed by a structural factory.

## TOML (`Bodu.Text.Toml`)

### Scalars

| .NET type | TOML representation (write) | Read accepts | Notes |
|---|---|---|---|
| `string` | string | string | |
| `bool` | boolean | boolean | |
| `char` | single-character string | single-character string | Multi-character strings are rejected. |
| `Guid` | string, canonical 36-character `D` format | `D`-format string | |
| `Uri` | string, the original URI text | string | Relative and absolute URIs round-trip. |
| `Version` | string, the component form (`"1.2.3.4"`) | version string | Leading/trailing whitespace is rejected, matching `System.Text.Json`. |
| `TimeSpan` | string, invariant constant format (`"1.02:03:04.5670000"`) | `"c"`-format string | Exact `System.Text.Json` parity. |
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
| `enum` (any) | string, the member name | string (case-insensitive) or integer | Per-member names via `[TomlStringEnumMemberName]`; see the enum converters below. |

### Binary data

| .NET type | TOML representation (write) | Read accepts | Notes |
|---|---|---|---|
| `byte[]` | integer array **or** Base64 string, per <xref:Bodu.Text.Toml.TomlByteArrayHandling> | either form | Default is the integer array. |
| `Memory<byte>` / `ReadOnlyMemory<byte>` | same as `byte[]` | either form | Shares the byte-array logic and setting. |

### Structural and document-model types

| .NET type | TOML representation | Notes |
|---|---|---|
| `Nullable<T>` | underlying type's form | TOML has no null; a null member is omitted before any converter runs. |
| arrays, `List<T>`, list interfaces, concrete `ICollection<T>`, `Queue<T>` / `Stack<T>` / `ConcurrentQueue<T>` / `ConcurrentStack<T>` / `ConcurrentBag<T>` | array | A `Stack<T>` round-trip reverses, matching `System.Text.Json`. |
| dictionaries with `string`, integer, `enum`, `Guid`, `bool`, or `char` keys | table | Non-string keys are written in invariant text. The newer scalars (`Version`, `TimeSpan`, `decimal`, `Half`, 128-bit integers) are deliberately not key types. |
| plain classes and structs | table | The catch-all object converter, consulted last. |
| `object`-typed members | runtime type's form on write; <xref:Bodu.Text.Toml.Document.TomlElement> on read | A bare `new object()` writes an empty table; null members are omitted. Mirrors `System.Text.Json`'s `JsonElement` behavior for `object` targets. |
| <xref:Bodu.Text.Toml.Nodes.TomlNode> (and `TomlObject` / `TomlArray` / `TomlValue`) | the node's own kind | Mutable DOM bridge. |
| <xref:Bodu.Text.Toml.Document.TomlElement> | the element's own kind | Read produces an element backed by an internal, garbage-collected document — no disposal needed. |
| <xref:Bodu.Text.Toml.Document.TomlDocument> | the document's root | A deserialized document is **caller-owned**: dispose it, as with `JsonDocument`. |

### Public enum converters

Registered on the options or referenced from a `[TomlConverter(...)]` attribute, mirroring `JsonStringEnumConverter` / `JsonNumberEnumConverter<TEnum>`:

- <xref:Bodu.Text.Toml.Serialization.TomlStringEnumConverter> / `TomlStringEnumConverter<TEnum>` — member-name strings with an optional naming policy and integer-on-read flag.
- `TomlNumberEnumConverter<TEnum>` — the underlying numeric value as a TOML integer.

## Bencode (`Bodu.Text.Bencode`)

Bencode (BEP 3) has exactly two scalar forms — integers and byte strings — and the built-in set deliberately covers only types with a native mapping. Boolean, floating-point, `char`, `Guid`, `Uri`, `Version`, `TimeSpan`, and the date-time types are **not** provisioned: serializing one without a registered converter surfaces a missing-converter error rather than silently inventing a lossy representation. See [Pattern 5 in Writing converters](converters.md#pattern-5--map-a-type-the-format-cannot-represent) for the bridging recipe.

### Scalars and binary data

| .NET type | Bencode representation (write) | Read accepts | Notes |
|---|---|---|---|
| `string` | byte string (UTF-8) | byte string | |
| `byte[]` | byte string | byte string | The native binary form; no transcoding. |
| `Memory<byte>` / `ReadOnlyMemory<byte>` | byte string | byte string | |
| `sbyte` `byte` `short` `ushort` `int` `uint` `long` `nint` `nuint` | integer (`i…e`) | integer | Checked conversions through the signed 64-bit surface. |
| `ulong` | integer | integer | Dedicated converter on the unsigned surface — the full `[0, ulong.MaxValue]` range round-trips. |
| `Int128` | integer | integer | Confined by checked conversion to the signed 64-bit surface. |
| `UInt128` | integer | integer | Rides the unsigned surface like `ulong`; values above `ulong.MaxValue` throw on write. |
| `enum` (any) | byte string, the member name | byte string (case-insensitive) or integer | Per-member names via `[BencodeStringEnumMemberName]`; see the enum converters below. |

### Structural and document-model types

| .NET type | Bencode representation | Notes |
|---|---|---|
| `Nullable<T>` | underlying type's form | Bencode has no null; a null member is omitted before any converter runs. |
| arrays, `List<T>`, list interfaces, `Queue<T>` / `Stack<T>` / concurrent collections | list (`l…e`) | A `Stack<T>` round-trip reverses, matching `System.Text.Json`. |
| dictionaries with `string`, integer, `enum`, `Guid`, `bool`, or `char` keys | dictionary (`d…e`) | Keys are emitted in canonical bytewise order. |
| plain classes and structs | dictionary | The catch-all object converter, consulted last. |
| `object`-typed members | runtime type's form on write; <xref:Bodu.Text.Bencode.Document.BencodeElement> on read | A bare `new object()` writes an empty dictionary; null members are omitted. |
| <xref:Bodu.Text.Bencode.Nodes.BencodeNode> family | the node's own kind | Mutable DOM bridge. |
| <xref:Bodu.Text.Bencode.Document.BencodeElement> | the element's own kind | Read produces an element backed by a non-pooled internal document — the `BencodeElement.Clone` lifetime, no disposal needed. The subtree is re-parsed under the serializer's dictionary-key leniency. |
| <xref:Bodu.Text.Bencode.Document.BencodeDocument> | the document's root | A deserialized document is **caller-owned**: dispose it to return its pooled buffer. |

### Public enum converters

- <xref:Bodu.Text.Bencode.Serialization.BencodeStringEnumConverter> / `BencodeStringEnumConverter<TEnum>` — member-name byte strings with an optional naming policy and integer-on-read flag.
- `BencodeNumberEnumConverter<TEnum>` — the underlying numeric value as a Bencode integer.

## Comparing the two

| Capability | TOML | Bencode |
|---|---|---|
| Native scalar kinds | string, integer, float, boolean, four date-time forms | integer, byte string |
| Types needing a representation decision | `decimal` (<xref:Bodu.Text.Toml.TomlDecimalHandling>), `byte[]` (<xref:Bodu.Text.Toml.TomlByteArrayHandling>) | none — byte strings are native binary |
| Types served only by a user converter | none in common use | `bool`, floats, `char`, `Guid`, `Uri`, `Version`, `TimeSpan`, date-times |
| Document root | must map to a table | any value kind |
| `object` / element / document bridges | yes | yes |
