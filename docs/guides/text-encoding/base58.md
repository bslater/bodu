---
title: Using Base58
---

# Using Base58

`Base58` is unlike the other encodings in this library: its radix (58) is **not a power of two**, so it cannot use
the bit-stream technique that Base16, Base32, and Base64 share. Instead the implementation treats the input as a
big-integer and repeatedly divides by 58 to extract digits.

Base58's defining feature is that its alphabet excludes the four visually ambiguous characters `0`, `O`, `I`, and
`l`. This makes it suitable for any context where a user might transcribe the encoded form by hand - Bitcoin
addresses, IPFS CIDs, Solana identifiers, Flickr short URLs.

```
input bytes        : 00 62 E9 07 B1 5C BF 27 D5 42 53 99 EB F6 F0 FB 50 EB B8 8F 18 C2 9B 7D 93
                                                                                (Bitcoin payload)
                     ┌── leading 0x00 → leading '1' character ──┐
                     │  remaining 24 bytes → big-integer encode │
encoded text       : 1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa          (Bitcoin alphabet)
```

## Quick reference

```csharp
using Bodu.Text.Encoding;

byte[] payload = Convert.FromHexString("0062E907B15CBF27D5425399EBF6F0FB50EBB88F18C29B7D93");

// Bitcoin alphabet (default)
string address = Base58.Encode(payload);
// "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa" - the genesis block's address

// Decode back
byte[] back = Base58.Decode(address);

// Ripple alphabet (XRP ledger)
string rippled = Base58.Encode(payload, Base58Variant.Ripple);

// Flickr alphabet (lower-case letters before upper-case)
string flickr = Base58.Encode(payload, Base58Variant.Flickr);
```

## Variant comparison

| Variant | Alphabet ordering | Used by |
|---|---|---|
| `BitcoinFlickr` (default) | Digits ascending, then upper-case letters (minus `O`, `I`), then lower-case letters (minus `l`) - the Bitcoin alphabet, despite the member's name | Bitcoin, IPFS, Solana, NEAR |
| `Ripple` | Permuted ordering specific to the XRP ledger | Ripple / XRP |
| `Flickr` | Digits ascending, then lower-case letters (minus `l`), then upper-case letters (minus `O`, `I`) | Flickr short photo URLs |

All three variants use the same 58 characters - only the *order* differs, so a string encoded under one variant
decodes to different bytes under another. `BinaryEncodings.Get("base58-flickr")` returns the `Flickr` variant.

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

<!-- compile -->
```csharp
// Round trip preserves leading zero bytes exactly:
byte[] withZeros = new byte[] { 0x00, 0x00, 0xDE, 0xAD };
string encoded = Base58.Encode(withZeros);  // "11Hwr"
byte[] back = Base58.Decode(encoded);       // back.SequenceEqual(withZeros)
```

## Excluded characters

The decoder rejects the four visually ambiguous characters that the alphabet intentionally omits:

```csharp
Base58.Decode("0Ajdvzr");  // FormatException - '0' (zero) excluded
Base58.Decode("OAjdvzr");  // FormatException - 'O' (capital O) excluded
Base58.Decode("IAjdvzr");  // FormatException - 'I' (capital I) excluded
Base58.Decode("lAjdvzr");  // FormatException - 'l' (lower L) excluded
```

This is intentional - a user transcribing `1A1zP` might write `lA1zP` or `IA1zP`, and the decoder rejecting those
inputs catches the typo at the input boundary rather than silently producing different bytes.

## Lenient parsing

| Flag | Effect |
|---|---|
| `BaseFormatStyles.IgnoreWhitespace` | Strip ASCII space / tab / CR / LF anywhere |
| `BaseFormatStyles.AllowPrefix` | No-op for Base58 |
| `BaseFormatStyles.AllowMissingPadding` | No-op for Base58 (no padding character) |

```csharp
// User-pasted address with stray spaces - strip and decode
byte[] payload = Base58.Decode(
    "  1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa  ",
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

Base58 uses big-integer arithmetic, so there is no streaming decode: each operation requires the entire input to be
available. The `EncodeToUtf8` / `DecodeFromUtf8` methods exist for API consistency with the other encodings, but
`DecodeFromUtf8` takes no `isFinalBlock` parameter and treats its input as complete. To bound that quadratic cost on
untrusted input, `Decode` rejects input longer than 65536 characters with `FormatException` (`TryDecode` and
`IsValid` return `false` for it).

## Validation and sizing

```csharp
Base58.IsValid("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa");  // true
Base58.IsValid("0A1zP1eP");                            // false ('0' excluded)
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

### Encode a GUID as an ambiguity-free token

`Base58` encodes a <xref:System.Guid> directly - a ~22-character token with none of the visually confusable
`0`/`O`/`I`/`l` characters, ideal for an identifier a human might read aloud or transcribe:

```csharp
Guid id = Guid.NewGuid();

string token = Base58.Encode(id);                       // no encode-side options - Base58 has no formatting flags
Guid back    = Base58.DecodeGuid(token);                // FormatException unless it decodes to 16 bytes
bool ok      = Base58.TryDecodeGuid(token, out Guid parsed);
```

The 16 bytes are the GUID's native mixed-endian layout (matching `Guid.TryWriteBytes`), so the round trip is exact.
Because Base58 preserves leading-zero bytes as leading `1` characters, a GUID whose first bytes are zero still
round-trips losslessly.

## Base58Check - checksum-protected payloads

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

- **[Base32 guide](base32.md)** - when you want human-friendly encoding but power-of-two radix.
- **[Base85 guide](base85.md)** - when you need the densest possible ASCII-safe encoding.
- **[`IBinaryEncoding` interface](binary-encodings-interface.md)** - runtime-selected encoding choice.
- **[Text & Serialization guides](../topics/text-and-serialization.md)** - every guide in this topic, across Bodu.Text.Encoding, Bodu.Text.Formats, and the Bencode / TOML serializers.
