---
title: Bodu.Text.Encoding — Guides
---

# Bodu.Text.Encoding — Guides

These guides cover the day-to-day use of each binary-to-text encoding the library ships. If you are new to the
package, start with the **[Introduction](../../docs/text-encoding/index.md)** and the
**[Core concepts](../../docs/text-encoding/concepts.md)** pages first — the guides below assume you know the
vocabulary (alphabet, variant, terminal quantum, padding, decoration, OperationStatus).

Part of the **[Text & Serialization](../topics/text-and-serialization.md)** topic.

## How the library works

![Encode and decode pipeline — binary bytes to encoded text and back](../../images/diagrams/encoding-pipeline.svg)

Every encoding follows the same four-stage pipeline — radix conversion, variant transform, optional decoration,
encoded output. The per-encoding guides drill into the stages that vary by family: the bit-stream packing for
Base16 / Base32 / Base64, the big-integer arithmetic for Base58, and the 4-byte block packing for Base85.

## At a glance

| Family | Expansion | Variants | Use cases |
|---|---|---|---|
| **[Base16](base16.md)** | 100 % | lower / upper case | Hex dumps, hash digests, low-level inspection |
| **[Base32](base32.md)** | 60 % | Standard, HexExtended, Crockford, Z-Base-32 | TOTP secrets, NSEC3 DNS labels, human-spoken IDs |
| **[Base64](base64.md)** | 33 % | Standard, URL-safe, MIME | MIME / SMTP, JWT, certificates, generic binary-in-text |
| **[Base58](base58.md)** | ≈ 37 % | Bitcoin/Flickr, Ripple | Bitcoin addresses, IPFS CIDs, Solana, Stellar |
| **[Base85](base85.md)** | 25 % | Ascii85 (Adobe), Z85 (ZeroMQ), GitCompact | PDF / PostScript, ZeroMQ wire keys, Git binary patches |
| **[Base45](base45.md)** | 50 % | RFC 9285 | QR-code payloads, EU Digital COVID Certificate |
| **[Base62](base62.md)** | ≈ 35 % | GMP-style | Short URLs, compact identifiers, slugs |
| **[Bech32](bech32.md)** | data + checksum | Bech32 (BIP 173), Bech32m (BIP 350) | Bitcoin SegWit addresses, Lightning invoices |
| **[`IBinaryEncoding` interface](binary-encodings-interface.md)** | — | the flat-byte encodings above | Runtime-selected encoding choice (config-driven serializers, plugins) |

### Escape-based encodings

`QuotedPrintable` and `PercentEncoding` are not flat-byte radix encodings — they escape a *subset* of octets as `=HH`
or `%HH` while leaving most printable ASCII literal, so their output length depends on the content. They are static
types and intentionally **not** `IBinaryEncoding` members (their modes / options carry information the parameterless
interface cannot express).

| Encoding | Escape form | Modes / options | Use cases |
|---|---|---|---|
| **[Quoted-Printable](quoted-printable.md)** | `=HH` (uppercase) | Binary / Text mode; 76-column soft wrapping; strict-vs-relaxed decode | MIME message bodies (RFC 2045 §6.7) |
| **[Percent-encoding](percent-encoding.md)** | `%HH` (uppercase) | UriComponent / PathSegment / Query / FormUrlEncoded | URI components, query strings, HTML form fields |

## Choosing between encodings

| Need | Pick |
|---|---|
| Compact ASCII transport, no special chars | Base64 (URL-safe) |
| Human-readable, spoken aloud | Base32 Crockford or Z-Base-32 |
| Crypto key / TOTP secret display | Base32 Standard |
| Hex dump for debugging / forensics | Base16 with `InsertSpacing | InsertLineBreaks` |
| Bitcoin / blockchain address (legacy) | Base58 Bitcoin/Flickr |
| Bitcoin SegWit / Lightning address | Bech32 / Bech32m |
| Checksum-protected address or key | Base58Check |
| Binary inside a QR code | Base45 |
| Compact URL-safe identifier or slug | Base62 |
| Embedded in PostScript / PDF | Base85 Ascii85 |
| Shell-safe binary key transport | Base85 Z85 |
| Git binary-patch payload alphabet | Base85 GitCompact |
| MIME message body (mostly-readable text) | Quoted-Printable |
| URI component / query / form field | Percent-encoding |

## API shape recap

Every encoding family follows the same pattern. The bullet list below is the entire public surface — the per-family
guides drill into the variant-specific options:

- **Encode**: `Encode(byte[]/span)` returning `string`, `Encode(byte[], int, int)`, `Encode(span, span)` returning
  `int`, `TryEncode(span, span, out int)` returning `bool`.
- **Decode**: `Decode(string/span)` returning `byte[]`, `Decode(char[], int, int)`,
  `TryDecode(span, span, out int)` returning `bool`.
- **BCL-style aliases**: `ToBase{N}String(...)`, `FromBase{N}String(...)`, `TryToBase{N}String(...)`.
- **UTF-8 path**: `EncodeToUtf8(span)` returning `byte[]`, `TryEncodeToUtf8(span, span, out int)`,
  `DecodeFromUtf8(span, span, out int, out int, …, isFinalBlock)` returning `OperationStatus`.
- **Streaming decode**: `FromBase{N}String(span<char>/<byte>, span<byte>, out int, out int)` returning
  `OperationStatus`.
- **Sizing**: `GetEncodedLength(int)`, `GetMaxEncodedLength(int)` (where exact requires the data),
  `GetMaxDecodedLength(int)`, `GetDecodedLength(span)`, `TryGetDecodedLength(span, out int)`.
- **Validation**: `IsValid(span)`, `IsBase{N}Digit(char)`.

## Where to go next

- **[Base16 guide](base16.md)** — formatting decorations, prefix handling, hex dumps.
- **[Base32 guide](base32.md)** — variants and when to pick each; TOTP / Crockford use cases.
- **[Base64 guide](base64.md)** — Standard / URL-safe / MIME; line wrapping; JWT.
- **[Base58 guide](base58.md)** — leading zeros, big-integer encoding; Bitcoin/IPFS.
- **[Base85 guide](base85.md)** — Ascii85 vs Z85 vs Git; the `z` shortcut; partial-group rules; Git compact and padded modes.
- **[Base45 guide](base45.md)** — RFC 9285; the QR-code payload encoding; group packing and strictness.
- **[Base62 guide](base62.md)** — GMP-style compact identifiers; leading-zero preservation.
- **[Bech32 guide](bech32.md)** — Bech32 / Bech32m; HRP, separator, checksum; 5-bit vs 8-bit data.
- **[Quoted-Printable guide](quoted-printable.md)** — MIME body `=HH` encoding; binary vs text mode; soft wrapping; strict-vs-relaxed decode.
- **[Percent-encoding guide](percent-encoding.md)** — RFC 3986 / WHATWG `%HH` encoding; component modes; form mode; string helpers.
- **[`IBinaryEncoding` interface](binary-encodings-interface.md)** — runtime-selected encoding pattern.
- **[Runnable samples](../../samples/text-encoding.md)** — offline sample projects under `samples/Text.Encoding/`: the catalogue tour, checksummed schemes, the registry, and a custom Base36 codec with contract tests.
- **[Encoding helpers and BOM detection](encoding-helpers.md)** — `System.Text.Encoding` helpers: `string`↔`byte[]` conversion, preamble/BOM handling, UTF classification, fallbacks, and chunked transcoding.
- **[Text & Serialization guides](../topics/text-and-serialization.md)** — every guide in this topic, across Bodu.Text.Encoding, Bodu.Text.Formats, and the Bencode / TOML serializers.
