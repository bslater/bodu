---
title: Bodu.Text.Encoding — Guides
---

# Bodu.Text.Encoding — Guides

These guides cover the day-to-day use of each binary-to-text encoding the library ships. If you are new to the
package, start with the **[Introduction](../../docs/text-encoding/index.md)** and the
**[Core concepts](../../docs/text-encoding/concepts.md)** pages first — the guides below assume you know the
vocabulary (alphabet, variant, terminal quantum, padding, decoration, OperationStatus).

## At a glance

| Family | Expansion | Variants | Use cases |
|---|---|---|---|
| **[Base16](base16.md)** | 100 % | lower / upper case | Hex dumps, hash digests, low-level inspection |
| **[Base32](base32.md)** | 60 % | Standard, HexExtended, Crockford, Z-Base-32 | TOTP secrets, NSEC3 DNS labels, human-spoken IDs |
| **[Base64](base64.md)** | 33 % | Standard, URL-safe, MIME | MIME / SMTP, JWT, certificates, generic binary-in-text |
| **[Base58](base58.md)** | ≈ 37 % | Bitcoin/Flickr, Ripple | Bitcoin addresses, IPFS CIDs, Solana, Stellar |
| **[Base85](base85.md)** | 25 % | Ascii85 (Adobe), Z85 (ZeroMQ) | PDF / PostScript, ZeroMQ wire keys |
| **[`IBinaryEncoding` interface](binary-encodings-interface.md)** | — | every variant above | Runtime-selected encoding choice (config-driven serializers, plugins) |

## Choosing between encodings

| Need | Pick |
|---|---|
| Compact ASCII transport, no special chars | Base64 (URL-safe) |
| Human-readable, spoken aloud | Base32 Crockford or Z-Base-32 |
| Crypto key / TOTP secret display | Base32 Standard |
| Hex dump for debugging / forensics | Base16 with `InsertSpacing | InsertLineBreaks` |
| Bitcoin / blockchain address | Base58 Bitcoin/Flickr |
| Embedded in PostScript / PDF | Base85 Ascii85 |
| Shell-safe binary key transport | Base85 Z85 |

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
- **[Base85 guide](base85.md)** — Ascii85 vs Z85; the `z` shortcut; partial-group rules.
- **[`IBinaryEncoding` interface](binary-encodings-interface.md)** — runtime-selected encoding pattern.
