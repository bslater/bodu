# Bodu.Text.Bencode

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

A Bencode (BEP 3) serializer for .NET 8. It maps plain CLR objects to and from Bencode through a configurable converter model, over a low-level, forward-only token reader and writer.

## Installation

```shell
dotnet add package Bodu.Text.Bencode
```

Targets `net8.0`.

## API shape

The public surface layers a high-level serializer, two document object models, and a low-level token reader/writer pair:

| Type(s) | Namespace | Role |
|---|---|---|
| `BencodeSerializer` | `Bodu.Text.Bencode` | Static entry point: `Serialize` / `Deserialize` between CLR objects and Bencode. |
| `BencodeSerializerOptions` | `Bodu.Text.Bencode` | Serializer configuration: converters, naming policy, ignore conditions, depth. |
| `Utf8BencodeReader` / `Utf8BencodeWriter` | `Bodu.Text.Bencode` | Forward-only, allocation-free `ref struct` token reader and writer. |
| `BencodeTokenType` | `Bodu.Text.Bencode` | Classifies the token the reader is positioned on. |
| `BencodeNamingPolicy` | `Bodu.Text.Bencode` | Converts member names to wire keys (for example camel case). |
| `BencodeConverter<T>` / `BencodeConverterFactory` | `Bodu.Text.Bencode.Serialization` | Custom per-type read/write logic plugged into the serializer. |
| `[BencodePropertyName]` / `[BencodeIgnore]` | `Bodu.Text.Bencode.Serialization` | Per-member attributes controlling wire names and inclusion. |
| `BencodeDocument` / `BencodeElement` | `Bodu.Text.Bencode.Document` | Read-only, low-allocation document object model. |
| `BencodeNode` / `BencodeObject` / `BencodeArray` / `BencodeValue` | `Bodu.Text.Bencode.Nodes` | Mutable document object model: parse, edit, write back. |

```csharp
using Bodu.Text.Bencode;

byte[] payload = BencodeSerializer.Serialize(new TorrentInfo { Name = "ubuntu.iso", Length = 1024 });
TorrentInfo info = BencodeSerializer.Deserialize<TorrentInfo>(payload);
```

- `Serialize` / `Deserialize` over `byte[]`, `ReadOnlySpan<byte>`, `IBufferWriter<byte>`, and `Stream` (synchronous and asynchronous), plus `SerializeToNode`, `SerializeToDocument`, and `Deserialize(BencodeNode)` bridges into the two DOMs.
- Output is always canonical Bencode: dictionary entries are emitted in ascending bytewise key order, and the writer rejects duplicate keys and (by default) a second root value.
- Strings and `byte[]` map to byte strings, the integer family to `i…e`, and enums to member-name byte strings. Types with no canonical Bencode form (Booleans, floating-point, date-times) require a registered `BencodeConverter<T>`; a `null` member is omitted on write.
- `Utf8BencodeReader` and `Utf8BencodeWriter` expose the low-level token surface directly for callers that do not want POCO mapping, including `ValueTextEquals`, `CopyString`, `TokenStartIndex`, the width-checked integer accessors, `WriteRawValue`, and the combined property-and-value overloads (`writer.WriteInteger("length", 42)`). By default the reader accepts only canonical BEP 3 (no leading or negative zeros, ascending unique dictionary keys, a single root with no trailing bytes).
- `BencodeElement.GetRawBytes()` returns a value's exact encoded slice — for example the `info` dictionary of a torrent, whose SHA-1 is the info-hash — and `WriteRawValue` re-emits such slices verbatim.

## Contracts and limits

**Integers.** BEP 3 integers are arbitrary-precision; this library supports the range [`long.MinValue`, `ulong.MaxValue`] on every surface. Values in (`long.MaxValue`, `ulong.MaxValue`] are readable through `Utf8BencodeReader.GetUInt64`, `BencodeElement.GetUInt64`, and `GetValue<ulong>()` on nodes, and writable through the `ulong` overload of `WriteInteger` and `BencodeValue.Create(ulong)`; anything outside the supported range is rejected with `BencodeFormatException`. Arbitrary-precision (`BigInteger`) values are not supported.

**Byte strings are bytes, not text.** `GetString` accessors (reader, element, node) and `string`-typed members decode as UTF-8 and substitute U+FFFD for invalid sequences. Binding a binary field — such as a torrent's `pieces` — to a `string` silently corrupts it; map binary content to `byte[]` (or read `ValueSpan` / `GetBytes`), which is always lossless.

**Nesting depth.** `Utf8BencodeReader`, `Utf8BencodeWriter`, and `BencodeDocument` default to a maximum depth of 256; `BencodeSerializerOptions.MaxDepth` defaults to 64 because the serializer is the typical entry point for untrusted input. All four are configurable.

**Single root.** A Bencode document is a single value. The reader rejects trailing bytes, and the writer rejects a second top-level value unless `BencodeWriterOptions.AllowMultipleRootValues` opts into concatenated-value framings.

**Lenient reading of real-world documents.** Older encoders occasionally emit unsorted or duplicate dictionary keys. `AllowUnsortedKeys` and `AllowDuplicateKeys` — available on `BencodeReaderOptions`, `BencodeDocumentOptions`, and `BencodeSerializerOptions` — relax those two rules independently while everything else stays strict. With duplicates permitted, the document model returns the first occurrence from name lookups (enumeration shows every pair), while the node tree and the serializer bind last-wins. Writing is always strict.

**Exceptions.** Failures are split by cause: `BencodeFormatException` (a `FormatException`, carrying the byte `Offset`) reports malformed input, and `BencodeSerializationException` reports values or documents that cannot be mapped. Catch both when handling should not distinguish the cause.

**Property-name matching.** `BencodeSerializerOptions.PropertyNameCaseInsensitive` defaults to `true`, so reads bind wire keys to members leniently. Wire keys themselves are raw bytes and case-sensitive; output never changes case.

## Runnable samples

The repository ships an offline, `dotnet run`-able sample for this package — a real
BitTorrent metainfo file read, verified, and re-authored end to end (DOM inspection,
canonical byte-exact round trips, the raw-slice info-hash, typed POCO mapping) — under
[`samples/Text.Bencode/`](https://github.com/bslater/bodu/tree/master/samples/Text.Bencode).

## Testing

Tests live in `test/` as MSTest classes mirroring `src/`. Run tiers via the runsettings files at the solution root:

```bash
dotnet test Bodu.Text.Bencode/test/Bodu.Text.Bencode.Test.csproj --settings bvt.runsettings
dotnet test Bodu.Text.Bencode/test/Bodu.Text.Bencode.Test.csproj --settings regression.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
