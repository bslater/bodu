---
title: Bodu.Text — Introduction
---

# Bodu.Text

**Bodu.Text** is the text-encoding helpers namespace of the Bodu suite, shipped in the `Bodu.Core` package. It sits directly on top of the BCL <xref:System.Text.Encoding?displayProperty=nameWithType> contract and adds the ergonomic, allocation-aware surface the BCL leaves out: byte-order-mark detection, span- and UTF-8-friendly transcoding, preamble handling, pooled-buffer conversions, and validation.

Unlike [Bodu.Text.Encoding](../text-encoding/index.md) — which implements binary-to-text codecs such as Base16/32/64/85 — `Bodu.Text` is concerned with character encodings: turning bytes into text and back through `System.Text.Encoding`, correctly and efficiently.

## Namespaces and headline types

### `Bodu.Text`

| Type | Purpose |
|---|---|
| <xref:Bodu.Text.EncodingDetection> | BOM-based heuristics for detecting which `System.Text.Encoding` produced a byte sequence — the five canonical Unicode preambles (UTF-8, UTF-16 LE/BE, UTF-32 LE/BE), with non-allocating `TryDetectByPreamble`. |
| <xref:Bodu.Text.EncodingExtensions> | Span-, UTF-8-, and `IBufferWriter<byte>`-friendly extensions on `System.Text.Encoding` — encode, decode, transcode, preamble inspection, classification, and validation, including pooled and chunked overloads. |
| <xref:Bodu.Text.StringEncodingExtensions> | Encoding-aware extensions on `string` / `ReadOnlySpan<char>` — `ToBytes`, `ToUtf8Bytes`, pooled conversions, preamble emission, and `Try*` write-to-span overloads. |

## Scenarios this library covers

| Scenario | Reach for |
|---|---|
| Detect a file's encoding from its byte-order-mark | `EncodingDetection.TryDetectByPreamble(bytes, out var encoding)` |
| Decode bytes, skipping any leading preamble | `encoding.GetStringSkippingPreamble(bytes)` |
| Convert a string to bytes without intermediate allocations | `value.ToUtf8Bytes()` / `value.GetUtf8BytesPooled()` |
| Transcode between two encodings over spans | `EncodingExtensions` transcode overloads |
| Validate that a byte span is well-formed for an encoding | `EncodingExtensions` validation overloads |

## Where to go next

- **[Bodu.Text.Encoding introduction](../text-encoding/index.md)** — the sibling package for binary-to-text codecs (Base16/32/58/64/85).
- **[Cross-library getting started](../getting-started.md)** — install commands and a minimal `Bodu.Text` sample.
- **[Bodu.Text API reference](xref:Bodu.Text)** — full type-by-type docs.
