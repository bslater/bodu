---
uid: Bodu.Text
---

![Bodu.Text](~/images/hero-text.svg)

# Bodu.Text

## Purpose

The **Bodu.Text** namespace ships inside the **`Bodu.Core` package** — it is not a separate library. It holds allocation-conscious helpers for the BCL <xref:System.Text.Encoding> itself: zero-ceremony `string`↔`byte[]` conversion, pooled / owned-memory surfaces, byte-order-mark (BOM / preamble) handling, UTF classification, fallback configuration, and chunked transcoding.

It is also the namespace root that the separately packaged text libraries extend: the binary-to-text *radix* encodings in <xref:Bodu.Text.Encoding>, the include/exclude pattern filtering in <xref:Bodu.Text.Filtering>, and the line-format libraries <xref:Bodu.Text.Delimited>, <xref:Bodu.Text.DotEnv>, and <xref:Bodu.Text.Ini> (see the [line-formats introduction](~/docs/formats/index.md)). None of those types live in this namespace; only the three encoding helpers below do.

## Static documentation

- **[Encoding helpers and BOM detection](~/guides/text-encoding/encoding-helpers.md)** — the `System.Text.Encoding` convenience surface.
- **[Bodu.Text.Formats overview](~/guides/formats/index.md)** — the document-format codecs and their shared pipeline.

## Key types

**`System.Text.Encoding` helpers**

- <xref:Bodu.Text.StringEncodingExtensions> — extension methods on `string` (UTF-8 fast paths, pooled / preamble-aware conversion).
- <xref:Bodu.Text.EncodingExtensions> — extension methods on `System.Text.Encoding` (preamble handling, UTF / ASCII classification, fallback configuration, rented / owned / pooled buffer conversion), on `Encoder` / `Decoder` (chunked `EncodeChunk` / `DecodeChunk` transcoding), and on `ReadOnlySpan<char>` / `ReadOnlySpan<byte>` (`ToBytes`, `ToChars`, `DecodeToString`, `Transcode`, buffer-size guards).
- <xref:Bodu.Text.EncodingDetection> — static BOM-sniffing: `TryDetectByPreamble(ReadOnlySpan<byte>, out Encoding?)`.

## Example

```csharp
using System.Text;
using Bodu.Text;

// Sniff a byte-order mark, then decode without the preamble bytes leaking into the text.
byte[] bytes = File.ReadAllBytes(path);
Encoding encoding = EncodingDetection.TryDetectByPreamble(bytes, out Encoding? detected)
    ? detected
    : Encoding.UTF8;
string text = encoding.GetStringSkippingPreamble(bytes);

// String → bytes without the Encoding ceremony.
byte[] utf8    = "héllo".ToUtf8Bytes();
byte[] latin1  = "héllo".ToBytes(Encoding.Latin1);
int   byteCount = "héllo".GetUtf8ByteCount();
```

## Notes

- **No extra package.** These helpers arrive with `Bodu.Core`; the radix codecs (`Base16` … `Base85`) are the separate `Bodu.Text.Encoding` package.
- **Span-first.** Every conversion has a span, `IBufferWriter<byte>`, or pooled-buffer overload, so the common cases avoid intermediate allocations.
- **See also:** the [encoding helpers guide](~/guides/text-encoding/encoding-helpers.md), the [Bodu.Text introduction](~/docs/text/index.md), and the [Bodu.Core introduction](~/docs/core/index.md).
