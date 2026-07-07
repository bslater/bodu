---
uid: Bodu.Text.Encoding
---

![Bodu.Text.Encoding](~/images/hero-text-encoding.svg)

## Purpose

**Bodu.Text.Encoding** is a focused, allocation-conscious library of **binary-to-text encodings**. The five core radix encodings .NET applications reach for — **Base16**, **Base32**, **Base64**, **Base58**, **Base85** — each carry the same modern API shape: span- and UTF-8-friendly overloads, `OperationStatus`-returning streaming methods, length-prediction helpers, validation predicates, and the unified <xref:Bodu.Text.Encoding.IBinaryEncoding> interface that lets code select an encoding at runtime. Alongside them sit three special-purpose encodings — **Base45** (RFC 9285, for QR-code payloads), **Base62** (GMP-style, for compact identifiers), and **Bech32 / Bech32m** (BIP 173 / 350, checksummed addresses with a human-readable part) — plus the convenience wrappers **Base58Check** and **Base64Url**.

The package fills two gaps that <xref:System.Convert> and `System.Buffers.Text.Base64` leave open: **variants** the BCL does not cover (base32hex, Crockford Base32, z-base-32, Base58 Bitcoin / Flickr / Ripple, Ascii85, Z85, Base45, Base62, Bech32 / Bech32m), and **lenient parsing** / **formatting decoration** — `0x` prefix tolerance, whitespace stripping, byte spacing, line breaks every 64 / 76 characters — for the encodings that benefit from them.

For self-framing document formats (CSV / TSV, DotEnv, INI, TOML), see the companion `Bodu.Text.Formats` package — namespaces <xref:Bodu.Text.Delimited>, <xref:Bodu.Text.DotEnv>, <xref:Bodu.Text.Ini>, and <xref:Bodu.Text.Toml>. For `System.Text.Json`-style object mapping to TOML or Bencode, see the standalone <xref:Bodu.Text.Toml> and <xref:Bodu.Text.Bencode> serializers.

## Static documentation

- **[Bodu.Text.Encoding introduction](~/docs/text-encoding/index.md)** — namespaces, headline types, scenarios.
- **[Bodu.Text.Encoding core concepts](~/docs/text-encoding/concepts.md)** — vocabulary: alphabet, variant, terminal quantum, padding, shortcut, decoration, `OperationStatus`.
- **[Bodu.Text.Encoding getting started](~/docs/text-encoding/getting-started.md)** — install and minimal samples for each encoding.
- **[Bodu.Text.Encoding guides](~/guides/text-encoding/index.md)** — per-encoding deep dives plus the `IBinaryEncoding` runtime-selection pattern.

## Key types

**Core radix encodings (`Bodu.Text.Encoding`)**

- <xref:Bodu.Text.Encoding.Base16> — hexadecimal; 4 bits per symbol; flexible formatting (case, `0x` prefix, line breaks, byte spacing); lenient parsing.
- <xref:Bodu.Text.Encoding.Base32> — 5 bits per symbol; four variants via <xref:Bodu.Text.Encoding.Base32Variant>: Standard (RFC 4648 §6), HexExtended (RFC 4648 §7), Crockford, ZBase32; padding control.
- <xref:Bodu.Text.Encoding.Base64> — 6 bits per symbol; three variants via <xref:Bodu.Text.Encoding.Base64Variant>: Standard (RFC 4648 §4), UrlSafe (RFC 4648 §5), Mime (RFC 2045 with 76-char wrap). Delegates inner conversion to the BCL for SIMD speed.
- <xref:Bodu.Text.Encoding.Base58> — non-power-of-two radix using big-integer divmod; two variants via <xref:Bodu.Text.Encoding.Base58Variant>: BitcoinFlickr (default), Ripple. Preserves leading zeros.
- <xref:Bodu.Text.Encoding.Base85> — 4-byte block → 5 chars; three variants via <xref:Bodu.Text.Encoding.Base85Variant>: Ascii85 (Adobe, with `z` shortcut), Z85 (RFC 32 ZeroMQ; 4-byte alignment), GitCompact (Git `base85.c` alphabet; compact self-delimiting tail plus the `EncodeGitPadded` / `DecodeGitPadded` line primitive).

**Special-purpose encodings**

- <xref:Bodu.Text.Encoding.Base45> — RFC 9285; the compact alphanumeric encoding carried inside a QR code's Alphanumeric mode. 45-character alphabet; no padding; not streamable.
- <xref:Bodu.Text.Encoding.Base62> — GMP-style alphabet `0-9 A-Z a-z`; big-integer divmod; leading zero bytes preserved as leading `0` characters. Suited to short URLs and compact identifiers.
- <xref:Bodu.Text.Encoding.Bech32> — BIP 173 (Bech32) and BIP 350 (Bech32m); a checksummed base-32 format comprising a human-readable part (HRP), the `1` separator, a 5-bit data part, and a six-symbol error-detecting checksum. The <xref:Bodu.Text.Encoding.Bech32Encoding> enum selects the scheme and is reported on decode.
- <xref:Bodu.Text.Encoding.Base58Check> — Base58 with the Bitcoin-style four-byte double-SHA-256 checksum appended on encode and verified on decode. The right entry point for address- and key-style payloads.
- <xref:Bodu.Text.Encoding.Base64Url> — the RFC 4648 §5 URL- and filename-safe Base64 as a first-class type (mirrors `System.Buffers.Text.Base64Url`), with padding omitted by default and a UTF-8 byte path.

**Escape-based encodings**

These escape a subset of octets (`=HH` / `%HH`) while passing most printable ASCII through literally, so their output is content-dependent and mode-driven. They are intentionally not <xref:Bodu.Text.Encoding.IBinaryEncoding> members.

- <xref:Bodu.Text.Encoding.QuotedPrintable> — MIME Quoted-Printable body encoding (RFC 2045 §6.7) with <xref:Bodu.Text.Encoding.QuotedPrintableEncodingMode> (Binary / Text) and <xref:Bodu.Text.Encoding.QuotedPrintableEncodingOptions> (line length, newline); strict-by-default decoding with opt-in lowercase-hex, bare-LF, and trailing-whitespace relaxations via <xref:Bodu.Text.Encoding.QuotedPrintableDecodingOptions>. Not RFC 2047 `Q` header encoding; not a MIME message parser.
- <xref:Bodu.Text.Encoding.PercentEncoding> — URI / form percent-encoding (RFC 3986 §2.1 plus the WHATWG `application/x-www-form-urlencoded` rules) with <xref:Bodu.Text.Encoding.PercentEncodingMode> (UriComponent / PathSegment / Query / FormUrlEncoded) and the <xref:Bodu.Text.Encoding.PercentEncoding.EncodeString*> / <xref:Bodu.Text.Encoding.PercentEncoding.DecodeString*> text helpers; uppercase hex emit, both-case accept, opt-in relaxed literals via <xref:Bodu.Text.Encoding.PercentDecodingOptions>. Not a URL parser.

**Runtime selection**

- <xref:Bodu.Text.Encoding.IBinaryEncoding> — unified contract for runtime-pluggable encoding choice. `Encode`, `Decode`, `GetMaxEncodedLength`, `GetMaxDecodedLength`, `IsValid`, `TryEncode`, `TryDecode`, plus `Name` and `Description`.
- <xref:Bodu.Text.Encoding.BinaryEncodings> — pre-configured singleton instances (`Base16Lower`, `Base16Upper`, `Base32`, `Base32Hex`, `Base32Crockford`, `Base32ZBase32`, `Base45`, `Base58`, `Base58Ripple`, `Base62`, `Base64`, `Base64Mime`, `Base64UrlSafe`, `Ascii85`, `Base85Git`, `Z85`) plus the `Get(name)` lookup for configuration-driven selection. (Bech32 and Base58Check require an HRP / checksum, and the escape-based Quoted-Printable / percent-encoding carry mode information, so none are surfaced through `IBinaryEncoding`.)
- <xref:Bodu.Text.Encoding.BinaryEncodingExtensions> — fluent extension methods on `byte[]`, `ReadOnlySpan<byte>`, and `string` (`ToBase16String`, `FromBase64String`, `ToBase64UrlString`, `Encode(IBinaryEncoding)`, …).

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

// --- Base85 Git: compact, self-delimiting binary-patch alphabet -------------
string gitB85 = Base85.Encode("hello"u8.ToArray(), Base85Variant.GitCompact);   // → "Xk~0{Zv"

// --- Quoted-Printable: MIME message body ------------------------------------
string qp = QuotedPrintable.Encode("café = møney"u8.ToArray());   // printable + =HH escapes
byte[] qpBack = QuotedPrintable.Decode(qp);

// --- Percent-encoding: URI component and form field -------------------------
string component = PercentEncoding.EncodeString("a/b?c=d");                       // → "a%2Fb%3Fc%3Dd"
string formField = PercentEncoding.EncodeString("a b+c", mode: PercentEncodingMode.FormUrlEncoded); // → "a+b%2Bc"

// --- Base45: encode a payload for a QR code (RFC 9285) -----------------------
string qrPayload = Base45.Encode("AB"u8.ToArray());   // → "BB8"

// --- Base62: compact identifier from random bytes ---------------------------
string shortId = Base62.Encode(RandomNumberGenerator.GetBytes(8));

// --- Bech32m: encode raw bytes under a human-readable part ------------------
byte[] program = Base16.Decode("751e76e8199196d454941c45d1b3a323f1433bd6");
string addr = Bech32.EncodeFromBytes("bc", program, Bech32Encoding.Bech32m);   // 8-bit → 5-bit groups
Bech32.DecodeToBytes(addr, out string hrp, out byte[] data, out Bech32Encoding scheme);
// hrp → "bc"; data → program; scheme → Bech32Encoding.Bech32m

// --- Base58Check: checksum-protected payload --------------------------------
string wif = Base58Check.Encode(secret);
byte[] recovered = Base58Check.Decode(wif);            // verifies, then strips checksum

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
- **The special-purpose encodings are single-shot.** Base45, Base62, and Bech32 each require the whole input at once (Base45 packs in two/three-character groups, Base62 uses big-integer divmod, Bech32 must compute a checksum over the entire data part), so they expose no `OperationStatus` streaming path. They throw `FormatException` on invalid input and offer `Try*` variants that report failure as `false`.
- **Bech32 is HRP-aware and checksum-verified.** <xref:Bodu.Text.Encoding.Bech32> encodes and decodes the *whole* string — human-readable part, `1` separator, data, and checksum — rather than a flat byte buffer, which is why it sits outside <xref:Bodu.Text.Encoding.IBinaryEncoding>. Decode reports which scheme (Bech32 vs Bech32m) validated the checksum through an out parameter; the BIP 173 90-character limit is enforced on decode, and `ConvertBits` converts between 8-bit bytes and 5-bit groups. <xref:Bodu.Text.Encoding.Base58Check> likewise verifies its appended four-byte double-SHA-256 checksum on decode and throws on a corrupted string.
- **Stateless encodings.** Every encoding type is a `static class` with no instance state. There is no lookup-table cache to share across instances (encodings are stateless), no thread-affinity, and no global configuration. The runtime-selection types in <xref:Bodu.Text.Encoding.BinaryEncodings> are pre-configured singletons over the same static APIs.
- **Decoder strictness.** Decoders are **strict** by default — only the canonical alphabet, padding, and quantum length are accepted — and **lenient** when one or more <xref:Bodu.Text.Encoding.BaseFormatStyles> flags are set: `AllowPrefix` accepts a `0x` / `0X` prefix; `IgnoreWhitespace` strips ASCII space, tab, CR, LF anywhere; `AllowMissingPadding` accepts inputs without trailing `=`. `Decode` throws `FormatException` on validation failure; `TryDecode` returns `false`.
- **Determinism and portability.** Encoders are deterministic — given a byte sequence and an option set they always produce the same canonical output across platforms and architectures. The `Pearson` family's permutation tables are bit-identical to the published references; the same applies to the BCL-delegated Base64 path.
- **No coupling to other Bodu packages at the consumer surface.** The only dependency is `Bodu.Core` for shared throw helpers. The package has no external NuGet references.
- **Escape-based encodings are not `IBinaryEncoding` members.** <xref:Bodu.Text.Encoding.QuotedPrintable> and <xref:Bodu.Text.Encoding.PercentEncoding> escape only a subset of octets and select behaviour through a mode / options object, so their output length is content-dependent and the parameterless <xref:Bodu.Text.Encoding.IBinaryEncoding> contract cannot represent them. Like the radix encodings they are stateless `static class`es with span-first `Try*` methods that never throw for malformed input or an undersized destination, throwing `Encode` / `Decode` wrappers, and deterministic length helpers. Quoted-Printable guarantees round-trip-safe output (it never emits decoder-rejected trailing whitespace); percent-encoding always emits uppercase hex and accepts both cases on decode.
- **See also:** the [introduction](~/docs/text-encoding/index.md), [core concepts](~/docs/text-encoding/concepts.md), and [getting-started](~/docs/text-encoding/getting-started.md); the per-encoding guides under [Bodu.Text.Encoding guides](~/guides/text-encoding/index.md) — including [Quoted-Printable](~/guides/text-encoding/quoted-printable.md) and [percent-encoding](~/guides/text-encoding/percent-encoding.md); and the [`IBinaryEncoding` interface](~/guides/text-encoding/binary-encodings-interface.md) for runtime selection.
