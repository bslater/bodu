---
title: Encoding helpers and BOM detection
---

# Encoding helpers and BOM detection

Separate from the binary radix encodings (Base16–Base85, in `Bodu.Text.Encoding`), the **`Bodu.Text`** library ships a set of helpers for working with the BCL <xref:System.Text.Encoding> itself: zero-ceremony `string`↔`byte[]` conversion, allocation-conscious (pooled / owned-memory) surfaces, byte-order-mark (BOM / preamble) handling, UTF classification, fallback configuration, and chunked transcoding. These are the everyday utilities that make `System.Text.Encoding` pleasant to use without hand-rolling preamble logic or `ArrayPool<byte>` plumbing.

Three types make up the surface:

- <xref:Bodu.Text.StringEncodingExtensions> — extension methods on `string`.
- <xref:Bodu.Text.EncodingExtensions> — extension methods on `System.Text.Encoding`.
- <xref:Bodu.Text.EncodingDetection> — static BOM-sniffing.

## Converting strings to bytes

`StringEncodingExtensions` removes the `encoding.GetBytes(...)` boilerplate and adds UTF-8 fast paths and preamble-aware variants:

```csharp
using Bodu.Text.Encoding;

byte[] utf8        = "héllo".ToUtf8Bytes();                       // UTF-8, no BOM
byte[] utf16       = "héllo".ToBytes(System.Text.Encoding.Unicode);
byte[] withBom     = "héllo".ToBytesWithPreamble(System.Text.Encoding.Unicode); // BOM prepended

int byteCount      = "héllo".GetUtf8ByteCount();                  // size without allocating
```

For hot paths, encode straight into a caller-supplied span or an `IBufferWriter<byte>`, or rent the buffer from the pool:

```csharp
Span<byte> destination = stackalloc byte[64];
int written = "héllo".EncodeUtf8To(destination);

if ("héllo".TryEncodeUtf8To(destination, out int n)) { /* … */ }

"héllo".WriteUtf8To(bufferWriter);                    // append to a pipeline

using PooledBufferBuilder<byte> pooled = "héllo".GetUtf8BytesPooled();   // ArrayPool-backed
```

## Byte-order marks (preambles)

`EncodingExtensions` centralizes BOM handling so you never slice preamble bytes by hand:

```csharp
System.Text.Encoding enc = System.Text.Encoding.UTF8;

bool hasBom   = enc.HasPreamble();
bool startsBom = enc.StartsWithPreamble(bytes);
ReadOnlySpan<byte> body = enc.StripPreamble(bytes);          // bytes minus any leading BOM
string text   = enc.GetStringSkippingPreamble(bytes);        // decode, ignoring a leading BOM
```

To detect the encoding *from* the bytes, use <xref:Bodu.Text.EncodingDetection>:

```csharp
System.Text.Encoding chosen =
    EncodingDetection.TryDetectByPreamble(bytes, out System.Text.Encoding? detected)
        ? detected
        : System.Text.Encoding.UTF8;     // sensible default when there is no BOM
```

## Classifying an encoding

```csharp
enc.IsUtf8();                 // true
enc.IsAscii();
enc.IsAnyUtf();               // UTF-8 / UTF-16 / UTF-32, either endianness
enc.IsUtf16LittleEndian();
enc.IsUtf32BigEndian();
```

## Fallback configuration

Switch an encoding between *throwing* on invalid data and *replacing* it, without mutating a shared instance:

```csharp
System.Text.Encoding strict  = System.Text.Encoding.UTF8.WithExceptionFallbacks();   // throws on malformed input
System.Text.Encoding lenient = System.Text.Encoding.UTF8.WithReplacementFallbacks(); // substitutes U+FFFD
```

## Allocation-conscious transcoding

For large or streamed payloads, the owned-memory and chunked surfaces avoid intermediate arrays:

```csharp
using IMemoryOwner<byte> owner = enc.GetBytesOwner(chars);    // caller disposes
using PooledBufferBuilder<char> chars = enc.GetCharsPooled(bytes);

// Incremental transcoding with explicit backpressure.
OperationStatus status = enc.EncodeChunk(charSpan, byteDestination, isFinal: true,
                                         out int charsRead, out int bytesWritten);
```

`EncodeChunk` / `DecodeChunk` return an <xref:System.Buffers.OperationStatus> (`Done`, `DestinationTooSmall`, `NeedMoreData`, `InvalidData`) so a caller can drive a pull-based loop and grow the destination as needed.

## API summary

| Type | Highlights |
|---|---|
| <xref:Bodu.Text.StringEncodingExtensions> | `ToUtf8Bytes` / `ToBytes` / `ToBytesWithPreamble`, `EncodeUtf8To` / `TryEncodeUtf8To`, `WriteUtf8To`, `GetUtf8ByteCount`, pooled variants. |
| <xref:Bodu.Text.EncodingExtensions> | `HasPreamble` / `StripPreamble` / `StartsWithPreamble` / `GetStringSkippingPreamble`, `IsUtf8` / `IsAscii` / `IsAnyUtf` / endianness checks, `WithExceptionFallbacks` / `WithReplacementFallbacks`, owned / pooled buffers, `EncodeChunk` / `DecodeChunk`. |
| <xref:Bodu.Text.EncodingDetection> | `TryDetectByPreamble` — identify an encoding from a leading BOM. |

## Where to go next

- [The IBinaryEncoding interface](binary-encodings-interface.md) — the binary-encoding contract (Base16–Base85).
- [Bodu.Text.Encoding overview](index.md) — the full namespace map.
- [Bodu.Text.Encoding API reference](xref:Bodu.Text.Encoding) — full namespace overview.
