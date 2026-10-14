---
title: Encoding helpers and BOM detection
---

# Encoding helpers and BOM detection

Separate from the binary radix encodings (Base16-Base85, in `Bodu.Text.Encoding`), the **Bodu.Core** package ships, in the `Bodu.Text` namespace, a set of helpers for working with the BCL <xref:System.Text.Encoding> itself: zero-ceremony `string`↔`byte[]` conversion, allocation-conscious (pooled / owned-memory) surfaces, byte-order-mark (BOM / preamble) handling, UTF classification, fallback configuration, and chunked transcoding. These are the everyday utilities that make `System.Text.Encoding` pleasant to use without hand-rolling preamble logic or `ArrayPool<byte>` plumbing.

Three types make up the surface:

- <xref:Bodu.Text.StringEncodingExtensions> - extension methods on `string`.
- <xref:Bodu.Text.EncodingExtensions> - extension methods on `System.Text.Encoding`.
- <xref:Bodu.Text.EncodingDetection> - static BOM-sniffing.

## Converting strings to bytes

`StringEncodingExtensions` removes the `encoding.GetBytes(...)` boilerplate and adds UTF-8 fast paths and preamble-aware variants:

```csharp
using Bodu.Text;

byte[] utf8        = "héllo".ToUtf8Bytes();                       // UTF-8, no BOM
byte[] utf16       = "héllo".ToBytes(System.Text.Encoding.Unicode);
byte[] withBom     = "héllo".ToBytesWithPreamble(System.Text.Encoding.Unicode); // BOM prepended

int byteCount      = "héllo".GetUtf8ByteCount();                  // size without allocating
```

For hot paths, encode straight into a caller-supplied span or an `IBufferWriter<byte>`, or rent the buffer from the pool:

```csharp
using System.Buffers;
using Bodu.Buffers;
using Bodu.Text;

Span<byte> destination = stackalloc byte[64];
int written = "héllo".EncodeUtf8To(destination);

if ("héllo".TryEncodeUtf8To(destination, out int n)) { /* … */ }

var bufferWriter = new ArrayBufferWriter<byte>();
"héllo".WriteUtf8To(bufferWriter);                    // append to a pipeline

using PooledBufferBuilder<byte> pooled = "héllo".GetUtf8BytesPooled();   // ArrayPool-backed (Bodu.Buffers)
```

## Byte-order marks (preambles)

`EncodingExtensions` centralizes BOM handling so you never slice preamble bytes by hand:

```csharp
using Bodu.Text;

System.Text.Encoding enc = System.Text.Encoding.UTF8;
byte[] bytes = [0xEF, 0xBB, 0xBF, (byte)'h', (byte)'i'];   // a UTF-8 BOM, then "hi"

bool hasBom   = enc.HasPreamble();
bool startsBom = enc.StartsWithPreamble(bytes);
ReadOnlySpan<byte> body = enc.StripPreamble(bytes);          // bytes minus any leading BOM
string text   = enc.GetStringSkippingPreamble(bytes);        // decode, ignoring a leading BOM
```

To detect the encoding *from* the bytes, use <xref:Bodu.Text.EncodingDetection>:

```csharp
using Bodu.Text;

byte[] bytes = File.ReadAllBytes("input.txt");

System.Text.Encoding chosen =
    EncodingDetection.TryDetectByPreamble(bytes, out System.Text.Encoding? detected)
        ? detected
        : System.Text.Encoding.UTF8;     // sensible default when there is no BOM
```

## Classifying an encoding

```csharp
using Bodu.Text;

System.Text.Encoding enc = System.Text.Encoding.UTF8;

enc.IsUtf8();                 // true
enc.IsAscii();
enc.IsAnyUtf();               // UTF-8 / UTF-16 / UTF-32, either endianness
enc.IsUtf16LittleEndian();
enc.IsUtf32BigEndian();
```

## Fallback configuration

Switch an encoding between *throwing* on invalid data and *replacing* it, without mutating a shared instance:

```csharp
using Bodu.Text;

System.Text.Encoding strict  = System.Text.Encoding.UTF8.WithExceptionFallbacks();   // throws on malformed input
System.Text.Encoding lenient = System.Text.Encoding.UTF8.WithReplacementFallbacks(); // substitutes U+FFFD
```

## Allocation-conscious transcoding

For large or streamed payloads, the owned-memory and chunked surfaces avoid intermediate arrays:

```csharp
using System.Buffers;
using Bodu.Buffers;
using Bodu.Text;

System.Text.Encoding enc = System.Text.Encoding.UTF8;
string text = "héllo, wörld";

using IMemoryOwner<byte> owner = enc.GetBytesOwner(text);           // caller disposes
using PooledBufferBuilder<char> chars = enc.GetCharsPooled(owner.Memory.Span);

// Incremental transcoding with explicit backpressure: EncodeChunk extends the stateful Encoder.
System.Text.Encoder encoder = enc.GetEncoder();
Span<byte> byteDestination = stackalloc byte[8];
OperationStatus status = encoder.EncodeChunk(text, byteDestination, flush: true,
                                             out int charsRead, out int bytesWritten);
```

`EncodeChunk` (on <xref:System.Text.Encoder>) and `DecodeChunk` (on <xref:System.Text.Decoder>) return an <xref:System.Buffers.OperationStatus>: `Done` when the whole chunk was converted, or `DestinationTooSmall` when output remains, so a caller can drive a pull-based loop and grow the destination as needed. Invalid data is never reported as a status: the encoder's or decoder's fallback handles it, throwing under the exception fallback.

## API summary

| Type | Highlights |
|---|---|
| <xref:Bodu.Text.StringEncodingExtensions> | `ToUtf8Bytes` / `ToBytes` / `ToBytesWithPreamble`, `EncodeUtf8To` / `TryEncodeUtf8To`, `WriteUtf8To`, `GetUtf8ByteCount`, pooled variants. |
| <xref:Bodu.Text.EncodingExtensions> | `HasPreamble` / `StripPreamble` / `StartsWithPreamble` / `GetStringSkippingPreamble`, `IsUtf8` / `IsAscii` / `IsAnyUtf` / endianness checks, `WithExceptionFallbacks` / `WithReplacementFallbacks`, owned / pooled buffers, `EncodeChunk` / `DecodeChunk`. |
| <xref:Bodu.Text.EncodingDetection> | `TryDetectByPreamble` - identify an encoding from a leading BOM. |

## Where to go next

- [Bodu.Core guides](index.md) - the namespace map of the package this namespace ships in.
- [Bodu.Text introduction](../../docs/text/index.md) - the namespace's surfaces and when to use each.
- [Bodu.Text API reference](xref:Bodu.Text) - full namespace overview.
- [The IBinaryEncoding interface](../text-encoding/binary-encodings-interface.md) - the binary-to-text contract of the separate `Bodu.Text.Encoding` package (Base16 to Base85), for bytes to a printable alphabet rather than to characters.
