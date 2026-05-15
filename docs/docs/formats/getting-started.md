---
title: Bodu.Text.Formats — Getting started
---

# Bodu.Text.Formats — Getting started

Unfamiliar with terms like *framed format*, *value tree*, *byte string*, *canonical encoding*, or *framing token*? Read [Core concepts](concepts.md) first.

## Install

```bash
dotnet add package Bodu.Text.Formats
```

Targets `net8.0`. The package depends on `Bodu.Core` for shared throw-helpers and on `Bodu.Text.Encoding` for the embedded `Base16` helpers used in test vectors; no other NuGet references.

## Minimal samples

### Decode a torrent file

```csharp
using Bodu.Text.Formats;

byte[] payload = File.ReadAllBytes("ubuntu.iso.torrent");

BencodedValue root = Bencode.Decode(payload);
BencodedDictionary doc = (BencodedDictionary)root;

string tracker = ((BencodedString)doc["announce"]).GetUtf8String();
BencodedDictionary info = (BencodedDictionary)doc["info"];
string name = ((BencodedString)info["name"]).GetUtf8String();
long pieceLength = ((BencodedInteger)info["piece length"]).Value;
```

The cast pattern is intentional — a bencoded document is dynamically typed (the same key can hold different kinds in different documents), so the consumer projects each field to its expected type. Use `switch (value.Kind)` for safer dispatch:

```csharp
foreach (var kvp in info.GetOrderedItems())
{
    switch (kvp.Value.Kind)
    {
        case BencodedValueKind.Integer:    Console.WriteLine($"{kvp.Key}: {((BencodedInteger)kvp.Value).Value}"); break;
        case BencodedValueKind.String:     Console.WriteLine($"{kvp.Key}: <{((BencodedString)kvp.Value).Length} bytes>"); break;
        case BencodedValueKind.List:       Console.WriteLine($"{kvp.Key}: list[{((BencodedList)kvp.Value).Count}]"); break;
        case BencodedValueKind.Dictionary: Console.WriteLine($"{kvp.Key}: dict[{((BencodedDictionary)kvp.Value).Count}]"); break;
    }
}
```

### Decode without throwing

```csharp
using Bodu.Text.Formats;

if (Bencode.TryDecode(input, out BencodedValue? value, out int consumed))
{
    // value is non-null and 'consumed' bytes were read
}
else
{
    // input was malformed — show an error
}
```

`TryDecode` succeeds when the parser reads exactly one well-formed value. `consumed` records how many bytes that value occupied. The throwing `Decode(ReadOnlySpan<byte>)` overload additionally rejects trailing bytes; `TryDecode` does not, leaving it to the caller to decide whether more values may follow.

### Build and encode a small document

```csharp
using Bodu.Text.Formats;

var info = new BencodedDictionary([
    new KeyValuePair<BencodedString, BencodedValue>(
        BencodedString.FromUtf8("length"),
        new BencodedInteger(1024)),
    new KeyValuePair<BencodedString, BencodedValue>(
        BencodedString.FromUtf8("name"),
        BencodedString.FromUtf8("hello.bin")),
]);

var doc = new BencodedDictionary([
    new KeyValuePair<BencodedString, BencodedValue>(
        BencodedString.FromUtf8("announce"),
        BencodedString.FromUtf8("https://tracker.example/announce")),
    new KeyValuePair<BencodedString, BencodedValue>(
        BencodedString.FromUtf8("info"),
        info),
]);

byte[] encoded = Bencode.Encode(doc);
```

The constructor accepts items in any order — `BencodedDictionary` stores them in raw byte-ordinal key order using <xref:Bodu.Text.Formats.BencodedStringComparer.Ordinal>, so `Encode` emits the canonical key ordering regardless of how the document was assembled.

### Pre-size the destination

```csharp
using Bodu.Text.Formats;

int size = Bencode.GetEncodedLength(doc);  // exact, not an upper bound

byte[] buffer = new byte[size];
bool ok = Bencode.TryEncode(doc, buffer, out int written);
// ok == true, written == size
```

`GetEncodedLength` does the same recursive walk as the encoder but only sums byte counts. It throws `OverflowException` if the encoded length would exceed `int.MaxValue`.

### Encode and decode streams

```csharp
using Bodu.Text.Formats;

// Write canonically to a stream.
using (FileStream fs = File.Create("doc.bencode"))
{
    Bencode.Encode(doc, fs);
}

// Read it back.
using (FileStream fs = File.OpenRead("doc.bencode"))
{
    BencodedValue root = Bencode.Decode(fs);
}
```

The stream overloads stage to an `ArrayPool<byte>` buffer sized exactly to `GetEncodedLength`. The stream is **not** closed by the codec — the caller's `using` block owns its lifetime. For async I/O use `Bencode.EncodeAsync` / `Bencode.DecodeAsync`:

```csharp
await using FileStream fs = File.OpenRead("doc.bencode");
BencodedValue root = await Bencode.DecodeAsync(fs, cancellationToken);
```

### Look up a key by UTF-8 text

```csharp
using Bodu.Text.Formats;

if (info.TryGetValue("piece length", out BencodedValue pieceLengthValue))
{
    long pieceLength = ((BencodedInteger)pieceLengthValue).Value;
}
```

The `TryGetValue(string)` overload converts the UTF-8 key to a `BencodedString` internally and dispatches through the same byte-ordered lookup as `TryGetValue(BencodedString)`.

### Validate a payload before processing

```csharp
using Bodu.Text.Formats;

try
{
    BencodedValue value = Bencode.Decode(networkPayload);
    Process(value);
}
catch (BencodeFormatException ex)
{
    log.Warn("Malformed bencode at byte offset {Offset}: {Message}", offset, ex.Message);
}
```

`BencodeFormatException` derives from `FormatException` and carries an English-language message that identifies the exact failure mode — *Unterminated bencoded dictionary*, *Bencoded dictionary keys must be unique and sorted by raw byte order*, *The bencoded string length exceeds the available input*, and so on. See [Core concepts](concepts.md#format-exception) for the full list.

## Round-trip example

```csharp
using Bodu.Text.Formats;

var original = new BencodedList([
    new BencodedInteger(-1024),
    BencodedString.FromUtf8("spam"),
    new BencodedDictionary([
        new KeyValuePair<BencodedString, BencodedValue>(
            BencodedString.FromUtf8("k"),
            new BencodedInteger(0)),
    ]),
]);

byte[] encoded = Bencode.Encode(original);
// encoded == "li-1024e4:spamd1:ki0eee"u8.ToArray()

BencodedValue decoded = Bencode.Decode(encoded);
byte[] reEncoded = Bencode.Encode(decoded);

Debug.Assert(encoded.SequenceEqual(reEncoded));   // round-trip is bit-exact
```

The library passes every positive Known Answer Test vector from BEP 3 in both directions, and rejects every negative vector with a `BencodeFormatException`. See [`BencodeTests.KnownAnswerVectors`](https://github.com/bslater/bodu) for the catalogue.

## Where to go next

- **[Bodu.Text.Formats guides](../../guides/formats/index.md)** — per-API deep dives.
- **[Core concepts](concepts.md)** — vocabulary refresher.
- **[Introduction](index.md)** — type map and scenario index.
