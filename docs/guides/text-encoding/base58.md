---
title: Using Base58
---

# Using Base58

`Base58` is unlike the other encodings in this library: its radix (58) is **not a power of two**, so it cannot use
the bit-stream technique that Base16, Base32, and Base64 share. Instead the implementation treats the input as a
big-integer and repeatedly divides by 58 to extract digits.

Base58's defining feature is that its alphabet excludes the four visually ambiguous characters `0`, `O`, `I`, and
`l`. This makes it suitable for any context where a user might transcribe the encoded form by hand — Bitcoin
addresses, IPFS CIDs, Solana / Stellar identifiers, Flickr short URLs.

```
input bytes        : 00 EB 15 23 1D FC EB 60 92 58 86 B6 7D 06 52 99 92 59 15 AE B1 72 C0 66 47
                                                                                (Bitcoin payload)
                     ┌── leading 0x00 → leading '1' character ──┐
                     │  remaining 24 bytes → big-integer encode │
encoded text       : 1NS17iag9jJgTHD1VXjvLCEnZuQ3rJDE9L          (Bitcoin/Flickr)
```

## Quick reference

```csharp
using Bodu.Text.Encoding;

byte[] payload = Convert.FromHexString("00EB15231DFCEB60925886B67D065299925915AEB172C06647");

// Bitcoin/Flickr alphabet (default)
string address = Base58.Encode(payload);
// "1NS17iag9jJgTHD1VXjvLCEnZuQ3rJDE9L"

// Decode back
byte[] back = Base58.Decode(address);

// Ripple alphabet (XRP ledger)
string rippled = Base58.Encode(payload, Base58Variant.Ripple);
```

## Variant comparison

| Variant | Alphabet ordering | Used by |
|---|---|---|
| `BitcoinFlickr` (default) | Digits ascending, then upper-case letters (minus `O`, `I`), then lower-case letters (minus `l`) | Bitcoin, IPFS, Solana, NEAR, Stellar, Flickr |
| `Ripple` | Permuted ordering specific to the XRP ledger | Ripple / XRP |

Both variants use the same 58 characters — only the *order* differs, so a string encoded under one variant
decodes to different bytes under the other.

## Leading zeros

Because Base58 uses big-integer arithmetic, leading zero bytes would normally be lost. The Bitcoin convention
preserves them as **leading `1` characters** in the encoded form (where `1` is `alphabet[0]`):

| Input bytes | Encoded |
|---|---|
| `0x00` | `1` |
| `0x00 0x00` | `11` |
| `0x00 0x01` | `12` |
| `0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00` | `1111111111` |

This is the rule that makes Bitcoin P2PKH addresses (which always start with the mainnet version byte `0x00`)
always start with `1` in their encoded form.

```csharp
// Round trip preserves leading zero bytes exactly:
byte[] withZeros = new byte[] { 0x00, 0x00, 0xDE, 0xAD };
string encoded = Base58.Encode(withZeros);  // "115Q"
byte[] back = Base58.Decode(encoded);       // back.SequenceEqual(withZeros)
```

## Excluded characters

The decoder rejects the four visually ambiguous characters that the alphabet intentionally omits:

```csharp
Base58.Decode("0Ajdvzr");  // FormatException — '0' (zero) excluded
Base58.Decode("OAjdvzr");  // FormatException — 'O' (capital O) excluded
Base58.Decode("IAjdvzr");  // FormatException — 'I' (capital I) excluded
Base58.Decode("lAjdvzr");  // FormatException — 'l' (lower L) excluded
```

This is intentional — a user transcribing `1NS17` might write `lNS17` or `INS17`, and the decoder rejecting those
inputs catches the typo at the input boundary rather than silently producing different bytes.

## Lenient parsing

| Flag | Effect |
|---|---|
| `BaseFormatStyles.IgnoreWhitespace` | Strip ASCII space / tab / CR / LF anywhere |
| `BaseFormatStyles.AllowPrefix` | No-op for Base58 |
| `BaseFormatStyles.AllowMissingPadding` | No-op for Base58 (no padding character) |

```csharp
// User-pasted address with stray spaces — strip and decode
byte[] payload = Base58.Decode(
    "  1NS17iag9jJgTHD1VXjvLCEnZuQ3rJDE9L  ",
    Base58Variant.BitcoinFlickr,
    BaseFormatStyles.IgnoreWhitespace);
```

## Span path

```csharp
// Predict an upper bound (exact length is data-dependent for Base58)
int maxChars = Base58.GetMaxEncodedLength(payload.Length);
char[] buffer = new char[maxChars];

int written = Base58.Encode(payload, buffer);
ReadOnlySpan<char> result = buffer.AsSpan(0, written);
```

Base58 uses big-integer arithmetic, so there is no `OperationStatus` streaming path: each operation requires the
entire input to be available. The `EncodeToUtf8` / `DecodeFromUtf8` methods exist for API consistency with the
other encodings but behave as a single-shot transform.

## Validation and sizing

```csharp
Base58.IsValid("1NS17iag9jJgTHD1VXjvLCEnZuQ3rJDE9L");  // true
Base58.IsValid("0NS17iag");                            // false ('0' excluded)
Base58.IsBase58Digit('1');                              // true
Base58.IsBase58Digit('0');                              // false

Base58.GetMaxEncodedLength(25);  // ≈ 35 (upper bound)
Base58.GetMaxDecodedLength(34);  // ≈ 25 (upper bound)
```

Exact encoded length depends on the actual data (specifically the leading-zero count and the magnitude of the
non-zero portion), so the library only exposes `GetMaxEncodedLength` / `GetMaxDecodedLength`. Code that needs the
exact length can call `Encode` / `Decode` and use the result's length.

## Common patterns

### Bitcoin P2PKH address payload extraction

```csharp
// payload = [version byte][20-byte HASH160][4-byte checksum]
byte[] payload = Base58.Decode(address);

byte version       = payload[0];
ReadOnlySpan<byte> hash160 = payload.AsSpan(1, 20);
ReadOnlySpan<byte> checksum = payload.AsSpan(21, 4);
```

(The example above decodes the raw payload and leaves checksum verification to the caller. For checksum-protected
payloads, prefer <xref:Bodu.Text.Encoding.Base58Check> below, which appends and verifies the 4-byte double-hash
checksum for you.)

### Round-trip short ID

```csharp
byte[] randomBytes = RandomNumberGenerator.GetBytes(8);
string shortId = Base58.Encode(randomBytes);  // ≈ 11 characters, no ambiguity
```

## Base58Check — checksum-protected payloads

<xref:Bodu.Text.Encoding.Base58Check> wraps Base58 with the Bitcoin-style 4-byte checksum: `Encode` appends a
truncated double-hash of the payload, and `Decode` verifies it (throwing on a corrupted string) before returning the
original bytes. This is the right entry point for address- and key-style payloads where a single mistyped character
must be rejected rather than silently decoded.

```csharp
using Bodu.Text.Encoding;

string encoded = Base58Check.Encode(payload);                 // payload + checksum, Base58
byte[] decoded = Base58Check.Decode(encoded);                 // verifies, then strips the checksum

bool valid = Base58Check.IsValid(encoded);                    // non-throwing validity probe
```

Like the core type, it offers a span path and explicit sizing, and accepts a `Base58Variant`
(default `BitcoinFlickr`):

```csharp
Span<byte> destination = stackalloc byte[Base58Check.GetMaxDecodedLength(encoded.Length)];
if (Base58Check.TryDecode(encoded, destination, out int written))
    Use(destination[..written]);
```

## Where to go next

- **[Base32 guide](base32.md)** — when you want human-friendly encoding but power-of-two radix.
- **[Base85 guide](base85.md)** — when you need the densest possible ASCII-safe encoding.
- **[`IBinaryEncoding` interface](binary-encodings-interface.md)** — runtime-selected encoding choice.
