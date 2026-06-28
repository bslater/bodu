---
title: Built-in converter catalog
---

# Built-in converter catalog

Every type that <xref:Bodu.Text.Bencode.BencodeSerializer> handles without a user converter is served by a **built-in converter** — internally an ordinary <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1>. This page catalogs that set — which .NET types are provisioned, how each is represented on the wire, and what the read path accepts. Resolution order and the rules for overriding a built-in with your own converter are covered in [Writing converters](converters.md).

The library follows the design shared across the [Bodu serializers](../index.md): exact-type scalar converters first, factories for open type families (nullables, enums, dictionaries, collections, plain objects) last, with the document object model bridges ahead of everything so a DOM value is never claimed by a structural factory.

Bencode (BEP 3) has exactly two scalar forms — integers and byte strings — and the built-in set deliberately covers only types with a native mapping. Boolean, floating-point, `char`, `Guid`, `Uri`, `Version`, `TimeSpan`, and the date-time types are **not** provisioned: serializing one without a registered converter surfaces a missing-converter error rather than silently inventing a lossy representation. See [Pattern 5 in Writing converters](converters.md#pattern-5--map-a-type-the-format-cannot-represent) for the bridging recipe.

## Scalars and binary data

| .NET type | Bencode representation (write) | Read accepts | Notes |
|---|---|---|---|
| `string` | byte string (UTF-8) | byte string | |
| `byte[]` | byte string | byte string | The native binary form; no transcoding. |
| `Memory<byte>` / `ReadOnlyMemory<byte>` | byte string | byte string | |
| `sbyte` `byte` `short` `ushort` `int` `uint` `long` `nint` `nuint` | integer (`i…e`) | integer | Checked conversions through the signed 64-bit surface; a value outside the target type's range throws <xref:Bodu.Text.Bencode.BencodeSerializationException> on read. |
| `ulong` | integer | integer | Dedicated converter on the unsigned surface — the full `[0, ulong.MaxValue]` range round-trips. |
| `Int128` | integer | integer | Confined by checked conversion to the signed 64-bit surface. |
| `UInt128` | integer | integer | Rides the unsigned surface like `ulong`; values above `ulong.MaxValue` throw on write. |
| `enum` (any) | byte string, the member name | byte string (case-insensitive) or integer | The default enum handling — no converter needed; per-member names via `[BencodeStringEnumMemberName]`. The explicit string and number enum converters below override it.|

## Structural and document-model types

| .NET type | Bencode representation | Notes |
|---|---|---|
| `Nullable<T>` | underlying type's form | Bencode has no null; a null member is omitted before any converter runs. |
| arrays, `List<T>`, list interfaces, `Queue<T>` / `Stack<T>` / concurrent collections | list (`l…e`) | A `Stack<T>` round-trip reverses: the writer emits pop order. |
| dictionaries with `string`, integer, `enum`, `Guid`, `bool`, or `char` keys | dictionary (`d…e`) | Keys are emitted in canonical bytewise order. |
| plain classes and structs | dictionary | The catch-all object converter, consulted last. |
| `object`-typed members | runtime type's form on write; <xref:Bodu.Text.Bencode.Document.BencodeElement> on read | A bare `new object()` writes an empty dictionary; null members are omitted. |
| <xref:Bodu.Text.Bencode.Nodes.BencodeNode> family | the node's own kind | Mutable DOM bridge. |
| <xref:Bodu.Text.Bencode.Document.BencodeElement> | the element's own kind | Read produces an element backed by a non-pooled internal document — the `BencodeElement.Clone` lifetime, no disposal needed. The subtree is re-parsed under the serializer's dictionary-key leniency. |
| <xref:Bodu.Text.Bencode.Document.BencodeDocument> | the document's root | A deserialized document is **caller-owned**: dispose it to return its pooled buffer. |

## Public enum converters

Registered on the options or referenced from a `[BencodeConverter(...)]` attribute:

- <xref:Bodu.Text.Bencode.Serialization.BencodeStringEnumConverter> / `BencodeStringEnumConverter<TEnum>` — member-name byte strings with an optional naming policy and integer-on-read flag.
- `BencodeNumberEnumConverter<TEnum>` — the underlying numeric value as a Bencode integer.

## Types that need a user converter

| Capability | Bencode |
|---|---|
| Native scalar kinds | integer, byte string |
| Types needing a representation decision | none — byte strings are native binary |
| Types served only by a user converter | `bool`, floating-point (`double` / `float` / `decimal`), `char`, `Guid`, `Uri`, `Version`, `TimeSpan`, the date-time types |
| Document root | any value kind |
| `object` / element / document bridges | yes |

Each of the converter-only types maps cleanly onto an integer or a byte string — see [Pattern 5 in Writing converters](converters.md#pattern-5--map-a-type-the-format-cannot-represent) and [Pattern 6 in Using Bencode](using.md#pattern-6--handle-the-kinds-bencode-cannot-represent) for worked bridges.

## See also

- [Writing converters](converters.md) — overriding a built-in, factories, and resolution order.
- [Using Bencode](using.md) — the format walk-through the tables above back up.
- [Mapping attributes](attributes.md) — the declarative layer over the converters.
- [Core concepts](../../../docs/serialization/bencode/concepts.md) — the value-mapping summary in the Bencode vocabulary.
- [Text & Serialization guides](../../topics/text-and-serialization.md) and the [topic overview](../../../docs/topics/text-and-serialization.md).
- API reference — <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1>.
