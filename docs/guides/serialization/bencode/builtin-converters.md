---
title: Built-in converter catalog
---

# Built-in converter catalog

Every type that <xref:Bodu.Text.Bencode.BencodeSerializer> handles without a user converter is served by a **built-in converter** - internally an ordinary <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1>. This page catalogs that set - which .NET types are provisioned, how each is represented on the wire, and what the read path accepts. Resolution order and the rules for overriding a built-in with your own converter are covered in [Writing converters](converters.md).

The library follows the design shared across the [Bodu serializers](../index.md): exact-type scalar converters first, factories for open type families (nullables, enums, dictionaries, collections, plain objects) last, with the document object model bridges ahead of everything so a DOM value is never claimed by a structural factory.

Bencode (BEP 3) has exactly two scalar forms - integers and byte strings - and the built-in set deliberately covers only types with a native mapping. Boolean, floating-point, `char`, `Guid`, `Uri`, `Version`, `TimeSpan`, and the date-time types are **not** provisioned: serializing one without a registered converter surfaces a missing-converter error rather than silently inventing a lossy representation. See [Pattern 5 in Writing converters](converters.md#pattern-5---map-a-type-the-format-cannot-represent) for the bridging recipe.

## Scalars and binary data

| .NET type | Bencode representation (write) | Read accepts | Notes |
|---|---|---|---|
| `string` | byte string (UTF-8) | byte string | |
| `byte[]` | byte string | byte string | The native binary form; no transcoding. |
| `Memory<byte>` / `ReadOnlyMemory<byte>` | byte string | byte string | |
| `sbyte` `byte` `short` `ushort` `int` `uint` `long` `nint` | integer (`i…e`) | integer | Checked conversions through the signed 64-bit surface; a value outside the target type's range throws <xref:Bodu.Text.Bencode.BencodeSerializationException> on read, with the integer's offset as its `BytesOffset`. |
| `ulong` | integer | integer | Dedicated converter on the unsigned surface - the full `[0, ulong.MaxValue]` range round-trips; a negative integer throws <xref:Bodu.Text.Bencode.BencodeSerializationException> on read, with its offset as the `BytesOffset`. |
| `nuint` | integer | integer | Dedicated converter on the unsigned surface, like `ulong`: a 64-bit process reads and writes the full `[0, ulong.MaxValue]` range. A 32-bit process rejects an integer above `uint.MaxValue`, and every process rejects a negative integer, each with <xref:Bodu.Text.Bencode.BencodeSerializationException> on read and the integer's offset as its `BytesOffset`. |
| `Int128` | integer | integer | Confined by checked conversion to the signed 64-bit surface; an integer beyond it throws <xref:Bodu.Text.Bencode.BencodeSerializationException> on read, as for the narrower types. |
| `UInt128` | integer | integer | Rides the unsigned surface like `ulong`; values above `ulong.MaxValue` throw on write, and a negative integer throws on read, with its offset as the `BytesOffset`. |
| `enum` (any) | byte string, the member name | byte string (a name, as described below) or integer | The default enum handling - no converter needed; per-member names via `[StringEnumMemberName]`. The explicit string and number enum converters below override it.|

## Structural and document-model types

| .NET type | Bencode representation | Notes |
|---|---|---|
| `Nullable<T>` | underlying type's form | Bencode has no null; a null member is omitted before any converter runs. |
| arrays, `List<T>`, list interfaces, `Queue<T>` / `Stack<T>` / concurrent collections | list (`l…e`) | A `Stack<T>` round-trip reverses: the writer emits pop order. |
| dictionaries with `string`, integer, `enum`, `Guid`, `bool`, or `char` keys | dictionary (`d…e`) | Keys are emitted in canonical bytewise order. |
| plain classes and structs | dictionary | The catch-all object converter, consulted last. Delegates, reflection types (`Type` and the other `MemberInfo` types) and pointers are not plain objects: without a converter of your own, a member of one fails with `NotSupportedException` naming its type. |
| `object`-typed members | runtime type's form on write; <xref:Bodu.Text.Bencode.Document.BencodeElement> on read | A bare `new object()` writes an empty dictionary; null members are omitted. |
| <xref:Bodu.Text.Bencode.Nodes.BencodeNode> family | the node's own kind | Mutable DOM bridge. |
| <xref:Bodu.Text.Bencode.Document.BencodeElement> | the element's own kind | Read produces an element backed by a non-pooled internal document - the `BencodeElement.Clone` lifetime, no disposal needed. The subtree is re-parsed under the serializer's dictionary-key leniency. |
| <xref:Bodu.Text.Bencode.Document.BencodeDocument> | the document's root | A deserialized document is **caller-owned**: dispose it to return its pooled buffer. |

## Public enum converters

Registered on the options or referenced from a `[Converter(...)]` attribute:

- <xref:Bodu.Text.Bencode.Serialization.BencodeStringEnumConverter> / `BencodeStringEnumConverter<TEnum>` - member-name byte strings with an optional naming policy and integer-on-read flag.
- `BencodeNumberEnumConverter<TEnum>` - the underlying numeric value as a Bencode integer.

An integer read as an enum, by the default handling or by either converter, must lie within the range of the enum's underlying type: one outside it raises <xref:Bodu.Text.Bencode.BencodeSerializationException> at the integer's offset rather than being narrowed into the type, so `i300e` never binds a `byte`-based enum as 44. An integer inside the range binds whether or not it names a member, and an enum based on `ulong` reads integers up to `ulong.MaxValue`, which `BencodeNumberEnumConverter<TEnum>` also writes.

Names are matched as System.Text.Json's `JsonStringEnumConverter` matches them, by the default handling and by the string-enum converter alike. A member's wire name, from `[StringEnumMemberName]` or the converter's naming policy, binds only in its own case. The CLR name of a member that `[StringEnumMemberName]` does not rename also binds, in any case, but a renamed member's CLR name does not: with `[StringEnumMemberName("on")]` on `Enabled`, `2:on` binds, and `2:ON`, `7:Enabled` and `7:enabled` do not. A combination of flags is written as the comma-separated wire names of its flags, in the order `Enum.ToString` lists them, so under `NamingPolicy.SnakeCaseLower` `CanRead | CanExecute` is `21:can_read, can_execute`. It is read from such a list in any order, each name trimmed of white space, with a trailing comma tolerated but no empty name anywhere else. A list holds names only: an integer binds, where integers are allowed, only as the whole byte string, so `4:1, 4` is rejected.

A string-enum converter created with `allowIntegerValues: false` reads names only, as System.Text.Json's `JsonStringEnumConverter` does from .NET 9. It rejects a Bencode integer, and a byte string that holds one too: a byte string that is not a member's wire name and has an integer in any of its comma-separated parts (an optional sign and the digits `0` to `9`, ignoring white space around them), such as `1:2`, `4: +2 ` or `7:Read, 2`, raises <xref:Bodu.Text.Bencode.BencodeSerializationException> at the byte string's offset rather than binding the integer. The wire names are matched first, so a member renamed with `[StringEnumMemberName("1")]` still reads from `1:1`. The default handling, and a converter that allows integers, bind a byte string that is an integer, such as `1:2`, as the integer it holds. On write, such a converter refuses a value that is neither a member nor a combination of defined flags, such as `(Status)99` or a flags value with an undefined bit, and raises <xref:Bodu.Text.Bencode.BencodeSerializationException> rather than writing the decimal text it would reject on read; the default handling, and a converter that allows integers, write it as that text, `2:99`.

## Types that need a user converter

| Capability | Bencode |
|---|---|
| Native scalar kinds | integer, byte string |
| Types needing a representation decision | none - byte strings are native binary |
| Types served only by a user converter | `bool`, floating-point (`double` / `float` / `decimal`), `char`, `Guid`, `Uri`, `Version`, `TimeSpan`, the date-time types |
| Document root | any value kind |
| `object` / element / document bridges | yes |

Each of the converter-only types maps cleanly onto an integer or a byte string - see [Pattern 5 in Writing converters](converters.md#pattern-5---map-a-type-the-format-cannot-represent) and [Pattern 6 in Using Bencode](using.md#pattern-6---handle-the-kinds-bencode-cannot-represent) for worked bridges.

## See also

- [Writing converters](converters.md) - overriding a built-in, factories, and resolution order.
- [Using Bencode](using.md) - the format walk-through the tables above back up.
- [Mapping attributes](attributes.md) - the declarative layer over the converters.
- [Core concepts](../../../docs/serialization/bencode/concepts.md) - the value-mapping summary in the Bencode vocabulary.
- [Text & Serialization guides](../../topics/text-and-serialization.md) and the [topic overview](../../../docs/topics/text-and-serialization.md).
- API reference - <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1>.
