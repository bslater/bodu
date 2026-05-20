---
uid: Bodu.Text.Encoding
---

![Bodu.Text.Encoding](~/images/hero-text.svg)

## Purpose

**Bodu.Text.Encoding** is a focused, allocation-conscious library of **binary-to-text encodings**. It implements the five practical radix encodings .NET applications reach for — **Base16**, **Base32**, **Base64**, **Base58**, **Base85** — and gives each the same modern API shape: span- and UTF-8-friendly overloads, `OperationStatus`-returning streaming methods, length-prediction helpers, validation predicates, and a unified <xref:Bodu.Text.Encoding.IBinaryEncoding> interface that lets code select an encoding at runtime.

The package fills two gaps that <xref:System.Convert> and `System.Buffers.Text.Base64` leave open: **variants** the BCL does not cover (base32hex, Crockford Base32, z-base-32, Base58 Bitcoin / Flickr / Ripple, Ascii85, Z85), and **lenient parsing** / **formatting decoration** — `0x` prefix tolerance, whitespace stripping, byte spacing, line breaks every 64 / 76 characters — for the encodings that benefit from them.

For structured binary serialization formats with their own framing grammar (Bencode, INI), see the companion <xref:Bodu.Text.Formats> package.

## Static documentation

- **[Bodu.Text.Encoding introduction](~/docs/text-encoding/index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Text.Encoding core concepts](~/docs/text-encoding/concepts.md)** — vocabulary: alphabet, variant, terminal quantum, padding, shortcut, decoration, `OperationStatus`.
- **[Bodu.Text.Encoding getting started](~/docs/text-encoding/getting-started.md)** — install and minimal samples for each encoding.
- **[Bodu.Text.Encoding guides](~/guides/text-encoding/index.md)** — per-encoding deep dives plus the `IBinaryEncoding` runtime-selection pattern.

## Key types

**Per-encoding static classes (`Bodu.Text.Encoding`)**

- <xref:Bodu.Text.Encoding.Base16> — hexadecimal; 4 bits per symbol; flexible formatting (case, `0x` prefix, line breaks, byte spacing); lenient parsing.
- <xref:Bodu.Text.Encoding.Base32> — 5 bits per symbol; four variants via <xref:Bodu.Text.Encoding.Base32Variant>: Standard (RFC 4648 §6), HexExtended (RFC 4648 §7), Crockford, ZBase32; padding control.
- <xref:Bodu.Text.Encoding.Base64> — 6 bits per symbol; three variants via <xref:Bodu.Text.Encoding.Base64Variant>: Standard (RFC 4648 §4), UrlSafe (RFC 4648 §5), Mime (RFC 2045 with 76-char wrap). Delegates inner conversion to the BCL for SIMD speed.
- <xref:Bodu.Text.Encoding.Base58> — non-power-of-two radix using big-integer divmod; two variants via <xref:Bodu.Text.Encoding.Base58Variant>: BitcoinFlickr (default), Ripple. Preserves leading zeros.
- <xref:Bodu.Text.Encoding.Base85> — 4-byte block → 5 chars; two variants via <xref:Bodu.Text.Encoding.Base85Variant>: Ascii85 (Adobe, with `z` shortcut), Z85 (RFC 32 ZeroMQ; 4-byte alignment).

**Runtime selection**

- <xref:Bodu.Text.Encoding.IBinaryEncoding> — unified contract for runtime-pluggable encoding choice. `Encode`, `Decode`, `GetEncodedLength`, `GetMaxDecodedLength`, `IsValid`.
- <xref:Bodu.Text.Encoding.BinaryEncodings> — pre-configured singleton instances (`Base16`, `Base32`, `Base32Crockford`, `Base64`, `Base64UrlSafe`, `Base58`, `Ascii85`, `Z85`, …) plus the `Get(name)` lookup for configuration-driven selection.
- <xref:Bodu.Text.Encoding.BinaryEncodingExtensions> — fluent extension methods on `byte[]`, `ReadOnlySpan<byte>`, and `string` (`ToBase16String`, `FromBase64String`, …).

**Shared option types**

- <xref:Bodu.Text.Encoding.BaseFormattingOptions> — encode-side flags: `UpperCase`, `InsertLineBreaks`, `IncludePrefix`, `InsertSpacing`, `OmitPadding`.
- <xref:Bodu.Text.Encoding.BaseFormatStyles> — decode-side flags: `AllowPrefix`, `IgnoreWhitespace`, `AllowMissingPadding`.

## Example

```csharp
using Bodu.Text.Encoding;
using System.Buffers;
using System.Security.Cryptography;

// --- Base16: print a hash digest as a formatted hex dump ----------------------
byte[] digest = SHA256.HashData("hello"u8.ToArray());
string dump   = Base16.Encode(digest,
    BaseFormattingOptions.UpperCase
        | BaseFormattingOptions.InsertSpacing
        | BaseFormattingOptions.IncludePrefix);
// dump → "0x2C F2 4D BA ..."

// --- Base64 URL-safe: decode a JWT segment with no padding -------------------
byte[] headerBytes = Base64.Decode(
    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9",
    Base64Variant.UrlSafe,
    BaseFormatStyles.AllowMissingPadding);

// --- Base32 Standard: TOTP secret for a user --------------------------------
byte[] secret  = RandomNumberGenerator.GetBytes(20);
string display = Base32.Encode(secret, Base32Variant.Standard, BaseFormattingOptions.OmitPadding);

// --- Base58 BitcoinFlickr: decode a mainnet P2PKH address -------------------
byte[] payload = Base58.Decode("1NS17iag9jJgTHD1VXjvLCEnZuQ3rJDE9L");

// --- Base85 Ascii85 with the z shortcut -------------------------------------
string ascii85 = Base85.Encode(new byte[] { 0, 0, 0, 0 }, Base85Variant.Ascii85);
// ascii85 → "z"

// --- Runtime selection via IBinaryEncoding ----------------------------------
IBinaryEncoding encoding = BinaryEncodings.Get("base64-urlsafe");
string token = encoding.Encode(secret);
byte[] back  = encoding.Decode(token);

// --- Streaming UTF-8 decode with OperationStatus ----------------------------
OperationStatus status = Base16.DecodeFromUtf8(
    utf8Source,
    outputBuffer,
    out int consumed,
    out int written,
    BaseFormatStyles.None,
    isFinalBlock: true);
```

## Notes

- **ASCII alphabets / UTF-8 byte path.** Every variant's alphabet is pure ASCII, so the UTF-8 byte form is bit-identical to the character form. The `EncodeToUtf8` / `DecodeFromUtf8` overloads are the natural choice when bytes come from a network or file pipeline — they avoid the allocation of an intermediate `string` or `char[]`.
- **`OperationStatus` and streaming.** Base16, Base32, and Base64 expose the `System.Buffers`-style `OperationStatus` return convention used by `System.Buffers.Text.Base64`: `Done`, `DestinationTooSmall`, `InvalidData`, and (streaming only, `isFinalBlock: false`) `NeedMoreData`. This makes them safe to drop into chunked stream pipelines without buffering the entire input.
- **Base58 and Base85 are not streamable.** Base58 needs the entire input for its big-integer divmod; Base85 needs fixed-size block packing. Both surfaces accept `isFinalBlock` for API consistency but ignore the flag — pass the entire input as a single span.
- **Stateless encodings.** Every encoding type is a `static class` with no instance state. There is no lookup-table cache to share across instances (encodings are stateless), no thread-affinity, and no global configuration. The runtime-selection types in <xref:Bodu.Text.Encoding.BinaryEncodings> are pre-configured singletons over the same static APIs.
- **Decoder strictness.** Decoders are **strict** by default — only the canonical alphabet, padding, and quantum length are accepted — and **lenient** when one or more <xref:Bodu.Text.Encoding.BaseFormatStyles> flags are set: `AllowPrefix` accepts a `0x` / `0X` prefix; `IgnoreWhitespace` strips ASCII space, tab, CR, LF anywhere; `AllowMissingPadding` accepts inputs without trailing `=`. `Decode` throws `FormatException` on validation failure; `TryDecode` returns `false`.
- **Determinism and portability.** Encoders are deterministic — given a byte sequence and an option set they always produce the same canonical output across platforms and architectures. The `Pearson` family's permutation tables are bit-identical to the published references; the same applies to the BCL-delegated Base64 path.
- **No coupling to other Bodu packages at the consumer surface.** The only dependency is `Bodu.Core` for shared throw helpers. The package has no external NuGet references.
- **See also:** the [introduction](~/docs/text-encoding/index.md), [core concepts](~/docs/text-encoding/concepts.md), and [getting-started](~/docs/text-encoding/getting-started.md); the per-encoding guides under [Bodu.Text.Encoding guides](~/guides/text-encoding/index.md); and the [`IBinaryEncoding` interface](~/guides/text-encoding/binary-encodings-interface.md) for runtime selection.
